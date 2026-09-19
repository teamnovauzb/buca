using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Scene-authored leaderboard shown after a failed attempt. Runtime code only
/// populates and animates the serialized UI; it never builds visual content.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [Header("Prebuilt panel structure")]
    public CanvasGroup group;
    public RectTransform card;
    public TMP_Text titleText;
    public TMP_Text myRankText;
    public TMP_Text continueHintText;

    [Header("Prebuilt entry rows")]
    public LeaderboardRow[] rows;

    [Header("Timing")]
    public float fadeInDuration = 0.35f;
    [Tooltip("Seconds the leaderboard remains on screen before the Luxodd transaction opens.")]
    public float autoAdvanceSeconds = 5f;

    [Header("Colors")]
    public Color normalRowColor = new Color(0.64f, 0.90f, 1f, 1f);
    public Color myRowColor = new Color(1f, 0.18f, 0.53f, 1f);

    bool _isAnimating;
    Action _onFinished;
    string _outcomeMessage = "YOU LOST";

    void Awake()
    {
        if (continueHintText != null) continueHintText.gameObject.SetActive(false);
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    public void Show(LeaderboardData[] entries, int myRank, int myScore,
        string myName, string outcomeMessage, Action onFinished)
    {
        if (_isAnimating)
        {
            if (string.Equals(_outcomeMessage, "TIME IS OVER", StringComparison.Ordinal))
            {
                Action chainedPrevious = _onFinished;
                _onFinished = () => { chainedPrevious?.Invoke(); onFinished?.Invoke(); };
                return;
            }

            Action previous = _onFinished;
            _onFinished = null;
            StopAllCoroutines();
            _isAnimating = false;
            previous?.Invoke();
        }

        _isAnimating = true;
        _onFinished = onFinished;
        _outcomeMessage = NormalizeOutcome(outcomeMessage);
        gameObject.SetActive(true);
        Populate(entries, myRank, myScore, myName);

        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        if (card != null) card.localScale = Vector3.one * 0.94f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelOpen();
        StartCoroutine(ShowRoutine());
    }

    public void Show(LeaderboardData[] entries, int myRank, int myScore,
        string myName, Action onFinished)
    {
        Show(entries, myRank, myScore, myName, "YOU LOST", onFinished);
    }

    public void Show(LeaderboardData[] entries, int myRank, int myScore, Action onFinished)
    {
        Show(entries, myRank, myScore, "YOU", "YOU LOST", onFinished);
    }

    void Populate(LeaderboardData[] entries, int myRank, int myScore, string myName)
    {
        if (titleText != null)
            titleText.text = $"{_outcomeMessage}  •  TOP 10 POINTS";

        bool meInList = false;
        if (entries != null)
        {
            foreach (LeaderboardData entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.playerName) && !string.IsNullOrEmpty(myName) &&
                    entry.playerName.Equals(myName, StringComparison.OrdinalIgnoreCase))
                {
                    meInList = true;
                    break;
                }
            }
        }

        if (myRankText != null)
        {
            if (meInList)
            {
                myRankText.text = string.Empty;
            }
            else
            {
                string rank = myRank > 0 ? $"#{myRank}" : "UNRANKED";
                string player = string.IsNullOrEmpty(myName) ? "YOU" : myName.ToUpperInvariant();
                myRankText.text = $"{player}   {rank}   POINTS  {myScore:N0}";
            }
        }

        if (rows != null)
        {
            foreach (LeaderboardRow row in rows)
                if (row != null) row.Clear();
        }

        int rowCount = rows != null ? rows.Length : 0;
        int entryCount = entries != null ? entries.Length : 0;
        if (entryCount == 0 && rowCount > 0)
        {
            rows[0]?.Populate(myRank > 0 ? myRank : 1,
                string.IsNullOrEmpty(myName) ? "YOU" : myName,
                myScore, myRowColor, true);
            return;
        }

        int count = Mathf.Min(rowCount, entryCount);
        for (int i = 0; i < count; i++)
        {
            LeaderboardRow row = rows[i];
            if (row == null) continue;
            LeaderboardData data = entries[i];
            bool isMe = !string.IsNullOrEmpty(data.playerName) && !string.IsNullOrEmpty(myName) &&
                        data.playerName.Equals(myName, StringComparison.OrdinalIgnoreCase);
            row.Populate(data.rank, data.playerName, data.score,
                isMe ? myRowColor : normalRowColor, isMe);
        }
    }

    IEnumerator ShowRoutine()
    {
        float duration = Mathf.Max(0.01f, fadeInDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (group != null) group.alpha = t;
            if (card != null) card.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, t);
            yield return null;
        }

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        if (card != null) card.localScale = Vector3.one;

        // Every terminal result follows the same deterministic platform flow:
        // show this authored leaderboard, then invoke its completion callback
        // exactly autoAdvanceSeconds later. LevelManager uses that callback to
        // open Luxodd's official Continue transaction.
        const float fadeOutDuration = 0.25f;
        float visibleDuration = Mathf.Max(0f, autoAdvanceSeconds);
        float holdDuration = Mathf.Max(0f, visibleDuration - fadeOutDuration);
        float countdown = holdDuration;
        while (countdown > 0f)
        {
            countdown -= Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeOutDuration));
            if (group != null) group.alpha = 1f - t;
            yield return null;
        }

        if (continueHintText != null) continueHintText.gameObject.SetActive(false);
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        gameObject.SetActive(false);

        Action callback = _onFinished;
        _onFinished = null;
        _isAnimating = false;
        callback?.Invoke();
    }

    static string NormalizeOutcome(string outcome)
    {
        if (!string.IsNullOrWhiteSpace(outcome) &&
            outcome.IndexOf("TIME", StringComparison.OrdinalIgnoreCase) >= 0)
            return "TIME IS OVER";
        return "YOU LOST";
    }

    [Serializable]
    public struct LeaderboardData
    {
        public int rank;
        public string playerName;
        public int score;
    }
}
