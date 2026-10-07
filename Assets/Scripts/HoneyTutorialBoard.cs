using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
/// <summary>A short real-time physics demonstration, shown before Level 7 starts.</summary>
public sealed class HoneyTutorialBoard : MonoBehaviour
{
    public GameObject stage;
    public AudioSource tutorialAudio;
    public AudioClip voice, enterSound, clickSound, launchSound, winSound;
    public Vector3 buttonPressDirection=Vector3.forward;
    public float honeyDepth=.65f;
    void Cue(AudioClip clip,float volume=1){if(tutorialAudio && clip)tutorialAudio.PlayOneShot(clip,volume);}
    public GameObject roomStage;
    public RenderTexture roomTexture;
    public Transform pouringDipper, pourStream, honeyVisual;
    public bool singleBoard;
    public float LastWeakX {get;private set;}
    public float LastStrongX {get;private set;}
    public Transform shootButton;
    public GameObject aimGuide;
    public CanvasGroup group;
    public RectTransform card;
    public Rigidbody demoPuck;
    public IcePatch honey;
    public Rigidbody secondPuck;
    public IcePatch secondHoney;
    public Renderer[] secondDots;
    public Renderer[] chargeDots;
    public Material lit, dim;
    public TMPro.TMP_Text caption;
    public RawImage backdrop;
    public RenderTexture blurTexture, blurScratch;
    public Material blurMaterial;
    public bool IsShowing {get;private set;}
    public void Begin(Action finished){if(IsShowing)return;StartCoroutine(Show(finished));}
    IEnumerator Show(Action finished)
    {
        IsShowing=true;group.alpha=0;stage.SetActive(false);
        if(roomStage)roomStage.SetActive(true);
        if(aimGuide)aimGuide.SetActive(false);
        if(honeyVisual)honeyVisual.localScale=new Vector3(.03f,.03f,honeyDepth);
        if(pouringDipper)pouringDipper.localPosition=Vector3.zero;
        if(pourStream)pourStream.localScale=Vector3.one;
        yield return new WaitForEndOfFrame();
        if(blurTexture!=null && blurScratch!=null && blurMaterial!=null)
        {
            if(roomTexture!=null)Graphics.Blit(roomTexture,blurTexture);
            for(int i=0;i<2;i++)
            {
                blurMaterial.SetVector("_BlurDirection",new Vector4(2,0,0,0));Graphics.Blit(blurTexture,blurScratch,blurMaterial);
                blurMaterial.SetVector("_BlurDirection",new Vector4(0,2,0,0));Graphics.Blit(blurScratch,blurTexture,blurMaterial);
            }
        }
        backdrop.texture=blurTexture;stage.SetActive(true);group.blocksRaycasts=true;demoPuck.isKinematic=true;
        ResetPuck();
        if(!singleBoard && secondPuck!=null){secondPuck.isKinematic=true;secondPuck.transform.localPosition=new Vector3(-3.2f,-1.05f,-.9f);secondPuck.transform.localScale=Vector3.one;secondHoney.patchDamping=honey.patchDamping;secondHoney.enabled=true;}
        caption.text=pouringDipper?"STICKY HONEY":"HOLD, THEN RELEASE";
        Cue(enterSound,.45f);
        yield return Slide(true,.55f);
        if(!pouringDipper && honeyVisual)honeyVisual.localScale=new Vector3(1,1,honeyDepth);
        Cue(voice,.85f);
        if(pouringDipper)yield return PourReveal();
        if(singleBoard) yield return MiniatureDemo();
        else for(int shotIndex=0;shotIndex<2;shotIndex++)
        {
            var moving=shotIndex==0?demoPuck:secondPuck;
            var patch=shotIndex==0?honey:secondHoney;
            patch.enabled=true;caption.text="HONEY SLOWS YOU";
            yield return new WaitForSecondsRealtime(.25f);
            moving.isKinematic=false;moving.linearDamping=.9f;moving.linearVelocity=Vector3.right*(shotIndex==0?5.5f:12f);
            float until=Time.realtimeSinceStartup+1.75f;
            while(Time.realtimeSinceStartup<until)
            {
                if(moving.transform.localPosition.x>3.1f){if(!moving.isKinematic){moving.linearVelocity=Vector3.zero;moving.isKinematic=true;}moving.transform.localScale=Vector3.Lerp(moving.transform.localScale,Vector3.zero,Time.unscaledDeltaTime*12);}
                yield return null;
            }
        }
        yield return Slide(false,.3f);demoPuck.isKinematic=true;secondPuck.isKinematic=true;honey.enabled=false;secondHoney.enabled=false;stage.SetActive(false);group.blocksRaycasts=false;IsShowing=false;Release();finished?.Invoke();
    }
    void ResetPuck(){demoPuck.isKinematic=true;demoPuck.transform.localPosition=singleBoard?new Vector3(-3.2f,0,-.70f):new Vector3(-3.2f,.7f,-.9f);demoPuck.transform.localScale=Vector3.one;demoPuck.position=demoPuck.transform.position;}
    IEnumerator PourReveal()
    {
        float began=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup-began<.8f)
        {
            float t=Mathf.Clamp01((Time.realtimeSinceStartup-began)/.8f);
            float scale=Mathf.SmoothStep(.03f,1,t);
            honeyVisual.localScale=new Vector3(scale,scale,honeyDepth);
            pourStream.localScale=new Vector3(.8f+Mathf.Sin(t*12)*.12f,1,.8f+Mathf.Sin(t*12)*.12f);
            yield return null;
        }
        honeyVisual.localScale=new Vector3(1,1,honeyDepth);
        began=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup-began<.18f)
        {
            float t=(Time.realtimeSinceStartup-began)/.18f;
            pourStream.localScale=new Vector3(1-t,1,1-t);
            pouringDipper.localPosition=Vector3.up*t*.12f;
            yield return null;
        }
        pourStream.localScale=Vector3.zero;
    }
    IEnumerator MiniatureDemo()
    {
        honey.enabled=true;caption.text="HOLD, THEN RELEASE";
        var rest=shootButton.localPosition;
        // A short charge visibly fails, then a longer charge demonstrates the solution.
        for(int attempt=0;attempt<2;attempt++)
        {
            ResetPuck();
            if(aimGuide)aimGuide.SetActive(true);
            float chargeTime=attempt==0?.6f:1.05f;
            Cue(clickSound,.45f);
            float began=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-began<chargeTime)
            {
                float t=(Time.realtimeSinceStartup-began)/chargeTime;
                int count=Mathf.CeilToInt(t*(attempt==0?3:12));
                for(int i=0;i<chargeDots.Length;i++)chargeDots[i].sharedMaterial=i<count?lit:dim;
                shootButton.localPosition=rest+buttonPressDirection*.06f;
                yield return null;
            }
            shootButton.localPosition=rest;
            Cue(launchSound,.5f);
            if(aimGuide)aimGuide.SetActive(false);
            demoPuck.isKinematic=false;demoPuck.linearDamping=.9f;demoPuck.linearVelocity=Vector3.right*(attempt==0?5.5f:14f);
            float until=Time.realtimeSinceStartup+(attempt==0?1.25f:1.65f);
            while(Time.realtimeSinceStartup<until)
            {
                if(demoPuck.transform.localPosition.x>3.1f)
                {
                    if(!demoPuck.isKinematic){demoPuck.linearVelocity=Vector3.zero;demoPuck.isKinematic=true;Cue(winSound,.6f);}
                    demoPuck.transform.localScale=Vector3.Lerp(demoPuck.transform.localScale,Vector3.zero,Time.unscaledDeltaTime*12);
                }
                yield return null;
            }
            if(attempt==0)LastWeakX=demoPuck.transform.localPosition.x;else LastStrongX=demoPuck.transform.localPosition.x;
        }
    }
    IEnumerator Slide(bool entering,float duration)
    {
        float start=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup-start<duration)
        {
            float t=Mathf.Clamp01((Time.realtimeSinceStartup-start)/duration);float ease=entering?1+2.2f*Mathf.Pow(t-1,3)+1.2f*Mathf.Pow(t-1,2):1-Mathf.Pow(1-t,3);
            card.anchoredPosition=new Vector2(0,entering?Mathf.LerpUnclamped(-1000,0,ease):Mathf.Lerp(0,-1000,t*t));
            card.localRotation=Quaternion.Euler(0,0,entering?Mathf.LerpUnclamped(-4,0,ease):t*2);
            card.localScale=Vector3.one*(entering?Mathf.LerpUnclamped(.92f,1,ease):Mathf.Lerp(1,.96f,t));
            group.alpha=entering?Mathf.Clamp01(t*3):1-t;yield return null;
        }
        card.anchoredPosition=new Vector2(0,entering?0:-1000);group.alpha=entering?1:0;card.localScale=Vector3.one;card.localRotation=Quaternion.identity;
    }
    void Release(){backdrop.texture=null;}
    void OnDisable(){if(tutorialAudio)tutorialAudio.Stop();if(roomStage)roomStage.SetActive(false);if(stage!=null)stage.SetActive(false);if(IsShowing){StopAllCoroutines();IsShowing=false;Release();}}
}
