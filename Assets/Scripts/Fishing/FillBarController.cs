using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a polished fill bar from a single NormalizedValue (0-1).
/// Attach to TensionBar_Panel. Wire all references in the Inspector.
/// </summary>
public class FillBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform fillBase;
    [SerializeField] private Image         fillDanger;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private RectTransform dangerZoneMarker;

    [Header("Settings")]
    [Tooltip("Visual threshold (0-1) where the red danger overlay begins to appear")]
    public float dangerThreshold = 0.75f;

    /// <summary>Single input. Set this each frame from your gameplay script (0-1).</summary>
    private float _normalizedValue;

    public float NormalizedValue
    {
        get => _normalizedValue;
        set => _normalizedValue = Mathf.Clamp01(value);
    }

    private float barWidth;
    private float indicatorX;

    private void Start()
    {
        if (fillBase == null)
        {
            Debug.LogError("[FillBarController] fillBase is not assigned.", this);
            enabled = false;
            return;
        }

        // barWidth comes from Fill_Container (fillBase's parent)
        barWidth   = ((RectTransform)fillBase.parent).rect.width;
        indicatorX = NormalizedValue * barWidth;

        // Position danger zone marker once — it never moves after this
        if (dangerZoneMarker != null)
        {
            Vector2 pos = dangerZoneMarker.anchoredPosition;
            pos.x = dangerThreshold * barWidth;
            dangerZoneMarker.anchoredPosition = pos;
        }
    }

    private void Update()
    {
        float targetWidth = NormalizedValue * barWidth;

        // Fill_Base — width tracks NormalizedValue directly
        fillBase.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

        // Fill_Danger — alpha lerps 0→1 above dangerThreshold
        if (fillDanger != null)
        {
            float range = 1f - dangerThreshold;
            float alpha = (range > 0f && NormalizedValue >= dangerThreshold)
                ? (NormalizedValue - dangerThreshold) / range
                : 0f;
            Color c = fillDanger.color;
            c.a = alpha;
            fillDanger.color = c;
        }

        // Indicator — snappy-elastic follow of the fill's leading edge
        indicatorX = Mathf.Lerp(indicatorX, targetWidth, 12f * Time.deltaTime);
        if (indicator != null)
        {
            Vector2 pos = indicator.anchoredPosition;
            pos.x = indicatorX;
            indicator.anchoredPosition = pos;
        }
    }
}
