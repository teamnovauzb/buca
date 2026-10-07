using UnityEngine;

/// <summary>One small hole-local celebration using only Editor-saved 3D meshes.</summary>
public sealed class ToyBoxWinCelebration : MonoBehaviour
{
    public Transform presentation, goldRing;
    public Transform[] sparkles;
    [Header("Saved wooden hole-in-one celebration")]
    public Transform woodenBanner;
    public GameObject fountainRoot;
    public ParticleSystem[] celebrationEffects;
    public AudioSource fireworkAudio;
    public AudioClip fireworkBoom;
    public float Duration => _holeInOne ? 2.85f : 1.6f;
    bool _holeInOne;
    Camera _viewCamera;
    public bool IsPlaying { get; private set; }
    float _elapsed;

    public void Play(Vector3 holePosition, bool holeInOne = false, Camera viewingCamera = null)
    {
        _elapsed=0; IsPlaying=true; _holeInOne=holeInOne && woodenBanner!=null;
        _viewCamera=viewingCamera!=null ? viewingCamera : Camera.main;
        presentation.position=holePosition;
        presentation.rotation=Quaternion.identity;
        presentation.gameObject.SetActive(true);
        if(woodenBanner!=null) woodenBanner.gameObject.SetActive(_holeInOne);
        if(fountainRoot!=null) fountainRoot.SetActive(_holeInOne);
        if(celebrationEffects!=null) foreach(var effect in celebrationEffects)
        {
            effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(_holeInOne) effect.Play(true);
        }
        if(fireworkAudio!=null)
        {
            fireworkAudio.Stop();
            if(_holeInOne && fireworkBoom!=null && Application.isPlaying)
            {
                var mixer=AudioManager.Instance;
                fireworkAudio.volume=mixer!=null ? mixer.masterVolume*mixer.sfxVolume*.8f : .8f;
                fireworkAudio.clip=fireworkBoom;
                fireworkAudio.Play();
            }
        }
        Sample(0);
    }

    void Update()
    {
        if(!IsPlaying) return;
        _elapsed+=Time.unscaledDeltaTime;
        Sample(_elapsed);
        if(_elapsed>=Duration) Hide();
    }

    public void Sample(float seconds)
    {
        if(_holeInOne)
        {
            goldRing.gameObject.SetActive(false);
            foreach(var sparkle in sparkles) sparkle.gameObject.SetActive(false);
            SampleBanner(seconds);
            return;
        }
        goldRing.gameObject.SetActive(true);
        foreach(var sparkle in sparkles) sparkle.gameObject.SetActive(true);
        float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(seconds/.12f));
        float exit=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.22f,Duration,seconds));
        presentation.localScale=Vector3.one;
        float ringSize=Mathf.Lerp(.7f,1.18f,Mathf.Clamp01(seconds/.4f))*enter*exit;
        goldRing.localScale=Vector3.one*ringSize;
        goldRing.localPosition=new Vector3(0,.50f+Mathf.Clamp01(seconds/Duration)*.38f,0);
        for(int i=0;i<sparkles.Length;i++)
        {
            float angle=i*Mathf.PI/3;
            float drift=Mathf.Clamp01(seconds/Duration);
            sparkles[i].localPosition=new Vector3(Mathf.Cos(angle)*(1.1f+drift*.35f),.65f+(i%3)*.24f+drift*.38f,Mathf.Sin(angle)*(1.1f+drift*.35f));
            sparkles[i].localRotation=Quaternion.Euler(0,i*53+seconds*70,0);
            float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-i*.025f)/.16f));
            float twinkle=.8f+.2f*Mathf.Sin(seconds*12+i);
            sparkles[i].localScale=Vector3.one*reveal*exit*twinkle;
        }
    }

    void SampleBanner(float seconds)
    {
        if(_viewCamera==null) return;
        float t=Mathf.Clamp01(seconds/.52f)-1f;
        float entrance=1f+2.1f*t*t*t+1.1f*t*t;
        float exit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.48f,Duration,seconds));
        const float distance=12f;
        float visibleWidth=_viewCamera.orthographic ? _viewCamera.orthographicSize*2*_viewCamera.aspect
            : 2*distance*Mathf.Tan(_viewCamera.fieldOfView*.5f*Mathf.Deg2Rad)*_viewCamera.aspect;
        float size=visibleWidth*.47f/8.8f;
        woodenBanner.position=_viewCamera.ViewportToWorldPoint(new Vector3(.5f,.44f-.13f*(1-entrance)-exit*.18f,distance));
        woodenBanner.rotation=_viewCamera.transform.rotation*Quaternion.Euler(6*(1-entrance),0,0);
        woodenBanner.localScale=Vector3.one*size*Mathf.Max(.001f,entrance*(1-exit));
    }

    public void Hide()
    {
        IsPlaying=false;
        if(fireworkAudio!=null)fireworkAudio.Stop();
        if(celebrationEffects!=null) foreach(var effect in celebrationEffects)
            if(effect!=null) effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(woodenBanner!=null) woodenBanner.gameObject.SetActive(false);
        presentation.gameObject.SetActive(false);
    }
    void OnDisable() { if(presentation!=null) Hide(); }
}
