using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Editor-only authoring pipeline for objects that used to be constructed by gameplay code.
/// The generated prefabs and their scene instances are ordinary serialized Unity assets, so
/// player builds only consume prebuilt content.
/// </summary>
public static class PrebuiltRuntimeContentBaker
{
    const string LoadedMenuRepairSessionKey = "RealBuca.VisualPolish.LoadedMenuRepairAttempted.v2";
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/Game.unity";
    const string PrefabFolder = "Assets/Prefabs/Prebuilt";
    const string MaterialFolder = "Assets/Materials/Prebuilt";
    const string AudioRigPrefabPath = PrefabFolder + "/AudioSourceRig.prefab";
    const string NavOutlinePrefabPath = PrefabFolder + "/ArcadeNavOutline.prefab";
    const string TrajectoryPrefabPath = PrefabFolder + "/TrajectoryPossiblePath.prefab";
    const string TrajectoryMaterialPath = MaterialFolder + "/TrajectoryPossiblePath.mat";
    const string ResumePromptPrefabPath = PrefabFolder + "/ReturnPlayerPrompt.prefab";
    const string LockOverlayPrefabPath = PrefabFolder + "/LevelLockOverlay.prefab";
    const string RouteMarkerPrefabPath = PrefabFolder + "/PreferredRouteMarker.prefab";
    const string RouteGoldMaterialPath = MaterialFolder + "/PreferredRouteGold.mat";
    const string RouteHaloMaterialPath = MaterialFolder + "/PreferredRouteHalo.mat";
    const string ObstacleIntroPrefabPath = PrefabFolder + "/ObstacleIntroView.prefab";
    const string HoleInOnePrefabPath = PrefabFolder + "/HoleInOneCelebration3D.prefab";
    const string HoleInOneEnergyMaterialPath = MaterialFolder + "/HoleInOneEnergy.mat";
    const string AimVisualRigPrefabPath = PrefabFolder + "/AimVisualRig.prefab";
    const string PuckEffectsRigPrefabPath = PrefabFolder + "/PuckEffectsRig.prefab";
    const string GameplayStatsPrefabPath = PrefabFolder + "/GameplayStatsPanel.prefab";
    const string MainMenuCountdownPrefabPath = PrefabFolder + "/MainMenuCountdown.prefab";
    const string AimGuideMaterialPath = MaterialFolder + "/AimGuide.mat";
    const string AimDotsMaterialPath = MaterialFolder + "/AimDots.mat";
    const string PowerArcMaterialPath = MaterialFolder + "/PowerArc.mat";
    const string PuckTrailMaterialPath = MaterialFolder + "/PuckTrail.mat";
    const string PuckGlowMaterialPath = MaterialFolder + "/PuckGlow.mat";
    const string AimDotTexturePath = MaterialFolder + "/AimDotTexture.asset";
    const string PuckGlowTexturePath = MaterialFolder + "/PuckGlowTexture.asset";
    const string FailurePanelPrefabPath = PrefabFolder + "/LevelFailedPanel.prefab";
    const string GolfScorecardPrefabPath = PrefabFolder + "/GolfScorecardPanel.prefab";
    const string VideoFolder = "Assets/Video";
    const string TutorialVideoClipPath = VideoFolder + "/HowToPlayArcade.mp4";
    const string TutorialRenderTexturePath = VideoFolder + "/HowToPlayArcade.renderTexture";
    const string DisplayFontPath = "Assets/Fonts/Fonts & Materials/Anton SDF.asset";
    const string UiFontPath = "Assets/Fonts/Fonts & Materials/Oswald Bold SDF.asset";

    /// <summary>
    /// Batch-mode entry point used to verify that the player-facing scripts compile
    /// without changing any authored scenes or assets.
    /// </summary>
    public static void CompileCheck()
    {
        Debug.Log("[PrebuiltRuntimeContentBaker] Compile check complete.");
    }

    [InitializeOnLoadMethod]
    static void RepairLoadedMainMenuVisualPolish()
    {
        if (Application.isBatchMode) return;
        EditorApplication.playModeStateChanged -= OnVisualPolishPlayModeChanged;
        EditorApplication.playModeStateChanged += OnVisualPolishPlayModeChanged;
        EditorApplication.delayCall += TryRepairMainMenuVisualPolish;
    }

    static void OnVisualPolishPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TryRepairMainMenuVisualPolish;
    }

    static void TryRepairMainMenuVisualPolish()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || SessionState.GetBool(LoadedMenuRepairSessionKey, false)) return;
        SessionState.SetBool(LoadedMenuRepairSessionKey, true);

        Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

        try
        {
            MainMenuController[] controllers = FindInScene<MainMenuController>(scene);
            bool needsRepair = false;
            foreach (MainMenuController controller in controllers)
            {
                if (controller.autoStartText == null
                    || controller.autoStartText.transform.parent == null
                    || controller.autoStartText.transform.parent.name != "MainMenuCountdown")
                {
                    needsRepair = true;
                    break;
                }
            }
            if (!needsRepair) return;

            foreach (MainMenuController controller in controllers)
                WireMainMenuCountdown(controller);
            foreach (ArcadeUINavigator navigator in FindInScene<ArcadeUINavigator>(scene))
            {
                navigator.selectedScale = 1.03f;
                navigator.highlightOutlinePadding = 8f;
                navigator.pulseHz = 1.35f;
                EditorUtility.SetDirty(navigator);
            }
            foreach (Button button in FindInScene<Button>(scene))
            {
                ColorBlock colors = button.colors;
                colors.fadeDuration = 0.06f;
                button.colors = colors;
                EditorUtility.SetDirty(button);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[PrebuiltRuntimeContentBaker] Repaired the loaded Main Menu countdown prefab reference.");
        }
        finally
        {
            if (openedHere && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("RealBuca/Prebuild/Step 1 - Core Runtime Objects")]
    public static void BakeCoreRuntimeObjects()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");
        EnsureFolder("Assets/Materials", "Prebuilt");

        GameObject audioRigPrefab = CreateOrUpdateAudioRigPrefab();
        GameObject navOutlinePrefab = CreateOrUpdateNavOutlinePrefab();

        BakeScene(MainMenuScenePath, scene =>
        {
            foreach (AudioManager manager in FindInScene<AudioManager>(scene))
                WireAudioRig(manager, audioRigPrefab);

            foreach (ArcadeUINavigator navigator in FindInScene<ArcadeUINavigator>(scene))
                WireNavigationOutline(navigator, navOutlinePrefab);
        });

        BakeScene(GameScenePath, scene =>
        {
            foreach (AudioManager manager in FindInScene<AudioManager>(scene))
                WireAudioRig(manager, audioRigPrefab);

            foreach (PuckController puck in FindInScene<PuckController>(scene))
                WireTrajectoryCoverage(puck);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] Step 1 complete: audio rigs, arcade outline, and trajectory coverage are serialized.");
    }

    [MenuItem("RealBuca/Prebuild/Step 2 - Main Menu Runtime UI")]
    public static void BakeMainMenuRuntimeUi()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");

        BakeScene(MainMenuScenePath, scene =>
        {
            MainMenuController[] menuControllers = FindInScene<MainMenuController>(scene);
            if (menuControllers.Length == 0)
                throw new InvalidOperationException("MainMenu scene has no MainMenuController.");

            TMP_FontAsset font = menuControllers[0].playLabel != null
                ? menuControllers[0].playLabel.font
                : TMP_Settings.defaultFontAsset;
            GameObject promptPrefab = CreateOrUpdateResumePromptPrefab(font);
            GameObject lockPrefab = CreateOrUpdateLockOverlayPrefab();

            foreach (MainMenuController controller in menuControllers)
                WireResumePrompt(controller, promptPrefab);

            foreach (LevelSelectController levelSelect in FindInScene<LevelSelectController>(scene))
                WireLevelLockOverlays(levelSelect, lockPrefab);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] Step 2 complete: resume prompt and level-lock overlays are serialized.");
    }

    [MenuItem("RealBuca/Prebuild/Step 3 - Preferred Routes")]
    public static void BakePreferredRoutes()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");
        EnsureFolder("Assets/Materials", "Prebuilt");

        Material gold = CreateOrUpdateLitMaterial(RouteGoldMaterialPath, "Preferred Route Gold",
            new Color(1f, 0.62f, 0.06f, 1f), new Color(2.6f, 1.15f, 0.08f, 1f));
        Material halo = CreateOrUpdateLitMaterial(RouteHaloMaterialPath, "Preferred Route Halo",
            new Color(0.90f, 0.38f, 0.025f, 1f), new Color(1.25f, 0.40f, 0.02f, 1f));
        GameObject markerPrefab = CreateOrUpdateRouteMarkerPrefab(gold, halo);

        BakeRouteIntoLevelPrefab(10, Route(2.30f, -2.00f, 2.30f, 0.70f, 1.45f, 3.25f), markerPrefab);
        BakeRouteIntoLevelPrefab(15, Route(2.60f, -2.20f, 0.80f, 0.75f, -1.35f, 3.35f), markerPrefab);
        BakeRouteIntoLevelPrefab(21, Route(-1.05f, -1.80f, 0.85f, 1.10f, -1.05f, 4.00f), markerPrefab);
        BakeRouteIntoLevelPrefab(25, Route(0.00f, -3.00f, 0.65f, 0.35f, -0.35f, 3.10f), markerPrefab);
        BakeRouteIntoLevelPrefab(26, Route(0.00f, -2.00f, 0.00f, 0.35f, 0.00f, 3.05f), markerPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] Step 3 complete: preferred-route markers are serialized in five level prefabs.");
    }

    [MenuItem("RealBuca/Prebuild/Step 4 - Gameplay Presentation Controllers")]
    public static void BakeGameplayPresentationControllers()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");
        EnsureFolder("Assets/Materials", "Prebuilt");

        GameObject obstacleIntroPrefab = CreateOrUpdateObstacleIntroPrefab();
        Material energyMaterial = CreateOrUpdateHoleInOneEnergyMaterial();
        GameObject holeInOnePrefab = CreateOrUpdateHoleInOnePrefab(energyMaterial);

        BakeScene(GameScenePath, scene =>
        {
            foreach (LevelManager manager in FindInScene<LevelManager>(scene))
                WireGameplayPresentationControllers(manager, obstacleIntroPrefab, holeInOnePrefab);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] Step 4 complete: obstacle intro and hole-in-one rigs are serialized.");
    }

    [MenuItem("RealBuca/Prebuild/Step 5 - Remove Legacy Setup Components")]
    public static void RemoveLegacySetupComponents()
    {
        BakeScene(GameScenePath, scene =>
        {
            foreach (BucaSetupHelper helper in FindInScene<BucaSetupHelper>(scene))
                UnityEngine.Object.DestroyImmediate(helper);
        });

        BakeScene(MainMenuScenePath, scene =>
        {
            foreach (MenuSetupHelper helper in FindInScene<MenuSetupHelper>(scene))
                UnityEngine.Object.DestroyImmediate(helper);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] Step 5 complete: legacy setup helpers were removed from player scenes.");
    }

    [MenuItem("RealBuca/Prebuild/Step 6 - Visual Polish")]
    public static void BakeVisualPolish()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");
        EnsureFolder("Assets/Materials", "Prebuilt");

        Texture2D aimDots = CreateOrUpdateSoftDotTexture(AimDotTexturePath, "AimDotTexture", 64, 16, 6.25f);
        Texture2D puckGlow = CreateOrUpdateSoftDotTexture(PuckGlowTexturePath, "PuckGlowTexture", 32, 32, 13f);
        Material aimGuide = CreateOrUpdateSpriteMaterial(AimGuideMaterialPath, "AimGuide", null);
        Material aimDotMaterial = CreateOrUpdateSpriteMaterial(AimDotsMaterialPath, "AimDots", aimDots);
        Material powerArcMaterial = CreateOrUpdateSpriteMaterial(PowerArcMaterialPath, "PowerArc", null);
        Material puckTrailMaterial = CreateOrUpdateSpriteMaterial(PuckTrailMaterialPath, "PuckTrail", null);
        Material puckGlowMaterial = CreateOrUpdateSpriteMaterial(PuckGlowMaterialPath, "PuckGlow", puckGlow);
        GameObject aimRigPrefab = CreateOrUpdateAimVisualRigPrefab(aimGuide, aimDotMaterial, powerArcMaterial);
        GameObject puckEffectsPrefab = CreateOrUpdatePuckEffectsRigPrefab(puckTrailMaterial, puckGlowMaterial);

        BakeScene(GameScenePath, scene =>
        {
            foreach (PuckController puck in FindInScene<PuckController>(scene))
            {
                WireAimVisualRig(puck, aimRigPrefab);
                WirePuckEffectsRig(puck, puckEffectsPrefab);
            }

            foreach (LevelManager manager in FindInScene<LevelManager>(scene))
            {
                WireGameplayStatsPanel(manager);
                if (manager.puckController != null)
                    manager.puckTrail = manager.puckController.GetComponentInChildren<TrailRenderer>(true);
                EditorUtility.SetDirty(manager);
            }
        });

        BakeScene(MainMenuScenePath, scene =>
        {
            foreach (MainMenuController controller in FindInScene<MainMenuController>(scene))
                WireMainMenuCountdown(controller);

            foreach (ArcadeUINavigator navigator in FindInScene<ArcadeUINavigator>(scene))
            {
                navigator.selectedScale = 1.03f;
                navigator.highlightOutlinePadding = 8f;
                navigator.pulseHz = 1.35f;
                EditorUtility.SetDirty(navigator);
            }

            foreach (Button button in FindInScene<Button>(scene))
            {
                ColorBlock colors = button.colors;
                colors.fadeDuration = 0.06f;
                button.colors = colors;
                EditorUtility.SetDirty(button);
            }
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BakeTypographyAndFailurePanel();
        Debug.Log("[PrebuiltRuntimeContentBaker] Step 6 complete: aim, stats, puck effects, countdown, and stable menu focus are serialized.");
    }

    [MenuItem("RealBuca/Prebuild/Refresh Gameplay Stats Panel Only")]
    public static void BakeGameplayStatsPanelOnly()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");

        BakeScene(GameScenePath, scene =>
        {
            foreach (LevelManager manager in FindInScene<LevelManager>(scene))
                WireGameplayStatsPanel(manager);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] Gameplay stats panel refreshed and serialized.");
    }

    [MenuItem("RealBuca/Prebuild/Step 8 - In-Game Transactions")]
    public static void BakeInGameTransactions()
    {
        BakeScene(MainMenuScenePath, scene =>
        {
            LuxoddGameBridge[] bridges = FindInScene<LuxoddGameBridge>(scene);
            if (bridges.Length != 1)
                throw new InvalidOperationException(
                    $"MainMenu must contain exactly one LuxoddGameBridge; found {bridges.Length}.");

            LuxoddGameBridge bridge = bridges[0];
            BucaInGameTransactionController controller =
                bridge.GetComponent<BucaInGameTransactionController>();
            if (controller == null)
                controller = bridge.gameObject.AddComponent<BucaInGameTransactionController>();

            // Read the already-authored plugin references without modifying the
            // protected Luxodd bridge or any plugin-owned script.
            SerializedObject serializedBridge = new SerializedObject(bridge);
            SerializedProperty socketProperty =
                serializedBridge.FindProperty("_webSocketService");
            SerializedProperty handlerProperty =
                serializedBridge.FindProperty("_commandHandler");

            MonoBehaviour socket = socketProperty != null
                ? socketProperty.objectReferenceValue as MonoBehaviour : null;
            MonoBehaviour handler = handlerProperty != null
                ? handlerProperty.objectReferenceValue as MonoBehaviour : null;
            if (socket == null || handler == null)
                throw new InvalidOperationException(
                    "LuxoddGameBridge is missing its serialized WebSocket service references.");

            controller.Configure(socket, handler);
            EditorUtility.SetDirty(controller);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] In-game Continue/Restart coordinator serialized.");
    }

    [MenuItem("RealBuca/Prebuild/Step 9 - Golf Scorecard")]
    public static void BakeGolfScorecard()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");
        TMP_FontAsset displayFont = LoadRequiredFont(DisplayFontPath, "display");
        TMP_FontAsset uiFont = LoadRequiredFont(UiFontPath, "UI");
        GameObject scorecardPrefab = CreateOrUpdateGolfScorecardPrefab(uiFont, displayFont);

        BakeScene(GameScenePath, scene =>
        {
            foreach (LevelCompletePanel panel in FindInScene<LevelCompletePanel>(scene))
                WireGolfScorecard(panel, scorecardPrefab);
            foreach (GameCompletePanel panel in FindInScene<GameCompletePanel>(scene))
                WireGolfScorecard(panel, scorecardPrefab);

            // Re-bake the compact live HUD so its serialized prefab includes
            // the new TO PAR row alongside strokes, par and points.
            foreach (LevelManager manager in FindInScene<LevelManager>(scene))
                WireGameplayStatsPanel(manager);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrebuiltRuntimeContentBaker] Golf scorecard and live to-par HUD serialized.");
    }

    [MenuItem("RealBuca/Prebuild/Step 7 - Typography And Failure Panel")]
    public static void BakeTypographyAndFailurePanel()
    {
        EnsureFolder("Assets/Prefabs", "Prebuilt");
        EnsureFolder("Assets", "Video");

        TMP_FontAsset displayFont = LoadRequiredFont(DisplayFontPath, "display");
        TMP_FontAsset uiFont = LoadRequiredFont(UiFontPath, "UI");
        VideoClip tutorialVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(TutorialVideoClipPath);
        if (tutorialVideo == null)
            throw new InvalidOperationException($"Missing tutorial video clip at '{TutorialVideoClipPath}'.");
        RenderTexture tutorialRenderTexture = CreateOrUpdateTutorialRenderTexture();
        int updatedTextCount = 0;

        BakeScene(GameScenePath, scene =>
        {
            updatedTextCount += ApplyTypography(FindInScene<TMP_Text>(scene), displayFont, uiFont);

            foreach (TimerDisplay timer in FindInScene<TimerDisplay>(scene))
            {
                timer.hudFont = displayFont;
                timer.statsFont = uiFont;
                EditorUtility.SetDirty(timer);
            }

            foreach (LevelFailedPanel panel in FindInScene<LevelFailedPanel>(scene))
                PolishAndSaveFailurePanel(
                    panel, displayFont, uiFont, tutorialVideo, tutorialRenderTexture);
        });

        BakeScene(MainMenuScenePath, scene =>
        {
            updatedTextCount += ApplyTypography(FindInScene<TMP_Text>(scene), displayFont, uiFont);

            foreach (MainMenuController controller in FindInScene<MainMenuController>(scene))
            {
                controller.autoStartFont = uiFont;
                EditorUtility.SetDirty(controller);
            }
        });

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                updatedTextCount += ApplyTypography(
                    prefabRoot.GetComponentsInChildren<TMP_Text>(true), displayFont, uiFont);

                foreach (TimerDisplay timer in prefabRoot.GetComponentsInChildren<TimerDisplay>(true))
                {
                    timer.hudFont = displayFont;
                    timer.statsFont = uiFont;
                    EditorUtility.SetDirty(timer);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PrebuiltRuntimeContentBaker] Typography and failure panel serialized. " +
                  $"Updated {updatedTextCount} TMP text components with Anton + Oswald Bold.");
    }

    static GameObject CreateOrUpdateObstacleIntroPrefab()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        Sprite uiSprite = BuiltinUiSprite();
        GameObject root = new GameObject("ObstacleIntroView", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
            typeof(CanvasGroup), typeof(ObstacleIntroController));
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 4800;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;

            Image dim = CreateUiImage(root.transform, "Dim", uiSprite,
                new Color(0.005f, 0.012f, 0.035f, 0.88f), Vector2.zero,
                new Vector2(1920f, 1080f)).GetComponent<Image>();
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            GameObject cardMotion = new GameObject("CardMotion", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform card = cardMotion.GetComponent<RectTransform>();
            card.SetParent(root.transform, false);
            SetCenteredRect(card, Vector2.zero, new Vector2(1420f, 820f));
            CanvasGroup cardGroup = cardMotion.GetComponent<CanvasGroup>();
            cardGroup.alpha = 0f;
            cardGroup.interactable = false;
            cardGroup.blocksRaycasts = false;

            Image glow = CreateUiImage(card, "CardGlow", uiSprite,
                new Color(0.12f, 0.82f, 1f, 0.18f), Vector2.zero,
                new Vector2(1420f, 820f)).GetComponent<Image>();
            glow.raycastTarget = false;

            Image cardImage = CreateUiImage(card, "Card", uiSprite,
                new Color(0.012f, 0.030f, 0.062f, 0.99f), Vector2.zero,
                new Vector2(1380f, 780f)).GetComponent<Image>();
            cardImage.raycastTarget = false;
            Outline cardOutline = cardImage.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.20f, 0.88f, 1f, 0.92f);
            cardOutline.effectDistance = new Vector2(4f, -4f);

            Image topAccent = CreateUiImage(cardImage.transform, "TopAccent", uiSprite,
                Color.cyan, new Vector2(0f, 376f), new Vector2(1320f, 7f)).GetComponent<Image>();
            topAccent.raycastTarget = false;
            Image sideAccent = CreateUiImage(cardImage.transform, "SideAccent", uiSprite,
                Color.cyan, new Vector2(-666f, 0f), new Vector2(7f, 730f)).GetComponent<Image>();
            sideAccent.raycastTarget = false;

            Image iconImage = CreateUiImage(cardImage.transform, "IconPanel", uiSprite,
                new Color(0.04f, 0.25f, 0.32f, 1f), new Vector2(-465f, 72f),
                new Vector2(250f, 250f)).GetComponent<Image>();
            iconImage.raycastTarget = false;
            Outline iconOutline = iconImage.gameObject.AddComponent<Outline>();
            iconOutline.effectColor = new Color(0.18f, 0.90f, 1f, 0.76f);
            iconOutline.effectDistance = new Vector2(3f, -3f);

            TMP_Text iconText = CreateUiText(iconImage.transform, "IconText", font, "RIM", 48f,
                Color.cyan, Vector2.zero, new Vector2(220f, 100f));
            iconText.fontStyle = FontStyles.Bold;
            iconText.characterSpacing = 2f;
            iconText.raycastTarget = false;

            TMP_Text eyebrow = CreateUiText(cardImage.transform, "Eyebrow", font,
                "LEVEL 1  •  NEW TRICK!", 23f, new Color(0.62f, 0.90f, 1f, 1f),
                new Vector2(115f, 305f), new Vector2(1040f, 42f));
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.characterSpacing = 2f;

            TMP_Text title = CreateUiText(cardImage.transform, "Title", font,
                "WATCH IT BOUNCE!", 58f, Color.white,
                new Vector2(115f, 225f), new Vector2(1040f, 90f));
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true;
            title.fontSizeMin = 36f;
            title.fontSizeMax = 58f;
            Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
            titleShadow.effectDistance = new Vector2(3f, -3f);

            TMP_Text description = CreateUiText(cardImage.transform, "Description", font,
                "HIT THE GLOWING RAIL  →  BOUNCE  →  GOAL!", 37f,
                new Color(0.92f, 0.97f, 1f, 1f), new Vector2(115f, 54f),
                new Vector2(760f, 160f));
            description.fontStyle = FontStyles.Bold;
            description.textWrappingMode = TextWrappingModes.Normal;
            description.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text advice = CreateUiText(cardImage.transform, "Advice", font,
                "AIM  •  BOUNCE  •  SCORE!", 31f, new Color(1f, 0.72f, 0.24f, 1f),
                new Vector2(0f, -183f), new Vector2(1120f, 58f));
            advice.fontStyle = FontStyles.Bold;
            advice.characterSpacing = 2f;

            TMP_Text hint = CreateUiText(cardImage.transform, "Hint", font,
                "PURPLE OR SKIP TO CONTINUE", 20f, new Color(0.62f, 0.76f, 0.90f, 1f),
                new Vector2(0f, -310f), new Vector2(800f, 42f));
            hint.characterSpacing = 2f;

            GameObject skipObject = CreateUiImage(root.transform, "SkipButton", uiSprite,
                new Color(0.43f, 0.075f, 0.76f, 1f), Vector2.zero,
                new Vector2(240f, 80f));
            RectTransform skipRect = skipObject.GetComponent<RectTransform>();
            skipRect.anchorMin = skipRect.anchorMax = new Vector2(1f, 1f);
            skipRect.pivot = new Vector2(1f, 1f);
            skipRect.anchoredPosition = new Vector2(-34f, -28f);
            Button skipButton = skipObject.AddComponent<Button>();
            skipButton.targetGraphic = skipObject.GetComponent<Image>();
            Navigation navigation = skipButton.navigation;
            navigation.mode = Navigation.Mode.None;
            skipButton.navigation = navigation;
            Outline skipOutline = skipObject.AddComponent<Outline>();
            skipOutline.effectColor = new Color(0.75f, 0.35f, 1f, 0.95f);
            skipOutline.effectDistance = new Vector2(3f, -3f);
            TMP_Text skipLabel = CreateUiText(skipObject.transform, "Label", font, "SKIP", 30f,
                Color.white, Vector2.zero, new Vector2(210f, 60f));
            skipLabel.fontStyle = FontStyles.Bold;
            skipLabel.characterSpacing = 3f;
            skipLabel.raycastTarget = false;

            SerializedObject serialized = new SerializedObject(root.GetComponent<ObstacleIntroController>());
            serialized.FindProperty("_canvasRoot").objectReferenceValue = root;
            serialized.FindProperty("_rootGroup").objectReferenceValue = rootGroup;
            serialized.FindProperty("_cardGroup").objectReferenceValue = cardGroup;
            serialized.FindProperty("_card").objectReferenceValue = card;
            serialized.FindProperty("_glow").objectReferenceValue = glow;
            serialized.FindProperty("_cardImage").objectReferenceValue = cardImage;
            serialized.FindProperty("_topAccent").objectReferenceValue = topAccent;
            serialized.FindProperty("_sideAccent").objectReferenceValue = sideAccent;
            serialized.FindProperty("_iconImage").objectReferenceValue = iconImage;
            serialized.FindProperty("_eyebrowText").objectReferenceValue = eyebrow;
            serialized.FindProperty("_iconText").objectReferenceValue = iconText;
            serialized.FindProperty("_titleText").objectReferenceValue = title;
            serialized.FindProperty("_descriptionText").objectReferenceValue = description;
            serialized.FindProperty("_adviceText").objectReferenceValue = advice;
            serialized.FindProperty("_hintText").objectReferenceValue = hint;
            serialized.FindProperty("_skipButton").objectReferenceValue = skipButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return PrefabUtility.SaveAsPrefabAsset(root, ObstacleIntroPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static Material CreateOrUpdateHoleInOneEnergyMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(HoleInOneEnergyMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            material = new Material(shader) { name = "Hole In One Energy" };
            AssetDatabase.CreateAsset(material, HoleInOneEnergyMaterialPath);
        }

        Color color = new Color(0.22f, 0.94f, 1f, 1f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject CreateOrUpdateHoleInOnePrefab(Material energyMaterial)
    {
        const int puckCount = 4;
        const int ringCount = 3;
        GameObject root = new GameObject("HoleInOneCelebration3D", typeof(HoleInOneCelebration3D));
        try
        {
            GameObject effectObject = new GameObject("HoleInOneCelebration3D_WorldSpace");
            effectObject.transform.SetParent(root.transform, false);

            TextMeshPro title = CreateWorldText(effectObject.transform, "HoleInOneTitle3D",
                "HOLE IN ONE", 8.2f, FontStyles.Bold);
            TextMeshPro subtitle = CreateWorldText(effectObject.transform, "HoleInOneSubtitle3D",
                "LEGENDARY SHOT", 3.6f, FontStyles.Bold | FontStyles.Italic);

            var rings = new List<LineRenderer>(ringCount);
            for (int i = 0; i < ringCount; i++)
            {
                GameObject ringObject = new GameObject($"EnergyRing3D_{i + 1}", typeof(LineRenderer));
                ringObject.transform.SetParent(effectObject.transform, false);
                LineRenderer ring = ringObject.GetComponent<LineRenderer>();
                ring.useWorldSpace = true;
                ring.loop = true;
                ring.positionCount = 48;
                ring.numCapVertices = 4;
                ring.numCornerVertices = 3;
                ring.widthMultiplier = 0.10f;
                ring.textureMode = LineTextureMode.Stretch;
                ring.alignment = LineAlignment.View;
                ring.sharedMaterial = energyMaterial;
                ring.enabled = false;
                rings.Add(ring);
            }

            var pucks = new List<GameObject>(puckCount);
            var filters = new List<MeshFilter>(puckCount);
            var renderers = new List<MeshRenderer>(puckCount);
            var trails = new List<TrailRenderer>(puckCount);
            for (int i = 0; i < puckCount; i++)
            {
                GameObject puck = new GameObject($"AwardPuck3D_{i + 1}",
                    typeof(MeshFilter), typeof(MeshRenderer), typeof(TrailRenderer));
                puck.transform.SetParent(effectObject.transform, false);
                MeshFilter filter = puck.GetComponent<MeshFilter>();
                MeshRenderer renderer = puck.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                TrailRenderer trail = puck.GetComponent<TrailRenderer>();
                trail.time = 0.34f;
                trail.minVertexDistance = 0.035f;
                trail.startWidth = 0.11f;
                trail.endWidth = 0.012f;
                trail.numCapVertices = 4;
                trail.numCornerVertices = 4;
                trail.alignment = LineAlignment.View;
                trail.sharedMaterial = energyMaterial;
                trail.startColor = i % 2 == 0
                    ? new Color(0.20f, 0.94f, 1f, 0.88f)
                    : new Color(1f, 0.70f, 0.16f, 0.88f);
                trail.endColor = new Color(1f, 0.20f, 0.58f, 0f);
                trail.emitting = false;
                puck.SetActive(false);
                pucks.Add(puck);
                filters.Add(filter);
                renderers.Add(renderer);
                trails.Add(trail);
            }

            GameObject burstObject = new GameObject("AwardPuckBurst3D", typeof(ParticleSystem));
            burstObject.transform.SetParent(effectObject.transform, false);
            ParticleSystem burst = burstObject.GetComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = burst.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 6.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.18f, 0.92f, 1f, 1f), new Color(1f, 0.55f, 0.12f, 1f));
            main.gravityModifier = 0.65f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 72;
            ParticleSystem.EmissionModule emission = burst.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 64) });
            ParticleSystem.ShapeModule shape = burst.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.42f;
            ParticleSystem.RotationOverLifetimeModule rotation = burst.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
            rotation.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
            rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            ParticleSystemRenderer burstRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
            burstRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            burstRenderer.sharedMaterial = energyMaterial;
            burstRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            burstRenderer.receiveShadows = false;

            GameObject lightObject = new GameObject("HoleInOneAwardLight3D", typeof(Light));
            lightObject.transform.SetParent(effectObject.transform, false);
            Light awardLight = lightObject.GetComponent<Light>();
            awardLight.type = LightType.Point;
            awardLight.color = new Color(1f, 0.72f, 0.22f);
            awardLight.range = 7.5f;
            awardLight.intensity = 0f;
            awardLight.shadows = LightShadows.None;
            awardLight.enabled = false;

            SerializedObject serialized = new SerializedObject(root.GetComponent<HoleInOneCelebration3D>());
            serialized.FindProperty("_effectRoot").objectReferenceValue = effectObject.transform;
            serialized.FindProperty("_title").objectReferenceValue = title;
            serialized.FindProperty("_subtitle").objectReferenceValue = subtitle;
            SetObjectArray(serialized.FindProperty("_orbitPucks"), pucks);
            SetObjectArray(serialized.FindProperty("_orbitFilters"), filters);
            SetObjectArray(serialized.FindProperty("_orbitRenderers"), renderers);
            SetObjectArray(serialized.FindProperty("_orbitTrails"), trails);
            SetObjectArray(serialized.FindProperty("_rings"), rings);
            serialized.FindProperty("_burst").objectReferenceValue = burst;
            serialized.FindProperty("_burstRenderer").objectReferenceValue = burstRenderer;
            serialized.FindProperty("_awardLight").objectReferenceValue = awardLight;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            effectObject.SetActive(false);
            return PrefabUtility.SaveAsPrefabAsset(root, HoleInOnePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static TextMeshPro CreateWorldText(Transform parent, string name, string value,
        float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        textObject.transform.SetParent(parent, false);
        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.characterSpacing = 5f;
        text.outlineWidth = 0.20f;
        text.outlineColor = new Color(0.01f, 0.025f, 0.07f, 1f);
        text.rectTransform.sizeDelta = new Vector2(15f, 2.2f);
        text.rectTransform.localScale = Vector3.one * 0.22f;
        return text;
    }

    static void WireGameplayPresentationControllers(LevelManager manager,
        GameObject obstacleIntroPrefab, GameObject holeInOnePrefab)
    {
        Transform obstacleTransform = manager.transform.Find("ObstacleIntroView");
        if (obstacleTransform != null && obstacleTransform.GetComponent<ObstacleIntroController>() == null)
        {
            UnityEngine.Object.DestroyImmediate(obstacleTransform.gameObject);
            obstacleTransform = null;
        }
        GameObject obstacleObject = obstacleTransform != null
            ? obstacleTransform.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(obstacleIntroPrefab, manager.transform);
        obstacleObject.name = "ObstacleIntroView";
        obstacleObject.SetActive(false);
        ObstacleIntroController obstacleIntro = obstacleObject.GetComponent<ObstacleIntroController>();

        Transform celebrationTransform = manager.transform.Find("HoleInOneCelebration3D");
        if (celebrationTransform != null && celebrationTransform.GetComponent<HoleInOneCelebration3D>() == null)
        {
            UnityEngine.Object.DestroyImmediate(celebrationTransform.gameObject);
            celebrationTransform = null;
        }
        GameObject celebrationObject = celebrationTransform != null
            ? celebrationTransform.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(holeInOnePrefab, manager.transform);
        celebrationObject.name = "HoleInOneCelebration3D";
        celebrationObject.SetActive(true);
        HoleInOneCelebration3D celebration = celebrationObject.GetComponent<HoleInOneCelebration3D>();

        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("_obstacleIntro").objectReferenceValue = obstacleIntro;
        serialized.FindProperty("holeInOneCelebration").objectReferenceValue = celebration;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    static Texture2D CreateOrUpdateSoftDotTexture(string path, string name,
        int width, int height, float radius)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = name
            };
            AssetDatabase.CreateAsset(texture, path);
        }
        else if (texture.width != width || texture.height != height)
        {
            texture.Reinitialize(width, height, TextureFormat.RGBA32, false);
        }

        texture.name = name;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 1;
        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        const float feather = 1.75f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((radius - distance) / feather));
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    static Material CreateOrUpdateSpriteMaterial(string path, string name, Texture2D texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = name;
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture != null ? texture : Texture2D.whiteTexture);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture != null ? texture : Texture2D.whiteTexture);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        material.renderQueue = 3000;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject CreateOrUpdateAimVisualRigPrefab(Material aimMaterial,
        Material dotMaterial, Material powerMaterial)
    {
        GameObject root = new GameObject("AimVisualRig");
        try
        {
            LineRenderer aim = CreateLineRenderer(root.transform, "AimDirection", aimMaterial,
                2, 0.07f, 0.025f, LineTextureMode.Stretch,
                BuildLineGradient(new Color(0.18f, 0.94f, 1f), new Color(1f, 0.78f, 0.24f), 0.80f, 0.28f));
            LineRenderer preview = CreateLineRenderer(root.transform, "TrajectoryDots", dotMaterial,
                0, 0.085f, 0.085f, LineTextureMode.Tile,
                BuildLineGradient(new Color(0.20f, 0.92f, 1f), new Color(1f, 0.76f, 0.20f), 0.96f, 0.34f));
            LineRenderer power = CreateLineRenderer(root.transform, "PowerArc", powerMaterial,
                0, 0.075f, 0.075f, LineTextureMode.Stretch,
                BuildLineGradient(new Color(1f, 0.90f, 0.25f), new Color(1f, 0.28f, 0.62f), 0.95f, 0.95f));
            LineRenderer coverage = CreateLineRenderer(root.transform, "PostBounceCoverageDisabled", aimMaterial,
                0, 0.01f, 0.01f, LineTextureMode.Stretch,
                BuildLineGradient(Color.clear, Color.clear, 0f, 0f));

            aim.enabled = false;
            preview.enabled = false;
            power.enabled = false;
            coverage.enabled = false;
            return PrefabUtility.SaveAsPrefabAsset(root, AimVisualRigPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static LineRenderer CreateLineRenderer(Transform parent, string name, Material material,
        int positionCount, float startWidth, float endWidth, LineTextureMode textureMode,
        Gradient gradient)
    {
        GameObject child = new GameObject(name, typeof(LineRenderer));
        child.transform.SetParent(parent, false);
        LineRenderer line = child.GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.textureMode = textureMode;
        line.numCornerVertices = 6;
        line.numCapVertices = 6;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.generateLightingData = false;
        line.allowOcclusionWhenDynamic = false;
        line.sharedMaterial = material;
        line.positionCount = positionCount;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.colorGradient = gradient;
        for (int i = 0; i < positionCount; i++)
            line.SetPosition(i, Vector3.zero);
        return line;
    }

    static Gradient BuildLineGradient(Color start, Color end, float startAlpha, float endAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(endAlpha, 1f) });
        return gradient;
    }

    static void WireAimVisualRig(PuckController puck, GameObject prefab)
    {
        var oldObjects = new HashSet<GameObject>();
        if (puck.aimLine != null) oldObjects.Add(puck.aimLine.gameObject);
        if (puck.previewLine != null) oldObjects.Add(puck.previewLine.gameObject);
        if (puck.previewCoverageLine != null) oldObjects.Add(puck.previewCoverageLine.gameObject);
        if (puck.powerArc != null) oldObjects.Add(puck.powerArc.gameObject);

        Transform existing = puck.transform.Find("AimVisualRig");
        if (existing != null)
        {
            oldObjects.Remove(existing.gameObject);
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, puck.transform);
        rig.name = "AimVisualRig";
        rig.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        rig.transform.localScale = Vector3.one;
        SetLayerRecursively(rig.transform, puck.gameObject.layer);

        puck.aimLine = RequireLine(rig.transform, "AimDirection");
        puck.previewLine = RequireLine(rig.transform, "TrajectoryDots");
        puck.powerArc = RequireLine(rig.transform, "PowerArc");
        puck.previewCoverageLine = RequireLine(rig.transform, "PostBounceCoverageDisabled");
        puck.previewCoverageLine.enabled = false;
        puck.previewCoverageLine.positionCount = 0;

        foreach (GameObject oldObject in oldObjects)
        {
            if (oldObject != null && oldObject != rig)
                UnityEngine.Object.DestroyImmediate(oldObject);
        }
        EditorUtility.SetDirty(puck);
    }

    static LineRenderer RequireLine(Transform root, string name)
    {
        Transform child = root.Find(name);
        if (child == null || child.GetComponent<LineRenderer>() == null)
            throw new InvalidOperationException($"Aim visual prefab is missing LineRenderer '{name}'.");
        return child.GetComponent<LineRenderer>();
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }

    static GameObject CreateOrUpdatePuckEffectsRigPrefab(Material trailMaterial, Material glowMaterial)
    {
        GameObject root = new GameObject("PuckEffectsRig");
        try
        {
            GameObject trailObject = new GameObject("PuckTrail", typeof(TrailRenderer));
            trailObject.transform.SetParent(root.transform, false);
            ConfigurePuckTrail(trailObject.GetComponent<TrailRenderer>(), trailMaterial);

            GameObject glowObject = new GameObject("PuckIdleGlow", typeof(ParticleSystem));
            glowObject.transform.SetParent(root.transform, false);
            ConfigurePuckGlow(glowObject.GetComponent<ParticleSystem>(), glowMaterial);
            return PrefabUtility.SaveAsPrefabAsset(root, PuckEffectsRigPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void WirePuckEffectsRig(PuckController puck, GameObject prefab)
    {
        PuckDynamics dynamics = puck.GetComponent<PuckDynamics>();
        TrailRenderer oldTrail = dynamics != null && dynamics.trail != null
            ? dynamics.trail : puck.GetComponent<TrailRenderer>();
        ParticleSystem oldGlow = dynamics != null ? dynamics.idleGlow : null;
        if (oldGlow == null)
        {
            Transform oldGlowTransform = puck.transform.Find("PuckIdleGlow");
            if (oldGlowTransform != null) oldGlow = oldGlowTransform.GetComponent<ParticleSystem>();
        }

        Transform existing = puck.transform.Find("PuckEffectsRig");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, puck.transform);
        rig.name = "PuckEffectsRig";
        rig.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        rig.transform.localScale = Vector3.one;
        SetLayerRecursively(rig.transform, puck.gameObject.layer);

        TrailRenderer trail = rig.GetComponentInChildren<TrailRenderer>(true);
        ParticleSystem glow = rig.GetComponentInChildren<ParticleSystem>(true);
        if (trail == null || glow == null)
            throw new InvalidOperationException("PuckEffectsRig prefab is missing its trail or glow particle system.");

        if (oldTrail != null && oldTrail != trail)
            UnityEngine.Object.DestroyImmediate(oldTrail);
        if (oldGlow != null && oldGlow != glow)
            UnityEngine.Object.DestroyImmediate(oldGlow.gameObject);

        if (dynamics != null)
        {
            dynamics.trail = trail;
            dynamics.idleGlow = glow;
            dynamics.trailWidthSlow = 0.075f;
            dynamics.trailWidthFast = 0.22f;
            dynamics.idleGlowRateSlow = 7f;
            dynamics.idleGlowRateFast = 22f;
            dynamics.emissionSlow = 1f;
            dynamics.emissionFast = 1f;
            EditorUtility.SetDirty(dynamics);
        }
    }

    static void ConfigurePuckTrail(TrailRenderer trail, Material trailMaterial)
    {
        trail.sharedMaterial = trailMaterial;
        trail.time = 0.24f;
        trail.minVertexDistance = 0.055f;
        trail.startWidth = 0.09f;
        trail.endWidth = 0f;
        trail.widthMultiplier = 1f;
        trail.numCornerVertices = 6;
        trail.numCapVertices = 8;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.generateLightingData = false;
        trail.allowOcclusionWhenDynamic = false;
        trail.colorGradient = BuildLineGradient(
            new Color(0.16f, 0.92f, 1f), new Color(1f, 0.73f, 0.22f), 0.78f, 0f);
    }

    static void ConfigurePuckGlow(ParticleSystem glow, Material glowMaterial)
    {
        var main = glow.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 32;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.44f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.095f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.18f, 0.90f, 1f, 0.72f),
            new Color(1f, 0.72f, 0.22f, 0.46f));

        var emission = glow.emission;
        emission.enabled = true;
        emission.rateOverTime = 7f;

        var shape = glow.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.34f;
        shape.radiusThickness = 0.25f;

        ParticleSystemRenderer renderer = glow.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = glowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 4;
        }
    }

    static void WireGameplayStatsPanel(LevelManager manager)
    {
        TMP_FontAsset fallbackFont = manager.shotCounter != null && manager.shotCounter.font != null
            ? manager.shotCounter.font : TMP_Settings.defaultFontAsset;
        TMP_FontAsset uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPath) ?? fallbackFont;
        TMP_FontAsset displayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath) ?? uiFont;
        GameObject oldTimer = manager.timerDisplay != null ? manager.timerDisplay.gameObject : null;
        GameObject oldShot = manager.shotCounter != null ? manager.shotCounter.gameObject : null;
        GameObject oldScore = manager.scoreDisplay != null ? manager.scoreDisplay.gameObject : null;
        GameObject oldLivesRoot = null;
        if (manager.lifeIcons != null && manager.lifeIcons.Length > 0 && manager.lifeIcons[0] != null)
        {
            Canvas oldCanvas = manager.lifeIcons[0].GetComponentInParent<Canvas>();
            if (oldCanvas != null && oldCanvas.name == "LivesHUD") oldLivesRoot = oldCanvas.gameObject;
        }

        GameObject prefab = CreateOrUpdateGameplayStatsPrefab(uiFont, displayFont);
        GameObject existing = FindSceneRoot(manager.gameObject.scene, "GameplayStatsPanel");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, manager.gameObject.scene);
        instance.name = "GameplayStatsPanel";

        manager.shotCounter = RequireText(instance.transform, "ShotCounter");
        manager.scoreDisplay = RequireText(instance.transform, "LiveScore");
        manager.timerDisplay = instance.GetComponentInChildren<TimerDisplay>(true);
        manager.useShotLives = false;
        manager.livesDisplay = null;
        manager.lifeIcons = new Image[0];

        DestroyIfOldUi(oldTimer, instance);
        DestroyIfOldUi(oldShot, instance);
        DestroyIfOldUi(oldScore, instance);
        DestroyIfOldUi(oldLivesRoot, instance);
        EditorUtility.SetDirty(manager);
    }

    static void DestroyIfOldUi(GameObject candidate, GameObject currentRoot)
    {
        if (candidate == null || candidate == currentRoot || candidate.transform.IsChildOf(currentRoot.transform)) return;
        UnityEngine.Object.DestroyImmediate(candidate);
    }

    static GameObject CreateOrUpdateGameplayStatsPrefab(
        TMP_FontAsset uiFont, TMP_FontAsset displayFont)
    {
        Sprite uiSprite = BuiltinUiSprite();
        GameObject root = new GameObject("GameplayStatsPanel", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 998;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject cardObject = CreateUiImage(root.transform, "StatsCard", uiSprite,
                new Color(0.006f, 0.025f, 0.055f, 0.95f), Vector2.zero, new Vector2(390f, 236f));
            RectTransform card = cardObject.GetComponent<RectTransform>();
            SetTopLeftRect(card, new Vector2(28f, -28f), new Vector2(390f, 236f));
            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.raycastTarget = false;
            Shadow shadow = cardObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
            shadow.effectDistance = new Vector2(5f, -6f);
            Outline outline = cardObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.86f, 1f, 0.92f);
            outline.effectDistance = new Vector2(1.25f, -1.25f);

            Image headerAccent = CreateUiImage(card, "HeaderAccent", uiSprite,
                new Color(0.05f, 0.88f, 1f, 1f), new Vector2(-171f, 73f),
                new Vector2(22f, 5f)).GetComponent<Image>();
            headerAccent.raycastTarget = false;

            TMP_Text header = CreateUiText(card, "Header", displayFont, "ROUND STATUS", 18f,
                new Color(0.40f, 0.82f, 1f, 0.96f), new Vector2(-47f, 93f), new Vector2(202f, 28f));
            header.alignment = TextAlignmentOptions.Left;
            header.fontStyle = FontStyles.Bold;
            header.characterSpacing = 3f;
            header.raycastTarget = false;

            TMP_Text shot = CreateUiText(card, "ShotCounter", uiFont,
                "<color=#B8E3F0>STROKES</color><pos=150><color=#FFFFFF>0</color>\n" +
                "<color=#B8E3F0>PAR</color><pos=150><color=#FFD95A>2</color>\n" +
                "<color=#B8E3F0>IF SUNK</color><pos=150><color=#5CFFB0>—</color>",
                22f, Color.white, new Vector2(-68f, 22f), new Vector2(220f, 96f));
            shot.alignment = TextAlignmentOptions.Left;
            shot.lineSpacing = 1f;
            shot.fontStyle = FontStyles.Bold;
            shot.raycastTarget = false;

            TMP_Text score = CreateUiText(card, "LiveScore", uiFont,
                "<color=#B8E3F0>POINTS</color><pos=150><color=#2DE2FF>0</color>",
                22f, Color.white, new Vector2(-68f, -73f), new Vector2(220f, 34f));
            score.alignment = TextAlignmentOptions.Left;
            score.fontStyle = FontStyles.Bold;
            score.raycastTarget = false;

            Image rowDividerA = CreateUiImage(card, "RowDividerA", uiSprite,
                new Color(0.10f, 0.69f, 0.92f, 0.18f), new Vector2(-68f, 22f),
                new Vector2(220f, 2f)).GetComponent<Image>();
            rowDividerA.raycastTarget = false;
            Image rowDividerB = CreateUiImage(card, "RowDividerB", uiSprite,
                new Color(0.10f, 0.69f, 0.92f, 0.18f), new Vector2(-68f, -10f),
                new Vector2(220f, 2f)).GetComponent<Image>();
            rowDividerB.raycastTarget = false;
            Image rowDividerC = CreateUiImage(card, "RowDividerC", uiSprite,
                new Color(0.10f, 0.69f, 0.92f, 0.18f), new Vector2(-68f, -42f),
                new Vector2(220f, 2f)).GetComponent<Image>();
            rowDividerC.raycastTarget = false;

            Image divider = CreateUiImage(card, "Divider", uiSprite,
                new Color(0.14f, 0.78f, 1f, 0.42f), new Vector2(65f, -8f),
                new Vector2(2f, 172f)).GetComponent<Image>();
            divider.raycastTarget = false;

            Image timerCapsule = CreateUiImage(card, "TimerCapsule", uiSprite,
                new Color(0.015f, 0.12f, 0.18f, 0.88f), new Vector2(130f, -8f),
                new Vector2(104f, 172f)).GetComponent<Image>();
            timerCapsule.raycastTarget = false;
            Outline timerOutline = timerCapsule.gameObject.AddComponent<Outline>();
            timerOutline.effectColor = new Color(0.12f, 0.71f, 0.91f, 0.58f);
            timerOutline.effectDistance = new Vector2(1.25f, -1.25f);

            TMP_Text timeLabel = CreateUiText(card, "TimeLabel", uiFont, "TIME", 17f,
                new Color(0.51f, 0.81f, 0.94f, 0.96f), new Vector2(130f, 43f), new Vector2(96f, 25f));
            timeLabel.fontStyle = FontStyles.Bold;
            timeLabel.characterSpacing = 3f;
            timeLabel.raycastTarget = false;

            GameObject timerObject = new GameObject("TimerDisplay", typeof(RectTransform), typeof(TimerDisplay));
            RectTransform timerRect = timerObject.GetComponent<RectTransform>();
            timerRect.SetParent(card, false);
            SetCenteredRect(timerRect, new Vector2(130f, -28f), new Vector2(100f, 72f));
            TMP_Text timerText = CreateUiText(timerRect, "TimerText", displayFont, "30", 50f,
                new Color(0.06f, 0.91f, 1f, 1f), Vector2.zero, new Vector2(100f, 68f));
            timerText.fontStyle = FontStyles.Bold;
            timerText.raycastTarget = false;
            TimerDisplay timer = timerObject.GetComponent<TimerDisplay>();
            timer.timerText = timerText;
            timer.hudFont = displayFont;
            timer.statsFont = uiFont;
            timer.normalColor = new Color(0.06f, 0.91f, 1f, 1f);
            timer.criticalColor = new Color(1f, 0.27f, 0.43f, 1f);
            timer.tickScaleBoost = 0.025f;

            return PrefabUtility.SaveAsPrefabAsset(root, GameplayStatsPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static TMP_Text RequireText(Transform root, string name)
    {
        Transform child = FindChildRecursive(root, name);
        if (child == null || child.GetComponent<TMP_Text>() == null)
            throw new InvalidOperationException($"Prebuilt UI prefab is missing TMP text '{name}'.");
        return child.GetComponent<TMP_Text>();
    }

    static void WireMainMenuCountdown(MainMenuController controller)
    {
        TMP_FontAsset font = controller.autoStartText != null && controller.autoStartText.font != null
            ? controller.autoStartText.font
            : (controller.playLabel != null ? controller.playLabel.font : TMP_Settings.defaultFontAsset);
        Transform textParent = controller.autoStartText != null
            ? controller.autoStartText.transform.parent : null;
        Transform parent = textParent != null && textParent.name == "MainMenuCountdown"
            ? textParent.parent
            : (textParent != null
                ? textParent
                : (controller.playRect != null
                    ? controller.playRect.parent
                    : controller.GetComponentInParent<Canvas>().transform));
        GameObject oldRoot = null;
        if (controller.autoStartText != null)
        {
            Transform oldTransform = controller.autoStartText.transform;
            oldRoot = oldTransform.parent != null && oldTransform.parent.name == "MainMenuCountdown"
                ? oldTransform.parent.gameObject : oldTransform.gameObject;
        }

        GameObject prefab = CreateOrUpdateMainMenuCountdownPrefab(font);
        Transform existing = parent.Find("MainMenuCountdown");
        if (existing != null && existing.gameObject != oldRoot)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = "MainMenuCountdown";
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-34f, -34f);
        rect.sizeDelta = new Vector2(470f, 96f);
        rect.localScale = Vector3.one;

        controller.autoStartText = RequireText(instance.transform, "CountdownText");
        controller.autoStartFont = font;
        controller.autoStartSeconds = 30f;
        EditorUtility.SetDirty(controller);
    }

    static GameObject CreateOrUpdateMainMenuCountdownPrefab(TMP_FontAsset font)
    {
        Sprite uiSprite = BuiltinUiSprite();
        GameObject root = new GameObject("MainMenuCountdown", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        try
        {
            SetCenteredRect(root.GetComponent<RectTransform>(), Vector2.zero, new Vector2(470f, 96f));
            Image background = root.GetComponent<Image>();
            background.sprite = uiSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.015f, 0.025f, 0.075f, 0.88f);
            background.raycastTarget = false;
            Shadow shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(6f, -7f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.86f, 1f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);

            Image accent = CreateUiImage(root.transform, "Accent", uiSprite,
                new Color(1f, 0.32f, 0.68f, 0.95f), new Vector2(-226f, 0f),
                new Vector2(6f, 72f)).GetComponent<Image>();
            accent.raycastTarget = false;
            TMP_Text text = CreateUiText(root.transform, "CountdownText", font,
                "AUTO START  <color=#9AD8FF>30</color>  SECONDS", 25f,
                new Color(0.72f, 0.88f, 1f, 0.94f), Vector2.zero, new Vector2(426f, 72f));
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 25f;
            text.raycastTarget = false;
            return PrefabUtility.SaveAsPrefabAsset(root, MainMenuCountdownPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static GameObject FindSceneRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    static void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 dimensions)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        rect.localScale = Vector3.one;
    }

    static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    static GameObject CreateOrUpdateAudioRigPrefab()
    {
        GameObject root = new GameObject("AudioSourceRig");
        try
        {
            for (int i = 0; i < 10; i++)
                CreateAudioSource(root.transform, $"SfxSource_{i:00}", false);

            CreateAudioSource(root.transform, "MusicA", true);
            CreateAudioSource(root.transform, "MusicB", true);
            CreateAudioSource(root.transform, "MagnetLoop", true);
            CreateAudioSource(root.transform, "WindLoop", true);
            CreateAudioSource(root.transform, "GravityLoop", true);

            return PrefabUtility.SaveAsPrefabAsset(root, AudioRigPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static AudioSource CreateAudioSource(Transform parent, string name, bool loop)
    {
        GameObject child = new GameObject(name, typeof(AudioSource));
        child.transform.SetParent(parent, false);
        AudioSource source = child.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = loop ? 0f : 1f;
        source.spatialBlend = 0f;
        return source;
    }

    static GameObject CreateOrUpdateNavOutlinePrefab()
    {
        GameObject root = new GameObject("ArcadeNavOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        try
        {
            Image image = root.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            image.color = new Color(1f, 0.85f, 0.30f, 1f);
            root.SetActive(false);
            return PrefabUtility.SaveAsPrefabAsset(root, NavOutlinePrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static GameObject CreateOrUpdateResumePromptPrefab(TMP_FontAsset font)
    {
        Sprite uiSprite = BuiltinUiSprite();
        GameObject root = new GameObject("ReturnPlayerPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            Image dimmer = root.GetComponent<Image>();
            dimmer.color = new Color(0.015f, 0.008f, 0.06f, 0.90f);
            dimmer.raycastTarget = true;

            GameObject card = CreateUiImage(root.transform, "ResumeCard", uiSprite,
                new Color(0.025f, 0.045f, 0.11f, 0.985f),
                new Vector2(0f, 0f), new Vector2(900f, 640f));
            Image cardImage = card.GetComponent<Image>();
            cardImage.raycastTarget = false;
            Shadow shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(14f, -18f);
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.88f, 1f, 0.92f);
            outline.effectDistance = new Vector2(4f, -4f);

            CreateUiImage(card.transform, "TopGlow", uiSprite,
                new Color(0.15f, 0.92f, 1f, 1f), new Vector2(0f, 282f), new Vector2(740f, 6f))
                .GetComponent<Image>().raycastTarget = false;
            CreateUiImage(card.transform, "GoldAccent", uiSprite,
                new Color(1f, 0.69f, 0.20f, 0.9f), new Vector2(0f, 20f), new Vector2(650f, 3f))
                .GetComponent<Image>().raycastTarget = false;

            TMP_Text eyebrow = CreateUiText(card.transform, "Eyebrow", font,
                "PROGRESS FOUND", 27f, new Color(0.36f, 0.88f, 1f, 1f),
                new Vector2(0f, 226f), new Vector2(780f, 44f));
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.characterSpacing = 7f;

            TMP_Text question = CreateUiText(card.transform, "Question", font,
                "CONTINUE FROM LEVEL 2?", 50f, Color.white,
                new Vector2(0f, 153f), new Vector2(820f, 70f));
            question.fontStyle = FontStyles.Bold;
            question.enableAutoSizing = true;
            question.fontSizeMin = 36f;
            question.fontSizeMax = 50f;
            question.overflowMode = TextOverflowModes.Truncate;

            TMP_Text hint = CreateUiText(card.transform, "Hint", font,
                "YOUR LAST LEVEL IS READY", 22f, new Color(0.72f, 0.76f, 0.88f, 1f),
                new Vector2(0f, 66f), new Vector2(760f, 36f));
            hint.characterSpacing = 3f;
            hint.overflowMode = TextOverflowModes.Truncate;

            CreateResumeButtonPrefab(card.transform, "ContinueSavedLevel", "CONTINUE LEVEL 2",
                font, uiSprite, new Vector2(0f, -65f), new Color(0.03f, 0.62f, 0.72f, 1f));
            CreateResumeButtonPrefab(card.transform, "StartLevelOne", "START LEVEL 1",
                font, uiSprite, new Vector2(0f, -184f), new Color(0.34f, 0.12f, 0.52f, 1f));

            TMP_Text controls = CreateUiText(card.transform, "Controls", font,
                "BLACK  SELECT     •     WHITE  BACK", 19f,
                new Color(0.50f, 0.72f, 0.86f, 0.88f),
                new Vector2(0f, -284f), new Vector2(760f, 34f));
            controls.characterSpacing = 2f;

            root.SetActive(false);
            return PrefabUtility.SaveAsPrefabAsset(root, ResumePromptPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void CreateResumeButtonPrefab(Transform parent, string name, string label,
        TMP_FontAsset font, Sprite sprite, Vector2 position, Color color)
    {
        GameObject buttonObject = CreateUiImage(parent, name, sprite, color, position, new Vector2(650f, 92f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.transition = Selectable.Transition.None;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        TMP_Text text = CreateUiText(buttonObject.transform, "Label", font, label, 30f,
            Color.white, Vector2.zero, new Vector2(610f, 72f));
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 2f;
        text.raycastTarget = false;
    }

    static GameObject CreateOrUpdateLockOverlayPrefab()
    {
        Sprite uiSprite = BuiltinUiSprite();
        GameObject root = new GameObject("LockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        try
        {
            Stretch(root.GetComponent<RectTransform>());
            Image overlay = root.GetComponent<Image>();
            overlay.sprite = uiSprite;
            overlay.type = Image.Type.Sliced;
            overlay.color = new Color(0.02f, 0.02f, 0.05f, 0.74f);
            overlay.raycastTarget = true;

            GameObject icon = new GameObject("LockIcon", typeof(RectTransform));
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.SetParent(root.transform, false);
            SetCenteredRect(iconRect, Vector2.zero, new Vector2(64f, 68f));

            Color lockColor = new Color(0.85f, 0.88f, 1f, 0.92f);
            CreateUiImage(icon.transform, "Body", uiSprite, lockColor,
                new Vector2(0f, -12f), new Vector2(46f, 35f)).GetComponent<Image>().raycastTarget = false;
            CreateUiImage(icon.transform, "ShackleTop", uiSprite, lockColor,
                new Vector2(0f, 18f), new Vector2(34f, 8f)).GetComponent<Image>().raycastTarget = false;
            CreateUiImage(icon.transform, "ShackleLeft", uiSprite, lockColor,
                new Vector2(-13f, 7f), new Vector2(8f, 28f)).GetComponent<Image>().raycastTarget = false;
            CreateUiImage(icon.transform, "ShackleRight", uiSprite, lockColor,
                new Vector2(13f, 7f), new Vector2(8f, 28f)).GetComponent<Image>().raycastTarget = false;

            root.SetActive(false);
            return PrefabUtility.SaveAsPrefabAsset(root, LockOverlayPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static Material CreateOrUpdateLitMaterial(string path, string name, Color baseColor, Color emission)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = name;
        material.color = baseColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject CreateOrUpdateRouteMarkerPrefab(Material gold, Material haloMaterial)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "PreferredRouteMarker";
        try
        {
            orb.transform.localScale = Vector3.one * 0.34f;
            MeshRenderer renderer = orb.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = gold;
            SphereCollider collider = orb.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1.45f;

            ScorePickup pickup = orb.AddComponent<ScorePickup>();
            pickup.bonusPoints = 125;
            pickup.idleSpinSpeed = 115f;
            pickup.idleBobAmplitude = 0.07f;

            GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            halo.name = "GoldRouteHalo";
            halo.transform.SetParent(orb.transform, false);
            halo.transform.localPosition = new Vector3(0f, -0.60f, 0f);
            halo.transform.localScale = new Vector3(1.75f, 0.035f, 1.75f);
            Collider haloCollider = halo.GetComponent<Collider>();
            if (haloCollider != null) UnityEngine.Object.DestroyImmediate(haloCollider);
            halo.GetComponent<MeshRenderer>().sharedMaterial = haloMaterial;

            return PrefabUtility.SaveAsPrefabAsset(orb, RouteMarkerPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(orb);
        }
    }

    static Vector3[] Route(float x1, float z1, float x2, float z2, float x3, float z3)
    {
        return new[]
        {
            new Vector3(x1, 0.22f, z1),
            new Vector3(x2, 0.22f, z2),
            new Vector3(x3, 0.22f, z3)
        };
    }

    static void BakeRouteIntoLevelPrefab(int levelNumber, Vector3[] positions, GameObject markerPrefab)
    {
        string path = $"Assets/Prefabs/Levels/Level_{levelNumber:00}.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform oldRoute = root.transform.Find("PreferredRoute_Bonus");
            if (oldRoute != null) UnityEngine.Object.DestroyImmediate(oldRoute.gameObject);

            GameObject routeRoot = new GameObject("PreferredRoute_Bonus");
            routeRoot.transform.SetParent(root.transform, false);
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject marker = (GameObject)PrefabUtility.InstantiatePrefab(markerPrefab, routeRoot.transform);
                marker.name = $"GoldRouteMarker_{i + 1}_Plus125";
                marker.transform.localPosition = positions[i];
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void WireResumePrompt(MainMenuController controller, GameObject prefab)
    {
        Canvas canvas = controller.GetComponentInParent<Canvas>();
        if (canvas == null && controller.playLabel != null)
            canvas = controller.playLabel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Canvas[] canvases = FindInScene<Canvas>(controller.gameObject.scene);
            if (canvases.Length > 0) canvas = canvases[0];
        }
        if (canvas == null)
            throw new InvalidOperationException($"{controller.name} has no parent Canvas for ReturnPlayerPrompt.");

        Transform existing = FindChildRecursive(canvas.transform, "ReturnPlayerPrompt");
        GameObject prompt = existing != null
            ? existing.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        prompt.name = "ReturnPlayerPrompt";

        Transform card = FindChildRecursive(prompt.transform, "ResumeCard");
        Transform question = FindChildRecursive(prompt.transform, "Question");
        Transform continueButton = FindChildRecursive(prompt.transform, "ContinueSavedLevel");
        Transform levelOneButton = FindChildRecursive(prompt.transform, "StartLevelOne");
        if (card == null || question == null || continueButton == null || levelOneButton == null)
            throw new InvalidOperationException("ReturnPlayerPrompt prefab hierarchy is incomplete.");

        Button[] buttons = { continueButton.GetComponent<Button>(), levelOneButton.GetComponent<Button>() };
        Image[] images = { continueButton.GetComponent<Image>(), levelOneButton.GetComponent<Image>() };
        TMP_Text[] labels =
        {
            FindChildRecursive(continueButton, "Label").GetComponent<TMP_Text>(),
            FindChildRecursive(levelOneButton, "Label").GetComponent<TMP_Text>()
        };

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("_resumePromptRoot").objectReferenceValue = prompt;
        serialized.FindProperty("_resumeQuestion").objectReferenceValue = question.GetComponent<TMP_Text>();
        SetObjectArray(serialized.FindProperty("_resumeButtons"), buttons);
        SetObjectArray(serialized.FindProperty("_resumeButtonImages"), images);
        SetObjectArray(serialized.FindProperty("_resumeButtonLabels"), labels);
        serialized.FindProperty("_resumePanelRect").objectReferenceValue = card.GetComponent<RectTransform>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        prompt.SetActive(false);
        EditorUtility.SetDirty(controller);
    }

    static void WireLevelLockOverlays(LevelSelectController controller, GameObject prefab)
    {
        for (int i = 0; i < controller.levels.Count; i++)
        {
            LevelSelectController.LevelEntry entry = controller.levels[i];
            if (entry == null || entry.button == null) continue;

            Transform existing = entry.button.transform.Find("LockOverlay");
            GameObject overlay = existing != null
                ? existing.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(prefab, entry.button.transform);
            overlay.name = "LockOverlay";
            overlay.SetActive(false);
            entry.lockOverlay = overlay;

            if (entry.stars != null)
            {
                foreach (Image star in entry.stars)
                {
                    if (star == null) continue;
                    Outline outline = star.GetComponent<Outline>();
                    if (outline == null) outline = star.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.02f, 0.08f, 0.16f, 0.82f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                    outline.useGraphicAlpha = true;
                    EditorUtility.SetDirty(star.gameObject);
                }
            }
        }
        EditorUtility.SetDirty(controller);
    }

    static GameObject CreateOrUpdateGolfScorecardPrefab(
        TMP_FontAsset uiFont, TMP_FontAsset displayFont)
    {
        Sprite uiSprite = BuiltinUiSprite();
        GameObject root = new GameObject("GolfScorecardPanel", typeof(RectTransform),
            typeof(CanvasGroup), typeof(GolfScorecardView));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetCenteredRect(rootRect, new Vector2(530f, 0f), new Vector2(760f, 720f));

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject cardObject = CreateUiImage(root.transform, "CourseCard", uiSprite,
                new Color(0.008f, 0.035f, 0.072f, 0.98f), Vector2.zero,
                new Vector2(760f, 720f));
            Image card = cardObject.GetComponent<Image>();
            card.raycastTarget = false;
            Shadow shadow = cardObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.62f);
            shadow.effectDistance = new Vector2(8f, -10f);
            Outline outline = cardObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.86f, 1f, 0.92f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Image accent = CreateUiImage(cardObject.transform, "HeaderAccent", uiSprite,
                new Color(0.12f, 0.91f, 1f, 1f), new Vector2(0f, 334f),
                new Vector2(670f, 4f)).GetComponent<Image>();
            accent.raycastTarget = false;

            TMP_Text title = CreateUiText(cardObject.transform, "Title", displayFont,
                "YOUR GOLF SCORE", 34f, new Color(0.86f, 0.98f, 1f, 1f),
                new Vector2(0f, 299f), new Vector2(680f, 48f));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 2f;
            title.raycastTarget = false;

            TMP_Text subtitle = CreateUiText(cardObject.transform, "Subtitle", uiFont,
                "FEWER STROKES IS BETTER", 16f,
                new Color(0.43f, 0.77f, 0.88f, 1f), new Vector2(0f, 263f),
                new Vector2(680f, 28f));
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.characterSpacing = 1.5f;
            subtitle.raycastTarget = false;

            Image resultBand = CreateUiImage(cardObject.transform, "CurrentResultBand", uiSprite,
                new Color(0.02f, 0.14f, 0.21f, 0.98f), new Vector2(0f, 205f),
                new Vector2(680f, 82f)).GetComponent<Image>();
            resultBand.raycastTarget = false;
            TMP_Text currentResult = CreateUiText(resultBand.transform, "CurrentResult", displayFont,
                "COURSE PROGRESS", 24f, new Color(0.87f, 0.98f, 1f, 1f),
                Vector2.zero, new Vector2(650f, 76f));
            currentResult.fontStyle = FontStyles.Bold;
            currentResult.raycastTarget = false;

            TMP_Text courseTotal = CreateUiText(cardObject.transform, "CourseTotal", uiFont,
                "NO COMPLETED HOLES", 18f, new Color(0.84f, 0.96f, 1f, 1f),
                new Vector2(0f, 137f), new Vector2(690f, 66f));
            courseTotal.fontStyle = FontStyles.Bold;
            courseTotal.raycastTarget = false;

            const int holeCount = 30;
            TMP_Text[] holeTexts = new TMP_Text[holeCount];
            Image[] holeBackgrounds = new Image[holeCount];
            for (int i = 0; i < holeCount; i++)
            {
                int row = i / 10;
                int column = i % 10;
                float x = -315f + column * 70f;
                float y = 47f - row * 112f;
                GameObject cellObject = CreateUiImage(cardObject.transform,
                    $"Hole_{i + 1:00}", uiSprite, new Color(0.025f, 0.08f, 0.13f, 0.88f),
                    new Vector2(x, y), new Vector2(62f, 98f));
                Image cell = cellObject.GetComponent<Image>();
                cell.raycastTarget = false;
                Outline cellOutline = cellObject.AddComponent<Outline>();
                cellOutline.effectColor = new Color(0.11f, 0.49f, 0.62f, 0.45f);
                cellOutline.effectDistance = new Vector2(1f, -1f);

                TMP_Text text = CreateUiText(cellObject.transform, "Value", uiFont,
                    $"<size=14><color=#426574>HOLE {i + 1:00}</color></size>\n" +
                    "<size=27><color=#35515E>—</color></size>",
                    18f, Color.white, Vector2.zero, new Vector2(58f, 92f));
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.lineSpacing = -3f;
                text.raycastTarget = false;
                holeTexts[i] = text;
                holeBackgrounds[i] = cell;
            }

            TMP_Text legend = CreateUiText(cardObject.transform, "Legend", uiFont,
                "<color=#5CFFB0>GREEN = UNDER PAR (GOOD)</color>     " +
                "<color=#64E9FF>BLUE = EVEN</color>     <color=#FF637D>RED = OVER PAR</color>",
                15f, Color.white, new Vector2(0f, -298f), new Vector2(700f, 32f));
            legend.fontStyle = FontStyles.Bold;
            legend.raycastTarget = false;

            GolfScorecardView view = root.GetComponent<GolfScorecardView>();
            view.Configure(group, currentResult, courseTotal, holeTexts, holeBackgrounds);
            return PrefabUtility.SaveAsPrefabAsset(root, GolfScorecardPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void WireGolfScorecard(LevelCompletePanel panel, GameObject prefab)
    {
        Transform existing = panel.transform.Find("GolfScorecardPanel");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, panel.transform);
        instance.name = "GolfScorecardPanel";
        RectTransform rect = instance.GetComponent<RectTransform>();
        SetCenteredRect(rect, new Vector2(530f, 0f), new Vector2(760f, 720f));
        panel.golfScorecard = instance.GetComponent<GolfScorecardView>();
        if (panel.card != null)
        {
            panel.card.anchoredPosition = new Vector2(-410f, 0f);
            EditorUtility.SetDirty(panel.card);
        }
        EditorUtility.SetDirty(panel);
    }

    static void WireGolfScorecard(GameCompletePanel panel, GameObject prefab)
    {
        Transform existing = panel.transform.Find("GolfScorecardPanel");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, panel.transform);
        instance.name = "GolfScorecardPanel";
        RectTransform rect = instance.GetComponent<RectTransform>();
        SetCenteredRect(rect, new Vector2(530f, 0f), new Vector2(760f, 720f));
        panel.golfScorecard = instance.GetComponent<GolfScorecardView>();
        if (panel.card != null)
        {
            panel.card.anchoredPosition = new Vector2(-410f, 0f);
            EditorUtility.SetDirty(panel.card);
        }
        EditorUtility.SetDirty(panel);
    }

    static GameObject CreateUiImage(Transform parent, string name, Sprite sprite, Color color,
        Vector2 position, Vector2 dimensions)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = result.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetCenteredRect(rect, position, dimensions);
        Image image = result.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return result;
    }

    static TMP_Text CreateUiText(Transform parent, string name, TMP_FontAsset font, string value,
        float fontSize, Color color, Vector2 position, Vector2 dimensions)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = result.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetCenteredRect(rect, position, dimensions);
        TMP_Text text = result.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 dimensions)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static Sprite BuiltinUiSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    static void WireAudioRig(AudioManager manager, GameObject prefab)
    {
        Transform existing = manager.transform.Find("AudioSourceRig");
        GameObject rig = existing != null
            ? existing.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(prefab, manager.transform);
        rig.name = "AudioSourceRig";

        AudioSource[] all = rig.GetComponentsInChildren<AudioSource>(true);
        var byName = new Dictionary<string, AudioSource>(StringComparer.Ordinal);
        foreach (AudioSource source in all)
            byName[source.name] = source;

        var pool = new List<AudioSource>(10);
        for (int i = 0; i < 10; i++)
        {
            AudioSource source = Require(byName, $"SfxSource_{i:00}", manager);
            source.outputAudioMixerGroup = manager.sfxGroup;
            pool.Add(source);
        }

        AudioSource musicA = Require(byName, "MusicA", manager);
        AudioSource musicB = Require(byName, "MusicB", manager);
        AudioSource magnet = Require(byName, "MagnetLoop", manager);
        AudioSource wind = Require(byName, "WindLoop", manager);
        AudioSource gravity = Require(byName, "GravityLoop", manager);
        musicA.outputAudioMixerGroup = manager.musicGroup;
        musicB.outputAudioMixerGroup = manager.musicGroup;
        magnet.outputAudioMixerGroup = manager.sfxGroup;
        wind.outputAudioMixerGroup = manager.sfxGroup;
        gravity.outputAudioMixerGroup = manager.sfxGroup;

        SerializedObject serialized = new SerializedObject(manager);
        SetObjectArray(serialized.FindProperty("_sfxPool"), pool);
        serialized.FindProperty("_musicA").objectReferenceValue = musicA;
        serialized.FindProperty("_musicB").objectReferenceValue = musicB;
        serialized.FindProperty("_magnetLoop").objectReferenceValue = magnet;
        serialized.FindProperty("_windLoop").objectReferenceValue = wind;
        serialized.FindProperty("_gravityLoop").objectReferenceValue = gravity;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    static AudioSource Require(Dictionary<string, AudioSource> sources, string name, AudioManager owner)
    {
        if (!sources.TryGetValue(name, out AudioSource result) || result == null)
            throw new InvalidOperationException($"Prebuilt audio source '{name}' is missing for {owner.name}.");
        return result;
    }

    static void WireNavigationOutline(ArcadeUINavigator navigator, GameObject prefab)
    {
        Transform existing = navigator.transform.Find("ArcadeNavOutline");
        GameObject outline = existing != null
            ? existing.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(prefab, navigator.transform);
        outline.name = "ArcadeNavOutline";
        outline.SetActive(false);

        SerializedObject serialized = new SerializedObject(navigator);
        serialized.FindProperty("_outlineGO").objectReferenceValue = outline;
        serialized.FindProperty("_outlineImg").objectReferenceValue = outline.GetComponent<Image>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(navigator);
    }

    static void WireTrajectoryCoverage(PuckController puck)
    {
        if (puck.previewLine == null)
            throw new InvalidOperationException($"{puck.name} has no previewLine to use as the authored trajectory reference.");

        Material material = CreateOrUpdateTrajectoryMaterial(puck.previewLine.sharedMaterial);
        GameObject prefab = CreateOrUpdateTrajectoryPrefab(material, puck);
        Transform parent = puck.previewLine.transform.parent;
        Transform existing = parent.Find("Trajectory Possible Path");
        GameObject coverageObject = existing != null
            ? existing.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        coverageObject.name = "Trajectory Possible Path";
        coverageObject.layer = puck.previewLine.gameObject.layer;

        LineRenderer line = coverageObject.GetComponent<LineRenderer>();
        ConfigureTrajectoryLine(line, material, puck);
        puck.previewCoverageLine = line;
        EditorUtility.SetDirty(puck);
    }

    static Material CreateOrUpdateTrajectoryMaterial(Material source)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(TrajectoryMaterialPath);
        if (material == null)
        {
            Shader shader = source != null ? source.shader : Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = "Trajectory Possible Path" };
            AssetDatabase.CreateAsset(material, TrajectoryMaterialPath);
        }

        if (source != null)
            material.shader = source.shader;
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", null);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject CreateOrUpdateTrajectoryPrefab(Material material, PuckController puck)
    {
        GameObject root = new GameObject("Trajectory Possible Path", typeof(LineRenderer));
        try
        {
            ConfigureTrajectoryLine(root.GetComponent<LineRenderer>(), material, puck);
            return PrefabUtility.SaveAsPrefabAsset(root, TrajectoryPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void ConfigureTrajectoryLine(LineRenderer line, Material material, PuckController puck)
    {
        line.enabled = false;
        line.positionCount = 0;
        line.useWorldSpace = true;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 8;
        line.numCapVertices = 8;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.generateLightingData = false;
        line.sortingLayerID = puck.previewLine.sortingLayerID;
        line.sortingOrder = puck.previewLine.sortingOrder - 1;
        line.widthMultiplier = 1f;
        line.widthCurve = AnimationCurve.Linear(0f, Mathf.Max(0.18f, puck.previewCoverageStartWidth),
                                                1f, Mathf.Max(puck.previewCoverageStartWidth, puck.previewCoverageEndWidth));
        line.colorGradient = BuildTrajectoryGradient();
        line.sharedMaterial = material;
        EditorUtility.SetDirty(line);
    }

    static Gradient BuildTrajectoryGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.16f, 0.88f, 1f), 0f),
                new GradientColorKey(new Color(0.32f, 0.62f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.24f, 0f),
                new GradientAlphaKey(0.12f, 0.55f),
                new GradientAlphaKey(0.025f, 1f)
            });
        return gradient;
    }

    static void SetObjectArray<T>(SerializedProperty property, IList<T> values) where T : UnityEngine.Object
    {
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    static TMP_FontAsset LoadRequiredFont(string path, string role)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
            throw new InvalidOperationException($"Missing {role} TMP font asset at '{path}'.");
        return font;
    }

    static int ApplyTypography(IEnumerable<TMP_Text> texts, TMP_FontAsset displayFont, TMP_FontAsset uiFont)
    {
        int updated = 0;
        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;
            TMP_FontAsset target = UsesDisplayTypography(text) ? displayFont : uiFont;
            if (text.font == target && text.fontSharedMaterial == target.material) continue;

            text.font = target;
            text.fontSharedMaterial = target.material;
            EditorUtility.SetDirty(text);
            updated++;
        }
        return updated;
    }

    static bool UsesDisplayTypography(TMP_Text text)
    {
        string objectName = text.gameObject.name;
        return objectName.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Headline", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Logo", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("TimerText", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("LevelNumber", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("ScoreValue", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("RankNumber", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static RenderTexture CreateOrUpdateTutorialRenderTexture()
    {
        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(TutorialRenderTexturePath);
        if (texture == null)
        {
            texture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
            {
                name = "HowToPlayArcade",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            AssetDatabase.CreateAsset(texture, TutorialRenderTexturePath);
        }
        else
        {
            texture.antiAliasing = 1;
            texture.useMipMap = false;
            texture.autoGenerateMips = false;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(texture);
        }
        return texture;
    }

    static Image EnsureUiImage(Transform parent, string name, Sprite sprite, Color color,
        Vector2 position, Vector2 dimensions)
    {
        Transform existing = parent.Find(name);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image == null)
            image = CreateUiImage(parent, name, sprite, color, position, dimensions).GetComponent<Image>();

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        SetCenteredRect(image.rectTransform, position, dimensions);
        EditorUtility.SetDirty(image);
        return image;
    }

    static TMP_Text EnsureUiText(Transform parent, string name, TMP_FontAsset font, string value,
        float fontSize, Color color, Vector2 position, Vector2 dimensions)
    {
        Transform existing = parent.Find(name);
        TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (text == null)
            text = CreateUiText(parent, name, font, value, fontSize, color, position, dimensions);

        text.text = value;
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(14f, fontSize * 0.62f);
        text.fontSizeMax = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        SetCenteredRect(text.rectTransform, position, dimensions);
        EditorUtility.SetDirty(text);
        return text;
    }

    static RawImage EnsureRawImage(Transform parent, string name, Texture texture,
        Vector2 position, Vector2 dimensions)
    {
        Transform existing = parent.Find(name);
        RawImage image = existing != null ? existing.GetComponent<RawImage>() : null;
        if (image == null)
        {
            GameObject imageObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            image = imageObject.GetComponent<RawImage>();
        }

        image.texture = texture;
        image.color = Color.white;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        image.raycastTarget = false;
        SetCenteredRect(image.rectTransform, position, dimensions);
        EditorUtility.SetDirty(image);
        return image;
    }

    static void ConfigureOutline(Graphic graphic, Color color, Vector2 distance)
    {
        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null) outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
        EditorUtility.SetDirty(outline);
    }

    static void BuildSecondChanceTutorialView(
        LevelFailedPanel panel, TMP_FontAsset displayFont, TMP_FontAsset uiFont,
        VideoClip videoClip, RenderTexture renderTexture)
    {
        Sprite uiSprite = BuiltinUiSprite();
        Color navy = new Color32(7, 13, 30, 248);
        Color cyan = new Color32(42, 210, 226, 255);
        Color cyanSoft = new Color32(100, 199, 233, 255);
        Color coral = new Color32(255, 80, 100, 255);
        Color pale = new Color32(216, 239, 245, 255);

        Transform existingView = panel.transform.Find("TutorialVideoView");
        Image viewImage = existingView != null ? existingView.GetComponent<Image>() : null;
        if (viewImage == null)
            viewImage = CreateUiImage(panel.transform, "TutorialVideoView", uiSprite, navy,
                Vector2.zero, new Vector2(1080f, 880f)).GetComponent<Image>();
        viewImage.sprite = uiSprite;
        viewImage.type = Image.Type.Sliced;
        viewImage.color = navy;
        viewImage.raycastTarget = false;
        RectTransform viewRect = viewImage.rectTransform;
        SetCenteredRect(viewRect, Vector2.zero, new Vector2(1080f, 880f));
        ConfigureOutline(viewImage, new Color32(12, 88, 108, 230), new Vector2(7f, -7f));

        Image topAccent = EnsureUiImage(viewRect, "TopAccent", uiSprite, cyan,
            new Vector2(0f, 420f), new Vector2(960f, 3f));
        topAccent.raycastTarget = false;

        TMP_Text title = EnsureUiText(viewRect, "TutorialTitle", displayFont,
            "WATCH THE MOVE", 58f, pale, new Vector2(0f, 365f), new Vector2(940f, 70f));
        title.characterSpacing = 1.5f;

        TMP_Text subtitle = EnsureUiText(viewRect, "TutorialSubtitle", uiFont,
            "REAL GAMEPLAY  •  AIM  •  CHARGE  •  FIRE", 27f, cyanSoft,
            new Vector2(0f, 313f), new Vector2(920f, 42f));
        subtitle.characterSpacing = 1f;

        Image videoFrame = EnsureUiImage(viewRect, "VideoFrame", uiSprite,
            new Color32(2, 7, 14, 255), new Vector2(0f, 22f), new Vector2(930f, 535f));
        videoFrame.raycastTarget = false;
        ConfigureOutline(videoFrame, new Color32(42, 210, 226, 210), new Vector2(2f, -2f));

        RawImage videoImage = EnsureRawImage(videoFrame.transform, "VideoImage", renderTexture,
            Vector2.zero, new Vector2(910f, 512f));

        TMP_Text hint = EnsureUiText(viewRect, "TutorialHint", uiFont,
            "THE VIDEO LOOPS  •  PRESS BLACK OR ENTER WHEN READY", 22f,
            new Color32(184, 196, 208, 255), new Vector2(0f, -274f), new Vector2(900f, 36f));
        hint.characterSpacing = 0.6f;

        Image buttonImage = EnsureUiImage(viewRect, "TutorialRetryButton", uiSprite,
            new Color32(34, 142, 94, 255), new Vector2(0f, -354f), new Vector2(430f, 82f));
        ConfigureOutline(buttonImage, new Color32(42, 210, 226, 205), new Vector2(2f, -2f));
        Button retryButton = buttonImage.GetComponent<Button>();
        if (retryButton == null) retryButton = buttonImage.gameObject.AddComponent<Button>();
        retryButton.targetGraphic = buttonImage;
        retryButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = retryButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.selectedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.88f, 0.82f, 1f);
        colors.disabledColor = new Color(0.35f, 0.45f, 0.4f, 0.7f);
        colors.fadeDuration = 0.08f;
        retryButton.colors = colors;

        TMP_Text retryLabel = EnsureUiText(buttonImage.transform, "Label", uiFont,
            "TRY IT NOW", 36f, Color.white, Vector2.zero, new Vector2(400f, 68f));
        retryLabel.fontStyle = FontStyles.Bold;

        VideoPlayer videoPlayer = viewImage.GetComponent<VideoPlayer>();
        if (videoPlayer == null) videoPlayer = viewImage.gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.isLooping = true;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        panel.standardView = panel.card != null ? panel.card.gameObject : null;
        panel.tutorialVideoView = viewImage.gameObject;
        panel.tutorialVideoRect = viewRect;
        panel.tutorialVideoImage = videoImage;
        panel.tutorialVideoPlayer = videoPlayer;
        panel.tutorialRetryButton = retryButton;
        panel.tutorialRetryButtonText = retryLabel;

        viewImage.gameObject.SetActive(false);
        EditorUtility.SetDirty(viewImage.gameObject);
        EditorUtility.SetDirty(retryButton);
        EditorUtility.SetDirty(videoPlayer);
        EditorUtility.SetDirty(panel);
    }

    static void PolishAndSaveFailurePanel(
        LevelFailedPanel panel, TMP_FontAsset displayFont, TMP_FontAsset uiFont,
        VideoClip videoClip, RenderTexture tutorialRenderTexture)
    {
        if (panel == null || panel.card == null)
            throw new InvalidOperationException("LevelFailedPanel is missing its authored card reference.");

        Transform titleTransform = FindChildRecursive(panel.card, "Title");
        Transform subtitleTransform = FindChildRecursive(panel.card, "Sub");
        if (titleTransform == null || subtitleTransform == null
            || panel.retryButton == null || panel.exitButton == null)
            throw new InvalidOperationException("LevelFailedPanel is missing its prebuilt title, subtitle, or buttons.");

        panel.titleText = titleTransform.GetComponent<TMP_Text>();
        panel.subtitleText = subtitleTransform.GetComponent<TMP_Text>();
        panel.retryButtonText = panel.retryButton.GetComponentInChildren<TMP_Text>(true);
        panel.exitButtonText = panel.exitButton.GetComponentInChildren<TMP_Text>(true);
        if (panel.titleText == null || panel.subtitleText == null
            || panel.retryButtonText == null || panel.exitButtonText == null)
            throw new InvalidOperationException("LevelFailedPanel text references are incomplete.");

        SetCenteredRect(panel.card, Vector2.zero, new Vector2(760f, 460f));

        RectTransform titleRect = panel.titleText.rectTransform;
        SetCenteredRect(titleRect, new Vector2(0f, 142f), new Vector2(680f, 100f));
        ConfigureFailureText(panel.titleText, displayFont, 72f, 44f, FontStyles.Bold);
        panel.titleText.characterSpacing = 1.5f;

        SetCenteredRect(panel.subtitleText.rectTransform, new Vector2(0f, 63f), new Vector2(640f, 44f));
        ConfigureFailureText(panel.subtitleText, uiFont, 30f, 21f, FontStyles.Normal);
        panel.subtitleText.characterSpacing = 1f;

        RectTransform retryRect = panel.retryButton.GetComponent<RectTransform>();
        RectTransform exitRect = panel.exitButton.GetComponent<RectTransform>();
        SetCenteredRect(retryRect, new Vector2(0f, -55f), new Vector2(480f, 92f));
        SetCenteredRect(exitRect, new Vector2(0f, -160f), new Vector2(480f, 92f));
        ConfigureFailureText(panel.retryButtonText, uiFont, 40f, 26f, FontStyles.Bold);
        ConfigureFailureText(panel.exitButtonText, uiFont, 40f, 26f, FontStyles.Bold);

        panel.titleRect = titleRect;
        panel.popInOrder = new[] { retryRect, exitRect };
        panel.autoUpgradeVisuals = false;
        panel.revealDuration = 0.38f;
        BuildSecondChanceTutorialView(
            panel, displayFont, uiFont, videoClip, tutorialRenderTexture);
        EditorUtility.SetDirty(panel);

        if (PrefabUtility.IsPartOfPrefabInstance(panel.gameObject))
        {
            string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(panel.gameObject);
            if (!string.Equals(sourcePath, FailurePanelPrefabPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"LevelFailedPanel is connected to unexpected prefab '{sourcePath}'.");
            PrefabUtility.ApplyPrefabInstance(panel.gameObject, InteractionMode.AutomatedAction);
        }
        else
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAssetAndConnect(
                panel.gameObject, FailurePanelPrefabPath, InteractionMode.AutomatedAction);
            if (saved == null)
                throw new InvalidOperationException("Could not save the prebuilt LevelFailedPanel prefab.");
        }
    }

    static void ConfigureFailureText(
        TMP_Text text, TMP_FontAsset font, float maxSize, float minSize, FontStyles style)
    {
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.fontStyle = style;
        text.fontSize = maxSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    static T[] FindInScene<T>(Scene scene) where T : Component
    {
        var results = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));
        return results.ToArray();
    }

    static void BakeScene(string path, Action<Scene> bake)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        try
        {
            bake(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save scene: " + path);
        }
        finally
        {
            if (openedHere && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
