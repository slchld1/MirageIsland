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

    [Header("Water")]
    [Tooltip("Padding from all edges of the water collider — fish will only spawn inside this boundary")]
    public float shoreMargin = 1f;

    [Header("Fish Blink")]
    public GameObject fishBlinkPrefab;
    public float blinkInterval = 4f;
    [Tooltip("How far away the fish spawns normally")]
    public float spawnRadius = 5f;
    [Tooltip("Close spawn distance — only used when closeSpawnChance triggers")]
    public float blinkRadius = 1.5f;
    [Tooltip("0–1 chance the fish spawns close to the bob instead of far away")]
    public float closeSpawnChance = 0.1f;
    [Tooltip("Seconds after cast before the fish first appears")]
    public float firstSpawnDelay = 2f;

    public event Action OnBite;

    private FishingLine fishingLine;
    private LayerMask   waterLayer;
    private float biteTimer;
    private float blinkTimer;
    private float spawnDelayTimer;
    private bool active;

    private void Awake()
    {
        fishingLine = GetComponent<FishingLine>();
        var controller = GetComponent<FishingController>();
        if (controller != null) waterLayer = controller.waterLayer;
    }

    private GameObject fishInstance;
    private FishBlink fishBlink;

    public void StartDetection()
    {
        active = true;
        biteTimer = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        blinkTimer = blinkInterval;
        spawnDelayTimer = firstSpawnDelay;
        DestroyFish(); // clear any leftover fish
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

        // Wait before spawning fish — gives the feel of a fish noticing the bobber and approaching
        if (fishInstance == null)
        {
            spawnDelayTimer -= Time.deltaTime;
            if (spawnDelayTimer <= 0f)
                SpawnFish();
            else
                return; // don't tick bite or blink until fish is in the water
        }

        // Keep fish swimming toward current bob position
        if (fishBlink != null && fishingLine != null)
        {
            fishBlink.SetTarget(fishingLine.BobPosition);
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

        float dist = UnityEngine.Random.value < closeSpawnChance ? blinkRadius : spawnRadius;
        Collider2D waterCol = Physics2D.OverlapPoint(bobPos, waterLayer);

        Vector2 spawnPos = bobPos;
        Vector2 bestFallback = bobPos;
        bool foundIdeal = false;

        for (int i = 0; i < 16; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 candidate = new Vector2(
                bobPos.x + Mathf.Cos(angle) * dist,
                bobPos.y + Mathf.Sin(angle) * dist
            );

            if (waterCol == null || !waterCol.OverlapPoint(candidate)) continue;

            // Save any in-water position as a fallback
            bestFallback = candidate;

            // Ideal: also inside the shoreMargin boundary
            Bounds b = waterCol.bounds;
            bool safeX = candidate.x > b.min.x + shoreMargin && candidate.x < b.max.x - shoreMargin;
            bool safeY = candidate.y > b.min.y + shoreMargin && candidate.y < b.max.y - shoreMargin;
            if (safeX && safeY) { spawnPos = candidate; foundIdeal = true; break; }
        }

        if (!foundIdeal) spawnPos = bestFallback;

        fishInstance = Instantiate(fishBlinkPrefab, spawnPos, Quaternion.identity);
        fishBlink = fishInstance.GetComponent<FishBlink>();
        fishBlink?.SetTarget(bobPos);
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
