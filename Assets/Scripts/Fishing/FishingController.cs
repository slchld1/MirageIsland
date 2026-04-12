using UnityEngine;
using UnityEngine.InputSystem;

public enum FishingState { Idle, Waiting, Minigame }

/// <summary>
/// State machine that owns the fishing flow: Idle → Waiting → Minigame → back.
/// Attach to the Player GameObject alongside FishingLine, FishBiteDetector, TugMinigame.
/// Requires a "Water" physics layer set up in Project Settings → Tags and Layers.
/// </summary>
public class FishingController : MonoBehaviour
{
    public static FishingController Instance { get; private set; }

    [Header("Water Detection")]
    [Tooltip("Set to the 'Water' layer in Project Settings")]
    public LayerMask waterLayer;

    public FishingRod ActiveRod { get; private set; }

    private HotbarController hotbarController;
    private Inventory inventory;
    private ItemDictionary itemDictionary;
    private FishingLine fishingLine;
    private FishBiteDetector biteDetector;
    private TugMinigame tugMinigame;
    private TugMinigameUI tugMinigameUI;

    private FishingState state = FishingState.Idle;

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
        biteDetector.OnBite      += OnFishBite;
        tugMinigame.OnCatch      += OnFishCaught;
        tugMinigame.OnEscape     += OnFishEscaped;
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
            case FishingState.Idle:    UpdateIdle();    break;
            case FishingState.Waiting: UpdateWaiting(); break;
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
            TryCast();
    }

    private void TryCast()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, waterLayer);
        if (hit == null) return;

        fishingLine.Initialize(mouseWorld);
        biteDetector.StartDetection();
        state = FishingState.Waiting;
        SoundEffectManager.Play("FishCast");
    }

    // ── Waiting ───────────────────────────────────────────────────────────────

    private void UpdateWaiting()
    {
        // Cancel on right-click or hotbar swap away from rod
        if (Mouse.current.rightButton.wasPressedThisFrame
            || !(hotbarController.GetActiveItem() is FishingRod))
        {
            CancelFishing();
            return;
        }

        float proximityBonus = fishingLine.GetProximityBonus();
        biteDetector.Tick(proximityBonus);
    }

    // ── Minigame ──────────────────────────────────────────────────────────────

    private void UpdateMinigame()
    {
        bool holding = Mouse.current.leftButton.isPressed;
        tugMinigame.Tick(holding);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void OnFishBite()
    {
        state = FishingState.Minigame;
        int rodTier = ActiveRod != null ? ActiveRod.rodTier : 1;
        tugMinigame.StartMinigame(rodTier);
        tugMinigameUI.Show();
        SoundEffectManager.Play("FishBite");
    }

    private void OnFishCaught()
    {
        tugMinigameUI.Hide();

        int rodTier  = ActiveRod != null ? ActiveRod.rodTier : 1;
        BaitType bait = ActiveRod != null ? ActiveRod.equippedBait : BaitType.None;
        TimeOfDay phase = DayCycleManager.Instance.CurrentPhase;

        int fishItemID = FishLootTable.Instance.Roll(rodTier, bait, phase);
        if (fishItemID >= 0)
        {
            GameObject fishPrefab = itemDictionary.GetItemPrefab(fishItemID);
            if (fishPrefab != null)
                inventory.AddItem(fishPrefab);
        }

        // Consume one bait on success
        if (ActiveRod != null && ActiveRod.equippedBait != BaitType.None)
        {
            ActiveRod.baitCount--;
            if (ActiveRod.baitCount <= 0)
            {
                ActiveRod.baitCount = 0;
                ActiveRod.equippedBait = BaitType.None;
            }
        }

        SoundEffectManager.Play("FishCatch");
        CancelFishing();
    }

    private void OnFishEscaped()
    {
        tugMinigameUI.Hide();
        tugMinigame.StopMinigame();
        // Return to Waiting — another fish can bite
        biteDetector.ResetForNextFish();
        state = FishingState.Waiting;
        SoundEffectManager.Play("FishEscape");
    }

    private void CancelFishing()
    {
        fishingLine.Hide();
        biteDetector.StopDetection();
        tugMinigame.StopMinigame();
        tugMinigameUI.Hide();
        state = FishingState.Idle;
        ActiveRod = null;
    }
}
