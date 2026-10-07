using UnityEngine;

/// <summary>Moves a saved goal flag around the cup and returns pole impacts toward the puck.</summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class OrbitingGoalFlag : MonoBehaviour
{
    public Transform goal;
    public float radius = .7632f;
    public float cycleSeconds = 12f;
    public float phaseDegrees = 148.39f;
    public float minimumReturnSpeed = 3.2f;
    public float maximumReturnSpeed = 8f;
    Rigidbody _body;
    OrbitingHole _movingGoal;
    float _startTime, _height, _lastHit = -100f;
    public int ImpactCount { get; private set; }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _startTime = BoardMechanicClock.Time;
        _height = transform.position.y;
        if (goal != null && goal.parent != null)
            _movingGoal = goal.parent.GetComponentInChildren<OrbitingHole>();
    }

    void FixedUpdate()
    {
        if (goal == null) return;
        _body.MovePosition(GetPredictedPosition(BoardMechanicClock.Time));
    }

    public Vector3 GetPredictedPosition(float time)
    {
        Vector3 center = goal != null ? goal.position : transform.position;
        if (_movingGoal != null && _movingGoal.isActiveAndEnabled)
            center = _movingGoal.GetPredictedPosition(time);
        float angle = phaseDegrees * Mathf.Deg2Rad + (time - _startTime) * Mathf.PI * 2 / Mathf.Max(1f, cycleSeconds);
        return new Vector3(center.x + Mathf.Cos(angle) * radius, _height, center.z + Mathf.Sin(angle) * radius);
    }

    public Vector3 GetPredictedVelocity(float time)
    {
        const float step = .005f;
        return (GetPredictedPosition(time + step) - GetPredictedPosition(time - step)) / (2 * step);
    }

    public Vector3 ReturnVelocity(Vector3 incoming, Vector3 outwardNormal)
    {
        outwardNormal.y = 0; incoming.y = 0;
        if (outwardNormal.sqrMagnitude < .0001f) outwardNormal = -incoming;
        if (outwardNormal.sqrMagnitude < .0001f) return Vector3.zero;
        return outwardNormal.normalized * Mathf.Clamp(incoming.magnitude * .8f, minimumReturnSpeed, maximumReturnSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        var puck = collision.rigidbody;
        if (puck == null || puck.isKinematic || !puck.detectCollisions) return;
        if (puck.GetComponent<PuckController>() == null && puck.GetComponent<TutorialPracticePuck>() == null) return;
        if (LevelManager.Instance != null && !LevelManager.Instance.GameplayInputAllowed) return;
        if (Time.time - _lastHit < .2f) return;
        _lastHit = Time.time;
        Vector3 away = puck.position - _body.position;
        puck.WakeUp();
        puck.linearVelocity = ReturnVelocity(collision.relativeVelocity, away);
        ImpactCount++;
    }
}
