using UnityEngine;

namespace Noir.Interaction
{
    public class BoardLocation : Interactable
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private BoardHighlight _highlight;

        public Transform Spawn => _spawnPoint;

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

        public void SetSelected(bool value)
        {
            if (_highlight)
                _highlight.SetSelected(value);
        }
    }
}