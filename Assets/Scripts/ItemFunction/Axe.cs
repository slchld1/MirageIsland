using UnityEngine;

public class Axe : Item
{
    [Header("Chop")]
    public int damage = 1;

    public override void UseItem()
    {
        // no-op - chopping is driven by LMB Action not selection via PlayerToolDispatcher

    }
}
