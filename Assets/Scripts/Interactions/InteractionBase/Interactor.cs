using System;
using Noir.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Noir.Interaction
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] PlayerModes modes;
        [SerializeField] float distance = 2.5f;
        [SerializeField] LayerMask mask = ~0;

        Transform view;
        IInteractable current;

        public PlayerModes Modes => modes;
        public event Action<IInteractable> CurrentChanged;

        void Awake() => view = Camera.main.transform;

        void OnDisable() => SetCurrent(null);

        void Update()
        {
            SetCurrent(FindTarget());

            if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                current.Interact(this);
        }

        IInteractable FindTarget()
        {
            if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
                return null;

            var target = hit.collider.GetComponentInParent<IInteractable>();
            return target is Behaviour b && b.isActiveAndEnabled ? target : null;
        }

        void SetCurrent(IInteractable target)
        {
            if (ReferenceEquals(target, current)) return;

            if ((current as Object) != null && current is IHoverable oldHover) oldHover.HoverExit();
            current = target;
            if (current is IHoverable newHover) newHover.HoverEnter();

            CurrentChanged?.Invoke(current);
        }
    }
}
