using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerToolDispatcher : MonoBehaviour
{
    [Header("Refs")]
    public HotbarController hotbar;
    public Camera cam;

    [Header("Layers")]
    public LayerMask treeLayer;

    [Header("Action Timing")]
    public float chopCooldown = 0.4f;
    public float nextChopAllowedAt = 0f;

    private void Update()
    {
        if (hotbar == null || cam == null) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Item active = hotbar.GetActiveItem();
        if (active == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        world.z = 0f;

        if(active is Axe axe)
        {
            if (Time.time < nextChopAllowedAt) return;

            Tree tree = FindTreeAt(world);
            if (tree != null) tree.TakeDamage(axe.damage, transform.position);
            nextChopAllowedAt = Time.time + chopCooldown;
        }
        // PlantableSeed + fruit-pick branches added later
    }

    private Tree FindTreeAt(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, treeLayer);
        return hit != null ? hit.GetComponent<Tree>() : null;
    }


}
