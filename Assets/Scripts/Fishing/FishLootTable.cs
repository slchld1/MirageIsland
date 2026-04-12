using System.Collections.Generic;
using UnityEngine;

public class FishLootTable : MonoBehaviour
{
    public static FishLootTable Instance { get; private set; }

    [Tooltip("All fish species. Add new entries here to expand the loot pool.")]
    public FishData[] fishEntries;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Returns the itemID of a randomly selected fish based on rod tier, bait, and time of day.
    /// Returns -1 if the pool is empty.
    /// </summary>
    public int Roll(int rodTier, BaitType bait, TimeOfDay phase)
    {
        var pool = new List<(int itemID, int weight)>();

        foreach (FishData fish in fishEntries)
        {
            int weight = fish.baseWeight
                + GetPhaseBonus(fish, phase)
                + GetBaitBonus(fish, bait)
                + GetRodBonus(fish, rodTier);

            if (weight > 0)
                pool.Add((fish.itemID, weight));
        }

        if (pool.Count == 0) return -1;

        int total = 0;
        foreach (var entry in pool) total += entry.weight;

        int roll = Random.Range(0, total);
        int cumulative = 0;
        foreach (var entry in pool)
        {
            cumulative += entry.weight;
            if (roll < cumulative) return entry.itemID;
        }

        return pool[pool.Count - 1].itemID;
    }

    private int GetPhaseBonus(FishData fish, TimeOfDay phase)
    {
        return phase switch
        {
            TimeOfDay.PreDawn => fish.preDawnBonus,
            TimeOfDay.Dawn    => fish.dawnBonus,
            TimeOfDay.Day     => fish.dayBonus,
            TimeOfDay.Dusk    => fish.duskBonus,
            TimeOfDay.Night   => fish.nightBonus,
            _                 => 0
        };
    }

    private int GetBaitBonus(FishData fish, BaitType bait)
    {
        if (fish.baitBonuses == null) return 0;
        foreach (BaitBonusEntry entry in fish.baitBonuses)
            if (entry.baitType == bait) return entry.bonus;
        return 0;
    }

    private int GetRodBonus(FishData fish, int rodTier)
    {
        if (fish.rodTierBonuses == null || rodTier - 1 >= fish.rodTierBonuses.Length) return 0;
        return fish.rodTierBonuses[rodTier - 1];
    }
}
