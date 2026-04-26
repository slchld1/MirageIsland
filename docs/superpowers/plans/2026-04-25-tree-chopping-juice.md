# Tree Chopping Juice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add hit shake, directional wood drops, fall animation, and first-hit fruit drop to the existing Tree System.

**Architecture:** Restructure `Tree_Oak.prefab` into `Top + Stump + FruitOverlay` children so the canopy can shake/topple while the stump base stays rooted. Tree.cs continues to own state/damage/save and now also computes the fall direction. A new `TreeAnimator.cs` sibling component owns all visual choreography (shake coroutine + fall sequence with rotate / impact callback / fade). The existing `mainRenderer` field is split into `topRenderer + stumpRenderer`. `TreeData` swaps `matureSprite` for `topSprite + stumpSprite`.

**Tech Stack:** Unity 2D, C#, Aseprite (or any pixel art editor) for sprite splitting, Unity Sprite Editor for pivot configuration. No automated tests — verification is manual in the Editor.

**Reference spec:** `docs/superpowers/specs/2026-04-25-tree-chopping-juice-design.md`

**Execution style note:** The user writes all code and does all Unity setup. The engineer's job (or Claude's, acting as guide) is to explain each step before typing, wait for the user to complete it, help debug, and move on only after in-Editor verification passes. Each task ends with a verification step and a commit.

**Builds on:** Tree System v1 (`docs/superpowers/plans/2026-04-24-tree-system.md`, Tasks 1–7 complete). Assumes `Tree_Oak.prefab`, `OakTree.asset`, `Tree.cs`, `TreeData.cs`, `Axe.cs`, `PlayerToolDispatcher.cs`, the `Tree` physics layer, and the wood drop prefab all exist and are wired up.

---

## Phase 1 — Art + Data foundation

### Task 1: Split mature sprite into Top + Stump

**Goal:** Produce two new sprite assets that visually align to form the existing mature tree, with correct pivots for the upcoming prefab restructure.

**Files:**
- Create: `Assets/Sprites/Trees/OakTree_Top.png`
- Create: `Assets/Sprites/Trees/OakTree_Stump.png`

- [ ] **Step 1: Open the existing mature sprite in Aseprite (or your pixel editor)**

Open `Assets/Sprites/Trees/OakTree_Mature.png` (96×96).

- [ ] **Step 2: Split horizontally**

Decide on a split line. A reasonable starting split for a 96×96 oak is **bottom 24px = stump, top 72px = upper tree**. Adjust to taste — the rule is "everything that should remain visible after the tree is felled goes in Stump; everything that falls + fades goes in Top."

In Aseprite:
1. **Stump:** Crop or canvas-resize a copy to keep only the bottom 24×96 of the canvas (or 24-tall slice centered on the trunk if your trunk isn't 96px wide). Save as `OakTree_Stump.png`.
2. **Top:** Copy a fresh duplicate of the mature sprite, erase or remove the bottom 24 rows that are now in `OakTree_Stump.png`. Save as `OakTree_Top.png` at the same canvas size as the original (96×96), with the bottom rows transparent. Keeping the same canvas size simplifies pivot math — pivot Y in normalized coords stays meaningful.

The two sprites stacked at the same world position should reproduce the original mature tree. Test by opening both in Aseprite layered on top of each other before moving on.

- [ ] **Step 3: Drop both files into Unity**

Move/copy `OakTree_Top.png` and `OakTree_Stump.png` into `Assets/Sprites/Trees/`. Unity will import them.

- [ ] **Step 4: Configure import settings on both sprites**

For each of `OakTree_Top.png` and `OakTree_Stump.png`, select the asset in the Project window and in the Inspector set:
- `Texture Type` = `Sprite (2D and UI)`
- `Sprite Mode` = `Single`
- `Pixels Per Unit` = `16`
- `Filter Mode` = `Point (no filter)`
- `Compression` = `None`

Click `Apply` after changing each.

- [ ] **Step 5: Set the pivots in the Sprite Editor**

For each sprite, click `Sprite Editor` in the Inspector, then in the editor window:
- `OakTree_Stump.png`: set `Pivot` to `Custom`, then drag the pivot crosshair to the **bottom-center of the sprite** (or set Pivot to `Bottom`). This is the rotation/anchor base of the stump.
- `OakTree_Top.png`: set `Pivot` to `Custom`, then drag the pivot crosshair to the **bottom-center of the visible (non-transparent) art** — i.e., where the upper trunk meets the (now-transparent) stump area. If you kept the 96×96 canvas with bottom 24 transparent, this means a custom pivot at roughly `(0.5, 24/96 ≈ 0.25)` in normalized coords, OR `(48, 24)` in pixel coords.

This pivot is what the fall animation will rotate around. Wrong pivot = bad-looking topple.

Click `Apply` in the Sprite Editor after each.

- [ ] **Step 6: Verify in Editor**

Drop each sprite individually into an empty scene. Confirm:
- Stump's transform position lands at the bottom of its art.
- Top's transform position lands at the bottom of the upper-trunk visible area (so when stacked at the same Y, top appears to sit on the stump cleanly).

Delete the test instances afterward.

- [ ] **Step 7: Commit**

```bash
git add "Assets/Sprites/Trees/OakTree_Top.png" "Assets/Sprites/Trees/OakTree_Top.png.meta" "Assets/Sprites/Trees/OakTree_Stump.png" "Assets/Sprites/Trees/OakTree_Stump.png.meta"
git commit -m "art: split OakTree_Mature into Top + Stump sprites with bottom pivots"
```

---

### Task 2: Update `TreeData` schema

**Goal:** Replace `matureSprite` with `topSprite` + `stumpSprite` on the `TreeData` ScriptableObject so the new state→sprite mapping has somewhere to point.

**Files:**
- Modify: `Assets/Scripts/Trees/TreeData.cs`

- [ ] **Step 1: Edit `TreeData.cs`**

Open `Assets/Scripts/Trees/TreeData.cs`. Find the `[Header("Sprites")]` block and replace it. The `Sprites` block should look like this after the edit:

```csharp
[Header("Sprites")]
public Sprite saplingSprite;
public Sprite topSprite;
public Sprite stumpSprite;
public Sprite fruitOverlay;
```

Delete the old `public Sprite matureSprite;` line.

- [ ] **Step 2: Verify compile**

Save. Return to Unity. Wait for recompile. The Console will show one expected error from `Tree.cs` referencing `treeData.matureSprite` — that's normal; we fix it in Task 5. Other errors should not appear.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Trees/TreeData.cs"
git commit -m "feat: replace TreeData.matureSprite with topSprite + stumpSprite"
```

---

### Task 3: Reassign sprites on `OakTree.asset`

**Goal:** Wire the new sprite fields. After this task the data is correct even though the prefab and code still need restructuring.

**Files:**
- Modify: `Assets/Data/Trees/OakTree.asset`

- [ ] **Step 1: Assign new sprite fields**

In Unity, select `Assets/Data/Trees/OakTree.asset`. In the Inspector:
- Drag `OakTree_Top.png` into the `Top Sprite` field.
- Drag `OakTree_Stump.png` into the `Stump Sprite` field.
- `Sapling Sprite` keeps its existing `OakTree_Sapling.png` assignment.
- `Fruit Overlay` stays whatever it was (likely empty for Oak).

The previously-assigned `Mature Sprite` reference is gone — Unity already dropped it when we removed the field in Task 2.

- [ ] **Step 2: Verify**

Confirm the OakTree asset Inspector shows the four sprite fields populated as expected.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Data/Trees/OakTree.asset"
git commit -m "data: reassign OakTree sprites to topSprite + stumpSprite"
```

---

## Phase 2 — Prefab restructure

### Task 4: Restructure `Tree_Oak.prefab`

**Goal:** Replace the single SpriteRenderer setup with `Top + Stump` children, parent FruitOverlay under Top, and add a `SortingGroup` so the tree sorts as a unit relative to other world objects.

**Files:**
- Modify: `Assets/Prefabs/Trees/Tree_Oak.prefab`

- [ ] **Step 1: Open the prefab**

In Project window, double-click `Tree_Oak.prefab` to enter prefab edit mode.

- [ ] **Step 2: Note the existing structure**

Currently:
```
Tree_Oak (root)         ← SpriteRenderer (matureSprite), Tree component, Box Collider 2D, Layer = Tree
└── FruitOverlay        ← SpriteRenderer (fruit overlay), disabled
```

We're moving to:
```
Tree_Oak (root)         ← Tree component, Box Collider 2D, SortingGroup, Layer = Tree (NO SpriteRenderer)
├── Top                 ← SpriteRenderer (topSprite)
│   └── FruitOverlay    ← SpriteRenderer (fruitOverlay), disabled
└── Stump               ← SpriteRenderer (stumpSprite)
```

- [ ] **Step 3: Remove the SpriteRenderer from the root**

On `Tree_Oak` root, right-click the `Sprite Renderer` component header → `Remove Component`. The root now has Tree (script) and Box Collider 2D only.

- [ ] **Step 4: Create the `Top` child**

Right-click `Tree_Oak` in the Hierarchy → `Create Empty` → name it `Top`. Set `Top`'s Transform position to `(0, 0, 0)`.

On `Top`: `Add Component → Sprite Renderer`. Drag `OakTree_Top.png` into the `Sprite` field. Set `Order in Layer = 1`.

- [ ] **Step 5: Re-parent `FruitOverlay` under `Top`**

In Hierarchy, drag `FruitOverlay` (currently a child of `Tree_Oak`) onto `Top` so it becomes a child of `Top`. Set its Transform localPosition to `(0, 0, 0)` and its Sprite Renderer's `Order in Layer = 2` (so fruit renders in front of the top sprite).

Leave its Sprite Renderer disabled (the `enabled` checkbox at the top-left of the component) — the alive-tree logic will turn it on when ripe.

- [ ] **Step 6: Create the `Stump` child**

Right-click `Tree_Oak` root → `Create Empty` → name it `Stump`. Set Transform position to `(0, 0, 0)`.

On `Stump`: `Add Component → Sprite Renderer`. Drag `OakTree_Stump.png` into the `Sprite` field. Set `Order in Layer = 0` (so it renders behind Top).

- [ ] **Step 7: Add `Sorting Group` to root**

On `Tree_Oak` root: `Add Component → Rendering → Sorting Group`. Leave defaults. This makes all child SpriteRenderers sort together as one unit when compared against other world sprites — the player's Y-sort relative to the tree just sees one tree, not three separate sprites.

- [ ] **Step 8: Verify visual stack**

In the prefab edit view, the tree should look identical to the previous mature tree — Top sitting cleanly on Stump, no gap, no overlap. Adjust Top's `localPosition.y` if needed (only if the pivots aren't precisely aligned).

- [ ] **Step 9: Save the prefab**

`Ctrl+S` (or `Cmd+S` on Mac). Exit prefab edit mode.

- [ ] **Step 10: Verify in scene**

Drop a `Tree_Oak.prefab` instance into your scene (or use the existing one). Press Play briefly to confirm it renders as a normal mature tree. Console should show one expected error — Tree.cs still references `mainRenderer` and `matureSprite`. Don't fix yet; that's Task 5.

- [ ] **Step 11: Commit**

```bash
git add "Assets/Prefabs/Trees/Tree_Oak.prefab"
git commit -m "feat: restructure Tree_Oak prefab into Top + Stump + FruitOverlay children with SortingGroup"
```

---

## Phase 3 — Tree.cs sprite renderers

### Task 5: Update `Tree.cs` renderer fields and `UpdateSprite`

**Goal:** Replace the single `mainRenderer` field with `topRenderer + stumpRenderer` and rewrite `UpdateSprite` against the new state→renderer mapping. Wire the new fields on the prefab.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`
- Modify: `Assets/Prefabs/Trees/Tree_Oak.prefab`

- [ ] **Step 1: Update the Renderer fields in `Tree.cs`**

Open `Assets/Scripts/Trees/Tree.cs`. Find the `[Header("Renderers (assign in prefab)")]` block. Replace it with:

```csharp
[Header("Renderers (assign in prefab)")]
public SpriteRenderer topRenderer;
public SpriteRenderer stumpRenderer;
public SpriteRenderer fruitRenderer;
```

The old `mainRenderer` field is gone.

- [ ] **Step 2: Rewrite `UpdateSprite`**

Replace the existing `UpdateSprite()` method body with this implementation, which drives both renderers per the spec table:

```csharp
private void UpdateSprite()
{
    if (treeData == null) return;

    switch (state)
    {
        case TreeState.Seedling:
            if (topRenderer != null)
            {
                topRenderer.sprite = treeData.saplingSprite;
                topRenderer.enabled = true;
                Color c1 = topRenderer.color; c1.a = 1f; topRenderer.color = c1;
            }
            if (stumpRenderer != null) stumpRenderer.enabled = false;
            break;

        case TreeState.Mature:
        case TreeState.Ripe:
            if (topRenderer != null)
            {
                topRenderer.sprite = treeData.topSprite;
                topRenderer.enabled = true;
                Color c2 = topRenderer.color; c2.a = 1f; topRenderer.color = c2;
            }
            if (stumpRenderer != null)
            {
                stumpRenderer.sprite = treeData.stumpSprite;
                stumpRenderer.enabled = true;
            }
            break;

        case TreeState.Stump:
            if (topRenderer != null) topRenderer.enabled = false;
            if (stumpRenderer != null)
            {
                stumpRenderer.sprite = treeData.stumpSprite;
                stumpRenderer.enabled = true;
            }
            break;
    }

    if (fruitRenderer != null)
    {
        bool showFruit = state == TreeState.Ripe && treeData.fruitOverlay != null;
        fruitRenderer.sprite = showFruit ? treeData.fruitOverlay : null;
        fruitRenderer.enabled = showFruit;
    }
}
```

The alpha-reset lines (`c1.a = 1f; topRenderer.color = c1;`) are important: when the regrowth cycle takes us from Stump → Mature, the fade-out from a previous fell could have left `topRenderer.color.a = 0`. Resetting to full alpha makes Top visible again.

- [ ] **Step 3: Wire the new renderer fields on the prefab**

Open `Tree_Oak.prefab`. On the root `Tree_Oak` GameObject, look at the Tree component. The `Top Renderer`, `Stump Renderer`, and `Fruit Renderer` slots should appear (replacing the old `Main Renderer` slot). Drag:
- `Top` child's `Sprite Renderer` → `Top Renderer` slot.
- `Stump` child's `Sprite Renderer` → `Stump Renderer` slot.
- `FruitOverlay` child's `Sprite Renderer` → `Fruit Renderer` slot.

Save the prefab.

- [ ] **Step 4: Verify all four states render correctly**

In a scene with a `Tree_Oak` instance, before pressing Play, change the Tree component's `State` field in the Inspector to each of the four values, and press Play to see what renders:
- `Seedling` → only the sapling sprite shown (on Top).
- `Mature` → Top + Stump together = full mature tree.
- `Ripe` → same as Mature (fruitOverlay only kicks in if Oak has a fruit assigned, which it doesn't currently).
- `Stump` → only the stump sprite shown.

After verification, set state back to `Mature`.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs" "Assets/Prefabs/Trees/Tree_Oak.prefab"
git commit -m "feat: split Tree.mainRenderer into topRenderer + stumpRenderer; rewrite UpdateSprite"
```

---

## Phase 4 — TreeAnimator + shake

### Task 6: Create `TreeAnimator.cs` with `PlayShake()`

**Goal:** New sibling component that owns visual juice. First feature: per-chop shake on the Top child.

**Files:**
- Create: `Assets/Scripts/Trees/TreeAnimator.cs`

- [ ] **Step 1: Create the script**

In Unity, right-click `Assets/Scripts/Trees/` → `Create → C# Script` → name it `TreeAnimator`.

- [ ] **Step 2: Replace contents**

```csharp
using System;
using System.Collections;
using UnityEngine;

public class TreeAnimator : MonoBehaviour
{
    [Header("Refs (assign in prefab)")]
    public Transform topTransform;
    public SpriteRenderer topRenderer;
    public SpriteRenderer fruitRenderer;

    [Header("Shake")]
    public float shakeDuration = 0.10f;
    public float shakeAmplitude = 0.05f;

    [Header("Fall")]
    public float fallRotateDuration = 0.50f;
    public float fallImpactFraction = 0.35f;
    public float fallLieDuration = 0.20f;
    public float fallFadeDuration = 0.30f;

    private Coroutine shakeRoutine;
    private Vector3 topBaseLocalPos;

    private void Awake()
    {
        if (topTransform != null) topBaseLocalPos = topTransform.localPosition;
    }

    public void PlayShake()
    {
        if (topTransform == null) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * shakeAmplitude;
            topTransform.localPosition = topBaseLocalPos + new Vector3(jitter.x, jitter.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        topTransform.localPosition = topBaseLocalPos;
        shakeRoutine = null;
    }
}
```

- [ ] **Step 3: Verify compile**

Save, return to Unity. Console should be clean.

- [ ] **Step 4: Add `TreeAnimator` to the prefab**

Open `Tree_Oak.prefab`. On the root: `Add Component → Tree Animator`. Wire its three Inspector slots:
- `Top Transform` → drag the `Top` child's Transform.
- `Top Renderer` → drag the `Top` child's `Sprite Renderer`.
- `Fruit Renderer` → drag the `FruitOverlay` child's `Sprite Renderer`.

Leave the timing values at their defaults.

- [ ] **Step 5: Add an `animator` field on `Tree.cs`**

Open `Tree.cs`. Add this field below the existing `[Header("Renderers (assign in prefab)")]` block:

```csharp
[Header("Animator (assign in prefab)")]
public TreeAnimator animator;
```

Save. Return to Unity. On `Tree_Oak.prefab` root, the Tree component now has an `Animator` slot. Drag the `Tree Animator` component (sitting on the same root GameObject) into that slot.

Save the prefab.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/Trees/TreeAnimator.cs" "Assets/Prefabs/Trees/Tree_Oak.prefab" "Assets/Scripts/Trees/Tree.cs"
git commit -m "feat: TreeAnimator component + PlayShake; wire on Tree_Oak"
```

---

### Task 7: Hook `PlayShake` into `Tree.TakeDamage`

**Goal:** Every chop triggers a Top-only shake.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`

- [ ] **Step 1: Add the shake call to `TakeDamage`**

Open `Tree.cs`. The current `TakeDamage` is:

```csharp
public void TakeDamage(int damage)
{
    if (treeData == null || !treeData.isChoppable) return;
    if (state == TreeState.Seedling || state == TreeState.Stump) return;

    hpRemaining -= damage;
    if (hpRemaining <= 0) Fell();
}
```

Add an `animator.PlayShake()` call right after the early-returns:

```csharp
public void TakeDamage(int damage)
{
    if (treeData == null || !treeData.isChoppable) return;
    if (state == TreeState.Seedling || state == TreeState.Stump) return;

    if (animator != null) animator.PlayShake();

    hpRemaining -= damage;
    if (hpRemaining <= 0) Fell();
}
```

(We'll add fruit-drop logic to this method in Task 10. For now we're isolating the shake step.)

- [ ] **Step 2: Verify in Editor**

Press Play. Select Axe in hotbar. Click on the Tree_Oak. Each click should:
- Shake the Top of the tree briefly (~0.1s).
- The Stump should NOT move during the shake.

Chop 4 times to fell — felling still works as before (no fall animation yet, just immediate stump swap). Wood drops still appear at the trunk position (no offset yet).

If shakes don't appear, check:
- TreeAnimator's `Top Transform` field is wired to the `Top` child Transform.
- Tree's `Animator` field is wired to the TreeAnimator component.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs"
git commit -m "feat: trigger PlayShake on every chop in Tree.TakeDamage"
```

---

## Phase 5 — Fall direction + directional drops

### Task 8: Pass chopper position from `PlayerToolDispatcher` and add `ComputeFallDirection`

**Goal:** Tree.cs can know where the player is at the moment of the chop. We'll pass it explicitly through the public API rather than having Tree go look up the player.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`
- Modify: `Assets/Scripts/Player/PlayerToolDispatcher.cs`

- [ ] **Step 1: Change `Tree.TakeDamage` signature**

In `Tree.cs`, replace the current `TakeDamage` with:

```csharp
public void TakeDamage(int damage, Vector3 chopperWorldPos)
{
    if (treeData == null || !treeData.isChoppable) return;
    if (state == TreeState.Seedling || state == TreeState.Stump) return;

    int fallDir = ComputeFallDirection(chopperWorldPos);

    if (animator != null) animator.PlayShake();

    hpRemaining -= damage;
    if (hpRemaining <= 0) Fell(fallDir);
}
```

Note: `Fell()` now takes an `int fallDir` parameter — we'll update its signature in Task 9.

- [ ] **Step 2: Add `ComputeFallDirection` helper**

Add this method to `Tree.cs` (below `TakeDamage`):

```csharp
[Header("Fall")]
public float fallAlignThreshold = 0.5f;

private int ComputeFallDirection(Vector3 chopperWorldPos)
{
    float dx = chopperWorldPos.x - transform.position.x;
    if (Mathf.Abs(dx) < fallAlignThreshold)
    {
        return UnityEngine.Random.value < 0.5f ? -1 : +1;
    }
    return dx > 0f ? -1 : +1;
}
```

Place the `[Header("Fall")] public float fallAlignThreshold = 0.5f;` line in the Runtime/Config region near the top of the class — anywhere in the field declarations is fine. Keep it `public` so you can tune the threshold from the Inspector.

- [ ] **Step 3: Update `PlayerToolDispatcher` to pass position**

Open `Assets/Scripts/Player/PlayerToolDispatcher.cs`. Find the existing axe branch in `Update()`:

```csharp
if (active is Axe axe)
{
    Tree tree = FindTreeAt(world);
    if (tree != null) tree.TakeDamage(axe.damage);
}
```

Change the `TakeDamage` call to pass the dispatcher's transform position (the dispatcher lives on the Player, so `transform.position` is the player's world position):

```csharp
if (active is Axe axe)
{
    Tree tree = FindTreeAt(world);
    if (tree != null) tree.TakeDamage(axe.damage, transform.position);
}
```

- [ ] **Step 4: Verify compile**

Save. Return to Unity. Console should be clean. (Tree.Fell() will now have a parameter mismatch — we fix it in Task 9. If you see that error, expected — proceed to next task.)

Actually, wait — `Fell()` is currently parameterless. Change it now to accept `fallDir` so the project compiles:

In `Tree.cs`, find:

```csharp
private void Fell()
{
    DropWood();
    ...
}
```

Change the signature to:

```csharp
private void Fell(int fallDir)
{
    DropWood();
    ...
}
```

(We don't yet *use* `fallDir` inside Fell — that's Task 9. We're just making it compile now.)

- [ ] **Step 5: Verify in Editor**

Play. Chop a tree from the east side — felling still works as before, drops still go to the trunk position (we haven't wired `fallDir` into drops yet). The point of this verification is just confirming the new signature path works and clicks still register.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs" "Assets/Scripts/Player/PlayerToolDispatcher.cs"
git commit -m "feat: TakeDamage takes chopper position; add ComputeFallDirection helper"
```

---

### Task 9: Make `DropWood` directional, use `fallDir` in `Fell`

**Goal:** Wood drops on the side opposite the player, in the same direction the tree (will eventually) fall.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`

- [ ] **Step 1: Update `DropWood` signature and offset math**

Replace the current `DropWood` with:

```csharp
private void DropWood(int fallDir)
{
    if (treeData.woodItem == null) return;
    int count = UnityEngine.Random.Range(treeData.woodDropMin, treeData.woodDropMax + 1);
    for (int i = 0; i < count; i++)
    {
        float xPush = fallDir * UnityEngine.Random.Range(0.6f, 1.0f);
        float yJit  = UnityEngine.Random.Range(-0.2f, 0.2f);
        Vector3 offset = new Vector3(xPush, yJit, 0f);
        GameObject drop = Instantiate(treeData.woodItem.gameObject, transform.position + offset, Quaternion.identity);
        var bounce = drop.GetComponent<BounceEffect>();
        if (bounce != null) bounce.StartBounce();
    }
}
```

The only changes from the current version: parameter `int fallDir`, and the new `xPush`/`yJit` offset formula.

- [ ] **Step 2: Update `Fell` to pass `fallDir` to `DropWood`**

Replace the `Fell` body. The existing structure is preserved; the only changes are the call to `DropWood(fallDir)` and removing the now-redundant `if (state == TreeState.Ripe) DropFruits()` line (we'll handle ripe-tree fruit dropping in Task 10's TakeDamage rewrite, where state is guaranteed not Ripe by the time we reach Fell):

```csharp
private void Fell(int fallDir)
{
    DropWood(fallDir);

    if (wasPlanted)
    {
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
```

- [ ] **Step 3: Leave `DropFruits` alone**

`DropFruits` keeps its existing signature and behavior — small random scatter, no directional push. Don't change it.

- [ ] **Step 4: Verify in Editor**

Play. Chop a tree from the **east side**. After the felling chop, wood drops should land on the **west side** (negative X relative to the trunk). Repeat from the **west** — drops should land **east**. From **directly south** (player.x ≈ tree.x), drops randomize L or R between runs.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs"
git commit -m "feat: DropWood scatters in fall direction (away from chopper)"
```

---

## Phase 6 — Fruit drop on first hit

### Task 10: First-hit-on-Ripe drops fruit, no damage

**Goal:** When a tree is in `Ripe` state, the first chop knocks the fruit off without taking HP damage. State returns to `Mature`. Subsequent chops behave normally.

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`

- [ ] **Step 1: Update `TakeDamage`**

Replace the current `TakeDamage`:

```csharp
public void TakeDamage(int damage, Vector3 chopperWorldPos)
{
    if (treeData == null || !treeData.isChoppable) return;
    if (state == TreeState.Seedling || state == TreeState.Stump) return;

    int fallDir = ComputeFallDirection(chopperWorldPos);

    if (animator != null) animator.PlayShake();

    if (state == TreeState.Ripe)
    {
        DropFruits();
        Enter(TreeState.Mature);
        return;
    }

    hpRemaining -= damage;
    if (hpRemaining <= 0) Fell(fallDir);
}
```

The new `if (state == TreeState.Ripe)` block runs after the shake but before the damage subtraction, and returns early so no HP is lost.

- [ ] **Step 2: Verify with a Ripe tree (Oak isn't ripe by default — temporarily set up bait or a fruit on Oak)**

Easiest test: temporarily assign a `Fruit Item` and a `Fruit Overlay` sprite to `OakTree.asset`, set `Ripen Hours` to `1`, and press Play. Wait one in-game hour for the tree to enter Ripe state. Then:
- Click once with axe → fruit drops at the trunk (small scatter), no horizontal push, tree state returns to `Mature`. HP should still be `4` (full).
- Click 4 more times → tree fells normally (5 chops total to fell a ripe tree).

After verifying, you can revert the Oak fruit fields if you want to keep Oak fruit-less.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs"
git commit -m "feat: first chop on Ripe tree drops fruit, no HP damage"
```

---

## Phase 7 — Fall animation

### Task 11: Add `PlayFell` rotation phase + impact callback to `TreeAnimator`

**Goal:** The Top child rotates 90° away from the player over 0.5s, fires an `onImpact` callback at 35% through the rotation. (Lie + fade phases come in Task 12.)

**Files:**
- Modify: `Assets/Scripts/Trees/TreeAnimator.cs`

- [ ] **Step 1: Add `PlayFell` and `FellRoutine`**

Append these methods to `TreeAnimator.cs`:

```csharp
public void PlayFell(int fallDir, Action onImpact, Action onComplete)
{
    StartCoroutine(FellRoutine(fallDir, onImpact, onComplete));
}

private IEnumerator FellRoutine(int fallDir, Action onImpact, Action onComplete)
{
    if (topTransform == null) { onComplete?.Invoke(); yield break; }

    float targetZ = (fallDir == -1) ? +90f : -90f;
    Vector3 startEuler = topTransform.localEulerAngles;
    bool impactFired = false;

    // Phase 1: rotate
    float elapsed = 0f;
    while (elapsed < fallRotateDuration)
    {
        float t = elapsed / fallRotateDuration;
        float eased = t * t;  // easeInQuad
        topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, Mathf.Lerp(0f, targetZ, eased));

        if (!impactFired && t >= fallImpactFraction)
        {
            impactFired = true;
            onImpact?.Invoke();
        }

        elapsed += Time.deltaTime;
        yield return null;
    }
    topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, targetZ);
    if (!impactFired) onImpact?.Invoke();  // safety net

    // Phases 2 + 3 added in Task 12

    onComplete?.Invoke();
}
```

The `eased = t * t` is easeInQuad — slow start, fast finish, looks like the tree gathers momentum as it falls.

- [ ] **Step 2: Verify compile**

Save. Console clean.

- [ ] **Step 3: Don't verify in Editor yet**

We won't see the rotation effect until Tree.cs starts calling `PlayFell` (Task 13). Move on.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scripts/Trees/TreeAnimator.cs"
git commit -m "feat: TreeAnimator.PlayFell rotation phase + impact callback"
```

---

### Task 12: Add lie + fade phases to `PlayFell`

**Goal:** After rotation completes, hold the pose briefly, then fade Top alpha 1→0. Fruit overlay fades in lockstep. End state: Top hidden, Stump still visible.

**Files:**
- Modify: `Assets/Scripts/Trees/TreeAnimator.cs`

- [ ] **Step 1: Replace the `// Phases 2 + 3 added in Task 12` line**

Replace that comment in `FellRoutine` with the lie + fade implementation. The full method now reads:

```csharp
private IEnumerator FellRoutine(int fallDir, Action onImpact, Action onComplete)
{
    if (topTransform == null) { onComplete?.Invoke(); yield break; }

    float targetZ = (fallDir == -1) ? +90f : -90f;
    Vector3 startEuler = topTransform.localEulerAngles;
    bool impactFired = false;

    // Phase 1: rotate
    float elapsed = 0f;
    while (elapsed < fallRotateDuration)
    {
        float t = elapsed / fallRotateDuration;
        float eased = t * t;  // easeInQuad
        topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, Mathf.Lerp(0f, targetZ, eased));

        if (!impactFired && t >= fallImpactFraction)
        {
            impactFired = true;
            onImpact?.Invoke();
        }

        elapsed += Time.deltaTime;
        yield return null;
    }
    topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, targetZ);
    if (!impactFired) onImpact?.Invoke();

    // Phase 2: lie still
    yield return new WaitForSeconds(fallLieDuration);

    // Phase 3: fade
    Color topStartColor   = topRenderer != null   ? topRenderer.color   : Color.white;
    Color fruitStartColor = fruitRenderer != null ? fruitRenderer.color : Color.white;
    elapsed = 0f;
    while (elapsed < fallFadeDuration)
    {
        float t = elapsed / fallFadeDuration;
        float a = Mathf.Lerp(1f, 0f, t);
        if (topRenderer != null)
        {
            Color c = topStartColor; c.a = a; topRenderer.color = c;
        }
        if (fruitRenderer != null && fruitRenderer.enabled)
        {
            Color c = fruitStartColor; c.a = a; fruitRenderer.color = c;
        }
        elapsed += Time.deltaTime;
        yield return null;
    }

    // End: hide top, reset rotation, full alpha for next regrowth cycle
    if (topRenderer != null)
    {
        topRenderer.enabled = false;
        Color c = topStartColor; c.a = 1f; topRenderer.color = c;
    }
    if (fruitRenderer != null)
    {
        fruitRenderer.enabled = false;
        Color c = fruitStartColor; c.a = 1f; fruitRenderer.color = c;
    }
    topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, 0f);

    onComplete?.Invoke();
}
```

The `topRenderer.enabled = false` at the end is the equivalent of "Top has fallen and dissolved away — only Stump remains visible." The alpha reset to 1 makes the renderer ready for the next regrowth cycle without leaving it stuck at 0.

- [ ] **Step 2: Verify compile**

Save. Console clean.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Trees/TreeAnimator.cs"
git commit -m "feat: TreeAnimator.PlayFell lie + fade phases"
```

---

### Task 13: Wire `PlayFell` into `Tree.Fell`; add `OnFellComplete`; collider toggle

**Goal:** When the felling chop lands, Tree calls `animator.PlayFell` with the right callbacks. Collider disables at the start of fall and re-enables after the fade. Drops happen mid-fall (via `onImpact`).

**Files:**
- Modify: `Assets/Scripts/Trees/Tree.cs`

- [ ] **Step 1: Cache the collider reference**

Add a private field at the top of the `Tree` class (near other Runtime fields):

```csharp
private Collider2D treeCollider;
```

And cache it in `Awake()`. The current `Awake` is:

```csharp
private void Awake()
{
    if (string.IsNullOrEmpty(treeID))
    {
        treeID = GlobalHelper.GenerateUniqueId(gameObject);
    }
    hpRemaining = treeData != null ? treeData.chopCount : 1;
    UpdateSprite();
}
```

Add the collider grab as the last line:

```csharp
private void Awake()
{
    if (string.IsNullOrEmpty(treeID))
    {
        treeID = GlobalHelper.GenerateUniqueId(gameObject);
    }
    hpRemaining = treeData != null ? treeData.chopCount : 1;
    UpdateSprite();
    treeCollider = GetComponent<Collider2D>();
}
```

- [ ] **Step 2: Rewrite `Fell` to delegate to the animator**

Replace `Fell` with:

```csharp
private int pendingFallDir;  // stashed for OnFellComplete

private void Fell(int fallDir)
{
    pendingFallDir = fallDir;

    if (treeCollider != null) treeCollider.enabled = false;

    if (animator != null)
    {
        animator.PlayFell(
            fallDir,
            onImpact:   () => DropWood(pendingFallDir),
            onComplete: OnFellComplete
        );
    }
    else
    {
        // No animator wired — fall back to immediate fell
        DropWood(fallDir);
        OnFellComplete();
    }
}

private void OnFellComplete()
{
    if (wasPlanted)
    {
        Destroy(gameObject);
        return;
    }
    if (!treeData.regrows)
    {
        permanentlyGone = true;
        Destroy(gameObject);
        return;
    }
    hpRemaining = treeData.chopCount;
    Enter(TreeState.Stump);
    if (treeCollider != null) treeCollider.enabled = true;
}
```

The `pendingFallDir` field is used because the `onImpact` callback closure could be created before the lambda captures the local — using a field guarantees we read the latest value. Equivalent to passing it through but slightly more defensive.

- [ ] **Step 3: Verify the full chop loop in Editor**

This is the big one. Press Play.

Chop a tree from the **east side** with `regrowHours = 1` (set on OakTree.asset for fast regrowth testing):

1. Each chop → Top shakes (~0.1s), Stump stays still.
2. 4th chop → Top rotates 90° to the **west** over 0.5s.
3. At ~35% through the rotation → wood prefabs spawn on the west side and bounce.
4. Top holds at 90° rotation for ~0.2s.
5. Top fades out over ~0.3s while Stump remains visible.
6. Stump persists. Wait ~1 in-game hour → Top reappears (mature sprite, full alpha), tree is back.
7. Walk through the position of the falling tree mid-animation — player should pass through (no collision). Once stump is back, walking into it should be blocked again.

Test from the **west** — should mirror (Top falls east, drops east).

Test from **directly south** — fall direction randomizes L/R between runs.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scripts/Trees/Tree.cs"
git commit -m "feat: Tree.Fell delegates to animator with onImpact + onComplete; collider toggle"
```

---

## Phase 8 — End-to-end verification

### Task 14: Walk through the verification matrix

**Goal:** Manually verify each row of the spec's verification matrix in Play mode. Anything that fails kicks the relevant phase back to a fix.

- [ ] **Step 1: Set up testing config**

On `OakTree.asset`:
- `regrowHours = 1` (so stump regrowth is testable in seconds, not minutes).
- Optionally assign a `Fruit Item` and `Fruit Overlay` for the fruit-drop test, with `ripenHours = 1`.

- [ ] **Step 2: Test each scenario from the spec matrix**

| Scenario | Pass? |
|---|---|
| Player east of tree, chops 4× — Top shakes per hit, stump stays still, falls west, wood drops west, fade → stump | [ ] |
| Player west of tree, chops 4× — mirrored: falls east, drops east | [ ] |
| Player due south of tree, chops 4× — random L or R fall (different runs differ); drops + fall agree | [ ] |
| Player due north of tree, chops 4× — random L or R fall | [ ] |
| Player diagonal SE, chops 4× — falls west deterministically | [ ] |
| Ripe tree, 1st chop — fruit drops at trunk (no horizontal push); state → Mature; HP unchanged | [ ] |
| Ripe tree, 1st chop then 4 more — 1 fruit + 4 wood drops total; tree fells normally on 5th total chop | [ ] |
| Mid-fall, player walks toward tree — no collision, player walks through | [ ] |
| Post-stump, player tries to walk into stump — blocked | [ ] |
| Random L/R fall — wood drops match the random direction (consistent per run) | [ ] |

For each row:
1. Reproduce the scenario.
2. If it passes, tick the box.
3. If it fails, note what went wrong and which task to revisit.

- [ ] **Step 3: Reset testing config**

After verification:
- `regrowHours` back to `72` on OakTree.asset.
- Remove the testing fruit overlay/item assignments unless you want Oak to ripen for real.

- [ ] **Step 4: Commit (only if config changes are worth keeping)**

If you adjusted any defaults you want to keep (e.g., shake amplitude, threshold), commit them:

```bash
git add "Assets/Data/Trees/OakTree.asset" "Assets/Prefabs/Trees/Tree_Oak.prefab"
git commit -m "chore: tune Tree juice defaults after manual verification"
```

If no config tweaks: skip the commit.

---

## Self-review checklist (completed before handoff)

**Spec coverage:**

| Spec section | Implemented in |
|---|---|
| Architecture (scripts split, prefab restructure) | Tasks 4, 6 |
| Fall direction algorithm | Task 8 |
| Fruit drop change (first-hit-on-Ripe) | Task 10 |
| Drop offset formula (xPush, yJit) | Task 9 |
| `PlayShake` coroutine | Task 6 |
| `PlayFell` rotation phase + onImpact | Task 11 |
| `PlayFell` lie + fade phases + onComplete | Task 12 |
| Tree.cs Fell flow + collider toggle | Task 13 |
| TreeData additions (topSprite, stumpSprite) | Task 2 |
| Sprite-state mapping rewrite | Task 5 |
| Art deliverables (sprite split + pivots) | Task 1 |
| Manual verification matrix | Task 14 |

**Placeholder scan:** No "TBD", "implement later", or vague steps. Every code block contains the actual code to type. Every Editor step names the exact menu path and field.

**Type consistency:**
- `TakeDamage(int, Vector3)` — defined Task 8, called from PlayerToolDispatcher (Task 8), invoked internally by `TreeAnimator` callbacks via `Fell` (Task 13).
- `Fell(int fallDir)` — defined Task 8 (signature change), bodied Task 9, used by Task 13.
- `DropWood(int fallDir)` — defined Task 9, called from Task 13's `onImpact` lambda.
- `ComputeFallDirection(Vector3)` — defined Task 8, called from `TakeDamage` (Task 10's final form).
- `TreeAnimator.PlayShake()` — Task 6, called Task 7.
- `TreeAnimator.PlayFell(int, Action, Action)` — Task 11 (rotation), Task 12 (lie + fade), called Task 13.
- `OnFellComplete()` — defined Task 13, called as `onComplete` callback from `PlayFell`.
- `topRenderer / stumpRenderer / fruitRenderer` fields — defined Task 5, used by `UpdateSprite` (Task 5) and TreeAnimator (Task 6 wiring).

All names match across tasks.

**Open assumptions documented inline:**
- Task 1 Step 5: pivot placement requires the user to make a visual judgment call (where exactly the upper trunk meets the stump).
- Task 4 Step 8: the visual "Top sits cleanly on Stump" check may need a small `Top.localPosition.y` tweak depending on how precisely the pivots align in the sprite editor.
- Task 13 Step 3: relies on `regrowHours = 1` for fast verification. Cleanup in Task 14 Step 3.

No gaps identified.
