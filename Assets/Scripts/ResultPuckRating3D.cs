#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Renders the result rating as three genuine 3D copies of Buca's live puck.
/// A single isolated camera and render texture keep this WebGL-friendly.
/// </summary>
public sealed class ResultPuckRating3D : MonoBehaviour
{
    const int PreviewLayer = 30;
    const int IconCount = 3;
    const float StageHeight = 1000f;

    readonly Transform[] _pucks = new Transform[IconCount];
    readonly MeshRenderer[] _renderers = new MeshRenderer[IconCount];
    readonly MeshRenderer[] _accentRings = new MeshRenderer[IconCount];
    readonly TrailRenderer[] _trails = new TrailRenderer[IconCount];
    readonly Material[] _puckMaterials = new Material[IconCount];
    readonly Vector3[] _restPositions = new Vector3[IconCount];
    readonly Vector3[] _restScales = new Vector3[IconCount];

    Image[] _fallbackIcons;
    RectTransform _viewport;
    RawImage _rawImage;
    GameObject _stageRoot;
    Camera _previewCamera;
    RenderTexture _renderTexture;
    Material _trailMaterial;
    Material _goldMaterial;
    Mesh _accentRingMesh;
    bool _ready;

    public bool IsReady => _ready && _previewCamera != null && _viewport != null;

    public static ResultPuckRating3D Ensure(Transform owner, Image[] fallbackIcons)
    {
        if (owner == null) return null;

        ResultPuckRating3D rating = owner.GetComponent<ResultPuckRating3D>();
        if (rating == null) rating = owner.gameObject.AddComponent<ResultPuckRating3D>();
        rating.Configure(fallbackIcons);
        return rating;
    }

    void Configure(Image[] fallbackIcons)
    {
        _fallbackIcons = fallbackIcons;
        if (!_ready) TryBuildStage();
        if (_ready) SetFallbackVisible(false);
    }

    void TryBuildStage()
    {
        GameObject livePuck = LevelManager.Instance != null ? LevelManager.Instance.Puck : null;
        MeshFilter sourceFilter = livePuck != null ? livePuck.GetComponent<MeshFilter>() : null;
        MeshRenderer sourceRenderer = livePuck != null ? livePuck.GetComponent<MeshRenderer>() : null;
        RectTransform row = FindRatingRow();

        if (sourceFilter == null || sourceFilter.sharedMesh == null ||
            sourceRenderer == null || sourceRenderer.sharedMaterial == null || row == null)
            return;

        BuildViewport(row);
        BuildStageRoot();
        BuildCamera();
        BuildLighting();
        BuildPucks(sourceFilter.sharedMesh, sourceRenderer.sharedMaterial, livePuck.transform.localScale);

        _ready = true;
        SetFallbackVisible(false);
        Hide();
    }

    RectTransform FindRatingRow()
    {
        if (_fallbackIcons == null) return null;
        for (int i = 0; i < _fallbackIcons.Length; i++)
        {
            if (_fallbackIcons[i] == null) continue;
            return _fallbackIcons[i].rectTransform.parent as RectTransform;
        }
        return null;
    }

    void BuildViewport(RectTransform row)
    {
        row.sizeDelta = new Vector2(680f, 195f);

        Transform existing = row.Find("PuckRating3DViewport");
        GameObject viewportGO = existing != null
            ? existing.gameObject
            : new GameObject("PuckRating3DViewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
        if (existing == null) viewportGO.transform.SetParent(row, false);

        _viewport = (RectTransform)viewportGO.transform;
        _viewport.anchorMin = _viewport.anchorMax = _viewport.pivot = new Vector2(0.5f, 0.5f);
        _viewport.anchoredPosition = new Vector2(0f, -2f);
        _viewport.sizeDelta = new Vector2(680f, 195f);

        _rawImage = viewportGO.GetComponent<RawImage>();
        _rawImage.color = Color.white;
        _rawImage.raycastTarget = false;

        // One modest render target is enough for all three pucks. Keeping it
        // below full UI resolution avoids WebGL frame spikes.
        _renderTexture = new RenderTexture(640, 192, 16, RenderTextureFormat.ARGB32)
        {
            name = "BucaResultPuckRating3D",
            antiAliasing = 1,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        _renderTexture.Create();
        _rawImage.texture = _renderTexture;
    }

    void BuildStageRoot()
    {
        _stageRoot = new GameObject("BucaResultPuckRating3DStage");
        _stageRoot.hideFlags = HideFlags.DontSave;
        _stageRoot.layer = PreviewLayer;
        _stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);
    }

    void BuildCamera()
    {
        GameObject cameraGO = new GameObject("RatingCamera", typeof(Camera));
        cameraGO.hideFlags = HideFlags.DontSave;
        cameraGO.layer = PreviewLayer;
        cameraGO.transform.SetParent(_stageRoot.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, 3.1f, -8.2f);
        cameraGO.transform.LookAt(_stageRoot.transform.position + new Vector3(0f, 0.03f, 0f));

        _previewCamera = cameraGO.GetComponent<Camera>();
        _previewCamera.orthographic = true;
        _previewCamera.orthographicSize = 1.04f;
        _previewCamera.nearClipPlane = 0.1f;
        _previewCamera.farClipPlane = 30f;
        _previewCamera.clearFlags = CameraClearFlags.SolidColor;
        _previewCamera.backgroundColor = new Color(0.018f, 0.008f, 0.045f, 0f);
        _previewCamera.cullingMask = 1 << PreviewLayer;
        _previewCamera.allowHDR = false;
        _previewCamera.allowMSAA = false;
        _previewCamera.targetTexture = _renderTexture;
        _previewCamera.enabled = false;
    }

    void BuildLighting()
    {
        GameObject keyGO = new GameObject("GoldKeyLight", typeof(Light));
        keyGO.hideFlags = HideFlags.DontSave;
        keyGO.layer = PreviewLayer;
        keyGO.transform.SetParent(_stageRoot.transform, false);
        keyGO.transform.localPosition = new Vector3(-1.8f, 3.0f, -2.8f);
        Light key = keyGO.GetComponent<Light>();
        key.type = LightType.Point;
        key.color = new Color(1f, 0.79f, 0.48f);
        key.intensity = 3.8f;
        key.range = 8f;
        key.cullingMask = 1 << PreviewLayer;
        key.shadows = LightShadows.None;

        GameObject rimGO = new GameObject("CyanRimLight", typeof(Light));
        rimGO.hideFlags = HideFlags.DontSave;
        rimGO.layer = PreviewLayer;
        rimGO.transform.SetParent(_stageRoot.transform, false);
        rimGO.transform.localPosition = new Vector3(0f, 1.4f, 1.8f);
        Light rim = rimGO.GetComponent<Light>();
        rim.type = LightType.Point;
        rim.color = new Color(0.10f, 0.88f, 1f);
        rim.intensity = 2.6f;
        rim.range = 10f;
        rim.cullingMask = 1 << PreviewLayer;
        rim.shadows = LightShadows.None;
    }

    void BuildPucks(Mesh mesh, Material sourceMaterial, Vector3 liveScale)
    {
        Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Sprites/Default");
        _trailMaterial = new Material(trailShader)
        {
            name = "BucaResultPuckGoldTrail",
            hideFlags = HideFlags.DontSave
        };
        if (_trailMaterial.HasProperty("_BaseColor"))
            _trailMaterial.SetColor("_BaseColor", new Color(1f, 0.62f, 0.06f, 1f));
        if (_trailMaterial.HasProperty("_Color"))
            _trailMaterial.SetColor("_Color", new Color(1f, 0.62f, 0.06f, 1f));

        _goldMaterial = new Material(sourceMaterial)
        {
            name = "BucaResultPuckGoldAccent",
            hideFlags = HideFlags.DontSave
        };
        Color gold = new Color(1f, 0.55f, 0.08f, 1f);
        if (_goldMaterial.HasProperty("_BaseColor")) _goldMaterial.SetColor("_BaseColor", gold);
        if (_goldMaterial.HasProperty("_Color")) _goldMaterial.SetColor("_Color", gold);
        if (_goldMaterial.HasProperty("_EmissionColor"))
        {
            _goldMaterial.EnableKeyword("_EMISSION");
            _goldMaterial.SetColor("_EmissionColor", new Color(0.34f, 0.10f, 0.005f, 1f));
        }
        if (_goldMaterial.HasProperty("_Metallic")) _goldMaterial.SetFloat("_Metallic", 0.96f);
        if (_goldMaterial.HasProperty("_Smoothness")) _goldMaterial.SetFloat("_Smoothness", 0.97f);

        _accentRingMesh = BuildAccentRingMesh(mesh.bounds);

        float sourceMax = Mathf.Max(liveScale.x, Mathf.Max(liveScale.y, liveScale.z));
        float displayScale = Mathf.Clamp(sourceMax * 2.10f, 1.16f, 1.34f);

        for (int i = 0; i < IconCount; i++)
        {
            GameObject puckGO = new GameObject($"RatingPuck_{i + 1}",
                typeof(MeshFilter), typeof(MeshRenderer), typeof(TrailRenderer));
            puckGO.hideFlags = HideFlags.DontSave;
            puckGO.layer = PreviewLayer;
            puckGO.transform.SetParent(_stageRoot.transform, false);

            _restPositions[i] = new Vector3((i - 1) * 1.70f, -0.10f, 0f);
            _restScales[i] = Vector3.one * displayScale;
            puckGO.transform.localPosition = _restPositions[i];
            puckGO.transform.localScale = _restScales[i];
            puckGO.transform.localRotation = Quaternion.identity;

            puckGO.GetComponent<MeshFilter>().sharedMesh = mesh;
            _puckMaterials[i] = new Material(sourceMaterial)
            {
                name = $"BucaResultPuckMaterial_{i + 1}",
                hideFlags = HideFlags.DontSave
            };
            _renderers[i] = puckGO.GetComponent<MeshRenderer>();
            _renderers[i].sharedMaterial = _puckMaterials[i];
            _renderers[i].shadowCastingMode = ShadowCastingMode.Off;
            _renderers[i].receiveShadows = false;

            GameObject ringGO = new GameObject("GoldShotRing", typeof(MeshFilter), typeof(MeshRenderer));
            ringGO.hideFlags = HideFlags.DontSave;
            ringGO.layer = PreviewLayer;
            ringGO.transform.SetParent(puckGO.transform, false);
            ringGO.GetComponent<MeshFilter>().sharedMesh = _accentRingMesh;
            _accentRings[i] = ringGO.GetComponent<MeshRenderer>();
            _accentRings[i].sharedMaterial = _goldMaterial;
            _accentRings[i].shadowCastingMode = ShadowCastingMode.Off;
            _accentRings[i].receiveShadows = false;

            _trails[i] = puckGO.GetComponent<TrailRenderer>();
            ConfigureTrail(_trails[i]);
            _pucks[i] = puckGO.transform;
            puckGO.SetActive(false);
        }
    }

    static Mesh BuildAccentRingMesh(Bounds puckBounds)
    {
        const int segments = 48;
        float puckRadius = Mathf.Max(puckBounds.extents.x, puckBounds.extents.z);
        float outer = puckRadius * 0.73f;
        float inner = outer * 0.72f;
        float top = puckBounds.max.y + 0.012f;

        var vertices = new Vector3[segments * 2];
        var normals = new Vector3[segments * 2];
        var uv = new Vector2[segments * 2];
        var triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i * 2] = new Vector3(x * inner, top, z * inner);
            vertices[i * 2 + 1] = new Vector3(x * outer, top, z * outer);
            normals[i * 2] = normals[i * 2 + 1] = Vector3.up;
            uv[i * 2] = new Vector2(i / (float)segments, 0f);
            uv[i * 2 + 1] = new Vector2(i / (float)segments, 1f);

            int next = (i + 1) % segments;
            int t = i * 6;
            triangles[t] = i * 2;
            triangles[t + 1] = next * 2 + 1;
            triangles[t + 2] = i * 2 + 1;
            triangles[t + 3] = i * 2;
            triangles[t + 4] = next * 2;
            triangles[t + 5] = next * 2 + 1;
        }

        var mesh = new Mesh { name = "BucaResultPuckGoldAccentRing" };
        mesh.hideFlags = HideFlags.DontSave;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    void ConfigureTrail(TrailRenderer trail)
    {
        trail.sharedMaterial = _trailMaterial;
        trail.time = 0.48f;
        trail.minVertexDistance = 0.025f;
        trail.widthMultiplier = 0.15f;
        trail.numCornerVertices = 5;
        trail.numCapVertices = 5;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;

        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.9f),
            new Keyframe(0.72f, 0.42f),
            new Keyframe(1f, 0f));
        trail.colorGradient = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(1f, 0.86f, 0.28f), 0f),
                new GradientColorKey(new Color(1f, 0.42f, 0.05f), 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        };
    }

    public void Prepare(int earnedCount)
    {
        if (!_ready) TryBuildStage();
        if (!IsReady) return;

        SetFallbackVisible(false);
        _viewport.gameObject.SetActive(true);
        if (_stageRoot != null) _stageRoot.SetActive(true);
        _previewCamera.enabled = true;

        for (int i = 0; i < IconCount; i++)
        {
            bool earned = i < earnedCount;
            SetPuckMaterial(i, earned);
            _pucks[i].gameObject.SetActive(false);
            _pucks[i].localPosition = _restPositions[i] + new Vector3(-0.46f, 0f, 0f);
            _pucks[i].localScale = Vector3.Scale(_restScales[i], new Vector3(0.72f, 0.72f, 0.72f));
            _pucks[i].localRotation = Quaternion.identity;
            _trails[i].emitting = false;
            _trails[i].Clear();
            if (_accentRings[i] != null) _accentRings[i].enabled = earned;
        }
    }

    void SetPuckMaterial(int index, bool earned)
    {
        Material material = _puckMaterials[index];
        if (material == null) return;

        Color baseColor = earned
            ? new Color(0.86f, 0.90f, 0.98f, 1f)
            : new Color(0.13f, 0.18f, 0.26f, 1f);
        Color emission = earned
            ? new Color(0.018f, 0.11f, 0.17f, 1f)
            : new Color(0.005f, 0.012f, 0.022f, 1f);

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", earned ? 0.94f : 0.72f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", earned ? 0.96f : 0.72f);
    }

    public IEnumerator RevealTogether(int earnedCount)
    {
        if (!IsReady) yield break;

        if (earnedCount > 0 && AudioManager.Instance != null)
            AudioManager.Instance.PlayStarReveal(Mathf.Clamp(earnedCount - 1, 0, 2));

        for (int i = 0; i < IconCount; i++)
        {
            bool earned = i < earnedCount;
            Transform puck = _pucks[i];
            Vector3 rest = _restPositions[i];
            puck.gameObject.SetActive(true);
            puck.localPosition = rest + new Vector3(-0.25f, -0.08f, 0f);
            puck.localScale = _restScales[i] * 0.78f;
            puck.localRotation = Quaternion.Euler(0f, -12f, -4f);
            _trails[i].Clear();
            _trails[i].emitting = earned;
        }

        // All three objects move in one synchronized premium jump. No serial
        // waiting, no spinning carousel, and no delayed text reveal.
        const float jumpDuration = 0.54f;
        float t = 0f;
        while (t < jumpDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / jumpDuration);
            float travel = 1f - Mathf.Pow(1f - k, 3f);
            float height = Mathf.Sin(k * Mathf.PI) * 0.43f;
            for (int i = 0; i < IconCount; i++)
            {
                Vector3 rest = _restPositions[i];
                Vector3 start = rest + new Vector3(-0.25f, -0.08f, 0f);
                _pucks[i].localPosition = Vector3.Lerp(start, rest, travel) + Vector3.up * height;
                _pucks[i].localScale = _restScales[i] * Mathf.Lerp(0.78f, 1f, travel);
                _pucks[i].localRotation = Quaternion.Euler(
                    Mathf.Sin(k * Mathf.PI) * -5f,
                    Mathf.Lerp(-12f, 14f, travel),
                    Mathf.Sin(k * Mathf.PI) * -6f);
            }
            yield return null;
        }

        for (int i = 0; i < IconCount; i++)
        {
            _trails[i].emitting = false;
            _pucks[i].localPosition = _restPositions[i];
        }

        // One restrained synchronized landing beat.
        const float landingDuration = 0.16f;
        t = 0f;
        while (t < landingDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / landingDuration);
            float impact = Mathf.Sin(k * Mathf.PI);
            for (int i = 0; i < IconCount; i++)
            {
                _pucks[i].localScale = Vector3.Scale(_restScales[i],
                    new Vector3(1f + impact * 0.05f, 1f - impact * 0.10f, 1f + impact * 0.05f));
                _pucks[i].localRotation = Quaternion.Slerp(
                    Quaternion.Euler(0f, 14f, 0f), Quaternion.identity, k);
            }
            yield return null;
        }

        for (int i = 0; i < IconCount; i++)
        {
            _pucks[i].localPosition = _restPositions[i];
            _pucks[i].localScale = _restScales[i];
            _pucks[i].localRotation = Quaternion.identity;
        }

        // Let the short trails naturally disappear, render the clean final
        // frame once, then stop the preview camera to remove ongoing GPU cost.
        yield return WaitUnscaled(0.50f);
        for (int i = 0; i < IconCount; i++) _trails[i].Clear();
        yield return null;
        _previewCamera.enabled = false;
        if (_stageRoot != null) _stageRoot.SetActive(false);
    }

    public void Hide()
    {
        if (_previewCamera != null) _previewCamera.enabled = false;
        if (_stageRoot != null) _stageRoot.SetActive(false);
        if (_viewport != null) _viewport.gameObject.SetActive(false);
        for (int i = 0; i < _trails.Length; i++)
        {
            if (_trails[i] == null) continue;
            _trails[i].emitting = false;
            _trails[i].Clear();
        }
    }

    void SetFallbackVisible(bool visible)
    {
        if (_fallbackIcons == null) return;
        for (int i = 0; i < _fallbackIcons.Length; i++)
            if (_fallbackIcons[i] != null) _fallbackIcons[i].gameObject.SetActive(visible);
    }

    static IEnumerator WaitUnscaled(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;
    }

    void OnDestroy()
    {
        if (_previewCamera != null) _previewCamera.targetTexture = null;
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        for (int i = 0; i < _puckMaterials.Length; i++)
            if (_puckMaterials[i] != null) Destroy(_puckMaterials[i]);
        if (_trailMaterial != null) Destroy(_trailMaterial);
        if (_goldMaterial != null) Destroy(_goldMaterial);
        if (_accentRingMesh != null) Destroy(_accentRingMesh);
        if (_stageRoot != null) Destroy(_stageRoot);
    }
}
#endif
