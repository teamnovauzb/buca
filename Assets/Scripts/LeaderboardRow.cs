using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// One row in the leaderboard panel. Supports two layouts:
///
/// • Separated mode (preferred, used by the new BucaSetupHelper layout):
///     rankText + nameText + scoreText + rankBadge image, each field
///     animated independently. Score counts up from 0.
///
/// • Combined mode (legacy): a single rowText that uses TMP &lt;pos&gt;
///     tags. Kept for backward compatibility with older scenes.
///
/// On Populate the row caches the target values; the entry animation
/// is driven by FadeIn() which slides + fades + counts up the score
/// + scale-pops the rank badge.
/// </summary>
public class LeaderboardRow : MonoBehaviour
{
    [Header("Mode A: combined text (legacy)")]
    public TMP_Text rowText;

    [Header("Mode B: separated texts (preferred)")]
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text scoreText;
    [Tooltip("Optional colored circle behind the rank number (gold/silver/bronze for top 3).")]
    public Image rankBadge;
    [Tooltip("Optional row background image — tinted yellow on the player's row.")]
    public Image rowBackground;

    public RectTransform rectTransform;

    [Header("Top-3 badge colors")]
    public Color goldBadgeColor   = new Color(1.00f, 0.84f, 0.25f, 1f);
    public Color silverBadgeColor = new Color(0.82f, 0.86f, 0.92f, 1f);
    public Color bronzeBadgeColor = new Color(0.86f, 0.55f, 0.30f, 1f);
    public Color defaultBadgeColor = new Color(0.30f, 0.22f, 0.45f, 1f);
    public Color playerRowTintColor = new Color(1f, 0.85f, 0.3f, 0.20f);

    Vector2 _basePos;

    // Cached populate values for FadeIn to consume
    int _targetScore;
    Color _targetTextColor = Color.white;
    Color _targetBadgeColor;
    Color _targetBgColor;
    bool _isPlayerRow;
    int _displayedScore;
    Coroutine _pulseCo;

    void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) _basePos = rectTransform.anchoredPosition;
    }

    public void Clear()
    {
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
        SetAlpha(rowText, 0f);
        SetAlpha(rankText, 0f);
        SetAlpha(nameText, 0f);
        SetAlpha(scoreText, 0f);
        if (rankBadge != null) { var c = rankBadge.color; c.a = 0f; rankBadge.color = c; }
        if (rowBackground != null) { var c = rowBackground.color; c.a = 0f; rowBackground.color = c; }
        if (rankText != null) rankText.text = "";
        if (nameText != null) nameText.text = "";
        if (scoreText != null) scoreText.text = "";
        if (rowText != null) rowText.text = "";
        _displayedScore = 0;
    }

    public void Populate(int rank, string playerName, int score, Color textColor)
    {
        _targetScore = score;
        _targetTextColor = textColor;
        // Player row = caller passed our highlight color (RGB matches playerRowTintColor's hue)
        _isPlayerRow = textColor.r > 0.9f && textColor.g > 0.7f && textColor.b < 0.5f;

        // Determine badge color from rank
        if (rank == 1) _targetBadgeColor = goldBadgeColor;
        else if (rank == 2) _targetBadgeColor = silverBadgeColor;
        else if (rank == 3) _targetBadgeColor = bronzeBadgeColor;
        else _targetBadgeColor = defaultBadgeColor;
        _targetBgColor = _isPlayerRow ? playerRowTintColor : new Color(0f, 0f, 0f, 0f);

        // Mode A: combined
        if (rowText != null && (nameText == null && scoreText == null))
        {
            string nA = string.IsNullOrEmpty(playerName) ? "---" : playerName;
            if (nA.Length > 14) nA = nA.Substring(0, 14);
            rowText.text = $"#{rank,-3} {nA}<pos=550><mspace=0.55em>{score,6}</mspace>";
            var c = textColor; c.a = 0f; rowText.color = c;
            return;
        }

        // Mode B: separated
        if (rankText != null)
        {
            rankText.text = rank > 0 ? rank.ToString() : "-";
            // Top-3 ranks get a punchier color; rest match row text
            Color rc = (rank >= 1 && rank <= 3) ? new Color(0.08f, 0.04f, 0.18f, 1f) : textColor;
            rc.a = 0f; rankText.color = rc;
        }
        if (nameText != null)
        {
            string n = string.IsNullOrEmpty(playerName) ? "---" : playerName;
            if (n.Length > 16) n = n.Substring(0, 16);
            nameText.text = n.ToUpperInvariant();
            var c = textColor; c.a = 0f; nameText.color = c;
        }
        if (scoreText != null)
        {
            // Start at 0 for count-up effect during FadeIn
            scoreText.text = "0";
            _displayedScore = 0;
            var c = textColor; c.a = 0f; scoreText.color = c;
        }
        if (rankBadge != null)
        {
            var c = _targetBadgeColor; c.a = 0f; rankBadge.color = c;
        }
        if (rowBackground != null)
        {
            var c = _targetBgColor; c.a = 0f; rowBackground.color = c;
        }
    }

    /// <summary>
    /// Animates the row in: slide-from-left, fade, score count-up, and
    /// a scale-pop on the rank badge. After this completes, if it's the
    /// player's row, a subtle pulse loop starts.
    /// </summary>
    public IEnumerator FadeIn(float duration)
    {
        if (rectTransform == null) yield break;
        float t = 0f;
        Vector2 startPos = _basePos + new Vector2(-100f, 0f);
        Vector3 badgeStartScale = Vector3.one * 0.4f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = 1f - Mathf.Pow(1f - k, 3f);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, _basePos, e);

            // Mode A: row text
            if (rowText != null && nameText == null && scoreText == null)
            {
                var c = _targetTextColor; c.a = e * 0.95f; rowText.color = c;
            }

            // Mode B: separated
            if (nameText != null)
            { var c = _targetTextColor; c.a = e * 0.95f; nameText.color = c; }
            if (rankText != null)
            {
                var rc = rankText.color;
                rc.a = e;
                rankText.color = rc;
            }
            if (scoreText != null)
            {
                int newDisplay = Mathf.RoundToInt(Mathf.Lerp(0f, _targetScore, EaseOutCubic(k)));
                if (newDisplay != _displayedScore)
                {
                    _displayedScore = newDisplay;
                    scoreText.text = _displayedScore.ToString("N0");
                }
                var c = scoreText.color; c.a = e; scoreText.color = c;
            }
            if (rankBadge != null)
            {
                var c = _targetBadgeColor; c.a = e; rankBadge.color = c;
                // Scale-pop with overshoot
                float s = EaseOutBack(k, 1.4f);
                rankBadge.rectTransform.localScale = Vector3.Lerp(badgeStartScale, Vector3.one, s);
            }
            if (rowBackground != null)
            {
                var c = _targetBgColor; c.a = e * _targetBgColor.a; rowBackground.color = c;
            }
            yield return null;
        }
        // Final state
        rectTransform.anchoredPosition = _basePos;
        if (scoreText != null)
        {
            _displayedScore = _targetScore;
            scoreText.text = _targetScore.ToString("N0");
            var c = scoreText.color; c.a = 1f; scoreText.color = c;
        }
        if (rankBadge != null)
        {
            rankBadge.rectTransform.localScale = Vector3.one;
            var c = _targetBadgeColor; c.a = 1f; rankBadge.color = c;
        }

        // Start the player-row pulse if applicable
        if (_isPlayerRow && rowBackground != null)
        {
            if (_pulseCo != null) StopCoroutine(_pulseCo);
            _pulseCo = StartCoroutine(PulsePlayerRow());
        }
    }

    IEnumerator PulsePlayerRow()
    {
        Color baseC = _targetBgColor;
        while (true)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.5f);
            var c = baseC; c.a = baseC.a * (0.55f + 0.45f * pulse);
            if (rowBackground != null) rowBackground.color = c;
            yield return null;
        }
    }

    static void SetAlpha(TMP_Text txt, float a)
    {
        if (txt == null) return;
        var c = txt.color; c.a = a; txt.color = c;
    }

    static float EaseOutBack(float t, float overshoot)
    {
        float s = t - 1f;
        return s * s * ((overshoot + 1f) * s + overshoot) + 1f;
    }

    static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
