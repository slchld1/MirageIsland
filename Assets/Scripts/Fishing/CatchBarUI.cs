using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays catch progress (0–1) during a fishing fight.
/// Attach to a child GameObject of the player alongside a vertical-fill Image.
/// </summary>
public class CatchBarUI : MonoBehaviour
{
    [Tooltip("Vertical fill image driven by catch progress (0–1).")]
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Called every frame while the fight is active. t = 0 (empty) to 1 (full).</summary>
    public virtual void OnProgressChanged(float t)
    {
        if (fillImage != null)
            fillImage.fillAmount = t;
    }
}