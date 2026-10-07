using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Saved wooden replay console. Playback is render-only and cannot affect a run.</summary>
[DefaultExecutionOrder(600)]
public sealed partial class BestShotReplay : MonoBehaviour
{
    public GameObject presentation, recordedStage;
    public Transform frame, backdrop, progressFill, progressKnob, skipCap;
    public Vector3 frameRest, skipRest;
    public Camera frameCamera, playbackCamera;
    public Collider skipHit;
    public MeshFilter[] savedMeshes;
    public MeshRenderer[] savedRenderers;
    public RenderTexture replayTexture, captureTexture, blurScratch, blurredBackdrop;
    public Material blurMaterial;
    public AudioSource replayAudio;
    public AudioClip revealSound;
    public float progressLeft = -2.6f, progressWidth = 6.8f;

    public bool IsShowing { get; private set; }
    public float Progress { get; private set; }
    Camera _sourceCamera;
    bool _sourceEnabled, _neutral;
    float _elapsed, _duration, _skipTime;
    Action _finished;

    void LateUpdate()
    {
        RecordLateFrame();
        if (!IsShowing) return;
        Fit();
        bool held = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return)
            || ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
        if (_neutral) { if (!held) _neutral = false; }
        else if (_elapsed > .5f && _skipTime <= 0)
        {
            bool clicked = Input.GetMouseButtonDown(0) && skipHit.Raycast(frameCamera.ScreenPointToRay(Input.mousePosition), out var hit, 100);
            if (clicked || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || ArcadeInputAdapter.ConfirmDown()) Skip();
        }
        Tick(Time.unscaledDeltaTime);
    }
    public bool TryShow(Camera sourceCamera, Action finished)
    {
        CancelRecording();
        if (IsShowing || !HasBestShot || sourceCamera == null || presentation == null || recordedStage == null
            || frameCamera == null || playbackCamera == null || replayTexture == null || skipHit == null) return false;
        _sourceCamera = sourceCamera;
        _sourceEnabled = sourceCamera.enabled;
        CaptureBackdrop(sourceCamera);
        PrepareRecordedVisuals();
        _finished = finished;
        _elapsed = _skipTime = 0;
        _duration = Mathf.Max(2.2f, Mathf.Min(4.5f, _best.Duration) / .65f);
        _neutral = true;
        skipCap.localPosition = skipRest;
        IsShowing = true;
        _sourceCamera.enabled = false;
        recordedStage.SetActive(true);
        presentation.SetActive(true);
        SampleReplay(0);
        SampleEntrance(0);
        Fit();
        if (Application.isPlaying && replayAudio != null && revealSound != null)
        {
            var audio = AudioManager.Instance;
            replayAudio.PlayOneShot(revealSound, .35f * (audio != null ? audio.masterVolume * audio.sfxVolume : 1f));
        }
        return true;
    }
    public void Tick(float delta)
    {
        if (!IsShowing) return;
        _elapsed += Mathf.Max(0, delta);
        SampleEntrance(_elapsed);
        if (_skipTime > 0)
        {
            _skipTime -= delta;
            if (_skipTime <= 0) Hide(true);
            return;
        }
        SampleReplay(Mathf.Clamp01((_elapsed - .55f) / _duration));
        if (_elapsed >= .55f + _duration + .45f) Hide(true);
    }
    public void Skip()
    {
        if (!IsShowing || _skipTime > 0) return;
        _skipTime = .13f;
        skipCap.localPosition = skipRest + Vector3.forward * .08f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
    }
    public void Hide(bool advance)
    {
        if (!IsShowing) return;
        IsShowing = false;
        presentation.SetActive(false);
        recordedStage.SetActive(false);
        if (replayAudio != null) replayAudio.Stop();
        if (_sourceCamera != null) _sourceCamera.enabled = _sourceEnabled;
        _sourceCamera = null;
        var done = _finished;
        _finished = null;
        if (advance) done?.Invoke();
    }
    public void SampleEntrance(float time)
    {
        float t = Mathf.Clamp01(time / .55f);
        float ease = 1f - Mathf.Pow(1f - t, 3);
        frame.localPosition = frameRest + Vector3.down * ((1 - ease) * .65f);
        frame.localScale = Vector3.one * (.93f + .07f * ease);
        frame.localRotation = Quaternion.Euler((1 - ease) * -6f, 0, 0);
    }
    void SetProgress(float value)
    {
        Progress = Mathf.Clamp01(value);
        var scale = progressFill.localScale;
        scale.x = Mathf.Max(.0001f, Progress);
        progressFill.localScale = scale;
        var position = progressFill.localPosition;
        position.x = progressLeft + progressWidth * Progress * .5f;
        progressFill.localPosition = position;
        position = progressKnob.localPosition;
        position.x = progressLeft + progressWidth * Progress;
        progressKnob.localPosition = position;
    }
    public void Fit()
    {
        float aspect = Mathf.Max(.2f, frameCamera.aspect);
        float distance = Mathf.Max(5.0f, 8.8f / aspect) / Mathf.Tan(frameCamera.fieldOfView * .5f * Mathf.Deg2Rad) + 1;
        frameCamera.transform.localPosition = new Vector3(0, 0, -distance);
        frameCamera.transform.localRotation = Quaternion.identity;
        float height = 80 * Mathf.Tan(frameCamera.fieldOfView * .5f * Mathf.Deg2Rad);
        backdrop.localScale = new Vector3(height * aspect * 1.01f, height * 1.01f, 1);
    }
    void CaptureBackdrop(Camera source)
    {
        if (captureTexture == null || blurMaterial == null) return;
        var previous = source.targetTexture;
        var active = RenderTexture.active;
        try
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = captureTexture };
            if (RenderPipeline.SupportsRenderRequest(source, request)) RenderPipeline.SubmitRenderRequest(source, request);
            else { source.targetTexture = captureTexture; source.Render(); }
            for (int i = 0; i < 3; i++)
            {
                blurMaterial.SetVector("_BlurDirection", new Vector4(3, 0, 0, 0));
                Graphics.Blit(i == 0 ? captureTexture : blurredBackdrop, blurScratch, blurMaterial);
                blurMaterial.SetVector("_BlurDirection", new Vector4(0, 3, 0, 0));
                Graphics.Blit(blurScratch, blurredBackdrop, blurMaterial);
            }
        }
        finally { source.targetTexture = previous; RenderTexture.active = active; }
    }
    void OnDisable() { CancelRecording(); Hide(false); }
}
