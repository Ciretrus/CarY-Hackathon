using Noir.Player;
using UnityEngine;

namespace Noir.Interaction
{
    public class Board : Interactable
    {
        [SerializeField] private Transform _viewAnchor;

        public Transform ViewAnchor => _viewAnchor;

        public override void Interact(Interactor interactor)
        {
            base.Interact(interactor);

            var mode = interactor.Modes.Get<BoardMode>();
            if (mode != null)
                mode.Open(this);
        }
    }
}