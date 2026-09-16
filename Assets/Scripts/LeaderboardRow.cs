using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One row in the leaderboard: rank + name + score.
///
/// Deliberately minimal — no slide-in, no score count-up, no medal badge,
/// no player-row pulse. <see cref="Populate"/> renders the row in its final
/// state immediately; the parent <see cref="LeaderboardPanel"/> does the one
/// gentle fade of the whole board via its CanvasGroup.
/// </summary>
public class LeaderboardRow : MonoBehaviour
{
    [Header("Row fields (preferred separated layout)")]
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text scoreText;
    [Tooltip("Optional row background image — tinted for the player's own row.")]
    public Image rowBackground;

    [Header("Legacy / compatibility (left null in the minimal layout)")]
    [Tooltip("Optional circle behind the rank number — unused in the minimal layout.")]
    public Image rankBadge;
    [Tooltip("Single combined-text mode — only used if the separated fields aren't wired.")]
    public TMP_Text rowText;

    public RectTransform rectTransform;

    [Tooltip("Tint painted behind the player's own row (theme hot pink #FF2E88).")]
    public Color playerRowTintColor = new Color(1f, 0.18f, 0.53f, 0.18f);

    void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

    public void Clear()
    {
        SetAlpha(rankText, 0f);
        SetAlpha(nameText, 0f);
        SetAlpha(scoreText, 0f);
        SetAlpha(rowText, 0f);
        if (rankBadge != null) { var c = rankBadge.color; c.a = 0f; rankBadge.color = c; }
        if (rowBackground != null) { var c = rowBackground.color; c.a = 0f; rowBackground.color = c; }
        if (rankText != null) rankText.text = "";
        if (nameText != null) nameText.text = "";
        if (scoreText != null) scoreText.text = "";
        if (rowText != null) rowText.text = "";
    }

    /// <summary>
    /// Renders the row in its final visible state (full opacity, final score).
    /// The panel fades the whole board in together, so there is no per-row
    /// animation here.
    /// </summary>
    public void Populate(int rank, string playerName, int score, Color textColor, bool isPlayerRow)
    {
        string n = string.IsNullOrEmpty(playerName) ? "---" : playerName;
        if (n.Length > 16) n = n.Substring(0, 16);
        n = n.ToUpperInvariant();

        // Legacy combined mode — only if the separated fields aren't present.
        if (rowText != null && nameText == null && scoreText == null)
        {
            rowText.text = $"#{rank,-3} {n}<pos=550><mspace=0.55em>{score,6}</mspace>";
            var rc = textColor; rc.a = 1f; rowText.color = rc;
            return;
        }

        if (rankText != null)
        {
            rankText.text = rank > 0 ? rank.ToString() : "-";
            var c = textColor; c.a = 1f; rankText.color = c;
        }
        if (nameText != null)
        {
            nameText.text = n;
            var c = textColor; c.a = 1f; nameText.color = c;
        }
        if (scoreText != null)
        {
            scoreText.text = score.ToString("N0");
            var c = textColor; c.a = 1f; scoreText.color = c;
        }
        // No medal badge in the minimal layout — keep any legacy badge hidden.
        if (rankBadge != null)
        {
            var c = rankBadge.color; c.a = 0f; rankBadge.color = c;
        }
        if (rowBackground != null)
        {
            rowBackground.color = isPlayerRow ? playerRowTintColor : new Color(0f, 0f, 0f, 0f);
        }
    }

    static void SetAlpha(TMP_Text txt, float a)
    {
        if (txt == null) return;
        var c = txt.color; c.a = a; txt.color = c;
    }
}
