using TMPro;
using UnityEngine;

/// <summary>
/// Displays the level countdown using scene-authored UI only. The component
/// updates text, color, and transform animation; it never creates renderers,
/// cameras, textures, meshes, materials, or helper GameObjects at runtime.
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("Prebuilt scene reference")]
    public TMP_Text timerText;

    [Header("BUCA HUD style")]
    public TMP_FontAsset hudFont;
    public TMP_FontAsset statsFont;

    [Header("Final countdown")]
    public Color normalColor = new Color(0.10f, 0.88f, 1f, 1f);
    public Color criticalColor = new Color(1f, 0.25f, 0.35f, 1f);
    [Min(1f)] public float finalWarningSeconds = 5f;

    [Header("Animation")]
    public float tickScaleBoost = 0.035f;
    public float tickDecayTime = 0.35f;
    public float smoothSpeed = 6f;

    float _maxTime;
    float _currentTime;
    int _lastDisplayedSecond = -1;
    float _currentTickBoost;
    float _currentPulse = 1f;
    float _warningBlend;
    Vector3 _baseScale = Vector3.one;

    void Awake()
    {
        _baseScale = transform.localScale;
        if (timerText == null)
        {
            Debug.LogError("[TimerDisplay] Prebuilt timerText is not assigned. " +
                           "The timer must be authored in the Game scene.", this);
            return;
        }

        timerText.raycastTarget = false;
    }

    public void Init(float maxTime)
    {
        _maxTime = Mathf.Max(0f, maxTime);
        _currentTime = _maxTime;
        _lastDisplayedSecond = -1;
        _currentTickBoost = 0f;
        _currentPulse = 1f;
        _warningBlend = 0f;
        transform.localScale = _baseScale;
        gameObject.SetActive(_maxTime > 0f);
        UpdateVisuals();
    }

    public void SetTime(float remaining)
    {
        _currentTime = Mathf.Max(0f, remaining);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (_maxTime <= 0f || timerText == null) return;

        int displaySecond = Mathf.CeilToInt(_currentTime);
        bool finalCountdown = displaySecond <= Mathf.CeilToInt(finalWarningSeconds);
        float lerpT = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        _warningBlend = Mathf.Lerp(_warningBlend, finalCountdown ? 1f : 0f, lerpT);

        float targetPulse = finalCountdown
            ? 1f + 0.01f * Mathf.Sin(Time.unscaledTime * 7f)
            : 1f;
        _currentPulse = Mathf.Lerp(_currentPulse, targetPulse, lerpT);

        if (displaySecond != _lastDisplayedSecond && _lastDisplayedSecond >= 0 && _currentTime > 0f)
            _currentTickBoost = Mathf.Max(_currentTickBoost, tickScaleBoost);

        if (displaySecond != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = displaySecond;
            timerText.text = displaySecond.ToString();
        }

        float tickLerp = 1f - Mathf.Exp(-(1f / Mathf.Max(0.01f, tickDecayTime)) * Time.unscaledDeltaTime);
        _currentTickBoost = Mathf.Lerp(_currentTickBoost, 0f, tickLerp);
        transform.localScale = _baseScale * (_currentPulse + _currentTickBoost);
        timerText.color = Color.Lerp(normalColor, criticalColor, _warningBlend);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
