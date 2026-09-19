#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Option B level picker: a real miniature Buca table rendered by a dedicated
/// perspective camera. Existing UI Buttons become transparent hit areas over
/// the physical level pucks, so storage, unlocking, mouse and arcade controls
/// keep using LevelSelectController as their single source of truth.
/// </summary>
[DisallowMultipleComponent]
public sealed class PremiumLevelMap3D : MonoBehaviour
{
    const int MapLayer = 27;
    const float StageHeight = 4200f;
    const int Columns = 6;

    sealed class NodeVisual
    {
        public Transform root;
        public Vector3 basePosition;
        public MeshRenderer halo;
        public MeshRenderer foot;
        public MeshRenderer body;
        public MeshRenderer upperStep;
        public MeshRenderer ring;
        public MeshRenderer innerRing;
        public MeshRenderer face;
        public TextMeshPro number;
        public readonly MeshRenderer[] stars = new MeshRenderer[3];
        public int earnedStars;
        public bool completed;
        public bool locked;
    }

    readonly List<Material> _materials = new List<Material>(16);
    readonly List<Texture2D> _textures = new List<Texture2D>(4);
    readonly List<Mesh> _meshes = new List<Mesh>(4);
    readonly List<MeshRenderer> _routeSegments = new List<MeshRenderer>(32);
    readonly List<Transform> _animatedProps = new List<Transform>(8);
    readonly List<NodeVisual> _nodes = new List<NodeVisual>(30);

    LevelSelectController _owner;
    RectTransform _card;
    RectTransform _viewport;
    RectTransform _hitAreas;
    RawImage _rawImage;
    RenderTexture _renderTexture;
    GameObject _stageRoot;
    Transform _mapRoot;
    Camera _camera;
    Light _selectionLight;
    Transform _routeSpark;
    TMP_FontAsset _font;
    Mesh _starMesh;

    Material _deepWood;
    Material _warmWood;
    Material _bronze;
    Material _gold;
    Material _goldDark;
    Material _goldHighlight;
    Material _silver;
    Material _sapphireDepth;
    Material _sapphireBody;
    Material _sapphireEdge;
    Material _darkFace;
    Material _glassFace;
    Material _glassHighlight;
    Material _lockedMetal;
    Material _cyan;
    Material _cyanDim;
    Material _purple;
    Material _starGold;
    Material _starDim;
    Mesh _tokenTorusMesh;

    int _selectedIndex;
    int _highestUnlocked;
    bool _ready;
    bool _visible;

    public bool IsReady => _ready && _camera != null && _viewport != null;

    public static PremiumLevelMap3D Ensure(LevelSelectController owner)
    {
        if (owner == null || owner.panelCard == null) return null;
        PremiumLevelMap3D map = owner.GetComponent<PremiumLevelMap3D>();
        if (map == null) map = owner.gameObject.AddComponent<PremiumLevelMap3D>();
        map._owner = owner;
        map._card = owner.panelCard;
        map._font = TMP_Settings.defaultFontAsset;
        map.BuildIfNeeded();
        return map;
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
        if (_viewport != null) _viewport.gameObject.SetActive(visible);
        if (_stageRoot != null) _stageRoot.SetActive(visible);
        if (_camera != null) _camera.enabled = visible;
    }

    public void SetSelected(int index)
    {
        if (!_ready || _nodes.Count == 0) return;
        _selectedIndex = Mathf.Clamp(index, 0, _nodes.Count - 1);
        ApplyAllNodeMaterials();
        PositionSelectionLight();
    }

    public void SetLevelState(int index, int stars, bool completed, bool locked)
    {
        if (!_ready || index < 0 || index >= _nodes.Count) return;
        NodeVisual node = _nodes[index];
        node.earnedStars = Mathf.Clamp(stars, 0, 3);
        node.completed = completed;
        node.locked = locked;
        if (!locked) _highestUnlocked = Mathf.Max(_highestUnlocked, index);
        ApplyNodeMaterials(index);
        ApplyRouteMaterials();
    }

    /// <summary>Returns the physically adjacent level on the winding route.</summary>
    public int Navigate(int current, int horizontal, int vertical, int count)
    {
        if (count <= 0) return 0;
        current = Mathf.Clamp(current, 0, count - 1);
        int row = current / Columns;
        int logicalColumn = current % Columns;
        int visualColumn = (row & 1) == 0
            ? logicalColumn
            : Columns - 1 - logicalColumn;
        int rowCount = Mathf.CeilToInt(count / (float)Columns);

        if (horizontal != 0)
        {
            int nextVisual = Mathf.Clamp(visualColumn + horizontal, 0, Columns - 1);
            int nextLogical = (row & 1) == 0
                ? nextVisual
                : Columns - 1 - nextVisual;
            int candidate = row * Columns + nextLogical;
            return candidate < count ? candidate : current;
        }

        if (vertical != 0)
        {
            int nextRow = Mathf.Clamp(row + vertical, 0, rowCount - 1);
            int nextLogical = (nextRow & 1) == 0
                ? visualColumn
                : Columns - 1 - visualColumn;
            int candidate = nextRow * Columns + nextLogical;
            if (candidate >= count) candidate = count - 1;
            return candidate;
        }

        return current;
    }

    void BuildIfNeeded()
    {
        if (_ready || _owner == null || _card == null || _owner.levels == null ||
            _owner.levels.Count == 0) return;

        PrepareExistingUi();
        BuildViewport();
        BuildMaterials();
        BuildStage();
        BuildTable();
        BuildRouteAndNodes(Mathf.Min(30, _owner.levels.Count));
        BuildDecorativeObstacles();
        AttachExistingButtons();
        _ready = true;
        SetVisible(false);
        SetSelected(0);
    }

    void PrepareExistingUi()
    {
        _card.sizeDelta = new Vector2(1680f, 960f);
        Image cardImage = _card.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = new Color(0.01f, 0.008f, 0.025f, 0.12f);
            cardImage.raycastTarget = false;
        }

        if (_owner.cardFitter != null)
        {
            _owner.cardFitter.designSize = new Vector2(1680f, 960f);
            _owner.cardFitter.Apply();
        }

        HideChild("Title");
        HideChild("ProgressTrack");
        HideChild("Grid");

        RectTransform countdown = FindRect("IdleCountdownPill");
        if (countdown != null)
        {
            countdown.anchorMin = countdown.anchorMax = countdown.pivot = new Vector2(0f, 1f);
            countdown.anchoredPosition = new Vector2(34f, -25f);
            countdown.sizeDelta = new Vector2(240f, 68f);
            Image bg = countdown.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.018f, 0.035f, 0.065f, 0.92f);
        }

        RectTransform footer = FindRect("FooterStats");
        if (footer != null)
        {
            footer.anchorMin = footer.anchorMax = footer.pivot = new Vector2(0.5f, 0f);
            footer.anchoredPosition = new Vector2(0f, 78f);
            footer.sizeDelta = new Vector2(1160f, 48f);
        }

        RectTransform back = FindRect("BackButton");
        if (back != null)
        {
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0.5f, 0f);
            back.anchoredPosition = new Vector2(0f, 18f);
            back.sizeDelta = new Vector2(260f, 56f);
        }
    }

    void BuildViewport()
    {
        GameObject viewportGo = new GameObject("PremiumLevelMap3DViewport",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        viewportGo.transform.SetParent(_card, false);
        viewportGo.transform.SetSiblingIndex(0);
        _viewport = viewportGo.GetComponent<RectTransform>();
        _viewport.anchorMin = _viewport.anchorMax = _viewport.pivot = new Vector2(0.5f, 0.5f);
        _viewport.sizeDelta = new Vector2(1650f, 930f);
        _viewport.anchoredPosition = Vector2.zero;
        _rawImage = viewportGo.GetComponent<RawImage>();
        _rawImage.color = Color.white;
        _rawImage.raycastTarget = false;
        _rawImage.maskable = false;

        _renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32)
        {
            name = "Buca Premium Level Map 3D",
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        _renderTexture.Create();
        _rawImage.texture = _renderTexture;

        GameObject hitsGo = new GameObject("LevelNodeHitAreas", typeof(RectTransform));
        hitsGo.transform.SetParent(_viewport, false);
        _hitAreas = hitsGo.GetComponent<RectTransform>();
        Stretch(_hitAreas);
    }

    void BuildMaterials()
    {
        _deepWood = CreateWoodMaterial("Map Deep Walnut",
            new Color(0.010f, 0.006f, 0.008f), new Color(0.070f, 0.028f, 0.018f), 0.68f);
        _warmWood = CreateWoodMaterial("Map Warm Walnut",
            new Color(0.030f, 0.012f, 0.010f), new Color(0.145f, 0.060f, 0.028f), 0.62f);
        _bronze = CreateMaterial("Map Bronze", new Color(0.28f, 0.10f, 0.025f),
            0.90f, 0.70f, new Color(0.012f, 0.002f, 0f));
        _gold = CreateMaterial("Map Polished Gold", new Color(0.88f, 0.47f, 0.075f),
            0.98f, 0.94f, new Color(0.075f, 0.018f, 0.001f));
        _goldDark = CreateMaterial("Map Gold Shadow", new Color(0.27f, 0.095f, 0.012f),
            0.96f, 0.78f, new Color(0.006f, 0.001f, 0f));
        _goldHighlight = CreateMaterial("Map Gold Highlight", new Color(1f, 0.72f, 0.20f),
            0.98f, 0.97f, new Color(0.12f, 0.035f, 0.002f));
        _silver = CreateMaterial("Map Silver", new Color(0.40f, 0.52f, 0.62f),
            0.92f, 0.86f, new Color(0.003f, 0.008f, 0.012f));
        // Premium token body: deep sapphire metal replaces the previous solid
        // orange/gold stack. Gold is now reserved for the bevels, so the real
        // depth of every button is much easier to read under the map lights.
        _sapphireDepth = CreateMaterial("Map Sapphire Shadow", new Color(0.008f, 0.020f, 0.060f),
            0.94f, 0.82f, new Color(0.001f, 0.004f, 0.014f));
        _sapphireBody = CreateMaterial("Map Polished Sapphire", new Color(0.020f, 0.125f, 0.245f),
            0.94f, 0.96f, new Color(0.002f, 0.020f, 0.050f));
        _sapphireEdge = CreateMaterial("Map Sapphire Highlight", new Color(0.055f, 0.285f, 0.440f),
            0.90f, 0.98f, new Color(0.004f, 0.055f, 0.090f));
        _darkFace = CreateMaterial("Map Puck Face", new Color(0.025f, 0.028f, 0.050f),
            0.68f, 0.82f, Color.black);
        _glassFace = CreateMaterial("Map Smoked Glass Face", new Color(0.055f, 0.040f, 0.030f),
            0.72f, 0.98f, new Color(0.004f, 0.002f, 0.001f));
        _glassHighlight = CreateMaterial("Map Glass Reflection", new Color(1f, 0.88f, 0.58f),
            0.70f, 1.0f, new Color(0.09f, 0.055f, 0.014f));
        _lockedMetal = CreateMaterial("Map Locked Puck", new Color(0.055f, 0.065f, 0.088f),
            0.76f, 0.50f, Color.black);
        _cyan = CreateMaterial("Map Cyan Glass", new Color(0.06f, 0.72f, 0.92f),
            0.55f, 0.95f, new Color(0.025f, 0.52f, 0.78f));
        _cyanDim = CreateMaterial("Map Dim Route", new Color(0.025f, 0.11f, 0.16f),
            0.44f, 0.84f, new Color(0.002f, 0.018f, 0.028f));
        _purple = CreateMaterial("Map Purple Bumpers", new Color(0.19f, 0.055f, 0.31f),
            0.54f, 0.86f, new Color(0.012f, 0.002f, 0.022f));
        _starGold = CreateMaterial("Map Earned Star", new Color(1f, 0.66f, 0.09f),
            0.96f, 0.94f, new Color(0.18f, 0.05f, 0.001f));
        _starDim = CreateMaterial("Map Empty Star", new Color(0.24f, 0.34f, 0.46f),
            0.92f, 0.90f, new Color(0.004f, 0.014f, 0.026f));
        _starMesh = BuildStarPrismMesh();
        _meshes.Add(_starMesh);
        _tokenTorusMesh = BuildTorusMesh("Buca Token Gold Bevel", 0.50f, 0.095f, 48, 12);
        _meshes.Add(_tokenTorusMesh);
    }

    void BuildStage()
    {
        _stageRoot = new GameObject("Buca Premium Level Map 3D Stage")
        {
            hideFlags = HideFlags.DontSave,
            layer = MapLayer
        };
        _stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);

        _mapRoot = new GameObject("Option B Miniature Buca Table").transform;
        _mapRoot.gameObject.hideFlags = HideFlags.DontSave;
        _mapRoot.gameObject.layer = MapLayer;
        _mapRoot.SetParent(_stageRoot.transform, false);

        GameObject cameraGo = new GameObject("Premium Level Map Camera", typeof(Camera))
        {
            hideFlags = HideFlags.DontSave,
            layer = MapLayer
        };
        cameraGo.transform.SetParent(_stageRoot.transform, false);
        cameraGo.transform.localPosition = new Vector3(0f, 14.2f, -12.2f);
        cameraGo.transform.LookAt(_stageRoot.transform.position + new Vector3(0f, 0.05f, -0.10f));
        _camera = cameraGo.GetComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = 1 << MapLayer;
        _camera.fieldOfView = 38f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 45f;
        _camera.allowHDR = true;
        _camera.allowMSAA = true;
        _camera.targetTexture = _renderTexture;

        AddDirectionalLight("Map Warm Key", new Color(1f, 0.76f, 0.48f), 1.42f,
            Quaternion.Euler(42f, -36f, 0f));
        AddDirectionalLight("Map Cool Fill", new Color(0.14f, 0.60f, 1f), 0.46f,
            Quaternion.Euler(26f, 142f, 0f));
        AddPointLight("Map Token Gold Sweep", new Vector3(-1.8f, 4.4f, -2.4f),
            new Color(1f, 0.61f, 0.22f), 2.25f, 11.0f, true);
        AddPointLight("Map Cyan Rim", new Vector3(-5.8f, 2.2f, -4.0f),
            new Color(0.04f, 0.72f, 1f), 2.0f, 8.5f, false);
        AddPointLight("Map Gold Rim", new Vector3(6.0f, 2.6f, 1.8f),
            new Color(1f, 0.46f, 0.10f), 1.75f, 8.0f, false);

        GameObject selectLightGo = new GameObject("Selected Level Spotlight", typeof(Light));
        selectLightGo.hideFlags = HideFlags.DontSave;
        selectLightGo.layer = MapLayer;
        selectLightGo.transform.SetParent(_mapRoot, false);
        _selectionLight = selectLightGo.GetComponent<Light>();
        _selectionLight.type = LightType.Point;
        _selectionLight.color = new Color(0.10f, 0.78f, 1f);
        _selectionLight.intensity = 2.4f;
        _selectionLight.range = 3.4f;
        _selectionLight.cullingMask = 1 << MapLayer;
        _selectionLight.shadows = LightShadows.None;
    }

    void BuildTable()
    {
        // A four-layer rounded chassis replaces the old flat orange rectangle.
        // Each visible lip is real geometry, so the table reads clearly in 3D.
        CreateRoundedSlab("Floating Walnut Chassis", _deepWood,
            new Vector3(0f, -0.58f, -0.10f), 20.80f, 13.20f, 0.60f, 1.18f);
        CreateRoundedSlab("Inset Bronze Chassis Lip", _bronze,
            new Vector3(0f, -0.285f, -0.10f), 20.30f, 12.70f, 0.17f, 1.06f);
        CreateRoundedSlab("Dark Recess", _darkFace,
            new Vector3(0f, -0.190f, -0.10f), 19.86f, 12.24f, 0.14f, 0.94f);
        CreateRoundedSlab("Premium Dark Walnut Playfield", _warmWood,
            new Vector3(0f, -0.080f, -0.10f), 19.20f, 11.60f, 0.18f, 0.82f);

        // Thick walnut rails, capped by bronze and a restrained inner cyan line.
        CreatePrimitive("Top Walnut Rail", PrimitiveType.Cube, _mapRoot, _deepWood,
            new Vector3(0f, 0.28f, 5.84f), new Vector3(19.30f, 0.52f, 0.48f));
        CreatePrimitive("Bottom Walnut Rail", PrimitiveType.Cube, _mapRoot, _deepWood,
            new Vector3(0f, 0.28f, -6.04f), new Vector3(19.30f, 0.52f, 0.48f));
        CreatePrimitive("Left Walnut Rail", PrimitiveType.Cube, _mapRoot, _deepWood,
            new Vector3(-9.65f, 0.28f, -0.10f), new Vector3(0.48f, 0.52f, 11.42f));
        CreatePrimitive("Right Walnut Rail", PrimitiveType.Cube, _mapRoot, _deepWood,
            new Vector3(9.65f, 0.28f, -0.10f), new Vector3(0.48f, 0.52f, 11.42f));

        CreatePrimitive("Top Gold Rail Inlay", PrimitiveType.Cube, _mapRoot, _goldDark,
            new Vector3(0f, 0.57f, 5.61f), new Vector3(18.62f, 0.060f, 0.085f));
        CreatePrimitive("Bottom Gold Rail Inlay", PrimitiveType.Cube, _mapRoot, _goldDark,
            new Vector3(0f, 0.57f, -5.81f), new Vector3(18.62f, 0.060f, 0.085f));
        CreatePrimitive("Left Gold Rail Inlay", PrimitiveType.Cube, _mapRoot, _goldDark,
            new Vector3(-9.42f, 0.57f, -0.10f), new Vector3(0.085f, 0.060f, 10.74f));
        CreatePrimitive("Right Gold Rail Inlay", PrimitiveType.Cube, _mapRoot, _goldDark,
            new Vector3(9.42f, 0.57f, -0.10f), new Vector3(0.085f, 0.060f, 10.74f));

        CreatePrimitive("Top Cyan Inner Rail", PrimitiveType.Cube, _mapRoot, _cyanDim,
            new Vector3(0f, 0.59f, 5.44f), new Vector3(18.12f, 0.046f, 0.046f));
        CreatePrimitive("Bottom Cyan Inner Rail", PrimitiveType.Cube, _mapRoot, _cyanDim,
            new Vector3(0f, 0.59f, -5.64f), new Vector3(18.12f, 0.046f, 0.046f));
        CreatePrimitive("Left Cyan Inner Rail", PrimitiveType.Cube, _mapRoot, _cyanDim,
            new Vector3(-9.25f, 0.59f, -0.10f), new Vector3(0.046f, 0.046f, 10.35f));
        CreatePrimitive("Right Cyan Inner Rail", PrimitiveType.Cube, _mapRoot, _cyanDim,
            new Vector3(9.25f, 0.59f, -0.10f), new Vector3(0.046f, 0.046f, 10.35f));

        // Rounded corner towers connect the rails and emphasize their depth.
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                CreatePrimitive("Rounded Walnut Corner", PrimitiveType.Cylinder, _mapRoot, _deepWood,
                    new Vector3(x * 9.54f, 0.28f, -0.10f + z * 5.84f),
                    new Vector3(0.66f, 0.26f, 0.66f));
                CreatePrimitive("Gold Corner Cap", PrimitiveType.Cylinder, _mapRoot, _gold,
                    new Vector3(x * 9.54f, 0.59f, -0.10f + z * 5.84f),
                    new Vector3(0.30f, 0.040f, 0.30f));
            }
        }

        // Physical title plaque is separated from the first row.
        Transform titlePlaque = new GameObject("3D Title Plaque").transform;
        titlePlaque.gameObject.hideFlags = HideFlags.DontSave;
        titlePlaque.gameObject.layer = MapLayer;
        titlePlaque.SetParent(_mapRoot, false);
        titlePlaque.localPosition = new Vector3(0f, 0.04f, 5.02f);
        CreatePrimitive("Title Walnut", PrimitiveType.Cube, titlePlaque, _deepWood,
            Vector3.zero, new Vector3(8.50f, 0.28f, 0.82f));
        CreatePrimitive("Title Gold Lip", PrimitiveType.Cube, titlePlaque, _gold,
            new Vector3(0f, 0.18f, -0.35f), new Vector3(7.45f, 0.042f, 0.065f));
        CreateFloorText("SELECT LEVEL", titlePlaque, new Vector3(0f, 0.18f, 0.02f),
            7.4f, 0.88f, new Color(0.94f, 0.97f, 1f), 4.0f, 30);

        CreatePrimitive("Footer Shelf", PrimitiveType.Cube, _mapRoot, _deepWood,
            new Vector3(0f, 0.08f, -5.18f), new Vector3(13.60f, 0.27f, 0.70f));
        CreatePrimitive("Footer Cyan Lip", PrimitiveType.Cube, _mapRoot, _cyanDim,
            new Vector3(0f, 0.25f, -4.86f), new Vector3(12.4f, 0.040f, 0.050f));
    }

    void BuildRouteAndNodes(int count)
    {
        Vector3[] positions = new Vector3[count];
        float[] rowOffsets = { 0f, 0.12f, -0.10f, 0.10f, 0f };
        for (int i = 0; i < count; i++)
        {
            int row = i / Columns;
            int logicalColumn = i % Columns;
            int visualColumn = (row & 1) == 0
                ? logicalColumn
                : Columns - 1 - logicalColumn;
            float x = -7.45f + visualColumn * 2.98f + rowOffsets[Mathf.Min(row, 4)];
            float z = 3.75f - row * 1.87f;
            positions[i] = new Vector3(x, 0f, z);
        }

        for (int i = 0; i + 1 < count; i++)
        {
            // Three nested cylinders form a raised premium rail. The old
            // single cyan tube looked like a flat UI line and fought with the
            // sapphire/gold tokens. Only the slim gold core changes with
            // progress; the dark base and bronze casing keep their 3D depth.
            Vector3 start = positions[i];
            Vector3 end = positions[i + 1];
            CreateTubeBetween($"Route {i + 1}-{i + 2} Sapphire Base",
                start + Vector3.up * 0.055f,
                end + Vector3.up * 0.055f,
                0.090f, _sapphireDepth);
            CreateTubeBetween($"Route {i + 1}-{i + 2} Bronze Casing",
                start + Vector3.up * 0.105f,
                end + Vector3.up * 0.105f,
                0.060f, _bronze);
            MeshRenderer core = CreateTubeBetween($"Route {i + 1}-{i + 2} Gold Core",
                start + Vector3.up * 0.150f,
                end + Vector3.up * 0.150f,
                0.028f, _goldDark);
            _routeSegments.Add(core);
        }

        for (int i = 0; i < count; i++)
            _nodes.Add(BuildNode(i, positions[i]));

        _routeSpark = CreatePrimitive("Route Gold Spark", PrimitiveType.Sphere,
            _mapRoot, _goldHighlight, positions[0] + Vector3.up * 0.24f, Vector3.one * 0.090f);
    }

    NodeVisual BuildNode(int index, Vector3 position)
    {
        var node = new NodeVisual();
        node.root = new GameObject($"Level {index + 1:00} 3D Puck").transform;
        node.root.gameObject.hideFlags = HideFlags.DontSave;
        node.root.gameObject.layer = MapLayer;
        node.root.SetParent(_mapRoot, false);
        node.root.localPosition = position;
        node.basePosition = position;

        // One shared premium token silhouette for every level. Selection only
        // adds a restrained cyan pool of light; it never turns the face cyan.
        node.halo = CreatePrimitive("Cyan Selection Halo", PrimitiveType.Cylinder,
            node.root, _cyan, new Vector3(0f, 0.035f, 0f),
            new Vector3(1.58f, 0.020f, 1.58f)).GetComponent<MeshRenderer>();
        node.halo.enabled = false;

        CreatePrimitive("Token Soft Shadow", PrimitiveType.Cylinder, node.root, _darkFace,
            new Vector3(0f, 0.10f, 0.055f), new Vector3(1.38f, 0.060f, 1.38f));
        node.foot = CreatePrimitive("Token State Shadow Foot", PrimitiveType.Cylinder,
            node.root, _sapphireDepth, new Vector3(0f, 0.21f, 0f),
            new Vector3(1.32f, 0.130f, 1.32f)).GetComponent<MeshRenderer>();
        node.body = CreatePrimitive("Token State Body", PrimitiveType.Cylinder,
            node.root, _sapphireBody, new Vector3(0f, 0.40f, 0f),
            new Vector3(1.27f, 0.125f, 1.27f)).GetComponent<MeshRenderer>();
        node.upperStep = CreatePrimitive("Token State Upper Step", PrimitiveType.Cylinder,
            node.root, _sapphireEdge, new Vector3(0f, 0.545f, 0f),
            new Vector3(1.18f, 0.060f, 1.18f)).GetComponent<MeshRenderer>();

        GameObject outerBevel = CreateMeshObject("Thick Polished Gold Rim", _tokenTorusMesh,
            _gold, node.root);
        outerBevel.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        outerBevel.transform.localScale = Vector3.one * 1.20f;
        node.ring = outerBevel.GetComponent<MeshRenderer>();

        node.face = CreatePrimitive("Smoked Glass Number Face", PrimitiveType.Cylinder,
            node.root, _glassFace, new Vector3(0f, 0.655f, 0f),
            new Vector3(0.94f, 0.048f, 0.94f)).GetComponent<MeshRenderer>();

        GameObject innerBevel = CreateMeshObject("Inner Gold Face Bevel", _tokenTorusMesh,
            _goldHighlight, node.root);
        innerBevel.transform.localPosition = new Vector3(0f, 0.725f, 0f);
        innerBevel.transform.localScale = Vector3.one * 0.91f;
        node.innerRing = innerBevel.GetComponent<MeshRenderer>();

        CreatePrimitive("Glass Specular Glint", PrimitiveType.Sphere, node.root, _glassHighlight,
            new Vector3(-0.28f, 0.800f, 0.22f), new Vector3(0.16f, 0.020f, 0.090f));

        node.number = CreateFloorText((index + 1).ToString(), node.root,
            new Vector3(0f, 0.820f, 0.012f), 1.34f, 0.84f,
            new Color(1f, 0.77f, 0.30f), 4.5f, 40);

        for (int s = 0; s < 3; s++)
        {
            Vector3 badgePosition = new Vector3((s - 1) * 0.39f, 0.895f, 0.43f);

            // A larger sapphire star immediately below the rating creates a
            // crisp 3D outline. Gold remains visible on completed tokens and
            // silver-blue remains visible on unfinished sapphire tokens.
            GameObject backing = CreateMeshObject($"Rating Star {s + 1} Sapphire Backing",
                _starMesh, _sapphireDepth, node.root);
            backing.transform.localPosition = badgePosition + Vector3.down * 0.041f;
            backing.transform.localScale = Vector3.one * 0.155f;
            backing.transform.localRotation = Quaternion.Euler(0f, (s - 1) * -8f, 0f);

            GameObject star = CreateMeshObject($"Rating Star {s + 1}", _starMesh,
                _starDim, node.root);
            // The upper rim is never hidden by the token on the following row.
            star.transform.localPosition = badgePosition;
            star.transform.localScale = Vector3.one * 0.130f;
            star.transform.localRotation = Quaternion.Euler(0f, (s - 1) * -8f, 0f);
            node.stars[s] = star.GetComponent<MeshRenderer>();
        }

        return node;
    }

    void BuildDecorativeObstacles()
    {
        CreatePremiumColumn("Left Turbo Column", new Vector3(-8.86f, 0f, 0.72f));
        CreatePremiumColumn("Right Turbo Column", new Vector3(8.86f, 0f, -0.86f));
        CreatePremiumColumn("Left Bonus Column", new Vector3(-8.82f, 0f, -3.08f));
        CreatePremiumColumn("Right Bonus Column", new Vector3(8.82f, 0f, 3.12f));
        CreateFlipper("Lower Left Guide", new Vector3(-8.25f, 0.24f, -4.54f), 24f);
        CreateFlipper("Upper Right Guide", new Vector3(8.25f, 0.24f, 4.34f), -24f);
    }

    void CreatePremiumColumn(string name, Vector3 position)
    {
        Transform root = new GameObject(name).transform;
        root.gameObject.hideFlags = HideFlags.DontSave;
        root.gameObject.layer = MapLayer;
        root.SetParent(_mapRoot, false);
        root.localPosition = position;

        // A layered physical column replaces the tiny three-piece marker.
        // Every visible band is separate geometry and catches the shared map
        // lighting, while the cyan crown uses emission for cabinet readability.
        CreatePrimitive("Column Soft Shadow", PrimitiveType.Cylinder, root, _darkFace,
            new Vector3(0f, 0.055f, 0.035f), new Vector3(0.82f, 0.050f, 0.82f));
        CreatePrimitive("Column Sapphire Plinth", PrimitiveType.Cylinder, root, _sapphireDepth,
            new Vector3(0f, 0.145f, 0f), new Vector3(0.70f, 0.075f, 0.70f));
        CreatePrimitive("Column Gold Base", PrimitiveType.Cylinder, root, _gold,
            new Vector3(0f, 0.255f, 0f), new Vector3(0.60f, 0.050f, 0.60f));
        CreatePrimitive("Column Lower Sapphire Body", PrimitiveType.Cylinder, root, _sapphireBody,
            new Vector3(0f, 0.485f, 0f), new Vector3(0.46f, 0.180f, 0.46f));

        GameObject lowerRing = CreateMeshObject("Column Lower Gold Collar", _tokenTorusMesh,
            _goldHighlight, root);
        lowerRing.transform.localPosition = new Vector3(0f, 0.695f, 0f);
        lowerRing.transform.localScale = Vector3.one * 0.48f;

        CreatePrimitive("Column Upper Sapphire Body", PrimitiveType.Cylinder, root, _sapphireEdge,
            new Vector3(0f, 0.855f, 0f), new Vector3(0.38f, 0.145f, 0.38f));

        GameObject crownRing = CreateMeshObject("Column Crown Gold Collar", _tokenTorusMesh,
            _gold, root);
        crownRing.transform.localPosition = new Vector3(0f, 1.035f, 0f);
        crownRing.transform.localScale = Vector3.one * 0.50f;

        CreatePrimitive("Column Crown Socket", PrimitiveType.Cylinder, root, _goldDark,
            new Vector3(0f, 1.085f, 0f), new Vector3(0.48f, 0.070f, 0.48f));
        CreatePrimitive("Column Cyan Gem", PrimitiveType.Sphere, root, _cyan,
            new Vector3(0f, 1.315f, 0f), new Vector3(0.38f, 0.38f, 0.38f));
        CreatePrimitive("Column Gem Highlight", PrimitiveType.Sphere, root, _glassHighlight,
            new Vector3(-0.095f, 1.405f, -0.075f), new Vector3(0.085f, 0.040f, 0.065f));

        _animatedProps.Add(root);
    }

    void CreateFlipper(string name, Vector3 position, float angle)
    {
        Transform root = new GameObject(name).transform;
        root.gameObject.hideFlags = HideFlags.DontSave;
        root.gameObject.layer = MapLayer;
        root.SetParent(_mapRoot, false);
        root.localPosition = position;
        root.localRotation = Quaternion.Euler(0f, angle, 0f);
        CreatePrimitive("Bronze Flipper Base", PrimitiveType.Cube, root, _bronze,
            Vector3.zero, new Vector3(1.35f, 0.20f, 0.30f));
        CreatePrimitive("Purple Flipper Face", PrimitiveType.Cube, root, _purple,
            new Vector3(0f, 0.16f, 0f), new Vector3(1.12f, 0.10f, 0.23f));
        CreatePrimitive("Flipper Cyan Edge", PrimitiveType.Cube, root, _cyanDim,
            new Vector3(0f, 0.24f, -0.105f), new Vector3(0.92f, 0.025f, 0.035f));
    }

    void AttachExistingButtons()
    {
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < _owner.levels.Count; i++)
        {
            LevelSelectController.LevelEntry entry = _owner.levels[i];
            if (entry == null || entry.button == null) continue;

            RectTransform buttonRect = entry.button.GetComponent<RectTransform>();
            buttonRect.SetParent(_hitAreas, false);
            Vector3 viewport = i < _nodes.Count
                ? _camera.WorldToViewportPoint(_nodes[i].root.position + Vector3.up * 0.25f)
                : new Vector3(0.5f, 0.5f, 1f);
            buttonRect.anchorMin = buttonRect.anchorMax =
                new Vector2(viewport.x, viewport.y);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(178f, 148f);
            buttonRect.localScale = Vector3.one;

            Image hitImage = entry.button.GetComponent<Image>();
            if (hitImage != null)
            {
                hitImage.sprite = null;
                hitImage.color = new Color(0f, 0f, 0f, 0f);
                hitImage.raycastTarget = true;
            }
            entry.button.transition = Selectable.Transition.None;

            if (entry.label != null) entry.label.gameObject.SetActive(false);
            if (entry.bestTimeLabel != null) entry.bestTimeLabel.gameObject.SetActive(false);
            if (entry.checkmark != null) entry.checkmark.gameObject.SetActive(false);
            if (entry.stars != null)
            {
                for (int s = 0; s < entry.stars.Length; s++)
                    if (entry.stars[s] != null)
                        entry.stars[s].transform.parent.gameObject.SetActive(false);
            }
            Transform lockOverlay = buttonRect.Find("LockOverlay");
            if (lockOverlay != null) lockOverlay.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (!_ready || !_visible || _nodes.Count == 0) return;
        float time = Time.unscaledTime;

        for (int i = 0; i < _nodes.Count; i++)
        {
            NodeVisual node = _nodes[i];
            bool selected = i == _selectedIndex;
            float lift = selected ? 0.12f + Mathf.Sin(time * 3.4f) * 0.025f : 0f;
            node.root.localPosition = node.basePosition + Vector3.up * lift;
            if (node.halo != null && selected)
            {
                float pulse = 1f + Mathf.Sin(time * 4.6f) * 0.055f;
                node.halo.transform.localScale = new Vector3(1.58f, 0.020f, 1.58f) * pulse;
            }
        }

        for (int i = 0; i < _animatedProps.Count; i++)
        {
            Transform prop = _animatedProps[i];
            if (prop != null) prop.Rotate(Vector3.up, 18f * Time.unscaledDeltaTime, Space.Self);
        }

        if (_routeSpark != null && _nodes.Count > 1)
        {
            float travel = Mathf.Repeat(time * 0.45f, _nodes.Count - 1);
            int segment = Mathf.Clamp(Mathf.FloorToInt(travel), 0, _nodes.Count - 2);
            float t = travel - segment;
            Vector3 a = _nodes[segment].basePosition;
            Vector3 b = _nodes[segment + 1].basePosition;
            _routeSpark.localPosition = Vector3.Lerp(a, b, Smooth(t)) + Vector3.up * 0.20f;
        }

        PositionSelectionLight();
    }

    void ApplyAllNodeMaterials()
    {
        for (int i = 0; i < _nodes.Count; i++) ApplyNodeMaterials(i);
        ApplyRouteMaterials();
    }

    void ApplyNodeMaterials(int index)
    {
        if (index < 0 || index >= _nodes.Count) return;
        NodeVisual node = _nodes[index];
        bool selected = index == _selectedIndex;
        if (node.halo != null)
        {
            node.halo.enabled = selected;
            node.halo.sharedMaterial = _cyan;
        }

        // State palette is carried by the actual 3D geometry, not a flat tint:
        // completed = trophy gold, available = sapphire/silver, locked = charcoal.
        Material footMaterial;
        Material bodyMaterial;
        Material stepMaterial;
        Material ringMaterial;
        Material innerRingMaterial;
        Material faceMaterial;

        if (node.locked)
        {
            footMaterial = _lockedMetal;
            bodyMaterial = _lockedMetal;
            stepMaterial = _sapphireDepth;
            ringMaterial = _sapphireDepth;
            innerRingMaterial = _lockedMetal;
            faceMaterial = _lockedMetal;
        }
        else if (node.completed)
        {
            footMaterial = _goldDark;
            bodyMaterial = _gold;
            stepMaterial = _goldHighlight;
            ringMaterial = selected ? _goldHighlight : _gold;
            innerRingMaterial = _goldHighlight;
            faceMaterial = _glassFace;
        }
        else
        {
            footMaterial = _sapphireDepth;
            bodyMaterial = _sapphireBody;
            stepMaterial = _sapphireEdge;
            ringMaterial = selected ? _cyan : _silver;
            innerRingMaterial = _silver;
            faceMaterial = _darkFace;
        }

        if (node.foot != null) node.foot.sharedMaterial = footMaterial;
        if (node.body != null) node.body.sharedMaterial = bodyMaterial;
        if (node.upperStep != null) node.upperStep.sharedMaterial = stepMaterial;
        if (node.ring != null) node.ring.sharedMaterial = ringMaterial;
        if (node.innerRing != null) node.innerRing.sharedMaterial = innerRingMaterial;
        if (node.face != null) node.face.sharedMaterial = faceMaterial;
        if (node.number != null)
        {
            if (node.locked)
                node.number.color = new Color(0.36f, 0.42f, 0.52f);
            else if (node.completed)
                node.number.color = selected
                    ? new Color(1f, 0.96f, 0.76f)
                    : new Color(1f, 0.77f, 0.30f);
            else
                node.number.color = selected
                    ? Color.white
                    : new Color(0.72f, 0.88f, 1f);
        }
        for (int s = 0; s < node.stars.Length; s++)
        {
            if (node.stars[s] != null)
                node.stars[s].sharedMaterial = s < node.earnedStars ? _starGold : _starDim;
        }
    }

    void ApplyRouteMaterials()
    {
        for (int i = 0; i < _routeSegments.Count; i++)
            if (_routeSegments[i] != null)
                _routeSegments[i].sharedMaterial = i < _highestUnlocked ? _goldHighlight : _goldDark;
    }

    void PositionSelectionLight()
    {
        if (_selectionLight == null || _nodes.Count == 0) return;
        Vector3 p = _nodes[Mathf.Clamp(_selectedIndex, 0, _nodes.Count - 1)].root.localPosition;
        _selectionLight.transform.localPosition = p + new Vector3(0f, 1.35f, -0.25f);
    }

    MeshRenderer CreateTubeBetween(string name, Vector3 a, Vector3 b,
        float radius, Material material)
    {
        Vector3 delta = b - a;
        Transform tube = CreatePrimitive(name, PrimitiveType.Cylinder, _mapRoot, material,
            (a + b) * 0.5f, new Vector3(radius, delta.magnitude * 0.5f, radius));
        tube.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        return tube.GetComponent<MeshRenderer>();
    }

    Transform CreateRoundedSlab(string name, Material material, Vector3 position,
        float width, float depth, float height, float cornerRadius)
    {
        Mesh mesh = BuildRoundedRectPrismMesh(name + " Mesh", width, depth,
            height, cornerRadius, 10);
        _meshes.Add(mesh);
        GameObject go = CreateMeshObject(name, mesh, material, _mapRoot);
        go.transform.localPosition = position;
        return go.transform;
    }

    Transform CreatePrimitive(string name, PrimitiveType type, Transform parent,
        Material material, Vector3 position, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.hideFlags = HideFlags.DontSave;
        go.layer = MapLayer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return go.transform;
    }

    GameObject CreateMeshObject(string name, Mesh mesh, Material material, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.hideFlags = HideFlags.DontSave;
        go.layer = MapLayer;
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return go;
    }

    TextMeshPro CreateFloorText(string value, Transform parent, Vector3 position,
        float width, float height, Color color, float fontSize, int sortingOrder)
    {
        GameObject go = new GameObject(value + " 3D Text", typeof(TextMeshPro));
        go.hideFlags = HideFlags.DontSave;
        go.layer = MapLayer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TextMeshPro text = go.GetComponent<TextMeshPro>();
        if (_font != null) text.font = _font;
        text.text = value;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.5f;
        text.fontSizeMax = fontSize;
        text.fontSize = fontSize;
        text.rectTransform.sizeDelta = new Vector2(width, height);
        text.renderer.sortingOrder = sortingOrder;
        Material fontMaterial = text.fontMaterial;
        SetMaterialFloat(fontMaterial, "_FaceDilate", 0.05f);
        SetMaterialFloat(fontMaterial, "_OutlineWidth", 0.055f);
        SetMaterialFloat(fontMaterial, "_Bevel", 0.30f);
        if (fontMaterial.HasProperty("_OutlineColor"))
            fontMaterial.SetColor("_OutlineColor", new Color(0.015f, 0.025f, 0.045f, 0.92f));
        return text;
    }

    Material CreateMaterial(string name, Color color, float metallic,
        float smoothness, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave
        };
        _materials.Add(material);
        SetColor(material, color, emission);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    Material CreateWoodMaterial(string name, Color dark, Color light, float smoothness)
    {
        Material material = CreateMaterial(name, Color.white, 0.05f, smoothness, Color.black);
        Texture2D wood = CreateWoodTexture(name, dark, light);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", wood);
            material.SetTextureScale("_BaseMap", new Vector2(3.4f, 2.0f));
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", wood);
            material.SetTextureScale("_MainTex", new Vector2(3.4f, 2.0f));
        }
        return material;
    }

    Texture2D CreateWoodTexture(string name, Color dark, Color light)
    {
        const int width = 256;
        const int height = 128;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
            name = name + " Grain",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.045f, y * 0.16f);
                float ribbon = Mathf.Sin(x * 0.19f +
                    Mathf.PerlinNoise(x * 0.012f, y * 0.04f) * 9f) * 0.10f;
                float plank = ((y / 32) & 1) == 0 ? 0.045f : -0.025f;
                float t = Mathf.Clamp01(0.24f + grain * 0.62f + ribbon + plank);
                pixels[y * width + x] = Color.Lerp(dark, light, t);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        _textures.Add(texture);
        return texture;
    }

    Mesh BuildRoundedRectPrismMesh(string meshName, float width, float depth,
        float height, float radius, int cornerSegments)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float halfHeight = height * 0.5f;
        radius = Mathf.Clamp(radius, 0.01f, Mathf.Min(halfWidth, halfDepth));
        int perimeterCount = cornerSegments * 4;
        var vertices = new Vector3[2 + perimeterCount * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>(perimeterCount * 12);
        vertices[0] = new Vector3(0f, halfHeight, 0f);
        vertices[1] = new Vector3(0f, -halfHeight, 0f);
        uv[0] = uv[1] = new Vector2(0.5f, 0.5f);

        Vector2[] centers =
        {
            new Vector2(halfWidth - radius, halfDepth - radius),
            new Vector2(-halfWidth + radius, halfDepth - radius),
            new Vector2(-halfWidth + radius, -halfDepth + radius),
            new Vector2(halfWidth - radius, -halfDepth + radius)
        };

        for (int corner = 0; corner < 4; corner++)
        {
            for (int segment = 0; segment < cornerSegments; segment++)
            {
                int perimeter = corner * cornerSegments + segment;
                float degrees = corner * 90f + segment * (90f / cornerSegments);
                float angle = degrees * Mathf.Deg2Rad;
                Vector2 p = centers[corner] +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                int top = 2 + perimeter;
                int bottom = 2 + perimeterCount + perimeter;
                vertices[top] = new Vector3(p.x, halfHeight, p.y);
                vertices[bottom] = new Vector3(p.x, -halfHeight, p.y);
                Vector2 mapped = new Vector2(p.x / width + 0.5f, p.y / depth + 0.5f);
                uv[top] = mapped;
                uv[bottom] = mapped;
            }
        }

        for (int i = 0; i < perimeterCount; i++)
        {
            int next = (i + 1) % perimeterCount;
            int top = 2 + i;
            int topNext = 2 + next;
            int bottom = 2 + perimeterCount + i;
            int bottomNext = 2 + perimeterCount + next;

            triangles.Add(0); triangles.Add(topNext); triangles.Add(top);
            triangles.Add(1); triangles.Add(bottom); triangles.Add(bottomNext);
            triangles.Add(top); triangles.Add(topNext); triangles.Add(bottomNext);
            triangles.Add(top); triangles.Add(bottomNext); triangles.Add(bottom);
        }

        Mesh mesh = new Mesh { name = meshName };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh BuildTorusMesh(string meshName, float majorRadius, float tubeRadius,
        int majorSegments, int tubeSegments)
    {
        int stride = tubeSegments + 1;
        var vertices = new Vector3[(majorSegments + 1) * stride];
        var normals = new Vector3[vertices.Length];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[majorSegments * tubeSegments * 6];

        for (int major = 0; major <= majorSegments; major++)
        {
            float u = major / (float)majorSegments;
            float around = u * Mathf.PI * 2f;
            Vector3 radial = new Vector3(Mathf.Cos(around), 0f, Mathf.Sin(around));
            for (int tube = 0; tube <= tubeSegments; tube++)
            {
                float v = tube / (float)tubeSegments;
                float cross = v * Mathf.PI * 2f;
                Vector3 normal = radial * Mathf.Cos(cross) + Vector3.up * Mathf.Sin(cross);
                int index = major * stride + tube;
                vertices[index] = radial * majorRadius + normal * tubeRadius;
                normals[index] = normal.normalized;
                uv[index] = new Vector2(u, v);
            }
        }

        int triangle = 0;
        for (int major = 0; major < majorSegments; major++)
        {
            for (int tube = 0; tube < tubeSegments; tube++)
            {
                int a = major * stride + tube;
                int b = (major + 1) * stride + tube;
                int c = b + 1;
                int d = a + 1;
                triangles[triangle++] = a;
                triangles[triangle++] = c;
                triangles[triangle++] = b;
                triangles[triangle++] = a;
                triangles[triangle++] = d;
                triangles[triangle++] = c;
            }
        }

        Mesh mesh = new Mesh { name = meshName };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh BuildStarPrismMesh()
    {
        const int points = 10;
        // A deeper extrusion catches the warm key and cool rim lights, keeping
        // even the smaller, newly-separated rating stars unmistakably 3D.
        const float thickness = 0.40f;
        var vertices = new Vector3[points * 2 + 2];
        var triangles = new List<int>(points * 12);
        vertices[0] = new Vector3(0f, thickness * 0.5f, 0f);
        vertices[1] = new Vector3(0f, -thickness * 0.5f, 0f);
        for (int i = 0; i < points; i++)
        {
            float radius = (i & 1) == 0 ? 1f : 0.43f;
            float angle = -Mathf.PI * 0.5f + i * Mathf.PI * 2f / points;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[2 + i] = new Vector3(x, thickness * 0.5f, z);
            vertices[2 + points + i] = new Vector3(x, -thickness * 0.5f, z);
        }
        for (int i = 0; i < points; i++)
        {
            int next = (i + 1) % points;
            int top = 2 + i;
            int topNext = 2 + next;
            int bottom = 2 + points + i;
            int bottomNext = 2 + points + next;
            triangles.Add(0); triangles.Add(topNext); triangles.Add(top);
            triangles.Add(1); triangles.Add(bottom); triangles.Add(bottomNext);
            triangles.Add(top); triangles.Add(topNext); triangles.Add(bottomNext);
            triangles.Add(top); triangles.Add(bottomNext); triangles.Add(bottom);
        }
        Mesh mesh = new Mesh { name = "Buca 3D Rating Star" };
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void AddDirectionalLight(string name, Color color, float intensity, Quaternion rotation)
    {
        GameObject go = new GameObject(name, typeof(Light));
        go.hideFlags = HideFlags.DontSave;
        go.layer = MapLayer;
        go.transform.SetParent(_stageRoot.transform, false);
        go.transform.localRotation = rotation;
        Light light = go.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.cullingMask = 1 << MapLayer;
        light.shadows = LightShadows.Soft;
    }

    void AddPointLight(string name, Vector3 position, Color color,
        float intensity, float range, bool shadows)
    {
        GameObject go = new GameObject(name, typeof(Light));
        go.hideFlags = HideFlags.DontSave;
        go.layer = MapLayer;
        go.transform.SetParent(_mapRoot, false);
        go.transform.localPosition = position;
        Light light = go.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.cullingMask = 1 << MapLayer;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
    }

    RectTransform FindRect(string childName)
    {
        Transform child = _card.Find(childName);
        return child != null ? child as RectTransform : null;
    }

    void HideChild(string childName)
    {
        Transform child = _card.Find(childName);
        if (child != null) child.gameObject.SetActive(false);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetColor(Material material, Color color, Color emission)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
    }

    static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    void OnDestroy()
    {
        if (_rawImage != null) _rawImage.texture = null;
        if (_renderTexture != null)
        {
            if (_renderTexture.IsCreated()) _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_stageRoot != null) Destroy(_stageRoot);
        for (int i = 0; i < _materials.Count; i++)
            if (_materials[i] != null) Destroy(_materials[i]);
        for (int i = 0; i < _textures.Count; i++)
            if (_textures[i] != null) Destroy(_textures[i]);
        for (int i = 0; i < _meshes.Count; i++)
            if (_meshes[i] != null) Destroy(_meshes[i]);
        _materials.Clear();
        _textures.Clear();
        _meshes.Clear();
    }
}
#endif
