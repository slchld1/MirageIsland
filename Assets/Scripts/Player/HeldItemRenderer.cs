using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the active hotbar item as a sprite held by the player.
/// Requires a child GameObject with a SpriteRenderer assigned as holdPoint.
/// The holdPoint's local X position will mirror when the player flips.
/// </summary>
public class HeldItemRenderer : MonoBehaviour
{
    [Tooltip("Child GameObject positioned at the player's hand. Must have a SpriteRenderer.")]
    public Transform holdPoint;

    [Tooltip("The Movement component on the player — used to detect facing direction.")]
    public Movement movement;

    [Tooltip("Local position when facing right")]
    public Vector2 holdOffset = new Vector2(0.3f, 0f);

    [Tooltip("Scale of the held item sprite")]
    public Vector2 holdScale = new Vector2(0.5f, 0.5f);

    [Tooltip("Seconds to wait after stopping before showing the item")]
    public float showDelay = 0.2f;

    [Tooltip("Invert the flip direction if the sprite is facing the wrong way")]
    public bool invertFlip = false;

    private HotbarController hotbarController;
    private SpriteRenderer   holdRenderer;
    private int              lastSlotIndex = -2;
    private float            idleTimer;

    private void Awake()
    {
        hotbarController = FindAnyObjectByType<HotbarController>();
        if (movement == null) movement = GetComponent<Movement>();

        if (holdPoint != null)
            holdRenderer = holdPoint.GetComponent<SpriteRenderer>();
        else
            Debug.LogWarning("[HeldItemRenderer] holdPoint is not assigned.", this);
    }

    private void Update()
    {
        if (holdRenderer == null || hotbarController == null) return;

        holdPoint.localPosition = new Vector3(holdOffset.x, holdOffset.y, holdPoint.localPosition.z);

        bool isMoving = movement != null && movement.IsMoving;
        if (isMoving)
            idleTimer = 0f;
        else
            idleTimer += Time.deltaTime;

        holdRenderer.enabled = !isMoving && idleTimer >= showDelay && holdRenderer.sprite != null;

        // Only update sprite when selection changes
        if (hotbarController.SelectedSlotIndex == lastSlotIndex) return;
        lastSlotIndex = hotbarController.SelectedSlotIndex;

        Item activeItem = hotbarController.GetActiveItem();
        if (activeItem != null)
        {
            Image icon = activeItem.GetComponent<Image>();
            holdRenderer.sprite = icon != null ? icon.sprite : null;
            holdPoint.localScale = new Vector3(holdScale.x, holdScale.y, 1f);
        }
        else
        {
            holdRenderer.sprite  = null;
            holdRenderer.enabled = false;
        }
    }
}
