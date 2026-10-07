using UnityEngine;

/// <summary>Displays live game values by selecting pre-baked solid digit meshes.</summary>
[DefaultExecutionOrder(-100)]
public sealed class ToyBoxGameplayHud : MonoBehaviour
{
    public LevelManager manager;
    public Mesh[] digitMeshes;
    public MeshFilter[] levelDigits, timeDigits, strokeDigits, parDigits;
    public Renderer[] timeFaces;
    public Material normalTime, urgentTime;
    public GameObject hints;
    public Collider joystickHit, shootHit;
    public static bool PanelInputActive { get; private set; }
    public static Vector2 PanelStick { get; private set; }
    public static bool PanelShoot { get; private set; }
    public static bool PanelUndo { get; private set; }
    public Collider undoHit;
    public Transform undoCap;
    public GameObject undoHighlight;
    Vector3 undoRest;
    Vector2 dragOrigin;
    bool draggingStick, holdingShoot, holdingUndo;
    void ReadPanelInput()
    {
        if(manager==null || !manager.GameplayInputAllowed || Camera.main==null) {PanelStick=Vector2.zero;PanelShoot=false;PanelUndo=false;draggingStick=false;holdingShoot=false;holdingUndo=false;return;}
        HandlePanelPointer(Camera.main.ScreenPointToRay(Input.mousePosition),Input.mousePosition,
            Input.GetMouseButtonDown(0),Input.GetMouseButton(0));
    }
    public void HandlePanelPointer(Ray ray,Vector2 position,bool pressed,bool held)
    {
        if(pressed)
        {
            draggingStick=joystickHit!=null && joystickHit.Raycast(ray,out var a,100);
            holdingShoot=!draggingStick && shootHit!=null && shootHit.Raycast(ray,out var b,100);
            holdingUndo=!draggingStick && !holdingShoot && undoHit!=null && undoHit.Raycast(ray,out var d,100);
            // Keep panel mode through release so charging can finish, but let
            // the next click on the board return to mouse slingshot aiming.
            PanelInputActive=draggingStick || holdingShoot || holdingUndo;
            if(PanelInputActive)dragOrigin=position;
        }
        if(!held){draggingStick=false;holdingShoot=false;holdingUndo=false;}
        PanelStick=draggingStick?Vector2.ClampMagnitude((position-dragOrigin)/70f,1):Vector2.zero;
        PanelShoot=holdingShoot;
        PanelUndo=holdingUndo;
    }
    public Transform shootCap;
    public GameObject shootHighlight;
    Vector3 shootRest;
    // Pull back to aim forward. Menu navigation keeps its normal direction.
    public static Vector2 MapGameplayStick(Vector2 stick) => new Vector2(stick.x, -stick.y);
    public Transform joystickPivot, joystickDirectionDot;
    public GameObject unlimitedShotsLabel, limitedLivesLabel;
    public MeshFilter[] livesLeftDigits, livesTotalDigits;
    public GameObject[] heartSlots, heartFills;
    public GameObject numericMoves;
    public void RefreshHearts(int remaining,int maximum)
    {
        bool numeric=maximum>5 && numericMoves!=null && livesLeftDigits!=null && livesLeftDigits.Length>0;
        if(numericMoves!=null)numericMoves.SetActive(numeric);
        if(numeric)Show(livesLeftDigits,remaining);
        if(heartSlots==null || heartFills==null) return;
        float spacing=Mathf.Min(.62f,3.1f/Mathf.Max(1,maximum));
        for(int i=0;i<heartSlots.Length;i++)
        {
            heartSlots[i].SetActive(!numeric && i<maximum);
            heartSlots[i].transform.localScale=new Vector3(Mathf.Min(1,spacing/.62f),1,1);
            heartSlots[i].transform.localPosition=new Vector3((i-(maximum-1)*.5f)*spacing,0,-.15f);
            if(i<heartFills.Length) heartFills[i].SetActive(i<remaining && i<maximum);
        }
    }
    Vector2 visualStick;
    void Awake() { if(shootCap!=null) shootRest=shootCap.localPosition; if(undoCap!=null) undoRest=undoCap.localPosition; }
    void Update() => ReadPanelInput();
    // Only moves the authored shaft/ball pivot; the socket and meshes stay fixed.
    void AnimateJoystick(Vector2 input, float deltaTime)
    {
        if(joystickPivot==null) return;
        input=Vector2.ClampMagnitude(input,1f);
        visualStick=Vector2.Lerp(visualStick,input,1f-Mathf.Exp(-22f*Mathf.Max(0,deltaTime)));
        if(input==Vector2.zero && visualStick.sqrMagnitude<.00001f) visualStick=Vector2.zero;
        float lean=Mathf.Tan(38f*Mathf.Deg2Rad);
        if(joystickDirectionDot!=null){joystickDirectionDot.gameObject.SetActive(visualStick.sqrMagnitude>.01f);joystickDirectionDot.localPosition=new Vector3(visualStick.x*.42f,.14f,visualStick.y*.42f);}
        joystickPivot.localRotation=Quaternion.FromToRotation(Vector3.up,
            new Vector3(visualStick.x*lean,1f,visualStick.y*lean).normalized);
    }
    void OnDisable()
    {
        PanelInputActive=false; PanelStick=Vector2.zero; PanelShoot=false;  PanelUndo=false;
        draggingStick=false;holdingShoot=false;holdingUndo=false;
        visualStick=Vector2.zero;
        if(joystickPivot!=null) joystickPivot.localRotation=Quaternion.identity;
        if(shootHighlight!=null) shootHighlight.SetActive(false);
        if(shootCap!=null) shootCap.localPosition=shootRest;
        if(undoHighlight!=null) undoHighlight.SetActive(false);
        if(undoCap!=null) undoCap.localPosition=undoRest;
        if(joystickDirectionDot!=null)joystickDirectionDot.gameObject.SetActive(false);
    }
    // Convert screen-space input into the authored model's local ground plane.
    Vector2 ReadJoystickVisualInput()
    {
        // The handle follows the hand; only PuckController reverses shot aiming.
        Vector2 stick=PanelStick.sqrMagnitude>.001f?PanelStick:ArcadeInputAdapter.GetStick();
        var camera=Camera.main;
        if(camera==null) return stick;
        Vector3 forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
        Vector3 right=Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized;
        Vector3 local=joystickPivot.parent.InverseTransformDirection(right*stick.x+forward*stick.y);
        return Vector2.ClampMagnitude(new Vector2(local.x,local.z),1f);
    }
    void LateUpdate()
    {
        // Keep the neutral shaft visually upright in the gameplay camera.
        // Only the moving pivot receives input tilt; the socket stays fixed.
        // The authored mount keeps the shaft upright relative to the physical pod.
        // Cabinet visuals follow the physical stick, not the retained shot heading.
        Vector2 direction=Vector2.zero;
        if(manager!=null && manager.GameplayInputAllowed && joystickPivot!=null)
        {
            var puck=manager.puckController;
            Vector3 aim=puck!=null ? puck.DisplayAimDirection : Vector3.zero;
            if(PanelInputActive || LuxoddGameBridge.IsArcadeInputActive)
                direction=ReadJoystickVisualInput();
            else if(aim.sqrMagnitude>.0001f)
            {
                Vector3 local=joystickPivot.parent.InverseTransformDirection(-aim); // mouse slingshot aim is opposite the hand movement
                direction=new Vector2(local.x,local.z).normalized;
            }
            else direction=ReadJoystickVisualInput();
        }
        AnimateJoystick(direction,Time.unscaledDeltaTime);
        bool shooting=manager!=null && manager.GameplayInputAllowed && manager.puckController!=null && manager.puckController.ShootControlHeld;
        if(shootHighlight!=null) shootHighlight.SetActive(shooting);
        if(shootCap!=null) shootCap.localPosition=Vector3.Lerp(shootCap.localPosition,shootRest+Vector3.forward*(shooting?.055f:0f),1f-Mathf.Exp(-25f*Time.unscaledDeltaTime));
        if(manager==null) return;
        bool undoHeld=PanelUndo || ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Green) || Input.GetKey(KeyCode.U);
        if(undoHighlight!=null) undoHighlight.SetActive(manager.CanUndoShot || manager.IsRewindingShot);
        if(undoCap!=null) undoCap.localPosition=undoRest+Vector3.forward*(undoHeld?.045f:0);
        if(unlimitedShotsLabel!=null) unlimitedShotsLabel.SetActive(!manager.useShotLives);
        if(limitedLivesLabel!=null) limitedLivesLabel.SetActive(manager.useShotLives);
        if(manager.useShotLives) RefreshHearts(manager.CurrentLives,manager.CurrentMaxLives);
        Show(levelDigits,manager.CurrentLevelNumber,true);
        Show(timeDigits,Mathf.CeilToInt(manager.CurrentTimeRemaining),true);
        Show(strokeDigits,manager.CurrentShotCount);
        Show(parDigits,manager.CurrentPar);
        foreach(var face in timeFaces) face.sharedMaterial=manager.CurrentTimeRemaining<=5?urgentTime:normalTime;
        if(hints!=null) hints.SetActive(!BoardCoachTutorial.IsActive && (manager.GameplayInputAllowed || manager.IsRewindingShot));
    }
    void Show(MeshFilter[] places,int value,bool padded=false)
    {
        value=Mathf.Max(0,value);
        int visible=1,remaining=value;
        while(remaining>=10 && visible<places.Length) { remaining/=10; visible++; }
        if(padded) visible=places.Length;
        for(int i=places.Length-1;i>=0;i--)
        {
            places[i].gameObject.SetActive(padded || value>0 || i==places.Length-1);
            places[i].transform.localPosition=Vector3.right*((i-(places.Length-visible)-(visible-1)*.5f)*places[i].transform.localScale.x*.85f);
            var mesh=digitMeshes[value%10]; value/=10;
            if(places[i].sharedMesh!=mesh) places[i].sharedMesh=mesh;
        }
    }
}
