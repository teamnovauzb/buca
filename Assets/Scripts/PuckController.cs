using UnityEngine;

/// <summary>
/// Buca!-style drag-back slingshot puck.
/// Mouse down anywhere → start aiming. Drag → set direction/power. Release → launch.
/// All scene objects (Rigidbody, Collider, AimLine) are pre-built by
/// RealBuca/Setup Game Scene — this script never creates anything at runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class PuckController : MonoBehaviour
{
    [Header("Slingshot")]
    [Tooltip("How much launch force per unit of drag. Higher = faster puck.")]
    public float forceMultiplier = 14f;
    [Tooltip("Maximum drag distance in world units — caps the max shot power.")]
    public float maxDragDistance = 3.5f;
    [Tooltip("Below this velocity the puck counts as stopped and can be aimed again.")]
    public float stopThreshold = 0.25f;
    [Tooltip("Hard cap on how fast the puck can fly after launch (prevents tunnelling).")]
    public float maxLaunchSpeed = 22f;

    [Header("Scene references (assigned by RealBuca/Setup Game Scene)")]
    public LineRenderer aimLine;

    Rigidbody _rb;
    Camera _cam;
    bool _isDragging;
    Vector3 _dragStartWorld;

    public Vector3 StartPosition { get; set; }

    // Cooldown so a single multi-contact collision only sparks once.
    float _lastSparkTime = -999f;
    const float SparkCooldown = 0.06f;

    [Header("Pre-hole magnetic assist")]
    [Tooltip("When the moving puck is within this distance of the hole, " +
             "apply a gentle pull toward it.")]
    public float magnetRange = 1.5f;
    [Tooltip("Peak pull force at range 0. Falls off to 0 at magnetRange.")]
    public float magnetForce = 8f;
    [Tooltip("Only pull if the puck's velocity direction is within this " +
             "cosine-angle of the hole direction. 1 = exact aim, 0 = 90°, " +
             "-1 = any direction.")]
    public float magnetAimDot = 0.3f;
    [Tooltip("Ignore pull below this speed (resting puck shouldn't teleport " +
             "into a nearby hole).")]
    public float magnetMinSpeed = 1.5f;

    [Header("Hit-stop")]
    [Tooltip("Velocity threshold above which a collision triggers a brief time-scale freeze.")]
    public float hitStopSpeedThreshold = 5f;
    [Tooltip("Time.timeScale applied during hit-stop (0.05 = near-freeze).")]
    public float hitStopScale = 0.05f;
    [Tooltip("Duration of hit-stop in UNSCALED seconds.")]
    public float hitStopDuration = 0.045f;

    [Header("Trajectory preview (optional)")]
    [Tooltip("Optional LineRenderer for the 2-bounce predicted path. Disable in scene if not wanted.")]
    public LineRenderer previewLine;
    [Tooltip("Which layers the preview ray is allowed to hit (set to the level-geometry layer).")]
    public LayerMask previewMask = ~0;
    public int previewBounces = 2;
    public float previewMaxDistance = 20f;
    public float previewSphereRadius = 0.25f;

    [Header("Drag power indicator (optional)")]
    [Tooltip("LineRenderer drawn as an arc around the puck that fills 0-360° by drag strength.")]
    public LineRenderer powerArc;
    [Tooltip("Radius of the arc around the puck.")]
    public float powerArcRadius = 0.6f;
    [Tooltip("Number of segments used to draw the arc (higher = smoother).")]
    public int powerArcSegments = 48;
    [Tooltip("Lift above puck so the arc doesn't z-fight with the floor.")]
    public float powerArcYOffset = 0.06f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (aimLine != null) aimLine.enabled = false;
        if (previewLine != null)
        {
            previewLine.enabled = false;
            previewLine.useWorldSpace = true;
            previewLine.positionCount = 0;
        }
        if (powerArc != null)
        {
            powerArc.enabled = false;
            powerArc.useWorldSpace = true;
            powerArc.loop = false;
        }
    }

    void FixedUpdate()
    {
        if (LevelManager.Instance == null) return;
        Vector3 speedVec = _rb.linearVelocity; speedVec.y = 0f;
        float speed = speedVec.magnitude;
        if (speed < magnetMinSpeed) return;

        Vector3 hole = LevelManager.Instance.GetCurrentHolePosition();
        if (hole == Vector3.positiveInfinity) return;

        Vector3 toHole = hole - transform.position; toHole.y = 0f;
        float dist = toHole.magnitude;
        if (dist > magnetRange || dist < 0.01f) return;

        // Only pull when heading roughly toward the hole.
        Vector3 velDir = speedVec / speed;
        Vector3 holeDir = toHole / dist;
        if (Vector3.Dot(velDir, holeDir) < magnetAimDot) return;

        // Falloff: 1 at distance 0 → 0 at magnetRange. Smoothstep for curve.
        float f = 1f - Mathf.Clamp01(dist / magnetRange);
        f = f * f * (3f - 2f * f);
        _rb.AddForce(holeDir * magnetForce * f, ForceMode.Acceleration);

        // Tell LevelManager we're actively being pulled in — drives the
        // hole's "swallow anticipation" pulse + enables NICE SAVE! combo.
        LevelManager.Instance.NotifyMagnetAssist();
        LevelManager.Instance.NotifyHoleAnticipation(f);
    }

    void OnCollisionEnter(Collision collision)
    {
        float speed = _rb.linearVelocity.magnitude;
        // Only spark when hitting something hard enough to register —
        // avoids spamming particles on resting contacts.
        if (speed < 1.2f) return;
        if (Time.time - _lastSparkTime < SparkCooldown) return;
        _lastSparkTime = Time.time;

        if (LevelManager.Instance != null && collision.contactCount > 0)
        {
            var contact = collision.GetContact(0);
            LevelManager.Instance.PlayWallSpark(contact.point, contact.normal);
            LevelManager.Instance.NotifyWallHit(collision.collider);

            // Hit-stop: brief time-scale dip when the impact is strong.
            // Uses unscaled time so the dip itself doesn't get slowed.
            if (speed >= hitStopSpeedThreshold)
                LevelManager.Instance.TriggerHitStop(hitStopScale, hitStopDuration);
        }
    }

    void Start()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        bool isMoving = _rb.linearVelocity.magnitude > stopThreshold;
        if (isMoving)
        {
            if (_isDragging) EndDrag();
            return;
        }

        // Auto-detect: if any joystick/button input was ever seen, use
        // arcade mode. Otherwise use mouse. Once arcade is detected it
        // stays active for the rest of the session.
        if (LuxoddGameBridge.IsArcadeInputActive)
            UpdateArcadeInput();
        else
            UpdateMouseInput();
    }

    // ─── Mouse input (existing behavior) ────────────────────
    void UpdateMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _dragStartWorld = GetMouseOnGround();
            if (aimLine != null) aimLine.enabled = true;
            UpdateAim();
        }
        else if (Input.GetMouseButton(0) && _isDragging)
        {
            UpdateAim();
        }
        else if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            Launch();
        }
    }

    // ─── Arcade joystick input ──────────────────────────────
    // Joystick direction = pull-back direction (aim)
    // Joystick magnitude = power (0..1 mapped to 0..maxDragDistance)
    // Black button (JoystickButton0) = launch
    [Header("Arcade input")]
    public float arcadeDeadzone = 0.2f;

    void UpdateArcadeInput()
    {
        float jx = Input.GetAxisRaw("Horizontal");
        float jy = Input.GetAxisRaw("Vertical");
        Vector2 stick = new Vector2(jx, jy);
        float mag = stick.magnitude;

        if (mag > arcadeDeadzone)
        {
            // Joystick is tilted — show aim. The drag vector in world space:
            // stick.x → world X, stick.y → world Z (camera is tilted top-down)
            float power = Mathf.Clamp01((mag - arcadeDeadzone) / (1f - arcadeDeadzone));
            Vector3 drag = new Vector3(stick.x, 0f, stick.y).normalized * power * maxDragDistance;

            if (!_isDragging)
            {
                _isDragging = true;
                if (aimLine != null) aimLine.enabled = true;
            }

            // Feed the same visual pipeline as mouse
            _arcadeDragOverride = drag;
            Vector3 origin = transform.position;
            Vector3 target = origin - drag;
            if (aimLine != null)
            {
                aimLine.SetPosition(0, origin);
                aimLine.SetPosition(1, target);
            }
            UpdatePreview(drag);
            UpdatePowerArc(drag);
        }
        else if (_isDragging && mag <= arcadeDeadzone)
        {
            // Joystick released back to center — NOT a launch.
            // Player must press the button to launch.
            // Keep aim visible at last direction until button or re-tilt.
        }

        // Black button = launch (JoystickButton0)
        if (_isDragging && Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            Vector3 drag = _arcadeDragOverride;
            EndDrag();
            if (drag.magnitude > 0.15f)
            {
                _rb.AddForce(-drag * forceMultiplier, ForceMode.Impulse);
                if (_rb.linearVelocity.magnitude > maxLaunchSpeed)
                    _rb.linearVelocity = _rb.linearVelocity.normalized * maxLaunchSpeed;
            }
            _arcadeDragOverride = Vector3.zero;
        }
    }

    Vector3 _arcadeDragOverride;

    Vector3 GetDragVector()
    {
        Vector3 current = GetMouseOnGround();
        Vector3 drag = current - _dragStartWorld;
        drag.y = 0;
        if (drag.magnitude > maxDragDistance)
            drag = drag.normalized * maxDragDistance;
        return drag;
    }

    void UpdateAim()
    {
        Vector3 drag = GetDragVector();
        Vector3 origin = transform.position;
        Vector3 target = origin - drag;
        if (aimLine != null)
        {
            aimLine.SetPosition(0, origin);
            aimLine.SetPosition(1, target);
        }
        UpdatePreview(drag);
        UpdatePowerArc(drag);
    }

    /// <summary>
    /// Draws a partial circle around the puck that fills 0-360° based
    /// on how much of the maxDragDistance is being used. Color lerps
    /// from yellow at low power to hot pink at max.
    /// </summary>
    void UpdatePowerArc(Vector3 drag)
    {
        if (powerArc == null) return;
        float fill = Mathf.Clamp01(drag.magnitude / maxDragDistance);
        if (fill <= 0.02f)
        {
            powerArc.enabled = false;
            powerArc.positionCount = 0;
            return;
        }
        powerArc.enabled = true;

        int count = Mathf.Max(2, Mathf.CeilToInt(powerArcSegments * fill) + 1);
        powerArc.positionCount = count;

        Vector3 center = transform.position + Vector3.up * powerArcYOffset;
        float totalAngle = 360f * fill;
        // Start angle sits at the top (north) so the arc sweeps clockwise.
        float startDeg = 90f;
        for (int i = 0; i < count; i++)
        {
            float a = (startDeg - (totalAngle * i) / (count - 1)) * Mathf.Deg2Rad;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * powerArcRadius, 0f, Mathf.Sin(a) * powerArcRadius);
            powerArc.SetPosition(i, p);
        }

        // Color shift: yellow (safe) → hot pink (dangerous max)
        Color c = Color.Lerp(new Color(1f, 0.9f, 0.25f, 1f),
                             new Color(1f, 0.25f, 0.6f, 1f),
                             fill);
        powerArc.startColor = c;
        powerArc.endColor   = c;
    }

    /// <summary>
    /// Projects the puck's predicted flight using SphereCast + reflection.
    /// Renders the path as a dotted line whose dot spacing is constant
    /// in world-units (texture tile count scales with total path length).
    /// Dot spacing scales down with drag strength so weak shots show
    /// a short preview and strong shots extend further — readable feedback.
    /// </summary>
    void UpdatePreview(Vector3 drag)
    {
        if (previewLine == null) return;
        if (drag.sqrMagnitude < 0.0225f)
        {
            previewLine.enabled = false;
            previewLine.positionCount = 0;
            return;
        }

        previewLine.enabled = true;
        var points = new System.Collections.Generic.List<Vector3>();
        Vector3 origin = transform.position;
        Vector3 dir = (-drag).normalized;
        points.Add(origin);

        // Preview length scales with drag strength so a gentle tap doesn't
        // show a wall-length path. Clamp to [2, previewMaxDistance].
        float dragFrac = Mathf.Clamp01(drag.magnitude / maxDragDistance);
        float allowed = Mathf.Lerp(2f, previewMaxDistance, dragFrac);

        float remaining = allowed;
        for (int i = 0; i <= previewBounces; i++)
        {
            if (remaining <= 0f) break;
            if (Physics.SphereCast(origin, previewSphereRadius, dir, out RaycastHit hit,
                                   remaining, previewMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 hitPoint = hit.point + hit.normal * previewSphereRadius;
                points.Add(hitPoint);
                remaining -= hit.distance;
                dir = Vector3.Reflect(dir, hit.normal);
                origin = hitPoint + dir * 0.02f;
            }
            else
            {
                points.Add(origin + dir * remaining);
                break;
            }
        }

        previewLine.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++) previewLine.SetPosition(i, points[i]);

        // Keep dot size constant in world units regardless of path length.
        if (previewLine.sharedMaterial != null)
        {
            float totalLen = 0f;
            for (int i = 1; i < points.Count; i++)
                totalLen += Vector3.Distance(points[i - 1], points[i]);
            // ~3 dots per world-unit reads well for this camera distance.
            float tiles = Mathf.Max(1f, totalLen * 3f);
            var scale = previewLine.sharedMaterial.mainTextureScale;
            scale.x = tiles;
            previewLine.sharedMaterial.mainTextureScale = scale;
        }
    }

    void Launch()
    {
        Vector3 drag = GetDragVector();
        EndDrag();
        if (drag.magnitude <= 0.15f) return;

        _rb.AddForce(-drag * forceMultiplier, ForceMode.Impulse);

        // Cap max speed so extreme drags don't tunnel the puck through walls.
        if (_rb.linearVelocity.magnitude > maxLaunchSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxLaunchSpeed;
    }

    void EndDrag()
    {
        _isDragging = false;
        if (aimLine != null) aimLine.enabled = false;
        if (previewLine != null)
        {
            previewLine.enabled = false;
            previewLine.positionCount = 0;
        }
        if (powerArc != null)
        {
            powerArc.enabled = false;
            powerArc.positionCount = 0;
        }
    }

    Vector3 GetMouseOnGround()
    {
        var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return transform.position;
    }

    public void ResetToStart()
    {
        _rb.isKinematic = true;
        transform.position = StartPosition;
        transform.rotation = Quaternion.identity;
        _rb.position = StartPosition;
        _rb.rotation = Quaternion.identity;
        Physics.SyncTransforms();
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        EndDrag();
    }
}
