using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A clearly readable 3D numbered billiard-ball timer in the former top-right Restart slot.
/// The ball turns red for the final five seconds.
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text timerText;

    [Header("BUCA HUD style")]
    [Tooltip("Bold arcade font shared by the timer and gameplay stats.")]
    public TMP_FontAsset hudFont;
    [Tooltip("Rounded bold font for the dimensional cyan-and-white score readout.")]
    public TMP_FontAsset statsFont;

    [Header("Final countdown")]
    public Color normalColor = new Color(0.10f, 0.88f, 1f, 1f);
    public Color criticalColor = new Color(1f, 0.25f, 0.35f, 1f);
    [Tooltip("The cue ball turns red and begins its gentle pulse at this many seconds.")]
    [Min(1f)] public float finalWarningSeconds = 5f;

    [Header("Animation")]
    [Tooltip("Peak scale boost on each new second. Low values feel subtle.")]
    public float tickScaleBoost = 0.08f;
    [Tooltip("Seconds for the tick bounce to fully decay.")]
    public float tickDecayTime = 0.35f;
    [Tooltip("How fast the display eases between phase colors + scales.")]
    public float smoothSpeed = 6f;

    float _maxTime;
    float _currentTime;
    int _lastDisplayedSecond = -1;
    int _lastTimeKey = int.MinValue;   // gates the per-frame timer-string rebuild

    // Smoothed runtime state — every frame we ease toward a target value
    // instead of recomputing from sin(time). This kills all visual snap.
    float _currentTickBoost;
    float _currentPulse = 1f;
    float _warningBlend;
    Vector3 _baseScale = Vector3.one;
    TMP_Text _combinedStatsText;
    PremiumStatsDisplay3D _premiumStats;
    RawImage _cueBallImage;
    GameObject _cueBallRig;
    Camera _cueBallCamera;
    RenderTexture _cueBallTexture;
    Material _ballMaterial;
    Material _numberPatchMaterial;
    Material _patchTrimMaterial;
    TextMeshPro _cueBallNumber;
    int _lastHudStrokes = int.MinValue;
    int _lastHudPar = int.MinValue;
    int _lastHudScore = int.MinValue;

    const int TimerLayer = 30; // Unused project layer; the isolated camera sees only this model.
    static readonly Vector3 CueRigPosition = new Vector3(1000f, 1000f, 1000f);
    static readonly Color BilliardBlue = new Color(0.12f, 0.72f, 0.94f, 1f);
    static readonly Color PatchIvory = new Color(0.99f, 0.97f, 0.91f, 1f);
    static readonly Color WarningRose = new Color(0.86f, 0.10f, 0.19f, 1f);

    void Awake()
    {
        // Older scenes still serialize the flat circle. Build the 3D model at
        // runtime so those scenes pick up the new timer without a scene rebuild.
        normalColor = new Color(0.10f, 0.88f, 1f, 1f);
        criticalColor = new Color(1f, 0.12f, 0.18f, 1f);
        finalWarningSeconds = 5f;
        tickScaleBoost = Mathf.Min(tickScaleBoost, 0.035f);
        ApplyPremiumColumnLayout();
        _baseScale = Vector3.one;
        _warningBlend = 0f;
    }

    public void Init(float maxTime)
    {
        // Re-apply the layout when a level starts.  This keeps older saved
        // scenes and every screen aspect ratio on the same clean HUD layout.
        ApplyPremiumColumnLayout();
        _maxTime = maxTime;
        _currentTime = maxTime;
        _lastDisplayedSecond = -1;
        _lastTimeKey = int.MinValue;
        _lastHudStrokes = int.MinValue;
        _lastHudPar = int.MinValue;
        _lastHudScore = int.MinValue;
        _currentTickBoost = 0f;
        _currentPulse = 1f;
        _warningBlend = 0f;
        UpdateVisuals();
        gameObject.SetActive(maxTime > 0f);
        if (_combinedStatsText != null)
            _combinedStatsText.gameObject.SetActive(maxTime > 0f && _premiumStats == null);
        if (_premiumStats != null) _premiumStats.SetVisible(maxTime > 0f);
    }

    public void SetTime(float remaining)
    {
        _currentTime = Mathf.Max(0f, remaining);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (_maxTime <= 0f || timerText == null) return;

        int displaySecond = Mathf.CeilToInt(_currentTime);
        bool finalCountdown = displaySecond <= Mathf.CeilToInt(finalWarningSeconds);
        float targetPulse = finalCountdown
            ? 1f + 0.025f * Mathf.Sin(Time.unscaledTime * 7f)
            : 1f;

        // ─── Ease smoothly toward targets ───────────────────────
        // Exponential smoothing frame-rate-independent via unscaled dt.
        float lerpT = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        _warningBlend = Mathf.Lerp(_warningBlend, finalCountdown ? 1f : 0f, lerpT);
        _currentPulse = Mathf.Lerp(_currentPulse, targetPulse, lerpT);

        // ─── Tick boost: gentle exponential decay ───────────────
        // Each second the integer ticks, add a small boost. Decay
        // exponentially so it fades organically instead of linearly.
        if (displaySecond != _lastDisplayedSecond && _lastDisplayedSecond >= 0 && _currentTime > 0f)
        {
            _currentTickBoost = Mathf.Max(_currentTickBoost, tickScaleBoost);
        }
        _lastDisplayedSecond = displaySecond;

        float tickLerp = 1f - Mathf.Exp(-(1f / Mathf.Max(0.01f, tickDecayTime)) * Time.unscaledDeltaTime);
        _currentTickBoost = Mathf.Lerp(_currentTickBoost, 0f, tickLerp);

        // ─── Apply to transform ─────────────────────────────────
        float finalScale = _currentPulse + _currentTickBoost;
        transform.localScale = _baseScale * finalScale;
        SetMaterialColor(_ballMaterial, Color.Lerp(BilliardBlue, WarningRose, _warningBlend));

        // ─── Text content ───────────────────────────────────────
        // Pad the number to a consistent visual width so changing
        // digits don't shift the text's center (a common source of
        // perceived jitter).
        // Rebuild the digits only when the shown second changes, not every
        // frame. Whole seconds give children a clear 30, 29, 28 ... sequence.
        int timeKey = displaySecond;
        LevelManager manager = LevelManager.Instance;
        int hudStrokes = manager != null ? manager.CurrentShotCount : 0;
        int hudPar = manager != null ? manager.CurrentPar : 1;
        int hudScore = manager != null ? manager.CurrentDisplayedScore : 0;
        bool statsChanged = hudStrokes != _lastHudStrokes
                         || hudPar != _lastHudPar
                         || hudScore != _lastHudScore;

        if (timeKey != _lastTimeKey)
        {
            _lastTimeKey = timeKey;
            timerText.text = displaySecond.ToString();
            if (_cueBallNumber != null)
            {
                _cueBallNumber.text = timerText.text;
                FitCueBallNumber();
            }
        }

        if (statsChanged)
        {
            _lastHudStrokes = hudStrokes;
            _lastHudPar = hudPar;
            _lastHudScore = hudScore;
            if (_combinedStatsText != null)
            {
                _combinedStatsText.text =
                    $"<color=#F8FEFF>STROKES</color><pos=260><color=#00E5F3>{hudStrokes}</color>\n" +
                    $"<color=#F8FEFF>PAR</color><pos=260><color=#FFC640>{hudPar}</color>\n" +
                    $"<color=#F8FEFF>SCORE</color><pos=260><color=#00E5F3>{hudScore:N0}</color>";
            }
            if (_premiumStats != null)
                _premiumStats.SetValues(hudStrokes, hudPar, hudScore);
        }
    }

    /// <summary>
    /// Keeps STROKES, PAR and SCORE in the top-left while the timer occupies
    /// the former Restart position in the opposite corner.
    /// </summary>
    void ApplyPremiumColumnLayout()
    {
        if (timerText == null) return;

        Transform hud = transform.parent;
        TMP_Text strokes = hud != null ? hud.Find("ShotCounter")?.GetComponent<TMP_Text>() : null;
        TMP_Text score = hud != null ? hud.Find("LiveScore")?.GetComponent<TMP_Text>() : null;
        // The old three-star strip competes with the new timer in the
        // top-right gameplay HUD. Keep level-map and result stars untouched.
        Transform gameplayStars = hud != null ? hud.Find("StarRating") : null;
        if (gameplayStars != null) gameplayStars.gameObject.SetActive(false);

        // Remove the rejected rail/wood chrome. The final HUD is one TMP block,
        // guaranteeing that the four rows can never drift out of order.
        Transform rejectedChrome = hud != null ? hud.Find("SelectedRailHudChrome") : null;
        if (rejectedChrome != null)
        {
            rejectedChrome.gameObject.SetActive(false);
            Destroy(rejectedChrome.gameObject);
        }

        _combinedStatsText = strokes;
        if (score != null) score.gameObject.SetActive(false);

        ConfigureTopLeft(strokes != null ? strokes.rectTransform : null,
            new Vector2(48f, -38f), new Vector2(465f, 224f));

        EnsurePremiumStats(hud, strokes);
        if (_combinedStatsText != null)
            _combinedStatsText.gameObject.SetActive(_premiumStats == null);
        EnsureCueBallTimer();

        ApplyTextStyle(strokes, 38f, true);
    }

    void EnsurePremiumStats(Transform hud, TMP_Text strokes)
    {
        if (hud == null || strokes == null) return;
        if (_premiumStats != null) return;

        Transform existing = hud.Find("Premium 3D Stats");
        GameObject statsObject = existing != null ? existing.gameObject
            : new GameObject("Premium 3D Stats", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage), typeof(PremiumStatsDisplay3D));
        if (existing == null) statsObject.transform.SetParent(hud, false);
        _premiumStats = statsObject.GetComponent<PremiumStatsDisplay3D>();
        TMP_FontAsset font = statsFont != null ? statsFont
            : (hudFont != null ? hudFont : strokes.font);
        if (_premiumStats == null || !_premiumStats.Initialize(font))
        {
            Debug.LogWarning("[TimerDisplay] 3D score lettering could not initialize; using text fallback.");
            if (existing == null) Destroy(statsObject);
            _premiumStats = null;
            return;
        }
        statsObject.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Centres the enlarged 3D cue ball where the old Restart button sat.
    /// The former button was 360 px wide at x=-32; its centre was x=-212.
    /// </summary>
    void EnsureCueBallTimer()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || timerText == null) return;

        root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(-212f, -108f);
        root.sizeDelta = new Vector2(188f, 188f);
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        // Retire any runtime children left by the old flat circular timer.
        foreach (string oldName in new[] { "Circle Back", "Circle Glow", "Circle Track", "Circle Progress" })
        {
            Transform oldShape = root.Find(oldName);
            if (oldShape != null) oldShape.gameObject.SetActive(false);
        }

        if (_cueBallImage == null)
        {
            Transform existing = root.Find("3D Cue Ball");
            GameObject imageObject = existing != null
                ? existing.gameObject
                : new GameObject("3D Cue Ball", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(RawImage));
            if (existing == null) imageObject.transform.SetParent(root, false);
            _cueBallImage = imageObject.GetComponent<RawImage>();
        }

        RectTransform ballRect = _cueBallImage.rectTransform;
        ballRect.anchorMin = ballRect.anchorMax = ballRect.pivot = new Vector2(0.5f, 0.5f);
        ballRect.anchoredPosition = Vector2.zero;
        ballRect.sizeDelta = new Vector2(188f, 188f);
        ballRect.localScale = Vector3.one;
        ballRect.localRotation = Quaternion.identity;
        _cueBallImage.color = Color.white;
        _cueBallImage.raycastTarget = false;

        if (_cueBallRig == null) BuildCueBallRig();
        _cueBallImage.texture = _cueBallTexture;
        _cueBallImage.gameObject.SetActive(_cueBallTexture != null);

        RectTransform timerRt = timerText.rectTransform;
        // A fixed centred rect is deliberate here. Some older scenes serialized
        // the legacy timer text with a top-half stretch anchor, which could leave
        // the number below the runtime circle even after its offsets were reset.
        timerRt.anchorMin = timerRt.anchorMax = new Vector2(0.5f, 0.5f);
        timerRt.pivot = new Vector2(0.5f, 0.5f);
        timerRt.anchoredPosition = Vector2.zero;
        timerRt.sizeDelta = new Vector2(150f, 150f);
        timerRt.localScale = Vector3.one;
        timerRt.localRotation = Quaternion.identity;
        // Keep the serialized UI text only as a safe fallback. The displayed
        // digits live on an actual 3D TextMeshPro object in the cue-ball rig.
        timerText.gameObject.SetActive(_cueBallTexture == null);
        timerText.transform.SetAsLastSibling();
    }

    void BuildCueBallRig()
    {
        _cueBallRig = new GameObject("Buca Timer Cue Ball 3D");
        _cueBallRig.transform.position = CueRigPosition;

        _cueBallTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
        {
            name = "Buca 3D Cue Ball Timer",
            antiAliasing = 4,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _cueBallTexture.Create();

        GameObject cameraObject = new GameObject("Cue Ball Preview Camera", typeof(Camera));
        cameraObject.transform.SetParent(_cueBallRig.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -3f);
        cameraObject.layer = TimerLayer;
        _cueBallCamera = cameraObject.GetComponent<Camera>();
        _cueBallCamera.orthographic = true;
        _cueBallCamera.orthographicSize = 0.98f;
        _cueBallCamera.nearClipPlane = 0.1f;
        _cueBallCamera.farClipPlane = 5f;
        _cueBallCamera.cullingMask = 1 << TimerLayer;
        _cueBallCamera.clearFlags = CameraClearFlags.SolidColor;
        _cueBallCamera.backgroundColor = Color.clear;
        _cueBallCamera.allowHDR = false;
        _cueBallCamera.allowMSAA = true;
        _cueBallCamera.targetTexture = _cueBallTexture;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? Shader.Find("Standard");
        if (lit == null)
        {
            Debug.LogError("[TimerDisplay] No Lit shader available for the 3D cue ball.");
            _cueBallCamera.targetTexture = null;
            Destroy(_cueBallRig);
            _cueBallRig = null;
            _cueBallTexture.Release();
            Destroy(_cueBallTexture);
            _cueBallTexture = null;
            return;
        }

        _ballMaterial = new Material(lit) { name = "Timer Billiard Ball Blue" };
        _numberPatchMaterial = new Material(lit) { name = "Timer Billiard Ball Number Circle" };
        _patchTrimMaterial = new Material(lit) { name = "Timer Billiard Ball Circle Edge" };
        SetMaterialColor(_ballMaterial, BilliardBlue);
        SetMaterialColor(_numberPatchMaterial, PatchIvory);
        SetMaterialColor(_patchTrimMaterial, new Color(0.01f, 0.12f, 0.24f, 1f));
        SetMaterialSmoothness(_ballMaterial, 0.62f);
        SetMaterialSmoothness(_numberPatchMaterial, 0.28f);
        SetMaterialSmoothness(_patchTrimMaterial, 0.35f);
        if (_numberPatchMaterial.HasProperty("_EmissionColor"))
        {
            _numberPatchMaterial.EnableKeyword("_EMISSION");
            _numberPatchMaterial.SetColor("_EmissionColor", PatchIvory * 0.3f);
        }
        // Unity's Metal and WebGL back-face conventions can differ for a
        // procedural cap. Paint must show from the isolated timer camera.
        if (_numberPatchMaterial.HasProperty("_Cull")) _numberPatchMaterial.SetFloat("_Cull", 0f);
        if (_patchTrimMaterial.HasProperty("_Cull")) _patchTrimMaterial.SetFloat("_Cull", 0f);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Real 3D Billiard Ball";
        sphere.transform.SetParent(_cueBallRig.transform, false);
        sphere.transform.localScale = Vector3.one * 1.55f;
        sphere.layer = TimerLayer;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = _ballMaterial;
        Destroy(sphere.GetComponent<Collider>());

        // Number circles on billiard balls are painted onto the curved shell.
        // Two shallow spherical caps read as paint and trim, not a floating puck.
        CreateCurvedNumberCircle("Number Circle Edge 3D", 0.61f, 0.007f, _patchTrimMaterial);
        CreateCurvedNumberCircle("Number Circle Ivory 3D", 0.56f, 0.04f, _numberPatchMaterial);

        GameObject numberObject = new GameObject("Seconds in 3D", typeof(TextMeshPro));
        numberObject.transform.SetParent(_cueBallRig.transform, false);
        numberObject.transform.localPosition = new Vector3(0f, 0f, -0.840f);
        numberObject.layer = TimerLayer;
        _cueBallNumber = numberObject.GetComponent<TextMeshPro>();
        _cueBallNumber.font = hudFont != null ? hudFont : timerText.font;
        _cueBallNumber.fontSize = 10f;
        _cueBallNumber.alignment = TextAlignmentOptions.Center;
        _cueBallNumber.fontStyle = FontStyles.Bold;
        _cueBallNumber.textWrappingMode = TextWrappingModes.NoWrap;
        _cueBallNumber.color = new Color(0.015f, 0.015f, 0.018f, 1f);
        _cueBallNumber.text = "27";
        FitCueBallNumber();

        GameObject lightObject = new GameObject("Soft Cue Ball Light", typeof(Light));
        lightObject.transform.SetParent(_cueBallRig.transform, false);
        lightObject.transform.localPosition = new Vector3(-0.7f, 0.9f, -1.6f);
        lightObject.layer = TimerLayer;
        Light keyLight = lightObject.GetComponent<Light>();
        keyLight.type = LightType.Point;
        keyLight.color = new Color(1f, 0.96f, 0.87f, 1f);
        keyLight.intensity = 1.55f;
        keyLight.range = 4f;
        keyLight.cullingMask = 1 << TimerLayer;
        keyLight.shadows = LightShadows.None;
    }

    void CreateCurvedNumberCircle(string objectName, float radius, float surfaceOffset, Material material)
    {
        const int segments = 48;
        const int rings = 6;
        const float ballRadius = 0.775f;
        Vector3[] vertices = new Vector3[1 + segments * rings];
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = new int[segments * (1 + (rings - 1) * 2) * 3];
        vertices[0] = new Vector3(0f, 0f, -ballRadius - surfaceOffset);
        normals[0] = Vector3.back;

        for (int ring = 1; ring <= rings; ring++)
        {
            float ringRadius = radius * ring / rings;
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle) * ringRadius;
                float y = Mathf.Sin(angle) * ringRadius;
                float z = -Mathf.Sqrt(ballRadius * ballRadius - ringRadius * ringRadius);
                int index = 1 + (ring - 1) * segments + segment;
                vertices[index] = new Vector3(x, y, z - surfaceOffset);
                normals[index] = new Vector3(x, y, z).normalized;
            }
        }

        int triangle = 0;
        for (int segment = 0; segment < segments; segment++)
        {
            int next = (segment + 1) % segments;
            triangles[triangle++] = 0;
            triangles[triangle++] = 1 + next;
            triangles[triangle++] = 1 + segment;
        }
        for (int ring = 1; ring < rings; ring++)
        {
            int inner = 1 + (ring - 1) * segments;
            int outer = inner + segments;
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                triangles[triangle++] = inner + segment;
                triangles[triangle++] = outer + next;
                triangles[triangle++] = outer + segment;
                triangles[triangle++] = inner + segment;
                triangles[triangle++] = inner + next;
                triangles[triangle++] = outer + next;
            }
        }

        Mesh mesh = new Mesh { name = objectName };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        GameObject circle = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        circle.transform.SetParent(_cueBallRig.transform, false);
        circle.layer = TimerLayer;
        circle.GetComponent<MeshFilter>().sharedMesh = mesh;
        circle.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    void FitCueBallNumber()
    {
        if (_cueBallNumber == null) return;
        _cueBallNumber.ForceMeshUpdate();
        Vector3 bounds = _cueBallNumber.textBounds.size;
        float scale = Mathf.Min(0.96f / Mathf.Max(0.01f, bounds.x),
            0.60f / Mathf.Max(0.01f, bounds.y));
        _cueBallNumber.transform.localScale = Vector3.one * scale;
    }

    static void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    static void SetMaterialSmoothness(Material material, float smoothness)
    {
        if (material == null) return;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
    }

    void OnEnable()
    {
        if (_cueBallCamera != null) _cueBallCamera.enabled = true;
    }

    void OnDisable()
    {
        if (_cueBallCamera != null) _cueBallCamera.enabled = false;
    }

    void OnDestroy()
    {
        if (_cueBallCamera != null) _cueBallCamera.targetTexture = null;
        if (_cueBallRig != null) Destroy(_cueBallRig);
        if (_cueBallTexture != null)
        {
            _cueBallTexture.Release();
            Destroy(_cueBallTexture);
        }
        if (_ballMaterial != null) Destroy(_ballMaterial);
        if (_numberPatchMaterial != null) Destroy(_numberPatchMaterial);
        if (_patchTrimMaterial != null) Destroy(_patchTrimMaterial);
    }

    void ApplyTextStyle(TMP_Text text, float size, bool twoLines)
    {
        if (text == null) return;

        // Oswald Bold gives the selected compact arcade/sports silhouette.
        TMP_FontAsset selectedFont = hudFont != null ? hudFont
            : (timerText != null ? timerText.font : null);
        if (selectedFont != null)
        {
            text.font = selectedFont;
            if (selectedFont.material != null)
                text.fontSharedMaterial = selectedFont.material;
        }

        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.characterSpacing = 1.2f;
        text.lineSpacing = twoLines ? 18f : 0f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = true;
        text.raycastTarget = false;
        text.color = Color.white;
        text.outlineWidth = 0.14f;
        text.outlineColor = new Color(0.005f, 0.015f, 0.025f, 0.96f);

        // Soft cyan underlay reproduces the restrained neon edge from the
        // selected mockup without making the glyphs fuzzy.
        Material mat = text.fontMaterial;
        if (mat != null && mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
        {
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor,
                new Color(0.08f, 0.78f, 1f, 0.18f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.22f);
        }
    }

    static void ConfigureTopLeft(RectTransform rt, Vector2 position, Vector2 size)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    public void Hide()
    {
        if (_combinedStatsText != null)
            _combinedStatsText.gameObject.SetActive(false);
        if (_premiumStats != null) _premiumStats.SetVisible(false);
        gameObject.SetActive(false);
    }
}
