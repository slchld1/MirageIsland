using System.Collections;
using UnityEngine;

/// <summary>
/// A persistent fish that swims left/right only toward a target X position.
/// Stays invisible most of the time and briefly pulses visible on command.
/// Managed entirely by FishBiteDetector — do not destroy manually.
/// Requires a SpriteRenderer on the same GameObject.
/// </summary>
public class Fish : MonoBehaviour
{
    [Tooltip("Slowest swim speed when far from the bob")]
    public float minSwimSpeed = 0.5f;
    [Tooltip("Fastest swim speed when close to the bob")]
    public float maxSwimSpeed = 2.5f;
    [Tooltip("Distance at which the fish reaches max speed")]
    public float approachDistance = 2f;

    private SpriteRenderer sr;
    private float targetX;
    private float targetY;
    private bool pulsing;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(0f); // invisible until pulsed
    }

    /// <summary>
    /// Called each frame by FishBiteDetector to update where the fish is swimming toward.
    /// </summary>
    public void SetTarget(Vector2 target)
    {
        float dir = target.x - transform.position.x;
        if (Mathf.Abs(dir) > 0.01f)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir > 0f ? 1f : -1f);
            transform.localScale = s;
        }
        targetX = target.x;
        targetY = target.y;
    }

    // Keep old method so nothing else breaks
    public void SetTargetX(float x) => SetTarget(new Vector2(x, targetY));

    private void Update()
    {
        // Accelerate as the fish closes in on the bob — slow approach, fast commit
        float dist = Vector2.Distance(transform.position, new Vector2(targetX, targetY));
        float t = 1f - Mathf.Clamp01(dist / approachDistance);
        float speed = Mathf.Lerp(minSwimSpeed, maxSwimSpeed, t);

        Vector2 newPos = Vector2.MoveTowards(transform.position, new Vector2(targetX, targetY), speed * Time.deltaTime);
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
    }

    /// <summary>
    /// Briefly flashes the fish visible then invisible. Safe to call while already pulsing.
    /// </summary>
    public void Pulse(float duration = 0.8f)
    {
        if (!pulsing)
            StartCoroutine(PulseRoutine(duration));
    }

    private IEnumerator PulseRoutine(float duration)
    {
        pulsing = true;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            SetAlpha(t < 0.5f ? t * 2f : (1f - t) * 2f);
            yield return null;
        }
        SetAlpha(0f);
        pulsing = false;
    }

    private void SetAlpha(float a)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
