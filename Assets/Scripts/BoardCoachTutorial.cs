using System;
using TMPro;
using UnityEngine;

/// <summary>A reversible visual replay over the actual board; never drives gameplay physics.</summary>
[DefaultExecutionOrder(500)]
public sealed class BoardCoachTutorial : MonoBehaviour
{
    public bool woodenConsole;
    public CanvasGroup group;
    public RectTransform header, footer, chargeAnchor, hand;
    public TMP_Text caption, clock, pauseLabel;
    public UnityEngine.UI.Image[] stepFaces;
    public UnityEngine.UI.Image progress, charge;
    public UnityEngine.UI.Button replay, play, pause;
    public Color selected = new Color(.35f,.83f,.65f);
    public Color idle = new Color(.12f,.18f,.18f,.94f);
    public static bool IsActive { get; private set; }
    public float PlaybackTime => _time;
    public bool Paused { get; private set; }
    public Vector3 DemoPosition => _ghost != null ? _ghost.transform.position : Vector3.zero;
    LevelManager _manager;
    ToyBoxGameplayHud _hud;
    GameObject _ghost, _guideObject;
    public GameObject demoPuck;
    public MeshRenderer[] demoRenderers;
    public LineRenderer demoGuide;
    Renderer[] _originals;
    bool[] _enabled;
    Vector3 _start, _goal, _shootRest;
    Quaternion _stickRest;
    LineRenderer _guide;
    Action _finished;
    float _time, _entrance;
    bool _neutral, _closing;
    bool _externalPlayback;
    public void UseAsPlaybackOverlay()
    {

        _externalPlayback=true;header.gameObject.SetActive(false);
        if(hand!=null)hand.gameObject.SetActive(false);
        chargeAnchor.anchorMin=new Vector2(.25f,.13f);chargeAnchor.anchorMax=new Vector2(.75f,.21f);
        chargeAnchor.offsetMin=chargeAnchor.offsetMax=Vector2.zero;
    }
    #if UNITY_EDITOR
    public void ApplyArcadePresentation()
    {
        if(woodenConsole)return;
        chargeAnchor.anchorMin=new Vector2(.18f,.13f);
        chargeAnchor.anchorMax=new Vector2(.82f,.21f);
        chargeAnchor.offsetMin=chargeAnchor.offsetMax=Vector2.zero;
        caption.fontSize=28;
        caption.alignment=TextAlignmentOptions.Center;
        caption.margin=new Vector4(16,6,16,6);
        footer.anchorMax=new Vector2(1,.11f);
        StyleButton(pause, .012f, .19f, new Color(.08f,.30f,.85f), "PAUSE", Color.white);
        StyleButton(replay, .60f, .79f, new Color(.12f,.72f,.35f), "WATCH AGAIN", Color.white);
        StyleButton(play, .81f, .988f, new Color(.035f,.045f,.05f), "YOUR TURN", new Color(1,.95f,.82f));
        Place((RectTransform)clock.transform.parent,.20f,.39f,.19f,.84f);
        Place((RectTransform)progress.transform.parent,.41f,.58f,.42f,.52f);
    }
    static void Place(RectTransform rect,float left,float right,float bottom,float top)
    {
        rect.anchorMin=new Vector2(left,bottom);rect.anchorMax=new Vector2(right,top);
        rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
    static void StyleButton(UnityEngine.UI.Button button,float left,float right,Color color,string label,Color textColor)
    {
        Place((RectTransform)button.transform,left,right,.16f,.86f);
        button.image.color=new Color(.07f,.14f,.15f,.35f);
        var text=button.GetComponentInChildren<TMP_Text>();text.text=label;text.color=textColor;text.fontSize=24;
        text.alignment=TextAlignmentOptions.MidlineLeft;
        text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
        text.rectTransform.offsetMin=new Vector2(94,0);text.rectTransform.offsetMax=new Vector2(-8,0);
        var face=button.GetComponentInChildren<ArcadeButtonFace>();
        if(face==null)
        {
            var cap=new GameObject("CabinetButton",typeof(RectTransform),typeof(CanvasRenderer),typeof(ArcadeButtonFace));
            cap.transform.SetParent(button.transform,false);face=cap.GetComponent<ArcadeButtonFace>();
        }
        face.capColor=color;
        face.arcadeButton=label=="PAUSE"?ArcadeInputAdapter.Button.Blue:label=="WATCH AGAIN"?ArcadeInputAdapter.Button.Green:ArcadeInputAdapter.Button.Black;
        face.raycastTarget=true;
        var rect=face.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);
        rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(4,0);rect.sizeDelta=new Vector2(86,86);
        face.SetVerticesDirty();
        var navigation=button.navigation;navigation.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=navigation;
    }
    #endif
    public void SetPauseLabel(bool paused){pauseLabel.text=paused?"RESUME":"PAUSE";}
    public static bool ControlsReleased => !Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Return)
        && !Input.GetKey(KeyCode.Space) && !Input.GetKey(KeyCode.R)
        && !ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)
        && !ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Green)
        && !ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Blue)
        && !ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Red);
    Camera _camera;
    Vector3 _cameraPosition, _honey;
    Quaternion _cameraRotation;
    float _cameraFov;
    float Duration => 6f;
    string _mechanic="MUD";
    GameObject _levelRoot;
    Transform _mechanicTarget;
    Vector3[] _path=Array.Empty<Vector3>();
    int _pathPhase=-1;
    bool _resetting;
    Vector3 _resetPosition, _resetScale;
    bool _savedDetectCollisions;
    Rigidbody _puckBody;
    bool _returning;
    float _returnTime;
    Vector3 _returnCameraPosition, _returnPuckPosition, _returnPuckScale, _deckScale;
    Quaternion _returnCameraRotation;
    float _returnFov;
    public bool IsReturning => _returning;
    public int LessonNumber { get; private set; } = 1;
    public int LessonCount { get; private set; } = 1;
    bool HasNextLesson => LessonNumber < LessonCount;

    public static bool TryShow(GameObject levelRoot,string mechanic,Action finished,int lessonNumber=1,int lessonCount=1)
    {
        var manager=LevelManager.Instance;
        var prefab=Resources.Load<BoardCoachTutorial>("Tutorials/BoardCoachTutorial");
        if(prefab==null || prefab.demoPuck==null || prefab.demoGuide==null || manager==null || manager.puck==null || Camera.main==null || IsActive)return false;
        var view=Instantiate(prefab);
        view._manager=manager;view._finished=finished;view._mechanic=mechanic;view._levelRoot=levelRoot;
        view.LessonCount=Mathf.Max(1,lessonCount);
        view.LessonNumber=Mathf.Clamp(lessonNumber,1,view.LessonCount);
        view.Setup();return true;
    }

    public static bool TryShow(Action finished)
    {
        return TryShow(null,"MUD",finished);
    }

    void Setup()
    {

        IsActive=true; _neutral=true; group.alpha=0;
        var continueLabel=play.GetComponentInChildren<TMP_Text>(true);
        if(continueLabel!=null)continueLabel.text=HasNextLesson?"NEXT LESSON":"YOUR TURN";
        _hud=FindAnyObjectByType<ToyBoxGameplayHud>(FindObjectsInactive.Include);
        if(_hud!=null)
        {
            if(_hud.hints!=null){_deckScale=_hud.hints.transform.localScale;_hud.hints.SetActive(false);_hud.hints.transform.localScale=Vector3.zero;}
            if(_hud.shootCap!=null)_shootRest=_hud.shootCap.localPosition;
            if(_hud.joystickPivot!=null)_stickRest=_hud.joystickPivot.localRotation;
        }
        _puckBody=_manager.puck.GetComponent<Rigidbody>();
        if(_puckBody!=null){_savedDetectCollisions=_puckBody.detectCollisions;_puckBody.detectCollisions=false;}
        _start=_manager.puck.transform.position;
        _goal=_manager.GetCurrentHolePosition(); _goal.y=_start.y;
        _honey=Vector3.Lerp(_start,_goal,.3f);
        _mechanicTarget=FindMechanic();
        if(_mechanicTarget!=null){_honey=_mechanicTarget.position;_honey.y=_start.y;}
        _camera=Camera.main;
        if(_camera!=null){_cameraPosition=_camera.transform.position;_cameraRotation=_camera.transform.rotation;_cameraFov=_camera.fieldOfView;}
        header.gameObject.SetActive(false);
        if(hand!=null)hand.gameObject.SetActive(false);
        _ghost=demoPuck;_ghost.transform.SetParent(null,false);_ghost.transform.localScale=Vector3.one;_ghost.SetActive(true);_ghost.transform.position=_start;
        _originals=_manager.puck.GetComponentsInChildren<Renderer>(true);
        _enabled=new bool[_originals.Length];
        foreach(var copy in demoRenderers)copy.enabled=false;
        int slot=0;
        for(int i=0;i<_originals.Length;i++)
        {
            var source=_originals[i];_enabled[i]=source.enabled;
            var filter=source.GetComponent<MeshFilter>();
            if(filter!=null && source.enabled && source.gameObject.activeInHierarchy && slot<demoRenderers.Length)
            {
                var copy=demoRenderers[slot++];copy.enabled=true;
                copy.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                copy.transform.localScale=source.transform.lossyScale;
                copy.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                copy.sharedMaterials=source.sharedMaterials;
            }
            source.enabled=false;
        }
        _guide=demoGuide;_guideObject=_guide.gameObject;
        _guideObject.SetActive(true);_guide.positionCount=2;
        replay.onClick.AddListener(Replay);play.onClick.AddListener(Continue);pause.onClick.AddListener(TogglePause);
        Sample(0);
    }
    void BeginReplayReset()
    {
        if(_ghost==null)return;
        _resetPosition=_ghost.transform.position;
        _resetScale=_ghost.transform.localScale;
        _resetting=true;
    }
    public void Replay(){if(_returning)return;BeginReplayReset();_pathPhase=-1;_time=0;Paused=false;BoardMechanicClock.SetPaused(false);SetPauseLabel(false);Sample(0);}
    public void TogglePause(){if(_returning)return;Paused=!Paused;BoardMechanicClock.SetPaused(Paused);SetPauseLabel(Paused);}
    void Update()
    {
        if(_closing || _externalPlayback)return;
        if(_returning)
        {
            _returnTime=Mathf.Min(1,_returnTime+Time.unscaledDeltaTime);
            // Release the confirm press before enabling gameplay or a queued lesson.
            if(_returnTime>=1 && ControlsReleased)
            {
                _closing=true;Restore();var done=_finished;_finished=null;
                done?.Invoke();Destroy(gameObject);
            }
            return;
        }
        _entrance=Mathf.Min(1,_entrance+Time.unscaledDeltaTime/.7f);
        float k=1-Mathf.Pow(1-_entrance,3);
        group.alpha=k;header.localScale=Vector3.one*Mathf.Lerp(.9f,1,k);
        header.anchoredPosition=new Vector2(0,Mathf.Lerp(80,0,k));
        footer.anchoredPosition=new Vector2(0,Mathf.Lerp(-100,0,k));
        if(_entrance>=1 && !Paused)_time=Mathf.Min(Duration,_time+Time.unscaledDeltaTime);
        Sample(_time);
        if(_neutral)
        {
            if(ControlsReleased)_neutral=false;
            return;
        }
        if(Input.GetKeyDown(KeyCode.R) || ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green))Replay();
        if(Input.GetKeyDown(KeyCode.Space) || ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Blue))TogglePause();
        if(Input.GetKeyDown(KeyCode.Return) || ArcadeInputAdapter.ConfirmDown())Continue();
    }
    Transform FindMechanic()
    {
        if(_levelRoot==null)return null;
        Transform nearest=null;
        float distance=float.PositiveInfinity;
        foreach(var item in _levelRoot.GetComponentsInChildren<MonoBehaviour>())
        {
            bool match=_mechanic switch
            {
                "MUD" => item is IcePatch mud && mud.patchDamping>1,
                "ICE" => item is IcePatch ice && ice.patchDamping<=1,
                "BANK" => item is BankingRail, "RIM" => item is RimRebound,
                "MOVE" => item is OrbitingHole, "FAST" => item is SpeedBoost,
                "+PTS" => item is ScorePickup, "!" => item is DeadlyTrigger,
                "PUSH" => item is BucaWindZone && item.name.Contains("Conveyor"),
                "WIND" => item is BucaWindZone && !item.name.Contains("Conveyor"),
                "PULL" => item is GravityWell, "JUMP" => item is BouncePad,
                "KICK" => item is KickerBumper, "WARP" => item is Teleporter,
                "SLIDE" => item is MovingWall, "SPIN" => item is RotatingWall,
                "GATE" => item is DisappearingWall, _ => false
            };
            if(match)
            {
                var collider=item.GetComponent<Collider>();
                Vector3 point=collider!=null?collider.ClosestPoint(_start):item.transform.position;
                float candidate=(point-_start).sqrMagnitude;
                if(candidate<distance){nearest=item.transform;distance=candidate;}
            }
        }
        if(nearest!=null)return nearest;
        if(_mechanic=="PEGS")foreach(var item in _levelRoot.GetComponentsInChildren<Transform>())
            if(item.name=="NM2_GuardPeg")return item;
        return null;
    }
    public void Sample(float t)
    {
        bool basic=_mechanic=="BASIC";
        // One complete demonstration per play. Replay is exclusively player-driven.
        t=Mathf.Clamp(t,0,Duration);
        float local=t;
        float power=basic?.65f:_mechanic=="PEGS"?1f:.9f;
        float yaw=0;
        var setup=_levelRoot!=null?_levelRoot.GetComponent<TutorialShotSetup>():null;
        if(setup!=null && setup.TryGet(_mechanic,out var shot))
        {power=basic?shot.firstPower:shot.secondPower;yaw=shot.yaw;}
        charge.fillAmount=Mathf.Clamp01((local-.8f)/1.2f)*power;
        charge.color=selected;progress.fillAmount=t/Duration;
        caption.text=basic?(t<1?"AIM WITH THE JOYSTICK":t<2?"HOLD BLACK TO CHARGE":"RELEASE BLACK — WATCH THE PUCK"):
            local<2?"WATCH HOW THIS MECHANIC WORKS":MechanicCaption();
        if(t>=Duration)caption.text=HasNextLesson?"READY FOR THE NEXT LESSON":"YOUR TURN — TRY IT ON THIS BOARD";
        string status=Paused?"PAUSED":t>=Duration?"READY":"WATCH";
        clock.text=woodenConsole ? $"LESSON {LessonNumber} OF {LessonCount}     |     0:{Mathf.FloorToInt(t):00} / 0:{Duration:00}" : $"{LessonNumber}/{LessonCount}  {status}  0:{Mathf.FloorToInt(t):00} / 0:{Duration:00}";
        if(_ghost==null)return;
        if(_mechanic=="WARP" && _mechanicTarget!=null &&
            _mechanicTarget.TryGetComponent<Teleporter>(out var portal) && portal.partner!=null)
        {
            SamplePortal(t,portal);
            return;
        }
        int phase=0;
        if(_pathPhase!=phase || (local>=2 && _pathPhase==phase && _path.Length==0))
        {
            _pathPhase=phase;_path=Array.Empty<Vector3>();
        }
        if(local>=2 && _path.Length==0)
        {
            Vector3 target=_mechanicTarget!=null?_mechanicTarget.position:_goal;
            if(_mechanic=="PEGS")target=_goal;
            // Long rails are not reached by aiming at their transform origin.
            if(_mechanicTarget!=null && (_mechanic=="RIM" || _mechanic=="BANK"))
            {
                var rail=_mechanicTarget.GetComponent<Collider>();
                if(rail!=null)target=rail.ClosestPoint(_start);
            }
            Vector3 direction=target-_start;direction.y=0;
            direction=Quaternion.Euler(0,yaw,0)*direction;
            var controller=_manager.puck.GetComponent<PuckController>();
            if(controller!=null)_path=controller.GetTutorialPath(direction,power);
        }
        _ghost.transform.localScale=Vector3.one;
        if(_path.Length>1 && local>=2)
        {
            float index=Mathf.Min((local-2)/Mathf.Max(.01f,Time.fixedDeltaTime),_path.Length-1);
            int i=Mathf.Min(Mathf.FloorToInt(index),_path.Length-2);
            // Predictor samples are collider centers; retain the model's original origin offset.
            Vector3 position=Vector3.Distance(_path[i],_path[i+1])>1.5f
                ? (index-i<.5f?_path[i]:_path[i+1]) : Vector3.Lerp(_path[i],_path[i+1],index-i);
            _ghost.transform.position=position+(_start-_path[0]);
        }
        else _ghost.transform.position=_start;
        if(_resetting)
        {
            // Reposition only while invisible, so Replay never travels through walls.
            if(local<.3f)
            {
                _ghost.transform.position=_resetPosition;
                _ghost.transform.localScale=_resetScale*(1-Mathf.SmoothStep(0,1,local/.3f));
            }
            else if(local<.6f)
            {
                _ghost.transform.position=_start;
                _ghost.transform.localScale=Vector3.one*Mathf.SmoothStep(0,1,(local-.3f)/.3f);
            }
            else _resetting=false;
        }
        if(_mechanic=="!" && setup!=null && _path.Length>1)
        {
            float impactTime=2+(_path.Length-1)*Time.fixedDeltaTime;
            _ghost.transform.localScale=Vector3.one*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((local-impactTime)/.3f)));
        }
        _guide.enabled=local>=2 && local<5.3f && _ghost.transform.localScale.x>.1f;
        _guide.SetPosition(0,_ghost.transform.position+Vector3.up*.08f);
        _guide.SetPosition(1,Vector3.Lerp(_start,_ghost.transform.position,.85f)+Vector3.up*.08f);
    }
    void SamplePortal(float t, Teleporter portal)
    {
        // Isolate the new mechanic: nearby honey must not stop the demonstration.
        Vector3 entry=portal.transform.position;entry.y=_start.y;
        Vector3 exit=portal.partner.transform.position;exit.y=_start.y;
        Vector3 direction=entry-_start;direction.y=0;
        if(direction.sqrMagnitude<.01f)direction=portal.transform.forward;
        direction.Normalize();
        Vector3 outDirection=portal.partner.transform.TransformDirection(portal.transform.InverseTransformDirection(direction));
        outDirection.y=0;outDirection.Normalize();
        Vector3 approach=entry-direction*1.2f;
        if(t<2) _ghost.transform.position=approach;
        else if(t<3.4f) _ghost.transform.position=Vector3.Lerp(approach,entry,(t-2)/1.4f);
        else _ghost.transform.position=Vector3.Lerp(exit,exit+outDirection*1.1f,Mathf.Clamp01((t-3.4f)/1.6f));
        _ghost.transform.localScale=Vector3.one;
        _resetting=false;
        _guide.enabled=false;
        caption.text=t>=Duration?"YOUR TURN — USE THE PORTALS TO CROSS":
            t<2?"AIM AT THE BRASS PORTAL":t<3.4f?"ENTER THIS PORTAL":"EXIT ITS PARTNER — KEEP MOVING";
    }
    string MechanicCaption() => _mechanic switch
    {
        "MUD"=>"HONEY SLOWS THE PUCK — COMPARE THE POWER",
        "ICE"=>"ICE KEEPS THE PUCK SLIDING", "FAST"=>"THE BOOST ADDS SPEED",
        "BANK" or "RIM"=>"WATCH THE ANGLE AFTER THE BOUNCE",
        "MOVE"=>"AIM WHERE THE MOVING GOAL WILL BE",
        "PUSH" or "WIND"=>"THE CURRENT PUSHES THE SHOT",
        "PULL"=>"THE WELL CURVES THE SHOT", "JUMP"=>"THE PAD CHANGES YOUR DIRECTION",
        "KICK"=>"THE BUMPER ADDS A KICK", "WARP"=>"ENTER ONE PORTAL — EXIT ITS PARTNER",
        "+PTS"=>"COLLECT A STAR — GAIN ONE EXTRA HEART", "!"=>"TOUCHING A HAZARD COSTS A LIFE",
        "PEGS"=>"AIM THROUGH THE GAP", _=>"WAIT FOR AN OPENING BEFORE YOU SHOOT"
    };
    void LateUpdate()
    {
        if(_closing || _externalPlayback || _camera==null)return;
        if(_manager.puckShadow!=null && _ghost!=null)
        {
            var shadow=_ghost.transform.position;shadow.y=.015f;_manager.puckShadow.position=shadow;
        }
        if(_returning)
        {
            float k=Mathf.SmoothStep(0,1,_returnTime);
            _camera.transform.position=Vector3.Lerp(_returnCameraPosition,_cameraPosition,k);
            _camera.transform.rotation=Quaternion.Slerp(_returnCameraRotation,_cameraRotation,k);
            _camera.fieldOfView=Mathf.Lerp(_returnFov,_cameraFov,k);
            _ghost.transform.position=Vector3.Lerp(_returnPuckPosition,_start,k);
            _ghost.transform.localScale=Vector3.Lerp(_returnPuckScale,Vector3.one,k);
            group.alpha=1-k;
            // The gameplay deck stays hidden through every lesson handoff.
            // The HUD reveals it only once the manager enables gameplay.
            return;
        }
        float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(_entrance));
        Vector3 focus=Vector3.Lerp(_start,_honey,.7f);
        if(_mechanic=="WARP" && _mechanicTarget!=null && _mechanicTarget.TryGetComponent<Teleporter>(out var portal) && portal.partner!=null)
            focus=(portal.transform.position+portal.partner.transform.position)*.5f;
        focus=Vector3.Lerp(focus,_ghost.transform.position,.55f);
        Vector3 direction=(_goal-_start).normalized;
        Vector3 position=focus-direction*6f+Vector3.up*7f;
        // Ease across playback resets and portal jumps instead of snapping the camera.
        float follow=1-Mathf.Exp(-6f*Time.unscaledDeltaTime);
        _camera.transform.position=Vector3.Lerp(_camera.transform.position,Vector3.Lerp(_cameraPosition,position,blend),follow);
        _camera.transform.rotation=Quaternion.Slerp(_camera.transform.rotation,Quaternion.Slerp(_cameraRotation,Quaternion.LookRotation(focus-position),blend),follow);
        _camera.fieldOfView=Mathf.Lerp(_cameraFov,48,blend);
    }
    public void Continue()
    {
        if(_neutral || _closing || _returning)return;
        if(_camera==null || _ghost==null)return;
        _returning=true;_returnTime=0;
        group.interactable=false;_guide.enabled=false;
        _returnCameraPosition=_camera.transform.position;_returnCameraRotation=_camera.transform.rotation;_returnFov=_camera.fieldOfView;
        _returnPuckPosition=_ghost.transform.position;_returnPuckScale=_ghost.transform.localScale;
    }
    void Restore()
    {
        BoardMechanicClock.SetPaused(false);
        IsActive=false;
        if(_puckBody!=null)_puckBody.detectCollisions=_savedDetectCollisions;
        if(_camera!=null){_camera.transform.SetPositionAndRotation(_cameraPosition,_cameraRotation);_camera.fieldOfView=_cameraFov;}
        if(_originals!=null)for(int i=0;i<_originals.Length;i++)if(_originals[i]!=null)_originals[i].enabled=_enabled[i];
        if(_hud!=null)
        {
            if(_hud.hints!=null)_hud.hints.transform.localScale=_deckScale;
            if(_hud.shootCap!=null)_hud.shootCap.localPosition=_shootRest;
            if(_hud.joystickPivot!=null)_hud.joystickPivot.localRotation=_stickRest;
        }
        // Setup detaches the demonstration puck so Canvas transforms cannot distort it.
        // Never reparent it during OnDisable: Unity is already changing this hierarchy.
        // This tutorial instance is disposable, so clean up its detached visual directly.
        if(_ghost!=null)
        {
            _ghost.SetActive(false);
            Destroy(_ghost);
            _ghost=null;
        }
        if(_guideObject!=null)_guideObject.SetActive(false);
    }
    void OnDisable(){if(!_externalPlayback && !_closing){_closing=true;_finished=null;Restore();}}
}
