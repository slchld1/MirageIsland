# Fishing Rod Held Animation — Design

**Date:** 2026-04-23
**Status:** Design approved, ready for implementation plan
**Scope:** First pass — 3 animation clips (Idle / Cast / Reel) for `BasicFishingRod`. Architecture must trivially extend to more rods and more per-state clips later.

## Goal

When the player holds a fishing rod:

1. Display a sprite distinct from the inventory icon.
2. Animate the held rod via a sprite-sheet Animator that reacts to the fishing state machine.

The current `HeldItemRenderer` pulls the sprite straight from the inventory `Image.sprite` and shows a single static frame. This design replaces that (for animated items only) with an Animator-driven renderer, while keeping non-animated items behaving exactly as they do today.

## Non-goals (this pass)

- Per-state clips for all 5 fishing states. Only 3 clips now.
- Player body / arm animation. The rod sprite animates alone.
- Authoring clips for tier 2 / tier 3 rods. The system will support them; clips are authored when those rods are added.

## Animation scope

Map the 5 `FishingState` values to 3 `FishingRodAnimState` values:

| FishingState | FishingRodAnimState |
|--------------|---------------------|
| `Idle`       | `Idle` (0)          |
| `Charging`   | `Cast` (1)          |
| `Casting`    | `Cast` (1)          |
| `Waiting`    | `Reel` (2)          |
| `Minigame`   | `Reel` (2)          |

This coarse mapping is intentional. When we want e.g. a distinct `Bite` flash or a `Charge` twitch, we promote that state out of its group and add a new clip — no state-machine surgery required.

## Architecture

### Data model changes — `FishingRod.cs`

Add one field:

```csharp
[Header("Held Visual")]
[Tooltip("Animator Override Controller for this rod's held animations. " +
         "Overrides the base FishingRodAnimator clips (Idle / Cast / Reel).")]
public AnimatorOverrideController heldAnimator;
```

No `heldSprite` field is needed — the `Idle` clip's first frame is what the player sees when no other state is active.

### Shared base: `FishingRodAnimator.controller`

Asset path: `Assets/Animations/Fishing/FishingRodAnimator.controller`

- **States:** `Idle`, `Cast`, `Reel`
- **Parameter:** `FishingState` (int)
- **Transitions:** each state has "Any State → <self>" transitions gated on `FishingState` equal to the matching int (Idle=0, Cast=1, Reel=2). "Has Exit Time" off; transition duration 0 for snappy swaps.
- **Clips:** empty placeholders on the base. Each rod's `AnimatorOverrideController` supplies the actual clips.

### Per-rod: `AnimatorOverrideController`

One override per rod type. `BasicRod_Override.overrideController` references `FishingRodAnimator` as its base and fills in `BasicRod_Idle`, `BasicRod_Cast`, `BasicRod_Reel`.

Adding a new rod = duplicate the override, drop in new clips, assign it to that rod prefab's `heldAnimator` field.

### New component: `FishingRodAnimationController.cs`

Lives on the `HoldPoint` GameObject alongside the Animator. Small and single-purpose.

```csharp
public enum FishingRodAnimState { Idle = 0, Cast = 1, Reel = 2 }

public class FishingRodAnimationController : MonoBehaviour
{
    private Animator animator;
    private static readonly int FishingStateHash = Animator.StringToHash("FishingState");

    private void Awake() { animator = GetComponent<Animator>(); }

    public void SetState(FishingRodAnimState state)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetInteger(FishingStateHash, (int)state);
    }
}
```

### Rendering — `HeldItemRenderer.cs` changes

Add a cached `Animator` reference. When the hotbar selection changes:

- **If the active item is a `FishingRod` with a `heldAnimator`:**
  - `animator.runtimeAnimatorController = rod.heldAnimator`
  - Do not manually write `holdRenderer.sprite` — the Animator owns it via the `Idle` clip.
- **Otherwise (non-rod item, or rod without an override assigned):**
  - `animator.runtimeAnimatorController = null`
  - Fall back to existing behavior: `holdRenderer.sprite = icon.sprite`

The idle-visibility logic (`holdRenderer.enabled = !isMoving && idleTimer >= showDelay`) is unchanged. Movement still hides the held sprite the same way it does today.

### State-change integration — `FishingController.cs`

Resolve `FishingRodAnimationController` lazily when `ActiveRod` becomes non-null (look it up from `HeldItemRenderer.holdPoint`). This avoids a hard reference to the player hierarchy.

Add:

```csharp
private FishingRodAnimationController rodAnim;
private void SetRodAnim(FishingRodAnimState s) => rodAnim?.SetState(s);
```

Call `SetRodAnim` at transitions:

- `EnterCharging()` → `Cast`
- `OnCastLanded()` → `Reel` (on success — when bobber lands in water)
- `CancelFishing()` → `Idle`
- `OnFishCaught()` → `Idle` (via `CancelFishing`)

`OnFishBite` does not change the anim state — it stays `Reel`, which covers both Waiting and Minigame.

## File layout

New files:

```
Assets/
  Animations/
    Fishing/
      FishingRodAnimator.controller
      BasicRod/
        BasicRod_Idle.anim
        BasicRod_Cast.anim
        BasicRod_Reel.anim
        BasicRod_Override.overrideController
  Scripts/
    Player/
      FishingRodAnimationController.cs
  Sprites/
    Items/
      FishingRod/
        BasicRod_Held_SpriteSheet.png   (sliced into frames)
```

Modified files:

- `Assets/Scripts/Fishing/FishingRod.cs`
- `Assets/Scripts/Player/HeldItemRenderer.cs`
- `Assets/Scripts/Fishing/FishingController.cs`
- Player prefab's `HoldPoint` GameObject (add `Animator` + `FishingRodAnimationController`)
- `BasicFishingRod.prefab` (assign `heldAnimator`)

## Authoring workflow (per rod)

1. Import sprite sheet; Sprite Mode → Multiple; slice frames in Sprite Editor.
2. Drag frame ranges into the scene to author `BasicRod_Idle.anim`, `BasicRod_Cast.anim`, `BasicRod_Reel.anim`.
3. Right-click `FishingRodAnimator.controller` → Create → Animator Override Controller. Name it `BasicRod_Override`. Assign the 3 clips.
4. On the `BasicFishingRod` prefab, drag `BasicRod_Override` into the `heldAnimator` field.

## Testing / verification

Manual, in the Editor:

1. **Idle playback** — select rod in hotbar, stand still, confirm Idle clip loops.
2. **Cast transition** — hold LMB, confirm Cast clip plays during Charging + Casting.
3. **Reel transition** — after bobber lands in water, confirm Reel clip plays.
4. **Return to Idle** — right-click cancel, or catch a fish, and confirm rod returns to Idle clip.
5. **Non-rod item** — select a seed/fish in hotbar, confirm static sprite shows with no animation.
6. **Rod swap mid-fish** — switch hotbar slot during Waiting or Minigame, confirm fishing cancels cleanly and the next item renders correctly.
7. **Movement hides held sprite** — walk around with rod selected; confirm held sprite hides while moving and returns after `showDelay`.

## Extension points

- **Add a 4th/5th clip** (e.g. `Bite`, `Charge`) — add the state + int value to the base controller, the enum, and the `FishingState → FishingRodAnimState` mapping; author the clip in each rod's override.
- **Animate non-rod items** — promote the `heldAnimator` field from `FishingRod` to the base `Item` class. `HeldItemRenderer` already checks for an override; no further changes needed.
- **Tier 2 / Tier 3 rods** — duplicate `BasicRod_Override`, swap clips, assign on the new rod prefab. No code changes.
