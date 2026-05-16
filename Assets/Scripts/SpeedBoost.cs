using UnityEngine;

/// <summary>
/// Trigger ring that adds a one-shot velocity boost when the puck
/// passes through. Direction = puck's current velocity (so it speeds
/// up rather than redirecting), unless `lockToForward` is set.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpeedBoost : MonoBehaviour
{
    [Tooltip("Speed added to the puck on entry.")]
    public float boostAmount = 8f;
    [Tooltip("Cooldown so re-entries don't snowball the puck infinitely.")]
    public float cooldown = 0.4f;
    [Tooltip("If true, the boost is along transform.forward instead of " +
             "the puck's incoming velocity direction.")]
    public bool lockToForward = false;
    [Tooltip("Hard cap on resulting speed so the puck doesn't tunnel through walls.")]
    public float maxSpeedAfter = 24f;

    float _lastFireTime = -999f;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time - _lastFireTime < cooldown) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (rb.GetComponent<PuckController>() == null) return;
        _lastFireTime = Time.time;

        Vector3 dir = lockToForward
            ? transform.forward
            : (rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : transform.forward);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        Vector3 v = rb.linearVelocity + dir * boostAmount;
        if (v.magnitude > maxSpeedAfter) v = v.normalized * maxSpeedAfter;
        rb.linearVelocity = v;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.PlayWallSpark(transform.position, dir);
            LevelManager.Instance.ShakeCamera(0.15f, 0.18f);
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(AudioManager.Instance.speedBoostSfx);
    }
}
