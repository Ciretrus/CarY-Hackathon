using UnityEngine;

namespace Noir.Interaction
{
    public class BoardEvidence : Interactable
    {
        [SerializeField] private string _id;
        [SerializeField] private BoardHighlight _highlight;
        [SerializeField] private SpriteRenderer _photo;
        [SerializeField] private MeshRenderer _photoQuad;

        public string ID => _id;
        public bool IsFound { get; private set; }

        public override void HoverEnter()
        {
            base.HoverEnter();
            if (_highlight)
                _highlight.SetHovered(true);
        }

        public override void HoverExit()
        {
            base.HoverExit();
            if (_highlight)
                _highlight.SetHovered(false);
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