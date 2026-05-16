using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Runs the main menu: loads/quits the game, and animates all the
/// pre-built menu visuals. Every GameObject it touches is created in
/// the scene by MainMenuBuilder — this script only animates references.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Name of the scene to load when Play is pressed. Must be added to Build Settings.")]
    public string gameSceneName = "Game";

    [Header("Scene references (assigned by RealBuca/Build Main Menu)")]
    public RectTransform titleRect;
    public TMP_Text titleText;
    public RectTransform playRect;
    public RectTransform quitRect;
    public Image playImage;
    public Image quitImage;
    public TMP_Text playLabel;
    public TMP_Text quitLabel;
    public Image flashOverlay;
    public ParticleSystem playBurst;
    public ParticleSystem quitBurst;
    public Transform orbitPuck;

    [Header("Auto-start timer")]
    [Tooltip("Seconds of idle before the game auto-starts. 0 = disabled. " +
             "QA req: max 30s for arcade-cabinet attract mode.")]
    public float autoStartSeconds = 30f;
    [Tooltip("TMP text showing the countdown. Assign in Inspector.")]
    public TMP_Text autoStartText;
    [Tooltip("Optional font override — drag your Bangers SDF (or any TMP font asset) " +
             "here and it will be applied at Start. Leave empty to use whatever " +
             "font is already on the TMP component.")]
    public TMPro.TMP_FontAsset autoStartFont;

    // Internal animation state
    float _idleTime;
    Vector2 _playBasePos, _quitBasePos, _titleBasePos;
    float _titleBaseSize;
    float _playPressScale = 1f, _quitPressScale = 1f;

    // Auto-start state
    float _autoStartTimer;
    bool _autoStarting;
    LevelSelectController _levelSelect;

    // Focus-grace: when the OS / editor gives focus back, Input events fire
    // spurious events for many frames. We detect this by BOTH:
    //   1. OnApplicationFocus / OnApplicationPause — fires for OS-level focus
    //   2. realtime gap detection — fires for any pause Unity didn't notify
    //      us about (editor tab switching, breakpoint pauses, etc.)
    // Either trigger arms a 30-frame grace where input checks are skipped.
    int _focusGraceFrames;
    const int FocusGraceFrameCount = 30; // ~0.5s @ 60fps
    float _lastRealtime;
    bool _stickEdgeWas;
    void OnApplicationFocus(bool hasFocus) { if (hasFocus) _focusGraceFrames = FocusGraceFrameCount; }
    void OnApplicationPause (bool paused)  { if (!paused) _focusGraceFrames = FocusGraceFrameCount; }

    void DetectRealtimeGap()
    {
        float now = Time.realtimeSinceStartup;
        if (_lastRealtime > 0f && (now - _lastRealtime) > 0.5f)
        {
            // >500ms gap between Update calls = the game was paused / minimized
            // / tab-switched. Arm focus-grace so input from the resume frame
            // doesn't reset the auto-start timer.
            _focusGraceFrames = FocusGraceFrameCount;
        }
        _lastRealtime = now;
    }

    void Start()
    {
        if (titleRect != null)   _titleBasePos = titleRect.anchoredPosition;
        if (playRect != null)    _playBasePos  = playRect.anchoredPosition;
        if (quitRect != null)    _quitBasePos  = quitRect.anchoredPosition;
        if (titleText != null)   _titleBaseSize = titleText.fontSize;

        // QA req #5: arcade attract mode caps auto-start at 30s. Old scenes
        // may still have 45s serialized — runtime-clamp so the cap holds
        // regardless of what's in the scene file.
        if (autoStartSeconds > 30f)
        {
            Debug.LogWarning($"[MainMenuController] autoStartSeconds was {autoStartSeconds} " +
                             "in the scene — clamping to 30 (arcade attract-mode requirement). " +
                             "Update the value in the Inspector to 30 to silence this warning.");
            autoStartSeconds = 30f;
        }
        _autoStartTimer = autoStartSeconds;

        // Show auto-start countdown immediately.
        if (autoStartText != null)
        {
            autoStartText.gameObject.SetActive(autoStartSeconds > 0f);
            autoStartText.text = Mathf.CeilToInt(autoStartSeconds).ToString();
            // Apply font + material override if one is assigned.
            if (autoStartFont != null)
            {
                autoStartText.font = autoStartFont;
                autoStartText.fontSharedMaterial = autoStartFont.material;
            }
        }

        StartCoroutine(EntranceAnimation());
    }

    // ═══════════════════════════════════════════════════════════
    // Auto-start: 45s idle → auto-launches the game
    // ═══════════════════════════════════════════════════════════
    void TickAutoStart()
    {
        if (autoStartSeconds <= 0f || _autoStarting) return;

        // Check if we just resumed from an unannounced pause (editor tab
        // switch, etc.) — arms focus-grace so the resumed-frame input
        // doesn't reset the auto-start timer.
        DetectRealtimeGap();

        // Resolve the level-select panel reference once it exists in the scene.
        if (_levelSelect == null)
            _levelSelect = FindFirstObjectByType<LevelSelectController>(FindObjectsInactive.Include);
        bool levelSelectOpen = _levelSelect != null && _levelSelect.IsOpen;

        if (!levelSelectOpen)
        {
            // Skip input checks during focus-grace frames so a window refocus
            // doesn't reset the timer with phantom events.
            if (_focusGraceFrames > 0)
            {
                _focusGraceFrames--;
                // Snapshot the CURRENT input state into the "was" trackers
                // every grace frame. Without this, when grace ends with the
                // stick still held (or a key still down from before refocus),
                // the very next frame sees stickEdgeNow=true && _stickEdgeWas=false
                // → registers as a fresh edge → resets the timer. Treating
                // grace frames as "input was already in this state" prevents
                // that phantom-edge fire.
                Vector2 graceStick = ArcadeInputAdapter.GetStick();
                _stickEdgeWas = Mathf.Abs(graceStick.x) > 0.5f || Mathf.Abs(graceStick.y) > 0.5f;
            }
            else
            {
                // EDGE-ONLY input detection. The previous design read held
                // states (Input.anyKey, mouse axes, sustained joystick) which
                // refocus + a single mouse-cursor jiggle would re-trigger
                // every frame. Edge detection (down events only) is immune to
                // refocus phantom events.
                Vector2 stick = ArcadeInputAdapter.GetStick();
                bool stickEdgeNow = Mathf.Abs(stick.x) > 0.5f || Mathf.Abs(stick.y) > 0.5f;
                bool stickEdge    = stickEdgeNow && !_stickEdgeWas;
                _stickEdgeWas     = stickEdgeNow;

                if (Input.anyKeyDown                  // any key DOWN this frame
                    || Input.GetMouseButtonDown(0)    // mouse left click
                    || Input.GetMouseButtonDown(1)    // mouse right click
                    || stickEdge)                     // joystick first push
                {
                    _autoStartTimer = autoStartSeconds;
                }

                // Arcade shortcuts only fire from the menu — not while the panel is up
                // (the panel handles its own Green-button = back).
                if (ArcadeInputAdapter.ConfirmDown()) { PlayGame(); return; }
                if (ArcadeInputAdapter.CancelDown()) { QuitGame(); return; }
            }
        }

        float prevTimer = _autoStartTimer;
        // Clamp dt so a long pause / refocus frame doesn't yank the timer
        // by ~0.33s (Unity's maximumDeltaTime cap). Cap at 1/30 = 33ms so
        // worst-case single-frame drift is barely perceptible.
        _autoStartTimer -= Mathf.Min(Time.deltaTime, 1f / 30f);

        // Audio: tick on every second of the last 5, alarm at 0
        if (AudioManager.Instance != null)
        {
            int prevSec = Mathf.CeilToInt(prevTimer);
            int curSec = Mathf.CeilToInt(_autoStartTimer);
            if (curSec != prevSec && curSec > 0 && curSec <= 5)
                AudioManager.Instance.PlayCountdownTick();
            else if (curSec <= 0 && prevSec > 0)
                AudioManager.Instance.PlayCountdownAlarm();
        }

        // Update countdown — always visible with full text + animated number.
        if (autoStartText != null)
        {
            // Force font + material every frame until it sticks. TMP keeps
            // a material instance linked to the old font's atlas texture —
            // changing font alone doesn't update the material, so the old
            // atlas renders and you see the old glyphs. Resetting
            // fontSharedMaterial to the new font's default material fixes it.
            if (autoStartFont != null && autoStartText.font != autoStartFont)
            {
                autoStartText.font = autoStartFont;
                autoStartText.fontSharedMaterial = autoStartFont.material;
                autoStartText.ForceMeshUpdate();
            }
            int secs = Mathf.CeilToInt(Mathf.Max(0f, _autoStartTimer));

            Color textColor;
            float scale;

            if (_autoStartTimer <= 5f)
            {
                // Last 5s — hot pink, fast pulse, bigger scale bounce.
                // Each second transition pops the number larger then settles.
                textColor = new Color(1f, 0.25f, 0.5f, 1f);
                float bounce = 1f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 7f));
                scale = bounce;
                autoStartText.text = $"STARTING IN  <size=150%><color=#FF4080>{secs}</color></size>";
            }
            else if (_autoStartTimer <= 15f)
            {
                // 15–5s — yellow warning, gentle pulse.
                textColor = new Color(1f, 0.85f, 0.3f, 0.95f);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 4f);
                scale = pulse;
                autoStartText.text = $"AUTO START IN  <size=130%><color=#FFD94A>{secs}</color></size>  SECONDS";
            }
            else
            {
                // 45–15s — calm white, steady, subtle breathing.
                textColor = new Color(1f, 1f, 1f, 0.65f);
                float breath = 1f + 0.02f * Mathf.Sin(Time.time * 1.5f);
                scale = breath;
                autoStartText.text = $"AUTO START IN  <size=120%>{secs}</size>  SECONDS";
            }

            autoStartText.color = textColor;
            autoStartText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        if (_autoStartTimer <= 0f)
        {
            _autoStarting = true;
            PlayGame();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Entrance: title drops in with overshoot, buttons slide up
    // ═══════════════════════════════════════════════════════════
    IEnumerator EntranceAnimation()
    {
        // Hide everything initially
        if (titleRect != null) titleRect.localScale = Vector3.zero;
        if (playRect  != null) playRect.anchoredPosition  = _playBasePos  + new Vector2(0f, -600f);
        if (quitRect  != null) quitRect.anchoredPosition  = _quitBasePos  + new Vector2(0f, -800f);
        SetButtonAlpha(playImage, playLabel, 0f);
        SetButtonAlpha(quitImage, quitLabel, 0f);

        yield return new WaitForSeconds(0.15f);

        // Title scales in with a bounce (overshoot back to 1)
        float tDur = 0.55f, t = 0f;
        while (t < tDur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / tDur);
            float s = EaseOutBack(k, 1.8f);
            if (titleRect != null) titleRect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (titleRect != null) titleRect.localScale = Vector3.one;

        // Play button slides up + fades in
        yield return AnimateButtonIn(playRect, playImage, playLabel, _playBasePos, 0.45f);
        // Small stagger
        yield return new WaitForSeconds(0.08f);
        // Quit button slides up + fades in
        yield return AnimateButtonIn(quitRect, quitImage, quitLabel, _quitBasePos, 0.45f);
    }

    IEnumerator AnimateButtonIn(RectTransform rt, Image img, TMP_Text label, Vector2 targetPos, float dur)
    {
        if (rt == null) yield break;
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = 1f - Mathf.Pow(1f - k, 3f); // easeOutCubic
            rt.anchoredPosition = Vector2.Lerp(start, targetPos, e);
            SetButtonAlpha(img, label, e);
            yield return null;
        }
        rt.anchoredPosition = targetPos;
        SetButtonAlpha(img, label, 1f);
    }

    // ═══════════════════════════════════════════════════════════
    // Idle loop: title pulses, buttons gently bob, puck orbits
    // ═══════════════════════════════════════════════════════════
    void Update()
    {
        _idleTime += Time.deltaTime;

        TickAutoStart();

        // Title — subtle scale pulse + color shift
        if (titleText != null)
        {
            float pulse = 1f + Mathf.Sin(_idleTime * 2.2f) * 0.025f;
            titleText.fontSize = _titleBaseSize * pulse;
            float hue = (Mathf.Sin(_idleTime * 0.7f) + 1f) * 0.5f; // 0..1
            titleText.color = Color.Lerp(
                new Color(1f, 0.3f, 0.65f),   // hot pink
                new Color(0.3f, 0.85f, 1f),   // cyan
                hue);
        }

        // Buttons — gentle bob + press-release scale spring
        if (playRect != null)
        {
            float bob = Mathf.Sin(_idleTime * 1.8f) * 3f;
            playRect.anchoredPosition = _playBasePos + new Vector2(0f, bob);
            _playPressScale = Mathf.Lerp(_playPressScale, 1f, Time.deltaTime * 8f);
            playRect.localScale = new Vector3(_playPressScale, _playPressScale, 1f);
        }
        if (quitRect != null)
        {
            float bob = Mathf.Sin(_idleTime * 1.8f + 0.6f) * 3f;
            quitRect.anchoredPosition = _quitBasePos + new Vector2(0f, bob);
            _quitPressScale = Mathf.Lerp(_quitPressScale, 1f, Time.deltaTime * 8f);
            quitRect.localScale = new Vector3(_quitPressScale, _quitPressScale, 1f);
        }

        // Orbit puck — rotates around a 6-unit circle behind the menu
        if (orbitPuck != null)
        {
            float a = _idleTime * 25f;
            float rad = a * Mathf.Deg2Rad;
            orbitPuck.localPosition = new Vector3(
                Mathf.Cos(rad) * 6f, Mathf.Sin(rad * 0.7f) * 2f, Mathf.Sin(rad) * 6f);
            orbitPuck.Rotate(60f * Time.deltaTime, 40f * Time.deltaTime, 0f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Button handlers — with press animation + flash + delay
    // ═══════════════════════════════════════════════════════════
    public void PlayGame()
    {
        if (playBurst != null) { playBurst.Clear(true); playBurst.Play(true); }
        _playPressScale = 1.18f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        StartCoroutine(FlashAndLoad(new Color(1f, 0.3f, 0.65f), gameSceneName));
    }

    public void QuitGame()
    {
        if (quitBurst != null) { quitBurst.Clear(true); quitBurst.Play(true); }
        _quitPressScale = 1.18f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        StartCoroutine(FlashAndQuit(new Color(0.3f, 0.85f, 1f)));
    }

    IEnumerator FlashAndLoad(Color c, string sceneName)
    {
        yield return FlashRoutine(c);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FlashAndQuit(Color c)
    {
        yield return FlashRoutine(c);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator FlashRoutine(Color c)
    {
        if (flashOverlay == null) { yield return new WaitForSeconds(0.15f); yield break; }
        float inDur = 0.18f, holdDur = 0.08f, outDur = 0.15f;
        float t = 0f;
        while (t < inDur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 0.7f, t / inDur);
            flashOverlay.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        yield return new WaitForSeconds(holdDur);
        t = 0f;
        while (t < outDur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.7f, 0f, t / outDur);
            flashOverlay.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════
    static void SetButtonAlpha(Image img, TMP_Text label, float a)
    {
        if (img != null) { var c = img.color; c.a = a; img.color = c; }
        if (label != null) { var c = label.color; c.a = a; label.color = c; }
    }

    static float EaseOutBack(float t, float overshoot)
    {
        float c = overshoot;
        float s = t - 1f;
        return s * s * ((c + 1f) * s + c) + 1f;
    }
}
