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

        bool lmb = Mouse.current.leftButton.wasPressedThisFrame;
        bool rmb = Mouse.current.rightButton.wasPressedThisFrame;
        if (lmb || rmb) Debug.Log($"LMB={lmb} RMB={rmb}");

        if (!lmb && !rmb) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        world.z = 0f;

        Item active = hotbar.GetActiveItem();


        if (lmb && active != null)
        {
            if(active is Axe axe)
            {
                if (Time.time < nextChopAllowedAt) return;

                Tree tree = FindTreeAt(world);
                if (tree != null) tree.TakeDamage(axe.damage, transform.position);
                nextChopAllowedAt = Time.time + chopCooldown;
            }
            // PlantableSeed + fruit-pick branches added later 
        }

        // RMB: universal pick (works with any item, including barehanded)
        if (rmb)
        {
            Tree tree = FindTreeAt(world);
            if (tree != null) tree.PickFruit();
        }


    }

    private Tree FindTreeAt(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, treeLayer);
        return hit != null ? hit.GetComponent<Tree>() : null;
    }


}
