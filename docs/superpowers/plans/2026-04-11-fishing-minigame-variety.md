# Fishing Minigame Variety Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-species fight phases and two reaction event types (Dart, Tug) to the fishing minigame, scaled by fish rarity (1–5).

**Architecture:** `FishData` gains a `rarity` field and a `FightPhase[]` array defining each species' fight pattern. `FishLootTable.Roll()` returns the full `FishData` instead of just an itemID so `FishingController` can pass it to `TugMinigame.StartMinigame()`. `TugMinigame` steps through phases sequentially, firing timed reaction events (Dart = tap Q/E to match direction, Tug = release + tap LMB) on top of the existing tension bar. `TugMinigameUI` displays arrow and "!" prompts during events.

**Tech Stack:** Unity 2D C#, Unity Input System (Mouse.current, Keyboard.current), Unity UI (Text/Image), ScriptableObjects via serialized MonoBehaviour fields.

---

## File Map

| File | Change |
|---|---|
| `Assets/Scripts/Fishing/FishData.cs` | Add `PhaseType` enum, `EventType` enum, `FightPhase` class, `rarity` + `phases` fields |
| `Assets/Scripts/Fishing/FishLootTable.cs` | Change `Roll()` return type from `int` to `FishData` |
| `Assets/Scripts/Fishing/TugMinigame.cs` | Add phase/event system, change `StartMinigame` signature, change `Tick` signature |
| `Assets/Scripts/Fishing/FishingController.cs` | Roll fish on bite, store `rolledFish`, pass to `StartMinigame`, update `Tick` call |
| `Assets/Scripts/Fishing/TugMinigameUI.cs` | Add dart and tug prompt UI elements, show/hide based on active event |

---

### Task 1: Add enums, FightPhase, and new fields to FishData

**Files:**
- Modify: `Assets/Scripts/Fishing/FishData.cs`

- [ ] **Step 1: Replace the entire contents of FishData.cs**

```csharp
using UnityEngine;

public enum PhaseType { Calm, Struggle, Tired }
public enum EventType { Dart, Tug }

[System.Serializable]
public class FightPhase
{
    public PhaseType type;
    [Tooltip("How long this phase lasts in seconds")]
    public float duration = 5f;
    [Tooltip("Base seconds between reaction events (scaled by rarity at runtime)")]
    public float eventInterval = 4f;
    [Tooltip("Which event types can fire during this phase")]
    public EventType[] possibleEvents;
}

[System.Serializable]
public class BaitBonusEntry
{
    public BaitType baitType;
    public int bonus;
}

[System.Serializable]
public class FishData
{
    public string fishName;
    public int itemID;
    [Tooltip("Base selection weight. Higher = more common.")]
    public int baseWeight;

    [Header("Fight Profile")]
    [Range(1, 5)]
    [Tooltip("1 = common (forgiving), 5 = legendary (tight windows, fast events)")]
    public int rarity = 1;
    [Tooltip("Fight phases played in order. Leave empty for plain tension-bar fight.")]
    public FightPhase[] phases;

    [Header("Time of Day Bonuses")]
    public int preDawnBonus;
    public int dawnBonus;
    public int dayBonus;
    public int duskBonus;
    public int nightBonus;

    [Header("Bait Bonuses")]
    public BaitBonusEntry[] baitBonuses;

    [Header("Rod Tier Bonuses")]
    [Tooltip("Index 0 = tier 1, index 1 = tier 2, index 2 = tier 3")]
    public int[] rodTierBonuses;
}
```

- [ ] **Step 2: Verify Unity compiles with no errors**

Open Unity. Check the Console window — no red errors about FishData, PhaseType, EventType, or FightPhase.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/FishData.cs
git commit -m "feat: add FightPhase, PhaseType, EventType enums and rarity field to FishData"
```

---

### Task 2: Update FishLootTable.Roll() to return FishData

**Files:**
- Modify: `Assets/Scripts/Fishing/FishLootTable.cs`

**Context:** `Roll()` currently returns `int` (itemID). We change it to return `FishData` so `FishingController` can pass the full fish data to the minigame. Callers that only need itemID use `fish?.itemID ?? -1`.

- [ ] **Step 1: Replace the entire contents of FishLootTable.cs**

```csharp
using System.Collections.Generic;
using UnityEngine;

public class FishLootTable : MonoBehaviour
{
    public static FishLootTable Instance { get; private set; }

    [Tooltip("All fish species. Add new entries here to expand the loot pool.")]
    public FishData[] fishEntries;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Returns a randomly selected FishData based on rod tier, bait, and time of day.
    /// Returns null if the pool is empty.
    /// </summary>
    public FishData Roll(int rodTier, BaitType bait, TimeOfDay phase)
    {
        var pool = new List<(FishData fish, int weight)>();

        if (fishEntries == null) return null;

        foreach (FishData fish in fishEntries)
        {
            if (fish == null) continue;

            int weight = fish.baseWeight
                + GetPhaseBonus(fish, phase)
                + GetBaitBonus(fish, bait)
                + GetRodBonus(fish, rodTier);

            if (weight > 0)
                pool.Add((fish, weight));
        }

        if (pool.Count == 0) return null;

        int total = 0;
        foreach (var entry in pool) total += entry.weight;

        int roll = Random.Range(0, total);
        int cumulative = 0;
        foreach (var entry in pool)
        {
            cumulative += entry.weight;
            if (roll < cumulative) return entry.fish;
        }

        return pool[pool.Count - 1].fish;
    }

    private int GetPhaseBonus(FishData fish, TimeOfDay phase)
    {
        return phase switch
        {
            TimeOfDay.PreDawn => fish.preDawnBonus,
            TimeOfDay.Dawn    => fish.dawnBonus,
            TimeOfDay.Day     => fish.dayBonus,
            TimeOfDay.Dusk    => fish.duskBonus,
            TimeOfDay.Night   => fish.nightBonus,
            _                 => 0
        };
    }

    private int GetBaitBonus(FishData fish, BaitType bait)
    {
        if (fish.baitBonuses == null) return 0;
        foreach (BaitBonusEntry entry in fish.baitBonuses)
            if (entry.baitType == bait) return entry.bonus;
        return 0;
    }

    private int GetRodBonus(FishData fish, int rodTier)
    {
        if (fish.rodTierBonuses == null || rodTier < 1 || rodTier - 1 >= fish.rodTierBonuses.Length) return 0;
        return fish.rodTierBonuses[rodTier - 1];
    }
}
```

- [ ] **Step 2: Verify Unity compiles with no errors**

Check the Console — expect one or more errors in `FishingController.cs` because it still calls `Roll()` expecting an `int`. That is expected and will be fixed in Task 4.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/FishLootTable.cs
git commit -m "feat: FishLootTable.Roll() returns FishData instead of itemID"
```

---

### Task 3: Add phase stepping and reaction events to TugMinigame

**Files:**
- Modify: `Assets/Scripts/Fishing/TugMinigame.cs`

**Context:** `StartMinigame` gains a `FishData fish` parameter. `Tick` gains a `bool lmbJustPressed` parameter. Phase stepping advances `currentPhaseIndex` when `phaseTimer` expires. Event firing picks a random `EventType` from the current phase and sets `ActiveEvent`. Event resolution checks Q/E (Dart) or `lmbJustPressed` (Tug) within `eventWindowTimer`. Missing the window applies a tension spike.

- [ ] **Step 1: Replace the entire contents of TugMinigame.cs**

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tension-based tug minigame with per-species fight phases and reaction events.
/// Hold LMB to raise tension. Stay in sweet zone to fill reel. React to Dart/Tug events.
/// Call Tick() each Update from FishingController while in Minigame state.
/// </summary>
public class TugMinigame : MonoBehaviour
{
    [Header("Tension Rates")]
    public float tensionRiseRate = 0.4f;
    public float tensionFallRate = 0.3f;

    [Header("Zones (defaults, widened by rod tier)")]
    public float sweetMin = 0.3f;
    public float sweetMax = 0.7f;
    public float dangerThreshold = 0.8f;

    [Header("Escape Timers")]
    [Tooltip("Seconds in danger zone before fish escapes")]
    public float baseDangerTime = 2f;
    [Tooltip("Seconds below sweet zone (too slack) before fish escapes")]
    public float slackEscapeTime = 3f;

    [Header("Reel")]
    public float reelFillRate = 0.15f;
    public float reelDrainRate = 0.05f;

    [Header("Reaction Events")]
    [Tooltip("Base reaction window in seconds at rarity 1. Scales down with rarity.")]
    public float baseReactionWindow = 1.2f;

    public float Tension      { get; private set; }
    public float ReelProgress { get; private set; }

    // Current active reaction event (null = none)
    public EventType? ActiveEvent    { get; private set; }
    // -1 = left (Q), 1 = right (E) — only meaningful when ActiveEvent == Dart
    public int        ActiveEventDir { get; private set; }

    public event Action OnCatch;
    public event Action OnEscape;

    private float dangerTimer;
    private float slackTimer;
    private float currentDangerTime;
    private bool  active;

    // Phase state
    private FishData currentFish;
    private int      currentPhaseIndex;
    private float    phaseTimer;
    private float    eventTimer;
    private float    eventWindowTimer;
    private float    rarityMult;

    public void StartMinigame(int rodTier, FishData fish)
    {
        Tension       = 0.5f;
        ReelProgress  = 0f;
        dangerTimer   = 0f;
        slackTimer    = 0f;
        active        = true;
        ActiveEvent   = null;

        // Rod tier widens sweet zone and extends danger timer
        float tierBonus = (rodTier - 1) * 0.05f;
        sweetMin          = Mathf.Max(0.15f, 0.3f - tierBonus);
        sweetMax          = Mathf.Min(0.85f, 0.7f + tierBonus);
        currentDangerTime = baseDangerTime + (rodTier - 1) * 0.5f;

        // Phase setup
        currentFish       = fish;
        currentPhaseIndex = 0;
        rarityMult        = RarityMultiplier(fish != null ? fish.rarity : 1);

        if (fish != null && fish.phases != null && fish.phases.Length > 0)
        {
            phaseTimer = fish.phases[0].duration;
            eventTimer = fish.phases[0].eventInterval * rarityMult;
        }
        else
        {
            // No phases — events never fire
            phaseTimer = float.MaxValue;
            eventTimer = float.MaxValue;
        }
    }

    public void StopMinigame()
    {
        active      = false;
        ActiveEvent = null;
    }

    /// <summary>
    /// Called each frame by FishingController.
    /// holdingButton = LMB is held. lmbJustPressed = LMB was pressed this frame.
    /// </summary>
    public void Tick(bool holdingButton, bool lmbJustPressed)
    {
        if (!active) return;

        // Tension
        if (holdingButton)
            Tension = Mathf.Clamp01(Tension + tensionRiseRate * Time.deltaTime);
        else
            Tension = Mathf.Clamp01(Tension - tensionFallRate * Time.deltaTime);

        bool inSweet  = Tension >= sweetMin && Tension <= sweetMax;
        bool inDanger = Tension > dangerThreshold;
        bool inSlack  = Tension < sweetMin;

        // Reel progress
        if (inSweet)
        {
            ReelProgress = Mathf.Clamp01(ReelProgress + reelFillRate * Time.deltaTime);
            if (ReelProgress >= 1f)
            {
                active = false;
                OnCatch?.Invoke();
                return;
            }
        }
        else
        {
            ReelProgress = Mathf.Clamp01(ReelProgress - reelDrainRate * Time.deltaTime);
        }

        // Danger timer
        if (inDanger)
        {
            dangerTimer += Time.deltaTime;
            if (dangerTimer >= currentDangerTime)
            {
                active = false;
                OnEscape?.Invoke();
                return;
            }
        }
        else
        {
            dangerTimer = Mathf.Max(0f, dangerTimer - Time.deltaTime * 0.5f);
        }

        // Slack timer
        if (inSlack)
        {
            slackTimer += Time.deltaTime;
            if (slackTimer >= slackEscapeTime)
            {
                active = false;
                OnEscape?.Invoke();
                return;
            }
        }
        else
        {
            slackTimer = Mathf.Max(0f, slackTimer - Time.deltaTime);
        }

        TickPhase();
        TickEvent(lmbJustPressed);
    }

    // ── Phase stepping ────────────────────────────────────────────────────────

    private void TickPhase()
    {
        if (currentFish == null || currentFish.phases == null || currentFish.phases.Length == 0) return;

        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f && currentPhaseIndex < currentFish.phases.Length - 1)
        {
            currentPhaseIndex++;
            FightPhase next = currentFish.phases[currentPhaseIndex];
            phaseTimer = next.duration;
            eventTimer = next.eventInterval * rarityMult;
        }
    }

    // ── Event firing and resolution ───────────────────────────────────────────

    private void TickEvent(bool lmbJustPressed)
    {
        if (currentFish == null || currentFish.phases == null || currentFish.phases.Length == 0) return;

        FightPhase phase = currentFish.phases[currentPhaseIndex];
        if (phase.possibleEvents == null || phase.possibleEvents.Length == 0) return;

        if (ActiveEvent == null)
        {
            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0f)
            {
                FireEvent(phase);
                eventTimer = phase.eventInterval * rarityMult;
            }
        }
        else
        {
            bool resolved = false;
            bool hit      = false;

            if (ActiveEvent == EventType.Dart)
            {
                bool pressedQ = Keyboard.current.qKey.wasPressedThisFrame;
                bool pressedE = Keyboard.current.eKey.wasPressedThisFrame;

                if ((ActiveEventDir == -1 && pressedQ) || (ActiveEventDir == 1 && pressedE))
                {
                    resolved = true; hit = true;
                }
                else if (pressedQ || pressedE) // wrong direction
                {
                    resolved = true; hit = false;
                }
            }
            else if (ActiveEvent == EventType.Tug)
            {
                if (lmbJustPressed)
                {
                    resolved = true; hit = true;
                }
            }

            eventWindowTimer -= Time.deltaTime;
            if (eventWindowTimer <= 0f && !resolved)
            {
                resolved = true; hit = false;
            }

            if (resolved)
            {
                if (hit)
                {
                    if (ActiveEvent == EventType.Dart)
                        Tension = Mathf.Clamp01(Tension - 0.15f);
                    else // Tug
                        ReelProgress = Mathf.Clamp01(ReelProgress + 0.1f);
                }
                else
                {
                    Tension = Mathf.Clamp01(Tension + 0.2f);
                }
                ActiveEvent = null;
            }
        }
    }

    private void FireEvent(FightPhase phase)
    {
        EventType type = phase.possibleEvents[UnityEngine.Random.Range(0, phase.possibleEvents.Length)];
        ActiveEvent      = type;
        ActiveEventDir   = (type == EventType.Dart) ? (UnityEngine.Random.value > 0.5f ? 1 : -1) : 0;
        eventWindowTimer = baseReactionWindow * rarityMult;
    }

    // rarity 1 = 1.0x, rarity 5 = 0.4x
    private static float RarityMultiplier(int rarity) =>
        1f - Mathf.Clamp(rarity - 1, 0, 4) * 0.15f;
}
```

- [ ] **Step 2: Verify Unity compiles**

Check Console — expect errors only in `FishingController.cs` (StartMinigame signature and Roll() return type). No errors in TugMinigame itself.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/TugMinigame.cs
git commit -m "feat: TugMinigame phase stepping and Dart/Tug reaction events"
```

---

### Task 4: Update FishingController — roll fish on bite, wire new signatures

**Files:**
- Modify: `Assets/Scripts/Fishing/FishingController.cs`

**Context:** Fish is now rolled in `OnFishBite()` (not `OnFishCaught()`) so we have `FishData` available to pass to `StartMinigame`. Store it in `rolledFish`. `OnFishCaught()` reads `rolledFish.itemID`. `UpdateMinigame()` passes `lmbJustPressed` to `Tick()`.

- [ ] **Step 1: Replace the entire contents of FishingController.cs**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public enum FishingState { Idle, Casting, Waiting, Minigame }

/// <summary>
/// State machine: Idle → Casting → Waiting → Minigame → back.
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

    public FishingRod ActiveRod { get; private set; }

    private HotbarController hotbarController;
    private Inventory         inventory;
    private ItemDictionary    itemDictionary;
    private FishingLine       fishingLine;
    private FishBiteDetector  biteDetector;
    private TugMinigame       tugMinigame;
    private TugMinigameUI     tugMinigameUI;

    private FishingState state     = FishingState.Idle;
    private FishData     rolledFish;

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
            TryCast();
    }

    private void TryCast()
    {
        Vector3 screenPos = Mouse.current.position.ReadValue();
        screenPos.z = -Camera.main.transform.position.z;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(screenPos);

        // Clamp to rod's cast distance
        Vector2 playerPos = transform.position;
        Vector2 dir       = mouseWorld - playerPos;
        float   maxDist   = ActiveRod != null ? ActiveRod.castDistance : 3f;
        if (dir.magnitude > maxDist)
            mouseWorld = playerPos + dir.normalized * maxDist;

        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, waterLayer);
        if (hit == null) return;

        float speed = ActiveRod != null ? ActiveRod.castSpeed : 6f;
        fishingLine.NudgeEnabled = false;
        fishingLine.Cast(mouseWorld, speed, OnCastLanded);
        state     = FishingState.Casting;
        IsFishing = true;
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

    // ── Minigame ──────────────────────────────────────────────────────────────

    private void UpdateMinigame()
    {
        bool holding      = Mouse.current.leftButton.isPressed;
        bool justPressed  = Mouse.current.leftButton.wasPressedThisFrame;
        tugMinigame.Tick(holding, justPressed);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void OnFishBite()
    {
        // Roll the fish species now so we have its fight profile
        int      rodTier  = ActiveRod != null ? ActiveRod.rodTier      : 1;
        BaitType bait     = ActiveRod != null ? ActiveRod.equippedBait : BaitType.None;
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
```

- [ ] **Step 2: Verify Unity compiles with no errors**

All four modified files should compile cleanly. Check Console — zero red errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/FishingController.cs
git commit -m "feat: roll fish on bite, pass FishData to StartMinigame, wire lmbJustPressed to Tick"
```

---

### Task 5: Add reaction event prompts to TugMinigameUI

**Files:**
- Modify: `Assets/Scripts/Fishing/TugMinigameUI.cs`

**Context:** Two new child GameObjects must be added to the minigame panel in the Unity Editor after this task: one for Dart (shows "← Q" or "E →") and one for Tug (shows "!"). The UI script enables/disables them and updates the dart text based on direction. The objects are assigned in the Inspector.

- [ ] **Step 1: Replace the entire contents of TugMinigameUI.cs**

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the tension bar, reel bar, and reaction event prompts during the tug minigame.
/// Attach to a Canvas child. Assign all references in the Inspector.
/// </summary>
public class TugMinigameUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Bars")]
    [Tooltip("Image with Image Type = Filled, Fill Method = Vertical")]
    public Image tensionFill;
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal")]
    public Image reelFill;

    [Header("Tension Colors")]
    public Color safeColor    = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor  = Color.red;

    [Header("Event Prompts")]
    [Tooltip("GameObject shown during a Dart event. Should contain a Text child.")]
    public GameObject dartPrompt;
    [Tooltip("Text component inside dartPrompt that shows '← Q' or 'E →'")]
    public Text dartPromptText;
    [Tooltip("GameObject shown during a Tug event")]
    public GameObject tugPrompt;

    private TugMinigame tugMinigame;

    private void Awake()
    {
        tugMinigame = FindAnyObjectByType<TugMinigame>();
        if (panel != null) panel.SetActive(false);
    }

    public void Show() { if (panel != null) panel.SetActive(true); }
    public void Hide() { if (panel != null) panel.SetActive(false); }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (tugMinigame == null) return;

        // Tension bar
        float t = tugMinigame.Tension;
        if (tensionFill != null)
        {
            tensionFill.fillAmount = t;
            tensionFill.color = t > 0.8f ? dangerColor : t > 0.6f ? warningColor : safeColor;
        }

        // Reel bar
        if (reelFill != null)
            reelFill.fillAmount = tugMinigame.ReelProgress;

        // Event prompts
        bool isDart = tugMinigame.ActiveEvent == EventType.Dart;
        bool isTug  = tugMinigame.ActiveEvent == EventType.Tug;

        if (dartPrompt != null)
        {
            dartPrompt.SetActive(isDart);
            if (isDart && dartPromptText != null)
                dartPromptText.text = tugMinigame.ActiveEventDir == -1 ? "← Q" : "E →";
        }

        if (tugPrompt != null)
            tugPrompt.SetActive(isTug);
    }
}
```

- [ ] **Step 2: Add prompt GameObjects to the minigame panel in the Unity Editor**

In the Hierarchy, find your TugMinigame canvas panel. Add two child GameObjects:

**DartPrompt:**
- Add a `Text` component (or TextMeshPro if you prefer — if using TMP, change `public Text dartPromptText` to `public TMPro.TextMeshProUGUI dartPromptText` in the script)
- Set font size large (e.g. 48), centered, white or yellow color
- Position it above the tension bar
- Set active = false in the Inspector (the script will enable it at runtime)

**TugPrompt:**
- Add a `Text` component with text "!"
- Same sizing/color as DartPrompt
- Position next to or below DartPrompt
- Set active = false in the Inspector

- [ ] **Step 3: Wire references in the Inspector**

Select the TugMinigameUI GameObject. In the Inspector:
- `Dart Prompt` → assign the DartPrompt GameObject
- `Dart Prompt Text` → assign the Text component inside DartPrompt
- `Tug Prompt` → assign the TugPrompt GameObject

- [ ] **Step 4: Verify in Play mode**

1. Enter Play mode. Select a fishing rod from the hotbar.
2. Cast into water. Wait for a bite.
3. During the minigame, a dart or tug prompt should appear periodically.
4. For a Dart: tap Q if arrow shows ←, E if arrow shows →. Tension should drop on hit.
5. For a Tug: release LMB then tap LMB. Reel should jump on hit.
6. If you ignore a prompt it disappears after ~1.2 seconds and tension spikes.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Fishing/TugMinigameUI.cs
git commit -m "feat: add Dart and Tug event prompts to TugMinigameUI"
```

---

## Inspector Setup for Fish Fight Profiles

After all tasks compile, open each FishData entry in the FishLootTable GameObject's Inspector and fill in the fight profile. Example for a common fish (rarity 1):

```
Rarity: 1
Phases:
  [0] Type: Calm,    Duration: 8,  Event Interval: 5, Possible Events: [Dart]
  [1] Type: Struggle, Duration: 5, Event Interval: 3, Possible Events: [Dart, Tug]
  [2] Type: Tired,   Duration: 4,  Event Interval: 6, Possible Events: [Tug]
```

Example for a rare fish (rarity 4):

```
Rarity: 4
Phases:
  [0] Type: Calm,    Duration: 4,  Event Interval: 3, Possible Events: [Dart]
  [1] Type: Struggle, Duration: 8, Event Interval: 2, Possible Events: [Dart, Tug]
  [2] Type: Tired,   Duration: 3,  Event Interval: 4, Possible Events: [Dart, Tug]
```

Fish with no `phases` entries fight using only the tension bar (original behavior).
