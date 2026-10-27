using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Saved 3D station scenery, right-hand departure and bonus-level selection.</summary>
public sealed partial class WorldStationRoute3D : MonoBehaviour
{
    public Camera viewCamera;
    public WorldVictoryCelebration3D victory, welcome;
    public StationSignalTimer3D signalTimer;
    float _bonusRemaining = 30;
    public Transform train, chimney, leftDoor, rightDoor, switchLever;
    public Transform[] wheels, smoke, sparkles;
    public Vector3[] route;
    public Vector3 trainScale;
    public GameObject[] firstWorldContent, chapterLocks;
    public ToyBoxMenuController.PuckButton[] firstWorldButtons;
    public GameObject worldOneRoom, worldTwoCity, worldOneLabel, worldTwoLabel;
    public UnityEngine.UI.Image transitionCurtain;
    public GameObject bonusRoot, lockedSign, openSign, gatewayGlow, gateSelection;
    public Collider gateHit;
    public ToyBoxMenuController.PuckButton[] bonusButtons;
    public ToyBoxMenuController.PuckButton bonusBack;
    public Material ivory, locked;
    public AudioSource steamAudio, eventAudio;
    public AudioClip departure, movingTrain, gateClose, arrivalWhistle;
    bool _gateClosePlayed, _arrivalWhistlePlayed;
    [Range(0, 1)] public float trainEventVolume = .68f;
    public Bounds framing = new Bounds(new Vector3(3.6f,2,0),new Vector3(29,8,16));
    public bool Unlocked { get; private set; }
    public bool Travelling { get; private set; }
    public bool BonusVisible => bonusRoot != null && bonusRoot.activeSelf;
    public bool Modal => (victory!=null&&victory.Playing) || Travelling || BonusVisible || _releaseAfterBack;
    public bool IsShowing => _standalone && gameObject.activeSelf;
    public int Selection => _focus;
    const float JourneyTimeScale = 1.6f;
    // Stop farther along the station curve, closer to the level wagons.
    const float WorldTwoTrainStop = .82f;
    float JourneySeconds => (Time.unscaledTime-_departureAt)/JourneyTimeScale;
    float _departureAt, _idleAt, _navAt, _pollAt;
    bool _standalone, _neutral, _chosen, _releaseAfterBack;
    int _focus;
    Action<int> _select;
    readonly List<Camera> _cameras = new List<Camera>();
    readonly List<AudioListener> _listeners = new List<AudioListener>();
    readonly List<Light> _lights = new List<Light>();
    Color _sky, _equator, _ground;bool _lightingCaptured;
    Vector3 _pointer;

    void OnEnable()
    {
        if(Application.isPlaying)CaptureStationLighting();
        _idleAt=Time.unscaledTime;
        _pointer=Input.mousePosition;
        UpdateTrainAudio();
    }
    void OnDisable()
    {
        if(steamAudio!=null)steamAudio.Stop();if(eventAudio!=null)eventAudio.Stop();
        if(victory!=null)victory.Stop();
        if(welcome!=null)welcome.Stop();
        Travelling=false;RestoreTransition();RestoreCameras();
    }
    void Update()
    {
        float now=Time.unscaledTime;
        if(_releaseAfterBack && !Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.Escape)
            && !ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black) && !ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Red)) _releaseAfterBack=false;
        UpdateTrainAudio();
        if(victory!=null&&victory.Playing)
        { Sample(now-_idleAt,-1); if(victory.Tick())BeginDeparture(); return; }
        Sample(now-_idleAt,Travelling?JourneySeconds:-1);
        if(Travelling && !_gateClosePlayed && JourneySeconds>=2.65f)
        { _gateClosePlayed=true; Sound(gateClose,.82f); }
        if(Travelling && !_arrivalWhistlePlayed && JourneySeconds>=5.1f)
        { _arrivalWhistlePlayed=true; Sound(arrivalWhistle,.85f); }
        if(Travelling&&JourneySeconds>=4.8f&&!BonusVisible)ShowBonus();
        if(Travelling&&JourneySeconds>=7.4f){Travelling=false;FinishTransition();if(welcome!=null)welcome.Play(eventAudio);}
        if(welcome!=null&&welcome.Playing){welcome.Tick();return;}
        if(BonusVisible&&!Travelling)
        {
            if(now>=_pollAt){RefreshBonus();_pollAt=now+.5f;}
            HandleBonusInput();
            TickBonusCountdown(Time.unscaledDeltaTime);
        }
    }
    void UpdateTrainAudio()
    {
        var audio = AudioManager.Instance;
        float volume = audio != null ? audio.masterVolume * audio.sfxVolume : 1f;
        if (eventAudio != null)
        {
            if (audio != null) eventAudio.outputAudioMixerGroup = audio.sfxGroup;
            eventAudio.volume = volume * trainEventVolume;
        }
        if (steamAudio == null) return;
        if (audio != null) steamAudio.outputAudioMixerGroup = audio.sfxGroup;
        float journey=Travelling?JourneySeconds:-1;
        float speed=TrainMotionSpeed(journey);
        bool moving=Travelling&&speed>.001f&&movingTrain!=null&&!(journey>=3.3f&&journey<5.05f);
        steamAudio.volume = moving ? volume*.60f*Mathf.SmoothStep(0,1,speed*3f) : 0f;
        steamAudio.pitch = moving ? .78f+speed*.58f : 1f;
        // A parked train is silent in either world, including after arrival or reset.
        if (!moving) { if (steamAudio.isPlaying) steamAudio.Stop(); return; }
        if (!Application.isPlaying) return;
        if(steamAudio.clip!=movingTrain){steamAudio.Stop();steamAudio.clip=movingTrain;}
        steamAudio.loop = true;
        if (!steamAudio.isPlaying) steamAudio.Play();
    }
    public void SetUnlocked(bool unlocked)
    {
        unlocked &= BucaLevelAvailability.PlayableLevelCount > 30;
        Unlocked=unlocked;
        if(lockedSign!=null)lockedSign.SetActive(!unlocked);
        if(openSign!=null)openSign.SetActive(unlocked);
        if(gatewayGlow!=null)gatewayGlow.SetActive(unlocked);
    }
    public void SetGateSelected(bool selected){if(gateSelection!=null)gateSelection.SetActive(selected);}
    public void OpenFromMenu(Action<int> select)
    {if(!Unlocked||Modal)return;_standalone=false;_select=select;BeginDeparture();}
    public void ShowJourney(Action<int> select)
    {
        if (BucaLevelAvailability.PlayableLevelCount <= 30) return;
        _standalone=true;_chosen=false;_select=select;
        _cameras.Clear();_listeners.Clear();
        foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(c.enabled&&c!=viewCamera){_cameras.Add(c);c.enabled=false;}
        foreach(var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))if(l.enabled&&!l.transform.IsChildOf(transform)){_listeners.Add(l);l.enabled=false;}
        CaptureStationLighting();
        gameObject.SetActive(true);viewCamera.enabled=true;
        SetUnlocked(true);ResetWorld();
        if(victory!=null)victory.Play(eventAudio);else BeginDeparture();
    }
    public void BeginDeparture()
    {
        if(Travelling||!Unlocked)return;
        CacheTransition();
        _chosen=false;_gateClosePlayed=false;_arrivalWhistlePlayed=false;_departureAt=Time.unscaledTime;Travelling=true;_neutral=true;
        Sound(departure,1);SetGateSelected(false);
    }
    public void ResetWorld()
    {
        if(victory!=null)victory.Stop();
        if(welcome!=null)welcome.Stop();
        if(eventAudio!=null)eventAudio.Stop();
        Travelling=false;UpdateTrainAudio();RestoreTransition();SetWorldEnvironment(false);_chosen=false;bonusRoot.SetActive(false);
        foreach(var g in firstWorldContent)if(g!=null)g.SetActive(true);
        train.gameObject.SetActive(true);_idleAt=Time.unscaledTime;
        RefreshFirstWorld();Sample(0,-1);
    }
    public void ShowBonus()
    {
        _bonusRemaining=30;
        if(signalTimer!=null)signalTimer.Display(30);
        SetWorldEnvironment(true);
        foreach(var g in firstWorldContent)if(g!=null)g.SetActive(false);
        bonusRoot.SetActive(true);train.gameObject.SetActive(true);
        bonusBack.cap.parent.gameObject.SetActive(!_standalone);
        var backLabel=bonusRoot.transform.Find("WORLD 1");if(backLabel!=null)backLabel.gameObject.SetActive(!_standalone);
        _focus=Mathf.Clamp(PlayerPrefs.GetInt("BucaCurrentLevel",30)-30,0,19);
        _focus=Mathf.Min(_focus,Mathf.Max(0,PlayerPrefs.GetInt(LevelSelectController.HighestUnlockedKey,30)-30));
        _neutral=true;_pointer=Input.mousePosition;RefreshBonus();
        if(AudioManager.Instance!=null)AudioManager.Instance.PlayButtonClick();
    }
    public void TickBonusCountdown(float elapsed)
    {
        if(!BonusVisible || Travelling || (welcome!=null&&welcome.Playing) || _chosen || signalTimer==null)return;
        _bonusRemaining=Mathf.Max(0,_bonusRemaining-Mathf.Max(0,elapsed));
        signalTimer.Display(_bonusRemaining);
        if(_bonusRemaining>0)return;
        int highest=Mathf.Clamp(PlayerPrefs.GetInt(LevelSelectController.HighestUnlockedKey,0)-30,0,19);
        ChooseBonus(_focus>=0&&_focus<20&&_focus<=highest?_focus:highest);
    }
    public void Hide()
    {
        RestoreCameras();_standalone=false;_select=null;gameObject.SetActive(false);
    }
    void RefreshFirstWorld()
    {
        int highest=BucaLevelAvailability.HighestUnlocked;
        if(chapterLocks!=null)for(int c=0;c<chapterLocks.Length;c++)chapterLocks[c].SetActive(c*6>highest);
        if(firstWorldButtons==null)return;
        for(int i=0;i<firstWorldButtons.Length;i++)
        {
            var b=firstWorldButtons[i];b.face.sharedMaterial=i<=highest?ivory:locked;b.lockMark.SetActive(i>highest);
            foreach(var award in b.awards)award.SetActive(PlayerPrefs.GetInt(LevelManager.PrefLevelBestStrokes+i,0)>0);
        }
    }
    void CaptureStationLighting()
    {
        if(_lightingCaptured)return;
        foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.enabled&&!l.transform.IsChildOf(transform)){_lights.Add(l);l.enabled=false;}
        _sky=RenderSettings.ambientSkyColor;_equator=RenderSettings.ambientEquatorColor;_ground=RenderSettings.ambientGroundColor;_lightingCaptured=true;
        RenderSettings.ambientSkyColor=new Color(.48f,.49f,.50f);RenderSettings.ambientEquatorColor=new Color(.37f,.35f,.31f);RenderSettings.ambientGroundColor=new Color(.23f,.21f,.18f);
    }
    void RestoreCameras()
    {
        foreach(var c in _cameras)if(c!=null)c.enabled=true;_cameras.Clear();
        foreach(var l in _listeners)if(l!=null)l.enabled=true;_listeners.Clear();
        foreach(var l in _lights)if(l!=null)l.enabled=true;_lights.Clear();
        if(_lightingCaptured){RenderSettings.ambientSkyColor=_sky;RenderSettings.ambientEquatorColor=_equator;RenderSettings.ambientGroundColor=_ground;_lightingCaptured=false;}
    }
    void Sound(AudioClip clip,float volume){if(Application.isPlaying&&eventAudio!=null&&clip!=null)eventAudio.PlayOneShot(clip,volume);}
    public Vector3 RoutePoint(float t)
    {
        t=Mathf.Clamp01(t);float f=t*(route.Length-1);int i=Mathf.Min(Mathf.FloorToInt(f),route.Length-2);
        return Vector3.Lerp(route[i],route[i+1],f-i);
    }
    static float TrainMotionSpeed(float seconds)
    {
        if(seconds<0)return 0;
        float phase=seconds<4.8f?(seconds-.5f)/2.8f:(seconds-4.9f)/2.3f;
        float t=Mathf.Clamp01(phase);
        // Normalized derivative of the same quintic ease used by train travel.
        return 16f*t*t*(1-t)*(1-t);
    }

    float WheelRouteDistance(float travel)
    {
        // Match RoutePoint's piecewise track and its straight extension inside the gate.
        if(route==null||route.Length<2)return 0;
        if(travel<0)return travel*9f;
        float segment=Mathf.Clamp01(travel)*(route.Length-1);
        int whole=Mathf.Min(Mathf.FloorToInt(segment),route.Length-2);
        float distance=0;
        for(int i=0;i<whole;i++)distance+=Vector3.Distance(route[i],route[i+1]);
        return distance+Vector3.Distance(route[whole],route[whole+1])*(segment-whole);
    }

    void RollTrainWheels(float travel,bool arriving)
    {
        // The engine faces forward on both trips, so both phases rotate the same way.
        float distance=arriving
            ? WheelRouteDistance(.78f)-WheelRouteDistance(-.35f)+WheelRouteDistance(travel)-WheelRouteDistance(-.35f)
            : WheelRouteDistance(.78f)-WheelRouteDistance(travel);
        // Authored tyres have a .62 radius before the locomotive's presentation scale.
        float radius=.62f*Mathf.Max(.001f,Mathf.Abs(trainScale.x));
        var rotation=Quaternion.AngleAxis(-distance/radius*Mathf.Rad2Deg,Vector3.forward);
        if(wheels!=null)foreach(var wheel in wheels)if(wheel!=null)wheel.localRotation=rotation;
    }

    public void Sample(float idleTime,float journeyTime=-1)
    {
        float open=journeyTime<0?1:journeyTime<4.8f?1-CinematicEase((journeyTime-2.65f)/.65f):CinematicEase((journeyTime-4.95f)/.60f);
        leftDoor.localRotation=Quaternion.Euler(0,-68*open,0);rightDoor.localRotation=Quaternion.Euler(0,68*open,0);
        if(switchLever!=null)switchLever.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-25,25,open));
        bool arriving=journeyTime>=4.8f;
        float depart=CinematicEase((journeyTime-.5f)/2.8f);
        float arrival=CinematicEase((journeyTime-4.9f)/2.3f);
        float travel=journeyTime<0?(BonusVisible?WorldTwoTrainStop:.78f):arriving?Mathf.Lerp(-.35f,WorldTwoTrainStop,arrival):Mathf.Lerp(.78f,-.35f,depart);
        float visibility=journeyTime<0||(arriving?travel>=-.12f:journeyTime<3.3f)?1:0;
        train.gameObject.SetActive(visibility>.001f);
        Vector3 p=RoutePoint(travel),d=RoutePoint(Mathf.Min(.9999f,travel+.002f))-RoutePoint(Mathf.Max(0,travel-.002f));
        if(travel<0){d=RoutePoint(.002f)-RoutePoint(0);p+=d.normalized*(travel*9f);}
        float yaw=-Mathf.Atan2(d.z,d.x)*Mathf.Rad2Deg+((arriving||(journeyTime<0&&BonusVisible))?0:180);
        float motion=TrainMotionSpeed(journeyTime);
        float breath=Mathf.Sin(idleTime*1.65f)+motion*.5f*Mathf.Sin(journeyTime*18);
        train.localPosition=p+Vector3.up*(.006f*motion*Mathf.Sin(journeyTime*18));
        train.localRotation=Quaternion.Euler(.12f*motion*Mathf.Sin(journeyTime*12),yaw,.10f*motion*Mathf.Sin(journeyTime*9));
        train.localScale=trainScale*visibility;
        RollTrainWheels(travel,arriving||(journeyTime<0&&BonusVisible));
        Vector3 stack=transform.InverseTransformPoint(chimney.position);
        for(int i=0;i<smoke.Length;i++)
        {
            float age=Mathf.Repeat(idleTime*.34f+Mathf.Max(0,journeyTime)*.30f+i/(float)smoke.Length,1);
            // The chimney plume must not leak above the station after entering it.
            smoke[i].gameObject.SetActive(visibility>.15f && (journeyTime<0 || travel>=0));
            Vector3 puff=stack+new Vector3(age*3.5f+Mathf.Sin(i*2.1f+age*4)*age*.22f,age*2.5f,age*.70f);
            smoke[i].position=transform.TransformPoint(puff);
            float scale=(1+motion*.25f)*(.14f+age*.75f)*Mathf.SmoothStep(0,1,age/.06f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,age)));
            smoke[i].localScale=new Vector3(scale*(1+age*.22f),scale,scale);
            smoke[i].localRotation=Quaternion.Euler(0,idleTime*12+i*47,age*14);
        }
        for(int i=0;i<sparkles.Length;i++)
        {
            sparkles[i].gameObject.SetActive(Unlocked&&!BonusVisible);
            sparkles[i].localScale=Vector3.one*(.06f+.09f*(.5f+.5f*Mathf.Sin(idleTime*1.8f+i*1.7f)));
            sparkles[i].localRotation=Quaternion.Euler(0,idleTime*14+i*15,0);
        }
    }
    public void Fit(float aspect)
    {
        Vector3 center=transform.TransformPoint(framing.center);float tan=Mathf.Tan(viewCamera.fieldOfView*Mathf.Deg2Rad*.5f),distance=0;
        for(int i=0;i<8;i++)
        {
            Vector3 corner=framing.center+Vector3.Scale(framing.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            Vector3 p=Quaternion.Inverse(viewCamera.transform.rotation)*(transform.TransformPoint(corner)-center);
            distance=Mathf.Max(distance,Mathf.Abs(p.y)/tan-p.z,Mathf.Abs(p.x)/(tan*aspect)-p.z);
        }
        viewCamera.transform.position=center-viewCamera.transform.forward*distance*1.025f;
    }
    void LateUpdate()
    {
        if(Travelling)SampleTransition(JourneySeconds);
        else if(_standalone)Fit(viewCamera.aspect);
    }
    void RefreshBonus()
    {
        int highest=BucaLevelAvailability.HighestUnlocked;
        for(int i=0;i<bonusButtons.Length;i++)
        {
            var b=bonusButtons[i];bool available=i+30<=highest;
            b.face.sharedMaterial=available?ivory:locked;b.lockMark.SetActive(!available);
            b.selectedRing.SetActive(i==_focus);
            for(int j=0;j<b.awards.Length;j++)b.awards[j].SetActive(PlayerPrefs.GetInt(LevelManager.PrefLevelBestStrokes+(i+30),0)>0);
            b.cap.localPosition=b.restPosition+(i==_focus?Vector3.up*.07f:Vector3.zero);
        }
        bonusBack.selectedRing.SetActive(_focus==20);
    }
    void HandleBonusInput()
    {
        bool held=Input.GetMouseButton(0)||Input.GetKey(KeyCode.Return)||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
        if(_neutral){if(!held)_neutral=false;return;}
        if(ArcadeInputAdapter.CancelDown()||Input.GetKeyDown(KeyCode.Escape)){Back();return;}
        Vector2 s=ArcadeInputAdapter.GetStick();
        if(Input.GetKey(KeyCode.LeftArrow))s.x=-1;if(Input.GetKey(KeyCode.RightArrow))s.x=1;
        if(Input.GetKey(KeyCode.UpArrow))s.y=1;if(Input.GetKey(KeyCode.DownArrow))s.y=-1;
        if(s.sqrMagnitude<.2f)_navAt=0;
        else if(Time.unscaledTime>=_navAt)
        {
            var dir=Mathf.Abs(s.x)>Mathf.Abs(s.y)?new Vector2(Mathf.Sign(s.x),0):new Vector2(0,Mathf.Sign(s.y));
            Vector2 origin=viewCamera.WorldToViewportPoint(GetButton(_focus).hit.bounds.center);float best=float.MaxValue;int index=_focus;
            for(int i=0;i<=(_standalone?19:20);i++)
            {
                Vector2 d=(Vector2)viewCamera.WorldToViewportPoint(GetButton(i).hit.bounds.center)-origin;d.x*=viewCamera.aspect;
                if(Vector2.Dot(d,dir)<.005f)continue;float score=d.magnitude+2.5f*Mathf.Abs(d.x*dir.y-d.y*dir.x);
                if(score<best){best=score;index=i;}
            }
            _focus=index;RefreshBonus();_navAt=Time.unscaledTime+.23f;
        }
        bool click=Input.GetMouseButtonDown(0);Vector3 pointer=Input.mousePosition;
        if(click||(pointer-_pointer).sqrMagnitude>.5f)
        {
            _pointer=pointer;Physics.SyncTransforms();var ray=viewCamera.ScreenPointToRay(pointer);
            for(int i=0;i<=(_standalone?19:20);i++)if(GetButton(i).hit.Raycast(ray,out _,150))
            {_focus=i;RefreshBonus();if(click){ChooseBonus(i);return;}break;}
        }
        if(Input.GetKeyDown(KeyCode.Return)||ArcadeInputAdapter.ConfirmDown())ChooseBonus(_focus);
    }
    ToyBoxMenuController.PuckButton GetButton(int i)=>i==20?bonusBack:bonusButtons[i];
    public void ChooseBonus(int index)
    {
        if(!BonusVisible||Travelling||(welcome!=null&&welcome.Playing)||_chosen)return;
        if(index==20){Back();return;}
        if(index<0||index>=20||index+30>BucaLevelAvailability.HighestUnlocked)return;
        _chosen=true;var callback=_select;
        if(_standalone)Hide();callback?.Invoke(index+30);
    }
    void Back()
    {
        if(_standalone)return;ResetWorld();_neutral=true;_releaseAfterBack=true;
        if(eventAudio!=null)eventAudio.Stop();
    }
}
