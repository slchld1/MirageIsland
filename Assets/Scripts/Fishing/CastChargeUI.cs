using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays cast charge level. Override OnChargeChanged / Show / Hide to swap visuals.
/// Attach to a child GameObject of the player alongside your chosen UI elements.
/// </summary>
public class CastChargeUI : MonoBehaviour
{
    [Tooltip("Horizontal fill image driven by charge level (0–1). Optional — leave null if using a custom subclass.")]
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

    /// <summary>Called every frame while charging. t = 0 (empty) to 1 (full).</summary>
    public virtual void OnChargeChanged(float t)
    {
        if (fillImage != null)
            fillImage.fillAmount = t;
    }
}
