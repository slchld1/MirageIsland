using UnityEngine;

public class Bait : Item
{
    public BaitType baitType;

    [Header("Hook chance multipliers")]
    [Tooltip("Multiplier on per-second hook chance while lure is stationary")]
    public float waitHookMultiplier = 1.0f;

    [Tooltip("Multiplier on per-pixel hook chance while reeling")]
    public float reelHookMultiplier = 1.0f;

}
