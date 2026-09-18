using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom-of-screen arcade control hints. The former on-screen Restart button
/// is intentionally removed; the top-right space now belongs to the timer.
/// </summary>
public class ControlHintBar : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup group;
    public LevelManager levelManager;

    [Header("Auto-fade behavior")]
    [Tooltip("Fade out while the puck is moving, in during idle. Set to false to keep hints always visible.")]
    public bool autoFadeWhileMoving = true;
    [Tooltip("Hide the bar after this many shots in the CURRENT level (resets each level). 0 = never hide.")]
    public int hideAfterShotsTaken = 3;
    public float fadeSpeed = 2.5f;
    [Range(0f, 1f)] public float idleAlpha = 0.95f;
    [Range(0f, 1f)] public float movingAlpha = 0.0f;
    [Tooltip("Always force the bar visible for this many seconds after a new level loads, even if the puck is moving.")]
    public float forceVisibleAtLevelStart = 1.5f;
    [Tooltip("Speed threshold above which the puck counts as 'moving'. Higher = ignores spawn-jitter / micro-noise.")]
    public float movingSpeedThreshold = 1.0f;

    int _shotsThisLevel;
    bool _permanentlyHidden;
    bool _lastWasIdle = true;
    float _levelStartTime;
    bool _everSawIdle;

    void Awake()
    {
        RemoveLegacyRestartButton();
    }

    void OnEnable()
    {
        RemoveLegacyRestartButton();
        _levelStartTime = Time.unscaledTime;
        _shotsThisLevel = 0;
        _permanentlyHidden = false;
        _lastWasIdle = true;
        _everSawIdle = false;
        if (group != null) group.alpha = idleAlpha;
    }

    void OnDisable()
    {
        RemoveLegacyRestartButton();
    }

    /// <summary>Show the hints again for the first shots of each level.</summary>
    public void ResetForNewLevel()
    {
        _levelStartTime = Time.unscaledTime;
        _shotsThisLevel = 0;
        _permanentlyHidden = false;
        _lastWasIdle = true;
        _everSawIdle = false;
        if (group != null) group.alpha = idleAlpha;
    }

    void Update()
    {
        // Keep normal hint behavior; only the on-screen Restart button is gone.
        if (levelManager == null) levelManager = LevelManager.Instance;

        if (group == null) return;
        if (levelManager == null)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, idleAlpha, fadeSpeed * Time.deltaTime);
            return;
        }

        if (hideAfterShotsTaken > 0 && levelManager.puckRigidbody != null)
        {
            float speed = levelManager.puckRigidbody.linearVelocity.magnitude;
            bool moving = speed >= movingSpeedThreshold;
            if (!moving) _everSawIdle = true;
            if (_everSawIdle && moving && _lastWasIdle)
            {
                _shotsThisLevel++;
                if (_shotsThisLevel >= hideAfterShotsTaken)
                    _permanentlyHidden = true;
            }
            _lastWasIdle = !moving;
        }

        float target = idleAlpha;
        bool inGracePeriod = (Time.unscaledTime - _levelStartTime) < forceVisibleAtLevelStart;

        if (inGracePeriod)
            target = idleAlpha;
        else if (_permanentlyHidden)
            target = 0f;
        else if (autoFadeWhileMoving && levelManager.puckRigidbody != null)
        {
            bool moving = levelManager.puckRigidbody.linearVelocity.magnitude > movingSpeedThreshold;
            target = moving ? movingAlpha : idleAlpha;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.deltaTime);
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    void RemoveLegacyRestartButton()
    {
        if (transform.parent == null) return;
        Transform oldButton = transform.parent.Find("QuickRestartHint");
        if (oldButton == null) return;
        oldButton.gameObject.SetActive(false);
        Destroy(oldButton.gameObject);
    }
}
