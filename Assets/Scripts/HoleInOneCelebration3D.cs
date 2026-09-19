using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// World-space hole-in-one celebration backed entirely by a saved prefab.
/// Runtime code only reuses and animates its serialized renderers and effects.
/// </summary>
public sealed class HoleInOneCelebration3D : MonoBehaviour
{
    const int RingSegments = 48;
    const float CelebrationDuration = 2.45f;

    [Header("Prebuilt rig")]
    [SerializeField] Transform _effectRoot;
    [SerializeField] TextMeshPro _title;
    [SerializeField] TextMeshPro _subtitle;
    [SerializeField] GameObject[] _orbitPucks;
    [SerializeField] MeshFilter[] _orbitFilters;
    [SerializeField] MeshRenderer[] _orbitRenderers;
    [SerializeField] TrailRenderer[] _orbitTrails;
    [SerializeField] LineRenderer[] _rings;
    [SerializeField] ParticleSystem _burst;
    [SerializeField] ParticleSystemRenderer _burstRenderer;
    [SerializeField] Light _awardLight;

    Camera _camera;
    Vector3 _focus;
    Coroutine _routine;

    public float MinimumDisplaySeconds => 2.25f;

    void Awake()
    {
        HideImmediate();
    }

    public void Play(Vector3 holePosition, Camera viewingCamera, GameObject sourcePuck)
    {
        if (!HasPrebuiltRig()) return;

        ConfigurePuckMeshes(sourcePuck);
        _camera = viewingCamera != null ? viewingCamera : Camera.main;
        _focus = holePosition;
        if (_routine != null) StopCoroutine(_routine);
        HideImmediate();
        _effectRoot.gameObject.SetActive(true);
        _routine = StartCoroutine(PlayRoutine());
    }

    bool HasPrebuiltRig()
    {
        bool valid = _effectRoot != null && _title != null && _subtitle != null &&
            _orbitPucks != null && _orbitPucks.Length > 0 &&
            _orbitFilters != null && _orbitFilters.Length == _orbitPucks.Length &&
            _orbitRenderers != null && _orbitRenderers.Length == _orbitPucks.Length &&
            _orbitTrails != null && _orbitTrails.Length == _orbitPucks.Length &&
            _rings != null && _rings.Length > 0 && _burst != null &&
            _burstRenderer != null && _awardLight != null;
        if (!valid)
            Debug.LogError("[HoleInOneCelebration3D] Prebuilt rig is incomplete. Run the runtime-content baker.", this);
        return valid;
    }

    void ConfigurePuckMeshes(GameObject sourcePuck)
    {
        if (sourcePuck == null) return;
        MeshFilter sourceFilter = sourcePuck.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = sourcePuck.GetComponent<MeshRenderer>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null || sourceRenderer == null) return;

        for (int i = 0; i < _orbitPucks.Length; i++)
        {
            _orbitFilters[i].sharedMesh = sourceFilter.sharedMesh;
            _orbitRenderers[i].sharedMaterials = sourceRenderer.sharedMaterials;
            _orbitRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _orbitRenderers[i].receiveShadows = false;
        }

        _burstRenderer.mesh = sourceFilter.sharedMesh;
        if (sourceRenderer.sharedMaterial != null)
            _burstRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
    }

    IEnumerator PlayRoutine()
    {
        _title.gameObject.SetActive(true);
        _subtitle.gameObject.SetActive(true);
        _title.transform.localScale = Vector3.one * 0.025f;
        _subtitle.transform.localScale = Vector3.one * 0.025f;
        SetTextAlpha(_title, 0f, new Color(1f, 0.78f, 0.18f));
        SetTextAlpha(_subtitle, 0f, new Color(0.24f, 0.94f, 1f));

        for (int i = 0; i < _orbitPucks.Length; i++)
        {
            _orbitPucks[i].SetActive(true);
            _orbitPucks[i].transform.localScale = Vector3.one * 0.01f;
            _orbitTrails[i].Clear();
            _orbitTrails[i].emitting = true;
        }

        _awardLight.enabled = true;
        _awardLight.transform.position = _focus + Vector3.up * 1.4f;
        _burst.transform.position = _focus + Vector3.up * 0.35f;
        _burst.Clear(true);
        _burst.Play(true);

        float elapsed = 0f;
        while (elapsed < CelebrationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float entrance = EaseOutBack(Mathf.Clamp01(elapsed / 0.48f), 1.15f);
            float fade = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(1.85f, CelebrationDuration, elapsed));

            UpdateText(entrance, fade, elapsed);
            UpdateOrbitPucks(entrance, fade, elapsed);
            UpdateRings(elapsed);

            float lightAttack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.18f));
            _awardLight.intensity = 4.2f * lightAttack * fade *
                (0.88f + Mathf.Sin(elapsed * 9f) * 0.12f);
            yield return null;
        }

        HideImmediate();
        _routine = null;
    }

    void UpdateText(float entrance, float fade, float elapsed)
    {
        _title.transform.position = _focus + Vector3.up * Mathf.Lerp(1.0f, 2.85f, entrance);
        _subtitle.transform.position = _focus + Vector3.up * Mathf.Lerp(0.8f, 2.25f, entrance);
        FaceCamera(_title.transform);
        FaceCamera(_subtitle.transform);

        float titlePulse = 1f + Mathf.Sin(elapsed * 6.2f) * 0.025f;
        _title.transform.localScale = Vector3.one * (0.22f * entrance * titlePulse);
        float subtitleEntrance = EaseOutCubic(Mathf.InverseLerp(0.18f, 0.60f, elapsed));
        _subtitle.transform.localScale = Vector3.one * (0.18f * subtitleEntrance);
        SetTextAlpha(_title, fade, new Color(1f, 0.78f, 0.18f));
        SetTextAlpha(_subtitle, fade * subtitleEntrance, new Color(0.24f, 0.94f, 1f));
    }

    void UpdateOrbitPucks(float entrance, float fade, float elapsed)
    {
        float radius = Mathf.Lerp(0.25f, 2.55f, Mathf.Clamp01(entrance));
        for (int i = 0; i < _orbitPucks.Length; i++)
        {
            float phase = i / (float)_orbitPucks.Length * Mathf.PI * 2f;
            float angle = phase + elapsed * 2.35f;
            float height = 1.25f + Mathf.Sin(angle * 2f) * 0.22f +
                Mathf.Sin(Mathf.Clamp01(elapsed / 0.55f) * Mathf.PI) * 0.85f;
            Transform puck = _orbitPucks[i].transform;
            puck.position = _focus + new Vector3(Mathf.Cos(angle) * radius,
                height, Mathf.Sin(angle) * radius);
            puck.rotation = Quaternion.Euler(elapsed * 260f + i * 31f,
                -angle * Mathf.Rad2Deg + 90f, elapsed * 190f);
            puck.localScale = Vector3.one *
                (0.46f * Mathf.Clamp01(entrance) * Mathf.Clamp01(fade * 2f));
            _orbitTrails[i].emitting = fade > 0.08f;
        }
    }

    void UpdateRings(float elapsed)
    {
        for (int ringIndex = 0; ringIndex < _rings.Length; ringIndex++)
        {
            float progress = Mathf.InverseLerp(ringIndex * 0.16f,
                ringIndex * 0.16f + 1.05f, elapsed);
            bool visible = progress > 0f && progress < 1f;
            LineRenderer ring = _rings[ringIndex];
            ring.enabled = visible;
            if (!visible) continue;

            float eased = EaseOutCubic(progress);
            float radius = Mathf.Lerp(0.32f, 4.6f, eased);
            float height = 0.08f + ringIndex * 0.11f +
                Mathf.Sin(progress * Mathf.PI) * 0.22f;
            for (int pointIndex = 0; pointIndex < RingSegments; pointIndex++)
            {
                float angle = pointIndex / (float)RingSegments * Mathf.PI * 2f;
                Vector3 circlePoint = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                ring.SetPosition(pointIndex,
                    _focus + circlePoint * radius + Vector3.up * height);
            }

            Color color = ringIndex == 0
                ? new Color(0.22f, 0.94f, 1f)
                : ringIndex == 1
                    ? new Color(1f, 0.70f, 0.14f)
                    : new Color(1f, 0.22f, 0.58f);
            color.a = Mathf.Sin(progress * Mathf.PI) * 0.95f;
            ring.startColor = color;
            ring.endColor = color;
            ring.widthMultiplier = Mathf.Lerp(0.15f, 0.035f, progress);
        }
    }

    void FaceCamera(Transform target)
    {
        if (_camera == null) return;
        Vector3 awayFromCamera = target.position - _camera.transform.position;
        if (awayFromCamera.sqrMagnitude > 0.001f)
            target.rotation = Quaternion.LookRotation(awayFromCamera.normalized, _camera.transform.up);
    }

    static void SetTextAlpha(TMP_Text text, float alpha, Color color)
    {
        color.a = Mathf.Clamp01(alpha);
        text.color = color;
    }

    void HideImmediate()
    {
        if (_effectRoot == null) return;
        if (_title != null) _title.gameObject.SetActive(false);
        if (_subtitle != null) _subtitle.gameObject.SetActive(false);
        if (_orbitPucks != null)
        {
            for (int i = 0; i < _orbitPucks.Length; i++)
            {
                if (_orbitTrails != null && i < _orbitTrails.Length && _orbitTrails[i] != null)
                {
                    _orbitTrails[i].emitting = false;
                    _orbitTrails[i].Clear();
                }
                if (_orbitPucks[i] != null) _orbitPucks[i].SetActive(false);
            }
        }
        if (_rings != null)
            for (int i = 0; i < _rings.Length; i++)
                if (_rings[i] != null) _rings[i].enabled = false;
        if (_burst != null)
            _burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_awardLight != null)
        {
            _awardLight.intensity = 0f;
            _awardLight.enabled = false;
        }
        _effectRoot.gameObject.SetActive(false);
    }

    static float EaseOutCubic(float value)
    {
        float x = 1f - Mathf.Clamp01(value);
        return 1f - x * x * x;
    }

    static float EaseOutBack(float value, float overshoot)
    {
        float x = Mathf.Clamp01(value) - 1f;
        return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
    }
}
