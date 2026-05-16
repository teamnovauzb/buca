using UnityEngine;

/// <summary>
/// A point-based attractor. While the puck is within `range`, applies
/// a continuous pull toward the well's position with smooth falloff.
/// Use to bend trajectories around obstacles.
///
/// Trigger collider isn't required (uses transform position + range
/// distance check) so the well can be a small visual marker without
/// blocking the puck.
/// </summary>
public class GravityWell : MonoBehaviour
{
    [Tooltip("Radius within which the well exerts force.")]
    public float range = 2.5f;
    [Tooltip("Peak pull force (acceleration units) at distance 0.")]
    public float strength = 9f;
    [Tooltip("If true, repels instead of attracts (push the puck away).")]
    public bool repel = false;

    Rigidbody _puckRb;

    void FixedUpdate()
    {
        if (_puckRb == null)
        {
            if (LevelManager.Instance == null || LevelManager.Instance.Puck == null) return;
            _puckRb = LevelManager.Instance.Puck.GetComponent<Rigidbody>();
            if (_puckRb == null) return;
        }

        Vector3 to = transform.position - _puckRb.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist > range || dist < 0.05f)
        {
            // Out of range — stop the loop
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetGravityLoopActive(false);
            return;
        }

        // Smoothstep falloff: 1 at center → 0 at range
        float f = 1f - Mathf.Clamp01(dist / range);
        f = f * f * (3f - 2f * f);

        Vector3 dir = to / dist;
        if (repel) dir = -dir;
        _puckRb.AddForce(dir * strength * f, ForceMode.Acceleration);

        // Audio: low pulsing hum, volume = pull strength
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGravityLoopActive(true, f);
    }

    void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGravityLoopActive(false);
    }
}
