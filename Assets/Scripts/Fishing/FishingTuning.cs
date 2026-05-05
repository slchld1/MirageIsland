using UnityEngine;

[CreateAssetMenu(menuName = "Fishing/Fishing Tuning", fileName = "FishingTuning")]
public class FishingTuning : ScriptableObject
{
    [Header("Cast / charge")]
    public float minCastDistance = 2.0f; // units (32px @ 16ppu)
    public float chargeOscillationRate = 0.8f;
    public float maxSpeedMultiplier = 1.5f;

    [Header("Hook math")]
    public float waitHookChancePerSecond = 0.06f; // 6% per second
    public float reelHookChancePerPixel = 0.01125f; // 9% per 8px
    public float slowReelSpeed = 16f; // px/s = 1 unit/s
    public float fastReelSpeed = 32f; // px/s = 2 unit/s

    [Header("Arena geometry (units)")]
    public float shoreCatchThreshold = 0.5f;
    public float nearShoreThreshold = 1.0f;
    public float reelStartBuffer = 0.5f;
    public int landSampleDensity = 8;
    public float lateralShrinkStep = 0.5f;
    public float minLateralHalfWidth = 4f;

    [Header("Fight - fish behavior")]
    public float baseFishStrength = 30f; // playtest
    public float baseDartChance = 0.5f;
    public float dartDurationBase = 0.6f; // rarity-scaled
    public float dartDurationPerRarity = 0.1f;
    public float restDurationBase = 1.0f;
    public float restDurationPerRarity = -0.1f; // shorter at high rarity
    public float dartStaminaCost = 25f;
    public float fishStaminaMaxBase = 100f;
    public float fishStaminaMaxPerRarity = 25f;
    public float staminaRegenWhileFollowing = 5f;
    public float staminaRegenWhileResting = 40f;
    public float followSpeed = 4f;

    [Tooltip("Three weights for lateral-L / lateral-R / straight-out rarity 1. Normalized internally.")]
    public Vector3 dartWeightsRarity1 = new Vector3(0.40f, 0.40f, 0.20f);
    public Vector3 dartWeightsRarity5 = new Vector3(0.30f, 0.30f, 0.40f);

    [Header("Fight - player input forces")]
    public float lateralHoldForce = 25f;
    public float lateralTapImpulse = 8f;
    public float straightOutTapImpulse = 5f;
    public float fleeWarnTapImpulse = 18f;
    public float fleeWarnStraightOutTapImpulse = 12f;
    [Range(0f, 1f)] public float fleeWarnThreshold = 0.85f;
    public float fleeWarnUiPulseRate = 4f;

    [Header("Tension")]
    public float tensionMax = 100f;
    public float tensionDecayPerSecond = 5f;
    public float tensionSnapWindow = 0.1f;

    [Header("Bite-flee opening")]
    public float biteFleeDuration = 0.45f;
    public float biteFleeStrength = 35f;
    public float biteFleeFollowSpeed = 6f;
    [Range(0f, 1f)] public float biteFleeInputDamp = 0.30f;
    public float biteFleeTensionPunch = 50f;

    [Header("Forced cancel")]
    public float playerMoveCancelDistance = 0.1f;

    [Header("Polar arena (revision 2026-05-03)")]
    [Tooltip("Half-angle of the greenzone sector, degrees. Lure outside |angle| > this builds tension.")]
    public float greenAngleDeg = 45f;

    [Tooltip("Greenzone extends this far past castLandRadius (units). Lure outside builds tension.")]
    public float greenRadiusBuffer = 1.5f;

    [Tooltip("Tension/sec added when lure is just outside greenzone. Scales up with severity.")]
    public float outOfZoneTensionRate = 30f;

    [Tooltip("Catch progress per second when centeringScore = 1.0 (perfectly centered + reeled in).")]
    public float catchFillRate = 0.5f;

    [Tooltip("Catch progress drained per second while fish is darting (any dart state).")]
    public float catchDecayRate = 0.1f;

    [Tooltip("Per-frame velocity damping for lure inertia. 0.92 = ~8% bleed per frame.")]
    public float lureDampingPerFrame = 0.92f;

    [Tooltip("Inner reference radius (units). Lure cannot meaningfully come closer than this.")]
    public float minRadius = 0.5f;

    [Header("Land detection")]
    [Tooltip("Layer mask for water tiles. Used to clamp fish + lure off land. Match the cast lander mask.")]
    public LayerMask waterLayerMask;
}
