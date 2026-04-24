# Tree System — Design

**Date:** 2026-04-24
**Status:** Design approved, ready for implementation plan
**Scope:** V1 of the tree/farming system. Covers two tree categories (scene-placed nature trees and player-planted baitTrees), chopping with an axe, fruit ripening/picking, regrowth, save/load. Watering, tilling, and axe tiers are parked as documented future extensions.

## Goal

Add an interactive tree system to the game with two distinct categories that share one codebase:

1. **Nature trees** — large scene-placed trees chopped for wood, occasionally bearing fruit. Regrow from stumps over in-game time.
2. **BaitTrees** — small, player-planted fruit trees. Grown from seeds, harvested for fishing bait, permanently removed when chopped.

Both categories share the same state machine, save format, and interaction plumbing. Species differences (size, timings, drops, regrowth behavior) live entirely in data (ScriptableObject), not code.

## Non-goals (this pass)

- Watering and crop decay (parked as future extension).
- Tilling / soil preparation with a shovel (parked as future extension).
- Axe tiers and damage scaling (parked as future extension).
- Procedural tree spawning (architecture is open to it, but v1 is hand-placed).
- Per-stage distinct growth sprites beyond sapling / mature / fruit-overlay.
- Shake-on-hit or fall animations.
- Tree sway / wind.

## Scope overview

Map the system into five new code units plus small additions to the save system:

- `TreeData` (ScriptableObject) — per-species configuration.
- `Tree.cs` (MonoBehaviour) — runtime state machine on every tree GameObject.
- `TreeDataDictionary` — runtime lookup from `treeDataID` → `TreeData` (mirrors `ItemDictionary`).
- `Axe : Item` — chopping tool item class.
- `PlantableSeed : Item` — seed item that spawns a specific tree.
- `Planter` — component on the player that validates planting and instantiates trees.
- `PlayerToolDispatcher` — single LMB switchboard on the player that routes to axe / seed / pick actions.

Save system: one new list `SavedData.treeSaveData`, one new serializable class `TreeSaveData`, one new method pair on `SaveController`.

## Architecture

### Data model — `TreeData` ScriptableObject

One `.asset` per species (e.g. `OakTree.asset`, `BaitTree.asset`). Stored under `Assets/Data/Trees/`.

```csharp
[CreateAssetMenu(fileName = "New TreeData", menuName = "Trees/Tree Data")]
public class TreeData : ScriptableObject
{
    [Header("Identity")]
    public int treeDataID;          // save key — do not renumber after shipping
    public string displayName;

    [Header("Chopping")]
    public int chopCount = 3;       // LMB swings to fell
    public int woodDropMin = 2;
    public int woodDropMax = 4;
    public Item woodItem;           // prefab dropped as logs/wood

    [Header("Fruit (leave fruitItem null for wood-only trees)")]
    public Item fruitItem;
    public int ripenHours = 48;
    public int fruitPerHarvest = 1;

    [Header("Regrowth")]
    public int regrowHours = 72;
    public bool regrows = true;

    [Header("Planting")]
    public bool isPlantable = false;
    public int growHours = 24;      // seedling → mature (planted trees only)

    [Header("Sprites")]
    public Sprite saplingSprite;    // covers stump / seedling / regrowing
    public Sprite matureSprite;
    public Sprite fruitOverlay;     // optional — drawn on top when ripe

    [Header("Prefab")]
    public GameObject treePrefab;   // referenced by Planter and the save loader
}
```

Design notes on this shape:

- `fruitItem == null` marks "this species has no fruit" — no separate boolean needed.
- `treeDataID` is the stable identifier used in save data for runtime-planted trees. It must not be renumbered across saves.
- Both item references (`woodItem`, `fruitItem`) point to `Item` prefabs directly. Matches the existing `Chest.itemPrefab` pattern.

### Runtime state — `Tree.cs`

One `MonoBehaviour` per tree GameObject, driving a four-state machine.

```csharp
public enum TreeState
{
    Seedling,  // only planted trees start here
    Mature,
    Ripe,
    Stump,     // only reached if treeData.regrows
}

public class Tree : MonoBehaviour
{
    public TreeData treeData;
    public bool wasPlanted;
    public string treeID;          // GlobalHelper-generated

    private TreeState state;
    private float stateEnteredAtHour;
    private int hpRemaining;

    private SpriteRenderer mainRenderer;
    private SpriteRenderer fruitRenderer;
}
```

Behavior per state:

| State      | Sprite                          | LMB w/ Axe          | LMB w/o Axe     | Auto-transition                                                |
|------------|---------------------------------|---------------------|------------------|----------------------------------------------------------------|
| `Seedling` | `saplingSprite`                 | ignored (too young) | —               | → `Mature` after `growHours`                                   |
| `Mature`   | `matureSprite`                  | `TakeDamage(1)`     | —               | → `Ripe` after `ripenHours` (only if `fruitItem != null`)      |
| `Ripe`     | `matureSprite` + `fruitOverlay` | `TakeDamage(1)` (fruits also drop if felled) | `PickFruit()` → `Mature` | —                                |
| `Stump`    | `saplingSprite`                 | ignored             | —               | → `Mature` after `regrowHours` (only if `treeData.regrows`)    |

Auto-transitions run from `Update()` by comparing `DayCycleManager.CurrentHour` to `stateEnteredAtHour`. No coroutines, no per-frame animation work — one integer compare per tree per frame.

The `HoursSince(start, now)` helper handles in-game day rollover (the current `DayCycleManager.CurrentHour` wraps 0–24); implementation must compute true elapsed hours across day boundaries.

### Chopping, picking, felling

Core entry points on `Tree`:

```csharp
public void TakeDamage(int damage);
public void PickFruit();
public void InitializeAsPlanted(TreeData data, float currentHour);
private void Fell();
```

`Fell()` behavior — enforces the permanence rule for planted trees structurally, not through data:

```csharp
private void Fell()
{
    DropWood();
    if (state == TreeState.Ripe) DropFruits();

    if (wasPlanted)
    {
        // Planted trees leave no trace — next SaveGame won't list them.
        Destroy(gameObject);
    }
    else if (!treeData.regrows)
    {
        // Scene tree that won't come back — save entry must flag it so load destroys the scene instance.
        permanentlyGone = true;
        Destroy(gameObject);
    }
    else
    {
        Enter(TreeState.Stump);  // scene tree with regrows=true
    }
}
```

The chop-outcome matrix:

| Tree type            | `wasPlanted` | `treeData.regrows` | After chop                                  |
|----------------------|--------------|--------------------|---------------------------------------------|
| Scene oak            | false        | true               | → `Stump`, regrows after `regrowHours`      |
| Scene dead snag      | false        | false              | Destroyed, save entry flagged `permanentlyGone` |
| Planted baitTree     | **true**     | (ignored)          | Destroyed, save entry removed               |

### Axe item — `Axe.cs`

Thin `Item` subclass. Mirrors `FishingRod` exactly — `UseItem()` is a no-op because the action is LMB-driven, not hotbar-number-driven.

```csharp
public class Axe : Item
{
    [Header("Chop")]
    public int damage = 1;

    public override void UseItem() { /* no-op — LMB path drives chopping */ }
}
```

No durability, no damage variance, no `woodMultiplier`. Those are future extensions.

### Planting flow

Two new classes plus a tilemap setup.

**`PlantableSeed.cs`** — the seed item. Holds a reference to the `TreeData` it grows into (the `TreeData` in turn holds the prefab reference).

```csharp
public class PlantableSeed : Item
{
    [Header("Plant")]
    public TreeData treeData;
    public override void UseItem() { /* no-op — LMB path drives planting */ }
}
```

**`Planter.cs`** — component on the player GameObject. Validates the placement, instantiates the tree, initializes it as planted.

```csharp
public class Planter : MonoBehaviour
{
    public LayerMask plantableLayer;
    public float maxPlantDistance = 2f;

    public bool TryPlant(PlantableSeed seed, Vector3 worldPos)
    {
        if (Vector3.Distance(transform.position, worldPos) > maxPlantDistance) return false;
        if (Physics2D.OverlapPoint(worldPos, plantableLayer) == null) return false;
        // reject overlap with existing tree at worldPos (radius check against tree layer)

        GameObject go = Instantiate(seed.treeData.treePrefab, worldPos, Quaternion.identity);
        Tree tree = go.GetComponent<Tree>();
        tree.InitializeAsPlanted(seed.treeData, DayCycleManager.Instance.CurrentHour);
        return true;
    }
}
```

**Tilemap setup (scene authoring):**

1. Add a layer named `Plantable` to Project Settings → Tags and Layers.
2. Create a `Plantable_Natural` tilemap on that layer with a `TilemapCollider2D` (collider type must match a shape — same gotcha as the Water tilemap fix from 2026-04-14 session). Paint tiles where planting is allowed.
3. Assign `plantableLayer` on the player's `Planter` component.
4. Future `Plantable_Tilled` tilemap (see Future Extensions) sits on the same layer — `Planter` requires no changes.

### LMB dispatch — `PlayerToolDispatcher.cs`

Single LMB handler on the player. Routes to the correct subsystem by reading the active hotbar item. This replaces per-tree LMB handling; trees don't subscribe to input directly.

```
On LMB:
    activeItem   = HotbarController.GetActiveItem()
    mouseWorld   = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue())

    switch activeItem:
        case Axe axe           → FindTreeAt(mouseWorld)?.TakeDamage(axe.damage)
        case PlantableSeed s   → if Planter.TryPlant(s, mouseWorld): inventory.Decrement(slot)
        default                → tree = FindTreeAt(mouseWorld); if tree?.state == Ripe: tree.PickFruit()
```

Tree lookup uses `Physics2D.OverlapPoint` against a `Tree` layer (new).

### Rendering

Two-GameObject prefab layout per tree:

```
Tree (root)
├── SpriteRenderer      ← trunk/body, swaps by state
└── FruitOverlay (child)
    └── SpriteRenderer  ← fruit icons, enabled only when Ripe
```

Sprites are assigned from `TreeData` on `Awake()` and swapped only on state-enter (not per frame). Fruit overlay `.enabled` toggles on entering/leaving `Ripe`.

All sprites import at **16 PPU** to match the project's pixel density. A 128×128 sprite at 16 PPU produces an 8-unit-tall tree — intended size for nature trees. BaitTrees author smaller sprites (e.g. 48×48 ≈ 3 units) rather than scaling the GameObject.

### Save / load

Extends the existing `SavedData` / `SaveController` pattern.

**Schema additions:**

```csharp
[System.Serializable]
public class TreeSaveData
{
    public string treeID;
    public int treeDataID;
    public bool wasPlanted;
    public Vector3 position;
    public int state;                // (int)TreeState
    public float stateEnteredAtHour;
    public int hpRemaining;
    public bool permanentlyGone;
}

// SavedData.cs
public List<TreeSaveData> treeSaveData;
```

**`TreeDataDictionary`** — new scene singleton, mirrors `ItemDictionary`:

```csharp
public class TreeDataDictionary : MonoBehaviour
{
    public List<TreeData> treeDataAssets;
    public TreeData Get(int id);
}
```

Used on load to resolve `treeDataID` back to the `TreeData` asset (and via it, to the prefab for planted-tree respawn).

**Save flow** — `SaveController.SaveGame` adds:

```csharp
treeSaveData = FindObjectsByType<Tree>(FindObjectsSortMode.None)
    .Select(t => t.GetSaveData())
    .ToList();
```

Already-destroyed trees (e.g. felled baitTrees) naturally don't appear in the iteration.

**Load flow** — `SaveController.LoadGame` runs two passes after inventory/chest load:

- **Pass 1 — patch scene-placed trees.** For each `Tree` in the scene, find its save entry by `treeID`. If `permanentlyGone`, destroy the GameObject. Otherwise call `tree.LoadFromSave(saved)`.
- **Pass 2 — respawn planted trees.** For each save entry with `wasPlanted == true`, resolve `treeDataID` through `TreeDataDictionary`, instantiate `treeData.treePrefab` at `position`, and call `tree.LoadFromSave(saved)`.

Pass ordering matters: scene patch before planted respawn so the scene query isn't polluted by fresh instances.

**`Tree.LoadFromSave`** restores all serialized fields and calls `UpdateSprite()` to reflect the loaded state.

**Edge cases handled:**

- Scene tree removed from editor between saves → orphaned save entry, skipped silently.
- Scene tree added to editor after save → no save entry, starts at default state.
- Mid-chop save (`hpRemaining > 0`) → reloads with same hp.
- Renumbered `treeDataID` → defensive null check in `Tree.Update` prevents crashes; spec rule: **do not renumber after shipping**.

## File layout

**New files:**

```
Assets/
  Data/
    Trees/
      OakTree.asset
      BaitTree.asset
  Prefabs/
    Trees/
      Tree_Oak.prefab
      Tree_Bait.prefab
    Inventory/
      Axe_Basic.prefab
      Seed_Bait.prefab
  Scripts/
    Trees/
      TreeData.cs
      Tree.cs
      TreeDataDictionary.cs
    ItemFunction/
      Axe.cs
      PlantableSeed.cs
    Player/
      PlayerToolDispatcher.cs
      Planter.cs
  Sprites/
    Trees/
      Oak_Sapling.png
      Oak_Mature.png
      Oak_Fruit.png
      Bait_Seedling.png
      Bait_Mature.png
      Bait_Fruit.png
```

**Modified files:**

- `Assets/Scripts/Map&Save/SavedData.cs` — add `TreeSaveData` class and `treeSaveData` list.
- `Assets/Scripts/Map&Save/SaveController.cs` — add tree save/load passes, hold a `TreeDataDictionary` reference.

**New scene setup:**

- New `Plantable` layer (Project Settings → Tags and Layers).
- `Plantable_Natural` tilemap on the `Plantable` layer, with a `TilemapCollider2D` whose tiles have physics shapes.
- `TreeDataDictionary` GameObject in the scene, populated with all `TreeData` assets.
- Player GameObject: add `PlayerToolDispatcher` and `Planter` components.
- `Axe_Basic` and `Seed_Bait` added to `ItemDictionary.itemPrefabs`.
- A handful of `Tree_Oak` instances hand-placed for testing.

## Testing / verification

Manual in-Editor scenarios:

| # | Scenario                                                                 | Expected                                                          |
|---|--------------------------------------------------------------------------|-------------------------------------------------------------------|
| 1 | Axe selected, LMB on `Tree_Oak` × `chopCount`                            | Tree fells, wood drops between min/max, stump appears             |
| 2 | Wait `regrowHours` after felling Oak                                     | Stump → mature sprite                                             |
| 3 | Oak reaches `ripenHours`; LMB without axe                                | Fruit drops, tree returns to Mature                               |
| 4 | Oak at Ripe; fell with Axe                                               | Drops wood **plus** the ripe fruit                                |
| 5 | Seed_Bait selected, LMB on Plantable tile                                | BaitTree spawns in Seedling state, seed count -1                  |
| 6 | Seed selected, LMB on non-Plantable tile                                 | Nothing plants, seed count unchanged                              |
| 7 | Planted baitTree hits `growHours` → `ripenHours`, LMB without axe        | Fruit picked, tree back to Mature                                 |
| 8 | Planted baitTree chopped with Axe                                        | Destroyed, not stumped, removed from save                         |
| 9 | Save mid-chop on Oak (partial `hpRemaining`), reload                     | Oak reloads with same `hpRemaining`                               |
| 10 | Save during Stump regrowth, reload, wait remaining hours                | Regrowth completes correctly                                      |
| 11 | Save after planting baitTree, reload                                    | BaitTree re-instantiates at same position and state               |
| 12 | LMB on empty ground with axe                                            | No error, no-op                                                   |
| 13 | Select axe via hotbar number (no LMB)                                   | No chop, no error — `UseItem()` is a no-op by design              |
| 14 | Plant over an existing tree                                             | Rejected, seed not consumed                                       |

## Future extensions

Documented here so future sessions can slot them in without re-designing.

- **Watering + decay.** Hooks: `TreeData.needsWatering`, `TreeData.hoursToDecay`, `Tree.lastWateredAtHour`, new `TreeState.Decayed`, `WateringCan : Item`, one additional case in `PlayerToolDispatcher`. Save schema gains `lastWateredAtHour`.
- **Tilling / soil prep.** Hooks: `Plantable_Tilled` tilemap on the same `Plantable` layer; `Shovel : Item`; `Tiller` component on player; dispatcher case. `Planter` unchanged.
- **Axe tiers.** Hooks: `Axe.woodMultiplier` (default `1f`); one-line change in `Tree.Fell` to multiply drops by the chopping axe's multiplier.
- **Procedural spawning.** `Tree` is already origin-agnostic (scene vs. runtime). A spawner component can instantiate trees at runtime using the same `Tree.InitializeAsPlanted` entry point (or a new `InitializeAsSpawned` sibling if behavior diverges).
- **Per-stage distinct sprites.** Replace the two sprite slots with `TreeData.stageSprites[]`.
- **Shake on hit / fall animation.** Short coroutine invoked from `Tree.TakeDamage` / `Tree.Fell`.
- **`Axe` → `Tool` promotion.** When a second tool (e.g. `Pickaxe`) is added, introduce a `Tool : Item` base class with `ToolType` enum + `power`. `Axe` becomes `Axe : Tool`. `PlayerToolDispatcher`'s `switch` changes from concrete-type cases to `Tool` + enum switch. No data/save changes.
