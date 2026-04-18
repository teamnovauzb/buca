using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a countdown timer HUD: a text label showing remaining seconds
/// + an optional fill bar that drains from right to left. Pulses red
/// and scales when time is critically low.
///
/// LevelManager calls SetTime() every frame. This component only displays;
/// the countdown logic lives in LevelManager.
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("Refs (assigned in scene or by BucaSetupHelper)")]
    public TMP_Text timerText;
    public Image timerBar;         // Image.type = Filled, fillMethod = Horizontal
    public Image timerBarBg;       // Background strip behind the fill bar

    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.9f);
    public Color warningColor = new Color(1f, 0.85f, 0.25f, 1f);
    public Color criticalColor = new Color(1f, 0.25f, 0.35f, 1f);

    [Header("Thresholds (fraction of total time remaining)")]
    [Tooltip("Below this fraction → warning (yellow).")]
    public float warningThreshold = 0.4f;
    [Tooltip("Below this fraction → critical (red + pulse).")]
    public float criticalThreshold = 0.18f;

    float _maxTime;
    float _currentTime;

    /// <summary>Call once when a new level loads to set the total time.</summary>
    public void Init(float maxTime)
    {
        _maxTime = maxTime;
        _currentTime = maxTime;
        if (timerBar != null) timerBar.fillAmount = 1f;
        UpdateVisuals();
        gameObject.SetActive(maxTime > 0f);
    }

    /// <summary>Called every frame by LevelManager with the remaining seconds.</summary>
    public void SetTime(float remaining)
    {
        _currentTime = Mathf.Max(0f, remaining);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (_maxTime <= 0f) return;
        float frac = _currentTime / _maxTime;

        // Text — show whole seconds, or 1-decimal when < 10s
        if (timerText != null)
        {
            if (_currentTime < 10f)
                timerText.text = _currentTime.ToString("F1");
            else
                timerText.text = Mathf.CeilToInt(_currentTime).ToString();
        }

        // Fill bar
        if (timerBar != null) timerBar.fillAmount = frac;

        // Color + pulse
        Color c;
        if (frac <= criticalThreshold)
        {
            c = criticalColor;
            // Fast pulse at critical — scales text between 1.0 and 1.2
            float pulse = 1f + 0.2f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            if (timerText != null)
                timerText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }
        else if (frac <= warningThreshold)
        {
            c = warningColor;
            if (timerText != null) timerText.rectTransform.localScale = Vector3.one;
        }
        else
        {
            c = normalColor;
            if (timerText != null) timerText.rectTransform.localScale = Vector3.one;
        }

        if (timerText != null) timerText.color = c;
        if (timerBar != null) timerBar.color = c;
    }

    /// <summary>Hides the timer (e.g. during transitions or when no time limit).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
