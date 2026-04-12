using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour


{
    private IInteractable interactableRange = null; //Closest Interactable
    public GameObject interactionIcon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interactionIcon.SetActive(false);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (interactableRange == null) return;

            interactableRange?.Interact();
            if (!interactableRange.CanInteract())
            {
                if (interactionIcon)
                {
                interactionIcon.SetActive(false);
                }
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) && interactable.CanInteract())
        {
            interactableRange = interactable;
            interactionIcon?.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.TryGetComponent(out IInteractable interactable)&& interactable == interactableRange)
        {
            interactableRange = null;
            interactionIcon?.SetActive(false);
        }
    }
}
