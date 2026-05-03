using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public enum FishingState { Idle, Charging, Casting, LureInWater, FishHooked, FightingFish, Caught, Escaped }
public enum LureSubMode { Stationary, Reeling }

/// <summary>
/// State machine: Idle → Charging → Casting → Waiting → Minigame → back.
/// Attach to the Player GameObject alongside FishingLine, FishBiteDetector, TugMinigame.
/// Requires a "Water" physics layer in Project Settings → Tags and Layers.
/// </summary>
public class FishingController : MonoBehaviour
{
    public static FishingController Instance { get; private set; }
    public static bool IsFishing { get; private set; }

    [Header("Water Detection")]
    [Tooltip("Set to the 'Water' layer in Project Settings")]
    public LayerMask waterLayer;

    [Header("Tuning")]
    [Tooltip("Drag FishingTuning.asset here")]
    public FishingTuning tuning;

    [Header("Cast Charge")]
    [SerializeField] private CastChargeUI castChargeUI;

    public FishingRod ActiveRod { get; private set; }
    public float ChargeLevel => chargeLevel;

    private HotbarController hotbarController;
    private Inventory         inventory;
    private ItemDictionary    itemDictionary;
    private FishingLine       fishingLine;

    private FishingState state     = FishingState.Idle;
    private FishData     rolledFish;
    private float chargeLevel;
    private float chargeDir = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        hotbarController = FindAnyObjectByType<HotbarController>();
        inventory        = FindAnyObjectByType<Inventory>();
        itemDictionary   = FindAnyObjectByType<ItemDictionary>();
        fishingLine      = GetComponent<FishingLine>();

    }

    private void Start()
    {

    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        switch (state)
        {
            case FishingState.Idle:     UpdateIdle();     break;
            case FishingState.Charging: UpdateCharging(); break;
            case FishingState.Casting:  UpdateCasting();  break;
            case FishingState.LureInWater: UpdateLureInWater(); break;
            case FishingState.FishHooked: UpdateFishHooked(); break;
            case FishingState.FightingFish: UpdateFightingFish(); break;
        }
    }

    // ── Idle ─────────────────────────────────────────────────────────────────

    private void UpdateIdle()
    {
        Item activeItem = hotbarController.GetActiveItem();
        ActiveRod = activeItem as FishingRod;
        if (ActiveRod == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            EnterCharging();
    }

    private void ExecuteCast(float charge)
    {
        castChargeUI?.Hide();

        Vector3 screenPos = Mouse.current.position.ReadValue();
        screenPos.z = -Camera.main.transform.position.z;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(screenPos);

        Vector2 playerPos = transform.position;
        Vector2 dir = mouseWorld - playerPos;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir = dir.normalized;

        float maxDist  = ActiveRod != null ? ActiveRod.castDistance : 3f;
        float castDist = Mathf.Lerp(tuning.minCastDistance, maxDist, charge);

        float baseSpeed = ActiveRod != null ? ActiveRod.castSpeed : 6f;
        float castSpeed = Mathf.Lerp(baseSpeed, baseSpeed * tuning.maxSpeedMultiplier, charge);

        Vector2 target = playerPos + dir * castDist;

        fishingLine.Cast(target, castSpeed, OnCastLanded);
        state = FishingState.Casting;
        SoundEffectManager.Play("FishCast");
    }

    // ── Casting ───────────────────────────────────────────────────────────────

    private void UpdateCasting()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
            CancelFishing();
    }

    private void OnCastLanded()
    {
        Collider2D hit = Physics2D.OverlapPoint(fishingLine.BobPosition, waterLayer);
        if (hit == null) { EndFishing(); return; }

        // add FightingArena.Snapshot here
        lureSubMode = LureSubMode.Stationary;
        lureInWaterTimer = 3f;
        state = FishingState.LureInWater;
    }


    // ── Charging ──────────────────────────────────────────────────────────────

    private void EnterCharging()
    {
        chargeLevel = 0f;
        chargeDir   = 1f;
        state       = FishingState.Charging;
        IsFishing   = true;
        castChargeUI?.Show();
    }

    private void UpdateCharging()
    {
        // Guard against hotbar swap during charge
        if (!(hotbarController.GetActiveItem() is FishingRod))
        {
            CancelFishing();
            return;
        }

        // Cancel on RMB
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelFishing();
            return;
        }

        // Oscillate charge 0 → 1 → 0 → ...
        chargeLevel += chargeDir * tuning.chargeOscillationRate * Time.deltaTime;
        if (chargeLevel >= 1f) { chargeLevel = 1f; chargeDir = -1f; }
        else if (chargeLevel <= 0f) { chargeLevel = 0f; chargeDir =  1f; }

        castChargeUI?.OnChargeChanged(chargeLevel);

        // Fire on LMB release
        if (Mouse.current.leftButton.wasReleasedThisFrame)
            ExecuteCast(chargeLevel);
    }


    // ── Events ────────────────────────────────────────────────────────────────


    private void CancelFishing()
    {
        EndFishing();
    }



    private LureSubMode lureSubMode = LureSubMode.Stationary;
    private float lureInWaterTimer; // TEMP for phase 2 stub

    // ── LureInWater (STUB — Phase 2) ─────────────────────────────────────────
    private void UpdateLureInWater()
    {
        // Phase 2 stub: just count down 3 seconds and end fishing. Phases 3+ replace this.
        lureInWaterTimer -= Time.deltaTime;
        if (lureInWaterTimer <= 0f) EndFishing();
    }

    // ── FishHooked (placeholder) ─────────────────────────────────────────────
    private void UpdateFishHooked() { /* Phase 5 fills in bite-flee opening */ }

    // ── FightingFish (placeholder) ───────────────────────────────────────────
    private void UpdateFightingFish() { /* Phase 6 fills in fight loop */ }

    private void EndFishing()
    {
        castChargeUI?.Hide();
        fishingLine.Hide();
        state = FishingState.Idle;
        IsFishing = false;
        ActiveRod = null;
    }
}
