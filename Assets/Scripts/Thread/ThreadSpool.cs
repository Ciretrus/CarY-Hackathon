using DG.Tweening;
using Noir.Interaction;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Noir.Interaction
{
    public class ThreadSpool : Interactable
    {
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private float _hoverAlpha = 0.3f;
        [SerializeField] private float _activeAlpha = 1f;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        private Tween _tween;
        private bool _isHovered, _isHighlighted;

        private void OnEnable()
        {
            _isHighlighted = false;
            _isHovered = false;
            _tween?.Kill();
            SetAlpha(0f);

        }
        private void OnDisable()
        {
            _tween?.Kill();
            SetAlpha(0f);
        }
        private void SetAlpha(float alpha)
        {
            if (_glow == null)
                return;
            Color color = _glow.color;
            color.a = alpha;
            _glow.color = color;
        }
        private void Refresh()
        {
            _tween?.Kill();
            float target = _isHighlighted ? _activeAlpha : _isHovered ? _hoverAlpha : 0f;
            _tween = _glow.DOFade(target, _duration).SetEase(_ease).SetLink(gameObject);
        }
        public override void HoverEnter()
        {
            base.HoverEnter();
            _isHovered = true;
            Refresh();
        }
        public override void HoverExit()
        {
            base.HoverExit();
            _isHovered = false;
            Refresh();
        }
        public void SetHighlighted(bool value)
        {
            if (_isHighlighted == value) 
                return;
            _isHighlighted = value;
            Refresh();
        }
    }

}
