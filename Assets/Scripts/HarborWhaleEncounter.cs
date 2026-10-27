using System.Collections;
using UnityEngine;
public sealed class HarborWhaleEncounter : MonoBehaviour
{
    public CanvasGroup fade,whaleVisibility,greeting;
    public RectTransform stage,whale,tail,flipper,waterMask;
    public UnityEngine.UI.RawImage artwork;
    public HarborWhaleWater water;
    public CinematicMapDetails mapDetails;
    public AudioSource effects,voice,ambience;
    public AudioClip splash,hello,waterLoop;
    public bool firstGreeting=true;
    public int completedLevel=31;
    Material mapMaterial;
    public float Duration=>firstGreeting?7.2f:4.4f;
    public float LandingTime=>firstGreeting?3.75f:2.65f;
    public static bool IsWhaleLevel(int index)=>index>=30 && index<50;
    public void Sample(float t)
    {
        fade.alpha=Mathf.SmoothStep(0,1,t/.3f)*(1-Mathf.SmoothStep(0,1,(t-Duration+.4f)/.4f));
        mapDetails.level=completedLevel;mapDetails.Refresh();stage.localScale=mapDetails.controls.localScale;
        var number=mapDetails.controls.Find("WorkshopFlipSelector/Destination").GetComponent<TMPro.TMP_Text>();number.text=completedLevel.ToString();
        if(mapMaterial==null){mapMaterial=new Material(artwork.material);artwork.material=mapMaterial;}
        mapMaterial.SetFloat("_WorldReveal",1);mapMaterial.SetFloat("_FlowTime",t);
        const float surface=-105;
        float flight=LandingTime-.65f;
        float u=Mathf.Clamp01((t-.65f)/flight);
        float height=4*u*(1-u)*(firstGreeting?210:120);
        float entry=Mathf.Clamp01((t-LandingTime)/.65f);
        float lift=Mathf.SmoothStep(0,1,(t-.5f)/.35f);
        float x=510+Mathf.Lerp(-15,15,u);
        float y=surface+height+12-85*(1-lift)-180*entry;
        if(waterMask!=null)waterMask.anchoredPosition=new Vector2(0,surface+400);
        whale.anchoredPosition=new Vector2(x,y)-(waterMask!=null?waterMask.anchoredPosition:Vector2.zero);
        whale.localScale=Vector3.one*.46f;
        // The nose turns down as the body descends; the raised tail is last through the surface mask.
        float pitch=Mathf.Lerp(-22,72,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,1,u)));
        whale.localRotation=Quaternion.Euler(0,0,pitch);
        whaleVisibility.alpha=t>=.5f && entry<1?1:0;
        tail.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*11)*18*(1-entry));
        flipper.localRotation=Quaternion.Euler(0,0,-12+Mathf.Sin(t*9)*15);
        greeting.alpha=firstGreeting?Mathf.SmoothStep(0,1,(t-1.2f)/.3f)*(1-Mathf.SmoothStep(0,1,(t-2.7f)/.4f)):0;
        Vector2 blowhole=new Vector2(x,y)+(Vector2)(Quaternion.Euler(0,0,pitch)*new Vector3(25,105)*.46f);
        float rainbow=firstGreeting?Mathf.SmoothStep(0,1,(t-1.2f)/.35f)*(1-Mathf.SmoothStep(0,1,(t-2.5f)/.4f)):0;
        water.center=new Vector2(525,surface);
        water.Sample(t,LandingTime,blowhole,rainbow);
    }
    public IEnumerator Play(int level)
    {
        completedLevel=level;firstGreeting=level==31;gameObject.SetActive(true);
        var audio=AudioManager.Instance;float volume=audio!=null?audio.masterVolume*audio.sfxVolume:1;
        foreach(var source in new[]{effects,voice,ambience}){source.volume=volume;if(audio!=null)source.outputAudioMixerGroup=audio.sfxGroup;}
        ambience.clip=waterLoop;ambience.loop=true;ambience.volume=volume*.2f;ambience.Play();
        bool launched=false,landed=false,greeted=false;float land=LandingTime;
        for(float t=0;t<Duration;t+=Time.unscaledDeltaTime)
        {
            Sample(t);
            if(t>=.65f&&!launched){effects.PlayOneShot(splash,.65f);launched=true;}
            if(t>=land&&!landed){effects.PlayOneShot(splash,.95f);landed=true;}
            if(firstGreeting&&t>=1.45f&&!greeted){voice.PlayOneShot(hello);greeted=true;}
            yield return null;
        }
        gameObject.SetActive(false);
    }
    void OnDisable(){if(effects!=null)effects.Stop();if(voice!=null)voice.Stop();if(ambience!=null)ambience.Stop();}
    void OnDestroy(){if(mapMaterial!=null){if(Application.isPlaying)Destroy(mapMaterial);else DestroyImmediate(mapMaterial);}}
}
