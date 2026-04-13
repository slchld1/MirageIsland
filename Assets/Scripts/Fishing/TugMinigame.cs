using System;
using UnityEngine;

/// <summary>
/// Tension-based tug minigame with per-species fight phases and reaction events.
/// Hold LMB to raise tension. Stay in sweet zone to fill reel. React to Dart/Tug events.
/// Call Tick() each Update from FishingController while in Minigame state.
/// </summary>
public class TugMinigame : MonoBehaviour
{
    [Header("Tension Rates")]
    public float tensionRiseRate = 0.4f;
    public float tensionFallRate = 0.3f;

    [Header("Zones (defaults, widened by rod tier)")]
    public float sweetMin = 0.3f;
    public float sweetMax = 0.7f;
    public float dangerThreshold = 0.8f;

    [Header("Escape Timers")]
    [Tooltip("Seconds in danger zone before fish escapes")]
    public float baseDangerTime = 2f;
    [Tooltip("Seconds below sweet zone (too slack) before fish escapes")]
    public float slackEscapeTime = 3f;

    [Header("Reel")]
    public float reelFillRate = 0.15f;
    public float reelDrainRate = 0.02f;

    [Header("Reaction Events")]
    [Tooltip("Dart reaction window in seconds at rarity 1. Scales down with rarity.")]
    public float baseReactionWindow = 1.2f;
    [Tooltip("Tug spam window in seconds at rarity 1. Scales down with rarity.")]
    public float baseTugWindow = 3f;
    [Tooltip("Reel progress lost on a missed Dart event or a Tug event with zero clicks")]
    public float eventMissPenalty = 0.15f;
    [Tooltip("Reel progress gained per click during a Tug event")]
    public float reelPerTugClick = 0.02f;
    [Tooltip("Reel progress gained on a successful Dart event")]
    public float dartHitBoost = 0.08f;

    public float Tension      { get; private set; }
    public float ReelProgress { get; private set; }

    // Current active reaction event (null = none)
    public EventType? ActiveEvent    { get; private set; }
    // -1 = left (Q), 1 = right (E) — only meaningful when ActiveEvent == Dart
    public int        ActiveEventDir { get; private set; }

    public event Action OnCatch;
    public event Action OnEscape;
    public event Action<EventType> OnEventFailed;

    private float dangerTimer;
    private float slackTimer;
    private float currentDangerTime;
    private bool  active;
    private int   tugClickCount;

    // Phase state
    private FishData currentFish;
    private int      currentPhaseIndex;
    private float    phaseTimer;
    private float    eventTimer;
    private float    eventWindowTimer;
    private float    rarityMult;

    public void StartMinigame(int rodTier, FishData fish)
    {
        Tension       = 0.5f;
        ReelProgress  = 0f;
        dangerTimer   = 0f;
        slackTimer    = 0f;
        active        = true;
        ActiveEvent   = null;

        // Rod tier widens sweet zone and extends danger timer
        float tierBonus = (rodTier - 1) * 0.05f;
        sweetMin          = Mathf.Max(0.15f, 0.3f - tierBonus);
        sweetMax          = Mathf.Min(0.85f, 0.7f + tierBonus);
        currentDangerTime = baseDangerTime + (rodTier - 1) * 0.5f;

        // Phase setup
        currentFish       = fish;
        currentPhaseIndex = 0;
        rarityMult        = RarityMultiplier(fish != null ? fish.rarity : 1);

        if (fish != null && fish.phases != null && fish.phases.Length > 0)
        {
            phaseTimer = fish.phases[0].duration;
            eventTimer = fish.phases[0].eventInterval * rarityMult;
        }
        else
        {
            // No phases — events never fire
            phaseTimer = float.MaxValue;
            eventTimer = float.MaxValue;
        }
    }

    public void StopMinigame()
    {
        active      = false;
        ActiveEvent = null;
    }

    /// <summary>
    /// Called each frame by FishingController.
    /// holdingButton = LMB is held. lmbJustPressed = LMB was pressed this frame.
    /// </summary>
    public void Tick(bool holdingButton, bool lmbJustPressed, bool pressedQ, bool pressedE)
    {
        if (!active) return;

        // Tension — frozen while any reaction event is active (LMB disabled)
        if (ActiveEvent == null)
        {
            if (holdingButton)
                Tension = Mathf.Clamp01(Tension + tensionRiseRate * Time.deltaTime);
            else
                Tension = Mathf.Clamp01(Tension - tensionFallRate * Time.deltaTime);
        }

        bool inSweet  = Tension >= sweetMin && Tension <= sweetMax;
        bool inDanger = Tension > dangerThreshold;
        bool inSlack  = Tension < sweetMin;

        // Reel progress — always drains during any reaction event
        if (ActiveEvent != null)
        {
            ReelProgress = Mathf.Clamp01(ReelProgress - reelDrainRate * Time.deltaTime);
        }
        else if (inSweet)
        {
            ReelProgress = Mathf.Clamp01(ReelProgress + reelFillRate * Time.deltaTime);
            if (ReelProgress >= 1f)
            {
                active = false;
                OnCatch?.Invoke();
                return;
            }
        }
        else
        {
            ReelProgress = Mathf.Clamp01(ReelProgress - reelDrainRate * Time.deltaTime);
        }

        // Danger timer
        if (inDanger)
        {
            dangerTimer += Time.deltaTime;
            if (dangerTimer >= currentDangerTime)
            {
                active = false;
                OnEscape?.Invoke();
                return;
            }
        }
        else
        {
            dangerTimer = Mathf.Max(0f, dangerTimer - Time.deltaTime * 0.5f);
        }

        // Slack timer
        if (inSlack)
        {
            slackTimer += Time.deltaTime;
            if (slackTimer >= slackEscapeTime)
            {
                active = false;
                OnEscape?.Invoke();
                return;
            }
        }
        else
        {
            slackTimer = Mathf.Max(0f, slackTimer - Time.deltaTime);
        }

        TickPhase();
        TickEvent(lmbJustPressed, pressedQ, pressedE);
    }

    // ── Phase stepping ────────────────────────────────────────────────────────

    private void TickPhase()
    {
        if (currentFish == null || currentFish.phases == null || currentFish.phases.Length == 0) return;

        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f && currentPhaseIndex < currentFish.phases.Length - 1)
        {
            currentPhaseIndex++;
            FightPhase next = currentFish.phases[currentPhaseIndex];
            phaseTimer = next.duration;
            eventTimer = next.eventInterval * rarityMult;
        }
    }

    // ── Event firing and resolution ───────────────────────────────────────────

    private void TickEvent(bool lmbJustPressed, bool pressedQ, bool pressedE)
    {
        if (currentFish == null || currentFish.phases == null || currentFish.phases.Length == 0) return;

        FightPhase phase = currentFish.phases[currentPhaseIndex];
        if (phase.possibleEvents == null || phase.possibleEvents.Length == 0) return;

        if (ActiveEvent == null)
        {
            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0f)
            {
                FireEvent(phase);
                eventTimer = phase.eventInterval * rarityMult;
            }
        }
        else
        {
            bool resolved = false;
            bool hit      = false;

            if (ActiveEvent == EventType.Dart)
            {
                if ((ActiveEventDir == -1 && pressedQ) || (ActiveEventDir == 1 && pressedE))
                {
                    resolved = true; hit = true;
                }
                else if (pressedQ || pressedE) // wrong direction
                {
                    resolved = true; hit = false;
                }
            }
            else if (ActiveEvent == EventType.Tug)
            {
                // Each click slowly fights the fish and gains reel progress
                if (lmbJustPressed)
                {
                    ReelProgress = Mathf.Clamp01(ReelProgress + reelPerTugClick);
                    tugClickCount++;
                    if (ReelProgress >= 1f)
                    {
                        active = false;
                        ActiveEvent = null;
                        OnCatch?.Invoke();
                        return;
                    }
                }
            }

            eventWindowTimer -= Time.deltaTime;
            if (eventWindowTimer <= 0f && !resolved)
            {
                resolved = true;
                hit = tugClickCount > 0;
            }

            if (resolved)
            {
                EventType failedType = ActiveEvent.Value;

                if (ActiveEvent == EventType.Dart)
                {
                    if (hit)
                        ReelProgress = Mathf.Clamp01(ReelProgress + dartHitBoost);
                    else
                    {
                        ReelProgress = Mathf.Clamp01(ReelProgress - eventMissPenalty);
                        OnEventFailed?.Invoke(failedType);
                    }
                }
                else // Tug — penalise only if player didn't click at all
                {
                    if (!hit)
                    {
                        ReelProgress = Mathf.Clamp01(ReelProgress - eventMissPenalty);
                        OnEventFailed?.Invoke(failedType);
                    }
                }

                ActiveEvent = null;
            }
        }
    }

    private void FireEvent(FightPhase phase)
    {
        EventType type = phase.possibleEvents[UnityEngine.Random.Range(0, phase.possibleEvents.Length)];
        ActiveEvent      = type;
        ActiveEventDir   = (type == EventType.Dart) ? (UnityEngine.Random.value > 0.5f ? 1 : -1) : 0;
        eventWindowTimer = (type == EventType.Tug ? baseTugWindow : baseReactionWindow) * rarityMult;
        tugClickCount    = 0;
    }

    // rarity 1 = 1.0x, rarity 5 = 0.4x
    private static float RarityMultiplier(int rarity) =>
        1f - Mathf.Clamp(rarity - 1, 0, 4) * 0.15f;
}
