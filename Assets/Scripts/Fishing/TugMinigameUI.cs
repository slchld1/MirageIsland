using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the tension bar and reel progress bar during the tug minigame.
/// Attach to a Canvas child GameObject. Assign the panel, tension fill image, and reel fill image.
/// The panel is hidden by default and shown by FishingController during minigame.
/// </summary>
public class TugMinigameUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;
    [Tooltip("Image with Image Type = Filled, Fill Method = Vertical")]
    public Image tensionFill;
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal")]
    public Image reelFill;

    [Header("Tension Colors")]
    public Color safeColor    = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor  = Color.red;

    private TugMinigame tugMinigame;

    private void Awake()
    {
        tugMinigame = FindAnyObjectByType<TugMinigame>();
        panel.SetActive(false);
    }

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);

    private void Update()
    {
        if (!panel.activeSelf) return;

        float t = tugMinigame.Tension;
        tensionFill.fillAmount = t;

        if (t > 0.8f)      tensionFill.color = dangerColor;
        else if (t > 0.6f) tensionFill.color = warningColor;
        else               tensionFill.color = safeColor;

        reelFill.fillAmount = tugMinigame.ReelProgress;
    }
}
