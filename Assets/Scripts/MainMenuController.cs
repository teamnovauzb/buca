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
    const float MainMenuAutoStartSeconds = 30f;
    float _autoStartTimer;
    bool _autoStarting;

    // Returning-player choice shown only after a manual PLAY press.
    GameObject _resumePromptRoot;
    TMP_Text _resumeQuestion;
    Button[] _resumeButtons;
    Image[] _resumeButtonImages;
    TMP_Text[] _resumeButtonLabels;
    RectTransform _resumePanelRect;
    int _resumeLevelIndex;
    int _resumeChoice;
    int _resumeInputBlockFrames;
    bool _resumePromptOpen;
    bool _resumeStickWasLeft;
    bool _resumeStickWasRight;
    bool _loadingGame;
    Texture2D _resumeRoundedTexture;
    Sprite _resumeRoundedSprite;

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

        // Update countdown — always visible with full text + animated number.
        if (autoStartText != null)
        {
            // Force font + material every frame until it sticks (TMP keeps a
            // material instance linked to the old font's atlas otherwise).
            if (autoStartFont != null && autoStartText.font != autoStartFont)
            {
                autoStartText.font = autoStartFont;
                autoStartText.fontSharedMaterial = autoStartFont.material;
                autoStartText.ForceMeshUpdate();
            }
            int secs = Mathf.Max(1, Mathf.CeilToInt(_autoStartTimer));

            Color textColor;
            float scale;

            if (_autoStartTimer <= 5f)
            {
                // Last 5s — hot magenta, fast pulse, bigger scale bounce.
                textColor = new Color(1f, 0.30f, 0.62f, 1f);
                float bounce = 1f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 7f));
                scale = bounce;
                autoStartText.text = $"STARTING IN  <size=150%><color=#FF3D9E>{secs}</color></size>";
            }
            else if (_autoStartTimer <= 15f)
            {
                // 15–5s — electric cyan warning, gentle pulse (synthwave palette).
                textColor = new Color(0.40f, 0.90f, 1f, 0.95f);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 4f);
                scale = pulse;
                autoStartText.text = $"AUTO START IN  <size=130%><color=#2DE2FF>{secs}</color></size>  SECONDS";
            }
            else
            {
                // 30–15s — calm light cyan, steady, subtle breathing.
                textColor = new Color(0.62f, 0.88f, 1f, 0.80f);
                float breath = 1f + 0.02f * Mathf.Sin(Time.time * 1.5f);
                scale = breath;
                autoStartText.text = $"AUTO START IN  <size=120%><color=#9AD8FF>{secs}</color></size>  SECONDS";
            }

            autoStartText.color = textColor;
            autoStartText.rectTransform.localScale = new Vector3(scale, scale, 1f);
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

        // Buttons — subtle position bob ONLY. ArcadeUINavigator owns the
        // selected-button scale highlight, so we don't touch scale here.
        if (playRect != null)
        {
            float bob = Mathf.Sin(_idleTime * 1.8f) * 3f;
            playRect.anchoredPosition = _playBasePos + new Vector2(0f, bob);
            _playPressScale = Mathf.Lerp(_playPressScale, 1f, Time.deltaTime * 8f);
        }
        if (quitRect != null)
        {
            float bob = Mathf.Sin(_idleTime * 1.8f + 0.6f) * 3f;
            quitRect.anchoredPosition = _quitBasePos + new Vector2(0f, bob);
            _quitPressScale = Mathf.Lerp(_quitPressScale, 1f, Time.deltaTime * 8f);
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
        _playPressScale = 1.18f;
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
        if (_resumePromptRoot != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MainMenuController] Cannot create resume prompt: no Canvas found.");
            return;
        }

        BuildResumeRoundedSprite();
        TMP_FontAsset font = playLabel != null ? playLabel.font : TMP_Settings.defaultFontAsset;

        _resumePromptRoot = new GameObject("ReturnPlayerPrompt", typeof(RectTransform));
        RectTransform root = _resumePromptRoot.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        Stretch(root);

        Image dimmer = _resumePromptRoot.AddComponent<Image>();
        dimmer.color = new Color(0.015f, 0.008f, 0.06f, 0.90f);
        dimmer.raycastTarget = true;

        GameObject panel = new GameObject("ResumeCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _resumePanelRect = panel.GetComponent<RectTransform>();
        _resumePanelRect.SetParent(root, false);
        _resumePanelRect.anchorMin = _resumePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        _resumePanelRect.pivot = new Vector2(0.5f, 0.5f);
        _resumePanelRect.sizeDelta = new Vector2(900f, 560f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = _resumeRoundedSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.025f, 0.045f, 0.11f, 0.985f);
        panelImage.raycastTarget = false;

        var panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        panelShadow.effectDistance = new Vector2(14f, -18f);
        var panelOutline = panel.AddComponent<UnityEngine.UI.Outline>();
        panelOutline.effectColor = new Color(0.18f, 0.88f, 1f, 0.92f);
        panelOutline.effectDistance = new Vector2(4f, -4f);

        CreateResumeBar(panel.transform, "TopGlow", new Vector2(0f, 246f),
            new Vector2(740f, 6f), new Color(0.15f, 0.92f, 1f, 1f));
        CreateResumeBar(panel.transform, "GoldAccent", new Vector2(0f, 35f),
            new Vector2(650f, 3f), new Color(1f, 0.69f, 0.20f, 0.9f));

        TMP_Text eyebrow = CreateResumeText(panel.transform, "Eyebrow", font,
            "PROGRESS FOUND", 27f, new Color(0.36f, 0.88f, 1f, 1f),
            new Vector2(0f, 188f), new Vector2(780f, 48f));
        eyebrow.fontStyle = FontStyles.Bold;
        eyebrow.characterSpacing = 7f;

        _resumeQuestion = CreateResumeText(panel.transform, "Question", font,
            "CONTINUE FROM LEVEL 2?", 50f, Color.white,
            new Vector2(0f, 135f), new Vector2(820f, 70f));
        _resumeQuestion.fontStyle = FontStyles.Bold;

        TMP_Text hint = CreateResumeText(panel.transform, "Hint", font,
            "YOUR LAST LEVEL IS READY", 22f, new Color(0.72f, 0.76f, 0.88f, 1f),
            new Vector2(0f, 70f), new Vector2(760f, 32f));
        hint.characterSpacing = 3f;

        _resumeButtons = new Button[2];
        _resumeButtonImages = new Image[2];
        _resumeButtonLabels = new TMP_Text[2];
        CreateResumeButton(panel.transform, 0, font, new Vector2(0f, -32f), "CONTINUE LEVEL 2");
        CreateResumeButton(panel.transform, 1, font, new Vector2(0f, -155f), "START LEVEL 1");

        TMP_Text controls = CreateResumeText(panel.transform, "Controls", font,
            "BLACK  SELECT     •     WHITE  BACK", 19f,
            new Color(0.50f, 0.72f, 0.86f, 0.88f),
            new Vector2(0f, -242f), new Vector2(760f, 34f));
        controls.characterSpacing = 2f;

        _resumePromptRoot.SetActive(false);
    }

    void CreateResumeButton(Transform parent, int index, TMP_FontAsset font,
        Vector2 position, string label)
    {
        GameObject go = new GameObject(index == 0 ? "ContinueSavedLevel" : "StartLevelOne",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(650f, 92f);

        Image image = go.GetComponent<Image>();
        image.sprite = _resumeRoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = index == 0
            ? new Color(0.03f, 0.62f, 0.72f, 1f)
            : new Color(0.34f, 0.12f, 0.52f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        int capturedIndex = index;
        button.onClick.AddListener(() =>
        {
            SetResumeChoice(capturedIndex);
            StartGameAtLevel(capturedIndex == 0 ? _resumeLevelIndex : 0);
        });

        TMP_Text text = CreateResumeText(go.transform, "Label", font, label, 30f,
            Color.white, Vector2.zero, new Vector2(610f, 72f));
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 2f;
        text.raycastTarget = false;

        _resumeButtons[index] = button;
        _resumeButtonImages[index] = image;
        _resumeButtonLabels[index] = text;
    }

    TMP_Text CreateResumeText(Transform parent, string name, TMP_FontAsset font,
        string value, float size, Color color, Vector2 position, Vector2 dimensions)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = dimensions;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    void CreateResumeBar(Transform parent, string name, Vector2 position,
        Vector2 dimensions, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = dimensions;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
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

    void BuildResumeRoundedSprite()
    {
        if (_resumeRoundedSprite != null) return;

        const int size = 64;
        const int radius = 15;
        _resumeRoundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _resumeRoundedTexture.name = "Runtime Resume Rounded Rectangle";
        _resumeRoundedTexture.wrapMode = TextureWrapMode.Clamp;
        _resumeRoundedTexture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float cx = Mathf.Clamp(x, radius, size - 1 - radius);
            float cy = Mathf.Clamp(y, radius, size - 1 - radius);
            float dx = x - cx;
            float dy = y - cy;
            float alpha = dx * dx + dy * dy <= radius * radius ? 1f : 0f;
            _resumeRoundedTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        _resumeRoundedTexture.Apply();
        _resumeRoundedSprite = Sprite.Create(_resumeRoundedTexture,
            new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        _resumeRoundedSprite.name = "Runtime Resume Rounded Sprite";
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    void OnDestroy()
    {
        if (_resumeRoundedSprite != null) Destroy(_resumeRoundedSprite);
        if (_resumeRoundedTexture != null) Destroy(_resumeRoundedTexture);
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
