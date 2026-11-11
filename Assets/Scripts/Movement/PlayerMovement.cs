using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] Transform view;
        [SerializeField, Range(3f, 5f)] float speed = 4f;
        [SerializeField] float acceleration = 20f;
        [SerializeField] float deceleration = 25f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float groundStick = -2f;
        [SerializeField] float snapDistance = 0.5f;
        [SerializeField] LayerMask groundMask = ~0;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        bool wasGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (view == null) view = Camera.main.transform;
        }

        void OnDisable() => horizontalVelocity = Vector3.zero;

        void Update()
        {
            transform.rotation = Quaternion.Euler(0f, view.eulerAngles.y, 0f);

            Vector3 target = ReadInput() * speed;
            float rate = target.sqrMagnitude > 0.01f ? acceleration : deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundStick;
            else
                verticalVelocity += gravity * Time.deltaTime;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            SnapToGround();
            wasGrounded = controller.isGrounded;
        }

        Vector3 ReadInput()
        {
            var k = Keyboard.current;
            if (k == null) return Vector3.zero;

            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float z = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);

            return Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        }

        void SnapToGround()
        {
            if (!wasGrounded || controller.isGrounded || verticalVelocity > 0f) return;

            float half = controller.height * 0.5f - controller.radius;
            Vector3 origin = transform.position + controller.center;

            if (Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit hit,
                    half + snapDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                controller.Move(Vector3.down * (hit.distance - half + controller.skinWidth));
            }
        }
    }
}
