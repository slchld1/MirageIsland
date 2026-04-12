# Fishing System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a real-time positional fishing system with LMB cast, Q/E line nudge, fish blink, and a tension-based tug minigame with loot determined by rod, bait, and time of day.

**Architecture:** State machine (`FishingController`) on the player delegates to focused components: `FishingLine` (LineRenderer + nudge), `FishBiteDetector` (countdown + fish blink), `TugMinigame` (tension logic). `FishLootTable` handles weighted loot rolls. `HotbarController` gains active slot tracking so `FishingController` knows when a rod is equipped.

**Tech Stack:** Unity 2D, C#, UnityEngine.InputSystem, LineRenderer, Physics2D.OverlapPoint, TMPro

---

## File Map

**New files:**
- `Assets/Scripts/Fishing/BaitType.cs` — BaitType enum
- `Assets/Scripts/Fishing/FishData.cs` — Serializable fish entry with weight modifiers
- `Assets/Scripts/Fishing/Bait.cs` — Extends Item, has BaitType field
- `Assets/Scripts/Fishing/FishingRod.cs` — Extends Item, rod tier + equipped bait
- `Assets/Scripts/Fishing/FishLootTable.cs` — Singleton, weighted random fish roll
- `Assets/Scripts/Fishing/FishBlink.cs` — Brief fish sprite flash, self-destructs
- `Assets/Scripts/Fishing/FishingLine.cs` — LineRenderer, Q/E nudge, proximity to last blink
- `Assets/Scripts/Fishing/FishBiteDetector.cs` — Countdown timer, spawns FishBlinks
- `Assets/Scripts/Fishing/TugMinigame.cs` — Tension meter, reel progress, catch/escape events
- `Assets/Scripts/Fishing/TugMinigameUI.cs` — Drives tension bar and reel progress bar UI
- `Assets/Scripts/Fishing/BaitSlotUI.cs` — Shows bait panel when rod is active hotbar slot
- `Assets/Scripts/Fishing/FishingController.cs` — State machine: Idle → Waiting → Minigame

**Modified files:**
- `Assets/Scripts/HotbarController.cs` — Add `SelectedSlotIndex` + `GetActiveItem()`
- `Assets/Scripts/ItemDictionary.cs` — Remove stray `using NUnit.Framework`

---

## Task 1: HotbarController — Active Slot Tracking

**Files:**
- Modify: `Assets/Scripts/HotbarController.cs`

- [ ] **Step 1: Remove stray NUnit import from ItemDictionary while we're touching the hotbar area**

Open `Assets/Scripts/ItemDictionary.cs` and remove line 1:
```csharp
using NUnit.Framework;
```
The file should start with:
```csharp
using System.Collections.Generic;
using UnityEngine;
```

- [ ] **Step 2: Add SelectedSlotIndex and GetActiveItem to HotbarController**

Replace the full contents of `Assets/Scripts/HotbarController.cs` with:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    public GameObject hotbarpanel;
    public GameObject slotPrefab;
    public int slotCount = 10;

    public int SelectedSlotIndex { get; private set; } = -1;

    private ItemDictionary itemDictionary;
    private Key[] hotbarKeys;

    private void Awake()
    {
        itemDictionary = FindAnyObjectByType<ItemDictionary>();

        hotbarKeys = new Key[slotCount];
        for (int i = 0; i < slotCount; i++)
            hotbarKeys[i] = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0;
    }

    void Update()
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                SelectedSlotIndex = i;
                UseItemInSlot(i);
            }
        }
    }

    void UseItemInSlot(int index)
    {
        Slot slot = hotbarpanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot.currentItem != null)
        {
            Item item = slot.currentItem.GetComponent<Item>();
            item.UseItem();
        }
    }

    public Item GetActiveItem()
    {
        if (SelectedSlotIndex < 0 || SelectedSlotIndex >= hotbarpanel.transform.childCount)
            return null;
        Slot slot = hotbarpanel.transform.GetChild(SelectedSlotIndex).GetComponent<Slot>();
        return slot.currentItem != null ? slot.currentItem.GetComponent<Item>() : null;
    }

    public List<InventorySaveData> GetHotbarItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        foreach (Transform slotTransform in hotbarpanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                invData.Add(new InventorySaveData { itemID = item.ID, slotIndex = slotTransform.GetSiblingIndex() });
            }
        }
        return invData;
    }

    public void SetHotbarItems(List<InventorySaveData> inventorySaveData)
    {
        foreach (Transform child in hotbarpanel.transform)
            Destroy(child.gameObject);

        for (int i = 0; i < slotCount; i++)
            Instantiate(slotPrefab, hotbarpanel.transform);

        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount)
            {
                Slot slot = hotbarpanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    slot.currentItem = item;
                }
            }
        }
    }
}
```

- [ ] **Step 3: Enter Play Mode and verify**

Press Play. Open a scene with the hotbar. Press keys 1–0. Confirm the game still works (items use as before). No console errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/HotbarController.cs Assets/Scripts/ItemDictionary.cs
git commit -m "feat: add active slot tracking to HotbarController, remove NUnit import"
```

---

## Task 2: BaitType Enum + FishData Struct

**Files:**
- Create: `Assets/Scripts/Fishing/BaitType.cs`
- Create: `Assets/Scripts/Fishing/FishData.cs`

- [ ] **Step 1: Create BaitType enum**

Create `Assets/Scripts/Fishing/BaitType.cs`:
```csharp
public enum BaitType { None, Worm, Fly, Lure }
```

- [ ] **Step 2: Create FishData and BaitBonusEntry**

Create `Assets/Scripts/Fishing/FishData.cs`:
```csharp
using UnityEngine;

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
    public int itemID;           // must match an ID in ItemDictionary
    public int baseWeight;

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

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/BaitType.cs Assets/Scripts/Fishing/FishData.cs
git commit -m "feat: add BaitType enum and FishData serializable struct"
```

---

## Task 3: Bait and FishingRod Items

**Files:**
- Create: `Assets/Scripts/Fishing/Bait.cs`
- Create: `Assets/Scripts/Fishing/FishingRod.cs`

- [ ] **Step 1: Create Bait class**

Create `Assets/Scripts/Fishing/Bait.cs`:
```csharp
public class Bait : Item
{
    public BaitType baitType;
}
```

- [ ] **Step 2: Create FishingRod class**

Create `Assets/Scripts/Fishing/FishingRod.cs`:
```csharp
using UnityEngine;

public class FishingRod : Item
{
    [Range(1, 3)]
    public int rodTier = 1;

    // Set by BaitSlotUI when bait is loaded
    [HideInInspector] public BaitType equippedBait = BaitType.None;
    [HideInInspector] public int baitCount = 0;

    public override void UseItem()
    {
        // Fishing is triggered by LMB click via FishingController.
        // Pressing the hotbar key just selects this slot.
    }
}
```

- [ ] **Step 3: Scene setup**

In Unity:
1. Create a new GameObject prefab for the fishing rod (e.g. "FishingRod_Basic").
2. Add a `FishingRod` component. Set `rodTier = 1`. Set `ID` and `Name` fields.
3. Add an `Image` component with your rod sprite (required by `Item.PickUp()`).
4. Add the prefab to `ItemDictionary.itemPrefabs` list in the Inspector.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Fishing/Bait.cs Assets/Scripts/Fishing/FishingRod.cs
git commit -m "feat: add Bait and FishingRod item classes"
```

---

## Task 4: FishLootTable

**Files:**
- Create: `Assets/Scripts/Fishing/FishLootTable.cs`

- [ ] **Step 1: Create FishLootTable singleton**

Create `Assets/Scripts/Fishing/FishLootTable.cs`:
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
    /// Returns the itemID of a randomly selected fish based on rod tier, bait, and time of day.
    /// Returns -1 if the pool is empty.
    /// </summary>
    public int Roll(int rodTier, BaitType bait, TimeOfDay phase)
    {
        var pool = new List<(int itemID, int weight)>();

        foreach (FishData fish in fishEntries)
        {
            int weight = fish.baseWeight
                + GetPhaseBonus(fish, phase)
                + GetBaitBonus(fish, bait)
                + GetRodBonus(fish, rodTier);

            if (weight > 0)
                pool.Add((fish.itemID, weight));
        }

        if (pool.Count == 0) return -1;

        int total = 0;
        foreach (var entry in pool) total += entry.weight;

        int roll = Random.Range(0, total);
        int cumulative = 0;
        foreach (var entry in pool)
        {
            cumulative += entry.weight;
            if (roll < cumulative) return entry.itemID;
        }

        return pool[pool.Count - 1].itemID;
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
        if (fish.rodTierBonuses == null || rodTier - 1 >= fish.rodTierBonuses.Length) return 0;
        return fish.rodTierBonuses[rodTier - 1];
    }
}
```

- [ ] **Step 2: Scene setup**

In Unity:
1. Create a new empty GameObject named "FishLootTable" in your scene.
2. Add the `FishLootTable` component.
3. Add at least one `FishData` entry to `fishEntries` (set `fishName`, `itemID`, `baseWeight = 60`). The `itemID` must match an entry in `ItemDictionary`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/FishLootTable.cs
git commit -m "feat: add FishLootTable with weighted random roll by rod/bait/phase"
```

---

## Task 5: FishBlink + FishingLine

**Files:**
- Create: `Assets/Scripts/Fishing/FishBlink.cs`
- Create: `Assets/Scripts/Fishing/FishingLine.cs`

- [ ] **Step 1: Create FishBlink**

Create `Assets/Scripts/Fishing/FishBlink.cs`:
```csharp
using UnityEngine;

/// <summary>
/// Spawned near the bob. Fades in and out briefly to hint at fish presence, then destroys itself.
/// Requires a SpriteRenderer on the same GameObject.
/// </summary>
public class FishBlink : MonoBehaviour
{
    public float duration = 0.6f;

    private SpriteRenderer sr;
    private float timer;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;
        float alpha = t < 0.5f ? t * 2f : (1f - t) * 2f;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;

        if (timer >= duration)
            Destroy(gameObject);
    }
}
```

- [ ] **Step 2: Create FishBlink prefab in Unity**

In Unity:
1. Create an empty GameObject, name it "FishBlink".
2. Add a `SpriteRenderer` component. Assign a small fish shadow/silhouette sprite.
3. Add the `FishBlink` script component.
4. Drag it to your Prefabs folder to make it a prefab. Delete the scene instance.

- [ ] **Step 3: Create FishingLine**

Create `Assets/Scripts/Fishing/FishingLine.cs`:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Draws a LineRenderer from the player to the cast point.
/// Q/E nudge moves the bob left/right. Tracks proximity to the last fish blink.
/// Attach to the player GameObject alongside a LineRenderer component.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class FishingLine : MonoBehaviour
{
    [Header("Nudge")]
    public float nudgeSpeed = 2f;
    public float maxNudgeDistance = 3f;

    [Header("Proximity")]
    [Tooltip("Max catch-rate bonus at closest range (e.g. 0.2 = +20% speed)")]
    public float maxProximityBonus = 0.2f;
    public float proximityRange = 2f;

    private LineRenderer lineRenderer;
    private Vector2 castPoint;
    private Vector2 bobPosition;

    public Vector2 BobPosition => bobPosition;

    // Set externally by FishBiteDetector when a blink spawns
    public Vector2 LastBlinkPosition { get; set; }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.enabled = false;
    }

    public void Initialize(Vector2 castTarget)
    {
        castPoint = castTarget;
        bobPosition = castTarget;
        LastBlinkPosition = castTarget;
        lineRenderer.enabled = true;
        UpdateLine();
    }

    public void Hide()
    {
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (!lineRenderer.enabled) return;

        float nudge = 0f;
        if (Keyboard.current.qKey.isPressed) nudge = -1f;
        else if (Keyboard.current.eKey.isPressed) nudge = 1f;

        if (nudge != 0f)
        {
            bobPosition += Vector2.right * nudge * nudgeSpeed * Time.deltaTime;
            float offset = Mathf.Clamp(bobPosition.x - castPoint.x, -maxNudgeDistance, maxNudgeDistance);
            bobPosition = new Vector2(castPoint.x + offset, castPoint.y);
            UpdateLine();
        }
    }

    private void UpdateLine()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, bobPosition);
    }

    public float GetProximityBonus()
    {
        float dist = Vector2.Distance(bobPosition, LastBlinkPosition);
        float t = 1f - Mathf.Clamp01(dist / proximityRange);
        return t * maxProximityBonus;
    }
}
```

- [ ] **Step 4: Add LineRenderer to Player**

In Unity:
1. Select the Player GameObject.
2. Add a `LineRenderer` component (Add Component → Effects → Line Renderer).
3. Set `Use World Space = true`.
4. Assign a simple white material (or use Sprites/Default).
5. Add the `FishingLine` script component to the player.

- [ ] **Step 5: Play Mode verify**

Enter Play Mode. The LineRenderer should be invisible (disabled). No errors.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Fishing/FishBlink.cs Assets/Scripts/Fishing/FishingLine.cs
git commit -m "feat: add FishBlink and FishingLine with Q/E nudge and proximity tracking"
```

---

## Task 6: FishBiteDetector

**Files:**
- Create: `Assets/Scripts/Fishing/FishBiteDetector.cs`

- [ ] **Step 1: Create FishBiteDetector**

Create `Assets/Scripts/Fishing/FishBiteDetector.cs`:
```csharp
using System;
using UnityEngine;

/// <summary>
/// Counts down to a fish bite. Spawns brief fish blinks near the bob.
/// Call Tick() each Update from FishingController while in Waiting state.
/// Attach to the player GameObject alongside FishingLine.
/// </summary>
public class FishBiteDetector : MonoBehaviour
{
    [Header("Bite Timing")]
    public float minWaitTime = 5f;
    public float maxWaitTime = 20f;

    [Header("Fish Blink")]
    public GameObject fishBlinkPrefab;
    public float blinkInterval = 4f;
    public float blinkRadius = 1.5f;

    public event Action OnBite;

    private FishingLine fishingLine;
    private float biteTimer;
    private float blinkTimer;
    private bool active;

    private void Awake()
    {
        fishingLine = GetComponent<FishingLine>();
    }

    public void StartDetection()
    {
        active = true;
        biteTimer = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        blinkTimer = blinkInterval;
    }

    public void StopDetection()
    {
        active = false;
    }

    public void ResetForNextFish()
    {
        active = true;
        biteTimer = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        blinkTimer = blinkInterval;
    }

    /// <summary>
    /// Called each frame by FishingController while waiting. proximityBonus is 0.0–0.2.
    /// </summary>
    public void Tick(float proximityBonus)
    {
        if (!active) return;

        float speedMultiplier = 1f + proximityBonus;
        biteTimer -= Time.deltaTime * speedMultiplier;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            SpawnBlink();
            blinkTimer = blinkInterval + UnityEngine.Random.Range(-1f, 1f);
        }

        if (biteTimer <= 0f)
        {
            active = false;
            OnBite?.Invoke();
        }
    }

    private void SpawnBlink()
    {
        if (fishBlinkPrefab == null) return;

        Vector2 blinkPos = fishingLine.BobPosition + UnityEngine.Random.insideUnitCircle * blinkRadius;
        GameObject blink = Instantiate(fishBlinkPrefab, blinkPos, Quaternion.identity);

        // Tell FishingLine where this blink appeared so proximity can be calculated
        fishingLine.LastBlinkPosition = blinkPos;
    }
}
```

- [ ] **Step 2: Scene setup**

In Unity:
1. Add `FishBiteDetector` component to the Player GameObject.
2. Assign the `FishBlink` prefab you created in Task 5 to the `fishBlinkPrefab` field.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/FishBiteDetector.cs
git commit -m "feat: add FishBiteDetector with countdown, fish blink spawning, and proximity bonus"
```

---

## Task 7: TugMinigame Logic

**Files:**
- Create: `Assets/Scripts/Fishing/TugMinigame.cs`

- [ ] **Step 1: Create TugMinigame**

Create `Assets/Scripts/Fishing/TugMinigame.cs`:
```csharp
using System;
using UnityEngine;

/// <summary>
/// Tension-based tug minigame. Hold LMB to raise tension, release to lower.
/// Stay in the sweet zone to fill reel progress. Catch fires OnCatch, escape fires OnEscape.
/// Call Tick() each Update from FishingController while in Minigame state.
/// Attach to the player GameObject.
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

    public float Tension { get; private set; }
    public float ReelProgress { get; private set; }

    public event Action OnCatch;
    public event Action OnEscape;

    private float dangerTimer;
    private float slackTimer;
    private float currentDangerTime;
    private bool active;

    public void StartMinigame(int rodTier)
    {
        Tension = 0.5f;
        ReelProgress = 0f;
        dangerTimer = 0f;
        slackTimer = 0f;
        active = true;

        // Rod tier widens sweet zone and extends danger timer
        float tierBonus = (rodTier - 1) * 0.05f;
        sweetMin = Mathf.Max(0.15f, 0.3f - tierBonus);
        sweetMax = Mathf.Min(0.85f, 0.7f + tierBonus);
        currentDangerTime = baseDangerTime + (rodTier - 1) * 0.5f;
    }

    public void StopMinigame()
    {
        active = false;
    }

    /// <summary>
    /// Called each frame by FishingController. holdingButton = LMB is held.
    /// </summary>
    public void Tick(bool holdingButton)
    {
        if (!active) return;

        // Update tension
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

        // Danger zone timer
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
            }
        }
        else
        {
            slackTimer = Mathf.Max(0f, slackTimer - Time.deltaTime);
        }
    }
}
```

- [ ] **Step 2: Add to Player**

In Unity: Add `TugMinigame` component to the Player GameObject.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/TugMinigame.cs
git commit -m "feat: add TugMinigame with tension meter, reel progress, catch/escape events"
```

---

## Task 8: TugMinigameUI

**Files:**
- Create: `Assets/Scripts/Fishing/TugMinigameUI.cs`

- [ ] **Step 1: Create TugMinigameUI**

Create `Assets/Scripts/Fishing/TugMinigameUI.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the tension bar and reel progress bar during the tug minigame.
/// Attach to a Canvas child GameObject. Assign the panel, tension fill image, and reel fill image.
/// The panel is hidden by default and shown by FishingController during minigame.
/// </summary>
public class TugMinigameUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;
    [Tooltip("Image with Image Type = Filled, Fill Method = Vertical")]
    public Image tensionFill;
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal")]
    public Image reelFill;

    [Header("Tension Colors")]
    public Color safeColor   = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor  = Color.red;

    private TugMinigame tugMinigame;

    private void Awake()
    {
        tugMinigame = FindAnyObjectByType<TugMinigame>();
        panel.SetActive(false);
    }

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);

    private void Update()
    {
        if (!panel.activeSelf) return;

        float t = tugMinigame.Tension;
        tensionFill.fillAmount = t;

        if (t > 0.8f)      tensionFill.color = dangerColor;
        else if (t > 0.6f) tensionFill.color = warningColor;
        else               tensionFill.color = safeColor;

        reelFill.fillAmount = tugMinigame.ReelProgress;
    }
}
```

- [ ] **Step 2: Build the UI in Unity**

In your Canvas hierarchy:
1. Create an empty GameObject named "TugMinigamePanel". This is the `panel` reference.
2. Inside it, create two UI Images:
   - "TensionBar": `Image Type = Filled`, `Fill Method = Vertical`, anchored center-left of screen.
   - "ReelBar": `Image Type = Filled`, `Fill Method = Horizontal`, anchored below tension bar.
3. Create another empty GameObject on the Canvas named "TugMinigameUI".
4. Add the `TugMinigameUI` script. Assign `panel = TugMinigamePanel`, `tensionFill`, `reelFill`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/TugMinigameUI.cs
git commit -m "feat: add TugMinigameUI driving tension bar and reel progress display"
```

---

## Task 9: BaitSlotUI

**Files:**
- Create: `Assets/Scripts/Fishing/BaitSlotUI.cs`

- [ ] **Step 1: Create BaitSlotUI**

Create `Assets/Scripts/Fishing/BaitSlotUI.cs`:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a small panel near the hotbar when a FishingRod is the active hotbar slot.
/// Displays current bait type and count. Clicking opens bait selection (future).
/// Attach to a Canvas child GameObject. Assign panel, baitIcon, and baitCountText.
/// </summary>
public class BaitSlotUI : MonoBehaviour
{
    public GameObject panel;
    public Image baitIcon;
    public TMP_Text baitCountText;

    [Tooltip("Sprite shown when no bait is loaded")]
    public Sprite noBaitSprite;

    private FishingController fishingController;

    private void Awake()
    {
        fishingController = FindAnyObjectByType<FishingController>();
        panel.SetActive(false);
    }

    private void Update()
    {
        FishingRod rod = fishingController.ActiveRod;

        if (rod == null)
        {
            panel.SetActive(false);
            return;
        }

        panel.SetActive(true);
        baitCountText.text = rod.baitCount > 0 ? rod.baitCount.ToString() : "0";

        if (baitIcon != null && noBaitSprite != null && rod.equippedBait == BaitType.None)
            baitIcon.sprite = noBaitSprite;
    }

    // Wired to the bait slot Button's OnClick in the Inspector
    public void OnBaitSlotClicked()
    {
        // Future: open a filtered inventory view showing only Bait items
        Debug.Log("Bait slot clicked — open bait inventory here.");
    }
}
```

- [ ] **Step 2: Build the UI in Unity**

In your Canvas hierarchy:
1. Create an empty GameObject "BaitSlotPanel" anchored near the hotbar.
2. Inside it, add an `Image` (bait icon) and a `TMP_Text` (count).
3. Add a `Button` component to the panel so clicks trigger `OnBaitSlotClicked`.
4. Create "BaitSlotUI" empty GameObject on Canvas, add `BaitSlotUI` script, assign references.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/BaitSlotUI.cs
git commit -m "feat: add BaitSlotUI panel that appears when fishing rod is active hotbar slot"
```

---

## Task 10: FishingController — State Machine

**Files:**
- Create: `Assets/Scripts/Fishing/FishingController.cs`

- [ ] **Step 1: Create FishingController**

Create `Assets/Scripts/Fishing/FishingController.cs`:
```csharp
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
```

- [ ] **Step 2: Add to Player**

In Unity:
1. Add `FishingController` to the Player GameObject.
2. In the Inspector, set `waterLayer` to your Water layer mask.

- [ ] **Step 3: Set up Water Layer**

In Unity:
1. Go to **Edit → Project Settings → Tags and Layers**.
2. Add a new layer named "Water".
3. Select your water tile/collider GameObjects and set their Layer to "Water".
4. Make sure each water object has a `Collider2D` (not set to trigger — it's used for overlap detection).

- [ ] **Step 4: Add sound effects (optional but wired)**

In `SoundEffectLibrary` Inspector, add named groups:
- `"FishCast"` — a splash or whoosh clip
- `"FishBite"` — a short alert/plop clip
- `"FishCatch"` — a success jingle
- `"FishEscape"` — a fail/splash clip

If you don't have clips yet, `SoundEffectManager.Play()` will silently no-op (it returns null from `GetRandomClip` and skips).

- [ ] **Step 5: Full play-through test**

Enter Play Mode and verify this sequence:
1. Put a `FishingRod` prefab in an inventory slot. Drag it to hotbar.
2. Press the hotbar key for that slot. `ActiveRod` should be set (check via Debug.Log or Inspector).
3. Left-click on a water collider. Line should appear from player to click point.
4. Press Q / E — line bob should nudge left/right.
5. Wait — fish blink sprites should appear near the bob briefly.
6. After some seconds, bite triggers — TugMinigame panel should appear.
7. Hold LMB — tension bar fills toward red. Release — it drops.
8. Keep tension in green zone — reel bar fills. On 100%: fish added to inventory, panel hides.
9. Right-click while waiting — line disappears, returns to idle.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Fishing/FishingController.cs
git commit -m "feat: add FishingController state machine — completes fishing system"
```

---

## Final Checklist

- [ ] All 10 `.cs` files compile with no errors in Unity console
- [ ] `FishLootTable` has at least one `FishData` entry with a valid `itemID`
- [ ] Player GameObject has: `FishingLine`, `FishBiteDetector`, `TugMinigame`, `FishingController`, `LineRenderer`
- [ ] Canvas has: `TugMinigameUI` (with panel, tension bar, reel bar), `BaitSlotUI` (with panel)
- [ ] Water colliders are on the "Water" layer
- [ ] `FishBiteDetector.fishBlinkPrefab` is assigned
- [ ] `ItemDictionary` contains at least one fish item prefab and one fishing rod prefab
