using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// "LEVEL FAILED" screen, shown by LevelManager when lives reach 0. Two choices:
///   • RETRY → reloads the same level (via the onRetry callback)
///   • EXIT  → returns to the main menu
///
/// Built + wired by the one-shot "RealBuca ▸ Add Lives System" tool. Stays GameObject-
/// active and hides via its CanvasGroup (so coroutines/animations are safe), matching
/// the LevelSelectController pattern. Works with mouse (editor) and the arcade buttons.
/// </summary>
public class LevelFailedPanel : MonoBehaviour
{
    [Header("Structure (wired by Add Lives System)")]
    public CanvasGroup group;
    public RectTransform card;
    public Button retryButton;
    public Button exitButton;

    [Header("Copy (auto-found by child name when left empty)")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text retryButtonText;
    public TMP_Text exitButtonText;

    [Header("Premium FX (optional — wired by Redesign Level Failed tool)")]
    public RectTransform titleRect;       // shaken on entry for impact
    public RectTransform[] popInOrder;    // staggered scale-pop after the card lands

    [Tooltip("Scene to load on Exit. Must be in Build Settings.")]
    public string mainMenuScene = "MainMenu";
    [Tooltip("Seconds allowed to choose after non-time-up losses. TIME'S UP stays until the player chooses Continue or End.")]
    [Min(3f)] public float sessionDecisionSeconds = 10f;

    [Header("Premium presentation")]
    [Tooltip("Upgrades older/plain generated panels at runtime, so every existing scene receives the same premium treatment without manual rewiring.")]
    public bool autoUpgradeVisuals = true;
    [Tooltip("Duration of the loss impact, card entrance, title reveal and button staging.")]
    [Range(0.65f, 1.6f)] public float revealDuration = 1.05f;

    System.Action _onRetry;
    System.Action _onEnd;
    bool _shown;
    float _shownTime;
    bool _sessionPrompt;
    bool _timeUpSessionPrompt;
    bool _secondChancePrompt;
    string _sessionStatus;
    bool _choicesReady;

    // Runtime premium layer. These are created once in Awake from lightweight
    // built-in UI graphics, keeping the scene asset and WebGL download small.
    Image _scrimImage;
    Image _cardImage;
    Image _frameImage;
    Image _glowImage;
    Image _impactFlash;
    Image _shockwave;
    Image _accentBar;
    Image _retryImage;
    Image _exitImage;
    RectTransform _frameRect;
    RectTransform _glowRect;
    RectTransform _impactFlashRect;
    RectTransform _shockwaveRect;
    RectTransform _warningBadge;
    RectTransform _retryRect;
    RectTransform _exitRect;
    CanvasGroup _cardGroup;
    CanvasGroup _subtitleGroup;
    CanvasGroup _retryGroup;
    CanvasGroup _exitGroup;
    TMP_Text _headerTag;
    TMP_Text _retryHint;
    TMP_Text _exitHint;
    PremiumFailureConsole3D _premium3D;
    Vector2 _cardHome;
    Vector2 _cardDesignSize;
    Color _subtitleBaseColor;
    readonly Color _cyan = new Color(0.20f, 0.78f, 0.90f, 1f);
    readonly Color _danger = new Color(1f, 0.25f, 0.36f, 1f);
    readonly Color _continueBase = new Color(0.035f, 0.34f, 0.32f, 1f);
    readonly Color _exitBase = new Color(0.055f, 0.062f, 0.095f, 1f);

    string _defaultTitle = "LEVEL FAILED";
    string _defaultSubtitle = "Out of lives";
    string _defaultRetry = "RETRY";
    string _defaultExit = "EXIT";

    void Awake()
    {
        // Some older generated scenes hid this canvas by serializing scale=0.
        // Restore layout here; CanvasGroup is the single source of visibility.
        if (transform.localScale == Vector3.zero) transform.localScale = Vector3.one;
        ResolveTextReferences();
        if (autoUpgradeVisuals) EnsurePremiumVisuals();
        CacheDefaultCopy();
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (retryButton != null) { retryButton.onClick.RemoveAllListeners(); retryButton.onClick.AddListener(Retry); }
        if (exitButton != null) { exitButton.onClick.RemoveAllListeners(); exitButton.onClick.AddListener(Exit); }
    }

    /// <summary>Called by LevelManager.FailSequence. onRetry reloads the level.</summary>
    public void Show(System.Action onRetry)
    {
        _onRetry = onRetry;
        _onEnd = null;
        _sessionPrompt = false;
        _timeUpSessionPrompt = false;
        _secondChancePrompt = false;
        _sessionStatus = null;
        ApplyCopy(_defaultTitle, _defaultSubtitle, _defaultRetry, _defaultExit);
        BeginShow();
    }

    /// <summary>
    /// Standalone equivalent of Luxodd's Continue transaction. Used only when
    /// no cabinet backend is connected, so Editor and desktop builds do not
    /// silently leave the game at time-up.
    /// </summary>
    public void ShowContinueChoice(string reason,
        System.Action onContinue, System.Action onEnd)
    {
        _onRetry = onContinue;
        _onEnd = onEnd;
        _sessionPrompt = true;
        _timeUpSessionPrompt = IsTimeUpReason(reason);
        _secondChancePrompt = false;
        _sessionStatus = "RESTART THIS LEVEL WITH FULL TIME";
        string heading = string.IsNullOrWhiteSpace(reason) ? "CONTINUE?" : reason.ToUpperInvariant();
        ApplyCopy(heading, _sessionStatus, "CONTINUE", "END GAME");
        BeginShow();
    }

    /// <summary>
    /// One-time Level 1 encouragement. It intentionally has no End option and no
    /// timeout: the player presses the primary button to receive the promised try.
    /// </summary>
    public void ShowFirstLevelSecondChance(System.Action onRetry)
    {
        _onRetry = onRetry;
        _onEnd = null;
        _sessionPrompt = false;
        _timeUpSessionPrompt = false;
        _secondChancePrompt = true;
        _sessionStatus = "THIS TIME YOU CAN DO IT";
        ApplyCopy("ONE MORE CHANCE FOR YOU", _sessionStatus, "TRY AGAIN", "END GAME");
        BeginShow();
    }

    void BeginShow()
    {
        StopAllCoroutines();
        if (autoUpgradeVisuals) EnsurePremiumVisuals();
        if (_premium3D != null)
        {
            _premium3D.SetSecondChanceMode(_secondChancePrompt);
            // A time-up decision must remain visible until the player chooses.
            _premium3D.Show(_sessionPrompt && !_timeUpSessionPrompt,
                Mathf.CeilToInt(sessionDecisionSeconds));
        }
        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            SetAnchored(exitRect, new Vector2(0f, _secondChancePrompt ? -182f : -164f),
                new Vector2(604f, 84f));
        }
        _shown = true;
        _choicesReady = false;
        _shownTime = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelOpen();
        StartCoroutine(FadeIn());
    }

    IEnumerator SessionDecisionTimeout()
    {
        float remaining = Mathf.Max(3f, sessionDecisionSeconds);
        while (_shown && _sessionPrompt && remaining > 0f)
        {
            if (subtitleText != null)
            {
                int displaySeconds = Mathf.CeilToInt(remaining);
                subtitleText.text = $"{_sessionStatus}   •   AUTO END {displaySeconds}s";
                if (_premium3D != null) _premium3D.SetCountdown(displaySeconds);
                bool urgent = remaining <= 3f;
                subtitleText.color = urgent
                    ? Color.Lerp(_subtitleBaseColor, _danger,
                        0.45f + 0.55f * Mathf.PingPong(Time.unscaledTime * 3.5f, 1f))
                    : _subtitleBaseColor;
            }
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (_shown && _sessionPrompt) Exit();
    }

    static bool IsTimeUpReason(string reason)
    {
        return !string.IsNullOrWhiteSpace(reason)
            && reason.IndexOf("TIME", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    IEnumerator FadeIn()
    {
        float total = Mathf.Clamp(revealDuration, 0.65f, 1.6f);
        float impactDur = total * 0.20f;
        float cardDur = total * 0.42f;

        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = true; // block gameplay clicks during reveal
        }
        if (_cardGroup != null) _cardGroup.alpha = 0f;
        if (_subtitleGroup != null) _subtitleGroup.alpha = 0f;
        if (_retryGroup != null) _retryGroup.alpha = 0f;
        if (_exitGroup != null) _exitGroup.alpha = 0f;
        if (_warningBadge != null) _warningBadge.localScale = Vector3.zero;
        if (_impactFlash != null) _impactFlash.color = new Color(_danger.r, _danger.g, _danger.b, 0f);

        if (card != null)
        {
            card.anchoredPosition = _cardHome + new Vector2(0f, 30f);
            card.localScale = Vector3.one * 0.92f;
            card.localRotation = Quaternion.identity;
        }
        if (_frameRect != null) _frameRect.localScale = Vector3.one * 0.96f;
        if (_glowRect != null) _glowRect.localScale = Vector3.one * 0.94f;
        if (_shockwaveRect != null) _shockwaveRect.localScale = Vector3.one * 0.60f;

        // Phase 1: full-screen red impact + expanding warning shockwave.
        float t = 0f;
        while (t < impactDur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / impactDur);
            if (group != null) group.alpha = Mathf.SmoothStep(0f, 1f, k);
            if (_impactFlash != null)
            {
                float a = Mathf.Sin(k * Mathf.PI) * 0.16f;
                _impactFlash.color = new Color(_danger.r, _danger.g, _danger.b, a);
            }
            if (_shockwaveRect != null)
                _shockwaveRect.localScale = Vector3.one * Mathf.Lerp(0.60f, 1.22f, EaseOutCubic(k));
            if (_shockwave != null)
                _shockwave.color = new Color(_danger.r, _danger.g, _danger.b, (1f - k) * 0.08f);
            yield return null;
        }
        if (_impactFlash != null) _impactFlash.color = new Color(_danger.r, _danger.g, _danger.b, 0f);
        if (_shockwave != null) _shockwave.color = new Color(_danger.r, _danger.g, _danger.b, 0f);

        // Phase 2: a controlled lift-and-settle. The old squash/rotation made
        // the modal feel like a debug HUD and distorted text during entry.
        t = 0f;
        while (t < cardDur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / cardDur);
            float settle = EaseOutBack(k, 0.85f);
            if (_cardGroup != null) _cardGroup.alpha = Mathf.Clamp01(k * 1.8f);
            if (card != null)
            {
                card.anchoredPosition = Vector2.LerpUnclamped(
                    _cardHome + new Vector2(0f, 30f), _cardHome, settle);
                float scale = Mathf.LerpUnclamped(0.92f, 1f, settle);
                card.localScale = Vector3.one * scale;
                card.localRotation = Quaternion.identity;
            }
            if (_frameRect != null) _frameRect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, settle);
            if (_glowRect != null) _glowRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, settle);
            yield return null;
        }
        if (card != null)
        {
            card.anchoredPosition = _cardHome;
            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;
        }
        if (_cardGroup != null) _cardGroup.alpha = 1f;
        if (_frameRect != null) _frameRect.localScale = Vector3.one;
        if (_glowRect != null) _glowRect.localScale = Vector3.one;

        // The approved 3D console is already complete when it settles.  Keep
        // both real button hit targets active without replaying old 2D pieces.
        if (_premium3D != null && _premium3D.IsReady)
        {
            if (_retryGroup != null) _retryGroup.alpha = 1f;
            if (_exitGroup != null) _exitGroup.alpha = 1f;
        }
        else
        {
            if (_warningBadge != null) yield return PopIn(_warningBadge, 0.16f, 1.25f);
            if (titleRect != null) yield return ShakeTitle();
            yield return FadeCanvas(_subtitleGroup, 0f, 1f, 0.12f);
            yield return RevealChoice(_retryRect, _retryGroup, 0.18f);
            yield return RevealChoice(_exitRect, _exitGroup, 0.16f);
        }

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        _choicesReady = true;
        _shownTime = Time.unscaledTime; // fresh grace after choices appear
        if (_sessionPrompt && !_timeUpSessionPrompt)
            StartCoroutine(SessionDecisionTimeout());
    }

    IEnumerator ShakeTitle()
    {
        Vector2 home = titleRect.anchoredPosition;
        float dur = 0.2f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            float damp = 1f - k;                                  // decays to 0
            float dx = Mathf.Sin(k * Mathf.PI * 7f) * 6f * damp;
            titleRect.anchoredPosition = home + new Vector2(dx, 0f);
            yield return null;
        }
        titleRect.anchoredPosition = home;
    }

    IEnumerator PopIn(RectTransform rt, float dur = 0.14f, float overshoot = 2.6f)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = Mathf.Lerp(0.4f, 1f, EaseOutBack(k, overshoot));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator RevealChoice(RectTransform rt, CanvasGroup choiceGroup, float duration)
    {
        if (rt == null) yield break;
        Vector2 home = rt.anchoredPosition;
        rt.anchoredPosition = home + new Vector2(34f, 0f);
        rt.localScale = new Vector3(0.93f, 0.93f, 1f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = EaseOutCubic(k);
            if (choiceGroup != null) choiceGroup.alpha = e;
            rt.anchoredPosition = Vector2.Lerp(home + new Vector2(34f, 0f), home, e);
            float s = Mathf.Lerp(0.93f, 1f, EaseOutBack(k, 1.2f));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.anchoredPosition = home;
        rt.localScale = Vector3.one;
        if (choiceGroup != null) choiceGroup.alpha = 1f;
    }

    IEnumerator FadeCanvas(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    void Update()
    {
        if (!_shown) return;

        AnimatePremiumIdle();

        if (!_choicesReady) return;
        if (Time.unscaledTime - _shownTime < 0.35f) return;   // grace so the fail input doesn't auto-press

        if (ArcadeInputAdapter.ConfirmDown() || Input.GetKeyDown(KeyCode.Return))
            Retry();
        else if (ArcadeInputAdapter.CancelDown()
                 || ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green)
                 || Input.GetKeyDown(KeyCode.Escape))
            Exit();
    }

    void Retry()
    {
        if (!_shown || !_choicesReady) return;
        _shown = false;
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Hide();
        var callback = _onRetry;
        ClearCallbacks();
        callback?.Invoke();
    }

    void Exit()
    {
        if (!_shown || !_choicesReady) return;
        _shown = false;
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        var callback = _onEnd;
        bool returnToMenu = !_sessionPrompt || callback == null;
        Hide();
        ClearCallbacks();
        if (callback != null) callback.Invoke();
        else if (returnToMenu) SceneManager.LoadScene(mainMenuScene);
    }

    void Hide()
    {
        _choicesReady = false;
        if (_premium3D != null) _premium3D.Hide();
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (card != null)
        {
            card.anchoredPosition = _cardHome;
            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;
        }
    }

    void ClearCallbacks()
    {
        _onRetry = null;
        _onEnd = null;
        _sessionPrompt = false;
        _timeUpSessionPrompt = false;
        _secondChancePrompt = false;
        _sessionStatus = null;
    }

    /// <summary>
    /// Converts the old flat card into a layered neon modal using only built-in
    /// UI primitives. It is idempotent and runs once, so WebGL receives no
    /// per-show allocations and old Game scenes do not need manual rewiring.
    /// </summary>
    void EnsurePremiumVisuals()
    {
        if (card == null) return;
        ResolveTextReferences();

        _cardImage = card.GetComponent<Image>();
        if (_cardImage != null)
            _cardImage.color = new Color(0.018f, 0.027f, 0.050f, 0.995f);

        _cardHome = card.anchoredPosition;
        _cardDesignSize = new Vector2(760f, 570f);
        card.sizeDelta = _cardDesignSize;
        _cardGroup = GetOrAddCanvasGroup(card.gameObject);

        Transform scrim = transform.Find("Scrim");
        if (scrim != null) _scrimImage = scrim.GetComponent<Image>();
        if (_scrimImage != null)
        {
            _scrimImage.color = new Color(0.003f, 0.008f, 0.022f, 0.90f);
            _scrimImage.raycastTarget = true;
        }

        Sprite panelSprite = _cardImage != null ? _cardImage.sprite : null;
        _impactFlash = GetOrCreateImage(transform, "PremiumImpactFlash", panelSprite);
        Stretch(_impactFlash.rectTransform);
        _impactFlash.raycastTarget = false;
        _impactFlashRect = _impactFlash.rectTransform;

        _shockwave = GetOrCreateImage(transform, "PremiumShockwave", panelSprite);
        ConfigureCentered(_shockwave.rectTransform, _cardHome, _cardDesignSize + new Vector2(90f, 70f));
        _shockwave.raycastTarget = false;
        _shockwaveRect = _shockwave.rectTransform;

        Image shadow = GetOrCreateImage(transform, "PremiumCardShadow", panelSprite);
        ConfigureCentered(shadow.rectTransform, _cardHome + new Vector2(0f, -12f),
            _cardDesignSize + new Vector2(30f, 30f));
        shadow.color = new Color(0f, 0f, 0f, 0.58f);
        shadow.raycastTarget = false;

        _glowImage = GetOrCreateImage(transform, "PremiumCardGlow", panelSprite);
        ConfigureCentered(_glowImage.rectTransform, _cardHome,
            _cardDesignSize + new Vector2(18f, 18f));
        _glowImage.color = new Color(_cyan.r, _cyan.g, _cyan.b, 0.04f);
        _glowImage.raycastTarget = false;
        _glowRect = _glowImage.rectTransform;

        _frameImage = GetOrCreateImage(transform, "PremiumCardFrame", panelSprite);
        ConfigureCentered(_frameImage.rectTransform, _cardHome,
            _cardDesignSize + new Vector2(4f, 4f));
        _frameImage.color = new Color(_cyan.r, _cyan.g, _cyan.b, 0.48f);
        _frameImage.raycastTarget = false;
        _frameRect = _frameImage.rectTransform;

        // Deterministic root draw order: scrim → impact → shockwave → shadow →
        // glow → frame → card. It stays correct even if the old scene had only
        // Scrim and Card.
        if (_scrimImage != null) _scrimImage.rectTransform.SetSiblingIndex(0);
        _impactFlashRect.SetSiblingIndex(1);
        _shockwaveRect.SetSiblingIndex(2);
        shadow.rectTransform.SetSiblingIndex(3);
        _glowRect.SetSiblingIndex(4);
        _frameRect.SetSiblingIndex(5);
        card.SetAsLastSibling();

        _accentBar = GetOrCreateImage(card, "PremiumAccentBar", null);
        SetAnchored(_accentBar.rectTransform, new Vector2(0f, 244f), new Vector2(112f, 4f));
        _accentBar.color = new Color(_danger.r, _danger.g, _danger.b, 0.82f);
        _accentBar.raycastTarget = false;

        Image separator = GetOrCreateImage(card, "PremiumSeparator", null);
        SetAnchored(separator.rectTransform, new Vector2(0f, -24f), new Vector2(540f, 1f));
        separator.color = new Color(_cyan.r, _cyan.g, _cyan.b, 0.10f);
        separator.raycastTarget = false;

        TMP_FontAsset font = titleText != null ? titleText.font
            : (subtitleText != null ? subtitleText.font : TMP_Settings.defaultFontAsset);

        _headerTag = GetOrCreateText(card, "PremiumHeaderTag", font);
        SetAnchored(_headerTag.rectTransform, new Vector2(0f, 161f), new Vector2(620f, 28f));
        _headerTag.text = "SESSION PAUSED";
        _headerTag.fontSize = 15f;
        _headerTag.fontStyle = FontStyles.Bold;
        _headerTag.characterSpacing = 4f;
        _headerTag.color = new Color(_cyan.r, _cyan.g, _cyan.b, 0.72f);
        _headerTag.alignment = TextAlignmentOptions.Center;

        Image badge = GetOrCreateImage(card, "PremiumWarningBadge", panelSprite);
        SetAnchored(badge.rectTransform, new Vector2(0f, 208f), new Vector2(48f, 48f));
        badge.color = _danger;
        badge.raycastTarget = false;
        badge.rectTransform.localRotation = Quaternion.identity;
        _warningBadge = badge.rectTransform;
        AddOutline(badge.gameObject, new Color(_danger.r, _danger.g, _danger.b, 0.30f), 3f);

        TMP_Text warningMark = GetOrCreateText(badge.transform, "Mark", font);
        Stretch(warningMark.rectTransform);
        warningMark.text = "!";
        warningMark.fontSize = 31f;
        warningMark.fontStyle = FontStyles.Bold;
        warningMark.color = Color.white;
        warningMark.alignment = TextAlignmentOptions.Center;
        warningMark.rectTransform.localRotation = Quaternion.identity;

        DisableLegacyCornerAccents(card);
        StylePrimaryCopy();
        StyleChoiceButton(retryButton, true, font);
        StyleChoiceButton(exitButton, false, font);

        _retryRect = retryButton != null ? retryButton.GetComponent<RectTransform>() : null;
        _exitRect = exitButton != null ? exitButton.GetComponent<RectTransform>() : null;
        _retryGroup = retryButton != null ? GetOrAddCanvasGroup(retryButton.gameObject) : null;
        _exitGroup = exitButton != null ? GetOrAddCanvasGroup(exitButton.gameObject) : null;
        _retryImage = retryButton != null ? retryButton.targetGraphic as Image : null;
        _exitImage = exitButton != null ? exitButton.targetGraphic as Image : null;

        if (subtitleText != null)
        {
            _subtitleGroup = GetOrAddCanvasGroup(subtitleText.gameObject);
            _subtitleBaseColor = new Color(0.76f, 0.84f, 0.94f, 0.94f);
            subtitleText.color = _subtitleBaseColor;
        }

        if (titleRect == null && titleText != null) titleRect = titleText.rectTransform;
        popInOrder = new[] { _warningBadge, _retryRect, _exitRect };

        EnsurePremium3DConsole(font);
    }

    void EnsurePremium3DConsole(TMP_FontAsset font)
    {
        _premium3D = PremiumFailureConsole3D.Ensure(card, font);
        if (_premium3D == null || !_premium3D.IsReady) return;

        // The RenderTexture supplies every visible element.  The old UGUI
        // controls stay only as transparent, correctly aligned hit targets.
        if (_cardImage != null) _cardImage.enabled = false;
        if (_frameImage != null) _frameImage.gameObject.SetActive(false);
        if (_glowImage != null) _glowImage.gameObject.SetActive(false);
        if (_impactFlash != null) _impactFlash.gameObject.SetActive(false);
        if (_shockwave != null) _shockwave.gameObject.SetActive(false);
        Transform shadow = transform.Find("PremiumCardShadow");
        if (shadow != null) shadow.gameObject.SetActive(false);
        if (_accentBar != null) _accentBar.gameObject.SetActive(false);
        Transform separator = card.Find("PremiumSeparator");
        if (separator != null) separator.gameObject.SetActive(false);
        if (_headerTag != null) _headerTag.gameObject.SetActive(false);
        if (_warningBadge != null) _warningBadge.gameObject.SetActive(false);
        if (titleText != null) titleText.enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        ConfigureInvisibleHitTarget(retryButton, retryButtonText,
            new Vector2(0f, -58f), new Vector2(604f, 84f));
        ConfigureInvisibleHitTarget(exitButton, exitButtonText,
            new Vector2(0f, -164f), new Vector2(604f, 84f));

        _premium3D.SetCopy(
            titleText != null ? titleText.text : "TIME'S UP!",
            retryButtonText != null ? retryButtonText.text : "CONTINUE",
            exitButtonText != null ? exitButtonText.text : "END GAME");
    }

    static void ConfigureInvisibleHitTarget(Button button, TMP_Text label,
        Vector2 position, Vector2 size)
    {
        if (button == null) return;
        RectTransform rt = button.GetComponent<RectTransform>();
        SetAnchored(rt, position, size);
        rt.SetAsLastSibling();
        button.transition = Selectable.Transition.None;
        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.enabled = true;
            image.raycastTarget = true;
            image.color = new Color(1f, 1f, 1f, 0.002f);
        }
        if (label != null) label.enabled = false;
        Shadow[] effects = button.GetComponents<Shadow>();
        for (int i = 0; i < effects.Length; i++) effects[i].enabled = false;
    }

    void StylePrimaryCopy()
    {
        if (titleText != null)
        {
            SetAnchored(titleText.rectTransform, new Vector2(0f, 91f), new Vector2(660f, 86f));
            titleText.fontSize = 68f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 1f;
            titleText.color = _danger;
            titleText.alignment = TextAlignmentOptions.Center;
            AddOutline(titleText.gameObject, new Color(0f, 0f, 0f, 0.72f), 2f);
        }

        if (subtitleText != null)
        {
            SetAnchored(subtitleText.rectTransform, new Vector2(0f, 23f), new Vector2(630f, 44f));
            subtitleText.fontSize = 23f;
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.characterSpacing = 0.8f;
            subtitleText.alignment = TextAlignmentOptions.Center;
        }
    }

    void StyleChoiceButton(Button button, bool primary, TMP_FontAsset font)
    {
        if (button == null) return;
        RectTransform rt = button.GetComponent<RectTransform>();
        SetAnchored(rt, new Vector2(0f, primary ? -91f : -188f), new Vector2(540f, 82f));

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.color = primary ? _continueBase : _exitBase;
            image.raycastTarget = true;
        }
        AddOutline(button.gameObject,
            primary ? new Color(_cyan.r, _cyan.g, _cyan.b, 0.52f)
                    : new Color(_danger.r, _danger.g, _danger.b, 0.24f),
            2f);
        AddShadow(button.gameObject, new Color(0f, 0f, 0f, 0.48f), new Vector2(0f, -5f));

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary
            ? new Color(0.80f, 1f, 0.98f, 1f)
            : new Color(1f, 0.84f, 0.86f, 1f);
        colors.pressedColor = new Color(0.78f, 0.84f, 0.88f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text label = primary ? retryButtonText : exitButtonText;
        if (label != null)
        {
            label.fontSize = 36f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 36f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 1.2f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(12f, 0f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);
        }

        Image keycap = GetOrCreateImage(button.transform, primary ? "BlackKeycap" : "WhiteKeycap",
            image != null ? image.sprite : null);
        keycap.gameObject.SetActive(false);
        SetAnchored(keycap.rectTransform, new Vector2(-209f, 0f), new Vector2(96f, 36f));
        keycap.color = primary ? new Color(0.015f, 0.02f, 0.03f, 1f) : new Color(0.94f, 0.96f, 1f, 1f);
        keycap.raycastTarget = false;
        AddOutline(keycap.gameObject,
            primary ? new Color(0.45f, 0.88f, 0.84f, 0.55f) : new Color(1f, 0.35f, 0.45f, 0.35f), 1f);

        TMP_Text keyLetter = GetOrCreateText(keycap.transform, "Letter", font);
        Stretch(keyLetter.rectTransform);
        keyLetter.text = primary ? "BLACK" : "WHITE";
        keyLetter.fontSize = 15f;
        keyLetter.fontStyle = FontStyles.Bold;
        keyLetter.characterSpacing = 1f;
        keyLetter.color = primary ? Color.white : new Color(0.08f, 0.10f, 0.14f, 1f);
        keyLetter.alignment = TextAlignmentOptions.Center;

        TMP_Text hint = GetOrCreateText(button.transform, "ControlHint", font);
        hint.gameObject.SetActive(false);
        if (primary) _retryHint = keyLetter; else _exitHint = keyLetter;
    }

    void DisableLegacyCornerAccents(RectTransform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            Transform horizontal = parent.Find($"CornerH{i}");
            Transform vertical = parent.Find($"CornerV{i}");
            if (horizontal != null) horizontal.gameObject.SetActive(false);
            if (vertical != null) vertical.gameObject.SetActive(false);
        }
    }

    void AnimatePremiumIdle()
    {
        if (!autoUpgradeVisuals || !_choicesReady) return;
        // The real 3D renderer owns all visible colours.  Do not let the old
        // UGUI hover pulse make its transparent button hit areas visible.
        if (_premium3D != null && _premium3D.IsReady) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.7f);
        if (_frameImage != null)
            _frameImage.color = new Color(_cyan.r, _cyan.g, _cyan.b, Mathf.Lerp(0.38f, 0.52f, pulse));
        if (_glowImage != null)
            _glowImage.color = new Color(_cyan.r, _cyan.g, _cyan.b, Mathf.Lerp(0.025f, 0.055f, pulse));
        if (_glowRect != null)
            _glowRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.006f, pulse);
        if (_accentBar != null)
            _accentBar.color = Color.Lerp(
                new Color(_danger.r, _danger.g, _danger.b, 0.70f),
                new Color(1f, 0.38f, 0.49f, 0.90f), pulse * 0.22f);
        if (_retryImage != null)
            _retryImage.color = Color.Lerp(_continueBase,
                new Color(0.045f, 0.39f, 0.36f, 1f), pulse * 0.18f);
        if (_exitImage != null) _exitImage.color = _exitBase;
    }

    static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        CanvasGroup found = go.GetComponent<CanvasGroup>();
        return found != null ? found : go.AddComponent<CanvasGroup>();
    }

    static Image GetOrCreateImage(Transform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null) go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    static TMP_Text GetOrCreateText(Transform parent, string name, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (existing == null) go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        if (text == null) text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.raycastTarget = false;
        return text;
    }

    static void ConfigureCentered(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void SetAnchored(RectTransform rt, Vector2 position, Vector2 size)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void AddOutline(GameObject go, Color color, float distance)
    {
        Outline outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
    }

    static void AddShadow(GameObject go, Color color, Vector2 distance)
    {
        Shadow[] shadows = go.GetComponents<Shadow>();
        Shadow shadow = null;
        for (int i = 0; i < shadows.Length; i++)
            if (!(shadows[i] is Outline)) { shadow = shadows[i]; break; }
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    void ResolveTextReferences()
    {
        if (card != null)
        {
            if (titleText == null)
            {
                Transform child = card.Find("Title");
                if (child != null) titleText = child.GetComponent<TMP_Text>();
            }
            if (subtitleText == null)
            {
                Transform child = card.Find("Sub");
                if (child != null) subtitleText = child.GetComponent<TMP_Text>();
            }
        }

        if (retryButtonText == null && retryButton != null)
            retryButtonText = retryButton.GetComponentInChildren<TMP_Text>(true);
        if (exitButtonText == null && exitButton != null)
            exitButtonText = exitButton.GetComponentInChildren<TMP_Text>(true);
    }

    void CacheDefaultCopy()
    {
        if (titleText != null && !string.IsNullOrWhiteSpace(titleText.text))
            _defaultTitle = titleText.text;
        if (subtitleText != null && !string.IsNullOrWhiteSpace(subtitleText.text))
            _defaultSubtitle = subtitleText.text;
        if (retryButtonText != null && !string.IsNullOrWhiteSpace(retryButtonText.text))
            _defaultRetry = retryButtonText.text;
        if (exitButtonText != null && !string.IsNullOrWhiteSpace(exitButtonText.text))
            _defaultExit = exitButtonText.text;
    }

    void ApplyCopy(string title, string subtitle, string retry, string exit)
    {
        string safeRetry = ForceButtonTextToOneLine(retry, "CONTINUE");
        string safeExit = ForceButtonTextToOneLine(exit, "END GAME");
        if (titleText != null) titleText.text = title;
        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.color = _subtitleBaseColor.a > 0f
                ? _subtitleBaseColor : new Color(0.76f, 0.84f, 0.94f, 0.94f);
        }
        if (retryButtonText != null)
        {
            retryButtonText.textWrappingMode = TextWrappingModes.NoWrap;
            retryButtonText.overflowMode = TextOverflowModes.Overflow;
            retryButtonText.text = safeRetry;
        }
        if (exitButtonText != null)
        {
            exitButtonText.textWrappingMode = TextWrappingModes.NoWrap;
            exitButtonText.overflowMode = TextOverflowModes.Overflow;
            exitButtonText.text = safeExit;
        }
        if (_headerTag != null)
            _headerTag.text = _secondChancePrompt
                ? "BUCA BONUS SHOT"
                : _sessionPrompt
                ? "SESSION PAUSED"
                : "ATTEMPT ENDED";
        if (_retryHint != null) _retryHint.text = "BLACK";
        if (_exitHint != null) _exitHint.text = "WHITE";
        if (_premium3D != null) _premium3D.SetCopy(title, safeRetry, safeExit);
    }

    static string ForceButtonTextToOneLine(string value, string fallback)
    {
        string safe = string.IsNullOrWhiteSpace(value) ? fallback : value;
        return safe.Replace('\r', ' ').Replace('\n', ' ').Trim().ToUpperInvariant();
    }

    static float EaseOutCubic(float t) { float x = 1f - Mathf.Clamp01(t); return 1f - x * x * x; }
    static float EaseOutBack(float t) { float s = t - 1f; return s * s * (2.70158f * s + 1.70158f) + 1f; }
    static float EaseOutBack(float t, float overshoot) { float s = t - 1f; return s * s * ((overshoot + 1f) * s + overshoot) + 1f; }
}
