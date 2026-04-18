using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime one-shot setup helper. Drop this on any GameObject in the
/// Game scene (a new empty "Setup Helper" works fine) and right-click
/// the component header → "Setup Everything" to spawn and auto-wire:
///
///   - Wall-spark ParticleSystem    (under LevelManager, assigned to LM.wallSparkBurst)
///   - Death-burst ParticleSystem   (under LevelManager, assigned to LM.deathBurst)
///   - Puck preview LineRenderer    (child of Puck, assigned to PuckController.previewLine)
///   - Puck spin decal              (small dark cube child, assigned to PuckSpinDecal)
///   - Star rating UI               (3 Images at the top of GameHUD, assigned to LM.starImages)
///   - Drag arrow UI                (bottom of GameHUD, with DragArrowHint, assigned to LM.dragArrow)
///
/// Every step is also exposed as its own ContextMenu item so you can
/// spawn them one at a time if you prefer.
///
/// Safe to run multiple times — each spawner removes any previous copy
/// it created first so re-runs stay clean.
///
/// Once you've run it and everything is wired, you can delete this
/// component from the scene.
/// </summary>
[DisallowMultipleComponent]
public class BucaSetupHelper : MonoBehaviour
{
    [Header("Auto-found on setup (you can override)")]
    public LevelManager levelManager;
    public GameObject puck;
    public Canvas gameHud;

    // ═══════════════════════════════════════════════════════════
    // Main entry — spawns everything in the correct order
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("★ Setup Everything")]
    public void SetupEverything()
    {
        ResolveReferences();
        if (!CheckRefs()) return;

        SpawnWallSparkBurst();
        SpawnDeathBurst();
        AddPuckPreviewLine();
        AddPuckSpinDecal();
        AddPuckDynamics();
        AddPuckPowerArc();
        SpawnStarRatingUI();
        SpawnDragArrowUI();
        SpawnLiveScoreDisplay();
        SpawnGameCompletePanel();
        SpawnLevelCompletePanel();
        SpawnComboText();
        SpawnEdgeGlow();
        SpawnTutorialOverlay();
        SpawnTimerDisplay();
        SpawnLeaderboardPanel();

        MarkDirty();
        Debug.Log("[BucaSetupHelper] ✔ Setup complete. You can remove this helper now.");
    }

    [ContextMenu("Resolve references (auto-find LevelManager / Puck / GameHUD)")]
    public void ResolveReferences()
    {
        if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            if (puck == null && levelManager.puck != null) puck = levelManager.puck;
            if (gameHud == null)
            {
                var canvases = levelManager.GetComponentsInChildren<Canvas>(true);
                foreach (var c in canvases) if (c.name == "GameHUD") { gameHud = c; break; }
                if (gameHud == null && canvases.Length > 0) gameHud = canvases[0];
            }
        }
        if (puck == null)
        {
            var pc = FindFirstObjectByType<PuckController>();
            if (pc != null) puck = pc.gameObject;
        }
        Debug.Log($"[BucaSetupHelper] Refs → LevelManager:{(levelManager!=null)}  Puck:{(puck!=null)}  GameHUD:{(gameHud!=null)}");
    }

    bool CheckRefs()
    {
        if (levelManager == null) { Debug.LogError("[BucaSetupHelper] LevelManager not found. Assign it manually."); return false; }
        if (puck == null)         { Debug.LogError("[BucaSetupHelper] Puck not found. Assign it manually."); return false; }
        if (gameHud == null)      { Debug.LogWarning("[BucaSetupHelper] GameHUD canvas not found — UI items will be skipped."); }
        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // 1) Wall-spark burst — small yellow sparks on wall contact
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("1. Spawn Wall-Spark Burst")]
    public void SpawnWallSparkBurst()
    {
        ResolveReferences();
        if (levelManager == null) return;

        DestroyChildByName(levelManager.transform, "WallSparkBurst");
        var go = new GameObject("WallSparkBurst");
        go.transform.SetParent(levelManager.transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.25f; main.loop = false; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new Color(1f, 0.9f, 0.35f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.05f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.15f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = NewParticleMat(new Color(1f, 0.9f, 0.35f));

        levelManager.wallSparkBurst = ps;
        Debug.Log("[BucaSetupHelper] ✔ WallSparkBurst created and assigned to LevelManager.wallSparkBurst");
    }

    // ═══════════════════════════════════════════════════════════
    // 2) Death burst — red/pink explosion
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("2. Spawn Death Burst")]
    public void SpawnDeathBurst()
    {
        ResolveReferences();
        if (levelManager == null) return;

        DestroyChildByName(levelManager.transform, "DeathBurst");
        var go = new GameObject("DeathBurst");
        go.transform.SetParent(levelManager.transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.6f; main.loop = false; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startColor = new Color(1f, 0.25f, 0.45f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.3f, 0.5f), 0f),
                    new GradientColorKey(new Color(0.7f, 0.1f, 0.2f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.4f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = NewParticleMat(new Color(1f, 0.3f, 0.5f));

        levelManager.deathBurst = ps;
        Debug.Log("[BucaSetupHelper] ✔ DeathBurst created and assigned to LevelManager.deathBurst");
    }

    // ═══════════════════════════════════════════════════════════
    // 3) Puck preview LineRenderer (trajectory)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("3. Add Puck Preview LineRenderer")]
    public void AddPuckPreviewLine()
    {
        ResolveReferences();
        if (puck == null) return;

        DestroyChildByName(puck.transform, "PreviewLine");
        var go = new GameObject("PreviewLine");
        go.transform.SetParent(puck.transform, false);
        go.transform.localPosition = Vector3.zero;

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        // Thin + slightly tapered — reads as a path, not a laser.
        lr.startWidth = 0.14f;
        lr.endWidth = 0.09f;
        lr.numCapVertices = 6;
        lr.numCornerVertices = 4;
        lr.textureMode = LineTextureMode.Tile;
        lr.alignment = LineAlignment.View;
        lr.enabled = false;

        // Dotted yellow texture that tiles along the line → renders as a
        // series of evenly spaced dots instead of a solid ribbon.
        var tex = BuildDotStripTexture();
        var mat = NewUnlitTransparentMat(new Color(1f, 0.93f, 0.35f, 0.9f));
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        // Tile one dot-cycle per 0.35 world-units of line length.
        mat.mainTextureScale = new Vector2(3f, 1f);
        lr.sharedMaterial = mat;
        lr.startColor = new Color(1f, 0.93f, 0.35f, 1f);
        lr.endColor   = new Color(1f, 0.93f, 0.35f, 0.2f);

        var pc = puck.GetComponent<PuckController>();
        if (pc != null) pc.previewLine = lr;

        Debug.Log("[BucaSetupHelper] ✔ PreviewLine (dotted) created and assigned to PuckController.previewLine");
    }

    /// <summary>
    /// Horizontal strip with a single filled dot in the middle + padding
    /// on either side. Repeated-tiled along the LineRenderer, this
    /// produces a dotted-path effect that naturally distributes dots
    /// regardless of how long the line is.
    /// </summary>
    static Texture2D BuildDotStripTexture()
    {
        const int w = 64, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
        float rMax = h * 0.38f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // Only the middle third of the strip contains the dot;
            // outer two thirds are transparent → gaps between dots.
            bool inDotBand = x > w * 0.33f && x < w * 0.67f;
            float d = Vector2.Distance(new Vector2(x, y), c);
            float a = 0f;
            if (inDotBand)
            {
                // Soft-edged circle
                a = Mathf.Clamp01(1f - (d - (rMax - 2f)) / 3f);
            }
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    // ═══════════════════════════════════════════════════════════
    // 4) Puck spin decal (dark child cube)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("4. Add Puck Spin Decal")]
    public void AddPuckSpinDecal()
    {
        ResolveReferences();
        if (puck == null) return;

        DestroyChildByName(puck.transform, "SpinDecal");

        // A small darker-yellow SPHERE sitting flush on the puck's surface.
        // Reads as a subtle seam/glint that rotates with the puck rather
        // than a black sticker. Sphere → no hard edges, same curvature as
        // the puck, so it blends as a "rotational highlight" instead of
        // standing out as debris glued on top.
        var decal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        decal.name = "SpinDecal";
        DestroyImmediate(decal.GetComponent<Collider>());
        decal.transform.SetParent(puck.transform, false);
        // Pushed slightly outside the puck surface so it's visible from
        // the tilted camera; tiny size so it doesn't dominate the puck.
        decal.transform.localPosition = new Vector3(0f, 0.24f, 0.42f);
        decal.transform.localScale = Vector3.one * 0.22f;

        // Subtle darker amber — contrasts just enough with the neon yellow
        // puck to read as a rotating marker without looking like a blemish.
        var mr = decal.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader) { name = "SpinDecalMat" };
        Color c = new Color(0.85f, 0.5f, 0.05f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
        mr.sharedMaterial = mat;

        // Wire the component
        var spin = puck.GetComponent<PuckSpinDecal>();
        if (spin == null) spin = puck.AddComponent<PuckSpinDecal>();
        spin.decalRenderer = mr;
        spin.decalColor = c;

        Debug.Log("[BucaSetupHelper] ✔ SpinDecal (subtle amber) created + PuckSpinDecal wired");
    }

    // ═══════════════════════════════════════════════════════════
    // 5) Star rating UI (3 images top-right)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("5. Spawn Star Rating UI")]
    public void SpawnStarRatingUI()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "StarRating");
        var container = new GameObject("StarRating", typeof(RectTransform));
        container.transform.SetParent(gameHud.transform, false);
        var crt = (RectTransform)container.transform;
        crt.anchorMin = new Vector2(1f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-30f, -30f);
        crt.sizeDelta = new Vector2(360f, 90f);

        var starSprite = BuildStarSprite();
        var imgs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var starGO = new GameObject($"Star_{i+1}", typeof(RectTransform));
            starGO.transform.SetParent(container.transform, false);
            var rt = (RectTransform)starGO.transform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-i * 105f, 0f);
            rt.sizeDelta = new Vector2(90f, 90f);

            var img = starGO.AddComponent<Image>();
            img.sprite = starSprite;
            img.color = new Color(1f, 1f, 1f, 0.15f);
            img.raycastTarget = false;
            imgs[i] = img;
        }

        levelManager.starImages = imgs;
        Debug.Log("[BucaSetupHelper] ✔ StarRating UI created and assigned to LevelManager.starImages");
    }

    // ═══════════════════════════════════════════════════════════
    // 6) Drag arrow UI
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("6. Spawn Drag Arrow UI")]
    public void SpawnDragArrowUI()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "DragArrow");
        var arrowGO = new GameObject("DragArrow", typeof(RectTransform));
        arrowGO.transform.SetParent(gameHud.transform, false);
        var rt = (RectTransform)arrowGO.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 280f);
        rt.sizeDelta = new Vector2(110f, 110f);
        rt.localRotation = Quaternion.identity;

        var img = arrowGO.AddComponent<Image>();
        img.sprite = BuildChevronSprite();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;

        var hint = arrowGO.AddComponent<DragArrowHint>();
        hint.arrow = rt;
        hint.arrowGraphic = img;
        hint.slideDistance = 160f;
        hint.loopDuration = 1.15f;
        hint.peakAlpha = 0.9f;

        levelManager.dragArrow = hint;

        // Optional: hide the old text hint if present — arrow replaces it.
        if (levelManager.dragHint != null)
        {
            var c = levelManager.dragHint.color;
            c.a = 0f;
            levelManager.dragHint.color = c;
        }

        Debug.Log("[BucaSetupHelper] ✔ DragArrow created and assigned to LevelManager.dragArrow");
    }

    // ═══════════════════════════════════════════════════════════
    // 7) PuckDynamics — auto-wire trail/idle-glow/renderer refs
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("7. Add Puck Dynamics (speed-based effects + rotation)")]
    public void AddPuckDynamics()
    {
        ResolveReferences();
        if (puck == null) return;

        var pd = puck.GetComponent<PuckDynamics>();
        if (pd == null) pd = puck.AddComponent<PuckDynamics>();
        pd.trail = puck.GetComponent<TrailRenderer>();
        pd.puckRenderer = puck.GetComponent<MeshRenderer>();
        // Idle glow was built as a child ParticleSystem named "PuckIdleGlow"
        var glow = puck.transform.Find("PuckIdleGlow");
        if (glow != null) pd.idleGlow = glow.GetComponent<ParticleSystem>();
        Debug.Log("[BucaSetupHelper] ✔ PuckDynamics added + wired");
    }

    // ═══════════════════════════════════════════════════════════
    // 8) Game-complete panel — full-screen YOU WIN card
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("8. Spawn Game-Complete Panel")]
    public void SpawnGameCompletePanel()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "GameCompletePanel");

        // Root panel covering the whole canvas
        var rootGO = new GameObject("GameCompletePanel", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        var rootRt = (RectTransform)rootGO.transform;
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero; rootRt.offsetMax = Vector2.zero;

        var cg = rootGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        // Full-screen dim overlay
        var dimGO = new GameObject("Dim", typeof(RectTransform));
        dimGO.transform.SetParent(rootGO.transform, false);
        var drt = (RectTransform)dimGO.transform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        var dim = dimGO.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.02f, 0.08f, 0.72f);
        dim.raycastTarget = true;

        // Center card
        var cardGO = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(rootGO.transform, false);
        var crt = (RectTransform)cardGO.transform;
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(820f, 900f);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.04f, 0.14f, 0.95f);
        cardImg.raycastTarget = true;

        // Title
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(cardGO.transform, false);
        var trt = (RectTransform)titleGO.transform;
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -60f);
        trt.sizeDelta = new Vector2(760f, 220f);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "YOU WIN!";
        title.fontSize = 160;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(
            new Color(1f, 0.4f, 0.8f),
            new Color(0.6f, 0.3f, 1f),
            new Color(0.3f, 0.85f, 1f),
            new Color(1f, 0.85f, 0.3f));
        title.outlineWidth = 0.22f;
        title.outlineColor = new Color(0.1f, 0.02f, 0.18f, 1f);

        // Stats
        var statsGO = new GameObject("Stats", typeof(RectTransform));
        statsGO.transform.SetParent(cardGO.transform, false);
        var srt = (RectTransform)statsGO.transform;
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, 40f);
        srt.sizeDelta = new Vector2(760f, 220f);
        var stats = statsGO.AddComponent<TextMeshProUGUI>();
        stats.text = "TOTAL STROKES  0\nSTARS  0 / 0";
        stats.fontSize = 64;
        stats.fontStyle = FontStyles.Bold;
        stats.alignment = TextAlignmentOptions.Center;
        stats.color = new Color(1f, 1f, 1f, 0.9f);

        // Replay button
        var replayBtn = BuildDialogButton(cardGO.transform, "ReplayButton", "REPLAY",
            new Vector2(0f, -240f), new Color(1f, 0.3f, 0.65f));
        // Main menu button
        var menuBtn = BuildDialogButton(cardGO.transform, "MainMenuButton", "MAIN MENU",
            new Vector2(0f, -380f), new Color(0.25f, 0.85f, 1f));

        // Component
        var gc = rootGO.AddComponent<GameCompletePanel>();
        gc.group = cg;
        gc.card = crt;
        gc.titleText = title;
        gc.statsText = stats;
        gc.replayButton = replayBtn;
        gc.mainMenuButton = menuBtn;
        gc.mainMenuSceneName = "MainMenu";

        // Wire to LevelManager
        levelManager.gameCompletePanel = gc;

        // Hidden until Show() is called
        rootGO.SetActive(false);
        Debug.Log("[BucaSetupHelper] ✔ GameCompletePanel created and wired");
    }

    // ═══════════════════════════════════════════════════════════
    // 8a) Live score display — shown below STROKES in game HUD
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("8a. Spawn Live Score Display")]
    public void SpawnLiveScoreDisplay()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "LiveScore");
        var go = new GameObject("LiveScore", typeof(RectTransform));
        go.transform.SetParent(gameHud.transform, false);
        var rt = (RectTransform)go.transform;
        // Anchor top-left, just below the STROKES text (which is at -36)
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(36f, -100f);
        rt.sizeDelta = new Vector2(380f, 60f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "SCORE  0";
        tmp.fontSize = 40;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(1f, 0.9f, 0.35f, 0.9f);
        tmp.outlineWidth = 0.12f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        tmp.raycastTarget = false;

        var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(2f, -2f);

        levelManager.scoreDisplay = tmp;
        Debug.Log("[BucaSetupHelper] ✔ LiveScore display created and assigned");
    }

    // ═══════════════════════════════════════════════════════════
    // 8b) Level-complete panel — score breakdown card per level
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("8b. Spawn Level-Complete Panel")]
    public void SpawnLevelCompletePanel()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "LevelCompletePanel");

        // Root — covers full screen
        var rootGO = new GameObject("LevelCompletePanel", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        var rootRt = (RectTransform)rootGO.transform;
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero; rootRt.offsetMax = Vector2.zero;

        var cg = rootGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        // Dim overlay
        var dimGO = new GameObject("Dim", typeof(RectTransform));
        dimGO.transform.SetParent(rootGO.transform, false);
        StretchFull((RectTransform)dimGO.transform);
        var dim = dimGO.AddComponent<Image>();
        dim.color = new Color(0.03f, 0.02f, 0.06f, 0.78f);
        dim.raycastTarget = true;

        // Center card
        var cardGO = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(rootGO.transform, false);
        var crt = (RectTransform)cardGO.transform;
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(980f, 980f);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.035f, 0.1f, 0.95f);

        // ── Stars (3 images across the top) ──
        var starsContainer = new GameObject("Stars", typeof(RectTransform));
        starsContainer.transform.SetParent(cardGO.transform, false);
        var strt = (RectTransform)starsContainer.transform;
        strt.anchorMin = new Vector2(0.5f, 1f);
        strt.anchorMax = new Vector2(0.5f, 1f);
        strt.pivot = new Vector2(0.5f, 1f);
        strt.anchoredPosition = new Vector2(0f, -40f);
        strt.sizeDelta = new Vector2(360f, 100f);

        var starSprite = BuildStarSprite();
        var starImgs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var sGO = new GameObject($"Star_{i + 1}", typeof(RectTransform));
            sGO.transform.SetParent(starsContainer.transform, false);
            var srt = (RectTransform)sGO.transform;
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2((i - 1) * 110f, 0f);
            srt.sizeDelta = new Vector2(90f, 90f);
            var img = sGO.AddComponent<Image>();
            img.sprite = starSprite;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;
            starImgs[i] = img;
        }

        // ── Score lines ──
        float lineY = -170f;
        float lineStep = -68f;
        var baseT     = MakeScoreLine(cardGO.transform, "BaseLine",       lineY); lineY += lineStep;
        var timeT     = MakeScoreLine(cardGO.transform, "TimeBonusLine",   lineY); lineY += lineStep;
        var railT     = MakeScoreLine(cardGO.transform, "RailBonusLine",   lineY); lineY += lineStep;
        var strokeT   = MakeScoreLine(cardGO.transform, "StrokeBonusLine", lineY); lineY += lineStep;
        var comboT    = MakeScoreLine(cardGO.transform, "ComboLine",       lineY); lineY += lineStep;
        var dividerT  = MakeScoreLine(cardGO.transform, "DividerLine",     lineY); lineY += lineStep * 0.6f;
        lineY += lineStep;
        var totalT    = MakeScoreLine(cardGO.transform, "TotalLine",       lineY, 82);

        // ── Auto-advance text at the bottom ──
        lineY += lineStep * 1.5f;
        var advanceGO = new GameObject("AutoAdvance", typeof(RectTransform));
        advanceGO.transform.SetParent(cardGO.transform, false);
        var art = (RectTransform)advanceGO.transform;
        art.anchorMin = new Vector2(0.5f, 0f);
        art.anchorMax = new Vector2(0.5f, 0f);
        art.pivot = new Vector2(0.5f, 0f);
        art.anchoredPosition = new Vector2(0f, 35f);
        art.sizeDelta = new Vector2(900f, 70f);
        var advTmp = advanceGO.AddComponent<TextMeshProUGUI>();
        advTmp.text = "";
        advTmp.fontSize = 36;
        advTmp.fontStyle = FontStyles.Bold;
        advTmp.alignment = TextAlignmentOptions.Center;
        advTmp.color = new Color(1f, 1f, 1f, 0f);
        advTmp.raycastTarget = false;

        // ── Wire component ──
        var lcp = rootGO.AddComponent<LevelCompletePanel>();
        lcp.group = cg;
        lcp.card = crt;
        lcp.starImages = starImgs;
        lcp.baseText = baseT;
        lcp.timeBonusText = timeT;
        lcp.railBonusText = railT;
        lcp.strokeBonusText = strokeT;
        lcp.comboText = comboT;
        lcp.dividerText = dividerT;
        lcp.totalText = totalT;
        lcp.autoAdvanceText = advTmp;

        levelManager.levelCompletePanel = lcp;

        rootGO.SetActive(false);
        Debug.Log("[BucaSetupHelper] ✔ LevelCompletePanel created and assigned");
    }

    static TMP_Text MakeScoreLine(Transform parent, string name, float yPos, int fontSize = 56)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(900f, 65f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(1f, 1f, 1f, 0f);
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        // Monospace-ish via TMP character spacing for alignment
        tmp.characterSpacing = 2f;

        return tmp;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Button BuildDialogButton(Transform parent, string name, string label, Vector2 pos, Color baseCol)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(520f, 120f);

        var img = go.AddComponent<Image>();
        img.color = baseCol;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var trt = (RectTransform)textGO.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 72;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.18f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.75f);
        return btn;
    }

    // ═══════════════════════════════════════════════════════════
    // 9) Drag power arc (LineRenderer child of Puck)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("9. Add Puck Power Arc")]
    public void AddPuckPowerArc()
    {
        ResolveReferences();
        if (puck == null) return;

        DestroyChildByName(puck.transform, "PowerArc");
        var go = new GameObject("PowerArc");
        go.transform.SetParent(puck.transform, false);
        go.transform.localPosition = Vector3.zero;

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 6;
        lr.alignment = LineAlignment.View;
        lr.enabled = false;
        lr.sharedMaterial = NewUnlitTransparentMat(Color.white);

        var pc = puck.GetComponent<PuckController>();
        if (pc != null) pc.powerArc = lr;
        Debug.Log("[BucaSetupHelper] ✔ PowerArc created and assigned");
    }

    // ═══════════════════════════════════════════════════════════
    // 10) Floating combo text (HOLE IN ONE!, PERFECT!, NICE SAVE!)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("10. Spawn Combo Text")]
    public void SpawnComboText()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "ComboText");
        var go = new GameObject("ComboText", typeof(RectTransform));
        go.transform.SetParent(gameHud.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 260f);
        rt.sizeDelta = new Vector2(900f, 180f);

        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 140;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableVertexGradient = false;
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = new Color(0.1f, 0.02f, 0.18f, 1f);
        tmp.color = new Color(1f, 0.9f, 0.3f);
        tmp.raycastTarget = false;

        var combo = go.AddComponent<FloatingComboText>();
        combo.text = tmp;
        combo.group = cg;

        levelManager.comboText = combo;
        Debug.Log("[BucaSetupHelper] ✔ ComboText created and assigned");
    }

    // ═══════════════════════════════════════════════════════════
    // 11) Screen-edge neon glow (yellow + pink overlay)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("11. Spawn Edge Glow Overlay")]
    public void SpawnEdgeGlow()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "EdgeGlow");
        var rootGO = new GameObject("EdgeGlow", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        // Slot it in front of the flash overlay if possible — it's a
        // lighter effect so should sit "under" the solid flash.
        rootGO.transform.SetSiblingIndex(0);
        var rrt = (RectTransform)rootGO.transform;
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

        var edgeSprite = BuildEdgeGlowSprite();

        var yellow = new GameObject("YellowGlow", typeof(RectTransform));
        yellow.transform.SetParent(rootGO.transform, false);
        var yrt = (RectTransform)yellow.transform;
        yrt.anchorMin = Vector2.zero; yrt.anchorMax = Vector2.one;
        yrt.offsetMin = Vector2.zero; yrt.offsetMax = Vector2.zero;
        var yimg = yellow.AddComponent<Image>();
        yimg.sprite = edgeSprite;
        yimg.type = Image.Type.Simple;
        yimg.color = new Color(1f, 0.85f, 0.3f, 0f);
        yimg.raycastTarget = false;

        var pink = new GameObject("PinkGlow", typeof(RectTransform));
        pink.transform.SetParent(rootGO.transform, false);
        var prt = (RectTransform)pink.transform;
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        var pimg = pink.AddComponent<Image>();
        pimg.sprite = edgeSprite;
        pimg.type = Image.Type.Simple;
        pimg.color = new Color(1f, 0.3f, 0.65f, 0f);
        pimg.raycastTarget = false;

        var edge = rootGO.AddComponent<EdgeGlowEffect>();
        edge.yellowGlow = yimg;
        edge.pinkGlow = pimg;

        levelManager.edgeGlow = edge;
        Debug.Log("[BucaSetupHelper] ✔ EdgeGlow created and assigned");
    }

    // ═══════════════════════════════════════════════════════════
    // 12) Tutorial overlay (ghost puck + ghost hand + instruction)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("12. Spawn Tutorial Overlay")]
    public void SpawnTutorialOverlay()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "Tutorial");
        var rootGO = new GameObject("Tutorial", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        var rrt = (RectTransform)rootGO.transform;
        rrt.anchorMin = new Vector2(0.5f, 0.5f);
        rrt.anchorMax = new Vector2(0.5f, 0.5f);
        rrt.pivot = new Vector2(0.5f, 0.5f);
        rrt.anchoredPosition = new Vector2(0f, -250f);
        rrt.sizeDelta = new Vector2(500f, 600f);

        var cg = rootGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var circle = BuildCircleSprite();

        // Ghost puck
        var puckGO = new GameObject("GhostPuck", typeof(RectTransform));
        puckGO.transform.SetParent(rootGO.transform, false);
        var prt = (RectTransform)puckGO.transform;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = new Vector2(0f, 60f);
        prt.sizeDelta = new Vector2(110f, 110f);
        var puckImg = puckGO.AddComponent<Image>();
        puckImg.sprite = circle;
        puckImg.color = new Color(1f, 0.9f, 0.3f, 0.75f);
        puckImg.raycastTarget = false;

        // Ghost hand (downward chevron)
        var handGO = new GameObject("GhostHand", typeof(RectTransform));
        handGO.transform.SetParent(rootGO.transform, false);
        var hrt = (RectTransform)handGO.transform;
        hrt.anchorMin = new Vector2(0.5f, 0.5f);
        hrt.anchorMax = new Vector2(0.5f, 0.5f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(0f, -40f);
        hrt.sizeDelta = new Vector2(80f, 80f);
        var handImg = handGO.AddComponent<Image>();
        handImg.sprite = circle;
        handImg.color = new Color(1f, 1f, 1f, 0.85f);
        handImg.raycastTarget = false;

        // Instruction text
        var textGO = new GameObject("Instruction", typeof(RectTransform));
        textGO.transform.SetParent(rootGO.transform, false);
        var trt = (RectTransform)textGO.transform;
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, 0f);
        trt.sizeDelta = new Vector2(700f, 80f);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "PULL BACK";
        tmp.fontSize = 52;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.95f);
        tmp.outlineWidth = 0.18f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        tmp.raycastTarget = false;

        var tut = rootGO.AddComponent<TutorialController>();
        tut.group = cg;
        tut.ghostPuck = prt;
        tut.ghostHand = hrt;
        tut.instructionText = tmp;

        levelManager.tutorial = tut;
        Debug.Log("[BucaSetupHelper] ✔ Tutorial overlay created and assigned");
    }

    // ═══════════════════════════════════════════════════════════
    // 13) Timer display (countdown text + fill bar)
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("13. Spawn Timer Display")]
    public void SpawnTimerDisplay()
    {
        ResolveReferences();
        if (levelManager == null || gameHud == null) return;

        DestroyChildByName(gameHud.transform, "TimerDisplay");
        var rootGO = new GameObject("TimerDisplay", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        var rrt = (RectTransform)rootGO.transform;
        // Top-center, just below the level label
        rrt.anchorMin = new Vector2(0.5f, 1f);
        rrt.anchorMax = new Vector2(0.5f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0f, -130f);
        rrt.sizeDelta = new Vector2(500f, 70f);

        // Bar background — dark translucent strip
        var barBgGO = new GameObject("BarBg", typeof(RectTransform));
        barBgGO.transform.SetParent(rootGO.transform, false);
        var bgrt = (RectTransform)barBgGO.transform;
        bgrt.anchorMin = new Vector2(0f, 0f);
        bgrt.anchorMax = new Vector2(1f, 0.45f);
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;
        var barBg = barBgGO.AddComponent<Image>();
        barBg.color = new Color(0.08f, 0.04f, 0.12f, 0.7f);
        barBg.raycastTarget = false;

        // Bar fill — bright, horizontal filled
        var barGO = new GameObject("BarFill", typeof(RectTransform));
        barGO.transform.SetParent(rootGO.transform, false);
        var brt = (RectTransform)barGO.transform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0.45f);
        brt.offsetMin = new Vector2(2f, 2f);
        brt.offsetMax = new Vector2(-2f, -2f);
        var barFill = barGO.AddComponent<Image>();
        barFill.color = new Color(1f, 1f, 1f, 0.9f);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = 0; // left to right
        barFill.fillAmount = 1f;
        barFill.raycastTarget = false;

        // Timer text — shows seconds remaining
        var textGO = new GameObject("TimerText", typeof(RectTransform));
        textGO.transform.SetParent(rootGO.transform, false);
        var trt = (RectTransform)textGO.transform;
        trt.anchorMin = new Vector2(0f, 0.4f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "30";
        tmp.fontSize = 42;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.9f);
        tmp.outlineWidth = 0.12f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        tmp.raycastTarget = false;

        // Wire component
        var td = rootGO.AddComponent<TimerDisplay>();
        td.timerText = tmp;
        td.timerBar = barFill;
        td.timerBarBg = barBg;

        levelManager.timerDisplay = td;
        Debug.Log("[BucaSetupHelper] ✔ TimerDisplay created and assigned to LevelManager.timerDisplay");
    }

    // ═══════════════════════════════════════════════════════════
    // 14) Leaderboard panel — shown after death/time-up before Continue
    // ═══════════════════════════════════════════════════════════
    [Tooltip("How many rows to show in the leaderboard. Set before spawning.")]
    public int leaderboardRowCount = 8;

    [ContextMenu("14. Spawn Leaderboard Panel")]
    public void SpawnLeaderboardPanel()
    {
        ResolveReferences();
        if (gameHud == null) return;

        DestroyChildByName(gameHud.transform, "LeaderboardPanel");

        // Root — full screen
        var rootGO = new GameObject("LeaderboardPanel", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        StretchFull((RectTransform)rootGO.transform);

        var cg = rootGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        // Dim background
        var dimGO = new GameObject("Dim", typeof(RectTransform));
        dimGO.transform.SetParent(rootGO.transform, false);
        StretchFull((RectTransform)dimGO.transform);
        var dim = dimGO.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.01f, 0.05f, 0.82f);
        dim.raycastTarget = true;

        // Center card
        var cardGO = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(rootGO.transform, false);
        var crt = (RectTransform)cardGO.transform;
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(820f, 1100f);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.03f, 0.12f, 0.96f);

        // Title
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(cardGO.transform, false);
        var trt = (RectTransform)titleGO.transform;
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -50f);
        trt.sizeDelta = new Vector2(780f, 120f);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "LEADERBOARD";
        title.fontSize = 88;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.5f, 0.8f),
            new Color(0.3f, 0.85f, 1f),
            new Color(1f, 0.85f, 0.3f));
        title.outlineWidth = 0.22f;
        title.outlineColor = new Color(0.1f, 0.02f, 0.18f, 1f);

        // "YOUR RANK #3 SCORE 1845"
        var myRankGO = new GameObject("MyRank", typeof(RectTransform));
        myRankGO.transform.SetParent(cardGO.transform, false);
        var mrrt = (RectTransform)myRankGO.transform;
        mrrt.anchorMin = new Vector2(0.5f, 1f);
        mrrt.anchorMax = new Vector2(0.5f, 1f);
        mrrt.pivot = new Vector2(0.5f, 1f);
        mrrt.anchoredPosition = new Vector2(0f, -175f);
        mrrt.sizeDelta = new Vector2(780f, 60f);
        var myRank = myRankGO.AddComponent<TextMeshProUGUI>();
        myRank.text = "YOUR RANK  #0   SCORE  0";
        myRank.fontSize = 42;
        myRank.fontStyle = FontStyles.Bold;
        myRank.alignment = TextAlignmentOptions.Center;
        myRank.color = new Color(1f, 0.9f, 0.3f, 0.95f);

        // Row container
        var rowsContainerGO = new GameObject("Rows", typeof(RectTransform));
        rowsContainerGO.transform.SetParent(cardGO.transform, false);
        var rcrt = (RectTransform)rowsContainerGO.transform;
        rcrt.anchorMin = new Vector2(0.5f, 1f);
        rcrt.anchorMax = new Vector2(0.5f, 1f);
        rcrt.pivot = new Vector2(0.5f, 1f);
        rcrt.anchoredPosition = new Vector2(0f, -260f);
        rcrt.sizeDelta = new Vector2(760f, 700f);

        // Build N rows
        float rowStartY = 0f;
        float rowHeight = 58f;
        float rowGap = 8f;
        var rows = new LeaderboardRow[Mathf.Max(1, leaderboardRowCount)];
        for (int i = 0; i < rows.Length; i++)
        {
            var rowGO = new GameObject($"Row_{i+1}", typeof(RectTransform));
            rowGO.transform.SetParent(rowsContainerGO.transform, false);
            var rrt = (RectTransform)rowGO.transform;
            rrt.anchorMin = new Vector2(0.5f, 1f);
            rrt.anchorMax = new Vector2(0.5f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, rowStartY - i * (rowHeight + rowGap));
            rrt.sizeDelta = new Vector2(760f, rowHeight);

            var rowTmp = rowGO.AddComponent<TextMeshProUGUI>();
            rowTmp.text = "";
            rowTmp.fontSize = 38;
            rowTmp.fontStyle = FontStyles.Bold;
            rowTmp.alignment = TextAlignmentOptions.Left;
            rowTmp.color = new Color(1f, 1f, 1f, 0f);
            rowTmp.raycastTarget = false;
            rowTmp.characterSpacing = 2f;

            var rowComp = rowGO.AddComponent<LeaderboardRow>();
            rowComp.rowText = rowTmp;
            rowComp.rectTransform = rrt;
            rows[i] = rowComp;
        }

        // Continue hint at bottom
        var hintGO = new GameObject("ContinueHint", typeof(RectTransform));
        hintGO.transform.SetParent(cardGO.transform, false);
        var hrt = (RectTransform)hintGO.transform;
        hrt.anchorMin = new Vector2(0.5f, 0f);
        hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 35f);
        hrt.sizeDelta = new Vector2(780f, 60f);
        var hintTmp = hintGO.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "";
        hintTmp.fontSize = 34;
        hintTmp.fontStyle = FontStyles.Bold;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = new Color(1f, 1f, 1f, 0f);
        hintTmp.raycastTarget = false;

        // Wire the panel component
        var panel = rootGO.AddComponent<LeaderboardPanel>();
        panel.group = cg;
        panel.card = crt;
        panel.titleText = title;
        panel.myRankText = myRank;
        panel.continueHintText = hintTmp;
        panel.rows = rows;

        // Find and assign to LuxoddGameBridge
        var bridge = FindFirstObjectByType<LuxoddGameBridge>();
        if (bridge != null)
        {
            bridge.leaderboardPanel = panel;
            Debug.Log("[BucaSetupHelper] ✔ LeaderboardPanel wired to LuxoddGameBridge.");
        }
        else
        {
            Debug.LogWarning("[BucaSetupHelper] LuxoddGameBridge not found in scene — " +
                             "assign leaderboardPanel manually in the Inspector.");
        }

        rootGO.SetActive(false);
        Debug.Log("[BucaSetupHelper] ✔ LeaderboardPanel created with " + rows.Length + " rows.");
    }

    // ══ Procedural helper sprites ══
    static Sprite BuildEdgeGlowSprite()
    {
        // Hollow radial: transparent in the middle, strong alpha at the edges.
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxD = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c) / maxD;
            d = Mathf.Clamp01(d);
            // Inverse smoothstep — opaque at edge (d→1), transparent near center.
            float a = Mathf.SmoothStep(0.55f, 1f, d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildCircleSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float r = size * 0.45f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            float a = Mathf.Clamp01((r - d) / 2f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════
    static void DestroyChildByName(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
    }

    static Material NewParticleMat(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Standard Unlit")
                  ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader) { name = "RuntimeParticleMat" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
        return mat;
    }

    static Material NewUnlitTransparentMat(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader) { name = "RuntimeUnlitMat" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return mat;
    }

    /// <summary>Procedural filled downward chevron (triangle) sprite.</summary>
    static Sprite BuildChevronSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // Downward-pointing triangle: widest at top, single pixel at bottom center.
            // Triangle inequality: |x - size/2| <= (size - y) * 0.5
            float halfWidth = (size - y) * 0.5f;
            bool inside = Mathf.Abs(x - size * 0.5f) <= halfWidth && y < size - 4;
            // Soft edge alpha
            float edge = halfWidth - Mathf.Abs(x - size * 0.5f);
            float a = inside ? Mathf.Clamp01(edge / 3f) : 0f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Procedural 5-point star sprite.</summary>
    static Sprite BuildStarSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float rOut = size * 0.46f;
        float rIn  = rOut * 0.44f;
        // Pre-compute 10 star vertices (5 outer + 5 inner, alternating)
        var verts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float r = (i % 2 == 0) ? rOut : rIn;
            float ang = -Mathf.PI / 2f + i * Mathf.PI / 5f;
            verts[i] = c + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool inside = PointInPolygon(new Vector2(x, y), verts);
            tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) /
                       (poly[j].y - poly[i].y + 1e-6f) + poly[i].x))
                inside = !inside;
            j = i;
        }
        return inside;
    }

    void MarkDirty()
    {
#if UNITY_EDITOR
        // So the scene shows the unsaved-changes asterisk and Ctrl+S will save.
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}
