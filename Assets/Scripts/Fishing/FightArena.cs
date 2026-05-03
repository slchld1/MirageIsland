using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Frozen-at-cast-land geometry snapshot for one fishing engagement.
/// All fight-loop math reads from these fields and never recomputes them.
/// Lateral axis is screen-aligned (world X for vertical-ish casts, world Y for horizontal-ish).
/// </summary>
public struct FightArena
{
    public Vector2 playerAnchor;
    public Vector2 castOrigin; // rod tip at cast-land
    public Vector2 lurePosAtLand;
    public Vector2 outward; //unit vector player -> lure
    public Vector2 lateral; // unit vector, screen-aligned, perpendicular axis
    public float maxOutward;
    public float lateralHalfW;

    public bool IsValid; // false if Snapshot rejected the arena

    /// <summary>
    /// Convert a world position into local arena coords (outward, lateral).
    /// </summary>
    
    public Vector2 WorldToLocal(Vector2 world)
    {
        Vector2 d = world - playerAnchor;
        return new Vector2(Vector2.Dot(d, outward), Vector2.Dot(d, lateral));
    }

    public Vector2 LocalToWorld(Vector2 local)
    {
        return playerAnchor + outward * local.x + lateral * local.y;
    }

    /// <summary>
    /// Build an arena snapshot from cast-land state.
    /// Returns IsValid=false if land-aware shrinking failed or cast-land guard tripped.
    /// </summary>
    /// 

    public static FightArena Snapshot(
        Vector2 playerAnchor,
        Vector2 castOrigin,
        Vector2 lurePosAtLand,
        FishingTuning tuning,
        LayerMask waterLayer)
    {
        FightArena a = new FightArena
        {
            playerAnchor = playerAnchor,
            castOrigin = castOrigin,
            lurePosAtLand = lurePosAtLand,
            IsValid = false,
        };

        Vector2 raw = lurePosAtLand - playerAnchor;
        float castMagnitude = raw.magnitude;
        if (castMagnitude < 0.0001f) return a;

        a.outward = raw / castMagnitude;

        // Lateral axis: screen-aligned. Whichever world axis the cast is closer to,
        // its perpendicular world axis becomes lateral.
        bool castIsMoreVertical = Mathf.Abs(raw.y) >= Mathf.Abs(raw.x);
        a.lateral = castIsMoreVertical ? Vector2.right : Vector2.up;

        // Initial lateral half-width: pick a generous starting width then shrink.
        // Start at half the cast distance, capped by some sane max.
        a.lateralHalfW = Mathf.Min(castMagnitude * 0.5f, 8f);
        a.maxOutward = castMagnitude;

        // Cast-land guard: arena would alredy be inside catch range
        if (a.maxOutward < tuning.shoreCatchThreshold + tuning.reelStartBuffer) return a;

        // Centerline land check: sample from playerAnchor toward lurePosAtLand
        if (CenterlineHitsLand(a, tuning.landSampleDensity, waterLayer)) return a;

        // Symmetrically shrink lateralHalfW until both lateral escape lines are water-clean
        // OR we drop below minLateralHalfWidth
        while (a.lateralHalfW >= tuning.minLateralHalfWidth)
        {
            if (LateralLinesAreWaterClean(a, tuning.landSampleDensity, waterLayer)) break;
            a.lateralHalfW -= tuning.lateralShrinkStep;
        }
        if (a.lateralHalfW < tuning.minLateralHalfWidth) return a;

        a.IsValid = true;
        return a;
    }

    private static bool CenterlineHitsLand(FightArena a, int samples, LayerMask waterLayer)
    {
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 p = Vector2.Lerp(a.playerAnchor, a.lurePosAtLand, t);
            if (Physics2D.OverlapPoint(p, waterLayer) == null) return true;
        }
        return false;
    }

    private static bool LateralLinesAreWaterClean(FightArena a, int samples, LayerMask waterLayer)
    {
        // Left and right escape lines run from (outward=0, lateral=±halfW) to (outward=maxOutward, lateral=±halfW)
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 local = new Vector2(t * a.maxOutward, side * a.lateralHalfW);
                Vector2 world = a.LocalToWorld(local);
                if (Physics2D.OverlapPoint(world, waterLayer) == null) return false;
            }
        }
        return true;
    }
}
