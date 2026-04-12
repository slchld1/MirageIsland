using System;
using UnityEngine;

/// <summary>
/// Tension-based tug minigame. Hold LMB to raise tension, release to lower.
/// Stay in the sweet zone to fill reel progress. Catch fires OnCatch, escape fires OnEscape.
/// Call Tick() each Update from FishingController while in Minigame state.
/// Attach to the player GameObject.
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
    public float reelDrainRate = 0.05f;

    public float Tension { get; private set; }
    public float ReelProgress { get; private set; }

    public event Action OnCatch;
    public event Action OnEscape;

    private float dangerTimer;
    private float slackTimer;
    private float currentDangerTime;
    private bool active;

    public void StartMinigame(int rodTier)
    {
        Tension = 0.5f;
        ReelProgress = 0f;
        dangerTimer = 0f;
        slackTimer = 0f;
        active = true;

        // Rod tier widens sweet zone and extends danger timer
        float tierBonus = (rodTier - 1) * 0.05f;
        sweetMin = Mathf.Max(0.15f, 0.3f - tierBonus);
        sweetMax = Mathf.Min(0.85f, 0.7f + tierBonus);
        currentDangerTime = baseDangerTime + (rodTier - 1) * 0.5f;
    }

    public void StopMinigame()
    {
        active = false;
    }

    /// <summary>
    /// Called each frame by FishingController. holdingButton = LMB is held.
    /// </summary>
    public void Tick(bool holdingButton)
    {
        if (!active) return;

        // Update tension
        if (holdingButton)
            Tension = Mathf.Clamp01(Tension + tensionRiseRate * Time.deltaTime);
        else
            Tension = Mathf.Clamp01(Tension - tensionFallRate * Time.deltaTime);

        bool inSweet  = Tension >= sweetMin && Tension <= sweetMax;
        bool inDanger = Tension > dangerThreshold;
        bool inSlack  = Tension < sweetMin;

        // Reel progress
        if (inSweet)
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

        // Danger zone timer
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
            }
        }
        else
        {
            slackTimer = Mathf.Max(0f, slackTimer - Time.deltaTime);
        }
    }
}
