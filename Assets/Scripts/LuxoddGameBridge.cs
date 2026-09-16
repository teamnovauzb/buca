using UnityEngine;
using System;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

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
    const int CampaignLevelCount = 30;

#if LUXODD_INTEGRATION
    [Header("Plugin references (drag from UnityPluginPrefab)")]
    [SerializeField] private WebSocketService _webSocketService;
    [SerializeField] private WebSocketCommandHandler _commandHandler;
    [SerializeField] private HealthStatusCheckService _healthCheckService;
    [SerializeField] private SessionFlowController _sessionFlowController;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void NotifyParentGameReady();
#endif

    [Header("Settings")]
    [Tooltip("Total number of levels in the game (for user state array sizing). " +
             "Auto-raised at runtime from LevelManager.levelPrefabs.Length, so a stale " +
             "serialized value can't truncate progress sync again (QA bug: later levels " +
             "showed un-completed because this was smaller than the 30-level campaign).")]
    public int totalLevels = CampaignLevelCount;

    [Header("Leaderboard panel (shown before Continue popup on death/time-up)")]
    public LeaderboardPanel leaderboardPanel;

    // Cached user state from server
    BucaUserState _serverState;
    bool _connected;
    bool _stateLoaded;
#if LUXODD_INTEGRATION
    bool _sessionOptionPending;
#endif

    /// <summary>
    /// True when the Luxodd host transaction bridge exists. Continue/Restart
    /// popups are parent-page messages and do not depend on the gameplay score
    /// WebSocket being connected at that exact frame.
    /// </summary>
    public bool PlatformTransactionsAvailable
    {
        get
        {
#if LUXODD_INTEGRATION
            return _webSocketService != null;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// True only in the deployed WebGL cabinet build. If the socket drops in
    /// that environment we must not grant an unpaid local Continue fallback.
    /// </summary>
    public bool PlatformTransactionsExpected
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR && LUXODD_INTEGRATION
            return true;
#else
            return false;
#endif
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // The bridge connects from MainMenu, before the Game scene exists and
        // before it can inspect LevelManager.levelPrefabs. Keep the complete
        // campaign size here so an early server response never truncates levels
        // 16-30 to the old serialized 15-level value.
        totalLevels = Mathf.Max(totalLevels, CampaignLevelCount);

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

        // Keep totalLevels in sync with the real level count. The serialized
        // scene value can go stale when levels are added — and a too-small
        // totalLevels makes the state
        // sync below silently drop progress for the missing levels.
        if (lm != null && lm.levelPrefabs != null && lm.levelPrefabs.Length > totalLevels)
        {
            Debug.Log($"[LuxoddBridge] totalLevels raised {totalLevels} → {lm.levelPrefabs.Length} " +
                      "(from LevelManager.levelPrefabs).");
            totalLevels = lm.levelPrefabs.Length;
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
#if UNITY_WEBGL && !UNITY_EDITOR
        // Luxodd keeps its splash screen visible until the WebGL player says
        // it is ready. This also tells the host that it can dispatch the
        // luxodd:session payload used by plugin 1.0.11.
        try
        {
            NotifyParentGameReady();
            Debug.Log("[LuxoddBridge] gameReady sent to host.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[LuxoddBridge] Could not notify the host that the game is ready: " +
                             exception.Message);
        }
#endif

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
#if UNITY_EDITOR
        // No Luxodd cabinet/server exists in the Editor, so a live connection
        // just spams "Unable to connect to the remote server". Skip it here —
        // the game runs in standalone mode (PlayerPrefs). The real cabinet
        // build (not UNITY_EDITOR) still connects normally.
        Debug.Log("[LuxoddBridge] Editor: skipping live server connection (standalone mode).");
        return;
#else
        // Outside the cabinet the WebSocket service may be unassigned — run
        // disconnected instead of NullReferencing. Progress saves via PlayerPrefs.
        if (_webSocketService == null)
        {
            Debug.LogWarning("[LuxoddBridge] No WebSocketService assigned — running disconnected. " +
                             "Progress is saved locally via PlayerPrefs.");
            return;
        }

        void OnConnected()
        {
            if (_connected) return;

            Debug.Log("[LuxoddBridge] Connected to server.");
            _connected = true;
            if (_healthCheckService != null) _healthCheckService.Activate();
            LoadUserState();
        }

        void OnConnectionFailed()
        {
            Debug.LogError("[LuxoddBridge] Connection failed.");
            _connected = false;
        }

        // Plugin 1.0.11 sends the real token and WebSocket URL through a
        // luxodd:session event. Use its SessionFlowController so the game
        // waits for that payload instead of connecting too early with an
        // empty token from the temporary blob URL.
        if (_sessionFlowController == null)
        {
            _sessionFlowController = FindFirstObjectByType<SessionFlowController>(
                FindObjectsInactive.Include);
        }

        if (_sessionFlowController != null)
        {
            _sessionFlowController.ActivateProcess(OnConnected, OnConnectionFailed);
        }
        else
        {
            Debug.LogWarning("[LuxoddBridge] SessionFlowController not found; using legacy URL connection.");
            _webSocketService.ConnectToServer(OnConnected, OnConnectionFailed);
        }
#endif
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
        ShowLeaderboard(0, "TIME IS OVER",
            () => ShowPaidContinueOptions(onContinue, onEnd));
    }

    /// <summary>Alias — deadly wall death now uses the same flow as time-up.</summary>
    public void OnPuckDeathWithLeaderboard(Action onContinue, Action onEnd)
    {
        ShowLeaderboard(0, "YOU LOST",
            () => ShowPaidContinueOptions(onContinue, onEnd));
    }

    /// <summary>
    /// Shows real Luxodd rankings when connected. In Editor/standalone (or if
    /// the ranking request fails), it still shows the same panel with the
    /// current player's score so the loss flow remains visually testable.
    /// </summary>
    public void ShowLeaderboard(int fallbackScore, string outcomeMessage, Action onFinished)
    {
#if LUXODD_INTEGRATION
        if (_connected && _commandHandler != null)
        {
            FetchAndShowLeaderboard(fallbackScore, outcomeMessage, onFinished);
            return;
        }
#endif
        ShowLocalLeaderboard(fallbackScore, outcomeMessage, onFinished);
    }

    public void ShowLeaderboard(int fallbackScore, Action onFinished)
    {
        ShowLeaderboard(fallbackScore, "YOU LOST", onFinished);
    }

    /// <summary>
    /// Opens Luxodd's official paid Continue/End transaction without showing
    /// the leaderboard again. LevelManager calls this after any local
    /// onboarding retry has already been resolved.
    /// </summary>
    public void ShowPaidContinueOptions(Action onContinue, Action onEnd)
    {
#if LUXODD_INTEGRATION
        if (_webSocketService == null)
        {
            Debug.LogError("[LuxoddBridge] Continue popup cannot open: WebSocketService is missing.");
            EndCurrentSession();
            onEnd?.Invoke();
            return;
        }

        // Session-option popups are sent through the WebGL parent bridge, not
        // through the score/leaderboard WebSocket. A temporary socket drop must
        // never skip the popup and eject the paying player to the description
        // page before they have chosen Continue or End.
        if (!_connected)
            Debug.LogWarning("[LuxoddBridge] Score WebSocket is disconnected; " +
                             "opening the host Continue transaction anyway.");
        TriggerContinuePopup(onContinue, onEnd);
#else
        onEnd?.Invoke();
#endif
    }

    void ShowLocalLeaderboard(int score, string outcomeMessage, Action onFinished)
    {
        if (leaderboardPanel == null)
        {
            Debug.LogWarning("[LuxoddBridge] No LeaderboardPanel assigned — skipping display.");
            onFinished?.Invoke();
            return;
        }

        int safeScore = Mathf.Max(0, score);
        var entry = new[]
        {
            new LeaderboardPanel.LeaderboardData
            {
                rank = 1,
                playerName = "YOU",
                score = safeScore
            }
        };
        leaderboardPanel.Show(entry, 1, safeScore, "YOU", outcomeMessage, onFinished);
    }

#if LUXODD_INTEGRATION
    void FetchAndShowLeaderboard(int fallbackScore, string outcomeMessage, Action onFinished)
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

                // Display only real Luxodd players. The scene owns exactly ten
                // rows, so cap the response to Top 10 instead of inventing demo
                // names or letting an oversized response overflow the panel.
                var list = new System.Collections.Generic.List<LeaderboardPanel.LeaderboardData>();
                int rowCapacity = Mathf.Clamp(
                    leaderboardPanel.rows != null ? leaderboardPanel.rows.Length : 10,
                    1, 10);
                if (response.Leaderboard != null)
                {
                    foreach (var src in response.Leaderboard)
                    {
                        if (list.Count >= rowCapacity) break;
                        list.Add(new LeaderboardPanel.LeaderboardData
                        {
                            rank = src.Rank,
                            playerName = src.PlayerName,
                            score = src.TotalScore
                        });
                    }
                }

                // When fewer than ten people have played, include the current
                // real Luxodd user in an available row. If Top 10 is already
                // full, their rank/score appears in the separate YOUR RANK line.
                bool meInList = false;
                foreach (var e in list)
                    if (!string.IsNullOrEmpty(e.playerName)
                        && e.playerName.Equals(myName, StringComparison.OrdinalIgnoreCase))
                    {
                        meInList = true;
                        break;
                    }
                if (!meInList && list.Count < rowCapacity)
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

                leaderboardPanel.Show(list.ToArray(), myRank, myScore, myName,
                    outcomeMessage, onFinished);
            },
            (code, msg) =>
            {
                Debug.LogWarning($"[LuxoddBridge] Leaderboard fetch failed ({code}): {msg}");
                ShowLocalLeaderboard(fallbackScore, outcomeMessage, onFinished);
            });
    }

    void TriggerContinuePopup(Action onContinue, Action onEnd)
    {
        if (_sessionOptionPending)
        {
            Debug.LogWarning("[LuxoddBridge] Ignoring duplicate Continue transaction request.");
            return;
        }
        _sessionOptionPending = true;
        Time.timeScale = 0f;
        StartCoroutine(OpenContinuePopupAfterPrimaryRelease(onContinue, onEnd));
    }

    System.Collections.IEnumerator OpenContinuePopupAfterPrimaryRelease(Action onContinue, Action onEnd)
    {
        // Black is both Buca's primary gameplay button and the Luxodd popup's
        // confirm button. Do not open the paid Continue popup while a shot/death
        // press is still being held or rapidly mashed.
        const float MinimumPostDeathDelay = 1f;
        const float RequiredNeutralTime = 0.25f;

        float delay = 0f;
        while (delay < MinimumPostDeathDelay)
        {
            delay += Time.unscaledDeltaTime;
            yield return null;
        }

        float neutralTime = 0f;
        while (neutralTime < RequiredNeutralTime)
        {
            bool primaryHeld = ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
            neutralTime = primaryHeld ? 0f : neutralTime + Time.unscaledDeltaTime;
            yield return null;
        }

        try
        {
            _webSocketService.SendSessionOptionContinue((action) =>
            {
                if (!_sessionOptionPending) return;
                _sessionOptionPending = false;
                Time.timeScale = 1f;
                switch (action)
                {
                    case SessionOptionAction.Continue:
                        Debug.Log("[LuxoddBridge] Player chose Continue (paid credits).");
                        onContinue?.Invoke();
                        break;
                    case SessionOptionAction.End:
                    case SessionOptionAction.Cancel:
                    default:
                        Debug.Log("[LuxoddBridge] Player chose End/Cancel.");
                        EndCurrentSession();
                        onEnd?.Invoke();
                        break;
                }
            });
        }
        catch (Exception exception)
        {
            _sessionOptionPending = false;
            Time.timeScale = 1f;
            Debug.LogError("[LuxoddBridge] Failed to open Continue transaction: " +
                           exception.Message);
            EndCurrentSession();
            onEnd?.Invoke();
        }

        // Do not add a local timeout here. Luxodd owns balance checks and its
        // top-up flow may legitimately stay open longer than 30 seconds. The
        // game remains paused until the host returns Continue or End/Cancel.
    }
#endif

    /// <summary>
    /// Called when the entire campaign is finished. Shows the Restart popup.
    /// </summary>
    public void OnCampaignComplete(int totalStrokes, int totalStars, int finalScore,
        Action onRestart = null, Action onEnd = null)
    {
#if LUXODD_INTEGRATION
        if (!PlatformTransactionsAvailable || _commandHandler == null)
        {
            Debug.LogError("[LuxoddBridge] Restart popup cannot open: required plugin service is missing.");
            EndCurrentSession();
            onEnd?.Invoke();
            return;
        }

        // Must send session results BEFORE showing Restart popup (per Luxodd docs).
        _commandHandler.SendLevelEndRequestCommand(totalLevels, finalScore,
            () =>
            {
                Debug.Log("[LuxoddBridge] Campaign results sent. Showing Restart popup.");
                TriggerRestartPopup(onEnd);
            },
            (code, msg) =>
            {
                // A score-report failure must not suppress the transaction UI.
                Debug.LogWarning($"[LuxoddBridge] Campaign results failed: {code} {msg}. " +
                                 "Opening Restart options anyway.");
                TriggerRestartPopup(onEnd);
            });
#else
        onRestart?.Invoke();
#endif
    }

#if LUXODD_INTEGRATION
    void TriggerRestartPopup(Action onEnd)
    {
        if (_sessionOptionPending)
        {
            Debug.LogWarning("[LuxoddBridge] Ignoring duplicate Restart transaction request.");
            return;
        }
        _sessionOptionPending = true;
        Time.timeScale = 0f;
        StartCoroutine(OpenRestartPopupAfterPrimaryRelease(onEnd));
    }

    System.Collections.IEnumerator OpenRestartPopupAfterPrimaryRelease(Action onEnd)
    {
        // Apply the same anti-mash protection as Continue: the gameplay Black
        // press must be released before it can confirm a paid platform option.
        const float MinimumDelay = 1f;
        const float RequiredNeutralTime = 0.25f;

        float delay = 0f;
        while (delay < MinimumDelay)
        {
            delay += Time.unscaledDeltaTime;
            yield return null;
        }

        float neutralTime = 0f;
        while (neutralTime < RequiredNeutralTime)
        {
            bool primaryHeld = ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
            neutralTime = primaryHeld ? 0f : neutralTime + Time.unscaledDeltaTime;
            yield return null;
        }

        try
        {
            _webSocketService.SendSessionOptionRestart((action) =>
            {
                // Luxodd creates the new session itself after Restart and does
                // not normally send a success callback. Only End/Cancel is
                // handled by the game. Some plugin versions may still echo
                // Restart; never reload the scene manually in that case.
                if (action == SessionOptionAction.Restart)
                {
                    Debug.Log("[LuxoddBridge] Restart accepted; waiting for Luxodd to create the new session.");
                    return;
                }

                if (!_sessionOptionPending) return;
                _sessionOptionPending = false;
                Time.timeScale = 1f;
                Debug.Log("[LuxoddBridge] Player chose End/Cancel after campaign completion.");
                EndCurrentSession();
                onEnd?.Invoke();
            });
        }
        catch (Exception exception)
        {
            _sessionOptionPending = false;
            Time.timeScale = 1f;
            Debug.LogError("[LuxoddBridge] Failed to open Restart transaction: " +
                           exception.Message);
            EndCurrentSession();
            onEnd?.Invoke();
        }

        // Restart success intentionally has no local callback or timeout. The
        // platform finalizes the old session and starts a new one itself.
    }
#endif

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
    /// <summary>
    /// Finalizes the current run and returns control to the Luxodd host. This
    /// remains safe when the gameplay WebSocket dropped: BackToSystem uses the
    /// host bridge directly, so a timeout can never leave gameplay running.
    /// </summary>
    public void EndCurrentSession(int zeroBasedLevelIndex = -1, int finalScore = 0)
    {
        Time.timeScale = 1f;
#if LUXODD_INTEGRATION
        if (_webSocketService == null)
        {
            Debug.LogError("[LuxoddBridge] WebSocketService is missing; using the local End Game fallback.");
            QuitStandaloneOrReturnToMenu();
            return;
        }

        int levelNumber = zeroBasedLevelIndex >= 0 ? zeroBasedLevelIndex + 1 : 0;
        int safeScore = Mathf.Max(0, finalScore);

        if (_connected && _commandHandler != null)
        {
            _commandHandler.SendLevelEndRequestCommand(levelNumber, safeScore,
                () => _webSocketService.BackToSystem(),
                (code, msg) =>
                {
                    Debug.LogWarning($"[LuxoddBridge] EndSession failed: {code} {msg}");
                    _webSocketService.BackToSystem();
                });
        }
        else
        {
            Debug.LogWarning("[LuxoddBridge] Ending without level_end ACK because the socket is disconnected.");
            _webSocketService.BackToSystem();
        }
#else
        QuitStandaloneOrReturnToMenu();
#endif
    }

    static void QuitStandaloneOrReturnToMenu()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR || UNITY_WEBGL
        // The Editor cannot quit itself and a browser cannot close its own tab.
        // Returning to MainMenu still exits the active game session cleanly.
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Called by MainMenuController.QuitGame to actually leave the game
    /// and return to the arcade cabinet's game-list screen. In WebGL,
    /// Application.Quit() is a no-op (browsers can't close themselves),
    /// so without this the cyan-flash-then-stuck bug occurs.
    /// Returns true if a Luxodd BackToSystem was issued; false if there's
    /// no connection (caller should fall back to Application.Quit which
    /// only works on standalone player anyway).
    /// </summary>
    public bool QuitToArcadeMenu()
    {
#if LUXODD_INTEGRATION
        if (_connected && _webSocketService != null)
        {
            Debug.Log("[LuxoddBridge] QuitToArcadeMenu → BackToSystem.");
            _webSocketService.BackToSystem();
            return true;
        }
        Debug.LogWarning("[LuxoddBridge] QuitToArcadeMenu called but Luxodd not connected — " +
                         "caller will fall back to Application.Quit (no-op in WebGL).");
        return false;
#else
        return false;
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // User state (server-side persistence, replaces PlayerPrefs)
    // ═══════════════════════════════════════════════════════════

    [Serializable]
    public class BucaUserState
    {
        public int currentLevel;
        public int highestUnlocked;
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
            highestUnlocked = 0,
            bestStars = new int[totalLevels],
            bestScores = new int[totalLevels]
        };
    }

    /// <summary>
    /// MERGES server state into PlayerPrefs so existing LevelManager code
    /// works unchanged. Called once after loading state from server.
    ///
    /// MERGE — never overwrite. The old version copied server values over
    /// local PlayerPrefs verbatim, so a default/stale server state (all
    /// zeros, or arrays sized for an older 5-level build) WIPED the local
    /// stars/scores for those level indices. That was QA's "I completed
    /// level 8 but the initial levels show un-completed" bug. Best-stars,
    /// best-scores, and current-level are all monotonic in this game, so
    /// max(server, local) is always the correct reconciliation.
    /// </summary>
    void ApplyServerStateToPlayerPrefs()
    {
        if (_serverState == null) return;

        int localCurrent = PlayerPrefs.GetInt("BucaCurrentLevel", 0);
        PlayerPrefs.SetInt("BucaCurrentLevel", Mathf.Max(_serverState.currentLevel, localCurrent));

        // highestUnlocked was added after the first server-state format shipped.
        // For old JSON (field missing => 0), reconstruct it from completed levels
        // and currentLevel once, then persist the explicit value on the next save.
        int serverHighest = Mathf.Clamp(_serverState.highestUnlocked, 0, Mathf.Max(0, totalLevels - 1));
        serverHighest = Mathf.Max(serverHighest,
            Mathf.Clamp(_serverState.currentLevel, 0, Mathf.Max(0, totalLevels - 1)));

        for (int i = 0; i < totalLevels; i++)
        {
            int serverStars = 0;
            int serverScore = 0;
            if (_serverState.bestStars != null && i < _serverState.bestStars.Length)
            {
                serverStars = _serverState.bestStars[i];
                int local = PlayerPrefs.GetInt(LevelManager.PrefLevelStars + i, 0);
                PlayerPrefs.SetInt(LevelManager.PrefLevelStars + i,
                                   Mathf.Max(serverStars, local));
            }
            if (_serverState.bestScores != null && i < _serverState.bestScores.Length)
            {
                serverScore = _serverState.bestScores[i];
                int local = PlayerPrefs.GetInt(LevelManager.PrefLevelScore + i, 0);
                PlayerPrefs.SetInt(LevelManager.PrefLevelScore + i,
                                   Mathf.Max(serverScore, local));
            }

            if ((serverStars > 0 || serverScore > 0) && i + 1 < totalLevels)
                serverHighest = Mathf.Max(serverHighest, i + 1);
        }

        int localHighest = PlayerPrefs.GetInt(LevelSelectController.HighestUnlockedKey, 0);
        PlayerPrefs.SetInt(LevelSelectController.HighestUnlockedKey,
            Mathf.Max(localHighest, serverHighest));
        PlayerPrefs.Save();

        // A response can arrive while the picker is already open. Refresh it
        // immediately instead of requiring the player to close/reopen the panel.
        var picker = FindFirstObjectByType<LevelSelectController>(FindObjectsInactive.Include);
        if (picker != null) picker.RefreshProgressFromStorage();
    }

    /// <summary>
    /// Intentional progress wipe. Because ApplyServerStateToPlayerPrefs
    /// MERGES with max(server, local), simply clearing PlayerPrefs isn't
    /// enough — the next load/session would restore old progress from the
    /// server. This zeroes the cached state AND pushes it to the server so
    /// the wipe sticks. Called by LevelManager.ResetProgressAndReloadScene.
    /// </summary>
    public void ResetServerProgress()
    {
#if LUXODD_INTEGRATION
        _serverState = CreateDefaultState();
        if (!_connected) return;
        string json = JsonConvert.SerializeObject(_serverState);
        _commandHandler.SendSetUserDataRequestCommand(json,
            () => Debug.Log("[LuxoddBridge] Server progress reset (zeros pushed)."),
            (code, msg) => Debug.LogWarning($"[LuxoddBridge] ResetServerProgress failed: {code} {msg}"));
#endif
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
        _serverState.highestUnlocked = PlayerPrefs.GetInt(
            LevelSelectController.HighestUnlockedKey, 0);
        // Resize (not just null-check) — server states written by an older
        // 5-level build come back with 5-length arrays; indexing [5..14]
        // below would throw IndexOutOfRange and the save would never happen.
        if (_serverState.bestStars == null || _serverState.bestStars.Length < totalLevels)
            System.Array.Resize(ref _serverState.bestStars, totalLevels);
        if (_serverState.bestScores == null || _serverState.bestScores.Length < totalLevels)
            System.Array.Resize(ref _serverState.bestScores, totalLevels);
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
