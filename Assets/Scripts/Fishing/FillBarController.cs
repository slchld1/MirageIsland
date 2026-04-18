using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a polished fill bar from a single NormalizedValue (0-1).
/// Attach to TensionBar_Panel. Wire all references in the Inspector.
/// </summary>
public class FillBarController : MonoBehaviour
{
    [Header("References")]
    public RectTransform fillBase;
    public Image         fillDanger;
    public RectTransform indicator;
    public RectTransform dangerZoneMarker;

    [Header("Settings")]
    [Tooltip("Visual threshold (0-1) where the red danger overlay begins to appear")]
    public float dangerThreshold = 0.75f;

    /// <summary>Single input. Set this each frame from your gameplay script (0-1).</summary>
    public float NormalizedValue { get; set; }

    private float barWidth;
    private float indicatorX;

    private void Start()
    {
        // barWidth comes from Fill_Container (fillBase's parent)
        barWidth   = ((RectTransform)fillBase.parent).rect.width;
        indicatorX = 0f;

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
            float alpha = NormalizedValue < dangerThreshold
                ? 0f
                : (NormalizedValue - dangerThreshold) / (1f - dangerThreshold);
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
