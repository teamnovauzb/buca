using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Runtime one-shot helper for the MainMenu scene. Drop on any GameObject
/// in the MainMenu scene, then either:
///
///   • Right-click header → "★ Setup Everything"
///       Builds the full level-select panel + auto-wires the "LEVELS"
///       button on the main menu to open it.
///   • Right-click → "Spawn Level Select Panel" / "Wire LEVELS Button"
///       Each step individually (re-runnable).
///
/// The panel is a full-screen overlay that's HIDDEN by default and only
/// shown when the player presses LEVELS. Closing it returns to the menu.
///
/// Delete the helper GameObject after running it once and saving the scene.
/// </summary>
[DisallowMultipleComponent]
public class MenuSetupHelper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How many level tiles to generate.")]
    public int levelCount = 15;
    [Tooltip("Tiles per row in the grid.")]
    public int columnsPerRow = 5;
    [Tooltip("Parent canvas to add the panel into. Auto-found if left empty.")]
    public Canvas mainMenuCanvas;
    [Tooltip("Name of the Game scene to load on tile click.")]
    public string gameSceneName = "Game";

    [Header("Auto-wire the LEVELS menu button")]
    [Tooltip("Button name to search for in the main menu (case-insensitive contains match).")]
    public string levelsButtonName = "LEVELS";

    LevelSelectController _spawnedController;

    [ContextMenu("★ Setup Everything")]
    public void SetupEverything()
    {
        ResolveCanvas();
        SpawnLevelSelectPanel();
        SpawnLevelsButton();   // creates the LEVELS button if missing
        WireLevelsButton();    // wires whatever LEVELS button exists to open the panel
        SpawnAudioManager();   // singleton; persists across scenes
        SetupArcadeNavigator();// gamepad navigation + button highlight on main menu
        Debug.Log("[MenuSetupHelper] ✔ Setup complete. Press LEVELS in Play mode.");
    }

    [ContextMenu("Setup Arcade Menu Navigator")]
    public void SetupArcadeNavigator()
    {
        ResolveCanvas();
        if (mainMenuCanvas == null) { Debug.LogError("[MenuSetupHelper] No Canvas found."); return; }

        // Collect every Button on the menu canvas EXCEPT those inside the
        // LevelSelectPanel (which has its own grid navigation).
        var allBtns = mainMenuCanvas.GetComponentsInChildren<Button>(true);
        var menuBtns = new System.Collections.Generic.List<Button>();
        foreach (var b in allBtns)
        {
            if (b.GetComponentInParent<LevelSelectController>() != null) continue;
            menuBtns.Add(b);
        }
        if (menuBtns.Count == 0)
        {
            Debug.LogWarning("[MenuSetupHelper] No menu buttons found to wire into ArcadeUINavigator.");
            return;
        }

        // Sort by Y position (top-to-bottom) so up/down navigation feels right
        menuBtns.Sort((a, b) =>
            ((RectTransform)b.transform).position.y.CompareTo(
                ((RectTransform)a.transform).position.y));

        // Strengthen each button's highlighted color so the gamepad-driven
        // highlight is actually visible. Without this the default tint is
        // a barely-perceptible 1.0/1.0/1.0 white.
        // ALSO disable Unity's built-in Automatic navigation (Mode=3 in YAML
        // = Selectable.Navigation.Mode.Automatic). Spatial nav was silently
        // racing ArcadeUINavigator and routing PLAY→QUIT, skipping LEVELS.
        // Mode.None lets ArcadeUINavigator be the only voice.
        foreach (var b in menuBtns)
        {
            var c = b.colors;
            c.highlightedColor = new Color(1.30f, 1.30f, 1.30f, 1f); // 30% brighter
            c.selectedColor    = new Color(1.40f, 1.20f, 0.50f, 1f); // gold tint
            c.colorMultiplier  = 1.0f;
            c.fadeDuration     = 0.08f;
            b.colors = c;
            b.transition = Selectable.Transition.ColorTint;

            // Kill Unity's built-in spatial nav — ArcadeUINavigator owns nav now.
            var btnNav = b.navigation;
            btnNav.mode = Navigation.Mode.None;
            b.navigation = btnNav;
        }

        // Find or add the navigator (exists on the canvas root)
        var nav = mainMenuCanvas.GetComponent<ArcadeUINavigator>();
        if (nav == null) nav = mainMenuCanvas.gameObject.AddComponent<ArcadeUINavigator>();
        nav.selectables = menuBtns.ToArray();
        Debug.Log($"[MenuSetupHelper] ✔ ArcadeUINavigator wired with {menuBtns.Count} buttons " +
                  $"({string.Join(", ", menuBtns.ConvertAll(b => b.name))})");
    }

    [ContextMenu("Spawn Audio Manager")]
    public void SpawnAudioManager()
    {
        var existing = FindFirstObjectByType<AudioManager>();
        AudioManager mgr;
        if (existing != null)
        {
            Debug.Log("[MenuSetupHelper] AudioManager already exists — re-wiring its clip slots.");
            mgr = existing;
        }
        else
        {
            var go = new GameObject("AudioManager");
            mgr = go.AddComponent<AudioManager>();
            Debug.Log("[MenuSetupHelper] ✔ AudioManager spawned.");
        }
#if UNITY_EDITOR
        mgr.AutoWireFromAssetsFolder();
#endif
    }

    void ResolveCanvas()
    {
        if (mainMenuCanvas != null) return;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases) if (c.name.Contains("MainMenu")) { mainMenuCanvas = c; break; }
        if (mainMenuCanvas == null && canvases.Length > 0) mainMenuCanvas = canvases[0];
    }

    // ═══════════════════════════════════════════════════════════
    // 1) Spawn full-screen Level Select panel
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("Spawn Level Select Panel")]
    public void SpawnLevelSelectPanel()
    {
        ResolveCanvas();
        if (mainMenuCanvas == null) { Debug.LogError("[MenuSetupHelper] No Canvas found."); return; }

        // Clean up any previous panel + the older "LevelSelect" grid that
        // the old SpawnLevelSelectGrid produced — both are replaced.
        var existing = mainMenuCanvas.transform.Find("LevelSelectPanel");
        if (existing != null) DestroyImmediate(existing.gameObject);
        var legacyGrid = mainMenuCanvas.transform.Find("LevelSelect");
        if (legacyGrid != null) DestroyImmediate(legacyGrid.gameObject);

        // Sprites
        var tileSp     = BuildTileSprite();
        var starSp     = BuildStarSprite();
        var roundedSp  = BuildRoundedSprite(28);
        var checkSp    = BuildCheckmarkSprite();
        var circleSp   = BuildSimpleCircleSprite();

        // ─────────────────────────────────────────────────────
        // Root: full-screen overlay
        // ─────────────────────────────────────────────────────
        var rootGO = new GameObject("LevelSelectPanel", typeof(RectTransform));
        rootGO.transform.SetParent(mainMenuCanvas.transform, false);
        var rrt = (RectTransform)rootGO.transform;
        StretchFull(rrt);

        var cg = rootGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        // Dim
        var dim = NewImageChild(rootGO.transform, "Dim", new Color(0.02f, 0.01f, 0.05f, 0.92f));
        StretchFull(dim.rectTransform);
        dim.raycastTarget = true;

        // ─────────────────────────────────────────────────────
        // Card
        // ─────────────────────────────────────────────────────
        int rows = Mathf.CeilToInt(levelCount / (float)Mathf.Max(1, columnsPerRow));
        float tileW = 165f, tileH = 175f, gap = 18f;
        float gridW = columnsPerRow * tileW + (columnsPerRow - 1) * gap;
        float gridH = rows * tileH + (rows - 1) * gap;
        float cardW = gridW + 120f;
        float cardH = gridH + 380f;

        var cardGO = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(rootGO.transform, false);
        var crt = (RectTransform)cardGO.transform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(cardW, cardH);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.sprite = roundedSp;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.05f, 0.03f, 0.10f, 0.96f);

        // ─────────────────────────────────────────────────────
        // Header + progress bar
        // ─────────────────────────────────────────────────────
        var titleGO = NewTextChild(cardGO.transform, "Title", "SELECT LEVEL", 78,
                                   new Color(1f, 1f, 1f, 0.98f), TextAlignmentOptions.Center);
        var trt = titleGO.rectTransform;
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -28f);
        trt.sizeDelta = new Vector2(800f, 100f);
        titleGO.fontStyle = FontStyles.Bold;
        titleGO.outlineWidth = 0.20f;
        titleGO.outlineColor = new Color(0.05f, 0.02f, 0.12f, 1f);

        // Progress bar (track + fill)
        var trackGO = NewImageChild(cardGO.transform, "ProgressTrack", new Color(0.1f, 0.06f, 0.18f, 0.85f));
        trackGO.sprite = roundedSp; trackGO.type = Image.Type.Sliced;
        var pkrt = (RectTransform)trackGO.transform;
        pkrt.anchorMin = pkrt.anchorMax = pkrt.pivot = new Vector2(0.5f, 1f);
        pkrt.anchoredPosition = new Vector2(0f, -130f);
        pkrt.sizeDelta = new Vector2(620f, 18f);

        var fillGO = NewImageChild(trackGO.transform, "ProgressFill", new Color(0.30f, 0.95f, 0.40f, 1f));
        fillGO.sprite = roundedSp; fillGO.type = Image.Type.Sliced;
        var fkrt = (RectTransform)fillGO.transform;
        fkrt.anchorMin = new Vector2(0f, 0f);
        fkrt.anchorMax = new Vector2(0f, 1f); // width driven by anchorMax.x at runtime
        fkrt.pivot = new Vector2(0f, 0.5f);
        fkrt.offsetMin = Vector2.zero; fkrt.offsetMax = Vector2.zero;

        var progressLabelGO = NewTextChild(trackGO.transform, "ProgressLabel", "0 / " + levelCount, 22,
                                            new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
        var plrt = progressLabelGO.rectTransform;
        plrt.anchorMin = new Vector2(0f, 0f); plrt.anchorMax = new Vector2(1f, 1f);
        plrt.offsetMin = Vector2.zero; plrt.offsetMax = Vector2.zero;
        progressLabelGO.fontStyle = FontStyles.Bold;
        progressLabelGO.raycastTarget = false;

        // ─────────────────────────────────────────────────────
        // Grid
        // ─────────────────────────────────────────────────────
        var gridGO = new GameObject("Grid", typeof(RectTransform));
        gridGO.transform.SetParent(cardGO.transform, false);
        var grt = (RectTransform)gridGO.transform;
        grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0.5f, 1f);
        grt.anchoredPosition = new Vector2(0f, -180f);
        grt.sizeDelta = new Vector2(gridW, gridH);

        var ctrl = rootGO.AddComponent<LevelSelectController>();
        ctrl.gameSceneName = gameSceneName;
        ctrl.panelGroup = cg;
        ctrl.panelCard = crt;
        ctrl.progressBarFill = fkrt;
        ctrl.columnsPerRow = columnsPerRow;

        float startX = -gridW * 0.5f + tileW * 0.5f;
        for (int i = 0; i < levelCount; i++)
        {
            int row = i / columnsPerRow;
            int col = i % columnsPerRow;
            var entry = BuildLevelTile(gridGO.transform, i, tileW, tileH,
                                       startX + col * (tileW + gap),
                                       -row * (tileH + gap),
                                       tileSp, starSp, checkSp,
                                       ctrl);
            ctrl.levels.Add(entry);
        }

        // ─────────────────────────────────────────────────────
        // Footer stats
        // ─────────────────────────────────────────────────────
        float footerY = -(180f + gridH + 30f);

        var statsGO = new GameObject("FooterStats", typeof(RectTransform));
        statsGO.transform.SetParent(cardGO.transform, false);
        var srt = (RectTransform)statsGO.transform;
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, footerY);
        srt.sizeDelta = new Vector2(cardW - 100f, 50f);

        ctrl.completedLabel = AddFooterLabel(statsGO.transform, "Completed", $"0/{levelCount} COMPLETED",
                                              new Vector2(-cardW * 0.32f, 0f), 220f);
        ctrl.starsLabel     = AddFooterStarLabel(statsGO.transform, $"0/{levelCount * 3}",
                                                  new Vector2(0f, 0f), 180f, starSp);
        ctrl.scoreLabel     = AddFooterLabel(statsGO.transform, "Score", "SCORE: 0",
                                              new Vector2(cardW * 0.30f, 0f), 220f);

        // ─────────────────────────────────────────────────────
        // Idle countdown — anchored TOP-RIGHT of the card, always visible.
        // Looks like an arcade attract-mode HUD: pill background with
        // clock-style label "AUTO START  15s". Pulses red when ≤ 5s.
        // Sits OUTSIDE the title/grid/footer flow so it never overlaps.
        // ─────────────────────────────────────────────────────
        var idlePillGO = new GameObject("IdleCountdownPill", typeof(RectTransform));
        idlePillGO.transform.SetParent(cardGO.transform, false);
        var pillRT = (RectTransform)idlePillGO.transform;
        // Anchor TOP-LEFT instead of top-right. The previous top-right
        // position collided with the right side of the SELECT LEVEL title.
        pillRT.anchorMin = pillRT.anchorMax = pillRT.pivot = new Vector2(0f, 1f);
        pillRT.anchoredPosition = new Vector2(40f, -22f);
        pillRT.sizeDelta = new Vector2(220f, 50f);
        var pillBg = idlePillGO.AddComponent<Image>();
        pillBg.sprite = roundedSp;
        pillBg.type = Image.Type.Sliced;
        pillBg.color = new Color(0.10f, 0.06f, 0.20f, 0.85f);
        pillBg.raycastTarget = false;

        var idleGO = new GameObject("IdleCountdown", typeof(RectTransform));
        idleGO.transform.SetParent(idlePillGO.transform, false);
        var idleRT = (RectTransform)idleGO.transform;
        idleRT.anchorMin = new Vector2(0f, 0f);
        idleRT.anchorMax = new Vector2(1f, 1f);
        idleRT.offsetMin = new Vector2(14f, 0f);
        idleRT.offsetMax = new Vector2(-14f, 0f);
        var idleTmp = idleGO.AddComponent<TextMeshProUGUI>();
        idleTmp.text = "AUTO START  15s";
        idleTmp.fontSize = 26;
        idleTmp.fontStyle = FontStyles.Bold;
        idleTmp.alignment = TextAlignmentOptions.Center;
        idleTmp.color = new Color(1f, 0.85f, 0.30f, 1f);
        idleTmp.characterSpacing = 3f;
        idleTmp.raycastTarget = false;
        idleTmp.outlineWidth = 0.18f;
        idleTmp.outlineColor = new Color(0.05f, 0.02f, 0.12f, 1f);
        ctrl.idleCountdownText = idleTmp;

        // ─────────────────────────────────────────────────────
        // Back button (Green button visual + label)
        // ─────────────────────────────────────────────────────
        var backGO = new GameObject("BackButton", typeof(RectTransform));
        backGO.transform.SetParent(cardGO.transform, false);
        var bkrt = (RectTransform)backGO.transform;
        bkrt.anchorMin = bkrt.anchorMax = bkrt.pivot = new Vector2(0.5f, 0f);
        bkrt.anchoredPosition = new Vector2(0f, 32f);
        bkrt.sizeDelta = new Vector2(260f, 60f);

        // Hit area (transparent so the icon+label show through)
        var backBg = backGO.AddComponent<Image>();
        backBg.color = new Color(0f, 0f, 0f, 0f);
        backBg.raycastTarget = true;
        var backBtn = backGO.AddComponent<Button>();
        backBtn.targetGraphic = backBg;
        ctrl.backButton = backBtn;

        // Green circle icon
        var dotGO = new GameObject("GreenDot", typeof(RectTransform));
        dotGO.transform.SetParent(backGO.transform, false);
        var dot = dotGO.AddComponent<Image>();
        dot.sprite = circleSp;
        dot.color = new Color(0.30f, 0.95f, 0.40f, 1f);
        dot.raycastTarget = false;
        var drt = (RectTransform)dotGO.transform;
        drt.anchorMin = new Vector2(0f, 0.5f); drt.anchorMax = new Vector2(0f, 0.5f); drt.pivot = new Vector2(0f, 0.5f);
        drt.anchoredPosition = new Vector2(20f, 0f);
        drt.sizeDelta = new Vector2(40f, 40f);

        // White dot highlight on the green button
        var dotHl = NewImageChild(dotGO.transform, "Highlight", new Color(1f, 1f, 1f, 0.45f));
        dotHl.sprite = circleSp;
        var dhrt = (RectTransform)dotHl.transform;
        dhrt.anchorMin = dhrt.anchorMax = dhrt.pivot = new Vector2(0.5f, 0.5f);
        dhrt.anchoredPosition = new Vector2(-6f, 6f);
        dhrt.sizeDelta = new Vector2(12f, 12f);

        var backLabel = NewTextChild(backGO.transform, "Label", "BACK", 36,
                                     new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.MidlineLeft);
        backLabel.fontStyle = FontStyles.Bold;
        var blrt = backLabel.rectTransform;
        blrt.anchorMin = new Vector2(0f, 0f); blrt.anchorMax = new Vector2(1f, 1f);
        blrt.offsetMin = new Vector2(80f, 0f); blrt.offsetMax = new Vector2(-10f, 0f);
        backLabel.characterSpacing = 4f;
        backLabel.raycastTarget = false;

        // Stay GameObject-active so coroutines work; CanvasGroup hides it
        // (alpha=0 + blocksRaycasts=false set in LevelSelectController.Awake).
        _spawnedController = ctrl;
        Debug.Log($"[MenuSetupHelper] ✔ Spawned LevelSelectPanel with {levelCount} tiles ({columnsPerRow}×{rows}).");
    }

    // ═══════════════════════════════════════════════════════════
    // 1b) Spawn a LEVELS button between PLAY and QUIT
    //     Clones PLAY's RectTransform/Image styling so the new
    //     button visually matches the existing menu.
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("Spawn LEVELS Button")]
    public void SpawnLevelsButton()
    {
        ResolveCanvas();
        if (mainMenuCanvas == null) { Debug.LogError("[MenuSetupHelper] No Canvas found."); return; }

        var existingButtons = mainMenuCanvas.GetComponentsInChildren<Button>(true);
        string needle = (levelsButtonName ?? "LEVELS").ToLowerInvariant();

        // Always destroy any pre-existing LEVELS button (matched by name OR
        // label text) before rebuilding. PLAY/QUIT are explicitly protected.
        foreach (var b in existingButtons)
        {
            if (b == null) continue;
            if (b.GetComponentInParent<LevelSelectController>() != null) continue;
            string n = b.name.ToLowerInvariant();
            var lbl0 = b.GetComponentInChildren<TMP_Text>(true);
            string lt0 = lbl0 != null ? lbl0.text.ToLowerInvariant() : "";
            if (n.Contains("play") || lt0.Contains("play")) continue;
            if (n.Contains("quit") || lt0.Contains("quit")) continue;
            if (n.Contains(needle) || lt0.Contains(needle))
            {
                Debug.Log($"[MenuSetupHelper] Removing existing LEVELS button '{b.name}'.");
                if (Application.isPlaying) Destroy(b.gameObject);
                else DestroyImmediate(b.gameObject);
            }
        }

        existingButtons = mainMenuCanvas.GetComponentsInChildren<Button>(true);

        // Find PLAY (style reference) + QUIT (positioning reference)
        Button playBtn = null, quitBtn = null;
        foreach (var b in existingButtons)
        {
            if (b.GetComponentInParent<LevelSelectController>() != null) continue;
            string n = b.name.ToLowerInvariant();
            var lbl = b.GetComponentInChildren<TMP_Text>(true);
            string lt = lbl != null ? lbl.text.ToLowerInvariant() : "";
            if (playBtn == null && (n.Contains("play") || lt.Contains("play"))) playBtn = b;
            if (quitBtn == null && (n.Contains("quit") || lt.Contains("quit"))) quitBtn = b;
        }

        if (playBtn == null)
        {
            Debug.LogWarning("[MenuSetupHelper] No PLAY button found to base styling on. " +
                             "Add a LEVELS button manually.");
            return;
        }

        // ════════════════════════════════════════════════════════════
        // Build the LEVELS button from SCRATCH — never clone PLAY.
        // Cloning copies the serialized UnityEvent persistent-listener
        // list, and Unity's RemovePersistentListener doesn't always
        // commit to disk reliably (cached SerializedObject state, prefab
        // override edge cases, etc.). Building from new GameObjects
        // guarantees the OnClick list starts empty — no PlayGame can
        // possibly survive into the new button.
        // ════════════════════════════════════════════════════════════
        var playRT = (RectTransform)playBtn.transform;
        var playImg = playBtn.GetComponent<Image>();
        var playLbl = playBtn.GetComponentInChildren<TMP_Text>(true);

        // 1) Empty GameObject + RectTransform with PLAY's anchors/size
        var newGO = new GameObject("LEVELSButton", typeof(RectTransform));
        newGO.transform.SetParent(playBtn.transform.parent, false);
        var newRT = (RectTransform)newGO.transform;
        newRT.anchorMin = playRT.anchorMin;
        newRT.anchorMax = playRT.anchorMax;
        newRT.pivot = playRT.pivot;
        newRT.sizeDelta = playRT.sizeDelta;
        newRT.localScale = Vector3.one;

        // 2) Position vertically between PLAY and QUIT
        Vector2 newPos = playRT.anchoredPosition;
        if (quitBtn != null)
        {
            var quitRT = (RectTransform)quitBtn.transform;
            newPos = (playRT.anchoredPosition + quitRT.anchoredPosition) * 0.5f;
            float gap = Mathf.Abs(playRT.anchoredPosition.y - quitRT.anchoredPosition.y);
            if (gap < 140f)
            {
                quitRT.anchoredPosition = new Vector2(quitRT.anchoredPosition.x,
                                                      quitRT.anchoredPosition.y - 80f);
                newPos.y -= 40f;
            }
        }
        else
        {
            newPos.y = playRT.anchoredPosition.y - 130f;
        }
        newRT.anchoredPosition = newPos;

        // 3) Image (background) — copy PLAY's sprite + a distinct orange tint
        var newImg = newGO.AddComponent<Image>();
        if (playImg != null)
        {
            newImg.sprite = playImg.sprite;
            newImg.type = playImg.type;
            newImg.pixelsPerUnitMultiplier = playImg.pixelsPerUnitMultiplier;
        }
        newImg.color = new Color(1f, 0.65f, 0.20f, playImg != null ? playImg.color.a : 1f);

        // 4) Button — fresh, OnClick is empty by definition
        var newBtn = newGO.AddComponent<Button>();
        newBtn.targetGraphic = newImg;
        newBtn.transition = Selectable.Transition.ColorTint;
        var btnColors = newBtn.colors;
        btnColors.highlightedColor = new Color(1.30f, 1.30f, 1.30f, 1f);
        btnColors.selectedColor    = new Color(1.40f, 1.20f, 0.50f, 1f);
        btnColors.fadeDuration     = 0.08f;
        newBtn.colors = btnColors;

        // 5) Label child — fresh TMP with PLAY's font/style/material
        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(newGO.transform, false);
        var lblRT = (RectTransform)lblGO.transform;
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero;
        lblRT.offsetMax = Vector2.zero;
        var newLbl = lblGO.AddComponent<TextMeshProUGUI>();
        newLbl.text = "LEVELS";
        newLbl.alignment = TextAlignmentOptions.Center;
        newLbl.color = new Color(1f, 1f, 1f, 1f);
        newLbl.raycastTarget = false;
        if (playLbl != null)
        {
            newLbl.font = playLbl.font;
            newLbl.fontSharedMaterial = playLbl.fontSharedMaterial;
            newLbl.fontSize = playLbl.fontSize;
            newLbl.fontStyle = playLbl.fontStyle;
            newLbl.outlineWidth = playLbl.outlineWidth;
            newLbl.outlineColor = playLbl.outlineColor;
            newLbl.characterSpacing = playLbl.characterSpacing;
        }
        else
        {
            newLbl.fontSize = 64;
            newLbl.fontStyle = FontStyles.Bold;
        }

        // 6) Atomic wire — add Show as the ONLY listener
        var ctrl = _spawnedController != null
            ? _spawnedController
            : mainMenuCanvas.GetComponentInChildren<LevelSelectController>(true);
        if (ctrl != null)
        {
#if UNITY_EDITOR
            UnityEditor.Events.UnityEventTools.AddPersistentListener(newBtn.onClick, ctrl.Show);
            UnityEditor.EditorUtility.SetDirty(newBtn);
            int afterCount = newBtn.onClick.GetPersistentEventCount();
            if (afterCount != 1)
                Debug.LogError($"[MenuSetupHelper] WIRING BUG: fresh LEVELSButton has " +
                               $"{afterCount} persistent listeners (expected 1).");
            Debug.Log($"[MenuSetupHelper] ✔ Built LEVELS button from scratch + wired " +
                      $"{ctrl.name}.Show() (persistent listener count = {afterCount}).");
#else
            newBtn.onClick.AddListener(ctrl.Show);
#endif
        }
        else
        {
            Debug.LogWarning("[MenuSetupHelper] Spawned LEVELS button but no LevelSelectController " +
                             "found yet — run 'Wire LEVELS Button' after 'Spawn Level Select Panel'.");
        }
    }

    /// <summary>
    /// Removes every listener (runtime + persistent) from a Button's
    /// OnClick. Persistent listeners need UnityEditor's UnityEventTools
    /// to clear — they're stored in the serialized data, not in the
    /// runtime invocation list.
    /// </summary>
    static void ClearAllListeners(Button btn)
    {
        btn.onClick.RemoveAllListeners();
#if UNITY_EDITOR
        int count = btn.onClick.GetPersistentEventCount();
        for (int i = count - 1; i >= 0; i--)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, i);
        UnityEditor.EditorUtility.SetDirty(btn);
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // 2) Wire the main-menu LEVELS button to open the panel
    // ═══════════════════════════════════════════════════════════
    [ContextMenu("Wire LEVELS Button")]
    public void WireLevelsButton()
    {
        ResolveCanvas();
        if (mainMenuCanvas == null) { Debug.LogError("[MenuSetupHelper] No Canvas found."); return; }

        // Find the panel (might already be in scene from previous runs)
        var ctrl = _spawnedController;
        if (ctrl == null) ctrl = mainMenuCanvas.GetComponentInChildren<LevelSelectController>(true);
        if (ctrl == null)
        {
            Debug.LogWarning("[MenuSetupHelper] No LevelSelectController found. Run 'Spawn Level Select Panel' first.");
            return;
        }

        // Find every Button whose name contains the search string
        var buttons = mainMenuCanvas.GetComponentsInChildren<Button>(true);
        int wired = 0;
        string needle = (levelsButtonName ?? "LEVELS").ToLowerInvariant();
        foreach (var b in buttons)
        {
            // Skip buttons that are children of the panel itself
            if (b.GetComponentInParent<LevelSelectController>() != null) continue;
            if (!b.name.ToLowerInvariant().Contains(needle))
            {
                // Also check the TMP label text inside the button
                var lbl = b.GetComponentInChildren<TMP_Text>(true);
                if (lbl == null) continue;
                if (!lbl.text.ToLowerInvariant().Contains(needle)) continue;
            }
            int beforeCount = b.onClick.GetPersistentEventCount();
            ClearAllListeners(b);
            // Use a persistent listener in the editor so it survives a save +
            // works after restart; falls back to runtime AddListener at runtime.
#if UNITY_EDITOR
            UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, ctrl.Show);
            UnityEditor.EditorUtility.SetDirty(b);
            int afterCount = b.onClick.GetPersistentEventCount();
            // Sanity check — if there's more than one persistent listener,
            // something else (an inspector wire? another script?) is also
            // listening and the button might still call PlayGame too.
            if (afterCount != 1)
                Debug.LogWarning($"[MenuSetupHelper] WIRING BUG: '{b.name}' has " +
                                 $"{afterCount} persistent listeners after wiring (expected 1). " +
                                 "Open the button in Inspector → OnClick to see what else is hooked.");
#else
            b.onClick.AddListener(ctrl.Show);
            int afterCount = b.onClick.GetPersistentEventCount();
#endif
            wired++;
            Debug.Log($"[MenuSetupHelper] ✔ Wired '{b.name}' → LevelSelectController.Show() " +
                      $"(was {beforeCount} listener{(beforeCount==1?"":"s")}, now {afterCount}).");
        }

        if (wired == 0)
            Debug.LogWarning($"[MenuSetupHelper] No button matching '{levelsButtonName}' found in canvas. " +
                             "Either rename your Levels button to contain that text, or assign the panel " +
                             "manually via OnClick → LevelSelectController.Show().");
    }

    // ═══════════════════════════════════════════════════════════
    // Tile builder
    // ═══════════════════════════════════════════════════════════
    LevelSelectController.LevelEntry BuildLevelTile(Transform parent, int index,
                                                    float tileW, float tileH,
                                                    float x, float y,
                                                    Sprite tileSp, Sprite starSp, Sprite checkSp,
                                                    LevelSelectController ctrl)
    {
        var tile = new GameObject($"Level_{index + 1}", typeof(RectTransform));
        tile.transform.SetParent(parent, false);
        var trt = (RectTransform)tile.transform;
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(x, y);
        trt.sizeDelta = new Vector2(tileW, tileH);

        var bg = tile.AddComponent<Image>();
        bg.sprite = tileSp;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.13f, 0.18f, 0.32f, 0.95f);

        var btn = tile.AddComponent<Button>();
        btn.targetGraphic = bg;
        // Disable Button's own color tinting so our selection highlight controls the color.
        btn.transition = Selectable.Transition.None;

        // Mouse hover → move the focus to this tile (so mouse + joystick share state).
        var trigger = tile.AddComponent<EventTrigger>();
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        int capturedIdx = index;
        enterEntry.callback.AddListener((_) => { if (ctrl != null) ctrl.SetSelected(capturedIdx); });
        trigger.triggers.Add(enterEntry);

        // Number
        var num = NewTextChild(tile.transform, "Number", (index + 1).ToString(), 78,
                               new Color(1f, 1f, 1f, 1f), TextAlignmentOptions.Center);
        var nrt = num.rectTransform;
        nrt.anchorMin = new Vector2(0f, 0f); nrt.anchorMax = new Vector2(1f, 1f);
        nrt.offsetMin = new Vector2(0f, 24f); nrt.offsetMax = new Vector2(0f, -12f);
        num.fontStyle = FontStyles.Bold;
        num.outlineWidth = 0.18f;
        num.outlineColor = new Color(0.05f, 0.02f, 0.12f, 1f);
        num.raycastTarget = false;

        // Stars row at bottom
        var starsRow = new GameObject("Stars", typeof(RectTransform));
        starsRow.transform.SetParent(tile.transform, false);
        var srt = (RectTransform)starsRow.transform;
        srt.anchorMin = new Vector2(0.5f, 0f); srt.anchorMax = new Vector2(0.5f, 0f); srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 14f);
        srt.sizeDelta = new Vector2(120f, 30f);
        var starImgs = new Image[3];
        for (int s = 0; s < 3; s++)
        {
            var sGO = new GameObject($"Star_{s + 1}", typeof(RectTransform));
            sGO.transform.SetParent(starsRow.transform, false);
            var srt2 = (RectTransform)sGO.transform;
            srt2.anchorMin = new Vector2(0f, 0.5f); srt2.anchorMax = new Vector2(0f, 0.5f); srt2.pivot = new Vector2(0f, 0.5f);
            srt2.anchoredPosition = new Vector2(s * 36f, 0f);
            srt2.sizeDelta = new Vector2(28f, 28f);
            var img = sGO.AddComponent<Image>();
            img.sprite = starSp;
            img.color = new Color(1f, 1f, 1f, 0.12f);
            img.raycastTarget = false;
            starImgs[s] = img;
        }

        // Checkmark badge top-left
        var checkGO = new GameObject("Check", typeof(RectTransform));
        checkGO.transform.SetParent(tile.transform, false);
        var ckrt = (RectTransform)checkGO.transform;
        ckrt.anchorMin = new Vector2(0f, 1f); ckrt.anchorMax = new Vector2(0f, 1f); ckrt.pivot = new Vector2(0f, 1f);
        ckrt.anchoredPosition = new Vector2(8f, -8f);
        ckrt.sizeDelta = new Vector2(32f, 32f);
        var check = checkGO.AddComponent<Image>();
        check.sprite = checkSp;
        check.color = new Color(0.30f, 0.95f, 0.40f, 1f);
        check.raycastTarget = false;
        check.enabled = false; // turned on by RefreshAll when score > 0

        // Optional best-time badge top-right (placeholder)
        var timeGO = NewTextChild(tile.transform, "Time", "", 18,
                                  new Color(0.55f, 0.85f, 1f, 0.85f), TextAlignmentOptions.TopRight);
        var trrt = timeGO.rectTransform;
        trrt.anchorMin = new Vector2(0f, 1f); trrt.anchorMax = new Vector2(1f, 1f); trrt.pivot = new Vector2(1f, 1f);
        trrt.anchoredPosition = new Vector2(-10f, -10f);
        trrt.sizeDelta = new Vector2(120f, 24f);
        timeGO.fontStyle = FontStyles.Bold;
        timeGO.raycastTarget = false;

        return new LevelSelectController.LevelEntry
        {
            displayName = $"LEVEL {index + 1}",
            button = btn,
            label = num,
            stars = starImgs,
            checkmark = check,
            bestTimeLabel = timeGO,
            tileBackground = bg,
        };
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════
    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Image NewImageChild(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI NewTextChild(Transform parent, string name, string text, float size,
                                        Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        return t;
    }

    static TMP_Text AddFooterLabel(Transform parent, string name, string text, Vector2 pos, float width)
    {
        var t = NewTextChild(parent, name, text, 28, new Color(1f, 1f, 1f, 0.92f), TextAlignmentOptions.Center);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 40f);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 2f;
        t.raycastTarget = false;
        return t;
    }

    static TMP_Text AddFooterStarLabel(Transform parent, string text, Vector2 pos, float width, Sprite starSp)
    {
        // Use a wider group so icon + text can sit side-by-side without overlap
        float groupW = Mathf.Max(width, 200f);
        var groupGO = new GameObject("StarsTotal", typeof(RectTransform));
        groupGO.transform.SetParent(parent, false);
        var grt = (RectTransform)groupGO.transform;
        grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0.5f, 0.5f);
        grt.anchoredPosition = pos;
        grt.sizeDelta = new Vector2(groupW, 40f);

        // Star icon anchored to LEFT edge of the group
        const float iconSize = 32f;
        const float iconLeftPad = 18f;
        const float gapAfterIcon = 12f;

        var iconGO = new GameObject("StarIcon", typeof(RectTransform));
        iconGO.transform.SetParent(groupGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = starSp;
        iconImg.color = new Color(1f, 0.9f, 0.3f, 1f);
        iconImg.raycastTarget = false;
        var irt = (RectTransform)iconGO.transform;
        irt.anchorMin = new Vector2(0f, 0.5f);
        irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(iconLeftPad, 0f);
        irt.sizeDelta = new Vector2(iconSize, iconSize);

        // Text fills from "after the icon" to the right edge — no overlap possible.
        var t = NewTextChild(groupGO.transform, "Text", text, 28,
                             new Color(1f, 1f, 1f, 0.92f), TextAlignmentOptions.MidlineLeft);
        var trt = t.rectTransform;
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.offsetMin = new Vector2(iconLeftPad + iconSize + gapAfterIcon, 0f);
        trt.offsetMax = new Vector2(-10f, 0f);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 2f;
        t.raycastTarget = false;
        return t;
    }

    // ─── Procedural sprites ──────────────────────────────────
    static Sprite BuildTileSprite()
    {
        const int size = 128, r = 22;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(0, Mathf.Max(r - x, x - (size - 1 - r)));
            float dy = Mathf.Max(0, Mathf.Max(r - y, y - (size - 1 - r)));
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01((r - d) / 1.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    /// <summary>Sliced rounded rect for big card / progress bar.</summary>
    static Sprite BuildRoundedSprite(int corner)
    {
        int size = corner * 2 + 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float a = 1f;
            bool L = x < corner, R = x >= size - corner;
            bool B = y < corner, T = y >= size - corner;
            if ((L || R) && (B || T))
            {
                float cx = L ? corner : size - 1 - corner;
                float cy = B ? corner : size - 1 - corner;
                float dx = x - cx, dy = y - cy;
                a = Mathf.Clamp01(corner - Mathf.Sqrt(dx * dx + dy * dy));
            }
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(corner, corner, corner, corner));
    }

    static Sprite BuildStarSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float rOut = size * 0.46f;
        float rIn = rOut * 0.44f;
        var verts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float r = (i % 2 == 0) ? rOut : rIn;
            float ang = -Mathf.PI / 2f + i * Mathf.PI / 5f;
            verts[i] = c + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, PointInPolygon(new Vector2(x, y), verts)
                ? Color.white : new Color(1f, 1f, 1f, 0f));
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildCheckmarkSprite()
    {
        // Simple thick-stroke checkmark drawn as two line segments
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
        DrawLine(tex, new Vector2(12, 32), new Vector2(28, 18), 6f);
        DrawLine(tex, new Vector2(28, 18), new Vector2(54, 46), 6f);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static void DrawLine(Texture2D tex, Vector2 a, Vector2 b, float thickness)
    {
        for (int y = 0; y < tex.height; y++)
        for (int x = 0; x < tex.width; x++)
        {
            float d = DistancePointSegment(new Vector2(x, y), a, b);
            float alpha = Mathf.Clamp01(thickness - d);
            if (alpha > 0f)
            {
                var prev = tex.GetPixel(x, y);
                var newC = new Color(1f, 1f, 1f, Mathf.Max(prev.a, alpha));
                tex.SetPixel(x, y, newC);
            }
        }
    }

    static float DistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
        Vector2 proj = a + ab * t;
        return Vector2.Distance(p, proj);
    }

    static Sprite BuildSimpleCircleSprite()
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
}
