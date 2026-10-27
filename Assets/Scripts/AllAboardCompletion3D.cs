using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Prebuilt toy station shown once at the end of a campaign.</summary>
public sealed class AllAboardCompletion3D : MonoBehaviour
{
    public GameObject presentation;
    public GameObject firstTitle, bonusTitle, nextLabel, replayLabel, bonusHint;
    public Camera stationCamera;
    public Transform station, train, nextCap, menuCap;
    public Transform[] wheels, sparks;
    public MeshFilter[] digits;
    public Mesh[] numeralMeshes;
    public Collider nextHit, menuHit;
    public AudioSource sound;
    public AudioClip fanfare, fireworks, tick, chime;
    public Vector3 trainRest;
    public float trainHeading;
    public Vector3 cameraPosition, cameraFocus;
    public Transform cityTrain;
    public Vector3 cityTrainRest;
    public Volume presentationVolume;
    public bool IsShowing => presentation != null && presentation.activeSelf;
    public int DisplayedScore { get; private set; }
    public bool ActionsReady => IsShowing && elapsed >= 6.8f && !chosen;
    Action nextAction, menuAction;
    float elapsed, lastTick;
    int score;
    bool chosen, neutral, chimed;

    public void Show(int total, Action next, Action menu, bool bonusComplete = false, bool campaignComplete = false)
    {
        firstTitle.SetActive(!bonusComplete);bonusTitle.SetActive(bonusComplete);
        bool replay = bonusComplete || campaignComplete;
        nextLabel.SetActive(!replay);replayLabel.SetActive(replay);bonusHint.SetActive(!replay);
        score=Mathf.Max(0,total);nextAction=next;menuAction=menu;
        elapsed=0;lastTick=0;chosen=false;neutral=false;chimed=false;
        presentation.SetActive(true);
        Sample(0);Play(fanfare,.65f);
    }
    public void Hide()
    {
        if(sound!=null)sound.Stop();
        presentation.SetActive(false);nextAction=null;menuAction=null;
    }
    void Play(AudioClip clip,float volume)
    {
        if(sound==null || clip==null || !Application.isPlaying)return;
        var audio=AudioManager.Instance;
        sound.PlayOneShot(clip,volume*(audio!=null?audio.masterVolume*audio.sfxVolume:1f));
    }
    void Update()
    {
        if(!IsShowing)return;
        elapsed+=Time.unscaledDeltaTime;if(!chosen)Sample(elapsed);
        if(elapsed>1.4f && elapsed<4.6f && elapsed-lastTick>.095f)
        {lastTick=elapsed;Play(tick,.25f);}
        if(!chimed && elapsed>=4.6f){chimed=true;Play(chime,.7f);Play(fireworks,.5f);}
        bool held=Input.GetMouseButton(0)||Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.Return)
            ||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Red);
        if(!ActionsReady){neutral=false;return;}
        if(!neutral){neutral=!held;return;}
        bool clicked=Input.GetMouseButtonDown(0);
        var ray=stationCamera.ScreenPointToRay(Input.mousePosition);
        if(ArcadeInputAdapter.ConfirmDown()||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space)
            ||clicked&&nextHit.Raycast(ray,out _,100))Choose(false);
        else if(ArcadeInputAdapter.CancelDown()||Input.GetKeyDown(KeyCode.Escape)
            ||clicked&&menuHit.Raycast(ray,out _,100))Choose(true);
    }
    public void Choose(bool menu)
    {
        if(!ActionsReady)return;
        chosen=true;
        if(AudioManager.Instance!=null)AudioManager.Instance.PlayButtonClick();
        var action=menu?menuAction:nextAction;
        StartCoroutine(Depart(menu, action));
    }
    IEnumerator Depart(bool menu, Action action)
    {
        var cap=menu?menuCap:nextCap;
        Vector3 rest=cap.localPosition;cap.localPosition=rest+Vector3.forward*.10f;
        yield return new WaitForSecondsRealtime(.12f);cap.localPosition=rest;
        if(!menu)
        {
            Play(fanfare,.35f);
            float t=0;
            while(t<.8f)
            {
                t+=Time.unscaledDeltaTime;PositionTrain(12*t*t);
                foreach(var wheel in wheels)wheel.localRotation=Quaternion.Euler(0,0,-t*t*900);
                yield return null;
            }
        }
        Hide();action?.Invoke();
    }
    void PositionTrain(float distance)
    {
        float slope=Mathf.Tan(trainHeading*Mathf.Deg2Rad);
        train.localPosition=trainRest+new Vector3(distance,0,-slope*distance+.055f*distance*distance);
        train.localRotation=Quaternion.Euler(0,Mathf.Atan(slope-.11f*distance)*Mathf.Rad2Deg,0);
    }
    // Also used by Editor previews; no transient meshes/materials or particle allocations.
    public void Sample(float seconds)
    {
        // Preserve the composition on narrow displays by moving the same perspective camera back.
        float fit=Mathf.Max(1,(16f/9f)/Mathf.Max(.3f,stationCamera.aspect));
        stationCamera.transform.localPosition=cameraFocus+(cameraPosition-cameraFocus)*fit;
        stationCamera.transform.localRotation=Quaternion.LookRotation(cameraFocus-stationCamera.transform.localPosition);
        float arrive=Mathf.SmoothStep(0,1,seconds/1.35f);
        station.localPosition=new Vector3(0,(1-arrive)*-1.2f,0);
        float travel=(1-Mathf.SmoothStep(0,1,(seconds-.25f)/2.0f))*10;
        PositionTrain(-travel);
        foreach(var wheel in wheels)wheel.localRotation=Quaternion.Euler(0,0,travel*90);
        if(cityTrain!=null)cityTrain.localPosition=cityTrainRest+Vector3.right*Mathf.Repeat(seconds*.55f,36);
        float count=Mathf.Clamp01((seconds-1.4f)/3.2f);
        DisplayedScore=Mathf.RoundToInt(score*(1-Mathf.Pow(1-count,3)));
        string value=DisplayedScore.ToString().PadLeft(6,'0');
        for(int i=0;i<digits.Length;i++)
        {
            int index=value.Length-digits.Length+i;
            digits[i].sharedMesh=numeralMeshes[index>=0?value[index]-'0':0];
        }
        nextCap.localScale=Vector3.one*(seconds>=5?1+.012f*Mathf.Sin(seconds*2):1);
        for(int i=0;i<sparks.Length;i++)
        {
            int burst=i/32;float t=seconds-(5.15f+burst*.18f);
            bool visible=t>=0&&t<2.1f;sparks[i].gameObject.SetActive(visible);
            if(!visible)continue;
            float angle=(i%32)*Mathf.PI/16;float speed=1.6f+(i%5)*.24f;
            Vector3 center=new Vector3(burst%2==0?-7.2f:7.2f,3.4f+(burst/2)*.8f,-.6f);
            sparks[i].localPosition=center+new Vector3(Mathf.Cos(angle)*speed*t,Mathf.Sin(angle)*speed*t-.6f*t*t,0);
            sparks[i].localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            float fade=Mathf.Clamp01((2.1f-t)/.7f);
            sparks[i].localScale=new Vector3(.13f,.035f,.035f)*fade;
        }
    }
}
