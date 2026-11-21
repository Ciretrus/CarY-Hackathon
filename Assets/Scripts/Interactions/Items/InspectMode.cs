using DG.Tweening;
using Noir.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Interaction
{
    public class InspectMode : PlayerMode
    {
        [SerializeField] Transform inspectPoint;
        [SerializeField] ClueDetector detector;
        [SerializeField] float rotateSpeed = 0.3f;
        [SerializeField] float moveDuration = 0.25f;
        [SerializeField] float takeDuration = 0.2f;

        [SerializeField] Camera inspectCamera;
        [SerializeField] MouseWheelZoom zoom = new();

        Transform item;
        Vector3 startPosition;
        Quaternion startRotation;
        bool taken;

        public void Inspect(Transform target)
        {
            item = target;
            if (!Open()) item = null;
        }

        protected override void OnEnter()
        {
            zoom.Begin(inspectCamera != null ? inspectCamera : Camera.main);
            startPosition = item.position;
            startRotation = item.rotation;
            taken = false;

            item.DOKill();
            item.DOMove(inspectPoint.position, moveDuration).SetEase(Ease.OutQuad);
            detector.Begin(item);
        }

        protected override void Tick()
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame && detector.TryCollect() && !detector.HasRemaining)
            {
                taken = true;
                Close();
                return;
            }

            zoom.Tick(Mouse.current);
            Vector2 delta = Mouse.current.delta.ReadValue() * rotateSpeed;
            item.Rotate(inspectPoint.up, -delta.x, Space.World);
            item.Rotate(inspectPoint.right, delta.y, Space.World);

            detector.Tick();
        }

        protected override void OnExit()
        {
            zoom.End();
            detector.End();
            item.DOKill();

            if (taken)
                Take(item);
            else
                PutBack(item);

            item = null;
        }

        void PutBack(Transform target)
        {
            target.DOMove(startPosition, moveDuration).SetEase(Ease.OutQuad);
            target.DORotateQuaternion(startRotation, moveDuration).SetEase(Ease.OutQuad);
        }

        void Take(Transform target)
        {
            Vector3 scale = target.localScale;
            target.DOScale(0f, takeDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    target.gameObject.SetActive(false);
                    target.localScale = scale;
                });
        }
    }
}
