# Fishing System Design
**Date:** 2026-04-11  
**Project:** Turtle Island (2D top-down Unity, C#)

---

## Overview

A real-time positional fishing system triggered by equipping a fishing rod in the hotbar and left-clicking on water. Includes line positioning with Q/E nudge, a brief fish blink mechanic, and a tension-based tug minigame. Loot is determined by rod quality, bait type, and time of day.

---

## Architecture

State machine owner (`FishingController`) delegates to focused components. Each phase is self-contained for easy tuning.

| Script | Responsibility |
|---|---|
| `FishingController` | State machine owner. Reads active hotbar slot to confirm rod equipped. |
| `FishingLine` | Draws line from player to cast point. Handles Q/E nudge. Tracks proximity to nearest fish. |
| `FishBiteDetector` | Countdown to next bite. Modified by proximity bonus, bait, rod, and DayCycleManager phase. Spawns fish blinks. |
| `TugMinigame` | Tension meter driven by LMB hold/release. Handles escape and catch outcomes. |
| `FishingRod` | Extends `Item`. Holds rod quality tier (1–3) and bait slot reference. |
| `FishLootTable` | Given rod tier + bait type + current phase → weighted random fish item ID. |
| `BaitSlotUI` | HUD panel showing active bait; opens filtered inventory for bait swapping. |
| `Bait` | Extends `Item`. Has `baitType` enum field. |

Fish species data lives in a plain serializable class for now — easy to migrate to ScriptableObjects later.

---

## State Flow

```
Idle
 └─ Rod in active hotbar slot + LMB click on water
      └─ Casting
           └─ Line drawn to click point; Q/E nudge active; fish blink occasionally
                └─ FishBiteDetector countdown expires → bite!
                     ├─ TugMinigame (hold LMB to pull)
                     │    ├─ Tension too high → fish escapes → back to Waiting
                     │    ├─ Too slow (slack too long) → fish escapes → back to Waiting
                     │    └─ Tension meter filled → catch! → Result
                     └─ Result: loot roll → AddItem to inventory → back to Idle
```

**Cancellation:** Right-click or hotbar swap cancels fishing and returns to Idle.  
**Escape behavior:** Fish escaping during tug returns to Waiting (not Idle) — another fish can bite.  
**Bait consumption:** Bait consumed on successful catch only.

---

## Casting & Line Positioning

- Player left-clicks a point on water; line draws from player to that world position.
- **Q/E keys** nudge the bob left/right while in Waiting state.
- Fish briefly blink (show sprite for ~0.5s) near the line at random intervals — partial visibility only, not exact location.
- Proximity of bob to nearest fish applies a small multiplier to bite countdown speed (e.g. ×1.0–×1.2 range). Not a guaranteed catch bonus — subtle.

---

## TugMinigame

**Tension meter:** float 0–1, shown as a vertical color-shifting bar (green → yellow → red).

| Zone | Range | Effect |
|---|---|---|
| Too slack | < 0.2 | Danger: fish runs if sustained too long |
| Sweet zone | 0.3 – 0.7 | Safe; reel progress fills |
| Danger zone | > 0.8 | Danger: fish strains; escapes if sustained |

- **Hold LMB** → tension rises steadily
- **Release LMB** → tension falls steadily
- **Reel progress bar** (separate indicator) fills only while tension is in the sweet zone; drains slightly outside it
- Reel progress reaching 100% = successful catch

**Rod quality effect:** higher tier widens sweet zone slightly, extends danger timer before escape.

**UI:** Tension bar + reel progress appear as HUD overlay during minigame only. Hidden otherwise.

---

## Loot Table

`FishLootTable` inputs: rod quality tier + bait type + `DayCycleManager` current phase.  
Output: weighted random fish item ID.

Weights are additive and renormalized before rolling. Designed for easy expansion — add new fish species, bait types, phase bonuses, or rod tiers without restructuring.

Example weight table (all values tunable in Inspector):

| Fish | Base | Dawn bonus | Night bonus | Good bait | Better rod |
|---|---|---|---|---|---|
| Common Fish | 60 | — | — | — | — |
| Rare Fish | 20 | — | +15 | +10 | +10 |
| Dawn Fish | 10 | +30 | — | — | — |
| Night Fish | 10 | — | +30 | — | — |

**Extension hooks (leave room for):**
- Weather modifier (future system)
- Fishing skill/level multiplier (future RPG progression)
- Location-based fish pools (different spots = different tables)
- Seasonal modifiers

---

## Bait Slot UI

- Small panel anchored near hotbar, visible only when a `FishingRod` is the active hotbar slot.
- Displays current bait sprite and remaining count.
- Clicking opens a filtered inventory view showing only `Bait` items.
- `Bait : Item` has a `baitType` enum field used by `FishLootTable`.

---

## Integration Points

- **Inventory:** `FishLootTable` result calls `Inventory.AddItem()` on catch.
- **DayCycleManager:** `FishBiteDetector` and `FishLootTable` read `DayCycleManager.Instance.CurrentPhase`.
- **HotbarController:** `FishingController` watches the active hotbar slot for a `FishingRod` component.
- **SoundEffectManager:** Hook points for cast splash, bite alert, escape, and catch sounds.
- **Item/Bait:** `Bait` and `FishingRod` extend existing `Item` class.

---

## Out of Scope (this iteration)

- Multiplayer / shared fishing spots
- Fishing skill XP / leveling
- Weather system integration
- Animated rod/cast arc
- ScriptableObject migration (deferred, easy to add later)
