using System;
using UnityEngine;

/// <summary>Operates only editor-saved meshes and animation clips. No runtime visual construction.</summary>
public sealed class WatchCopyTutorial3D : MonoBehaviour
{
    [Serializable] public sealed class Lesson
    {
        public string key;
        public GameObject root;
        public AnimationClip demonstration;
        public Transform aimArrow;
        public GameObject goalGlow;
        public TutorialPractice3D practice;
        public bool demonstrationScores = true;
    }
    public GameObject presentation;
    public Camera lessonCamera, gameplayCamera;
    public GameObject gameplayHud;
    public Lesson[] lessons;
    public GameObject[] messages, progressLights;
    public Transform hand, joystick, fireCap;
    public Transform coachBody, coachArm;
    public Transform[] coachEyes;
    public GameObject[] coachMessages;
    public Collider aimHit, fireHit, replayHit, skipHit;
    public GameObject aimGlow, fireGlow, watchLabel, yourTurnLabel;
    public bool IsShowing { get; private set; }
    public int Step => _step;
    public TutorialPractice3D CurrentPractice => lessons[_lesson].practice;
    public string CurrentMechanic => IsShowing ? lessons[_lesson].key : "";
    public Transform playbackFill;
    public Collider pauseHit;
    public GameObject pausedLabel, skipLabel, letsPlayLabel, nextLessonLabel, replayLabel, retryLabel;
    public bool PointerFireHeld { get; private set; }
    public bool IsPaused { get; private set; }
    public float PlaybackProgress => Mathf.Clamp01(_time / 14f);
    string[] _queue;
    int _queueIndex, _lesson, _step, _width, _height;
    float _time, _choiceRemaining, _pressRemaining;
    int _pendingControl=-1;
    public float ChoiceRemaining => _choiceRemaining;
    public GameObject[] countdownLabels;
    public Transform repeatCap, continueCap;
    public Vector3 repeatRest, continueRest;
    bool _cameraWasEnabled, _hudWasActive;
    public GameObject[] controlSelectors;
    public int SelectedControl { get; private set; } = 2;
    float _nextNavigation;
    Vector3 _lastPointer;
    bool _awaitNeutral;
    Action _finished;
    Action<string> _lessonSeen;
    bool _launchCue, _goalCue;
    CinematicMechanicTutorial _cinematic;
    public bool CinematicActive => _cinematic!=null;

    public void Begin(string[] mechanics, Action finished, Action<string> lessonSeen=null)
    {
        if(IsShowing || mechanics == null || mechanics.Length == 0) return;
        _queue=mechanics; _queueIndex=0; _finished=finished; _lessonSeen=lessonSeen; IsShowing=true;
        _cameraWasEnabled=gameplayCamera!=null && gameplayCamera.enabled;
        _hudWasActive=gameplayHud!=null && gameplayHud.activeSelf;
        if(gameplayCamera!=null) gameplayCamera.enabled=false;
        if(gameplayHud!=null) gameplayHud.SetActive(false);
        presentation.SetActive(true);
        SelectedControl=0; _awaitNeutral=true; _lastPointer=Input.mousePosition;
        RefreshSelectors();
        FitCamera();
        OpenLesson();
    }

    void OpenLesson()
    {
        _lesson=Array.FindIndex(lessons,x=>x.key==_queue[_queueIndex]);
        if(_lesson<0) _lesson=0;
        for(int i=0;i<lessons.Length;i++) lessons[i].root.SetActive(i==_lesson);
        Replay();
        _cinematic=CinematicMechanicTutorial.Open(this,lessons[_lesson]);
    }

    public void Replay()
    {
        if(!IsShowing) return;
        if(_cinematic!=null){_cinematic.Replay();return;}
        foreach(var lesson in lessons) if(lesson.practice!=null) lesson.practice.StopPractice();
        lessons[_lesson].root.SetActive(true);
        _step=0; _time=0; _choiceRemaining=0; IsPaused=false; PointerFireHeld=false;
        _pendingControl=-1; RestoreCaps();
        _launchCue = _goalCue = false;
        RefreshPlayback();
    }

    public void Skip()
    {
        if(IsShowing) { _lessonSeen?.Invoke(lessons[_lesson].key); ContinueTutorial(); }
    }

    public void TogglePause()
    {
        if(_cinematic!=null){_cinematic.TogglePause();return;}
        if(!IsShowing || _step!=0) return;
        IsPaused=!IsPaused;
        RefreshPlayback();
    }

    void RefreshPlayback()
    {
        float sample=Mathf.Clamp((_time-1.5f)/1.5f,0,7.5f);
        if(_step==0) lessons[_lesson].demonstration.SampleAnimation(presentation,sample);
        hand.gameObject.SetActive(coachBody==null && _step==0 && sample<4f);
        if(_step==0)
        {
            AnimateDemonstrationHand(sample);
        }
        RefreshDemonstrationControls(sample);
        RefreshCoach(sample);
        int message=_step==1?-1:_time<1.5f?0:sample<2.3f?5:sample<4f?6:7;
        for(int i=0;i<messages.Length;i++) messages[i].SetActive(i==message);
        float charge=_step==2?0:Mathf.Clamp01((sample-2.3f)/1.2f);
        for(int i=0;i<progressLights.Length;i++) progressLights[i].SetActive(charge >= (i+1)/3f);
        aimGlow.SetActive(_step==0 && sample<2f);
        fireGlow.SetActive(_step==0 && sample>=2f && sample<4f);
        lessons[_lesson].goalGlow.SetActive(lessons[_lesson].demonstrationScores && sample>=7f);
        watchLabel.SetActive(_step==0 && !IsPaused);
        pausedLabel.SetActive(_step==0 && IsPaused);
        yourTurnLabel.SetActive(false);
        pauseHit.enabled=_step==0;
        for(int i=0;i<countdownLabels.Length;i++) countdownLabels[i].SetActive(_step==1 && i==Mathf.Clamp(Mathf.CeilToInt(_choiceRemaining)-1,0,4));
        skipLabel.SetActive(_step!=1);
        letsPlayLabel.SetActive(_step==1 && _queueIndex==_queue.Length-1);
        nextLessonLabel.SetActive(_step==1 && _queueIndex<_queue.Length-1);
        replayLabel.SetActive(_step!=2); retryLabel.SetActive(_step==2);
        playbackFill.localScale=new Vector3(Mathf.Max(.001f,PlaybackProgress),1,1);
        playbackFill.localPosition=new Vector3(-4f+4f*PlaybackProgress,1.37f,5.29f);
    }

    public void SampleCinematicControls(float sample, bool showControls)
    {
        hand.gameObject.SetActive(showControls && sample<4);
        if(showControls){RefreshDemonstrationControls(sample);AnimateDemonstrationHand(sample);}
    }

    void RefreshDemonstrationControls(float sample)
    {
        // Invert the same vertical axis as gameplay: pull toward the player to
        // aim forward. The handle follows the currently displayed aim, not an
        // unrelated left/right animation baked into the old demonstration.
        Vector3 aim = lessons[_lesson].aimArrow.forward;
        Vector3 forward = Vector3.ProjectOnPlane(lessonCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(lessonCamera.transform.right, Vector3.up).normalized;
        Vector2 shot = new Vector2(Vector3.Dot(aim, right), Vector3.Dot(aim, forward));
        Vector2 physical = ToyBoxGameplayHud.MapGameplayStick(shot);
        Vector3 lean = joystick.parent.InverseTransformDirection(right * physical.x + forward * physical.y);
        float amount = _step == 0 && sample < 2.3f ? .7f : 0f;
        joystick.localRotation = Quaternion.FromToRotation(Vector3.up,
            new Vector3(lean.x * amount, 1, lean.z * amount).normalized);
    }

    void RefreshCoach(float sample)
    {
        if(coachBody==null) return;
        coachBody.localRotation=Quaternion.Euler(0,Mathf.Sin(_time*1.1f)*3f,Mathf.Sin(_time*1.6f)*2f);
        if(coachArm!=null) coachArm.localRotation=Quaternion.Euler(0,0,Mathf.Sin(_time*2f)*7f);
        float phase=Mathf.Repeat(_time,3.7f);
        float blink=phase<.24f?Mathf.Max(.06f,Mathf.Abs(phase-.12f)/.12f):1f;
        foreach(var eye in coachEyes) if(eye!=null) eye.localScale=new Vector3(1,blink,1);
        int cue=_step==1?4:_time<1.5f?0:sample<2.3f?1:sample<3.5f?2:3;
        for(int i=0;i<coachMessages.Length;i++) coachMessages[i].SetActive(i==cue);
    }

    // Sampled poses keep pause/replay deterministic and synchronize fingertip
    // contact with the cap rather than sliding a rigid glove across the button.
    void AnimateDemonstrationHand(float sample)
    {
        float travel=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.7f,2.3f,sample));
        float release=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.98f,4f,sample));
        float press=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.3f,2.5f,sample))*(1-release);
        Vector3 cap=fireCap.localPosition;
        cap.y=.30f-.12f*press;
        fireCap.localPosition=cap;

        hand.localRotation=Quaternion.Slerp(Quaternion.Euler(0,0,0),Quaternion.Euler(18,0,0),travel);
        for(int i=0;i<4;i++)
        {
            var finger=hand.Find("Finger"+i);
            if(finger==null) continue;
            finger.localRotation=Quaternion.Euler(i==0?0:78,0,0);
        }
        var thumb=hand.Find("Thumb");
        if(thumb!=null) thumb.localRotation=Quaternion.Euler(25,Mathf.Lerp(-35,-55,travel),0);

        var index=hand.Find("Finger0");
        Vector3 fingertip=index!=null?index.TransformPoint(new Vector3(0,-.17f,.395f)):hand.position;
        Vector3 fingertipOffset=fingertip-hand.position;
        // Keep the whole glove outside the ball. Anchor the fingertip near its
        // front edge rather than placing the palm inside the joystick grip.
        Vector3 grip=joystick.TransformPoint(new Vector3(0,.67f,-.43f))-fingertipOffset;
        Vector3 contact=fireCap.TransformPoint(new Vector3(0,.14f,0));
        Vector3 pressPose=contact-fingertipOffset+hand.parent.up*(.22f*(1-press)+.42f*release);
        hand.position=Vector3.Lerp(grip,pressPose,travel)+hand.parent.up*(Mathf.Sin(travel*Mathf.PI)*.45f);
    }

    public void SelectControl(int index)
    {
        SelectedControl=Mathf.Clamp(index,0,2);
        if(_step==1 && SelectedControl==1) SelectedControl=2;
        RefreshSelectors();
        if(_pendingControl<0) RestoreCaps();
    }

    void RefreshSelectors()
    {
        for(int i=0;i<controlSelectors.Length;i++) controlSelectors[i].SetActive(i==SelectedControl);
    }

    void RestoreCaps()
    {
        repeatCap.localPosition=repeatRest+(SelectedControl==0?Vector3.up*.08f:Vector3.zero);
        continueCap.localPosition=continueRest+(SelectedControl==2?Vector3.up*.08f:Vector3.zero);
    }

    public void ActivateSelectedControl()
    {
        if(!IsShowing || _pendingControl>=0) return;
        _pendingControl=SelectedControl; _pressRemaining=.16f;
        if(SelectedControl==0) repeatCap.localPosition=repeatRest+Vector3.down*.13f;
        if(SelectedControl==2) continueCap.localPosition=continueRest+Vector3.down*.13f;
        if(AudioManager.Instance!=null) AudioManager.Instance.PlayButtonClick();
    }

    void ContinueTutorial()
    {
        if(_cinematic!=null){_cinematic.Close();_cinematic=null;}
        if(++_queueIndex<_queue.Length) OpenLesson();
        else Close(true);
    }

    public bool HandlePointer(Vector3 pointer,bool click)
    {
        if(!lessonCamera.pixelRect.Contains(pointer)) return false;
        Ray ray=lessonCamera.ScreenPointToRay(pointer);
        Collider[] controls={replayHit,pauseHit,skipHit};
        int selected=-1; float nearest=float.MaxValue;
        for(int i=0;i<controls.Length;i++)
            if(controls[i].Raycast(ray,out var hit,150f) && hit.distance<nearest)
            { selected=i; nearest=hit.distance; }
        if(selected<0) return false;
        SelectControl(selected);
        if(click) ActivateSelectedControl();
        return true;
    }

    void Update()
    {
        if(!IsShowing || _cinematic!=null) return;
        if(_pendingControl>=0) { Tick(Time.unscaledDeltaTime,false,false); return; }
        if(_width!=Screen.width || _height!=Screen.height) FitCamera();
        bool held=ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black) || Input.GetKey(KeyCode.Space)
            || Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0);
        if(_awaitNeutral)
        {
            if(!held) _awaitNeutral=false;
            Tick(Time.unscaledDeltaTime,false,false);
            return;
        }
        if(Input.GetKeyDown(KeyCode.Escape)
            || ArcadeInputAdapter.CancelDown()) { Skip(); return; }
        if(Input.GetKeyDown(KeyCode.R)) { Replay(); return; }
        Vector2 stick=ArcadeInputAdapter.GetStick();
        if(Input.GetKey(KeyCode.LeftArrow)) stick.x=-1;
        if(Input.GetKey(KeyCode.RightArrow)) stick.x=1;
        if(Input.GetKey(KeyCode.UpArrow)) stick.y=1;
        if(Input.GetKey(KeyCode.DownArrow)) stick.y=-1;
        if(stick.sqrMagnitude<.2f) _nextNavigation=0;
        else if(Time.unscaledTime>=_nextNavigation)
        {
            int direction=Mathf.Abs(stick.x)>=Mathf.Abs(stick.y)?(stick.x>0?1:-1):(stick.y<0?1:-1);
            SelectControl(_step==1?(SelectedControl==0?2:0):(SelectedControl+direction+3)%3);
            _nextNavigation=Time.unscaledTime+.22f;
        }
        bool click=Input.GetMouseButtonDown(0);
        if(click || (Input.mousePosition-_lastPointer).sqrMagnitude>.5f)
        {
            _lastPointer=Input.mousePosition;
            if(HandlePointer(_lastPointer,click) && click) return;
        }
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)
            || ArcadeInputAdapter.ConfirmDown()) { ActivateSelectedControl(); return; }
        Tick(Time.unscaledDeltaTime,false,false);
    }

    public void Tick(float delta, bool aimed, bool fireHeld)
    {
        if(!IsShowing) return;
        if(_pendingControl>=0)
        {
            _pressRemaining-=Mathf.Max(0,delta);
            if(_pressRemaining>0) return;
            int action=_pendingControl; _pendingControl=-1; RestoreCaps();
            if(action==0) Replay();
            else if(action==1) TogglePause();
            else if(_step==1) ContinueTutorial();
            else Skip();
            return;
        }
        if(IsPaused) return;
        if(_step==1)
        {
            _choiceRemaining=Mathf.Max(0,_choiceRemaining-Mathf.Max(0,delta));
            if(_choiceRemaining<=0) { ContinueTutorial(); return; }
            RefreshPlayback();
            return;
        }
        _time=Mathf.Min(14f,_time+Mathf.Max(0,delta));
        if (!_launchCue && _time >= 7.5f)
        {
            _launchCue = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.puckLaunchSfx);
        }
        if (!_goalCue && _time >= 12f && lessons[_lesson].demonstrationScores)
        {
            _goalCue = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.holeSinkSfx);
        }
        if(_time>=14f)
        {
            lessons[_lesson].demonstration.SampleAnimation(presentation,7.5f);
            _lessonSeen?.Invoke(lessons[_lesson].key);
            _step=1; _choiceRemaining=5; IsPaused=false;
            SelectControl(2);
        }
        RefreshPlayback();
    }

    // Retained for saved legacy practice assets; watch-only tutorials never activate them.
    public void ShowPracticeCharge(float charge) { }
    public void CompletePractice() { }
    public void ContinueAfterPractice() { }

    public void FitCamera()
    {
        _width=Screen.width; _height=Screen.height;
        float aspect=Mathf.Max(.4f,(float)Mathf.Max(1,Screen.width)/Mathf.Max(1,Screen.height));
        FitCameraAspect(aspect);
    }

    public void FitCameraAspect(float aspect)
    {
        Vector3 target=new Vector3(coachBody!=null?-1.6f:0,.25f,-1.6f);
        float tan=Mathf.Tan(lessonCamera.fieldOfView*Mathf.Deg2Rad*.5f),distance=0;
        foreach(float x in new[]{coachBody!=null?-9.2f:-5.9f,5.9f}) foreach(var yz in new[]{new Vector2(-1.0f,-8.4f),new Vector2(coachBody!=null?5f:2.3f,5.7f)})
        {
            Vector3 p=Quaternion.Inverse(lessonCamera.transform.localRotation)*(new Vector3(x,yz.x,yz.y)-target);
            distance=Mathf.Max(distance,Mathf.Abs(p.y)/tan-p.z,Mathf.Abs(p.x)/(tan*aspect)-p.z);
        }
        lessonCamera.transform.localPosition=target-lessonCamera.transform.forward*(distance*1.025f);
    }

    void Close(bool invoke)
    {
        if(_cinematic!=null){_cinematic.Close();_cinematic=null;}
        if(!IsShowing) return;
        foreach(var lesson in lessons) if(lesson.practice!=null) lesson.practice.StopPractice();
        IsShowing=false;
        presentation.SetActive(false);
        if(gameplayCamera!=null) gameplayCamera.enabled=_cameraWasEnabled;
        if(gameplayHud!=null) gameplayHud.SetActive(_hudWasActive);
        var callback=_finished; _finished=null; _lessonSeen=null;
        if(invoke) callback?.Invoke();
    }
    void OnDisable() { Close(false); }
}
