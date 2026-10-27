using System;
using UnityEngine;

/// <summary>Scene-owned curtain survives the map closing and blocks input until Home is revealed.</summary>
public sealed class MenuReturnTransition : MonoBehaviour
{
    public Canvas overlay;
    public UnityEngine.UI.Image curtain;
    public Camera homeCamera;
    public AudioClip sweepSound;
    public bool Busy { get; private set; }
    public const float CoverSeconds=.28f, RevealSeconds=.48f;
    float elapsed, restFov;
    bool swapped;
    Action revealHome;
    static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}

    public void Begin(Action showHome)
    {
        if(Busy)return;
        Busy=true;swapped=false;elapsed=0;revealHome=showHome;
        restFov=homeCamera!=null?homeCamera.fieldOfView:0;
        if(overlay!=null)overlay.gameObject.SetActive(true);
        SetCover(0);
        if(Application.isPlaying && AudioManager.Instance!=null)AudioManager.Instance.PlaySfx(sweepSound,.42f);
    }
    void Update(){if(Application.isPlaying && Busy)Tick(Time.unscaledDeltaTime);}
    public void Tick(float dt)
    {
        if(!Busy)return;
        elapsed+=Mathf.Max(0,dt);
        if(!swapped)
        {
            SetCover(Ease(elapsed/CoverSeconds));
            if(elapsed<CoverSeconds)return;
            // Swap only under an opaque curtain, after the lever has finished moving.
            SetCover(1);swapped=true;
            var action=revealHome;revealHome=null;action?.Invoke();
        }
        float t=Mathf.Clamp01((elapsed-CoverSeconds)/RevealSeconds);
        if(homeCamera!=null)homeCamera.fieldOfView=restFov*Mathf.Lerp(1.035f,1,Ease(t));
        SetCover(1-Ease(t));
        if(t>=1)Finish();
    }
    void SetCover(float alpha){if(curtain!=null)curtain.color=new Color(.018f,.035f,.055f,alpha);}
    void Finish()
    {
        if(Busy && homeCamera!=null)homeCamera.fieldOfView=restFov;
        Busy=false;revealHome=null;SetCover(0);
        if(overlay!=null)overlay.gameObject.SetActive(false);
    }
    void OnDisable()=>Finish();
}
