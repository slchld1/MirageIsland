# Tension Bar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the existing simple `tensionFill` Image in `TugMinigameUI` with a polished `TensionBar` prefab driven by a new `FillBarController` script.

**Architecture:** `FillBarController` is a self-contained MonoBehaviour that takes a single `NormalizedValue` float (0–1) each frame and drives fill width, danger overlay alpha, and indicator position. `TugMinigameUI` is updated to hold a reference to `FillBarController` and feed `tugMinigame.Tension` into it each Update. The prefab hierarchy is built manually in the Unity Editor per the setup instructions in Task 3.

**Tech Stack:** Unity 2D, C#, Unity UI (RectTransform, Image, Mask), TextMeshPro

---

### Task 1: Write FillBarController.cs

**Files:**
- Create: `Assets/Scripts/Fishing/FillBarController.cs`

- [ ] **Step 1: Create the script**

Create `Assets/Scripts/Fishing/FillBarController.cs` with the following content:

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a polished fill bar from a single NormalizedValue (0-1).
/// Attach to TensionBar_Panel. Wire all references in the Inspector.
/// </summary>
public class FillBarController : MonoBehaviour
{
    [Header("References")]
    public RectTransform fillBase;
    public Image         fillDanger;
    public RectTransform indicator;
    public RectTransform dangerZoneMarker;

    [Header("Settings")]
    [Tooltip("Visual threshold (0-1) where the red danger overlay begins to appear")]
    public float dangerThreshold = 0.75f;

    /// <summary>Single input. Set this each frame from your gameplay script (0-1).</summary>
    public float NormalizedValue { get; set; }

    private float barWidth;
    private float indicatorX;

    private void Start()
    {
        // barWidth comes from Fill_Container (fillBase's parent)
        barWidth   = ((RectTransform)fillBase.parent).rect.width;
        indicatorX = 0f;

        // Position danger zone marker once — it never moves after this
        if (dangerZoneMarker != null)
        {
            Vector2 pos = dangerZoneMarker.anchoredPosition;
            pos.x = dangerThreshold * barWidth;
            dangerZoneMarker.anchoredPosition = pos;
        }
    }

    private void Update()
    {
        float targetWidth = NormalizedValue * barWidth;

        // Fill_Base — width tracks NormalizedValue directly
        fillBase.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

        // Fill_Danger — alpha lerps 0→1 above dangerThreshold
        if (fillDanger != null)
        {
            float alpha = NormalizedValue < dangerThreshold
                ? 0f
                : (NormalizedValue - dangerThreshold) / (1f - dangerThreshold);
            Color c = fillDanger.color;
            c.a = alpha;
            fillDanger.color = c;
        }

        // Indicator — snappy-elastic follow of the fill's leading edge
        indicatorX = Mathf.Lerp(indicatorX, targetWidth, 12f * Time.deltaTime);
        if (indicator != null)
        {
            Vector2 pos = indicator.anchoredPosition;
            pos.x = indicatorX;
            indicator.anchoredPosition = pos;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles in Unity**

Switch to the Unity Editor. The console should show no errors for `FillBarController.cs`. If there are errors, fix them before continuing.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/FillBarController.cs Assets/Scripts/Fishing/FillBarController.cs.meta
git commit -m "feat: add FillBarController for polished tension bar"
```

---

### Task 2: Update TugMinigameUI.cs

**Files:**
- Modify: `Assets/Scripts/Fishing/TugMinigameUI.cs`

- [ ] **Step 1: Replace the file contents**

Replace `Assets/Scripts/Fishing/TugMinigameUI.cs` entirely with:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the tension bar, reel bar, and reaction event prompts during the tug minigame.
/// Attach to a Canvas child. Assign all references in the Inspector.
/// </summary>
public class TugMinigameUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Bars")]
    [Tooltip("FillBarController on the TensionBar_Panel prefab")]
    public FillBarController tensionBar;
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal")]
    public Image reelFill;

    [Header("Event Prompts")]
    [Tooltip("GameObject shown during a Dart event. Should contain a Text child.")]
    public GameObject dartPrompt;
    [Tooltip("Text component inside dartPrompt that shows '← Q' or 'E →'")]
    public Text dartPromptText;
    [Tooltip("GameObject shown during a Tug event")]
    public GameObject tugPrompt;
    [Tooltip("Text component inside tugPrompt")]
    public Text tugPromptText;

    [Header("Fail Flash")]
    public Color failColor = Color.red;
    public float failFlashDuration = 0.5f;

    private TugMinigame tugMinigame;
    private bool flashingDart;
    private bool flashingTug;

    private void Awake()
    {
        tugMinigame = FindAnyObjectByType<TugMinigame>();
        if (tugMinigame == null) Debug.LogError("[TugMinigameUI] TugMinigame not found in scene.", this);
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (tugMinigame != null)
            tugMinigame.OnEventFailed += OnEventFailed;
    }

    private void OnDestroy()
    {
        if (tugMinigame != null)
            tugMinigame.OnEventFailed -= OnEventFailed;
    }

    public void Show() { if (panel != null) panel.SetActive(true); }
    public void Hide() { if (panel != null) panel.SetActive(false); }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (tugMinigame == null) return;

        // Tension bar
        if (tensionBar != null)
            tensionBar.NormalizedValue = tugMinigame.Tension;

        // Reel bar
        if (reelFill != null)
            reelFill.fillAmount = tugMinigame.ReelProgress;

        // Event prompts — keep visible while flashing
        bool isDart = tugMinigame.ActiveEvent == EventType.Dart;
        bool isTug  = tugMinigame.ActiveEvent == EventType.Tug;

        if (dartPrompt != null)
        {
            dartPrompt.SetActive(isDart || flashingDart);
            if (isDart && dartPromptText != null)
                dartPromptText.text = tugMinigame.ActiveEventDir == -1 ? "← Q" : "E →";
        }

        if (tugPrompt != null)
            tugPrompt.SetActive(isTug || flashingTug);
    }

    private void OnEventFailed(EventType type)
    {
        if (type == EventType.Dart && dartPrompt != null)
            StartCoroutine(FlashFailed(type, dartPrompt, dartPromptText));
        else if (type == EventType.Tug && tugPrompt != null)
            StartCoroutine(FlashFailed(type, tugPrompt, tugPromptText));
    }

    private IEnumerator FlashFailed(EventType type, GameObject prompt, Text label)
    {
        if (type == EventType.Dart) flashingDart = true;
        else                        flashingTug  = true;

        prompt.SetActive(true);

        Color original = label != null ? label.color : Color.white;
        float timer = 0f;

        while (timer < failFlashDuration)
        {
            timer += Time.deltaTime;
            if (label != null)
                label.color = Color.Lerp(failColor, original, timer / failFlashDuration);
            yield return null;
        }

        if (label != null)
            label.color = original;

        if (type == EventType.Dart) flashingDart = false;
        else                        flashingTug  = false;
    }
}
```

- [ ] **Step 2: Verify it compiles in Unity**

Switch to the Unity Editor. Check the console — no errors. The existing `TugMinigameUI` Inspector will show a missing reference for `tensionBar` (the old `tensionFill` slot is gone). That's expected — you'll wire it in Task 4.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Fishing/TugMinigameUI.cs
git commit -m "refactor: replace tensionFill Image with FillBarController in TugMinigameUI"
```

---

### Task 3: Build the TensionBar Prefab in the Unity Editor

This task is done entirely in the Unity Editor. Follow each step in order.

**Files:**
- Create prefab (in Editor): `Assets/Prefabs/UI/TensionBar.prefab` (or wherever your UI prefabs live)

- [ ] **Step 1: Create the root GameObject**

In the Hierarchy, right-click your existing HUD Canvas → **UI → Empty** → name it `TensionBar_Panel`.

- [ ] **Step 2: Add Bar_Background**

Right-click `TensionBar_Panel` → **UI → Image** → name it `Bar_Background`.
- Set color to `#5A3E2B` (R:90 G:62 B:43)
- Set size to **200 × 20**
- (Optional) assign a 9-sliced sprite if you have one; plain color is fine

- [ ] **Step 3: Add Bar_InnerBG**

Right-click `Bar_Background` → **UI → Image** → name it `Bar_InnerBG`.
- Set color to `#3D2B1A` (R:61 G:43 B:26)
- Anchor: stretch-stretch, with a small inset (e.g., Left/Right/Top/Bottom all at 2)

- [ ] **Step 4: Add Fill_Container**

Right-click `Bar_InnerBG` → **UI → Image** → name it `Fill_Container`.
- Size: **200 × 20** (match Bar_InnerBG inner area)
- Anchor: left-center (anchor min/max X = 0, Y = 0.5)
- Add a **Mask** component (Component → UI → Mask). Uncheck "Show Mask Graphic" if you don't want the white default visible.
- The Image component is required for Mask to function — set its color to white or clear.

- [ ] **Step 5: Add Fill_Base inside Fill_Container**

Right-click `Fill_Container` → **UI → Image** → name it `Fill_Base`.
- Color: `#E9C46A` (R:233 G:196 B:106)
- Anchor: left-stretch (anchor min X=0, max X=0, min Y=0, max Y=1)
- Pivot: (0, 0.5)
- Width will be set by FillBarController at runtime — set it to 0 for now

- [ ] **Step 6: Add Fill_Danger inside Fill_Container**

Right-click `Fill_Container` → **UI → Image** → name it `Fill_Danger`.
- Color: `#C1121F` (R:193 G:18 B:31), alpha = 0
- Same anchor/pivot as Fill_Base (anchor left-stretch, pivot (0, 0.5))
- Width should match Fill_Base (also driven by script — set width = 200 for now so it covers Full_Base when alpha goes up)

  > Note: Fill_Danger sits on top of Fill_Base and its alpha is driven to 0→1 as tension rises above the danger threshold. It does NOT need its width driven — set it to full bar width (200) and leave it. Only the alpha changes.

- [ ] **Step 7: Add Fill_Sheen inside Fill_Container**

Right-click `Fill_Container` → **UI → Image** → name it `Fill_Sheen`.
- Color: white, alpha = 31 (12% of 255 ≈ 31)
- Anchor: left-stretch for X, top 30% for Y (anchor min Y = 0.7, max Y = 1.0)
- This creates a thin highlight strip along the top of the bar

- [ ] **Step 8: Add DangerZone_Marker inside Bar_InnerBG**

Right-click `Bar_InnerBG` → **UI → Image** → name it `DangerZone_Marker`.
- Color: white or light grey (visible against bar background)
- Size: **2 × 20** (2px wide vertical line, full bar height)
- Anchor: left-center (anchor X = 0, Y = 0.5), pivot (0.5, 0.5)
- anchoredPosition.x will be set by FillBarController.Start() — leave at 0 for now

- [ ] **Step 9: Add Indicator inside Bar_InnerBG**

Right-click `Bar_InnerBG` → **UI → Image** → name it `Indicator`.
- Color: `#F4A261` (R:244 G:162 B:97)
- Size: **6 × 14**
- Anchor: left-center (anchor X = 0, Y = 0.5), pivot **(0.5, 0.5)**
- anchoredPosition.x tracked by FillBarController at runtime

- [ ] **Step 10: Add Indicator_Shadow inside Indicator**

Right-click `Indicator` → **UI → Image** → name it `Indicator_Shadow`.
- Color: black, alpha = 102 (40% of 255 ≈ 102)
- Size: **6 × 14** (same as Indicator)
- Anchor: center-center, pivot (0.5, 0.5)
- anchoredPosition: **(2, -2)** — 2px right, 2px down

- [ ] **Step 11: Add Label_TENSION**

Right-click `TensionBar_Panel` → **UI → Text - TextMeshPro** → name it `Label_TENSION`.
- Text: `TENSION`
- Font: choose a monospace or pixel-style font. Enable All Caps in the font style dropdown.
- Position it below or above the bar as you like.

- [ ] **Step 12: Attach FillBarController**

Select `TensionBar_Panel`. In the Inspector → **Add Component** → search `FillBarController` → add it.

- [ ] **Step 13: Wire FillBarController references**

With `TensionBar_Panel` selected, in the `FillBarController` Inspector slots:
- **fillBase** → drag `Fill_Base`
- **fillDanger** → drag `Fill_Danger`
- **indicator** → drag `Indicator`
- **dangerZoneMarker** → drag `DangerZone_Marker`
- **dangerThreshold** → leave at `0.75`

- [ ] **Step 14: Save as prefab**

Drag `TensionBar_Panel` from the Hierarchy into `Assets/Prefabs/UI/` (create the folder if needed) to create `TensionBar.prefab`.

- [ ] **Step 15: Commit**

```bash
git add Assets/Prefabs/UI/TensionBar.prefab Assets/Prefabs/UI/TensionBar.prefab.meta
git commit -m "feat: add TensionBar prefab with FillBarController hierarchy"
```

---

### Task 4: Wire TensionBar into the Scene

**Files:**
- Modify scene: `Assets/Scenes/SampleScene.unity` (via Editor)

- [ ] **Step 1: Place TensionBar in the HUD**

In the Hierarchy, drag `TensionBar.prefab` onto your HUD Canvas (or `HUD_Root` if it exists). Position it where the old tension bar was.

- [ ] **Step 2: Remove the old tensionFill Image**

Delete (or disable) whatever old Image GameObject was previously assigned to `TugMinigameUI.tensionFill` — it's no longer needed.

- [ ] **Step 3: Wire tensionBar in TugMinigameUI**

Select the GameObject that has `TugMinigameUI` on it. In the Inspector, find the **Tension Bar** slot (previously `Tension Fill`). Drag the `TensionBar_Panel` GameObject into it.

- [ ] **Step 4: Verify in Play Mode**

Enter Play Mode and trigger the fishing minigame. Check:
- [ ] Yellow bar (Fill_Base) grows as you hold LMB
- [ ] Amber indicator (Indicator) follows the leading edge with a slight lag
- [ ] Red overlay (Fill_Danger) fades in when tension exceeds ~75%
- [ ] DangerZone_Marker appears as a vertical line at the 75% mark and never moves
- [ ] Bar resets properly when minigame ends

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/SampleScene.unity
git commit -m "feat: wire TensionBar prefab into fishing minigame HUD"
```
