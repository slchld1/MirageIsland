using System;
using System.Collections;
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
    public float nudgeSpeed = 1f;
    [Tooltip("How far the bobber can move from the player via Q/E nudge")]
    [SerializeField] private float maxNudgeDistance = 0.3f;

    [Header("Proximity")]
    [Tooltip("Max catch-rate bonus at closest range (e.g. 0.2 = +20% speed)")]
    public float maxProximityBonus = 0.2f;
    public float proximityRange = 2f;

    [Header("Rod Tip")]
    [Tooltip("The HoldPoint transform from HeldItemRenderer — line starts here + rod's tipOffset.")]
    public Transform holdPoint;
    [Tooltip("Fallback tip offset used when no rod is active. Also shown in the gizmo for tuning.")]
    public Vector2 tipOffset = new Vector2(0.2f, 0.3f);

    [Header("Bobber")]
    [Tooltip("Prefab with a SpriteRenderer — sits at the end of the line in the water")]
    public GameObject bobberPrefab;

    private LineRenderer     lineRenderer;
    private FishingController fishingController;
    private Vector2 castPoint;
    private Vector2 bobPosition;
    private GameObject bobberInstance;

    public Vector2 BobPosition => bobPosition;
    public bool NudgeEnabled { get; set; } = true;

    // Set externally by FishBiteDetector when a blink spawns
    public Vector2 LastBlinkPosition { get; set; }

    private void Awake()
    {
        lineRenderer      = GetComponent<LineRenderer>();
        fishingController = GetComponent<FishingController>();

        if (holdPoint == null)
        {
            HeldItemRenderer held = GetComponent<HeldItemRenderer>();
            if (held != null) holdPoint = held.holdPoint;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.enabled = false;
    }

    public bool IsCasting { get; private set; }

    /// <summary>
    /// Animates the bobber from the player to castTarget at the given speed,
    /// then calls onLanded. While casting, the line is drawn from player to
    /// the bobber's current mid-air position.
    /// </summary>
    public void Cast(Vector2 castTarget, float speed, Action onLanded)
    {
        castPoint    = castTarget;
        bobPosition  = RodTipPosition; // start at rod tip
        LastBlinkPosition = castTarget;
        lineRenderer.enabled = true;
        SpawnBobber();
        StartCoroutine(CastRoutine(castTarget, speed, onLanded));
    }

    private IEnumerator CastRoutine(Vector2 target, float speed, Action onLanded)
    {
        IsCasting = true;
        while (Vector2.Distance(bobPosition, target) > 0.01f)
        {
            bobPosition = Vector2.MoveTowards(bobPosition, target, speed * Time.deltaTime);
            UpdateLine();
            if (bobberInstance != null)
                bobberInstance.transform.position = bobPosition;
            yield return null;
        }
        bobPosition = target;
        UpdateLine();
        if (bobberInstance != null)
            bobberInstance.transform.position = bobPosition;
        IsCasting = false;
        onLanded?.Invoke();
    }

    public void Hide()
    {
        StopAllCoroutines();
        IsCasting = false;
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
            // Clamp so the bobber stays within maxNudgeDistance of the original cast point
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

    private Vector3 RodTipPosition
    {
        get
        {
            Vector3 origin = holdPoint != null ? holdPoint.position : transform.position;
            return origin + (Vector3)tipOffset;
        }
    }

    private void UpdateLine()
    {
        lineRenderer.SetPosition(0, RodTipPosition);
        lineRenderer.SetPosition(1, bobPosition);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(RodTipPosition, 0.05f);
    }

    public float GetProximityBonus()
    {
        float dist = Vector2.Distance(bobPosition, LastBlinkPosition);
        float t = 1f - Mathf.Clamp01(dist / proximityRange);
        return t * maxProximityBonus;
    }
}
