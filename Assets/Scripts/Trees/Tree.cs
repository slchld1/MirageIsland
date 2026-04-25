using UnityEngine;
public enum TreeState
{
    Seedling,
    Mature,
    Ripe,
    Stump,
}

public class Tree : MonoBehaviour
{
    [Header("Config")]
    public TreeData treeData;

    [Header("Runtime")]
    public TreeState state = TreeState.Mature;
    public bool wasPlanted = false;
    public string treeID;
    public float stateEnteredAtTotalHours;
    public int hpRemaining;
    public bool permanentlyGone;

    [Header("Renderers (assign in prefab)")]
    public SpriteRenderer topRenderer;
    public SpriteRenderer stumpRenderer;
    public SpriteRenderer fruitRenderer;

    [Header("Animator (assign in prefab")]
    public TreeAnimator animator;

    [Header("Fall")]
    public float fallAlignThreshold = 0.5f;
    public float woodDropMinDistance = 1.2f;
    public float woodDropMaxDistance = 2.0f;

    private void Awake()
    {
        if (string.IsNullOrEmpty(treeID))
        {
            treeID = GlobalHelper.GenerateUniqueId(gameObject);
        }
        hpRemaining = treeData != null ? treeData.chopCount : 1;
        UpdateSprite();
    }
    private void Update()
    {
        if (treeData == null || DayCycleManager.Instance == null) return;

        if (stateEnteredAtTotalHours == 0f)
        {
            stateEnteredAtTotalHours = DayCycleManager.Instance.TotalHours;
            return;
        }

        float elapsed = DayCycleManager.Instance.TotalHours - stateEnteredAtTotalHours;

        switch (state)
        {
            case TreeState.Seedling:
                if (elapsed >= treeData.growHours) Enter(TreeState.Mature);
                break;
            case TreeState.Mature:
                if (treeData.fruitItem != null && elapsed >= treeData.ripenHours) Enter(TreeState.Ripe);
                break;
            case TreeState.Stump:
                if (treeData.regrows && elapsed >= treeData.regrowHours) Enter(TreeState.Mature);
                break;
        }
    }

    private void Enter(TreeState next)
    {
        state = next;
        if (DayCycleManager.Instance != null)
        {
            stateEnteredAtTotalHours = DayCycleManager.Instance.TotalHours;
        }
        UpdateSprite();
    }

    public void TakeDamage(int damage, Vector3 chopperWorldPos)
    {
        if (treeData == null || !treeData.isChoppable) return;
        if (state == TreeState.Seedling || state == TreeState.Stump) return;

        int fallDir = ComputeFallDirection(chopperWorldPos);

        if (animator != null) animator.PlayShake();

        if (state == TreeState.Ripe)
        {
            DropFruits();
            Enter(TreeState.Mature);
            return;
        }

        hpRemaining -= damage;
        if (hpRemaining <= 0) Fell(fallDir);
    }

    private int ComputeFallDirection(Vector3 chopperWorldPos)
    {
        float dx = chopperWorldPos.x - transform.position.x;
        if (Mathf.Abs(dx) < fallAlignThreshold)
        {
            return UnityEngine.Random.value < 0.5f ? -1 : 1;
        }
        return dx > 0f ? -1 : 1;
    }

    private void Fell(int fallDir)
    {
        DropWood(fallDir);

        if(wasPlanted)
        {
            Destroy(gameObject);
        }
        else if (!treeData.regrows)
        {
            permanentlyGone = true;
            Destroy(gameObject);
        }
        else
        {
            hpRemaining = treeData.chopCount;
            Enter(TreeState.Stump);
        }
    }

    private void DropWood(int fallDir)
    {
        if (treeData.woodItem == null) return;
        int count = Random.Range(treeData.woodDropMin, treeData.woodDropMax + 1);
        for (int i = 0; i < count; i++)
        {
            float xPush = fallDir * UnityEngine.Random.Range(woodDropMinDistance, woodDropMaxDistance);
            float yJit = UnityEngine.Random.Range(-0.2f, 0.2f);
            Vector3 offset = new Vector3(xPush, yJit, 0f);
            GameObject drop = Instantiate(treeData.woodItem.gameObject, transform.position + offset, Quaternion.identity);
            var bounce = drop.GetComponent<BounceEffect>();
            if (bounce != null) bounce.StartBounce();
        }
    }

    private void DropFruits()
    {
        if (treeData.fruitItem == null) return;
        for (int i = 0;i < treeData.fruitPerHarvest;i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
            GameObject drop = Instantiate(treeData.fruitItem.gameObject, transform.position + offset, Quaternion.identity);
            var bounce = drop.GetComponent <BounceEffect>();
            if (bounce != null) bounce.StartBounce();
        }
    }
    

    private void UpdateSprite()
    {
        if (treeData == null) return;

        switch(state)
        {
            case TreeState.Seedling:
                if (topRenderer != null)
                {
                    topRenderer.sprite = treeData.saplingSprite;
                    topRenderer.enabled = true;
                    Color c1 = topRenderer.color; c1.a = 1f; topRenderer.color = c1;
                }
                if (stumpRenderer != null) stumpRenderer.enabled = false;
                break;

            case TreeState.Mature:
            case TreeState.Ripe:
                if (topRenderer != null)
                {
                    topRenderer.sprite = treeData.topSprite;
                    topRenderer.enabled = true;
                    Color c2 = topRenderer.color; c2.a = 1f; topRenderer.color = c2;
                }
                if (stumpRenderer != null)
                {
                    stumpRenderer.sprite = treeData.stumpSprite;
                    stumpRenderer.enabled = true;
                }
                break;

            case TreeState.Stump:
                if (topRenderer != null) topRenderer.enabled = false;
                if (stumpRenderer != null)
                {
                    stumpRenderer.sprite = treeData.stumpSprite;
                    stumpRenderer.enabled = true;
                }
                break;
        }

        if(fruitRenderer != null)
        {
            bool showFruit = state == TreeState.Ripe && treeData.fruitOverlay != null;
            fruitRenderer.sprite = showFruit ? treeData.fruitOverlay : null;
            fruitRenderer.enabled = showFruit;
        }
    }

}
