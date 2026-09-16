using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// A genuine world-space celebration used only for a hole-in-one. Nothing in
/// this effect is Canvas UI: the title is TextMeshPro geometry, the award
/// pucks are real meshes, the shockwaves are LineRenderers, and the burst uses
/// mesh particles. Objects are built once and reused to stay WebGL-friendly.
/// </summary>
public sealed class HoleInOneCelebration3D : MonoBehaviour
{
    const int OrbitPuckCount = 4;
    const int RingCount = 3;
    const int RingSegments = 48;
    const float CelebrationDuration = 2.45f;

    Transform _effectRoot;
    TextMeshPro _title;
    TextMeshPro _subtitle;
    readonly GameObject[] _orbitPucks = new GameObject[OrbitPuckCount];
    readonly TrailRenderer[] _orbitTrails = new TrailRenderer[OrbitPuckCount];
    readonly LineRenderer[] _rings = new LineRenderer[RingCount];
    readonly Vector3[] _unitCircle = new Vector3[RingSegments + 1];
    ParticleSystem _burst;
    ParticleSystemRenderer _burstRenderer;
    Light _awardLight;
    Material _energyMaterial;
    Camera _camera;
    Vector3 _focus;
    Coroutine _routine;
    bool _built;

    public float MinimumDisplaySeconds => 2.25f;

    void Awake()
    {
        BuildOnce();
        HideImmediate();
    }

    public void Play(Vector3 holePosition, Camera viewingCamera, GameObject sourcePuck)
    {
        BuildOnce();
        ConfigurePuckMeshes(sourcePuck);

        _camera = viewingCamera != null ? viewingCamera : Camera.main;
        _focus = holePosition;
        if (_routine != null) StopCoroutine(_routine);
        HideImmediate();
        _effectRoot.gameObject.SetActive(true);
        _routine = StartCoroutine(PlayRoutine());
    }

    void BuildOnce()
    {
        if (_built) return;
        _built = true;

        GameObject root = new GameObject("HoleInOneCelebration3D_WorldSpace");
        root.transform.SetParent(transform, false);
        _effectRoot = root.transform;

        for (int i = 0; i <= RingSegments; i++)
        {
            float angle = i / (float)RingSegments * Mathf.PI * 2f;
            _unitCircle[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        _energyMaterial = CreateEnergyMaterial();
        _title = CreateWorldText("HoleInOneTitle3D", "HOLE IN ONE", 8.2f, FontStyles.Bold);
        _subtitle = CreateWorldText("HoleInOneSubtitle3D", "LEGENDARY SHOT", 3.6f,
            FontStyles.Bold | FontStyles.Italic);

        for (int i = 0; i < RingCount; i++)
        {
            GameObject ringGo = new GameObject($"EnergyRing3D_{i + 1}");
            ringGo.transform.SetParent(_effectRoot, false);
            LineRenderer ring = ringGo.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.numCapVertices = 4;
            ring.numCornerVertices = 3;
            ring.widthMultiplier = 0.10f;
            ring.textureMode = LineTextureMode.Stretch;
            ring.alignment = LineAlignment.View;
            ring.sharedMaterial = _energyMaterial;
            ring.enabled = false;
            _rings[i] = ring;
        }

        for (int i = 0; i < OrbitPuckCount; i++)
        {
            GameObject puck = new GameObject($"AwardPuck3D_{i + 1}");
            puck.transform.SetParent(_effectRoot, false);
            puck.AddComponent<MeshFilter>();
            puck.AddComponent<MeshRenderer>();

            TrailRenderer trail = puck.AddComponent<TrailRenderer>();
            trail.time = 0.34f;
            trail.minVertexDistance = 0.035f;
            trail.startWidth = 0.11f;
            trail.endWidth = 0.012f;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 4;
            trail.alignment = LineAlignment.View;
            trail.sharedMaterial = _energyMaterial;
            trail.startColor = i % 2 == 0
                ? new Color(0.20f, 0.94f, 1f, 0.88f)
                : new Color(1f, 0.70f, 0.16f, 0.88f);
            trail.endColor = new Color(1f, 0.20f, 0.58f, 0f);
            trail.emitting = false;

            _orbitPucks[i] = puck;
            _orbitTrails[i] = trail;
        }

        BuildMeshBurst();

        GameObject lightGo = new GameObject("HoleInOneAwardLight3D");
        lightGo.transform.SetParent(_effectRoot, false);
        _awardLight = lightGo.AddComponent<Light>();
        _awardLight.type = LightType.Point;
        _awardLight.color = new Color(1f, 0.72f, 0.22f);
        _awardLight.range = 7.5f;
        _awardLight.intensity = 0f;
        _awardLight.shadows = LightShadows.None;
        _awardLight.enabled = false;
    }

    TextMeshPro CreateWorldText(string objectName, string message, float fontSize,
        FontStyles style)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(_effectRoot, false);
        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.characterSpacing = 5f;
        text.outlineWidth = 0.20f;
        text.outlineColor = new Color(0.01f, 0.025f, 0.07f, 1f);
        text.rectTransform.sizeDelta = new Vector2(15f, 2.2f);
        text.rectTransform.localScale = Vector3.one * 0.22f;
        return text;
    }

    void BuildMeshBurst()
    {
        GameObject go = new GameObject("AwardPuckBurst3D");
        go.transform.SetParent(_effectRoot, false);
        _burst = go.AddComponent<ParticleSystem>();
        _burstRenderer = go.GetComponent<ParticleSystemRenderer>();
        _burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _burst.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 6.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.18f, 0.92f, 1f, 1f),
            new Color(1f, 0.55f, 0.12f, 1f));
        main.gravityModifier = 0.65f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 72;

        ParticleSystem.EmissionModule emission = _burst.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 64) });

        ParticleSystem.ShapeModule shape = _burst.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.42f;

        ParticleSystem.RotationOverLifetimeModule rotation = _burst.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
        rotation.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
        rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

        _burstRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        _burstRenderer.sharedMaterial = _energyMaterial;
        _burstRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _burstRenderer.receiveShadows = false;
    }

    void ConfigurePuckMeshes(GameObject sourcePuck)
    {
        if (sourcePuck == null) return;
        MeshFilter sourceFilter = sourcePuck.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = sourcePuck.GetComponent<MeshRenderer>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null || sourceRenderer == null) return;

        for (int i = 0; i < _orbitPucks.Length; i++)
        {
            MeshFilter filter = _orbitPucks[i].GetComponent<MeshFilter>();
            MeshRenderer renderer = _orbitPucks[i].GetComponent<MeshRenderer>();
            filter.sharedMesh = sourceFilter.sharedMesh;
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
            float normalized = Mathf.Clamp01(elapsed / CelebrationDuration);
            float entrance = EaseOutBack(Mathf.Clamp01(elapsed / 0.48f), 1.15f);
            float fade = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(1.85f, CelebrationDuration, elapsed));

            UpdateText(entrance, fade, elapsed);
            UpdateOrbitPucks(entrance, fade, elapsed);
            UpdateRings(elapsed);

            float lightAttack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.18f));
            _awardLight.intensity = 4.2f * lightAttack * fade
                * (0.88f + Mathf.Sin(elapsed * 9f) * 0.12f);
            yield return null;
        }

        HideImmediate();
        _routine = null;
    }

    void UpdateText(float entrance, float fade, float elapsed)
    {
        Vector3 titlePosition = _focus + Vector3.up * Mathf.Lerp(1.0f, 2.85f, entrance);
        Vector3 subtitlePosition = _focus + Vector3.up * Mathf.Lerp(0.8f, 2.25f, entrance);
        _title.transform.position = titlePosition;
        _subtitle.transform.position = subtitlePosition;
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
            float height = 1.25f + Mathf.Sin(angle * 2f) * 0.22f
                + Mathf.Sin(Mathf.Clamp01(elapsed / 0.55f) * Mathf.PI) * 0.85f;
            Transform puck = _orbitPucks[i].transform;
            puck.position = _focus + new Vector3(Mathf.Cos(angle) * radius,
                height, Mathf.Sin(angle) * radius);
            puck.rotation = Quaternion.Euler(elapsed * 260f + i * 31f,
                -angle * Mathf.Rad2Deg + 90f, elapsed * 190f);
            float scale = 0.46f * Mathf.Clamp01(entrance) * Mathf.Clamp01(fade * 2f);
            puck.localScale = Vector3.one * scale;
            _orbitTrails[i].emitting = fade > 0.08f;
        }
    }

    void UpdateRings(float elapsed)
    {
        Color[] colors =
        {
            new Color(0.22f, 0.94f, 1f),
            new Color(1f, 0.70f, 0.14f),
            new Color(1f, 0.22f, 0.58f)
        };

        for (int r = 0; r < _rings.Length; r++)
        {
            float progress = Mathf.InverseLerp(r * 0.16f, r * 0.16f + 1.05f, elapsed);
            bool visible = progress > 0f && progress < 1f;
            LineRenderer ring = _rings[r];
            ring.enabled = visible;
            if (!visible) continue;

            float eased = EaseOutCubic(progress);
            float radius = Mathf.Lerp(0.32f, 4.6f, eased);
            float height = 0.08f + r * 0.11f + Mathf.Sin(progress * Mathf.PI) * 0.22f;
            for (int i = 0; i < RingSegments; i++)
                ring.SetPosition(i, _focus + _unitCircle[i] * radius + Vector3.up * height);

            Color color = colors[r];
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
        for (int i = 0; i < _orbitPucks.Length; i++)
        {
            if (_orbitTrails[i] != null)
            {
                _orbitTrails[i].emitting = false;
                _orbitTrails[i].Clear();
            }
            if (_orbitPucks[i] != null) _orbitPucks[i].SetActive(false);
        }
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

    static Material CreateEnergyMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        Material material = new Material(shader)
        {
            name = "Buca Hole-In-One Energy 3D",
            hideFlags = HideFlags.HideAndDontSave
        };
        Color color = new Color(0.22f, 0.94f, 1f, 1f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        return material;
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

    void OnDestroy()
    {
        if (_energyMaterial != null) Destroy(_energyMaterial);
    }
}
