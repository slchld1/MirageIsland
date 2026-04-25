using System;
using System.Collections;
using UnityEngine;

public class TreeAnimator : MonoBehaviour
{
    [Header("Refs (assign in prefab)")]
    public Transform topTransform;
    public SpriteRenderer topRenderer;
    public SpriteRenderer fruitRenderer;

    [Header("Shake")]
    public float shakeDuration = 0.10f;
    public float shakeAmplitude = 0.05f;

    [Header("Fall")]
    public float fallRotateDuration = 0.50f;
    public float fallImpactFraction = 0.35f;
    public float fallLieDuration = 0.20f;
    public float fallFadeDuration = 0.30f;

    private Coroutine shakeRoutine;
    private Vector3 topBaseLocalPos;

    private void Awake()
    {
        if (topTransform != null) topBaseLocalPos = topTransform.localPosition;
    }

    public void PlayShake()
    {
        //Will be called every Chop
        if (topTransform == null) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    public void PlayFell(int fallDir, Action onImpact, Action onComplete)
    {
        StartCoroutine(FellRoutine(fallDir, onImpact, onComplete));
    }

    private IEnumerator FellRoutine(int fallDir, Action onImpact, Action onComplete)
    {
        if (topTransform == null) { onComplete?.Invoke(); yield break; }

        float targetZ = (fallDir == -1) ? +90f : -90f;
        Vector3 startEuler = topTransform.localEulerAngles;
        bool impactFired = false;

        // Phase 1: rotate
        float elapsed = 0f;
        
        while (elapsed < fallRotateDuration)
        {
            float t = elapsed / fallRotateDuration;
            float eased = t * t; //easeInQuad
            topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, Mathf.Lerp(0f, targetZ, eased));

            if (!impactFired && t >= fallImpactFraction)
            {
                impactFired = true;
                onImpact?.Invoke();
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, targetZ);
        if (!impactFired) onImpact?.Invoke(); // safety net

        // Phase 2: lie still
        yield return new WaitForSeconds(fallLieDuration);

        //Phase 3: fade
        Color topStartColor = topRenderer != null ? topRenderer.color : Color.white;
        Color fruitStartColor = fruitRenderer != null ? fruitRenderer.color : Color.white;
        elapsed = 0f;
        while (elapsed < fallFadeDuration)
        {
            float t = elapsed / fallFadeDuration;
            float a = Mathf.Lerp(1f, 0f, t);
            if (topRenderer != null)
            {
                Color c = topStartColor; c.a = a; topRenderer.color = c;
            }
            if (fruitRenderer != null && fruitRenderer.enabled)
            {
                Color c = fruitStartColor; c.a = a; fruitRenderer.color = c;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (topRenderer != null)
        {
            topRenderer.enabled = false;
            Color c = topStartColor; c.a = 1f; topRenderer.color = c;
        }
        if (fruitRenderer != null)
        {
            fruitRenderer.enabled = false;
            Color c = fruitStartColor; c.a = 1f; fruitRenderer.color = c;
        }
        topTransform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, 0f);

        onComplete?.Invoke();
    }
    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * shakeAmplitude;
            topTransform.localPosition = topBaseLocalPos + new Vector3(jitter.x, jitter.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        topTransform.localPosition = topBaseLocalPos;
        shakeRoutine = null;
    }
}
