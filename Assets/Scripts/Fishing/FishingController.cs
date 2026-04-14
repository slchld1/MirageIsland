using UnityEngine;
using UnityEngine.InputSystem;

public enum FishingState { Idle, Charging, Casting, Waiting, Minigame }

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

    [Header("Cast Charge")]
    [Tooltip("Shortest cast distance at zero charge (world units)")]
    public float minCastDistance = 0.5f;
    [Tooltip("Oscillation speed — full cycles (0→1→0) per second")]
    public float chargeRate = 0.8f;
    [Tooltip("Cast speed multiplier at full charge, relative to rod.castSpeed")]
    public float maxSpeedMultiplier = 1.5f;
    [SerializeField] private CastChargeUI castChargeUI;

    public FishingRod ActiveRod { get; private set; }
    public float ChargeLevel => chargeLevel;

    private HotbarController hotbarController;
    private Inventory         inventory;
    private ItemDictionary    itemDictionary;
    private FishingLine       fishingLine;
    private FishBiteDetector  biteDetector;
    private TugMinigame       tugMinigame;
    private TugMinigameUI     tugMinigameUI;

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
        biteDetector     = GetComponent<FishBiteDetector>();
        tugMinigame      = GetComponent<TugMinigame>();
        tugMinigameUI    = FindAnyObjectByType<TugMinigameUI>();
    }

    private void Start()
    {
        biteDetector.OnBite  += OnFishBite;
        tugMinigame.OnCatch  += OnFishCaught;
        tugMinigame.OnEscape += OnFishEscaped;
    }

    private void OnDestroy()
    {
        if (biteDetector != null) biteDetector.OnBite  -= OnFishBite;
        if (tugMinigame  != null)
        {
            tugMinigame.OnCatch  -= OnFishCaught;
            tugMinigame.OnEscape -= OnFishEscaped;
        }
    }

    private void Update()
    {
        switch (state)
        {
            case FishingState.Idle:     UpdateIdle();     break;
            case FishingState.Charging: UpdateCharging(); break;
            case FishingState.Casting:  UpdateCasting();  break;
            case FishingState.Waiting:  UpdateWaiting();  break;
            case FishingState.Minigame: UpdateMinigame(); break;
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
        float castDist = Mathf.Lerp(minCastDistance, maxDist, charge);

        float baseSpeed = ActiveRod != null ? ActiveRod.castSpeed : 6f;
        float castSpeed = Mathf.Lerp(baseSpeed, baseSpeed * maxSpeedMultiplier, charge);

        Vector2 target = playerPos + dir * castDist;

        fishingLine.NudgeEnabled = false;
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
        if (hit == null)
        {
            CancelFishing();
            return;
        }
        fishingLine.NudgeEnabled = true;
        biteDetector.StartDetection();
        state = FishingState.Waiting;
    }

    // ── Waiting ───────────────────────────────────────────────────────────────

    private void UpdateWaiting()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame
            || !(hotbarController.GetActiveItem() is FishingRod))
        {
            CancelFishing();
            return;
        }

        float proximityBonus = fishingLine.GetProximityBonus();
        biteDetector.Tick(proximityBonus);
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
        chargeLevel += chargeDir * chargeRate * Time.deltaTime;
        if (chargeLevel >= 1f) { chargeLevel = 1f; chargeDir = -1f; }
        else if (chargeLevel <= 0f) { chargeLevel = 0f; chargeDir =  1f; }

        castChargeUI?.OnChargeChanged(chargeLevel);

        // Fire on LMB release
        if (Mouse.current.leftButton.wasReleasedThisFrame)
            ExecuteCast(chargeLevel);
    }

    // ── Minigame ──────────────────────────────────────────────────────────────

    private void UpdateMinigame()
    {
        bool holding     = Mouse.current.leftButton.isPressed;
        bool justPressed = Mouse.current.leftButton.wasPressedThisFrame;
        bool pressedQ    = Keyboard.current?.qKey.wasPressedThisFrame ?? false;
        bool pressedE    = Keyboard.current?.eKey.wasPressedThisFrame ?? false;
        tugMinigame.Tick(holding, justPressed, pressedQ, pressedE);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void OnFishBite()
    {
        // Roll the fish species now so we have its fight profile for the minigame
        int       rodTier = ActiveRod != null ? ActiveRod.rodTier      : 1;
        BaitType  bait    = ActiveRod != null ? ActiveRod.equippedBait : BaitType.None;
        TimeOfDay phase   = DayCycleManager.Instance.CurrentPhase;
        rolledFish        = FishLootTable.Instance.Roll(rodTier, bait, phase);

        state = FishingState.Minigame;
        fishingLine.NudgeEnabled = false;
        tugMinigame.StartMinigame(rodTier, rolledFish);
        tugMinigameUI.Show();
        SoundEffectManager.Play("FishBite");
    }

    private void OnFishCaught()
    {
        tugMinigameUI.Hide();

        if (rolledFish != null)
        {
            GameObject fishPrefab = itemDictionary.GetItemPrefab(rolledFish.itemID);
            if (fishPrefab != null)
                inventory.AddItem(fishPrefab);
        }

        // Consume one bait on success
        if (ActiveRod != null && ActiveRod.equippedBait != BaitType.None)
        {
            ActiveRod.baitCount--;
            if (ActiveRod.baitCount <= 0)
            {
                ActiveRod.baitCount    = 0;
                ActiveRod.equippedBait = BaitType.None;
            }
        }

        rolledFish = null;
        SoundEffectManager.Play("FishCatch");
        CancelFishing();
    }

    private void OnFishEscaped()
    {
        tugMinigameUI.Hide();
        tugMinigame.StopMinigame();
        fishingLine.NudgeEnabled = true;
        biteDetector.ResetForNextFish();
        rolledFish = null;
        state = FishingState.Waiting;
        SoundEffectManager.Play("FishEscape");
    }

    private void CancelFishing()
    {
        castChargeUI?.Hide();
        fishingLine.Hide();
        fishingLine.NudgeEnabled = true;
        biteDetector.StopDetection();
        tugMinigame.StopMinigame();
        tugMinigameUI.Hide();
        rolledFish = null;
        state      = FishingState.Idle;
        IsFishing  = false;
        ActiveRod  = null;
    }
}
