using UnityEngine;
using System;

#if LUXODD_INTEGRATION
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
using Luxodd.Game.Scripts.Game.Leaderboard;
using Newtonsoft.Json;
#endif

/// <summary>
/// Central Luxodd platform integration. Handles:
///   • WebSocket connection + health check
///   • Level begin/end tracking with scores
///   • In-game transactions (Continue on timer expiry, Restart on campaign end)
///   • Server-side user state (replaces PlayerPrefs for persistence)
///   • Leaderboard fetching
///
/// Wrap your project in the LUXODD_INTEGRATION scripting define symbol
/// (Project Settings → Player → Scripting Define Symbols) after importing
/// the Luxodd plugin. Without it, this script compiles but does nothing —
/// the game runs standalone with mouse + PlayerPrefs.
///
/// Place this on a persistent GameObject in the Game scene. LevelManager
/// calls the public On*() methods at each hook point.
/// </summary>
public class LuxoddGameBridge : MonoBehaviour
{
    public static LuxoddGameBridge Instance { get; private set; }

#if LUXODD_INTEGRATION
    [Header("Plugin references (drag from UnityPluginPrefab)")]
    [SerializeField] private WebSocketService _webSocketService;
    [SerializeField] private WebSocketCommandHandler _commandHandler;
    [SerializeField] private HealthStatusCheckService _healthCheckService;
#endif

    [Header("Settings")]
    [Tooltip("Total number of levels in the game (for user state array sizing).")]
    public int totalLevels = 5;

    [Header("Leaderboard panel (shown before Continue popup on death/time-up)")]
    public LeaderboardPanel leaderboardPanel;

    // Cached user state from server
    BucaUserState _serverState;
    bool _connected;
    bool _stateLoaded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Persist across scene loads — this bridge starts in the MainMenu
        // scene, stays connected through the Game scene, etc. The plugin
        // prefab (via LuxoddPersistor) also persists so references stay valid.
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        // When a new scene loads, auto-wire ourselves into whatever
        // LevelManager exists there (if any).
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // After a scene loads, auto-wire ourselves into any scene-local
        // objects that need a bridge reference. Both LevelManager and
        // LeaderboardPanel live in the Game scene, but this bridge lives
        // in MainMenu (via DontDestroyOnLoad) — so at edit time they
        // can't reference each other. This runtime wiring bridges them.
        var lm = FindFirstObjectByType<LevelManager>();
        if (lm != null && lm.luxoddBridge == null)
        {
            lm.luxoddBridge = this;
            Debug.Log($"[LuxoddBridge] Auto-wired to LevelManager in scene '{scene.name}'.");
        }

        // Find the LeaderboardPanel in the freshly-loaded scene (it's
        // typically inside the GameHUD canvas). FindFirstObjectByType
        // also finds inactive objects if we pass true, which is needed
        // since the panel may be hidden by default.
        // Scene-match guard: if FindFirstObjectByType returns a panel from a
        // DontDestroyOnLoad bucket or a stale additive scene, prefer the one
        // belonging to the just-loaded scene.
        var allPanels = FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        LeaderboardPanel best = null;
        foreach (var p in allPanels)
        {
            if (p == null) continue;
            if (p.gameObject.scene == scene) { best = p; break; }
            if (best == null) best = p; // fallback to any
        }
        if (best != null)
        {
            leaderboardPanel = best;
            Debug.Log($"[LuxoddBridge] Auto-wired LeaderboardPanel in scene '{scene.name}'.");
        }
    }

    void Start()
    {
#if LUXODD_INTEGRATION
        ConnectToServer();
#else
        Debug.Log("[LuxoddBridge] LUXODD_INTEGRATION not defined — running standalone.");
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // Connection
    // ═══════════════════════════════════════════════════════════
#if LUXODD_INTEGRATION
    void ConnectToServer()
    {
        _webSocketService.ConnectToServer(
            () =>
            {
                Debug.Log("[LuxoddBridge] Connected to server.");
                _connected = true;
                _healthCheckService.Activate();
                LoadUserState();
            },
            () =>
            {
                Debug.LogError("[LuxoddBridge] Connection failed.");
                _connected = false;
            });
    }
#endif

    // ═══════════════════════════════════════════════════════════
    // Level tracking — called by LevelManager
    // ═══════════════════════════════════════════════════════════

    /// <summary>Called by LevelManager.LoadLevel() after the level is set up.</summary>
    public void OnLevelBegin(int levelIndex)
    {
#if LUXODD_INTEGRATION
        if (!_connected) return;
        int levelNumber = levelIndex + 1; // Luxodd uses 1-based
        _commandHandler.SendLevelBeginRequestCommand(levelNumber,
            () => Debug.Log($"[LuxoddBridge] LevelBegin({levelNumber}) sent."),
            (code, msg) => Debug.LogWarning($"[LuxoddBridge] LevelBegin failed: {code} {msg}"));
#endif
    }

    /// <summary>Called by LevelManager.CompleteSequence() after score is calculated.</summary>
    public void OnLevelEnd(int levelIndex, int score)
    {
#if LUXODD_INTEGRATION
        Debug.Log($"[LuxoddBridge] OnLevelEnd invoked. levelIndex={levelIndex}, score={score}, connected={_connected}");
        if (!_connected)
        {
            Debug.LogWarning("[LuxoddBridge] Not connected — LevelEnd NOT sent. Check that the Luxodd plugin " +
                             "connected at startup (did you start from MainMenu scene?).");
            return;
        }
        int levelNumber = levelIndex + 1;
        Debug.Log($"[LuxoddBridge] → Sending level_end_request(level={levelNumber}, score={score})...");
        _commandHandler.SendLevelEndRequestCommand(levelNumber, score,
            () => Debug.Log($"[LuxoddBridge] ✓ LevelEnd({levelNumber}, score={score}) ACK from server."),
            (code, msg) => Debug.LogWarning($"[LuxoddBridge] ✗ LevelEnd failed: {code} {msg}"));
#else
        Debug.LogWarning("[LuxoddBridge] OnLevelEnd called but LUXODD_INTEGRATION not defined — no-op.");
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // In-game transactions
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Called by LevelManager.TimeUpSequence() when the timer expires
    /// OR by DeathSequence() on deadly wall death.
    /// Fetches the leaderboard, shows it, then triggers the Luxodd
    /// Continue popup. If Continue chosen → onContinue. If End → onEnd.
    /// </summary>
    public void OnTimeUp(Action onContinue, Action onEnd)
    {
#if LUXODD_INTEGRATION
        if (!_connected) { onEnd?.Invoke(); return; }

        // Step 1: fetch leaderboard, show panel, THEN trigger Continue popup
        FetchAndShowLeaderboard(() => TriggerContinuePopup(onContinue, onEnd));
#else
        // Standalone: just restart the level (no credits system)
        onContinue?.Invoke();
#endif
    }

    /// <summary>Alias — deadly wall death now uses the same flow as time-up.</summary>
    public void OnPuckDeathWithLeaderboard(Action onContinue, Action onEnd)
    {
        OnTimeUp(onContinue, onEnd);
    }

#if LUXODD_INTEGRATION
    void FetchAndShowLeaderboard(Action onFinished)
    {
        _commandHandler.SendLeaderboardRequestCommand(
            (response) =>
            {
                if (leaderboardPanel == null)
                {
                    Debug.LogWarning("[LuxoddBridge] No leaderboardPanel assigned — skipping display.");
                    onFinished?.Invoke();
                    return;
                }

                // Current user data from server (may have rank=0 if unranked)
                int myRank = response.CurrentUserData != null ? response.CurrentUserData.Rank : 0;
                int myScore = response.CurrentUserData != null ? response.CurrentUserData.TotalScore : 0;
                string myName = response.CurrentUserData != null ? response.CurrentUserData.PlayerName : "YOU";
                if (string.IsNullOrEmpty(myName)) myName = "YOU";

                // Build row list: leaderboard entries + current user appended if not already in list
                var list = new System.Collections.Generic.List<LeaderboardPanel.LeaderboardData>();
                if (response.Leaderboard != null)
                {
                    foreach (var src in response.Leaderboard)
                    {
                        list.Add(new LeaderboardPanel.LeaderboardData
                        {
                            rank = src.Rank,
                            playerName = src.PlayerName,
                            score = src.TotalScore
                        });
                    }
                }

                // If the current user isn't in the list, add them so they
                // always appear as a row (even unranked with 0 score).
                bool meInList = false;
                foreach (var e in list)
                    if (!string.IsNullOrEmpty(e.playerName) && e.playerName == myName) { meInList = true; break; }
                if (!meInList)
                {
                    list.Add(new LeaderboardPanel.LeaderboardData
                    {
                        rank = myRank > 0 ? myRank : list.Count + 1,
                        playerName = myName,
                        score = myScore
                    });
                }

                // Update myRank to the row we actually show (helps the
                // panel highlight correctly when original rank was 0).
                if (myRank <= 0)
                {
                    foreach (var e in list)
                        if (e.playerName == myName) { myRank = e.rank; break; }
                }

                leaderboardPanel.Show(list.ToArray(), myRank, myScore, myName, onFinished);
            },
            (code, msg) =>
            {
                Debug.LogWarning($"[LuxoddBridge] Leaderboard fetch failed ({code}): {msg}");
                onFinished?.Invoke();
            });
    }

    void TriggerContinuePopup(Action onContinue, Action onEnd)
    {
        Time.timeScale = 0f;
        bool callbackFired = false;
        _webSocketService.SendSessionOptionContinue((action) =>
        {
            callbackFired = true;
            Time.timeScale = 1f;
            switch (action)
            {
                case SessionOptionAction.Continue:
                    Debug.Log("[LuxoddBridge] Player chose Continue (paid credits).");
                    onContinue?.Invoke();
                    break;
                case SessionOptionAction.End:
                default:
                    Debug.Log("[LuxoddBridge] Player chose End.");
                    EndSessionAndReturn();
                    onEnd?.Invoke();
                    break;
            }
        });
        // Safety net — if Luxodd never fires the callback (network drop, popup
        // closed externally, etc.), restore Time.timeScale and treat as End so
        // the game doesn't freeze forever at timeScale=0.
        StartCoroutine(ContinuePopupTimeoutCo(() => callbackFired, onEnd));
    }

    System.Collections.IEnumerator ContinuePopupTimeoutCo(Func<bool> isFired, Action onEnd)
    {
        const float TimeoutSeconds = 30f;
        float t = 0f;
        while (t < TimeoutSeconds)
        {
            if (isFired()) yield break;
            t += UnityEngine.Time.unscaledDeltaTime;
            yield return null;
        }
        if (isFired()) yield break;
        Debug.LogWarning("[LuxoddBridge] Continue popup callback never fired after " +
                         TimeoutSeconds + "s — restoring Time.timeScale and invoking onEnd.");
        UnityEngine.Time.timeScale = 1f;
        onEnd?.Invoke();
    }
#endif

    /// <summary>
    /// Called when the entire campaign is finished. Shows the Restart popup.
    /// </summary>
    public void OnCampaignComplete(int totalStrokes, int totalStars, int finalScore)
    {
#if LUXODD_INTEGRATION
        if (!_connected) return;

        // Must send session results BEFORE showing Restart popup (per Luxodd docs).
        _commandHandler.SendLevelEndRequestCommand(totalLevels, finalScore,
            () =>
            {
                Debug.Log("[LuxoddBridge] Campaign results sent. Showing Restart popup.");
                _webSocketService.SendSessionOptionRestart((action) =>
                {
                    // Restart: system handles new session automatically.
                    // End: return to system.
                    if (action == SessionOptionAction.End)
                    {
                        _webSocketService.BackToSystem();
                    }
                });
            },
            (code, msg) => Debug.LogWarning($"[LuxoddBridge] Campaign results failed: {code} {msg}"));
#endif
    }

    /// <summary>Called on deadly wall death — free respawn, no popup.</summary>
    public void OnPuckDeath()
    {
        // No credits consumed on deadly wall death — just a free respawn.
        // This method exists as a hook in case you want analytics later.
#if LUXODD_INTEGRATION
        Debug.Log("[LuxoddBridge] Puck death (free respawn, no transaction).");
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // Session end
    // ═══════════════════════════════════════════════════════════
    void EndSessionAndReturn()
    {
#if LUXODD_INTEGRATION
        _commandHandler.SendLevelEndRequestCommand(0, 0,
            () => _webSocketService.BackToSystem(),
            (code, msg) =>
            {
                Debug.LogWarning($"[LuxoddBridge] EndSession failed: {code} {msg}");
                _webSocketService.BackToSystem();
            });
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // User state (server-side persistence, replaces PlayerPrefs)
    // ═══════════════════════════════════════════════════════════

    [Serializable]
    public class BucaUserState
    {
        public int currentLevel;
        public int[] bestStars;
        public int[] bestScores;
    }

    void LoadUserState()
    {
#if LUXODD_INTEGRATION
        _commandHandler.SendGetUserDataRequestCommand(
            (response) =>
            {
                if (response != null)
                {
                    try
                    {
                        _serverState = JsonConvert.DeserializeObject<BucaUserState>(response.ToString());
                        Debug.Log($"[LuxoddBridge] User state loaded: level={_serverState.currentLevel}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[LuxoddBridge] Failed to parse user state: {e.Message}");
                        _serverState = CreateDefaultState();
                    }
                }
                else
                {
                    Debug.Log("[LuxoddBridge] No user state on server — creating default.");
                    _serverState = CreateDefaultState();
                }
                _stateLoaded = true;
                ApplyServerStateToPlayerPrefs();
            },
            (code, msg) =>
            {
                Debug.LogWarning($"[LuxoddBridge] LoadUserState failed: {code} {msg}");
                _serverState = CreateDefaultState();
                _stateLoaded = true;
            });
#endif
    }

    BucaUserState CreateDefaultState()
    {
        return new BucaUserState
        {
            currentLevel = 0,
            bestStars = new int[totalLevels],
            bestScores = new int[totalLevels]
        };
    }

    /// <summary>
    /// Copies server state into PlayerPrefs so existing LevelManager code
    /// works unchanged. Called once after loading state from server.
    /// </summary>
    void ApplyServerStateToPlayerPrefs()
    {
        if (_serverState == null) return;
        PlayerPrefs.SetInt("BucaCurrentLevel", _serverState.currentLevel);
        for (int i = 0; i < totalLevels; i++)
        {
            if (_serverState.bestStars != null && i < _serverState.bestStars.Length)
                PlayerPrefs.SetInt(LevelManager.PrefLevelStars + i, _serverState.bestStars[i]);
            if (_serverState.bestScores != null && i < _serverState.bestScores.Length)
                PlayerPrefs.SetInt(LevelManager.PrefLevelScore + i, _serverState.bestScores[i]);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Pushes current PlayerPrefs to the server. Call after any level
    /// completion or significant progress change.
    /// </summary>
    public void SaveUserState()
    {
#if LUXODD_INTEGRATION
        if (!_connected || _serverState == null) return;

        _serverState.currentLevel = PlayerPrefs.GetInt("BucaCurrentLevel", 0);
        if (_serverState.bestStars == null) _serverState.bestStars = new int[totalLevels];
        if (_serverState.bestScores == null) _serverState.bestScores = new int[totalLevels];
        for (int i = 0; i < totalLevels; i++)
        {
            _serverState.bestStars[i] = PlayerPrefs.GetInt(LevelManager.PrefLevelStars + i, 0);
            _serverState.bestScores[i] = PlayerPrefs.GetInt(LevelManager.PrefLevelScore + i, 0);
        }

        string json = JsonConvert.SerializeObject(_serverState);
        _commandHandler.SendSetUserDataRequestCommand(json,
            () => Debug.Log("[LuxoddBridge] User state saved to server."),
            (code, msg) => Debug.LogWarning($"[LuxoddBridge] SaveUserState failed: {code} {msg}"));
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // Leaderboard
    // ═══════════════════════════════════════════════════════════

    /// <summary>Fetches the leaderboard. Results come via the callback.</summary>
    public void FetchLeaderboard(Action<string> onResult)
    {
#if LUXODD_INTEGRATION
        if (!_connected) { onResult?.Invoke("Not connected"); return; }
        _commandHandler.SendLeaderboardRequestCommand(
            (response) =>
            {
                string result = $"Your rank: #{response.CurrentUserData.Rank} " +
                                $"(Score: {response.CurrentUserData.TotalScore})\n" +
                                $"Total players: {response.Leaderboard.Count}";
                Debug.Log($"[LuxoddBridge] Leaderboard: {result}");
                onResult?.Invoke(result);
            },
            (code, msg) =>
            {
                Debug.LogWarning($"[LuxoddBridge] Leaderboard failed: {code} {msg}");
                onResult?.Invoke("Leaderboard unavailable");
            });
#else
        onResult?.Invoke("Leaderboard: standalone mode");
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // Arcade input detection (used by PuckController)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if arcade joystick/button input has been detected
    /// at any point during this session. Once true, stays true.
    /// Routes through ArcadeInputAdapter so Luxodd's deadzone + axis
    /// inversion settings apply consistently.
    /// </summary>
    public static bool IsArcadeInputActive => ArcadeInputAdapter.DetectAnyArcadeInput();
}
