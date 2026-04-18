using UnityEngine;

/// <summary>
/// Attached to a level's Hole. Fires LevelManager.CompleteLevel() once
/// when the puck enters. Guards against re-entry: the shrink-into-hole
/// animation moves the puck inside the trigger, which without this flag
/// would fire CompleteLevel again mid-animation.
/// </summary>
public class HoleTrigger : MonoBehaviour
{
    bool _triggered;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (other.attachedRigidbody == null) return;
        if (LevelManager.Instance == null) return;

        _triggered = true;
        LevelManager.Instance.CompleteLevel();
    }
}
