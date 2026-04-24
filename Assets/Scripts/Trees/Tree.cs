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
