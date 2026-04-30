using UnityEngine;

public class Planter : MonoBehaviour
{
    [Header("Config")]
    public LayerMask plantableLayer;
    public LayerMask treeLayer;
    public float maxPlantDistance = 2f;
    public float treeOverlapRadius = 0.5f;

    public bool TryPlant(PlantableSeed seed, Vector3 worldPos)
    {
        if (seed == null || seed.treeData == null || seed.treeData.treePrefab == null) return false;

        if (Vector3.Distance(transform.position, worldPos) > maxPlantDistance) return false;

        if (Physics2D.OverlapPoint(worldPos, plantableLayer) == null) return false;

        if (Physics2D.OverlapCircle(worldPos, treeOverlapRadius, treeLayer) != null) return false;

        GameObject go = Instantiate(seed.treeData.treePrefab, worldPos, Quaternion.identity);
        TreeMain tree = go.GetComponent<TreeMain>();
        if (tree == null)
        {
            Destroy(go);
            return false;
        }
        float currentHour = DayCycleManager.Instance != null ? DayCycleManager.Instance.CurrentHour : 0f;
        tree.InitializeAsPlanted(seed.treeData, currentHour);
        return true;
    }
}