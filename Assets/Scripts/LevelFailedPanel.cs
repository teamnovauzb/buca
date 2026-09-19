using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Scene-authored failure dialog. Runtime code updates copy, visibility, input,
/// and animation only; all visual objects and components are prebuilt.
/// </summary>
public class LevelFailedPanel : MonoBehaviour
{
    [Header("Prebuilt structure")]
    public CanvasGroup group;
    public RectTransform card;
    public Button retryButton;
    public Button exitButton;

    [Header("Prebuilt copy references")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text retryButtonText;
    public TMP_Text exitButtonText;

    [Header("Prebuilt how-to video view")]
    public GameObject standardView;
    public GameObject tutorialVideoView;
    public RectTransform tutorialVideoRect;
    public RawImage tutorialVideoImage;
    public VideoPlayer tutorialVideoPlayer;
    public Button tutorialRetryButton;
    public TMP_Text tutorialRetryButtonText;

    [Header("Optional authored animation references")]
    public RectTransform titleRect;
    public RectTransform[] popInOrder;

    public string mainMenuScene = "MainMenu";
    [Min(3f)] public float sessionDecisionSeconds = 10f;

    [Header("Presentation")]
    [Tooltip("Retained for scene compatibility. Visual upgrades are editor-baked, never runtime-generated.")]
    public bool autoUpgradeVisuals = false;
    [Range(0.2f, 1.6f)] public float revealDuration = 0.5f;

    System.Action _onRetry;
    System.Action _onEnd;
    bool _shown;
    float _shownTime;
    bool _sessionPrompt;
    bool _timeUpSessionPrompt;
    bool _secondChancePrompt;
    bool _choicesReady;
    string _sessionStatus;
    Vector3 _cardBaseScale = Vector3.one;
    Vector3 _tutorialBaseScale = Vector3.one;
    Color _subtitleBaseColor = Color.white;

    string _defaultTitle = "LEVEL FAILED";
    string _defaultSubtitle = "Out of lives";
    string _defaultRetry = "RETRY";
    string _defaultExit = "EXIT";

    void Awake()
    {
        if (transform.localScale == Vector3.zero) transform.localScale = Vector3.one;
        ResolveTextReferences();
        CacheDefaultCopy();
        if (card != null) _cardBaseScale = card.localScale == Vector3.zero ? Vector3.one : card.localScale;
        if (tutorialVideoRect != null)
            _tutorialBaseScale = tutorialVideoRect.localScale == Vector3.zero
                ? Vector3.one
                : tutorialVideoRect.localScale;
        if (subtitleText != null) _subtitleBaseColor = subtitleText.color;
        if (standardView == null && card != null) standardView = card.gameObject;
        SetPresentationView(false);
        HideImmediate();

        BindRetryButton(retryButton);
        BindRetryButton(tutorialRetryButton);
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(Exit);
        }
    }

    public void Show(System.Action onRetry)
    {
        _onRetry = onRetry;
        _onEnd = null;
        _sessionPrompt = false;
        _timeUpSessionPrompt = false;
        _secondChancePrompt = false;
        _sessionStatus = null;
        ApplyCopy(_defaultTitle, _defaultSubtitle, _defaultRetry, _defaultExit);
        BeginShow();
    }

    public void ShowContinueChoice(string reason, System.Action onContinue, System.Action onEnd)
    {
        _onRetry = onContinue;
        _onEnd = onEnd;
        _sessionPrompt = true;
        _timeUpSessionPrompt = IsTimeUpReason(reason);
        _secondChancePrompt = false;
        _sessionStatus = "RESTART THIS LEVEL WITH FULL TIME";
        string heading = string.IsNullOrWhiteSpace(reason) ? "CONTINUE?" : reason.ToUpperInvariant();
        ApplyCopy(heading, _sessionStatus, "CONTINUE", "END GAME");
        BeginShow();
    }

    public void ShowFirstLevelSecondChance(System.Action onRetry)
    {
        _onRetry = onRetry;
        _onEnd = null;
        _sessionPrompt = false;
        _timeUpSessionPrompt = false;
        _secondChancePrompt = true;
        _sessionStatus = "THIS TIME YOU CAN DO IT";
        ApplyCopy("HOW TO PLAY", "AIM  •  CHARGE  •  FIRE", "TRY IT NOW", "END GAME");
        BeginShow();
    }

    void BeginShow()
    {
        StopAllCoroutines();
        _shown = true;
        _choicesReady = false;
        _shownTime = Time.unscaledTime;
        SetPresentationView(_secondChancePrompt);
        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(!_secondChancePrompt);
        if (tutorialRetryButton != null)
            tutorialRetryButton.gameObject.SetActive(_secondChancePrompt);
        if (_secondChancePrompt) RestartTutorialVideo();
        else StopTutorialVideo();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelOpen();
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = true;
        }
        RectTransform revealTarget = _secondChancePrompt && tutorialVideoRect != null
            ? tutorialVideoRect
            : card;
        Vector3 revealBaseScale = revealTarget == tutorialVideoRect
            ? _tutorialBaseScale
            : _cardBaseScale;
        if (revealTarget != null) revealTarget.localScale = revealBaseScale * 0.96f;

        float duration = Mathf.Max(0.01f, revealDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (group != null) group.alpha = t;
            if (revealTarget != null)
                revealTarget.localScale = revealBaseScale * Mathf.Lerp(0.96f, 1f, t);
            yield return null;
        }

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        if (revealTarget != null) revealTarget.localScale = revealBaseScale;
        _choicesReady = true;
        _shownTime = Time.unscaledTime;

        if (_sessionPrompt && !_timeUpSessionPrompt)
            StartCoroutine(SessionDecisionTimeout());
    }

    IEnumerator SessionDecisionTimeout()
    {
        float remaining = Mathf.Max(3f, sessionDecisionSeconds);
        while (_shown && _sessionPrompt && remaining > 0f)
        {
            if (subtitleText != null)
                subtitleText.text = $"{_sessionStatus}   •   AUTO END {Mathf.CeilToInt(remaining)}s";
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (_shown && _sessionPrompt) Exit();
    }

    void Update()
    {
        if (!_shown || !_choicesReady) return;
        if (Time.unscaledTime - _shownTime < 0.35f) return;

        if (ArcadeInputAdapter.ConfirmDown() || Input.GetKeyDown(KeyCode.Return))
            Retry();
        else if (!_secondChancePrompt &&
                 (ArcadeInputAdapter.CancelDown() ||
                  ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green) ||
                  Input.GetKeyDown(KeyCode.Escape)))
            Exit();
    }

    void Retry()
    {
        if (!_shown || !_choicesReady) return;
        _shown = false;
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        HideImmediate();
        System.Action callback = _onRetry;
        ClearCallbacks();
        callback?.Invoke();
    }

    void Exit()
    {
        if (!_shown || !_choicesReady || _secondChancePrompt) return;
        _shown = false;
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        System.Action callback = _onEnd;
        bool returnToMenu = !_sessionPrompt || callback == null;
        HideImmediate();
        ClearCallbacks();
        if (callback != null) callback.Invoke();
        else if (returnToMenu) SceneManager.LoadScene(mainMenuScene);
    }

    void HideImmediate()
    {
        _choicesReady = false;
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        if (card != null) card.localScale = _cardBaseScale;
        if (tutorialVideoRect != null) tutorialVideoRect.localScale = _tutorialBaseScale;
        StopTutorialVideo();
    }

    void ClearCallbacks()
    {
        _onRetry = null;
        _onEnd = null;
        _sessionPrompt = false;
        _timeUpSessionPrompt = false;
        _secondChancePrompt = false;
        _sessionStatus = null;
    }

    void ResolveTextReferences()
    {
        if (card != null)
        {
            if (titleText == null)
            {
                Transform child = card.Find("Title");
                if (child != null) titleText = child.GetComponent<TMP_Text>();
            }
            if (subtitleText == null)
            {
                Transform child = card.Find("Sub");
                if (child != null) subtitleText = child.GetComponent<TMP_Text>();
            }
        }
        if (retryButtonText == null && retryButton != null)
            retryButtonText = retryButton.GetComponentInChildren<TMP_Text>(true);
        if (exitButtonText == null && exitButton != null)
            exitButtonText = exitButton.GetComponentInChildren<TMP_Text>(true);
        if (tutorialRetryButtonText == null && tutorialRetryButton != null)
            tutorialRetryButtonText = tutorialRetryButton.GetComponentInChildren<TMP_Text>(true);
    }

    void BindRetryButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Retry);
    }

    void SetPresentationView(bool tutorialRequested)
    {
        bool showTutorial = tutorialRequested && tutorialVideoView != null;
        if (standardView != null) standardView.SetActive(!showTutorial);
        if (tutorialVideoView != null) tutorialVideoView.SetActive(showTutorial);
    }

    void RestartTutorialVideo()
    {
        if (tutorialVideoPlayer == null || tutorialVideoPlayer.clip == null) return;
        tutorialVideoPlayer.Stop();
        tutorialVideoPlayer.Play();
    }

    void StopTutorialVideo()
    {
        if (tutorialVideoPlayer != null) tutorialVideoPlayer.Stop();
    }

    void CacheDefaultCopy()
    {
        if (titleText != null && !string.IsNullOrWhiteSpace(titleText.text)) _defaultTitle = titleText.text;
        if (subtitleText != null && !string.IsNullOrWhiteSpace(subtitleText.text)) _defaultSubtitle = subtitleText.text;
        if (retryButtonText != null && !string.IsNullOrWhiteSpace(retryButtonText.text)) _defaultRetry = retryButtonText.text;
        if (exitButtonText != null && !string.IsNullOrWhiteSpace(exitButtonText.text)) _defaultExit = exitButtonText.text;
    }

    void ApplyCopy(string title, string subtitle, string retry, string exit)
    {
        if (titleText != null) titleText.text = title;
        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.color = _subtitleBaseColor;
        }
        if (retryButtonText != null)
        {
            retryButtonText.textWrappingMode = TextWrappingModes.NoWrap;
            retryButtonText.text = ForceButtonTextToOneLine(retry, "CONTINUE");
        }
        if (exitButtonText != null)
        {
            exitButtonText.textWrappingMode = TextWrappingModes.NoWrap;
            exitButtonText.text = ForceButtonTextToOneLine(exit, "END GAME");
        }
    }

    static bool IsTimeUpReason(string reason)
    {
        return !string.IsNullOrWhiteSpace(reason) &&
               reason.IndexOf("TIME", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string ForceButtonTextToOneLine(string value, string fallback)
    {
        string safe = string.IsNullOrWhiteSpace(value) ? fallback : value;
        return safe.Replace('\r', ' ').Replace('\n', ' ').Trim().ToUpperInvariant();
    }
}
