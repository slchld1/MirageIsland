using UnityEngine;

public class PlantableSeed : Item
{
    [Header("Plant")]
    public TreeData treeData;

    public override void UseItem()
    {
        // No-op — PlayerToolDispatcher handles LMB planting.
    }
}
