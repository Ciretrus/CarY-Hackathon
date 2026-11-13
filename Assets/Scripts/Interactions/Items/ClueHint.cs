using DG.Tweening;
using Noir.Interaction;
using UnityEngine;

namespace Noir.UI
{
    public class ClueHint : MonoBehaviour
    {
        [SerializeField] private ClueDetector _detector;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _duration = 0.25f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        private Tween _tween;

        private void Awake()
        {
            if (_detector == null)
                _detector = FindFirstObjectByType<ClueDetector>(FindObjectsInactive.Include);
            if (_group == null)
                _group = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            _group.alpha = 0f;
            _detector.VisibleChanged += OnVisibleChanged;
        }

        private void OnDisable()
        {
            if (_detector != null)
                _detector.VisibleChanged -= OnVisibleChanged;
            _tween?.Kill();
            _group.alpha = 0f;
        }

        private void OnVisibleChanged(ClueSpot spot)
        {
            _tween?.Kill();
            _tween = _group
                .DOFade(spot != null ? 1f : 0f, _duration)
                .SetEase(_ease)
                .SetLink(gameObject);
        }
    }
}
