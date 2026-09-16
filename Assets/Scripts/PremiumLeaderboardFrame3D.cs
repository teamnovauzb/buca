using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Real lit 3D frame behind the leaderboard UI. The rows remain regular UI
/// text for maximum readability, while every visible board, trim and row plate
/// is rendered from meshes with depth, material response and shadows.
/// </summary>
public sealed class PremiumLeaderboardFrame3D : MonoBehaviour
{
    const int PreviewLayer = 31;
    const float StageHeight = 3200f;
    const int RowCount = 10;

    readonly List<Material> _materials = new List<Material>(12);
    readonly List<Mesh> _meshes = new List<Mesh>(40);
    readonly List<Texture2D> _textures = new List<Texture2D>(4);
    readonly MeshRenderer[] _rowBackings = new MeshRenderer[RowCount];
    readonly MeshRenderer[] _rowFaces = new MeshRenderer[RowCount];

    RectTransform _viewport;
    RawImage _rawImage;
    RenderTexture _renderTexture;
    GameObject _stageRoot;
    Transform _board;
    Camera _camera;
    Material _normalBacking;
    Material _normalFace;
    Material _highlightBacking;
    Material _highlightFace;
    bool _ready;

    public bool IsReady => _ready && _viewport != null && _camera != null;

    public static PremiumLeaderboardFrame3D Ensure(Transform owner)
    {
        if (owner == null) return null;
        PremiumLeaderboardFrame3D frame = owner.GetComponent<PremiumLeaderboardFrame3D>();
        if (frame == null) frame = owner.gameObject.AddComponent<PremiumLeaderboardFrame3D>();
        frame.EnsureBuilt();
        return frame;
    }

    public void SetHighlightedRow(int rowIndex)
    {
        EnsureBuilt();
        for (int i = 0; i < RowCount; i++)
        {
            bool selected = i == rowIndex;
            if (_rowBackings[i] != null)
                _rowBackings[i].sharedMaterial = selected ? _highlightBacking : _normalBacking;
            if (_rowFaces[i] != null)
                _rowFaces[i].sharedMaterial = selected ? _highlightFace : _normalFace;
        }
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
        BuildBoard();
        _ready = true;
        SetHighlightedRow(-1);
        Hide();
    }

    void BuildViewport()
    {
        Transform existing = transform.Find("PremiumLeaderboardFrame3DViewport");
        GameObject viewportGO = existing != null
            ? existing.gameObject
            : new GameObject("PremiumLeaderboardFrame3DViewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
        if (existing == null) viewportGO.transform.SetParent(transform, false);

        _viewport = (RectTransform)viewportGO.transform;
        _viewport.anchorMin = Vector2.zero;
        _viewport.anchorMax = Vector2.one;
        _viewport.offsetMin = new Vector2(-28f, -28f);
        _viewport.offsetMax = new Vector2(28f, 28f);
        _viewport.localPosition = Vector3.zero;
        _viewport.localRotation = Quaternion.identity;
        _viewport.localScale = Vector3.one;

        _rawImage = viewportGO.GetComponent<RawImage>();
        _rawImage.color = Color.white;
        _rawImage.raycastTarget = false;
        _renderTexture = new RenderTexture(900, 1300, 24, RenderTextureFormat.ARGB32)
        {
            name = "BucaPremiumLeaderboardFrame3D",
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
        _stageRoot = new GameObject("BucaPremiumLeaderboardFrame3DStage")
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        _stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);

        GameObject cameraGO = new GameObject("PremiumLeaderboardFrontCamera", typeof(Camera))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        cameraGO.transform.SetParent(_stageRoot.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, 0f, -15f);
        cameraGO.transform.localRotation = Quaternion.identity;
        _camera = cameraGO.GetComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = 1 << PreviewLayer;
        _camera.orthographic = true;
        _camera.orthographicSize = 6.05f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 35f;
        _camera.allowHDR = true;
        _camera.allowMSAA = true;
        _camera.targetTexture = _renderTexture;
        _camera.enabled = false;

        AddPointLight("Leaderboard Warm Key", new Vector3(-4.0f, 5.2f, -4.2f),
            new Color(1f, 0.66f, 0.30f), 2.35f, 14f, true);
        AddPointLight("Leaderboard Cool Fill", new Vector3(4.1f, 2.2f, -3.6f),
            new Color(0.18f, 0.68f, 1f), 1.35f, 13f, false);
        AddPointLight("Leaderboard Gold Foot", new Vector3(0f, -5.0f, -3.2f),
            new Color(1f, 0.42f, 0.10f), 1.25f, 8f, false);

        _board = new GameObject("Straight Premium Leaderboard Board").transform;
        _board.gameObject.hideFlags = HideFlags.DontSave;
        _board.gameObject.layer = PreviewLayer;
        _board.SetParent(_stageRoot.transform, false);
        _board.localPosition = Vector3.zero;
        _board.localRotation = Quaternion.identity;
        _board.localScale = Vector3.one;
    }

    void BuildBoard()
    {
        Material walnut = CreateWoodMaterial("Leaderboard Walnut",
            new Color(0.032f, 0.009f, 0.004f, 1f),
            new Color(0.21f, 0.060f, 0.018f, 1f), 0.62f);
        Material innerWood = CreateWoodMaterial("Leaderboard Inner Walnut",
            new Color(0.018f, 0.006f, 0.003f, 1f),
            new Color(0.115f, 0.032f, 0.010f, 1f), 0.54f);
        Material gold = CreateLitMaterial("Leaderboard Champagne Gold",
            new Color(0.70f, 0.42f, 0.14f, 1f), 0.94f, 0.90f,
            new Color(0.028f, 0.009f, 0.001f, 1f));
        Material black = CreateLitMaterial("Leaderboard Black Inset",
            new Color(0.007f, 0.012f, 0.021f, 1f), 0.22f, 0.54f, Color.black);
        Material cyan = CreateLitMaterial("Leaderboard Cyan Corners",
            new Color(0.02f, 0.53f, 0.62f, 1f), 0.72f, 0.96f,
            new Color(0.006f, 0.30f, 0.42f, 1f));

        _normalBacking = CreateLitMaterial("Leaderboard Row Bronze",
            new Color(0.19f, 0.095f, 0.027f, 1f), 0.82f, 0.80f,
            new Color(0.006f, 0.002f, 0f, 1f));
        _normalFace = CreateLitMaterial("Leaderboard Row Black",
            new Color(0.012f, 0.024f, 0.040f, 1f), 0.28f, 0.60f,
            new Color(0.002f, 0.008f, 0.014f, 1f));
        _highlightBacking = gold;
        _highlightFace = CreateLitMaterial("Leaderboard Player Purple",
            new Color(0.31f, 0.045f, 0.39f, 1f), 0.58f, 0.78f,
            new Color(0.10f, 0.006f, 0.14f, 1f));

        // Four nested solids make the frame physically deep. Their front faces
        // step toward the camera, so the gold is genuine trim rather than a line.
        CreateBox("Leaderboard Walnut Back", 8.00f, 11.32f, 0.72f, 0.24f,
            new Vector3(0f, 0f, 0.48f), walnut);
        CreateBox("Leaderboard Outer Gold Bevel", 7.72f, 11.04f, 0.50f, 0.20f,
            new Vector3(0f, 0f, 0.10f), gold);
        CreateBox("Leaderboard Inner Wood Bevel", 7.46f, 10.78f, 0.42f, 0.17f,
            new Vector3(0f, 0f, -0.19f), innerWood);
        CreateBox("Leaderboard Black Face", 7.18f, 10.50f, 0.32f, 0.14f,
            new Vector3(0f, 0f, -0.47f), black);

        CreateBox("Leaderboard Title Gold Backing", 6.94f, 1.34f, 0.28f, 0.13f,
            new Vector3(0f, 4.72f, -0.68f), gold);
        CreateBox("Leaderboard Title Black Face", 6.72f, 1.12f, 0.25f, 0.11f,
            new Vector3(0f, 4.72f, -0.87f), black);
        CreateBox("Leaderboard Header Beam", 6.84f, 0.08f, 0.12f, 0.025f,
            new Vector3(0f, 3.48f, -0.83f), gold);

        for (int i = 0; i < RowCount; i++)
        {
            float y = 2.58f - i * 0.70f;
            _rowBackings[i] = CreateBox($"Leaderboard Row {i + 1} Backing",
                6.78f, 0.60f, 0.20f, 0.085f,
                new Vector3(0f, y, -0.70f), _normalBacking);
            _rowFaces[i] = CreateBox($"Leaderboard Row {i + 1} Face",
                6.58f, 0.43f, 0.18f, 0.070f,
                new Vector3(0f, y + 0.025f, -0.86f), _normalFace);
        }

        CreateBox("Leaderboard Bottom Gold Beam", 6.82f, 0.10f, 0.13f, 0.025f,
            new Vector3(0f, -4.42f, -0.82f), gold);
        CreateCornerRails("Leaderboard Upper Left", -3.25f, 5.03f, -1f, 1f, cyan);
        CreateCornerRails("Leaderboard Upper Right", 3.25f, 5.03f, 1f, 1f, cyan);
        CreateCornerRails("Leaderboard Lower Left", -3.25f, -4.92f, -1f, -1f, cyan);
        CreateCornerRails("Leaderboard Lower Right", 3.25f, -4.92f, 1f, -1f, cyan);

        CreateRivet("Leaderboard Rivet TL", new Vector3(-3.57f, 5.25f, -0.82f), gold);
        CreateRivet("Leaderboard Rivet TR", new Vector3(3.57f, 5.25f, -0.82f), gold);
        CreateRivet("Leaderboard Rivet BL", new Vector3(-3.57f, -5.25f, -0.82f), gold);
        CreateRivet("Leaderboard Rivet BR", new Vector3(3.57f, -5.25f, -0.82f), gold);
    }

    void CreateCornerRails(string name, float x, float y, float horizontalSign,
        float verticalSign, Material material)
    {
        CreateBox(name + " Vertical", 0.065f, 0.50f, 0.11f, 0.022f,
            new Vector3(x, y - verticalSign * 0.21f, -0.91f), material);
        CreateBox(name + " Horizontal", 0.50f, 0.065f, 0.11f, 0.022f,
            new Vector3(x - horizontalSign * 0.21f, y, -0.91f), material);
    }

    MeshRenderer CreateBox(string name, float width, float height, float depth,
        float bevel, Vector3 position, Material material)
    {
        Mesh mesh = BuildChamferedBoxMesh(width, height, depth, bevel);
        _meshes.Add(mesh);
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(_board, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return renderer;
    }

    void CreateRivet(string name, Vector3 position, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.hideFlags = HideFlags.DontSave;
        go.layer = PreviewLayer;
        go.transform.SetParent(_board, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * 0.18f;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    Material CreateWoodMaterial(string name, Color dark, Color light, float smoothness)
    {
        Material material = CreateLitMaterial(name, Color.white, 0.04f,
            smoothness, Color.black, false);
        Texture2D wood = CreateWoodTexture(name, dark, light);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", wood);
            material.SetTextureScale("_BaseMap", new Vector2(1.5f, 3.4f));
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", wood);
            material.SetTextureScale("_MainTex", new Vector2(1.5f, 3.4f));
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
        SetMaterialFloat(material, "_ClearCoatMask", 0.44f);
        SetMaterialFloat(material, "_ClearCoatSmoothness", 0.90f);
        if (material.HasProperty("_EmissionColor") && emission.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        if (addTexture)
        {
            Texture2D brushed = CreateBrushedTexture(name);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", brushed);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", brushed);
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
            float noise = Mathf.PerlinNoise(x * 0.029f + 2.7f, y * 0.12f + 7.9f);
            float wave = Mathf.Sin(y * 0.32f + noise * 4.8f + x * 0.016f) * 0.17f;
            float grain = Mathf.Clamp01(0.48f + wave + (noise - 0.5f) * 0.13f);
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
                + Mathf.Sin(y * 1.73f + x * 0.12f) * 0.034f
                + (Mathf.PerlinNoise(x * 0.19f, y * 0.51f) - 0.5f) * 0.04f);
            texture.SetPixel(x, y, new Color(value, value, value, 1f));
        }
        texture.Apply(false, true);
        _textures.Add(texture);
        return texture;
    }

    void AddPointLight(string name, Vector3 position, Color color, float intensity,
        float range, bool shadows)
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
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
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
        Mesh mesh = new Mesh
        {
            name = "Buca Chamfered Leaderboard Box",
            hideFlags = HideFlags.DontSave
        };
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
        for (int i = 0; i < _materials.Count; i++)
            if (_materials[i] != null) Destroy(_materials[i]);
        for (int i = 0; i < _meshes.Count; i++)
            if (_meshes[i] != null) Destroy(_meshes[i]);
        for (int i = 0; i < _textures.Count; i++)
            if (_textures[i] != null) Destroy(_textures[i]);
    }
}
