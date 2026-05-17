using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Full-screen level-select panel. Shown when the player clicks the
/// "LEVELS" button on the main menu; hidden by default. Builds a grid
/// of tiles (one per entry in `levels`) with star ratings + completion
/// checkmarks, shows campaign totals in the footer, and handles
/// open/close animations.
///
/// Tiles persist their per-level stars/score in PlayerPrefs (managed
/// by LevelManager during gameplay). On click, the level index is
/// stashed in PlayerPrefs as "BucaPendingLevel" and the Game scene is
/// loaded; LevelManager reads that value in Start() and jumps to it.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [System.Serializable]
    public class LevelEntry
    {
        public string displayName = "LEVEL 1";
        public Button button;
        public TMP_Text label;
        public Image[] stars;            // 3 star Images per tile
        public Image checkmark;          // green check shown when score > 0
        public TMP_Text bestTimeLabel;   // optional time badge top-right
        public Image tileBackground;     // for "currently selected" highlight
    }

    [Header("Scene refs")]
    public List<LevelEntry> levels = new List<LevelEntry>();
    public string gameSceneName = "Game";

    [Header("Panel structure (assigned by MenuSetupHelper)")]
    public CanvasGroup panelGroup;
    public RectTransform panelCard;
    public Button backButton;

    [Header("Footer stats labels")]
    public TMP_Text completedLabel;       // "30/30 Completed"
    public TMP_Text starsLabel;           // "30/90"
    public TMP_Text scoreLabel;           // "Score: 1318"
    public RectTransform progressBarFill; // green bar filling 0..1

    [Header("Star colors")]
    public Color starLit   = new Color(1f, 0.9f, 0.3f, 1f);
    public Color starUnlit = new Color(1f, 1f, 1f, 0.12f);

    [Header("Selection (joystick navigation)")]
    [Tooltip("How many tiles per row — needed for up/down navigation. Set by MenuSetupHelper.")]
    public int columnsPerRow = 5;
    [Tooltip("Tile background color when this tile has the navigation focus.")]
    public Color selectedTileColor = new Color(0.30f, 0.62f, 1.0f, 1f);
    [Tooltip("Tile background color for non-focused tiles.")]
    public Color unselectedTileColor = new Color(0.13f, 0.18f, 0.32f, 0.95f);
    [Tooltip("Scale applied to the focused tile.")]
    public float selectedScale = 1.10f;
    [Tooltip("Seconds between repeat-steps when the joystick is held.")]
    public float navRepeatDelay = 0.20f;

    [Header("Open/close animation")]
    public float fadeDuration = 0.28f;

    [Header("Idle auto-close (arcade attract mode)")]
    [Tooltip("Seconds of no input before the panel auto-closes. " +
             "QA req: 15s for the level select panel (max 30s for any arcade menu). 0 = disabled.")]
    public float idleAutoCloseSeconds = 15f;
    [Tooltip("QA req #4: on idle timeout, AUTO-START THE GAME with the currently focused tile " +
             "instead of returning to the main menu (which would just spawn another 30s wait). " +
             "Set false if you want classic close-to-main-menu behavior.")]
    public bool idleAutoStartsGame = true;
    [Tooltip("Optional TMP_Text showing the countdown when it drops below 10s. Built by MenuSetupHelper.")]
    public TMP_Text idleCountdownText;

    public const string PendingLevelKey = "BucaPendingLevel";

    bool _open;
    Coroutine _animCo;
    int _selectedIndex;
    float _nextNavTime;
    float _idleTimer;
    Vector3 _lastMousePos;
    int _inputGraceFramesAfterOpen;

    // Color states for the always-visible idle countdown pill
    static readonly Color _idleNormalColor  = new Color(1f, 0.85f, 0.30f, 1f); // amber
    static readonly Color _idleWarningColor = new Color(1f, 0.30f, 0.30f, 1f); // hot red

    /// <summary>True between Show() being called and the close fade finishing. Polled by MainMenuController so the auto-start timer keeps counting while the panel is open.</summary>
    public bool IsOpen => _open;

    void Awake()
    {
        // Hide via CanvasGroup only — DON'T SetActive(false), or StartCoroutine
        // will fail in Show() (Unity can't start coroutines on inactive GOs and
        // SetActive→StartCoroutine in the same frame is racy when the panel is
        // a child of a canvas that propagates activation later).
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }

        // QA req #4: level-select panel auto-closes after 15s idle (tighter
        // than the 30s general arcade ceiling). Runtime-clamp so an old
        // scene with a higher serialized value still complies — this fix
        // applies the moment Play starts, no Inspector edit needed.
        if (idleAutoCloseSeconds > 15f)
        {
            Debug.LogWarning($"[LevelSelectController] idleAutoCloseSeconds was {idleAutoCloseSeconds} " +
                             "in the scene — clamping to 15 (QA requirement). " +
                             "Update the value in the Inspector to 15 to silence this warning.");
            idleAutoCloseSeconds = 15f;
        }
    }

    void Start()
    {
        // Wire each tile's button to load that level. NOTE: do NOT overwrite
        // e.label.text here — the tile builder already set it to just the
        // level number ("1", "2", ...). Writing the displayName ("LEVEL 1")
        // makes the giant 78-px text wrap to "LEV / EL" inside the tile.
        // displayName stays as metadata for future tooltips / accessibility.
        for (int i = 0; i < levels.Count; i++)
        {
            int capturedIndex = i; // closure safety
            var e = levels[i];
            if (e.button != null)
            {
                e.button.onClick.RemoveAllListeners();
                e.button.onClick.AddListener(() => StartLevel(capturedIndex));
            }
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Hide);
        }
    }

    void Update()
    {
        if (!_open) return;

        // Decrement the input-grace counter armed in Show(). While > 0 we
        // ignore confirm/back so the same button press that OPENED the
        // panel can't immediately load a level or close the panel.
        if (_inputGraceFramesAfterOpen > 0) _inputGraceFramesAfterOpen--;

        // ── Idle auto-close (arcade attract-mode requirement) ──────
        // Reset the timer on ANY input so an engaged player isn't kicked.
        // Uses unscaled time so a paused popup doesn't cheese the timer.
        TickIdleAutoClose();

        // GREEN button = Back  (skipped during input-grace window)
        if (_inputGraceFramesAfterOpen <= 0 &&
            (ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green)
             || Input.GetKeyDown(KeyCode.Escape)))
        {
            Hide();
            return;
        }

        // BLACK button = play the currently focused tile (skipped during grace)
        if (_inputGraceFramesAfterOpen <= 0 &&
            (ArcadeInputAdapter.ConfirmDown() || Input.GetKeyDown(KeyCode.Return)))
        {
            if (_selectedIndex >= 0 && _selectedIndex < levels.Count)
                StartLevel(_selectedIndex);
            return;
        }

        // Joystick / arrow-key grid navigation with repeat delay
        Vector2 stick = ArcadeInputAdapter.GetStick();
        float navX = stick.x;
        float navY = stick.y;
        if (Input.GetKey(KeyCode.LeftArrow))  navX = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) navX =  1f;
        if (Input.GetKey(KeyCode.UpArrow))    navY =  1f;
        if (Input.GetKey(KeyCode.DownArrow))  navY = -1f;

        if (Time.unscaledTime >= _nextNavTime)
        {
            int newIdx = _selectedIndex;
            int curRow = _selectedIndex / columnsPerRow;
            int curCol = _selectedIndex % columnsPerRow;

            if (navX > 0.5f)
            {
                // Right — clamp inside the same row (don't jump to next row)
                int rowMax = Mathf.Min(levels.Count - 1, (curRow + 1) * columnsPerRow - 1);
                newIdx = Mathf.Min(rowMax, _selectedIndex + 1);
            }
            else if (navX < -0.5f)
            {
                // Left — clamp inside the same row
                int rowMin = curRow * columnsPerRow;
                newIdx = Mathf.Max(rowMin, _selectedIndex - 1);
            }
            else if (navY > 0.5f)
            {
                // Up — same column, prev row
                int candidate = _selectedIndex - columnsPerRow;
                if (candidate >= 0) newIdx = candidate;
            }
            else if (navY < -0.5f)
            {
                // Down — same column, next row. If the next row is partial
                // and lacks this column, snap to the last valid index instead
                // of leaving us stuck on a non-existent slot.
                int candidate = _selectedIndex + columnsPerRow;
                if (candidate < levels.Count) newIdx = candidate;
                else if (curRow * columnsPerRow + curCol < levels.Count - 1)
                    newIdx = levels.Count - 1;
            }

            // Clamp safety net — should never fire after the row math above,
            // but defends against any future reordering of the logic.
            newIdx = Mathf.Clamp(newIdx, 0, levels.Count - 1);

            if (newIdx != _selectedIndex)
            {
                _selectedIndex = newIdx;
                UpdateSelectionVisuals();
                _nextNavTime = Time.unscaledTime + navRepeatDelay;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayNavTick();
            }
        }
        // Reset repeat when the stick returns to center
        if (Mathf.Abs(navX) < 0.2f && Mathf.Abs(navY) < 0.2f)
            _nextNavTime = 0f;
    }

    int _focusGraceFrames;
    const int FocusGraceFrameCount = 30; // ~0.5s @ 60fps
    float _lastRealtime;
    bool _stickEdgeWas;

    void OnApplicationFocus(bool hasFocus) { if (hasFocus) BeginFocusGrace(); }
    void OnApplicationPause (bool paused)  { if (!paused) BeginFocusGrace(); }

    void BeginFocusGrace()
    {
        // Window/tab refocus can fire spurious mouse-position jumps and
        // Input.anyKey for many frames — eat those so the idle timer
        // doesn't reset just because the player alt-tabbed back in.
        _focusGraceFrames = FocusGraceFrameCount;
        // Reset the cached mouse position so the next-frame delta
        // calculation doesn't see a huge jump as "movement".
        _lastMousePos = Input.mousePosition;
    }

    /// <summary>
    /// Detects an unannounced pause via realtime gap. Editor tab switches
    /// (Game tab → other tab) don't fire OnApplicationFocus or Pause but
    /// DO leave a >500ms gap between Update calls — this catches that case.
    /// </summary>
    void DetectRealtimeGap()
    {
        float now = Time.realtimeSinceStartup;
        if (_lastRealtime > 0f && (now - _lastRealtime) > 0.5f)
            BeginFocusGrace();
        _lastRealtime = now;
    }

    void TickIdleAutoClose()
    {
        if (idleAutoCloseSeconds <= 0f) return;

        // Catch unannounced pauses (editor tab switch, breakpoint, etc.)
        DetectRealtimeGap();

        // During focus-grace frames, skip BOTH input detection AND the timer
        // tick. We don't want the timer to keep counting down past the player
        // either — they just literally can't see the game during alt-tab.
        if (_focusGraceFrames > 0)
        {
            _focusGraceFrames--;
            _lastMousePos = Input.mousePosition;
            // Snapshot input state every grace frame so a held stick / mouse
            // button doesn't register as a fresh edge the moment grace ends.
            Vector2 graceStick = ArcadeInputAdapter.GetStick();
            _stickEdgeWas = Mathf.Abs(graceStick.x) > 0.5f || Mathf.Abs(graceStick.y) > 0.5f;
            return;
        }

        // EDGE-ONLY input detection. Previously held states (anyKey,
        // mouse motion, sustained joystick, mouse button held) would
        // re-trigger every frame after a refocus and reset the timer
        // on phantom events. Edge detection requires fresh DOWN events.
        Vector2 stick = ArcadeInputAdapter.GetStick();
        bool stickEdgeNow = Mathf.Abs(stick.x) > 0.5f || Mathf.Abs(stick.y) > 0.5f;
        bool stickEdge    = stickEdgeNow && !_stickEdgeWas;
        _stickEdgeWas     = stickEdgeNow;

        bool anyArcadeButtonDown = false;
        for (int i = 0; i < 8; i++)
            if (ArcadeInputAdapter.GetButtonDown((ArcadeInputAdapter.Button)i))
            { anyArcadeButtonDown = true; break; }

        // Mouse position delta — only count as "movement" if it's a real
        // intentional drag (>50px), not a tiny jiggle or a refocus jump.
        Vector3 mp = Input.mousePosition;
        Vector3 mouseDelta = mp - _lastMousePos;
        bool mouseMoved = mouseDelta.sqrMagnitude > 2500f && mouseDelta.sqrMagnitude < 40000f;
        _lastMousePos = mp;

        bool active = stickEdge
                    || anyArcadeButtonDown
                    || Input.anyKeyDown
                    || mouseMoved
                    || Input.GetMouseButtonDown(0);

        if (active)
        {
            _idleTimer = idleAutoCloseSeconds;
        }
        else
        {
            // Clamp dt so a long pause / refocus frame doesn't yank the
            // timer by Unity's maximumDeltaTime (~0.33s). Cap at 33ms.
            _idleTimer -= Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        }

        // Always visible — pill in the top-right corner counts down from
        // idleAutoCloseSeconds → 0. Below 5s the text turns red and pulses
        // to draw attention. Falls back to a Console log every second if
        // no UI text is wired (in case the panel wasn't re-spawned).
        int curSec = Mathf.Max(0, Mathf.CeilToInt(_idleTimer));
        if (idleCountdownText != null)
        {
            idleCountdownText.text = $"AUTO START  {curSec}s";
            bool warning = _idleTimer <= 5f && _idleTimer > 0f;
            if (warning)
            {
                // Pulsing red at 4 Hz with subtle scale kick
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 4f);
                idleCountdownText.color = Color.Lerp(_idleWarningColor,
                    new Color(1f, 0.6f, 0.6f, 1f), pulse);
                float s = 1f + 0.1f * pulse;
                idleCountdownText.transform.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                idleCountdownText.color = _idleNormalColor;
                idleCountdownText.transform.localScale = Vector3.one;
            }
        }
        else if (curSec <= 5 && curSec > 0)
        {
            // No UI wired — log so we can verify the timer is alive
            Debug.Log($"[LevelSelectController] Idle auto-close in {curSec}s");
        }

        if (_idleTimer <= 0f)
        {
            _idleTimer = idleAutoCloseSeconds; // reset so we don't fire twice
            if (idleAutoStartsGame
                && _selectedIndex >= 0 && _selectedIndex < levels.Count)
            {
                // QA req #4: bypass the "return to main menu, wait another
                // 30s for auto-start" two-step. On idle timeout, jump
                // straight into the currently-focused level. The selection
                // defaults to the player's last-played level (set in Show())
                // so the auto-start picks an appropriate one.
                Debug.Log($"[LevelSelectController] Idle auto-close expired — auto-starting " +
                          $"level {_selectedIndex + 1} (player's last-played).");
                StartLevel(_selectedIndex);
            }
            else
            {
                Debug.Log("[LevelSelectController] Idle auto-close expired — closing panel.");
                Hide();
            }
        }
    }

    /// <summary>Called by tile pointer-enter events so mouse hover also moves the focus.</summary>
    public void SetSelected(int index)
    {
        if (!_open) return;
        if (index < 0 || index >= levels.Count) return;
        if (index == _selectedIndex) return;
        _selectedIndex = index;
        UpdateSelectionVisuals();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayNavTick();
    }

    void UpdateSelectionVisuals()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            var e = levels[i];
            bool sel = (i == _selectedIndex);
            if (e.tileBackground != null)
                e.tileBackground.color = sel ? selectedTileColor : unselectedTileColor;
            if (e.button != null)
                e.button.transform.localScale = sel
                    ? Vector3.one * selectedScale
                    : Vector3.one;
        }
    }

    // ─────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────
    public void Show()
    {
        // Render above everything else in the canvas (otherwise sibling
        // order can leave the menu buttons painting on top of the panel).
        transform.SetAsLastSibling();
        RefreshAll();

        // Initial focus = last played level (clamped), so the joystick
        // navigation starts in a meaningful spot.
        int saved = PlayerPrefs.GetInt("BucaCurrentLevel", 0);
        _selectedIndex = Mathf.Clamp(saved, 0, Mathf.Max(0, levels.Count - 1));
        UpdateSelectionVisuals();

        // Reset idle timer so the panel doesn't auto-close immediately
        _idleTimer = idleAutoCloseSeconds;
        _lastMousePos = Input.mousePosition;
        if (idleCountdownText != null)
        {
            int secs = Mathf.CeilToInt(idleAutoCloseSeconds);
            idleCountdownText.text = $"AUTO START  {secs}s";
            idleCountdownText.color = _idleNormalColor;
            idleCountdownText.transform.localScale = Vector3.one;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelOpen();

        // CRITICAL: arm an input-grace window so the SAME button press that
        // opened this panel doesn't immediately fire the panel's own confirm
        // logic (which would call StartLevel and load the Game scene).
        // Without this, clicking LEVELS would: open the panel → next frame
        // the panel sees the still-down Black/Enter and calls StartLevel.
        _inputGraceFramesAfterOpen = 12; // ~0.2s @ 60fps — long enough that
                                         // the click input has fully released

        if (_animCo != null) StopCoroutine(_animCo);
        _animCo = StartCoroutine(FadeRoutine(true));
    }

    public void Hide()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelClose();
        if (_animCo != null) StopCoroutine(_animCo);
        _animCo = StartCoroutine(FadeRoutine(false));
    }

    public void StartLevel(int index)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        PlayerPrefs.SetInt(PendingLevelKey, index);
        PlayerPrefs.SetInt("BucaCurrentLevel", index);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    // ─────────────────────────────────────────────────────────
    // Refresh
    // ─────────────────────────────────────────────────────────
    void RefreshAll()
    {
        int totalCompleted = 0;
        int totalStars = 0;
        int totalScore = 0;

        for (int i = 0; i < levels.Count; i++)
        {
            int stars = PlayerPrefs.GetInt(LevelManager.PrefLevelStars + i, 0);
            int score = PlayerPrefs.GetInt(LevelManager.PrefLevelScore + i, 0);
            var entry = levels[i];

            if (entry.stars != null)
                for (int s = 0; s < entry.stars.Length; s++)
                {
                    if (entry.stars[s] == null) continue;
                    entry.stars[s].color = (s < stars) ? starLit : starUnlit;
                }

            bool completed = score > 0 || stars > 0;
            if (entry.checkmark != null) entry.checkmark.enabled = completed;

            if (entry.bestTimeLabel != null)
                entry.bestTimeLabel.text = ""; // hook here if you track best times

            if (completed) totalCompleted++;
            totalStars += stars;
            totalScore += score;
        }

        int maxStars = levels.Count * 3;
        if (completedLabel != null) completedLabel.text = $"{totalCompleted}/{levels.Count} COMPLETED";
        if (starsLabel != null) starsLabel.text = $"{totalStars}/{maxStars}";
        if (scoreLabel != null) scoreLabel.text = $"SCORE: {totalScore:N0}";
        if (progressBarFill != null)
        {
            float fill = levels.Count > 0 ? (float)totalCompleted / levels.Count : 0f;
            progressBarFill.anchorMax = new Vector2(fill, 1f);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Open/close fade
    // ─────────────────────────────────────────────────────────
    IEnumerator FadeRoutine(bool show)
    {
        if (panelGroup == null) yield break;
        float startAlpha = panelGroup.alpha;
        float targetAlpha = show ? 1f : 0f;
        float t = 0f;

        if (show && panelCard != null) panelCard.localScale = Vector3.one * 0.92f;
        _open = show;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            float e = show ? EaseOutBack(k, 1.4f) : k;
            panelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
            if (panelCard != null)
            {
                float s = show ? Mathf.Lerp(0.92f, 1f, e) : Mathf.Lerp(1f, 0.95f, k);
                panelCard.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }

        panelGroup.alpha = targetAlpha;
        panelGroup.interactable = show;
        panelGroup.blocksRaycasts = show;
        // Stay GameObject-active even when hidden — see Awake() comment.
        _animCo = null;
    }

    static float EaseOutBack(float t, float overshoot)
    {
        float s = t - 1f;
        return s * s * ((overshoot + 1f) * s + overshoot) + 1f;
    }
}
