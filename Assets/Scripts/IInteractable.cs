
public interface IInteractable
{
    void Interact();
    bool CanInteract();
    string InteractionPrompt { get; } // e.g. "Open", "Talk", "Pick up"
}
