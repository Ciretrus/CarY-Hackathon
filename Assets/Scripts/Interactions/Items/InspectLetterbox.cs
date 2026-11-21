using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Noir.Interaction
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class InspectLetterbox : MonoBehaviour
    {
        [SerializeField] private InspectMode inspectMode;
        [SerializeField, Range(0.01f, 0.25f)] private float barHeight = 0.08f;
        [SerializeField, Min(0f)] private float duration = 0.32f;

        private RectTransform top;
        private RectTransform bottom;
        private Tween motion;
        private float progress;
        private bool showing;

        private void Awake()
        {
            if (inspectMode == null)
            {
                Debug.LogWarning("InspectLetterbox: назначьте Inspect Mode в Inspector.", this);
                enabled = false;
                return;
            }

            top = CreateBar("Inspect Bar - Top");
            bottom = CreateBar("Inspect Bar - Bottom");
            SetProgress(0f);
        }

        private RectTransform CreateBar(string barName)
        {
            var bar = new GameObject(barName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)bar.transform;
            rect.SetParent(transform, false);
            rect.SetAsFirstSibling();
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var image = bar.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }

        private void LateUpdate()
        {
            bool inspecting = inspectMode != null && inspectMode.isActiveAndEnabled;
            if (showing == inspecting) return;

            showing = inspecting;
            motion?.Kill();
            float target = showing ? 1f : 0f;
            float travelTime = duration * Mathf.Abs(target - progress);
            if (travelTime <= 0f)
            {
                SetProgress(target);
                return;
            }

            motion = DOTween.To(() => progress, SetProgress, target, travelTime)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetRecyclable(false);
        }

        private void SetProgress(float value)
        {
            progress = value;
            float visibleHeight = barHeight * value;
            top.anchorMin = new Vector2(0f, 1f - visibleHeight);
            top.anchorMax = new Vector2(1f, 1f - visibleHeight + barHeight);
            bottom.anchorMin = new Vector2(0f, visibleHeight - barHeight);
            bottom.anchorMax = new Vector2(1f, visibleHeight);
        }

        private void OnDisable()
        {
            motion?.Kill();
            motion = null;
            showing = false;
            if (top != null && bottom != null) SetProgress(0f);
        }
    }
}