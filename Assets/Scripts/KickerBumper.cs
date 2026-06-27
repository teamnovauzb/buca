using UnityEngine;

/// <summary>
/// NEW MECHANIC — Kicker Bumper (pinball pop-bumper).
///
/// A solid round post you can aim INTO on purpose: when the puck touches it, the
/// puck is flung straight away from the bumper's center, FASTER than it arrived.
/// Perfect for the speed-capped puck — it recovers the momentum a gentle launch
/// can't supply, so multi-bank routes finally arrive with energy.
///
/// This is a normal gameplay component on a prefab piece (exactly like RailLight
/// or BouncePad) — NOT a runtime auto-apply hook. The one-shot "Add New Mechanics"
/// editor tool stamps the bumper piece + this component into the chosen level
/// prefabs; after that the tool can be deleted and THIS script stays.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KickerBumper : MonoBehaviour
{
    [Tooltip("Lowest speed the puck leaves at, even from a gentle tap.")]
    public float kickSpeed = 10f;
    [Tooltip("Multiplies the puck's incoming speed; the bigger of (speed × this) and kickSpeed wins.")]
    public float restitutionGain = 1.15f;
    [Tooltip("Hard speed cap — keep equal to PuckController.maxLaunchSpeed (14) so the puck never tunnels.")]
    public float maxSpeed = 14f;

    const float Cooldown = 0.08f;   // stops one contact from firing many times
    float _lastHit = -999f;

    void OnCollisionEnter(Collision c)
    {
        var rb = c.rigidbody;
        if (rb == null || rb.GetComponent<PuckController>() == null) return;   // only the puck
        if (Time.time - _lastHit < Cooldown) return;
        _lastHit = Time.time;

        // Radial direction: straight out from the bumper's center toward the puck.
        Vector3 outDir = rb.position - transform.position;
        outDir.y = 0f;
        if (outDir.sqrMagnitude < 1e-4f) outDir = c.relativeVelocity;          // degenerate fallback
        outDir.y = 0f;
        if (outDir.sqrMagnitude < 1e-4f) return;
        outDir.Normalize();

        float speedIn = rb.linearVelocity.magnitude;
        float outSpeed = Mathf.Min(Mathf.Max(speedIn * restitutionGain, kickSpeed), maxSpeed);
        rb.linearVelocity = outDir * outSpeed;

        // The puck's own OnCollisionEnter already sparks, lights this rail, and
        // plays the wall-hit sound (via LevelManager.NotifyWallHit), so we don't
        // duplicate those — just add a little extra camera pop unique to the bumper.
        if (LevelManager.Instance != null) LevelManager.Instance.ShakeCamera(0.16f, 0.16f);
    }
}
