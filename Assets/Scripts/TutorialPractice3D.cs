using UnityEngine;

/// <summary>A saved, isolated practice board using the real gameplay mechanic components.</summary>
public sealed class TutorialPractice3D : MonoBehaviour
{
    public string mechanic;
    public Rigidbody puck;
    public Transform arrow, goal;
    public Vector3 startPosition;
    public GameObject[] collectibles;
    public bool MechanicUsed { get; private set; }
    public bool Completed { get; private set; }
    public int Shots { get; private set; }
    WatchCopyTutorial3D _view;
    Camera _camera;
    float _yaw, _charge, _elapsed;
    bool _held, _ready, _dragging;
    Vector3 _dragStart;

    public void Begin(WatchCopyTutorial3D view, Camera camera)
    {
        _view=view; _camera=camera; Completed=false; Shots=0;
        gameObject.SetActive(true);
        ResetShot();
    }

    public void ResetShot()
    {
        puck.isKinematic=false; puck.linearVelocity=Vector3.zero; puck.angularVelocity=Vector3.zero;
        puck.position=transform.TransformPoint(startPosition); puck.linearDamping=.9f;
        MechanicUsed=false; _charge=0; _held=false; _ready=false; _dragging=false; _elapsed=0;
        _yaw=Quaternion.LookRotation(goal.position-puck.position).eulerAngles.y;
        foreach(var item in collectibles) if(item!=null) item.SetActive(true);
        arrow.gameObject.SetActive(true);
    }

    void Update()
    {
        if(_view==null || Completed) return;
        bool fire=_view.PointerFireHeld || Input.GetKey(KeyCode.Space) || ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
        if(!_ready) { if(!fire && !Input.GetMouseButton(0)) _ready=true; return; }
        _elapsed+=Time.unscaledDeltaTime;
        bool resting=puck.linearVelocity.sqrMagnitude<.08f;
        if(resting)
        {
            Vector2 stick=ArcadeInputAdapter.GetStick();
            if(Input.GetKey(KeyCode.LeftArrow)) stick.x=-1;
            if(Input.GetKey(KeyCode.RightArrow)) stick.x=1;
            _yaw+=stick.x*100f*Time.unscaledDeltaTime;
            if(Input.GetMouseButtonDown(0) && BoardPoint(out var point) && Vector3.Distance(point,puck.position)<1.2f)
            { _dragging=true; _dragStart=point; }
            if(_dragging && BoardPoint(out var drag))
            {
                Vector3 delta=_dragStart-drag; delta.y=0;
                if(delta.sqrMagnitude>.01f) _yaw=Quaternion.LookRotation(delta).eulerAngles.y;
                _charge=Mathf.Clamp01(delta.magnitude/3.5f);
                if(Input.GetMouseButtonUp(0)) { Launch(Quaternion.Euler(0,_yaw,0)*Vector3.forward,_charge); _dragging=false; }
            }
            if(!_dragging)
            {
                if(fire) _charge=Mathf.Clamp01(_charge+Time.unscaledDeltaTime/2f);
                if(_held && !fire) Launch(Quaternion.Euler(0,_yaw,0)*Vector3.forward,_charge);
            }
        }
        _held=fire;
        arrow.gameObject.SetActive(resting);
        arrow.position=puck.position+Vector3.up*.03f;
        arrow.rotation=Quaternion.Euler(0,_yaw,0);
        _view.ShowPracticeCharge(_charge);
        if(_elapsed>1f && (Mathf.Abs(transform.InverseTransformPoint(puck.position).x)>5.7f || Mathf.Abs(transform.InverseTransformPoint(puck.position).z)>6f)) ResetShot();
    }

    bool BoardPoint(out Vector3 point)
    {
        var plane=new Plane(Vector3.up,puck.position);
        Ray ray=_camera.ScreenPointToRay(Input.mousePosition);
        if(plane.Raycast(ray,out float distance)) { point=ray.GetPoint(distance); return true; }
        point=default; return false;
    }

    public void Launch(Vector3 direction,float power)
    {
        if(Completed || power<.05f) { _charge=0; return; }
        direction.y=0;
        if(direction.sqrMagnitude<.001f) return;
        puck.linearVelocity=direction.normalized*Mathf.Lerp(2f,14f,Mathf.Clamp01(power));
        Shots++; _elapsed=0; _charge=0;
    }

    public void UseMechanic() { if(Shots>0) MechanicUsed=true; }

    public void ReachGoal()
    {
        if(Completed || Shots==0) return;
        bool requiresUse=mechanic!="BASIC" && mechanic!="!" && mechanic!="MOVE" && mechanic!="SLIDE" && mechanic!="SPIN" && mechanic!="GATE";
        if(requiresUse && !MechanicUsed) { ResetShot(); return; }
        Completed=true; puck.linearVelocity=Vector3.zero; puck.isKinematic=true;
        arrow.gameObject.SetActive(false);
        _view?.CompletePractice();
    }

    public void StopPractice()
    {
        if(!puck.isKinematic) puck.linearVelocity=Vector3.zero;
        puck.isKinematic=true; _view=null;
        gameObject.SetActive(false);
    }
}
