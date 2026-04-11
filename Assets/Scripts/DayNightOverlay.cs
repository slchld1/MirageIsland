using UnityEngine;
using UnityEngine.UI;

// Attach to a full-screen Canvas Image that sits above the world but below the HUD.
// Set the Image raycast target to OFF.
public class DayNightOverlay : MonoBehaviour
{
    [Header("Overlay Image")]
    [SerializeField] private Image overlayImage;

    [Header("Phase Colours")]
    [SerializeField] private Color nightColor   = new Color(0.05f, 0.05f, 0.25f, 0.75f);
    [SerializeField] private Color preDawnColor = new Color(0.15f, 0.05f, 0.3f,  0.45f);
    [SerializeField] private Color dawnColor    = new Color(1f,    0.5f,  0.2f,  0.2f);
    [SerializeField] private Color dayColor     = new Color(1f,    1f,    1f,    0f);
    [SerializeField] private Color duskColor    = new Color(1f,    0.35f, 0.05f, 0.3f);

    [Header("Dusk Warning Pulse")]
    [SerializeField] private Image duskWarningImage;   // separate UI element, e.g. a vignette border
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMinAlpha = 0f;
    [SerializeField] private float pulseMaxAlpha = 0.4f;

    private void Update()
    {
        if (DayCycleManager.Instance == null) return;

        float hour = DayCycleManager.Instance.CurrentHour;

        overlayImage.color = GetOverlayColor(hour);

        UpdateDuskPulse(hour);
    }

    private Color GetOverlayColor(float hour)
    {
        // Midnight → Pre-dawn: full night
        if (hour < 5f)
            return nightColor;
        // Pre-dawn → Dawn
        if (hour < 6f)
            return Color.Lerp(nightColor, dawnColor, Mathf.InverseLerp(5f, 6f, hour));
        // Dawn → Day
        if (hour < 8f)
            return Color.Lerp(dawnColor, dayColor, Mathf.InverseLerp(6f, 8f, hour));
        // Day
        if (hour < 18f)
            return dayColor;
        // Day → Dusk
        if (hour < 20f)
            return Color.Lerp(dayColor, duskColor, Mathf.InverseLerp(18f, 20f, hour));
        // Dusk → Night (2-hour transition)
        if (hour < 22f)
            return Color.Lerp(duskColor, nightColor, Mathf.InverseLerp(20f, 22f, hour));
        // Full night
        return nightColor;
    }

    private void UpdateDuskPulse(float hour)
    {
        if (duskWarningImage == null) return;

        bool isDusk = hour >= 18f && hour < 20f;
        duskWarningImage.gameObject.SetActive(isDusk);

        if (isDusk)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            Color c = duskWarningImage.color;
            c.a = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, pulse);
            duskWarningImage.color = c;
        }
    }
}
