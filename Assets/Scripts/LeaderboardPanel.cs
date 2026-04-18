using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Animated leaderboard panel shown after death or time-up, before the
/// Luxodd Continue popup. Displays the player's rank + top N entries.
///
/// Flow:
///   1. Panel fades in with scale bounce
///   2. Entries reveal one by one (top-down, staggered)
///   3. Player's row highlights in yellow
///   4. After autoAdvanceSeconds OR JoystickButton0/Space, onFinished fires
///   5. Panel fades out → Luxodd Continue popup is triggered
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [Header("Panel structure")]
    public CanvasGroup group;
    public RectTransform card;
    public TMP_Text titleText;
    public TMP_Text myRankText;
    public TMP_Text continueHintText;

    [Header("Entry rows (pre-built in scene by BucaSetupHelper)")]
    [Tooltip("Rows to populate — index 0 = rank #1. Typically 5–10 rows.")]
    public LeaderboardRow[] rows;

    [Header("Timing")]
    public float fadeInDuration = 0.35f;
    public float rowDelay = 0.08f;
    public float autoAdvanceSeconds = 4f;

    [Header("Colors")]
    public Color normalRowColor = new Color(1f, 1f, 1f, 0.85f);
    public Color myRowColor = new Color(1f, 0.9f, 0.3f, 1f);

    bool _waitingForInput;
    bool _skipped;
    Action _onFinished;

    void Awake()
    {
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
    }

    void Update()
    {
        if (!_waitingForInput) return;
        if (Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.JoystickButton0)
            || Input.GetMouseButtonDown(0))
        {
            _skipped = true;
        }
    }

    /// <summary>
    /// Shows the panel with the given leaderboard data, then calls
    /// onFinished after autoAdvanceSeconds or input.
    /// </summary>
    public void Show(LeaderboardData[] entries, int myRank, int myScore, string myName, Action onFinished)
    {
        _onFinished = onFinished;
        _skipped = false;
        _waitingForInput = false;
        gameObject.SetActive(true);
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (card != null) card.localScale = Vector3.zero;
        StartCoroutine(ShowRoutine(entries, myRank, myScore, myName));
    }

    /// <summary>Legacy overload for calls that don't provide a name.</summary>
    public void Show(LeaderboardData[] entries, int myRank, int myScore, Action onFinished)
    {
        Show(entries, myRank, myScore, "YOU", onFinished);
    }

    IEnumerator ShowRoutine(LeaderboardData[] entries, int myRank, int myScore, string myName)
    {
        // Fade in + scale bounce
        if (titleText != null) titleText.text = "LEADERBOARD";
        if (myRankText != null)
        {
            // "UNRANKED" is friendlier than "#0" when the player has no score yet.
            string rankStr = myRank > 0 ? $"#{myRank}" : "UNRANKED";
            string nameStr = string.IsNullOrEmpty(myName) ? "YOU" : myName.ToUpper();
            myRankText.text = $"{nameStr}   {rankStr}   SCORE  {myScore}";
        }
        if (continueHintText != null)
        {
            var c = continueHintText.color; c.a = 0f; continueHintText.color = c;
        }

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeInDuration);
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

        // Clear rows first
        if (rows != null)
            foreach (var r in rows)
                if (r != null) r.Clear();

        // If no entries at all, show a single placeholder row with the
        // player's info so the panel doesn't look empty.
        if ((entries == null || entries.Length == 0) && rows != null && rows.Length > 0)
        {
            var row = rows[0];
            if (row != null)
            {
                row.Populate(myRank > 0 ? myRank : 1,
                    string.IsNullOrEmpty(myName) ? "YOU" : myName,
                    myScore,
                    myRowColor);
                yield return row.FadeIn(0.25f);
            }
        }
        else
        {
            // Populate rows one by one. Highlight the row matching the
            // current player's name — more reliable than matching by rank
            // (rank can be 0 for unranked, or differ if we appended).
            int count = Mathf.Min(rows?.Length ?? 0, entries?.Length ?? 0);
            for (int i = 0; i < count; i++)
            {
                var row = rows[i];
                var data = entries[i];
                if (row == null) continue;
                bool isMe = !string.IsNullOrEmpty(data.playerName)
                            && !string.IsNullOrEmpty(myName)
                            && data.playerName.Equals(myName, System.StringComparison.OrdinalIgnoreCase);
                Color rowColor = isMe ? myRowColor : normalRowColor;
                row.Populate(data.rank, data.playerName, data.score, rowColor);
                yield return row.FadeIn(0.25f);
                yield return WaitUnscaled(rowDelay);
            }
        }

        // Show continue hint + wait for input/timer
        if (continueHintText != null)
        {
            float fadeT = 0f, fadeDur = 0.3f;
            while (fadeT < fadeDur)
            {
                fadeT += Time.unscaledDeltaTime;
                var c = continueHintText.color; c.a = Mathf.Clamp01(fadeT / fadeDur) * 0.8f;
                continueHintText.color = c;
                yield return null;
            }
        }

        _waitingForInput = true;
        float countdown = autoAdvanceSeconds;
        while (countdown > 0f && !_skipped)
        {
            countdown -= Time.unscaledDeltaTime;
            if (continueHintText != null)
            {
                int secs = Mathf.CeilToInt(countdown);
                continueHintText.text = $"CONTINUE IN {secs}s  |  PRESS SPACE";
            }
            yield return null;
        }

        _waitingForInput = false;
        // Fade out
        float outT = 0f, outDur = 0.25f;
        float startAlpha = group != null ? group.alpha : 1f;
        while (outT < outDur)
        {
            outT += Time.unscaledDeltaTime;
            if (group != null) group.alpha = Mathf.Lerp(startAlpha, 0f, outT / outDur);
            yield return null;
        }
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        gameObject.SetActive(false);

        _onFinished?.Invoke();
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

    [Serializable]
    public struct LeaderboardData
    {
        public int rank;
        public string playerName;
        public int score;
    }
}
