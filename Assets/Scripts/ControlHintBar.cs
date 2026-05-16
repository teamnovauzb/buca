using UnityEngine;

/// <summary>
/// Bottom-of-screen hint bar that shows the player which arcade control
/// does what (joystick → AIM, Black button → LAUNCH, etc.).
///
/// Built by BucaSetupHelper.SpawnControlHintBar(). Each hint = a button
/// icon + a short label, laid out horizontally. Auto-fades out while the
/// puck is in flight, fades back in when the puck is idle. Hides
/// permanently after the player has taken `hideAfterShotsTaken` shots
/// in the CURRENT level — `ResetForNewLevel()` re-shows it whenever
/// LevelManager loads a new level so a hint reminder is always available
/// for the first few shots of every level.
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
    bool _everSawIdle; // don't count "shots" until puck has actually settled at least once

    void OnEnable()
    {
        // Reset per-level state every time the bar is re-enabled
        _levelStartTime = Time.unscaledTime;
        _shotsThisLevel = 0;
        _permanentlyHidden = false;
        _lastWasIdle = true;
        _everSawIdle = false;
        if (group != null) group.alpha = idleAlpha;
    }

    /// <summary>
    /// Called by LevelManager.LoadLevel after each level swap. Resets
    /// the per-level shot counter so the hint bar shows again for the
    /// first N shots of every new level.
    /// </summary>
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
        if (group == null) return;

        // Find LevelManager once it exists in the scene. Until it does,
        // hold the bar at idleAlpha so we never blank out the hints
        // while the scene is still bootstrapping.
        if (levelManager == null) levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, idleAlpha, fadeSpeed * Time.deltaTime);
            return;
        }

        // ── Shot detection (only counts after we've observed at least one
        //    truly-idle moment, so puck-spawn jitter doesn't fire shots).
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

        // ── Compute target alpha
        float target = idleAlpha;
        bool inGracePeriod = (Time.unscaledTime - _levelStartTime) < forceVisibleAtLevelStart;

        if (inGracePeriod)
        {
            // Always visible for a moment after a new level starts
            target = idleAlpha;
        }
        else if (_permanentlyHidden)
        {
            target = 0f;
        }
        else if (autoFadeWhileMoving && levelManager.puckRigidbody != null)
        {
            bool moving = levelManager.puckRigidbody.linearVelocity.magnitude > movingSpeedThreshold;
            target = moving ? movingAlpha : idleAlpha;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.deltaTime);
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
