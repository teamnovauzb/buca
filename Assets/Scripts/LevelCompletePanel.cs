using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Animated level-complete score breakdown card. Shows star rating,
/// then reveals each score line one at a time with a count-up effect,
/// then shows the total. A 5-second auto-advance timer counts down;
/// pressing Space (or tapping) skips to the next level immediately.
///
/// The panel is disabled by default and shown only via Show().
/// </summary>
public class LevelCompletePanel : MonoBehaviour
{
    [Header("Panel structure")]
    public CanvasGroup group;
    public RectTransform card;

    [Header("Rating icons (3 player-puck Images)")]
    public Image[] starImages;
    public Color starLit = Color.white;
    public Color starUnlit = new Color(0.34f, 0.46f, 0.58f, 0.55f);

    [Header("Score lines (TMP_Text — one per row)")]
    public TMP_Text baseText;
    public TMP_Text timeBonusText;
    public TMP_Text railBonusText;
    public TMP_Text strokeBonusText;
    public TMP_Text comboText;
    public TMP_Text dividerText;
    public TMP_Text totalText;

    [Header("Auto-advance")]
    public TMP_Text autoAdvanceText;
    public float autoAdvanceSeconds = 5f;

    [Header("Prebuilt golf scorecard")]
    public GolfScorecardView golfScorecard;

    [Header("Timing")]
    public float lineDelay = 0.22f;
    public float countUpDuration = 0.35f;
    public float starRevealDelay = 0.15f;

    bool _waitingForInput;
    bool _skipped;
    System.Action _onContinue;

    void Awake()
    {
        RestoreOriginalLayout();

        // Just zero out visuals — do NOT call SetActive(false) here.
        // The BucaSetupHelper already builds this panel as inactive.
        // If Awake() deactivates, it fights with Show()'s SetActive(true)
        // and kills the coroutine before it can start.
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
    }

    void Update()
    {
        if (!_waitingForInput) return;
        // Only player-owned action buttons skip. Orange/White belong to the
        // Luxodd system overlay/back flow and must not advance gameplay.
        // Space / mouse are retained silently for editor testing.
        if (ArcadeInputAdapter.AnyGameplayButtonDown()
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetMouseButtonDown(0))
        {
            _skipped = true;
        }
    }

    /// <summary>
    /// Shows the panel with full animated breakdown. Calls onContinue
    /// when the player presses Space/taps or the 5-second timer expires.
    /// </summary>
    public void Show(ScoreCalculator.ScoreBreakdown score, System.Action onContinue)
    {
        _onContinue = onContinue;
        _skipped = false;
        _waitingForInput = false;

        // Snappier reveal — cap the per-line timing so the breakdown reads fast.
        // (These serialized values were what made the results feel slow; capping
        // in code applies the fix regardless of what's saved on the scene object.)
        lineDelay       = Mathf.Min(lineDelay, 0.07f);
        countUpDuration = Mathf.Min(countUpDuration, 0.16f);
        starRevealDelay = Mathf.Min(starRevealDelay, 0.05f);

        // Must activate BEFORE StartCoroutine — Unity can't run
        // coroutines on inactive GameObjects.
        gameObject.SetActive(true);
        RestoreOriginalLayout();
        PopulateGolfScorecard();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelOpen();

        // Reset visuals immediately so the first frame isn't a flash of
        // stale content from the previous level.
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (card != null) card.localScale = Vector3.zero;

        StartCoroutine(RevealSequence(score));
    }

    public void Hide()
    {
        _waitingForInput = false;
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Restores the scene-authored result card. The generated 3D totem and its
    /// blurred backdrop are deliberately left disabled.
    /// </summary>
    void RestoreOriginalLayout()
    {
        Transform premiumViewport = transform.Find("PremiumResultTotem3DViewport");
        if (premiumViewport != null) premiumViewport.gameObject.SetActive(false);
        Transform blur = transform.Find("BlurredBackground");
        if (blur != null) blur.gameObject.SetActive(false);

        Transform dim = transform.Find("Dim");
        if (dim != null)
        {
            Image dimImage = dim.GetComponent<Image>();
            if (dimImage != null) dimImage.color = new Color(0.03f, 0.02f, 0.06f, 0.78f);
            dim.gameObject.SetActive(true);
        }
        if (card != null) card.gameObject.SetActive(true);

        if (starImages != null)
            foreach (Image icon in starImages)
                if (icon != null) icon.gameObject.SetActive(true);

        ShowLegacyLine(baseText);
        ShowLegacyLine(timeBonusText);
        ShowLegacyLine(railBonusText);
        ShowLegacyLine(strokeBonusText);
        ShowLegacyLine(comboText);
        ShowLegacyLine(dividerText);
        ShowLegacyLine(totalText);
        ShowLegacyLine(autoAdvanceText);
    }

    static void ShowLegacyLine(TMP_Text text)
    {
        if (text == null) return;
        text.gameObject.SetActive(true);
    }

    void PopulateGolfScorecard()
    {
        LevelManager manager = LevelManager.Instance;
        if (golfScorecard == null || manager == null) return;
        golfScorecard.Populate(manager.CampaignScorecard,
            manager.CurrentLevelNumber, manager.TotalLevels);
    }

    IEnumerator RevealSequence(ScoreCalculator.ScoreBreakdown score)
    {
        // Reset the original card before its score-count animation.
        ClearLine(baseText);
        ClearLine(timeBonusText);
        ClearLine(railBonusText);
        ClearLine(strokeBonusText);
        ClearLine(comboText);
        ClearLine(dividerText);
        ClearLine(totalText);
        ClearLine(autoAdvanceText);
        if (starImages != null)
        {
            foreach (Image star in starImages)
            {
                if (star == null) continue;
                star.gameObject.SetActive(true);
                Color hidden = Color.white;
                hidden.a = 0f;
                star.color = hidden;
                star.rectTransform.localScale = Vector3.one;
                star.rectTransform.localRotation = Quaternion.identity;
            }
        }

        // Fade and pop in the original scene-authored card.
        if (card != null) card.localScale = Vector3.one * 0.86f;
        float t = 0f, fadeDur = 0.24f;
        while (t < fadeDur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDur);
            float smooth = Mathf.SmoothStep(0f, 1f, k);
            if (group != null) group.alpha = smooth;
            if (card != null)
                card.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, EaseOutBack(k, 1.25f));
            yield return null;
        }
        if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
        if (card != null) card.localScale = Vector3.one;

        // Restore the old three-star reveal.
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                Color target = (i < score.stars) ? starLit : starUnlit;
                yield return AnimateStar(starImages[i], target);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayStarReveal(i);
                yield return WaitUnscaled(starRevealDelay);
            }
        }

        // Restore the original score breakdown and count-up sequence.
        yield return RevealLine(baseText, "BASE SCORE", score.basePoints);
        yield return RevealLine(timeBonusText, "TIME BONUS", score.timeBonus);
        yield return RevealLine(railBonusText, "RAIL BONUS", score.railBonus);
        yield return RevealLine(strokeBonusText, "STROKE BONUS", score.strokeBonus);

        if (comboText != null && score.comboMultiplier > 1f)
        {
            yield return WaitUnscaled(lineDelay);
            comboText.text = $"{score.comboName}<pos=550>× {score.comboMultiplier:0.0}";
            yield return FadeInText(comboText);
        }

        if (dividerText != null)
        {
            yield return WaitUnscaled(lineDelay);
            dividerText.text = "────────────────────────────";
            yield return FadeInText(dividerText);
        }

        if (totalText != null)
        {
            yield return WaitUnscaled(lineDelay);
            totalText.text = FormatLine("TOTAL", score.total);
            yield return FadeInText(totalText);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayScoreTick();
            yield return PunchScaleFlash(totalText, 1.10f, 0.24f,
                new Color(1f, 0.86f, 0.30f, 1f));
        }

        // Wait for input or auto-advance, as the old panel did.
        _waitingForInput = true;
        float countdown = autoAdvanceSeconds;
        if (autoAdvanceText != null)
            autoAdvanceText.color = new Color(0.70f, 0.88f, 1f, 0.75f);
        while (countdown > 0f && !_skipped)
        {
            countdown -= Time.unscaledDeltaTime;
            if (autoAdvanceText != null)
            {
                int secs = Mathf.Max(1, Mathf.CeilToInt(countdown));
                autoAdvanceText.text =
                    $"<color=#7BEAFF>NEXT LEVEL IN</color>  " +
                    $"<color=#FFD45C>{secs}s</color>";
                var c = autoAdvanceText.color;
                c.a = 0.75f;
                autoAdvanceText.color = c;
            }
            yield return null;
        }

        _waitingForInput = false;
        Hide();
        _onContinue?.Invoke();
    }

    IEnumerator RevealLine(TMP_Text text, string label, int value)
    {
        if (text == null) yield break;
        yield return WaitUnscaled(lineDelay);

        // Fade in the label with "0" then count up to real value
        text.text = FormatLine(label, 0);
        yield return FadeInText(text);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayScoreTick();

        float t = 0f;
        while (t < countUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / countUpDuration);
            // EaseOutQuad for the count-up
            float e = 1f - (1f - k) * (1f - k);
            int display = Mathf.RoundToInt(Mathf.Lerp(0f, value, e));
            text.text = FormatLine(label, display);
            yield return null;
        }
        text.text = FormatLine(label, value);
    }

    IEnumerator AnimateStar(Image img, Color target)
    {
        var rt = img.rectTransform;
        Vector2 restPosition = rt.anchoredPosition;
        const float duration = 0.42f;
        float t = 0f;
        bool earned = target.a > 0.8f;
        Color accent = earned ? new Color(0.58f, 0.96f, 1f, 1f) : target;

        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one * 0.72f;
        rt.anchoredPosition = restPosition + new Vector2(0f, -18f);
        Color start = accent;
        start.a = 0f;
        img.color = start;

        // A controlled rise, tiny overshoot and settle. No spinning and no
        // giant zoom: the movement feels like a premium award badge locking in.
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float rise = 1f - Mathf.Pow(1f - Mathf.Min(1f, k / 0.72f), 3f);
            float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, k));
            float s = k < 0.72f
                ? Mathf.Lerp(0.72f, 1.075f, rise)
                : Mathf.Lerp(1.075f, 1f, settle);
            rt.localScale = new Vector3(s, s, 1f);
            rt.anchoredPosition = Vector2.LerpUnclamped(
                restPosition + new Vector2(0f, -18f), restPosition, rise);

            Color c = Color.Lerp(accent, target, Mathf.SmoothStep(0.35f, 1f, k));
            c.a = Mathf.SmoothStep(0f, target.a, Mathf.Min(1f, k / 0.58f));
            img.color = c;
            yield return null;
        }

        // One subtle energy beat on earned pucks; disabled pucks settle quietly.
        if (earned)
        {
            t = 0f;
            const float pulseDuration = 0.14f;
            while (t < pulseDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / pulseDuration);
                float pulse = 1f + Mathf.Sin(k * Mathf.PI) * 0.035f;
                rt.localScale = Vector3.one * pulse;
                yield return null;
            }
        }

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = restPosition;
        img.color = target;
    }

    IEnumerator FadeInText(TMP_Text text)
    {
        if (text == null) yield break;
        var rt = text.rectTransform;
        Vector2 rest = rt.anchoredPosition;                // resting pos (no layout group → safe to slide)
        Vector2 from = rest + new Vector2(-120f, 0f);      // start off to the left
        Color baseC = text.color; baseC.a = 0.95f;         // the line's own color (set in scene)
        Color flash = Color.white; flash.a = 1f;           // brief white pop on entry
        float dur = 0.3f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = EaseOutBack(k, 2.1f);
            // Slide in from the left + pop + white flash — a lively, unmistakable entrance.
            rt.anchoredPosition = Vector2.LerpUnclamped(from, rest, e);
            float s = Mathf.Lerp(0.72f, 1f, e);
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-5f, 0f, e));
            Color c = Color.Lerp(flash, baseC, k);          // flash white → settle to color
            c.a = Mathf.Clamp01(k * 1.6f) * 0.95f;
            text.color = c;
            yield return null;
        }
        rt.anchoredPosition = rest;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        text.color = baseC;
    }

    // A quick scale-up + color flash, used to punctuate the TOTAL (the climax).
    IEnumerator PunchScaleFlash(TMP_Text text, float peak, float dur, Color flash)
    {
        if (text == null) yield break;
        var rt = text.rectTransform;
        Color baseColor = text.color;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float bump = Mathf.Sin(k * Mathf.PI);              // 0 → 1 → 0
            rt.localScale = Vector3.one * (1f + (peak - 1f) * bump);
            // quick damped wiggle for an impactful "slam" then settle
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 3f) * 5f * (1f - k));
            text.color = Color.Lerp(baseColor, flash, bump * 0.7f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        text.color = baseColor;
    }

    void ClearLine(TMP_Text text)
    {
        if (text == null) return;
        text.text = "";
        var c = text.color; c.a = 0f; text.color = c;
    }

    static string FormatLine(string label, int value)
    {
        // Label in proportional font, then jump to a fixed column for
        // the value. <pos=550> is far enough right that even "STROKE BONUS"
        // (the longest label) doesn't overlap the number.
        return $"{label}<pos=550><mspace=0.55em>{value,6}</mspace>";
    }

    static IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    static float EaseOutBack(float t, float overshoot)
    {
        float s = t - 1f;
        return s * s * ((overshoot + 1f) * s + overshoot) + 1f;
    }
}
