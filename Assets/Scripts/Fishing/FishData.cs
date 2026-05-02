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
    public int itemID;
    [Tooltip("Base selection weight. Higher = more common.")]
    public int baseWeight;

    [Header("Fight Profile")]
    [Range(1, 5)]
    [Tooltip("1 = common (forgiving), 5 = legendary (tight windows, fast events)")]
    public int rarity = 1;

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
