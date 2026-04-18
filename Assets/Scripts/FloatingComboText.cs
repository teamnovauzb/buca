using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// One-shot floating praise text ("HOLE IN ONE!", "PERFECT!", "NICE SAVE!").
/// Call Show() with a message + color. Scales in with bounce, holds, then
/// floats up and fades out. Uses unscaled time so hit-stop doesn't pause it.
/// </summary>
public class FloatingComboText : MonoBehaviour
{
    public TMP_Text text;
    public CanvasGroup group;

    [Header("Motion")]
    public float driftPixels = 90f;
    public float inDuration  = 0.28f;
    public float holdDuration = 0.65f;
    public float outDuration = 0.4f;
    public float startScale = 1.8f;

    Vector2 _basePos;
    RectTransform _rt;
    Coroutine _playing;

    void Awake()
    {
        if (text != null) _rt = text.rectTransform;
        else _rt = GetComponent<RectTransform>();
        if (_rt != null) _basePos = _rt.anchoredPosition;
        if (group != null) group.alpha = 0f;
    }

    public void Show(string message, Color color)
    {
        if (text == null || _rt == null) return;
        if (_playing != null) StopCoroutine(_playing);
        _playing = StartCoroutine(Routine(message, color));
    }

    IEnumerator Routine(string message, Color color)
    {
        text.text = message;
        text.color = color;
        _rt.anchoredPosition = _basePos;
        _rt.localScale = Vector3.one * startScale;

        // In — scale down to 1 with overshoot + fade in
        float t = 0f;
        while (t < inDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / inDuration);
            float s = Mathf.Lerp(startScale, 1f, EaseOutBack(k, 1.7f));
            _rt.localScale = new Vector3(s, s, 1f);
            if (group != null) group.alpha = k;
            yield return null;
        }
        _rt.localScale = Vector3.one;
        if (group != null) group.alpha = 1f;

        // Hold
        float hold = 0f;
        while (hold < holdDuration) { hold += Time.unscaledDeltaTime; yield return null; }

        // Out — drift up + fade out
        t = 0f;
        while (t < outDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / outDuration);
            _rt.anchoredPosition = _basePos + new Vector2(0f, driftPixels * k);
            if (group != null) group.alpha = 1f - k;
            yield return null;
        }
        _rt.anchoredPosition = _basePos;
        if (group != null) group.alpha = 0f;
        _playing = null;
    }

    static float EaseOutBack(float t, float overshoot)
    {
        float s = t - 1f;
        return s * s * ((overshoot + 1f) * s + overshoot) + 1f;
    }
}
