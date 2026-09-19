using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

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
    [Tooltip("Main-menu countdown duration. Runtime-enforced to exactly 30 seconds.")]
    public float autoStartSeconds = 30f;
    [Tooltip("TMP text showing the countdown. Assign in Inspector.")]
    public TMP_Text autoStartText;
    [Tooltip("Serialized reference retained for compatibility. The countdown font is editor-baked on the TMP component.")]
    public TMPro.TMP_FontAsset autoStartFont;

    // Internal animation state
    float _idleTime;
    Vector2 _playBasePos, _quitBasePos, _titleBasePos;
    float _titleBaseSize;

    // Auto-start state
    const float MainMenuAutoStartSeconds = 30f;
    float _autoStartTimer;
    bool _autoStarting;

    [Header("Prebuilt returning-player prompt")]
    [Tooltip("Authored ReturnPlayerPrompt prefab instance. This UI is never constructed at runtime.")]
    [SerializeField] GameObject _resumePromptRoot;
    [SerializeField] TMP_Text _resumeQuestion;
    [SerializeField] Button[] _resumeButtons;
    [SerializeField] Image[] _resumeButtonImages;
    [SerializeField] TMP_Text[] _resumeButtonLabels;
    [SerializeField] RectTransform _resumePanelRect;

    // Returning-player choice shown only after a manual PLAY press.
    int _resumeLevelIndex;
    int _resumeChoice;
    int _resumeInputBlockFrames;
    bool _resumePromptOpen;
    bool _resumeStickWasLeft;
    bool _resumeStickWasRight;
    bool _loadingGame;
    bool _resumeButtonsWired;

    public bool ResumePromptOpen => _resumePromptOpen;

    void Start()
    {
        if (titleRect != null)   _titleBasePos = titleRect.anchoredPosition;
        if (playRect != null)    _playBasePos  = playRect.anchoredPosition;
        if (quitRect != null)    _quitBasePos  = quitRect.anchoredPosition;
        if (titleText != null)   _titleBaseSize = titleText.fontSize;

        // Main-menu requirement: always count 30, 29 ... 2, 1 and then start.
        // Enforce this at runtime so stale serialized scene values cannot make
        // the countdown begin at 15, 18, 45, or any other value.
        if (!Mathf.Approximately(autoStartSeconds, MainMenuAutoStartSeconds))
        {
            Debug.LogWarning($"[MainMenuController] autoStartSeconds was {autoStartSeconds} " +
                             "in the scene — setting it to 30 (main-menu requirement). " +
                             "Update the value in the Inspector to 30 to silence this warning.");
            autoStartSeconds = MainMenuAutoStartSeconds;
        }
        _autoStartTimer = MainMenuAutoStartSeconds;

        // Show auto-start countdown immediately.
        if (autoStartText != null)
        {
            autoStartText.gameObject.SetActive(autoStartSeconds > 0f);
            int seconds = Mathf.CeilToInt(autoStartSeconds);
            autoStartText.text = $"AUTO START  <color=#9AD8FF>{seconds}</color>  SECONDS";
            autoStartText.rectTransform.localScale = Vector3.one;
        }

        StartCoroutine(EntranceAnimation());
    }

    // ═══════════════════════════════════════════════════════════
    // Auto-start: CONTINUOUS attract-mode countdown.
    //
    // The countdown displays 30 → 1 and then auto-launches —
    // it NEVER resets on input and does NOT pause while the level picker
    // is open. (QA: the background timer must keep counting no matter what
    // the player does — pressing level buttons, opening the picker, etc.
    // The old version reset on input + paused under the picker, which made
    // it look frozen.)
    // ═══════════════════════════════════════════════════════════
    void TickAutoStart()
    {
        if (autoStartSeconds <= 0f || _autoStarting || _resumePromptOpen) return;

        float prevTimer = _autoStartTimer;
        // This is a real arcade wall-clock deadline. Using a 30 FPS clamp made
        // a 30-second countdown last 45 seconds at 20 FPS. Unscaled time keeps
        // the deadline accurate even if another UI temporarily changes timeScale.
        _autoStartTimer -= Time.unscaledDeltaTime;

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

        // Launch before updating the label so zero is never rendered. The last
        // visible number is always 1, for its complete one-second interval.
        if (_autoStartTimer <= 0f)
        {
            _autoStarting = true;
            // Attract mode always begins a fresh run from Level 1. Do not route
            // through the saved-progress or currently highlighted level.
            StartGameAtLevel(0);
            return;
        }

        // Update countdown text and warning color. Its authored prefab owns the
        // layout and scale so it cannot jitter against other menu animation.
        if (autoStartText != null)
        {
            int secs = Mathf.Max(1, Mathf.CeilToInt(_autoStartTimer));

            Color textColor;

            if (_autoStartTimer <= 5f)
            {
                // Last 5s — hot magenta warning without moving the layout.
                textColor = new Color(1f, 0.30f, 0.62f, 1f);
                autoStartText.text = $"STARTING IN  <size=150%><color=#FF3D9E>{secs}</color></size>";
            }
            else if (_autoStartTimer <= 15f)
            {
                // 15–5s — electric cyan warning (synthwave palette).
                textColor = new Color(0.40f, 0.90f, 1f, 0.95f);
                autoStartText.text = $"AUTO START IN  <size=130%><color=#2DE2FF>{secs}</color></size>  SECONDS";
            }
            else
            {
                // 30–15s — calm light cyan.
                textColor = new Color(0.62f, 0.88f, 1f, 0.80f);
                autoStartText.text = $"AUTO START IN  <size=120%><color=#9AD8FF>{secs}</color></size>  SECONDS";
            }

            autoStartText.color = textColor;
            autoStartText.rectTransform.localScale = Vector3.one;
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

        if (_resumePromptOpen)
            TickResumePromptInput();

        // Title — subtle scale pulse + color shift
        if (titleText != null)
        {
            float pulse = 1f + Mathf.Sin(_idleTime * 2.2f) * 0.025f;
            titleText.fontSize = _titleBaseSize * pulse;
            float hue = (Mathf.Sin(_idleTime * 0.7f) + 1f) * 0.5f; // 0..1
            titleText.color = Color.Lerp(
                new Color(1f, 0.22f, 0.72f),  // vivid magenta
                new Color(0.25f, 0.92f, 1f),  // electric cyan
                hue);
        }

        // Button positions remain at their authored anchors after the one-time
        // entrance. Continuous bobbing fought the focus highlight and read as
        // a glitch on cabinet displays.

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
        if (_loadingGame || _resumePromptOpen) return;

        // The 30-second attract-mode launch has nobody present to answer a
        // dialog, so preserve its established Level-1 behaviour. A real PLAY
        // press offers to continue whenever a later saved level exists.
        if (!_autoStarting && TryGetResumeLevel(out int savedLevel))
        {
            ShowResumePrompt(savedLevel);
            return;
        }

        StartGameAtLevel(0);
    }

    void StartGameAtLevel(int zeroBasedLevel)
    {
        if (_loadingGame) return;
        _loadingGame = true;
        HideResumePrompt(restoreMenuNavigation: false);
        PlayerPrefs.SetInt(LevelSelectController.PendingLevelKey, Mathf.Max(0, zeroBasedLevel));
        PlayerPrefs.Save();

        if (playBurst != null) { playBurst.Clear(true); playBurst.Play(true); }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        StartCoroutine(FlashAndLoad(new Color(1f, 0.3f, 0.65f), gameSceneName));
    }

    bool TryGetResumeLevel(out int savedLevel)
    {
        const string CurrentLevelKey = "BucaCurrentLevel";
        savedLevel = 0;

        if (!PlayerPrefs.HasKey(CurrentLevelKey))
            return false;

        // Levels are saved as zero-based indexes. Level 1 needs no question,
        // because both choices would lead to exactly the same place.
        savedLevel = Mathf.Clamp(PlayerPrefs.GetInt(CurrentLevelKey, 0), 0, 29);
        return savedLevel > 0;
    }

    void ShowResumePrompt(int savedLevel)
    {
        EnsureResumePrompt();
        if (_resumePromptRoot == null) return;

        _resumeLevelIndex = Mathf.Clamp(savedLevel, 1, 29);
        _resumeQuestion.text = $"CONTINUE FROM LEVEL {_resumeLevelIndex + 1}?";
        _resumeButtonLabels[0].text = $"CONTINUE LEVEL {_resumeLevelIndex + 1}";
        _resumeButtonLabels[1].text = "START LEVEL 1";

        _resumeChoice = 0;
        _resumeStickWasLeft = false;
        _resumeStickWasRight = false;
        _resumeInputBlockFrames = 2; // Do not reuse the PLAY press as a dialog confirm.
        _resumePromptOpen = true;
        _resumePromptRoot.SetActive(true);
        _resumePromptRoot.transform.SetAsLastSibling();
        SetResumeChoice(0);

        if (_resumePanelRect != null)
            StartCoroutine(AnimateResumePromptIn());
    }

    void HideResumePrompt(bool restoreMenuNavigation = true)
    {
        _resumePromptOpen = false;
        if (_resumePromptRoot != null)
            _resumePromptRoot.SetActive(false);

        // ArcadeUINavigator reasserts its own focus on the following frame.
        // Keep the argument descriptive at call sites where a scene load follows.
        _ = restoreMenuNavigation;
    }

    void TickResumePromptInput()
    {
        if (_resumeInputBlockFrames > 0)
        {
            _resumeInputBlockFrames--;
            return;
        }

        Vector2 stick = ArcadeInputAdapter.GetStick();
        if (Input.GetKey(KeyCode.UpArrow)) stick.y = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) stick.y = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) stick.x = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) stick.x = 1f;

        bool towardContinue = stick.y > 0.5f || stick.x < -0.5f;
        bool towardLevelOne = stick.y < -0.5f || stick.x > 0.5f;

        if (towardContinue && !_resumeStickWasLeft)
            SetResumeChoice(0);
        else if (towardLevelOne && !_resumeStickWasRight)
            SetResumeChoice(1);

        _resumeStickWasLeft = towardContinue;
        _resumeStickWasRight = towardLevelOne;

        if (ArcadeInputAdapter.ConfirmDown() || Input.GetKeyDown(KeyCode.Return))
        {
            if (_resumeChoice == 0)
                StartGameAtLevel(_resumeLevelIndex);
            else
                StartGameAtLevel(0);
            return;
        }

        if (ArcadeInputAdapter.CancelDown() || Input.GetKeyDown(KeyCode.Escape))
        {
            HideResumePrompt();
            return;
        }

        ApplyResumeChoiceVisuals();
    }

    void SetResumeChoice(int choice)
    {
        _resumeChoice = Mathf.Clamp(choice, 0, 1);
        ApplyResumeChoiceVisuals();

        if (EventSystem.current != null && _resumeButtons != null)
            EventSystem.current.SetSelectedGameObject(_resumeButtons[_resumeChoice].gameObject);
    }

    void ApplyResumeChoiceVisuals()
    {
        if (_resumeButtons == null || _resumeButtonImages == null) return;

        for (int i = 0; i < _resumeButtons.Length; i++)
        {
            bool selected = i == _resumeChoice;
            Color baseColor = i == 0
                ? new Color(0.02f, 0.42f, 0.51f, 1f)
                : new Color(0.22f, 0.09f, 0.37f, 1f);
            Color selectedColor = i == 0
                ? new Color(0.03f, 0.68f, 0.78f, 1f)
                : new Color(0.48f, 0.18f, 0.72f, 1f);

            _resumeButtonImages[i].color = selected ? selectedColor : baseColor;
            float pulse = selected ? 1.025f + Mathf.Sin(Time.unscaledTime * 6f) * 0.01f : 1f;
            _resumeButtons[i].transform.localScale = Vector3.one * pulse;
            _resumeButtonLabels[i].color = selected ? Color.white : new Color(0.78f, 0.83f, 0.94f, 1f);
        }
    }

    void EnsureResumePrompt()
    {
        bool valid = _resumePromptRoot != null && _resumeQuestion != null &&
                     _resumePanelRect != null && _resumeButtons != null &&
                     _resumeButtonImages != null && _resumeButtonLabels != null &&
                     _resumeButtons.Length == 2 && _resumeButtonImages.Length == 2 &&
                     _resumeButtonLabels.Length == 2;
        if (!valid)
        {
            Debug.LogError("[MainMenuController] The prebuilt returning-player prompt is incomplete. " +
                           "Run RealBuca/Prebuild/Step 2 - Main Menu Runtime UI in the Unity Editor.", this);
            return;
        }

        if (_resumeButtonsWired) return;
        _resumeButtons[0].onClick.AddListener(ContinueSavedLevelFromPrompt);
        _resumeButtons[1].onClick.AddListener(StartLevelOneFromPrompt);
        _resumeButtonsWired = true;
    }

    void ContinueSavedLevelFromPrompt()
    {
        SetResumeChoice(0);
        StartGameAtLevel(_resumeLevelIndex);
    }

    void StartLevelOneFromPrompt()
    {
        SetResumeChoice(1);
        StartGameAtLevel(0);
    }

    IEnumerator AnimateResumePromptIn()
    {
        const float duration = 0.24f;
        float elapsed = 0f;
        _resumePanelRect.localScale = Vector3.one * 0.78f;
        while (elapsed < duration && _resumePromptOpen)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t, 1.25f);
            _resumePanelRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.78f, 1f, eased);
            yield return null;
        }
        if (_resumePanelRect != null)
            _resumePanelRect.localScale = Vector3.one;
    }

    public void QuitGame()
    {
        if (quitBurst != null) { quitBurst.Clear(true); quitBurst.Play(true); }
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

        // Try Luxodd's BackToSystem first — Application.Quit() is a no-op
        // in WebGL (the cyan-flash-then-stuck Quit bug). If the bridge is
        // present and connected, it returns us to the cabinet's game-list.
        if (LuxoddGameBridge.Instance != null && LuxoddGameBridge.Instance.QuitToArcadeMenu())
            yield break;

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
