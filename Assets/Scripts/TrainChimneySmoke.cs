using UnityEngine;

/// <summary>One chimney puff at each chuff in the playing train sound.</summary>
public sealed class TrainChimneySmoke : MonoBehaviour
{
    public const int ChuffsPerLoop = 4;
    public ParticleSystem plume;
    AudioSource trackedSource;
    AudioClip trackedClip;
    int previousSample = -1;

    void OnEnable()
    {
        ResetClock();
        if (plume == null) return;
        var emission = plume.emission;
        emission.enabled = false;
        plume.Clear();
    }

    void OnDisable()
    {
        ResetClock();
        if (plume != null) plume.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void ResetClock()
    {
        trackedSource = null; trackedClip = null; previousSample = -1;
    }

    public void Tick(float deltaTime, AudioSource source, AudioClip chug, bool restarted)
    {
        if (plume == null || !plume.gameObject.activeInHierarchy) { ResetClock(); return; }
        if (!Application.isPlaying && deltaTime > 0)
            plume.Simulate(deltaTime, false, false, false);
        if (source == null || chug == null || source.clip != chug || !source.isPlaying)
        {
            // Existing puffs finish rising; no new puffs during a station stop.
            ResetClock(); return;
        }

        int sample = Mathf.Clamp(source.timeSamples, 0, chug.samples - 1);
        int beat = (int)((long)sample * ChuffsPerLoop / chug.samples);
        int count;
        if (restarted || trackedSource != source || trackedClip != chug || previousSample < 0)
            count = beat + 1; // Includes the first chuk at sample zero.
        else
        {
            int previousBeat = (int)((long)previousSample * ChuffsPerLoop / chug.samples);
            count = beat - previousBeat;
            if (sample < previousSample) count += ChuffsPerLoop;
        }
        // Follow the real audio playhead, including pitch changes and loop wrapping.
        // No independent emission timer can drift away from the audible chuks.
        if (count > 0)
        {
            if (Application.isPlaying && !plume.isPlaying) plume.Play(false);
            plume.Emit(count);
        }
        previousSample = sample; trackedSource = source; trackedClip = chug;
    }
}
