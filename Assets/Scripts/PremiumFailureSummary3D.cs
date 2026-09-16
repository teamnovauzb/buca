using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Genuine 3D failure summary used on the left of the terminal leaderboard.
/// It is deliberately straight-on so the compact golf data remains readable,
/// while layered meshes, bevels, shadows and real lights provide depth.
/// </summary>
public sealed class PremiumFailureSummary3D : MonoBehaviour
{
    const int PreviewLayer = 27;
    const float StageHeight = 2600f;

    static readonly Color Coral = new Color(1f, 0.25f, 0.34f, 1f);
    static readonly Color Gold = new Color(1f, 0.70f, 0.22f, 1f);
    static readonly Color Cyan = new Color(0.16f, 0.88f, 1f, 1f);
    static readonly Color Ice = new Color(0.84f, 0.94f, 1f, 1f);

    readonly List<Material> _materials = new List<Material>(10);
    readonly List<Mesh> _meshes = new List<Mesh>(12);
    readonly List<Texture2D> _textures = new List<Texture2D>(4);

    TMP_FontAsset _font;
    RectTransform _viewport;
    RawImage _rawImage;
    RenderTexture _renderTexture;
    GameObject _stageRoot;
    Transform _summary;
    Camera _camera;
    bool _ready;

    TextMeshPro[] _title;
    TextMeshPro[] _level;
    TextMeshPro[] _holeResult;
    TextMeshPro[] _strokes;

    public bool IsReady => _ready && _viewport != null && _camera != null;

    public static PremiumFailureSummary3D Ensure(Transform owner, TMP_FontAsset font)
    {
        if (owner == null) return null;
        PremiumFailureSummary3D summary = owner.GetComponent<PremiumFailureSummary3D>();
        if (summary == null) summary = owner.gameObject.AddComponent<PremiumFailureSummary3D>();
        summary._font = font != null ? font : TMP_Settings.defaultFontAsset;
        summary.EnsureBuilt();
        return summary;
    }

    public void SetData(string outcome, int levelNumber, int strokes, int par)
    {
        EnsureBuilt();
        if (!IsReady) return;

        int difference = strokes - Mathf.Max(1, par);
        bool hasAttempt = strokes > 0;
        string score = !hasAttempt ? "—" : FormatToPar(difference);
        string golfName = GetGolfName(strokes, difference);
        string holeLine = !hasAttempt ? "NO SCORE" : score == "E"
            ? golfName : golfName + "  " + score;
        Color resultColor = hasAttempt && difference <= 0 ? Gold : Coral;
        SetStack(_title, NormalizeOutcome(outcome), Coral);
        SetStack(_level, $"LEVEL {Mathf.Max(1, levelNumber)}", Cyan);
        SetStack(_holeResult, holeLine, resultColor);
        SetStack(_strokes, $"STROKES  {Mathf.Max(0, strokes)}   •   PAR  {Mathf.Max(1, par)}", Ice);
    }

    public void Show()
    {
        EnsureBuilt();
        if (!IsReady) return;
        _viewport.gameObject.SetActive(true);
        _viewport.SetSiblingIndex(0);
        if (_stageRoot != null) _stageRoot.SetActive(true);
        _camera.enabled = true;
    }

    public void Hide()
    {
        if (_camera != null) _camera.enabled = false;
        if (_stageRoot != null) _stageRoot.SetActive(false);
        if (_viewport != null) _viewport.gameObject.SetActive(false);
    }

    void EnsureBuilt()
    {
        if (_ready) return;
        BuildViewport();
        BuildStage();
        BuildSummary();
        _ready = true;
        Hide();
    }

    void BuildViewport()
    {
        Transform existing = transform.Find("PremiumFailureSummary3DViewport");
        GameObject viewportGO = existing != null
            ? existing.gameObject
            : new GameObject("PremiumFailureSummary3DViewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
        if (existing == null) viewportGO.transform.SetParent(transform, false);
        _viewport = (RectTransform)viewportGO.transform;
        _viewport.anchorMin = Vector2.zero;
        _viewport.anchorMax = Vector2.one;
        _viewport.offsetMin = Vector2.zero;
        _viewport.offsetMax = Vector2.zero;
        _viewport.localPosition = Vector3.zero;
        _viewport.localRotation = Quaternion.identity;
        _viewport.localScale = Vector3.one;

        _rawImage = viewportGO.GetComponent<RawImage>();
        _rawImage.color = Color.white;
        _rawImage.raycastTarget = false;
        _renderTexture = new RenderTexture(1050, 900, 24, RenderTextureFormat.ARGB32)
        {
            name = "BucaPremiumFailureSummary3D",
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
        _stageRoot = new GameObject("BucaPremiumFailureSummary3DStage")
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        _stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);

        GameObject cameraGO = new GameObject("PremiumFailureSummaryFrontCamera", typeof(Camera))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        cameraGO.transform.SetParent(_stageRoot.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, 0f, -12f);
        cameraGO.transform.localRotation = Quaternion.identity;
        _camera = cameraGO.GetComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = 1 << PreviewLayer;
        _camera.orthographic = true;
        _camera.orthographicSize = 3.10f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 30f;
        _camera.allowHDR = true;
        _camera.allowMSAA = true;
        _camera.targetTexture = _renderTexture;
        _camera.enabled = false;

        AddDirectionalLight("Warm Summary Key", new Color(1f, 0.74f, 0.46f),
            1.30f, Quaternion.Euler(30f, -36f, 0f), true);
        AddDirectionalLight("Cool Summary Fill", new Color(0.20f, 0.70f, 1f),
            0.42f, Quaternion.Euler(18f, 140f, 0f), false);
        AddPointLight("Summary Gold Rim", new Vector3(-3.0f, 2.7f, -2.6f),
            new Color(1f, 0.52f, 0.18f), 1.7f, 5.5f);
        AddPointLight("Summary Cyan Rim", new Vector3(3.0f, -1.6f, -2.4f),
            new Color(0.04f, 0.72f, 1f), 1.4f, 5.5f);

        _summary = new GameObject("Straight Premium Failure Summary").transform;
        _summary.gameObject.hideFlags = HideFlags.DontSave;
        _summary.gameObject.layer = PreviewLayer;
        _summary.SetParent(_stageRoot.transform, false);
        _summary.localPosition = Vector3.zero;
        _summary.localRotation = Quaternion.identity;
        _summary.localScale = Vector3.one;
    }

    void BuildSummary()
    {
        Material walnut = CreateWoodMaterial("Summary Walnut",
            new Color(0.038f, 0.012f, 0.007f, 1f),
            new Color(0.19f, 0.064f, 0.024f, 1f), 0.58f);
        Material gold = CreateLitMaterial("Summary Champagne Gold",
            new Color(0.66f, 0.40f, 0.16f, 1f), 0.92f, 0.86f,
            new Color(0.028f, 0.010f, 0.002f, 1f));
        Material black = CreateLitMaterial("Summary Matte Black",
            new Color(0.010f, 0.014f, 0.020f, 1f), 0.20f, 0.48f, Color.black);
        Material deepBlack = CreateLitMaterial("Summary Raised Black",
            new Color(0.006f, 0.010f, 0.015f, 1f), 0.30f, 0.58f, Color.black);
        Material cyan = CreateLitMaterial("Summary Cyan Rail",
            new Color(0.02f, 0.50f, 0.60f, 1f), 0.72f, 0.94f,
            new Color(0.008f, 0.25f, 0.35f, 1f));
        Material goldRail = CreateLitMaterial("Summary Gold Rail",
            new Color(0.74f, 0.43f, 0.13f, 1f), 0.94f, 0.90f,
            new Color(0.045f, 0.013f, 0.001f, 1f));

        CreateBox("Summary Walnut Back", 7.22f, 5.65f, 0.62f, 0.20f,
            new Vector3(0f, 0f, 0.42f), walnut);
        CreateBox("Summary Gold Trim", 6.90f, 5.33f, 0.43f, 0.17f,
            new Vector3(0f, 0f, 0.05f), gold);
        CreateBox("Summary Black Inset", 6.62f, 5.05f, 0.32f, 0.14f,
            new Vector3(0f, 0f, -0.25f), black);

        // Selected option B: two substantial physical decks with a single
        // gold separator.  Each black face has its own raised gold backing.
        CreateBox("Upper Deck Gold Backing", 6.40f, 1.92f, 0.34f, 0.15f,
            new Vector3(0f, 1.38f, -0.40f), gold);
        CreateBox("Upper Deck Black Face", 6.18f, 1.70f, 0.34f, 0.14f,
            new Vector3(0f, 1.38f, -0.61f), deepBlack);
        CreateBox("Lower Deck Gold Backing", 6.40f, 2.58f, 0.34f, 0.15f,
            new Vector3(0f, -0.82f, -0.40f), gold);
        CreateBox("Lower Deck Black Face", 6.18f, 2.36f, 0.34f, 0.14f,
            new Vector3(0f, -0.82f, -0.61f), deepBlack);
        CreateBox("Solid Gold Separator Beam", 6.46f, 0.24f, 0.24f, 0.06f,
            new Vector3(0f, 0.25f, -0.66f), goldRail);

        // Straight cube rails reproduce option B's restrained cyan L corners.
        CreateCornerRails("Upper Left", -2.83f, 2.08f, -1f, 1f, cyan);
        CreateCornerRails("Upper Right", 2.83f, 0.68f, 1f, -1f, cyan);
        CreateCornerRails("Lower Left", -2.83f, -1.83f, -1f, -1f, cyan);
        CreateCornerRails("Lower Right", 2.83f, -1.83f, 1f, -1f, cyan);

        _title = CreateTextStack("Outcome", "TIME IS OVER",
            new Vector3(0f, 1.68f, -0.84f), 5.45f, 0.62f, Coral, 3.20f);
        _level = CreateTextStack("Level", "LEVEL 1",
            new Vector3(0f, 1.10f, -0.84f), 5.45f, 0.38f, Cyan, 1.76f);
        _holeResult = CreateTextStack("Hole Result", "BOGEY  +1",
            new Vector3(0f, -0.45f, -0.84f), 5.45f, 0.72f, Coral, 3.35f);
        _strokes = CreateTextStack("Strokes", "STROKES  3   •   PAR  2",
            new Vector3(0f, -1.18f, -0.84f), 5.45f, 0.40f, Ice, 1.60f);
    }

    void CreateCornerRails(string name, float x, float y, float horizontalSign,
        float verticalSign, Material material)
    {
        CreateBox(name + " Vertical", 0.065f, 0.48f, 0.12f, 0.022f,
            new Vector3(x, y - verticalSign * 0.205f, -0.84f), material);
        CreateBox(name + " Horizontal", 0.48f, 0.065f, 0.12f, 0.022f,
            new Vector3(x - horizontalSign * 0.205f, y, -0.84f), material);
    }

    GameObject CreateBox(string name, float width, float height, float depth,
        float bevel, Vector3 position, Material material)
    {
        Mesh mesh = BuildChamferedBoxMesh(width, height, depth, bevel);
        _meshes.Add(mesh);
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(_summary, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return go;
    }

    TextMeshPro[] CreateTextStack(string name, string content, Vector3 position,
        float width, float height, Color color, float fontSize)
    {
        TextMeshPro[] layers = new TextMeshPro[3];
        layers[0] = CreateWorldText(name + " Deep Shadow", content,
            position + new Vector3(0.055f, -0.050f, 0.080f), width, height,
            new Color(0f, 0f, 0f, 0.92f), fontSize, 0);
        layers[1] = CreateWorldText(name + " Depth", content,
            position + new Vector3(0.027f, -0.024f, 0.040f), width, height,
            new Color(0.12f, 0.055f, 0.025f, 1f), fontSize, 1);
        layers[2] = CreateWorldText(name, content, position, width, height,
            color, fontSize, 2);
        return layers;
    }

    TextMeshPro CreateWorldText(string name, string content, Vector3 position,
        float width, float height, Color color, float fontSize, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(TextMeshPro))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(_summary, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (_font != null) tmp.font = _font;
        tmp.text = content;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.5f;
        tmp.fontSizeMax = fontSize;
        tmp.characterSpacing = 0.8f;
        tmp.rectTransform.sizeDelta = new Vector2(width, height);
        tmp.renderer.sortingOrder = sortingOrder;
        Material textMaterial = tmp.fontMaterial;
        SetMaterialFloat(textMaterial, "_FaceDilate", 0.07f);
        SetMaterialFloat(textMaterial, "_OutlineWidth", sortingOrder == 2 ? 0.042f : 0f);
        SetMaterialFloat(textMaterial, "_Bevel", sortingOrder == 2 ? 0.34f : 0f);
        SetMaterialFloat(textMaterial, "_BevelWidth", sortingOrder == 2 ? 0.15f : 0f);
        if (textMaterial.HasProperty("_OutlineColor"))
            textMaterial.SetColor("_OutlineColor", new Color(0.01f, 0.015f, 0.02f, 0.92f));
        return tmp;
    }

    static void SetStack(TextMeshPro[] stack, string value, Color frontColor)
    {
        if (stack == null) return;
        for (int i = 0; i < stack.Length; i++)
        {
            if (stack[i] == null) continue;
            stack[i].text = value;
            if (i == stack.Length - 1) stack[i].color = frontColor;
            stack[i].ForceMeshUpdate();
        }
    }

    static string NormalizeOutcome(string outcome)
    {
        if (!string.IsNullOrWhiteSpace(outcome)
            && outcome.ToUpperInvariant().Contains("TIME")) return "TIME IS OVER";
        return "YOU LOST";
    }

    static string GetGolfName(int strokes, int difference)
    {
        if (strokes <= 0) return "NO SCORE";
        if (strokes == 1) return "HOLE IN ONE";
        if (difference <= -2) return "EAGLE";
        if (difference == -1) return "BIRDIE";
        if (difference == 0) return "PAR";
        if (difference == 1) return "BOGEY";
        if (difference == 2) return "DOUBLE BOGEY";
        return difference + " OVER PAR";
    }

    static string FormatToPar(int value)
    {
        if (value == 0) return "E";
        return value > 0 ? "+" + value : value.ToString();
    }

    Material CreateWoodMaterial(string name, Color dark, Color light, float smoothness)
    {
        Material material = CreateLitMaterial(name, Color.white, 0.04f, smoothness, Color.black, false);
        Texture2D wood = CreateWoodTexture(name, dark, light);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", wood);
            material.SetTextureScale("_BaseMap", new Vector2(1.6f, 3.0f));
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", wood);
            material.SetTextureScale("_MainTex", new Vector2(1.6f, 3.0f));
        }
        return material;
    }

    Material CreateLitMaterial(string name, Color color, float metallic,
        float smoothness, Color emission, bool addTexture = true)
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
        SetMaterialFloat(material, "_ClearCoatMask", 0.48f);
        SetMaterialFloat(material, "_ClearCoatSmoothness", 0.90f);
        if (material.HasProperty("_EmissionColor") && emission.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        if (addTexture)
        {
            Texture2D texture = CreateBrushedTexture(name);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }
        return material;
    }

    Texture2D CreateWoodTexture(string name, Color dark, Color light)
    {
        const int width = 128;
        const int height = 64;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
            name = name + " Grain",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float noise = Mathf.PerlinNoise(x * 0.028f + 5.2f, y * 0.12f + 9.1f);
            float wave = Mathf.Sin(y * 0.31f + noise * 4.4f + x * 0.018f) * 0.17f;
            float grain = Mathf.Clamp01(0.48f + wave + (noise - 0.5f) * 0.12f);
            texture.SetPixel(x, y, Color.Lerp(dark, light, grain));
        }
        texture.Apply(false, true);
        _textures.Add(texture);
        return texture;
    }

    Texture2D CreateBrushedTexture(string name)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false, true)
        {
            name = name + " Brushed",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float value = Mathf.Clamp01(0.90f
                + Mathf.Sin(y * 1.71f + x * 0.13f) * 0.034f
                + (Mathf.PerlinNoise(x * 0.21f, y * 0.53f) - 0.5f) * 0.04f);
            texture.SetPixel(x, y, new Color(value, value, value, 1f));
        }
        texture.Apply(false, true);
        _textures.Add(texture);
        return texture;
    }

    void AddDirectionalLight(string name, Color color, float intensity,
        Quaternion rotation, bool shadows)
    {
        GameObject go = new GameObject(name, typeof(Light))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(_stageRoot.transform, false);
        go.transform.localPosition = rotation * new Vector3(0f, 0f, -6f);
        Light light = go.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity * 2.4f;
        light.range = 14f;
        light.cullingMask = 1 << PreviewLayer;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
    }

    void AddPointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name, typeof(Light))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(_stageRoot.transform, false);
        go.transform.localPosition = position;
        Light light = go.GetComponent<Light>();
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
        Mesh mesh = new Mesh { name = "Buca Chamfered Failure Summary Box", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddPolygonFace(Vector2[] outline, float z, Vector3 normal,
        bool reverse, List<Vector3> vertices, List<Vector3> normals,
        List<Vector2> uv, List<int> triangles)
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

    static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

    void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_stageRoot != null) Destroy(_stageRoot);
        for (int i = 0; i < _materials.Count; i++) if (_materials[i] != null) Destroy(_materials[i]);
        for (int i = 0; i < _meshes.Count; i++) if (_meshes[i] != null) Destroy(_meshes[i]);
        for (int i = 0; i < _textures.Count; i++) if (_textures[i] != null) Destroy(_textures[i]);
    }
}
