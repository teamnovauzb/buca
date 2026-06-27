using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// "LEVEL FAILED" screen, shown by LevelManager when lives reach 0. Two choices:
///   • RETRY → reloads the same level (via the onRetry callback)
///   • EXIT  → returns to the main menu
///
/// Built + wired by the one-shot "RealBuca ▸ Add Lives System" tool. Stays GameObject-
/// active and hides via its CanvasGroup (so coroutines/animations are safe), matching
/// the LevelSelectController pattern. Works with mouse (editor) and the arcade buttons.
/// </summary>
public class LevelFailedPanel : MonoBehaviour
{
    [Header("Structure (wired by Add Lives System)")]
    public CanvasGroup group;
    public RectTransform card;
    public Button retryButton;
    public Button exitButton;

    [Header("Premium FX (optional — wired by Redesign Level Failed tool)")]
    public RectTransform titleRect;       // shaken on entry for impact
    public RectTransform[] popInOrder;    // staggered scale-pop after the card lands

    [Tooltip("Scene to load on Exit. Must be in Build Settings.")]
    public string mainMenuScene = "MainMenu";

    System.Action _onRetry;
    bool _shown;
    float _shownTime;

    void Awake()
    {
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (retryButton != null) { retryButton.onClick.RemoveAllListeners(); retryButton.onClick.AddListener(Retry); }
        if (exitButton != null) { exitButton.onClick.RemoveAllListeners(); exitButton.onClick.AddListener(Exit); }
    }

    /// <summary>Called by LevelManager.FailSequence. onRetry reloads the level.</summary>
    public void Show(System.Action onRetry)
    {
        _onRetry = onRetry;
        _shown = true;
        _shownTime = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelOpen();
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        if (card != null) card.localScale = Vector3.one * 0.7f;
        // hide stagger elements until the card lands
        if (popInOrder != null) foreach (var p in popInOrder) if (p != null) p.localScale = Vector3.zero;

        float t = 0f, dur = 0.32f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            if (group != null) group.alpha = Mathf.Clamp01(k * 1.5f);
            if (card != null) { float s = Mathf.Lerp(0.7f, 1f, EaseOutBack(k, 2.2f)); card.localScale = new Vector3(s, s, 1f); }
            yield return null;
        }
        if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
        if (card != null) card.localScale = Vector3.one;

        // Title "impact" shake — sells the failure.
        if (titleRect != null) yield return ShakeTitle();

        // Buttons (and any other elements) pop in one after another.
        if (popInOrder != null)
            foreach (var p in popInOrder)
            {
                if (p == null) continue;
                yield return PopIn(p);
            }
    }

    IEnumerator ShakeTitle()
    {
        Vector2 home = titleRect.anchoredPosition;
        float dur = 0.3f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            float damp = 1f - k;                                  // decays to 0
            float dx = Mathf.Sin(k * Mathf.PI * 9f) * 18f * damp;
            titleRect.anchoredPosition = home + new Vector2(dx, 0f);
            yield return null;
        }
        titleRect.anchoredPosition = home;
    }

    IEnumerator PopIn(RectTransform rt)
    {
        float dur = 0.14f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = Mathf.Lerp(0.4f, 1f, EaseOutBack(k, 2.6f));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void Update()
    {
        if (!_shown) return;
        if (Time.unscaledTime - _shownTime < 0.35f) return;   // grace so the fail input doesn't auto-press

        if (ArcadeInputAdapter.ConfirmDown() || Input.GetKeyDown(KeyCode.Return))
            Retry();
        else if (ArcadeInputAdapter.CancelDown()
                 || ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green)
                 || ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Red)
                 || Input.GetKeyDown(KeyCode.Escape))
            Exit();
    }

    void Retry()
    {
        _shown = false;
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Hide();
        _onRetry?.Invoke();
    }

    void Exit()
    {
        _shown = false;
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        SceneManager.LoadScene(mainMenuScene);
    }

    void Hide()
    {
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
    }

    static float EaseOutBack(float t) { float s = t - 1f; return s * s * (2.70158f * s + 1.70158f) + 1f; }
    static float EaseOutBack(float t, float overshoot) { float s = t - 1f; return s * s * ((overshoot + 1f) * s + overshoot) + 1f; }
}
