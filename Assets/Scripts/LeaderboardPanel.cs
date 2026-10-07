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
    public bool sculptedHeading;
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

    public bool arcadeResult;
    public bool LevelsSelected { get; private set; }
    bool choiceMade;
    float choiceReadyAt;
    public void ChooseRetry() { Choose(false); }
    public void ChooseLevels() { Choose(true); }
    void Choose(bool levels)
    {
        if (arcadeResult || !_isAnimating || choiceMade || Time.unscaledTime < choiceReadyAt) return;
        LevelsSelected=levels; choiceMade=true; AudioManager.Instance?.PlayButtonClick();
    }
    bool _isAnimating;
    float _neutralSeconds;
    public int PresentationId { get; private set; }
    public int PlayerScore { get; private set; }
    public float ShownAtUnscaled { get; private set; }
    public void RefreshEntries(int presentationId, LeaderboardData[] entries, int rank, int score, string name)
    {
        if (_isAnimating && presentationId == PresentationId) Populate(entries, rank, score, name);
    }
    static float _releasedAt = float.NegativeInfinity;
    public static bool InputSettledAfterDisplay =>
        Time.realtimeSinceStartup - _releasedAt < 1f &&
        !ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);

    void Update()
    {
        if (_isAnimating && !arcadeResult && Time.unscaledTime >= choiceReadyAt)
        {
            if (ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green) || ArcadeInputAdapter.ConfirmDown()) ChooseRetry();
            else if (ArcadeInputAdapter.CancelDown()) ChooseLevels();
        }
        if (_isAnimating)
            _neutralSeconds = ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)
                ? 0f : _neutralSeconds + Time.unscaledDeltaTime;
    }

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
        // A duplicate failure must never complete the active presentation early
        // or queue another paid transaction.
        if (_isAnimating) return;

        PresentationId++;
        ShownAtUnscaled = Time.unscaledTime;
        choiceMade=false; LevelsSelected=false; choiceReadyAt=Time.unscaledTime+3.4f;
        PlayerScore = Mathf.Max(0, myScore);
        _neutralSeconds = 0f;
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
            titleText.text = sculptedHeading ? _outcomeMessage : $"<size=120%>BUCA</size>\n<size=52%>CHAMPIONS</size>\n<size=30%>{_outcomeMessage}</size>";

        bool meInList = false;
        if (entries != null)
        {
            for (int i = 0; i < Mathf.Min(entries.Length, rows != null ? rows.Length : 0); i++)
            {
                LeaderboardData entry = entries[i];
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
            rows[0]?.Populate(myRank,
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
        float shownAt = ShownAtUnscaled;
        float phaseStarted = shownAt;
        float duration = Mathf.Max(0.01f, fadeInDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Time.unscaledTime - phaseStarted;
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

        // Four seconds for the reason, four for standings, then one platform handoff.
        // The same unscaled clock drives both the visual phase and this callback.
        const float fadeOutDuration = 0.25f;
        float visibleDuration = arcadeResult ? 8f : Mathf.Max(0f, autoAdvanceSeconds);
        float fadeOutAt = shownAt + Mathf.Max(duration, visibleDuration - fadeOutDuration);
        while (Time.unscaledTime < fadeOutAt) yield return null;

        phaseStarted = Time.unscaledTime;
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed = Time.unscaledTime - phaseStarted;
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

        if (_neutralSeconds >= 0.25f) _releasedAt = Time.realtimeSinceStartup;
        Action callback = _onFinished;
        _onFinished = null;
        _isAnimating = false;
        callback?.Invoke();
    }

    static string NormalizeOutcome(string outcome)
    {
        if (!string.IsNullOrWhiteSpace(outcome) &&
            outcome.IndexOf("TIME", StringComparison.OrdinalIgnoreCase) >= 0)
            return "TIME'S UP!";
        return "OUT OF HEARTS!";
    }

    [Serializable]
    public struct LeaderboardData
    {
        public int rank;
        public string playerName;
        public int score;
    }
}
