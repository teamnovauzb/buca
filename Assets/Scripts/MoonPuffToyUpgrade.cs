using UnityEngine;
using UnityEngine.UI;

/// <summary>A sampled, unscaled star-pixel transformation followed by a journey along the actual 30–31 railway.</summary>
public sealed class MoonPuffToyUpgrade : MonoBehaviour
{
    public const float Duration = 9.3f;
    public const float DepartureTime = 5.65f, ArrivalTime = 8.6f;
    public RawImage artwork, trainImage;
    public RectTransform ticket30, ticket31;
    public Image ticket31Face;
    public RectTransform emeraldLevelBadge;
    public TMPro.TMP_Text emeraldLevelNumber;
    public Color lockedTicket, arrivedTicket;
    public Camera trainCamera;
    public SuitcaseTrainMechanism mechanism;
    public MoonPuffToyParts parts;
    public Transform originalRoof;
    public Transform[] sparkles;
    public Vector3[] route;
    public float[] wheelTravel;
    public float trainScale = 1.45f, displayScale = .725f;
    public bool wholeMap;
    public AudioSource motionAudio, accentsAudio;
    public AudioClip chug, click, readyChime, finishingChuk;

    StarPixelTrainMorph pixelMorph;
    RenderTexture texture;
    Transform renderStage;
    Vector3 roofRest;
    Vector3[] wheelRest;
    MaterialPropertyBlock lampProperties;
    float lastSeconds = -1;
    int accent;
    static readonly float[] ClickTimes = { 1.95f, 2.35f, 2.8f, 3.25f, 3.9f, 4.35f };
    static readonly Vector2 ArtSize = new Vector2(TwilightIllustratedMenu.ArtWidth, TwilightIllustratedMenu.ArtHeight);
    public float SampledSeconds { get; private set; }

    void Initialize()
    {
        if (texture != null) return;
        // Keep the 3D render independent of the screen-space Canvas scale.
        renderStage = trainCamera.transform.parent;
        renderStage.SetParent(null, false);
        renderStage.position = new Vector3(0, -1000, 0);
        renderStage.localScale = Vector3.one;
        texture = new RenderTexture(1200, 800, 24, RenderTextureFormat.ARGB32)
        { name = "MoonPuffUpgradeCamera", antiAliasing = 4 };
        texture.Create(); trainCamera.targetTexture = texture; trainImage.texture = texture;
        roofRest = originalRoof.localPosition;
        wheelRest = new Vector3[mechanism.moonlight.wheels.Length];
        for (int i = 0; i < wheelRest.Length; i++) wheelRest[i] = mechanism.moonlight.wheels[i].pivot.localPosition;
        lampProperties = new MaterialPropertyBlock();
        pixelMorph = new StarPixelTrainMorph(mechanism);
    }

    public void Sample(float seconds, bool playSound = false)
    {
        Initialize(); renderStage.gameObject.SetActive(true); SampledSeconds = seconds;
        if (seconds < lastSeconds || lastSeconds < 0)
        {
            accent = 0; motionAudio.Stop(); accentsAudio.Stop();
            parts.smoke.plume.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        float journey = Smooth((seconds - DepartureTime) / (ArrivalTime - DepartureTime));
        float index = journey * (route.Length - 1);
        int a = Mathf.Min(Mathf.FloorToInt(index), route.Length - 2);
        float fraction = index - a;
        Vector3 point = RoutePoint(index);
        Vector3 direction = RoutePoint(index + 6f) - RoutePoint(index - 6f);
        float bridgeSlope = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        float travelled = Mathf.Lerp(wheelTravel[a], wheelTravel[a + 1], fraction);
        // The illustrated bridge changes elevation, not the locomotive’s compass heading.
        mechanism.transform.localRotation = Quaternion.identity;
        mechanism.Pose(travelled, trainScale, seconds >= 1.4f);

        // The complete models remain grounded; only the pixel boundary changes their appearance.
        originalRoof.localPosition = roofRest;
        originalRoof.localRotation = Quaternion.identity;
        foreach (var part in new[] { parts.cab, parts.boiler, parts.chimney, parts.roof, parts.crescent })
        { part.localPosition = Vector3.zero; part.localRotation = Quaternion.identity; }
        for (int i = 0; i < wheelRest.Length; i++)
            mechanism.moonlight.wheels[i].pivot.localPosition = wheelRest[i];
        mechanism.Pose(travelled, trainScale, seconds >= 4.35f);
        float light = Smooth((seconds - 4.35f) / .4f);
        lampProperties.SetColor("_EmissionColor", new Color(.30f, .20f, .07f) * light);
        parts.lamp.SetPropertyBlock(lampProperties);
        foreach (var sparkle in sparkles) sparkle.gameObject.SetActive(false);
        pixelMorph.Sample(seconds);

        // Full-map reveals retain both worlds; standalone previews can use the close crop.
        Vector2 center = new Vector2(point.x, point.z) * 20 + ArtSize * .5f + Vector2.up * 56;
        Vector2 crop = new Vector2(520, 520 * 590f / 852);
        if(wholeMap){crop=ArtSize;center=ArtSize*.5f;}
        center.x = Mathf.Clamp(center.x, crop.x / 2, ArtSize.x - crop.x / 2);
        center.y = Mathf.Clamp(center.y, crop.y / 2, ArtSize.y - crop.y / 2);
        artwork.uvRect = new Rect((center - crop / 2) / ArtSize, crop / ArtSize);
        Vector2 panel = artwork.rectTransform.rect.size;
        Vector2 panelScale = panel / crop;
        var imageRect = trainImage.rectTransform;
        float showcase = wholeMap ? Mathf.Lerp(2.4f, 1, Smooth((seconds - 4.65f) / .9f)) : 1;
        imageRect.sizeDelta = new Vector2(228, 152) * displayScale * panelScale * showcase;
        Vector3 cameraOffset = Vector3.Lerp(new Vector3(3.6f, 5.4f, -9), new Vector3(0, 8, -8), Smooth((seconds - 4.65f) / .9f));
        trainCamera.transform.localPosition = Vector3.up * 2 + cameraOffset;
        trainCamera.transform.localRotation = Quaternion.LookRotation(-cameraOffset);
        float groundOffset = 2 * trainCamera.transform.up.y * 20;
        float slopeWeight=Smooth((seconds-4.65f)/.9f);
        imageRect.localRotation=Quaternion.Euler(0,0,bridgeSlope*slopeWeight);
        Position(imageRect, point, center, panelScale, 0);
        Vector2 groundedOffset=imageRect.localRotation*new Vector3(0,groundOffset*displayScale*showcase-7,0);
        imageRect.anchoredPosition+=groundedOffset*panelScale;
        // Keep tickets on the rail and hide the destination when the train occupies it.
        Position(ticket30, route[0], center, panelScale, 0);
        float arrivalClearance = 0;
        Position(ticket31, route[route.Length - 1], center, panelScale, arrivalClearance);
        ticket30.localScale=ticket31.localScale=new Vector3(panelScale.x,panelScale.y,1);
        // Completed level 30 remains available; the occupied level 31 marker is hidden.
        ticket30.gameObject.SetActive(journey > 0f);
        ticket31.gameObject.SetActive(journey < .999f);
        var completedBadge=ticket30.GetComponentInChildren<WorkshopStationBadge>(true);
        if(completedBadge!=null){completedBadge.SetLocked(false);completedBadge.SetState(false,false);}
        if(ticket31Face!=null)ticket31Face.color = journey >= .999f ? arrivedTicket : lockedTicket;
        var destinationBadge=ticket31.GetComponentInChildren<WorkshopStationBadge>();
        if(destinationBadge!=null){destinationBadge.SetLocked(false);destinationBadge.SetState(false,journey>=.999f);}
        foreach(var number in ticket31.GetComponentsInChildren<TMPro.TMP_Text>(true))
            number.color=journey>=.999f?new Color(1,.98f,.85f):new Color(.025f,.09f,.22f);
        if(emeraldLevelBadge!=null)emeraldLevelBadge.gameObject.SetActive(false);
        trainCamera.orthographicSize = 3.8f * Mathf.Abs(trainCamera.transform.parent.lossyScale.x);

        if (playSound && Application.isPlaying) Audio(seconds, journey);
        else if (!Application.isPlaying)
        {
            // Deterministic finishing puff for editor previews of the same animation.
            var plume = parts.smoke.plume;
            plume.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (seconds >= 4.85f && seconds < 6.1f)
            {
                plume.useAutoRandomSeed = false; plume.randomSeed = 3031;
                plume.Emit(1); plume.Simulate(seconds - 4.85f, false, false, false);
            }
        }
        lastSeconds = seconds;
    }

    static void Position(RectTransform rect, Vector3 p, Vector2 center, Vector2 scale, float offset)
    {
        rect.anchoredPosition = (new Vector2(p.x, p.z) * 20 + ArtSize * .5f - center + Vector2.up * offset) * scale;
    }

    // Cubic interpolation keeps position and tangent continuous between baked route samples.
    Vector3 RoutePoint(float index)
    {
        index = Mathf.Clamp(index, 0, route.Length - 1);
        int i = Mathf.Min(Mathf.FloorToInt(index), route.Length - 2);
        float t = index - i;
        Vector3 b=route[i], c=route[i+1];
        Vector3 a=i>0?route[i-1]:2*b-c;
        Vector3 d=i+2<route.Length?route[i+2]:2*c-b;
        return .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
    }

    void Audio(float seconds, float journey)
    {
        float volume = AudioManager.Instance != null ? AudioManager.Instance.masterVolume * AudioManager.Instance.sfxVolume : 1;
        motionAudio.volume = volume * .42f; accentsAudio.volume = volume * .55f;
        if (AudioManager.Instance != null)
            motionAudio.outputAudioMixerGroup = accentsAudio.outputAudioMixerGroup = AudioManager.Instance.sfxGroup;
        while (accent < ClickTimes.Length && seconds >= ClickTimes[accent])
        {
            accentsAudio.PlayOneShot(click, .65f); accent++;
        }
        if (lastSeconds < 4.4f && seconds >= 4.4f) accentsAudio.PlayOneShot(readyChime, .6f);
        if (lastSeconds < 4.85f && seconds >= 4.85f)
        {
            accentsAudio.PlayOneShot(finishingChuk, .6f);
            parts.smoke.plume.Play(false); parts.smoke.plume.Emit(1);
        }
        bool restarted = false;
        if (journey > 0 && journey < 1)
        {
            if (!motionAudio.isPlaying) { motionAudio.clip = chug; motionAudio.loop = true; motionAudio.Play(); restarted = true; }
            float speed = lastSeconds >= 0 ? Mathf.Max(.35f, (journey - Smooth((lastSeconds - DepartureTime) / (ArrivalTime - DepartureTime))) /
                Mathf.Max(.001f, Time.unscaledDeltaTime) * wheelTravel[wheelTravel.Length - 1] /
                (trainScale * 2 * Mathf.PI * mechanism.moonlight.wheels[0].radius)) : .35f;
            motionAudio.pitch = Mathf.Clamp(speed * chug.length, .35f, 3);
        }
        else motionAudio.Stop();
        parts.smoke.Tick(Time.unscaledDeltaTime, motionAudio, chug, restarted);
    }

    static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * t * (t * (t * 6 - 15) + 10); }
    void OnDisable()
    {
        if (motionAudio != null) motionAudio.Stop();
        if (accentsAudio != null) accentsAudio.Stop();
        if (renderStage != null) renderStage.gameObject.SetActive(false);
        lastSeconds = -1;
    }
    void OnDestroy()
    {
        if (pixelMorph != null) pixelMorph.Dispose();
        if (texture == null) return;
        if (trainCamera != null) trainCamera.targetTexture = null;
        if (trainImage != null) trainImage.texture = null;
        texture.Release();
        if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture);
        if (renderStage != null)
        {
            if (Application.isPlaying) Destroy(renderStage.gameObject); else DestroyImmediate(renderStage.gameObject);
        }
    }
}
