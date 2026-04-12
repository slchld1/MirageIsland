using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Draws a LineRenderer from the player to the cast point.
/// Q/E nudge moves the bob left/right. Tracks proximity to the last fish blink.
/// Attach to the player GameObject alongside a LineRenderer component.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class FishingLine : MonoBehaviour
{
    [Header("Nudge")]
    public float nudgeSpeed = 2f;
    public float maxNudgeDistance = 3f;

    [Header("Proximity")]
    [Tooltip("Max catch-rate bonus at closest range (e.g. 0.2 = +20% speed)")]
    public float maxProximityBonus = 0.2f;
    public float proximityRange = 2f;

    [Header("Bobber")]
    [Tooltip("Prefab with a SpriteRenderer — sits at the end of the line in the water")]
    public GameObject bobberPrefab;

    private LineRenderer lineRenderer;
    private Vector2 castPoint;
    private Vector2 bobPosition;
    private GameObject bobberInstance;

    public Vector2 BobPosition => bobPosition;
    public bool NudgeEnabled { get; set; } = true;

    // Set externally by FishBiteDetector when a blink spawns
    public Vector2 LastBlinkPosition { get; set; }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.enabled = false;
    }

    public void Initialize(Vector2 castTarget)
    {
        castPoint = castTarget;
        bobPosition = castTarget;
        LastBlinkPosition = castTarget;
        lineRenderer.enabled = true;
        UpdateLine();
        SpawnBobber();
    }

    public void Hide()
    {
        lineRenderer.enabled = false;
        if (bobberInstance != null)
        {
            Destroy(bobberInstance);
            bobberInstance = null;
        }
    }

    private void Update()
    {
        if (!lineRenderer.enabled) return;

        float nudge = 0f;
        if (NudgeEnabled)
        {
            if (Keyboard.current.qKey.isPressed) nudge = -1f;
            else if (Keyboard.current.eKey.isPressed) nudge = 1f;
        }

        if (nudge != 0f)
        {
            bobPosition += Vector2.right * nudge * nudgeSpeed * Time.deltaTime;
            float offset = Mathf.Clamp(bobPosition.x - castPoint.x, -maxNudgeDistance, maxNudgeDistance);
            bobPosition = new Vector2(castPoint.x + offset, castPoint.y);
            UpdateLine();
        }

        // Keep bobber sitting on the bob position
        if (bobberInstance != null)
            bobberInstance.transform.position = bobPosition;
    }

    private void SpawnBobber()
    {
        if (bobberPrefab == null) return;
        if (bobberInstance != null) Destroy(bobberInstance);
        bobberInstance = Instantiate(bobberPrefab, bobPosition, Quaternion.identity);
    }

    private void UpdateLine()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, bobPosition);
    }

    public float GetProximityBonus()
    {
        float dist = Vector2.Distance(bobPosition, LastBlinkPosition);
        float t = 1f - Mathf.Clamp01(dist / proximityRange);
        return t * maxProximityBonus;
    }
}
