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
        if (!collision.CompareTag("Item")) return;

        Item item = collision.GetComponent<Item>();
        if (item == null) return;

        if (!collision.enabled) return; //already being processed
        collision.enabled = false; // lock immediately

        bool itemAdded = inventoryController.AddItem(collision.gameObject);

        if (itemAdded)
        {
            item.PickUp();
            Destroy(collision.gameObject);
        }
        else
        {
            collision.enabled = true;           // inventory full - let player retry
        }
     
    }

}
