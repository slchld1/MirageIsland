using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the tension bar, reel bar, and reaction event prompts during the tug minigame.
/// Attach to a Canvas child. Assign all references in the Inspector.
/// </summary>
public class TugMinigameUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Bars")]
    [Tooltip("Image with Image Type = Filled, Fill Method = Vertical")]
    public Image tensionFill;
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal")]
    public Image reelFill;

    [Header("Tension Colors")]
    public Color safeColor    = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor  = Color.red;

    [Header("Event Prompts")]
    [Tooltip("GameObject shown during a Dart event. Should contain a Text child.")]
    public GameObject dartPrompt;
    [Tooltip("Text component inside dartPrompt that shows '← Q' or 'E →'")]
    public Text dartPromptText;
    [Tooltip("GameObject shown during a Tug event")]
    public GameObject tugPrompt;

    private TugMinigame tugMinigame;

    private void Awake()
    {
        tugMinigame = FindAnyObjectByType<TugMinigame>();
        if (tugMinigame == null) Debug.LogError("[TugMinigameUI] TugMinigame not found in scene.", this);
        if (panel != null) panel.SetActive(false);
    }

    public void Show() { if (panel != null) panel.SetActive(true); }
    public void Hide() { if (panel != null) panel.SetActive(false); }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (tugMinigame == null) return;

        // Tension bar
        float t = tugMinigame.Tension;
        if (tensionFill != null)
        {
            tensionFill.fillAmount = t;
            tensionFill.color = t > 0.8f ? dangerColor : t > 0.6f ? warningColor : safeColor;
        }

        // Reel bar
        if (reelFill != null)
            reelFill.fillAmount = tugMinigame.ReelProgress;

        // Event prompts
        bool isDart = tugMinigame.ActiveEvent == EventType.Dart;
        bool isTug  = tugMinigame.ActiveEvent == EventType.Tug;

        if (dartPrompt != null)
        {
            dartPrompt.SetActive(isDart);
            if (isDart)
            {
                if (dartPromptText != null)
                    dartPromptText.text = tugMinigame.ActiveEventDir == -1 ? "← Q" : "E →";
                else
                    Debug.LogWarning("[TugMinigameUI] dartPromptText is not assigned.", this);
            }
        }

        if (tugPrompt != null)
            tugPrompt.SetActive(isTug);
    }
}
