using UnityEngine;

// Saved marker meshes follow the same sampled path as the shot preview.
[DefaultExecutionOrder(200)]
public sealed class TrajectoryMarkers : MonoBehaviour
{
    public LineRenderer path;
    public Transform[] arrows;
    public Transform endpoint;
    void LateUpdate()
    {
        bool visible=path!=null && path.enabled && path.positionCount>1;
        float length=0;
        if(visible)for(int i=1;i<path.positionCount;i++)length+=Vector3.Distance(path.GetPosition(i-1),path.GetPosition(i));
        visible &= length>.5f;
        foreach(var arrow in arrows)arrow.gameObject.SetActive(visible);
        endpoint.gameObject.SetActive(visible);
        if(!visible)return;
        for(int a=0;a<arrows.Length;a++)
        {
            float distance=length*(a+1)/(arrows.Length+1);
            for(int i=1;i<path.positionCount;i++)
            {
                Vector3 from=path.GetPosition(i-1),to=path.GetPosition(i);
                float segment=Vector3.Distance(from,to);
                if(distance<=segment || i==path.positionCount-1)
                {
                    arrows[a].position=Vector3.Lerp(from,to,segment>0?distance/segment:0)+Vector3.up*.025f;
                    Vector3 direction=Vector3.ProjectOnPlane(to-from,Vector3.up);
                    if(direction.sqrMagnitude>.00001f)arrows[a].rotation=Quaternion.LookRotation(direction,Vector3.up);
                    break;
                }
                distance-=segment;
            }
        }
        endpoint.position=path.GetPosition(path.positionCount-1)+Vector3.up*.02f;
    }
    void OnDisable(){foreach(var a in arrows)if(a!=null)a.gameObject.SetActive(false);if(endpoint!=null)endpoint.gameObject.SetActive(false);}
}
