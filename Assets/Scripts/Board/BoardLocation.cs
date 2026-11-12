using DG.Tweening;
using Noir.Interaction;
using UnityEngine;

namespace Noir.Interaction
{
    public class BoardLocation : Interactable
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private float _hoverAlpha = 0.3f, _selectedAlpha = 1f, _duration = 0.15f;
        [SerializeField] Ease ease = Ease.OutQuad;
        private Tween _tween;
        private bool _isHovered, _isSelected;
        public Transform Spawn => _spawnPoint;
        private void Awake() => SetAlpha(0f);
        private void OnDisable()
        {
            _tween?.Kill();
            _isHovered = false;
            _isSelected = false;
            SetAlpha(0f);
        }
        private void Refresh()
        {
            _tween?.Kill();
            float target = _isSelected ? _selectedAlpha : (_isHovered ? _hoverAlpha : 0f);
            _tween = _glow.DOFade(target, _duration).SetEase(ease).SetLink(gameObject);
        }
        private void SetAlpha(float alpha)
        {
            Color color = _glow.color;
            color.a = alpha;
            _glow.color = color;
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
        public void SetSelected(bool value)
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            Refresh();
        }

    }

}
