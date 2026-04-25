using System;
using UnityEngine;

public enum TimeOfDay { Night, PreDawn, Dawn, Day, Dusk }

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    [Header("Time Settings")]
    [Tooltip("How many real seconds equal one full 24-hour in-game day. Default = 1440 (24 real minutes).")]
    [SerializeField] private float realSecondsPerDay = 1440f;
    [SerializeField] private float startHour = 5f;

    // Fired when the phase changes (e.g. Day → Dusk)
    public static event Action<TimeOfDay> OnPhaseChanged;
    [Header("GetCurrent")]
    public float CurrentHour { get; private set; }
    public TimeOfDay CurrentPhase { get; private set; }
    public int CurrentDay { get; private set; }
    public float TotalHours => CurrentDay * 24f + CurrentHour;


    private float elapsed;
    private TimeOfDay lastPhase;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Only set default time if SaveController hasn't already set it via LoadGame
        if (elapsed == 0f) SetTime(startHour);
    }

    private void Update()
    {
        if (PauseController.IsGamePaused) return;

        elapsed += Time.deltaTime;
        if (elapsed >= realSecondsPerDay)
        {
            elapsed -= realSecondsPerDay;
            CurrentDay++;
        }
        
        

        CurrentHour = (elapsed / realSecondsPerDay) * 24f;

        TimeOfDay newPhase = GetPhase(CurrentHour);
        if (newPhase != lastPhase)
        {
            CurrentPhase = newPhase;
            lastPhase = newPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }

    // Sets the clock to a specific hour (0-24) and syncs elapsed time
    public void SetTime(float hour)
    {
        elapsed = (hour / 24f) * realSecondsPerDay;
        CurrentHour = hour;
        CurrentPhase = GetPhase(hour);
        lastPhase = CurrentPhase;
    }
    public void SetTime(float hour, int day)
    {
        SetTime(hour);
        CurrentDay = day;
    }

    // Returns 0-1 progress through the current phase (useful for smooth transitions)
    public float GetPhaseProgress()
    {
        return CurrentPhase switch
        {
            TimeOfDay.PreDawn => Mathf.InverseLerp(5f, 6f, CurrentHour),
            TimeOfDay.Dawn    => Mathf.InverseLerp(6f, 8f, CurrentHour),
            TimeOfDay.Day     => Mathf.InverseLerp(8f, 18f, CurrentHour),
            TimeOfDay.Dusk    => Mathf.InverseLerp(18f, 20f, CurrentHour),
            TimeOfDay.Night   => CurrentHour >= 20f
                                    ? Mathf.InverseLerp(20f, 24f, CurrentHour) * 0.5f
                                    : 0.5f + Mathf.InverseLerp(0f, 5f, CurrentHour) * 0.5f,
            _                 => 0f
        };
    }

    public string GetFormattedTime()
    {
        int hours = (int)CurrentHour;
        int minutes = (int)((CurrentHour - hours) * 60f);
        string period = hours < 12 ? "AM" : "PM";
        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;
        return $"{displayHour}:{minutes:D2} {period}";
    }

    public static TimeOfDay GetPhase(float hour)
    {
        if (hour >= 5f && hour < 6f)  return TimeOfDay.PreDawn;
        if (hour >= 6f && hour < 8f)  return TimeOfDay.Dawn;
        if (hour >= 8f && hour < 18f) return TimeOfDay.Day;
        if (hour >= 18f && hour < 20f) return TimeOfDay.Dusk;
        return TimeOfDay.Night;
    }
}
