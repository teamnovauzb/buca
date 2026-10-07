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

    void OnEnable() { _consumed=false; }
    void OnDisable() { CancelInvoke(nameof(Rearm)); _consumed=false; }
    void OnTriggerEnter(Collider other) => TryKill(other);
    void OnTriggerStay(Collider other) => TryKill(other);

    void TryKill(Collider other)
    {
        if (_consumed) return;
        var rb = other.attachedRigidbody;
        if (rb == null || (rb.GetComponent<PuckController>() == null && rb.GetComponent<TutorialPracticePuck>() == null)) return;
        var practicePuck=rb.GetComponent<TutorialPracticePuck>();
        if(practicePuck!=null) { practicePuck.practice.ResetShot(); return; }
        if (LevelManager.Instance == null || LevelManager.Instance.IsRewindingShot) return;

        _consumed = true;
        LevelManager.Instance.KillPuck();
        // Brief cooldown so the trigger can arm again after the respawn
        // teleport — otherwise touching the same deadly wall twice in one
        // level wouldn't kill you a second time.
        Invoke(nameof(Rearm), 0.8f);
    }

    void Rearm() { _consumed = false; }
}
