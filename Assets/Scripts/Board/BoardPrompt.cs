using DG.Tweening;
using Noir.Interaction;
using Noir.Player;
using UnityEngine;

namespace Noir.UI
{
    public class BoardPrompt : MonoBehaviour
    {
        [SerializeField] private BoardMode _mode;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        private Tween _tween;

        void OnEnable()
        {
            _group.alpha = 0f;
            _mode.SelectionChanged += OnSelectionChanged;
        }

        void OnDisable()
        {
            if (_mode != null) _mode.SelectionChanged -= OnSelectionChanged;
            _tween?.Kill();
            _group.alpha = 0f;
        }

        void OnSelectionChanged(BoardLocation location)
        {
            _tween?.Kill();
            _tween = _group
                .DOFade(location != null ? 1f : 0f, _duration)
                .SetEase(_ease)
                .SetLink(gameObject);
        }
    }
}