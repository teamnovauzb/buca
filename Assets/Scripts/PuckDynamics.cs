using UnityEngine;

/// <summary>
/// Runtime feel-layer on the puck:
///   • Scales TrailRenderer width, idle-glow emission rate, and puck
///     material emission intensity to the puck's current speed.
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
    public float trailWidthSlow = 0.18f;
    public float trailWidthFast = 0.75f;

    [Header("Trail color scaling")]
    [Tooltip("Trail color at slow speed — warm mellow yellow.")]
    public Color trailColorSlow = new Color(1f, 0.88f, 0.3f, 1f);
    [Tooltip("Trail color at fast speed — hot white-cyan for 'danger'.")]
    public Color trailColorFast = new Color(1.4f, 1.3f, 1.2f, 1f);
    [Tooltip("Apply colors via TrailRenderer.colorGradient (keeps fade-to-zero alpha).")]
    public bool driveTrailColor = true;

    [Header("Idle glow emission rate scaling")]
    public float idleGlowRateSlow = 14f;
    public float idleGlowRateFast = 46f;

    [Header("Puck emission HDR scaling")]
    [Tooltip("Multiplier applied to the puck material's emission color " +
             "at slowSpeed. Values >1 brighten.")]
    public float emissionSlow = 1.0f;
    public float emissionFast = 1.55f;

    [Header("Rotation follows velocity")]
    [Tooltip("If true, overrides rigidbody rotation so the puck visually " +
             "rolls in the direction of travel (keeps SpinDecal readable).")]
    public bool alignRotationToVelocity = true;
    [Tooltip("Degrees per world-unit of travel — classic rolling sphere = 360/(2πr). " +
             "For a 0.6-unit sphere this is about 191.")]
    public float rollDegreesPerUnit = 191f;

    Rigidbody _rb;
    Color _emissionBase;
    ParticleSystem.EmissionModule _emissionModule;
    bool _hasEmissionBase;
    Quaternion _visualRot = Quaternion.identity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (idleGlow != null) _emissionModule = idleGlow.emission;

        // Instance the puck material so emission tweaks don't leak.
        if (puckRenderer != null && puckRenderer.sharedMaterial != null)
        {
            var mat = puckRenderer.material; // forces instance
            // Use base color as "emission base" when no _EmissionColor exists
            // (OpaqueUnlitMat puck does its glow via base color, not emission).
            if (mat.HasProperty("_EmissionColor"))
            {
                _emissionBase = mat.GetColor("_EmissionColor");
                _hasEmissionBase = _emissionBase.maxColorComponent > 0.01f;
            }
            if (!_hasEmissionBase && mat.HasProperty("_BaseColor"))
            {
                _emissionBase = mat.GetColor("_BaseColor");
                _hasEmissionBase = true;
            }
        }

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

            // Trail color lerp — gentle yellow at slow → hot white at fast.
            if (driveTrailColor)
            {
                Color c = Color.Lerp(trailColorSlow, trailColorFast, k);
                var grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = grad;
            }
        }

        // --- Idle glow emission rate ---
        if (idleGlow != null)
        {
            var rate = _emissionModule.rateOverTime;
            rate.constant = Mathf.Lerp(idleGlowRateSlow, idleGlowRateFast, k);
            _emissionModule.rateOverTime = rate;
        }

        // --- Puck material emission boost ---
        if (_hasEmissionBase && puckRenderer != null && puckRenderer.material != null)
        {
            float mul = Mathf.Lerp(emissionSlow, emissionFast, k);
            var mat = puckRenderer.material;
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", _emissionBase * mul);
            else if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", _emissionBase * mul);
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
