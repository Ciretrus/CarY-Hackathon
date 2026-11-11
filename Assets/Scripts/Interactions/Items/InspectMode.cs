using DG.Tweening;
using Noir.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Interaction
{
    public class InspectMode : PlayerMode
    {
        [SerializeField] Transform inspectPoint;
        [SerializeField] float rotateSpeed = 0.3f;
        [SerializeField] float moveDuration = 0.25f;

        Transform item;
        Vector3 startPosition;
        Quaternion startRotation;

        public void Inspect(Transform target)
        {
            item = target;
            if (!Open()) item = null;
        }

        protected override void OnEnter()
        {
            startPosition = item.position;
            startRotation = item.rotation;

            item.DOKill();
            item.DOMove(inspectPoint.position, moveDuration).SetEase(Ease.OutQuad);
        }

        protected override void Tick()
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            Vector2 delta = Mouse.current.delta.ReadValue() * rotateSpeed;
            item.Rotate(inspectPoint.up, -delta.x, Space.World);
            item.Rotate(inspectPoint.right, delta.y, Space.World);
        }

        protected override void OnExit()
        {
            item.DOKill();
            item.DOMove(startPosition, moveDuration).SetEase(Ease.OutQuad);
            item.DORotateQuaternion(startRotation, moveDuration).SetEase(Ease.OutQuad);
            item = null;
        }
    }
}
