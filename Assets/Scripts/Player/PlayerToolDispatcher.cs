using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerToolDispatcher : MonoBehaviour
{
    [Header("Refs")]
    public HotbarController hotbar;
    public Camera cam;
    public Planter planter;

    [Header("Layers")]
    public LayerMask treeLayer;

    [Header("Action Timing")]
    public float chopCooldown = 0.4f;
    public float nextChopAllowedAt = 0f;
    public bool IsChopping =>Time.time < nextChopAllowedAt;
    public Animator playerAnimator;

    [Header("Cache field")]
    private TreeMain pendingChopTarget;
    private int pendingChopDamage;
    private static int hitCount, fireCount;


    private void Awake()
    {
        if (playerAnimator != null)
        {
            float clipLen = 1f; // fall back
            foreach ( var c in playerAnimator.runtimeAnimatorController.animationClips )
                if (c.name == "AxeRight") { clipLen = c.length; break; }
            playerAnimator.SetFloat("ChopSpeed", clipLen / chopCooldown);
        }
    }
    private void Update()
    {
        if (hotbar == null || cam == null) return;
        if (Mouse.current == null) return;

        bool lmb = Mouse.current.leftButton.wasPressedThisFrame;
        bool lmbHeld = Mouse.current.leftButton.isPressed;
        bool rmb = Mouse.current.rightButton.wasPressedThisFrame;
        if (lmb || rmb) Debug.Log($"LMB={lmb} RMB={rmb}");

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        world.z = 0f;

        Item active = hotbar.GetActiveItem();


        if (lmbHeld && active != null)
        {
            if(active is Axe axe)
            {
                if (Time.time >= nextChopAllowedAt)
                {
                    TreeMain tree = FindTreeAt(world);
                    fireCount++;

                    if (tree != null && tree.IsPlayerWithinChopRange(transform.position))
                    {
                        pendingChopTarget = tree;
                        pendingChopDamage = axe.damage;
                    }
                    else
                    {
                        pendingChopTarget = null;
                    }
                    if (playerAnimator != null) playerAnimator.SetTrigger("Chop");
                    nextChopAllowedAt = Time.time + chopCooldown;
                }
            }
            // PlantableSeed + fruit-pick branches added later 
            else if (active is PlantableSeed seed && planter !=null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (planter.TryPlant(seed, world))
                    {
                        hotbar.ConsumeActive();
                    }
                }
            }
        }

        // RMB: universal pick (works with any item, including barehanded)
        if (rmb)
        {
            TreeMain tree = FindTreeAt(world);
            if (tree != null) tree.PickFruit();
        }

        if (playerAnimator != null)
            playerAnimator.SetBool("Chopping", IsChopping);

    }

    private TreeMain FindTreeAt(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, treeLayer);
        return hit != null ? hit.GetComponent<TreeMain>() : null;
    }

    public void ApplyChopHit()
    {
        hitCount++;
      if (pendingChopTarget != null)
        {
            pendingChopTarget.TakeDamage(pendingChopDamage, transform.position);
            pendingChopTarget = null;
        }
    }

}
