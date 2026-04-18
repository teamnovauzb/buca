using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Celebratory end-of-game panel shown when the last level is sunk.
/// Fades in with a scale-bounce, shows total strokes + total stars,
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

    public void Show(int totalStrokes, int totalStars, int maxStars)
    {
        // Activate first — Unity can't run coroutines on inactive objects.
        gameObject.SetActive(true);

        // Reset visuals immediately so the first frame isn't stale.
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (card != null) card.localScale = Vector3.zero;

        if (statsText != null)
            statsText.text = $"TOTAL STROKES  {totalStrokes}\nSTARS  {totalStars} / {maxStars}";
        StartCoroutine(ShowRoutine());
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
