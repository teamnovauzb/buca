using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// One row in the leaderboard panel. Fades in with a slide-from-left
/// effect when populated. Uses TMP rich text with &lt;pos&gt; tags to
/// right-align the score number in a clean column.
/// </summary>
public class LeaderboardRow : MonoBehaviour
{
    public TMP_Text rowText;
    public RectTransform rectTransform;

    Vector2 _basePos;

    void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) _basePos = rectTransform.anchoredPosition;
    }

    public void Clear()
    {
        if (rowText == null) return;
        rowText.text = "";
        var c = rowText.color; c.a = 0f; rowText.color = c;
    }

    public void Populate(int rank, string playerName, int score, Color color)
    {
        if (rowText == null) return;
        // Truncate long names
        string name = string.IsNullOrEmpty(playerName) ? "---" : playerName;
        if (name.Length > 14) name = name.Substring(0, 14);
        // Format: #1   PlayerName                        1234
        rowText.text = $"#{rank,-3} {name}<pos=550><mspace=0.55em>{score,6}</mspace>";
        var c = color; c.a = 0f; rowText.color = c;
    }

    public IEnumerator FadeIn(float duration)
    {
        if (rowText == null || rectTransform == null) yield break;
        float t = 0f;
        Color target = rowText.color; target.a = 0.95f;
        Vector2 startPos = _basePos + new Vector2(-80f, 0f);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, _basePos, e);
            var c = rowText.color; c.a = e * target.a; rowText.color = c;
            yield return null;
        }
        rectTransform.anchoredPosition = _basePos;
        rowText.color = target;
    }
}
