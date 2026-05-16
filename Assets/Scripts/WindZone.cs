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

    Rigidbody _puckRb;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (_puckRb == null || _puckRb.gameObject != other.gameObject)
        {
            if (other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.GetComponent<PuckController>() == null) return;
            _puckRb = other.attachedRigidbody;
        }
        Vector3 f = transform.forward * forceMagnitude;
        if (addLift) f += Vector3.up * (forceMagnitude * 0.15f);
        _puckRb.AddForce(f, ForceMode.Acceleration);

        // Audio: drive the wind loop while puck is inside
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetWindLoopActive(true, Mathf.Clamp01(forceMagnitude / 12f));
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        if (other.attachedRigidbody.GetComponent<PuckController>() == null) return;
        _puckRb = null;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetWindLoopActive(false);
    }
}
