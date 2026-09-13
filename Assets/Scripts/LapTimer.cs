using UnityEngine;
using UnityEngine.Events;

public class LapTimer : MonoBehaviour
{
    [Header("Settings")]
    public int totalLaps = 3;
    public float minLapTime = 15f; // Must race at least 15s to complete a lap (prevents start line re-trigger)

    [Header("Events")]
    public UnityEvent<int, float> onLapCompleted; // lap number, lap time
    public UnityEvent onRaceFinished;

    public int   CurrentLap      { get; private set; } = 1;
    public float CurrentLapTime  { get; private set; } = 0f;
    public float BestLapTime     { get; private set; } = float.MaxValue;
    public float TotalRaceTime   { get; private set; } = 0f;
    public bool  RaceStarted     { get; private set; } = false;
    public bool  RaceFinished    { get; private set; } = false;

    private const string carTag = "PlayerCar";

    public void StartRace(int laps)
    {
        totalLaps = laps;
        CurrentLap = 1;
        CurrentLapTime = 0f;
        TotalRaceTime = 0f;
        BestLapTime = float.MaxValue;
        RaceStarted = true;
        RaceFinished = false;
        Debug.Log($"[LapTimer] Race Started with {totalLaps} Laps!");
    }

    void Update()
    {
        if (RaceStarted && !RaceFinished)
        {
            CurrentLapTime += Time.deltaTime;
            TotalRaceTime  += Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!RaceStarted || RaceFinished) return;

        // Check player identification
        bool isPlayer = other.CompareTag(carTag) ||
                        (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(carTag)) ||
                        other.GetComponentInParent<CarController>() != null;

        if (!isPlayer) return;

        // Must have been racing for at least minLapTime (avoids triggering while crossing start line at release)
        if (CurrentLapTime < minLapTime) return;

        // Lap completed!
        float completedTime = CurrentLapTime;
        if (completedTime < BestLapTime) BestLapTime = completedTime;

        onLapCompleted?.Invoke(CurrentLap, completedTime);
        Debug.Log($"[LapTimer] Lap {CurrentLap} / {totalLaps} finished in {FormatTime(completedTime)}");

        CurrentLap++;
        CurrentLapTime = 0f;

        // Check if finished
        if (CurrentLap > totalLaps)
        {
            RaceFinished = true;
            CurrentLap = totalLaps;
            onRaceFinished?.Invoke();
            Debug.Log($"[LapTimer] RACE FINISHED! Total: {FormatTime(TotalRaceTime)}, Best: {FormatTime(BestLapTime)}");
        }
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return "00:00.000";
        int m  = (int)(seconds / 60f);
        int s  = (int)(seconds % 60f);
        int ms = (int)((seconds - Mathf.Floor(seconds)) * 1000f);
        return $"{m:00}:{s:00}.{ms:000}";
    }
}
