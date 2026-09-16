using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Runtime orchestrator. All GameObjects it needs are pre-built in the
/// Game scene by RealBuca/Setup Game Scene — this script never creates
/// anything at runtime. It only swaps level prefabs, teleports the puck,
/// and plays animations on pre-existing UI + VFX objects.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Levels (assigned automatically by RealBuca/Setup Game Scene)")]
    public GameObject[] levelPrefabs;

    [Header("Scene references (assigned by RealBuca/Setup Game Scene)")]
    public GameObject puck;
    public Rigidbody puckRigidbody;
    public TrailRenderer puckTrail;
    public Transform puckShadow;
    public PuckController puckController;
    public ParticleSystem winBurst;
    public GameObject winRing;
    public MeshRenderer winRingRenderer;
    public ParticleSystem deathBurst;
    public ParticleSystem wallSparkBurst;

    [Header("Star rating UI (optional — assign an array of 3 Images)")]
    [Tooltip("3 UI Images that light up with the earned star count on win.")]
    public UnityEngine.UI.Image[] starImages;
    public Color starLitColor = new Color(1f, 0.9f, 0.3f, 1f);
    public Color starUnlitColor = new Color(1f, 1f, 1f, 0.15f);
    [Tooltip("Stroke cap for 3 stars — below this = 3, up to 2× is 2, above is 1.")]
    public int threeStarStrokes = 2;

    [Header("Level-complete panel (score breakdown after each level)")]
    public LevelCompletePanel levelCompletePanel;

    [Header("Game-complete panel (optional — shown after final level)")]
    public GameCompletePanel gameCompletePanel;

    [Header("Leaderboard panel (fallback for standalone — without LuxoddGameBridge)")]
    [Tooltip("If assigned, the leaderboard appears on death/time-up even when no LuxoddGameBridge is present. Auto-found at Start.")]
    public LeaderboardPanel leaderboardPanel;

    [Header("Shot rules")]
    [Tooltip("BUCA life mode: a missed shot removes one visible heart. Levels 1-24 use three hearts; Levels 25-30 use four.")]
    public bool useShotLives = true;

    [Header("Legacy lives system (used only when Use Shot Lives is enabled)")]
    [Tooltip("Fallback lives count when a level has no LevelSettings. Set to the campaign maximum so the HUD has enough heart slots.")]
    [Min(1)]
    public int maxLives = 6;
    [Tooltip("HUD text showing remaining lives. Wired by 'RealBuca ▸ Add Lives System'.")]
    public TMP_Text livesDisplay;
    [Tooltip("Heart icons (preferred over the text). Wired by 'RealBuca ▸ Add Lives System'.")]
    public UnityEngine.UI.Image[] lifeIcons;
    public Color lifeFullColor = new Color(1f, 0.19f, 0.24f, 1f);   // vivid red heart
    public Color lifeEmptyColor = new Color(1f, 0.19f, 0.24f, 0.16f); // faint red (life lost)
    [Tooltip("Shown when lives reach 0. Wired by 'RealBuca ▸ Add Lives System'.")]
    public LevelFailedPanel levelFailedPanel;
    [Tooltip("Show the leaderboard panel before respawning on deadly-wall death.")]
    public bool showLeaderboardOnDeath = true;
    [Tooltip("Show the leaderboard panel before restarting on time-up.")]
    public bool showLeaderboardOnTimeUp = true;

    [Header("Timer")]
    [Tooltip("Per-level settings array — index matches levelPrefabs. Timer values are normalized to 30 seconds at runtime.")]
    public LevelSettings[] levelSettings;
    [Tooltip("Required timer duration for every level: 30 seconds.")]
    public float defaultTimeLimit = 30f;
    [Tooltip("When a level has no LevelSettings asset, tighten only its star target by chapter. The timer remains 30 seconds.")]
    public bool useProgressiveDifficulty = true;
    public TimerDisplay timerDisplay;

    [Header("Level 1 encouragement")]
    [Tooltip("Once per session, a failed Level 1 attempt receives a free retry with full hearts and a fresh timer before any Luxodd Continue transaction.")]
    public bool grantLevelOneSecondChance = true;

    [Header("Tutorial (optional — only shows on first play of level 1)")]
    public TutorialController tutorial;

    [Header("Combo text (optional — floating HOLE IN ONE / PERFECT / NICE SAVE)")]
    public FloatingComboText comboText;

    [Header("World-space 3D celebration")]
    [Tooltip("True 3D hole-in-one reward. Auto-created at runtime for existing scenes.")]
    public HoleInOneCelebration3D holeInOneCelebration;

    [Header("Screen edge neon (optional)")]
    public EdgeGlowEffect edgeGlow;

    [Header("Luxodd integration (optional)")]
    public LuxoddGameBridge luxoddBridge;

    [Header("FOV kick on win/death (optional)")]
    [Tooltip("How much to change camera FOV during win (+ = zoom in). 0 disables.")]
    public float winFovKick = -4.5f;
    [Tooltip("How much to change camera FOV during death (+ = zoom in).")]
    public float deathFovKick = 5f;
    [Tooltip("Duration of the FOV kick interpolation in seconds.")]
    public float fovKickDuration = 0.8f;
    // TMP text so you can swap font asset, style, gradient, outline,
    // etc. directly in the Inspector on each runtime-animated piece of HUD.
    public TMP_Text levelLabel;
    public TMP_Text levelBanner;
    public Image flashOverlay;
    public TMP_Text shotCounter;
    [Tooltip("Live running score estimate shown below strokes.")]
    public TMP_Text scoreDisplay;
    [Tooltip("Legacy text hint. Leave empty if you use dragArrow instead.")]
    public TMP_Text dragHint;
    [Tooltip("Ghost-arrow hint. Fades in while the puck is idle, out while moving.")]
    public DragArrowHint dragArrow;

    [Header("Options")]
    [Tooltip("Delay between sinking the puck and loading the next level.")]
    public float transitionDelay = 0.9f;
    [Tooltip("After the last level, restart from level 1.")]
    public bool loopAtEnd = true;
    [Tooltip("Clear saved progress on Start (useful for testing).")]
    public bool resetProgressOnStart = false;

    [Header("Quick restart")]
    [Tooltip("Hold this player-owned arcade button to rebuild only the current level attempt. Purple is intentionally separate from Black (fire), White (back) and Luxodd's system controls.")]
    public ArcadeInputAdapter.Button quickRestartButton = ArcadeInputAdapter.Button.Purple;
    [Tooltip("Hold duration prevents an accidental tap from erasing a good attempt.")]
    [Min(0.35f)]
    public float quickRestartHoldSeconds = 1f;

    [Header("Puck")]
    // Keep in sync with BuildPuck() in GameSceneSetup.cs — both must
    // match so the shrink-into-hole and grow-back-in animations don't pop.
    public float puckSize = 0.6f;

    [Header("Camera follow")]
    public float followStrength = 0.14f;
    public float followSmoothTime = 0.35f;

    [Header("Debug Keys")]
    public KeyCode debugWinKey   = KeyCode.N;
    public KeyCode debugPrevKey  = KeyCode.P;
    public KeyCode debugResetKey = KeyCode.R;

    const string PrefKey = "BucaCurrentLevel";

    int _currentIndex;
    GameObject _currentInstance;
    bool _isTransitioning;

    // Camera shake + smooth follow state
    Camera _mainCam;
    Vector3 _camRestPos;
    Quaternion _camRestRot;
    float _shakeAmount, _shakeTime;
    // Cinematic victory camera push-in (see CompleteSequence / VictoryCameraPush).
    float _winCamBlend;          // 0 = rest, 1 = fully pushed toward the sunk hole
    Vector3 _winCamPushOffset;   // additive world offset applied at blend = 1
    Vector3 _followOffset, _followVel;

    // Coroutine handles so re-triggering doesn't double-animate
    Coroutine _bannerCo, _flashCo;

    // Shot counter + drag hint state
    int _shotCount;
    bool _puckWasStopped = true;
    float _dragHintAlpha;
    bool _dragHintArcadeMode;
    bool _dragHintTextInitialized;
    // Timer state
    float _timeRemaining;
    float _timeLimit;
    bool _timerActive;
    bool _timeUpTriggered;
    bool _levelOneSecondChanceUsed;
    ObstacleIntroController _obstacleIntro;
    bool _obstacleIntroActive;
    float _quickRestartHoldElapsed;
    bool _quickRestartConsumedForCurrentHold;

    // Live score display
    int _displayedScore;
    float _scoreDisplayVel;
    int _lastShownScore = int.MinValue;   // gates the per-frame score-string rebuild

    // Accumulated bonus from pickups (added to per-level score)
    int _bonusScoreThisLevel;

    // Rail tracking for star rating
    int _totalRailsInLevel;
    int _litRailCount;
    int _comboChain;        // fresh walls lit during the current shot
    int _comboShownMult;    // highest multiplier already announced this shot
    int _lives;
    int _currentMaxLives;
    bool _shotInProgress;   // a launched shot is currently in flight
    bool _levelFailed;

    [System.Serializable]
    public struct HoleScoreEntry
    {
        public int holeNumber;
        public int strokes;
        public int par;
        public int toPar;

        public HoleScoreEntry(int holeNumber, int strokes, int par)
        {
            this.holeNumber = holeNumber;
            this.strokes = Mathf.Max(1, strokes);
            this.par = Mathf.Max(1, par);
            toPar = this.strokes - this.par;
        }
    }

    // Campaign totals for the game-complete screen + live HUD score.
    // Keep an honest golf scorecard separately from Luxodd's points total:
    // fewer strokes is better in golf, while the server ranks score points.
    int _campaignTotalStrokes;
    int _campaignTotalPar;
    int _campaignTotalStars;
    int _campaignTotalScore; // cumulative sum of all completed-level scores this session
    readonly List<HoleScoreEntry> _campaignScorecard = new List<HoleScoreEntry>(30);
    public const string PrefLevelStars = "BucaStars_L"; // + index
    public const string PrefLevelScore = "BucaScore_L"; // + index
    public const string PrefLevelBestStrokes = "BucaBestStrokes_L"; // + index
    public const string PrefLevelPar = "BucaPar_L"; // + index

    // Magnet assist tracking (for "NICE SAVE!" combo text)
    float _lastMagnetAssistTime = -999f;
    public void NotifyMagnetAssist() { _lastMagnetAssistTime = Time.time; }

    // Hole "swallow anticipation" — puck is being pulled toward hole,
    // brighten the ring and scale it slightly. Value decays each frame
    // so as soon as the puck leaves magnet range the effect fades.
    float _holeAnticipation;
    float _holeAnticipationPulse;
    public void NotifyHoleAnticipation(float strength)
    {
        // Keep the highest this frame so multiple assists in the same frame
        // (shouldn't happen, but safe) pick the strongest pull.
        if (strength > _holeAnticipation) _holeAnticipation = strength;
    }

    // FOV kick state
    float _baseFov;
    float _fovOffset, _fovOffsetVel, _fovOffsetTarget;

    public int CurrentLevelIndex => _currentIndex;
    public int TotalLevels => levelPrefabs != null ? levelPrefabs.Length : 0;
    public GameObject Puck => puck;
    public GameObject CurrentLevelRoot => _currentInstance;
    public int CurrentLevelNumber => _currentIndex + 1;
    public int CurrentShotCount => _shotCount;
    public int CurrentPar => GetThreeStarStrokesForLevel(_currentIndex);
    public int CurrentDisplayedScore => _displayedScore;
    public int CampaignCompletedHoles => _campaignScorecard.Count;
    public int CampaignTotalStrokes => _campaignTotalStrokes;
    public int CampaignTotalPar => _campaignTotalPar;
    public int CampaignToPar => _campaignTotalStrokes - _campaignTotalPar;
    public int CampaignTotalScore => _campaignTotalScore;
    public IReadOnlyList<HoleScoreEntry> CampaignScorecard => _campaignScorecard;
    public bool LevelOneSecondChanceAvailable => grantLevelOneSecondChance
        && !_levelOneSecondChanceUsed;
    public bool QuickRestartAvailable => GameplayInputAllowed;
    public float QuickRestartHoldProgress => quickRestartHoldSeconds <= 0f
        ? 0f
        : Mathf.Clamp01(_quickRestartHoldElapsed / quickRestartHoldSeconds);
    /// <summary>
    /// Single authoritative gameplay-input gate. The puck must not keep
    /// accepting aim/fire input during transitions, failures or time-up.
    /// </summary>
    public bool GameplayInputAllowed => !_isTransitioning && !_levelFailed
        && !_obstacleIntroActive
        && _timerActive && !_timeUpTriggered && _timeRemaining > 0f;

    public static string FormatToPar(int difference)
    {
        if (difference == 0) return "E";
        return difference > 0 ? $"+{difference}" : difference.ToString();
    }

    /// <summary>
    /// Compact, readable golf card for result UI. Each item is one completed
    /// hole and its score relative to par (for example H03 -1 or H04 E).
    /// </summary>
    public string BuildCourseScorecard(int maxEntries = 30, int entriesPerLine = 10)
    {
        if (_campaignScorecard.Count == 0) return "NO COMPLETED HOLES";

        int safeMax = Mathf.Max(1, maxEntries);
        int safePerLine = Mathf.Max(1, entriesPerLine);
        int start = Mathf.Max(0, _campaignScorecard.Count - safeMax);
        var text = new StringBuilder(256);
        int shown = 0;
        for (int i = start; i < _campaignScorecard.Count; i++)
        {
            if (shown > 0)
                text.Append(shown % safePerLine == 0 ? "\n" : "   •   ");

            HoleScoreEntry entry = _campaignScorecard[i];
            text.Append("H");
            text.Append(entry.holeNumber.ToString("00"));
            text.Append(" ");
            text.Append(FormatToPar(entry.toPar));
            shown++;
        }
        return text.ToString();
    }

    void RecordCompletedHole(ScoreCalculator.ScoreBreakdown score)
    {
        var entry = new HoleScoreEntry(_currentIndex + 1, score.strokesUsed, score.par);
        _campaignScorecard.Add(entry);
        _campaignTotalStrokes += entry.strokes;
        _campaignTotalPar += entry.par;
        _campaignTotalStars += score.stars;
        _campaignTotalScore += score.total;
    }

    void ResetCampaignScorecard()
    {
        _campaignScorecard.Clear();
        _campaignTotalStrokes = 0;
        _campaignTotalPar = 0;
        _campaignTotalStars = 0;
        _campaignTotalScore = 0;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ApplyPremiumLevelTitleStyle();

        // Hearts are part of the active game HUD. Force this on at runtime so
        // older scene serialization cannot silently keep the life row hidden.
        useShotLives = true;
        ApplyLivesHudLayout();
        UpdateLivesDisplay();

        // Runtime-built and self-wiring: old Game scenes do not need a manual
        // prefab reference or an editor setup pass to receive obstacle intros.
        _obstacleIntro = GetComponent<ObstacleIntroController>();
        if (_obstacleIntro == null)
            _obstacleIntro = gameObject.AddComponent<ObstacleIntroController>();

        // Existing Game scenes need no editor rebuild: install the genuine
        // world-space celebration once and keep its objects pooled for reuse.
        if (holeInOneCelebration == null)
            holeInOneCelebration = GetComponent<HoleInOneCelebration3D>();
        if (holeInOneCelebration == null)
            holeInOneCelebration = gameObject.AddComponent<HoleInOneCelebration3D>();
    }

    void Start()
    {
        if (levelPrefabs == null || levelPrefabs.Length == 0)
        {
            Debug.LogError("[LevelManager] No level prefabs assigned. " +
                           "Drag your level prefabs into LevelManager.levelPrefabs.");
            return;
        }

        if (resetProgressOnStart)
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.DeleteKey(LevelSelectController.HighestUnlockedKey);
        }

        // If the level-select screen stashed a "pending" level to play,
        // honor it and clear the flag so normal progress resumes after.
        const string pendingKey = "BucaPendingLevel";
        int startIdx;
        if (PlayerPrefs.HasKey(pendingKey))
        {
            startIdx = PlayerPrefs.GetInt(pendingKey, 0);
            PlayerPrefs.DeleteKey(pendingKey);
        }
        else
        {
            startIdx = PlayerPrefs.GetInt(PrefKey, 0);
        }
        _currentIndex = Mathf.Clamp(startIdx, 0, levelPrefabs.Length - 1);

        _mainCam = Camera.main;
        if (_mainCam != null)
        {
            _camRestPos = _mainCam.transform.position;
            _camRestRot = _mainCam.transform.rotation;
            _baseFov = _mainCam.fieldOfView;
        }

        if (winRing != null) winRing.SetActive(false);
        if (flashOverlay != null) flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        if (levelBanner != null) levelBanner.color = new Color(1f, 1f, 1f, 0f);

        // Auto-find leaderboard panel — needed for the standalone fallback
        // path so the panel still appears on death/time-up without a bridge.
        if (leaderboardPanel == null)
            leaderboardPanel = FindFirstObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);
        if (levelFailedPanel == null)
            levelFailedPanel = FindFirstObjectByType<LevelFailedPanel>(FindObjectsInactive.Include);

        LoadLevel(_currentIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(debugWinKey))   CompleteLevel();
        if (Input.GetKeyDown(debugPrevKey))  GoToPrevious();
        // Full campaign reset remains a debug-only modified shortcut.
        bool resetModifier = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (resetModifier && Input.GetKeyDown(debugResetKey)) ResetProgress();

        TickQuickRestartInput(resetModifier);
        TickShotDetection();
        TickDragHint();
        TickTimer();
        TickLiveScore();
    }

    /// <summary>
    /// Player-facing restart for a bad shot. Purple must be held for one second;
    /// R is an editor/desktop fallback, while Shift+R keeps its existing full
    /// campaign-reset meaning. Holding the button across a modal or level change
    /// is consumed until release, preventing an accidental restart on the next
    /// playable frame.
    /// </summary>
    void TickQuickRestartInput(bool resetModifier)
    {
        bool held = ArcadeInputAdapter.GetButton(quickRestartButton)
            || (!resetModifier && Input.GetKey(debugResetKey));

        if (!held)
        {
            _quickRestartHoldElapsed = 0f;
            _quickRestartConsumedForCurrentHold = false;
            return;
        }

        if (!QuickRestartAvailable)
        {
            _quickRestartHoldElapsed = 0f;
            _quickRestartConsumedForCurrentHold = true;
            return;
        }

        if (_quickRestartConsumedForCurrentHold) return;

        _quickRestartHoldElapsed += Time.unscaledDeltaTime;
        float requiredHold = Mathf.Max(0.35f, quickRestartHoldSeconds);
        if (_quickRestartHoldElapsed < requiredHold) return;

        _quickRestartHoldElapsed = requiredHold;
        _quickRestartConsumedForCurrentHold = true;
        RestartCurrentLevel();
    }

    /// <summary>Watch for a "stopped → moving" transition; count that as a shot.</summary>
    void TickShotDetection()
    {
        if (puckRigidbody == null || _isTransitioning) return;
        bool stopped = puckRigidbody.linearVelocity.magnitude < 0.25f;
        if (_puckWasStopped && !stopped)
        {
            _shotCount++;
            _shotInProgress = true;   // a launched shot is now in flight
            _comboChain = 0;          // a fresh combo chain starts each shot
            _comboShownMult = 0;
            if (shotCounter != null)
                shotCounter.text = FormatStrokeHud();
            // First-shot tutorial dismissal
            if (tutorial != null) tutorial.MarkSeen();
        }
        else if (!_puckWasStopped && stopped && _shotInProgress)
        {
            // The puck came to rest WITHOUT sinking — a sink sets _isTransitioning,
            // which exits this method early, so reaching here means a MISSED shot.
            _shotInProgress = false;
            OnMissedShot();
        }
        _puckWasStopped = stopped;
    }

    // ── Optional legacy lives system ───────────────────────────
    // Normal BUCA play has unlimited strokes until time expires. Every stroke
    // still lowers the score through ScoreCalculator, so careful play remains
    // valuable without blocking players who need extra low-power adjustments.
    void OnMissedShot()
    {
        if (_levelFailed || _isTransitioning) return;

        if (!useShotLives)
        {
            // A miss is already recorded in _shotCount. Give light feedback,
            // but do not consume a life or end the level.
            ShakeCamera(0.06f, 0.08f);
            return;
        }

        _lives = Mathf.Max(0, _lives - 1);
        UpdateLivesDisplay();

        ShakeCamera(0.12f, 0.15f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWallHit(6f); // soft "miss" thud

        if (_lives <= 0) FailLevel();
        else FlashScreen(new Color(1f, 0.35f, 0.40f), 0.22f, 0.28f);              // brief red pulse
    }

    void FailLevel()
    {
        if (_levelFailed) return;
        _levelFailed = true;
        StartCoroutine(FailSequence());
    }

    System.Collections.IEnumerator FailSequence()
    {
        _isTransitioning = true;
        _timerActive = false;
        HideGameplayHudForResult();

        if (puckRigidbody != null)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            puckRigidbody.isKinematic = true;
        }

        FlashScreen(new Color(1f, 0.20f, 0.25f), 0.6f, 0.5f);
        ShakeCamera(0.25f, 0.35f);
        if (deathBurst != null && puck != null)
        {
            deathBurst.transform.position = puck.transform.position;
            deathBurst.Clear(true);
            deathBurst.Play(true);
        }

        yield return new WaitForSecondsRealtime(0.55f);

        if (AudioManager.Instance != null && AudioManager.Instance.gameOverMusic != null)
            AudioManager.Instance.PlayMusic(AudioManager.Instance.gameOverMusic, 0.4f);

        // Legacy shot-lives mode still uses the same platform transaction as
        // time-up: show the result, then let Luxodd offer Continue or End.
        yield return ShowTerminalLeaderboardAndOfferContinue("YOU LOST");
    }

    /// <summary>
    /// Backwards-compatible UI callback. Retry now follows the player-facing
    /// restart rule: reset the current level, not its timer or campaign state.
    /// </summary>
    public void RetryLevel()
    {
        if (_timeRemaining <= 0f || _timeUpTriggered) return;
        RestartCurrentLevelPreservingTimer(_timeRemaining, "RESTARTED!");
    }

    /// <summary>
    /// Public hook for a future on-screen Restart button. Resets the current
    /// level's puck, rails, score pickups, strokes and hearts, while preserving
    /// campaign totals, Continue credits and the remaining timer.
    /// </summary>
    public void RestartCurrentLevel()
    {
        if (_isTransitioning || _levelFailed || !_timerActive
            || _timeUpTriggered || _timeRemaining <= 0f) return;
        RestartCurrentLevelPreservingTimer(_timeRemaining, "RESTARTED!");
    }

    void RestartCurrentLevelPreservingTimer(float remaining, string banner)
    {
        if (levelPrefabs == null || _currentIndex < 0 || _currentIndex >= levelPrefabs.Length)
            return;

        _isTransitioning = true;
        _levelFailed = false;
        // Never carry a collision hit-stop into the rebuilt attempt.
        if (_hitStopCo != null)
        {
            StopCoroutine(_hitStopCo);
            _hitStopCo = null;
        }
        Time.timeScale = 1f;

        // This is still the same Luxodd level attempt, so do not send a duplicate
        // LevelBegin event. Only the local attempt state is rebuilt.
        LoadLevelInternal(_currentIndex, resetTimer: false,
            notifyLuxodd: false, preservedTime: remaining);

        // A deadly-wall animation shrinks the puck and its shadow to zero.
        // LoadLevelInternal rebuilds the board and physics state, but the puck
        // is a persistent scene object, so that zero scale used to survive the
        // full-hearts restart and make the player appear missing. Restore every
        // persistent visual here for all same-level restart paths.
        RestorePuckAfterAttemptReset();

        _isTransitioning = false;
        FlashScreen(new Color(0.25f, 0.9f, 1f), 0.28f, 0.24f);
        ShowBanner(banner);
    }

    void RestorePuckAfterAttemptReset()
    {
        if (puck != null)
        {
            if (!puck.activeSelf) puck.SetActive(true);
            puck.transform.localScale = Vector3.one * puckSize;
        }

        if (puckShadow != null)
        {
            if (!puckShadow.gameObject.activeSelf) puckShadow.gameObject.SetActive(true);
            puckShadow.localScale = new Vector3(
                puckSize * 1.9f, 0.004f, puckSize * 1.9f);
        }

        if (puckTrail != null)
        {
            puckTrail.Clear();
            puckTrail.emitting = true;
        }
    }

    /// <summary>
    /// Resolves one terminal failure. The leaderboard is shown once, then the
    /// Luxodd Continue transaction is opened. Continue restarts the current
    /// level with its full authored timer; End returns to the Luxodd host.
    /// A matching in-game prompt is used when running standalone.
    /// </summary>
    IEnumerator ShowTerminalLeaderboardAndOfferContinue(string reason)
    {
        // Level 1 is the player's onboarding moment. The first terminal failure
        // earns one encouraging retry before any leaderboard or paid Luxodd flow.
        if (_currentIndex == 0 && LevelOneSecondChanceAvailable
            && levelFailedPanel != null)
        {
            yield return ShowLevelOneSecondChance();
            yield break;
        }

        bool leaderboardFinished = false;
        int visibleScore = Mathf.Max(_campaignTotalScore, _displayedScore);

        if (luxoddBridge != null)
        {
            luxoddBridge.ShowLeaderboard(visibleScore, reason,
                () => leaderboardFinished = true);
        }
        else if (leaderboardPanel != null)
        {
            var localEntry = new[]
            {
                new LeaderboardPanel.LeaderboardData
                {
                    rank = 1,
                    playerName = "YOU",
                    score = visibleScore
                }
            };
            leaderboardPanel.Show(localEntry, 1, visibleScore, "YOU", reason,
                () => leaderboardFinished = true);
        }
        else
        {
            Debug.LogWarning("[LevelManager] No LeaderboardPanel found; continuing the session flow without it.");
            leaderboardFinished = true;
        }

        while (!leaderboardFinished) yield return null;

        bool decisionFinished = false;
        bool continueSelected = false;

        if (luxoddBridge != null && luxoddBridge.PlatformTransactionsExpected)
        {
            luxoddBridge.ShowPaidContinueOptions(
                () => { continueSelected = true; decisionFinished = true; },
                () => { decisionFinished = true; });
        }
        else if (levelFailedPanel != null)
        {
            // Editor/standalone fallback mirrors the cabinet choice so the
            // flow never silently exits merely because no host is connected.
            levelFailedPanel.ShowContinueChoice(reason,
                () => { continueSelected = true; decisionFinished = true; },
                () => { decisionFinished = true; });
        }
        else
        {
            EndCurrentSession();
            yield break;
        }

        while (!decisionFinished) yield return null;

        if (continueSelected)
        {
            ContinueCurrentLevelWithFreshTimer();
        }
        else if (luxoddBridge == null || !luxoddBridge.PlatformTransactionsExpected)
        {
            // The platform bridge owns End when connected. The local fallback
            // must explicitly end the standalone session itself.
            EndCurrentSession();
        }
    }

    IEnumerator ShowLevelOneSecondChance()
    {
        _levelOneSecondChanceUsed = true;
        bool retrySelected = false;

        if (AudioManager.Instance != null
            && AudioManager.Instance.niceSaveSfx != null)
            AudioManager.Instance.PlaySfx(AudioManager.Instance.niceSaveSfx);

        levelFailedPanel.ShowFirstLevelSecondChance(
            () => retrySelected = true);
        while (!retrySelected) yield return null;

        RestartLevelOneWithSecondChance();
    }

    void RestartLevelOneWithSecondChance()
    {
        Time.timeScale = 1f;
        _isTransitioning = true;
        _levelFailed = false;
        _timeUpTriggered = false;
        RestoreGameplayHudAfterResult();

        // This is a local onboarding retry inside the same Luxodd level attempt,
        // so do not emit a duplicate LevelBegin transaction.
        LoadLevelInternal(0, resetTimer: true,
            notifyLuxodd: false, preservedTime: 0f);
        RestorePuckAfterAttemptReset();

        _isTransitioning = false;
        FlashScreen(new Color(0.25f, 0.95f, 0.72f), 0.40f, 0.30f);
        ShowBanner("ONE MORE CHANCE!");
    }

    /// <summary>
    /// Applies a successful Luxodd Continue purchase. This is a new attempt at
    /// the same level: board, puck, strokes and timer reset, while campaign
    /// totals and previously earned level results remain intact.
    /// </summary>
    void ContinueCurrentLevelWithFreshTimer()
    {
        Time.timeScale = 1f;
        _isTransitioning = true;
        _levelFailed = false;
        _timeUpTriggered = false;
        RestoreGameplayHudAfterResult();

        LoadLevelInternal(_currentIndex, resetTimer: true,
            notifyLuxodd: true, preservedTime: 0f);
        RestorePuckAfterAttemptReset();

        _isTransitioning = false;
        FlashScreen(new Color(0.25f, 0.9f, 1f), 0.32f, 0.28f);
        ShowBanner("CONTINUE!");
    }

    void RestoreGameplayHudAfterResult()
    {
        if (levelLabel != null) levelLabel.gameObject.SetActive(true);
        if (levelBanner != null) levelBanner.gameObject.SetActive(true);
        if (shotCounter != null) shotCounter.gameObject.SetActive(true);
        if (dragHint != null) dragHint.gameObject.SetActive(true);
        if (dragArrow != null) dragArrow.gameObject.SetActive(true);

        var hintBar = FindFirstObjectByType<ControlHintBar>(FindObjectsInactive.Include);
        if (hintBar != null)
        {
            hintBar.gameObject.SetActive(true);
            hintBar.ResetForNewLevel();
        }
    }

    /// <summary>
    /// Removes every underlying gameplay label before a win/loss result or
    /// leaderboard is captured. Result panels own the screen while visible.
    /// </summary>
    void HideGameplayHudForResult()
    {
        if (levelLabel != null) levelLabel.gameObject.SetActive(false);
        if (levelBanner != null) levelBanner.gameObject.SetActive(false);
        if (shotCounter != null) shotCounter.gameObject.SetActive(false);
        if (scoreDisplay != null) scoreDisplay.gameObject.SetActive(false);
        if (livesDisplay != null) livesDisplay.gameObject.SetActive(false);
        if (dragHint != null) dragHint.gameObject.SetActive(false);
        if (dragArrow != null) dragArrow.gameObject.SetActive(false);
        if (timerDisplay != null) timerDisplay.Hide();

        if (lifeIcons != null)
            for (int i = 0; i < lifeIcons.Length; i++)
                if (lifeIcons[i] != null) lifeIcons[i].gameObject.SetActive(false);

        var hintBar = FindFirstObjectByType<ControlHintBar>(FindObjectsInactive.Include);
        if (hintBar != null) hintBar.gameObject.SetActive(false);
    }

    void EndCurrentSession()
    {
        _timerActive = false;
        _timeUpTriggered = true;

        if (luxoddBridge != null)
        {
            luxoddBridge.EndCurrentSession(_currentIndex, _campaignTotalScore);
            return;
        }

        Time.timeScale = 1f;
#if UNITY_EDITOR || UNITY_WEBGL
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
#else
        Application.Quit();
#endif
    }

    void UpdateLivesDisplay()
    {
        if (!useShotLives)
        {
            if (livesDisplay != null) livesDisplay.gameObject.SetActive(false);
            if (lifeIcons != null)
            {
                for (int i = 0; i < lifeIcons.Length; i++)
                    if (lifeIcons[i] != null) lifeIcons[i].gameObject.SetActive(false);
            }
            return;
        }

        // Preferred: heart icons (red = remaining, faint = lost). The scene
        // contains enough slots for the largest budget; hide unused slots on
        // later chapters so the HUD always matches the current level.
        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                {
                    lifeIcons[i].gameObject.SetActive(true);
                    lifeIcons[i].gameObject.SetActive(i < _currentMaxLives);
                    lifeIcons[i].color = (i < _lives) ? lifeFullColor : lifeEmptyColor;
                }
            }
            return;
        }
        // Fallback: text hearts (if no icons were wired).
        if (livesDisplay != null)
        {
            livesDisplay.gameObject.SetActive(true);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _currentMaxLives; i++)
                sb.Append(i < _lives ? "<color=#FF2E5B>♥</color> " : "<color=#FFFFFF30>♥</color> ");
            livesDisplay.text = sb.ToString();
        }
    }

    /// <summary>
    /// Keeps the glossy heart icons directly below the three-row stats column.
    /// The hearts live on their own screen-space canvas, so anchoring them to
    /// its top-left corner makes the placement stable at every aspect ratio.
    /// </summary>
    void ApplyLivesHudLayout()
    {
        if (lifeIcons == null || lifeIcons.Length == 0) return;

        const float startX = 48f;
        const float topOffset = 300f;
        const float iconSize = 52f;
        const float spacing = 62f;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            Image heart = lifeIcons[i];
            if (heart == null) continue;

            RectTransform rt = heart.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(startX + i * spacing, -topOffset);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.localScale = Vector3.one;

            heart.preserveAspect = true;
            heart.raycastTarget = false;
        }
    }

    /// <summary>Counts down the level timer and triggers time-up on expiry.</summary>
    int _lastTimerTickSecond = -1;

    /// <summary>
    /// A death/restart animation temporarily gates TickTimer. If the original
    /// allocation expires during that animation, route into the one canonical
    /// time-up flow exactly once.
    /// </summary>
    void TriggerTimeUpAfterInterruptedTransition()
    {
        if (_timeUpTriggered) return;
        _timeRemaining = 0f;
        _timerActive = false;
        _timeUpTriggered = true;
        _isTransitioning = false;
        if (timerDisplay != null) timerDisplay.SetTime(0f);
        StartCoroutine(TimeUpSequence());
    }

    void TickTimer()
    {
        if (!_timerActive || _isTransitioning || _obstacleIntroActive || _timeUpTriggered) return;

        float prev = _timeRemaining;
        _timeRemaining -= Time.deltaTime;
        if (timerDisplay != null) timerDisplay.SetTime(_timeRemaining);

        // Audio: drive tense-mode music swap + per-second tick in last 5s
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.NotifyTimerRemaining(_timeRemaining, true);
            int curSec = Mathf.CeilToInt(_timeRemaining);
            int prevSec = Mathf.CeilToInt(prev);
            if (curSec != prevSec && curSec > 0 && curSec <= 5)
            {
                AudioManager.Instance.PlayTimerCountdownVoice(curSec);
                _lastTimerTickSecond = curSec;
            }
        }

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _timerActive = false;
            _timeUpTriggered = true;
            StartCoroutine(TimeUpSequence());
        }
    }

    /// <summary>
    /// Time ran out: stop gameplay immediately, show the terminal leaderboard,
    /// then return to Luxodd. The puck remains visible behind the result.
    /// </summary>
    IEnumerator TimeUpSequence()
    {
        _isTransitioning = true;
        _timerActive = false;
        HideGameplayHudForResult();

        // Freeze physics and clear a partially charged/aimed shot immediately.
        if (puckRigidbody != null)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            puckRigidbody.isKinematic = true;
        }
        if (puckController != null) puckController.ResetInputState();

        // VFX: red-orange flash + strong shake
        ShakeCamera(0.5f, 0.55f);
        FlashScreen(new Color(1f, 0.4f, 0.15f), 0.6f, 0.5f);
        if (deathFovKick != 0f) StartCoroutine(KickFov(deathFovKick, fovKickDuration));

        // The leaderboard screen owns the single loss message. Do not launch
        // the old white center banner underneath it (duplicate TIME'S UP).
        if (levelBanner != null)
        {
            Color hiddenBanner = levelBanner.color;
            hiddenBanner.a = 0f;
            levelBanner.color = hiddenBanner;
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.timeUpSfx);
            AudioManager.Instance.SetMagnetLoopActive(false);
            AudioManager.Instance.SetWindLoopActive(false);
            AudioManager.Instance.SetGravityLoopActive(false);
            if (AudioManager.Instance.gameOverMusic != null)
                AudioManager.Instance.PlayMusic(AudioManager.Instance.gameOverMusic, 0.4f);
        }

        if (deathBurst != null && puck != null)
        {
            deathBurst.transform.position = puck.transform.position;
            deathBurst.Clear(true);
            deathBurst.Play(true);
        }

        // Keep the player visible. The previous implementation shrank it to
        // zero and waited 1.3 seconds before opening the modal, which looked
        // like gameplay was continuing with a missing player.
        Vector3 baseScale = puck != null && puck.transform.localScale.sqrMagnitude > 0.001f
            ? puck.transform.localScale
            : Vector3.one * puckSize;
        if (puck != null) puck.transform.localScale = baseScale;
        if (puckShadow != null)
            puckShadow.localScale = new Vector3(puckSize * 1.9f, 0.004f, puckSize * 1.9f);
        if (puckTrail != null) puckTrail.emitting = false;

        // Begin the terminal result UI in this frame. Its reveal animation
        // supplies the transition before the session returns to Luxodd.
        yield return ShowTerminalLeaderboardAndOfferContinue("TIME'S UP!");
        yield break;
    }

    /// <summary>
    /// Computes a live running-score estimate and shows the campaign
    /// total (finished levels + current-level projection). This matches
    /// what the leaderboard would display — gives consistent feedback
    /// with the server-side ranking.
    /// </summary>
    void TickLiveScore()
    {
        if (scoreDisplay == null || _isTransitioning) return;

        int starStrokes = GetThreeStarStrokesForLevel(_currentIndex);

        // Project what this level would score if finished right now.
        var est = ScoreCalculator.Calculate(
            Mathf.Max(1, _shotCount), _timeRemaining, _timeLimit,
            _litRailCount, _totalRailsInLevel,
            starStrokes, 0);

        // Cumulative total = finished levels + current projection + pickup bonus.
        int target = _campaignTotalScore + est.total + _bonusScoreThisLevel;

        float smooth = Mathf.SmoothDamp(_displayedScore, target, ref _scoreDisplayVel, 0.25f);
        _displayedScore = Mathf.RoundToInt(smooth);

        // Only rebuild the string when the number actually changes (not 60×/sec).
        if (_displayedScore != _lastShownScore)
        {
            _lastShownScore = _displayedScore;
            scoreDisplay.text = FormatScoreHud(_displayedScore);
        }
    }

    /// <summary>
    /// Fade the drag hint(s) in while the puck is stationary and available
    /// for input; fade out as soon as it starts moving or during a
    /// level transition. Supports either legacy text hint (dragHint) or
    /// the ghost-arrow hint (dragArrow) — both can be assigned at once.
    /// </summary>
    void TickDragHint()
    {
        bool visible = !_isTransitioning
                    && puckRigidbody != null
                    && puckRigidbody.linearVelocity.magnitude < 0.25f;
        float target = visible ? 1f : 0f;
        _dragHintAlpha = Mathf.MoveTowards(_dragHintAlpha, target, Time.deltaTime * 2.5f);

        // QA req: the serialized "DRAG BACK TO AIM" text describes the MOUSE
        // slingshot, but the arcade mechanic is tilt-the-stick-TOWARD-the-
        // target + hold the button — the old text was actively misleading on
        // the cabinet. Swap wording the moment arcade input is detected
        // (cached so we only touch TMP when the mode actually flips).
        if (dragHint != null)
        {
            bool arcade = LuxoddGameBridge.IsArcadeInputActive;
            if (arcade != _dragHintArcadeMode || !_dragHintTextInitialized)
            {
                _dragHintArcadeMode = arcade;
                _dragHintTextInitialized = true;
                dragHint.text = arcade
                    ? "AIM: STICK  •  BLACK: POWER"
                    : "DRAG TO AIM";
            }
        }

        if (dragHint != null)
        {
            float pulse = visible ? (0.75f + 0.25f * Mathf.Sin(Time.time * 3.2f)) : 1f;
            var c = dragHint.color;
            c.a = _dragHintAlpha * pulse * 0.85f;
            dragHint.color = c;
        }

        if (dragArrow != null)
        {
            // Arrow drives its own alpha envelope; we just gate visibility.
            dragArrow.externalAlpha = _dragHintAlpha;
        }
    }

    void LateUpdate()
    {
        if (_mainCam != null)
        {
            // Smooth follow offset
            Vector3 target = Vector3.zero;
            if (puck != null)
            {
                Vector3 p = puck.transform.position;
                target = new Vector3(p.x * followStrength, 0f, p.z * followStrength * 0.7f);
            }
            _followOffset = Vector3.SmoothDamp(_followOffset, target, ref _followVel, followSmoothTime);

            // Shake offset
            Vector3 shake = Vector3.zero;
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                float a = _shakeAmount * Mathf.Clamp01(_shakeTime);
                float x = (Mathf.PerlinNoise(Time.time * 28f, 0.1f) - 0.5f) * 2f * a;
                float y = (Mathf.PerlinNoise(0.2f, Time.time * 28f) - 0.5f) * 2f * a;
                shake = new Vector3(x, y, 0f);
            }

            _mainCam.transform.position = _camRestPos + _followOffset + shake
                + _winCamBlend * _winCamPushOffset;
            _mainCam.transform.rotation = _camRestRot;
        }

        // Shadow blob tracks the puck on the floor plane
        if (puckShadow != null && puck != null)
        {
            Vector3 pp = puck.transform.position;
            puckShadow.position = new Vector3(pp.x, 0.015f, pp.z);
        }

        // Hole "swallow anticipation" — ring scales + brightens while
        // the magnet is pulling the puck in. Decays when not being pulled.
        _holeAnticipation = Mathf.MoveTowards(_holeAnticipation, 0f, Time.deltaTime * 1.8f);
        _holeAnticipationPulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 9f);
        ApplyHoleAnticipation();

        // FOV kick (win/death) — exponentially eased toward target, then
        // target decays to 0 so the camera returns to base FOV on its own.
        if (_mainCam != null)
        {
            _fovOffsetTarget = Mathf.MoveTowards(_fovOffsetTarget, 0f, Time.deltaTime * 8f);
            _fovOffset = Mathf.SmoothDamp(_fovOffset, _fovOffsetTarget, ref _fovOffsetVel, 0.18f);
            _mainCam.fieldOfView = _baseFov + _fovOffset;
        }

        // Drive edge glow based on puck state
        DriveEdgeGlow();
    }

    // ═══════════════════════════════════════════════════════════
    // Level flow
    // ═══════════════════════════════════════════════════════════

    public void LoadLevel(int index)
    {
        LoadLevelInternal(index, resetTimer: true, notifyLuxodd: true, preservedTime: 0f);
    }

    /// <summary>
    /// Shared level initializer. Normal progression starts a fresh 30-second
    /// timer and reports LevelBegin. A current-level Restart rebuilds the same
    /// gameplay state but carries the existing timer and avoids a duplicate
    /// Luxodd level-begin event.
    /// </summary>
    void LoadLevelInternal(int index, bool resetTimer, bool notifyLuxodd, float preservedTime)
    {
        if (index < 0) return;
        if (levelPrefabs == null || index >= levelPrefabs.Length) return;

        if (_currentInstance != null)
        {
            // Destroy is deferred until the end of the frame. Disable the old
            // hierarchy first so its colliders/triggers cannot overlap the newly
            // spawned level for one physics step.
            _currentInstance.SetActive(false);
            Destroy(_currentInstance);
        }
        _currentIndex = index;

        _currentInstance = Instantiate(levelPrefabs[index]);
        _currentInstance.name = levelPrefabs[index].name;
        RemoveMisleadingInertDecor(_currentInstance);
        PreferredRouteBuilder.Install(_currentInstance, _currentIndex + 1);

        PlayerPrefs.SetInt(PrefKey, _currentIndex);
        PlayerPrefs.Save();

        PositionPuckAtStart();
        _shotCount = 0;
        _puckWasStopped = true;
        _currentMaxLives = useShotLives ? GetMaxLivesForLevel(_currentIndex) : 0;
        _lives = _currentMaxLives;
        _shotInProgress = false;
        _levelFailed = false;
        UpdateLivesDisplay();
        if (shotCounter != null) shotCounter.text = FormatStrokeHud();
        // Don't reset displayed score to 0 — keep campaign-total continuity.
        // TickLiveScore immediately recomputes with the new level's data.
        _displayedScore = _campaignTotalScore;
        _scoreDisplayVel = 0f;
        _bonusScoreThisLevel = 0;
        _lastMagnetAssistTime = -999f;
        if (scoreDisplay != null) scoreDisplay.text = FormatScoreHud(_campaignTotalScore);

        // Count rails + reset any lit state from previous playthrough
        var rails = _currentInstance.GetComponentsInChildren<RailLight>(true);
        _totalRailsInLevel = rails.Length;
        _litRailCount = 0;
        _comboChain = 0;
        _comboShownMult = 0;
        for (int i = 0; i < rails.Length; i++) rails[i].Reset();

        // Hide star UI until the win moment
        SetStarDisplay(0, false);

        // A normal level load always starts at 30. A retry/restart carries the
        // remaining allocation, so it cannot be used to manufacture more time.
        _timeLimit = GetTimeLimitForLevel(_currentIndex);
        _timeRemaining = resetTimer
            ? _timeLimit
            : Mathf.Clamp(preservedTime, 0f, _timeLimit);
        _timerActive = _timeLimit > 0f && _timeRemaining > 0f;
        _timeUpTriggered = false;
        if (timerDisplay != null)
        {
            timerDisplay.Init(_timeLimit);
            timerDisplay.SetTime(_timeRemaining);
        }

        UpdateHud();

        // Reset the control-hint bar so it shows again for the first
        // few shots of every new level (otherwise once it auto-hides
        // in level 1 it stays hidden forever across LoadLevel calls).
        var hintBar = FindFirstObjectByType<ControlHintBar>(FindObjectsInactive.Include);
        if (hintBar != null)
        {
            hintBar.gameObject.SetActive(true);
            hintBar.ResetForNewLevel();
        }

        // Audio: level-start whoosh + restore gameplay music. We need to
        // explicitly request gameplayMusic here because TimeUpSequence /
        // DeathSequence may have swapped to gameOverMusic — without this
        // the player respawns into the game-over track stuck on loop.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.levelStartSfx);
            if (AudioManager.Instance.gameplayMusic != null)
                AudioManager.Instance.PlayMusic(AudioManager.Instance.gameplayMusic, 0.6f);
            // Reset all looping-mechanic sources — we may have left magnet/
            // wind/gravity hum playing if the previous level got interrupted.
            AudioManager.Instance.SetMagnetLoopActive(false);
            AudioManager.Instance.SetWindLoopActive(false);
            AudioManager.Instance.SetGravityLoopActive(false);
        }

        // Luxodd: notify server that a new level started.
        if (notifyLuxodd && luxoddBridge != null)
        {
            Debug.Log($"[LevelManager] Calling luxoddBridge.OnLevelBegin(level={_currentIndex + 1})");
            luxoddBridge.OnLevelBegin(_currentIndex);
        }
        else if (notifyLuxodd)
        {
            Debug.LogWarning("[LevelManager] luxoddBridge is NULL — LevelBegin NOT sent. " +
                             "Start from MainMenu scene to ensure the Luxodd bridge is loaded.");
        }

        BeginObstacleIntroForCurrentLevel();
    }

    /// <summary>
    /// Removes old cosmetic pieces that visually read like raised obstacles but
    /// intentionally have no collider or gameplay behaviour. Target rings,
    /// lane inlays and mechanic telegraphs remain because their flat visual
    /// language communicates the goal/route. The centre medallion did not
    /// communicate anything and confused players, so it is hidden immediately
    /// on every instantiated level and then destroyed safely.
    /// </summary>
    static void RemoveMisleadingInertDecor(GameObject levelRoot)
    {
        if (levelRoot == null) return;

        Transform[] all = levelRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform item = all[i];
            if (item == null) continue;
            if (!item.name.Equals("NM4_MedalDark", System.StringComparison.Ordinal) &&
                !item.name.Equals("NM4_MedalBrass", System.StringComparison.Ordinal))
                continue;

            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// Pauses only gameplay input, puck physics and the local 30-second timer
    /// while a first-encounter mechanic card is visible. No life, time or shot
    /// can be lost before the paying player knows what the obstacle does.
    /// </summary>
    void BeginObstacleIntroForCurrentLevel()
    {
        _obstacleIntroActive = false;
        if (_obstacleIntro == null || _currentInstance == null) return;

        bool started = _obstacleIntro.TryShowForLevel(
            _currentInstance, _currentIndex + 1, FinishObstacleIntro);
        if (!started) return;

        _obstacleIntroActive = true;
        if (puckRigidbody != null)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            puckRigidbody.isKinematic = true;
        }
        if (puckController != null) puckController.ResetInputState();
    }

    void FinishObstacleIntro()
    {
        if (!_obstacleIntroActive) return;
        _obstacleIntroActive = false;

        // Re-teleport through the authoritative rigidbody-safe path. This also
        // clears a held Black press so it cannot immediately fire the puck when
        // the tutorial video closes.
        if (!_timeUpTriggered && _currentInstance != null)
            PositionPuckAtStart();
    }

    public void CompleteLevel()
    {
        if (_isTransitioning) return;
        if (_obstacleIntroActive) return;
        StartCoroutine(CompleteSequence());
    }

    // Cinematic victory push-in: eases the camera toward the sunk hole and back.
    // Purely additive on the rig pose (LateUpdate adds _winCamBlend * _winCamPushOffset),
    // so the camera always returns exactly to its rest pose afterward.
    IEnumerator VictoryCameraPush(Vector3 focus, float duration)
    {
        Vector3 toFocus = new Vector3(focus.x - _camRestPos.x, 0f, focus.z - _camRestPos.z);
        _winCamPushOffset = toFocus * 0.30f + new Vector3(0f, -1.3f, 1.6f);

        float inDur = duration * 0.45f;
        float holdDur = duration * 0.15f;
        float outDur = Mathf.Max(0.01f, duration - inDur - holdDur);

        float t = 0f;
        while (t < inDur)  { t += Time.deltaTime; _winCamBlend = Mathf.SmoothStep(0f, 1f, t / inDur);  yield return null; }
        _winCamBlend = 1f;
        yield return new WaitForSeconds(holdDur);
        t = 0f;
        while (t < outDur) { t += Time.deltaTime; _winCamBlend = Mathf.SmoothStep(1f, 0f, t / outDur); yield return null; }
        _winCamBlend = 0f;
    }

    IEnumerator CompleteSequence()
    {
        _isTransitioning = true;

        // The result presentation owns the screen from the instant the hole is
        // completed. In particular, the three/four gameplay hearts must never
        // remain layered above a win result or Luxodd leaderboard.
        HideGameplayHudForResult();

        // Freeze puck immediately
        if (puckRigidbody != null)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            puckRigidbody.isKinematic = true;
        }

        // Find hole position for the burst origin
        Vector3 burstPos = puck != null ? puck.transform.position : Vector3.zero;
        if (_currentInstance != null)
        {
            var holeTf = _currentInstance.transform.Find("Hole");
            if (holeTf != null) burstPos = new Vector3(holeTf.position.x, 0.1f, holeTf.position.z);
        }

        // Decide the reward tier before any visuals start. A hole-in-one uses
        // only world-space effects; ordinary wins retain the HUD flash.
        bool recentAssist = (Time.time - _lastMagnetAssistTime) < 0.6f;
        int comboType = 0;
        if (_shotCount == 1)
            comboType = 4; // HOLE IN ONE — optional rails never downgrade it
        else if (_litRailCount >= _totalRailsInLevel && _totalRailsInLevel > 0)
            comboType = 3; // PERFECT
        else if (recentAssist)
            comboType = 1; // NICE SAVE

        PlayWinBurst(burstPos);
        StartCoroutine(AnimateWinRing(burstPos));
        ShakeCamera(0.25f, 0.35f);
        if (comboType != 4)
            FlashScreen(new Color(0.55f, 0.95f, 1f), 0.35f, 0.35f);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMagnetLoopActive(false);
        }

        // Use familiar golf language for the main result. Rail and assist
        // bonuses still affect scoring, but they no longer hide the stroke result.
        int starStrokes = GetThreeStarStrokesForLevel(_currentIndex);
        if (comboType == 4 && holeInOneCelebration != null)
        {
            // No flat Canvas callout for a hole-in-one. The award appears in
            // the game world above the hole and uses the live puck's 3D mesh.
            holeInOneCelebration.Play(burstPos, _mainCam, puck);
        }
        // The shared 3D result totem presents the golf result once. Do not
        // also place a large flat BOGEY/PAR callout over it.
        if (AudioManager.Instance != null)
        {
            // Let the dedicated hole-in-one jingle stand on its own instead of
            // muddying it by playing the ordinary level-complete sound too.
            if (comboType != 4)
                AudioManager.Instance.PlaySfx(AudioManager.Instance.levelCompleteSfx);
            AudioManager.Instance.PlayCombo(comboType);
        }

        // FOV zoom-in during win sequence
        if (winFovKick != 0f) StartCoroutine(KickFov(winFovKick, fovKickDuration));

        // Calculate score via the unified formula.
        var score = ScoreCalculator.Calculate(
            _shotCount, _timeRemaining, _timeLimit,
            _litRailCount, _totalRailsInLevel,
            starStrokes, comboType);

        // Roll in pickup bonuses collected during the level
        score.total += _bonusScoreThisLevel;

        // Persist best stars + best score (monotonic — never decrease).
        int prevBestStars = PlayerPrefs.GetInt(PrefLevelStars + _currentIndex, 0);
        if (score.stars > prevBestStars)
            PlayerPrefs.SetInt(PrefLevelStars + _currentIndex, score.stars);
        int prevBestScore = PlayerPrefs.GetInt(PrefLevelScore + _currentIndex, 0);
        if (score.total > prevBestScore)
            PlayerPrefs.SetInt(PrefLevelScore + _currentIndex, score.total);
        int prevBestStrokes = PlayerPrefs.GetInt(PrefLevelBestStrokes + _currentIndex, int.MaxValue);
        if (score.strokesUsed < prevBestStrokes)
            PlayerPrefs.SetInt(PrefLevelBestStrokes + _currentIndex, score.strokesUsed);
        // Store the authored par with the best-strokes record so the Main Menu
        // can render a persistent per-hole golf result without a LevelManager.
        PlayerPrefs.SetInt(PrefLevelPar + _currentIndex, score.par);

        // ── Progression: completing this level UNLOCKS the next one. ──
        // BucaHighestLevel = highest playable level index. Levels above it stay
        // locked in the picker. Monotonic — only ever increases.
        const string highestKey = "BucaHighestLevel";
        int newUnlock = Mathf.Min(_currentIndex + 1, levelPrefabs.Length - 1);
        if (newUnlock > PlayerPrefs.GetInt(highestKey, 0))
            PlayerPrefs.SetInt(highestKey, newUnlock);

        PlayerPrefs.Save();

        // Luxodd: report level completion with score to server.
        if (luxoddBridge != null)
        {
            Debug.Log($"[LevelManager] Calling luxoddBridge.OnLevelEnd(level={_currentIndex + 1}, score={score.total})");
            luxoddBridge.OnLevelEnd(_currentIndex, score.total);
            luxoddBridge.SaveUserState();
        }
        else
        {
            Debug.LogWarning("[LevelManager] luxoddBridge is NULL — level score NOT sent to server. " +
                             "Did you start the game from the MainMenu scene? The bridge must be " +
                             "carried over via DontDestroyOnLoad from MainMenu.");
        }

        // Record this completed hole once. Result screens read the same entry,
        // so current-hole, running total and final total can never disagree.
        RecordCompletedHole(score);

        // ── Cinematic sink: spiral the puck down the hole throat (decaying
        //    radius + downward dip + spin) instead of a flat shrink-to-center,
        //    with a camera push-in toward the hole. This is the payoff every
        //    shot builds to — the single biggest "premium" moment. ──
        float shrinkDur = 0.5f;
        float t = 0f;
        Vector3 baseScale = Vector3.one * puckSize;
        Vector3 startScale = puck != null ? puck.transform.localScale : baseScale;
        Vector3 startPos = puck != null ? puck.transform.position : Vector3.zero;
        Vector3 holePos = new Vector3(burstPos.x, startPos.y, burstPos.z);

        Vector3 fromHole = startPos - holePos; fromHole.y = 0f;
        float startRadius = Mathf.Max(fromHole.magnitude, 0.35f); // visible orbit even from dead-center
        float startAngle = Mathf.Atan2(fromHole.z, fromHole.x);
        const float swirlTurns = 2.2f;                            // loops on the way down
        if (puckTrail != null) puckTrail.emitting = true;         // trail draws the spiral arc

        // Camera eases toward the hole during the swirl, then back to rest.
        StartCoroutine(VictoryCameraPush(holePos, shrinkDur + 0.15f));

        while (t < shrinkDur && puck != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / shrinkDur);
            float ease = k * k;                                   // accelerate as it falls in
            float radius = Mathf.Lerp(startRadius, 0f, ease);
            float angle = startAngle + swirlTurns * Mathf.PI * 2f * k;
            float dip = -0.42f * ease;                            // sinks below the lip
            puck.transform.position = holePos +
                new Vector3(Mathf.Cos(angle) * radius, dip, Mathf.Sin(angle) * radius);
            puck.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ease);
            puck.transform.Rotate(0f, 760f * Time.deltaTime, 0f, Space.Self); // fast spin
            yield return null;
        }
        if (puck != null) puck.transform.localScale = Vector3.zero;
        if (puckTrail != null) puckTrail.emitting = false;
        if (puckShadow != null) puckShadow.localScale = Vector3.zero;

        float minimumWinDisplay = comboType == 4 && holeInOneCelebration != null
            ? holeInOneCelebration.MinimumDisplaySeconds
            : transitionDelay;
        yield return new WaitForSeconds(Mathf.Max(0f,
            Mathf.Max(transitionDelay, minimumWinDisplay) - shrinkDur));

        // Advance level
        int next = _currentIndex + 1;
        bool finishedCampaign = next >= levelPrefabs.Length;
        int campaignMaxLevels = levelPrefabs.Length;

        if (finishedCampaign)
        {
            // Platform Restart/End takes priority over the old local loop.
            // Otherwise loopAtEnd silently jumps to level 1 and the backend's
            // enabled Restart transaction never appears.
            if (luxoddBridge != null && luxoddBridge.PlatformTransactionsExpected)
            {
                if (gameCompletePanel != null)
                    gameCompletePanel.Show(_campaignTotalStrokes, _campaignTotalPar, _campaignTotalStars,
                        campaignMaxLevels * 3);

                _isTransitioning = false;
                luxoddBridge.OnCampaignComplete(
                    _campaignTotalStrokes, _campaignTotalStars, _campaignTotalScore,
                    null, // Luxodd creates the restarted session; never reload locally.
                    () => { }); // the bridge owns the host-return action
                yield break;
            }
            else if (loopAtEnd)
            {
                next = 0;
            }
            else if (gameCompletePanel != null)
            {
                // Luxodd: report campaign completion + trigger Restart popup.
                // _campaignTotalScore here ALREADY includes this level's score
                // (added on line ~681 a moment ago), so we don't need to add
                // score.total again. Sending the cumulative campaign total is
                // what the leaderboard ranking expects.
                gameCompletePanel.Show(_campaignTotalStrokes, _campaignTotalPar,
                    _campaignTotalStars, campaignMaxLevels * 3);
                _isTransitioning = false;
                yield break;
            }
            else { _isTransitioning = false; yield break; }
        }
        // Show the level-complete score breakdown panel. The panel handles
        // the 5-second auto-advance timer + Space-to-skip. When the player
        // continues (either way), it calls our AdvanceToNextLevel callback.
        if (levelCompletePanel != null)
        {
            levelCompletePanel.Show(score, () => StartCoroutine(AdvanceAfterPanel(next)));
            // CompleteSequence yields here — AdvanceAfterPanel picks up.
            yield break;
        }
        else
        {
            // Fallback: no panel → advance immediately (old behavior).
            yield return DoLevelTransition(next);
        }
    }

    /// <summary>Called by LevelCompletePanel's onContinue callback after
    /// the player presses Space or the 5-second timer expires.</summary>
    IEnumerator AdvanceAfterPanel(int nextIndex)
    {
        yield return DoLevelTransition(nextIndex);
    }

    /// <summary>
    /// Shared level-swap animation: loads the next authored board material
    /// immediately, then grows the puck back in.
    /// </summary>
    IEnumerator DoLevelTransition(int nextIndex)
    {
        // Standalone loop mode starts a genuinely new course. Clear the prior
        // run only after its final result screen has finished displaying.
        if (nextIndex == 0 && levelPrefabs != null
            && _currentIndex == levelPrefabs.Length - 1)
            ResetCampaignScorecard();

        RestoreGameplayHudAfterResult();
        LoadLevel(nextIndex);

        Vector3 baseScale = Vector3.one * puckSize;
        float growDur = 0.3f, t = 0f;
        while (t < growDur && puck != null)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / growDur);
            puck.transform.localScale = Vector3.Lerp(Vector3.zero, baseScale, k);
            yield return null;
        }
        if (puck != null) puck.transform.localScale = baseScale;
        if (puckShadow != null)
            puckShadow.localScale = new Vector3(puckSize * 1.9f, 0.004f, puckSize * 1.9f);
        if (puckTrail != null) { puckTrail.Clear(); puckTrail.emitting = true; }

        _isTransitioning = false;
    }

    public void GoToPrevious()
    {
        int prev = _currentIndex - 1;
        if (prev < 0) prev = levelPrefabs.Length - 1;
        LoadLevel(prev);
    }

    public void ResetProgress()
    {
        ResetCampaignScorecard();
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.DeleteKey(LevelSelectController.HighestUnlockedKey);
        PlayerPrefs.Save();
        LoadLevel(0);
    }

    /// <summary>Clear progress + per-level stars/scores, then reload the Game scene.</summary>
    public void ResetProgressAndReloadScene()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.DeleteKey(LevelSelectController.HighestUnlockedKey);
        if (levelPrefabs != null)
            for (int i = 0; i < levelPrefabs.Length; i++)
            {
                PlayerPrefs.DeleteKey(PrefLevelStars + i);
                PlayerPrefs.DeleteKey(PrefLevelScore + i);
                PlayerPrefs.DeleteKey(PrefLevelBestStrokes + i);
                PlayerPrefs.DeleteKey(PrefLevelPar + i);
            }

        PlayerPrefs.Save();

        // Also wipe the SERVER copy. The Luxodd state sync merges with
        // max(server, local), so without this the old server progress
        // would simply flow back on the next session, silently undoing
        // the reset the player just asked for.
        if (luxoddBridge != null) luxoddBridge.ResetServerProgress();

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void RespawnPuck()
    {
        if (puckController != null) puckController.ResetToStart();
        ShakeCamera(0.2f, 0.2f);
    }

    /// <summary>
    /// Dramatic death: explosion burst at puck, red flash, strong shake,
    /// then the puck respawns at the start position with a grow-in.
    /// </summary>
    public void KillPuck()
    {
        if (_isTransitioning) return;
        if (_obstacleIntroActive) return;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        _isTransitioning = true;
        float deathStartedAt = Time.unscaledTime;

        // Freeze physics
        if (puckRigidbody != null)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            puckRigidbody.isKinematic = true;
        }

        Vector3 deathPos = puck != null ? puck.transform.position : Vector3.zero;

        // VFX: red flash + strong shake + explosion burst
        if (deathBurst != null)
        {
            deathBurst.transform.position = deathPos;
            deathBurst.Clear(true);
            deathBurst.Play(true);
        }
        ShakeCamera(0.4f, 0.45f);
        FlashScreen(new Color(1f, 0.25f, 0.3f), 0.55f, 0.4f);
        if (deathFovKick != 0f) StartCoroutine(KickFov(deathFovKick, fovKickDuration));
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.puckDeathSfx);
            AudioManager.Instance.SetMagnetLoopActive(false);
            AudioManager.Instance.SetWindLoopActive(false);
            AudioManager.Instance.SetGravityLoopActive(false);
        }

        // Puck quickly scales to 0 (it "explodes")
        float t = 0f, dur = 0.18f;
        Vector3 baseScale = Vector3.one * puckSize;
        Vector3 startScale = puck != null ? puck.transform.localScale : baseScale;
        if (puckTrail != null) puckTrail.emitting = false;
        while (t < dur && puck != null)
        {
            t += Time.deltaTime;
            float k = t / dur;
            puck.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
            yield return null;
        }
        if (puck != null) puck.transform.localScale = Vector3.zero;
        if (puckShadow != null) puckShadow.localScale = Vector3.zero;

        // Brief pause at zero scale so the player reads "I died"
        yield return new WaitForSeconds(0.3f);

        // The launched stroke is already counted. In normal unlimited-stroke
        // mode, a deadly hazard costs time and resets the puck, but never ends
        // the level because of a heart budget.
        _shotInProgress = false;
        _puckWasStopped = true;
        if (useShotLives)
        {
            _lives = Mathf.Max(0, _lives - 1);
            UpdateLivesDisplay();
        }

        float remainingAfterDeath = Mathf.Max(
            0f, _timeRemaining - (Time.unscaledTime - deathStartedAt));
        if (remainingAfterDeath <= 0f)
        {
            if (puck != null) puck.transform.localScale = baseScale;
            if (puckShadow != null)
                puckShadow.localScale = new Vector3(puckSize * 1.9f, 0.004f, puckSize * 1.9f);
            if (puckTrail != null) { puckTrail.Clear(); puckTrail.emitting = true; }
            TriggerTimeUpAfterInterruptedTransition();
            yield break;
        }

        // A deadly hazard consuming the final heart is the same terminal loss
        // as any other zero-heart path: show the result, then close the session.
        if (useShotLives && _lives <= 0)
        {
            if (puck != null) puck.transform.localScale = baseScale;
            if (puckShadow != null)
                puckShadow.localScale = new Vector3(puckSize * 1.9f, 0.004f, puckSize * 1.9f);
            if (puckTrail != null) { puckTrail.Clear(); puckTrail.emitting = false; }
            _timerActive = false;
            HideGameplayHudForResult();
            if (AudioManager.Instance != null && AudioManager.Instance.gameOverMusic != null)
                AudioManager.Instance.PlayMusic(AudioManager.Instance.gameOverMusic, 0.4f);
            yield return ShowTerminalLeaderboardAndOfferContinue("YOU LOST");
            yield break;
        }

        // Teleport to start
        PositionPuckAtStart();

        // Grow back in
        float t2 = 0f;
        float growDur = 0.3f;
        while (t2 < growDur && puck != null)
        {
            t2 += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t2 / growDur);
            puck.transform.localScale = Vector3.Lerp(Vector3.zero, baseScale, k);
            yield return null;
        }
        if (puck != null) puck.transform.localScale = baseScale;
        if (puckShadow != null)
            puckShadow.localScale = new Vector3(puckSize * 1.9f, 0.004f, puckSize * 1.9f);
        if (puckTrail != null) { puckTrail.Clear(); puckTrail.emitting = true; }

        // The respawn animation also counts against the same allocation.
        _timeRemaining = Mathf.Max(
            0f, _timeRemaining - (Time.unscaledTime - deathStartedAt));
        if (timerDisplay != null) timerDisplay.SetTime(_timeRemaining);
        if (_timeRemaining <= 0f)
        {
            TriggerTimeUpAfterInterruptedTransition();
            yield break;
        }

        _isTransitioning = false;
    }

    /// <summary>
    /// Returns the current level's "Hole" child position in world space.
    /// Returns Vector3.positiveInfinity if no level/hole is found — callers
    /// should check for that sentinel and skip any magnet/follow logic.
    /// </summary>
    public Vector3 GetCurrentHolePosition()
    {
        if (_currentInstance == null) return Vector3.positiveInfinity;
        var hole = _currentInstance.transform.Find("Hole");
        if (hole == null) return Vector3.positiveInfinity;
        return hole.position;
    }

    /// <summary>
    /// Visually highlights the current level's Hole_Ring when the puck
    /// is being drawn in by the magnet assist. Scales 1.0..1.25 and
    /// pulses emission slightly — feels like the hole is "hungry".
    /// </summary>
    void ApplyHoleAnticipation()
    {
        if (_currentInstance == null) return;
        var ring = _currentInstance.transform.Find("Hole_Ring");
        if (ring == null) return;

        float strength = _holeAnticipation;
        float pulse = 1f + strength * 0.25f * _holeAnticipationPulse;
        ring.localScale = new Vector3(1.15f * pulse, 0.04f, 1.15f * pulse);

        var mr = ring.GetComponent<Renderer>();
        if (mr == null || mr.material == null) return;
        if (!mr.material.HasProperty("_EmissionColor")) return;
        // Boost emission when anticipation is high.
        Color baseEmission = new Color(3.2f, 2.8f, 1.2f);
        Color boost = baseEmission * (1f + strength * 1.3f);
        mr.material.SetColor("_EmissionColor", boost);
        mr.material.EnableKeyword("_EMISSION");
    }

    IEnumerator KickFov(float delta, float duration)
    {
        _fovOffsetTarget = delta;
        yield return new WaitForSeconds(duration * 0.5f);
        _fovOffsetTarget = 0f;
    }

    /// <summary>
    /// Feeds the EdgeGlowEffect based on what the puck is doing:
    ///  - Near deadly wall (fast approach) / just killed → pink
    ///  - Near hole / being pulled in → warm yellow
    ///  - Idle → 0
    /// </summary>
    void DriveEdgeGlow()
    {
        if (edgeGlow == null) return;

        // Yellow "near hole" intensity — driven by magnet assist strength.
        float yellow = _holeAnticipation;
        // Pink "danger" — ramp up when moving fast and far from hole.
        float pink = 0f;
        if (puckRigidbody != null)
        {
            float speed = puckRigidbody.linearVelocity.magnitude;
            pink = Mathf.Clamp01((speed - 10f) / 8f) * (1f - yellow);
        }
        edgeGlow.SetIntensities(yellow, pink);
    }

    /// <summary>Plays the wall-spark burst at a given world point (called by PuckController).</summary>
    public void PlayWallSpark(Vector3 pos, Vector3 normal)
    {
        if (wallSparkBurst == null) return;
        wallSparkBurst.transform.position = pos;
        wallSparkBurst.transform.rotation = Quaternion.LookRotation(normal);
        wallSparkBurst.Clear(true);
        wallSparkBurst.Play(true);
    }

    /// <summary>
    /// Called by PuckController on every collision. If the collider has
    /// a RailLight and it's not yet lit, lights it and increments the
    /// contribution toward the star-rating score.
    /// </summary>
    public void NotifyWallHit(Collider col)
    {
        if (col == null) return;
        var rail = col.GetComponentInParent<RailLight>();
        if (rail == null) return;
        if (rail.LightUp())
        {
            _litRailCount++;
            RegisterCombo(col.transform.position);
        }
    }

    // ── Combo & flair scoring ──────────────────────────────────
    // Each fresh wall the puck lights in a single shot extends a chain that raises
    // a ×1→×5 multiplier and awards escalating BONUS points — added on TOP of the
    // normal score via AddBonusScore, so base scoring/rules are unchanged. The chain
    // resets each new shot (and on level load).
    const int ComboBasePoints = 25;
    void RegisterCombo(Vector3 worldPos)
    {
        _comboChain++;
        int mult = Mathf.Min(_comboChain, 5);
        AddBonusScore(ComboBasePoints * mult, worldPos);     // additive bonus + spark
        if (mult >= 2 && mult > _comboShownMult && comboText != null)
        {
            _comboShownMult = mult;
            comboText.Show($"COMBO  ×{mult}!", ComboColor(mult));
        }
    }

    static Color ComboColor(int mult)
    {
        switch (mult)
        {
            case 5:  return new Color(1f, 0.30f, 0.50f);   // hot pink
            case 4:  return new Color(1f, 0.55f, 0.20f);   // orange
            case 3:  return new Color(1f, 0.85f, 0.30f);   // gold
            default: return new Color(0.50f, 0.95f, 1f);   // cyan
        }
    }

    /// <summary>
    /// Shows the leaderboard panel with placeholder names and the
    /// player's local campaign score. Used as the standalone fallback
    /// when no LuxoddGameBridge is wired in the scene — without this
    /// the panel never appears outside a real arcade environment.
    /// </summary>
    System.Collections.IEnumerator ShowStandaloneLeaderboard()
    {
        // No fake/placeholder leaderboard in standalone — those demo names
        // (ARCADE_KING, P1_HERO, …) must never show to a real player. On a real
        // Luxodd arcade the genuine leaderboard is shown via LuxoddGameBridge on
        // a separate path; in standalone we just continue (restart/respawn).
        yield break;
    }

    /// <summary>
    /// 3 stars = finished within the per-level threeStarStrokes target.
    /// 2 stars = finished within 2× the target.
    /// 1 star  = finished the level at all.
    /// Rails award bonus score only; they never downgrade an efficient shot.
    ///
    /// Reads the per-level stroke target from levelSettings if present;
    /// falls back to the LevelManager-wide threeStarStrokes default.
    /// (The other scoring sites do this lookup too — keeping them in sync.)
    /// </summary>
    int CalculateStars()
    {
        int starStrokes = GetThreeStarStrokesForLevel(_currentIndex);

        if (_shotCount <= starStrokes) return 3;
        if (_shotCount <= starStrokes * 2) return 2;
        return 1;
    }

    /// <summary>Every level uses the same clear 30-second countdown.</summary>
    float GetTimeLimitForLevel(int index)
    {
        return 30f;
    }

    /// <summary>
    /// Legacy fallback used only when Use Shot Lives is explicitly enabled.
    /// Normal BUCA gameplay has no heart/shot limit.
    /// </summary>
    int GetMaxLivesForLevel(int index)
    {
        // One authoritative campaign table: Levels 1-24 use three hearts;
        // the final chapter uses four hearts.
        return index >= 24 ? 4 : 3;
    }

    /// <summary>
    /// Golf-style HUD: the player always sees the current stroke count and the
    /// level's par target together, so star expectations are never hidden.
    /// </summary>
    string FormatStrokeHud()
    {
        int par = GetThreeStarStrokesForLevel(_currentIndex);
        return $"<color=#DDF7FF>STROKES</color><pos=260><color=#FFFFFF>{_shotCount}</color>\n" +
               $"<color=#DDF7FF>PAR</color><pos=260><color=#FFD633>{par}</color>";
    }

    static string FormatScoreHud(int score)
    {
        return $"<color=#DDF7FF>SCORE</color><pos=260><color=#00DFFF>{score:N0}</color>";
    }

    static Color GetGolfResultColor(int strokesToPar)
    {
        if (strokesToPar <= -2) return new Color(1f, 0.84f, 0.24f);
        if (strokesToPar == -1) return new Color(0.35f, 1f, 0.68f);
        if (strokesToPar == 0) return new Color(0.30f, 0.88f, 1f);
        return new Color(1f, 0.43f, 0.50f);
    }

    /// <summary>Returns the authored par for the current level.</summary>
    int GetThreeStarStrokesForLevel(int index)
    {
        if (levelSettings != null && index >= 0 && index < levelSettings.Length
            && levelSettings[index] != null)
            return Mathf.Max(1, levelSettings[index].threeStarStrokes);

        if (!useProgressiveDifficulty) return Mathf.Max(1, threeStarStrokes);
        return index >= 24 ? 1 : Mathf.Max(1, threeStarStrokes);
    }

    void SetStarDisplay(int stars, bool visible)
    {
        if (starImages == null) return;
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            var c = (i < stars) ? starLitColor : starUnlitColor;
            c.a = visible ? c.a : 0f;
            starImages[i].color = c;
        }
    }

    IEnumerator AnimateStarReveal(int stars)
    {
        if (starImages == null) yield break;
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            var rt = starImages[i].rectTransform;
            float dur = 0.35f, t = 0f;
            Color target = (i < stars) ? starLitColor : starUnlitColor;
            // Audio: ascending "ting" per earned star
            if (i < stars && AudioManager.Instance != null)
                AudioManager.Instance.PlayStarReveal(i);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                float s = Mathf.Lerp(1.6f, 1f, e);
                rt.localScale = new Vector3(s, s, 1f);
                var c = target; c.a = e * target.a;
                starImages[i].color = c;
                yield return null;
            }
            rt.localScale = Vector3.one;
            starImages[i].color = target;
            yield return new WaitForSeconds(0.12f);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Puck teleport (rigidbody-safe)
    // ═══════════════════════════════════════════════════════════
    void PositionPuckAtStart()
    {
        if (puck == null || _currentInstance == null) return;

        var startMarker = _currentInstance.transform.Find("PuckStart");
        Vector3 startPos = startMarker != null
            ? new Vector3(startMarker.position.x, puckSize * 0.5f, startMarker.position.z)
            : new Vector3(0f, puckSize * 0.5f, -5f);

        var rb = puckRigidbody;
        if (rb != null)
        {
            rb.isKinematic = true;
            puck.transform.position = startPos;
            puck.transform.rotation = Quaternion.identity;
            rb.position = startPos;
            rb.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            puck.transform.position = startPos;
        }

        if (puckTrail != null) puckTrail.Clear();
        if (puckController != null)
        {
            puckController.StartPosition = startPos;
            puckController.ResetInputState();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // VFX — operate on pre-built objects
    // ═══════════════════════════════════════════════════════════
    void PlayWinBurst(Vector3 pos)
    {
        if (winBurst == null) return;
        winBurst.transform.position = pos;
        winBurst.Clear(true);
        winBurst.Play(true);
    }

    IEnumerator AnimateWinRing(Vector3 pos)
    {
        if (winRing == null) yield break;

        winRing.transform.position = pos + new Vector3(0f, 0.02f, 0f);
        winRing.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
        winRing.SetActive(true);

        Material mat = winRingRenderer != null ? winRingRenderer.material : null;
        Color baseC = new Color(1f, 0.95f, 0.4f, 1f);
        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor")) baseC = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) baseC = mat.GetColor("_Color");
        }

        float dur = 0.6f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float s = Mathf.Lerp(0.2f, 4.5f, k);
            winRing.transform.localScale = new Vector3(s, 0.01f, s);
            if (mat != null)
            {
                var c = new Color(baseC.r, baseC.g, baseC.b, 1f - k);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
            }
            yield return null;
        }
        winRing.SetActive(false);
    }

    public void ShakeCamera(float amount, float duration)
    {
        _shakeAmount = amount;
        _shakeTime = duration;
    }

    /// <summary>
    /// Called by ScorePickup when the puck collects an orb. Adds to
    /// this level's bonus total and fires a small spark at the position.
    /// </summary>
    public void AddBonusScore(int amount, Vector3 worldPos)
    {
        _bonusScoreThisLevel += amount;
        // Reuse the wall spark burst as a generic "ping" visual.
        if (wallSparkBurst != null)
        {
            wallSparkBurst.transform.position = worldPos;
            wallSparkBurst.Clear(true);
            wallSparkBurst.Play(true);
        }
    }

    Coroutine _hitStopCo;
    /// <summary>Brief time-scale dip to sell impact weight (hit-stop).</summary>
    public void TriggerHitStop(float scale, float durationUnscaled)
    {
        if (_hitStopCo != null) StopCoroutine(_hitStopCo);
        _hitStopCo = StartCoroutine(HitStopRoutine(scale, durationUnscaled));
    }

    IEnumerator HitStopRoutine(float scale, float duration)
    {
        float prev = Time.timeScale;
        Time.timeScale = scale;
        // WaitForSecondsRealtime uses unscaled time, so the freeze itself
        // isn't slowed — it always lasts exactly `duration` seconds.
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
        _hitStopCo = null;
    }

    // ═══════════════════════════════════════════════════════════
    // HUD animations (operate on pre-built UI)
    // ═══════════════════════════════════════════════════════════
    void UpdateHud()
    {
        ApplyPremiumLevelTitleStyle();
        if (levelLabel != null)
            // Use a fixed compact gap. Two normal spaces expanded with the
            // title's character spacing and made the number look detached.
            levelLabel.text = $"<color=#78E8FF>LEVEL</color><space=10px>" +
                              $"<color=#FFFFFF>{_currentIndex + 1}</color>";
        ShowBanner($"LEVEL {_currentIndex + 1}");
    }

    /// <summary>
    /// Keeps the persistent level title above the board, readable on every
    /// aspect ratio and visually separate from the left stats column.
    /// Presentation only; level flow and gameplay are unchanged.
    /// </summary>
    void ApplyPremiumLevelTitleStyle()
    {
        if (levelLabel == null) return;

        RectTransform rt = levelLabel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -22f);
        rt.sizeDelta = new Vector2(560f, 86f);
        rt.localScale = Vector3.one;

        levelLabel.alignment = TextAlignmentOptions.Center;
        levelLabel.fontStyle = FontStyles.Bold;
        levelLabel.enableAutoSizing = true;
        levelLabel.fontSizeMin = 38f;
        levelLabel.fontSizeMax = 58f;
        levelLabel.characterSpacing = 3f;
        levelLabel.textWrappingMode = TextWrappingModes.NoWrap;
        levelLabel.overflowMode = TextOverflowModes.Overflow;
        levelLabel.richText = true;
        levelLabel.raycastTarget = false;
        levelLabel.color = Color.white;
        levelLabel.outlineWidth = 0.18f;
        levelLabel.outlineColor = new Color(0.01f, 0.03f, 0.08f, 0.96f);

        // This is a Canvas element, so it remains above the 3D board while
        // full-screen result/leaderboard panels can still cover it correctly.
    }

    public void ShowBanner(string text)
    {
        if (levelBanner == null) return;
        if (_bannerCo != null) StopCoroutine(_bannerCo);
        _bannerCo = StartCoroutine(BannerRoutine(text));
    }

    IEnumerator BannerRoutine(string text)
    {
        levelBanner.text = text;
        var rt = levelBanner.rectTransform;

        float inDur = 0.28f, t = 0f;
        while (t < inDur)
        {
            t += Time.deltaTime;
            float k = t / inDur;
            float e = 1f - Mathf.Pow(1f - k, 3f);
            levelBanner.color = new Color(1f, 1f, 1f, e);
            float s = Mathf.Lerp(1.45f, 1f, e);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        levelBanner.color = Color.white;
        rt.localScale = Vector3.one;

        yield return new WaitForSeconds(0.55f);

        float outDur = 0.35f;
        t = 0f;
        while (t < outDur)
        {
            t += Time.deltaTime;
            float k = t / outDur;
            levelBanner.color = new Color(1f, 1f, 1f, 1f - k);
            float s = Mathf.Lerp(1f, 1.15f, k);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        levelBanner.color = new Color(1f, 1f, 1f, 0f);
        rt.localScale = Vector3.one;
        _bannerCo = null;
    }

    public void FlashScreen(Color color, float peakAlpha = 0.55f, float duration = 0.35f)
    {
        if (flashOverlay == null) return;
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(FlashRoutine(color, peakAlpha, duration));
    }

    IEnumerator FlashRoutine(Color color, float peakAlpha, float duration)
    {
        float half = duration * 0.35f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, peakAlpha, t / half);
            flashOverlay.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        float outDur = duration - half;
        t = 0f;
        while (t < outDur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(peakAlpha, 0f, t / outDur);
            flashOverlay.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        flashOverlay.color = new Color(color.r, color.g, color.b, 0f);
        _flashCo = null;
    }
}
