using UnityEngine;
using DG.Tweening;

namespace Noir.Interaction
{
    public class BoardEvidence : Interactable
    {
        [SerializeField] private string _id;
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private float _hoverAlpha = 0.3f;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        private Tween _tween;
        private bool _isHovered;

        public string ID => _id;
        public bool IsFound { get; private set; }

        void OnEnable()
        {
            _isHovered = false;
            SetAlpha(0f);
        }

        void OnDisable()
        {
            _tween?.Kill();
            _isHovered = false;
            SetAlpha(0f);
        }
        private void Refresh()
        {
            _tween?.Kill();
            float target = _isHovered ? _hoverAlpha : 0f;
            _tween = _glow.DOFade(target, _duration).SetEase(_ease).SetLink(gameObject);
        }
        private void SetAlpha(float alpha)
        {
            if (_glow == null)
                return;
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
        public void MarkFound()
        {
            if (IsFound)
                return;
            IsFound = true;
            gameObject.SetActive(true);
        }
    }
}

