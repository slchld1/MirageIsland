# Cast Charge Mechanic — Design Spec
**Date:** 2026-04-13  
**Project:** Turtle Island (Unity 2D, C#)

---

## Overview

Replace the instant-cast with a Stardew Valley-style oscillating power bar. Holding LMB charges a power meter that bounces between 0 and 1. Releasing fires the bobber — the charge level at release determines cast distance and bobber travel speed. The bobber always animates to its target; if it lands outside water the cast cancels.

---

## State Machine Change

Add `FishingState.Charging` between Idle and Casting.

```
Idle → (LMB held, rod equipped) → Charging → (LMB released) → Casting → Waiting → Minigame
                                            → (RMB pressed)  → Idle
```

No other states change.

---

## Charge Logic

Tracked in `FishingController`:

| Field | Type | Default | Notes |
|---|---|---|---|
| `minCastDistance` | float | 0.5 | Shortest possible cast (full charge = `rod.castDistance`) |
| `chargeRate` | float | 0.8 | How fast the bar oscillates (full cycles/sec) |
| `maxSpeedMultiplier` | float | 1.5 | Cast speed at full charge relative to `rod.castSpeed` |

**Each frame in `UpdateCharging()`:**
```
chargeLevel += chargeDir * chargeRate * Time.deltaTime
if chargeLevel >= 1 → chargeLevel = 1, chargeDir = -1
if chargeLevel <= 0 → chargeLevel = 0, chargeDir =  1
```

**On LMB release:**
```
castDist  = Lerp(minCastDistance, rod.castDistance, chargeLevel)
castSpeed = Lerp(rod.castSpeed, rod.castSpeed * maxSpeedMultiplier, chargeLevel)
target    = playerPos + (mouseWorldPos - playerPos).normalized * castDist
→ enter Casting, fire fishingLine.Cast(target, castSpeed, OnCastLanded)
```

**Public property** `ChargeLevel` (float, 0–1) exposed for UI polling.

---

## Water Check

Remove the pre-cast `OverlapPoint` gate from `TryCast`. Instead, in `OnCastLanded`:

```
if Physics2D.OverlapPoint(fishingLine.BobPosition, waterLayer) == null
    → CancelFishing()   // bobber visually lands, then disappears
else
    → biteDetector.StartDetection(), state = Waiting
```

The bobber always animates to its target — the cancel happens after it arrives.

---

## UI Architecture

New script: `CastChargeUI.cs` (attach to a child GameObject of the player).

```csharp
public virtual void OnChargeChanged(float t) { ... }  // t = 0–1
public virtual void Show() { ... }
public virtual void Hide() { ... }
```

`FishingController` holds `[SerializeField] CastChargeUI castChargeUI` and calls:
- `castChargeUI?.Show()` on entering Charging
- `castChargeUI?.OnChargeChanged(chargeLevel)` each frame while Charging
- `castChargeUI?.Hide()` on leaving Charging (release or cancel)

The default implementation drives a `UnityEngine.UI.Image` fill image. Override or replace the class to swap in any visual later — the charge logic never changes.

---

## Files Changed

| File | Change |
|---|---|
| `Assets/Scripts/Fishing/FishingController.cs` | Add `Charging` state, `UpdateCharging()`, refactor `TryCast`, move water check to `OnCastLanded`, expose `ChargeLevel`, wire `CastChargeUI` |
| `Assets/Scripts/Fishing/CastChargeUI.cs` | **New** — virtual `OnChargeChanged`, `Show`, `Hide` with default fill-image implementation |

`FishingRod.cs`, `FishingLine.cs`, and all other fishing scripts are unchanged.

---

## Inspector Setup

1. Create a child GameObject on the Player (e.g. `CastChargeBar`)
2. Add a World Space Canvas → background Image + fill Image
3. Attach `CastChargeUI` to `CastChargeBar`, assign `fillImage` in Inspector
4. Assign `CastChargeBar`'s `CastChargeUI` component to `FishingController.castChargeUI`
5. Tune `minCastDistance`, `chargeRate`, `maxSpeedMultiplier` on `FishingController`

(Steps 1–4 are optional if you plan to build a custom UI — `castChargeUI` can be left unassigned until ready.)
