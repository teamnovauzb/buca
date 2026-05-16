using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Central audio service for RealBuca. Singleton, persists across all
/// scenes (DontDestroyOnLoad), routes every gameplay/UI sound through
/// a pooled set of AudioSources, and crossfades background music when
/// the active scene changes.
///
/// Architecture
///   • One AudioSource per simultaneous SFX (pool of `sfxPoolSize`).
///   • Two AudioSources for music — `_musicA` and `_musicB` — used as
///     the two ends of a crossfade so changing tracks doesn't pop.
///   • Per-mechanic looping AudioSources (magnet, wind, gravity well)
///     whose volume is driven by `SetXxxLoopActive(true, strength)`.
///
/// Volume model
///   final = clip * masterVolume * (musicVolume | sfxVolume)
///   Volumes are saved to PlayerPrefs and restored on Awake.
///
/// Wiring
///   Call AudioManager.Instance.PlaySfx(clip) from anywhere. Drag the
///   actual AudioClip assets onto the inspector slots — every script
///   in the project pulls from this single source of truth.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────
    // Music (looping)
    // ─────────────────────────────────────────────────────────
    [Header("Music (looping)")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip tenseGameplayMusic;   // crossfaded in when timer < tenseTimeThreshold
    public AudioClip gameOverMusic;
    public AudioClip winMusic;
    [Tooltip("Seconds remaining on the level timer at which we crossfade to tenseGameplayMusic.")]
    public float tenseTimeThreshold = 10f;

    // ─────────────────────────────────────────────────────────
    // Player SFX
    // ─────────────────────────────────────────────────────────
    [Header("Player SFX")]
    public AudioClip puckLaunchSfx;
    [Tooltip("Variations randomized per impact so repeated hits don't sound identical.")]
    public AudioClip[] wallHitSfx;
    public AudioClip deadlyHitSfx;
    public AudioClip pickupCollectSfx;
    public AudioClip holeSinkSfx;
    public AudioClip magnetAssistLoop;     // looped, volume driven by SetMagnetLoopActive

    // ─────────────────────────────────────────────────────────
    // Level SFX
    // ─────────────────────────────────────────────────────────
    [Header("Level SFX")]
    public AudioClip levelStartSfx;
    public AudioClip levelCompleteSfx;
    public AudioClip timeUpSfx;
    public AudioClip puckDeathSfx;
    [Tooltip("3 clips for 1/2/3-star reveal. Plays in sequence per star.")]
    public AudioClip[] starRevealSfx;
    public AudioClip timerLowTickSfx;       // each second of the last 5

    // ─────────────────────────────────────────────────────────
    // Mechanic SFX
    // ─────────────────────────────────────────────────────────
    [Header("Mechanic SFX")]
    public AudioClip bouncePadSfx;
    public AudioClip speedBoostSfx;
    public AudioClip teleporterSfx;
    public AudioClip windZoneLoop;          // looped while puck inside
    public AudioClip gravityWellLoop;       // looped while puck in range
    public AudioClip wallVanishSfx;
    public AudioClip wallReappearSfx;

    // ─────────────────────────────────────────────────────────
    // Combo SFX (level-complete callouts)
    // ─────────────────────────────────────────────────────────
    [Header("Combo SFX")]
    public AudioClip holeInOneSfx;
    public AudioClip perfectSfx;
    public AudioClip oneShotSfx;
    public AudioClip niceSaveSfx;

    // ─────────────────────────────────────────────────────────
    // UI SFX
    // ─────────────────────────────────────────────────────────
    [Header("UI SFX")]
    public AudioClip buttonClickSfx;
    public AudioClip buttonHoverSfx;
    public AudioClip panelOpenSfx;
    public AudioClip panelCloseSfx;
    public AudioClip navTickSfx;             // joystick step in level select
    public AudioClip scoreCountTickSfx;      // light tick during count-up animations
    public AudioClip countdownTickSfx;       // last 5s pre-auto-start tick
    public AudioClip countdownAlarmSfx;      // at 0

    // ─────────────────────────────────────────────────────────
    // Mixer + volumes
    // ─────────────────────────────────────────────────────────
    [Header("Mixer (optional)")]
    [Tooltip("Optional AudioMixer for routing. Leave null and we'll set volumes directly on AudioSource.")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Volumes (0..1)")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume   = 1f;
    [Range(0f, 1f)] public float ambientVolume = 0.6f;

    [Header("Tuning")]
    [Tooltip("Number of pooled SFX AudioSources — sets the max number of overlapping one-shots.")]
    public int sfxPoolSize = 10;
    public float musicCrossfadeDuration = 1.5f;

    // ─────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────
    AudioSource[] _sfxPool;
    int _sfxPoolIndex;
    AudioSource _musicA, _musicB, _activeMusic;
    AudioSource _magnetLoop, _windLoop, _gravityLoop;

    Coroutine _musicCo;
    bool _tenseModeActive;

    const string PrefMaster  = "BucaAudio_Master";
    const string PrefMusic   = "BucaAudio_Music";
    const string PrefSfx     = "BucaAudio_Sfx";
    const string PrefAmbient = "BucaAudio_Ambient";

    // ═══════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        LoadVolumes();
        BuildAudioSources();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // First-scene boot: SceneManager.sceneLoaded fires for *subsequent*
        // scene loads but NOT the initial scene the player launches into.
        // We need to manually kick off music for the first scene — but only
        // if the active scene wasn't already handled by sceneLoaded (which
        // can happen if Awake → Start runs after a quick LoadScene call).
        if (!_initialSceneHandled)
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    bool _initialSceneHandled;

    void BuildAudioSources()
    {
        // SFX pool
        _sfxPool = new AudioSource[Mathf.Max(1, sfxPoolSize)];
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            var go = new GameObject($"SfxSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = sfxGroup;
            _sfxPool[i] = src;
        }

        // Music A/B for crossfade
        _musicA = NewLoopSource("MusicA", musicGroup);
        _musicB = NewLoopSource("MusicB", musicGroup);
        _activeMusic = _musicA;

        // Per-mechanic looping sources
        _magnetLoop  = NewLoopSource("MagnetLoop",  sfxGroup);
        _windLoop    = NewLoopSource("WindLoop",    sfxGroup);
        _gravityLoop = NewLoopSource("GravityLoop", sfxGroup);
    }

    AudioSource NewLoopSource(string name, AudioMixerGroup group)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
        src.outputAudioMixerGroup = group;
        return src;
    }

    // ═══════════════════════════════════════════════════════════
    // SFX API
    // ═══════════════════════════════════════════════════════════
    public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || _sfxPool == null) return;
        var src = _sfxPool[_sfxPoolIndex];
        _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Length;
        src.pitch = pitch;
        src.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume * masterVolume);
    }

    public void PlaySfxRandom(AudioClip[] clips, float volume = 1f, float pitch = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySfx(clips[Random.Range(0, clips.Length)], volume, pitch);
    }

    /// <summary>
    /// Plays a wall-hit clip with volume/pitch scaled by impact speed.
    /// Caller passes the puck's speed in m/s — soft taps barely register,
    /// hard slams play loud + slightly higher-pitched.
    /// </summary>
    public void PlayWallHit(float impactSpeed)
    {
        if (wallHitSfx == null || wallHitSfx.Length == 0) return;
        float t = Mathf.Clamp01(impactSpeed / 15f);
        float vol = Mathf.Lerp(0.30f, 1f, t);
        float pitch = Random.Range(0.92f, 1.08f) * Mathf.Lerp(0.88f, 1.12f, t);
        PlaySfxRandom(wallHitSfx, vol, pitch);
    }

    /// <summary>Pickup chime — pitch rises with combo count for "rising" reward feel.</summary>
    public void PlayPickup(int comboIndex)
    {
        if (pickupCollectSfx == null) return;
        float pitch = 1f + Mathf.Clamp(comboIndex, 0, 8) * 0.06f;
        PlaySfx(pickupCollectSfx, 1f, pitch);
    }

    /// <summary>
    /// Plays a star reveal "ting" — uses starRevealSfx[starIndex] if the
    /// array has that index, otherwise the last clip with rising pitch.
    /// Call once per star as the reveal animation lands on it.
    /// </summary>
    public void PlayStarReveal(int starIndex)
    {
        if (starRevealSfx == null || starRevealSfx.Length == 0) return;
        int safeIdx = Mathf.Min(starIndex, starRevealSfx.Length - 1);
        float pitch = 1f + starIndex * 0.10f; // 1.0, 1.1, 1.2, ...
        PlaySfx(starRevealSfx[safeIdx], 1f, pitch);
    }

    /// <summary>Maps a comboType (0..4) to its corresponding SFX.</summary>
    public void PlayCombo(int comboType)
    {
        switch (comboType)
        {
            case 4: PlaySfx(holeInOneSfx); break;
            case 3: PlaySfx(perfectSfx);    break;
            case 2: PlaySfx(oneShotSfx);    break;
            case 1: PlaySfx(niceSaveSfx);   break;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Looping mechanic SFX (driven by gameplay each frame)
    // ═══════════════════════════════════════════════════════════
    public void SetMagnetLoopActive(bool active, float strength = 1f)
        => DriveLoop(_magnetLoop, magnetAssistLoop, active, strength * 0.5f);

    public void SetWindLoopActive(bool active, float strength = 1f)
        => DriveLoop(_windLoop, windZoneLoop, active, strength * ambientVolume);

    public void SetGravityLoopActive(bool active, float strength = 1f)
        => DriveLoop(_gravityLoop, gravityWellLoop, active, strength * ambientVolume);

    /// <summary>
    /// Generic loop helper. Starts the source if active+clip exists;
    /// fades volume toward target. When active=false, fades to 0 then
    /// stops automatically.
    /// </summary>
    void DriveLoop(AudioSource src, AudioClip clip, bool active, float targetStrength)
    {
        if (src == null) return;
        if (clip == null)
        {
            if (src.isPlaying) src.Stop();
            return;
        }
        if (active)
        {
            if (src.clip != clip) { src.clip = clip; }
            if (!src.isPlaying) src.Play();
            float target = Mathf.Clamp01(targetStrength) * sfxVolume * masterVolume;
            src.volume = Mathf.MoveTowards(src.volume, target, Time.unscaledDeltaTime * 4f);
        }
        else
        {
            src.volume = Mathf.MoveTowards(src.volume, 0f, Time.unscaledDeltaTime * 3f);
            if (src.volume <= 0.001f && src.isPlaying) src.Stop();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Music API
    // ═══════════════════════════════════════════════════════════
    public void PlayMusic(AudioClip clip, float fadeSeconds = -1f)
    {
        if (clip == null) { StopMusic(fadeSeconds); return; }
        if (_activeMusic == null) return;
        if (_activeMusic.clip == clip && _activeMusic.isPlaying) return;
        if (fadeSeconds < 0f) fadeSeconds = musicCrossfadeDuration;
        StopMusicCoroutine();
        _musicCo = StartCoroutine(CrossfadeMusicCo(clip, fadeSeconds));
    }

    public void StopMusic(float fadeSeconds = 0.5f)
    {
        StopMusicCoroutine();
        if (_activeMusic == null) return;
        _musicCo = StartCoroutine(FadeOutCo(_activeMusic, fadeSeconds));
    }

    void StopMusicCoroutine()
    {
        if (_musicCo != null)
        {
            StopCoroutine(_musicCo);
            _musicCo = null;
        }
    }

    IEnumerator CrossfadeMusicCo(AudioClip newClip, float duration)
    {
        var oldSrc = _activeMusic;
        var newSrc = (_activeMusic == _musicA) ? _musicB : _musicA;
        newSrc.clip = newClip;
        newSrc.volume = 0f;
        newSrc.Play();

        float t = 0f;
        float startOldVol = oldSrc.volume;
        float targetVol = musicVolume * masterVolume;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            oldSrc.volume = Mathf.Lerp(startOldVol, 0f, k);
            newSrc.volume = Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }
        oldSrc.Stop();
        oldSrc.volume = 0f;
        _activeMusic = newSrc;
        _activeMusic.volume = targetVol;
        _musicCo = null;
    }

    IEnumerator FadeOutCo(AudioSource src, float duration)
    {
        float t = 0f;
        float start = src.volume;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / Mathf.Max(0.001f, duration));
            yield return null;
        }
        src.Stop();
        src.volume = 0f;
        _musicCo = null;
    }

    /// <summary>
    /// Drives the tense / normal gameplay music swap based on a
    /// remaining-time signal. LevelManager calls this each frame.
    /// </summary>
    public void NotifyTimerRemaining(float seconds, bool timerActive)
    {
        if (!timerActive || tenseGameplayMusic == null || gameplayMusic == null) return;
        // Hysteresis: enter tense at <= threshold, exit only after seconds rises
        // back above threshold + 1.5s. Prevents flapping between tracks if the
        // remaining time hovers right at the boundary (e.g., paused popup
        // briefly nudging the value across).
        const float ExitMargin = 1.5f;
        if (!_tenseModeActive && seconds <= tenseTimeThreshold && seconds > 0f)
        {
            _tenseModeActive = true;
            PlayMusic(tenseGameplayMusic, 0.6f);
        }
        else if (_tenseModeActive && seconds > tenseTimeThreshold + ExitMargin)
        {
            _tenseModeActive = false;
            PlayMusic(gameplayMusic, 1.0f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Scene-based music auto-switch
    // ═══════════════════════════════════════════════════════════
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _initialSceneHandled = true;
        _tenseModeActive = false;
        string n = scene.name == null ? "" : scene.name.ToLowerInvariant();
        if (n.Contains("menu"))
        {
            if (menuMusic != null) PlayMusic(menuMusic);
        }
        else if (n.Contains("game"))
        {
            if (gameplayMusic != null) PlayMusic(gameplayMusic);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Volume control (saved to PlayerPrefs)
    // ═══════════════════════════════════════════════════════════
    public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetMusicVolume(float v)  { musicVolume  = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetSfxVolume(float v)    { sfxVolume    = Mathf.Clamp01(v); SaveVolumes(); }
    public void SetAmbientVolume(float v){ ambientVolume = Mathf.Clamp01(v); SaveVolumes(); }

    void ApplyVolumes()
    {
        // Re-apply music volume to whichever source is currently active
        if (_activeMusic != null && _activeMusic.isPlaying)
            _activeMusic.volume = musicVolume * masterVolume;
        // Loop sources will rebalance themselves on next DriveLoop tick
    }

    void LoadVolumes()
    {
        masterVolume  = PlayerPrefs.GetFloat(PrefMaster,  masterVolume);
        musicVolume   = PlayerPrefs.GetFloat(PrefMusic,   musicVolume);
        sfxVolume     = PlayerPrefs.GetFloat(PrefSfx,     sfxVolume);
        ambientVolume = PlayerPrefs.GetFloat(PrefAmbient, ambientVolume);
    }

    void SaveVolumes()
    {
        PlayerPrefs.SetFloat(PrefMaster,  masterVolume);
        PlayerPrefs.SetFloat(PrefMusic,   musicVolume);
        PlayerPrefs.SetFloat(PrefSfx,     sfxVolume);
        PlayerPrefs.SetFloat(PrefAmbient, ambientVolume);
        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════════════════
    // UI convenience wrappers (so call sites don't repeat null-checks)
    // ═══════════════════════════════════════════════════════════
    public void PlayButtonClick() => PlaySfx(buttonClickSfx);
    public void PlayButtonHover() => PlaySfx(buttonHoverSfx, 0.45f);
    public void PlayPanelOpen()   => PlaySfx(panelOpenSfx);
    public void PlayPanelClose()  => PlaySfx(panelCloseSfx);
    public void PlayNavTick()     => PlaySfx(navTickSfx, 0.7f);
    public void PlayScoreTick()   => PlaySfx(scoreCountTickSfx, 0.4f, Random.Range(0.95f, 1.05f));
    public void PlayCountdownTick() => PlaySfx(countdownTickSfx);
    public void PlayCountdownAlarm() => PlaySfx(countdownAlarmSfx);

#if UNITY_EDITOR
    // ═══════════════════════════════════════════════════════════
    // Editor-only: one-click wiring of every clip slot from the
    // organized Assets/Audio/ folder structure that the README
    // describes. Right-click the component header → "⚙ Auto-Wire
    // Clips From Assets/Audio/" and every same-named .ogg/.wav
    // gets loaded and assigned. Saves you ~37 manual drags.
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("Auto-Wire All Clips")]
    public void AutoWireFromAssetsFolder()
    {
        const string root = "Assets/Audio";
        int wired = 0, missing = 0;

        // Music (still in Music/ — user may not have populated yet)
        wired += TryAssign(ref menuMusic,             root, "Music/MenuMusic",          ref missing);
        wired += TryAssign(ref gameplayMusic,         root, "Music/GameplayMusic",      ref missing);
        wired += TryAssign(ref tenseGameplayMusic,    root, "Music/TenseGameplay",      ref missing);
        wired += TryAssign(ref gameOverMusic,         root, "Music/GameOver",           ref missing);
        wired += TryAssign(ref winMusic,              root, "Music/WinJingle",          ref missing);

        // Player
        wired += TryAssign(ref puckLaunchSfx,         root, "Player/PuckLaunch",        ref missing);
        wired += TryAssignArray(ref wallHitSfx,       root, "Player/WallHit_", 5,       ref missing);
        wired += TryAssign(ref deadlyHitSfx,          root, "Player/DeadlyHit",         ref missing);
        wired += TryAssign(ref pickupCollectSfx,      root, "Player/PickupCollect",     ref missing);
        wired += TryAssign(ref holeSinkSfx,           root, "Player/HoleSink",          ref missing);
        wired += TryAssign(ref magnetAssistLoop,      root, "Player/MagnetAssistLoop",  ref missing);

        // Level
        wired += TryAssign(ref levelStartSfx,         root, "Level/LevelStart",         ref missing);
        wired += TryAssign(ref levelCompleteSfx,      root, "Level/LevelComplete",      ref missing);
        wired += TryAssign(ref timeUpSfx,             root, "Level/TimeUp",             ref missing);
        wired += TryAssign(ref puckDeathSfx,          root, "Level/PuckDeath",          ref missing);
        wired += TryAssignArray(ref starRevealSfx,    root, "Level/StarReveal_", 3,     ref missing);
        wired += TryAssign(ref timerLowTickSfx,       root, "Level/TimerLowTick",       ref missing);

        // Mechanic
        wired += TryAssign(ref bouncePadSfx,          root, "Mechanic/BouncePad",       ref missing);
        wired += TryAssign(ref speedBoostSfx,         root, "Mechanic/SpeedBoost",      ref missing);
        wired += TryAssign(ref teleporterSfx,         root, "Mechanic/Teleporter",      ref missing);
        wired += TryAssign(ref windZoneLoop,          root, "Mechanic/WindZoneLoop",    ref missing);
        wired += TryAssign(ref gravityWellLoop,       root, "Mechanic/GravityWellLoop", ref missing);
        wired += TryAssign(ref wallVanishSfx,         root, "Mechanic/WallVanish",      ref missing);
        wired += TryAssign(ref wallReappearSfx,       root, "Mechanic/WallReappear",    ref missing);

        // Combo
        wired += TryAssign(ref holeInOneSfx,          root, "Combo/HoleInOne",          ref missing);
        wired += TryAssign(ref perfectSfx,            root, "Combo/Perfect",            ref missing);
        wired += TryAssign(ref oneShotSfx,            root, "Combo/OneShot",            ref missing);
        wired += TryAssign(ref niceSaveSfx,           root, "Combo/NiceSave",           ref missing);

        // UI
        wired += TryAssign(ref buttonClickSfx,        root, "UI/ButtonClick",           ref missing);
        wired += TryAssign(ref buttonHoverSfx,        root, "UI/ButtonHover",           ref missing);
        wired += TryAssign(ref panelOpenSfx,          root, "UI/PanelOpen",             ref missing);
        wired += TryAssign(ref panelCloseSfx,         root, "UI/PanelClose",            ref missing);
        wired += TryAssign(ref navTickSfx,            root, "UI/NavTick",               ref missing);
        wired += TryAssign(ref scoreCountTickSfx,     root, "UI/ScoreCountTick",        ref missing);
        wired += TryAssign(ref countdownTickSfx,      root, "UI/CountdownTick",         ref missing);
        wired += TryAssign(ref countdownAlarmSfx,     root, "UI/CountdownAlarm",        ref missing);

        EditorUtility.SetDirty(this);
        Debug.Log($"[AudioManager] ✔ Auto-wired {wired} clip slots from {root}/. " +
                  (missing > 0 ? $"({missing} slot{(missing==1?"":"s")} left empty — file not found.)" : "All slots assigned."));
    }

    /// <summary>Loads `Assets/Audio/{relPathNoExt}.ogg` (then .wav, then .mp3) into `field` if it's currently null. Returns 1 on success, 0 on miss.</summary>
    static int TryAssign(ref AudioClip field, string root, string relPathNoExt, ref int missing)
    {
        if (field != null) return 0; // don't overwrite existing manual wiring
        var clip = LoadAnyExt(root, relPathNoExt);
        if (clip != null) { field = clip; return 1; }
        missing++;
        return 0;
    }

    /// <summary>Same as TryAssign but for an array — loads `relPathPrefix1.ogg` through `relPathPrefixN.ogg`.</summary>
    static int TryAssignArray(ref AudioClip[] arr, string root, string relPathPrefix, int count, ref int missing)
    {
        if (arr != null && arr.Length > 0 && arr[0] != null) return 0;
        var loaded = new System.Collections.Generic.List<AudioClip>(count);
        for (int i = 1; i <= count; i++)
        {
            var clip = LoadAnyExt(root, relPathPrefix + i);
            if (clip != null) loaded.Add(clip);
        }
        if (loaded.Count == 0) { missing++; return 0; }
        arr = loaded.ToArray();
        return 1;
    }

    static AudioClip LoadAnyExt(string root, string relPathNoExt)
    {
        string[] exts = { ".ogg", ".wav", ".mp3" };
        foreach (var ext in exts)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{root}/{relPathNoExt}{ext}");
            if (clip != null) return clip;
        }
        return null;
    }
#endif
}
