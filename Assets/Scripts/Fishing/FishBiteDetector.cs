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

    private GameObject fishInstance;
    private FishBlink fishBlink;

    public void StartDetection()
    {
        active = true;
        biteTimer = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        blinkTimer = blinkInterval;
        SpawnFish();
    }

    public void StopDetection()
    {
        active = false;
        DestroyFish();
    }

    public void ResetForNextFish()
    {
        active = true;
        biteTimer = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        blinkTimer = blinkInterval;
        // Keep the same fish swimming — it just escaped, it's still nearby
    }

    /// <summary>
    /// Called each frame by FishingController while waiting. proximityBonus is 0.0–0.2.
    /// </summary>
    public void Tick(float proximityBonus)
    {
        if (!active) return;

        // Keep fish swimming toward current bob X
        if (fishBlink != null && fishingLine != null)
        {
            fishBlink.SetTargetX(fishingLine.BobPosition.x);
            fishingLine.LastBlinkPosition = fishInstance.transform.position;
        }

        float speedMultiplier = 1f + proximityBonus;
        biteTimer -= Time.deltaTime * speedMultiplier;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            fishBlink?.Pulse();
            blinkTimer = blinkInterval + UnityEngine.Random.Range(-1f, 1f);
        }

        if (biteTimer <= 0f)
        {
            active = false;
            OnBite?.Invoke();
        }
    }

    private void SpawnFish()
    {
        if (fishBlinkPrefab == null || fishingLine == null) return;
        DestroyFish();

        Vector2 bobPos = fishingLine.BobPosition;

        // Spawn to one side of the bob at blinkRadius distance, same Y as the bob
        float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        Vector2 spawnPos = new Vector2(bobPos.x + side * blinkRadius, bobPos.y);

        fishInstance = Instantiate(fishBlinkPrefab, spawnPos, Quaternion.identity);
        fishBlink = fishInstance.GetComponent<FishBlink>();
        fishBlink?.SetTargetX(bobPos.x);
    }

    private void DestroyFish()
    {
        if (fishInstance != null)
        {
            Destroy(fishInstance);
            fishInstance = null;
            fishBlink = null;
        }
    }
}
