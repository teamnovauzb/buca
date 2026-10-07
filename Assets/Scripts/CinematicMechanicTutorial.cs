using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>Shared cinematic playback for the campaign's saved mechanic demonstrations.</summary>
[DefaultExecutionOrder(600)]
public sealed class CinematicMechanicTutorial : MonoBehaviour
{
    WatchCopyTutorial3D owner;
    WatchCopyTutorial3D.Lesson lesson;
    BoardCoachTutorial ui;
    Transform puck;
    Vector3 start, cameraRest;
    Quaternion rotationRest;
    float fovRest, time, entrance;
    bool paused, neutral=true, closed, exiting;
    readonly Dictionary<Renderer,bool> hidden=new Dictionary<Renderer,bool>();
    public float PlaybackTime=>time;
    public float Duration=>lesson.key=="BASIC"?6:12;
    public string Mechanic=>lesson.key;
    public bool Paused=>paused;
    public static CinematicMechanicTutorial Open(WatchCopyTutorial3D owner,WatchCopyTutorial3D.Lesson lesson)
    {
        var prefab=Resources.Load<BoardCoachTutorial>("Tutorials/BoardCoachTutorial");
        if(prefab==null || lesson.demonstration==null || lesson.root.transform.Find("Puck")==null)return null;
        var overlay=Instantiate(prefab);overlay.UseAsPlaybackOverlay();
        var view=overlay.GetComponent<CinematicMechanicTutorial>();
        if(view==null){Destroy(overlay.gameObject);return null;}
        view.ui=overlay;view.owner=owner;view.lesson=lesson;view.puck=lesson.root.transform.Find("Puck");
        lesson.demonstration.SampleAnimation(owner.presentation,0);view.start=view.puck.localPosition;
        view.cameraRest=owner.lessonCamera.transform.position;view.rotationRest=owner.lessonCamera.transform.rotation;view.fovRest=owner.lessonCamera.fieldOfView;
        // Retain the saved board and mechanic meshes; hide the old sign/control furniture.
        var common=owner.presentation.transform.Find("Common");
        if(common!=null)foreach(Transform child in common)
        {
            string n=child.name;
            bool board=n=="BlueCabinet" || n=="MapleBoard" || n=="SideRail" || n=="EndRail" || n=="CornerPlug";
            bool basicControls=lesson.key=="BASIC" && (n=="JoystickBase" || child==owner.fireCap.parent || n=="ControlShelf" || n=="Glove");
            if(!board && !basicControls)view.Hide(child);
        }
        if(owner.coachBody!=null)view.Hide(owner.coachBody.parent);
        foreach(Transform child in lesson.root.transform)
            if(child.name!="Puck" && child.name!="Goal" && child.name!="Mechanic" && child.name!="AimArrow")
                if(child.GetComponent<Renderer>()!=null)view.Hide(child);
        overlay.replay.onClick.AddListener(view.Replay);
        overlay.pause.onClick.AddListener(view.TogglePause);
        overlay.play.onClick.AddListener(view.Continue);
        view.Sample(0);view.enabled=true;return view;
    }
    void Hide(Transform root)
    {
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            if(!hidden.ContainsKey(renderer)){hidden.Add(renderer,renderer.enabled);renderer.enabled=false;}
    }
    public void Replay(){time=0;paused=false;ui.SetPauseLabel(false);Sample(0);}
    public void TogglePause(){paused=!paused;ui.SetPauseLabel(paused);}
    void Update()
    {
        if(closed || exiting)return;
        entrance=Mathf.Min(1,entrance+Time.unscaledDeltaTime/.65f);
        ui.group.alpha=Mathf.SmoothStep(0,1,entrance);
        if(entrance>=1 && !paused)time=Mathf.Min(Duration,time+Time.unscaledDeltaTime);
        Sample(time);
        if(neutral){if(BoardCoachTutorial.ControlsReleased)neutral=false;return;}
        if(Input.GetKeyDown(KeyCode.R)||ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green))Replay();
        if(Input.GetKeyDown(KeyCode.Space)||ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Blue))TogglePause();
        if(Input.GetKeyDown(KeyCode.Return)||ArcadeInputAdapter.ConfirmDown()||Input.GetKeyDown(KeyCode.Escape))Continue();
    }
    public void Sample(float seconds)
    {
        bool basic=lesson.key=="BASIC", wrong=!basic && seconds<4;
        float sample=basic?Mathf.Clamp(seconds*1.25f,0,7.5f):wrong?0:Mathf.Clamp((seconds-4)*1.05f,0,7.5f);
        lesson.demonstration.SampleAnimation(owner.presentation,sample);
        owner.SampleCinematicControls(sample,basic);
        if(wrong)
        {
            Vector3 end=WrongTarget();
            puck.localPosition=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-.7f)/1.8f)));
            lesson.aimArrow.gameObject.SetActive(false);
        }
        else lesson.aimArrow.gameObject.SetActive(sample<4);
        lesson.goalGlow.SetActive(!wrong && sample>=7 && lesson.demonstrationScores);
        ui.progress.fillAmount=seconds/Duration;
        ui.charge.fillAmount=wrong?.22f:Mathf.Clamp01((sample-2.3f)/1.4f);
        ui.charge.color=wrong?new Color(.95f,.35f,.22f):ui.selected;
        ui.caption.text=seconds>=Duration?"YOUR TURN — TRY IT ON THE BOARD":basic?(sample<2.3f?"AIM WITH THE JOYSTICK":sample<4?"HOLD BLACK TO CHARGE":"RELEASE BLACK TO SHOOT"):Advice(lesson.key,wrong);
        ui.clock.text=$"{(paused?"PAUSED":"WATCH")}  ·  0:{Mathf.FloorToInt(seconds):00} / 0:{Duration:00}";
        // Sampling clips can enable old meshes: keep the legacy furniture hidden.
        foreach(var item in hidden)if(item.Key!=null)item.Key.enabled=false;
    }
    Vector3 WrongTarget()
    {
        switch(lesson.key)
        {
            case "BANK":case "RIM":return new Vector3(1.23f,start.y,-.4f);
            case "ICE":case "FAST":return new Vector3(2.5f,start.y,4.7f);
            case "MOVE":return new Vector3(-1.2f,start.y,3.9f);
            case "PUSH":case "WIND":return new Vector3(3f,start.y,1.5f);
            case "PULL":return new Vector3(-.35f,start.y,.2f);
            case "WARP":return new Vector3(2.5f,start.y,1.5f);
            case "JUMP":case "KICK":return new Vector3(2.5f,start.y,1);
            case "+PTS":return new Vector3(2.4f,start.y,3.5f);
            case "PEGS":return new Vector3(-.7f,start.y,2.7f);
            default:return new Vector3(0,start.y,0);
        }
    }
    public static string Advice(string key,bool wrong)
    {
        switch(key)
        {
            case "RIM":case "BANK":return wrong?"STRAIGHT AT THE WALL? WATCH THE ANGLE":"AIM FOR A BANK — BOUNCE TOWARD THE GOAL";
            case "MUD":return wrong?"TOO LITTLE POWER — HONEY STOPS YOU":"HOLD LONGER TO CLEAR THE HONEY";
            case "ICE":return wrong?"TOO MUCH POWER — YOU SLIDE TOO FAR":"USE LESS POWER ON ICE";
            case "FAST":return wrong?"EXTRA SPEED CAN SEND YOU PAST":"LET THE BOOST DO THE WORK";
            case "MOVE":return wrong?"THE GOAL MOVES AWAY":"AIM WHERE THE GOAL WILL BE";
            case "!":return wrong?"THE DIRECT ROUTE HITS THE TRAP":"AIM AROUND THE HAZARD";
            case "PUSH":case "WIND":return wrong?"THE CURRENT PUSHES YOU OFF COURSE":"AIM AGAINST THE PUSH";
            case "PULL":return wrong?"THE PULL BENDS YOUR SHOT":"AIM WIDE — LET THE SHOT CURVE";
            case "JUMP":return wrong?"MISSING THE PAD MISSES THE TURN":"HIT THE PAD TO CHANGE DIRECTION";
            case "KICK":return wrong?"MISSING THE BUMPER MISSES THE BOOST":"HIT THE BUMPER — WATCH THE BOOST";
            case "WARP":return wrong?"MISSING THE ENTRANCE MISSES THE SHORTCUT":"ENTER HERE — COME OUT THERE";
            case "+PTS":return wrong?"THE WIDE ROUTE MISSES THE GOLD":"LINE UP THE GOLD FOR BONUS POINTS";
            case "PEGS":return wrong?"THE POSTS BLOCK A WIDE SHOT":"LINE UP THE GAP BETWEEN THE POSTS";
            default:return wrong?"SHOOT TOO EARLY AND THE WAY IS BLOCKED":"WAIT FOR THE OPENING — THEN RELEASE";
        }
    }
    void LateUpdate()
    {
        if(closed || exiting || owner==null || !owner.IsShowing)return;
        var stage=owner.presentation.transform;
        Vector3 local=stage.InverseTransformPoint(puck.position);
        Vector3 target=stage.TransformPoint(new Vector3(Mathf.Clamp(local.x,-2,2),.2f,Mathf.Clamp(local.z,-2.8f,2.6f)));
        if(lesson.key=="BASIC")target=Vector3.Lerp(stage.TransformPoint(new Vector3(0,.2f,-4.2f)),target,Mathf.SmoothStep(0,1,Mathf.Clamp01((time-2.7f)/1.1f)));
        Vector3 position=target+stage.TransformVector(new Vector3(0,9,-9));
        float blend=Mathf.SmoothStep(0,1,entrance);
        owner.lessonCamera.transform.position=Vector3.Lerp(cameraRest,position,blend);
        owner.lessonCamera.transform.rotation=Quaternion.Slerp(rotationRest,Quaternion.LookRotation(target-position,stage.up),blend);
        owner.lessonCamera.fieldOfView=Mathf.Lerp(fovRest,48,blend);
    }
    public void Continue(){if(!neutral&&!closed&&!exiting)StartCoroutine(ReturnToGame());}
    IEnumerator ReturnToGame()
    {
        exiting=true;
        var camera=owner.lessonCamera;
        Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        for(float t=0;t<1;t+=Time.unscaledDeltaTime/.55f)
        {
            float ease=Mathf.SmoothStep(0,1,t);
            camera.transform.position=Vector3.Lerp(position,cameraRest,ease);
            camera.transform.rotation=Quaternion.Slerp(rotation,rotationRest,ease);
            camera.fieldOfView=Mathf.Lerp(fov,fovRest,ease);ui.group.alpha=1-ease;
            yield return null;
        }
        owner.Skip();
    }
    public void Close()
    {
        if(closed)return;closed=true;
        foreach(var item in hidden)if(item.Key!=null)item.Key.enabled=item.Value;
        if(owner!=null && owner.lessonCamera!=null){owner.lessonCamera.transform.SetPositionAndRotation(cameraRest,rotationRest);owner.lessonCamera.fieldOfView=fovRest;}
        Destroy(gameObject);
    }
    void OnDisable(){if(Application.isPlaying && owner!=null)Close();}
}
