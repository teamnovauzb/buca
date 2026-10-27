using TMPro;
using UnityEngine;

/// <summary>A hinged departure-board leaf, shared by mouse, keyboard and joystick selection.</summary>
[ExecuteAlways, DefaultExecutionOrder(100)]
public sealed class WorkshopFlipDisplay : MonoBehaviour
{
    [System.Serializable]
    public sealed class Half
    {
        public RectTransform root;
        public UnityEngine.UI.RawImage face;
        public TMP_Text label;
    }

    public TwilightIllustratedMenu view;
    public TMP_Text number;
    public GameObject leaves;
    public Half upper, lower, turningUpper, turningLower;
    public UnityEngine.UI.Image shadow;
    public AudioSource flipAudio;
    public AudioClip flipSound;
    public const float FlipSeconds = .42f;
    public bool IsFlipping { get; private set; }
    public int DisplayedNumber => displayed;
    public int TargetNumber => target;
    int displayed = -1, requested = -1, from, target;
    float started;
    bool Ready => number != null && leaves != null && turningUpper != null && turningUpper.root != null;

    void OnEnable()
    {
        // The font's line metrics exceed the decorative card height. The leaf masks
        // already clip the animation; TMP truncation can otherwise remove every digit.
        if(number!=null)number.overflowMode=TextOverflowModes.Overflow;
        foreach(var half in new[]{upper,lower,turningUpper,turningLower})
            if(half!=null && half.label!=null)half.label.overflowMode=TextOverflowModes.Overflow;
        if (Ready && view != null && view.map != null) SetNumber(view.map.Selected + 1, true);
    }

    void LateUpdate()
    {
        if (!Ready || view == null || view.map == null) return;
        SetNumber(view.map.Selected + 1, !Application.isPlaying);
        if (!IsFlipping) return;
        float progress = (Time.unscaledTime - started) / FlipSeconds;
        Sample(from, target, progress);
        if (progress < 1) return;
        displayed = target;
        IsFlipping = false;
        // Finish the current leaf before turning to the latest selection during a held joystick.
        if (requested != displayed) BeginFlip();
    }

    public void SetNumber(int value, bool immediate = false)
    {
        if (!Ready) return;
        requested = Mathf.Clamp(value, 1, view != null && view.stopRects != null ? view.stopRects.Length : 50);
        if (immediate || displayed < 0)
        {
            displayed = target = requested;
            IsFlipping = false;
            Sample(displayed, displayed, 1);
        }
        else if (!IsFlipping && requested != displayed) BeginFlip();
        else if (!IsFlipping) number.text = displayed.ToString();
    }

    void BeginFlip()
    {
        from = displayed;
        target = requested;
        started = Time.unscaledTime;
        IsFlipping = true;
        Sample(from, target, 0);
        if (!Application.isPlaying || flipAudio == null || flipSound == null) return;
        var audio = AudioManager.Instance;
        flipAudio.volume = (audio != null ? audio.masterVolume * audio.sfxVolume : 1) * .62f;
        if (audio != null) flipAudio.outputAudioMixerGroup = audio.sfxGroup;
        flipAudio.pitch = 1;
        flipAudio.PlayOneShot(flipSound);
    }

    public void Sample(int before, int after, float progress)
    {
        if (!Ready) return;
        progress = Mathf.Clamp01(progress);
        bool turning = before != after && progress < 1;
        number.text = after.ToString();
        number.enabled = !turning;
        leaves.SetActive(turning);
        if (!turning) return;
        upper.label.text = turningLower.label.text = after.ToString();
        lower.label.text = turningUpper.label.text = before.ToString();
        turningUpper.root.gameObject.SetActive(progress < .48f);
        turningLower.root.gameObject.SetActive(progress >= .48f);
        float lift = Mathf.Clamp01(progress / .48f);
        Pose(turningUpper, -90 * lift * lift);
        float fall = Mathf.InverseLerp(.48f, .88f, progress);
        float angle = 90 * Mathf.Pow(1 - fall, 3);
        if (progress > .88f) angle = -3 * Mathf.Sin((progress - .88f) / .12f * Mathf.PI);
        Pose(turningLower, angle);
        if (shadow != null) shadow.color = new Color(0, .015f, .03f, .24f * Mathf.Sin(progress * Mathf.PI));
    }

    void Pose(Half leaf, float angle)
    {
        leaf.root.localRotation = Quaternion.Euler(angle, 0, 0);
        float lift = Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad);
        leaf.root.localScale = new Vector3(1 + lift * .035f, 1, 1);
        float shade = 1 - lift * .4f;
        leaf.face.color = new Color(shade, shade, shade, 1);
        var ink = number.color;
        leaf.label.color = new Color(ink.r * shade, ink.g * shade, ink.b * shade, ink.a);
    }

    void OnDisable()
    {
        if (flipAudio != null) flipAudio.Stop();
        IsFlipping = false;
        if (number != null) number.enabled = true;
        if (leaves != null) leaves.SetActive(false);
        displayed = -1;
    }
}
