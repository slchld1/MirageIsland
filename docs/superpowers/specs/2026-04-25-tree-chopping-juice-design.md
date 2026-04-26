# Tree Chopping Juice — Design

**Status:** Approved by user, ready for implementation plan
**Author:** Brainstormed with Claude, 2026-04-25
**Builds on:** `2026-04-24-tree-system-design.md` (Tree System v1)

## Goal

Add visual game-feel to the chop-tree loop:

1. **Hit shake** — tree jitters briefly on every axe hit.
2. **Directional wood drops** — wood drops scatter on the side opposite the player, in line with the fall direction.
3. **Fall animation** — on the felling chop, tree topples 90° away from the player, drops wood mid-fall, lies still briefly, then crossfades into the stump.
4. **Fruit-drop on first hit** — chopping a Ripe tree once knocks the fruit off (no HP damage), tree returns to Mature.

## Architecture

Two scripts and a restructured prefab.

### Scripts

- **`Tree.cs`** (existing, modified) — owns state, damage, save, drops. Computes fall direction. Toggles its own collider. Calls into the animator at the right moments.
- **`TreeAnimator.cs`** (new, sibling component) — owns visual choreography only. Two public methods: `PlayShake()` and `PlayFell(int fallDir, Action onImpact, Action onComplete)`.

Tree.cs holds a `public TreeAnimator animator;` field, wired in the prefab.

**Why split:** Tree.cs is approaching ~200 lines after Tasks 13–16 (save/load) land. Adding ~100 more lines of coroutines mixes game logic with visual timing. A separate animator keeps each file focused on a single responsibility and matches the codebase's existing pattern (`FishingController` separate from `FishingUI`).

### Prefab structure

Tree_Oak.prefab is restructured into two children so the upper tree can shake/fall while the stump base stays rooted:

```
Tree_Oak (root)                         ← Tree.cs, TreeAnimator.cs, Collider2D, root pivot at trunk base
├── Top (child, SpriteRenderer)         ← shakes, rotates, fades on fell
│   └── FruitOverlay (child)            ← fruit sprite, fades with Top
└── Stump (child, SpriteRenderer)       ← always present, never moves
```

The `Top` child is what the animator manipulates. `Stump` never moves. When the tree is alive, the two sprites are authored to align so the tree reads as one continuous visual. When the tree is felled, only `Top` rotates and fades — the stump quietly remains.

Top's pivot must sit at the **base of the upper sprite** (where it visually meets the stump). Rotating `Top.localEulerAngles.z` then pivots the canopy around its bottom edge, which is what makes the topple look right.

The Collider2D stays on the root, sized to the stump area (per existing prefab setup). This collider serves three roles:
- Click detection (`Physics2D.OverlapPoint` from PlayerToolDispatcher).
- Player blocking when alive.
- Player blocking when stump (re-enabled after fall).

It's disabled briefly during the fall sequence (player walks through freely).

## Fall direction algorithm

Computed once at the start of `Fell()` and at the start of `TakeDamage()` for fruit drops (cheap — just one subtraction + abs check).

```
input:  Vector3 playerWorldPos
output: int fallDir   // -1 = west/left, +1 = east/right

dx = playerWorldPos.x - transform.position.x

if |dx| < threshold:        // threshold = 0.5 world units, exposed as Tree field
    fallDir = Random.value < 0.5f ? -1 : +1
else if dx > 0:             // player is east of tree
    fallDir = -1            // tree falls west, away from player
else:                       // player is west of tree
    fallDir = +1            // tree falls east, away from player
```

The `|dx| < threshold` branch handles the "behind" case symmetrically — covers both directly-north and directly-south player positions, since neither has a clear left/right cue.

The wood drops use the same `fallDir` so visuals are consistent: the tree falls west, the wood lands west.

## Fruit drop change

Fruits no longer drop in `Fell()`. They drop on the **first** axe hit against a Ripe tree, with no HP damage taken on that hit:

```
TakeDamage(damage):
    if not choppable / wrong state: return
    fallDir = ComputeFallDirection(player.position)
    animator.PlayShake()

    if state == Ripe:
        DropFruits()              // no fallDir — fruits scatter straight down
        Enter(Mature)             // resets ripenHours timer via Enter()
        return                    // first hit harvests, no damage

    hpRemaining -= damage
    if hpRemaining <= 0:
        Fell(fallDir)
```

`Fell()` no longer needs the `if (state == Ripe) DropFruits()` line — state is guaranteed Mature or non-fruiting by the time we reach Fell.

`DropFruits` keeps its original signature (no fallDir parameter). Drops scatter with a small random offset on both axes (~±0.3 world units) at the trunk position — fruit "falls straight off the tree."

`DropWood` takes a `Vector2 fallDir` parameter and uses:

```
xPush = fallDir.x * Random.Range(0.6f, 1.0f)
yJit  = Random.Range(-0.2f, 0.2f)
offset = new Vector3(xPush, yJit, 0f)
```

So drops land 0.6–1.0 world units in the fall direction, with a small Y wobble so they don't visually stack.

## TreeAnimator — `PlayShake()`

Shakes the **Top child only** so the stump base stays rooted.

```
duration:  0.10s
amplitude: 0.05 world units (~1 px at 16 PPU)
basePos:   topTransform.localPosition snapshot at start
            (where topTransform = animator's serialized ref to the Top child)

each frame:
    elapsed += Time.deltaTime
    topTransform.localPosition = basePos + (Random.insideUnitCircle * amplitude)
end:
    topTransform.localPosition = basePos
```

If a chop arrives while a shake is in flight, the in-flight coroutine is canceled and a fresh one starts (so rapid chops don't drift the canopy off its base).

## TreeAnimator — `PlayFell(int fallDir, Action onImpact, Action onComplete)`

Animates the **Top child only**. Stump renderer is untouched throughout — it's already showing the right sprite from the alive state.

Three sequential phases, total ~1.0s.

**Phase 1 — rotate (0.50s):**

```
targetZ = (fallDir == -1) ? +90f : -90f      // CCW = falls left, CW = falls right
ease = easeInQuad
rotate topTransform.localEulerAngles.z from 0 → targetZ over 0.5s

at 35% elapsed (~0.175s): fire onImpact()    // wood drops here
```

**Phase 2 — lie still (0.20s):**

```
hold pose, no movement
```

**Phase 3 — fade (0.30s):**

```
fade topRenderer.color.a and fruitRenderer.color.a from 1 → 0
```

No sprite swap during the fade — the Stump child is already visible underneath. As Top fades to alpha 0, the stump is revealed.

**End (after 1.00s total):**

```
topTransform.localEulerAngles = (0, 0, 0)    // reset rotation in case we ever re-show
topRenderer.enabled = false                  // hide top entirely (Stump state)
fruitRenderer.enabled = false
fire onComplete()
```

When the tree later regrows from Stump → Mature, `Tree.UpdateSprite()` re-enables `topRenderer`, restores its alpha to 1, and the cycle repeats.

**Pivot:** the Top child's pivot must be at the bottom edge of its sprite (where it meets the stump visually). Rotating `topTransform.localEulerAngles.z` then pivots the canopy around that bottom edge, producing a clean topple. The user sets this pivot in the sprite's Import Settings (Custom pivot) when authoring `OakTree_Top.png`.

## Tree.cs — Fell flow

```
Fell(int fallDir):
    GetComponent<Collider2D>().enabled = false   // no clicks during fall
    animator.PlayFell(
        fallDir,
        onImpact:   () => DropWood(fallDir),
        onComplete: OnFellComplete
    )

OnFellComplete():
    if wasPlanted:
        Destroy(gameObject)         // existing behavior preserved
        return
    if !treeData.regrows:
        permanentlyGone = true
        Destroy(gameObject)
        return
    Enter(TreeState.Stump)          // UpdateSprite hides Top + shows Stump; Enter() stamps stateEnteredAtTotalHours for regrowth timer
    hpRemaining = treeData.chopCount
    GetComponent<Collider2D>().enabled = true
```

The collider toggles off at the start of Fell and back on after the fade. Per the design decision: player can walk through the falling tree freely.

The existing collider already sits roughly at the stump position (per current Tree_Oak prefab setup), so the same Box Collider 2D is reused for the live tree (click target + blocking) and the stump (blocking). No resize needed.

## TreeData additions

The `matureSprite` field is replaced by two new fields representing the split mature art:

```csharp
[Header("Sprites")]
public Sprite saplingSprite;        // existing — used in Seedling state
public Sprite topSprite;            // NEW — upper portion of mature/ripe tree
public Sprite stumpSprite;          // NEW — lower portion (visible in Mature/Ripe AND Stump states)
public Sprite fruitOverlay;         // existing
// public Sprite matureSprite;      // REMOVED — replaced by topSprite + stumpSprite
```

### Sprite-state mapping

`Tree.UpdateSprite()` is rewritten to drive both renderers (Top + Stump) based on state:

| State | `topRenderer` | `stumpRenderer` | `fruitRenderer` |
|---|---|---|---|
| Seedling | sprite = `saplingSprite`, enabled | enabled = false | enabled = false |
| Mature | sprite = `topSprite`, enabled, alpha = 1 | sprite = `stumpSprite`, enabled | enabled = false |
| Ripe | sprite = `topSprite`, enabled, alpha = 1 | sprite = `stumpSprite`, enabled | enabled = true (`fruitOverlay`) |
| Stump | enabled = false | sprite = `stumpSprite`, enabled | enabled = false |

Tree.cs caches references to both renderers (`public SpriteRenderer topRenderer; public SpriteRenderer stumpRenderer; public SpriteRenderer fruitRenderer;`), wired in the prefab.

### Art deliverables

User authors three new sprites for OakTree (and any future species):
- `OakTree_Top.png` — upper canopy + thin upper trunk. Pivot at the bottom edge.
- `OakTree_Stump.png` — lower thick trunk that becomes the visible stump. Pivot at base of trunk.
- (`OakTree_Sapling.png` already exists from Task 3.)

The Top and Stump sprites must visually align: stacked together they should look like a continuous tree. The author can split the existing 96×96 `OakTree_Mature.png` in Aseprite (e.g., bottom 24px = stump, top 72px = top), then assign each portion to the corresponding sprite. Pivots are configured in Unity's Sprite Editor.

## Manual verification matrix

After implementation, the following should hold in a Play-mode test:

| Scenario | Expected behavior |
|---|---|
| Player east of tree, chops 4× | Top of tree shakes per hit (stump base does NOT move); on 4th, top falls west, wood drops west, top fades → stump remains |
| Player west of tree, chops 4× | Same, mirrored: top falls east, wood east |
| Player due south of tree, chops 4× | Random L or R fall (different runs differ); drops + fall agree |
| Player due north of tree, chops 4× | Same: random L/R |
| Player diagonal SE, chops 4× | Falls west (deterministic, dx > threshold) |
| Ripe tree, 1st chop | Fruit drops at trunk (no horizontal push); tree → Mature; HP unchanged |
| Ripe tree, 1st chop then 4 more | 1 fruit + 4 wood drops total; tree fells normally on 5th total chop |
| Mid-fall, player walks toward tree | No collision; player can walk through |
| Post-stump, player tries to walk into stump | Blocked (collider re-enabled) |
| Fall direction during random L/R | Wood drops match the same random direction (visually consistent) |

## Out of scope (future work)

- Sound effects (chop thud, tree falling crash, leaves rustle).
- Dust particle on chop.
- Leaf burst at fall impact.
- Screen shake on fell (camera-level effect).
- Variable fall direction based on facing or aim, beyond player position.
- Multi-frame stump art (e.g., axe-bite marks on each chop). The shake covers per-hit feedback for v1.
