using UnityEngine;

public sealed partial class ToyBoxResults3D
{
    public AudioSource cheerSource;
    public AudioClip happyCheer, holeInOneFanfare;
    public Transform[] celebrationStars;
    public Transform[] celebrationRays;
    float celebrationTime;
    bool fullCelebration;

    void BeginCelebration(bool holeInOne)
    {
        celebrationTime=0;
        fullCelebration=holeInOne;
        if(cheerSource!=null)
        {
            cheerSource.Stop();
            if(Application.isPlaying)
            {
                var audio=AudioManager.Instance;
                float volume=audio!=null?audio.masterVolume*audio.sfxVolume:1f;
                // Give normal wins a softer cheer; reserve the louder cheer for a hole in one.
                if(happyCheer!=null)cheerSource.PlayOneShot(happyCheer,(holeInOne?.8f:.4f)*volume);
                if(holeInOne && holeInOneFanfare!=null)
                    cheerSource.PlayOneShot(holeInOneFanfare,(happyCheer!=null?.45f:.8f)*volume);
            }
        }
        SampleCelebration(0);
    }

    void TickCelebration(float delta)
    {
        celebrationTime+=Mathf.Max(0,delta);
        SampleCelebration(celebrationTime);
    }

    // Animate Editor-saved geometry in viewport coordinates. The effect plane sits
    // behind the award, so the score and controls remain readable at every aspect.
    public void SampleCelebration(float seconds)
    {
        if(resultsCamera==null || presentation==null)return;
        var camera=resultsCamera;
        float depth=Vector3.Dot(presentation.transform.position-camera.transform.position,camera.transform.forward)+1.8f;
        float height=camera.orthographic?camera.orthographicSize*2:2*depth*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
        float aspect=Mathf.Max(.2f,camera.aspect);
        Quaternion facing=camera.transform.rotation;
        if(ShowingWellDone && !ScorecardShowing)wellDone.SampleConfetti(seconds);
        if(celebrationStars!=null) for(int i=0;i<celebrationStars.Length;i++)
        {
            var piece=celebrationStars[i];if(piece==null)continue;
            bool shower=i>=80;
            float seed=Fraction(i*.61803399f+.13f);
            float spread=Fraction(i*.38196601f+.37f);
            float delay=shower?.65f+seed*1.15f:.05f+(i%20)*.038f;
            float t=seconds-delay;
            float life=shower?3.45f:3.1f;
            bool visible=t>=0 && t<life && !ScorecardShowing && !ShowingWellDone && (fullCelebration || i%2==0);
            piece.gameObject.SetActive(visible);if(!visible)continue;
            float x,y;
            if(shower)
            {
                x=.025f+spread*.95f+Mathf.Sin(t*1.7f+i)*.028f;
                y=1.10f-(.13f+seed*.13f)*t-.028f*t*t;
            }
            else
            {
                float side=i%2==0?1:-1;
                x=(side>0?.015f:.985f)+side*(.12f+spread*.34f)*t;
                y=-.03f+(.67f+seed*.42f)*t-.23f*t*t;
            }
            piece.position=camera.ViewportToWorldPoint(new Vector3(x,y,depth));
            piece.rotation=facing*Quaternion.Euler(90+t*(90+seed*90),i*47+t*105,i*31+t*135);
            float envelope=Mathf.Clamp01(t/.10f)*Mathf.Clamp01((life-t)/.6f);
            float size=height*(.0065f+seed*.006f)*envelope;
            piece.localScale=Vector3.one*size;
        }
        if(celebrationRays!=null) for(int i=0;i<celebrationRays.Length;i++)
        {
            var ray=celebrationRays[i];if(ray==null)continue;
            int side=i/40, spoke=i%40;
            float t=seconds-(side==0?.32f:.64f);
            bool visible=fullCelebration && !ScorecardShowing && t>=0 && t<1.25f;
            ray.gameObject.SetActive(visible);if(!visible)continue;
            float angle=(spoke*9f+side*4.5f)*Mathf.Deg2Rad;
            float variation=.38f+Fraction(spoke*.618f)*.62f;
            float radius=(.012f+.28f*(1-Mathf.Exp(-t*2.7f)))*variation;
            float x=(side==0?.18f:.82f)+Mathf.Cos(angle)*radius/aspect;
            float y=(side==0?.73f:.79f)+Mathf.Sin(angle)*radius-.045f*t*t;
            ray.position=camera.ViewportToWorldPoint(new Vector3(x,y,depth+.15f));
            ray.rotation=facing*Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            float fade=Mathf.Pow(Mathf.Clamp01((1.25f-t)/1.25f),1.2f);
            float length=spoke%7==0?.006f:.028f+.085f*variation;
            ray.localScale=new Vector3(height*length*fade,height*.004f*fade,1);
        }
    }
    static float Fraction(float value) => value-Mathf.Floor(value);

    void StopCelebration()
    {
        if(cheerSource!=null)cheerSource.Stop();
        if(wellDone!=null)wellDone.StopConfetti();
        if(celebrationStars!=null)foreach(var item in celebrationStars)if(item!=null)item.gameObject.SetActive(false);
        if(celebrationRays!=null)foreach(var item in celebrationRays)if(item!=null)item.gameObject.SetActive(false);
    }
}
