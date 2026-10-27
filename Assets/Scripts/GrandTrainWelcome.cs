using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class GrandTrainWelcome : MonoBehaviour
{
    public CanvasGroup fade, controls;
    public RectTransform train;
    public RectTransform[] animals, wheels, tickets;
    public RectTransform[] ticketGrips;
    public Vector2[] animalPositions, handPositions;
    public RawImage artwork;
    public RabbitGiftCelebration celebration;
    public TMP_Text headline, subtitle;
    public Button continueButton;
    public AudioSource voice, effects, trainSound;
    public AudioClip trainLoop;
    public GrandAnimalFace[] faces;
    public AudioClip firstLine, welcomeLine, cheer, arrival;
    public bool automaticJourney;
    public const float Duration=12f;
    Material mapMaterial;
    bool confirmed;
    public void Sample(float t)
    {
        var mapDetails=GetComponent<CinematicMapDetails>();if(mapDetails!=null)mapDetails.Refresh();
        if(faces!=null)foreach(var face in faces)face.Sample(t);
        fade.alpha=Mathf.SmoothStep(0,1,t/.2f);
        float enter=1-Mathf.Pow(1-Mathf.Clamp01(t/.85f),3);
        float settle=t>.85f?Mathf.Sin((t-.85f)*16)*Mathf.Exp(-(t-.85f)*5)*24:0;
        train.anchoredPosition=new Vector2(2300*(1-enter)+settle,-120);
        train.localRotation=Quaternion.Euler(0,0,-1.5f*Mathf.Sin(t*12)*(1-enter));
        foreach(var wheel in wheels)wheel.localRotation=Quaternion.Euler(0,0,enter*1080);
        for(int i=0;i<animals.Length;i++)
        {
            float bounce=Mathf.Abs(Mathf.Sin(t*5+i*.6f))*12*Mathf.SmoothStep(0,1,t-.65f);
            animals[i].anchoredPosition=animalPositions[i]+new Vector2(0,bounce);
            animals[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*4+i)*2.5f);
            float release=4.8f+i*.48f;
            float p=Mathf.Clamp01((t-release)/1.35f);
            float offer=Mathf.Sin(Mathf.Clamp01((t-release+.4f)/.8f)*Mathf.PI);
            animals[i].localScale=Vector3.one*(1+.035f*offer);
            Vector2 localHand=handPositions[i]-animalPositions[i];
            Vector3 handWorld=animals[i].TransformPoint(localHand);
            Vector2 hand=((RectTransform)tickets[i].parent).InverseTransformPoint(handWorld);
            Vector2 start=hand+new Vector2(24,17);
            Vector2 end=new Vector2((i-2)*195,-225);
            float travel=Mathf.SmoothStep(0,1,p);
            tickets[i].anchoredPosition=Vector2.Lerp(start,end,travel)+new Vector2(0,Mathf.Sin(p*Mathf.PI)*65);
            tickets[i].localScale=Vector3.one*Mathf.Lerp(.48f,1.1f,travel);
            tickets[i].localRotation=Quaternion.Euler(0,0,Mathf.LerpAngle(animals[i].localEulerAngles.z,(i-2)*7,travel));
            tickets[i].gameObject.SetActive(true);
            if(ticketGrips!=null && i<ticketGrips.Length)
            {
                var grip=ticketGrips[i];
                grip.gameObject.SetActive(p<=0);
                grip.position=handWorld;grip.rotation=animals[i].rotation;
                grip.localScale=animals[i].localScale;
            }
            tickets[i].GetComponent<WorkshopStationBadge>().SetLocked(false);
        }
        headline.text=t<7.8f?"YOU DID IT!":automaticJourney?"ALL ABOARD!":"WELCOME TO WORLD 2!";
        subtitle.text=t<2.8f?"YEEEEY!":t<7.8f?"HERE IS YOUR TICKET!":"AMAZING! LET’S GO!";
        headline.alpha=subtitle.alpha=Mathf.SmoothStep(0,1,(t-.8f)/.5f);
        controls.alpha=automaticJourney?0:Mathf.SmoothStep(0,1,(t-11.5f)/.5f);
        controls.blocksRaycasts=controls.interactable=!automaticJourney && t>=Duration;
        if(mapMaterial==null){mapMaterial=new Material(artwork.material);artwork.material=mapMaterial;}
        mapMaterial.SetFloat("_WorldReveal",automaticJourney?0:Mathf.SmoothStep(0,1,(t-7.8f)/3f));
        mapMaterial.SetFloat("_FlowTime",t);
        celebration.Sample(t+2.1f);
    }
    public IEnumerator Play()
    {
        gameObject.SetActive(true);confirmed=false;continueButton.onClick.AddListener(Confirm);
        var audio=AudioManager.Instance;
        foreach(var source in new[]{voice,effects}) {source.volume=audio!=null?audio.masterVolume*audio.sfxVolume:.8f;if(audio!=null)source.outputAudioMixerGroup=audio.sfxGroup;}
        if(arrival!=null)effects.PlayOneShot(arrival,.55f);
        if(trainSound!=null && trainLoop!=null)
        {
            trainSound.clip=trainLoop;trainSound.loop=true;trainSound.pitch=1.35f;
            if(audio!=null)trainSound.outputAudioMixerGroup=audio.sfxGroup;
            trainSound.Play();
        }
        bool yay=false,welcome=false;
        float t=0;
        while(!confirmed && (!automaticJourney || t<Duration))
        {
            Sample(t);
            if(trainSound!=null)
            {
                trainSound.volume=(audio!=null?audio.masterVolume*audio.sfxVolume:1)*.85f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.25f,t)));
                if(t>=1.25f&&trainSound.isPlaying)trainSound.Stop();
            }
            if(t>=1&&!yay){effects.PlayOneShot(cheer);yay=true;}
            if(t>=7.8f&&!welcome){effects.PlayOneShot(cheer,.8f);welcome=true;}
            t+=Time.unscaledDeltaTime;yield return null;
        }
        controls.blocksRaycasts=false;
        if(!automaticJourney)
            for(float f=0;f<.3f;f+=Time.unscaledDeltaTime){fade.alpha=1-f/.3f;yield return null;}
        continueButton.onClick.RemoveListener(Confirm);gameObject.SetActive(false);
    }
    void Confirm(){if(controls.interactable)confirmed=true;}
    void OnDisable(){if(voice!=null)voice.Stop();if(effects!=null)effects.Stop();if(trainSound!=null)trainSound.Stop();}
    void OnDestroy(){if(mapMaterial!=null)Destroy(mapMaterial);}
}
