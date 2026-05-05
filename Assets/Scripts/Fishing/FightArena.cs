using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Frozen-at-cast geometry snapshot for one fishing engagement.
/// Polar layout centered on the player. All fight-loop math reads these and never recomputes them.
/// Per-frame polar derivations (radius, angle, liveOutward, liveTangent) live on FishingFight.
/// </summary>
public struct FightArena
{
    public Vector2 playerAnchor;
    public Vector2 castOrigin; // rod tip at cast-land
    public Vector2 lurePosAtLand;
    public Vector2 castDir; // unit vector player -> lurePosAtLand. Defines greenzone center.
    public float castLandRadius; // distance player -> lurePosAtLand.
    public float greenMaxRadius; // outer greenzone bound = castLandRadius + greenRadiusBuffer.
    public float greenAngleDeg; // half-angle of greenzone sector.
    public float minRadius; // inner reference for catch math.
    public bool lateralKeysAreQE; // true = Q/E control lateral. false = W/S

    public bool IsValid; // false if Snapshot rejected the arena

    /// <summary>
    /// Convert a world position to polar (radius, angleDeg) relative to player+castDir.
    /// angleDeg is in [-180, 180], 0 = aligned with castDir.
    /// </summary>

    public Vector2 WorldToPolar(Vector2 world)
    {
        Vector2 d = world - playerAnchor;
        float r = d.magnitude;
        float a = Vector2.SignedAngle(castDir, d);
        return new Vector2(r, a);
    }

    /// <summary>
    /// Live "outward" axis: from player toward the lure right now.
    /// Falls back to castDir if lure is exactly on the player.
    /// </summary>
    
    public Vector2 LiveOutward(Vector2 lureWorld)
    {
        Vector2 d = lureWorld - playerAnchor;
        float m = d.magnitude;
        return m > 0.0001f ? d / m : castDir;
    }

    /// <summary>
    /// Live "tangent" axis: 90 degrees CCW from LiveOutward. +tangent = swing CCW around player.
    /// </summary>
    
    public Vector2 LiveTangent(Vector2 lureWorld)
    {
        Vector2 o = LiveOutward(lureWorld);
        return new Vector2(-o.y, o.x);
    }

    /// <summary>
    /// True if a world position is inside the greenzone sector.
    /// </summary>
    
    public bool IsInGreenZone(Vector2 world)
    {
        Vector2 polar = WorldToPolar(world);
        return polar.x <= greenMaxRadius && Mathf.Abs(polar.y) <= greenAngleDeg;
    }

    /// <summary>
    /// Build a polar arena snapshot at cast-land.
    /// IsValid=false if the greenzone is unusably small or all-land.
    /// </summary>

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

        a.castDir = raw / castMagnitude;
        a.castLandRadius = castMagnitude;
        a.greenAngleDeg = tuning.greenAngleDeg;
        a.greenMaxRadius = castMagnitude + tuning.greenRadiusBuffer;
        a.minRadius = tuning.minRadius;
        a.lateralKeysAreQE = Mathf.Abs(a.castDir.y) >= Mathf.Abs(a.castDir.x);

        // Greenzone all-land guard: if every sample inside the sector is land, cancel.
        if (GreenzoneIsAllLand(a, tuning, waterLayer)) return a;

        a.IsValid = true;
        return a;
    }

    private static bool GreenzoneIsAllLand(FightArena a, FishingTuning tuning, LayerMask waterLayer)
    {
        // Sample a fan of rays inside the sector. If any sample is water, accept the arena.
        const int angleSamples = 5;  // -1, -0.5, 0, +0.5, +1 of greenAngleDeg
        const int radiusSamples = 4;  // 0.25, 0.5, 0.75, 1.0 of greenMaxRadius
        for (int i = 0; i <= angleSamples; i++)
        {
            float t = (i / (float)(angleSamples - 1)) * 2f - 1f; // -1..+1
            float angleRad = (a.greenAngleDeg * t) * Mathf.Deg2Rad;
            float c = Mathf.Cos(angleRad);
            float s = Mathf.Sin(angleRad);
            Vector2 dir = new Vector2(
                a.castDir.x * c - a.castDir.y * s,
                a.castDir.x * s + a.castDir.y * c);
            for (int j = 1; j <= radiusSamples; j++)
            {
                float r = a.greenMaxRadius * (j / (float)radiusSamples);
                Vector2 p = a.playerAnchor + dir * r;
                if (Physics2D.OverlapPoint(p, waterLayer) != null) return false;
            }
        }
        return true;
    }
}
