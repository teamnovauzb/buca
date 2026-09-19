using System;
using System.Collections;
using UnityEngine;

#if LUXODD_INTEGRATION
using Luxodd.Game.Scripts.Network;
using Luxodd.Game.Scripts.Network.CommandHandler;
#endif

/// <summary>
/// Game-owned coordinator for Luxodd Continue and Restart transactions.
/// The component is authored into the persistent MainMenu Luxodd root by the
/// editor baker. It never creates UI or scene objects at runtime.
/// </summary>
public sealed class BucaInGameTransactionController : MonoBehaviour
{
    public static BucaInGameTransactionController Instance { get; private set; }

    [Header("Read-only Luxodd service references")]
    [SerializeField] MonoBehaviour webSocketService;
    [SerializeField] MonoBehaviour commandHandler;

    bool _transactionPending;

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

    public bool IsReady
    {
        get
        {
#if LUXODD_INTEGRATION
            return webSocketService is WebSocketService
                && commandHandler is WebSocketCommandHandler;
#else
            return false;
#endif
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Editor-baked wiring; never called by player code.</summary>
    public void Configure(MonoBehaviour socket, MonoBehaviour handler)
    {
        webSocketService = socket;
        commandHandler = handler;
    }

    /// <summary>
    /// Opens Luxodd Continue. The current session is deliberately not finalized.
    /// Continue resumes the same run; End submits the result and returns to Luxodd.
    /// </summary>
    public bool TryShowContinue(int oneBasedLevel, int score,
        Action onContinue, Action onEnd)
    {
#if LUXODD_INTEGRATION
        if (!TryBeginTransaction("Continue")) return false;
        OpenContinueTransaction(
            Mathf.Max(1, oneBasedLevel), Mathf.Max(0, score), onContinue, onEnd);
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// Finalizes the current session, then opens Luxodd Restart. Restart success
    /// intentionally has no game callback because Luxodd creates the new session.
    /// </summary>
    public bool TryShowRestart(int oneBasedLevel, int score, Action onEnd)
    {
#if LUXODD_INTEGRATION
        if (!TryBeginTransaction("Restart")) return false;
        StartCoroutine(RestartRoutine(
            Mathf.Max(1, oneBasedLevel), Mathf.Max(0, score), onEnd));
        return true;
#else
        return false;
#endif
    }

#if LUXODD_INTEGRATION
    bool TryBeginTransaction(string transactionName)
    {
        if (_transactionPending)
        {
            Debug.LogWarning($"[BucaTransactions] Ignoring duplicate {transactionName} request.");
            return false;
        }

        if (!(webSocketService is WebSocketService)
            || !(commandHandler is WebSocketCommandHandler))
        {
            Debug.LogError($"[BucaTransactions] Cannot open {transactionName}: " +
                           "the prebuilt Luxodd service references are missing.");
            return false;
        }

        _transactionPending = true;
        Time.timeScale = 0f;
        return true;
    }

    void OpenContinueTransaction(int oneBasedLevel, int score,
        Action onContinue, Action onEnd)
    {
        WebSocketService socket = (WebSocketService)webSocketService;
        try
        {
            socket.SendSessionOptionContinue(action =>
            {
                if (!_transactionPending) return;

                if (action == SessionOptionAction.Continue)
                {
                    _transactionPending = false;
                    Time.timeScale = 1f;
                    Debug.Log("[BucaTransactions] Continue accepted; resuming the same session.");
                    onContinue?.Invoke();
                    return;
                }

                Debug.Log("[BucaTransactions] Continue declined; finalizing the session.");
                _transactionPending = false;
                FinalizeAndReturn(oneBasedLevel, score, onEnd);
            });
        }
        catch (Exception exception)
        {
            Debug.LogError("[BucaTransactions] Continue popup failed: " + exception.Message);
            _transactionPending = false;
            FinalizeAndReturn(oneBasedLevel, score, onEnd);
        }
    }

    IEnumerator RestartRoutine(int oneBasedLevel, int score, Action onEnd)
    {
        yield return WaitForPrimaryRelease();

        WebSocketCommandHandler handler = (WebSocketCommandHandler)commandHandler;
        handler.SendLevelEndRequestCommand(oneBasedLevel, score,
            OpenRestartPopup,
            (code, message) =>
            {
                Debug.LogWarning($"[BucaTransactions] Session result failed ({code}): {message}. " +
                                 "Opening Restart anyway so the player is not ejected.");
                OpenRestartPopup();
            });

        void OpenRestartPopup()
        {
            if (!_transactionPending) return;
            try
            {
                ((WebSocketService)webSocketService).SendSessionOptionRestart(action =>
                {
                    // Luxodd normally gives no callback for successful Restart.
                    // If a plugin version echoes it, remain paused and let the
                    // host create the new session; never reload locally.
                    if (action == SessionOptionAction.Restart)
                    {
                        Debug.Log("[BucaTransactions] Restart accepted; waiting for Luxodd's new session.");
                        return;
                    }

                    if (!_transactionPending) return;
                    _transactionPending = false;
                    Time.timeScale = 1f;
                    Debug.Log("[BucaTransactions] Restart declined; returning to Luxodd.");
                    ((WebSocketService)webSocketService).BackToSystem();
                    onEnd?.Invoke();
                });
            }
            catch (Exception exception)
            {
                Debug.LogError("[BucaTransactions] Restart popup failed: " + exception.Message);
                _transactionPending = false;
                Time.timeScale = 1f;
                ((WebSocketService)webSocketService).BackToSystem();
                onEnd?.Invoke();
            }
        }
    }

    void FinalizeAndReturn(int oneBasedLevel, int score, Action onEnd)
    {
        WebSocketCommandHandler handler = (WebSocketCommandHandler)commandHandler;
        WebSocketService socket = (WebSocketService)webSocketService;
        handler.SendLevelEndRequestCommand(oneBasedLevel, score,
            ReturnToSystem,
            (code, message) =>
            {
                Debug.LogWarning($"[BucaTransactions] Session result failed ({code}): {message}.");
                ReturnToSystem();
            });

        void ReturnToSystem()
        {
            Time.timeScale = 1f;
            socket.BackToSystem();
            onEnd?.Invoke();
        }
    }

    static IEnumerator WaitForPrimaryRelease()
    {
        // Black is both BUCA fire and the Luxodd confirm button. Delay the
        // transaction until the failure input has been fully released.
        const float minimumDelay = 0.65f;
        const float neutralDuration = 0.20f;

        float delay = 0f;
        while (delay < minimumDelay)
        {
            delay += Time.unscaledDeltaTime;
            yield return null;
        }

        float neutral = 0f;
        while (neutral < neutralDuration)
        {
            bool held = ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
            neutral = held ? 0f : neutral + Time.unscaledDeltaTime;
            yield return null;
        }
    }
#endif
}
