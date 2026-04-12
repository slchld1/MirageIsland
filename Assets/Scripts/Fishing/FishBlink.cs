using UnityEngine;

/// <summary>
/// Spawns away from the bob and swims toward it, fading in then out.
/// Call Initialize() immediately after Instantiate to set the target.
/// Requires a SpriteRenderer on the same GameObject.
/// </summary>
public class FishBlink : MonoBehaviour
{
    public float duration = 1.2f;
    public float swimSpeed = 1.5f;

    private SpriteRenderer sr;
    private Vector2 target;
    private float timer;
    private bool initialized;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        timer = 0f;
    }

    /// <summary>
    /// Call right after Instantiate. Sets the bob position the fish swims toward.
    /// </summary>
    public void Initialize(Vector2 bobPosition)
    {
        target = bobPosition;
        initialized = true;

        // Flip sprite to face the direction of travel
        float direction = bobPosition.x - transform.position.x;
        if (direction != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction > 0f ? 1f : -1f);
            transform.localScale = scale;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        // Fade in first half, fade out second half
        float alpha = t < 0.5f ? t * 2f : (1f - t) * 2f;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        // Swim toward bob
        if (initialized)
            transform.position = Vector2.MoveTowards(transform.position, target, swimSpeed * Time.deltaTime);

        if (timer >= duration)
            Destroy(gameObject);
    }
}
