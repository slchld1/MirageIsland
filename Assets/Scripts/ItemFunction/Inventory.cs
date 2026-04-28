using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Inventory : MonoBehaviour
{
    private ItemDictionary itemDictionary;

    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemDictionary = FindAnyObjectByType<ItemDictionary>();

    }
    public bool AddItem(GameObject itemPrefab)
    {
        Item incoming = itemPrefab.GetComponent<Item>();
        int remaining = incoming.count;

        // Pass 1 - try to stack onto an existing slot with the same item
        foreach(Transform slotTransform in inventoryPanel.transform)
        {
            if (remaining == 0) break;
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;

            Item slotItem = slot.currentItem.GetComponent<Item>();
            if (slotItem.ID != incoming.ID) continue;

            int space = incoming.maxStack - slot.count;
            if (space <= 0) continue;

            int add = Mathf.Min(space, remaining);
            slot.count += add;
            remaining -= add;
            slot.RefreshCountText();
        }
            foreach (Transform slotTransform in inventoryPanel.transform)
            {
                if (remaining == 0) break;
                Slot slot = slotTransform.GetComponent<Slot>();
                if (slot == null || slot.currentItem != null) continue;

                int put = Mathf.Min(incoming.maxStack, remaining);
                GameObject newItem = Instantiate(itemPrefab, slot.transform);
                newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                slot.currentItem = newItem;
                slot.count = put;
                slot.RefreshCountText();
                remaining -= put;
            }

        incoming.count = remaining;

        if (remaining > 0)
        {
            Debug.Log("Inventory is full, " + remaining + " left over");
            return false;
        }
        return true;
    }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        foreach(Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if(slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                invData.Add(new InventorySaveData { itemID = item.ID, slotIndex = slotTransform.GetSiblingIndex() });
            }
        }
        return invData;
    }

    public void SetInventoryItems(List<InventorySaveData> inventorySaveData)
    {
        //Clear inventory panel = avoid duplicates
        foreach(Transform child in inventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        //Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, inventoryPanel.transform);
        }

        //Populate slots with saved items
        foreach(InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount)
            {
                Slot slot = inventoryPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if(itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    slot.currentItem = item;
                }
            }
        }
    }

}
