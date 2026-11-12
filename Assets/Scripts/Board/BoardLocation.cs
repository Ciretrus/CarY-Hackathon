using DG.Tweening;
using Noir.Interaction;
using UnityEngine;

namespace Noir.Interaction
{
    public class BoardLocation : Interactable
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Renderer _glow;
        [SerializeField] private float _hoverAlpha = 0.3f, _selectedAlpha = 1f, _duration = 0.15f;
        [SerializeField] Ease ease = Ease.OutQuad;
        private Material _mat;
        private Tween _tween;
        private bool _isHovered, _isSelected;
        public Transform Spawn => _spawnPoint;
        private void Awake()
        {
            _mat = _glow.material;
            _mat.SetFloat("_Alpha", 0f);
        }
        private void OnDisable()
        {
            if (_mat != null)
                Destroy(_mat);
        }
        public override void HoverExit()
        {
            base.HoverExit();
            _isHovered = false;
            Refresh();
        }
        public void SetSelected(bool value)
        {
            _isSelected = value;
            Refresh();
        }
        void Refresh()
        {
            _tween?.Kill();
            float target = _isSelected ? _selectedAlpha : (_isHovered ? _hoverAlpha : 0f);
            _tween = _mat.DOFloat(target, "_Alpha", _duration).SetEase(ease).SetLink(gameObject);
        }
    }

}
