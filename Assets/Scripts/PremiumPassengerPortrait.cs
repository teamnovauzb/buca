using UnityEngine;

/// <summary>Detailed passenger artwork remains readable as its 3D carriage rounds the map.</summary>
public sealed class PremiumPassengerPortrait : MonoBehaviour
{
    public Camera renderingCamera;
    Vector3 authoredScale;
    bool scaleCaptured;
    public void FaceCamera()
    {
        if(renderingCamera==null||transform.parent==null)return;
        // Keep the passenger upright. Copying the camera's downward pitch laid
        // the upper body across the rear wall, which could cut through its face.
        Vector3 facing=Vector3.ProjectOnPlane(renderingCamera.transform.forward,Vector3.up);
        if(facing.sqrMagnitude>.0001f)transform.rotation=Quaternion.LookRotation(facing,Vector3.up);
        // An upright portrait is shortened vertically by the elevated train camera.
        // Compensate that projection without tilting the seated animal into the wagon.
        if(!scaleCaptured){authoredScale=transform.localScale;scaleCaptured=true;}
        float verticalProjection=Mathf.Max(.5f,Mathf.Abs(Vector3.Dot(transform.up,renderingCamera.transform.up)));
        transform.localScale=new Vector3(authoredScale.x,authoredScale.y/verticalProjection,authoredScale.z);
        var sprite=GetComponent<SpriteRenderer>().sprite;
        // Anchor the seated body, rather than the padded bottom of the atlas cell,
        // to the middle of the wagon floor. Keep this true through every turn.
        Vector2 seatPixels=sprite.name switch
        {
            "rabbit"=>new Vector2(287.5f,58),
            "fox"=>new Vector2(254.5f,60),
            "bear"=>new Vector2(267.5f,58),
            "panda"=>new Vector2(281.5f,76),
            "penguin"=>new Vector2(281.5f,85),
            _=>new Vector2(256,65)
        };
        Vector2 offset=(seatPixels-sprite.pivot)/sprite.pixelsPerUnit;
        transform.position=transform.parent.TransformPoint(new Vector3(0,.98f,0))
            -transform.TransformVector(new Vector3(offset.x,offset.y,0));
    }
    void LateUpdate()=>FaceCamera();
}
