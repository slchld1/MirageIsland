using UnityEngine;

public class FishingRod : Item
{
    [Range(1, 3)]
    public int rodTier = 1;

    // Set by BaitSlotUI when bait is loaded
    [HideInInspector] public BaitType equippedBait = BaitType.None;
    [HideInInspector] public int baitCount = 0;

    public override void UseItem()
    {
        // Fishing is triggered by LMB click via FishingController.
        // Pressing the hotbar key just selects this slot.
    }
}
