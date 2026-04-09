using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    public bool IsOpened { get; private set; }
    public string ChestID { get; private set; }
    public GameObject itemPrefab;
    public Sprite openedSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChestID ??= GlobalHelper.GenerateUniqueId(gameObject); //UniqueID
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public bool CanInteract()
    {
        return !IsOpened;
    }

    public void Interact()
    {
        if(!CanInteract()) return;
        OpenChest();
    }
    private void OpenChest()
    {
        SetOpened(true);


        //DropItem
        if(itemPrefab)
        {
            GameObject droppedItem = Instantiate(itemPrefab, transform.position + Vector3.down, Quaternion.identity);
            droppedItem.GetComponent<BounceEffect>().startBounce();
        }

    }

    public void SetOpened(bool opened)
    {
        if(IsOpened = opened)
        {
            GetComponent<SpriteRenderer>().sprite = openedSprite;
        }
    }
}
