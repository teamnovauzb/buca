using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

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
    int _highestUnlocked;                             // levels 0.._highestUnlocked are playable
    const string HighestUnlockedKey = "BucaHighestLevel";
    int _lastIdleLogSec = -1;                          // throttles the idle-countdown log to 1/sec

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

        // BUGFIX (Enter loaded the wrong level): keyboard nav moves OUR
        // highlight (_selectedIndex), but Unity's EventSystem keeps the old
        // (current-progress) tile selected — so the built-in "Submit" on Enter
        // fires that stale tile's button and loads the wrong level, overriding
        // our StartLevel(_selectedIndex). Lock the EventSystem selection to our
        // focused tile every frame so Submit always hits the focused level.
        if (EventSystem.current != null && _selectedIndex >= 0 && _selectedIndex < levels.Count
            && levels[_selectedIndex].button != null
            && EventSystem.current.currentSelectedGameObject != levels[_selectedIndex].button.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(levels[_selectedIndex].button.gameObject);
        }

        // ── Idle auto-close (arcade attract-mode requirement) ──────
        // Reset the timer on ANY input so an engaged player isn't kicked.
        // Uses unscaled time so a paused popup doesn't cheese the timer.
        TickIdleAutoClose();

        // BACK = GREEN, RED, or WHITE button (or Escape).
        // GREEN is the canonical cabinet back button, but on generic
        // gamepads Luxodd's fallback maps Green→leftShoulder — so QA
        // pressing B/Circle (which maps to Red) got nothing ("back button
        // not working using game pad"). Red and White are unused inside
        // the picker, so accepting all three is safe on cabinet AND fixes
        // gamepad testing. (Skipped during the input-grace window.)
        if (_inputGraceFramesAfterOpen <= 0 &&
            (ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green)
             || ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Red)
             || ArcadeInputAdapter.CancelDown()   // White
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
    // Extended from 30 → 120 frames (~2s @ 60fps) for the same reason as
    // MainMenuController: the click that refocuses the window fires
    // GetMouseButtonDown on the same frame OnApplicationFocus delivers,
    // and Unity's input pipeline takes a few frames to fully settle.
    const int FocusGraceFrameCount = 120;
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

    // (DetectRealtimeGap inlined into TickIdleAutoClose — the gap check
    // now BLOCKS the same-frame input rather than just arming forward
    // grace, fixing the WebGL refocus-click input-replay leak.)

    void TickIdleAutoClose()
    {
        if (idleAutoCloseSeconds <= 0f) return;

        // Continuous countdown at REAL speed — never resets on input and is
        // never paused by focus-grace. (It used to clamp dt to 1/30 AND skip
        // every frame during a 120-frame "refocus grace"; at the editor's low
        // 4K framerate that made the 15s timer crawl — the "too slow" bug.)
        _idleTimer -= Time.unscaledDeltaTime;

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
        else if (curSec <= 5 && curSec > 0 && curSec != _lastIdleLogSec)
        {
            // No UI wired — log ONCE per second (not every frame) so it doesn't
            // spam ~60×/s into the build's console.
            _lastIdleLogSec = curSec;
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
        // Start focus on the player's current level, but never on a LOCKED
        // tile (RefreshAll above has just computed _highestUnlocked).
        int saved = PlayerPrefs.GetInt("BucaCurrentLevel", 0);
        _selectedIndex = Mathf.Clamp(saved, 0, Mathf.Min(_highestUnlocked, Mathf.Max(0, levels.Count - 1)));
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

    bool _starting;
    public void StartLevel(int index)
    {
        // Guard: pressing Enter can fire BOTH our own handler and Unity's
        // EventSystem "Submit" in the same frame. Without this, the second call
        // could overwrite the pending level. Load exactly once.
        if (_starting) return;

        // Progression LOCK: a level beyond the highest unlocked one can't be
        // started — by mouse OR keyboard. Pressing Enter on it does nothing.
        if (index > PlayerPrefs.GetInt(HighestUnlockedKey, 0))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayNavTick(); // soft "locked" blip
            return;
        }

        _starting = true;
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
        // ── Progression unlock ─────────────────────────────────────
        // Levels 0.._highestUnlocked are playable; the rest are locked.
        // Driven SOLELY by the saved highest-completed value: it defaults to 0,
        // so a FRESH game has only Level 1 unlocked, and it is bumped exactly
        // one step when a level is completed (in LevelManager). We deliberately
        // do NOT reconstruct it from per-level star data — that was the bug that
        // auto-unlocked old progress.
        _highestUnlocked = Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedKey, 0), 0, Mathf.Max(0, levels.Count - 1));

        int totalCompleted = 0;
        int totalStars = 0;
        int totalScore = 0;

        for (int i = 0; i < levels.Count; i++)
        {
            int stars = PlayerPrefs.GetInt(LevelManager.PrefLevelStars + i, 0);
            int score = PlayerPrefs.GetInt(LevelManager.PrefLevelScore + i, 0);
            var entry = levels[i];
            bool locked = i > _highestUnlocked;

            if (entry.stars != null)
                for (int s = 0; s < entry.stars.Length; s++)
                {
                    if (entry.stars[s] == null) continue;
                    entry.stars[s].color = (s < stars) ? starLit : starUnlit;
                }

            bool completed = score > 0 || stars > 0;
            if (entry.checkmark != null) entry.checkmark.enabled = completed && !locked;

            if (entry.bestTimeLabel != null)
                entry.bestTimeLabel.text = "";

            SetTileLocked(entry, locked);

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

    /// <summary>Apply/clear the locked look on a tile: disable its button so it
    /// can't be started (mouse OR keyboard), and show a dim padlock overlay.
    /// The overlay is created lazily, so no tile rebuild is needed.</summary>
    void SetTileLocked(LevelEntry entry, bool locked)
    {
        if (entry.button != null) entry.button.interactable = !locked;

        Transform tileT = entry.button != null ? entry.button.transform
                        : (entry.tileBackground != null ? entry.tileBackground.transform : null);
        if (tileT == null) return;

        var lockT = tileT.Find("LockOverlay");
        if (locked && lockT == null)
        {
            var ov = new GameObject("LockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ov.transform.SetParent(tileT, false);
            var rt = (RectTransform)ov.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var ovImg = ov.GetComponent<Image>();
            ovImg.color = new Color(0.02f, 0.02f, 0.05f, 0.74f); // dim + disabled look
            ovImg.raycastTarget = true;                          // swallow clicks on locked tiles

            var ic = new GameObject("LockIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ic.transform.SetParent(ov.transform, false);
            var irt = (RectTransform)ic.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(58f, 58f);
            irt.anchoredPosition = Vector2.zero;
            var icImg = ic.GetComponent<Image>();
            icImg.sprite = LockSprite();
            icImg.color = new Color(0.85f, 0.88f, 1f, 0.92f);
            icImg.raycastTarget = false;

            lockT = ov.transform;
        }
        if (lockT != null) lockT.gameObject.SetActive(locked);
    }

    static Sprite _lockSprite;
    /// <summary>Procedural padlock sprite (white, tintable) — no art asset needed.</summary>
    static Sprite LockSprite()
    {
        if (_lockSprite != null) return _lockSprite;
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        var clear = new Color32(255, 255, 255, 0);
        var white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        // Body: rectangle, x[14..50] y[6..34]
        for (int y = 6; y <= 34; y++)
            for (int x = 14; x <= 50; x++)
                px[y * S + x] = white;
        // Shackle: ring centred (32,33), radius 8..13, upper half only
        var c = new Vector2(32f, 33f);
        for (int y = 33; y < 58; y++)
            for (int x = 12; x < 52; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d >= 8f && d <= 13f) px[y * S + x] = white;
            }
        // Keyhole: punch a hole + slot in the body
        for (int y = 12; y <= 28; y++)
            for (int x = 27; x <= 37; x++)
            {
                bool hole = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 24f)) <= 3.4f;
                bool slot = x >= 31 && x <= 33 && y >= 16 && y <= 24;
                if (hole || slot) px[y * S + x] = clear;
            }

        tex.SetPixels32(px); tex.Apply();
        _lockSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return _lockSprite;
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
