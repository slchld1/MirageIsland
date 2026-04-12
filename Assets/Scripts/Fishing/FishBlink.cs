using System.Collections;
using UnityEngine;

/// <summary>
/// A persistent fish that swims left/right only toward a target X position.
/// Stays invisible most of the time and briefly pulses visible on command.
/// Managed entirely by FishBiteDetector — do not destroy manually.
/// Requires a SpriteRenderer on the same GameObject.
/// </summary>
public class FishBlink : MonoBehaviour
{
    [Tooltip("How fast the fish swims horizontally toward the bob")]
    public float swimSpeed = 0.8f;

    private SpriteRenderer sr;
    private float targetX;
    private bool pulsing;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(0f); // invisible until pulsed
    }

    /// <summary>
    /// Called each frame by FishBiteDetector to update where the fish is swimming toward.
    /// </summary>
    public void SetTargetX(float x)
    {
        float dir = x - transform.position.x;
        if (Mathf.Abs(dir) > 0.01f)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir > 0f ? 1f : -1f);
            transform.localScale = s;
        }
        targetX = x;
    }

    private void Update()
    {
        // Only move horizontally — Y never changes
        float newX = Mathf.MoveTowards(transform.position.x, targetX, swimSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
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
