using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a small panel near the hotbar when a FishingRod is the active hotbar slot.
/// Displays current bait type and count. Clicking opens bait selection (future).
/// Attach to a Canvas child GameObject. Assign panel, baitIcon, and baitCountText.
/// </summary>
public class BaitSlotUI : MonoBehaviour
{
    public GameObject panel;
    public Image baitIcon;
    public TMP_Text baitCountText;

    [Tooltip("Sprite shown when no bait is loaded")]
    public Sprite noBaitSprite;

    private FishingController fishingController;

    private void Awake()
    {
        fishingController = FindAnyObjectByType<FishingController>();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (fishingController == null) { panel.SetActive(false); return; }
        FishingRod rod = fishingController.ActiveRod;

        if (rod == null)
        {
            panel.SetActive(false);
            return;
        }

        panel.SetActive(true);
        baitCountText.text = rod.baitCount > 0 ? rod.baitCount.ToString() : "0";

        if (baitIcon != null && noBaitSprite != null && rod.equippedBait == BaitType.None)
            baitIcon.sprite = noBaitSprite;
    }

    // Wired to the bait slot Button's OnClick in the Inspector
    public void OnBaitSlotClicked()
    {
        // Future: open a filtered inventory view showing only Bait items
        Debug.Log("Bait slot clicked — open bait inventory here.");
    }
}
