using UnityEngine;

/// <summary>Operates the saved paint workshop; no runtime geometry or materials.</summary>
public sealed class ToyPaintSkinSelector : MonoBehaviour
{
    public ToyBoxMenuController menu;
    public Camera viewCamera;
    public ToyBoxMenuController.PuckButton[] buttons;
    public BucaSkinBinding board, puck;
    public GameObject[] potRings;
    public Vector3[] framingPoints;
    public int PreviewIndex { get; private set; }
    public int FocusIndex { get; private set; }
    float nextMove;
    float fittedAspect=-1;
    bool waitRelease;
    Vector3 lastPointer;
    public void Open()
    {
        PreviewIndex=BucaSkinBinding.Selected;FocusIndex=PreviewIndex;
        waitRelease=true;lastPointer=Input.mousePosition;nextMove=0;
        Focus(PreviewIndex);Fit((float)Screen.width/Mathf.Max(1,Screen.height));
    }
    public void Preview(int index)
    {
        PreviewIndex=Mathf.Clamp(index,0,4);board.Apply(PreviewIndex);puck.Apply(PreviewIndex);
        for(int i=0;i<5;i++)potRings[i].SetActive(i==PreviewIndex);
    }
    public void Focus(int index)
    {
        FocusIndex=Mathf.Clamp(index,0,6);
        if(FocusIndex<5)Preview(FocusIndex);
        for(int i=0;i<buttons.Length;i++)buttons[i].selectedRing.SetActive(i==FocusIndex);
    }
    public void Activate()
    {
        if(FocusIndex==5)menu.ClosePaintWorkshop();
        else if(FocusIndex==6){BucaSkinBinding.Select(PreviewIndex);menu.ClosePaintWorkshop();}
        else
        {
            Preview(FocusIndex);
            BucaSkinBinding.Select(PreviewIndex);
        }
        if(AudioManager.Instance!=null)AudioManager.Instance.PlayButtonClick();
    }
    public void Move(Vector2 direction)
    {
        if(Mathf.Abs(direction.y)>Mathf.Abs(direction.x))
            Focus(direction.y<0?(FocusIndex<3?5:6):(FocusIndex==5?0:FocusIndex==6?4:FocusIndex));
        else if(FocusIndex<5)Focus(Mathf.Clamp(FocusIndex+(direction.x>0?1:-1),0,4));
        else Focus(FocusIndex==5?6:5);
    }
    public void Fit(float aspect)
    {
        if(framingPoints==null||framingPoints.Length==0)return;
        if(Mathf.Abs(fittedAspect-aspect)<.0001f)return;fittedAspect=aspect;
        // Framing points are authored relative to the workshop, which is offset from the main menu.
        var cameraRotation=Quaternion.Inverse(transform.rotation)*viewCamera.transform.rotation;
        var rotation=Quaternion.Inverse(cameraRotation);
        var points=new Vector3[framingPoints.Length];float tan=Mathf.Tan(viewCamera.fieldOfView*Mathf.Deg2Rad*.5f),cameraZ=float.MaxValue;
        for(int i=0;i<points.Length;i++){points[i]=rotation*framingPoints[i];cameraZ=Mathf.Min(cameraZ,points[i].z-Mathf.Abs(points[i].x)/(tan*Mathf.Max(.2f,aspect)));}
        foreach(var a in points)foreach(var b in points)cameraZ=Mathf.Min(cameraZ,(a.z+b.z-(a.y-b.y)/tan)*.5f);
        cameraZ-=.65f;float minY=float.MinValue,maxY=float.MaxValue;
        foreach(var p in points){float halfHeight=(p.z-cameraZ)*tan;minY=Mathf.Max(minY,p.y-halfHeight);maxY=Mathf.Min(maxY,p.y+halfHeight);}
        viewCamera.transform.position=transform.TransformPoint(cameraRotation*new Vector3(0,(minY+maxY)*.5f,cameraZ));
    }

    void Update()
    {
        viewCamera.rect=menu.menuCamera.rect;
        Fit((float)Screen.width*viewCamera.rect.width/Mathf.Max(1,Screen.height*viewCamera.rect.height));
        if(waitRelease)
        {
            if(!ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)&&!Input.GetKey(KeyCode.Return)&&!Input.GetMouseButton(0))waitRelease=false;
            return;
        }
        if(ArcadeInputAdapter.CancelDown()||Input.GetKeyDown(KeyCode.Escape)){menu.ClosePaintWorkshop();return;}
        var stick=ArcadeInputAdapter.GetStick();
        if(Input.GetKey(KeyCode.LeftArrow))stick.x=-1;if(Input.GetKey(KeyCode.RightArrow))stick.x=1;
        if(Input.GetKey(KeyCode.UpArrow))stick.y=1;if(Input.GetKey(KeyCode.DownArrow))stick.y=-1;
        if(stick.sqrMagnitude<.2f)nextMove=0;
        else if(Time.unscaledTime>=nextMove){Move(stick);nextMove=Time.unscaledTime+.22f;}
        bool click=Input.GetMouseButtonDown(0);
        if(click||(Input.mousePosition-lastPointer).sqrMagnitude>.5f)
        {
            lastPointer=Input.mousePosition;var ray=viewCamera.ScreenPointToRay(lastPointer);
            float near=float.MaxValue;int picked=-1;
            for(int i=0;i<buttons.Length;i++)if(buttons[i].hit.Raycast(ray,out var hit,150)&&hit.distance<near){near=hit.distance;picked=i;}
            if(picked>=0){Focus(picked);if(click){Activate();return;}}
        }
        if(ArcadeInputAdapter.ConfirmDown()||Input.GetKeyDown(KeyCode.Return)){Activate();return;}
        for(int i=0;i<buttons.Length;i++)buttons[i].cap.localPosition=Vector3.Lerp(buttons[i].cap.localPosition,buttons[i].restPosition+(i==FocusIndex?Vector3.up*.065f:Vector3.zero),1-Mathf.Exp(-14*Time.unscaledDeltaTime));
    }
}
