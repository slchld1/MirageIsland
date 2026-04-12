using System;
using UnityEngine;

/// <summary>
/// Counts down to a fish bite. Spawns brief fish blinks near the bob.
/// Call Tick() each Update from FishingController while in Waiting state.
/// Attach to the player GameObject alongside FishingLine.
/// </summary>
public class FishBiteDetector : MonoBehaviour
{
    [Header("Bite Timing")]
    public float minWaitTime = 5f;
    public float maxWaitTime = 20f;

    [Header("Fish Blink")]
    public GameObject fishBlinkPrefab;
    public float blinkInterval = 4f;
    public float blinkRadius = 1.5f;

    public event Action OnBite;

    private FishingLine fishingLine;
    private float biteTimer;
    private float blinkTimer;
    private bool active;

    private void Awake()
    {
        fishingLine = GetComponent<FishingLine>();
    }

    public void StartDetection()
    {
        active = true;
        biteTimer = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        blinkTimer = blinkInterval;
    }

    public void StopDetection()
    {
        active = false;
    }

    public void ResetForNextFish()
    {
        active = true;
        biteTimer = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        blinkTimer = blinkInterval;
    }

    /// <summary>
    /// Called each frame by FishingController while waiting. proximityBonus is 0.0–0.2.
    /// </summary>
    public void Tick(float proximityBonus)
    {
        if (!active) return;

        float speedMultiplier = 1f + proximityBonus;
        biteTimer -= Time.deltaTime * speedMultiplier;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            SpawnBlink();
            blinkTimer = blinkInterval + UnityEngine.Random.Range(-1f, 1f);
        }

        if (biteTimer <= 0f)
        {
            active = false;
            OnBite?.Invoke();
        }
    }

    private void SpawnBlink()
    {
        if (fishBlinkPrefab == null) return;
        if (fishingLine == null) return;

        Vector2 blinkPos = fishingLine.BobPosition + UnityEngine.Random.insideUnitCircle * blinkRadius;
        GameObject blink = Instantiate(fishBlinkPrefab, blinkPos, Quaternion.identity);

        // Tell FishingLine where this blink appeared so proximity can be calculated
        fishingLine.LastBlinkPosition = blinkPos;
    }
}
