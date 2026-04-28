using UnityEngine;
using TMPro;

public class Slot : MonoBehaviour
{
    public GameObject currentItem; //The item currently held in the slot

    public int count = 0;
    public TextMeshProUGUI countText;

    public void RefreshCountText()
    {
        if (countText == null) return;

        if (count > 1)
        {
            countText.text = count.ToString();
            countText.gameObject.SetActive(true);
        }
        else
        {
            countText.gameObject.SetActive(false);
        }
    }

}
