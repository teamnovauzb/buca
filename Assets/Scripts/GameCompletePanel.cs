using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Celebratory end-of-game panel shown when the last level is sunk.
/// Fades in with a scale-bounce, shows the final golf score + total stars,
/// and exposes Replay (restart from level 1) + Main Menu buttons.
///
/// The whole panel is disabled by default and shown only via Show().
/// </summary>
public class GameCompletePanel : MonoBehaviour
{
    [Header("Refs (assigned in scene or by BucaSetupHelper)")]
    public CanvasGroup group;
    public RectTransform card;
    public TMP_Text titleText;
    public TMP_Text statsText;
    public Button replayButton;
    public Button mainMenuButton;
    public string mainMenuSceneName = "MainMenu";

    [Header("Prebuilt golf scorecard")]
    public GolfScorecardView golfScorecard;

    void Awake()
    {
        // Zero out visuals only — do NOT SetActive(false) here.
        // BucaSetupHelper already builds this inactive. Re-deactivating
        // in Awake() would fight Show()'s SetActive(true) and prevent
        // the coroutine from starting.
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (card  != null) card.localScale = Vector3.zero;

        if (replayButton   != null) replayButton.onClick.AddListener(Replay);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoMainMenu);
    }

    /// <summary>Legacy overload retained for older scene/event bindings.</summary>
    public void Show(int totalStrokes, int totalStars, int maxStars)
    {
        Show(totalStrokes, 0, totalStars, maxStars);
    }

    public void Show(int totalStrokes, int totalPar, int totalStars, int maxStars)
    {
        // Activate first — Unity can't run coroutines on inactive objects.
        gameObject.SetActive(true);

        // Reset visuals immediately so the first frame isn't stale.
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (card != null) card.localScale = Vector3.zero;

        if (statsText != null)
        {
            // Four concise golf rows need more vertical room than the legacy
            // two-line summary. Normalize old generated scenes at runtime.
            statsText.rectTransform.anchoredPosition = Vector2.zero;
            statsText.rectTransform.sizeDelta = new Vector2(760f, 280f);
            statsText.fontSize = 48f;
            statsText.alignment = TextAlignmentOptions.Center;
            if (totalPar > 0)
            {
                int toPar = totalStrokes - totalPar;
                string golfScore = LevelManager.FormatToPar(toPar);
                string relation = toPar == 0
                    ? "EVEN PAR"
                    : toPar < 0
                        ? $"{Mathf.Abs(toPar)} UNDER PAR"
                        : $"{toPar} OVER PAR";
                string resultColor = toPar <= 0 ? "#FFD84D" : "#FF4D72";
                statsText.text =
                    $"<color=#75EDFF>FINAL GOLF SCORE</color>  " +
                    $"<color={resultColor}>{golfScore}</color>\n" +
                    $"<color=#DDF7FF>TOTAL STROKES  {totalStrokes}   •   COURSE PAR  {totalPar}</color>\n" +
                    $"<color={resultColor}>{relation}</color>\n" +
                    $"<color=#75EDFF>PUCK RATING  {totalStars} / {maxStars}</color>";
            }
            else
            {
                statsText.text = $"TOTAL STROKES  {totalStrokes}\nPUCK RATING  {totalStars} / {maxStars}";
            }
        }

        LevelManager manager = LevelManager.Instance;
        if (golfScorecard != null && manager != null)
            golfScorecard.Populate(manager.CampaignScorecard,
                manager.CurrentLevelNumber, manager.TotalLevels);
        StartCoroutine(ShowRoutine());
    }

    /// <summary>
    /// Luxodd owns Restart/End once the official transaction opens. Hide the
    /// local replay/menu actions so they cannot bypass session finalization.
    /// </summary>
    public void SetLocalActionsVisible(bool visible)
    {
        if (replayButton != null) replayButton.gameObject.SetActive(visible);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(visible);
    }

    IEnumerator ShowRoutine()
    {
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (card  != null) card.localScale = Vector3.zero;

        // Fade/scale in with overshoot — uses unscaled time in case the
        // caller has set Time.timeScale = 0 or similar.
        float dur = 0.55f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            if (group != null) group.alpha = k;
            if (card  != null)
            {
                float s = EaseOutBack(k, 2.0f);
                card.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
        if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
        if (card  != null) card.localScale = Vector3.one;
    }

    void Replay()
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteKey("BucaCurrentLevel");
        if (LevelManager.Instance != null) LevelManager.Instance.ResetProgressAndReloadScene();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    static float EaseOutBack(float t, float overshoot)
    {
        float s = t - 1f;
        return s * s * ((overshoot + 1f) * s + overshoot) + 1f;
    }
}
