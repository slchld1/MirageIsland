# Cast Charge Mechanic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace instant-cast with an oscillating power bar that determines cast distance and speed on LMB release.

**Architecture:** Add `FishingState.Charging` to the existing state machine. `FishingController` owns all charge logic and exposes `ChargeLevel` for UI polling. `CastChargeUI` is a virtual-method component so the UI can be replaced without touching charge logic.

**Tech Stack:** Unity 2D, C#, UnityEngine.UI.Image (fill), UnityEngine.InputSystem

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Fishing/CastChargeUI.cs` | **Create** | Virtual UI contract — show/hide/fill |
| `Assets/Scripts/Fishing/FishingController.cs` | **Modify** | Charging state, charge oscillation, ExecuteCast, water check at landing |

---

### Task 1: Create `CastChargeUI.cs`

**Files:**
- Create: `Assets/Scripts/Fishing/CastChargeUI.cs`

- [ ] **Step 1: Create the file**

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays cast charge level. Override OnChargeChanged / Show / Hide to swap visuals.
/// Attach to a child GameObject of the player alongside your chosen UI elements.
/// </summary>
public class CastChargeUI : MonoBehaviour
{
    [Tooltip("Horizontal fill image driven by charge level (0–1). Optional — leave null if using a custom subclass.")]
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Called every frame while charging. t = 0 (empty) to 1 (full).</summary>
    public virtual void OnChargeChanged(float t)
    {
        if (fillImage != null)
            fillImage.fillAmount = t;
    }
}
```

- [ ] **Step 2: Verify Unity compiles** — open Unity, check Console for errors. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/CastChargeUI.cs
git commit -m "feat: add CastChargeUI virtual component"
```

---

### Task 2: Add charge infrastructure to `FishingController`

Add the `Charging` state, new serialized fields, private charge tracking, and the `ChargeLevel` property. No behaviour changes yet.

**Files:**
- Modify: `Assets/Scripts/Fishing/FishingController.cs`

- [ ] **Step 1: Add `Charging` to the state enum (line 4)**

Replace:
```csharp
public enum FishingState { Idle, Casting, Waiting, Minigame }
```
With:
```csharp
public enum FishingState { Idle, Charging, Casting, Waiting, Minigame }
```

- [ ] **Step 2: Add charge fields after the existing private fields block (after line 31 `private FishData rolledFish;`)**

```csharp
[Header("Cast Charge")]
[Tooltip("Shortest cast distance at zero charge (world units)")]
public float minCastDistance = 0.5f;
[Tooltip("Oscillation speed — full cycles (0→1→0) per second")]
public float chargeRate = 0.8f;
[Tooltip("Cast speed multiplier at full charge, relative to rod.castSpeed")]
public float maxSpeedMultiplier = 1.5f;
[SerializeField] private CastChargeUI castChargeUI;

private float chargeLevel;
private float chargeDir = 1f;

public float ChargeLevel => chargeLevel;
```

- [ ] **Step 3: Add `Charging` case to the `Update` switch (after the `Casting` case)**

Replace:
```csharp
switch (state)
{
    case FishingState.Idle:     UpdateIdle();     break;
    case FishingState.Casting:  UpdateCasting();  break;
    case FishingState.Waiting:  UpdateWaiting();  break;
    case FishingState.Minigame: UpdateMinigame(); break;
}
```
With:
```csharp
switch (state)
{
    case FishingState.Idle:     UpdateIdle();     break;
    case FishingState.Charging: UpdateCharging(); break;
    case FishingState.Casting:  UpdateCasting();  break;
    case FishingState.Waiting:  UpdateWaiting();  break;
    case FishingState.Minigame: UpdateMinigame(); break;
}
```

- [ ] **Step 4: Verify Unity compiles** — check Console. Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Fishing/FishingController.cs
git commit -m "feat: add Charging state infrastructure to FishingController"
```

---

### Task 3: Implement `EnterCharging()` and `UpdateCharging()`

**Files:**
- Modify: `Assets/Scripts/Fishing/FishingController.cs`

- [ ] **Step 1: Replace `UpdateIdle()` so LMB press enters `Charging` instead of calling `TryCast` directly**

Replace:
```csharp
private void UpdateIdle()
{
    Item activeItem = hotbarController.GetActiveItem();
    ActiveRod = activeItem as FishingRod;
    if (ActiveRod == null) return;

    if (Mouse.current.leftButton.wasPressedThisFrame)
        TryCast();
}
```
With:
```csharp
private void UpdateIdle()
{
    Item activeItem = hotbarController.GetActiveItem();
    ActiveRod = activeItem as FishingRod;
    if (ActiveRod == null) return;

    if (Mouse.current.leftButton.wasPressedThisFrame)
        EnterCharging();
}

private void EnterCharging()
{
    chargeLevel = 0f;
    chargeDir   = 1f;
    state       = FishingState.Charging;
    IsFishing   = true;
    castChargeUI?.Show();
}
```

- [ ] **Step 2: Add `UpdateCharging()` after `EnterCharging()`**

```csharp
private void UpdateCharging()
{
    // Cancel on RMB
    if (Mouse.current.rightButton.wasPressedThisFrame)
    {
        castChargeUI?.Hide();
        state     = FishingState.Idle;
        IsFishing = false;
        ActiveRod = null;
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
```

- [ ] **Step 3: Verify Unity compiles** — `UpdateCharging` calls `ExecuteCast` which doesn't exist yet; expect a compile error. That's fine — you'll add it in Task 4.

- [ ] **Step 4: Commit (as WIP — compile error expected)**

```bash
git add Assets/Scripts/Fishing/FishingController.cs
git commit -m "feat: implement EnterCharging and UpdateCharging"
```

---

### Task 4: Replace `TryCast` with `ExecuteCast` and move water check to `OnCastLanded`

**Files:**
- Modify: `Assets/Scripts/Fishing/FishingController.cs`

- [ ] **Step 1: Replace `TryCast()` with `ExecuteCast(float charge)`**

Remove the entire `TryCast()` method and replace it with:

```csharp
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
```

- [ ] **Step 2: Replace `OnCastLanded()` to check water at landing**

Replace:
```csharp
private void OnCastLanded()
{
    fishingLine.NudgeEnabled = true;
    biteDetector.StartDetection();
    state = FishingState.Waiting;
}
```
With:
```csharp
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
```

- [ ] **Step 3: Verify Unity compiles** — check Console. Expected: no errors.

- [ ] **Step 4: Play-mode verification**
  1. Equip a fishing rod in the hotbar
  2. Hold LMB — verify no cast fires immediately
  3. Watch the power bar fill and bounce (if `CastChargeUI` is wired up) or check `FishingController.ChargeLevel` via the Inspector
  4. Release LMB over water — bobber should travel and line should appear; short hold = short cast, longer hold = farther cast
  5. Release LMB aimed at land — bobber should animate to land then disappear (cancel fires)
  6. Hold LMB then press RMB — cast should cancel immediately with no bobber appearing
  7. Verify full charge feels noticeably snappier than minimum charge

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Fishing/FishingController.cs
git commit -m "feat: implement cast charge — ExecuteCast, water check at landing"
```

---

### Task 5: Inspector setup notes

No code changes. Reference for wiring up the UI in Unity Editor.

- [ ] **Step 1: Basic UI setup (optional — skip if building custom UI later)**
  1. In the Player hierarchy, create a child GameObject named `CastChargeBar`
  2. Add a `Canvas` component → set **Render Mode: World Space**, scale it small (e.g. 0.01, 0.01, 1)
  3. Position it above the player sprite (e.g. Y offset ~0.8 units)
  4. Inside the Canvas, create a background `Image` (dark, stretched horizontally)
  5. Inside the Canvas, create a fill `Image` — set `Image Type: Filled`, `Fill Method: Horizontal`, `Fill Origin: Left`
  6. Attach `CastChargeUI` to `CastChargeBar`, assign the fill `Image` to `fillImage`
  7. Assign `CastChargeBar`'s `CastChargeUI` component to `FishingController → castChargeUI`

- [ ] **Step 2: Tune charge fields on `FishingController` in the Inspector**
  - `minCastDistance` — how far the bobber goes at zero charge (default 0.5)
  - `chargeRate` — oscillation speed (default 0.8; higher = faster bouncing bar)
  - `maxSpeedMultiplier` — how much faster the bobber travels at full charge (default 1.5)

- [ ] **Step 3: Final play-mode check** — cast at various charge levels, confirm feel matches intent.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "chore: inspector setup for cast charge UI"
```

---

## Self-Review

**Spec coverage:**
- Charging state → Tasks 2–3 ✓
- Oscillation (0→1→0) → Task 3 `UpdateCharging()` ✓
- Distance from charge level → Task 4 `ExecuteCast` ✓
- Speed from charge level → Task 4 `ExecuteCast` ✓
- Water check at landing → Task 4 `OnCastLanded` ✓
- Power bar UI with virtual contract → Task 1 `CastChargeUI` ✓
- UI wired via `castChargeUI?.Show/Hide/OnChargeChanged` → Tasks 3–4 ✓

**No placeholders detected.**

**Type consistency:**
- `ChargeLevel` (float property) defined in Task 2, read in Task 3 via `castChargeUI?.OnChargeChanged(chargeLevel)` ✓
- `ExecuteCast(float charge)` defined and called consistently across Tasks 3–4 ✓
- `CastChargeUI` methods `Show()`, `Hide()`, `OnChargeChanged(float)` match across Task 1 and calls in Tasks 3–4 ✓
