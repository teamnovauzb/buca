using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Runtime-built 3D presentation for the fail / Continue decision.  The real
/// UI Buttons remain above its RenderTexture as invisible hit targets, so the
/// existing Luxodd callbacks and arcade input are not replaced.
/// </summary>
public sealed class PremiumFailureConsole3D : MonoBehaviour
{
    const int PreviewLayer = 28;
    const float StageHeight = 2400f;

    static readonly Color Coral = new Color(1f, 0.24f, 0.20f, 1f);
    static readonly Color Champagne = new Color(0.88f, 0.68f, 0.40f, 1f);
    static readonly Color Cyan = new Color(0.08f, 0.78f, 0.86f, 1f);

    readonly List<Material> _materials = new List<Material>(10);
    readonly List<Mesh> _meshes = new List<Mesh>(10);
    readonly List<Texture2D> _textures = new List<Texture2D>(4);

    TMP_FontAsset _font;
    RectTransform _viewport;
    RawImage _rawImage;
    RenderTexture _renderTexture;
    GameObject _stageRoot;
    Transform _board;
    Camera _camera;
    GameObject _countdownTag;
    GameObject _continueButtonObject;
    GameObject _endButtonObject;
    GameObject _secondChanceVisual;
    Transform _secondChancePuck;
    Transform _secondChanceHoleRim;
    Vector3 _secondChancePuckHome;
    Vector3 _secondChanceHoleRimHomeScale;
    TextMeshPro[] _titleLayers;
    TextMeshPro[] _countdownLayers;
    TextMeshPro[] _continueLayers;
    TextMeshPro[] _endLayers;
    TextMeshPro[] _secondChanceSubtitleLayers;
    bool _ready;
    bool _secondChanceMode;
    string _title = "TIME'S UP!";
    string _continue = "CONTINUE";
    string _end = "END GAME";

    public bool IsReady => _ready && _viewport != null && _camera != null;

    public static PremiumFailureConsole3D Ensure(Transform owner, TMP_FontAsset font)
    {
        if (owner == null) return null;
        PremiumFailureConsole3D console = owner.GetComponent<PremiumFailureConsole3D>();
        if (console == null) console = owner.gameObject.AddComponent<PremiumFailureConsole3D>();
        console._font = ResolveUiFont(font != null ? font : TMP_Settings.defaultFontAsset);
        console.EnsureBuilt();
        return console;
    }

    public void SetCopy(string title, string continueLabel, string endLabel)
    {
        _title = ForceSingleLine(title, "TIME'S UP!");
        _continue = ForceSingleLine(continueLabel, "CONTINUE");
        _end = ForceSingleLine(endLabel, "END GAME");
        if (!_ready) return;
        SetStack(_titleLayers, _title, _secondChanceMode ? Champagne : Coral);
        SetStack(_continueLayers, _continue, Champagne);
        SetStack(_endLayers, _end, Champagne);
    }

    /// <summary>
    /// Switches the failure console into the one-time Level 1 reward layout.
    /// The normal End choice disappears and a small illuminated BUCA course,
    /// puck and flag make the message a game moment rather than plain text.
    /// </summary>
    public void SetSecondChanceMode(bool enabled)
    {
        _secondChanceMode = enabled;
        if (!_ready) return;
        if (_continueButtonObject != null) _continueButtonObject.SetActive(true);
        if (_secondChanceVisual != null) _secondChanceVisual.SetActive(enabled);
        if (_endButtonObject != null) _endButtonObject.SetActive(true);
        SetEndChoiceCenter(enabled ? -2.25f : -2.03f);
        SetStackActive(_endLayers, true);
        SetStackActive(_secondChanceSubtitleLayers, enabled);
        if (_countdownTag != null && enabled) _countdownTag.SetActive(false);
        SetStack(_titleLayers, _title, enabled ? Champagne : Coral);
    }

    public void Show(bool showCountdown, int seconds)
    {
        EnsureBuilt();
        if (!IsReady) return;
        SetCopy(_title, _continue, _end);
        if (_countdownTag != null) _countdownTag.SetActive(showCountdown && !_secondChanceMode);
        if (showCountdown && !_secondChanceMode) SetCountdown(seconds);
        if (_secondChanceVisual != null) _secondChanceVisual.SetActive(_secondChanceMode);
        if (_endButtonObject != null) _endButtonObject.SetActive(true);
        SetEndChoiceCenter(_secondChanceMode ? -2.25f : -2.03f);
        SetStackActive(_endLayers, true);
        SetStackActive(_secondChanceSubtitleLayers, _secondChanceMode);
        _viewport.gameObject.SetActive(true);
        _viewport.SetSiblingIndex(0);
        if (_stageRoot != null) _stageRoot.SetActive(true);
        _camera.enabled = true;
    }

    public void SetCountdown(int seconds)
    {
        if (!_ready) return;
        SetStack(_countdownLayers, Mathf.Max(0, seconds) + "s", Champagne);
    }

    public void Hide()
    {
        if (_camera != null) _camera.enabled = false;
        if (_stageRoot != null) _stageRoot.SetActive(false);
        if (_viewport != null) _viewport.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_secondChanceMode || _secondChanceVisual == null
            || !_secondChanceVisual.activeInHierarchy) return;

        float time = Time.unscaledTime;
        if (_secondChancePuck != null)
        {
            float roll = 0.5f + 0.5f * Mathf.Sin(time * 2.2f);
            _secondChancePuck.localPosition = _secondChancePuckHome
                + new Vector3(0f, roll * 0.24f, 0f);
            _secondChancePuck.localRotation = Quaternion.Euler(0f, 0f, -time * 150f);
        }
        if (_secondChanceHoleRim != null)
        {
            float pulse = 1f + Mathf.Sin(time * 3.6f) * 0.08f;
            _secondChanceHoleRim.localScale = new Vector3(
                _secondChanceHoleRimHomeScale.x * pulse,
                _secondChanceHoleRimHomeScale.y,
                _secondChanceHoleRimHomeScale.z * pulse);
        }
    }

    void EnsureBuilt()
    {
        if (_ready) return;
        BuildViewport();
        BuildStage();
        BuildBoard();
        _ready = true;
        Hide();
    }

    void BuildViewport()
    {
        Transform existing = transform.Find("PremiumFailureConsole3DViewport");
        GameObject viewportGO = existing != null
            ? existing.gameObject
            : new GameObject("PremiumFailureConsole3DViewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
        if (existing == null) viewportGO.transform.SetParent(transform, false);

        _viewport = (RectTransform)viewportGO.transform;
        _viewport.anchorMin = Vector2.zero;
        _viewport.anchorMax = Vector2.one;
        _viewport.offsetMin = Vector2.zero;
        _viewport.offsetMax = Vector2.zero;
        _viewport.localRotation = Quaternion.identity;
        _viewport.localScale = Vector3.one;
        _viewport.SetSiblingIndex(0);

        _rawImage = viewportGO.GetComponent<RawImage>();
        _rawImage.color = Color.white;
        _rawImage.raycastTarget = false;

        _renderTexture = new RenderTexture(1200, 900, 24, RenderTextureFormat.ARGB32)
        {
            name = "BucaPremiumFailureConsole3D",
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
        _stageRoot = new GameObject("BucaPremiumFailureConsole3DStage")
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        _stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);

        GameObject cameraGO = new GameObject("PremiumFailureFrontCamera", typeof(Camera))
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
        _camera.orthographicSize = 3.70f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 30f;
        _camera.allowHDR = true;
        _camera.allowMSAA = true;
        _camera.targetTexture = _renderTexture;
        _camera.enabled = false;

        AddDirectionalLight("Warm Key", new Color(1f, 0.75f, 0.50f), 1.30f,
            Quaternion.Euler(28f, -34f, 0f), true);
        AddDirectionalLight("Cool Fill", new Color(0.24f, 0.70f, 1f), 0.42f,
            Quaternion.Euler(16f, 142f, 0f), false);
        AddPointLight("Gold Frame Light", new Vector3(-3.9f, 2.6f, -2.8f),
            new Color(1f, 0.58f, 0.22f), 2.0f, 6.5f);
        AddPointLight("Cyan Rail Light", new Vector3(3.5f, 0.1f, -2.4f),
            new Color(0.05f, 0.72f, 0.90f), 1.35f, 5.5f);

        _board = new GameObject("Straight Premium Timeout Board").transform;
        _board.gameObject.hideFlags = HideFlags.DontSave;
        _board.gameObject.layer = PreviewLayer;
        _board.SetParent(_stageRoot.transform, false);
        // Deliberately identity: the approved design must be perfectly front-on.
        _board.localPosition = Vector3.zero;
        _board.localRotation = Quaternion.identity;
        _board.localScale = Vector3.one;
    }

    void BuildBoard()
    {
        Material walnut = CreateWoodMaterial("Timeout Walnut",
            new Color(0.040f, 0.014f, 0.008f, 1f),
            new Color(0.20f, 0.075f, 0.030f, 1f), 0.58f);
        Material gold = CreateLitMaterial("Timeout Champagne Gold",
            new Color(0.66f, 0.40f, 0.17f, 1f), 0.92f, 0.86f,
            new Color(0.030f, 0.012f, 0.002f, 1f));
        Material black = CreateLitMaterial("Timeout Matte Black",
            new Color(0.012f, 0.014f, 0.016f, 1f), 0.18f, 0.46f, Color.black);
        Material green = CreateLitMaterial("Timeout Emerald",
            new Color(0.018f, 0.25f, 0.20f, 1f), 0.48f, 0.72f,
            new Color(0.002f, 0.025f, 0.018f, 1f));
        Material burgundy = CreateLitMaterial("Timeout Burgundy",
            new Color(0.32f, 0.045f, 0.030f, 1f), 0.42f, 0.68f,
            new Color(0.022f, 0.001f, 0.001f, 1f));
        Material coral = CreateLitMaterial("Timeout Coral Title",
            new Color(0.92f, 0.11f, 0.075f, 1f), 0.56f, 0.78f,
            new Color(0.13f, 0.012f, 0.006f, 1f));
        Material cyan = CreateLitMaterial("Timeout Cyan Rail",
            new Color(0.020f, 0.50f, 0.58f, 1f), 0.72f, 0.92f,
            new Color(0.008f, 0.25f, 0.33f, 1f));

        // Five simple, physically layered boxes reproduce the approved image.
        CreateBox("Thick Walnut Back", 9.55f, 6.45f, 0.62f, 0.18f,
            new Vector3(0f, 0f, 0.42f), walnut);
        CreateBox("Champagne Gold Trim", 9.14f, 6.04f, 0.43f, 0.16f,
            new Vector3(0f, 0f, 0.05f), gold);
        CreateBox("Matte Black Inset", 8.80f, 5.70f, 0.32f, 0.13f,
            new Vector3(0f, 0f, -0.25f), black);

        _continueButtonObject = CreateBox("Continue Button", 7.45f, 1.02f, 0.38f, 0.13f,
            new Vector3(0f, -0.72f, -0.52f), green);
        _endButtonObject = CreateBox("End Game Button", 7.45f, 1.02f, 0.38f, 0.13f,
            new Vector3(0f, -2.03f, -0.52f), burgundy);

        CreateBox("Cyan Separator", 7.78f, 0.065f, 0.15f, 0.026f,
            new Vector3(0f, 0.15f, -0.48f), cyan);
        CreateBox("Gold Bottom Rail", 7.78f, 0.065f, 0.15f, 0.026f,
            new Vector3(0f, -2.82f, -0.48f), gold);

        _countdownTag = CreateBox("Countdown Tag", 1.48f, 0.60f, 0.26f, 0.10f,
            new Vector3(0f, 0.78f, -0.48f), black);

        _titleLayers = CreateTextStack("Title", _title,
            new Vector3(0f, 1.78f, -0.57f), 7.75f, 1.18f, Coral, 4.8f);
        _countdownLayers = CreateTextStack("Countdown", "10s",
            new Vector3(0f, 0f, -0.21f), 1.22f, 0.48f, Champagne, 2.45f,
            _countdownTag.transform);
        // Extra horizontal room plus a slightly lower maximum size guarantees
        // CONTINUE remains one line even with the widest fallback TMP font.
        _continueLayers = CreateTextStack("Continue Label", _continue,
            new Vector3(0f, -0.72f, -0.76f), 7.05f, 0.72f, Champagne, 2.72f);
        _endLayers = CreateTextStack("End Label", _end,
            new Vector3(0f, -2.03f, -0.76f), 6.70f, 0.72f, Champagne, 3.0f);

        // Keep the title material as genuine lit geometry around the TMP face:
        // a thin coral bevel plate catches the same real lights as the board.
        CreateBox("Title Depth Accent", 7.25f, 0.05f, 0.10f, 0.02f,
            new Vector3(0f, 1.18f, -0.48f), coral);

        BuildSecondChanceVisual(gold, black, cyan);
    }

    void BuildSecondChanceVisual(Material gold, Material black, Material cyan)
    {
        _secondChanceVisual = new GameObject("Level 1 Second Chance Mini BUCA Board")
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        _secondChanceVisual.transform.SetParent(_board, false);

        // The reward contains a small copy of the real BUCA playfield. Its wood
        // lane, cyan rails, V-shaped bumpers and single target make it instantly
        // recognizable without incorrectly presenting the game as golf.
        CreateTextStack("BUCA Mark", "BUCA",
            new Vector3(-2.35f, 0.53f, -0.79f), 3.05f, 0.72f,
            Cyan, 3.15f, _secondChanceVisual.transform);

        Material miniWood = CreateWoodMaterial("Mini BUCA Walnut",
            new Color(0.055f, 0.020f, 0.010f, 1f),
            new Color(0.28f, 0.105f, 0.040f, 1f), 0.54f);
        Material bumperMaterial = CreateLitMaterial("Mini BUCA Bumper",
            new Color(0.74f, 0.94f, 0.98f, 1f), 0.18f, 0.86f,
            new Color(0.015f, 0.17f, 0.21f, 1f));

        const float laneX = 1.55f;
        const float laneY = 0.50f;
        CreateBox("Mini BUCA Wood Playfield", 3.10f, 1.38f, 0.22f, 0.11f,
            new Vector3(laneX, laneY, -0.69f), miniWood, _secondChanceVisual.transform);
        CreateBox("Mini BUCA Left Gutter", 0.13f, 1.18f, 0.12f, 0.035f,
            new Vector3(0.13f, laneY, -0.85f), black, _secondChanceVisual.transform);
        CreateBox("Mini BUCA Right Gutter", 0.13f, 1.18f, 0.12f, 0.035f,
            new Vector3(2.97f, laneY, -0.85f), black, _secondChanceVisual.transform);
        CreateBox("Mini BUCA Left Cyan Rail", 0.060f, 1.10f, 0.10f, 0.020f,
            new Vector3(0.27f, laneY, -0.94f), cyan, _secondChanceVisual.transform);
        CreateBox("Mini BUCA Right Cyan Rail", 0.060f, 1.10f, 0.10f, 0.020f,
            new Vector3(2.83f, laneY, -0.94f), cyan, _secondChanceVisual.transform);
        CreateBox("Mini BUCA Top Cyan Rail", 2.62f, 0.060f, 0.10f, 0.020f,
            new Vector3(laneX, 1.12f, -0.94f), cyan, _secondChanceVisual.transform);
        CreateBox("Mini BUCA Bottom Gold Rail", 2.62f, 0.050f, 0.09f, 0.018f,
            new Vector3(laneX, -0.12f, -0.93f), gold, _secondChanceVisual.transform);

        Material puckMaterial = CreateLitMaterial("Second Chance Ivory Puck",
            new Color(0.94f, 0.97f, 1f, 1f), 0.28f, 0.93f,
            new Color(0.05f, 0.10f, 0.12f, 1f));
        _secondChancePuck = CreatePrimitive("Bonus Puck", PrimitiveType.Sphere,
            new Vector3(laneX, 0.02f, -1.02f), new Vector3(0.27f, 0.27f, 0.13f),
            Quaternion.identity, puckMaterial, _secondChanceVisual.transform).transform;
        _secondChancePuckHome = _secondChancePuck.localPosition;

        _secondChanceHoleRim = CreatePrimitive("BUCA Target Rim", PrimitiveType.Cylinder,
            new Vector3(laneX, 0.91f, -0.99f), new Vector3(0.30f, 0.050f, 0.30f),
            Quaternion.Euler(90f, 0f, 0f), cyan, _secondChanceVisual.transform).transform;
        _secondChanceHoleRimHomeScale = _secondChanceHoleRim.localScale;
        CreatePrimitive("BUCA Target", PrimitiveType.Cylinder,
            new Vector3(laneX, 0.91f, -1.05f), new Vector3(0.20f, 0.06f, 0.20f),
            Quaternion.Euler(90f, 0f, 0f), black, _secondChanceVisual.transform);

        GameObject leftBumper = CreateBox("Mini BUCA Left V Bumper",
            0.70f, 0.075f, 0.10f, 0.025f,
            new Vector3(1.26f, 0.64f, -1.01f), bumperMaterial,
            _secondChanceVisual.transform);
        leftBumper.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        GameObject rightBumper = CreateBox("Mini BUCA Right V Bumper",
            0.70f, 0.075f, 0.10f, 0.025f,
            new Vector3(1.84f, 0.64f, -1.01f), bumperMaterial,
            _secondChanceVisual.transform);
        rightBumper.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);

        for (int i = 0; i < 5; i++)
        {
            float y = 0.30f + i * 0.105f;
            CreatePrimitive("Mini BUCA Aim Dot " + (i + 1), PrimitiveType.Sphere,
                new Vector3(laneX, y, -1.04f), new Vector3(0.045f, 0.045f, 0.025f),
                Quaternion.identity, cyan, _secondChanceVisual.transform);
        }

        _secondChanceSubtitleLayers = CreateCleanTextStack(
            "Second Chance Encouragement", "THIS TIME YOU CAN DO IT",
            new Vector3(0f, -1.43f, -0.79f), 7.20f, 0.34f,
            Cyan, 1.40f, _secondChanceVisual.transform);
        _secondChanceVisual.SetActive(false);
    }

    GameObject CreateBox(string name, float width, float height, float depth,
        float bevel, Vector3 position, Material material, Transform parent = null)
    {
        Mesh mesh = BuildChamferedBoxMesh(width, height, depth, bevel);
        _meshes.Add(mesh);
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(parent != null ? parent : _board, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return go;
    }

    GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Quaternion rotation, Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.hideFlags = HideFlags.DontSave;
        go.layer = PreviewLayer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
        return go;
    }

    TextMeshPro[] CreateTextStack(string name, string content, Vector3 position,
        float width, float height, Color color, float fontSize, Transform parent = null)
    {
        Transform textParent = parent != null ? parent : _board;
        TextMeshPro[] layers = new TextMeshPro[3];
        layers[0] = CreateWorldText(name + " Deep Shadow", textParent, content,
            position + new Vector3(0.060f, -0.055f, 0.080f), width, height,
            new Color(0f, 0f, 0f, 0.90f), fontSize, 0);
        layers[1] = CreateWorldText(name + " Depth", textParent, content,
            position + new Vector3(0.030f, -0.027f, 0.040f), width, height,
            new Color(0.15f, 0.070f, 0.030f, 1f), fontSize, 1);
        layers[2] = CreateWorldText(name, textParent, content, position, width,
            height, color, fontSize, 2);
        return layers;
    }

    TextMeshPro[] CreateCleanTextStack(string name, string content, Vector3 position,
        float width, float height, Color color, float fontSize, Transform parent = null)
    {
        Transform textParent = parent != null ? parent : _board;
        TextMeshPro[] layers = new TextMeshPro[2];
        layers[0] = CreateWorldText(name + " Soft Shadow", textParent, content,
            position + new Vector3(0.025f, -0.030f, 0.045f), width, height,
            new Color(0f, 0f, 0f, 0.78f), fontSize, 1);
        layers[1] = CreateWorldText(name, textParent, content, position, width,
            height, color, fontSize, 2);

        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].characterSpacing = 1.15f;
            Material material = layers[i].fontMaterial;
            SetMaterialFloat(material, "_FaceDilate", i == layers.Length - 1 ? 0.035f : 0f);
            SetMaterialFloat(material, "_OutlineWidth", i == layers.Length - 1 ? 0.025f : 0f);
            SetMaterialFloat(material, "_Bevel", 0f);
            SetMaterialFloat(material, "_BevelWidth", 0f);
        }
        return layers;
    }

    TextMeshPro CreateWorldText(string name, Transform parent, string content,
        Vector3 localPosition, float width, float height, Color color,
        float fontSize, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(TextMeshPro))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (_font != null) tmp.font = _font;
        tmp.text = content;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = Vector4.zero;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = fontSize;
        tmp.characterSpacing = 0.8f;
        tmp.rectTransform.sizeDelta = new Vector2(width, height);
        tmp.renderer.sortingOrder = sortingOrder;

        Material textMaterial = tmp.fontMaterial;
        SetMaterialFloat(textMaterial, "_FaceDilate", 0.08f);
        SetMaterialFloat(textMaterial, "_OutlineWidth", sortingOrder == 2 ? 0.045f : 0f);
        SetMaterialFloat(textMaterial, "_Bevel", sortingOrder == 2 ? 0.38f : 0f);
        SetMaterialFloat(textMaterial, "_BevelWidth", sortingOrder == 2 ? 0.16f : 0f);
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

    static void SetStackActive(TextMeshPro[] stack, bool active)
    {
        if (stack == null) return;
        for (int i = 0; i < stack.Length; i++)
        {
            if (stack[i] != null) stack[i].gameObject.SetActive(active);
        }
    }

    void SetEndChoiceCenter(float centerY)
    {
        if (_endButtonObject != null)
        {
            Vector3 buttonPosition = _endButtonObject.transform.localPosition;
            buttonPosition.y = centerY;
            _endButtonObject.transform.localPosition = buttonPosition;
        }

        if (_endLayers == null || _endLayers.Length == 0) return;
        TextMeshPro front = _endLayers[_endLayers.Length - 1];
        if (front == null) return;
        float deltaY = centerY - front.transform.localPosition.y;
        for (int i = 0; i < _endLayers.Length; i++)
        {
            if (_endLayers[i] == null) continue;
            Vector3 layerPosition = _endLayers[i].transform.localPosition;
            layerPosition.y += deltaY;
            _endLayers[i].transform.localPosition = layerPosition;
        }
    }

    static TMP_FontAsset ResolveUiFont(TMP_FontAsset fallback)
    {
        // Oswald is already referenced by the in-game HUD, so it is available
        // without adding another runtime asset or increasing the WebGL build.
        TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFonts.Length; i++)
        {
            TMP_FontAsset candidate = loadedFonts[i];
            if (candidate != null && candidate.name == "Oswald Bold SDF")
                return candidate;
        }
        return fallback;
    }

    static string ForceSingleLine(string value, string fallback)
    {
        string safe = string.IsNullOrWhiteSpace(value) ? fallback : value;
        return safe.Replace('\r', ' ').Replace('\n', ' ').Trim().ToUpperInvariant();
    }

    Material CreateWoodMaterial(string name, Color dark, Color light, float smoothness)
    {
        Material material = CreateLitMaterial(name, Color.white, 0.04f, smoothness, Color.black, false);
        Texture2D wood = CreateWoodTexture(name, dark, light);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", wood);
            material.SetTextureScale("_BaseMap", new Vector2(1.5f, 3.0f));
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", wood);
            material.SetTextureScale("_MainTex", new Vector2(1.5f, 3.0f));
        }
        return material;
    }

    Material CreateLitMaterial(string name, Color color, float metallic,
        float smoothness, Color emission, bool addBrushedTexture = true)
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

        if (addBrushedTexture)
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
            float noise = Mathf.PerlinNoise(x * 0.028f + 3.1f, y * 0.12f + 7.4f);
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
                + Mathf.Sin(y * 1.73f + x * 0.11f) * 0.035f
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

    void AddPointLight(string name, Vector3 position, Color color,
        float intensity, float range)
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

        Mesh mesh = new Mesh { name = "Buca Chamfered Timeout Box", hideFlags = HideFlags.DontSave };
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
