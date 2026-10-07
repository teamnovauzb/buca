using UnityEngine;

/// <summary>Local narration shared by picture lessons and the saved animated demonstrations.</summary>
public sealed class TutorialVoice : MonoBehaviour
{
    AudioSource _voice;
    public static TutorialVoice For(GameObject owner)
    {
        var voice = owner.GetComponent<TutorialVoice>();
        return voice;
    }

    public void Play(string key)
    {
        if (_voice == null) _voice=GetComponent<AudioSource>();
        if (_voice == null) return;
        _voice.Stop();
        string name = key == "+PTS" ? "POINTS" : key == "!" ? "HAZARD" : key;
        _voice.clip = Resources.Load<AudioClip>("Audio/TutorialVoice/" + name);
        ApplyVolume();
        if (_voice.clip != null) _voice.Play();
    }

    void ApplyVolume()
    {
        if (_voice == null) return;
        var audio = AudioManager.Instance;
        _voice.volume = audio != null ? audio.masterVolume * audio.sfxVolume : 1f;
        _voice.outputAudioMixerGroup = audio != null ? audio.sfxGroup : null;
    }
    void Update() { ApplyVolume(); }
    public void Pause(bool paused) { if (_voice != null) { if (paused) _voice.Pause(); else _voice.UnPause(); } }
    public void Stop() { if (_voice != null) _voice.Stop(); }
    void OnDisable() { Stop(); }
}
