using UnityEngine;

/// <summary>
/// NEW MECHANIC — Rim Rebound (raised lip), from CROKINOLE's rim / a curling sheet edge.
///
/// A clean, PREDICTABLE bank: the puck reflects off the rim and KEEPS most of its
/// speed (≈95%), so banks read as "angle-in = angle-out" containment rather than the
/// energy LOSS of a plain wall (which bleeds ~25%) or the energy GAIN of a Banking
/// Rail. It's the gentle, reliable cousin used to keep shots on the table.
///
/// Normal gameplay component on a prefab piece (like BankingRail) — not a runtime
/// hook. Lives alongside a RailLight so the rim flashes when used; the puck's own
/// collision handler plays the spark + sound, so we don't duplicate FX here.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RimRebound : MonoBehaviour
{
    [Tooltip("Fraction of impact speed kept after the rebound. 0.95 = clean containment with a slight, natural loss.")]
    public float speedKeep = 0.95f;
    [Tooltip("Hard speed cap — keep equal to PuckController.maxLaunchSpeed (14).")]
    public float maxSpeed = 14f;

    const float Cooldown = 0.05f;
    float _lastHit = -999f;

    void OnCollisionEnter(Collision c)
    {
        var rb = c.rigidbody;
        if (rb == null || rb.GetComponent<PuckController>() == null) return;
        if (Time.time - _lastHit < Cooldown) return;
        _lastHit = Time.time;

        // Outward surface normal (contact point → puck center).
        Vector3 n = rb.position - c.GetContact(0).point;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-4f) n = -c.relativeVelocity;
        n.y = 0f;
        n.Normalize();

        float impactSpeed = c.relativeVelocity.magnitude;
        Vector3 v = rb.linearVelocity;
        v.y = 0f;

        // Keep the engine's bounce DIRECTION (reflect only if still heading in),
        // then set the magnitude — a predictable, readable rebound.
        Vector3 outDir = (Vector3.Dot(v, n) < 0f) ? Vector3.Reflect(v, n) : v;
        outDir.y = 0f;
        if (outDir.sqrMagnitude < 1e-4f) outDir = n;
        outDir.Normalize();

        float outSpeed = Mathf.Min(impactSpeed * speedKeep, maxSpeed);
        rb.linearVelocity = outDir * outSpeed;
    }
}
