using UnityEngine;
using TMPro;

/// <summary>
/// Smooth text-only countdown timer.
/// Format: "TIME LEFT  X  SECONDS" with animated number.
///
/// Key smoothness choices:
///   - Pulse scale is interpolated toward a target each frame (not
///     recomputed from raw sin(time)), so transitions between phases
///     don't snap.
///   - Tick boost decays exponentially, producing a gentle bounce
///     rather than an instant snap.
///   - No rotational wobble (was the main source of jitter).
///   - Color lerped smoothly across phase boundaries.
///   - Number is padded to a constant character width so TMP's
///     auto-layout doesn't shift the text center as digits change.
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text timerText;

    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.95f);
    public Color warningColor = new Color(1f, 0.85f, 0.25f, 1f);
    public Color criticalColor = new Color(1f, 0.25f, 0.35f, 1f);

    [Header("Thresholds (fraction of total time remaining)")]
    public float warningThreshold = 0.4f;
    public float criticalThreshold = 0.18f;

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

    // Smoothed runtime state — every frame we ease toward a target value
    // instead of recomputing from sin(time). This kills all visual snap.
    float _currentTickBoost;
    float _currentPulse = 1f;
    Color _currentColor;
    Vector3 _baseScale = Vector3.one;

    void Awake()
    {
        if (timerText != null && timerText.rectTransform.localScale != Vector3.zero)
            _baseScale = timerText.rectTransform.localScale;
        _currentColor = normalColor;
    }

    public void Init(float maxTime)
    {
        _maxTime = maxTime;
        _currentTime = maxTime;
        _lastDisplayedSecond = -1;
        _currentTickBoost = 0f;
        _currentPulse = 1f;
        _currentColor = normalColor;
        UpdateVisuals();
        gameObject.SetActive(maxTime > 0f);
    }

    public void SetTime(float remaining)
    {
        _currentTime = Mathf.Max(0f, remaining);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (_maxTime <= 0f || timerText == null) return;

        float frac = _currentTime / _maxTime;

        // ─── Determine phase target values ──────────────────────
        Color targetColor;
        float targetPulse;
        string numColorHex;

        if (frac <= criticalThreshold)
        {
            targetColor = criticalColor;
            // Gentle breathing — slow enough to read as a pulse, not a flicker.
            targetPulse = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f);
            numColorHex = "FF4055";
        }
        else if (frac <= warningThreshold)
        {
            targetColor = warningColor;
            targetPulse = 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 2.5f);
            numColorHex = "FFC94A";
        }
        else
        {
            targetColor = normalColor;
            targetPulse = 1f + 0.015f * Mathf.Sin(Time.unscaledTime * 1.2f);
            numColorHex = "FFFFFF";
        }

        // ─── Ease smoothly toward targets ───────────────────────
        // Exponential smoothing frame-rate-independent via unscaled dt.
        float lerpT = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        _currentColor = Color.Lerp(_currentColor, targetColor, lerpT);
        _currentPulse = Mathf.Lerp(_currentPulse, targetPulse, lerpT);

        // ─── Tick boost: gentle exponential decay ───────────────
        // Each second the integer ticks, add a small boost. Decay
        // exponentially so it fades organically instead of linearly.
        int displaySecond = Mathf.CeilToInt(_currentTime);
        if (displaySecond != _lastDisplayedSecond && _lastDisplayedSecond >= 0 && _currentTime > 0f)
        {
            _currentTickBoost = Mathf.Max(_currentTickBoost, tickScaleBoost);
        }
        _lastDisplayedSecond = displaySecond;

        float tickLerp = 1f - Mathf.Exp(-(1f / Mathf.Max(0.01f, tickDecayTime)) * Time.unscaledDeltaTime);
        _currentTickBoost = Mathf.Lerp(_currentTickBoost, 0f, tickLerp);

        // ─── Apply to transform ─────────────────────────────────
        float finalScale = _currentPulse + _currentTickBoost;
        timerText.rectTransform.localScale = _baseScale * finalScale;
        timerText.color = _currentColor;

        // ─── Text content ───────────────────────────────────────
        // Pad the number to a consistent visual width so changing
        // digits don't shift the text's center (a common source of
        // perceived jitter).
        string numberStr;
        if (_currentTime < 10f)
            numberStr = _currentTime.ToString("F1");
        else
            numberStr = displaySecond.ToString();

        string word = (displaySecond == 1) ? "SECOND" : "SECONDS";
        timerText.text = $"TIME LEFT  <size=140%><color=#{numColorHex}><mspace=0.55em>{numberStr,-4}</mspace></color></size>{word}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
