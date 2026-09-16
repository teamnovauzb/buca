using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Builds the level result as genuine 3D geometry and renders it with one
/// short-lived camera. The camera stops after the entrance animation, so the
/// final frame has no continuous rendering cost in WebGL.
/// </summary>
public sealed class PremiumResultTotem3D : MonoBehaviour
{
    const int PreviewLayer = 29;
    const int PuckCount = 3;
    const float StageHeight = 2000f;

    static readonly Color Cyan = new Color(0.10f, 0.90f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.64f, 0.08f, 1f);
    static readonly Color Coral = new Color(1f, 0.26f, 0.38f, 1f);
    static readonly Color Ice = new Color(0.84f, 0.95f, 1f, 1f);

    readonly List<Material> _materials = new List<Material>(16);
    readonly List<Mesh> _meshes = new List<Mesh>(8);
    readonly List<Texture2D> _textures = new List<Texture2D>(12);
    readonly Transform[] _pucks = new Transform[PuckCount];
    readonly MeshRenderer[] _puckRenderers = new MeshRenderer[PuckCount];
    readonly MeshRenderer[] _puckRings = new MeshRenderer[PuckCount];
    readonly MeshRenderer[] _puckFaceRenderers = new MeshRenderer[PuckCount];
    readonly Vector3[] _puckRestPositions = new Vector3[PuckCount];
    readonly Vector3[] _puckRestScales = new Vector3[PuckCount];

    RectTransform _viewport;
    RawImage _rawImage;
    RenderTexture _renderTexture;
    GameObject _stageRoot;
    Camera _camera;
    Transform _totem;
    TMP_FontAsset _font;
    TextMeshPro _levelText;
    TextMeshPro _resultText;
    TextMeshPro _strokesText;
    Material _puckShellMaterial;
    Material _earnedPuckFaceMaterial;
    Material _unearnedPuckFaceMaterial;
    Material _earnedPuckRingMaterial;
    Material _unearnedPuckRingMaterial;
    bool _ready;

    public bool IsReady => _ready && _camera != null && _viewport != null;

    public static PremiumResultTotem3D Ensure(Transform owner, TMP_FontAsset font)
    {
        if (owner == null) return null;

        PremiumResultTotem3D result = owner.GetComponent<PremiumResultTotem3D>();
        if (result == null) result = owner.gameObject.AddComponent<PremiumResultTotem3D>();
        result._font = font != null ? font : TMP_Settings.defaultFontAsset;
        return result;
    }

    public void Prepare(ScoreCalculator.ScoreBreakdown score)
    {
        if (!_ready) Build();
        if (!IsReady) return;

        LevelManager manager = LevelManager.Instance;
        int levelNumber = manager != null ? manager.CurrentLevelNumber : 1;
        int earned = Mathf.Clamp(score.stars, 0, PuckCount);
        string puckReward = earned == 1 ? "1 PUCK WON" : $"{earned} PUCKS WON";
        string strokeWord = score.strokesUsed == 1 ? "STROKE" : "STROKES";
        string golfMeaning = ScoreCalculator.FormatStrokesToPar(score.strokesToPar);

        SetText(_levelText, $"LEVEL {levelNumber}  •  {puckReward}");
        SetText(_resultText, score.golfResult);
        SetText(_strokesText,
            $"{score.strokesUsed} {strokeWord}  /  PAR {score.par}  •  {golfMeaning}");

        Color resultColor = score.strokesToPar <= 0 ? Gold : Coral;
        _resultText.color = resultColor;

        for (int i = 0; i < PuckCount; i++)
        {
            bool isEarned = i < earned;
            if (_puckRenderers[i] != null)
                _puckRenderers[i].sharedMaterial = _puckShellMaterial;
            if (_puckFaceRenderers[i] != null)
                _puckFaceRenderers[i].sharedMaterial = isEarned
                    ? _earnedPuckFaceMaterial
                    : _unearnedPuckFaceMaterial;
            if (_puckRings[i] != null)
            {
                _puckRings[i].sharedMaterial = isEarned
                    ? _earnedPuckRingMaterial
                    : _unearnedPuckRingMaterial;
                _puckRings[i].enabled = true;
            }
            if (_pucks[i] != null)
            {
                _pucks[i].localPosition = _puckRestPositions[i];
                _pucks[i].localScale = _puckRestScales[i];
                _pucks[i].localRotation = GetPuckRestRotation(i);
            }
        }

        _totem.localPosition = new Vector3(0f, -4.8f, 0f);
        _totem.localScale = new Vector3(0.94f, 0.94f, 0.94f);
        _totem.localRotation = Quaternion.identity;
        _viewport.gameObject.SetActive(false);
        _camera.enabled = false;
    }

    public IEnumerator Reveal(int earnedCount)
    {
        if (!IsReady) yield break;

        _viewport.gameObject.SetActive(true);
        _viewport.SetAsLastSibling();
        if (_stageRoot != null) _stageRoot.SetActive(true);
        _camera.enabled = true;

        // The complete result is present from the first frame; the physical
        // totem simply rises from the table and settles as one object.
        const float riseDuration = 0.72f;
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            float eased = EaseOutBack(t, 1.15f);
            _totem.localPosition = Vector3.LerpUnclamped(
                new Vector3(0f, -4.8f, 0f), Vector3.zero, eased);
            float scale = Mathf.LerpUnclamped(0.94f, 1f, eased);
            _totem.localScale = Vector3.one * scale;
            _totem.localRotation = Quaternion.identity;
            yield return null;
        }

        _totem.localPosition = Vector3.zero;
        _totem.localScale = Vector3.one;
        _totem.localRotation = Quaternion.identity;

        // The real player pucks confirm their rating one by one with a short,
        // controlled jump. Nothing spins or flies across the screen.
        for (int i = 0; i < PuckCount; i++)
        {
            yield return AnimatePuckLock(i, i < earnedCount);
        }

        // Keep one clean final frame in the RenderTexture, then stop the
        // camera. The result remains visible without an ongoing GPU cost.
        yield return null;
        _camera.enabled = false;
        if (_stageRoot != null) _stageRoot.SetActive(false);
    }

    public void Hide()
    {
        if (_camera != null) _camera.enabled = false;
        if (_stageRoot != null) _stageRoot.SetActive(false);
        if (_viewport != null) _viewport.gameObject.SetActive(false);
    }

    IEnumerator AnimatePuckLock(int index, bool earned)
    {
        Transform puck = _pucks[index];
        if (puck == null) yield break;

        if (earned && AudioManager.Instance != null)
            AudioManager.Instance.PlayStarReveal(Mathf.Clamp(index, 0, 2));

        Vector3 rest = _puckRestPositions[index];
        Vector3 restScale = _puckRestScales[index];
        Quaternion restRotation = GetPuckRestRotation(index);
        const float duration = 0.26f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float jump = Mathf.Sin(t * Mathf.PI) * (earned ? 0.32f : 0.10f);
            float settle = 1f + Mathf.Sin(t * Mathf.PI) * (earned ? 0.08f : 0.025f);
            puck.localPosition = rest + Vector3.up * jump;
            puck.localScale = restScale * settle;
            float turn = Mathf.Sin(t * Mathf.PI) * (earned ? 28f : 12f);
            puck.localRotation = Quaternion.AngleAxis(turn, Vector3.up) * restRotation;
            yield return null;
        }

        puck.localPosition = rest;
        puck.localScale = restScale;
        puck.localRotation = restRotation;
    }

    static Quaternion GetPuckRestRotation(int index)
    {
        // Keep every player facing the viewer. A very small symmetric fan still
        // reveals genuine thickness without making any puck look sideways.
        float yaw = index == 0 ? -9f : index == 1 ? 0f : 9f;
        return Quaternion.AngleAxis(yaw, Vector3.up)
            * Quaternion.Euler(-90f, 0f, 0f);
    }

    void Build()
    {
        BuildViewport();
        BuildStage();
        BuildMaterials();
        BuildTotem();
        BuildPucks();
        _ready = true;
        Hide();
    }

    void BuildViewport()
    {
        Transform existing = transform.Find("PremiumResultTotem3DViewport");
        GameObject viewportGO = existing != null
            ? existing.gameObject
            : new GameObject("PremiumResultTotem3DViewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
        if (existing == null) viewportGO.transform.SetParent(transform, false);

        _viewport = (RectTransform)viewportGO.transform;
        _viewport.anchorMin = _viewport.anchorMax = _viewport.pivot = new Vector2(0.5f, 0.5f);
        _viewport.anchoredPosition = new Vector2(0f, 18f);
        _viewport.sizeDelta = new Vector2(1040f, 800f);
        _viewport.SetAsLastSibling();

        _rawImage = viewportGO.GetComponent<RawImage>();
        _rawImage.color = Color.white;
        _rawImage.raycastTarget = false;

        _renderTexture = new RenderTexture(1152, 896, 24, RenderTextureFormat.ARGB32)
        {
            name = "BucaPremiumResultTotem3D",
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        _renderTexture.Create();
        _rawImage.texture = _renderTexture;
    }

    void BuildStage()
    {
        _stageRoot = new GameObject("BucaPremiumResultTotem3DStage")
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        _stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);

        GameObject cameraGO = new GameObject("PremiumResultCamera", typeof(Camera))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        cameraGO.transform.SetParent(_stageRoot.transform, false);
        // Centre the camera so the complete result faces the player squarely.
        // Perspective, bevels, shadows and restrained puck angles retain the
        // genuine 3D read without skewing the board to either side.
        cameraGO.transform.localPosition = new Vector3(0f, 0.35f, -14.4f);
        cameraGO.transform.LookAt(_stageRoot.transform.position + new Vector3(0f, 0.10f, 0f));
        _camera = cameraGO.GetComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = 1 << PreviewLayer;
        _camera.fieldOfView = 28.5f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 30f;
        _camera.allowHDR = true;
        _camera.allowMSAA = true;
        _camera.targetTexture = _renderTexture;
        _camera.enabled = false;

        AddDirectionalLight("WarmKey", new Color(1f, 0.76f, 0.48f), 1.25f,
            Quaternion.Euler(34f, -42f, 0f));
        AddDirectionalLight("CoolFill", new Color(0.18f, 0.70f, 1f), 0.48f,
            Quaternion.Euler(20f, 138f, 0f));

        GameObject rimGO = new GameObject("CyanRim", typeof(Light))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        rimGO.transform.SetParent(_stageRoot.transform, false);
        rimGO.transform.localPosition = new Vector3(3.8f, 1.1f, -1.4f);
        Light rim = rimGO.GetComponent<Light>();
        rim.type = LightType.Point;
        rim.color = Cyan;
        rim.intensity = 2.15f;
        rim.range = 7.5f;
        rim.cullingMask = 1 << PreviewLayer;
        rim.shadows = LightShadows.None;

        _totem = new GameObject("PremiumResultTotem").transform;
        _totem.gameObject.hideFlags = HideFlags.DontSave;
        _totem.gameObject.layer = PreviewLayer;
        _totem.SetParent(_stageRoot.transform, false);

        AddPointLight("Gold Edge Light", new Vector3(-4.0f, 2.45f, -2.3f),
            new Color(1f, 0.53f, 0.16f), 2.40f, 7f);
        AddPointLight("Cyan Edge Light", new Vector3(4.1f, -0.35f, -2.0f),
            new Color(0.08f, 0.72f, 1f), 2.20f, 7f);
        AddPointLight("Puck Sculpt Light", new Vector3(-0.7f, 3.55f, -3.1f),
            new Color(0.76f, 0.92f, 1f), 1.35f, 5.2f);
        AddPointLight("Left Column Rim Light", new Vector3(-4.35f, 0.05f, -1.4f),
            new Color(0.10f, 0.66f, 0.78f), 0.52f, 3.8f);
        AddPointLight("Right Column Rim Light", new Vector3(4.35f, 0.05f, -1.4f),
            new Color(0.10f, 0.66f, 0.78f), 0.52f, 3.8f);
    }

    void BuildMaterials()
    {
        // The reference shape remains mechanical, while the broad lower faces
        // are now polished walnut. Real grain makes the result feel connected
        // to the Buca playfield instead of another generic black UI panel.
        Material darkMetal = CreateWoodMaterial("Totem Polished Walnut",
            new Color(0.045f, 0.020f, 0.014f, 1f),
            new Color(0.17f, 0.075f, 0.034f, 1f), 0.68f);
        Material blackMetal = CreateWoodMaterial("Totem Deep Walnut",
            new Color(0.025f, 0.010f, 0.008f, 1f),
            new Color(0.095f, 0.034f, 0.016f, 1f), 0.60f);
        Material goldMetal = CreateLitMaterial("Totem Gold",
            new Color(0.82f, 0.34f, 0.035f, 1f), 0.72f, 0.64f,
            new Color(0.12f, 0.026f, 0.001f, 1f));
        Material goldLight = CreateLitMaterial("Totem Gold Light",
            new Color(1f, 0.62f, 0.18f, 1f), 0.72f, 0.94f,
            new Color(0.70f, 0.18f, 0.01f, 1f));
        Material cyanLight = CreateLitMaterial("Totem Cyan Light",
            new Color(0.18f, 0.82f, 0.94f, 1f), 0.65f, 0.94f,
            new Color(0.035f, 0.68f, 0.82f, 1f));
        Material columnBronze = CreateLitMaterial("Column Dark Bronze",
            new Color(0.16f, 0.070f, 0.025f, 1f), 0.86f, 0.68f,
            new Color(0.010f, 0.003f, 0.001f, 1f));

        // Every rating object uses one coherent premium material language:
        // brushed silver shell, cyan glass face and champagne-gold ring.
        // Rating state is light-on/light-off, never a different object colour.
        _puckShellMaterial = CreatePuckMaterial("Premium Silver Puck Shell",
            new Color(0.48f, 0.61f, 0.72f, 1f),
            new Color(0.006f, 0.018f, 0.025f, 1f));
        _earnedPuckFaceMaterial = CreateLitMaterial("Earned Cyan Glass Lens",
            new Color(0.035f, 0.38f, 0.50f, 1f), 0.54f, 0.96f,
            new Color(0.012f, 0.18f, 0.24f, 1f));
        _unearnedPuckFaceMaterial = CreateLitMaterial("Dim Cyan Glass Face",
            new Color(0.035f, 0.12f, 0.17f, 1f), 0.78f, 0.84f,
            new Color(0.002f, 0.012f, 0.018f, 1f));
        _earnedPuckRingMaterial = CreateLitMaterial("Premium Gold Puck Ring",
            new Color(0.78f, 0.48f, 0.12f, 1f), 0.98f, 0.88f,
            new Color(0.12f, 0.045f, 0.003f, 1f));
        _unearnedPuckRingMaterial = CreateLitMaterial("Dim Gold Puck Ring",
            new Color(0.22f, 0.15f, 0.075f, 1f), 0.94f, 0.70f,
            Color.black);

        // Store construction materials by name for BuildTotem.
        _darkMetal = darkMetal;
        _blackMetal = blackMetal;
        _goldMetal = goldMetal;
        _goldLight = goldLight;
        _cyanLight = cyanLight;
        _columnBronze = columnBronze;
    }

    Material _darkMetal;
    Material _blackMetal;
    Material _goldMetal;
    Material _goldLight;
    Material _cyanLight;
    Material _columnBronze;

    void BuildTotem()
    {
        // Layered pedestal rings give the totem a physical footprint and a
        // surface that can catch the warm key and cool rim lights.
        Mesh torus = BuildTorusMesh(2.28f, 0.15f, 52, 10);
        _meshes.Add(torus);
        GameObject baseRing = CreateMeshObject("Recessed Base Ring", torus, _goldMetal, _totem);
        baseRing.transform.localPosition = new Vector3(0f, -1.64f, 0.36f);
        baseRing.transform.localRotation = Quaternion.identity;
        baseRing.transform.localScale = new Vector3(1.18f, 1f, 0.62f);

        Mesh innerTorus = BuildTorusMesh(1.92f, 0.075f, 52, 8);
        _meshes.Add(innerTorus);
        GameObject innerRing = CreateMeshObject("Cyan Pedestal Ring", innerTorus, _cyanLight, _totem);
        innerRing.transform.localPosition = new Vector3(0f, -1.61f, 0.20f);
        innerRing.transform.localRotation = Quaternion.identity;
        innerRing.transform.localScale = new Vector3(1.18f, 1f, 0.62f);

        CreateCylinder("Raised Pedestal", new Vector3(0f, -1.73f, 0.48f),
            new Vector3(2.60f, 0.13f, 1.40f), _blackMetal, Quaternion.identity);

        // Keep the columns outside the widest gold trim. The previous overlap
        // intersected the panel corners and produced the clipped wedges shown
        // in the bug screenshot.
        CreatePremiumSideColumn("Left", -4.02f);
        CreatePremiumSideColumn("Right", 4.02f);

        CreateRow("Level Band", 0.76f, 6.55f, 0.58f, _darkMetal,
            out _levelText, Ice, 2.65f);
        CreateRow("Result Band", -0.12f, 7.05f, 0.92f, _blackMetal,
            out _resultText, Coral, 3.75f);
        CreateRow("Strokes Band", -1.00f, 6.55f, 0.58f, _darkMetal,
            out _strokesText, Cyan, 2.45f);

        // Thin illuminated seams supply depth without turning the result into
        // another flat cyan rectangle.
        float[] seams = { 0.40f, -0.65f, -1.38f };
        for (int i = 0; i < seams.Length; i++)
            CreateBar($"Cyan Seam {i + 1}", new Vector3(0f, seams[i], -0.08f),
                new Vector3(6.12f, 0.040f, 0.045f), _cyanLight);

        // Small metal mounting feet under the three rating pucks.
        for (int i = 0; i < PuckCount; i++)
        {
            float x = (i - 1) * 1.72f;
            CreateBar($"Puck Stand {i + 1}", new Vector3(x, 1.34f, 0.04f),
                new Vector3(0.13f, 0.30f, 0.13f), _goldMetal);
            CreateBar($"Puck Shelf {i + 1}", new Vector3(x, 1.20f, 0.04f),
                new Vector3(1.25f, 0.10f, 0.16f), _goldMetal);
        }
    }

    void CreatePremiumSideColumn(string sideName, float x)
    {
        // Option B: one substantial machined tower. A smooth 64-sided bronze
        // body supplies real curved volume; the cyan tube is physically inset
        // against its front instead of pretending the whole column is a line.
        Mesh bodyMesh = BuildPremiumPuckShellMesh(0.29f, 1.38f, 0.085f, 64);
        Mesh glassMesh = BuildPremiumPuckShellMesh(0.073f, 1.08f, 0.030f, 40);
        _meshes.Add(bodyMesh);
        _meshes.Add(glassMesh);

        GameObject body = CreateMeshObject($"{sideName} Bronze Column Body",
            bodyMesh, _columnBronze, _totem);
        body.transform.localPosition = new Vector3(x, 0.02f, 0.08f);

        GameObject glass = CreateMeshObject($"{sideName} Recessed Cyan Glass",
            glassMesh, _cyanLight, _totem);
        glass.transform.localPosition = new Vector3(x, 0.02f, -0.225f);

        // Exactly three broad machined collars, as in the selected concept.
        // Short solid cylinders expose top, front and curved side surfaces.
        float[] collarY = { 1.10f, 0.02f, -1.06f };
        for (int i = 0; i < collarY.Length; i++)
        {
            CreateCylinder($"{sideName} Broad Gold Collar {i + 1}",
                new Vector3(x, collarY[i], 0.05f),
                new Vector3(0.72f, 0.070f, 0.72f),
                _goldMetal, Quaternion.identity);
        }

        // Restrained stacked crown: no orb, gemstone or decorative symbol.
        CreateCylinder($"{sideName} Gold Crown Ring",
            new Vector3(x, 1.42f, 0.07f),
            new Vector3(0.72f, 0.070f, 0.72f), _goldMetal, Quaternion.identity);
        CreateCylinder($"{sideName} Bronze Crown",
            new Vector3(x, 1.54f, 0.07f),
            new Vector3(0.62f, 0.075f, 0.62f), _columnBronze, Quaternion.identity);
        CreateCylinder($"{sideName} Crown Lip",
            new Vector3(x, 1.66f, 0.07f),
            new Vector3(0.70f, 0.050f, 0.70f), _goldMetal, Quaternion.identity);

        // Heavy round pedestal gives the tower weight and a visible footprint.
        CreateCylinder($"{sideName} Gold Base Collar",
            new Vector3(x, -1.34f, 0.08f),
            new Vector3(0.76f, 0.065f, 0.76f), _goldMetal, Quaternion.identity);
        CreateCylinder($"{sideName} Bronze Pedestal",
            new Vector3(x, -1.48f, 0.10f),
            new Vector3(0.88f, 0.095f, 0.88f), _columnBronze, Quaternion.identity);
        CreateCylinder($"{sideName} Gold Pedestal Rim",
            new Vector3(x, -1.62f, 0.10f),
            new Vector3(0.82f, 0.050f, 0.82f), _goldMetal, Quaternion.identity);

        // Small separated bridges connect the post to each gold backplate. They
        // preserve the assembled look without allowing meshes to intersect.
        float sign = Mathf.Sign(x);
        float connectorX = sign * 3.82f;
        float[] connectorY = { 0.76f, -0.12f, -1.00f };
        for (int i = 0; i < connectorY.Length; i++)
            CreateBar($"{sideName} Column Bridge {i + 1}",
                new Vector3(connectorX, connectorY[i], 0.10f),
                new Vector3(0.34f, 0.070f, 0.16f), _goldMetal);
    }

    void CreateRow(string name, float y, float width, float height, Material material,
        out TextMeshPro text, Color textColor, float fontSize)
    {
        // The backing is substantially deeper than the front face. Together
        // with the three-quarter camera this exposes real top/right side faces,
        // which is the key difference between a 3D totem and a flat UI card.
        Mesh trimMesh = BuildChamferedBoxMesh(width + 0.28f, height + 0.18f, 0.82f, 0.17f);
        _meshes.Add(trimMesh);
        // The deep rear shell is walnut too. Metallic gold on this large side
        // face reflected the cool light as the grey slab beside each row.
        GameObject trim = CreateMeshObject(name + " Walnut Backing", trimMesh, material, _totem);
        trim.transform.localPosition = new Vector3(0f, y, 0.20f);

        Mesh mesh = BuildChamferedBoxMesh(width, height, 0.42f, 0.13f);
        _meshes.Add(mesh);
        GameObject panel = CreateMeshObject(name, mesh, material, _totem);
        panel.transform.localPosition = new Vector3(0f, y, -0.15f);

        // A second, thinner beveled walnut slab sits physically above the main
        // body. This creates a real highlight/shadow step around every row and
        // prevents the texture from reading as a flat printed rectangle.
        Mesh faceMesh = BuildChamferedBoxMesh(
            Mathf.Max(0.5f, width - 0.18f),
            Mathf.Max(0.32f, height - 0.11f), 0.12f, 0.095f);
        _meshes.Add(faceMesh);
        GameObject raisedFace = CreateMeshObject(name + " Raised Walnut Face",
            faceMesh, material, panel.transform);
        raisedFace.transform.localPosition = new Vector3(0f, 0f, -0.25f);

        // A dark offset copy gives the lettering physical depth; the visible
        // copy sits in front of the metal rather than on a Canvas.
        CreateWorldText(name + " Depth", panel.transform, string.Empty,
            new Vector3(0.050f, -0.045f, -0.325f), width - 0.35f, height * 0.90f,
            new Color(0f, 0f, 0f, 0.92f), fontSize, 0);
        CreateWorldText(name + " Depth Mid", panel.transform, string.Empty,
            new Vector3(0.026f, -0.022f, -0.350f), width - 0.35f, height * 0.90f,
            new Color(0.02f, 0.025f, 0.035f, 1f), fontSize, 0);
        text = CreateWorldText(name + " Text", panel.transform, string.Empty,
            new Vector3(0f, 0f, -0.380f), width - 0.35f, height * 0.90f,
            textColor, fontSize, 1);
    }

    void BuildPucks()
    {
        // The gameplay player's shape is a puck, but its low-sided gameplay
        // mesh looks faceted at this close result-screen scale. This smooth,
        // presentation-only shell keeps the same identity while adding a real
        // rounded bevel and enough geometry for clean specular highlights.
        Mesh premiumShell = BuildPremiumPuckShellMesh(0.68f, 0.31f, 0.10f, 64);
        Mesh shoulderRing = BuildTorusMesh(0.575f, 0.052f, 56, 10);
        Mesh goldRing = BuildTorusMesh(0.455f, 0.045f, 56, 10);
        Mesh sideBand = BuildTorusMesh(0.680f, 0.028f, 64, 8);
        Mesh rearRing = BuildTorusMesh(0.575f, 0.040f, 56, 9);
        _meshes.Add(premiumShell);
        _meshes.Add(shoulderRing);
        _meshes.Add(goldRing);
        _meshes.Add(sideBand);
        _meshes.Add(rearRing);

        for (int i = 0; i < PuckCount; i++)
        {
            GameObject puckGO = new GameObject($"Premium 3D Player Puck {i + 1}",
                typeof(MeshFilter), typeof(MeshRenderer));
            puckGO.GetComponent<MeshFilter>().sharedMesh = premiumShell;

            puckGO.hideFlags = HideFlags.DontSave;
            puckGO.layer = PreviewLayer;
            puckGO.transform.SetParent(_totem, false);
            _puckRestPositions[i] = new Vector3((i - 1) * 1.72f, 2.03f, -0.02f);
            puckGO.transform.localPosition = _puckRestPositions[i];
            puckGO.transform.localRotation = GetPuckRestRotation(i);
            _puckRestScales[i] = Vector3.one;
            puckGO.transform.localScale = _puckRestScales[i];

            _pucks[i] = puckGO.transform;
            _puckRenderers[i] = puckGO.GetComponent<MeshRenderer>();
            if (_puckRenderers[i] != null)
            {
                _puckRenderers[i].sharedMaterial = _puckShellMaterial;
                _puckRenderers[i].shadowCastingMode = ShadowCastingMode.On;
                _puckRenderers[i].receiveShadows = true;
            }

            // A raised centre band and rear rim make the full body thickness
            // readable from the three-quarter angles—not only the front face.
            GameObject band = CreateMeshObject($"Gold Side Band {i + 1}",
                sideBand, _goldMetal, puckGO.transform);
            band.transform.localPosition = Vector3.zero;

            GameObject back = CreateMeshObject($"Rear Bevel Ring {i + 1}",
                rearRing, _puckShellMaterial, puckGO.transform);
            back.transform.localPosition = new Vector3(0f, -0.285f, 0f);

            // Two raised front layers expose real curved highlights. Every
            // layer is parented to the player so it jumps and turns as one mesh.
            GameObject shoulder = CreateMeshObject($"Silver Shoulder {i + 1}",
                shoulderRing, _puckShellMaterial, puckGO.transform);
            shoulder.transform.localPosition = new Vector3(0f, 0.295f, 0f);

            GameObject ring = CreateMeshObject($"Gold Lens Retainer {i + 1}",
                goldRing, _unearnedPuckRingMaterial, puckGO.transform);
            ring.transform.localPosition = new Vector3(0f, 0.365f, 0f);
            _puckRings[i] = ring.GetComponent<MeshRenderer>();

            // A shallow sphere is a curved glass lens, not another flat disc.
            // It catches the warm key and cyan rim lights across its surface.
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lens.name = $"Curved Glass Lens {i + 1}";
            lens.hideFlags = HideFlags.DontSave;
            lens.layer = PreviewLayer;
            lens.transform.SetParent(puckGO.transform, false);
            lens.transform.localPosition = new Vector3(0f, 0.348f, 0f);
            lens.transform.localScale = new Vector3(0.78f, 0.20f, 0.78f);
            Collider lensCollider = lens.GetComponent<Collider>();
            if (lensCollider != null) Destroy(lensCollider);
            _puckFaceRenderers[i] = lens.GetComponent<MeshRenderer>();
            if (_puckFaceRenderers[i] != null)
            {
                _puckFaceRenderers[i].sharedMaterial = _unearnedPuckFaceMaterial;
                _puckFaceRenderers[i].shadowCastingMode = ShadowCastingMode.On;
                _puckFaceRenderers[i].receiveShadows = true;
            }
        }
    }

    TextMeshPro CreateWorldText(string name, Transform parent, string content,
        Vector3 localPosition, float width, float height, Color color,
        float fontSize, int sortingOrder)
    {
        GameObject textGO = new GameObject(name, typeof(TextMeshPro))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        textGO.transform.SetParent(parent, false);
        textGO.transform.localPosition = localPosition;
        textGO.transform.localRotation = Quaternion.identity;

        TextMeshPro tmp = textGO.GetComponent<TextMeshPro>();
        if (_font != null) tmp.font = _font;
        tmp.text = content;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = fontSize;
        tmp.characterSpacing = 1.2f;
        tmp.rectTransform.sizeDelta = new Vector2(width, height);
        tmp.renderer.sortingOrder = sortingOrder;

        // TMP remains world-space geometry here. A restrained bevel and outline
        // help the face catch light while the offset copies provide actual
        // visible side depth.
        Material textMaterial = tmp.fontMaterial;
        SetMaterialFloat(textMaterial, "_FaceDilate", 0.08f);
        SetMaterialFloat(textMaterial, "_OutlineWidth", sortingOrder > 0 ? 0.055f : 0f);
        SetMaterialFloat(textMaterial, "_Bevel", sortingOrder > 0 ? 0.42f : 0f);
        SetMaterialFloat(textMaterial, "_BevelWidth", sortingOrder > 0 ? 0.18f : 0f);
        if (textMaterial.HasProperty("_OutlineColor"))
            textMaterial.SetColor("_OutlineColor", new Color(0.02f, 0.035f, 0.055f, 0.92f));
        return tmp;
    }

    static void SetText(TextMeshPro text, string value)
    {
        if (text == null) return;
        TextMeshPro[] layers = text.transform.parent.GetComponentsInChildren<TextMeshPro>(true);
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].text = value;
            layers[i].ForceMeshUpdate();
        }
    }

    void CreateBar(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.hideFlags = HideFlags.DontSave;
        bar.layer = PreviewLayer;
        bar.transform.SetParent(_totem, false);
        bar.transform.localPosition = position;
        bar.transform.localScale = scale;
        Collider collider = bar.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        MeshRenderer renderer = bar.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    void CreateCylinder(string name, Vector3 position, Vector3 scale,
        Material material, Quaternion rotation)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.hideFlags = HideFlags.DontSave;
        cylinder.layer = PreviewLayer;
        cylinder.transform.SetParent(_totem, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localScale = scale;
        cylinder.transform.localRotation = rotation;
        Collider collider = cylinder.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        MeshRenderer renderer = cylinder.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    void CreateVisibleLamp(string name, Vector3 position, Material material)
    {
        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = name;
        lamp.hideFlags = HideFlags.DontSave;
        lamp.layer = PreviewLayer;
        lamp.transform.SetParent(_totem, false);
        lamp.transform.localPosition = position;
        lamp.transform.localScale = Vector3.one * 0.18f;
        Collider collider = lamp.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        MeshRenderer renderer = lamp.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    GameObject CreateMeshObject(string name, Mesh mesh, Material material, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return go;
    }

    Material CreatePuckMaterial(string name, Color color, Color emission)
    {
        return CreateLitMaterial(name, color, 0.97f, 0.82f, emission);
    }

    Material CreateWoodMaterial(string name, Color dark, Color light, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material material = new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.DontSave
        };
        _materials.Add(material);
        SetMaterialColor(material, Color.white);
        SetMaterialFloat(material, "_Metallic", 0.025f);
        SetMaterialFloat(material, "_Smoothness", smoothness);
        SetMaterialFloat(material, "_ClearCoatMask", 0.28f);
        SetMaterialFloat(material, "_ClearCoatSmoothness", 0.78f);

        Texture2D wood = CreateWoodTexture(name, dark, light);
        Texture2D woodNormal = CreateWoodNormalTexture(name);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", wood);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", wood);
        if (material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", woodNormal);
            material.EnableKeyword("_NORMALMAP");
            SetMaterialFloat(material, "_BumpScale", 0.42f);
        }
        if (material.HasProperty("_BaseMap"))
            material.SetTextureScale("_BaseMap", new Vector2(1.8f, 1.15f));
        if (material.HasProperty("_MainTex"))
            material.SetTextureScale("_MainTex", new Vector2(1.8f, 1.15f));
        return material;
    }

    Texture2D CreateWoodTexture(string materialName, Color dark, Color light)
    {
        const int width = 128;
        const int height = 64;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
            name = materialName + " Grain",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        int seed = 29;
        for (int i = 0; i < materialName.Length; i++)
            seed = (seed * 31 + materialName[i]) & 0x7fffffff;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float grain = SampleWoodGrain(x, y, seed);
            Color pixel = Color.Lerp(dark, light, grain);
            texture.SetPixel(x, y, pixel);
        }

        texture.Apply(false, true);
        _textures.Add(texture);
        return texture;
    }

    Texture2D CreateWoodNormalTexture(string materialName)
    {
        const int width = 128;
        const int height = 64;
        Texture2D normal = new Texture2D(
            width, height, TextureFormat.RGBA32, false, true)
        {
            name = materialName + " Grain Normal",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        int seed = 29;
        for (int i = 0; i < materialName.Length; i++)
            seed = (seed * 31 + materialName[i]) & 0x7fffffff;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float left = SampleWoodGrain(x - 1, y, seed);
            float right = SampleWoodGrain(x + 1, y, seed);
            float down = SampleWoodGrain(x, y - 1, seed);
            float up = SampleWoodGrain(x, y + 1, seed);
            Vector3 n = new Vector3((left - right) * 1.8f,
                (down - up) * 2.6f, 1f).normalized;
            normal.SetPixel(x, y, new Color(
                n.x * 0.5f + 0.5f,
                n.y * 0.5f + 0.5f,
                n.z * 0.5f + 0.5f, 1f));
        }

        normal.Apply(false, true);
        _textures.Add(normal);
        return normal;
    }

    static float SampleWoodGrain(int x, int y, int seed)
    {
        float flow = Mathf.PerlinNoise((x + seed % 41) * 0.026f,
            (y + seed % 59) * 0.095f);
        float longWave = Mathf.Sin(y * 0.22f + flow * 4.2f + x * 0.010f);
        float fine = Mathf.Sin(y * 0.74f + x * 0.020f + seed * 0.007f);
        return Mathf.Clamp01(0.50f + longWave * 0.15f
            + fine * 0.035f + (flow - 0.5f) * 0.10f);
    }

    Material CreateLitMaterial(string name, Color color, float metallic,
        float smoothness, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.DontSave
        };
        _materials.Add(material);
        SetMaterialColor(material, color);
        SetMaterialFloat(material, "_Metallic", metallic);
        SetMaterialFloat(material, "_Smoothness", smoothness);
        SetMaterialFloat(material, "_ClearCoatMask", 0.72f);
        SetMaterialFloat(material, "_ClearCoatSmoothness", 0.96f);
        SetEmission(material, emission);

        // A tiny deterministic brushed texture stops large metal faces from
        // reading as single-colour UI rectangles. It is generated once at
        // runtime and is small enough to be negligible in WebGL memory.
        if (!name.Contains(" Light"))
        {
            Texture2D brushed = CreateBrushedMetalTexture(name);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", brushed);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", brushed);
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", new Vector2(2.5f, 8f));
            if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", new Vector2(2.5f, 8f));
        }
        return material;
    }

    Texture2D CreateBrushedMetalTexture(string materialName)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false, true)
        {
            name = materialName + " Brushed Grain",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        int seed = 17;
        for (int i = 0; i < materialName.Length; i++)
            seed = (seed * 31 + materialName[i]) & 0x7fffffff;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float line = Mathf.Sin((y + seed * 0.013f) * 1.73f) * 0.040f;
            float fine = Mathf.Sin((y * 7.1f + x * 0.19f + seed * 0.003f)) * 0.018f;
            float noise = Mathf.PerlinNoise((x + seed % 37) * 0.11f, (y + seed % 53) * 0.37f);
            float value = Mathf.Clamp01(0.86f + line + fine + (noise - 0.5f) * 0.055f);
            texture.SetPixel(x, y, new Color(value, value, value, 1f));
        }

        texture.Apply(false, true);
        _textures.Add(texture);
        return texture;
    }

    static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property)) material.SetFloat(property, value);
    }

    static void SetEmission(Material material, Color emission)
    {
        if (!material.HasProperty("_EmissionColor")) return;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission);
    }

    void AddDirectionalLight(string name, Color color, float intensity, Quaternion rotation)
    {
        GameObject lightGO = new GameObject(name, typeof(Light))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        lightGO.transform.SetParent(_stageRoot.transform, false);
        lightGO.transform.localPosition = rotation * new Vector3(0f, 0f, -6f);
        Light light = lightGO.GetComponent<Light>();
        // Keep preview lighting spatially confined. Directional lights remain
        // global when a driver ignores light-layer masks and can overexpose the
        // next playable level after this result screen closes.
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity * 2.4f;
        light.range = 14f;
        light.cullingMask = 1 << PreviewLayer;
        light.shadows = LightShadows.Soft;
    }

    void AddPointLight(string name, Vector3 localPosition, Color color,
        float intensity, float range)
    {
        GameObject lightGO = new GameObject(name, typeof(Light))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        lightGO.transform.SetParent(_stageRoot.transform, false);
        lightGO.transform.localPosition = localPosition;
        Light light = lightGO.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.cullingMask = 1 << PreviewLayer;
        light.shadows = LightShadows.None;
    }

    static Mesh BuildChamferedBoxMesh(float width, float height, float depth, float bevel)
    {
        float x = width * 0.5f;
        float y = height * 0.5f;
        float z = depth * 0.5f;
        float b = Mathf.Min(bevel, Mathf.Min(x, y) * 0.45f);
        Vector2[] outline =
        {
            new Vector2(-x + b, -y), new Vector2(x - b, -y),
            new Vector2(x, -y + b), new Vector2(x, y - b),
            new Vector2(x - b, y), new Vector2(-x + b, y),
            new Vector2(-x, y - b), new Vector2(-x, -y + b)
        };

        var vertices = new List<Vector3>(64);
        var normals = new List<Vector3>(64);
        var uv = new List<Vector2>(64);
        var triangles = new List<int>(96);

        AddPolygonFace(outline, -z, Vector3.back, true, vertices, normals, uv, triangles);
        AddPolygonFace(outline, z, Vector3.forward, false, vertices, normals, uv, triangles);

        for (int i = 0; i < outline.Length; i++)
        {
            int next = (i + 1) % outline.Length;
            Vector2 a = outline[i];
            Vector2 c = outline[next];
            Vector3 normal = new Vector3(c.y - a.y, a.x - c.x, 0f).normalized;
            int start = vertices.Count;
            vertices.Add(new Vector3(a.x, a.y, -z));
            vertices.Add(new Vector3(c.x, c.y, -z));
            vertices.Add(new Vector3(c.x, c.y, z));
            vertices.Add(new Vector3(a.x, a.y, z));
            for (int n = 0; n < 4; n++) normals.Add(normal);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        Mesh mesh = new Mesh { name = "Buca Chamfered Result Band" };
        mesh.hideFlags = HideFlags.DontSave;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddPolygonFace(Vector2[] outline, float z, Vector3 normal, bool reverse,
        List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
    {
        int center = vertices.Count;
        vertices.Add(new Vector3(0f, 0f, z));
        normals.Add(normal);
        uv.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i < outline.Length; i++)
        {
            vertices.Add(new Vector3(outline[i].x, outline[i].y, z));
            normals.Add(normal);
            uv.Add(new Vector2(outline[i].x, outline[i].y));
        }
        for (int i = 0; i < outline.Length; i++)
        {
            int a = center + 1 + i;
            int b = center + 1 + ((i + 1) % outline.Length);
            triangles.Add(center);
            triangles.Add(reverse ? b : a);
            triangles.Add(reverse ? a : b);
        }
    }

    static Mesh BuildPremiumPuckShellMesh(float radius, float halfDepth,
        float bevel, int segments)
    {
        segments = Mathf.Max(24, segments);
        float innerRadius = Mathf.Max(0.05f, radius - bevel);
        float[] ringRadius = { innerRadius, radius, radius, innerRadius };
        float[] ringY =
        {
            -halfDepth,
            -halfDepth + bevel,
            halfDepth - bevel,
            halfDepth
        };

        var vertices = new List<Vector3>((segments + 1) * 4 + segments * 2 + 2);
        var normals = new List<Vector3>(vertices.Capacity);
        var uv = new List<Vector2>(vertices.Capacity);
        var triangles = new List<int>(segments * 30);

        for (int row = 0; row < ringRadius.Length; row++)
        {
            float vertical = row == 0 ? -0.70f : row == 3 ? 0.70f : 0f;
            float radialWeight = row == 0 || row == 3 ? 0.714f : 1f;
            for (int segment = 0; segment <= segments; segment++)
            {
                float u = segment / (float)segments;
                float angle = u * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices.Add(radial * ringRadius[row] + Vector3.up * ringY[row]);
                normals.Add((radial * radialWeight + Vector3.up * vertical).normalized);
                uv.Add(new Vector2(u, row / 3f));
            }
        }

        int stride = segments + 1;
        for (int row = 0; row < 3; row++)
        for (int segment = 0; segment < segments; segment++)
        {
            int current = row * stride + segment;
            int next = current + stride;
            triangles.Add(current);
            triangles.Add(next);
            triangles.Add(current + 1);
            triangles.Add(current + 1);
            triangles.Add(next);
            triangles.Add(next + 1);
        }

        AddPuckCap(-halfDepth, innerRadius, false, segments,
            vertices, normals, uv, triangles);
        AddPuckCap(halfDepth, innerRadius, true, segments,
            vertices, normals, uv, triangles);

        Mesh mesh = new Mesh { name = "Buca Premium Beveled Player Puck" };
        mesh.hideFlags = HideFlags.DontSave;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddPuckCap(float y, float radius, bool top, int segments,
        List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv,
        List<int> triangles)
    {
        Vector3 normal = top ? Vector3.up : Vector3.down;
        int center = vertices.Count;
        vertices.Add(new Vector3(0f, y, 0f));
        normals.Add(normal);
        uv.Add(new Vector2(0.5f, 0.5f));

        int rim = vertices.Count;
        for (int segment = 0; segment <= segments; segment++)
        {
            float u = segment / (float)segments;
            float angle = u * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, y, z));
            normals.Add(normal);
            uv.Add(new Vector2(x / (radius * 2f) + 0.5f,
                z / (radius * 2f) + 0.5f));
        }

        for (int segment = 0; segment < segments; segment++)
        {
            if (top)
            {
                triangles.Add(center);
                triangles.Add(rim + segment + 1);
                triangles.Add(rim + segment);
            }
            else
            {
                triangles.Add(center);
                triangles.Add(rim + segment);
                triangles.Add(rim + segment + 1);
            }
        }
    }

    static Mesh BuildTorusMesh(float majorRadius, float minorRadius,
        int majorSegments, int minorSegments)
    {
        int vertexCount = (majorSegments + 1) * (minorSegments + 1);
        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var uv = new Vector2[vertexCount];
        var triangles = new int[majorSegments * minorSegments * 6];

        int vertex = 0;
        for (int major = 0; major <= majorSegments; major++)
        {
            float u = major / (float)majorSegments;
            float a = u * Mathf.PI * 2f;
            Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int minor = 0; minor <= minorSegments; minor++)
            {
                float v = minor / (float)minorSegments;
                float b = v * Mathf.PI * 2f;
                Vector3 normal = radial * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                vertices[vertex] = radial * majorRadius + normal * minorRadius;
                normals[vertex] = normal;
                uv[vertex] = new Vector2(u, v);
                vertex++;
            }
        }

        int triangle = 0;
        int row = minorSegments + 1;
        for (int major = 0; major < majorSegments; major++)
        for (int minor = 0; minor < minorSegments; minor++)
        {
            int current = major * row + minor;
            int next = current + row;
            triangles[triangle++] = current;
            triangles[triangle++] = next;
            triangles[triangle++] = current + 1;
            triangles[triangle++] = current + 1;
            triangles[triangle++] = next;
            triangles[triangle++] = next + 1;
        }

        Mesh mesh = new Mesh { name = "Buca Result Torus" };
        mesh.hideFlags = HideFlags.DontSave;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    static float EaseOutBack(float t, float overshoot)
    {
        float s = t - 1f;
        return s * s * ((overshoot + 1f) * s + overshoot) + 1f;
    }

    void OnDestroy()
    {
        if (_camera != null) _camera.targetTexture = null;
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        for (int i = 0; i < _materials.Count; i++)
            if (_materials[i] != null) Destroy(_materials[i]);
        for (int i = 0; i < _meshes.Count; i++)
            if (_meshes[i] != null) Destroy(_meshes[i]);
        for (int i = 0; i < _textures.Count; i++)
            if (_textures[i] != null) Destroy(_textures[i]);
        if (_stageRoot != null) Destroy(_stageRoot);
    }
}
