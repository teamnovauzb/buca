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

    [Header("Stars (3 Images)")]
    public Image[] starImages;
    public Color starLit = new Color(1f, 0.9f, 0.3f, 1f);
    public Color starUnlit = new Color(1f, 1f, 1f, 0.15f);

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

    [Header("Timing")]
    public float lineDelay = 0.22f;
    public float countUpDuration = 0.35f;
    public float starRevealDelay = 0.15f;

    bool _waitingForInput;
    bool _skipped;
    System.Action _onContinue;

    void Awake()
    {
        // Just zero out visuals — do NOT call SetActive(false) here.
        // The BucaSetupHelper already builds this panel as inactive.
        // If Awake() deactivates, it fights with Show()'s SetActive(true)
        // and kills the coroutine before it can start.
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
    }

    void Update()
    {
        if (!_waitingForInput) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)
            || ArcadeInputAdapter.ConfirmDown())
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

        // Must activate BEFORE StartCoroutine — Unity can't run
        // coroutines on inactive GameObjects.
        gameObject.SetActive(true);
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

    IEnumerator RevealSequence(ScoreCalculator.ScoreBreakdown score)
    {
        // Reset all lines to invisible
        ClearLine(baseText);
        ClearLine(timeBonusText);
        ClearLine(railBonusText);
        ClearLine(strokeBonusText);
        ClearLine(comboText);
        ClearLine(dividerText);
        ClearLine(totalText);
        ClearLine(autoAdvanceText);
        if (starImages != null)
            foreach (var s in starImages) if (s != null) s.color = new Color(1f, 1f, 1f, 0f);

        // Fade in panel
        if (card != null) card.localScale = Vector3.zero;
        float t = 0f, fadeDur = 0.4f;
        while (t < fadeDur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDur);
            if (group != null) group.alpha = k;
            if (card != null)
            {
                float s = EaseOutBack(k, 1.6f);
                card.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
        if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
        if (card != null) card.localScale = Vector3.one;

        // Stars — reveal one at a time with scale bounce
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                Color target = (i < score.stars) ? starLit : starUnlit;
                yield return AnimateStar(starImages[i], target);
                yield return WaitUnscaled(starRevealDelay);
            }
        }

        yield return WaitUnscaled(0.2f);

        // Score lines — each counts up from 0
        yield return RevealLine(baseText, "BASE", score.basePoints);
        yield return RevealLine(timeBonusText, "TIME BONUS", score.timeBonus);
        yield return RevealLine(railBonusText, "RAIL BONUS", score.railBonus);
        yield return RevealLine(strokeBonusText, "STROKE BONUS", score.strokeBonus);

        // Combo line (only if multiplier > 1)
        if (score.comboMultiplier > 1.001f)
        {
            yield return WaitUnscaled(lineDelay);
            if (comboText != null)
            {
                string comboVal = $"x{score.comboMultiplier:F1}";
                comboText.text = $"COMBO<pos=550><mspace=0.55em>{comboVal,6}</mspace>";
                yield return FadeInText(comboText);
            }
        }

        // Divider
        yield return WaitUnscaled(lineDelay);
        if (dividerText != null)
        {
            dividerText.text = "───────────────";
            yield return FadeInText(dividerText);
        }

        // Total — count up with bigger font moment
        yield return WaitUnscaled(lineDelay * 1.5f);
        yield return RevealLine(totalText, "TOTAL", score.total);

        // Now wait for input or auto-advance
        _waitingForInput = true;
        float countdown = autoAdvanceSeconds;
        while (countdown > 0f && !_skipped)
        {
            countdown -= Time.unscaledDeltaTime;
            if (autoAdvanceText != null)
            {
                int secs = Mathf.CeilToInt(countdown);
                autoAdvanceText.text = $"NEXT LEVEL IN {secs}s  |  PRESS SPACE";
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
        float dur = 0.3f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = EaseOutBack(k, 2f);
            float s = Mathf.Lerp(2f, 1f, e);
            rt.localScale = new Vector3(s, s, 1f);
            Color c = target;
            c.a = k * target.a;
            img.color = c;
            yield return null;
        }
        rt.localScale = Vector3.one;
        img.color = target;
    }

    IEnumerator FadeInText(TMP_Text text)
    {
        float dur = 0.18f, t = 0f;
        Color c = text.color;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / dur) * 0.95f;
            text.color = c;
            yield return null;
        }
        c.a = 0.95f;
        text.color = c;
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
