using UnityEngine;

/// <summary>Animates pre-authored surface glints; never creates meshes, materials or objects.</summary>
public sealed class ToyBoxSurfaceShine : MonoBehaviour
{
    public Transform[] glints;
    public Transform[] anchors;
    public Vector3[] offsets;
    public Camera viewCamera;
    float elapsed;

    void OnEnable() { elapsed=0; Sample(0); }
    void Update() { elapsed+=Time.unscaledDeltaTime; Sample(elapsed); }

    public void Sample(float seconds)
    {
        if(glints==null || anchors==null || offsets==null || viewCamera==null) return;
        for(int i=0;i<glints.Length;i++)
        {
            float phase=Mathf.Repeat(seconds+i*1.13f,11f);
            float amount=phase<1.6f?Mathf.Pow(Mathf.Sin(phase/1.6f*Mathf.PI),2):0;
            glints[i].position=anchors[i].TransformPoint(offsets[i]);
            glints[i].rotation=viewCamera.transform.rotation*Quaternion.AngleAxis(phase*24,Vector3.forward);
            glints[i].localScale=Vector3.one*(amount*.17f);
        }
    }
}
