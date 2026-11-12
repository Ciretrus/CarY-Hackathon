using System;
using DG.Tweening;
using UnityEngine;

namespace Noir.UI
{
    public class FlashbackScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _fadeIn = 1f;
        [SerializeField] private float _fadeOut = 1f;
        [SerializeField] private float _hold = 0.15f;
        [SerializeField] private Ease _ease = Ease.InOutQuad;

        private Sequence _sequence;

        public bool IsPlaying => _sequence.IsActive();

        void Awake()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        public void Play(Action onWhite, Action onComplete = null)
        {
            if (IsPlaying)
                return;

            _sequence = DOTween.Sequence()
                .Append(_group.DOFade(1f, _fadeIn).SetEase(_ease))
                .AppendCallback(() => onWhite?.Invoke())
                .AppendInterval(_hold)
                .Append(_group.DOFade(0f, _fadeOut).SetEase(_ease))
                .OnComplete(() => onComplete?.Invoke())
                .SetLink(gameObject);
        }
    }
}