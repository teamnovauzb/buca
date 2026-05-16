using UnityEngine;

/// <summary>
/// Pair-based teleporter. Drop one Teleporter in the level and assign
/// `partner` to its mate; when the puck enters, it warps to the partner
/// with velocity preserved (rotated to the partner's forward direction).
///
/// A short post-warp cooldown on BOTH ends prevents instant ping-pong.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Teleporter : MonoBehaviour
{
    [Tooltip("The other teleporter in this pair. The puck warps from THIS to PARTNER.")]
    public Teleporter partner;
    [Tooltip("Seconds after warp during which both ends ignore re-entry.")]
    public float cooldown = 0.4f;
    [Tooltip("Multiplier applied to puck speed on warp (1 = preserve, >1 = boost).")]
    public float exitSpeedMultiplier = 1.0f;

    float _disabledUntil;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time < _disabledUntil) return;
        if (partner == null) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (rb.GetComponent<PuckController>() == null) return;

        Vector3 inVel = rb.linearVelocity;
        float speed = inVel.magnitude;
        // Direction relative to this gate's forward → re-emit relative to partner's forward
        Vector3 localDir = transform.InverseTransformDirection(inVel.normalized);
        Vector3 outDir = partner.transform.TransformDirection(localDir);
        outDir.y = 0f;
        if (outDir.sqrMagnitude < 0.001f) outDir = partner.transform.forward;
        outDir.Normalize();

        // Place puck just past the partner's center along its forward so it
        // doesn't immediately re-trigger the partner.
        Vector3 exitPos = partner.transform.position
                          + outDir * 0.6f
                          + Vector3.up * (other.transform.position.y - transform.position.y);

        // Set position via the Rigidbody only — `rb.position` is the
        // physics-authoritative location; SyncTransforms propagates it to
        // the GameObject's Transform on the same frame, keeping any child
        // colliders / visuals aligned. Setting both rb.position AND
        // other.transform.position separately can desync if `other` is a
        // child collider whose attachedRigidbody is on a different GO.
        rb.position = exitPos;
        Physics.SyncTransforms();
        rb.linearVelocity = outDir * speed * exitSpeedMultiplier;

        // Disable both ends briefly
        _disabledUntil = Time.time + cooldown;
        partner._disabledUntil = Time.time + cooldown;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.PlayWallSpark(transform.position, Vector3.up);
            LevelManager.Instance.PlayWallSpark(exitPos, Vector3.up);
            LevelManager.Instance.ShakeCamera(0.22f, 0.22f);
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(AudioManager.Instance.teleporterSfx);
    }
}
