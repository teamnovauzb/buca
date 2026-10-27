using UnityEngine;

/// <summary>Unscaled, camera-relative campaign milestone; never moves the level trays.</summary>
public sealed class WorldVictoryCelebration3D : MonoBehaviour
{
    public Transform presentation;
    public UnityEngine.Rendering.Volume celebrationVolume;
    public Transform[] stars, confetti, rays;
    public Transform[] fireworks, floatingStars, ribbons;
    public AudioClip celebrationSound;
    public AudioClip cheeringSound;
    public AudioClip fireworkBurstSound;
    AudioSource burstSource;
    float previousBurstTime;
    public bool arrivalTicket;
    public float Duration => 6.3f;
    public bool Playing { get; private set; }
    float started;
    public void Play(AudioSource source)
    {
        started=Time.unscaledTime;Playing=true;gameObject.SetActive(true);
        burstSource=source;previousBurstTime=-1;
        if(source!=null&&celebrationSound!=null)source.PlayOneShot(celebrationSound,.85f);
        if(source!=null&&cheeringSound!=null)source.PlayOneShot(cheeringSound,.75f);
        Sample(0);
    }
    public void Stop(){if(celebrationVolume!=null)celebrationVolume.weight=0;Playing=false;gameObject.SetActive(false);}
    public bool Tick()
    {
        float t=Time.unscaledTime-started;Sample(t);
        PlayFireworkBursts(t);
        if(t<Duration)return false;
        Stop();return true;
    }
    void PlayFireworkBursts(float t)
    {
        bool crossed=false;
        int count=fireworks==null?0:(fireworks.Length+23)/24;
        for(int burst=0;burst<count;burst++)for(int repeat=0;repeat<2;repeat++)
        {
            float cue=.25f+burst*.30f+repeat*2.3f;
            if(cue<5.6f && previousBurstTime<cue && t>=cue && t-cue<.2f)crossed=true;
        }
        if(crossed && Playing && burstSource!=null && fireworkBurstSound!=null)
            burstSource.PlayOneShot(fireworkBurstSound,.55f);
        previousBurstTime=t;
    }
    public void Sample(float t)
    {
        float entry=Mathf.Clamp01(t/.85f);
        float pop=1+Mathf.Pow(1-entry,3)*Mathf.Sin(entry*10-1.57f);
        float leave=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-5.35f)/.95f));
        if(celebrationVolume!=null)celebrationVolume.weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.45f))*leave;
        presentation.localScale=Vector3.one*Mathf.Max(.001f,pop*leave);
        presentation.localPosition=new Vector3(0,.10f*Mathf.Sin(t*1.3f),0);
        if(arrivalTicket)presentation.localRotation=Quaternion.Euler(0,Mathf.Sin(t*1.1f)*3,5+Mathf.Sin(t*1.3f)*1.5f);
        for(int i=0;i<stars.Length;i++)
        {
            float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.35f-i*.18f)/.40f));
            stars[i].localScale=Vector3.one*reveal;
            stars[i].localRotation=Quaternion.Euler(0,Mathf.Sin(t*2+i)*12,Mathf.Sin(t*1.2f+i)*5);
        }
        for(int i=0;i<confetti.Length;i++)
        {
            float delay=(i%3)*.35f+(i%11)*.025f,age=t-delay;
            bool active=age>=0&&age<5.6f;confetti[i].gameObject.SetActive(active);if(!active)continue;
            float side=i%2==0?-1:1;float seed=Mathf.Repeat(i*.618034f,1);
            confetti[i].localPosition=new Vector3(side*(4.6f-age*(.35f+seed*2.8f))+.40f*Mathf.Sin(i*12.7f), -2.8f+age*(3.3f+Mathf.Repeat(i*.317f,1)*3.4f)-.82f*age*age, -.6f-seed*1.2f);
            var position=confetti[i].localPosition;
            // Keep the milestone and its three stars unobstructed.
            if(position.y>-.15f&&Mathf.Abs(position.x)<2.05f)position.x=(i%2==0?-1:1)*(2.15f+seed*.7f);
            confetti[i].localPosition=position;
            confetti[i].localRotation=Quaternion.Euler(age*(100+i%8*30),age*120,i*37+age*95);
        }
        if(fireworks!=null)for(int i=0;i<fireworks.Length;i++)
        {
            int burst=i/24, spoke=i%24;
            float age=t-(.25f+burst*.30f);
            if(age>2.1f)age-=2.3f;
            bool visible=age>=0&&age<2.1f&&t<5.6f;
            fireworks[i].gameObject.SetActive(visible);if(!visible)continue;
            float a=spoke*Mathf.PI*2/24;
            Vector3 center=new Vector3((burst%2==0?-1:1)*(3.1f+burst%3*.25f),.6f+(burst%3)*.72f,.4f);
            float radius=age*(1.8f-age*.28f);
            fireworks[i].localPosition=center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius-age*age*.20f,0);
            var sparkPosition=fireworks[i].localPosition;
            if(sparkPosition.y>-.15f&&Mathf.Abs(sparkPosition.x)<2.05f)sparkPosition.x=(burst%2==0?-1:1)*2.2f;
            fireworks[i].localPosition=sparkPosition;
            fireworks[i].localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90);
            fireworks[i].localScale=Vector3.one*(1-Mathf.Clamp01(age/2.1f));
        }
        if(floatingStars!=null)for(int i=0;i<floatingStars.Length;i++)
        {
            float seed=Mathf.Repeat(i*.618f,1),side=i%2==0?-1:1;
            floatingStars[i].localPosition=new Vector3(side*(3.5f+seed*.9f),2.8f-Mathf.Repeat(t*.65f+i*.37f,5.5f),-.35f);
            floatingStars[i].localRotation=Quaternion.Euler(0,t*55+i*17,t*23+i*31);
            floatingStars[i].localScale=Vector3.one*(.07f+seed*.09f)*leave*Mathf.Clamp01(t*2);
        }
        if(ribbons!=null)for(int i=0;i<ribbons.Length;i++)
        {
            float side=i%2==0?-1:1,seed=Mathf.Repeat(i*.618f,1);
            ribbons[i].localPosition=new Vector3(side*(3.8f+seed*.7f)+.15f*Mathf.Sin(t*2+i),3.3f-Mathf.Repeat(t*.9f+i*.51f,6.5f),-.8f);
            ribbons[i].localRotation=Quaternion.Euler(15,t*65+i*33,side*30+Mathf.Sin(t+i)*25);
            ribbons[i].localScale=Vector3.one*leave*Mathf.Clamp01(t*2);
        }
        for(int i=0;i<rays.Length;i++)
        {
            float pulse=Mathf.Exp(-Mathf.Max(0,t-.25f)*1.15f)+.20f;
            rays[i].localScale=new Vector3(1,Mathf.Max(.01f,pulse),1);
        }
    }
}
