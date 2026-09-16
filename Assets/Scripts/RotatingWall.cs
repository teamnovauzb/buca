using UnityEngine;

/// <summary>
/// A wall that rotates on its own Y axis like a windmill. Use a long
/// capsule attached to a kinematic rigidbody — the rotation sweeps the
/// puck out of the way like a turnstile.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RotatingWall : MonoBehaviour
{
    [Tooltip("Degrees per second around the local Y axis.")]
    public float speedDegPerSec = 60f;
    [Tooltip("Phase offset (degrees) so multiple windmills don't sync.")]
    public float phaseDeg = 0f;

    Rigidbody _rb;
    float _baseYaw;
    float _basePitch;
    float _baseRoll;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        Vector3 initialEuler = transform.eulerAngles;
        _basePitch = initialEuler.x;
        _baseYaw = initialEuler.y + phaseDeg;
        _baseRoll = initialEuler.z;
    }

    void FixedUpdate()
    {
        _rb.MoveRotation(GetPredictedRotation(Time.time));
    }

    /// <summary>
    /// Returns the wall rotation at an absolute game time without changing its
    /// Rigidbody. This keeps trajectory prediction deterministic and side-effect free.
    /// </summary>
    public Quaternion GetPredictedRotation(float absoluteTime)
    {
        float yaw = _baseYaw + absoluteTime * speedDegPerSec;
        return Quaternion.Euler(_basePitch, yaw, _baseRoll);
    }

    /// <summary>World-space velocity of a point carried by the rotating wall.</summary>
    public Vector3 GetPredictedPointVelocity(Vector3 worldPoint)
    {
        Vector3 angularVelocity = Vector3.up * (speedDegPerSec * Mathf.Deg2Rad);
        return Vector3.Cross(angularVelocity, worldPoint - transform.position);
    }
}
