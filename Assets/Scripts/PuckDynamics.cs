using UnityEngine;

/// <summary>
/// Runtime feel-layer on the puck:
///   • Scales the authored TrailRenderer width and idle-glow emission rate
///     to the puck's current speed.
///   • Forces the puck's visual rotation axis to match its velocity
///     direction so the SpinDecal rolls the correct way regardless of
///     rigidbody angular velocity.
///
/// Attach to the Puck GameObject. Assign refs in the Inspector (or let
/// BucaSetupHelper auto-wire them).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PuckDynamics : MonoBehaviour
{
    [Header("Refs")]
    public TrailRenderer trail;
    public ParticleSystem idleGlow;
    public MeshRenderer puckRenderer;

    [Header("Speed mapping")]
    [Tooltip("Speeds up to here map to 'slow' visuals.")]
    public float slowSpeed = 1.5f;
    [Tooltip("Speeds at or above here map to 'fast' visuals.")]
    public float fastSpeed = 14f;

    [Header("Trail width scaling")]
    public float trailWidthSlow = 0.08f;
    public float trailWidthFast = 0.24f;

    [Header("Idle glow emission rate scaling")]
    public float idleGlowRateSlow = 7f;
    public float idleGlowRateFast = 22f;

    [HideInInspector] public float emissionSlow = 1f;
    [HideInInspector] public float emissionFast = 1f;

    [Header("Rotation follows velocity")]
    [Tooltip("If true, overrides rigidbody rotation so the puck visually " +
             "rolls in the direction of travel (keeps SpinDecal readable).")]
    public bool alignRotationToVelocity = true;
    [Tooltip("Degrees per world-unit of travel — classic rolling sphere = 360/(2πr). " +
             "For a 0.6-unit sphere this is about 191.")]
    public float rollDegreesPerUnit = 191f;

    Rigidbody _rb;
    Quaternion _visualRot = Quaternion.identity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Rotation override needs the rigidbody not to fight us — freeze
        // its angular axis but keep physics velocity alone.
        if (alignRotationToVelocity)
        {
            _rb.freezeRotation = true;
            _visualRot = transform.rotation;
        }
    }

    void LateUpdate()
    {
        float speed = _rb.linearVelocity.magnitude;
        float k = Mathf.InverseLerp(slowSpeed, fastSpeed, speed);

        // --- Trail width ---
        if (trail != null)
        {
            float w = Mathf.Lerp(trailWidthSlow, trailWidthFast, k);
            trail.startWidth = w;
            trail.endWidth = 0f;
        }

        // --- Idle glow emission rate ---
        // Fetch the module FRESH each frame — Unity forbids caching a module
        // struct in a field (a stored copy throws "Do not create your own
        // module instances ...").
        if (idleGlow != null)
        {
            var emission = idleGlow.emission;
            var rate = emission.rateOverTime;
            rate.constant = Mathf.Lerp(idleGlowRateSlow, idleGlowRateFast, k);
            emission.rateOverTime = rate;
        }

        // --- Rotation follows velocity ---
        if (alignRotationToVelocity)
        {
            // Rolling axis = up × velocityDir (right-hand rule on XZ plane)
            Vector3 v = _rb.linearVelocity; v.y = 0f;
            if (v.sqrMagnitude > 0.001f)
            {
                Vector3 axis = Vector3.Cross(Vector3.up, v.normalized);
                float deltaDeg = v.magnitude * Time.deltaTime * rollDegreesPerUnit;
                _visualRot = Quaternion.AngleAxis(deltaDeg, axis) * _visualRot;
            }
            transform.rotation = _visualRot;
        }
    }
}
