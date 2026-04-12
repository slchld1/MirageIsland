# Fishing Minigame Variety Design

## Goal

Make the fishing minigame feel different each catch by giving each fish species a defined fight pattern (phase sequence + reaction events) and a rarity rating that scales difficulty.

## Architecture

`FishData` gains a `rarity` field (1–5) and a `FightPhase[]` array. `TugMinigame` steps through phases sequentially, firing timed reaction events the player must answer with a keypress or click. `TugMinigameUI` displays prompts. Rarity applies a global difficulty multiplier at runtime — no extra data per rarity level.

## Data Structures

### FightPhase (serializable struct on FishData)

| Field | Type | Description |
|---|---|---|
| `type` | `PhaseType` (Calm/Struggle/Tired) | Visual/audio label only — no mechanical difference currently |
| `duration` | `float` | Seconds this phase lasts |
| `eventInterval` | `float` | Base seconds between reaction events |
| `possibleEvents` | `EventType[]` | Which events can fire: Dart, Tug, or both |

### FishData additions

| Field | Type | Description |
|---|---|---|
| `rarity` | `int` (1–5) | 1 = common, 5 = legendary |
| `phases` | `FightPhase[]` | Ordered fight sequence |

### Rarity difficulty curve

| Rarity | Event interval multiplier | Reaction window multiplier |
|---|---|---|
| 1 | 1.0× | 1.0× (full window) |
| 2 | 0.85× | 0.85× |
| 3 | 0.70× | 0.70× |
| 4 | 0.55× | 0.55× |
| 5 | 0.40× | 0.40× |

Applied once when the minigame starts: `scaledInterval = phase.eventInterval * rarityMultiplier`.

## Reaction Events

Two event types fire during a phase:

**Dart** — fish darts left or right.
- UI shows ← or → arrow prompt.
- Player taps Q (left) or E (right) within the reaction window.
- Miss: tension spike (+0.2).
- Hit: tension relief (−0.15).

**Tug** — fish makes a sharp pull.
- UI shows a "!" prompt.
- Player releases LMB and taps LMB once within the reaction window.
- Miss: tension spike (+0.2).
- Hit: reel progress bonus (+0.1).

Base reaction window: **1.2 seconds**, scaled by rarity multiplier.

## TugMinigame Changes

New internal state:
- `currentPhaseIndex` — which phase is active
- `phaseTimer` — counts down phase duration, advances index on expiry
- `eventTimer` — counts down to next event fire
- `activeEvent` — currently displayed event (null if none)
- `eventWindowTimer` — counts down the player's reaction window

Flow each `Tick()`:
1. Tick `phaseTimer` → advance phase if expired
2. Tick `eventTimer` → fire a random event from current phase's `possibleEvents`
3. If `activeEvent != null`, tick `eventWindowTimer` → apply miss penalty if expired
4. Check player input against `activeEvent` → resolve hit or miss

`StartMinigame(int rodTier, FishData fish)` — takes fish to read rarity and phases. Existing `rodTier` sweet-zone widening stays unchanged.

## TugMinigameUI Changes

New prompts displayed over the existing tension/reel bars:
- Dart prompt: left/right arrow sprite, shown for the duration of the reaction window
- Tug prompt: "!" text or sprite, shown for the duration of the reaction window
- Prompts disappear on hit, miss, or window expiry

## Scope / Out of Scope

**In scope:**
- `FightPhase` struct and `EventType` enum
- `rarity` field on `FishData`
- Phase stepping and event firing in `TugMinigame`
- Two event types: Dart and Tug
- UI prompts in `TugMinigameUI`

**Out of scope (polish later):**
- Per-phase music/sound changes
- Phase type (Calm/Struggle/Tired) affecting tension bar behavior
- Visual bobber animation during events
- Combo multipliers or streak bonuses
