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
        [SerializeField, Min(0.05f)] float minDistance = 0.5f;
        [SerializeField, Min(0.05f)] float maxDistance = 2.5f;
        [SerializeField, Min(0.01f)] float distanceStep = 0.15f;
        [SerializeField, Min(0f)] float zoomSmoothTime = 0.08f;

        Transform view;
        float distanceOffset;
        float targetOffset;
        float zoomVelocity;
        float enterTime;

        Transform item;
        Vector3 startPosition;
        Quaternion startRotation;
        bool taken;

        public void Inspect(Transform target)
        {
            if (target == null || Modes.Current != null) return;
            item = target;
            if (!Open()) item = null;
        }

        protected override void OnEnter()
        {
            var camera = inspectCamera != null ? inspectCamera : Camera.main;
            view = camera != null ? camera.transform : inspectPoint.parent;
            distanceOffset = targetOffset = zoomVelocity = enterTime = 0f;
            startPosition = item.position;
            startRotation = item.rotation;
            taken = false;

            item.DOKill();
            UpdatePosition(0f);
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

            Vector3 origin = view != null ? view.position : inspectPoint.position - inspectPoint.forward;
            float restingDistance = Vector3.Distance(origin, inspectPoint.position);
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                float requested = restingDistance + targetOffset - Mathf.Sign(scroll) * distanceStep;
                targetOffset = Mathf.Clamp(requested, minDistance, Mathf.Max(minDistance, maxDistance)) - restingDistance;
            }
            UpdatePosition(Time.deltaTime);
            Vector2 delta = Mouse.current.delta.ReadValue() * rotateSpeed;
            item.Rotate(inspectPoint.up, -delta.x, Space.World);
            item.Rotate(inspectPoint.right, delta.y, Space.World);

            detector.Tick();
        }

        void UpdatePosition(float deltaTime)
        {
            if (deltaTime > 0f)
                distanceOffset = zoomSmoothTime <= 0f ? targetOffset : Mathf.SmoothDamp(
                distanceOffset, targetOffset, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, deltaTime);
            Vector3 direction = view != null ? inspectPoint.position - view.position : inspectPoint.forward;
            if (direction.sqrMagnitude < 0.000001f) direction = inspectPoint.forward;
            Vector3 destination = inspectPoint.position + direction.normalized * distanceOffset;
            enterTime += deltaTime;
            float t = moveDuration > 0f ? Mathf.Clamp01(enterTime / moveDuration) : 1f;
            item.position = Vector3.Lerp(startPosition, destination, t * (2f - t));
        }

        protected override void OnExit()
        {
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