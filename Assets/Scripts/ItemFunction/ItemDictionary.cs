using System.Collections.Generic;
using UnityEngine;

public class ItemDictionary : MonoBehaviour
{
    public List<Item> itemPrefabs;
    private Dictionary<int, GameObject> itemDictionary;

    private void Awake()
    {
        itemDictionary = new Dictionary<int, GameObject>();

        foreach (Item item in itemPrefabs)
        {
            if (item == null) continue;
            if (item.ID <= 0)
            {
                Debug.LogWarning($"Item '{item.Name}' has ID {item.ID} — set a valid ID (> 0) on the prefab.");
                continue;
            }
            if (itemDictionary.ContainsKey(item.ID))
            {
                Debug.LogWarning($"Duplicate item ID {item.ID} ('{item.Name}'). Skipping.");
                continue;
            }
            itemDictionary[item.ID] = item.gameObject;
        }
    }

    public GameObject GetItemPrefab(int itemID)
    {
        itemDictionary.TryGetValue(itemID, out GameObject prefab);
        if(prefab == null)
        {
            Debug.LogWarning($"Item with ID {itemID} not found in dictionary");
        }
        return prefab;
    }
}

