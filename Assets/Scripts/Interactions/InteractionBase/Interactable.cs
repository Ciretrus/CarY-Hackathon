using UnityEngine;
using UnityEngine.Events;

namespace Noir.Interaction
{
    public class Interactable : MonoBehaviour, IInteractable, IHoverable
    {
        [SerializeField] UnityEvent onInteract = new();
        [SerializeField] UnityEvent onHoverEnter = new();
        [SerializeField] UnityEvent onHoverExit = new();

        public virtual void Interact(Interactor interactor) => onInteract.Invoke();
        public virtual void HoverEnter() => onHoverEnter.Invoke();
        public virtual void HoverExit() => onHoverExit.Invoke();
    }
}
