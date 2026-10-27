using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The existing harbor clears and lights up after completing level 30.</summary>
public sealed class WorldTwoHarborReveal : MonoBehaviour
{
    public RawImage artwork;
    public CanvasGroup fade;
    public float duration = 18f;
    public float fogRevealSeconds=6f;
    public bool preserveMapControls;
    [Tooltip("Time when the upgraded train departs. World 2 stays misty until it arrives at level 31.")]
    public float revealStartSeconds = 7f;
    public MoonPuffToyUpgrade trainUpgrade;
    public Button continueButton;
    public AudioSource chimeSource;
    public AudioClip revealChime;
    public RectTransform ticketLayer;
    public CanvasGroup[] worldTwoTickets;
    bool ticketsInitialized;
    public bool WaitingForPlay { get; private set; }
    bool confirmed, neutral;
    int nextChime;
    static readonly float[] ChimeSteps={.18f,.30f,.43f,.55f,.67f,.79f,.91f};
    Material animated;

    public void FitMapToScreen()
    {
        if(artwork==null)return;
        var footer=transform.Find("RevealFooter");
        if(footer!=null)
        {
            var background=footer.GetComponent<Image>();
            if(background!=null){background.enabled=false;background.raycastTarget=false;}
        }
        var rect=artwork.rectTransform;
        var viewport=rect.parent as RectTransform;
        if(viewport!=null&&viewport.gameObject!=gameObject)
        {
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;
            viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            viewport.localScale=Vector3.one;
        }
        var fitter=rect.GetComponent<AspectRatioFitter>();
        if(fitter!=null){fitter.aspectMode=AspectRatioFitter.AspectMode.None;fitter.enabled=false;}
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
        rect.offsetMin=rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;
    }

    void OnEnable(){FitMapToScreen();}

    public float RevealProgress { get; private set; }
    public float TrainDepartureSeconds => Mathf.Clamp(revealStartSeconds, 0f, Mathf.Max(0f, duration - 2f));
    public float TrainArrivalSeconds => Mathf.Max(TrainDepartureSeconds + .1f, duration - fogRevealSeconds);
    public float RevealStart => trainUpgrade != null ? TrainArrivalSeconds : TrainDepartureSeconds;

    public float TrainAnimationTime(float seconds)
    {
        float start = TrainDepartureSeconds;
        float arrival = TrainArrivalSeconds;
        if (seconds <= start)
            return Mathf.Lerp(0, MoonPuffToyUpgrade.DepartureTime, start > 0 ? seconds / start : 1);
        if (seconds <= arrival)
            return Mathf.Lerp(MoonPuffToyUpgrade.DepartureTime, MoonPuffToyUpgrade.ArrivalTime,
                Mathf.InverseLerp(start, arrival, seconds));
        return Mathf.Lerp(MoonPuffToyUpgrade.ArrivalTime, MoonPuffToyUpgrade.Duration,
            Mathf.InverseLerp(arrival, duration, seconds));
    }

    public void Sample(float seconds, bool playSound = false)
    {
        FitMapToScreen();
        var details=GetComponent<CinematicMapDetails>();if(details!=null)details.Refresh();
        if(animated==null)
        {
            animated=new Material(artwork.material);
            artwork.material=animated;
        }
        // The harbor opens only after the train reaches level 31, never during the crossing.
        artwork.uvRect = new Rect(0, 0, 1, 1);
        float progress=Mathf.SmoothStep(0,1,Mathf.InverseLerp(RevealStart, duration, seconds));
        RevealProgress=progress;
        animated.SetFloat("_WorldReveal",progress);
        animated.SetFloat("_FlowTime",seconds);
        if(ticketLayer!=null)
        {
            ticketLayer.localScale=new Vector3(artwork.rectTransform.rect.width/1672f,artwork.rectTransform.rect.height/941f,1);
            if(!ticketsInitialized)
            {
                for(int child=0;child<ticketLayer.childCount;child++)
                {
                    int level=child+(trainUpgrade!=null&&child>=29?2:0);
                    var ticket=ticketLayer.GetChild(child);
                    var badge=ticket.GetComponentInChildren<WorkshopStationBadge>(true);
                    if(badge!=null){badge.SetLocked(level>30);badge.SetState(false,false);}
                    foreach(var number in ticket.GetComponentsInChildren<TMPro.TMP_Text>(true))number.color=new Color(.025f,.09f,.22f);
                }
                ticketsInitialized=true;
            }
        }
        if(worldTwoTickets!=null)for(int i=0;i<worldTwoTickets.Length;i++)
            if(worldTwoTickets[i]!=null)worldTwoTickets[i].alpha=Mathf.Lerp(.18f,1,Mathf.SmoothStep(0,1,(progress-i*.035f)/.18f));
        fade.alpha=1;
        if (trainUpgrade != null)
        {
            trainUpgrade.wholeMap=true;
            trainUpgrade.Sample(TrainAnimationTime(seconds), playSound);
        }
        while(playSound&&chimeSource!=null&&revealChime!=null&&nextChime<ChimeSteps.Length&&progress>=ChimeSteps[nextChime])
        {
            var audio=AudioManager.Instance;
            chimeSource.volume=audio!=null?audio.masterVolume*audio.sfxVolume*.45f:.45f;
            if(audio!=null)chimeSource.outputAudioMixerGroup=audio.sfxGroup;
            chimeSource.pitch=1+nextChime*.055f;
            chimeSource.PlayOneShot(revealChime);nextChime++;
        }
    }

    public IEnumerator Play()
    {
        float elapsed=0;
        confirmed=false;neutral=false;nextChime=0;WaitingForPlay=false;
        if(continueButton!=null){if(!preserveMapControls)continueButton.gameObject.SetActive(false);continueButton.interactable=false;continueButton.onClick.AddListener(Confirm);}
        gameObject.SetActive(true);
        while(elapsed<duration)
        {
            Sample(elapsed, true);
            elapsed+=Time.unscaledDeltaTime;
            yield return null;
        }
        Sample(duration);
        WaitingForPlay=true;
        if(continueButton!=null){continueButton.gameObject.SetActive(true);continueButton.interactable=true;}
        while(!confirmed)yield return null;
        WaitingForPlay=false;
        if(continueButton!=null)continueButton.onClick.RemoveListener(Confirm);
        gameObject.SetActive(false);
    }

    void Confirm(){if(WaitingForPlay)confirmed=true;}
    void Update()
    {
        if(!WaitingForPlay)return;
        bool held=Input.GetMouseButton(0)||Input.GetKey(KeyCode.Return)||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
        if(!neutral){if(!held)neutral=true;return;}
        if(Input.GetKeyDown(KeyCode.Return)||ArcadeInputAdapter.ConfirmDown())Confirm();
    }

    void OnDestroy()
    {
        if(animated==null)return;
        if(Application.isPlaying)Destroy(animated);else DestroyImmediate(animated);
    }
}
