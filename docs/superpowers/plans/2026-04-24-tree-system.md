# Tree System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a tree system with scene-placed nature trees (Oak) and player-planted baitTrees, sharing one state machine and save format. First playable loop: chop oaks for wood, plant bait seeds, harvest bait from mature baitTrees.

**Architecture:** One `Tree.cs` MonoBehaviour with a 4-state machine (`Seedling` / `Mature` / `Ripe` / `Stump`), driven by in-game hours from `DayCycleManager`. Per-species config lives in `TreeData` ScriptableObjects. LMB input is dispatched through a single `PlayerToolDispatcher` on the player that switches on the active hotbar item (Axe → chop, Seed → plant, otherwise → pick fruit).

**Tech Stack:** Unity 2D, C#, Unity Input System, Tilemaps, ScriptableObjects. No test framework — verification is manual in the Editor (mirrors how the fishing system was built).

**Reference spec:** `docs/superpowers/specs/2026-04-24-tree-system-design.md`

**Execution style note:** The user writes all code and does all Unity setup. The engineer's job (or Claude's, acting as guide) is to explain each step before typing, wait for the user to complete it, help debug, and move on only after in-Editor verification passes. Each task ends with a verification step and a commit.

---

## Phase 1 — Data foundation (TreeData + one Oak asset)

### Task 1: Create `TreeData` ScriptableObject class

**Goal:** Define the per-species config type so we can author species as `.asset` files.

**Files:**
- Create: `Assets/Scripts/Trees/TreeData.cs`

- [ ] **Step 1: Create the Trees folder and file**

In the Unity Project window, right-click `Assets/Scripts` → `Create → Folder` → name it `Trees`. Then right-click the new `Trees` folder → `Create → C# Script` → name it `TreeData`. Double-click to open in your editor.

- [ ] **Step 2: Replace the file contents with the `TreeData` class**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "New TreeData", menuName = "Trees/Tree Data")]
public class TreeData : ScriptableObject
{
    [Header("Identity")]
    public int treeDataID;
    public string displayName;

    [Header("Chopping")]
    public int chopCount = 3;
    public int woodDropMin = 2;
    public int woodDropMax = 4;
    public Item woodItem;

    [Header("Fruit (leave fruitItem null for wood-only trees)")]
    public Item fruitItem;
    public int ripenHours = 48;
    public int fruitPerHarvest = 1;

    [Header("Regrowth")]
    public int regrowHours = 72;
    public bool regrows = true;

    [Header("Planting")]
    public bool isPlantable = false;
    public int growHours = 24;

    [Header("Sprites")]
    public Sprite saplingSprite;
    public Sprite matureSprite;
    public Sprite fruitOverlay;

    [Header("Prefab")]
    public GameObject treePrefab;
}
```

- [ ] **Step 3: Create a data folder and the OakTree asset**

In Unity:
1. Create folder `Assets/Data/Trees/` (create both `Data` and `Trees` if missing).
2. Right-click in `Assets/Data/Trees/` → `Create → Trees → Tree Data` → name it `OakTree`.
3. Select `OakTree` and fill these fields in the Inspector (leave unset fields at default):
   - `treeDataID` = `1`
   - `displayName` = `Oak`
   - `chopCount` = `4`
   - `woodDropMin` = `2`, `woodDropMax` = `4`
   - `regrows` = checked
   - `regrowHours` = `72`
   - `isPlantable` = unchecked
   - (Sprites + prefab will be filled in later tasks once they exist.)

- [ ] **Step 4: Verify in Editor**

Select `OakTree.asset` and confirm the Inspector shows all the fields grouped under their headers, with the Oak values you entered.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Trees/TreeData.cs "Assets/Data/Trees/OakTree.asset" "Assets/Data/Trees/OakTree.asset.meta" "Assets/Data/Trees.meta" "Assets/Data.meta" "Assets/Scripts/Trees.meta"
git commit -m "feat: add TreeData ScriptableObject + OakTree asset"
```

---

### Task 2: Create `Tree.cs` skeleton (state enum, fields, sprite update)

**Goal:** The runtime component that lives on every tree. This task only does the data + rendering half — no time transitions, no chopping yet.

**Files:**
- Create: `Assets/Scripts/Trees/Tree.cs`

- [ ] **Step 1: Create the script**

In Unity, right-click `Assets/Scripts/Trees/` → `Create → C# Script` → name it `Tree`. Open in editor.

- [ ] **Step 2: Replace contents**

```csharp
using UnityEngine;

public enum TreeState
{
    Seedling,
    Mature,
    Ripe,
    Stump,
}

public class Tree : MonoBehaviour
{
    [Header("Config")]
    public TreeData treeData;

    [Header("Runtime")]
    public TreeState state = TreeState.Mature;
    public bool wasPlanted = false;
    public string treeID;

    [Header("Renderers (assign in prefab)")]
    public SpriteRenderer mainRenderer;
    public SpriteRenderer fruitRenderer;

    private void Awake()
    {
        if (string.IsNullOrEmpty(treeID))
        {
            treeID = GlobalHelper.GenerateUniqueId(gameObject);
        }
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (treeData == null || mainRenderer == null) return;

        switch (state)
        {
            case TreeState.Seedling: mainRenderer.sprite = treeData.saplingSprite; break;
            case TreeState.Mature:   mainRenderer.sprite = treeData.matureSprite;  break;
            case TreeState.Ripe:     mainRenderer.sprite = treeData.matureSprite;  break;
            case TreeState.Stump:    mainRenderer.sprite = treeData.saplingSprite; break;
        }

        if (fruitRenderer != null)
        {
            bool showFruit = state == TreeState.Ripe && treeData.fruitOverlay != null;
            fruitRenderer.sprite = showFruit ? treeData.fruitOverlay : null;
            fruitRenderer.enabled = showFruit;
        }
    }
}
```

- [ ] **Step 3: Verify script compiles**

Save the file. Return to Unity. Wait for the "recompiling" spinner to finish. Check the Console: there should be **no errors**. (A warning about `fruitRenderer` being unassigned is fine — we haven't built the prefab yet.)

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Trees/Tree.cs
git commit -m "feat: add Tree MonoBehaviour skeleton with state enum and sprite update"
```

---

### Task 3: Build the Oak tree prefab + drop one in the scene

**Goal:** A visible, static Oak tree rendering the correct sprite based on state. No behavior yet — just the visual scaffolding.

**Files:**
- Create: `Assets/Prefabs/Trees/Tree_Oak.prefab`

- [ ] **Step 1: Import Oak sprites**

If you don't have sprites yet:
1. Drop your Oak sprite PNGs into `Assets/Sprites/Trees/` (create the folder).
2. Select each PNG. In the Inspector: set `Pixels Per Unit = 16`, `Filter Mode = Point (no filter)`, `Compression = None`. Click Apply.
3. For a first pass, use any two images — one for `Oak_Sapling.png`, one for `Oak_Mature.png`. You can replace with better art later.

- [ ] **Step 2: Fill sprite fields on OakTree.asset**

Select `Assets/Data/Trees/OakTree.asset`. Drag:
- `Oak_Sapling.png` → `Sapling Sprite`
- `Oak_Mature.png` → `Mature Sprite`
- (Leave `Fruit Overlay` empty for now — Oak doesn't need fruit for initial testing.)

- [ ] **Step 3: Build the prefab GameObject hierarchy**

In the Scene Hierarchy (not Project):
1. Right-click → `Create Empty` → name it `Tree_Oak`.
2. Set its position to something visible (e.g. `(0, 0, 0)`).
3. On `Tree_Oak`, click `Add Component → Sprite Renderer`. This is the main renderer.
4. Right-click `Tree_Oak` in Hierarchy → `Create Empty` as child → name it `FruitOverlay`.
5. On `FruitOverlay`, add `Sprite Renderer` component. In the Inspector, uncheck its `Enabled` toggle (leftmost corner of the component header). Set its `Order in Layer` to `1` (higher than the parent's default `0`).
6. On `Tree_Oak`, click `Add Component → Tree` (your script). In the Tree component:
   - Drag `OakTree.asset` into `Tree Data`
   - Drag `Tree_Oak`'s own `Sprite Renderer` into `Main Renderer`
   - Drag `FruitOverlay`'s `Sprite Renderer` into `Fruit Renderer`

- [ ] **Step 4: Save as prefab**

Drag `Tree_Oak` from the Hierarchy into `Assets/Prefabs/Trees/` (create the folder). This creates `Tree_Oak.prefab`. The scene instance is now a prefab instance (shown in blue in Hierarchy).

- [ ] **Step 5: Wire the prefab reference back into OakTree.asset**

Select `OakTree.asset`. Drag `Tree_Oak.prefab` (from the Project window) into the `Tree Prefab` field.

- [ ] **Step 6: Verify in Editor**

With the Oak in your scene, press Play. The tree should render the mature sprite. In the Inspector, change `State` on the Tree component to `Seedling` or `Stump` — the sprite should swap to the sapling sprite. Change it back to `Mature` — swap back. Exit Play mode.

Note: `UpdateSprite` only runs in `Awake()`, so changing state in the Inspector after Play starts won't swap live. That's fine; we'll add `Update()` tick in the next phase. The manual way to verify now: set state in Inspector BEFORE pressing Play, then check the sprite on Play start.

- [ ] **Step 7: Commit**

```bash
git add "Assets/Sprites/Trees" "Assets/Prefabs/Trees" "Assets/Data/Trees/OakTree.asset"
git commit -m "feat: add Tree_Oak prefab with sapling/mature sprites"
```

---

## Phase 2 — Time-driven state transitions

### Task 4: Add hour-based auto-transitions to `Tree`

**Goal:** Trees automatically progress through states as in-game hours pass. Mature → Ripe, Stump → Mature, Seedling → Mature. Driven by `DayCycleManager.CurrentHour`.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`

- [ ] **Step 1: Understand what we're adding and why**

Every tree needs to ask "how many in-game hours have passed since I entered my current state?" and transition when enough time has elapsed. `DayCycleManager.CurrentHour` wraps 0–24, so we need a helper that handles day rollover by tracking **total elapsed hours** using a monotonically increasing value.

The simplest reliable approach: snapshot the total elapsed hours (from game start) at every state change, and compare to current total. We'll compute total elapsed as `day * 24 + currentHour`.

**Before writing this, check one thing:** does `DayCycleManager` expose a day counter or a `TotalHours`? Open `Assets/Scripts/DayCycle/DayCycleManager.cs` and look.

- [ ] **Step 2: Inspect DayCycleManager**

Open `Assets/Scripts/DayCycle/DayCycleManager.cs` and note which of these it exposes:
- `CurrentHour` (confirmed — we already use it)
- A day counter (`CurrentDay`, `DayNumber`, etc.)?
- A total-hours accessor (`TotalHours`, `ElapsedHours`)?

**Report what you find back to me before proceeding** — depending on what exists, the tick code below may need to adapt. The code below assumes `CurrentHour` only, which works but has a subtle edge case (see Step 3 explanation).

- [ ] **Step 3: Add `Update()` with state transitions**

Replace `Tree.cs` with the version below. This adds:
- `stateEnteredAtHour` field (saved, drives the "how long in state?" check)
- `Enter(TreeState)` helper that sets state, stamps the hour, and refreshes sprite
- `Update()` that checks elapsed hours and transitions

```csharp
using UnityEngine;

public enum TreeState
{
    Seedling,
    Mature,
    Ripe,
    Stump,
}

public class Tree : MonoBehaviour
{
    [Header("Config")]
    public TreeData treeData;

    [Header("Runtime")]
    public TreeState state = TreeState.Mature;
    public bool wasPlanted = false;
    public string treeID;
    public float stateEnteredAtHour;

    [Header("Renderers (assign in prefab)")]
    public SpriteRenderer mainRenderer;
    public SpriteRenderer fruitRenderer;

    private void Awake()
    {
        if (string.IsNullOrEmpty(treeID))
        {
            treeID = GlobalHelper.GenerateUniqueId(gameObject);
        }
        UpdateSprite();
    }

    private void Start()
    {
        // Stamp initial state entry on first frame so HoursSince has a baseline.
        if (stateEnteredAtHour == 0f && DayCycleManager.Instance != null)
        {
            stateEnteredAtHour = DayCycleManager.Instance.CurrentHour;
        }
    }

    private void Update()
    {
        if (treeData == null || DayCycleManager.Instance == null) return;

        float now = DayCycleManager.Instance.CurrentHour;
        float elapsed = HoursSince(stateEnteredAtHour, now);

        switch (state)
        {
            case TreeState.Seedling:
                if (elapsed >= treeData.growHours) Enter(TreeState.Mature);
                break;
            case TreeState.Mature:
                if (treeData.fruitItem != null && elapsed >= treeData.ripenHours) Enter(TreeState.Ripe);
                break;
            case TreeState.Stump:
                if (treeData.regrows && elapsed >= treeData.regrowHours) Enter(TreeState.Mature);
                break;
        }
    }

    private void Enter(TreeState next)
    {
        state = next;
        if (DayCycleManager.Instance != null)
        {
            stateEnteredAtHour = DayCycleManager.Instance.CurrentHour;
        }
        UpdateSprite();
    }

    private float HoursSince(float startHour, float nowHour)
    {
        // CurrentHour wraps 0-24. Assume at most one wrap between state-enter and now
        // (a tree won't sit mid-state across days without Update running).
        // If 'now' is less than 'start', we've wrapped once — add 24.
        return nowHour >= startHour ? nowHour - startHour : (24f - startHour) + nowHour;
    }

    private void UpdateSprite()
    {
        if (treeData == null || mainRenderer == null) return;

        switch (state)
        {
            case TreeState.Seedling: mainRenderer.sprite = treeData.saplingSprite; break;
            case TreeState.Mature:   mainRenderer.sprite = treeData.matureSprite;  break;
            case TreeState.Ripe:     mainRenderer.sprite = treeData.matureSprite;  break;
            case TreeState.Stump:    mainRenderer.sprite = treeData.saplingSprite; break;
        }

        if (fruitRenderer != null)
        {
            bool showFruit = state == TreeState.Ripe && treeData.fruitOverlay != null;
            fruitRenderer.sprite = showFruit ? treeData.fruitOverlay : null;
            fruitRenderer.enabled = showFruit;
        }
    }
}
```

**Note on `HoursSince`:** this 1-wrap-max assumption is only safe if `Update()` runs often enough that we never miss a wrap. For trees this is trivially true (Update is per-frame). If/when `DayCycleManager` exposes a monotonic hour counter, we'll replace this with a direct subtraction.

- [ ] **Step 4: Verify in Editor — growth**

Set up a quick repeatable test:
1. In the scene, select your `Tree_Oak` instance.
2. In the Tree component, set `State = Stump`, `Stateentered At Hour = 0`.
3. On `OakTree.asset`, temporarily change `regrowHours` to `1` (so regrowth takes 1 in-game hour for fast testing).
4. Press Play. The tree should show the sapling sprite, then swap to the mature sprite after ~1 in-game hour passes (check your day cycle speed — if 1 in-game hour takes 30 real seconds, you'll wait ~30s).
5. Stop Play. Reset `regrowHours` back to `72` on `OakTree.asset`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Trees/Tree.cs
git commit -m "feat: implement Tree Update loop with hour-based state transitions"
```

---

## Phase 3 — Axe + chopping

### Task 5: Create `Axe.cs` item class and Axe prefab

**Goal:** Add a new Item subclass for the axe and a prefab the player can pick up.

**Files:**
- Create: `Assets/Scripts/ItemFunction/Axe.cs`
- Create: `Assets/Prefabs/Inventory/Axe_Basic.prefab`

- [ ] **Step 1: Create the Axe script**

In `Assets/Scripts/ItemFunction/`, right-click → `Create → C# Script` → name it `Axe`. Replace contents:

```csharp
using UnityEngine;

public class Axe : Item
{
    [Header("Chop")]
    public int damage = 1;

    public override void UseItem()
    {
        // No-op. Chopping is driven by LMB via PlayerToolDispatcher,
        // not by hotbar number-key selection.
    }
}
```

- [ ] **Step 2: Build the Axe prefab**

Easiest path: duplicate the existing `FishingRod_Basic.prefab` as a template, then swap the component and icon.

1. In Project: right-click `Assets/Prefabs/Inventory/FishingRod_Basic.prefab` → Duplicate. Rename the copy to `Axe_Basic.prefab`.
2. Open `Axe_Basic.prefab` (double-click).
3. Remove the `FishingRod` component.
4. `Add Component → Axe`.
5. Fill fields on `Axe`:
   - `ID` = next unused item ID (check `ItemDictionary` in the scene — pick a number no other item uses, e.g. `10`)
   - `Name` = `Basic Axe`
   - `damage` = `1`
6. Replace the `Image` component's `Source Image` with an axe icon sprite (any placeholder is fine for now).
7. Save prefab.

- [ ] **Step 3: Register in ItemDictionary**

Select the `ItemDictionary` GameObject in the scene Hierarchy. In the Inspector, add `Axe_Basic` to the `Item Prefabs` list (increase size, drag prefab in).

- [ ] **Step 4: Verify in Editor**

Press Play. Open inventory if needed. You should be able to pick up the axe if placed in the world (drop a copy near the player), or spawn it into inventory via whatever dev-console you use. Select it in the hotbar — nothing should happen (no chopping yet, no Use behavior — that's correct).

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/ItemFunction/Axe.cs" "Assets/Prefabs/Inventory/Axe_Basic.prefab" "Assets/Prefabs/Inventory/Axe_Basic.prefab.meta"
git commit -m "feat: add Axe item class and Axe_Basic prefab"
```

---

### Task 6: Add `Tree` layer and `TakeDamage` / `Fell` methods

**Goal:** Trees take damage and fall when HP hits zero. We set up a `Tree` physics layer now so the dispatcher can find trees via `OverlapPoint` in the next task.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`
- Modify: `Assets/Prefabs/Trees/Tree_Oak.prefab`

- [ ] **Step 1: Add a `Tree` layer**

In Unity: `Edit → Project Settings → Tags and Layers → Layers`. Find an empty User Layer slot (e.g., `User Layer 11`) and type `Tree`. Close the window.

- [ ] **Step 2: Add a trigger collider to Tree_Oak prefab**

Open `Tree_Oak.prefab`. On the root `Tree_Oak` GameObject:
1. Set the GameObject's Layer (top-right of Inspector) to `Tree`. Apply to children when Unity asks.
2. `Add Component → Box Collider 2D`. Check `Is Trigger`. Adjust `Size` so it covers the trunk (doesn't need to be tight — it's just for click detection).

Save the prefab.

- [ ] **Step 3: Extend `Tree.cs` with HP and damage methods**

Add these fields and methods to `Tree.cs`:

```csharp
// Add to the Runtime header section:
public int hpRemaining;
public bool permanentlyGone;  // used in save data

// Add inside Awake(), after the treeID assignment:
hpRemaining = treeData != null ? treeData.chopCount : 1;

// Add these methods anywhere in the class:

public void TakeDamage(int damage)
{
    if (state == TreeState.Seedling || state == TreeState.Stump) return;  // can't chop
    hpRemaining -= damage;
    if (hpRemaining <= 0) Fell();
}

private void Fell()
{
    DropWood();
    if (state == TreeState.Ripe) DropFruits();

    if (wasPlanted)
    {
        // Planted trees leave no trace — next save won't list them.
        Destroy(gameObject);
    }
    else if (!treeData.regrows)
    {
        permanentlyGone = true;
        Destroy(gameObject);
    }
    else
    {
        hpRemaining = treeData.chopCount;
        Enter(TreeState.Stump);
    }
}

private void DropWood()
{
    if (treeData.woodItem == null) return;
    int count = Random.Range(treeData.woodDropMin, treeData.woodDropMax + 1);
    for (int i = 0; i < count; i++)
    {
        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
        GameObject drop = Instantiate(treeData.woodItem.gameObject, transform.position + offset, Quaternion.identity);
        var bounce = drop.GetComponent<BounceEffect>();
        if (bounce != null) bounce.StartBounce();
    }
}

private void DropFruits()
{
    if (treeData.fruitItem == null) return;
    for (int i = 0; i < treeData.fruitPerHarvest; i++)
    {
        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
        GameObject drop = Instantiate(treeData.fruitItem.gameObject, transform.position + offset, Quaternion.identity);
        var bounce = drop.GetComponent<BounceEffect>();
        if (bounce != null) bounce.StartBounce();
    }
}
```

Note the pattern: mirrors `Chest.Interact()` — instantiate item prefab at tree position + small random offset, trigger BounceEffect so it visibly drops.

- [ ] **Step 4: Assign a wood Item prefab on OakTree.asset**

You need a wood item prefab to drop. If you don't have one yet:
1. Duplicate an existing simple item prefab (e.g. a fish prefab) into `Assets/Prefabs/Inventory/Wood.prefab`.
2. On Wood prefab: set `Item.ID` to a new unused value (e.g., `11`), `Name = "Wood"`. Replace its `Image` sprite with a log/wood icon.
3. Add `Wood.prefab` to `ItemDictionary.Item Prefabs`.
4. On `OakTree.asset`, drag `Wood.prefab` into the `Wood Item` field.

- [ ] **Step 5: Verify TakeDamage manually**

Play mode:
1. With a Tree_Oak in scene, open its Tree component in the Inspector.
2. Play, then right-click the `TakeDamage` method header in the Inspector — no, that doesn't work for methods. Instead, we'll verify via the dispatcher in Task 7.
3. Temporary verification: add this to your `Start()` in Tree.cs and remove after testing: `// test: Invoke("TakeDamage_Test", 2f);` — skip for now, we'll test end-to-end in Task 7.

**For this task, just verify the code compiles** — save in Unity, check Console for errors.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs" "Assets/Prefabs/Trees/Tree_Oak.prefab" "Assets/Prefabs/Inventory/Wood.prefab" "Assets/Prefabs/Inventory/Wood.prefab.meta" "Assets/Data/Trees/OakTree.asset" "ProjectSettings/TagManager.asset"
git commit -m "feat: Tree layer, TakeDamage, Fell with wood drop, stump regrowth"
```

---

### Task 7: Create `PlayerToolDispatcher.cs` with Axe branch

**Goal:** Single LMB handler on the player that reads the active hotbar item, finds a tree at mouse position, and calls `TakeDamage`.

**Files:**
- Create: `Assets/Scripts/Player/PlayerToolDispatcher.cs`
- Modify: Player GameObject in scene (add component)

- [ ] **Step 1: Create the script**

In `Assets/Scripts/Player/`, create `PlayerToolDispatcher.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerToolDispatcher : MonoBehaviour
{
    [Header("Refs")]
    public HotbarController hotbar;
    public Camera cam;

    [Header("Layers")]
    public LayerMask treeLayer;

    public void OnPrimaryAction(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (hotbar == null || cam == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        world.z = 0f;

        Item active = hotbar.GetActiveItem();

        if (active is Axe axe)
        {
            Tree tree = FindTreeAt(world);
            if (tree != null) tree.TakeDamage(axe.damage);
        }
        // Seed + fruit-pick branches added in later tasks.
    }

    private Tree FindTreeAt(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, treeLayer);
        return hit != null ? hit.GetComponent<Tree>() : null;
    }
}
```

- [ ] **Step 2: Hook up the input action**

You already have Unity Input System wired (fishing uses it). Find your `PlayerInput` component on the Player GameObject, look at its Actions asset. You need an action called `PrimaryAction` (or similar) bound to LMB.

**Check first:** does your fishing system already have an LMB action? Look at `FishingController.cs` — the method signature that takes `InputAction.CallbackContext` for LMB. If it's called something specific (e.g., `OnFire`, `OnPrimary`, `OnCast`), reuse that same action name in your new dispatcher method, OR add a new action `PrimaryAction` to the Actions asset bound to LMB.

Report what you find in fishing; I'll tell you which to do based on how the existing system handles LMB.

- [ ] **Step 3: Add `PlayerToolDispatcher` to the Player**

In the scene:
1. Select the Player GameObject.
2. `Add Component → Player Tool Dispatcher`.
3. In the Inspector: drag `HotbarController` GameObject into `Hotbar`, drag `Main Camera` into `Cam`.
4. For `Tree Layer`: click the dropdown and tick only `Tree`.
5. In the `PlayerInput` component, find the event for LMB action → add a handler pointing to `PlayerToolDispatcher.OnPrimaryAction`.

(Same pattern you use for existing fishing input.)

- [ ] **Step 4: Verify — chop a tree**

1. Play.
2. Put an axe in your hotbar (or grant via dev shortcut) and select it.
3. Left-click on your Tree_Oak 4 times (`chopCount = 4`).
4. On the 4th click: tree should disappear (switched to Stump state — sapling sprite shows), wood prefabs should drop nearby and bounce.
5. Wait `regrowHours` in-game. Stump should turn back into mature tree.

If clicks don't register:
- Check the Collider2D on Tree_Oak is a Trigger and covers the trunk area.
- Check the Tree GameObject's Layer is `Tree`.
- Check Dispatcher's Tree Layer mask has `Tree` ticked.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Player/PlayerToolDispatcher.cs"
git commit -m "feat: PlayerToolDispatcher with Axe chop branch"
```

---

## Phase 4 — Fruit picking

### Task 8: Add fruit overlay rendering + `PickFruit` + dispatcher branch

**Goal:** Mature trees ripen into Ripe state, player can LMB without axe to pick fruit.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`
- Modify: `Assets/Scripts/Player/PlayerToolDispatcher.cs`

- [ ] **Step 1: Add `PickFruit` to `Tree.cs`**

```csharp
public void PickFruit()
{
    if (state != TreeState.Ripe) return;
    DropFruits();
    Enter(TreeState.Mature);
    // ripenHours timer resets automatically because Enter() stamps stateEnteredAtHour.
}
```

- [ ] **Step 2: Add fruit-pick branch to dispatcher**

Add the `else` branch to `OnPrimaryAction`:

```csharp
if (active is Axe axe)
{
    Tree tree = FindTreeAt(world);
    if (tree != null) tree.TakeDamage(axe.damage);
}
else
{
    Tree tree = FindTreeAt(world);
    if (tree != null && tree.state == TreeState.Ripe) tree.PickFruit();
}
```

- [ ] **Step 3: Configure OakTree to have fruit**

For testing only, give Oak a fruit:
1. On `OakTree.asset`: assign `Fruit Item` = an existing fruit item prefab (e.g., fish prefab placeholder), `Fruit Overlay` = any small sprite, `Ripen Hours` = `2` (short for testing).
2. Play, start with tree in Mature state, wait ~2 in-game hours → fruit overlay appears.
3. Deselect axe (or select another item), LMB the tree — fruit drops, overlay vanishes, tree returns to Mature.

After verifying, reset `Ripen Hours` back to `48` (or keep short if you plan to test a lot).

- [ ] **Step 4: Verify Ripe + fell behavior**

Still with oak's ripen hours low: wait for Ripe, select axe, chop through. Final chop should drop **both** wood and fruit (look for both prefab types bouncing near the stump).

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs" "Assets/Scripts/Player/PlayerToolDispatcher.cs"
git commit -m "feat: fruit ripen cycle + LMB pick + fell-drops-fruit"
```

---

## Phase 5 — Planting (baitTrees)

### Task 9: Create Plantable layer + Plantable_Natural tilemap

**Goal:** A tilemap layer the player can paint to designate plantable ground.

**Files:**
- Modify: scene + ProjectSettings

- [ ] **Step 1: Add the Plantable layer**

`Edit → Project Settings → Tags and Layers`. Add layer `Plantable` on next free slot.

- [ ] **Step 2: Create the tilemap**

In scene: `GameObject → 2D Object → Tilemap → Rectangular`. Name the new Tilemap `Plantable_Natural`. Set its Layer (top-right) to `Plantable`.

- [ ] **Step 3: Add a TilemapCollider2D**

On `Plantable_Natural`: `Add Component → Tilemap Collider 2D`. Make sure it's NOT a composite-only setup — we need actual per-tile shapes.

**Important (per 2026-04-14 session memory):** any tile you paint on this tilemap must have a physics shape defined in its Sprite Editor. If `Physics2D.OverlapPoint` returns null when testing, the tile's collider type is likely `None`. Check the Tile asset's `Collider Type` is set to `Sprite` or `Grid`.

- [ ] **Step 4: Paint a small plantable area**

Create or select a tile palette with a grass/dirt tile. Paint a small patch (e.g., 5×5 tiles) next to your player spawn. Save.

- [ ] **Step 5: Verify with a quick OverlapPoint check**

Temporary in-script test:
1. In Play mode, click-log mouse world position (or use Scene view to note a coord inside the painted area).
2. No formal verification yet — we'll test via planting in Task 12.

- [ ] **Step 6: Commit**

```bash
git add "ProjectSettings/TagManager.asset" "Assets/Scenes/SampleScene.unity"
git commit -m "chore: add Plantable layer + Plantable_Natural tilemap"
```

---

### Task 10: Create `PlantableSeed.cs` and `Planter.cs`

**Goal:** The seed item class and the player-side planter service.

**Files:**
- Create: `Assets/Scripts/ItemFunction/PlantableSeed.cs`
- Create: `Assets/Scripts/Player/Planter.cs`

- [ ] **Step 1: Create PlantableSeed**

In `Assets/Scripts/ItemFunction/`:

```csharp
using UnityEngine;

public class PlantableSeed : Item
{
    [Header("Plant")]
    public TreeData treeData;

    public override void UseItem()
    {
        // No-op — PlayerToolDispatcher handles LMB planting.
    }
}
```

- [ ] **Step 2: Create Planter**

In `Assets/Scripts/Player/`:

```csharp
using UnityEngine;

public class Planter : MonoBehaviour
{
    [Header("Config")]
    public LayerMask plantableLayer;
    public LayerMask treeLayer;
    public float maxPlantDistance = 2f;
    public float treeOverlapRadius = 0.5f;

    public bool TryPlant(PlantableSeed seed, Vector3 worldPos)
    {
        if (seed == null || seed.treeData == null || seed.treeData.treePrefab == null) return false;

        if (Vector3.Distance(transform.position, worldPos) > maxPlantDistance) return false;

        if (Physics2D.OverlapPoint(worldPos, plantableLayer) == null) return false;

        if (Physics2D.OverlapCircle(worldPos, treeOverlapRadius, treeLayer) != null) return false;

        GameObject go = Instantiate(seed.treeData.treePrefab, worldPos, Quaternion.identity);
        Tree tree = go.GetComponent<Tree>();
        if (tree == null)
        {
            Destroy(go);
            return false;
        }
        tree.InitializeAsPlanted(seed.treeData, DayCycleManager.Instance != null ? DayCycleManager.Instance.CurrentHour : 0f);
        return true;
    }
}
```

- [ ] **Step 3: Add `InitializeAsPlanted` to Tree.cs**

```csharp
public void InitializeAsPlanted(TreeData data, float currentHour)
{
    treeData = data;
    wasPlanted = true;
    state = TreeState.Seedling;
    stateEnteredAtHour = currentHour;
    hpRemaining = data.chopCount;
    if (string.IsNullOrEmpty(treeID))
    {
        treeID = GlobalHelper.GenerateUniqueId(gameObject);
    }
    UpdateSprite();
}
```

- [ ] **Step 4: Attach Planter to the Player**

Select Player in scene → `Add Component → Planter`. Configure:
- `Plantable Layer` = tick `Plantable`
- `Tree Layer` = tick `Tree`
- `Max Plant Distance` = `2`
- `Tree Overlap Radius` = `0.5`

- [ ] **Step 5: Verify compile**

Save, return to Unity, check Console is clean.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/ItemFunction/PlantableSeed.cs" "Assets/Scripts/Player/Planter.cs" "Assets/Scripts/Trees/Tree.cs"
git commit -m "feat: PlantableSeed item, Planter service, Tree.InitializeAsPlanted"
```

---

### Task 11: Wire seed branch in dispatcher + inventory decrement

**Goal:** Selecting a seed and LMB on a plantable tile spawns a baitTree and consumes one seed.

**Files:**
- Modify: `Assets/Scripts/Player/PlayerToolDispatcher.cs`

- [ ] **Step 1: Inspect how inventory decrement works**

Open `Assets/Scripts/ItemFunction/Slot.cs` and `Inventory.cs`. Look for a method like `RemoveItem`, `DecrementSlot`, or similar on the hotbar/inventory side. We need a way to remove one item from the active hotbar slot.

**Report back what you find** — if no clean API exists, we'll add a small `HotbarController.ConsumeActive()` helper as a preliminary step.

- [ ] **Step 2: Add seed branch to dispatcher**

Assuming a helper named `hotbar.ConsumeActive()` that removes one from the active slot:

```csharp
[Header("Refs")]
public HotbarController hotbar;
public Camera cam;
public Planter planter;   // new field

[Header("Layers")]
public LayerMask treeLayer;

public void OnPrimaryAction(InputAction.CallbackContext ctx)
{
    if (!ctx.performed) return;
    if (hotbar == null || cam == null) return;

    Vector2 screenPos = Mouse.current.position.ReadValue();
    Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
    world.z = 0f;

    Item active = hotbar.GetActiveItem();

    if (active is Axe axe)
    {
        Tree tree = FindTreeAt(world);
        if (tree != null) tree.TakeDamage(axe.damage);
    }
    else if (active is PlantableSeed seed && planter != null)
    {
        if (planter.TryPlant(seed, world))
        {
            hotbar.ConsumeActive();
        }
    }
    else
    {
        Tree tree = FindTreeAt(world);
        if (tree != null && tree.state == TreeState.Ripe) tree.PickFruit();
    }
}
```

- [ ] **Step 3: Assign Planter reference**

On the Player's `PlayerToolDispatcher` component, drag the Player's `Planter` component into the `Planter` field.

- [ ] **Step 4: Don't verify yet**

We need a baitTree + seed to test this. Task 12 creates them.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Player/PlayerToolDispatcher.cs"
git commit -m "feat: dispatcher seed branch with inventory decrement"
```

---

### Task 12: Create BaitTree TreeData, prefab, and seed

**Goal:** A plantable, harvestable baitTree + the seed that grows it.

**Files:**
- Create: `Assets/Data/Trees/BaitTree.asset`
- Create: `Assets/Prefabs/Trees/Tree_Bait.prefab`
- Create: `Assets/Prefabs/Inventory/Seed_Bait.prefab`
- Create: BaitTree sprites + fruit (Bait) item if not existing

- [ ] **Step 1: BaitTree asset**

`Assets/Data/Trees/` → right-click → `Create → Trees → Tree Data` → name `BaitTree`. Fill:
- `treeDataID` = `2`
- `displayName` = `Bait Tree`
- `chopCount` = `1`
- `woodDropMin` = `0`, `woodDropMax` = `1`
- `woodItem` = `Wood.prefab` (or leave null)
- `fruitItem` = existing Bait prefab (check `Assets/Prefabs/Inventory/` — you said Bait already exists; if not, quickly make one)
- `ripenHours` = `12`
- `fruitPerHarvest` = `2`
- `regrows` = unchecked
- `regrowHours` = `0`
- `isPlantable` = checked
- `growHours` = `24`
- `saplingSprite`, `matureSprite`, `fruitOverlay` = your bait-tree sprites

- [ ] **Step 2: BaitTree prefab**

Duplicate `Tree_Oak.prefab` → rename `Tree_Bait.prefab`. Open it:
- Tree component's `Tree Data` = `BaitTree.asset`
- Main Renderer sprite = bait-tree mature sprite (will be overridden at runtime anyway)
- Collider2D size = smaller (baitTree is smaller)

On `BaitTree.asset`, set `Tree Prefab` = `Tree_Bait.prefab`.

- [ ] **Step 3: Seed prefab**

Duplicate `Axe_Basic.prefab` → rename `Seed_Bait.prefab`. Open it:
- Remove `Axe` component
- `Add Component → Plantable Seed`
- On `PlantableSeed`: `ID` = `12`, `Name` = `Bait Seed`, `Tree Data` = `BaitTree.asset`
- Replace icon sprite

Register `Seed_Bait.prefab` in `ItemDictionary`.

- [ ] **Step 4: Verify plant → grow → harvest**

1. Play. Put a Seed_Bait into your hotbar.
2. Select it. LMB on a Plantable tile within `maxPlantDistance`.
3. A Tree_Bait should spawn in Seedling state. Seed count should drop by 1.
4. Wait `growHours` (24 in-game) — set to `1` for faster test if needed. Seedling → Mature.
5. Wait `ripenHours` (12) — Mature → Ripe.
6. Deselect seed / select any non-axe item. LMB on the baitTree → bait drops.
7. Select axe, LMB once (`chopCount = 1`) → baitTree destroyed permanently. No stump.

- [ ] **Step 5: Verify planting rejection**

Try to plant:
- Too far from player → no spawn, seed count unchanged.
- On non-Plantable ground → no spawn, seed count unchanged.
- Right on top of an existing tree → no spawn, seed count unchanged.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Data/Trees/BaitTree.asset" "Assets/Prefabs/Trees/Tree_Bait.prefab" "Assets/Prefabs/Inventory/Seed_Bait.prefab"
git commit -m "feat: BaitTree data + prefab + Seed_Bait; full planting loop works"
```

---

## Phase 6 — Save / load

### Task 13: Extend SavedData with TreeSaveData

**Goal:** Serialize tree state across save/load cycles.

**Files:**
- Modify: `Assets/Scripts/Map&Save/SavedData.cs`

- [ ] **Step 1: Append TreeSaveData class + list**

Open `SavedData.cs`. Add below the existing `ChestSaveData` class:

```csharp
[System.Serializable]
public class TreeSaveData
{
    public string treeID;
    public int treeDataID;
    public bool wasPlanted;
    public Vector3 position;
    public int state;
    public float stateEnteredAtHour;
    public int hpRemaining;
    public bool permanentlyGone;
}
```

And inside `SavedData`:

```csharp
public List<TreeSaveData> treeSaveData;
```

- [ ] **Step 2: Verify compile**

Save, confirm Console is clean.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Map&Save/SavedData.cs"
git commit -m "feat: SavedData includes TreeSaveData schema"
```

---

### Task 14: Create TreeDataDictionary

**Goal:** Look up a `TreeData` by `treeDataID` on load (needed to re-spawn planted trees).

**Files:**
- Create: `Assets/Scripts/Trees/TreeDataDictionary.cs`

- [ ] **Step 1: Create script**

```csharp
using System.Collections.Generic;
using UnityEngine;

public class TreeDataDictionary : MonoBehaviour
{
    public List<TreeData> treeDataAssets;
    private Dictionary<int, TreeData> map;

    private void Awake()
    {
        map = new Dictionary<int, TreeData>();
        foreach (TreeData data in treeDataAssets)
        {
            if (data == null) continue;
            if (data.treeDataID <= 0)
            {
                Debug.LogWarning($"TreeData '{data.displayName}' has invalid ID {data.treeDataID}.");
                continue;
            }
            if (map.ContainsKey(data.treeDataID))
            {
                Debug.LogWarning($"Duplicate TreeData ID {data.treeDataID}.");
                continue;
            }
            map[data.treeDataID] = data;
        }
    }

    public TreeData Get(int id)
    {
        map.TryGetValue(id, out TreeData data);
        if (data == null) Debug.LogWarning($"TreeData ID {id} not found.");
        return data;
    }
}
```

- [ ] **Step 2: Add the dictionary GameObject to the scene**

Create empty GameObject `TreeDataDictionary` in the scene. Add `TreeDataDictionary` component. In the Inspector, populate `Tree Data Assets` with `OakTree.asset` and `BaitTree.asset`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Trees/TreeDataDictionary.cs" "Assets/Scenes/SampleScene.unity"
git commit -m "feat: TreeDataDictionary for runtime lookup"
```

---

### Task 15: Add `GetSaveData` and `LoadFromSave` to Tree

**Goal:** Each tree can serialize its full state and restore from it.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`

- [ ] **Step 1: Add save/load methods**

```csharp
public TreeSaveData GetSaveData()
{
    return new TreeSaveData
    {
        treeID = treeID,
        treeDataID = treeData != null ? treeData.treeDataID : 0,
        wasPlanted = wasPlanted,
        position = transform.position,
        state = (int)state,
        stateEnteredAtHour = stateEnteredAtHour,
        hpRemaining = hpRemaining,
        permanentlyGone = permanentlyGone,
    };
}

public void LoadFromSave(TreeSaveData data, TreeDataDictionary dict)
{
    treeID = data.treeID;
    wasPlanted = data.wasPlanted;
    transform.position = data.position;
    state = (TreeState)data.state;
    stateEnteredAtHour = data.stateEnteredAtHour;
    hpRemaining = data.hpRemaining;
    permanentlyGone = data.permanentlyGone;
    if (data.treeDataID > 0 && dict != null)
    {
        treeData = dict.Get(data.treeDataID);
    }
    UpdateSprite();
}
```

- [ ] **Step 2: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs"
git commit -m "feat: Tree GetSaveData / LoadFromSave"
```

---

### Task 16: Integrate tree save/load in SaveController

**Goal:** SaveController captures and restores all trees. Two-pass load: scene trees patched, planted trees re-instantiated.

**Files:**
- Modify: `Assets/Scripts/Map&Save/SaveController.cs`

- [ ] **Step 1: Add references in SaveController**

Add near the existing fields:

```csharp
private Tree[] trees;
private TreeDataDictionary treeDataDictionary;
```

In `InitializeComponents()`:

```csharp
trees = FindObjectsByType<Tree>(FindObjectsSortMode.None);
treeDataDictionary = FindAnyObjectByType<TreeDataDictionary>();
```

- [ ] **Step 2: Populate tree save data in SaveGame**

Inside `SavedData saveData = new SavedData { ... }`, add:

```csharp
treeSaveData = GetTreeState(),
```

And add the method:

```csharp
private List<TreeSaveData> GetTreeState()
{
    List<TreeSaveData> list = new List<TreeSaveData>();
    Tree[] current = FindObjectsByType<Tree>(FindObjectsSortMode.None);
    foreach (Tree tree in current)
    {
        list.Add(tree.GetSaveData());
    }
    return list;
}
```

(We requery inside SaveGame rather than using the cached `trees` field because trees may have been destroyed or spawned since `Start`.)

- [ ] **Step 3: Load trees in LoadGame**

After `LoadChestState(saveData.chestSaveData);`, add:

```csharp
LoadTreeState(saveData.treeSaveData);
```

And the method:

```csharp
private void LoadTreeState(List<TreeSaveData> saved)
{
    if (saved == null) return;

    // Pass 1 — patch scene-placed trees
    Tree[] sceneTrees = FindObjectsByType<Tree>(FindObjectsSortMode.None);
    foreach (Tree tree in sceneTrees)
    {
        TreeSaveData entry = saved.FirstOrDefault(s => s.treeID == tree.treeID);
        if (entry == null) continue;

        if (entry.permanentlyGone)
        {
            Destroy(tree.gameObject);
            continue;
        }

        tree.LoadFromSave(entry, treeDataDictionary);
    }

    // Pass 2 — re-instantiate planted trees
    foreach (TreeSaveData entry in saved.Where(s => s.wasPlanted))
    {
        TreeData data = treeDataDictionary != null ? treeDataDictionary.Get(entry.treeDataID) : null;
        if (data == null || data.treePrefab == null) continue;

        GameObject go = Instantiate(data.treePrefab, entry.position, Quaternion.identity);
        Tree tree = go.GetComponent<Tree>();
        if (tree != null) tree.LoadFromSave(entry, treeDataDictionary);
    }
}
```

Make sure `using System.Linq;` is at the top of `SaveController.cs` (it already is — you have `.FirstOrDefault` elsewhere).

- [ ] **Step 4: Verify save/load scenarios**

1. **Mid-chop save:** Chop a Tree_Oak twice (2/4 HP remaining). Save. Quit to main menu, reload. Tree should still have 2 HP remaining — two more chops fell it.
2. **Stump regrowth save:** Fell an Oak, wait partway through `regrowHours`, save + reload. It should continue regrowing and mature at the correct total time.
3. **Planted tree save:** Plant a baitTree, save + reload. BaitTree should re-spawn at same position in same state.
4. **Permanent gone:** (If you have a non-regrowing scene tree — e.g., temporarily set OakTree.asset `regrows` to false) Chop, save, reload. Scene tree should be destroyed on load.
5. **Planted tree felled + saved:** Plant bait, chop it, save, reload. No baitTree should appear.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Map&Save/SaveController.cs"
git commit -m "feat: SaveController integrates tree save/load with two-pass restore"
```

---

## Self-review checklist (completed before handoff)

**Spec coverage:**

| Spec section | Implemented in |
|---|---|
| `TreeData` ScriptableObject | Task 1 |
| `Tree.cs` state machine | Tasks 2, 4, 6, 8, 10, 15 |
| `Axe.cs` | Task 5 |
| `PlantableSeed.cs` + `Planter.cs` | Task 10 |
| `PlayerToolDispatcher.cs` | Tasks 7, 8, 11 |
| Rendering (main + fruit overlay) | Tasks 2, 3, 8 |
| `Plantable` layer + tilemap | Task 9 |
| `TreeDataDictionary` | Task 14 |
| `SavedData.TreeSaveData` | Task 13 |
| `SaveController` integration | Task 16 |
| Chop + wood drop + stump + regrow | Tasks 6, 7 |
| Fruit ripen + pick | Task 8 |
| Planted-tree permanence rule | Task 6 (`Fell` wasPlanted branch) |
| Example species (Oak + BaitTree) | Tasks 1, 12 |
| Manual verification matrix | Steps 4 in Tasks 7, 8, 12, 16 |
| Future extensions (watering, tilling, axe tiers) | Out of scope for plan — documented in spec |

No gaps identified.

**Placeholder scan:** No "TBD", "implement later", etc.

**Type consistency:** `TreeState` enum, `Tree.state` field, `Tree.TakeDamage(int)`, `Tree.PickFruit()`, `Tree.InitializeAsPlanted(TreeData, float)`, `Tree.LoadFromSave(TreeSaveData, TreeDataDictionary)`, `TreeData.treeDataID` — used consistently across all tasks.

**Known open questions for the implementer (flagged inline in tasks):**

- Task 4 Step 2: check `DayCycleManager` for a day counter / total-hours accessor — if present, simplify `HoursSince`.
- Task 7 Step 2: check existing LMB input action name in `FishingController` before naming the new dispatcher method.
- Task 11 Step 1: check `HotbarController`/`Inventory` for an existing "remove one from active slot" API before assuming `hotbar.ConsumeActive()` exists.

These are real unknowns that depend on the current codebase, not placeholders — they're explicit prompts to pause and report findings.
