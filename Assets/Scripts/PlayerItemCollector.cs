using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    private Inventory inventoryController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventoryController = FindAnyObjectByType<Inventory>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Item"))
        {
            Item item = collision.GetComponent<Item>();
            if(item != null)
            {
                //Add to inventory
                bool itemAdded = inventoryController.AddItem(collision.gameObject);

                if(itemAdded)
                {
                    item.PickUp();
                    Destroy(collision.gameObject);
                }
            }
        }
    }

}
