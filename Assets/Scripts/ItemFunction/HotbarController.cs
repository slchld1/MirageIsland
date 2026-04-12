using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    public GameObject hotbarpanel;
    public GameObject slotPrefab;
    public int slotCount = 10;

    public int SelectedSlotIndex { get; private set; } = -1;

    private ItemDictionary itemDictionary;
    private Key[] hotbarKeys;

    private void Awake()
    {
        itemDictionary = FindAnyObjectByType<ItemDictionary>();

        hotbarKeys = new Key[slotCount];
        for (int i = 0; i < slotCount; i++)
            hotbarKeys[i] = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0;
    }

    void Update()
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                SelectedSlotIndex = i;
                UseItemInSlot(i);
            }
        }
    }

    void UseItemInSlot(int index)
    {
        if (index < 0 || index >= hotbarpanel.transform.childCount) return;
        Slot slot = hotbarpanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot == null || slot.currentItem == null) return;
        Item item = slot.currentItem.GetComponent<Item>();
        if (item == null) return;
        item.UseItem();
    }

    public Item GetActiveItem()
    {
        if (SelectedSlotIndex < 0 || SelectedSlotIndex >= hotbarpanel.transform.childCount)
            return null;
        Slot slot = hotbarpanel.transform.GetChild(SelectedSlotIndex).GetComponent<Slot>();
        if (slot == null || slot.currentItem == null) return null;
        return slot.currentItem.GetComponent<Item>();
    }

    public List<InventorySaveData> GetHotbarItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        foreach (Transform slotTransform in hotbarpanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;
            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null) continue;
            invData.Add(new InventorySaveData { itemID = item.ID, slotIndex = slotTransform.GetSiblingIndex() });
        }
        return invData;
    }

    public void SetHotbarItems(List<InventorySaveData> inventorySaveData)
    {
        foreach (Transform child in hotbarpanel.transform)
            Destroy(child.gameObject);

        for (int i = 0; i < slotCount; i++)
            Instantiate(slotPrefab, hotbarpanel.transform);

        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount)
            {
                Slot slot = hotbarpanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    slot.currentItem = item;
                }
            }
        }
    }
}
