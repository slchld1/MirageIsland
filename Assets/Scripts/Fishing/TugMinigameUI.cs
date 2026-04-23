using System.Collections;
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

    [Header("Follow Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector2 headOffset = new Vector2(0f, 1.5f);

    [Header("Bars")]
    [Tooltip("FillBarController on the TensionBar_Panel prefab")]
    [SerializeField] private FillBarController tensionBar;
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal")]
    public Image reelFill;

    [Header("Event Prompts")]
    [Tooltip("GameObject shown during a Dart event. Should contain a Text child.")]
    public GameObject dartPrompt;
    [Tooltip("Text component inside dartPrompt that shows '← Q' or 'E →'")]
    public Text dartPromptText;
    [Tooltip("GameObject shown during a Tug event")]
    public GameObject tugPrompt;
    [Tooltip("Text component inside tugPrompt")]
    public Text tugPromptText;

    [Header("Fail Flash")]
    public Color failColor = Color.red;
    public float failFlashDuration = 0.5f;

    private TugMinigame tugMinigame;
    private bool flashingDart;
    private bool flashingTug;
    private Canvas rootCanvas;
    private RectTransform panelRect;

    private void Awake()
    {
        tugMinigame = FindAnyObjectByType<TugMinigame>();
        if (tugMinigame == null) Debug.LogError("[TugMinigameUI] TugMinigame not found in scene.", this);
        if (panel != null)
        {
            panel.SetActive(false);
            panelRect = panel.GetComponent<RectTransform>();
        }
        rootCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (tugMinigame != null)
            tugMinigame.OnEventFailed += OnEventFailed;
    }

    private void OnDestroy()
    {
        if (tugMinigame != null)
            tugMinigame.OnEventFailed -= OnEventFailed;
    }

    public void Show() { if (panel != null) panel.SetActive(true); }
    public void Hide() { if (panel != null) panel.SetActive(false); }

    private void PositionPanel()
    {
        if (playerTransform == null || panelRect == null || rootCanvas == null) return;
        Vector3 worldPos = playerTransform.position + (Vector3)(headOffset);
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.GetComponent<RectTransform>(), screenPos, uiCam, out Vector2 localPoint))
            panelRect.localPosition = localPoint;
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (tugMinigame == null) return;

        PositionPanel();

        // Tension bar
        if (tensionBar != null)
            tensionBar.NormalizedValue = tugMinigame.Tension;

        // Reel bar
        if (reelFill != null)
            reelFill.fillAmount = tugMinigame.ReelProgress;

        // Event prompts — keep visible while flashing
        bool isDart = tugMinigame.ActiveEvent == EventType.Dart;
        bool isTug  = tugMinigame.ActiveEvent == EventType.Tug;

        if (dartPrompt != null)
        {
            dartPrompt.SetActive(isDart || flashingDart);
            if (isDart && dartPromptText != null)
                dartPromptText.text = tugMinigame.ActiveEventDir == -1 ? "← Q" : "E →";
        }

        if (tugPrompt != null)
            tugPrompt.SetActive(isTug || flashingTug);
    }

    private void OnEventFailed(EventType type)
    {
        if (type == EventType.Dart && dartPrompt != null)
            StartCoroutine(FlashFailed(type, dartPrompt, dartPromptText));
        else if (type == EventType.Tug && tugPrompt != null)
            StartCoroutine(FlashFailed(type, tugPrompt, tugPromptText));
    }

    private IEnumerator FlashFailed(EventType type, GameObject prompt, Text label)
    {
        if (type == EventType.Dart) flashingDart = true;
        else                        flashingTug  = true;

        prompt.SetActive(true);

        Color original = label != null ? label.color : Color.white;
        float timer = 0f;

        while (timer < failFlashDuration)
        {
            timer += Time.deltaTime;
            if (label != null)
                label.color = Color.Lerp(failColor, original, timer / failFlashDuration);
            yield return null;
        }

        if (label != null)
            label.color = original;

        if (type == EventType.Dart) flashingDart = false;
        else                        flashingTug  = false;
    }
}
