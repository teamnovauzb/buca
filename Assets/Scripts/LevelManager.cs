using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Timer")]
    [Tooltip("Per-level settings array — index matches levelPrefabs. Leave entries " +
             "null to use the default time limit.")]
    public LevelSettings[] levelSettings;
    [Tooltip("Default time limit used when no LevelSettings asset exists for a level.")]
    public float defaultTimeLimit = 30f;
    public TimerDisplay timerDisplay;

    [Header("Tutorial (optional — only shows on first play of level 1)")]
    public TutorialController tutorial;

    [Header("Combo text (optional — floating HOLE IN ONE / PERFECT / NICE SAVE)")]
    public FloatingComboText comboText;

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
    Vector3 _followOffset, _followVel;

    // Coroutine handles so re-triggering doesn't double-animate
    Coroutine _bannerCo, _flashCo;

    // Shot counter + drag hint state
    int _shotCount;
    bool _puckWasStopped = true;
    float _dragHintAlpha;

    // Timer state
    float _timeRemaining;
    float _timeLimit;
    bool _timerActive;
    bool _timeUpTriggered;

    // Live score display
    int _displayedScore;
    float _scoreDisplayVel;

    // Rail tracking for star rating
    int _totalRailsInLevel;
    int _litRailCount;

    // Campaign totals for the game-complete screen
    int _campaignTotalStrokes;
    int _campaignTotalStars;
    public const string PrefLevelStars = "BucaStars_L"; // + index
    public const string PrefLevelScore = "BucaScore_L"; // + index

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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (levelPrefabs == null || levelPrefabs.Length == 0)
        {
            Debug.LogError("[LevelManager] No level prefabs assigned. Run RealBuca > Setup Game Scene.");
            return;
        }

        if (resetProgressOnStart) PlayerPrefs.DeleteKey(PrefKey);

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

        LoadLevel(_currentIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(debugWinKey))   CompleteLevel();
        if (Input.GetKeyDown(debugPrevKey))  GoToPrevious();
        if (Input.GetKeyDown(debugResetKey)) ResetProgress();

        TickShotDetection();
        TickDragHint();
        TickTimer();
        TickLiveScore();
    }

    /// <summary>Watch for a "stopped → moving" transition; count that as a shot.</summary>
    void TickShotDetection()
    {
        if (puckRigidbody == null || _isTransitioning) return;
        bool stopped = puckRigidbody.linearVelocity.magnitude < 0.25f;
        if (_puckWasStopped && !stopped)
        {
            _shotCount++;
            if (shotCounter != null)
                shotCounter.text = $"STROKES  {_shotCount}";
            // First-shot tutorial dismissal
            if (tutorial != null) tutorial.MarkSeen();
        }
        _puckWasStopped = stopped;
    }

    /// <summary>Counts down the level timer and triggers time-up on expiry.</summary>
    void TickTimer()
    {
        if (!_timerActive || _isTransitioning || _timeUpTriggered) return;

        _timeRemaining -= Time.deltaTime;
        if (timerDisplay != null) timerDisplay.SetTime(_timeRemaining);

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _timeUpTriggered = true;
            StartCoroutine(TimeUpSequence());
        }
    }

    /// <summary>
    /// Time ran out: dramatic VFX, then either:
    /// - Luxodd mode: show Continue popup (player pays credits to retry)
    /// - Standalone mode: restart same level immediately
    /// </summary>
    IEnumerator TimeUpSequence()
    {
        _isTransitioning = true;

        // Freeze puck
        if (puckRigidbody != null)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            puckRigidbody.isKinematic = true;
        }

        // VFX: red-orange flash + strong shake
        ShakeCamera(0.5f, 0.55f);
        FlashScreen(new Color(1f, 0.4f, 0.15f), 0.6f, 0.5f);
        if (deathFovKick != 0f) StartCoroutine(KickFov(deathFovKick, fovKickDuration));

        ShowBanner("TIME'S UP!");

        if (deathBurst != null && puck != null)
        {
            deathBurst.transform.position = puck.transform.position;
            deathBurst.Clear(true);
            deathBurst.Play(true);
        }

        // Shrink puck out
        float t = 0f, dur = 0.25f;
        Vector3 baseScale = Vector3.one * puckSize;
        if (puckTrail != null) puckTrail.emitting = false;
        while (t < dur && puck != null)
        {
            t += Time.deltaTime;
            puck.transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, t / dur);
            yield return null;
        }
        if (puck != null) puck.transform.localScale = Vector3.zero;
        if (puckShadow != null) puckShadow.localScale = Vector3.zero;

        yield return new WaitForSeconds(1.3f);

        // Luxodd: show Continue popup (player pays credits to retry).
        // Standalone: just restart immediately.
        if (luxoddBridge != null)
        {
            bool waitingForChoice = true;
            luxoddBridge.OnTimeUp(
                onContinue: () =>
                {
                    // Player paid credits → restart the same level
                    waitingForChoice = false;
                },
                onEnd: () =>
                {
                    // Player chose End → session ends, system handles exit.
                    // Nothing more to do here; bridge calls BackToSystem.
                    waitingForChoice = false;
                    _isTransitioning = false;
                });

            // Wait until the Luxodd popup resolves
            while (waitingForChoice) yield return null;

            // If we're still transitioning, player chose Continue — restart level
            if (!_isTransitioning) yield break;
        }

        // Restart the same level
        LoadLevel(_currentIndex);

        // Grow puck back in
        t = 0f;
        float growDur = 0.3f;
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

    /// <summary>
    /// Computes a live running-score estimate based on current state
    /// (strokes so far, time left, rails lit) and animates the HUD
    /// counter toward it. Gives the player real-time feedback — the
    /// score climbs as they light rails and ticks down as time passes.
    /// </summary>
    void TickLiveScore()
    {
        if (scoreDisplay == null || _isTransitioning) return;

        int starStrokes = threeStarStrokes;
        if (levelSettings != null && _currentIndex < levelSettings.Length && levelSettings[_currentIndex] != null)
            starStrokes = levelSettings[_currentIndex].threeStarStrokes;

        // Estimate current score as if the player won right now with no combo.
        var est = ScoreCalculator.Calculate(
            Mathf.Max(1, _shotCount), _timeRemaining, _timeLimit,
            _litRailCount, _totalRailsInLevel,
            starStrokes, 0);

        // Smooth the displayed value so it doesn't jitter every frame.
        float target = est.total;
        float smooth = Mathf.SmoothDamp(_displayedScore, target, ref _scoreDisplayVel, 0.25f);
        _displayedScore = Mathf.RoundToInt(smooth);

        scoreDisplay.text = $"SCORE  {_displayedScore}";
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

            _mainCam.transform.position = _camRestPos + _followOffset + shake;
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
        if (levelPrefabs == null || index < 0 || index >= levelPrefabs.Length) return;

        if (_currentInstance != null) Destroy(_currentInstance);

        _currentIndex = index;
        _currentInstance = Instantiate(levelPrefabs[index]);
        _currentInstance.name = levelPrefabs[index].name;

        PlayerPrefs.SetInt(PrefKey, _currentIndex);
        PlayerPrefs.Save();

        PositionPuckAtStart();
        _shotCount = 0;
        _puckWasStopped = true;
        if (shotCounter != null) shotCounter.text = "STROKES  0";
        _displayedScore = 0;
        _scoreDisplayVel = 0f;
        if (scoreDisplay != null) scoreDisplay.text = "SCORE  0";

        // Count rails + reset any lit state from previous playthrough
        var rails = _currentInstance.GetComponentsInChildren<RailLight>(true);
        _totalRailsInLevel = rails.Length;
        _litRailCount = 0;
        for (int i = 0; i < rails.Length; i++) rails[i].Reset();

        // Hide star UI until the win moment
        SetStarDisplay(0, false);

        // Timer — read per-level settings or fall back to default
        _timeLimit = defaultTimeLimit;
        if (levelSettings != null && _currentIndex < levelSettings.Length && levelSettings[_currentIndex] != null)
            _timeLimit = levelSettings[_currentIndex].timeLimit;
        _timeRemaining = _timeLimit;
        _timerActive = _timeLimit > 0f;
        _timeUpTriggered = false;
        if (timerDisplay != null) timerDisplay.Init(_timeLimit);

        UpdateHud();

        // Luxodd: notify server that a new level started.
        if (luxoddBridge != null)
        {
            Debug.Log($"[LevelManager] Calling luxoddBridge.OnLevelBegin(level={_currentIndex + 1})");
            luxoddBridge.OnLevelBegin(_currentIndex);
        }
        else
        {
            Debug.LogWarning("[LevelManager] luxoddBridge is NULL — LevelBegin NOT sent. " +
                             "Start from MainMenu scene to ensure the Luxodd bridge is loaded.");
        }
    }

    public void CompleteLevel()
    {
        if (_isTransitioning) return;
        StartCoroutine(CompleteSequence());
    }

    IEnumerator CompleteSequence()
    {
        _isTransitioning = true;

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

        PlayWinBurst(burstPos);
        StartCoroutine(AnimateWinRing(burstPos));
        ShakeCamera(0.25f, 0.35f);
        FlashScreen(new Color(0.55f, 0.95f, 1f), 0.35f, 0.35f);

        // Determine combo type for scoring.
        bool recentAssist = (Time.time - _lastMagnetAssistTime) < 0.6f;
        int comboType = 0;
        if (_shotCount == 1 && _litRailCount >= _totalRailsInLevel && _totalRailsInLevel > 0)
            comboType = 4; // HOLE IN ONE
        else if (_litRailCount >= _totalRailsInLevel && _totalRailsInLevel > 0)
            comboType = 3; // PERFECT
        else if (_shotCount == 1)
            comboType = 2; // ONE SHOT
        else if (recentAssist)
            comboType = 1; // NICE SAVE

        // Combo text feedback.
        if (comboText != null)
        {
            switch (comboType)
            {
                case 4: comboText.Show("HOLE IN ONE!", new Color(1f, 0.9f, 0.3f)); break;
                case 3: comboText.Show("PERFECT!", new Color(1f, 0.5f, 0.95f)); break;
                case 2: comboText.Show("ONE SHOT!", new Color(1f, 0.9f, 0.3f)); break;
                case 1: comboText.Show("NICE SAVE!", new Color(0.5f, 1f, 0.8f)); break;
            }
        }

        // FOV zoom-in during win sequence
        if (winFovKick != 0f) StartCoroutine(KickFov(winFovKick, fovKickDuration));

        // Calculate score via the unified formula.
        int starStrokes = threeStarStrokes;
        if (levelSettings != null && _currentIndex < levelSettings.Length && levelSettings[_currentIndex] != null)
            starStrokes = levelSettings[_currentIndex].threeStarStrokes;
        var score = ScoreCalculator.Calculate(
            _shotCount, _timeRemaining, _timeLimit,
            _litRailCount, _totalRailsInLevel,
            starStrokes, comboType);

        // Persist best stars + best score (monotonic — never decrease).
        int prevBestStars = PlayerPrefs.GetInt(PrefLevelStars + _currentIndex, 0);
        if (score.stars > prevBestStars)
            PlayerPrefs.SetInt(PrefLevelStars + _currentIndex, score.stars);
        int prevBestScore = PlayerPrefs.GetInt(PrefLevelScore + _currentIndex, 0);
        if (score.total > prevBestScore)
            PlayerPrefs.SetInt(PrefLevelScore + _currentIndex, score.total);
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

        // Track campaign totals for the end screen.
        _campaignTotalStrokes += _shotCount;
        _campaignTotalStars   += score.stars;

        // Shrink puck into hole
        float shrinkDur = 0.35f;
        float t = 0f;
        Vector3 baseScale = Vector3.one * puckSize;
        Vector3 startScale = puck != null ? puck.transform.localScale : baseScale;
        Vector3 startPos = puck != null ? puck.transform.position : Vector3.zero;
        Vector3 targetPos = new Vector3(burstPos.x, startPos.y, burstPos.z);
        if (puckTrail != null) puckTrail.emitting = false;

        while (t < shrinkDur && puck != null)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / shrinkDur);
            puck.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
            puck.transform.position = Vector3.Lerp(startPos, targetPos, k);
            yield return null;
        }
        if (puck != null) puck.transform.localScale = Vector3.zero;
        if (puckShadow != null) puckShadow.localScale = Vector3.zero;

        yield return new WaitForSeconds(Mathf.Max(0f, transitionDelay - shrinkDur));

        // Advance level
        int next = _currentIndex + 1;
        bool finishedCampaign = next >= levelPrefabs.Length;
        if (finishedCampaign)
        {
            if (loopAtEnd)
            {
                next = 0;
            }
            else if (gameCompletePanel != null)
            {
                // Luxodd: report campaign completion + trigger Restart popup.
                if (luxoddBridge != null)
                    luxoddBridge.OnCampaignComplete(_campaignTotalStrokes, _campaignTotalStars, score.total);

                gameCompletePanel.Show(_campaignTotalStrokes, _campaignTotalStars, levelPrefabs.Length * 3);
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
            // Hide timer during the panel
            if (timerDisplay != null) timerDisplay.Hide();

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
    /// Shared level-swap animation: captures outgoing floor color,
    /// loads the next level, cross-fades floor, grows puck back in.
    /// </summary>
    IEnumerator DoLevelTransition(int nextIndex)
    {
        Color? outgoingFloorColor = GetCurrentFloorColor();

        LoadLevel(nextIndex);

        if (outgoingFloorColor.HasValue)
            StartCoroutine(FadeFloorFromTo(outgoingFloorColor.Value, 0.55f));

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
        PlayerPrefs.DeleteKey(PrefKey);
        LoadLevel(0);
    }

    /// <summary>Clear progress + per-level stars/scores, then reload the Game scene.</summary>
    public void ResetProgressAndReloadScene()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        if (levelPrefabs != null)
            for (int i = 0; i < levelPrefabs.Length; i++)
            {
                PlayerPrefs.DeleteKey(PrefLevelStars + i);
                PlayerPrefs.DeleteKey(PrefLevelScore + i);
            }
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
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        _isTransitioning = true;

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

        // Luxodd: show leaderboard → trigger Continue popup.
        // Standalone: just respawn immediately (no credits system).
        if (luxoddBridge != null)
        {
            bool waitingForChoice = true;
            bool playerContinued = false;

            luxoddBridge.OnPuckDeathWithLeaderboard(
                onContinue: () =>
                {
                    playerContinued = true;
                    waitingForChoice = false;
                },
                onEnd: () =>
                {
                    // Session ending — bridge handles BackToSystem
                    waitingForChoice = false;
                    _isTransitioning = false;
                });

            while (waitingForChoice) yield return null;

            if (!playerContinued)
            {
                // Player chose End — session is being closed by Luxodd
                yield break;
            }
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

        _isTransitioning = false;
    }

    /// <summary>Gets the current level's Floor material base color, or null if none.</summary>
    Color? GetCurrentFloorColor()
    {
        if (_currentInstance == null) return null;
        var floor = _currentInstance.transform.Find("Floor");
        if (floor == null) return null;
        var mr = floor.GetComponent<Renderer>();
        if (mr == null || mr.material == null) return null;
        if (mr.material.HasProperty("_BaseColor")) return mr.material.GetColor("_BaseColor");
        if (mr.material.HasProperty("_Color"))     return mr.material.GetColor("_Color");
        return null;
    }

    /// <summary>
    /// Cross-fades the new level's Floor color from the previous level's
    /// color into the new level's "natural" color over <duration> seconds.
    /// Uses material instancing — doesn't mutate the shared asset.
    /// </summary>
    IEnumerator FadeFloorFromTo(Color fromColor, float duration)
    {
        if (_currentInstance == null) yield break;
        var floor = _currentInstance.transform.Find("Floor");
        if (floor == null) yield break;
        var mr = floor.GetComponent<Renderer>();
        if (mr == null) yield break;

        // Instance the material so we don't mutate the shared asset.
        var mat = mr.material;
        string prop = mat.HasProperty("_BaseColor") ? "_BaseColor"
                    : (mat.HasProperty("_Color") ? "_Color" : null);
        if (prop == null) yield break;

        Color toColor = mat.GetColor(prop);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            mat.SetColor(prop, Color.Lerp(fromColor, toColor, k));
            yield return null;
        }
        mat.SetColor(prop, toColor);
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
        if (rail.LightUp()) _litRailCount++;
    }

    /// <summary>
    /// 3 stars = finished under threeStarStrokes AND lit all rails.
    /// 2 stars = finished under 2×threeStarStrokes OR lit all rails.
    /// 1 star  = finished the level at all.
    /// </summary>
    int CalculateStars()
    {
        bool allLit = _totalRailsInLevel > 0 && _litRailCount >= _totalRailsInLevel;
        if (_shotCount <= threeStarStrokes && allLit) return 3;
        if (_shotCount <= threeStarStrokes || allLit)  return 2;
        return 1;
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
        if (puckController != null) puckController.StartPosition = startPos;
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
        if (levelLabel != null) levelLabel.text = $"LEVEL {_currentIndex + 1}";
        ShowBanner($"LEVEL {_currentIndex + 1}");
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
