using UnityEngine;

/// <summary>Shared board-motion time, excluding pauses in the tutorial player.</summary>
public static class BoardMechanicClock
{
    static bool paused;
    static float pausedAt, pausedDuration;
    static bool playback;
    static float playbackStart, playbackTime;
    public static float Time => playback ? playbackStart + playbackTime : (paused ? pausedAt : UnityEngine.Time.time) - pausedDuration;

    public static void BeginPlayback()
    {
        playbackStart = Time;
        playbackTime = 0;
        playback = true;
    }
    public static void SetPlaybackTime(float seconds)
    {
        if (playback) playbackTime = Mathf.Max(0, seconds);
    }
    public static void EndPlayback()
    {
        if (!playback) return;
        float end = Time;
        playback = false;
        paused = false;
        pausedDuration = UnityEngine.Time.time - end;
    }

    public static void SetPaused(bool value)
    {
        if(value==paused)return;
        if(value)pausedAt=UnityEngine.Time.time;
        else pausedDuration+=UnityEngine.Time.time-pausedAt;
        paused=value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){paused=playback=false;pausedAt=pausedDuration=playbackStart=playbackTime=0;}
}
