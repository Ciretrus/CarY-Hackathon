namespace Noir.Interaction
{
    public class InspectableItem : Interactable
    {
        public override void Interact(Interactor interactor)
        {
            base.Interact(interactor);
            interactor.Modes.Get<InspectMode>()?.Inspect(transform);
        }
    }
}
