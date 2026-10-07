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

    static readonly System.Collections.Generic.Dictionary<Rigidbody, System.Collections.Generic.HashSet<IcePatch>> active = new();
    readonly System.Collections.Generic.Dictionary<Collider,Rigidbody> contacts = new();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry() => active.Clear();
    void OnTriggerEnter(Collider other) => Track(other);
    void OnTriggerStay(Collider other) => Track(other);
    void Track(Collider other)
    {
        var rb=other.attachedRigidbody;
        if(rb==null || (rb.GetComponent<PuckController>()==null && rb.GetComponent<TutorialPracticePuck>()==null))return;
        contacts[other]=rb;
        if(!active.TryGetValue(rb,out var patches)){patches=new();active.Add(rb,patches);}
        patches.Add(this);ApplyDamping(rb,patches);
    }
    static void ApplyDamping(Rigidbody rb,System.Collections.Generic.HashSet<IcePatch> patches)
    {
        float damping=0;
        foreach(var patch in patches)if(patch!=null)damping=Mathf.Max(damping,patch.patchDamping);
        if(rb!=null)rb.linearDamping=damping;
    }
    void OnTriggerExit(Collider other)
    {
        if(!contacts.TryGetValue(other,out var rb))return;
        contacts.Remove(other);
        foreach(var remaining in contacts.Values)if(remaining==rb)return;
        Release(rb);
    }
    void Release(Rigidbody rb)
    {
        if(rb==null || !active.TryGetValue(rb,out var patches))return;
        patches.Remove(this);
        if(patches.Count==0){rb.linearDamping=restoreDamping;active.Remove(rb);}
        else ApplyDamping(rb,patches);
    }
    void OnDisable()
    {
        foreach(var rb in new System.Collections.Generic.HashSet<Rigidbody>(contacts.Values))Release(rb);
        contacts.Clear();
    }
}
