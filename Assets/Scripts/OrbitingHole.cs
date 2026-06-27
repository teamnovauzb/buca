using UnityEngine;

/// <summary>
/// NEW MECHANIC — Orbiting Hole (moving target).
///
/// Drifts the goal hole in a slow circle so you must LEAD your shot instead of
/// aiming dead-on. It is placed on an empty "Hole_Orbiter" object that is a
/// SIBLING of the existing "Hole" and "Hole_Ring" — it moves BOTH of them
/// together every physics step WITHOUT reparenting them. That matters: the
/// LevelManager finds "Hole"/"Hole_Ring" as DIRECT children for the magnet
/// assist, the ring pulse, and the win-burst, so leaving the parenting untouched
/// keeps all of those working (they read the live hole position automatically).
///
/// The injector also gives "Hole" a kinematic Rigidbody so its trigger sweeps
/// cleanly through the puck (MovePosition) and never misses a sink mid-orbit.
///
/// Normal gameplay component — not a runtime hook.
/// </summary>
public class OrbitingHole : MonoBehaviour
{
    [Tooltip("Orbit radius in world units.")]
    public float radius = 0.85f;
    [Tooltip("Seconds for one full loop. Bigger = slower / easier to read.")]
    public float cycleSeconds = 5f;
    [Tooltip("Starting angle offset (in turns) so different levels start in different spots.")]
    public float phase = 0f;

    Transform _hole, _ring;
    Rigidbody _holeRb;
    Vector3 _holeBase, _ringBase;
    bool _ready;

    void Awake()
    {
        var root = transform.parent;
        if (root == null) { enabled = false; return; }

        _hole = root.Find("Hole");
        _ring = root.Find("Hole_Ring");
        if (_hole == null) { enabled = false; return; }

        _holeBase = _hole.position;                 // captured BEFORE any movement
        if (_ring != null) _ringBase = _ring.position;
        _holeRb = _hole.GetComponent<Rigidbody>();
        _ready = true;
    }

    void FixedUpdate()
    {
        if (!_ready) return;

        float t = (Time.time / Mathf.Max(0.1f, cycleSeconds) + phase) * Mathf.PI * 2f;
        Vector3 off = new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * radius;

        Vector3 holePos = _holeBase + off;
        if (_holeRb != null && _holeRb.isKinematic) _holeRb.MovePosition(holePos);  // sweep the trigger
        else _hole.position = holePos;

        if (_ring != null) _ring.position = _ringBase + off;                         // ring follows in lockstep
    }
}
