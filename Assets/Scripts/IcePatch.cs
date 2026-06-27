using UnityEngine;

/// <summary>
/// NEW MECHANIC — Ice Patch (slick floor).
///
/// A flat trigger zone that drops the puck's drag while it's inside, so the puck
/// keeps almost all its speed and slides far (a shot you'd let coast instead
/// rockets on and overshoots). Set patchDamping HIGH instead and you get the
/// sticky "mud" twin. The puck's normal drag is restored when it leaves.
///
/// A shared reference count keeps OVERLAPPING patches and re-entries correct: the
/// puck's real drag is only restored once it has left the LAST patch. restoreDamping
/// is a baked constant (the puck Rigidbody's normal Linear Damping = 0.9), so the
/// restore is always exact — never a value accidentally captured while on ice.
///
/// Normal gameplay component on a prefab piece — not a runtime hook.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IcePatch : MonoBehaviour
{
    [Tooltip("Puck drag while on this surface. ~0.05 = slippery ice. Raise to ~6 for sticky mud.")]
    public float patchDamping = 0.05f;
    [Tooltip("Drag to restore on exit — the puck Rigidbody's normal Linear Damping (0.9).")]
    public float restoreDamping = 0.9f;

    static int _activeCount;     // how many ice/mud patches the puck is currently inside
    static Rigidbody _puck;
    bool _counted;               // did THIS patch count the puck?

    void OnTriggerEnter(Collider o)
    {
        var rb = o.attachedRigidbody;
        if (rb == null || rb.GetComponent<PuckController>() == null) return;
        if (_counted) return;
        _puck = rb;
        _activeCount++;
        _counted = true;
        rb.linearDamping = patchDamping;
    }

    void OnTriggerExit(Collider o)
    {
        var rb = o.attachedRigidbody;
        if (rb == null || rb.GetComponent<PuckController>() == null) return;
        Release(rb);
    }

    // If the level is unloaded (this object destroyed) while the puck is still on
    // the ice, restore drag so the puck isn't left slippery in the next level.
    void OnDisable()
    {
        if (_counted) Release(_puck);
    }

    void Release(Rigidbody rb)
    {
        if (!_counted) return;
        _counted = false;
        _activeCount = Mathf.Max(0, _activeCount - 1);
        if (_activeCount == 0 && rb != null) rb.linearDamping = restoreDamping;
    }
}
