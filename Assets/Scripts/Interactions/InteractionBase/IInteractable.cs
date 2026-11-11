namespace Noir.Interaction
{
    public interface IInteractable
    {
        void Interact(Interactor interactor);
    }

    public interface IHoverable
    {
        void HoverEnter();
        void HoverExit();
    }
}
