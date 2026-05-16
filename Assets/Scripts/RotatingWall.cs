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

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _baseYaw = transform.eulerAngles.y + phaseDeg;
    }

    void FixedUpdate()
    {
        float yaw = _baseYaw + Time.time * speedDegPerSec;
        Quaternion q = Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z);
        _rb.MoveRotation(q);
    }
}
