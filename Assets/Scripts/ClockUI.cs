using UnityEngine;
using TMPro;

public class ClockUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private TMP_Text phaseLabel;

    [Header("Update Interval")]
    [SerializeField] private float updateInterval = 5f;

    private float timer;

    private System.Collections.IEnumerator Start()
    {
        yield return null; // wait one frame for SaveController to load the time
        if (DayCycleManager.Instance == null) yield break;
        clockText.text = DayCycleManager.Instance.GetFormattedTime();
        if (phaseLabel != null)
            phaseLabel.text = DayCycleManager.Instance.CurrentPhase.ToString();
    }

    private void Update()
    {
        if (DayCycleManager.Instance == null) return;

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            clockText.text = DayCycleManager.Instance.GetFormattedTime();

            if (phaseLabel != null)
                phaseLabel.text = DayCycleManager.Instance.CurrentPhase.ToString();
        }
    }

    // Call these from Button OnClick events in the Inspector
    public void DebugSetNight()   => DayCycleManager.Instance?.SetTime(21f);
    public void DebugSetDawn()    => DayCycleManager.Instance?.SetTime(6f);
    public void DebugSetDay()     => DayCycleManager.Instance?.SetTime(10f);
    public void DebugSetDusk()    => DayCycleManager.Instance?.SetTime(18f);
}
