using UnityEngine;

/// <summary>
/// Buca!-style drag-back slingshot puck.
/// Mouse down anywhere → start aiming. Drag → set direction/power. Release → launch.
/// Gameplay physics objects are pre-built by RealBuca/Setup Game Scene. The
/// transparent post-bounce coverage renderer is authored in the Game scene.
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
    SphereCollider _puckCollider;
    Camera _cam;
    bool _isDragging;
    Vector3 _dragStartWorld;
    Color _aimLineDefaultStart;
    Color _aimLineDefaultEnd;
    bool _aimLineColorsCached;
    bool _fineAimVisualActive;

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
    [Tooltip("Prebuilt transparent uncertainty corridor drawn only after the first rebound.")]
    public LineRenderer previewCoverageLine;
    [Tooltip("Width of the possible-path area immediately after the first rebound.")]
    [Range(0.18f, 0.6f)] public float previewCoverageStartWidth = 0.28f;
    [Tooltip("Width of the possible-path area at the end of the uncertain route.")]
    [Range(0.35f, 1.2f)] public float previewCoverageEndWidth = 0.72f;
    [Tooltip("Which layers the preview ray is allowed to hit (set to the level-geometry layer).")]
    public LayerMask previewMask = ~0;
    public int previewBounces = 2;
    public float previewMaxDistance = 20f;
    [Tooltip("Fallback seconds per prediction step. During play the preview uses Unity's fixed physics timestep so damping and continuous forces match the real puck.")]
    [Range(0.01f, 0.08f)] public float previewSimulationStep = 0.025f;
    [Tooltip("Safety cap for curved-path prediction work per rendered frame.")]
    [Range(40, 300)] public int previewMaxSteps = 180;
    [Tooltip("Fallback cast radius when the puck has no SphereCollider. Normally the preview derives the real scaled collider radius automatically.")]
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
        _puckCollider = GetComponent<SphereCollider>();

        // Thin raised mechanics must never be tunneled through at full power.
        // ContinuousDynamic tests the moving puck against static AND kinematic
        // rails, while the stronger depenetration cap resolves any tiny overlap
        // before it becomes visible.
        if (_rb != null)
        {
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.maxDepenetrationVelocity = Mathf.Max(_rb.maxDepenetrationVelocity, 28f);
            _rb.solverIterations = Mathf.Max(_rb.solverIterations, 12);
            _rb.solverVelocityIterations = Mathf.Max(_rb.solverVelocityIterations, 4);
        }

        // QA: "ball too fast" — cap the launch to a calmer speed. Lower these
        // two numbers to make it slower, raise them to make it faster.
        maxLaunchSpeed = Mathf.Min(maxLaunchSpeed, 14f);
        forceMultiplier = Mathf.Min(forceMultiplier, 5f);

        // Aim precision: the magnet is now a tiny RIM-DROP assist that cannot
        // deflect an aimed shot. It only nudges a puck that's ALREADY at the
        // hole's lip AND heading nearly straight in; everywhere else the puck
        // travels EXACTLY where aimed. To fully disable the assist, set
        // magnetForce = 0. To make the hole more forgiving again, raise
        // magnetRange (catch radius) and/or lower magnetAimDot (aim tolerance).
        magnetRange  = 0.85f;  // only within ~0.85u of the hole (was 2.0 — much tighter)
        magnetForce  = 7f;     // gentle nudge, not a yank (was 12)
        magnetAimDot = 0.8f;   // must head within ~37° of the hole (was 0.5 ≈ 60°)

        if (aimLine != null)
        {
            _aimLineDefaultStart = aimLine.startColor;
            _aimLineDefaultEnd = aimLine.endColor;
            _aimLineColorsCached = true;
            aimLine.enabled = false;
        }
        if (previewLine != null)
        {
            previewLine.enabled = false;
            previewLine.useWorldSpace = true;
            previewLine.positionCount = 0;
        }
        if (previewCoverageLine != null)
        {
            previewCoverageLine.enabled = false;
            previewCoverageLine.positionCount = 0;
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
        // Default: magnet not active this frame. Each early-return below
        // hits this StopMagnetLoop() so the looping audio source actually
        // fades to silent and stops — without it, the loop would only ever
        // get SetMagnetLoopActive(true, ...) and would linger indefinitely
        // after the puck leaves the hole's pull zone.
        if (LevelManager.Instance == null || !LevelManager.Instance.GameplayInputAllowed)
        {
            StopMagnetLoop();
            return;
        }
        Vector3 speedVec = _rb.linearVelocity; speedVec.y = 0f;
        float speed = speedVec.magnitude;
        if (speed < magnetMinSpeed) { StopMagnetLoop(); return; }

        Vector3 hole = LevelManager.Instance.GetCurrentHolePosition();
        if (hole == Vector3.positiveInfinity) { StopMagnetLoop(); return; }

        Vector3 toHole = hole - transform.position; toHole.y = 0f;
        float dist = toHole.magnitude;
        if (dist > magnetRange || dist < 0.01f) { StopMagnetLoop(); return; }

        // Only pull when heading roughly toward the hole.
        Vector3 velDir = speedVec / speed;
        Vector3 holeDir = toHole / dist;
        if (Vector3.Dot(velDir, holeDir) < magnetAimDot) { StopMagnetLoop(); return; }

        // Falloff: 1 at distance 0 → 0 at magnetRange. Smoothstep for curve.
        float f = 1f - Mathf.Clamp01(dist / magnetRange);
        f = f * f * (3f - 2f * f);
        _rb.AddForce(holeDir * magnetForce * f, ForceMode.Acceleration);

        // Tell LevelManager we're actively being pulled in — drives the
        // hole's "swallow anticipation" pulse + enables NICE SAVE! combo.
        LevelManager.Instance.NotifyMagnetAssist();
        LevelManager.Instance.NotifyHoleAnticipation(f);

        // Audio: magnet hum loop driven by pull strength
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMagnetLoopActive(true, f);
    }

    static void StopMagnetLoop()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMagnetLoopActive(false, 0f);
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

            // Audio: wall hit with speed-scaled volume + pitch
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayWallHit(speed);
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

        // Time-up and transition screens own input completely. Without this
        // gate the hidden/frozen puck could still charge, rotate its guide and
        // appear to keep playing behind the Continue modal.
        if (LevelManager.Instance != null && !LevelManager.Instance.GameplayInputAllowed)
        {
            if (_isDragging) EndDrag();
            _arcadePower = 0f;
            _arcadeAimDir = Vector3.zero;
            ResetVerticalAimLatch();
            _arcadeFireWasHeld = ReadArcadeFireHeld();
            return;
        }

        bool isMoving = _rb.linearVelocity.magnitude > stopThreshold;
        if (isMoving)
        {
            if (_isDragging) EndDrag();
            // Keep arcade edge-detection in sync even while puck flies, so a
            // held-then-released Red during flight can't cause a spurious
            // fire on the very next stopped frame. Also clear stale aim/power
            // — neither makes sense to retain while the puck is in motion.
            _arcadeFireWasHeld = ReadArcadeFireHeld();
            _arcadePower = 0f;
            _arcadeAimDir = Vector3.zero;
            ResetVerticalAimLatch();
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
    // Digital-cabinet model (no pressure-sensitive stick required):
    //   Joystick LEFT / RIGHT → ROTATES AIM continuously through 0..360 degrees
    //   Joystick UP / DOWN → quadrant-aware rotation:
    //       right half: UP turns left,  DOWN turns right
    //       left half:  UP turns right, DOWN turns left
    //     A vertical hold keeps its chosen turn direction across the quadrant
    //     boundary; the mapping is re-evaluated after the stick is released.
    //   HOLD Black button → POWER builds up over `chargeTimeToMax` seconds
    //   RELEASE Black → fire with whatever power was reached
    //   HOLD Green button → rotate aim slowly for fine adjustments
    //   White button → cancel current charge
    //
    // Aim can be rotated with the joystick at any time,
    // including while Black is being held — power keeps building, aim stays live.
    [Header("Arcade input")]
    [SerializeField]
    [Tooltip("Gameplay-facing Luxodd adapter. Run RealBuca/Setup Luxodd Arcade Joystick to create and assign it.")]
    BucaArcadeControlAdapter _arcadeInput;
    public float arcadeDeadzone = 0.2f;
    [Tooltip("Degrees per second while the digital joystick is held in an aiming direction. " +
             "100 gives a full 360-degree turn in 3.6 seconds and still allows fine taps.")]
    [Range(30f, 240f)] public float arcadeAimRotationSpeed = 100f;
    [Tooltip("Degrees per second while the Green fine-aim button is held. Releasing Green immediately returns to normal speed.")]
    [Range(10f, 90f)] public float fineAimRotationSpeed = 30f;
    [Tooltip("Trajectory refresh cap for WebGL. Aim remains smooth while expensive physics prediction runs at this rate.")]
    [Range(10f, 60f)] public float arcadePreviewRefreshRate = 30f;
    [Tooltip("Seconds for Red-hold to fill the power meter from 0 to 100%. " +
             "QA req: raised to 2.0s so players have finer control over shot strength.")]
    public float chargeTimeToMax = 2.0f;
    [Tooltip("Minimum length (as a fraction of full power) the aim guide is drawn at " +
             "BEFORE charging, so the player can see the throw direction before pressing fire.")]
    [Range(0.1f, 0.6f)] public float aimGuideMinFraction = 0.35f;
    [Tooltip("Power must reach at least this fraction (0..1) for a release to count as a real shot.")]
    public float minChargeToFire = 0.05f;

    Vector3 _arcadeAimDir;    // unit vector in XZ — where the puck will fly
    float   _arcadePower;     // 0..1 — accumulated charge while Black is held
    bool    _arcadeFireWasHeld; // edge detection for "released Black this frame"
    bool    _blockArcadeFireUntilRelease; // prevents a held Fire carrying through a restart
    Vector3 _arcadeDragOverride; // legacy, kept so GetDragVector etc still compile

    // Conventional planar angle: 0° = world right (+X), 90° = forward (+Z),
    // 180° = world left (-X), 270° = back (-Z).
    float   _arcadeAimAngleDegrees;
    bool    _arcadeAimInitialized;
    bool    _verticalAimWasHeld;
    float   _latchedVerticalTurn;
    float   _nextArcadePreviewTime;
    float   _lastArcadePreviewPower = -1f;
    Vector3 _lastArcadePreviewAim = Vector3.positiveInfinity;
    Vector3 _lastArcadePreviewOrigin = Vector3.positiveInfinity;
    Vector3 _displayedPreviewLaunchVelocity;
    Vector3 _displayedPreviewOrigin = Vector3.positiveInfinity;
    bool    _hasDisplayedPreviewLaunch;
    // Reused buffer keeps the live trajectory preview allocation-free. Results
    // from SphereCastNonAlloc are not ordered, so TryGetPreviewHit explicitly
    // selects the closest valid surface and ignores the puck's own collider.
    readonly RaycastHit[] _previewHits = new RaycastHit[128];
    readonly System.Collections.Generic.List<Vector3> _previewPoints
        = new System.Collections.Generic.List<Vector3>(192);
    const float PreviewSurfaceSeparation = 0.01f;
    const float PreviewCoincidentHitTolerance = 0.01f;

    // Cached mechanics belonging to the currently-instantiated level. The
    // cache changes only when LevelManager swaps the level root, avoiding a
    // scene-wide search every frame while still predicting gameplay modifiers.
    GameObject _previewMechanicsRoot;
    GravityWell[] _previewGravityWells = System.Array.Empty<GravityWell>();
    BucaWindZone[] _previewWindZones = System.Array.Empty<BucaWindZone>();
    IcePatch[] _previewIcePatches = System.Array.Empty<IcePatch>();
    SpeedBoost[] _previewSpeedBoosts = System.Array.Empty<SpeedBoost>();
    Teleporter[] _previewTeleporters = System.Array.Empty<Teleporter>();
    HoleTrigger[] _previewHoles = System.Array.Empty<HoleTrigger>();
    DeadlyTrigger[] _previewDeadlyTriggers = System.Array.Empty<DeadlyTrigger>();
    readonly System.Collections.Generic.HashSet<UnityEngine.Object> _previewTriggeredMechanics
        = new System.Collections.Generic.HashSet<UnityEngine.Object>();
    readonly System.Collections.Generic.List<PreviewDynamicCollider> _previewDynamicColliders
        = new System.Collections.Generic.List<PreviewDynamicCollider>(8);
    readonly System.Collections.Generic.HashSet<Collider> _previewDynamicColliderSet
        = new System.Collections.Generic.HashSet<Collider>();

    struct PreviewDynamicCollider
    {
        public Collider collider;
        public Transform body;
        public MovingWall movingWall;
        public RotatingWall rotatingWall;
        public Vector3 bodyLocalOffset;
        public Quaternion bodyLocalRotation;
        public float broadphaseRadius;
    }

    struct PreviewDynamicHit
    {
        public Collider collider;
        public float distance;
        public Vector3 planarNormal;
        public Vector3 surfaceVelocity;
        public bool terminalTrigger;
    }

    void UpdateArcadeInput()
    {
        Vector2 stick = ReadArcadeAim();
        float mag = stick.magnitude;
        // Luxodd's cabinet joystick is digital, so it supplies direction only.
        // Shot strength is deliberately time-based on the Black gameplay
        // button; the reserved Orange help button is untouched.
        bool fireHeld = ReadArcadeFireHeld();
        bool fineAimHeld = ReadArcadeFineAimHeld();

        // A Restart may happen while Fire is held. Require that old press to be
        // released before charging again, otherwise the rebuilt level could fire
        // immediately from stale input state.
        if (_blockArcadeFireUntilRelease)
        {
            if (!fireHeld) _blockArcadeFireUntilRelease = false;
            fireHeld = false;
        }

        // 1) Update aim from joystick whenever it's tilted past deadzone.
        //    Aim persists when stick returns to center — player can pre-aim,
        //    let go, then start charging.
        if (mag > arcadeDeadzone)
        {
            if (!_arcadeAimInitialized)
            {
                // Preserve the previous first-touch behavior (+Z for every
                // direction except DOWN, which starts at -Z) while storing the
                // angle in the conventional 0° RIGHT / 180° LEFT convention.
                _arcadeAimAngleDegrees = stick.y < -arcadeDeadzone ? 270f : 90f;
                _arcadeAimInitialized = true;
            }

            float absX = Mathf.Abs(stick.x);
            float absY = Mathf.Abs(stick.y);
            bool verticalHeld = absY > arcadeDeadzone;

            // Once a vertical command starts, keep its turn direction latched
            // until Y returns to neutral. This is what lets a held UP/DOWN pass
            // cleanly from one half of the circle into the other instead of
            // reversing or jittering on the boundary.
            bool useVertical = verticalHeld && (_verticalAimWasHeld || absY > absX);
            float turnInput = 0f;
            if (useVertical)
            {
                if (!_verticalAimWasHeld)
                    _latchedVerticalTurn = GetVerticalTurnForQuadrant(
                        _arcadeAimAngleDegrees, stick.y);

                _verticalAimWasHeld = true;
                turnInput = _latchedVerticalTurn;
            }
            else
            {
                _verticalAimWasHeld = false;
                _latchedVerticalTurn = 0f;

                // The old LEFT/RIGHT behavior is preserved exactly. The minus
                // sign only compensates for changing the stored angle from the
                // old 0°=+Z convention to conventional 0°=RIGHT.
                if (absX > arcadeDeadzone)
                    turnInput = -Mathf.Sign(stick.x);
            }

            if (turnInput != 0f)
            {
                // Clamp a single-frame hitch so returning from a browser stall
                // cannot make the aim jump by a large, unpredictable angle.
                float safeDeltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float normalRotationSpeed = arcadeAimRotationSpeed >= 30f
                    ? arcadeAimRotationSpeed : 100f;
                float preciseRotationSpeed = Mathf.Clamp(
                    fineAimRotationSpeed, 10f, normalRotationSpeed);
                float rotationSpeed = fineAimHeld
                    ? preciseRotationSpeed : normalRotationSpeed;
                _arcadeAimAngleDegrees = Mathf.Repeat(
                    _arcadeAimAngleDegrees
                    + turnInput * rotationSpeed * safeDeltaTime,
                    360f);
            }

            float radians = _arcadeAimAngleDegrees * Mathf.Deg2Rad;
            _arcadeAimDir = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
            if (!_isDragging)
            {
                _isDragging = true;
                if (aimLine != null) aimLine.enabled = true;
            }
        }
        else
        {
            // Releasing the stick commits the new quadrant. The next UP/DOWN
            // press calculates its direction from that new side.
            ResetVerticalAimLatch();
        }

        // 2) While Black is held AND we have an aim direction → charge power.
        bool hasAim = _arcadeAimDir.sqrMagnitude > 0.001f;
        SetFineAimVisual(fineAimHeld && hasAim);
        float prevPower = _arcadePower;
        if (fireHeld && hasAim)
        {
            _arcadePower = Mathf.Clamp01(_arcadePower + Time.deltaTime / Mathf.Max(0.1f, chargeTimeToMax));
            if (!_isDragging)
            {
                _isDragging = true;
                if (aimLine != null) aimLine.enabled = true;
            }
            // Diagnostic log every 25% milestone so QA can VERIFY the gradual
            // build-up timing in the Console. Logs at ~25%, 50%, 75%, 100%.
            int prevTier = Mathf.FloorToInt(prevPower * 4f);
            int curTier  = Mathf.FloorToInt(_arcadePower * 4f);
            if (curTier > prevTier && _arcadePower < 1.0f)
                Debug.Log($"[PuckController] Charge {Mathf.RoundToInt(_arcadePower * 100)}% " +
                          $"(elapsed ≈ {(_arcadePower * chargeTimeToMax):F2}s of {chargeTimeToMax:F2}s)");
            else if (curTier > prevTier)
                Debug.Log($"[PuckController] Charge 100% (full power after {chargeTimeToMax:F2}s of holding Red)");
        }

        // 3) Black RELEASE (with charge) → FIRE in aim direction at current power.
        //    Re-evaluate aim freshly here (don't trust the cached `hasAim`
        //    from earlier in the frame) — defensive against any state mutation
        //    between line 254 and here.
        bool hasAimNow = _arcadeAimDir.sqrMagnitude > 0.001f;
        if (_arcadeFireWasHeld && !fireHeld && _arcadePower >= minChargeToFire && hasAimNow)
        {
            Vector3 launchDrag = -_arcadeAimDir * _arcadePower * maxDragDistance;
            Vector3 launchVelocity = GetCommittedLaunchVelocity(launchDrag);
            // Deterministic aim: clear any residual drift/spin so the shot flies
            // along the most recently RENDERED trajectory. Aim can change on
            // the release frame before the capped preview refresh runs; using
            // that unseen direction made the puck disagree with the guide.
            ApplyLaunchVelocity(launchVelocity);

            // Audio: launch sfx, pitch + volume scale with charge
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(AudioManager.Instance.puckLaunchSfx,
                    volume: 0.6f + 0.4f * _arcadePower,
                    pitch: 0.85f + 0.4f * _arcadePower);

            EndDrag();
            _arcadePower = 0f;
            _arcadeAimDir = Vector3.zero;
            ResetVerticalAimLatch();
        }
        // Released Black with too little charge → cancel quietly. Clear BOTH
        // power AND aim so the visual block at the end of Update doesn't
        // render a stale aim line for an extra frame.
        else if (_arcadeFireWasHeld && !fireHeld)
        {
            _arcadePower = 0f;
            _arcadeAimDir = Vector3.zero;
            ResetVerticalAimLatch();
            // Also drop drag-state so EndDrag's job (hide aim/preview) happens
            if (_isDragging) EndDrag();
        }
        _arcadeFireWasHeld = fireHeld;

        // 4) White button → cancel an in-progress charge, keep puck at rest.
        if (_isDragging && ReadArcadeCancelDown())
        {
            EndDrag();
            _arcadePower = 0f;
            _arcadeAimDir = Vector3.zero;
            ResetVerticalAimLatch();
            return;
        }

        // 5) Visuals.
        //    QA req #2: the throw-direction guide must be visible BEFORE the
        //    fire button is pressed. Previously the aim line + trajectory used
        //    `_arcadePower` for their length, which is 0 until the player holds
        //    Red — so the guide was invisible while just aiming. Now the aim
        //    line + trajectory preview use a MINIMUM visual length
        //    (aimGuideMinFraction) so the direction shows the moment the stick
        //    is tilted. The POWER ARC still uses the real charge (empty → full)
        //    so the player still sees the strength building separately.
        if (_isDragging && hasAim)
        {
            // Aim line + trajectory: always visible at >= aimGuideMinFraction,
            // growing toward full length as charge builds.
            float guideFraction = Mathf.Max(_arcadePower, aimGuideMinFraction);
            Vector3 aimVec = _arcadeAimDir * guideFraction * maxDragDistance;
            Vector3 origin = GetTrajectoryOrigin();
            Vector3 target = origin + aimVec; // points TOWARD the shot direction
            if (aimLine != null)
            {
                aimLine.enabled = true;
                aimLine.SetPosition(0, origin);
                aimLine.SetPosition(1, target);
            }
            // The direction line has a minimum readable length, but trajectory
            // prediction must use the REAL charge. Previously a 5% shot drew a
            // 35% path and then travelled far less than promised.
            Vector3 realShotVec = _arcadeAimDir * _arcadePower * maxDragDistance;

            bool previewChanged =
                (_arcadeAimDir - _lastArcadePreviewAim).sqrMagnitude > 0.000001f
                || Mathf.Abs(_arcadePower - _lastArcadePreviewPower) > 0.0025f
                || (origin - _lastArcadePreviewOrigin).sqrMagnitude > 0.0001f
                // Even with steady aim/full charge, dynamic walls keep changing
                // the future contact point and must refresh at the configured cap.
                || _previewDynamicColliders.Count > 0;

            if (previewChanged && Time.unscaledTime >= _nextArcadePreviewTime)
            {
                float refreshRate = arcadePreviewRefreshRate >= 10f
                    ? Mathf.Clamp(arcadePreviewRefreshRate, 10f, 60f) : 30f;
                _nextArcadePreviewTime = Time.unscaledTime + 1f / refreshRate;
                _lastArcadePreviewAim = _arcadeAimDir;
                _lastArcadePreviewPower = _arcadePower;
                _lastArcadePreviewOrigin = origin;
                UpdatePreview(-realShotVec);
            }

            // Power arc: reflects ACTUAL charge — empty before holding Red,
            // fills as power builds. Uses _arcadePower (not the guide minimum).
            Vector3 powerVec = _arcadeAimDir * _arcadePower * maxDragDistance;
            UpdatePowerArc(-powerVec);
        }
    }

    /// <summary>
    /// Returns the signed angular motion for a NEW vertical-stick hold.
    /// At 0° (RIGHT), UP turns left (+) and DOWN turns right (-).
    /// At 180° (LEFT), the mapping is reversed. The caller latches this result
    /// for the full hold so crossing 90°/270° never makes the aim bounce back.
    /// </summary>
    public static float GetVerticalTurnForQuadrant(float angleDegrees, float verticalInput)
    {
        if (Mathf.Abs(verticalInput) <= Mathf.Epsilon) return 0f;

        float angle = Mathf.Repeat(angleDegrees, 360f);
        float horizontalPosition = Mathf.Cos(angle * Mathf.Deg2Rad);

        // Treat the exact top/bottom boundaries as belonging to the RIGHT
        // half. The requested horizontal edge cases remain unambiguous:
        // 0° is RIGHT and 180° is LEFT.
        bool rightHalf = horizontalPosition >= -0.0001f;
        float verticalSign = Mathf.Sign(verticalInput);
        return rightHalf ? verticalSign : -verticalSign;
    }

    void ResetVerticalAimLatch()
    {
        _verticalAimWasHeld = false;
        _latchedVerticalTurn = 0f;
    }

    Vector2 ReadArcadeAim()
    {
        return _arcadeInput != null && _arcadeInput.isActiveAndEnabled
            ? _arcadeInput.AimVector
            : ArcadeInputAdapter.GetStick();
    }

    bool ReadArcadeFireHeld()
    {
        return _arcadeInput != null && _arcadeInput.isActiveAndEnabled
            ? _arcadeInput.IsShootButtonPressed
            : ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
    }

    bool ReadArcadeFineAimHeld()
    {
        return _arcadeInput != null && _arcadeInput.isActiveAndEnabled
            ? _arcadeInput.IsFineTuneButtonPressed
            : ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Green);
    }

    bool ReadArcadeCancelDown()
    {
        return _arcadeInput != null && _arcadeInput.isActiveAndEnabled
            ? _arcadeInput.CancelPressedThisFrame
            : ArcadeInputAdapter.CancelDown();
    }

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
        Vector3 origin = GetTrajectoryOrigin();
        Vector3 target = origin - drag;   // slingshot: aim line points OPPOSITE the drag
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
    /// Projects the puck's predicted flight using the current physics pose and
    /// the puck's real world-space collider radius. All collision normals are
    /// projected onto the XZ gameplay plane before reflection because the
    /// Rigidbody is Y-locked; reflecting a bevel's 3D normal was the source of
    /// intermittent sideways/reversed preview segments at corners.
    /// </summary>
    void UpdatePreview(Vector3 drag)
    {
        if (previewLine == null)
        {
            _hasDisplayedPreviewLaunch = false;
            return;
        }
        if (drag.sqrMagnitude < 0.0225f)
        {
            HideTrajectoryPreview();
            return;
        }

        // Moving/rotating level geometry can change after the last physics
        // step. Synchronize once so every cast uses the latest transforms.
        Physics.SyncTransforms();

        Vector3 launchVelocity = CalculateLaunchVelocity(drag);
        if (launchVelocity.sqrMagnitude < 0.0001f)
        {
            HideTrajectoryPreview();
            return;
        }

        RefreshPreviewMechanicsCache();
        _previewTriggeredMechanics.Clear();

        previewLine.enabled = true;
        _previewPoints.Clear();
        System.Collections.Generic.List<Vector3> points = _previewPoints;
        Vector3 origin = GetTrajectoryOrigin();
        _displayedPreviewLaunchVelocity = launchVelocity;
        _displayedPreviewOrigin = origin;
        _hasDisplayedPreviewLaunch = true;
        Vector3 velocity = launchVelocity;
        float castRadius = GetPreviewCastRadius();
        Collider previousCollider = null;
        points.Add(origin);

        // Simulate the same launch velocity, damping, continuous forces and
        // one-shot mechanics as gameplay. Small swept-sphere steps preserve the
        // existing material-aware bounce solver while allowing curved paths.
        float dragFrac = Mathf.Clamp01(drag.magnitude / Mathf.Max(0.01f, maxDragDistance));
        float maxTravel = Mathf.Lerp(2f, previewMaxDistance, dragFrac);
        float travelled = 0f;
        // Use the exact cadence that advances the real Rigidbody. The old
        // 0.025s preview step disagreed with the project's 0.02s fixed step,
        // accumulating visible drift over longer and force-curved paths.
        float physicsStep = Time.fixedDeltaTime > 0f
            ? Time.fixedDeltaTime : previewSimulationStep;
        float dt = Mathf.Clamp(physicsStep, 0.01f, 0.08f);
        int bounceCount = 0;
        int firstBouncePointIndex = -1;
        int maxSteps = Mathf.Clamp(previewMaxSteps, 40, 300);
        float previewStartTime = Time.time;
        float simulatedTime = 0f;
        bool terminateTrajectory = false;

        for (int step = 0; step < maxSteps && travelled < maxTravel; step++)
        {
            ApplyPreviewContinuousForces(ref velocity, origin, castRadius, dt);
            velocity.y = 0f;
            if (velocity.magnitude < stopThreshold) break;

            float stepTime = dt;
            int contactsThisStep = 0;
            while (stepTime > 0.0001f && travelled < maxTravel && contactsThisStep < 3)
            {
                float speed = velocity.magnitude;
                if (speed < stopThreshold) { stepTime = 0f; break; }

                Vector3 dir = velocity / speed;
                float requestedTravel = Mathf.Min(speed * stepTime, maxTravel - travelled);
                if (requestedTravel <= 0.0001f) { stepTime = 0f; break; }

                bool hasStaticHit = TryGetPreviewHit(
                    origin, castRadius, dir, requestedTravel, previousCollider,
                    out RaycastHit staticHit, out Vector3 staticPlanarNormal);
                float segmentStartTime = previewStartTime + simulatedTime + (dt - stepTime);
                float requestedDuration = requestedTravel / speed;
                bool hasDynamicHit = TryGetPredictedDynamicPreviewHit(
                    origin, dir, requestedTravel, segmentStartTime, requestedDuration,
                    previousCollider, out PreviewDynamicHit dynamicHit);

                bool dynamicHitFirst = hasDynamicHit
                    && (!hasStaticHit || dynamicHit.distance <= staticHit.distance);
                if (hasStaticHit || hasDynamicHit)
                {
                    float travel = Mathf.Max(0f,
                        dynamicHitFirst ? dynamicHit.distance : staticHit.distance);
                    Vector3 centerAtImpact = origin + dir * travel;
                    travelled += travel;
                    points.Add(centerAtImpact);

                    // A moving deadly trigger ends the real path at its predicted
                    // contact point. It is not a rebound and should remain part of
                    // the exact line rather than opening an uncertainty corridor.
                    if (dynamicHitFirst && dynamicHit.terminalTrigger)
                    {
                        origin = centerAtImpact;
                        terminateTrajectory = true;
                        stepTime = 0f;
                        break;
                    }

                    if (firstBouncePointIndex < 0)
                        firstBouncePointIndex = points.Count - 1;

                    Collider hitCollider;
                    if (dynamicHitFirst)
                    {
                        hitCollider = dynamicHit.collider;
                        velocity = ResolvePreviewCollisionVelocity(
                            velocity, dynamicHit.planarNormal, hitCollider,
                            dynamicHit.surfaceVelocity, centerAtImpact);
                    }
                    else
                    {
                        hitCollider = staticHit.collider;
                        velocity = ResolvePreviewCollisionVelocity(
                            velocity, staticPlanarNormal, staticHit, centerAtImpact);
                    }
                    velocity.y = 0f;
                    bounceCount++;
                    contactsThisStep++;
                    if (velocity.sqrMagnitude < 0.0001f || bounceCount > previewBounces)
                    {
                        origin = centerAtImpact;
                        stepTime = 0f;
                        break;
                    }

                    float consumed = speed > 0.0001f ? travel / speed : stepTime;
                    stepTime = Mathf.Max(0f, stepTime - consumed);
                    previousCollider = hitCollider;
                    origin = centerAtImpact + velocity.normalized * PreviewSurfaceSeparation;
                    travelled += PreviewSurfaceSeparation;
                }
                else
                {
                    origin += dir * requestedTravel;
                    travelled += requestedTravel;
                    previousCollider = null;
                    stepTime = 0f;
                }

                if (!ApplyPreviewTriggerMechanics(ref origin, ref velocity, castRadius))
                {
                    stepTime = 0f;
                    step = maxSteps;
                    break;
                }
            }

            if (points.Count == 0 || Vector3.SqrMagnitude(points[points.Count - 1] - origin) > 0.0025f)
                points.Add(origin);
            simulatedTime += dt;
            if (terminateTrajectory || bounceCount > previewBounces) break;
        }

        // Keep one clean centre-line through authored rebound predictions. The
        // old widening post-bounce coverage strip could expand into a large
        // opaque wedge and hide the board.
        int exactPointCount = points.Count;
        previewLine.positionCount = exactPointCount;
        for (int i = 0; i < exactPointCount; i++)
            previewLine.SetPosition(i, points[i]);

        if (previewCoverageLine != null)
        {
            previewCoverageLine.enabled = false;
            previewCoverageLine.positionCount = 0;
        }
    }

    void HideTrajectoryPreview()
    {
        _hasDisplayedPreviewLaunch = false;
        if (previewLine != null)
        {
            previewLine.enabled = false;
            previewLine.positionCount = 0;
        }
        if (previewCoverageLine != null)
        {
            previewCoverageLine.enabled = false;
            previewCoverageLine.positionCount = 0;
        }
    }

    void RefreshPreviewMechanicsCache()
    {
        GameObject root = LevelManager.Instance != null
            ? LevelManager.Instance.CurrentLevelRoot : null;
        if (root == _previewMechanicsRoot) return;

        _previewMechanicsRoot = root;
        if (root == null)
        {
            _previewGravityWells = System.Array.Empty<GravityWell>();
            _previewWindZones = System.Array.Empty<BucaWindZone>();
            _previewIcePatches = System.Array.Empty<IcePatch>();
            _previewSpeedBoosts = System.Array.Empty<SpeedBoost>();
            _previewTeleporters = System.Array.Empty<Teleporter>();
            _previewHoles = System.Array.Empty<HoleTrigger>();
            _previewDeadlyTriggers = System.Array.Empty<DeadlyTrigger>();
            _previewDynamicColliders.Clear();
            _previewDynamicColliderSet.Clear();
            return;
        }

        _previewGravityWells = root.GetComponentsInChildren<GravityWell>(true);
        _previewWindZones = root.GetComponentsInChildren<BucaWindZone>(true);
        _previewIcePatches = root.GetComponentsInChildren<IcePatch>(true);
        _previewSpeedBoosts = root.GetComponentsInChildren<SpeedBoost>(true);
        _previewTeleporters = root.GetComponentsInChildren<Teleporter>(true);
        _previewHoles = root.GetComponentsInChildren<HoleTrigger>(true);
        _previewDeadlyTriggers = root.GetComponentsInChildren<DeadlyTrigger>(true);

        // Physics casts can only query the moving obstacles at their current
        // transforms. Cache them separately so UpdatePreview can ignore those
        // stale cast hits and test each collider at its predicted arrival-time pose.
        _previewDynamicColliders.Clear();
        _previewDynamicColliderSet.Clear();
        MovingWall[] movingWalls = root.GetComponentsInChildren<MovingWall>(true);
        for (int i = 0; i < movingWalls.Length; i++)
            AddPreviewDynamicColliders(movingWalls[i], movingWalls[i], null);
        RotatingWall[] rotatingWalls = root.GetComponentsInChildren<RotatingWall>(true);
        for (int i = 0; i < rotatingWalls.Length; i++)
            AddPreviewDynamicColliders(rotatingWalls[i], null, rotatingWalls[i]);
    }

    void AddPreviewDynamicColliders(Component motionBody, MovingWall movingWall,
                                    RotatingWall rotatingWall)
    {
        if (motionBody == null) return;
        Transform body = motionBody.transform;
        Quaternion inverseBodyRotation = Quaternion.Inverse(body.rotation);
        Collider[] colliders = motionBody.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !_previewDynamicColliderSet.Add(collider)) continue;
            _previewDynamicColliders.Add(new PreviewDynamicCollider
            {
                collider = collider,
                body = body,
                movingWall = movingWall,
                rotatingWall = rotatingWall,
                // Store the already-scaled rigid offset. Dynamic walls do not
                // resize at runtime, so predicted pose reconstruction stays exact.
                bodyLocalOffset = inverseBodyRotation * (collider.transform.position - body.position),
                bodyLocalRotation = inverseBodyRotation * collider.transform.rotation,
                broadphaseRadius = Vector3.Distance(body.position, collider.bounds.center)
                    + collider.bounds.extents.magnitude + GetPreviewCastRadius()
            });
        }
    }

    void ApplyPreviewContinuousForces(ref Vector3 velocity, Vector3 position,
                                      float radius, float dt)
    {
        // Gravity wells use the same smoothstep falloff as GravityWell.FixedUpdate.
        for (int i = 0; i < _previewGravityWells.Length; i++)
        {
            GravityWell well = _previewGravityWells[i];
            if (well == null || !well.isActiveAndEnabled || well.range <= 0f) continue;
            Vector3 to = well.transform.position - position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > well.range || dist < 0.05f) continue;
            float f = 1f - Mathf.Clamp01(dist / well.range);
            f = f * f * (3f - 2f * f);
            Vector3 dir = to / dist;
            if (well.repel) dir = -dir;
            velocity += dir * well.strength * f * dt;
        }

        // Wind applies only while the predicted puck volume overlaps its trigger.
        for (int i = 0; i < _previewWindZones.Length; i++)
        {
            BucaWindZone wind = _previewWindZones[i];
            if (wind == null || !wind.isActiveAndEnabled) continue;
            Collider trigger = wind.GetComponent<Collider>();
            if (!PreviewOverlaps(trigger, position, radius)) continue;
            Vector3 force = wind.transform.forward * wind.forceMagnitude;
            if (wind.addLift) force += Vector3.up * (wind.forceMagnitude * 0.15f);
            force.y = 0f; // the real puck is locked to the gameplay plane
            velocity += force * dt;
        }

        // Match the near-hole magnetic assist using the current hole pose.
        if (LevelManager.Instance != null && magnetForce > 0f && velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 hole = LevelManager.Instance.GetCurrentHolePosition();
            if (hole != Vector3.positiveInfinity)
            {
                Vector3 planarVelocity = velocity; planarVelocity.y = 0f;
                float speed = planarVelocity.magnitude;
                Vector3 toHole = hole - position; toHole.y = 0f;
                float dist = toHole.magnitude;
                if (speed >= magnetMinSpeed && dist > 0.01f && dist <= magnetRange)
                {
                    Vector3 holeDir = toHole / dist;
                    if (Vector3.Dot(planarVelocity / speed, holeDir) >= magnetAimDot)
                    {
                        float f = 1f - Mathf.Clamp01(dist / magnetRange);
                        f = f * f * (3f - 2f * f);
                        velocity += holeDir * magnetForce * f * dt;
                    }
                }
            }
        }

        float damping = _rb != null ? Mathf.Max(0f, _rb.linearDamping) : 0.9f;
        for (int i = 0; i < _previewIcePatches.Length; i++)
        {
            IcePatch patch = _previewIcePatches[i];
            if (patch == null || !patch.isActiveAndEnabled) continue;
            if (PreviewOverlaps(patch.GetComponent<Collider>(), position, radius))
            {
                damping = Mathf.Max(0f, patch.patchDamping);
                break;
            }
        }
        // PhysX applies linear damping once per fixed step using this decay.
        // Matching it prevents the preview from travelling farther than the
        // real puck before a rail or trigger contact.
        velocity *= Mathf.Max(0f, 1f - damping * dt);
    }

    bool ApplyPreviewTriggerMechanics(ref Vector3 position, ref Vector3 velocity,
                                      float radius)
    {
        for (int i = 0; i < _previewSpeedBoosts.Length; i++)
        {
            SpeedBoost boost = _previewSpeedBoosts[i];
            if (boost == null || !boost.isActiveAndEnabled
                || !PreviewOverlaps(boost.GetComponent<Collider>(), position, radius)
                || !_previewTriggeredMechanics.Add(boost)) continue;

            Vector3 dir = boost.lockToForward
                ? boost.transform.forward
                : (velocity.sqrMagnitude > 0.01f ? velocity.normalized : boost.transform.forward);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();
            velocity += dir * boost.boostAmount;
            if (velocity.magnitude > boost.maxSpeedAfter)
                velocity = velocity.normalized * boost.maxSpeedAfter;
        }

        for (int i = 0; i < _previewTeleporters.Length; i++)
        {
            Teleporter gate = _previewTeleporters[i];
            if (gate == null || gate.partner == null || !gate.isActiveAndEnabled
                || !PreviewOverlaps(gate.GetComponent<Collider>(), position, radius)
                || !_previewTriggeredMechanics.Add(gate)) continue;

            float speed = velocity.magnitude;
            Vector3 localDir = gate.transform.InverseTransformDirection(
                speed > 0.0001f ? velocity / speed : gate.transform.forward);
            Vector3 outDir = gate.partner.transform.TransformDirection(localDir);
            outDir.y = 0f;
            if (outDir.sqrMagnitude < 0.001f) outDir = gate.partner.transform.forward;
            outDir.Normalize();
            position = gate.partner.transform.position + outDir * 0.6f
                     + Vector3.up * (position.y - gate.transform.position.y);
            velocity = outDir * speed * gate.exitSpeedMultiplier;
            _previewTriggeredMechanics.Add(gate.partner);
            break;
        }

        // The real path ends as soon as either terminal trigger receives the puck.
        for (int i = 0; i < _previewHoles.Length; i++)
        {
            HoleTrigger hole = _previewHoles[i];
            Collider holeCollider = hole != null ? hole.GetComponent<Collider>() : null;
            if (hole != null && hole.isActiveAndEnabled
                && !_previewDynamicColliderSet.Contains(holeCollider)
                && PreviewOverlaps(holeCollider, position, radius))
                return false;
        }
        for (int i = 0; i < _previewDeadlyTriggers.Length; i++)
        {
            DeadlyTrigger deadly = _previewDeadlyTriggers[i];
            Collider deadlyCollider = deadly != null ? deadly.GetComponent<Collider>() : null;
            if (deadly != null && deadly.isActiveAndEnabled
                && !_previewDynamicColliderSet.Contains(deadlyCollider)
                && PreviewOverlaps(deadlyCollider, position, radius))
                return false;
        }
        return true;
    }

    static bool PreviewOverlaps(Collider collider, Vector3 center, float radius)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            return false;
        Vector3 closest = collider.ClosestPoint(center);
        return Vector3.SqrMagnitude(closest - center) <= radius * radius + 0.0001f;
    }

    Vector3 GetTrajectoryOrigin()
    {
        if (_rb == null) return transform.position;
        if (_puckCollider == null) return _rb.position;

        // Rigidbody.position is the authoritative physics pose. Transform can
        // be one rendered frame behind while interpolation is enabled.
        Vector3 scaledCenter = Vector3.Scale(_puckCollider.center, transform.lossyScale);
        return _rb.position + _rb.rotation * scaledCenter;
    }

    float GetPreviewCastRadius()
    {
        if (_puckCollider == null) return Mathf.Max(0.01f, previewSphereRadius);

        Vector3 scale = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        float worldRadius = _puckCollider.radius * maxScale;

        // A tiny skin keeps a floor that merely touches the Y-locked puck from
        // becoming a zero-distance horizontal cast hit.
        return Mathf.Max(0.01f, worldRadius - PreviewSurfaceSeparation);
    }

    /// <summary>
    /// Tests moving and rotating colliders at the time the simulated puck reaches
    /// them. Physics.SphereCast only sees their current pose, so those stale hits
    /// are excluded from the normal query and reconstructed here without moving
    /// any live scene objects.
    /// </summary>
    bool TryGetPredictedDynamicPreviewHit(
        Vector3 origin, Vector3 direction, float distance,
        float segmentStartTime, float segmentDuration, Collider previousCollider,
        out PreviewDynamicHit bestHit)
    {
        bestHit = default;
        if (_puckCollider == null || _previewDynamicColliders.Count == 0
            || distance <= 0f || segmentDuration <= 0f)
            return false;

        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < _previewDynamicColliders.Count; i++)
        {
            PreviewDynamicCollider dynamicCollider = _previewDynamicColliders[i];
            Collider collider = dynamicCollider.collider;
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                continue;
            if ((previewMask.value & (1 << collider.gameObject.layer)) == 0) continue;
            if (dynamicCollider.movingWall != null
                && !dynamicCollider.movingWall.isActiveAndEnabled) continue;
            if (dynamicCollider.rotatingWall != null
                && !dynamicCollider.rotatingWall.isActiveAndEnabled) continue;

            HoleTrigger hole = collider.GetComponentInParent<HoleTrigger>();
            DeadlyTrigger deadly = collider.GetComponentInParent<DeadlyTrigger>();
            bool terminalTrigger = collider.isTrigger
                && ((hole != null && hole.isActiveAndEnabled)
                    || (deadly != null && deadly.isActiveAndEnabled));
            if (collider.isTrigger && !terminalTrigger) continue;

            if (!TryFindPredictedDynamicContact(
                    dynamicCollider, origin, direction, distance,
                    segmentStartTime, segmentDuration, previousCollider,
                    terminalTrigger, out float hitFraction,
                    out Vector3 planarNormal, out Vector3 surfaceVelocity))
                continue;

            float hitDistance = distance * hitFraction;
            if (hitDistance >= bestDistance) continue;
            bestDistance = hitDistance;
            bestHit = new PreviewDynamicHit
            {
                collider = collider,
                distance = hitDistance,
                planarNormal = planarNormal,
                surfaceVelocity = surfaceVelocity,
                terminalTrigger = terminalTrigger
            };
        }
        return bestHit.collider != null;
    }

    bool TryFindPredictedDynamicContact(
        PreviewDynamicCollider dynamicCollider,
        Vector3 origin, Vector3 direction, float distance,
        float segmentStartTime, float segmentDuration, Collider previousCollider,
        bool terminalTrigger, out float hitFraction,
        out Vector3 planarNormal, out Vector3 surfaceVelocity)
    {
        hitFraction = 0f;
        planarNormal = Vector3.zero;
        surfaceVelocity = Vector3.zero;

        // Cheap swept-sphere broad phase: most preview steps are nowhere near a
        // dynamic wall. Avoid dozens of ComputePenetration calls in those steps,
        // which matters on the WebGL cabinet build.
        Vector3 puckEnd = origin + direction * distance;
        Vector3 bodyStart = GetPredictedDynamicBodyPosition(
            dynamicCollider, segmentStartTime);
        Vector3 bodyEnd = GetPredictedDynamicBodyPosition(
            dynamicCollider, segmentStartTime + segmentDuration);
        Vector3 bodyMidpoint = (bodyStart + bodyEnd) * 0.5f;
        float broadphaseRadius = dynamicCollider.broadphaseRadius
            + Vector3.Distance(bodyStart, bodyEnd) * 0.5f;
        if (SqrDistancePointToSegment(bodyMidpoint, origin, puckEnd)
            > broadphaseRadius * broadphaseRadius)
            return false;

        bool startsOverlapping = TryGetPredictedDynamicOverlap(
            dynamicCollider, origin, segmentStartTime,
            out Vector3 startSeparationNormal);
        if (startsOverlapping && dynamicCollider.collider != previousCollider)
        {
            Vector3 startSurfaceVelocity = GetPredictedDynamicSurfaceVelocity(
                dynamicCollider, origin, segmentStartTime);
            if (terminalTrigger || IsApproachingPredictedSurface(
                    direction, distance, segmentDuration,
                    startSeparationNormal, startSurfaceVelocity))
            {
                planarNormal = GetUsablePlanarNormal(startSeparationNormal, direction);
                surfaceVelocity = startSurfaceVelocity;
                return terminalTrigger || planarNormal.sqrMagnitude > 0.0001f;
            }
        }

        // Each physics step is at most 0.08s and normal gameplay uses 0.02s.
        // Eight temporal samples prevent a fast bar from crossing completely
        // through the puck's swept volume between the endpoints of a step.
        const int TemporalSamples = 8;
        const int ContactRefinementIterations = 7;
        bool waitingForSeparation = startsOverlapping
            && dynamicCollider.collider == previousCollider;
        float lastClearFraction = 0f;

        for (int sample = 1; sample <= TemporalSamples; sample++)
        {
            float fraction = sample / (float)TemporalSamples;
            Vector3 center = origin + direction * (distance * fraction);
            float absoluteTime = segmentStartTime + segmentDuration * fraction;
            bool overlaps = TryGetPredictedDynamicOverlap(
                dynamicCollider, center, absoluteTime, out Vector3 separationNormal);

            if (waitingForSeparation)
            {
                if (!overlaps)
                {
                    waitingForSeparation = false;
                    lastClearFraction = fraction;
                }
                continue;
            }

            if (!overlaps)
            {
                lastClearFraction = fraction;
                continue;
            }

            float low = lastClearFraction;
            float high = fraction;
            for (int iteration = 0; iteration < ContactRefinementIterations; iteration++)
            {
                float mid = (low + high) * 0.5f;
                Vector3 midCenter = origin + direction * (distance * mid);
                float midTime = segmentStartTime + segmentDuration * mid;
                if (TryGetPredictedDynamicOverlap(
                        dynamicCollider, midCenter, midTime, out _))
                    high = mid;
                else
                    low = mid;
            }

            hitFraction = high;
            Vector3 hitCenter = origin + direction * (distance * hitFraction);
            float hitTime = segmentStartTime + segmentDuration * hitFraction;
            TryGetPredictedDynamicOverlap(
                dynamicCollider, hitCenter, hitTime, out separationNormal);
            surfaceVelocity = GetPredictedDynamicSurfaceVelocity(
                dynamicCollider, hitCenter, hitTime);
            planarNormal = GetUsablePlanarNormal(separationNormal, direction);

            if (terminalTrigger) return true;
            if (planarNormal.sqrMagnitude < 0.0001f) return false;
            return IsApproachingPredictedSurface(
                direction, distance, segmentDuration, planarNormal, surfaceVelocity);
        }
        return false;
    }

    static Vector3 GetPredictedDynamicBodyPosition(
        PreviewDynamicCollider dynamicCollider, float absoluteTime)
    {
        return dynamicCollider.movingWall != null
            ? dynamicCollider.movingWall.GetPredictedPosition(absoluteTime)
            : dynamicCollider.body.position;
    }

    static float SqrDistancePointToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.000001f) return (point - start).sqrMagnitude;
        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return (point - (start + segment * t)).sqrMagnitude;
    }

    bool TryGetPredictedDynamicOverlap(
        PreviewDynamicCollider dynamicCollider, Vector3 puckCenter, float absoluteTime,
        out Vector3 separationNormal)
    {
        separationNormal = Vector3.zero;
        Collider collider = dynamicCollider.collider;
        if (_puckCollider == null || collider == null) return false;

        Vector3 bodyPosition = dynamicCollider.body.position;
        Quaternion bodyRotation = dynamicCollider.body.rotation;
        if (dynamicCollider.movingWall != null)
            bodyPosition = dynamicCollider.movingWall.GetPredictedPosition(absoluteTime);
        if (dynamicCollider.rotatingWall != null)
            bodyRotation = dynamicCollider.rotatingWall.GetPredictedRotation(absoluteTime);

        Vector3 colliderPosition = bodyPosition
            + bodyRotation * dynamicCollider.bodyLocalOffset;
        Quaternion colliderRotation = bodyRotation * dynamicCollider.bodyLocalRotation;

        Quaternion puckRotation = _rb != null ? _rb.rotation : transform.rotation;
        Vector3 scaledCenter = Vector3.Scale(_puckCollider.center, transform.lossyScale);
        Vector3 puckTransformPosition = puckCenter - puckRotation * scaledCenter;
        return Physics.ComputePenetration(
            _puckCollider, puckTransformPosition, puckRotation,
            collider, colliderPosition, colliderRotation,
            out separationNormal, out _);
    }

    static Vector3 GetUsablePlanarNormal(Vector3 separationNormal, Vector3 direction)
    {
        separationNormal.y = 0f;
        if (separationNormal.sqrMagnitude > 0.0001f)
            return separationNormal.normalized;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? -direction.normalized : Vector3.zero;
    }

    static bool IsApproachingPredictedSurface(
        Vector3 direction, float distance, float duration,
        Vector3 planarNormal, Vector3 surfaceVelocity)
    {
        Vector3 puckVelocity = direction * (distance / Mathf.Max(0.0001f, duration));
        Vector3 relativeVelocity = puckVelocity - surfaceVelocity;
        relativeVelocity.y = 0f;
        return Vector3.Dot(relativeVelocity, planarNormal) < -0.0001f;
    }

    static Vector3 GetPredictedDynamicSurfaceVelocity(
        PreviewDynamicCollider dynamicCollider, Vector3 worldPoint, float absoluteTime)
    {
        Vector3 velocity = Vector3.zero;
        if (dynamicCollider.movingWall != null)
            velocity += dynamicCollider.movingWall.GetPredictedVelocity(absoluteTime);
        if (dynamicCollider.rotatingWall != null)
            velocity += dynamicCollider.rotatingWall.GetPredictedPointVelocity(worldPoint);
        velocity.y = 0f;
        return velocity;
    }

    bool TryGetPreviewHit(Vector3 origin, float radius, Vector3 direction,
                          float distance, Collider previousCollider,
                          out RaycastHit bestHit, out Vector3 bestPlanarNormal)
    {
        bestHit = default;
        bestPlanarNormal = Vector3.zero;
        float bestDistance = float.PositiveInfinity;

        int count = Physics.SphereCastNonAlloc(origin, radius, direction, _previewHits,
            distance, previewMask, QueryTriggerInteraction.Ignore);

        // First find the nearest valid travel distance. SphereCastNonAlloc does
        // not guarantee result order, so relying on the first result made exact
        // corner shots intermittently choose a different wall.
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = _previewHits[i];
            if (!TryGetValidPreviewNormal(candidate, direction, previousCollider,
                                          out Vector3 planarNormal))
                continue;

            if (candidate.distance < bestDistance)
            {
                bestDistance = candidate.distance;
                bestHit = candidate;
                bestPlanarNormal = planarNormal;
            }
        }

        if (bestHit.collider == null) return false;

        // A puck can reach two faces at the same instant at an exact corner.
        // PhysX resolves both contacts, while reflecting off one arbitrary hit
        // makes the line depend on query ordering. Weighting each coincident
        // face by approach speed produces a stable combined contact normal.
        Vector3 combinedNormal = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = _previewHits[i];
            if (candidate.distance > bestDistance + PreviewCoincidentHitTolerance)
                continue;
            if (!TryGetValidPreviewNormal(candidate, direction, previousCollider,
                                          out Vector3 planarNormal))
                continue;

            combinedNormal = AccumulatePlanarContactNormal(
                direction, combinedNormal, planarNormal);
        }

        if (combinedNormal.sqrMagnitude > 0.0001f)
            bestPlanarNormal = combinedNormal.normalized;
        return true;
    }

    bool TryGetValidPreviewNormal(RaycastHit candidate, Vector3 direction,
                                  Collider previousCollider, out Vector3 planarNormal)
    {
        planarNormal = Vector3.zero;
        Collider candidateCollider = candidate.collider;
        if (candidateCollider == null) return false;
        if (candidateCollider == _puckCollider || candidate.rigidbody == _rb) return false;
        // Moving/rotating colliders are tested at their future poses by
        // TryGetPredictedDynamicPreviewHit. Their current cast hits are stale.
        if (_previewDynamicColliderSet.Contains(candidateCollider)) return false;

        planarNormal = candidate.normal;
        planarNormal.y = 0f;
        if (planarNormal.sqrMagnitude < 0.0001f) return false; // floor/ceiling
        planarNormal.Normalize();

        float approachDot = Vector3.Dot(direction, planarNormal);
        if (approachDot >= -0.0001f) return false;

        // The small post-bounce offset can still overlap the surface just hit.
        // Never let that zero-distance repeat consume another bounce segment.
        if (candidateCollider == previousCollider
            && candidate.distance <= PreviewSurfaceSeparation * 2f)
            return false;

        return true;
    }

    /// <summary>
    /// Adds one approaching face to an order-independent corner normal.
    /// Public only so editor validation can lock down coincident-hit behavior.
    /// </summary>
    public static Vector3 AccumulatePlanarContactNormal(
        Vector3 direction, Vector3 accumulatedNormal, Vector3 candidateNormal)
    {
        direction.y = 0f;
        candidateNormal.y = 0f;
        if (direction.sqrMagnitude < 0.0001f
            || candidateNormal.sqrMagnitude < 0.0001f)
            return accumulatedNormal;

        direction.Normalize();
        candidateNormal.Normalize();
        float approachWeight = -Vector3.Dot(direction, candidateNormal);
        if (approachWeight <= 0.0001f) return accumulatedNormal;
        return accumulatedNormal + candidateNormal * approachWeight;
    }

    Vector3 ResolvePreviewCollisionVelocity(Vector3 incomingVelocity, Vector3 planarNormal,
                                             RaycastHit hit, Vector3 centerAtImpact)
    {
        Vector3 surfaceVelocity = Vector3.zero;
        if (hit.rigidbody != null)
        {
            surfaceVelocity = hit.rigidbody.GetPointVelocity(centerAtImpact);
            surfaceVelocity.y = 0f;
        }

        return ResolvePreviewCollisionVelocity(
            incomingVelocity, planarNormal, hit.collider,
            surfaceVelocity, centerAtImpact);
    }

    Vector3 ResolvePreviewCollisionVelocity(
        Vector3 incomingVelocity, Vector3 planarNormal, Collider hitCollider,
        Vector3 surfaceVelocity, Vector3 centerAtImpact)
    {

        GetCombinedPreviewMaterialResponse(hitCollider,
            out float restitution, out float dynamicFriction);

        // PhysX suppresses restitution below this relative normal speed. This
        // matters most for shallow/glancing shots, where drawing a full mirror
        // bounce used to disagree with the puck sliding along the rail.
        Vector3 relativeIncoming = incomingVelocity - surfaceVelocity;
        float approachSpeed = -Vector3.Dot(relativeIncoming, planarNormal);
        if (approachSpeed < Physics.bounceThreshold)
            restitution = 0f;

        Vector3 outgoing = CalculatePlanarBounceVelocity(
            incomingVelocity, planarNormal, surfaceVelocity,
            restitution, dynamicFriction);

        // These solid mechanics deliberately replace/adjust the normal engine
        // rebound. Mirror their final direction and speed in the preview only.
        KickerBumper kicker = hitCollider != null
            ? hitCollider.GetComponentInParent<KickerBumper>() : null;
        if (kicker != null)
        {
            Vector3 radial = centerAtImpact - kicker.transform.position;
            radial.y = 0f;
            if (radial.sqrMagnitude > 0.0001f)
            {
                float speed = Mathf.Min(
                    Mathf.Max(incomingVelocity.magnitude * kicker.restitutionGain, kicker.kickSpeed),
                    kicker.maxSpeed);
                outgoing = radial.normalized * speed;
            }
        }
        else
        {
            BankingRail bankingRail = hitCollider != null
                ? hitCollider.GetComponentInParent<BankingRail>() : null;
            if (bankingRail != null && outgoing.sqrMagnitude > 0.0001f)
                outgoing = outgoing.normalized * Mathf.Min(
                    incomingVelocity.magnitude * bankingRail.bankBoost, bankingRail.maxSpeed);

            RimRebound rim = hitCollider != null
                ? hitCollider.GetComponentInParent<RimRebound>() : null;
            if (rim != null && outgoing.sqrMagnitude > 0.0001f)
                outgoing = outgoing.normalized * Mathf.Min(
                    incomingVelocity.magnitude * rim.speedKeep, rim.maxSpeed);
        }

        return outgoing;
    }

    void GetCombinedPreviewMaterialResponse(Collider surfaceCollider,
                                             out float restitution,
                                             out float dynamicFriction)
    {
        // Unity's collider-with-no-material defaults. The puck material uses
        // Minimum friction and Maximum bounce, so these combine to the same
        // coefficients PhysX uses for the real contact.
        const float DefaultDynamicFriction = 0.6f;
        const float DefaultBounciness = 0f;

        PhysicsMaterial puckMaterial = _puckCollider != null
            ? _puckCollider.sharedMaterial : null;
        PhysicsMaterial surfaceMaterial = surfaceCollider != null
            ? surfaceCollider.sharedMaterial : null;

        float puckBounce = puckMaterial != null
            ? puckMaterial.bounciness : DefaultBounciness;
        float surfaceBounce = surfaceMaterial != null
            ? surfaceMaterial.bounciness : DefaultBounciness;
        PhysicsMaterialCombine bounceMode = SelectMaterialCombineMode(
            puckMaterial != null ? puckMaterial.bounceCombine : PhysicsMaterialCombine.Average,
            surfaceMaterial != null ? surfaceMaterial.bounceCombine : PhysicsMaterialCombine.Average);
        restitution = CombineMaterialValue(puckBounce, surfaceBounce, bounceMode);

        float puckFriction = puckMaterial != null
            ? puckMaterial.dynamicFriction : DefaultDynamicFriction;
        float surfaceFriction = surfaceMaterial != null
            ? surfaceMaterial.dynamicFriction : DefaultDynamicFriction;
        PhysicsMaterialCombine frictionMode = SelectMaterialCombineMode(
            puckMaterial != null ? puckMaterial.frictionCombine : PhysicsMaterialCombine.Average,
            surfaceMaterial != null ? surfaceMaterial.frictionCombine : PhysicsMaterialCombine.Average);
        dynamicFriction = CombineMaterialValue(puckFriction, surfaceFriction, frictionMode);
    }

    static PhysicsMaterialCombine SelectMaterialCombineMode(
        PhysicsMaterialCombine first, PhysicsMaterialCombine second)
    {
        return GetMaterialCombinePriority(first) >= GetMaterialCombinePriority(second)
            ? first : second;
    }

    static int GetMaterialCombinePriority(PhysicsMaterialCombine mode)
    {
        // PhysX precedence is Maximum > Multiply > Minimum > Average. Enum
        // numeric values do not follow that order, so keep it explicit.
        switch (mode)
        {
            case PhysicsMaterialCombine.Maximum:  return 3;
            case PhysicsMaterialCombine.Multiply: return 2;
            case PhysicsMaterialCombine.Minimum:  return 1;
            default:                              return 0;
        }
    }

    static float CombineMaterialValue(float first, float second,
                                      PhysicsMaterialCombine mode)
    {
        switch (mode)
        {
            case PhysicsMaterialCombine.Minimum:  return Mathf.Min(first, second);
            case PhysicsMaterialCombine.Multiply: return first * second;
            case PhysicsMaterialCombine.Maximum:  return Mathf.Max(first, second);
            default:                              return (first + second) * 0.5f;
        }
    }

    /// <summary>
    /// Calculates the direction a Y-locked puck takes after hitting a surface.
    /// Surface velocity is included so moving rails predict from relative
    /// motion rather than reflecting against a stale, stationary wall.
    /// Public for deterministic editor validation; it does not change physics.
    /// </summary>
    public static Vector3 CalculatePlanarBounceVelocity(
        Vector3 incomingVelocity, Vector3 collisionNormal, Vector3 surfaceVelocity)
    {
        return CalculatePlanarBounceVelocity(
            incomingVelocity, collisionNormal, surfaceVelocity, 1f, 0f);
    }

    /// <summary>
    /// Material-aware overload used by the live preview. Restitution changes
    /// the normal rebound and dynamic friction reduces tangential travel using
    /// the same Coulomb impulse limit as a sliding PhysX contact.
    /// </summary>
    public static Vector3 CalculatePlanarBounceVelocity(
        Vector3 incomingVelocity, Vector3 collisionNormal, Vector3 surfaceVelocity,
        float restitution, float dynamicFriction)
    {
        incomingVelocity.y = 0f;
        surfaceVelocity.y = 0f;
        collisionNormal.y = 0f;
        if (collisionNormal.sqrMagnitude < 0.0001f) return incomingVelocity;
        collisionNormal.Normalize();

        Vector3 relativeVelocity = incomingVelocity - surfaceVelocity;
        float normalSpeed = Vector3.Dot(relativeVelocity, collisionNormal);
        if (normalSpeed < 0f)
        {
            restitution = Mathf.Clamp01(restitution);
            dynamicFriction = Mathf.Max(0f, dynamicFriction);

            float approachSpeed = -normalSpeed;
            Vector3 tangentVelocity = relativeVelocity - collisionNormal * normalSpeed;
            float tangentSpeed = tangentVelocity.magnitude;
            float frictionSpeedLoss = dynamicFriction * (1f + restitution) * approachSpeed;
            float outgoingTangentSpeed = Mathf.Max(0f, tangentSpeed - frictionSpeedLoss);

            relativeVelocity = collisionNormal * (approachSpeed * restitution);
            if (tangentSpeed > 0.0001f && outgoingTangentSpeed > 0f)
                relativeVelocity += tangentVelocity * (outgoingTangentSpeed / tangentSpeed);
        }

        Vector3 result = relativeVelocity + surfaceVelocity;
        result.y = 0f;
        return result;
    }

    Vector3 CalculateLaunchVelocity(Vector3 drag)
    {
        // ForceMode.Impulse changes velocity by impulse / mass. Keeping this
        // calculation in one place guarantees preview and gameplay use the
        // same mass-aware launch speed and the same safety cap.
        float mass = _rb != null ? Mathf.Max(0.0001f, _rb.mass) : 1f;
        Vector3 velocity = (-drag * forceMultiplier) / mass;
        velocity.y = 0f;
        if (velocity.magnitude > maxLaunchSpeed)
            velocity = velocity.normalized * maxLaunchSpeed;
        return velocity;
    }

    Vector3 GetCommittedLaunchVelocity(Vector3 currentDrag)
    {
        Vector3 fallback = CalculateLaunchVelocity(currentDrag);
        if (!_hasDisplayedPreviewLaunch) return fallback;

        // Do not reuse a preview belonging to an older puck pose. Normal
        // sub-threshold resting drift is below this allowance; a reset,
        // teleport or level swap invalidates it and falls back safely.
        float originTolerance = Mathf.Max(
            0.02f, stopThreshold * Mathf.Max(Time.fixedDeltaTime, 0.02f) * 2f);
        if (Vector3.SqrMagnitude(GetTrajectoryOrigin() - _displayedPreviewOrigin)
            > originTolerance * originTolerance)
            return fallback;

        return _displayedPreviewLaunchVelocity;
    }

    void ApplyLaunchVelocity(Vector3 launchVelocity)
    {
        if (_rb == null) return;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.linearVelocity = launchVelocity;
        _rb.WakeUp();
    }

    void Launch()
    {
        Vector3 drag = GetDragVector();
        if (drag.magnitude <= 0.15f)
        {
            EndDrag();
            return;
        }

        Vector3 launchVelocity = GetCommittedLaunchVelocity(drag);
        EndDrag();

        // Slingshot: pull BACK and the puck flies forward. Commit to the last
        // trajectory the player saw instead of an unrendered mouse-up position.
        ApplyLaunchVelocity(launchVelocity);

        // Audio: launch sound, pitch scales with shot power
        if (AudioManager.Instance != null)
        {
            float power = Mathf.Clamp01(drag.magnitude / maxDragDistance);
            AudioManager.Instance.PlaySfx(AudioManager.Instance.puckLaunchSfx,
                volume: 0.7f + 0.3f * power,
                pitch: 0.9f + 0.3f * power);
        }
    }

    void EndDrag()
    {
        _isDragging = false;
        SetFineAimVisual(false);
        if (aimLine != null) aimLine.enabled = false;
        HideTrajectoryPreview();
        if (powerArc != null)
        {
            powerArc.enabled = false;
            powerArc.positionCount = 0;
        }
    }

    void SetFineAimVisual(bool active)
    {
        if (aimLine == null || !_aimLineColorsCached || _fineAimVisualActive == active)
            return;

        _fineAimVisualActive = active;
        if (active)
        {
            // A restrained green tint gives immediate cabinet feedback without
            // adding another large UI panel or obscuring the course.
            aimLine.startColor = new Color(0.34f, 1f, 0.55f, _aimLineDefaultStart.a);
            aimLine.endColor = new Color(0.20f, 0.90f, 0.42f, _aimLineDefaultEnd.a);
        }
        else
        {
            aimLine.startColor = _aimLineDefaultStart;
            aimLine.endColor = _aimLineDefaultEnd;
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
        ResetInputState();
    }

    /// <summary>
    /// Clears mouse drag, arcade aim, charge and cached trajectory state. Called
    /// by LevelManager on every level load/restart so no input from the discarded
    /// attempt can launch the freshly positioned puck.
    /// </summary>
    public void ResetInputState()
    {
        EndDrag();
        _arcadePower = 0f;
        _arcadeAimDir = Vector3.zero;
        _arcadeFireWasHeld = false;
        _arcadeAimAngleDegrees = 0f;
        _arcadeAimInitialized = false;
        ResetVerticalAimLatch();
        _blockArcadeFireUntilRelease = ReadArcadeFireHeld();
        _nextArcadePreviewTime = 0f;
        _lastArcadePreviewPower = -1f;
        _lastArcadePreviewAim = Vector3.positiveInfinity;
        _lastArcadePreviewOrigin = Vector3.positiveInfinity;
    }
}
