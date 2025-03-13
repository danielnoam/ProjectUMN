
public enum InteractorType
{
    Player,
    Robot,
    Both,
}

public interface Iinteractor
{
    InteractorType InteractorType { get; }
    void InteractWith();
    void OnInteractionStart(Interactable interactable);
    void OnInteractionEnd(Interactable interactable);
    void CancelInteraction(Interactable interactable);
}