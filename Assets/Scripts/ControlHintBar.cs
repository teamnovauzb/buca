using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom-of-screen hint bar that shows the player which arcade control
/// does what (joystick → AIM, Black button → CHARGE / FIRE, etc.).
///
/// Built by BucaSetupHelper.SpawnControlHintBar(). Each hint = a button
/// icon + a short label, laid out horizontally. Auto-fades out while the
/// puck is in flight, fades back in when the puck is idle. Hides
/// permanently after the player has taken `hideAfterShotsTaken` shots
/// in the CURRENT level — `ResetForNewLevel()` re-shows it whenever
/// LevelManager loads a new level so a hint reminder is always available
/// for the first few shots of every level.
/// </summary>
public class ControlHintBar : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup group;
    public LevelManager levelManager;

    [Header("Auto-fade behavior")]
    [Tooltip("Fade out while the puck is moving, in during idle. Set to false to keep hints always visible.")]
    public bool autoFadeWhileMoving = true;
    [Tooltip("Hide the bar after this many shots in the CURRENT level (resets each level). 0 = never hide.")]
    public int hideAfterShotsTaken = 3;
    public float fadeSpeed = 2.5f;
    [Range(0f, 1f)] public float idleAlpha = 0.95f;
    [Range(0f, 1f)] public float movingAlpha = 0.0f;
    [Tooltip("Always force the bar visible for this many seconds after a new level loads, even if the puck is moving.")]
    public float forceVisibleAtLevelStart = 1.5f;
    [Tooltip("Speed threshold above which the puck counts as 'moving'. Higher = ignores spawn-jitter / micro-noise.")]
    public float movingSpeedThreshold = 1.0f;

    int _shotsThisLevel;
    bool _permanentlyHidden;
    bool _lastWasIdle = true;
    float _levelStartTime;
    bool _everSawIdle; // don't count "shots" until puck has actually settled at least once

    // The quick-restart chip is a sibling of this auto-fading hint bar. It stays
    // visible while the puck is moving, because that is exactly when a player is
    // most likely to decide that a shot was bad and restart the attempt.
    RectTransform _quickRestartRoot;
    CanvasGroup _quickRestartGroup;
    TMP_Text _quickRestartLabel;
    TMP_Text _quickRestartSubLabel;
    Button _quickRestartButton;
    Image _quickRestartProgressFill;

    void Awake()
    {
        EnsureQuickRestartHint();
    }

    void OnEnable()
    {
        EnsureQuickRestartHint();
        if (_quickRestartRoot != null) _quickRestartRoot.gameObject.SetActive(true);

        // Reset per-level state every time the bar is re-enabled
        _levelStartTime = Time.unscaledTime;
        _shotsThisLevel = 0;
        _permanentlyHidden = false;
        _lastWasIdle = true;
        _everSawIdle = false;
        if (group != null) group.alpha = idleAlpha;
    }

    void OnDisable()
    {
        // LevelManager disables this bar for win/time-up/transaction screens.
        // The restart chip is a sibling, so explicitly hide it with the bar.
        if (_quickRestartRoot != null) _quickRestartRoot.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by LevelManager.LoadLevel after each level swap. Resets
    /// the per-level shot counter so the hint bar shows again for the
    /// first N shots of every new level.
    /// </summary>
    public void ResetForNewLevel()
    {
        _levelStartTime = Time.unscaledTime;
        _shotsThisLevel = 0;
        _permanentlyHidden = false;
        _lastWasIdle = true;
        _everSawIdle = false;
        if (group != null) group.alpha = idleAlpha;
    }

    void Update()
    {
        // Find LevelManager once it exists in the scene. Until it does,
        // hold the bar at idleAlpha so we never blank out the hints
        // while the scene is still bootstrapping.
        if (levelManager == null) levelManager = LevelManager.Instance;
        UpdateQuickRestartHint();

        if (group == null) return;
        if (levelManager == null)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, idleAlpha, fadeSpeed * Time.deltaTime);
            return;
        }

        // ── Shot detection (only counts after we've observed at least one
        //    truly-idle moment, so puck-spawn jitter doesn't fire shots).
        if (hideAfterShotsTaken > 0 && levelManager.puckRigidbody != null)
        {
            float speed = levelManager.puckRigidbody.linearVelocity.magnitude;
            bool moving = speed >= movingSpeedThreshold;
            if (!moving) _everSawIdle = true;
            if (_everSawIdle && moving && _lastWasIdle)
            {
                _shotsThisLevel++;
                if (_shotsThisLevel >= hideAfterShotsTaken)
                    _permanentlyHidden = true;
            }
            _lastWasIdle = !moving;
        }

        // ── Compute target alpha
        float target = idleAlpha;
        bool inGracePeriod = (Time.unscaledTime - _levelStartTime) < forceVisibleAtLevelStart;

        if (inGracePeriod)
        {
            // Always visible for a moment after a new level starts
            target = idleAlpha;
        }
        else if (_permanentlyHidden)
        {
            target = 0f;
        }
        else if (autoFadeWhileMoving && levelManager.puckRigidbody != null)
        {
            bool moving = levelManager.puckRigidbody.linearVelocity.magnitude > movingSpeedThreshold;
            target = moving ? movingAlpha : idleAlpha;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.deltaTime);
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    /// <summary>
    /// Adds a compact, always-readable restart instruction to existing Game
    /// scenes at runtime. No scene rebuild or manual Inspector wiring is needed.
    /// It intentionally lives outside the main auto-fading control bar.
    /// </summary>
    void EnsureQuickRestartHint()
    {
        if (_quickRestartRoot != null || transform.parent == null) return;

        Transform existing = transform.parent.Find("QuickRestartHint");
        if (existing != null)
        {
            _quickRestartRoot = existing as RectTransform;
            _quickRestartGroup = existing.GetComponent<CanvasGroup>();
            Transform title = existing.Find("Label");
            if (title != null) _quickRestartLabel = title.GetComponent<TMP_Text>();
            Transform subtitle = existing.Find("SubLabel");
            if (subtitle != null) _quickRestartSubLabel = subtitle.GetComponent<TMP_Text>();
            _quickRestartButton = existing.GetComponent<Button>();
            if (_quickRestartButton != null)
            {
                _quickRestartButton.onClick.RemoveListener(OnQuickRestartClicked);
                _quickRestartButton.onClick.AddListener(OnQuickRestartClicked);
            }
            Transform fill = existing.Find("ProgressFill");
            if (fill != null) _quickRestartProgressFill = fill.GetComponent<Image>();
            Transform oldCircle = existing.Find("PurpleRestartButton");
            if (oldCircle != null) oldCircle.gameObject.SetActive(false);
            return;
        }

        Sprite roundedSprite = null;
        Transform existingBackground = transform.Find("Background");
        if (existingBackground != null)
        {
            Image source = existingBackground.GetComponent<Image>();
            if (source != null) roundedSprite = source.sprite;
        }

        var root = new GameObject("QuickRestartHint", typeof(RectTransform),
            typeof(CanvasGroup), typeof(Image), typeof(Outline), typeof(Button));
        root.transform.SetParent(transform.parent, false);
        root.transform.SetAsLastSibling();
        _quickRestartRoot = (RectTransform)root.transform;
        _quickRestartRoot.anchorMin = new Vector2(1f, 1f);
        _quickRestartRoot.anchorMax = new Vector2(1f, 1f);
        _quickRestartRoot.pivot = new Vector2(1f, 1f);
        _quickRestartRoot.anchoredPosition = new Vector2(-32f, -28f);
        _quickRestartRoot.sizeDelta = new Vector2(360f, 78f);

        Image background = root.GetComponent<Image>();
        background.sprite = roundedSprite;
        background.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = new Color(0.015f, 0.025f, 0.075f, 0.90f);
        background.raycastTarget = true;

        Outline outline = root.GetComponent<Outline>();
        outline.effectColor = new Color(0.68f, 0.32f, 1f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        _quickRestartGroup = root.GetComponent<CanvasGroup>();
        _quickRestartGroup.alpha = 0f;
        _quickRestartGroup.interactable = false;
        _quickRestartGroup.blocksRaycasts = false;

        _quickRestartButton = root.GetComponent<Button>();
        _quickRestartButton.targetGraphic = background;
        _quickRestartButton.transition = Selectable.Transition.ColorTint;
        ColorBlock buttonColors = _quickRestartButton.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        buttonColors.pressedColor = new Color(0.80f, 0.72f, 0.92f, 1f);
        buttonColors.selectedColor = Color.white;
        buttonColors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        buttonColors.fadeDuration = 0.08f;
        _quickRestartButton.colors = buttonColors;
        _quickRestartButton.onClick.AddListener(OnQuickRestartClicked);

        RectTransform labelRect = CreateRect("Label", root.transform,
            new Vector2(16f, 0f), new Vector2(328f, 50f));
        _quickRestartLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_Text template = GetComponentInChildren<TMP_Text>(true);
        if (template != null && template.font != null)
            _quickRestartLabel.font = template.font;
        _quickRestartLabel.text = "RESTART";
        _quickRestartLabel.fontSize = 23f;
        _quickRestartLabel.fontStyle = FontStyles.Bold;
        _quickRestartLabel.alignment = TextAlignmentOptions.Center;
        _quickRestartLabel.color = new Color(0.82f, 0.92f, 1f, 1f);
        _quickRestartLabel.characterSpacing = 1.5f;
        _quickRestartLabel.textWrappingMode = TextWrappingModes.NoWrap;
        _quickRestartLabel.overflowMode = TextOverflowModes.Overflow;
        _quickRestartLabel.raycastTarget = false;

        RectTransform subLabelRect = CreateRect("SubLabel", root.transform,
            new Vector2(80f, -15f), new Vector2(258f, 22f));
        _quickRestartSubLabel = subLabelRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (template != null && template.font != null)
            _quickRestartSubLabel.font = template.font;
        _quickRestartSubLabel.text = string.Empty;
        _quickRestartSubLabel.fontSize = 13.5f;
        _quickRestartSubLabel.fontStyle = FontStyles.Bold;
        _quickRestartSubLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _quickRestartSubLabel.color = new Color(0.68f, 0.73f, 0.88f, 1f);
        _quickRestartSubLabel.characterSpacing = 1.1f;
        _quickRestartSubLabel.textWrappingMode = TextWrappingModes.NoWrap;
        _quickRestartSubLabel.overflowMode = TextOverflowModes.Overflow;
        _quickRestartSubLabel.raycastTarget = false;
        _quickRestartSubLabel.gameObject.SetActive(false);

        RectTransform trackRect = CreateRect("ProgressTrack", root.transform,
            new Vector2(16f, 5f), new Vector2(328f, 4f));
        trackRect.anchorMin = trackRect.anchorMax = trackRect.pivot = Vector2.zero;
        trackRect.anchoredPosition = new Vector2(16f, 5f);
        Image track = trackRect.gameObject.AddComponent<Image>();
        track.sprite = roundedSprite;
        track.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        track.color = new Color(0.38f, 0.24f, 0.55f, 0.42f);
        track.raycastTarget = false;

        RectTransform fillRect = CreateRect("ProgressFill", root.transform,
            new Vector2(16f, 5f), new Vector2(328f, 4f));
        fillRect.anchorMin = fillRect.anchorMax = fillRect.pivot = Vector2.zero;
        fillRect.anchoredPosition = new Vector2(16f, 5f);
        _quickRestartProgressFill = fillRect.gameObject.AddComponent<Image>();
        _quickRestartProgressFill.sprite = roundedSprite;
        _quickRestartProgressFill.type = roundedSprite != null ? Image.Type.Filled : Image.Type.Simple;
        _quickRestartProgressFill.fillMethod = Image.FillMethod.Horizontal;
        _quickRestartProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _quickRestartProgressFill.fillAmount = 0f;
        _quickRestartProgressFill.color = new Color(0.78f, 0.40f, 1f, 1f);
        _quickRestartProgressFill.raycastTarget = false;
    }

    void UpdateQuickRestartHint()
    {
        if (_quickRestartRoot == null) EnsureQuickRestartHint();
        if (_quickRestartRoot == null || _quickRestartGroup == null) return;

        bool available = levelManager != null && levelManager.QuickRestartAvailable;
        float progress = available ? levelManager.QuickRestartHoldProgress : 0f;
        float targetAlpha = available ? 0.94f : 0f;
        _quickRestartGroup.alpha = Mathf.MoveTowards(_quickRestartGroup.alpha,
            targetAlpha, 5f * Time.unscaledDeltaTime);
        _quickRestartGroup.interactable = available;
        _quickRestartGroup.blocksRaycasts = available;
        if (_quickRestartButton != null) _quickRestartButton.interactable = available;

        if (_quickRestartProgressFill != null)
            _quickRestartProgressFill.fillAmount = progress;
        if (_quickRestartLabel != null)
        {
            _quickRestartLabel.text = progress > 0.001f
                ? "RESTARTING..."
                : "RESTART";
            _quickRestartLabel.color = Color.Lerp(
                new Color(0.82f, 0.92f, 1f, 1f),
                new Color(0.92f, 0.68f, 1f, 1f), progress);
        }

        if (_quickRestartSubLabel != null)
            _quickRestartSubLabel.gameObject.SetActive(false);

        float eased = progress * progress * (3f - 2f * progress);
        _quickRestartRoot.localScale = Vector3.one * (1f + eased * 0.035f);
    }

    void OnQuickRestartClicked()
    {
        if (levelManager == null) levelManager = LevelManager.Instance;
        if (levelManager != null && levelManager.QuickRestartAvailable)
            levelManager.RestartCurrentLevel();
    }

    static RectTransform CreateRect(string objectName, Transform parent,
        Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    static Image CreateImage(string objectName, Transform parent, Sprite sprite,
        Color color, Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent, Vector2.zero, size);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
