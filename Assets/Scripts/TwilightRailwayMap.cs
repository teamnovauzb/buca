using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Two-world railway selector. Distances drive both the engine and wheel animation.</summary>
public sealed class TwilightRailwayMap : MonoBehaviour
{
    [Serializable] public class Stop
    {
        public Transform platform;
        public Collider hit;
        public Renderer face;
        public GameObject selected, completed, locked;
        public float distance;
    }
    public RailwayMenuLever menuLever;
    public Camera viewCamera;
    public TwilightIllustratedMenu illustratedMenu;
    public Stop[] stops;
    public Vector3[] railway;
    public Transform train;
    public SuitcaseTrainMechanism trainMechanism;
    public TrainChimneySmoke chimneySmoke;
    public TrainChimneySmoke moonlightChimneySmoke;
    public Transform[] wheels;
    public Transform[] steam;
    public Vector3 steamOrigin = new Vector3(1.1f, 2.7f, 0);
    [Tooltip("Compensates the illustrated train camera's foreshortening when following a 2D route.")]
    public float trainProjectionDepth = 1f;
    [Tooltip("Route distance sampled across the wheelbase for a continuous heading on bends.")]
    public float trainHeadingSpan = .08f;
    [Tooltip("Displayed train scale relative to route coordinates; keeps rolling distance matched to the rails.")]
    public float trainWheelTravelScale = 1f;
    [Tooltip("Distance from the engine centre to the stop it approaches, in route units.")]
    public float trainNoseOffset;
    public Collider playHit, backHit;
    public TMP_Text playLabel, statusLabel, countdownLabel;
    public Material ivory, lockedFace;
    public AudioSource motionAudio, eventAudio;
    public AudioClip whistle, rolling;
    [Tooltip("Four steam chuffs per wheel revolution for the wooden World 1 train.")]
    public AudioClip woodenChug;
    public AudioClip arrival;
    public float stationPause = .18f;
    [Tooltip("Testing only: keep level 1 playable and lock levels 2–50 without changing saved progress.")]
    public bool lockLevelsAfterFirstForTesting;
    public GameObject playWords,lockedWords;
    public GameObject[] playNumberDisplays;
    public string gameScene = "Game";
    public Bounds framing = new Bounds(new Vector3(0,7,0),new Vector3(63,23,39));
    public int Selected => selected;
    public int Parked => parked;
    public bool Moving => moving;
    public bool Travelling => moving || parked != destination || (passengers != null && passengers.Boarding);
    public bool EnteringTrack => parked < 0;
    public float RouteDistance => distance;
    public int HighestUnlocked => highest;
    public bool IsLevelLocked(int index) => index > highest || BucaLevelAvailability.IsManuallyLocked(index);
    public float Countdown => countdown;
    public const float DepartureSeconds = 30f;
    public void GoBack() => back?.Invoke();
    public void RequestMenuReturn()
    {
        if(menuLever!=null)menuLever.Pull();
        else GoBack();
    }
    int selected, parked, destination, highest;
    float distance, fromDistance, toDistance, elapsed, duration, pause, nextNav, countdown;
    bool moving, neutral, loading, playWhenParked;
    float[] lengths;
    float[] wheelLengths;
    float wheelProjectionDepth;
    Quaternion[] wheelRest;
    bool journeyWhistle;
    Light[] outsideLights;
    Action back;
    public Action<int> PlayInCurrentSession;
    public bool RequireArrivalBeforePlay;
    int navigationDirection;
    UnityEngine.EventSystems.EventSystem navigationEvents;
    bool previousNavigationEvents;
    AnimalTrainCarriages passengers;
    const float NavigationDeadzoneSquared = .2f;
    const float NavigationFirstRepeat = .4f, NavigationRepeat = .22f;

    void OnEnable()
    {
        if(!Application.isPlaying)return;
        var all=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        outsideLights=System.Array.FindAll(all,l=>l.enabled&&!l.transform.IsChildOf(transform));
        foreach(var light in outsideLights)light.enabled=false;
        TakeNavigation();
    }
    void Awake(){Cache();}
    void Cache()
    {
        if(railway==null||railway.Length<2)return;
        lengths=new float[railway.Length];
        for(int i=1;i<lengths.Length;i++)lengths[i]=lengths[i-1]+Vector3.Distance(railway[i-1],railway[i]);
        CacheWheelLengths();
        if(wheelRest==null||wheelRest.Length!=wheels.Length)
        {wheelRest=new Quaternion[wheels.Length];for(int i=0;i<wheels.Length;i++)wheelRest[i]=wheels[i].localRotation;}
    }
    public void Open(Action goBack)
    {
        PrepareOpen(goBack);
        PlayInCurrentSession=null;RequireArrivalBeforePlay=false;
        if(Application.isPlaying)AnimalTrainCollection.BeginJourney();
        selected=destination=0;
        parked=stops[0].distance>.01f ? -1 : 0;
        distance=0;
        Refresh();Pose();
        if(Application.isPlaying)
        {
            passengers.Initialize();
            passengers.ResetPassengers();
        }
        if(illustratedMenu!=null)illustratedMenu.BeginEntrance();
    }
    void PrepareOpen(Action goBack)
    {
        GameScenePreloader.Prepare(gameScene);
        back=goBack;Cache();loading=false;moving=false;playWhenParked=false;
        if(menuLever!=null)menuLever.ResetLever();
        highest=ReadHighestUnlocked();
        neutral=true;countdown=DepartureSeconds;pause=0;
        navigationDirection=0;nextNav=0;
        if(Application.isPlaying)TakeNavigation();
        journeyWhistle=false;
    }
    int ReadHighestUnlocked() => lockLevelsAfterFirstForTesting ? 0 :
        Mathf.Clamp(BucaLevelAvailability.HighestUnlocked,0,stops.Length-1);
    public void OpenAfterCompletion(int completed, int next, Action<int> play)
    {
        PrepareOpen(() => SceneManager.LoadScene("MainMenu"));
        PlayInCurrentSession=play;RequireArrivalBeforePlay=true;
        if(illustratedMenu!=null)illustratedMenu.ShowImmediately();
        selected=parked=destination=Mathf.Clamp(completed,0,stops.Length-1);
        distance=stops[parked].distance;
        Refresh();Pose();
        if(Application.isPlaying)
        {
            passengers.Initialize();
            passengers.BeginPendingBoarding();
        }
        Select(next);
        // Allow the map entrance and newly collected carriage to appear before departure.
        pause=.8f;
    }
    void OnDisable()
    {
        if(navigationEvents!=null)
        {
            navigationEvents.sendNavigationEvents=previousNavigationEvents;
            navigationEvents=null;
        }
        if(outsideLights!=null){foreach(var light in outsideLights)if(light!=null)light.enabled=true;outsideLights=null;}
        if(motionAudio!=null)motionAudio.Stop();if(eventAudio!=null)eventAudio.Stop();
    }
    void TakeNavigation()
    {
        if(illustratedMenu==null||navigationEvents!=null)return;
        navigationEvents=UnityEngine.EventSystems.EventSystem.current;
        if(navigationEvents==null)return;
        previousNavigationEvents=navigationEvents.sendNavigationEvents;
        // The map handles joystick/Submit. Pointer clicks still use the EventSystem.
        // Otherwise Black can also activate a previously clicked footer button.
        navigationEvents.sendNavigationEvents=false;
        navigationEvents.SetSelectedGameObject(null);
    }
    public bool StepSelection(int direction)
    {
        int next=Mathf.Clamp(selected+Math.Sign(direction),0,stops.Length-1);
        if(loading||next==selected)return false;
        Select(next);
        if(illustratedMenu!=null)illustratedMenu.Refresh();
        if(UnityEngine.EventSystems.EventSystem.current!=null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        return true;
    }
    void Navigate(Vector2 stick,float now)
    {
        if(stick.sqrMagnitude<NavigationDeadzoneSquared)
        {navigationDirection=0;nextNav=0;return;}
        int direction=Mathf.Abs(stick.x)>=Mathf.Abs(stick.y)?(stick.x>0?1:-1):(stick.y<0?1:-1);
        bool changed=direction!=navigationDirection;
        if(!changed&&now<nextNav)return;
        StepSelection(direction);
        navigationDirection=direction;
        nextNav=now+(changed?NavigationFirstRepeat:NavigationRepeat);
    }
    public void Select(int index)
    {
        if(loading || (menuLever!=null && menuLever.Returning))return;
        selected=Mathf.Clamp(index,0,stops.Length-1);countdown=DepartureSeconds;playWhenParked=false;
        if(!IsLevelLocked(selected)){if(parked==destination)journeyWhistle=false;destination=selected;}
        Refresh();
    }
    void Refresh()
    {
        for(int i=0;i<stops.Length;i++)
        {
            var s=stops[i];s.selected.SetActive(i==selected);s.locked.SetActive(IsLevelLocked(i));
            s.face.sharedMaterial=IsLevelLocked(i)?lockedFace:ivory;
            s.completed.SetActive(PlayerPrefs.GetInt(LevelManager.PrefLevelBestStrokes+i,0)>0||PlayerPrefs.GetInt(LevelManager.PrefLevelStars+i,0)>0);
        }
        if(playNumberDisplays!=null&&playNumberDisplays.Length==stops.Length)
        {
            playWords.SetActive(!IsLevelLocked(selected));lockedWords.SetActive(IsLevelLocked(selected));
            for(int i=0;i<playNumberDisplays.Length;i++)playNumberDisplays[i].SetActive(i==selected&&!IsLevelLocked(selected));
        }
        playLabel.text=IsLevelLocked(selected)?"LOCKED":"PLAY LEVEL "+(selected+1);
        statusLabel.text=selected>=BucaLevelAvailability.PlayableLevelCount?"LEVEL UNAVAILABLE":
            IsLevelLocked(selected)?"COMPLETE THE PREVIOUS LEVEL":(selected<30?"WORLD 1  /  THE PLAYROOM":"WORLD 2  /  THE MOONLIT CITY");
    }
    void Update()
    {
        if(!Application.isPlaying||loading||lengths==null)return;
        if(illustratedMenu!=null&&illustratedMenu.Entering)return;
        int unlocked=ReadHighestUnlocked();
        if(unlocked!=highest){highest=unlocked;Refresh();}
        TakeNavigation();
        var stick=ArcadeInputAdapter.GetStick();
        if(Input.GetKey(KeyCode.LeftArrow))stick.x=-1;if(Input.GetKey(KeyCode.RightArrow))stick.x=1;
        if(Input.GetKey(KeyCode.UpArrow))stick.y=1;if(Input.GetKey(KeyCode.DownArrow))stick.y=-1;
        bool held=Input.GetMouseButton(0)||Input.GetKey(KeyCode.Return)||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)
            ||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Green)||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Blue);
        if(neutral){if(!held&&stick.sqrMagnitude<NavigationDeadzoneSquared)neutral=false;return;}
        if((menuLever==null || !menuLever.Returning) && (Input.GetKeyDown(KeyCode.Escape)||ArcadeInputAdapter.CancelDown())){RequestMenuReturn();return;}
        if(menuLever!=null && menuLever.TickReturn(Time.unscaledDeltaTime))return;
        TickTrain(Time.unscaledDeltaTime);
        if(!Travelling)countdown=Mathf.Max(0,countdown-Time.unscaledDeltaTime);
        countdownLabel.text="START IN  "+Mathf.CeilToInt(countdown).ToString("00");
        if(countdown<=0){Select(Mathf.Min(selected,highest));RequestPlay();return;}
        if(!NavigateArcadeButtons())Navigate(stick,Time.unscaledTime);
        if(illustratedMenu == null && Input.GetMouseButtonDown(0)&&viewCamera.pixelRect.Contains(Input.mousePosition))
        {
            Physics.SyncTransforms();var ray=viewCamera.ScreenPointToRay(Input.mousePosition);
            if(playHit.Raycast(ray,out _,200)){RequestPlay();return;}
            if(backHit.Raycast(ray,out _,200)){back?.Invoke();return;}
            for(int i=0;i<stops.Length;i++)if(stops[i].hit.Raycast(ray,out _,200)){Select(i);break;}
        }
        if(Input.GetKeyDown(KeyCode.Return)||ArcadeInputAdapter.ConfirmDown())RequestPlay();
    }
    public void RequestPlay()
    {
        if(IsLevelLocked(selected)||loading || (menuLever!=null && menuLever.Returning))return;
        if(EnteringTrack || (passengers!=null && passengers.Boarding) || (RequireArrivalBeforePlay&&Travelling))
        {playWhenParked=true;return;}
        // An explicit Play press starts the selected level without waiting for train travel.
        playWhenParked=false;
        Load();
    }
    bool NavigateArcadeButtons()
    {
        bool previous=ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green);
        bool next=ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Blue);
        if(previous!=next)StepSelection(previous?-1:1);
        return previous||next;
    }
    void Load()
    {
        if(IsLevelLocked(selected))return;
        if(PlayInCurrentSession!=null){loading=true;PlayInCurrentSession(selected);return;}
        loading=true;PlayerPrefs.SetInt(LevelSelectController.PendingLevelKey,selected);PlayerPrefs.Save();
        if(AudioManager.Instance!=null)AudioManager.Instance.PlayButtonClick();
        GameScenePreloader.Open(gameScene);
    }
    public void TickTrain(float dt)
    {
        if(passengers!=null && passengers.Boarding)
        {
            if(motionAudio!=null && motionAudio.isPlaying)motionAudio.Stop();
            Pose();return;
        }
        float volume=AudioManager.Instance!=null?AudioManager.Instance.masterVolume*AudioManager.Instance.sfxVolume:1;
        if(eventAudio!=null){eventAudio.volume=volume;if(AudioManager.Instance!=null)eventAudio.outputAudioMixerGroup=AudioManager.Instance.sfxGroup;}
        float previous=distance;
        if(!moving)
        {
            pause=Mathf.Max(0,pause-dt);
            if(playWhenParked&&parked==selected&&pause<=0){Load();return;}
            if(parked!=destination&&pause<=0)
            {
                fromDistance=distance;int next=parked+Math.Sign(destination-parked);
                toDistance=stops[next].distance;elapsed=0;duration=Mathf.Clamp(Mathf.Abs(toDistance-fromDistance)/5f,.65f,3.4f);moving=true;
                if(!journeyWhistle&&eventAudio!=null&&whistle!=null){eventAudio.PlayOneShot(whistle,.32f);journeyWhistle=true;}
            }
        }
        else
        {
            elapsed+=dt;float t=Mathf.Clamp01(elapsed/duration);float ease=t*t*t*(t*(t*6-15)+10);
            distance=Mathf.Lerp(fromDistance,toDistance,ease);
            if(t>=1)
            {
                parked+=Math.Sign(toDistance-fromDistance);distance=stops[parked].distance;moving=false;pause=stationPause;
                if(eventAudio!=null&&arrival!=null)eventAudio.PlayOneShot(arrival,.16f);
            }
        }
        Pose();
        float speed=dt>0?Mathf.Abs(distance-previous)/dt:0;
        bool chugRestarted=false;
        if(motionAudio!=null)
        {
            bool wooden=woodenChug!=null&&trainMechanism!=null&&(!trainMechanism.IsMoonlight||moonlightChimneySmoke!=null);
            AudioClip clip=wooden?woodenChug:rolling;
            if(wooden)
            {
                float wheelTravel=Mathf.Abs(WheelDistanceAt(distance-trainNoseOffset)-WheelDistanceAt(previous-trainNoseOffset));
                var engine=trainMechanism.IsMoonlight?trainMechanism.moonlight:trainMechanism.woodland;
                float radius=engine.wheels[0].radius*Mathf.Abs(train.localScale.x);
                float revolutions=dt>0?wheelTravel/(dt*2*Mathf.PI*Mathf.Max(.001f,radius)):0;
                // The clip contains one wheel revolution: four alternating steam chuffs.
                motionAudio.pitch=Mathf.Clamp(revolutions*clip.length,.35f,3f);
                motionAudio.volume=volume*.55f*Mathf.Clamp01(speed/2.5f);
            }
            else
            {
                motionAudio.volume=volume*.28f*Mathf.Clamp01(speed/4);
                motionAudio.pitch=.75f+Mathf.Clamp01(speed/6)*.4f;
            }
            if(AudioManager.Instance!=null)motionAudio.outputAudioMixerGroup=AudioManager.Instance.sfxGroup;
            if(moving&&speed>.02f&&clip!=null)
            {
                if(motionAudio.clip!=clip){motionAudio.Stop();motionAudio.clip=clip;}
                motionAudio.loop=true;
                if(!motionAudio.isPlaying){motionAudio.Play();chugRestarted=wooden;}
            }
            else if(motionAudio.isPlaying)motionAudio.Stop();
        }
        for(int i=0;i<steam.Length;i++)
        {float a=Mathf.Repeat(Time.unscaledTime*.5f+i*.19f,1);steam[i].localPosition=steamOrigin+new Vector3(-a*.9f,a*2.6f,0);steam[i].localScale=Vector3.one*(.18f+a*.65f);steam[i].gameObject.SetActive(moving&&a<.85f);}
        if(chimneySmoke!=null)chimneySmoke.Tick(dt,motionAudio,woodenChug,chugRestarted);
        if(moonlightChimneySmoke!=null)moonlightChimneySmoke.Tick(dt,motionAudio,woodenChug,chugRestarted);
    }
    public Vector3 Point(float d)
    {
        if(lengths==null)Cache();d=Mathf.Clamp(d,0,lengths[lengths.Length-1]);
        int i=Array.BinarySearch(lengths,d);if(i<0)i=~i;i=Mathf.Clamp(i,1,lengths.Length-1);
        return Vector3.Lerp(railway[i-1],railway[i],Mathf.InverseLerp(lengths[i-1],lengths[i],d));
    }
    void CacheWheelLengths()
    {
        wheelProjectionDepth=trainProjectionDepth;
        wheelLengths=new float[railway.Length];
        for(int i=1;i<railway.Length;i++)
        {
            var segment=railway[i]-railway[i-1];
            segment.z*=trainProjectionDepth;
            wheelLengths[i]=wheelLengths[i-1]+segment.magnitude;
        }
    }
    public float WheelDistanceAt(float d)
    {
        if(lengths==null)Cache();
        if(wheelLengths==null||wheelLengths.Length!=railway.Length||wheelProjectionDepth!=trainProjectionDepth)
            CacheWheelLengths();
        if(d<0) return d*wheelLengths[1]/Mathf.Max(.001f,lengths[1]) /Mathf.Max(.001f,trainWheelTravelScale);
        d=Mathf.Clamp(d,0,lengths[lengths.Length-1]);
        int i=Array.BinarySearch(lengths,d);if(i<0)i=~i;i=Mathf.Clamp(i,1,lengths.Length-1);
        return Mathf.Lerp(wheelLengths[i-1],wheelLengths[i],Mathf.InverseLerp(lengths[i-1],lengths[i],d))
            /Mathf.Max(.001f,trainWheelTravelScale);
    }
    void Pose()
    {
        if(train==null)return;
        if(Application.isPlaying && passengers==null)
        {
            passengers=GetComponent<AnimalTrainCarriages>()??gameObject.AddComponent<AnimalTrainCarriages>();
            passengers.map=this;
        }
        float engineDistance=distance-trainNoseOffset;
        train.localPosition=Point(engineDistance);
        float halfSpan=Mathf.Max(.04f,trainHeadingSpan*.5f);
        var delta=Point(engineDistance+halfSpan)-Point(engineDistance-halfSpan);
        train.localRotation=Quaternion.Euler(0,-Mathf.Atan2(delta.z*trainProjectionDepth,delta.x)*Mathf.Rad2Deg,0)*Quaternion.Euler(0,0,Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);
        if(trainMechanism!=null)
        {
            // Switch engines at the actual crossing, not when a distant level is selected.
            float crossing=stops.Length>30?(stops[29].distance+stops[30].distance)*.5f:float.PositiveInfinity;
            trainMechanism.Pose(WheelDistanceAt(engineDistance),train.localScale.x,engineDistance>=crossing);
        }
        else for(int i=0;i<wheels.Length;i++)wheels[i].localRotation=wheelRest[i]*Quaternion.AngleAxis(-distance/(.62f*train.localScale.x)*Mathf.Rad2Deg,Vector3.forward);
    }
    public void Fit(float aspect)
    {
        viewCamera.transform.rotation=Quaternion.Euler(34,-5,0);viewCamera.fieldOfView=34;
        Vector3 target=transform.TransformPoint(framing.center);float tan=Mathf.Tan(viewCamera.fieldOfView*Mathf.Deg2Rad*.5f),dist=0;
        // Fit the actual diorama silhouette instead of an empty 3D bounding box.
        // The tall skyline is at the rear; there is no tall geometry at the front.
        var corners=new[]{new Vector3(-31,-2,-18.8f),new Vector3(31,-2,-18.8f),
            new Vector3(-31,21,21),new Vector3(31,21,21),
            new Vector3(-31,7,14),new Vector3(31,7,14)};
        foreach(var corner in corners)
        {
            var p=Quaternion.Inverse(viewCamera.transform.rotation)*(transform.TransformPoint(corner)-target);
            dist=Mathf.Max(dist,Mathf.Abs(p.y)/tan-p.z,Mathf.Abs(p.x)/(tan*aspect)-p.z);
        }
        viewCamera.transform.position=target-viewCamera.transform.forward*(dist*.97f);
    }
}
