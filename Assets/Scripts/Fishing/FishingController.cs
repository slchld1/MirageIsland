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
    [SerializeField] private CatchBarUI catchBarUI;

    [Header("Fish")]
    [Tooltip("Prefab with Fish.cs + SpriteRenderer; spawned at lure on hook")]
    public GameObject fishPrefab;

    public FishingRod ActiveRod { get; private set; }
    public float ChargeLevel => chargeLevel;

    private HotbarController hotbarController;
    private Inventory         inventory;
    private ItemDictionary    itemDictionary;
    private FishingLine       fishingLine;

    private FishingState state     = FishingState.Idle;
    private FightArena arena;
    private float chargeLevel;
    private float chargeDir = 1f;

    private LureReeler lureReeler;
    private Fish activeFish;
    private FishingFight activeFight;
    private FishData rolledFish;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        hotbarController = FindAnyObjectByType<HotbarController>();
        inventory        = FindAnyObjectByType<Inventory>();
        itemDictionary   = FindAnyObjectByType<ItemDictionary>();
        fishingLine      = GetComponent<FishingLine>();
        lureReeler       = GetComponent<LureReeler>();

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

        arena = FightArena.Snapshot(
            playerAnchor: transform.position,
            castOrigin: fishingLine.RodTipPosition,
            lurePosAtLand: fishingLine.BobPosition,
            tuning: tuning,
            waterLayer: waterLayer);

        if (!arena.IsValid)
        {
            EndFishing();
            return;
        }

        lureReeler.Begin(arena, tuning);
        lureSubMode = LureSubMode.Stationary;
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
        bool lmbHeld = Mouse.current.leftButton.isPressed;
        bool shiftHeld = Keyboard.current?.leftShiftKey.isPressed ?? false;

        // Sub-mode transition driven by LMB
        lureSubMode = lmbHeld ? LureSubMode.Reeling : LureSubMode.Stationary;

        float waitMult = ActiveBaitWaitMultiplier();
        float reelMult = ActiveBaitReelMultiplier();

        if (lureSubMode == LureSubMode.Stationary)
        {
            float p = tuning.waitHookChancePerSecond * waitMult * Time.deltaTime;
            if (Random.value < p) { OnFishHooked(); return; }
        }
        else // Reeling
        {
            float pxReeled = lureReeler.Tick(lmbHeld, shiftHeld);
            float p = tuning.reelHookChancePerPixel * pxReeled * reelMult;
            if (Random.value < p)
            {
                Debug.Log($"[Fishing] Stationary hit (p={p:F4})");
                OnFishHooked();
                return;
            }
            // Empty reel-back end-of-cast
            if (lureReeler.IsAtShore()) { EndFishing(); return; }
        }
    }

    private float ActiveBaitWaitMultiplier()
    {
        Bait bait = ActiveBait();
        return bait != null ? bait.waitHookMultiplier : 1f;
    }

    private float ActiveBaitReelMultiplier()
    {
        Bait bait = ActiveBait();
        return bait != null ? bait.reelHookMultiplier : 1f;
    }

    private Bait ActiveBait()
    {
        if (ActiveRod == null || ActiveRod.equippedBait == BaitType.None) return null;
        if (itemDictionary == null) return null;

        foreach (Item item in itemDictionary.itemPrefabs)
        {
            Bait bait = item as Bait;
            if (bait != null && bait.baitType == ActiveRod.equippedBait) return bait;
        }
        return null;

    }


    private void OnFishHooked()
    {
        // Bait consumed on hook (NOT on cast)
        if (ActiveRod != null && ActiveRod.equippedBait != BaitType.None)
        {
            ActiveRod.baitCount--;
            if (ActiveRod.baitCount <= 0)
            {
                ActiveRod.baitCount = 0;
                ActiveRod.equippedBait = BaitType.None;
            }
        }
        lureReeler.Stop();

        // Roll the fish species
        int rodTier = ActiveRod != null ? ActiveRod.rodTier : 1;
        BaitType baitT = ActiveRod != null ? ActiveRod.equippedBait : BaitType.None;
        TimeOfDay phase = DayCycleManager.Instance.CurrentPhase;
        rolledFish = FishLootTable.Instance.Roll(rodTier, baitT, phase);

        // Spawn fish + fight
        Vector2 lurePos = fishingLine.BobPosition;
        GameObject fishGo = Instantiate(fishPrefab, lurePos, Quaternion.identity);
        activeFish = fishGo.GetComponent<Fish>();
        activeFight = gameObject.AddComponent<FishingFight>();
        activeFight.Init(arena, tuning, fishingLine, activeFish, this);
        activeFish.Init(rolledFish, arena, tuning, activeFight, lurePos);

        // Apply bite-flee tension punch (one-shot — the punch is implemented in Phase 6)
        activeFight.PunchTension(tuning.biteFleeTensionPunch);

        Debug.Log($"[Fishing] HOOKED via {lureSubMode}. Bait left: {(ActiveRod != null ? ActiveRod.baitCount : 0)}");
        state = FishingState.FishHooked;
        catchBarUI?.Show();
        SoundEffectManager.Play("FishBite");

    }
    // ── FishHooked (placeholder) ─────────────────────────────────────────────
    private void UpdateFishHooked() 
    {
        activeFish.Tick(Time.deltaTime);
        activeFight.Tick(Time.deltaTime);

        if (activeFight != null) catchBarUI?.OnProgressChanged(activeFight.CatchProgress);

        if (!activeFish.IsBiteFleeing)
        {
            state = FishingState.FightingFish;
        }
    }

    // ── FightingFish (placeholder) ───────────────────────────────────────────
    private void UpdateFightingFish()
    {
        activeFish.Tick(Time.deltaTime);
        activeFight.Tick(Time.deltaTime);

        if (activeFight != null) catchBarUI?.OnProgressChanged(activeFight.CatchProgress);
    }

    private void EndFightWithEscape()
    {
        SoundEffectManager.Play("FishEscape");
        CleanupFight();
        Debug.Log("[Fishing] ESCAPED");
        state = FishingState.Idle;
        IsFishing = false;
        ActiveRod = null;
        fishingLine.Hide();
    }

    private void EndFightWithCatch()
    {
        if (rolledFish != null)
        {
            GameObject prefab = itemDictionary.GetItemPrefab(rolledFish.itemID);
            if (prefab != null) inventory.AddItem(prefab);
        }
        SoundEffectManager.Play("FishCatch");
        CleanupFight();
        Debug.Log($"[Fishing] CAUGHT: {rolledFish?.fishName}");
        state = FishingState.Idle;
        IsFishing = false;
        ActiveRod = null;
        fishingLine.Hide();
    }

    public void NotifyEscape()
    {
        EndFightWithEscape();
    }

    public void NotifyCatch()
    {
        EndFightWithCatch();
    }

    private void CleanupFight()
    {
        catchBarUI?.Hide();
        if (activeFish != null) Destroy(activeFish.gameObject);
        if (activeFight != null) Destroy(activeFight);
        activeFish = null;
        activeFight = null;
        rolledFish = null;
    }

    private void EndFishing()
    {
        Debug.Log($"[Fishing] END from state={state}");
        castChargeUI?.Hide();
        fishingLine.Hide();
        state = FishingState.Idle;
        IsFishing = false;
        ActiveRod = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!arena.IsValid) return;

        Vector2 anchor = arena.playerAnchor;

        // Centerline (player -> cast landing)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(anchor, anchor + arena.castDir * arena.castLandRadius);

        // Greenzone sector
        Gizmos.color = Color.green;
        const int sectorSegments = 24;
        Vector2 prevArc = anchor + RotateVec(arena.castDir, -arena.greenAngleDeg) * arena.greenMaxRadius;
        Gizmos.DrawLine(anchor, prevArc); // first edge ray
        for (int i = 1; i <= sectorSegments; i++)
        {
            float t = (float)i / sectorSegments;
            float deg = Mathf.Lerp(-arena.greenAngleDeg, arena.greenAngleDeg, t);
            Vector2 next = anchor + RotateVec(arena.castDir, deg) * arena.greenMaxRadius;
            Gizmos.DrawLine(prevArc, next);
            prevArc = next;
        }
        Gizmos.DrawLine(prevArc, anchor); // closing edge ray

        // Inner refernce radius
        Gizmos.color = Color.yellow;
        const int innerSegments = 24;
        Vector2 prevInner = anchor + Vector2.right * arena.minRadius;
        for (int i = 1; i <= innerSegments; i++)
        {
            float a = (i / (float)innerSegments) * Mathf.PI * 2f;
            Vector2 next = anchor + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * arena.minRadius;
            Gizmos.DrawLine(prevArc, next);
            prevInner = next;
        }
    }

    private static Vector2 RotateVec(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r);
        float s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
