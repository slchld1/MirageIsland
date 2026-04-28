using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseCursorIndicator : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer spriteRenderer;
    public Camera cam;
    public HotbarController hotbar;

    [Header("Layers")]
    public LayerMask treeLayer;
    public LayerMask waterLayer;

    [Header("Colors")]
    public Color idleColor = new Color(1f, 1f, 1f, 0.3f);
    public Color activeColor = new Color(0.2f, 1f, 0.2f, 0.6f);

    [Header("Grid")]
    public float gridSize = 1f;

    private void Update()
    {
        if (cam == null || spriteRenderer == null) return;
        if (Mouse.current == null) return;

        // Read raw mouse screen position
        Vector2 screenPos = Mouse.current.position.ReadValue();

        // Convert to world position
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.localScale.z));

        // Snap to grid cell center
        worldPos.x = Mathf.Floor(worldPos.x / gridSize) * gridSize + (gridSize * 0.5f);
        worldPos.y = Mathf.Floor(worldPos.y / gridSize) * gridSize + (gridSize * 0.5f);
        worldPos.z = 0f;

        // Move the cursor
        transform.position = worldPos;

        // Check interaction + tint
        spriteRenderer.color = CanInteractAt(worldPos) ? activeColor : idleColor;

    }

    private bool CanInteractAt(Vector3 worldPos)
    {
        Item active = hotbar != null ? hotbar.GetActiveItem() : null;

        // Tree at this cell?
        Collider2D treeHit = Physics2D.OverlapPoint(worldPos, treeLayer);
        if (treeHit != null)
        {
            TreeMain tree = treeHit.GetComponent<TreeMain>();
            if (tree != null)
            {
                // Axe chop available on Mature or Ripe trees
                if (active is Axe && (tree.state == TreeState.Mature || tree.state == TreeState.Ripe))
                    return true;

                // RMB pick available on Ripe trees with fruit (works barehanded too)
                if (tree.state == TreeState.Ripe && tree.currentFruitCount > 0)
                    return true;
            }
        }

        // Water at this cell with fishing rod?
        if (active is FishingRod)
        {
            Collider2D waterHit = Physics2D.OverlapPoint(worldPos, waterLayer);
            if (waterHit != null) return true;
        }

        return false;
    }
}
