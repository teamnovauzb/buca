using UnityEngine;

/// <summary>
/// Attached to pink deadly walls. On contact: triggers the death
/// sequence on LevelManager (explosion burst + red flash + strong
/// camera shake + respawn).
/// Guards re-entry so a brief contact doesn't fire twice.
/// </summary>
public class DeadlyTrigger : MonoBehaviour
{
    bool _consumed;

    void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;
        if (other.attachedRigidbody == null) return;
        if (LevelManager.Instance == null) return;

        _consumed = true;
        LevelManager.Instance.KillPuck();
        // Brief cooldown so the trigger can arm again after the respawn
        // teleport — otherwise touching the same deadly wall twice in one
        // level wouldn't kill you a second time.
        Invoke(nameof(Rearm), 0.8f);
    }

    void Rearm() { _consumed = false; }
}
