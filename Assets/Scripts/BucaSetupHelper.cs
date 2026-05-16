using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
        SpawnControlHintBar();
        SpawnAudioManager();

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

        // ─── Arcade-mode visuals (joystick + Black button + power ring) ─
        // Hidden by default; TutorialController flips them on when arcade
        // input is detected. Keeps the existing mouse demo unchanged.

        // Joystick (base disc + tilting ball)
        var joyGO = new GameObject("GhostJoystick", typeof(RectTransform));
        joyGO.transform.SetParent(rootGO.transform, false);
        var joyRT = (RectTransform)joyGO.transform;
        joyRT.anchorMin = joyRT.anchorMax = joyRT.pivot = new Vector2(0.5f, 0.5f);
        joyRT.anchoredPosition = new Vector2(-110f, -50f);
        joyRT.sizeDelta = new Vector2(110f, 110f);

        var joyBaseGO = new GameObject("Base", typeof(RectTransform));
        joyBaseGO.transform.SetParent(joyGO.transform, false);
        var jbrt = (RectTransform)joyBaseGO.transform;
        jbrt.anchorMin = jbrt.anchorMax = jbrt.pivot = new Vector2(0.5f, 0.5f);
        jbrt.anchoredPosition = Vector2.zero;
        jbrt.sizeDelta = new Vector2(110f, 110f);
        var joyBaseImg = joyBaseGO.AddComponent<Image>();
        joyBaseImg.sprite = circle;
        joyBaseImg.color = new Color(0.20f, 0.20f, 0.25f, 0.85f);
        joyBaseImg.raycastTarget = false;

        var joyBallGO = new GameObject("Ball", typeof(RectTransform));
        joyBallGO.transform.SetParent(joyGO.transform, false);
        var jballRT = (RectTransform)joyBallGO.transform;
        jballRT.anchorMin = jballRT.anchorMax = jballRT.pivot = new Vector2(0.5f, 0.5f);
        jballRT.anchoredPosition = Vector2.zero;
        jballRT.sizeDelta = new Vector2(60f, 60f);
        var joyBallImg = joyBallGO.AddComponent<Image>();
        joyBallImg.sprite = circle;
        joyBallImg.color = new Color(0.95f, 0.25f, 0.25f, 1f);
        joyBallImg.raycastTarget = false;
        // Highlight dot on ball
        var hlGO = new GameObject("BallHighlight", typeof(RectTransform));
        hlGO.transform.SetParent(joyBallGO.transform, false);
        var hlRT = (RectTransform)hlGO.transform;
        hlRT.anchorMin = hlRT.anchorMax = hlRT.pivot = new Vector2(0.5f, 0.5f);
        hlRT.anchoredPosition = new Vector2(-10f, 10f);
        hlRT.sizeDelta = new Vector2(18f, 18f);
        var hl = hlGO.AddComponent<Image>();
        hl.sprite = circle;
        hl.color = new Color(1f, 1f, 1f, 0.6f);
        hl.raycastTarget = false;

        // Black button — solid dark circle that brightens when "pressed"
        var blkGO = new GameObject("GhostBlackButton", typeof(RectTransform));
        blkGO.transform.SetParent(rootGO.transform, false);
        var blkRT = (RectTransform)blkGO.transform;
        blkRT.anchorMin = blkRT.anchorMax = blkRT.pivot = new Vector2(0.5f, 0.5f);
        blkRT.anchoredPosition = new Vector2(110f, -50f);
        blkRT.sizeDelta = new Vector2(90f, 90f);
        var blkImg = blkGO.AddComponent<Image>();
        blkImg.sprite = circle;
        blkImg.color = new Color(0.18f, 0.18f, 0.22f, 1f);
        blkImg.raycastTarget = false;
        // White outline ring around the button
        var blkRingGO = new GameObject("Ring", typeof(RectTransform));
        blkRingGO.transform.SetParent(blkGO.transform, false);
        var blkRingRT = (RectTransform)blkRingGO.transform;
        blkRingRT.anchorMin = blkRingRT.anchorMax = blkRingRT.pivot = new Vector2(0.5f, 0.5f);
        blkRingRT.anchoredPosition = Vector2.zero;
        blkRingRT.sizeDelta = new Vector2(105f, 105f);
        var blkRing = blkRingGO.AddComponent<Image>();
        blkRing.sprite = circle;
        blkRing.color = new Color(0.85f, 0.85f, 0.95f, 0.7f);
        blkRing.raycastTarget = false;
        // Render ring behind the button
        blkRingGO.transform.SetSiblingIndex(0);

        // Power ring — radial-fill image around the puck that fills as charge builds
        var ringGO = new GameObject("GhostPowerRing", typeof(RectTransform));
        ringGO.transform.SetParent(rootGO.transform, false);
        var ringRT = (RectTransform)ringGO.transform;
        ringRT.anchorMin = ringRT.anchorMax = ringRT.pivot = new Vector2(0.5f, 0.5f);
        ringRT.anchoredPosition = prt.anchoredPosition; // sits over the puck
        ringRT.sizeDelta = new Vector2(150f, 150f);
        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite = BuildHollowDiscSprite();
        ringImg.color = new Color(1f, 0.85f, 0.30f, 0.95f);
        ringImg.type = Image.Type.Filled;
        ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillOrigin = (int)Image.Origin360.Top;
        ringImg.fillClockwise = true;
        ringImg.fillAmount = 0f;
        ringImg.raycastTarget = false;

        // Hidden by default — TutorialController toggles them on for arcade mode
        joyGO.SetActive(false);
        blkGO.SetActive(false);
        ringGO.SetActive(false);

        var tut = rootGO.AddComponent<TutorialController>();
        tut.group = cg;
        tut.ghostPuck = prt;
        tut.ghostHand = hrt;
        tut.instructionText = tmp;
        tut.ghostJoystick = joyRT;
        tut.ghostJoystickBall = jballRT;
        tut.ghostBlackButton = blkImg;
        tut.ghostPowerRing = ringImg;

        levelManager.tutorial = tut;
        Debug.Log("[BucaSetupHelper] ✔ Tutorial overlay created (mouse + arcade demos) and assigned");
    }

    /// <summary>Hollow ring sprite for the power-fill indicator.</summary>
    static Sprite BuildHollowDiscSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float rOut = size * 0.45f;
        float rIn = size * 0.30f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            float a = 0f;
            if (d <= rOut && d >= rIn)
                a = Mathf.Clamp01(Mathf.Min(rOut - d, d - rIn));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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

        // Timer text — shows seconds remaining, centered in the root.
        // No fill bar — the text's scale + color animations carry the
        // visual feedback on their own (see TimerDisplay).
        var textGO = new GameObject("TimerText", typeof(RectTransform));
        textGO.transform.SetParent(rootGO.transform, false);
        var trt = (RectTransform)textGO.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "30";
        tmp.fontSize = 56;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.95f);
        tmp.outlineWidth = 0.15f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        tmp.raycastTarget = false;

        // Wire component
        var td = rootGO.AddComponent<TimerDisplay>();
        td.timerText = tmp;

        levelManager.timerDisplay = td;
        Debug.Log("[BucaSetupHelper] ✔ TimerDisplay (text-only) created and assigned to LevelManager.timerDisplay");
    }

    // ═══════════════════════════════════════════════════════════
    // 14) Leaderboard panel — shown after death/time-up before Continue
    // ═══════════════════════════════════════════════════════════
    [Tooltip("How many rows to show in the leaderboard. Set before spawning.")]
    public int leaderboardRowCount = 10;

    [ContextMenu("14. Spawn Leaderboard Panel")]
    public void SpawnLeaderboardPanel()
    {
        ResolveReferences();
        if (gameHud == null) return;

        DestroyChildByName(gameHud.transform, "LeaderboardPanel");

        // ─────────────────────────────────────────────────────
        // Sprites — built once, reused per element
        // ─────────────────────────────────────────────────────
        var circleSprite  = BuildCircleSprite();
        var glowSprite    = BuildSoftGlowSprite();
        var roundedRectSp = BuildRoundedRectSprite(28);
        var thinStripeSp  = BuildVerticalGradientStripeSprite();

        // ─────────────────────────────────────────────────────
        // Root — full screen container with CanvasGroup for fade
        // ─────────────────────────────────────────────────────
        var rootGO = new GameObject("LeaderboardPanel", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        StretchFull((RectTransform)rootGO.transform);

        var cg = rootGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        // Full-screen dim
        var dimGO = new GameObject("Dim", typeof(RectTransform));
        dimGO.transform.SetParent(rootGO.transform, false);
        StretchFull((RectTransform)dimGO.transform);
        var dim = dimGO.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.01f, 0.05f, 0.86f);
        dim.raycastTarget = true;

        // ─────────────────────────────────────────────────────
        // Card — 820 × 1100, centered, rounded background
        // ─────────────────────────────────────────────────────
        var cardGO = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(rootGO.transform, false);
        var crt = (RectTransform)cardGO.transform;
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(820f, 1180f);

        // Glow halo BEHIND the card (large, soft circle that rotates)
        var haloGO = new GameObject("GlowHalo", typeof(RectTransform));
        haloGO.transform.SetParent(cardGO.transform, false);
        var halort = (RectTransform)haloGO.transform;
        halort.anchorMin = halort.anchorMax = halort.pivot = new Vector2(0.5f, 0.5f);
        halort.anchoredPosition = Vector2.zero;
        halort.sizeDelta = new Vector2(1300f, 1300f);
        var halo = haloGO.AddComponent<Image>();
        halo.sprite = glowSprite;
        halo.color = new Color(0.95f, 0.45f, 0.85f, 0.18f);
        halo.raycastTarget = false;

        // Card background fill
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.sprite = roundedRectSp;
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 1.0f;
        cardImg.color = new Color(0.07f, 0.04f, 0.16f, 0.97f);

        // Card border outline (slightly larger than card, pulses alpha)
        var borderGO = new GameObject("BorderOutline", typeof(RectTransform));
        borderGO.transform.SetParent(cardGO.transform, false);
        var brt = (RectTransform)borderGO.transform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.offsetMin = new Vector2(-6f, -6f);
        brt.offsetMax = new Vector2( 6f,  6f);
        var border = borderGO.AddComponent<Image>();
        border.sprite = roundedRectSp;
        border.type = Image.Type.Sliced;
        border.color = new Color(1f, 0.85f, 0.35f, 0.85f);
        border.raycastTarget = false;
        // Border is the OUTER box, render it first so card sits on top
        borderGO.transform.SetSiblingIndex(0);
        // Halo even further behind
        haloGO.transform.SetSiblingIndex(0);

        // ─────────────────────────────────────────────────────
        // Title bar
        // ─────────────────────────────────────────────────────
        var titleBarGO = new GameObject("TitleBar", typeof(RectTransform));
        titleBarGO.transform.SetParent(cardGO.transform, false);
        var tbrt = (RectTransform)titleBarGO.transform;
        tbrt.anchorMin = new Vector2(0.5f, 1f); tbrt.anchorMax = new Vector2(0.5f, 1f); tbrt.pivot = new Vector2(0.5f, 1f);
        tbrt.anchoredPosition = new Vector2(0f, -30f);
        tbrt.sizeDelta = new Vector2(780f, 130f);
        // Mask so the shimmer doesn't escape the title strip
        var titleMask = titleBarGO.AddComponent<RectMask2D>();

        // Shimmer streak — moves across the title via LeaderboardCardFX
        var shimmerGO = new GameObject("Shimmer", typeof(RectTransform));
        shimmerGO.transform.SetParent(titleBarGO.transform, false);
        var shrt = (RectTransform)shimmerGO.transform;
        shrt.anchorMin = shrt.anchorMax = shrt.pivot = new Vector2(0.5f, 0.5f);
        shrt.anchoredPosition = Vector2.zero;
        shrt.sizeDelta = new Vector2(160f, 130f);
        shrt.localRotation = Quaternion.Euler(0f, 0f, 18f);
        var shimmer = shimmerGO.AddComponent<Image>();
        shimmer.sprite = thinStripeSp;
        shimmer.color = new Color(1f, 1f, 1f, 0.18f);
        shimmer.raycastTarget = false;

        // Title text on top of the mask
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(titleBarGO.transform, false);
        var trt = (RectTransform)titleGO.transform;
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(780f, 130f);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "LEADERBOARD";
        title.fontSize = 88;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(
            new Color(1f, 0.92f, 0.35f),
            new Color(1f, 0.55f, 0.85f),
            new Color(0.35f, 0.85f, 1f),
            new Color(1f, 0.85f, 0.35f));
        title.outlineWidth = 0.24f;
        title.outlineColor = new Color(0.10f, 0.02f, 0.18f, 1f);
        title.raycastTarget = false;

        // Subtitle — your-rank line directly below title
        var myRankGO = new GameObject("MyRank", typeof(RectTransform));
        myRankGO.transform.SetParent(cardGO.transform, false);
        var mrrt = (RectTransform)myRankGO.transform;
        mrrt.anchorMin = new Vector2(0.5f, 1f); mrrt.anchorMax = new Vector2(0.5f, 1f); mrrt.pivot = new Vector2(0.5f, 1f);
        mrrt.anchoredPosition = new Vector2(0f, -180f);
        mrrt.sizeDelta = new Vector2(780f, 56f);
        var myRank = myRankGO.AddComponent<TextMeshProUGUI>();
        myRank.text = "YOUR RANK  #0   SCORE  0";
        myRank.fontSize = 38;
        myRank.fontStyle = FontStyles.Bold;
        myRank.alignment = TextAlignmentOptions.Center;
        myRank.color = new Color(1f, 0.9f, 0.35f, 0.95f);
        myRank.raycastTarget = false;

        // Header strip: RANK  PLAYER  SCORE
        BuildHeaderStrip(cardGO.transform);

        // ─────────────────────────────────────────────────────
        // Rows container + 10 rows
        // ─────────────────────────────────────────────────────
        var rowsContainerGO = new GameObject("Rows", typeof(RectTransform));
        rowsContainerGO.transform.SetParent(cardGO.transform, false);
        var rcrt = (RectTransform)rowsContainerGO.transform;
        rcrt.anchorMin = new Vector2(0.5f, 1f); rcrt.anchorMax = new Vector2(0.5f, 1f); rcrt.pivot = new Vector2(0.5f, 1f);
        rcrt.anchoredPosition = new Vector2(0f, -300f);
        rcrt.sizeDelta = new Vector2(760f, 800f);

        int n = Mathf.Max(1, leaderboardRowCount);
        float rowHeight = 64f;
        float rowGap    = 6f;
        var rows = new LeaderboardRow[n];
        for (int i = 0; i < n; i++)
        {
            rows[i] = BuildLeaderboardRow(rowsContainerGO.transform, i,
                                          rowHeight, rowGap,
                                          circleSprite, roundedRectSp);
        }

        // ─────────────────────────────────────────────────────
        // Continue hint
        // ─────────────────────────────────────────────────────
        var hintGO = new GameObject("ContinueHint", typeof(RectTransform));
        hintGO.transform.SetParent(cardGO.transform, false);
        var hrt = (RectTransform)hintGO.transform;
        hrt.anchorMin = new Vector2(0.5f, 0f); hrt.anchorMax = new Vector2(0.5f, 0f); hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 35f);
        hrt.sizeDelta = new Vector2(780f, 60f);
        var hintTmp = hintGO.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "";
        hintTmp.fontSize = 34;
        hintTmp.fontStyle = FontStyles.Bold;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = new Color(1f, 1f, 1f, 0f);
        hintTmp.raycastTarget = false;

        // ─────────────────────────────────────────────────────
        // Components: panel + card FX
        // ─────────────────────────────────────────────────────
        var panel = rootGO.AddComponent<LeaderboardPanel>();
        panel.group = cg;
        panel.card = crt;
        panel.titleText = title;
        panel.myRankText = myRank;
        panel.continueHintText = hintTmp;
        panel.rows = rows;

        var fx = cardGO.AddComponent<LeaderboardCardFX>();
        fx.glowHalo = halort;
        fx.titleShimmer = shrt;
        fx.borderOutline = border;

        // ─── Wire to LuxoddGameBridge ─────────────────────────
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
        Debug.Log("[BucaSetupHelper] ✔ LeaderboardPanel created with " + n + " rows + animated FX.");
    }

    // ─────────────────────────────────────────────────────────
    // Header strip ("RANK   PLAYER   SCORE")
    // ─────────────────────────────────────────────────────────
    static void BuildHeaderStrip(Transform parent)
    {
        var headerGO = new GameObject("HeaderStrip", typeof(RectTransform));
        headerGO.transform.SetParent(parent, false);
        var hrt = (RectTransform)headerGO.transform;
        hrt.anchorMin = new Vector2(0.5f, 1f); hrt.anchorMax = new Vector2(0.5f, 1f); hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = new Vector2(0f, -250f);
        hrt.sizeDelta = new Vector2(760f, 36f);

        AddHeaderLabel(headerGO.transform, "RANK",   new Vector2(-330f, 0f), 60f);
        AddHeaderLabel(headerGO.transform, "PLAYER", new Vector2( -90f, 0f), 320f);
        AddHeaderLabel(headerGO.transform, "SCORE",  new Vector2( 290f, 0f), 180f);
    }

    static void AddHeaderLabel(Transform parent, string text, Vector2 pos, float width)
    {
        var go = new GameObject($"Header_{text}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 36f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = 24;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.8f, 0.7f, 1f, 0.55f);
        t.characterSpacing = 4f;
        t.raycastTarget = false;
    }

    // ─────────────────────────────────────────────────────────
    // One leaderboard row: bg + badge + rank + name + score
    // ─────────────────────────────────────────────────────────
    static LeaderboardRow BuildLeaderboardRow(Transform parent, int index,
                                              float rowHeight, float rowGap,
                                              Sprite circleSprite, Sprite roundedRectSp)
    {
        var rowGO = new GameObject($"Row_{index + 1}", typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);
        var rrt = (RectTransform)rowGO.transform;
        rrt.anchorMin = new Vector2(0.5f, 1f); rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0f, -index * (rowHeight + rowGap));
        rrt.sizeDelta = new Vector2(760f, rowHeight);

        // Row background (transparent by default; tinted for player row)
        var bgGO = new GameObject("RowBackground", typeof(RectTransform));
        bgGO.transform.SetParent(rowGO.transform, false);
        var bgrt = (RectTransform)bgGO.transform;
        bgrt.anchorMin = new Vector2(0f, 0f); bgrt.anchorMax = new Vector2(1f, 1f);
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        var rowBg = bgGO.AddComponent<Image>();
        rowBg.sprite = roundedRectSp;
        rowBg.type = Image.Type.Sliced;
        rowBg.color = new Color(0f, 0f, 0f, 0f);
        rowBg.raycastTarget = false;

        // Rank badge (circle)
        var badgeGO = new GameObject("RankBadge", typeof(RectTransform));
        badgeGO.transform.SetParent(rowGO.transform, false);
        var bdrt = (RectTransform)badgeGO.transform;
        bdrt.anchorMin = bdrt.anchorMax = bdrt.pivot = new Vector2(0f, 0.5f);
        bdrt.anchoredPosition = new Vector2(50f, 0f);
        bdrt.sizeDelta = new Vector2(48f, 48f);
        var badge = badgeGO.AddComponent<Image>();
        badge.sprite = circleSprite;
        badge.color = new Color(0.30f, 0.22f, 0.45f, 0f);
        badge.raycastTarget = false;

        // Rank text (sits on top of badge)
        var rankGO = new GameObject("RankText", typeof(RectTransform));
        rankGO.transform.SetParent(rowGO.transform, false);
        var rkrt = (RectTransform)rankGO.transform;
        rkrt.anchorMin = rkrt.anchorMax = rkrt.pivot = new Vector2(0f, 0.5f);
        rkrt.anchoredPosition = new Vector2(50f, 0f);
        rkrt.sizeDelta = new Vector2(48f, 48f);
        var rankTmp = rankGO.AddComponent<TextMeshProUGUI>();
        rankTmp.text = "";
        rankTmp.fontSize = 30;
        rankTmp.fontStyle = FontStyles.Bold;
        rankTmp.alignment = TextAlignmentOptions.Center;
        rankTmp.color = new Color(1f, 1f, 1f, 0f);
        rankTmp.raycastTarget = false;

        // Name text — left-aligned, bold
        var nameGO = new GameObject("NameText", typeof(RectTransform));
        nameGO.transform.SetParent(rowGO.transform, false);
        var nrt = (RectTransform)nameGO.transform;
        nrt.anchorMin = new Vector2(0f, 0.5f); nrt.anchorMax = new Vector2(0f, 0.5f); nrt.pivot = new Vector2(0f, 0.5f);
        nrt.anchoredPosition = new Vector2(110f, 0f);
        nrt.sizeDelta = new Vector2(420f, rowHeight);
        var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "";
        nameTmp.fontSize = 36;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = new Color(1f, 1f, 1f, 0f);
        nameTmp.characterSpacing = 2f;
        nameTmp.raycastTarget = false;
        nameTmp.enableWordWrapping = false;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;

        // Score text — right-aligned, monospaced figures
        var scoreGO = new GameObject("ScoreText", typeof(RectTransform));
        scoreGO.transform.SetParent(rowGO.transform, false);
        var srt = (RectTransform)scoreGO.transform;
        srt.anchorMin = new Vector2(1f, 0.5f); srt.anchorMax = new Vector2(1f, 0.5f); srt.pivot = new Vector2(1f, 0.5f);
        srt.anchoredPosition = new Vector2(-30f, 0f);
        srt.sizeDelta = new Vector2(220f, rowHeight);
        var scoreTmp = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreTmp.text = "";
        scoreTmp.fontSize = 38;
        scoreTmp.fontStyle = FontStyles.Bold;
        scoreTmp.alignment = TextAlignmentOptions.Right;
        scoreTmp.color = new Color(1f, 1f, 1f, 0f);
        scoreTmp.characterSpacing = 2f;
        scoreTmp.raycastTarget = false;

        var rowComp = rowGO.AddComponent<LeaderboardRow>();
        rowComp.rectTransform = rrt;
        rowComp.rankBadge = badge;
        rowComp.rankText = rankTmp;
        rowComp.nameText = nameTmp;
        rowComp.scoreText = scoreTmp;
        rowComp.rowBackground = rowBg;
        return rowComp;
    }

    // ─────────────────────────────────────────────────────────
    // Sprite builders for leaderboard FX
    // ─────────────────────────────────────────────────────────

    /// <summary>Bright in middle, transparent at edges — for glow halos.</summary>
    static Sprite BuildSoftGlowSprite()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxD = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c) / maxD;
            d = Mathf.Clamp01(d);
            // Smooth falloff: opaque at center → transparent at edge
            float a = 1f - Mathf.SmoothStep(0f, 1f, d);
            // Squared for softer falloff
            a = a * a;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Sliced rounded rectangle for cards/row backgrounds. Only the
    /// 4 corner quadrants get the arc; the 4 straight edges and the
    /// interior are fully opaque so 9-slicing keeps the rounded look
    /// when stretched.
    /// </summary>
    static Sprite BuildRoundedRectSprite(int corner)
    {
        int size = corner * 2 + 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float a = 1f;
            // Determine which corner quadrant (if any) this pixel falls in.
            // Arc center is `corner` units inset from the corresponding corner.
            bool leftSide   = x < corner;
            bool rightSide  = x >= size - corner;
            bool bottomSide = y < corner;
            bool topSide    = y >= size - corner;

            if ((leftSide || rightSide) && (bottomSide || topSide))
            {
                float cx = leftSide ? corner : size - 1 - corner;
                float cy = bottomSide ? corner : size - 1 - corner;
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                a = Mathf.Clamp01(corner - dist);
            }
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        // 9-slice borders = `corner` on each side so the radius doesn't stretch
        var border = new Vector4(corner, corner, corner, corner);
        return Sprite.Create(tex, new Rect(0, 0, size, size),
                             new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, border);
    }

    /// <summary>Vertical white gradient stripe for the title shimmer.</summary>
    static Sprite BuildVerticalGradientStripeSprite()
    {
        const int w = 64, h = 4;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++)
        {
            // Alpha peaks in the middle (max ~0.9) and fades to 0 at sides
            float xn = (x / (float)(w - 1)) * 2f - 1f; // -1..1
            float a = Mathf.Pow(1f - Mathf.Abs(xn), 2.5f) * 0.9f;
            for (int y = 0; y < h; y++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
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
    // 16) Audio manager — DontDestroyOnLoad singleton with all SFX/Music slots
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("16. Spawn Audio Manager")]
    public void SpawnAudioManager()
    {
        // Singleton-style: don't double-create
        var existing = FindFirstObjectByType<AudioManager>();
        AudioManager mgr;
        if (existing != null)
        {
            Debug.Log("[BucaSetupHelper] AudioManager already exists — re-wiring its clip slots.");
            mgr = existing;
        }
        else
        {
            var go = new GameObject("AudioManager");
            mgr = go.AddComponent<AudioManager>();
            // No parent — needs to live at scene root for DontDestroyOnLoad to work.
            Debug.Log("[BucaSetupHelper] ✔ AudioManager spawned.");
        }
#if UNITY_EDITOR
        mgr.AutoWireFromAssetsFolder();
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // 15) Control hint bar — joystick + button icons at the bottom
    // ═══════════════════════════════════════════════════════════
    [System.Serializable]
    public class HintItemConfig
    {
        public ControlIcon icon = ControlIcon.JoystickStick;
        public string label = "AIM";
    }
    public enum ControlIcon { JoystickStick, BlackButton, RedButton, GreenButton, YellowButton, BlueButton, PurpleButton, WhiteButton }

    [Header("Control hints (bottom of HUD)")]
    [Tooltip("Hints shown in the control bar. Default = joystick AIM + Black LAUNCH.")]
    public HintItemConfig[] controlHints = new HintItemConfig[]
    {
        new HintItemConfig { icon = ControlIcon.JoystickStick, label = "AIM" },
        new HintItemConfig { icon = ControlIcon.BlackButton,   label = "LAUNCH" },
    };
    [Tooltip("Hide the bar permanently after this many shots. 0 = always visible.")]
    public int hintHideAfterShots = 3;
    [Tooltip("Fade hints out while the puck is moving, back in when idle.")]
    public bool hintAutoFadeWhileMoving = true;

    [ContextMenu("15. Spawn Control Hint Bar")]
    public void SpawnControlHintBar()
    {
        ResolveReferences();
        if (gameHud == null) return;

        DestroyChildByName(gameHud.transform, "ControlHintBar");

        // Sprites for the icons
        var circleSp   = BuildCircleSprite();
        var roundedSp  = BuildRoundedRectSprite(20);
        var stickBaseSp = BuildCircleSprite();   // small filled circle for the base disc

        // Root
        var rootGO = new GameObject("ControlHintBar", typeof(RectTransform));
        rootGO.transform.SetParent(gameHud.transform, false);
        var rrt = (RectTransform)rootGO.transform;
        rrt.anchorMin = new Vector2(0.5f, 0f); rrt.anchorMax = new Vector2(0.5f, 0f); rrt.pivot = new Vector2(0.5f, 0f);
        rrt.anchoredPosition = new Vector2(0f, 22f);
        // Width set after items are laid out

        var bgGo = new GameObject("Background", typeof(RectTransform));
        bgGo.transform.SetParent(rootGO.transform, false);
        var bgrt = (RectTransform)bgGo.transform;
        bgrt.anchorMin = new Vector2(0f, 0f); bgrt.anchorMax = new Vector2(1f, 1f);
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        var bg = bgGo.AddComponent<Image>();
        bg.sprite = roundedSp;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        var cg = rootGO.AddComponent<CanvasGroup>();
        // Visible immediately so the bar shows up in the Scene view at edit
        // time. ControlHintBar.Update() takes over once Play starts and
        // adjusts alpha based on puck state.
        cg.alpha = 0.95f; cg.interactable = false; cg.blocksRaycasts = false;

        // Layout: walk left-to-right, place each hint with icon + label
        float x = 28f;            // running cursor
        float padY = 14f;         // top/bottom padding
        float itemGap = 32f;
        float iconSize = 44f;
        float labelGap = 10f;
        float maxLabelW = 0f;

        if (controlHints == null || controlHints.Length == 0)
        {
            controlHints = new HintItemConfig[]
            {
                new HintItemConfig { icon = ControlIcon.JoystickStick, label = "AIM" },
                new HintItemConfig { icon = ControlIcon.BlackButton,   label = "LAUNCH" },
            };
        }

        var labelTexts = new List<TMP_Text>();
        for (int i = 0; i < controlHints.Length; i++)
        {
            var cfg = controlHints[i] ?? new HintItemConfig();

            // Icon
            float iconW = (cfg.icon == ControlIcon.JoystickStick) ? iconSize + 8f : iconSize;
            BuildHintIcon(rootGO.transform, cfg.icon, new Vector2(x, 0f), iconSize, circleSp, stickBaseSp);

            x += iconW + labelGap;

            // Label TMP
            var lblGO = new GameObject($"Hint_{i + 1}_Label", typeof(RectTransform));
            lblGO.transform.SetParent(rootGO.transform, false);
            var lrt = (RectTransform)lblGO.transform;
            lrt.anchorMin = new Vector2(0f, 0.5f); lrt.anchorMax = new Vector2(0f, 0.5f); lrt.pivot = new Vector2(0f, 0.5f);
            lrt.anchoredPosition = new Vector2(x, 0f);
            lrt.sizeDelta = new Vector2(180f, 44f);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text = (cfg.label ?? "").ToUpperInvariant();
            tmp.fontSize = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.95f, 0.95f, 1f, 0.95f);
            tmp.characterSpacing = 4f;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            labelTexts.Add(tmp);

            // Measure label width post-render, but for layout we estimate
            // 17 px per char × bold for the cursor advance.
            float estW = Mathf.Max(40f, tmp.text.Length * 17f);
            x += estW + itemGap;
            if (estW > maxLabelW) maxLabelW = estW;
        }
        // Trailing right padding
        x += 12f;
        rrt.sizeDelta = new Vector2(Mathf.Max(360f, x), iconSize + padY * 2f);

        // Re-center each label vertically (anchored at left already)
        // and set the bar component
        var bar = rootGO.AddComponent<ControlHintBar>();
        bar.group = cg;
        bar.levelManager = levelManager;
        bar.hideAfterShotsTaken = hintHideAfterShots;
        bar.autoFadeWhileMoving = hintAutoFadeWhileMoving;

        Debug.Log("[BucaSetupHelper] ✔ ControlHintBar spawned with " + controlHints.Length + " hints.");
    }

    // ─────────────────────────────────────────────────────────
    // Hint-icon builders
    // ─────────────────────────────────────────────────────────
    static void BuildHintIcon(Transform parent, ControlIcon icon, Vector2 anchoredPos,
                              float iconSize, Sprite circleSp, Sprite stickBaseSp)
    {
        switch (icon)
        {
            case ControlIcon.JoystickStick:
                BuildJoystickStickIcon(parent, anchoredPos, iconSize, circleSp, stickBaseSp);
                break;
            default:
                BuildArcadeButtonIcon(parent, anchoredPos, iconSize, circleSp,
                                      ColorForIcon(icon), icon == ControlIcon.WhiteButton);
                break;
        }
    }

    /// <summary>
    /// Joystick mini-icon: a grey base disc + a thin grey shaft + a
    /// red ball on top. Stylized — matches arcade reference visuals.
    /// </summary>
    static void BuildJoystickStickIcon(Transform parent, Vector2 anchoredPos, float iconSize,
                                        Sprite circleSp, Sprite baseSp)
    {
        var root = new GameObject("Hint_Joystick", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rrt = (RectTransform)root.transform;
        rrt.anchorMin = new Vector2(0f, 0.5f); rrt.anchorMax = new Vector2(0f, 0.5f); rrt.pivot = new Vector2(0f, 0.5f);
        rrt.anchoredPosition = anchoredPos;
        rrt.sizeDelta = new Vector2(iconSize + 8f, iconSize + 16f);

        // Base disc (bottom)
        var baseGO = new GameObject("Base", typeof(RectTransform));
        baseGO.transform.SetParent(root.transform, false);
        var brt = (RectTransform)baseGO.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 2f);
        brt.sizeDelta = new Vector2(iconSize * 0.6f, iconSize * 0.18f);
        var baseImg = baseGO.AddComponent<Image>();
        baseImg.sprite = baseSp;
        baseImg.color = new Color(0.55f, 0.55f, 0.6f, 0.95f);
        baseImg.raycastTarget = false;

        // Stick (vertical bar)
        var stickGO = new GameObject("Stick", typeof(RectTransform));
        stickGO.transform.SetParent(root.transform, false);
        var srt = (RectTransform)stickGO.transform;
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, iconSize * 0.18f);
        srt.sizeDelta = new Vector2(6f, iconSize * 0.5f);
        var stickImg = stickGO.AddComponent<Image>();
        stickImg.color = new Color(0.45f, 0.45f, 0.5f, 1f);
        stickImg.raycastTarget = false;

        // Red ball on top
        var ballGO = new GameObject("Ball", typeof(RectTransform));
        ballGO.transform.SetParent(root.transform, false);
        var ballrt = (RectTransform)ballGO.transform;
        ballrt.anchorMin = ballrt.anchorMax = ballrt.pivot = new Vector2(0.5f, 1f);
        ballrt.anchoredPosition = new Vector2(0f, 0f);
        ballrt.sizeDelta = new Vector2(iconSize * 0.55f, iconSize * 0.55f);
        var ballImg = ballGO.AddComponent<Image>();
        ballImg.sprite = circleSp;
        ballImg.color = new Color(0.95f, 0.25f, 0.25f, 1f);
        ballImg.raycastTarget = false;

        // Subtle highlight on the ball — small white dot offset top-left
        var hlGO = new GameObject("Highlight", typeof(RectTransform));
        hlGO.transform.SetParent(ballGO.transform, false);
        var hrt = (RectTransform)hlGO.transform;
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(-iconSize * 0.10f, iconSize * 0.10f);
        hrt.sizeDelta = new Vector2(iconSize * 0.20f, iconSize * 0.20f);
        var hl = hlGO.AddComponent<Image>();
        hl.sprite = circleSp;
        hl.color = new Color(1f, 1f, 1f, 0.55f);
        hl.raycastTarget = false;
    }

    /// <summary>
    /// Arcade button mini-icon: a colored filled circle with a soft
    /// inner highlight that reads as a 3D dome. White button gets a
    /// dark stroke ring so it's visible on the dark hint background.
    /// </summary>
    static void BuildArcadeButtonIcon(Transform parent, Vector2 anchoredPos, float iconSize,
                                       Sprite circleSp, Color buttonColor, bool whiteButton)
    {
        var root = new GameObject("Hint_Button", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rrt = (RectTransform)root.transform;
        rrt.anchorMin = new Vector2(0f, 0.5f); rrt.anchorMax = new Vector2(0f, 0.5f); rrt.pivot = new Vector2(0f, 0.5f);
        rrt.anchoredPosition = anchoredPos;
        rrt.sizeDelta = new Vector2(iconSize, iconSize);

        // Optional dark ring for the white button so it doesn't disappear
        if (whiteButton)
        {
            var ringGO = new GameObject("Ring", typeof(RectTransform));
            ringGO.transform.SetParent(root.transform, false);
            var rgrt = (RectTransform)ringGO.transform;
            rgrt.anchorMin = rgrt.anchorMax = rgrt.pivot = new Vector2(0.5f, 0.5f);
            rgrt.anchoredPosition = Vector2.zero;
            rgrt.sizeDelta = new Vector2(iconSize + 4f, iconSize + 4f);
            var ring = ringGO.AddComponent<Image>();
            ring.sprite = circleSp;
            ring.color = new Color(0.15f, 0.15f, 0.20f, 1f);
            ring.raycastTarget = false;
        }

        // Body
        var bodyGO = new GameObject("Body", typeof(RectTransform));
        bodyGO.transform.SetParent(root.transform, false);
        var bdrt = (RectTransform)bodyGO.transform;
        bdrt.anchorMin = bdrt.anchorMax = bdrt.pivot = new Vector2(0.5f, 0.5f);
        bdrt.anchoredPosition = Vector2.zero;
        bdrt.sizeDelta = new Vector2(iconSize, iconSize);
        var body = bodyGO.AddComponent<Image>();
        body.sprite = circleSp;
        body.color = buttonColor;
        body.raycastTarget = false;

        // Highlight dot (top-left for a dome-like 3D feel)
        var hlGO = new GameObject("Highlight", typeof(RectTransform));
        hlGO.transform.SetParent(root.transform, false);
        var hrt = (RectTransform)hlGO.transform;
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(-iconSize * 0.15f, iconSize * 0.15f);
        hrt.sizeDelta = new Vector2(iconSize * 0.30f, iconSize * 0.30f);
        var hl = hlGO.AddComponent<Image>();
        hl.sprite = circleSp;
        // White button highlight is subtle; colored buttons get a brighter dot
        hl.color = whiteButton
            ? new Color(1f, 1f, 1f, 0.55f)
            : new Color(1f, 1f, 1f, 0.45f);
        hl.raycastTarget = false;
    }

    static Color ColorForIcon(ControlIcon icon) => icon switch
    {
        ControlIcon.BlackButton  => new Color(0.10f, 0.10f, 0.12f, 1f),
        ControlIcon.RedButton    => new Color(0.95f, 0.20f, 0.25f, 1f),
        ControlIcon.GreenButton  => new Color(0.25f, 0.85f, 0.40f, 1f),
        ControlIcon.YellowButton => new Color(1.00f, 0.85f, 0.20f, 1f),
        ControlIcon.BlueButton   => new Color(0.30f, 0.55f, 1.00f, 1f),
        ControlIcon.PurpleButton => new Color(0.65f, 0.35f, 0.95f, 1f),
        ControlIcon.WhiteButton  => new Color(0.95f, 0.95f, 0.97f, 1f),
        _ => Color.gray,
    };

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
