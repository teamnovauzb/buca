using UnityEngine;

/// <summary>
/// Trigger pad that bounces the puck back with extra impulse when it
/// passes through. Direction = pad's local forward (transform.forward).
/// Reads the puck's incoming velocity, kills it, and replaces it with
/// a strong shot in the pad direction so chained pads can route the
/// puck around the field.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BouncePad : MonoBehaviour
{
    [Tooltip("Speed (m/s) the puck leaves the pad at.")]
    public float launchSpeed = 14f;
    [Tooltip("Cooldown so a single overlap doesn't fire twice in adjacent FixedUpdates.")]
    public float cooldown = 0.15f;

    float _lastFireTime = -999f;

    void Awake()
    {
        // Force trigger so the puck passes through visually
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time - _lastFireTime < cooldown) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (rb.GetComponent<PuckController>() == null) return;

        Vector3 dir = transform.forward; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
        dir.Normalize();

        // Directional gate: only fire if the puck is heading INTO the pad
        // (i.e., moving against the launch direction). Without this guard, a
        // puck that flies through a pad from behind would be re-launched
        // forward, and two facing pads could ping-pong the puck forever.
        Vector3 incomingVel = rb.linearVelocity; incomingVel.y = 0f;
        if (incomingVel.sqrMagnitude > 0.04f)
        {
            // dot < 0 means puck velocity opposes the pad's forward = approaching front face
            if (Vector3.Dot(incomingVel.normalized, dir) > 0.5f)
                return; // puck is travelling WITH the pad direction → don't double-launch
        }

        _lastFireTime = Time.time;

        // Replace velocity entirely — pads override momentum so chains work
        rb.linearVelocity = dir * launchSpeed;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.PlayWallSpark(transform.position, dir);
            LevelManager.Instance.ShakeCamera(0.18f, 0.18f);
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(AudioManager.Instance.bouncePadSfx);
    }
}
