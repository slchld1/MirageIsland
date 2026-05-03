using UnityEngine;

/// <summary>
/// Drives the lure inward along the arena outward axis while LMB is held.
/// Reports distance reeled per frame so FishingController can roll hook chance.
/// </summary>
public class LureReeler : MonoBehaviour
{
    [Tooltip("Drag the FishingLine here so we can move BobPosition.")]
    public FishingLine fishingLine;

    private FishingTuning tuning;
    private FightArena arena;
    private bool active;

    public void Begin(FightArena fightArena, FishingTuning t)
    {
        arena = fightArena;
        tuning = t;
        active = true;
    }

    public void Stop() { active = false; }

    /// <summary>
    /// Step the reel forward this frame. Returns pixels-reeled-this-frame (0 if not reeling).
    /// </summary>
    public float Tick(bool lmbHeld, bool shiftHeld)
    {
        if (!active || !lmbHeld) return 0f;

        float speed = shiftHeld ? tuning.fastReelSpeed : tuning.slowReelSpeed;
        float pxThisFrame = speed * Time.deltaTime;
        float unitsThisFrame = pxThisFrame / 16f; // 16 PPU

        Vector2 cur = fishingLine.BobPosition;
        Vector2 inward = -arena.outward * unitsThisFrame;
        Vector2 next = cur + inward;
        fishingLine.SetBobPosition(next);

        return pxThisFrame;
    }

    /// <summary>True if the lure is now within shoreCatchThreshold of the player anchor.</summary>
    public bool IsAtShore()
    {
        return Vector2.Distance(fishingLine.BobPosition, arena.playerAnchor)
            <= tuning.shoreCatchThreshold;
    }

}