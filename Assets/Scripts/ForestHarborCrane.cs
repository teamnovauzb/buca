using UnityEngine;
using UnityEngine.UI;

/// <summary>A small quay crane: hoist clear, slew, set down, then return the load.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ForestHarborCrane : MonoBehaviour
{
    public Transform turntable, boomTip, cargo, cable, pulley, hook;
    public Camera craneCamera;
    public RawImage picture;
    public HarborCraneShadow cargoShadow;
    public Transform[] chainLinks;
    [Min(8)] public float cycleSeconds = 24;
    [Range(15, 100)] public float dockAngle = 42;
    public float Elapsed => elapsed;
    public string Phase { get; private set; }
    float elapsed;
    bool paused, focused = true;
    RenderTexture savedTarget, runtimeTarget;

    void OnEnable()
    {
        focused = Application.isFocused;
        paused = false;
        if (Application.isPlaying && craneCamera != null && picture != null)
        {
            savedTarget = craneCamera.targetTexture;
            if (savedTarget != null)
            {
                runtimeTarget = new RenderTexture(savedTarget) { name = "Harbor crane (runtime)" };
                runtimeTarget.Create();
                craneCamera.targetTexture = runtimeTarget;
                picture.texture = runtimeTarget;
            }
        }
        Pose(elapsed);
    }

    void OnDisable()
    {
        if (craneCamera != null) craneCamera.enabled = false;
        if (runtimeTarget == null) return;
        if (craneCamera != null) craneCamera.targetTexture = savedTarget;
        if (picture != null) picture.texture = savedTarget;
        runtimeTarget.Release();
        Destroy(runtimeTarget);
        runtimeTarget = null;
    }

    void OnApplicationPause(bool value) => paused = value;
    void OnApplicationFocus(bool value) => focused = value;

    void Update()
    {
        bool visible = picture != null && picture.isActiveAndEnabled;
        if (craneCamera != null) craneCamera.enabled = visible;
        if (!Application.isPlaying || !visible || paused || (!focused && !Application.runInBackground)) return;
        elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime, Mathf.Max(8, cycleSeconds));
        Pose(elapsed);
    }

    static float Ease(float t) { t = Mathf.Clamp01(t); return t*t*t*(t*(t*6-15)+10); }

    public void Pose(float seconds)
    {
        if (turntable == null || boomTip == null || cargo == null || cable == null) return;
        float t = Mathf.Repeat(seconds / Mathf.Max(8, cycleSeconds), 1) * 24;
        float angle = 0, height = 1.28f, motion = 0;
        const float high = 2.40f, low = .39f;
        if (t < 1.2f) Phase = "Taking the load";
        else if (t < 4.2f)
        {
            float u = (t-1.2f)/3;
            height = Mathf.Lerp(1.28f, high, Ease(u)); motion = Mathf.Sin(u*Mathf.PI);
            Phase = "Hoisting";
        }
        else if (t < 7.5f)
        {
            float u = (t-4.2f)/3.3f;
            height = high; angle = dockAngle*Ease(u); motion = Mathf.Sin(u*Mathf.PI);
            Phase = "Turning toward dock";
        }
        else if (t < 10.5f)
        {
            float u = (t-7.5f)/3;
            height = Mathf.Lerp(high, low, Ease(u)); angle = dockAngle; motion = .5f*Mathf.Sin(u*Mathf.PI);
            Phase = "Lowering onto dock";
        }
        else if (t < 12) { height = low; angle = dockAngle; Phase = "Resting on dock"; }
        else if (t < 15)
        {
            float u = (t-12)/3;
            height = Mathf.Lerp(low, high, Ease(u)); angle = dockAngle; motion = Mathf.Sin(u*Mathf.PI);
            Phase = "Lifting from dock";
        }
        else if (t < 18.3f)
        {
            float u = (t-15)/3.3f;
            height = high; angle = dockAngle*(1-Ease(u)); motion = -Mathf.Sin(u*Mathf.PI);
            Phase = "Turning toward water";
        }
        else if (t < 21.3f)
        {
            float u = (t-18.3f)/3;
            height = Mathf.Lerp(high, 1.28f, Ease(u)); motion = -.5f*Mathf.Sin(u*Mathf.PI);
            Phase = "Lowering waterside";
        }
        else Phase = "Taking the load";

        turntable.localRotation = Quaternion.Euler(0, angle, 0);
        Vector3 tip = transform.InverseTransformPoint(boomTip.position);
        // The load lags slightly while the jib accelerates; it settles completely on the dock.
        float swing = motion * Mathf.Sin(t*2.1f)*.035f;
        cargo.localPosition = new Vector3(tip.x+swing, height, tip.z+swing*.4f);
        cargo.localRotation = Quaternion.Euler(0, angle*.72f, -motion*Mathf.Sin(t*2.1f)*2);
        Vector3 attachment = transform.InverseTransformPoint(hook.position);
        SetLine(cable, tip, attachment);
        if (pulley != null) pulley.localRotation = Quaternion.Euler(0, 0, (height-1.28f)*318);
        if (chainLinks != null)
        {
            float length = Vector3.Distance(tip, attachment);
            for (int i=0; i<chainLinks.Length; i++)
            {
                var link = chainLinks[i];
                float distance = (i+.5f)*.085f;
                link.gameObject.SetActive(distance < length-.02f);
                link.localPosition = Vector3.Lerp(tip, attachment, Mathf.Clamp01(distance/Mathf.Max(.01f,length)));
                link.localRotation = Quaternion.Euler(90, angle+(i%2)*90, 0);
            }
        }
        if (cargoShadow != null && craneCamera != null && picture != null)
        {
            var ground = cargo.localPosition; ground.y = 0;
            Vector3 uv = craneCamera.WorldToViewportPoint(transform.TransformPoint(ground));
            var rect = picture.rectTransform;
            cargoShadow.rectTransform.anchoredPosition = rect.anchoredPosition +
                new Vector2((uv.x-.5f)*rect.rect.width, (uv.y-.5f)*rect.rect.height);
            float raised = Mathf.InverseLerp(low, high, height);
            cargoShadow.rectTransform.sizeDelta = Vector2.Lerp(new Vector2(32,14), new Vector2(40,18), raised);
            cargoShadow.color = new Color(.025f,.045f,.07f, angle > dockAngle*.75f ? Mathf.Lerp(.24f,.08f,raised) : .025f);
        }
    }

    static void SetLine(Transform line, Vector3 a, Vector3 b)
    {
        line.localPosition = (a+b)*.5f;
        Vector3 direction = (b-a).normalized;
        // Avoid FromToRotation's near-180-degree snap as the nearly vertical load sways.
        line.localRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.right,direction),direction);
        line.localScale = new Vector3(1, Vector3.Distance(a,b), 1);
    }
}
