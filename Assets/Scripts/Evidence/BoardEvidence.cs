using UnityEngine;
using DG.Tweening;
using TMPro;

namespace Noir.Interaction
{
    public class BoardEvidence : Interactable
    {
        [SerializeField] private string _id;
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private SpriteRenderer _photo;
        [SerializeField] private float _hoverAlpha = 0.3f;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private Ease _ease = Ease.OutQuad;
        [SerializeField] private MeshRenderer _photoQuad;

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
        public void SetPhoto(Texture2D texture)
        {
            if (texture == null)
                return;
            if (_photoQuad != null)
            {
                var mat = _photoQuad.material;
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", texture);
                else
                    mat.mainTexture = texture;
            }
            if (_photo == null)
                return;
            float width = _photo.sprite != null ? _photo.sprite.bounds.size.x : 1f;
            var rect = new Rect(0f, 0f, texture.width, texture.height);
            _photo.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), texture.width / width);
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

