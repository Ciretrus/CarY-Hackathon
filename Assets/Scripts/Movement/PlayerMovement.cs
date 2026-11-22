using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

            bool groundedBeforeMove = controller.isGrounded;

            if (groundedBeforeMove && verticalVelocity < 0f)
                verticalVelocity = groundStick;
            else
                verticalVelocity += gravity * Time.deltaTime;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            // Используем состояние ДО Move(), так как в Unity isGrounded иногда мигает на склонах
            if (groundedBeforeMove && !controller.isGrounded && verticalVelocity <= 0f)
            {
                SnapToGround();
            }
        }

        void SnapToGround()
        {
            // Немного уменьшаем радиус каста, чтобы не цепляться за вертикальные стены сбоку
            float castRadius = controller.radius * 0.9f;
            float half = (controller.height * 0.5f) - castRadius;
            Vector3 origin = transform.position + controller.center;

            if (Physics.SphereCast(origin, castRadius, Vector3.down, out RaycastHit hit,
                    half + snapDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                // Проверяем, что поверхность не круче допустимого угла подъема
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle <= controller.slopeLimit)
                {
                    float drop = hit.distance - half - controller.skinWidth;
                    if (drop > 0.001f)
                    {
                        controller.Move(Vector3.down * drop);
                    }
                }
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
            wasGrounded = false;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
