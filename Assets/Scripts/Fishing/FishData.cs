using UnityEngine;

[System.Serializable]
public class BaitBonusEntry
{
    public BaitType baitType;
    public int bonus;
}

[System.Serializable]
public class FishData
{
    public string fishName;
    public int itemID;           // must match an ID in ItemDictionary
    public int baseWeight;

    [Header("Time of Day Bonuses")]
    public int preDawnBonus;
    public int dawnBonus;
    public int dayBonus;
    public int duskBonus;
    public int nightBonus;

    [Header("Bait Bonuses")]
    public BaitBonusEntry[] baitBonuses;

    [Header("Rod Tier Bonuses")]
    [Tooltip("Index 0 = tier 1, index 1 = tier 2, index 2 = tier 3")]
    public int[] rodTierBonuses;
}
