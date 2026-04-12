using UnityEngine;

/// <summary>
/// Spawned near the bob. Fades in and out briefly to hint at fish presence, then destroys itself.
/// Requires a SpriteRenderer on the same GameObject.
/// </summary>
public class FishBlink : MonoBehaviour
{
    public float duration = 0.6f;

    private SpriteRenderer sr;
    private float timer;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;
        float alpha = t < 0.5f ? t * 2f : (1f - t) * 2f;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;

        if (timer >= duration)
            Destroy(gameObject);
    }
}
