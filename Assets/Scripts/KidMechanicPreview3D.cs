#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// A tiny, isolated 3D theatre used by the first-encounter mechanic intro.
/// The puck mascot demonstrates the mechanic instead of asking young players
/// to understand a paragraph of technical instructions.
/// </summary>
[DisallowMultipleComponent]
public sealed class KidMechanicPreview3D : MonoBehaviour
{
    const int PreviewLayer = 26;
    const float StageHeight = 3100f;

    readonly List<Material> _materials = new List<Material>(8);
    readonly List<Texture2D> _textures = new List<Texture2D>(4);
    readonly List<GameObject> _temporaryProps = new List<GameObject>(24);
    readonly List<Transform> _tokens = new List<Transform>(4);

    RawImage _image;
    RenderTexture _renderTexture;
    GameObject _stageRoot;
    Transform _demoRoot;
    Transform _puck;
    Transform _movingProp;
    Transform _portalA;
    Transform _portalB;
    Transform _goal;
    Transform _goalPulse;
    Transform _impactFlash;
    Camera _camera;
    TrailRenderer _puckTrail;
    Material _accentMaterial;
    Material _puckEdgeMaterial;
    Material _puckFaceMaterial;
    Material _darkMaterial;
    Material _whiteMaterial;
    Material _blackMaterial;
    Material _goldMaterial;
    Material _woodMaterial;
    Material _railMaterial;
    Material _laneMaterial;
    string _kind = "BANK";
    float _startedAt;
    float _lastPhase;
    bool _ready;

    public static KidMechanicPreview3D Ensure(RawImage target)
    {
        if (target == null) return null;
        KidMechanicPreview3D preview = target.GetComponent<KidMechanicPreview3D>();
        if (preview == null) preview = target.gameObject.AddComponent<KidMechanicPreview3D>();
        preview._image = target;
        preview.EnsureBuilt();
        return preview;
    }

    public void Configure(string kind, Color accent)
    {
        EnsureBuilt();
        if (!_ready) return;

        _kind = string.IsNullOrWhiteSpace(kind) ? "BANK" : kind;
        _startedAt = Time.unscaledTime;
        _lastPhase = 0f;
        if (_puckTrail != null) _puckTrail.Clear();
        SetMaterialColor(_accentMaterial, accent, accent * 0.34f);
        SetMaterialColor(_puckEdgeMaterial,
            Color.Lerp(accent, Color.white, 0.18f), accent * 0.20f);
        RebuildMechanicProps();
        SetVisible(true);
        Tick();
    }

    public void SetVisible(bool visible)
    {
        if (_stageRoot != null) _stageRoot.SetActive(visible);
        if (_camera != null) _camera.enabled = visible;
        if (_image != null) _image.enabled = visible;
    }

    public void Tick()
    {
        if (!_ready || _puck == null || _camera == null || !_camera.enabled) return;

        float elapsed = Time.unscaledTime - _startedAt;
        float phase = Mathf.Repeat(elapsed / 3.25f, 1f);
        if (phase < _lastPhase && _puckTrail != null) _puckTrail.Clear();
        _lastPhase = phase;
        Vector3 start = new Vector3(-2.30f, 0f, -0.83f);
        Vector3 finish = new Vector3(2.18f, 0f, 0.82f);
        float x = Mathf.Lerp(start.x, finish.x, Smooth(phase));
        float y = 0f;
        float z = Mathf.Lerp(start.z, finish.z, Smooth(phase));
        float squash = 1f;
        float impact = 0f;
        bool showPuck = true;

        if (_goal != null)
            _goal.localPosition = new Vector3(finish.x, 0f, finish.z);

        switch (_kind)
        {
            case "BANK":
            {
                // Stop the puck OUTSIDE the front face of the blue wall, then
                // send it away in a strong V. The old contact point (z=1.35)
                // was inside a wall centred at z=1.30, teaching the opposite
                // of the real mechanic.
                const float contactX = 0.72f;
                const float contactZ = 0.88f;
                Vector3 bounceFinish = new Vector3(2.18f, 0f, -0.48f);
                if (_goal != null) _goal.localPosition = bounceFinish;

                if (phase < 0.54f)
                {
                    float t = Smooth(phase / 0.54f);
                    x = Mathf.Lerp(start.x, contactX, t);
                    z = Mathf.Lerp(start.z, contactZ, t);
                }
                else
                {
                    float t = Smooth((phase - 0.54f) / 0.46f);
                    x = Mathf.Lerp(contactX, bounceFinish.x, t);
                    z = Mathf.Lerp(contactZ, bounceFinish.z, t);
                }
                impact = ImpactPulse(phase, 0.54f, 0.075f);
                squash = 1f - impact * 0.22f;
                break;
            }
            case "RIM":
            {
                if (phase < 0.54f)
                {
                    float t = Smooth(phase / 0.54f);
                    x = Mathf.Lerp(start.x, 0.72f, t);
                    z = Mathf.Lerp(start.z, 0.88f, t);
                }
                else
                {
                    float t = Smooth((phase - 0.54f) / 0.46f);
                    x = Mathf.Lerp(0.72f, finish.x, t);
                    z = Mathf.Lerp(0.88f, finish.z, t);
                }
                impact = ImpactPulse(phase, 0.54f, 0.075f);
                squash = 1f - impact * 0.22f;
                break;
            }
            case "KICK":
            {
                if (phase < 0.48f)
                {
                    x = Mathf.Lerp(start.x, 0.15f, Smooth(phase / 0.48f));
                    z = Mathf.Lerp(start.z, -0.12f, Smooth(phase / 0.48f));
                }
                else
                {
                    float t = Smooth((phase - 0.48f) / 0.52f);
                    x = Mathf.Lerp(0.15f, finish.x, t);
                    z = Mathf.Lerp(-0.12f, finish.z, t) + Mathf.Sin(t * Mathf.PI) * 0.42f;
                }
                impact = ImpactPulse(phase, 0.48f, 0.075f);
                squash = 1f - impact * 0.28f;
                break;
            }
            case "MUD":
            {
                float slowPhase = phase < 0.33f
                    ? phase * 1.25f
                    : 0.4125f + (phase - 0.33f) * 0.50f;
                x = Mathf.Lerp(start.x, finish.x, Smooth(slowPhase));
                z = Mathf.Lerp(start.z, finish.z, Smooth(slowPhase)) +
                    Mathf.Sin(phase * 18f) * 0.035f * Mathf.SmoothStep(0f, 1f, phase);
                squash = 1f - Mathf.Sin(phase * 14f) * 0.025f;
                break;
            }
            case "ICE":
            case "FAST":
            {
                float t = Mathf.Clamp01(phase * 1.45f);
                x = Mathf.Lerp(start.x, finish.x, Smooth(t));
                z = Mathf.Lerp(start.z, finish.z, Smooth(t));
                break;
            }
            case "MOVE":
            {
                float goalZ = 0.45f + Mathf.Sin(elapsed * 2.25f) * 0.62f;
                if (_goal != null)
                    _goal.localPosition = new Vector3(finish.x, 0f, goalZ);
                x = Mathf.Lerp(start.x, finish.x, Smooth(phase));
                z = Mathf.Lerp(start.z, goalZ, Smooth(phase)) +
                    Mathf.Sin(phase * Mathf.PI) * 0.24f;
                break;
            }
            case "+PTS":
                x = Mathf.Lerp(start.x, finish.x, Smooth(phase));
                z = Mathf.Lerp(start.z, finish.z, Smooth(phase));
                for (int i = 0; i < _tokens.Count; i++)
                {
                    Transform token = _tokens[i];
                    if (token == null) continue;
                    float pop = 1f + Mathf.Max(0f,
                        1f - Mathf.Abs(x - token.localPosition.x) * 4f) * 0.55f;
                    token.localScale = Vector3.one * (0.20f * pop);
                    token.Rotate(Vector3.up, 150f * Time.unscaledDeltaTime, Space.Self);
                }
                break;
            case "!":
                x = Mathf.Lerp(start.x, finish.x, Smooth(phase));
                z = Mathf.Lerp(start.z, finish.z, Smooth(phase)) +
                    Mathf.Sin(phase * Mathf.PI * 2f) * 0.52f;
                break;
            case "PUSH":
            case "WIND":
                x = Mathf.Lerp(start.x, finish.x, Smooth(phase));
                z = Mathf.Lerp(start.z, finish.z, Smooth(phase)) +
                    Mathf.Sin(phase * Mathf.PI) * 0.55f;
                break;
            case "PULL":
                x = Mathf.Lerp(start.x, finish.x, Smooth(phase));
                z = Mathf.Lerp(start.z, finish.z, Smooth(phase)) +
                    Mathf.Sin(phase * Mathf.PI) * 0.70f;
                break;
            case "JUMP":
                x = Mathf.Lerp(start.x, finish.x, Smooth(phase));
                z = Mathf.Lerp(start.z, finish.z, Smooth(phase));
                y = Mathf.Max(0f, Mathf.Sin(Mathf.Clamp01((phase - 0.28f) / 0.55f)
                    * Mathf.PI)) * 1.18f;
                break;
            case "WARP":
                if (phase < 0.42f)
                {
                    x = Mathf.Lerp(start.x, -0.70f, Smooth(phase / 0.42f));
                    z = Mathf.Lerp(start.z, -0.20f, Smooth(phase / 0.42f));
                }
                else if (phase < 0.56f)
                {
                    x = -0.70f;
                    z = -0.20f;
                    showPuck = false;
                }
                else
                {
                    float t = Smooth((phase - 0.56f) / 0.44f);
                    x = Mathf.Lerp(0.78f, finish.x, t);
                    z = Mathf.Lerp(0.20f, finish.z, t);
                }
                PulsePortal(_portalA, elapsed);
                PulsePortal(_portalB, elapsed + 0.7f);
                break;
        }

        if (_goalPulse != null)
        {
            float distance = Vector2.Distance(new Vector2(x, z),
                new Vector2(_goal.localPosition.x, _goal.localPosition.z));
            float glow = 1f + Mathf.Clamp01(1f - distance * 2.4f) *
                (0.10f + Mathf.Sin(elapsed * 10f) * 0.035f);
            _goalPulse.localScale = Vector3.one * glow;
        }

        if (_impactFlash != null)
        {
            _impactFlash.gameObject.SetActive(impact > 0.01f);
            float flashScale = Mathf.Lerp(0.18f, 0.82f,
                Mathf.Sin(Mathf.Clamp01(impact) * Mathf.PI * 0.5f));
            _impactFlash.localScale = Vector3.one * flashScale;
        }

        _puck.gameObject.SetActive(showPuck);
        if (_puckTrail != null) _puckTrail.emitting = showPuck;
        _puck.localPosition = new Vector3(x, y + Mathf.Sin(elapsed * 5.5f) * 0.025f, z);
        _puck.localScale = new Vector3(1.06f / squash, squash, 1.06f / squash);
        _puck.localRotation = Quaternion.Euler(0f, elapsed * 145f, 0f);
    }

    void EnsureBuilt()
    {
        if (_ready) return;
        if (_image == null) _image = GetComponent<RawImage>();
        if (_image == null) return;

        // Widescreen because the tutorial now occupies the whole instruction
        // board rather than a small picture-in-picture panel.
        _renderTexture = new RenderTexture(1280, 640, 24, RenderTextureFormat.ARGB32)
        {
            name = "BucaKidMechanicPreview3D",
            antiAliasing = 4,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        _renderTexture.Create();
        _image.texture = _renderTexture;
        _image.color = Color.white;
        _image.raycastTarget = false;

        _stageRoot = new GameObject("Buca Kid Mechanic Preview Stage")
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        _stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);

        GameObject cameraGo = new GameObject("Kid Preview Camera", typeof(Camera))
        {
            hideFlags = HideFlags.DontSave,
            layer = PreviewLayer
        };
        cameraGo.transform.SetParent(_stageRoot.transform, false);
        cameraGo.transform.localPosition = new Vector3(0f, 5.20f, -6.80f);
        cameraGo.transform.localRotation = Quaternion.LookRotation(
            new Vector3(0f, -4.92f, 6.84f), Vector3.up);
        _camera = cameraGo.GetComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = 1 << PreviewLayer;
        _camera.fieldOfView = 28f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 30f;
        _camera.allowHDR = true;
        _camera.allowMSAA = true;
        _camera.targetTexture = _renderTexture;

        _darkMaterial = CreateMaterial("Kid Preview Floor",
            new Color(0.012f, 0.030f, 0.060f, 1f), 0.20f, 0.62f,
            new Color(0.002f, 0.008f, 0.016f, 1f));
        _whiteMaterial = CreateMaterial("Kid Preview Eye White",
            new Color(0.94f, 0.98f, 1f, 1f), 0.05f, 0.74f, Color.black);
        _blackMaterial = CreateMaterial("Kid Preview Eye Black",
            new Color(0.005f, 0.008f, 0.012f, 1f), 0.15f, 0.68f, Color.black);
        _goldMaterial = CreateMaterial("Kid Preview Gold",
            new Color(1f, 0.56f, 0.09f, 1f), 0.88f, 0.86f,
            new Color(0.12f, 0.035f, 0.002f, 1f));
        _woodMaterial = CreateWoodMaterial();
        _railMaterial = CreateMaterial("Kid Preview Raised Rails",
            new Color(0.20f, 0.075f, 0.024f, 1f), 0.26f, 0.70f,
            new Color(0.020f, 0.006f, 0.001f, 1f));
        _laneMaterial = CreateMaterial("Kid Preview Lane Glow",
            new Color(0.06f, 0.78f, 1f, 1f), 0.64f, 0.90f,
            new Color(0.04f, 0.72f, 1f, 1f));
        _accentMaterial = CreateMaterial("Kid Preview Accent",
            new Color(0.16f, 0.88f, 1f, 1f), 0.64f, 0.88f,
            new Color(0.02f, 0.26f, 0.34f, 1f));
        _puckEdgeMaterial = CreateMaterial("Kid Preview Puck Edge",
            new Color(0.32f, 0.88f, 1f, 1f), 0.82f, 0.92f,
            new Color(0.01f, 0.18f, 0.24f, 1f));
        _puckFaceMaterial = CreateMaterial("Kid Preview Puck Face",
            new Color(0.58f, 0.80f, 1f, 1f), 0.22f, 0.86f,
            new Color(0.01f, 0.05f, 0.08f, 1f));

        _demoRoot = new GameObject("Animated Demo").transform;
        _demoRoot.gameObject.hideFlags = HideFlags.DontSave;
        _demoRoot.gameObject.layer = PreviewLayer;
        _demoRoot.SetParent(_stageRoot.transform, false);

        BuildMiniCourse();

        BuildPuckMascot();
        AddLights();
        _ready = true;
        RebuildMechanicProps();
        SetVisible(false);
    }

    /// <summary>
    /// Builds a real miniature Buca course instead of an abstract theatre.
    /// Its short landscape layout is intentionally unique, so it teaches the
    /// mechanic without copying or spoiling any scored level.
    /// </summary>
    void BuildMiniCourse()
    {
        Transform tableBase = CreatePrimitive("Tutorial Table Base", PrimitiveType.Cube,
            _demoRoot, _railMaterial, new Vector3(0f, -0.30f, 0.04f),
            new Vector3(6.46f, 0.28f, 3.78f));
        tableBase.GetComponent<Renderer>().receiveShadows = true;

        Transform floor = CreatePrimitive("Tutorial Wood Playfield", PrimitiveType.Cube,
            _demoRoot, _woodMaterial, new Vector3(0f, -0.10f, 0.04f),
            new Vector3(5.94f, 0.12f, 3.30f));
        floor.GetComponent<Renderer>().receiveShadows = true;

        // Thick raised rails give the preview the same physical tabletop
        // language as the game while keeping this course's layout original.
        CreatePrimitive("Top Raised Rail", PrimitiveType.Cube, _demoRoot,
            _railMaterial, new Vector3(0f, 0.20f, 1.69f),
            new Vector3(6.20f, 0.30f, 0.20f));
        CreatePrimitive("Bottom Raised Rail", PrimitiveType.Cube, _demoRoot,
            _railMaterial, new Vector3(0f, 0.20f, -1.61f),
            new Vector3(6.20f, 0.30f, 0.20f));
        CreatePrimitive("Left Raised Rail", PrimitiveType.Cube, _demoRoot,
            _railMaterial, new Vector3(-3.00f, 0.20f, 0.04f),
            new Vector3(0.20f, 0.30f, 3.20f));
        CreatePrimitive("Right Raised Rail", PrimitiveType.Cube, _demoRoot,
            _railMaterial, new Vector3(3.00f, 0.20f, 0.04f),
            new Vector3(0.20f, 0.30f, 3.20f));

        // Recessed cyan strips trace the playable area and make the depth
        // readable even on a small arcade screen.
        CreatePrimitive("Top Lane Light", PrimitiveType.Cube, _demoRoot,
            _laneMaterial, new Vector3(0f, 0.365f, 1.53f),
            new Vector3(5.72f, 0.026f, 0.035f));
        CreatePrimitive("Bottom Lane Light", PrimitiveType.Cube, _demoRoot,
            _laneMaterial, new Vector3(0f, 0.365f, -1.45f),
            new Vector3(5.72f, 0.026f, 0.035f));
        CreatePrimitive("Left Lane Light", PrimitiveType.Cube, _demoRoot,
            _laneMaterial, new Vector3(-2.84f, 0.365f, 0.04f),
            new Vector3(0.035f, 0.026f, 2.82f));
        CreatePrimitive("Right Lane Light", PrimitiveType.Cube, _demoRoot,
            _laneMaterial, new Vector3(2.84f, 0.365f, 0.04f),
            new Vector3(0.035f, 0.026f, 2.82f));

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                CreatePrimitive("Brass Corner", PrimitiveType.Sphere, _demoRoot,
                    _goldMaterial, new Vector3(x * 2.93f, 0.44f, 0.04f + z * 1.58f),
                    Vector3.one * 0.16f);
            }
        }

        // A dedicated launch marker makes the animated shot read instantly.
        CreatePrimitive("Launch Ring", PrimitiveType.Cylinder, _demoRoot,
            _laneMaterial, new Vector3(-2.30f, 0.025f, -0.83f),
            new Vector3(0.43f, 0.025f, 0.43f));
        CreatePrimitive("Launch Centre", PrimitiveType.Cylinder, _demoRoot,
            _darkMaterial, new Vector3(-2.30f, 0.055f, -0.83f),
            new Vector3(0.31f, 0.025f, 0.31f));

        _goal = new GameObject("Tutorial Goal").transform;
        _goal.gameObject.hideFlags = HideFlags.DontSave;
        _goal.gameObject.layer = PreviewLayer;
        _goal.SetParent(_demoRoot, false);
        _goal.localPosition = new Vector3(2.18f, 0f, 0.82f);

        _goalPulse = new GameObject("Goal Celebration Pulse").transform;
        _goalPulse.gameObject.hideFlags = HideFlags.DontSave;
        _goalPulse.gameObject.layer = PreviewLayer;
        _goalPulse.SetParent(_goal, false);

        CreatePrimitive("Goal Brass Plate", PrimitiveType.Cylinder, _goalPulse,
            _goldMaterial, new Vector3(0f, 0.035f, 0f),
            new Vector3(0.61f, 0.035f, 0.61f));
        CreatePrimitive("Goal Cyan Ring", PrimitiveType.Cylinder, _goalPulse,
            _laneMaterial, new Vector3(0f, 0.075f, 0f),
            new Vector3(0.49f, 0.025f, 0.49f));
        CreatePrimitive("Goal Hole", PrimitiveType.Cylinder, _goalPulse,
            _blackMaterial, new Vector3(0f, 0.105f, 0f),
            new Vector3(0.32f, 0.026f, 0.32f));

        // Two small angled bumpers make the course clearly different from
        // Level 2. The featured mechanic is rebuilt between these landmarks.
        Transform bumperA = CreatePrimitive("Tutorial Bumper A", PrimitiveType.Cube,
            _demoRoot, _goldMaterial, new Vector3(-0.94f, 0.25f, 0.70f),
            new Vector3(0.86f, 0.22f, 0.13f));
        bumperA.localRotation = Quaternion.Euler(0f, 22f, 0f);
        Transform bumperB = CreatePrimitive("Tutorial Bumper B", PrimitiveType.Cube,
            _demoRoot, _goldMaterial, new Vector3(0.72f, 0.25f, -0.70f),
            new Vector3(0.78f, 0.22f, 0.13f));
        bumperB.localRotation = Quaternion.Euler(0f, -28f, 0f);
    }

    void BuildPuckMascot()
    {
        _puck = new GameObject("Happy Puck").transform;
        _puck.gameObject.hideFlags = HideFlags.DontSave;
        _puck.gameObject.layer = PreviewLayer;
        _puck.SetParent(_demoRoot, false);

        CreatePrimitive("Puck Rim", PrimitiveType.Cylinder, _puck,
            _puckEdgeMaterial, new Vector3(0f, 0.20f, 0f),
            new Vector3(0.64f, 0.15f, 0.64f));
        CreatePrimitive("Puck Face", PrimitiveType.Cylinder, _puck,
            _puckFaceMaterial, new Vector3(0f, 0.34f, 0f),
            new Vector3(0.53f, 0.055f, 0.53f));

        _puckTrail = _puck.gameObject.AddComponent<TrailRenderer>();
        _puckTrail.sharedMaterial = _laneMaterial;
        _puckTrail.time = 0.72f;
        _puckTrail.minVertexDistance = 0.035f;
        _puckTrail.startWidth = 0.18f;
        _puckTrail.endWidth = 0.025f;
        _puckTrail.numCornerVertices = 5;
        _puckTrail.numCapVertices = 5;
        _puckTrail.alignment = LineAlignment.View;
        _puckTrail.textureMode = LineTextureMode.Stretch;
        _puckTrail.shadowCastingMode = ShadowCastingMode.Off;
        _puckTrail.receiveShadows = false;

        CreateEye(-0.18f);
        CreateEye(0.18f);
        CreatePrimitive("Smile", PrimitiveType.Cube, _puck, _blackMaterial,
            new Vector3(0f, 0.48f, -0.24f), new Vector3(0.22f, 0.025f, 0.055f));
    }

    void CreateEye(float x)
    {
        CreatePrimitive("Eye White", PrimitiveType.Sphere, _puck, _whiteMaterial,
            new Vector3(x, 0.49f, -0.16f), Vector3.one * 0.15f);
        CreatePrimitive("Eye Pupil", PrimitiveType.Sphere, _puck, _blackMaterial,
            new Vector3(x, 0.54f, -0.205f), Vector3.one * 0.072f);
    }

    void RebuildMechanicProps()
    {
        for (int i = 0; i < _temporaryProps.Count; i++)
        {
            if (_temporaryProps[i] == null) continue;
            _temporaryProps[i].SetActive(false);
            Destroy(_temporaryProps[i]);
        }
        _temporaryProps.Clear();
        _tokens.Clear();
        _movingProp = null;
        _portalA = null;
        _portalB = null;
        _impactFlash = null;

        switch (_kind)
        {
            case "MOVE":
                // The real target on the miniature course moves in Tick().
                break;
            case "MUD":
            case "ICE":
                CreatePad(_kind == "MUD" ? 0.88f : 0.40f);
                break;
            case "FAST":
            case "PUSH":
            case "WIND":
                CreatePad(0.50f);
                CreateArrows();
                break;
            case "+PTS":
                for (int i = 0; i < 3; i++)
                {
                    Transform token = CreatePrimitive("Bonus Pop", PrimitiveType.Sphere,
                        _demoRoot, _goldMaterial,
                        new Vector3(-0.72f + i * 0.72f, 0.34f, -0.25f + i * 0.28f),
                        Vector3.one * 0.20f);
                    _tokens.Add(token);
                    _temporaryProps.Add(token.gameObject);
                }
                break;
            case "!":
                for (int i = 0; i < 3; i++)
                {
                    Transform hazard = CreatePrimitive("Silly Pink Trap", PrimitiveType.Cylinder,
                        _demoRoot, _accentMaterial,
                        new Vector3(-0.25f + i * 0.55f, 0.48f, (i - 1) * 0.34f),
                        new Vector3(0.20f, 0.48f, 0.20f));
                    _temporaryProps.Add(hazard.gameObject);
                }
                break;
            case "PULL":
                _movingProp = CreatePrimitive("Gravity Blob", PrimitiveType.Sphere,
                    _demoRoot, _accentMaterial, new Vector3(0.42f, 0.50f, 0f),
                    Vector3.one * 0.72f);
                _temporaryProps.Add(_movingProp.gameObject);
                break;
            case "JUMP":
                CreatePad(0.52f);
                CreateArrows();
                Transform jumpWall = CreatePrimitive("Jump Over Me", PrimitiveType.Cube,
                    _demoRoot, _goldMaterial, new Vector3(0.58f, 0.40f, 0.27f),
                    new Vector3(0.16f, 0.40f, 1.30f));
                _temporaryProps.Add(jumpWall.gameObject);
                break;
            case "KICK":
                _movingProp = CreatePrimitive("Kicker Bumper", PrimitiveType.Cylinder,
                    _demoRoot, _accentMaterial, new Vector3(0.15f, 0.42f, -0.12f),
                    new Vector3(0.58f, 0.42f, 0.58f));
                _temporaryProps.Add(_movingProp.gameObject);
                break;
            case "WARP":
                _portalA = CreateRing("Portal A", -0.70f, -0.20f, _accentMaterial);
                _portalB = CreateRing("Portal B", 0.78f, 0.20f, _goldMaterial);
                break;
            default:
                CreateBounceRail();
                break;
        }
    }

    void CreateBounceRail()
    {
        Transform rail = CreatePrimitive("Super Bounce Rail", PrimitiveType.Cube,
            _demoRoot, _accentMaterial, new Vector3(0.72f, 0.36f, 1.30f),
            new Vector3(1.36f, 0.36f, 0.16f));
        _temporaryProps.Add(rail.gameObject);
        _impactFlash = CreatePrimitive("Bounce Impact Flash", PrimitiveType.Sphere,
            _demoRoot, _goldMaterial, new Vector3(0.72f, 0.63f, 1.24f),
            Vector3.one * 0.18f);
        _impactFlash.gameObject.SetActive(false);
        _temporaryProps.Add(_impactFlash.gameObject);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Transform cap = CreatePrimitive("Rail Light", PrimitiveType.Sphere,
                _demoRoot, _goldMaterial,
                new Vector3(0.72f + sign * 0.68f, 0.45f, 1.30f),
                Vector3.one * 0.23f);
            _temporaryProps.Add(cap.gameObject);
        }
    }

    void CreatePad(float brightness)
    {
        Transform pad = CreatePrimitive("Magic Floor Patch", PrimitiveType.Cube,
            _demoRoot, _accentMaterial, new Vector3(0f, 0.035f, 0f),
            new Vector3(2.55f, 0.07f, 1.35f));
        pad.localScale = new Vector3(2.55f, 0.07f + brightness * 0.02f, 1.35f);
        _temporaryProps.Add(pad.gameObject);
    }

    void CreateArrows()
    {
        for (int i = 0; i < 3; i++)
        {
            Transform arrow = CreatePrimitive("Speed Arrow", PrimitiveType.Cube,
                _demoRoot, _goldMaterial, new Vector3(-0.72f + i * 0.72f, 0.14f, 0f),
                new Vector3(0.38f, 0.045f, 0.13f));
            arrow.localRotation = Quaternion.Euler(0f, -18f, 0f);
            _temporaryProps.Add(arrow.gameObject);
        }
    }

    Transform CreateRing(string name, float x, float z, Material material)
    {
        Transform ring = new GameObject(name).transform;
        ring.gameObject.hideFlags = HideFlags.DontSave;
        ring.gameObject.layer = PreviewLayer;
        ring.SetParent(_demoRoot, false);
        ring.localPosition = new Vector3(x, 0f, z);
        _temporaryProps.Add(ring.gameObject);
        for (int i = 0; i < 14; i++)
        {
            float angle = i * Mathf.PI * 2f / 14f;
            CreatePrimitive("Ring Light", PrimitiveType.Sphere, ring, material,
                new Vector3(Mathf.Cos(angle) * 0.48f, 0.28f,
                    Mathf.Sin(angle) * 0.48f), Vector3.one * 0.13f);
        }
        return ring;
    }

    void PulsePortal(Transform portal, float time)
    {
        if (portal == null) return;
        float pulse = 1f + Mathf.Sin(time * 5.5f) * 0.08f;
        portal.localScale = Vector3.one * pulse;
        portal.Rotate(Vector3.up, 55f * Time.unscaledDeltaTime, Space.Self);
    }

    void AddLights()
    {
        GameObject keyGo = new GameObject("Kid Preview Key Light", typeof(Light));
        keyGo.hideFlags = HideFlags.DontSave;
        keyGo.layer = PreviewLayer;
        keyGo.transform.SetParent(_stageRoot.transform, false);
        keyGo.transform.localPosition = new Vector3(-2.4f, 4.1f, -2.8f);
        Light key = keyGo.GetComponent<Light>();
        // A local point light cannot leak onto the real game board even if a
        // platform ignores per-light layer masks. This keeps every level at
        // the same exposure after the preview has been shown.
        key.type = LightType.Point;
        key.color = new Color(1f, 0.76f, 0.52f, 1f);
        key.intensity = 4.2f;
        key.range = 10.5f;
        key.cullingMask = 1 << PreviewLayer;
        key.shadows = LightShadows.Soft;

        GameObject fillGo = new GameObject("Kid Preview Cyan Fill", typeof(Light));
        fillGo.hideFlags = HideFlags.DontSave;
        fillGo.layer = PreviewLayer;
        fillGo.transform.SetParent(_stageRoot.transform, false);
        fillGo.transform.localPosition = new Vector3(2.8f, 3.5f, -1.6f);
        Light fill = fillGo.GetComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(0.12f, 0.82f, 1f, 1f);
        fill.intensity = 2.8f;
        fill.range = 8.5f;
        fill.cullingMask = 1 << PreviewLayer;
        fill.shadows = LightShadows.None;

        GameObject rimGo = new GameObject("Kid Preview Gold Rim", typeof(Light));
        rimGo.hideFlags = HideFlags.DontSave;
        rimGo.layer = PreviewLayer;
        rimGo.transform.SetParent(_stageRoot.transform, false);
        rimGo.transform.localPosition = new Vector3(0.4f, 4.6f, 2.7f);
        Light rim = rimGo.GetComponent<Light>();
        rim.type = LightType.Point;
        rim.color = new Color(1f, 0.56f, 0.24f, 1f);
        rim.intensity = 2.4f;
        rim.range = 9.5f;
        rim.cullingMask = 1 << PreviewLayer;
        rim.shadows = LightShadows.None;
    }

    Transform CreatePrimitive(string name, PrimitiveType type, Transform parent,
        Material material, Vector3 localPosition, Vector3 localScale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.hideFlags = HideFlags.DontSave;
        SetLayerRecursively(go, PreviewLayer);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return go.transform;
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
        SetMaterialColor(material, color, emission);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    Material CreateWoodMaterial()
    {
        Material material = CreateMaterial("Kid Preview Warm Wood",
            new Color(0.82f, 0.52f, 0.28f, 1f), 0.08f, 0.56f,
            new Color(0.020f, 0.006f, 0.001f, 1f));
        Texture2D wood = CreateWoodTexture();
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", wood);
            material.SetTextureScale("_BaseMap", new Vector2(2.8f, 1.7f));
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", wood);
            material.SetTextureScale("_MainTex", new Vector2(2.8f, 1.7f));
        }
        return material;
    }

    Texture2D CreateWoodTexture()
    {
        const int width = 192;
        const int height = 96;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
            name = "Kid Preview Tutorial Wood",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };

        var pixels = new Color[width * height];
        Color dark = new Color(0.20f, 0.055f, 0.018f, 1f);
        Color light = new Color(0.62f, 0.25f, 0.075f, 1f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.060f, y * 0.14f);
                float longGrain = Mathf.Sin(x * 0.23f +
                    Mathf.PerlinNoise(x * 0.018f, y * 0.045f) * 8f) * 0.10f;
                float plank = ((y / 24) & 1) == 0 ? 0.045f : -0.025f;
                float blend = Mathf.Clamp01(0.28f + grain * 0.58f + longGrain + plank);
                pixels[y * width + x] = Color.Lerp(dark, light, blend);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        _textures.Add(texture);
        return texture;
    }

    static void SetMaterialColor(Material material, Color color, Color emission)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    static float Smooth(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    static float ImpactPulse(float phase, float center, float width)
    {
        return Mathf.Clamp01(1f - Mathf.Abs(phase - center) / Mathf.Max(0.001f, width));
    }

    void OnDestroy()
    {
        if (_image != null) _image.texture = null;
        if (_renderTexture != null)
        {
            if (_renderTexture.IsCreated()) _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_stageRoot != null) Destroy(_stageRoot);
        for (int i = 0; i < _materials.Count; i++)
            if (_materials[i] != null) Destroy(_materials[i]);
        _materials.Clear();
        for (int i = 0; i < _textures.Count; i++)
            if (_textures[i] != null) Destroy(_textures[i]);
        _textures.Clear();
    }
}
#endif
