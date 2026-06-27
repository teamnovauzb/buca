using UnityEngine;

/// <summary>
/// NEW MECHANIC — Banking Rail ("live" rail).
///
/// A solid rail that gives a clean bank shot WITHOUT bleeding speed: the puck
/// reflects off it and its speed is topped back up to roughly what it came in
/// with (× bankBoost). This makes deliberate one- and two-cushion bank shots into
/// the hole actually viable — reading the reflection line off it is the skill.
///
/// We keep the physics engine's own bounce DIRECTION (so the puck's bounciness
/// feels natural) and only restore the magnitude — that avoids the classic
/// "double bounce" bug from manually reflecting an already-reflected velocity.
///
/// Normal gameplay component on a prefab piece (like RailLight) — not a runtime
/// hook. Lives alongside a RailLight so the rail flashes when used.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BankingRail : MonoBehaviour
{
    [Tooltip("Exit speed = impact speed × this. 1.0 = perfectly elastic, >1 = a small boost.")]
    public float bankBoost = 1.05f;
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

        // Outward surface normal (from the contact point toward the puck center).
        Vector3 n = rb.position - c.GetContact(0).point;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-4f) n = -c.relativeVelocity;
        n.y = 0f;
        n.Normalize();

        float impactSpeed = c.relativeVelocity.magnitude;   // speed at the moment of contact
        Vector3 v = rb.linearVelocity;
        v.y = 0f;

        // If the engine hasn't bounced us yet (still heading into the rail), reflect;
        // if it already bounced (heading outward), keep that direction.
        Vector3 outDir = (Vector3.Dot(v, n) < 0f) ? Vector3.Reflect(v, n) : v;
        outDir.y = 0f;
        if (outDir.sqrMagnitude < 1e-4f) outDir = n;          // fall back to straight-out
        outDir.Normalize();

        float outSpeed = Mathf.Min(impactSpeed * bankBoost, maxSpeed);
        rb.linearVelocity = outDir * outSpeed;

        // No extra FX here — the puck's own OnCollisionEnter already sparks, lights
        // this rail, and plays the wall-hit sound for solid collisions like this one.
    }
}
