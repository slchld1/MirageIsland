# Tension Bar — Design Spec
*Date: 2026-04-18*

## Overview

Replace the existing simple `tensionFill` Image in `TugMinigameUI` with a polished
`TensionBar` prefab driven by a new `FillBarController` script.

---

## Deliverables

1. `Assets/Scripts/Fishing/FillBarController.cs` — driving script
2. `TensionBar.prefab` — full UI hierarchy (Editor setup; path TBD by user)
3. Updated `Assets/Scripts/Fishing/TugMinigameUI.cs` — wires in `FillBarController`

---

## Prefab Hierarchy

```
Canvas
└── HUD_Root                    [CanvasGroup]
    └── TensionBar_Panel        [RectTransform]  ← FillBarController attached here
        ├── Bar_Background      [Image]  — 9-sliced dark border, color #5A3E2B
        │   └── Bar_InnerBG     [Image]  — color #3D2B1A
        │       ├── Fill_Container  [Mask + Image]  — clips all fills
        │       │   ├── Fill_Base       [Image]  — color #E9C46A, width driven by script
        │       │   ├── Fill_Danger     [Image]  — color #C1121F, alpha driven by script
        │       │   └── Fill_Sheen      [Image]  — white at 12% alpha, top 30% height
        │       ├── DangerZone_Marker   [Image]  — 2px wide vertical line, set once in Start
        │       └── Indicator           [Image]  — color #F4A261, 6×14px, pivot (0.5, 0.5)
        │           └── Indicator_Shadow [Image] — black at 40% alpha, offset +2px right/down
        └── Label_TENSION       [TextMeshProUGUI]  — text "TENSION", monospace/pixel, all caps
```

**Size:** Bar_Background / Fill_Container = 200 × 20px. All fill images left-anchored.

---

## FillBarController.cs

**Attached to:** `TensionBar_Panel`

| Field | Type | Default | Notes |
|---|---|---|---|
| `NormalizedValue` | `float` | — | Public input, 0–1, set each frame by caller |
| `dangerThreshold` | `float` | `0.75` | Visual only — when red overlay starts |
| `fillBase` | `RectTransform` | — | Serialized ref |
| `fillDanger` | `Image` | — | Serialized ref |
| `indicator` | `RectTransform` | — | Serialized ref |
| `dangerZoneMarker` | `RectTransform` | — | Serialized ref, positioned once in Start |

**Behavior:**

- `Fill_Base` width: `SetSizeWithCurrentAnchors(Horizontal, NormalizedValue × barWidth)`
- `Fill_Danger` alpha: 0 when `NormalizedValue < dangerThreshold`; lerps 0→1 above it
- `Indicator` x: `Lerp(current, NormalizedValue × barWidth, 12f × Time.deltaTime)` — snappy-elastic
- `DangerZone_Marker` x: set once in `Start()` to `dangerThreshold × barWidth`, never moves
- `barWidth` cached from `Fill_Container` rect width in `Start()`

---

## TugMinigameUI.cs Changes

- **Remove:** `public Image tensionFill` field and all color/fillAmount logic for it
- **Add:** `public FillBarController tensionBar` serialized field
- **Update():** `tensionBar.NormalizedValue = tugMinigame.Tension;`

The `dangerThreshold` values are independent:
- `FillBarController.dangerThreshold` = visual red overlay onset (default 0.75)
- `TugMinigame.dangerThreshold` = gameplay fish-escape trigger (default 0.8)

---

## Visual Specs

| Element | Color | Size |
|---|---|---|
| Bar_Background | `#5A3E2B` | 200 × 20px |
| Bar_InnerBG | `#3D2B1A` | inset |
| Fill_Base | `#E9C46A` | height = full, width driven |
| Fill_Danger | `#C1121F` | same as Fill_Base, alpha driven |
| Fill_Sheen | white 12% alpha | top 30% height strip |
| Indicator | `#F4A261` | 6 × 14px |
| Indicator_Shadow | black 40% alpha | +2px right/down offset |
| Label_TENSION | TMP monospace, all caps | — |

---

## Out of Scope

- Linking `FillBarController.dangerThreshold` to `TugMinigame.dangerThreshold` at runtime
- Animating the reel bar (unchanged from current implementation)
