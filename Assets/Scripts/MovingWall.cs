using UnityEngine;

/// <summary>
/// A wall that slides back and forth along an axis. Creates a
/// timing challenge — the puck has to be launched when the gap
/// is in the right place.
///
/// Attached to moving walls in level prefabs (e.g. Level_07, Level_15).
/// The physics engine is NOT notified of the movement as forces;
/// this is purely kinematic translation. For proper puck collision,
/// the wall's Rigidbody must be kinematic + Interpolate so the puck
/// detects the motion correctly.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingWall : MonoBehaviour
{
    [Tooltip("Direction of travel in LOCAL space (normalized automatically).")]
    public Vector3 axis = Vector3.right;
    [Tooltip("Total distance traveled (peak-to-peak). Wall oscillates ±(distance/2) from its start.")]
    public float distance = 2f;
    [Tooltip("Seconds for one full back-and-forth cycle.")]
    public float cycleSeconds = 2.5f;
    [Tooltip("Random phase offset so two walls in the same level don't sync perfectly.")]
    public float phase = 0f;

    Vector3 _basePos;
    Rigidbody _rb;

    void Awake()
    {
        _basePos = transform.position;
        _rb = GetComponent<Rigidbody>();
        // Kinematic + interpolate is the correct setup for moving platforms.
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.useGravity = false;
    }

    void FixedUpdate()
    {
        // MovePosition is the kinematic-safe way to translate a rigidbody
        // so the physics engine picks up the motion for puck collision.
        _rb.MovePosition(GetPredictedPosition(Time.time));
    }

    /// <summary>
    /// Returns the wall pose at an absolute game time without moving it. The
    /// trajectory preview uses the same equation as FixedUpdate, so it tests the
    /// wall where it will be when the puck arrives instead of at its current pose.
    /// </summary>
    public Vector3 GetPredictedPosition(float absoluteTime)
    {
        float cycle = Mathf.Max(0.01f, cycleSeconds);
        float t = (absoluteTime / cycle + phase) * Mathf.PI * 2f;
        float offset = Mathf.Sin(t) * (distance * 0.5f);
        Vector3 dir = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.right;
        return _basePos + dir * offset;
    }

    /// <summary>World-space linear velocity at an absolute game time.</summary>
    public Vector3 GetPredictedVelocity(float absoluteTime)
    {
        float cycle = Mathf.Max(0.01f, cycleSeconds);
        float t = (absoluteTime / cycle + phase) * Mathf.PI * 2f;
        float speed = Mathf.Cos(t) * (distance * 0.5f) * (Mathf.PI * 2f / cycle);
        Vector3 dir = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.right;
        return dir * speed;
    }
}
