using UnityEditor.Build;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    private Transform playerTransform;

    public float minDropDistance = 1f;
    public float maxDropDistance = 2f;


    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent; //Save OG Parent
        transform.SetParent(transform.root); //Above other canvas'
        canvasGroup.blocksRaycasts = false; //
        canvasGroup.alpha = 0.6f; //semi-transparent during drag

    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position; //Follow the mouse
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        
        canvasGroup.blocksRaycasts = true; //Enable raycasts
        canvasGroup.alpha = 1f; //No longer transparent

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>(); //Slot where item dropped
        if (dropSlot == null )
        {
            GameObject dropItem = eventData.pointerEnter;
            if (dropItem != null )
            {
                dropSlot = dropItem.GetComponentInParent<Slot>();
            }
        }
        Slot originalSlot = originalParent.GetComponent<Slot>();

        if (dropSlot != null) 
        { 
            //Is a slot under drop point
            if(dropSlot.currentItem != null)
            {
                //Slot has an item = swap items
                dropSlot.currentItem.transform.SetParent(originalSlot.transform);
                originalSlot.currentItem = dropSlot.currentItem;
                dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }
            else
            {
                originalSlot.currentItem = null;
            }
            
            //Move item into drop slot
            transform.SetParent(dropSlot.transform);
            dropSlot.currentItem = gameObject;
        }
        else
        {
            //If where we're dropping is not within the inventory
            if(!IsWithinInventory(eventData.position))
            {
                //Drop item
                DropItem(originalSlot);
            }else
            {
                //No slot under drop point
                transform.SetParent(originalParent);
            }
        }

        GetComponent<RectTransform>().anchoredPosition = Vector2.zero; //Center

    }

    bool IsWithinInventory(Vector2 mousePosition)
    {
        RectTransform inventoryRect = originalParent.parent.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, mousePosition);
    }

    void DropItem(Slot originalSlot)
    {
        originalSlot.currentItem = null;

        if (playerTransform == null)
        {
            Debug.LogError("Missing 'Player' tag");
            return;
        }
        //Player facing direction
        Vector2 facingDirection = Vector2.down;

        //Random drop position
        float dropOffset = Random.Range(minDropDistance, maxDropDistance);
        Vector2 dropPosition = (Vector2)playerTransform.position + (facingDirection * dropOffset);
        
        //Horizontal scatter to prevent stacking
        float horizontalScatter = Random.Range(-0.5f, 0.5f);
        dropPosition.x += horizontalScatter;

        //Instantiate drop item
        GameObject dropItem = Instantiate(gameObject, dropPosition, Quaternion.identity);
        dropItem.GetComponent<BounceEffect>().StartBounce();
        //Destroy the UI one
        Destroy(gameObject);

    }

}
