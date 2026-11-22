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

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        CapsuleCollider[] bodyColliders;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            bodyColliders = GetComponentsInChildren<CapsuleCollider>(true);
            if (view == null && Camera.main != null) view = Camera.main.transform;
        }

        void OnEnable() => IgnoreOwnCapsules();

        void IgnoreOwnCapsules()
        {
            if (!controller.enabled) return;
            foreach (var body in bodyColliders)
                if (body != null && body.enabled && body.gameObject.activeInHierarchy && !body.isTrigger)
                    Physics.IgnoreCollision(controller, body, true);
        }

        void OnDisable()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        void Update()
        {
            if (!controller.enabled) return;
            if (view != null) transform.rotation = Quaternion.Euler(0f, view.eulerAngles.y, 0f);

            Vector3 target = ReadInput() * speed;
            float rate = target.sqrMagnitude > 0.01f ? acceleration : deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate * Time.deltaTime);

            // Bound the time step after a slow frame, including the first frame after loading.
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt * 60f));
            float step = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                if (controller.isGrounded && verticalVelocity <= 0f)
                    verticalVelocity = groundStick;
                else
                    verticalVelocity += gravity * step;
                CollisionFlags hits = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * step);
                if ((hits & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
                    verticalVelocity = groundStick;
            }
        }

        Vector3 ReadInput()
        {
            var k = Keyboard.current;
            if (k == null) return Vector3.zero;

            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float z = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);

            return Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        }

        public void Warp(Vector3 pos)
        {
            controller.enabled = false;
            transform.position = pos;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            controller.enabled = true;
            Physics.SyncTransforms();
            IgnoreOwnCapsules();
        }
    }
}
