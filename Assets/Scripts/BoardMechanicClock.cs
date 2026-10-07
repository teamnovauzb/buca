using UnityEngine;

/// <summary>Shared board-motion time, excluding pauses in the tutorial player.</summary>
public static class BoardMechanicClock
{
    static bool paused;
    static float pausedAt, pausedDuration;
    public static float Time => (paused ? pausedAt : UnityEngine.Time.time) - pausedDuration;

    public static void SetPaused(bool value)
    {
        if(value==paused)return;
        if(value)pausedAt=UnityEngine.Time.time;
        else pausedDuration+=UnityEngine.Time.time-pausedAt;
        paused=value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){paused=false;pausedAt=pausedDuration=0;}
}
