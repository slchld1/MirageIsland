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

    [Header("Renderers (assign in prefab)")]
    public SpriteRenderer mainRenderer;
    public SpriteRenderer fruitRenderer;

    private void Awake()
    {
        if(string.IsNullOrEmpty(treeID))
        {
            treeID = GlobalHelper.GenerateUniqueId(gameObject);
        }
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
    

    private void UpdateSprite()
    {
        if (treeData == null || mainRenderer == null) return;

        switch(state)
        {
            case TreeState.Seedling: mainRenderer.sprite = treeData.saplingSprite; break;
            case TreeState.Mature: mainRenderer.sprite = treeData.matureSprite; break;
            case TreeState.Ripe: mainRenderer.sprite = treeData.matureSprite; break;
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
