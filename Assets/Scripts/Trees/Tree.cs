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
    public SpriteRenderer mainRenderer;
    public SpriteRenderer fruitRenderer;

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

    public void TakeDamage(int damage)
    {
        if (treeData == null || !treeData.isChoppable) return;
        if (state == TreeState.Seedling || state == TreeState.Stump) return;

        hpRemaining -= damage;
        if (hpRemaining <= 0) Fell();
    }

    private void Fell()
    {
        DropWood();
        if (state == TreeState.Ripe) DropFruits();

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

    private void DropWood()
    {
        if (treeData.woodItem == null) return;
        int count = Random.Range(treeData.woodDropMin, treeData.woodDropMax + 1);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
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
        if (treeData == null || mainRenderer == null) return;

        switch(state)
        {
            case TreeState.Seedling: mainRenderer.sprite = treeData.saplingSprite; break;
            case TreeState.Mature: mainRenderer.sprite = treeData.topSprite; break;
            case TreeState.Ripe: mainRenderer.sprite = treeData.topSprite; break;
            case TreeState.Stump: mainRenderer.sprite = treeData.saplingSprite;  break;
        }

        if(fruitRenderer != null)
        {
            bool showFruit = state == TreeState.Ripe && treeData.fruitOverlay != null;
            fruitRenderer.sprite = showFruit ? treeData.fruitOverlay : null;
            fruitRenderer.enabled = showFruit;
        }
    }

}
