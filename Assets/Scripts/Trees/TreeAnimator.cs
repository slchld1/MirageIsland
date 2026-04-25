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
