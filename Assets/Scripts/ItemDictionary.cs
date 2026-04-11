using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ItemDictionary : MonoBehaviour
{
    public List<Item> itemPrefabs;
    private Dictionary<int, GameObject> itemDictionary;

    private void Awake()
    {
        itemDictionary = new Dictionary<int, GameObject>();

        //Auto Increment IDs
        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            if (itemPrefabs[i] == null) continue;
            itemPrefabs[i].ID = i + 1;
            itemDictionary[itemPrefabs[i].ID] = itemPrefabs[i].gameObject;
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

