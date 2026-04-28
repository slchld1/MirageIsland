using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    private Transform playerTransform;
    private ItemDictionary itemDictionary;

    public float minDropDistance = 2f;
    public float maxDropDistance = 3f;

    private bool isDragging = false;
    private Slot originalSlot; // cached source slot



    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
    }
    void Update()
    {
        if (isDragging && Input.GetMouseButtonDown(1))
        {
            HandleRmbDuringDrag();
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent; //Save OG Parent
        transform.SetParent(transform.root); //Above other canvas'
        canvasGroup.blocksRaycasts = false; //
        canvasGroup.alpha = 0.6f; //semi-transparent during drag
        originalSlot = originalParent.GetComponent<Slot>();
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position; //Follow the mouse
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true; //Enable raycasts
        canvasGroup.alpha = 1f; //No longer transparent
        isDragging = false;

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>(); //Slot where item dropped
        if (dropSlot == null )
        {
            GameObject pe = eventData.pointerEnter;
            if (pe != null )
            {
                dropSlot = pe.GetComponentInParent<Slot>();
            }
        }

        // No slot under cursor
        if (dropSlot == null)
        {
            if (!IsWithinInventory(eventData.position))
            {
                DropItem(originalSlot);
            }
            else
            {
                transform.SetParent(originalParent);
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }
            return;
        }

        // Drop on source slot - snap back
        if (dropSlot == originalSlot)
        {
            transform.SetParent(originalParent);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return;
        }    

        // Drop on empty slot - transfer item + count
        if (dropSlot.currentItem == null)
        {
            transform.SetParent(dropSlot.transform);
            dropSlot.currentItem = gameObject;
            dropSlot.count = originalSlot.count;
            dropSlot.RefreshCountText();

            originalSlot.currentItem = null;
            originalSlot.count = 0;
            originalSlot.RefreshCountText();

            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return;
        }

        // Drop on slot with same item - merge with overflow
        Item draggedItem = GetComponent<Item>();
        Item dropItem = dropSlot.currentItem.GetComponent<Item>();

        if (dropItem.ID == draggedItem.ID)
        {
            int total = dropSlot.count + originalSlot.count;
            int maxStack = draggedItem.maxStack;

            if (total <= maxStack)
            {
                // All fits - dropSlot absorbs everything, dragged is destroyed
                dropSlot.count = total;
                dropSlot.RefreshCountText();
                originalSlot.currentItem = null;
                originalSlot.count = 0;
                originalSlot.RefreshCountText();
                Destroy(gameObject);
            }
            else
            {
                // Overflow -dropSlot fills to max, dragged returns to source
                dropSlot.count = maxStack;
                dropSlot.RefreshCountText();
                originalSlot.count = total - maxStack;
                originalSlot.RefreshCountText();
                transform.SetParent(originalParent);
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }
            return;
        }
        // Drop on slot with different item -swap items + counts
        int draggedCount = originalSlot.count;
        int swappedCount = dropSlot.count;

        dropSlot.currentItem.transform.SetParent(originalSlot.transform);
        dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        originalSlot.currentItem = dropSlot.currentItem;
        originalSlot.count = swappedCount;
        originalSlot.RefreshCountText();

        transform.SetParent(dropSlot.transform);
        dropSlot.currentItem = gameObject;
        dropSlot.count = draggedCount;
        dropSlot.RefreshCountText();

        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    bool IsWithinInventory(Vector2 mousePosition)
    {
        RectTransform inventoryRect = originalParent.parent.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, mousePosition);
    }

    void DropItem(Slot originalSlot)
    {
        if (playerTransform == null)
        {
            Debug.LogError("Missing 'Player' tag");
            return;
        }

        // Look up the clean world prefab by item ID
        Item draggedItem = GetComponent<Item>();
        GameObject worldPrefab = itemDictionary.GetItemPrefab(draggedItem.ID);
        if (worldPrefab == null)
        {
            Debug.LogError($"No world prefab for item ID {draggedItem.ID}");
            return;
        }
        // One drop position at a safe radius
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minDropDistance, maxDropDistance);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        Vector2 dropPos = (Vector2)playerTransform.position + offset;

        GameObject dropItem = Instantiate(worldPrefab, dropPos, Quaternion.identity);
        dropItem.GetComponent<Item>().count = originalSlot.count;
        dropItem.GetComponent<BounceEffect>().StartBounce();
        

        // Clear the slot
        originalSlot.currentItem = null;
        originalSlot.count = 0;
        originalSlot.RefreshCountText();

        // Destroy the UI item being dragged
        Destroy(gameObject);
    }

    Slot FindSlotUnderCursor()
    {
        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = Input.mousePosition;

        List<RaycastResult> result = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, result);

        foreach (RaycastResult r in result)
        {
            Slot s = r.gameObject.GetComponent<Slot>();
            if (s == null) s = r.gameObject.GetComponentInParent<Slot>();
            if (s != null) return s;
        }
        return null;
    }

    void DropOneInWorld()
    {
        Item draggedItem = GetComponent<Item>();
        GameObject worldPrefab = itemDictionary.GetItemPrefab(draggedItem.ID);
        if (worldPrefab == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minDropDistance, maxDropDistance);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        Vector2 dropPos = (Vector2)playerTransform.position + offset;

        GameObject dropItem = Instantiate(worldPrefab, dropPos, Quaternion.identity);
        dropItem.GetComponent<Item>().count = 1;
        dropItem.GetComponent<BounceEffect>().StartBounce();
    }

    void PlacedOneInSlot(Slot targetSlot)
    {
        Item draggedItem = GetComponent<Item>();
        GameObject prefab = itemDictionary.GetItemPrefab(draggedItem.ID);
        if (prefab == null) return;

        GameObject newItem = Instantiate(prefab, targetSlot.transform);
        newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        targetSlot.currentItem = newItem;
        targetSlot.count = 1;
        targetSlot.RefreshCountText();
    }

    void HandleRmbDuringDrag()
    {
        if (originalSlot == null) return;

        Slot targetSlot = FindSlotUnderCursor();

        // RMB on the source slot itself does nothing
        if (targetSlot == originalSlot) return;

        Item draggedItem = GetComponent<Item>();
        bool didAction = false;

        if (targetSlot == null)
        {
            DropOneInWorld();
            didAction = true;
        }
        else if (targetSlot.currentItem == null)
        {
            PlacedOneInSlot(targetSlot);
            didAction = true;
        }
        else
        {
            Item targetItem = targetSlot.currentItem.GetComponent<Item>();
            if (targetItem.ID == draggedItem.ID && targetSlot.count < draggedItem.maxStack)
            {
                targetSlot.count++;
                targetSlot.RefreshCountText();
                didAction = true;
            }
            // else: different itme or at max - no-op
        }

        if (!didAction) return;

        // Decrement source
        originalSlot.count--;
        originalSlot.RefreshCountText();

        //End drag if source is empty
        if (originalSlot.count <= 0)
        {
            originalSlot.currentItem = null;
            Destroy(gameObject);
        }
    }
}
