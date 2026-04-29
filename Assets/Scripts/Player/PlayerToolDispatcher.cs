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
    public bool IsChopping =>Time.time < nextChopAllowedAt;
    public Animator playerAnimator;

    private void Update()
    {
        if (hotbar == null || cam == null) return;
        if (Mouse.current == null) return;

        bool lmb = Mouse.current.leftButton.wasPressedThisFrame;
        bool lmbHeld = Mouse.current.leftButton.isPressed;
        bool rmb = Mouse.current.rightButton.wasPressedThisFrame;
        if (lmb || rmb) Debug.Log($"LMB={lmb} RMB={rmb}");

        if (!lmbHeld && !rmb) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        world.z = 0f;

        Item active = hotbar.GetActiveItem();


        if (lmbHeld && active != null)
        {
            if(active is Axe axe)
            {
                if (Time.time < nextChopAllowedAt) return;

                if (playerAnimator != null) playerAnimator.SetTrigger("Chop");
                TreeMain tree = FindTreeAt(world);
                if (tree != null && tree.IsPlayerWithinChopRange(transform.position))
                    {
                    tree.TakeDamage(axe.damage, transform.position);
                    }
                    nextChopAllowedAt = Time.time + chopCooldown;
            }
            // PlantableSeed + fruit-pick branches added later 
        }

        // RMB: universal pick (works with any item, including barehanded)
        if (rmb)
        {
            TreeMain tree = FindTreeAt(world);
            if (tree != null) tree.PickFruit();
        }


    }

    private TreeMain FindTreeAt(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, treeLayer);
        return hit != null ? hit.GetComponent<TreeMain>() : null;
    }


}
