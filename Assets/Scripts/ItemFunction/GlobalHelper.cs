using UnityEngine;

public static class GlobalHelper
{
    public static string GenerateUniqueId(GameObject obj)
    {
        return $"{obj.scene.name}_{obj.name}_{obj.transform.GetSiblingIndex()}";  //Chest_ID
    }
}
