using UnityEngine;

/// <summary>
/// Trigger volume that applies a continuous force to the puck while
/// it's inside. Use it as a side-wind that pushes shots off course,
/// or as a tailwind to make a long shot reach a far target.
///
/// Force direction = transform.forward × forceMagnitude.
///
/// Named BucaWindZone (not WindZone) to avoid colliding with
/// UnityEngine.WindZone in the global namespace.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BucaWindZone : MonoBehaviour
{
    [Tooltip("Force in m/s² applied along transform.forward while puck is inside.")]
    public float forceMagnitude = 8f;
    [Tooltip("If true, also applies a small upward component so the puck visibly drifts.")]
    public bool addLift = false;

    [Tooltip("Carry the puck along the arrows instead of applying ordinary wind.")]
    public bool conveyor;
    public float conveyorSpeed = 3f;

    // Shared with the aim preview so the displayed turn matches the actual belt.
    public Vector3 ConveyorVelocity(Vector3 incoming)
    {
        Vector3 direction=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
        return direction*Mathf.Max(.1f,conveyorSpeed)+Vector3.up*incoming.y;
    }

    readonly RaycastHit[] _beltHits=new RaycastHit[16];
    public bool ConveyorExitBlocked(Vector3 center,float radius)
    {
        Vector3 direction=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
        int count=Physics.SphereCastNonAlloc(center,Mathf.Max(.01f,radius-.01f),direction,
            _beltHits,Mathf.Max(.12f,conveyorSpeed*Time.fixedDeltaTime+.04f),~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++) {
            var hit=_beltHits[i];
            if(hit.collider==null || hit.collider.transform.IsChildOf(transform))continue;
            var body=hit.rigidbody;
            if(body!=null && (body.GetComponent<PuckController>()!=null || body.GetComponent<TutorialPracticePuck>()!=null))continue;
            if(Vector3.Dot(hit.normal,direction)<-.2f)return true;
        }
        return false;
    }
    void Carry(Rigidbody body)
    {
        var sphere=body.GetComponent<SphereCollider>();
        float radius=sphere!=null?sphere.radius*Mathf.Max(Mathf.Abs(body.transform.lossyScale.x),Mathf.Abs(body.transform.lossyScale.z)):.3f;
        // Stop before a solid exit obstacle instead of fighting its collision response.
        body.linearVelocity=ConveyorExitBlocked(body.worldCenterOfMass,radius)
            ? Vector3.up*body.linearVelocity.y : ConveyorVelocity(body.linearVelocity);
    }

    Rigidbody _puckRb;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var body=other.attachedRigidbody;
        if(body!=null && !body.isKinematic &&
           (body.GetComponent<PuckController>()!=null || body.GetComponent<TutorialPracticePuck>()!=null))
        {
            body.WakeUp();
            if(conveyor)Carry(body);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (_puckRb == null || _puckRb.gameObject != other.gameObject)
        {
            if (other.attachedRigidbody == null) return;
            if ((other.attachedRigidbody.GetComponent<PuckController>() == null && other.attachedRigidbody.GetComponent<TutorialPracticePuck>() == null)) return;
            _puckRb = other.attachedRigidbody;
        }
        if (_puckRb.isKinematic) return;
        Vector3 f = transform.forward * forceMagnitude;
        if (addLift) f += Vector3.up * (forceMagnitude * 0.15f);
        if(conveyor)Carry(_puckRb);
        else _puckRb.AddForce(f, ForceMode.Acceleration);

        // Audio: drive the wind loop while puck is inside
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetWindLoopActive(true, Mathf.Clamp01(forceMagnitude / 12f));
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        if ((other.attachedRigidbody.GetComponent<PuckController>() == null && other.attachedRigidbody.GetComponent<TutorialPracticePuck>() == null)) return;
        _puckRb = null;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetWindLoopActive(false);
    }
}
