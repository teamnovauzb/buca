using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Compact circular level timer. It is positioned on the same vertical
/// centreline as the top-right Restart chip, directly below it. The ring is
/// cyan during normal play and turns red/pulses only for 5, 4, 3, 2, 1.
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text timerText;

    [Header("BUCA HUD style")]
    [Tooltip("Bold arcade font shared by the timer and gameplay stats.")]
    public TMP_FontAsset hudFont;

    [Header("Circular timer")]
    public Color normalColor = new Color(0.10f, 0.88f, 1f, 1f);
    public Color criticalColor = new Color(1f, 0.25f, 0.35f, 1f);
    [Tooltip("The circle turns red and begins its urgent pulse at this many seconds.")]
    [Min(1f)] public float finalWarningSeconds = 5f;

    [Header("Animation")]
    [Tooltip("Peak scale boost on each new second. Low values feel subtle.")]
    public float tickScaleBoost = 0.08f;
    [Tooltip("Seconds for the tick bounce to fully decay.")]
    public float tickDecayTime = 0.35f;
    [Tooltip("How fast the display eases between phase colors + scales.")]
    public float smoothSpeed = 6f;

    float _maxTime;
    float _currentTime;
    int _lastDisplayedSecond = -1;
    int _lastTimeKey = int.MinValue;   // gates the per-frame timer-string rebuild

    // Smoothed runtime state — every frame we ease toward a target value
    // instead of recomputing from sin(time). This kills all visual snap.
    float _currentTickBoost;
    float _currentPulse = 1f;
    Color _currentColor;
    Vector3 _baseScale = Vector3.one;
    TMP_Text _combinedStatsText;
    Image _timerDisc;
    Image _timerTrack;
    Image _timerProgress;
    Image _timerGlow;
    int _lastHudStrokes = int.MinValue;
    int _lastHudPar = int.MinValue;
    int _lastHudScore = int.MinValue;
    static Sprite _discSprite;
    static Sprite _ringSprite;

    void Awake()
    {
        // Existing scenes serialized the former text timer as white with an
        // oversized 0.30 tick bounce. Normalize them to the approved circle so
        // the result is identical without manually rebuilding the scene.
        normalColor = new Color(0.10f, 0.88f, 1f, 1f);
        criticalColor = new Color(1f, 0.12f, 0.18f, 1f);
        finalWarningSeconds = 5f;
        tickScaleBoost = Mathf.Min(tickScaleBoost, 0.08f);
        ApplyPremiumColumnLayout();
        _baseScale = Vector3.one;
        _currentColor = normalColor;
    }

    public void Init(float maxTime)
    {
        // Re-apply the layout when a level starts.  This keeps older saved
        // scenes and every screen aspect ratio on the same clean HUD layout.
        ApplyPremiumColumnLayout();
        _maxTime = maxTime;
        _currentTime = maxTime;
        _lastDisplayedSecond = -1;
        _lastTimeKey = int.MinValue;
        _lastHudStrokes = int.MinValue;
        _lastHudPar = int.MinValue;
        _lastHudScore = int.MinValue;
        _currentTickBoost = 0f;
        _currentPulse = 1f;
        _currentColor = normalColor;
        UpdateVisuals();
        gameObject.SetActive(maxTime > 0f);
        if (_combinedStatsText != null)
            _combinedStatsText.gameObject.SetActive(maxTime > 0f);
    }

    public void SetTime(float remaining)
    {
        _currentTime = Mathf.Max(0f, remaining);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (_maxTime <= 0f || timerText == null) return;

        float fraction = Mathf.Clamp01(_currentTime / _maxTime);
        int displaySecond = Mathf.CeilToInt(_currentTime);
        bool finalCountdown = displaySecond <= Mathf.CeilToInt(finalWarningSeconds);
        Color targetColor = finalCountdown ? criticalColor : normalColor;
        float targetPulse = finalCountdown
            ? 1f + 0.055f * Mathf.Sin(Time.unscaledTime * 7f)
            : 1f;

        // ─── Ease smoothly toward targets ───────────────────────
        // Exponential smoothing frame-rate-independent via unscaled dt.
        float lerpT = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        _currentColor = Color.Lerp(_currentColor, targetColor, lerpT);
        _currentPulse = Mathf.Lerp(_currentPulse, targetPulse, lerpT);

        // ─── Tick boost: gentle exponential decay ───────────────
        // Each second the integer ticks, add a small boost. Decay
        // exponentially so it fades organically instead of linearly.
        if (displaySecond != _lastDisplayedSecond && _lastDisplayedSecond >= 0 && _currentTime > 0f)
        {
            _currentTickBoost = Mathf.Max(_currentTickBoost, tickScaleBoost);
        }
        _lastDisplayedSecond = displaySecond;

        float tickLerp = 1f - Mathf.Exp(-(1f / Mathf.Max(0.01f, tickDecayTime)) * Time.unscaledDeltaTime);
        _currentTickBoost = Mathf.Lerp(_currentTickBoost, 0f, tickLerp);

        // ─── Apply to transform ─────────────────────────────────
        float finalScale = _currentPulse + _currentTickBoost;
        transform.localScale = _baseScale * finalScale;
        timerText.color = Color.white;
        if (_timerProgress != null)
        {
            _timerProgress.fillAmount = fraction;
            _timerProgress.color = _currentColor;
        }
        if (_timerTrack != null)
            _timerTrack.color = finalCountdown
                ? new Color(0.58f, 0.045f, 0.070f, 0.96f)
                : new Color(0.035f, 0.48f, 0.58f, 0.94f);
        if (_timerGlow != null)
        {
            Color glow = _currentColor;
            glow.a = finalCountdown
                ? 0.40f + 0.20f * Mathf.PingPong(Time.unscaledTime * 3.5f, 1f)
                : 0.28f;
            _timerGlow.color = glow;
        }

        // ─── Text content ───────────────────────────────────────
        // Pad the number to a consistent visual width so changing
        // digits don't shift the text's center (a common source of
        // perceived jitter).
        // Rebuild the string ONLY when the shown value or phase color changes
        // (not 60×/sec) — avoids a per-frame string allocation. The smooth color
        // ease still applies every frame via timerText.color above, so it looks
        // identical. Always show whole seconds so the timer reads as one clear
        // sequence: 30, 29, 28 ... 2, 1. Decimal tenths looked random to kids.
        int timeKey = displaySecond;
        LevelManager manager = LevelManager.Instance;
        int hudStrokes = manager != null ? manager.CurrentShotCount : 0;
        int hudPar = manager != null ? manager.CurrentPar : 1;
        int hudScore = manager != null ? manager.CurrentDisplayedScore : 0;
        bool statsChanged = hudStrokes != _lastHudStrokes
                         || hudPar != _lastHudPar
                         || hudScore != _lastHudScore;

        if (timeKey != _lastTimeKey)
        {
            _lastTimeKey = timeKey;
            timerText.text = displaySecond.ToString();
        }

        if (statsChanged)
        {
            _lastHudStrokes = hudStrokes;
            _lastHudPar = hudPar;
            _lastHudScore = hudScore;
            if (_combinedStatsText != null)
            {
                _combinedStatsText.text =
                    $"<color=#DDF7FF>STROKES</color><pos=260><color=#FFFFFF>{hudStrokes}</color>\n" +
                    $"<color=#DDF7FF>PAR</color><pos=260><color=#FFD633>{hudPar}</color>\n" +
                    $"<color=#DDF7FF>SCORE</color><pos=260><color=#00DFFF>{hudScore:N0}</color>";
            }
        }
    }

    /// <summary>
    /// Keeps STROKES, PAR and SCORE in the top-left while the timer gets its own
    /// unmissable position below Restart in the opposite corner.
    /// </summary>
    void ApplyPremiumColumnLayout()
    {
        if (timerText == null) return;

        Transform hud = transform.parent;
        TMP_Text strokes = hud != null ? hud.Find("ShotCounter")?.GetComponent<TMP_Text>() : null;
        TMP_Text score = hud != null ? hud.Find("LiveScore")?.GetComponent<TMP_Text>() : null;

        // Remove the rejected rail/wood chrome. The final HUD is one TMP block,
        // guaranteeing that the four rows can never drift out of order.
        Transform rejectedChrome = hud != null ? hud.Find("SelectedRailHudChrome") : null;
        if (rejectedChrome != null)
        {
            rejectedChrome.gameObject.SetActive(false);
            Destroy(rejectedChrome.gameObject);
        }

        _combinedStatsText = strokes;
        if (_combinedStatsText != null) _combinedStatsText.gameObject.SetActive(true);
        if (score != null) score.gameObject.SetActive(false);

        ConfigureTopLeft(strokes != null ? strokes.rectTransform : null,
            new Vector2(48f, -38f), new Vector2(465f, 224f));

        EnsureCircularTimer();

        ApplyTextStyle(strokes, 38f, true);
    }

    /// <summary>
    /// Creates the approved small circle from lightweight runtime UI shapes.
    /// Restart is 360 px wide, right-aligned at x=-32, so its exact centre is
    /// x=-212. The timer uses that same x coordinate and sits just below it.
    /// </summary>
    void EnsureCircularTimer()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || timerText == null) return;

        root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(-212f, -168f);
        root.sizeDelta = new Vector2(96f, 96f);
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        if (_discSprite == null) _discSprite = CreateCircularSprite(false);
        if (_ringSprite == null) _ringSprite = CreateCircularSprite(true);

        _timerDisc = EnsureImage("Circle Back", root, _discSprite,
            new Vector2(90f, 90f), new Color(0.018f, 0.075f, 0.105f, 0.98f));
        _timerGlow = EnsureImage("Circle Glow", root, _ringSprite,
            new Vector2(106f, 106f), new Color(0.10f, 0.88f, 1f, 0.28f));
        _timerTrack = EnsureImage("Circle Track", root, _ringSprite,
            new Vector2(96f, 96f), new Color(0.035f, 0.48f, 0.58f, 0.94f));
        _timerProgress = EnsureImage("Circle Progress", root, _ringSprite,
            new Vector2(96f, 96f), normalColor);
        _timerProgress.type = Image.Type.Filled;
        _timerProgress.fillMethod = Image.FillMethod.Radial360;
        _timerProgress.fillOrigin = (int)Image.Origin360.Top;
        _timerProgress.fillClockwise = true;
        _timerProgress.fillAmount = 1f;

        _timerDisc.transform.SetAsFirstSibling();
        _timerGlow.transform.SetSiblingIndex(1);
        _timerTrack.transform.SetSiblingIndex(2);
        _timerProgress.transform.SetSiblingIndex(3);

        RectTransform timerRt = timerText.rectTransform;
        // A fixed centred rect is deliberate here. Some older scenes serialized
        // the legacy timer text with a top-half stretch anchor, which could leave
        // the number below the runtime circle even after its offsets were reset.
        timerRt.anchorMin = timerRt.anchorMax = new Vector2(0.5f, 0.5f);
        timerRt.pivot = new Vector2(0.5f, 0.5f);
        timerRt.anchoredPosition = Vector2.zero;
        timerRt.sizeDelta = new Vector2(88f, 88f);
        timerRt.localScale = Vector3.one;
        timerRt.localRotation = Quaternion.identity;
        timerText.gameObject.SetActive(true);
        if (hudFont != null)
        {
            timerText.font = hudFont;
            if (hudFont.material != null) timerText.fontSharedMaterial = hudFont.material;
        }
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.fontSize = 43f;
        timerText.enableAutoSizing = true;
        timerText.fontSizeMin = 30f;
        timerText.fontSizeMax = 43f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.textWrappingMode = TextWrappingModes.NoWrap;
        timerText.overflowMode = TextOverflowModes.Overflow;
        timerText.raycastTarget = false;
        timerText.color = Color.white;
        timerText.outlineWidth = 0.18f;
        timerText.outlineColor = new Color(0f, 0f, 0f, 0.92f);
        timerText.transform.SetAsLastSibling();
    }

    static Image EnsureImage(string objectName, Transform parent, Sprite sprite,
        Vector2 size, Color color)
    {
        Transform existing = parent.Find(objectName);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
        if (existing == null) go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Sprite CreateCircularSprite(bool ring)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = ring ? "Buca Timer Ring" : "Buca Timer Disc",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - half) / half;
            float dy = (y - half) / half;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float outer = 1f - Mathf.SmoothStep(0.94f, 1f, distance);
            float alpha = ring
                ? outer * Mathf.SmoothStep(0.70f, 0.77f, distance)
                : outer;
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    void ApplyTextStyle(TMP_Text text, float size, bool twoLines)
    {
        if (text == null) return;

        // Oswald Bold gives the selected compact arcade/sports silhouette.
        TMP_FontAsset selectedFont = hudFont != null ? hudFont
            : (timerText != null ? timerText.font : null);
        if (selectedFont != null)
        {
            text.font = selectedFont;
            if (selectedFont.material != null)
                text.fontSharedMaterial = selectedFont.material;
        }

        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.characterSpacing = 1.2f;
        text.lineSpacing = twoLines ? 18f : 0f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = true;
        text.raycastTarget = false;
        text.color = Color.white;
        text.outlineWidth = 0.14f;
        text.outlineColor = new Color(0.005f, 0.015f, 0.025f, 0.96f);

        // Soft cyan underlay reproduces the restrained neon edge from the
        // selected mockup without making the glyphs fuzzy.
        Material mat = text.fontMaterial;
        if (mat != null && mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
        {
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor,
                new Color(0.08f, 0.78f, 1f, 0.18f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.22f);
        }
    }

    static void ConfigureTopLeft(RectTransform rt, Vector2 position, Vector2 size)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    public void Hide()
    {
        if (_combinedStatsText != null)
            _combinedStatsText.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}
