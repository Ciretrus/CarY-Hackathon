using Noir.Player;
using UnityEngine;

namespace Noir.Interaction
{
    public class Board : Interactable
    {
        [SerializeField] private Transform _viewAnchor;
        [SerializeField] ThreadSpool _spool;
        [SerializeField] ThreadEraser _eraser;

        public Transform ViewAnchor => _viewAnchor;
        public ThreadSpool Spool => _spool;
        public ThreadEraser Eraser => _eraser;

        public override void Interact(Interactor interactor)
        {
            base.Interact(interactor);

            var mode = interactor.Modes.Get<BoardMode>();
            if (mode != null)
                mode.Open(this);
        }
    }
}