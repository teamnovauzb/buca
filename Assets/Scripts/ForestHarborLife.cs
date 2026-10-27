using System;
using UnityEngine;

/// <summary>Quiet map-only water ambience and two independent harbor journeys.</summary>
public sealed class ForestHarborLife : MonoBehaviour
{
    [Serializable] public sealed class Boat
    {
        public RectTransform root, picture, reflection;
        public HarborWakeGraphic wake;
        [Tooltip("Closed water route in the map's original 1672 x 941 artwork pixels.")]
        public Vector2[] route;
        public float lapSeconds = 60, phase, nativeFacing = -1, bobPixels = 1.2f;
        [NonSerialized] internal Vector2[] samples;
        [NonSerialized] internal float[] lengths;
        [NonSerialized] internal float totalLength;
    }
    public Boat sailboat, tugboat;
    public AudioSource waterAudio;
    [Range(0,1)] public float waterVolume = .28f;
    public AudioSource riverAudio;
    [Range(0,1)] public float riverVolume = .34f;
    public AudioSource windAudio;
    [Range(0,1)] public float windVolume = .38f;
    float elapsed;
    bool focused = true, paused;

    void OnEnable()
    {
        focused = Application.isFocused;
        paused = false;
        Pose(elapsed);
        if (!Application.isPlaying) return;
        Silence();
    }
    void OnDisable()
    {
        Silence();
    }
    void OnApplicationFocus(bool value) { focused = value; if(!value) Silence(); }
    void OnApplicationPause(bool value) { paused = value; if(value) Silence(); }
    static void StopWater(AudioSource source) { if(source != null){source.Stop();source.volume=0;} }
    void Silence() { StopWater(waterAudio); StopWater(riverAudio); StopWater(windAudio); }
    void Update()
    {
        if (!Application.isPlaying) return;
        if(focused && !paused){elapsed += Time.unscaledDeltaTime;Pose(elapsed);}
        UpdateWater(Time.unscaledDeltaTime);
    }
    public float TargetWaterVolume()
        => TargetAmbienceVolume(waterVolume);
    public float TargetRiverVolume()
        => TargetAmbienceVolume(riverVolume);
    float TargetAmbienceVolume(float volume)
    {
        if (!focused || paused || !isActiveAndEnabled) return 0;
        var audio = AudioManager.Instance;
        return audio != null ? volume * audio.masterVolume * audio.sfxVolume * audio.ambientVolume : volume * .6f;
    }
    void UpdateWater(float dt)
    {
        UpdateSource(waterAudio,TargetWaterVolume(),dt);
        UpdateSource(riverAudio,TargetRiverVolume(),dt);
        UpdateSource(windAudio,TargetAmbienceVolume(windVolume),dt);
    }
    static void UpdateSource(AudioSource source,float target,float dt)
    {
        if(source == null || source.clip == null) return;
        var audio = AudioManager.Instance;
        if(audio != null) source.outputAudioMixerGroup = audio.sfxGroup;
        if(target <= .0001f){StopWater(source);return;}
        if(!source.isPlaying)source.Play();
        source.volume = Mathf.MoveTowards(source.volume,target,dt*.12f);
    }
    public void Pose(float seconds)
    {
        PoseBoat(sailboat,seconds); PoseBoat(tugboat,seconds,true);
        // Farther (higher) hulls render behind nearer ones when their routes approach.
        if(sailboat?.root != null && tugboat?.root != null)
        {
            if(sailboat.root.anchoredPosition.y > tugboat.root.anchoredPosition.y)sailboat.root.SetAsFirstSibling();
            else tugboat.root.SetAsFirstSibling();
        }
    }
    static void PoseBoat(Boat boat,float seconds,bool harborSwell=false)
    {
        if(boat?.root == null || boat.picture == null || boat.route == null || boat.route.Length<4)return;
        float phase = seconds / Mathf.Max(1,boat.lapSeconds) + boat.phase;
        Vector2 point = RoutePoint(boat,phase), ahead = RoutePoint(boat,phase+.001f), behind=RoutePoint(boat,phase-.001f);
        Vector2 direction=(ahead-behind).normalized;
        boat.root.anchoredPosition = new Vector2(point.x-836,470.5f-point.y);
        float bob = Mathf.Sin(seconds*1.6f+boat.phase*11)*boat.bobPixels;
        float roll=Mathf.Sin(seconds*1.1f+boat.phase*7)*.35f;
        float sway=0;
        if(harborSwell)
        {
            // Overlapping swells: roll leads the heavier hull's vertical response.
            // Slow amplitude variation prevents a metronomic left/right shake.
            float wave=seconds*1.55f+boat.phase*11;
            float swell=.84f+.16f*Mathf.Sin(seconds*.37f+.8f);
            roll=(3.6f*Mathf.Sin(wave)+.75f*Mathf.Sin(seconds*2.27f+1.2f))*swell;
            bob=1.25f*Mathf.Sin(wave-.65f)+.4f*Mathf.Sin(seconds*.73f+1.1f);
            sway=.85f*Mathf.Sin(wave-.3f);
        }
        boat.picture.anchoredPosition = new Vector2(sway,bob);
        // A mild perspective change keeps the boat readable through either turn.
        float facing = Mathf.Sign(direction.x)*boat.nativeFacing*Mathf.Lerp(.78f,1,Mathf.Abs(direction.x));
        boat.picture.localScale = new Vector3(facing,1,1);
        boat.picture.localRotation = Quaternion.Euler(0,0,-direction.y*1.8f+roll);
        float contactY=bob-boat.picture.sizeDelta.y*boat.picture.pivot.y+3;
        if(boat.reflection!=null)
        {
            boat.reflection.anchoredPosition=new Vector2(sway,contactY+1);
            boat.reflection.localScale=new Vector3(facing,1,1);
            boat.reflection.localRotation=Quaternion.Euler(0,0,-roll*.3f);
        }
        if(boat.wake != null)
        {
            boat.wake.rectTransform.anchoredPosition=new Vector2(sway*.5f,contactY+2);
            boat.wake.rectTransform.localRotation=Quaternion.Euler(0,0,roll*.15f);
            boat.wake.phase=Mathf.Repeat(seconds*.25f+boat.phase,1);
            boat.wake.direction=new Vector2(direction.x,-direction.y);
            boat.wake.SetVerticesDirty();
        }
    }
    public static Vector2 RoutePoint(Boat boat,float phase)
    {
        if(boat.samples == null) BakeRoute(boat);
        float distance=Mathf.Repeat(phase,1)*boat.totalLength;
        int i=Array.BinarySearch(boat.lengths,distance);if(i<0)i=~i;i=Mathf.Clamp(i,1,boat.lengths.Length-1);
        return Vector2.Lerp(boat.samples[i-1],boat.samples[i],Mathf.InverseLerp(boat.lengths[i-1],boat.lengths[i],distance));
    }
    static void BakeRoute(Boat boat)
    {
        const int subdivisions=32;int count=boat.route.Length;
        boat.samples=new Vector2[count*subdivisions+1];boat.lengths=new float[boat.samples.Length];
        for(int i=0;i<boat.samples.Length;i++)
        {
            int segment=i/subdivisions%count;float t=(i%subdivisions)/(float)subdivisions;
            var a=boat.route[(segment+count-1)%count];var b=boat.route[segment];var c=boat.route[(segment+1)%count];var d=boat.route[(segment+2)%count];
            boat.samples[i]=.5f*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
            if(i>0)boat.lengths[i]=boat.lengths[i-1]+Vector2.Distance(boat.samples[i-1],boat.samples[i]);
        }
        boat.totalLength=boat.lengths[boat.lengths.Length-1];
    }
}
