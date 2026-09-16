#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// User-run, idempotent campaign rebalance.
///
/// Run: RealBuca -> Rebalance All 30 Levels
///
/// This command does not add gameplay mechanics. Every level uses the same
/// 30-second timer. Difficulty comes from its goal size, scoring target, layout,
/// and the mechanics already in that prefab. Visual guides explain existing wind,
/// gravity, teleporter, and deadly-zone behavior.
///
/// It never enters Play Mode. Objects created by this tool use the DP_ prefix,
/// so running the command again repairs/replaces them without duplicates.
/// </summary>
public static class RebalanceAllLevelDifficulties
{
    const int BalanceVersion = 2;
    const int LevelCount = 30;
    const string PrefabFolder = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";
    const string LevelSettingsFolder = "Assets/Settings/Levels";
    const string GameScenePath = "Assets/Scenes/Game.unity";
    const string GeneratedPrefix = "DP_";
    const float FixedTimeLimit = 30f;

    // These are layout-specific par targets, not a simple chapter-wide value.
    static readonly int[] ThreeStarStrokes =
    {
        2, 2, 3, 3, 4, 4,
        3, 4, 3, 5, 4, 5,
        4, 5, 5, 4, 5, 5,
        4, 5, 5, 5, 4, 5,
        5, 5, 5, 6, 5, 6
    };

    // Missed-shot budgets: more room to learn early, strict final chapter.
    static readonly int[] MaxLives =
    {
        6, 6, 6, 6, 6, 6,
        5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5,
        4, 4, 4, 4, 4, 4,
        3, 3, 3, 3, 3, 3
    };

    static readonly string[] LevelNames =
    {
        "FIRST SHOT", "EASY CURVE", "CORNER POCKET", "ZIG ZAG", "NARROW ESCAPE", "DOUBLE BOUNCE",
        "THE FUNNEL", "SNAKE PATH", "CROSSROADS", "LABYRINTH", "PRECISION RUN", "SHARP ANGLES",
        "WIND CURRENT", "FINAL MAZE", "MASTER BUCA", "TWIN GAPS", "BUMPER ALLEY", "SPINNING GAUNTLET",
        "WIND TUNNEL", "TELEPORT MAZE", "BOUNCE CHAIN", "MOVING CROSS", "GRAVITY BEND", "DEADLY CORRIDOR",
        "SPEED RUN", "PINBALL", "TWIN WINDMILLS", "THE VAULT", "HAZARD GAUNTLET", "GRAND FINALE"
    };

    // One readable identity per level, based only on mechanics already present.
    static readonly string[] DifficultyFocus =
    {
        "Open lane: learn shot power.",
        "Single bank: learn one controlled rebound.",
        "Corner route: combine aim and one bank.",
        "Alternating rails: choose a clean zig-zag line.",
        "Narrow exit: reduce shot width and speed.",
        "Double bank: plan two rebounds before shooting.",
        "Funnel gate: time the existing moving wall.",
        "Snake lane: maintain control through repeated turns.",
        "Crossroads: select the safer branch.",
        "Maze route: use several controlled strokes.",
        "Precision corridor: avoid overpowered shots.",
        "Sharp angles: read difficult rail rebounds.",
        "Wind current: compensate for the visible force direction.",
        "Deadly slalom: prioritize safety over speed.",
        "Mixed challenge: combine existing late-chapter obstacles.",
        "Twin gaps: pass two increasingly narrow openings.",
        "Bumper alley: predict strong existing rebounds.",
        "Spinning gauntlet: time two counter-rotating walls.",
        "Wind tunnel: aim against a stronger crosswind.",
        "Teleport maze: understand the linked existing portals.",
        "Bounce chain: control three existing launch pads.",
        "Moving cross: time two gates and avoid side hazards.",
        "Gravity bend: curve around the marked gravity range.",
        "Deadly corridor: weave through a dangerous diamond.",
        "Speed run: control the existing boost through blockers.",
        "Pinball: manage launch, spinners, and rebound angles.",
        "Twin windmills: cross a large sweeping obstacle safely.",
        "Vault route: pass offset doors and a deadly moving gate.",
        "Hazard gauntlet: combine a sweeping wall and crosswind.",
        "Grand finale: master the existing full mechanic mix."
    };

    static Material _defaultRing;
    static Material _telegraphMaterial;
    static Material _dangerMaterial;

    [MenuItem("RealBuca/Rebalance All 30 Levels")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play Mode",
                "This command edits level prefabs and settings. Stop Play Mode, then run it again.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureMaterials();

        int saved = 0;
        int issues = 0;
        for (int level = 1; level <= LevelCount; level++)
        {
            string path = $"{PrefabFolder}/Level_{level:D2}.prefab";
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError($"[DifficultyRebalance] Missing {path}.");
                issues++;
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CleanGeneratedObjects(root);
                ConfigureProfile(root, level);
                ConfigureHoleSize(root, level);
                TuneExistingMechanics(root, level);

                MakeHazardsHonest(root);
                AddMechanicTelegraphs(root);
                issues += ValidateLevel(root, level);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                saved++;
            }
            catch (Exception exception)
            {
                issues++;
                Debug.LogError($"[DifficultyRebalance] Level {level:D2} failed: {exception}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        issues += CreateAndWireLevelSettings();
        RemoveLegacyGoalKeyAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            issues == 0 ? "Difficulty rebalance complete" : "Difficulty rebalance needs attention",
            $"Updated {saved}/{LevelCount} level prefabs.\n\n" +
            "CHAPTER 1 (L1-6): forgiving fundamentals\n" +
            "CHAPTER 2 (L7-12): precision and moving obstacles\n" +
            "CHAPTER 3 (L13-18): environmental forces and mixed timing\n" +
            "CHAPTER 4 (L19-24): combined mechanics and route planning\n" +
            "CHAPTER 5 (L25-30): strongest existing obstacles and tightest goals\n\n" +
            "No new gameplay mechanics were added. " +
            "Every level starts at 30 seconds; difficulty comes from its goal, par, layout, and obstacles.\n\n" +
            (issues == 0
                ? "No duplicate DP_ objects were created. You can now inspect or test the levels yourself."
                : $"Found {issues} setup issue(s). Check the Console messages before testing."),
            "OK");
    }

    static void ConfigureProfile(GameObject root, int level)
    {
        LevelDifficultyProfile profile = root.GetComponent<LevelDifficultyProfile>();
        if (profile == null)
            profile = root.AddComponent<LevelDifficultyProfile>();

        int chapter = ((level - 1) / 6) + 1;
        profile.balanceVersion = BalanceVersion;
        profile.levelNumber = level;
        profile.chapter = chapter;
        profile.difficultyRating = chapter;
        profile.difficultyIndex = level;
        profile.mechanicHint = DifficultyFocus[level - 1];
        EditorUtility.SetDirty(profile);
    }

    static void ConfigureHoleSize(GameObject root, int level)
    {
        Transform ring = root.transform.Find("Hole_Ring");
        Transform hole = root.transform.Find("Hole");
        if (ring == null || hole == null) return;

        float progress = (level - 1f) / (LevelCount - 1f);
        float ringScale = Mathf.Lerp(1.14f, 0.76f, Mathf.Pow(progress, 0.95f));
        ring.localScale = new Vector3(ringScale, 0.04f, ringScale);

        float innerScale = ringScale * 0.74f;
        hole.localScale = new Vector3(innerScale, 0.05f, innerScale);
        SphereCollider trigger = hole.GetComponent<SphereCollider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
            // Preserve the project's established capture model while scaling it
            // smoothly with the visible goal instead of changing it in jumps.
            trigger.radius = innerScale * 0.59f;
        }

        Renderer ringRenderer = ring.GetComponent<Renderer>();
        if (ringRenderer != null && _defaultRing != null)
            ringRenderer.sharedMaterial = _defaultRing;
    }

    static void TuneExistingMechanics(GameObject root, int level)
    {
        // Assign absolute values from level progress so the command is fully
        // repeatable: running it twice produces the same prefabs.
        float progress = (level - 1f) / (LevelCount - 1f);

        RotatingWall[] rotatingWalls = root.GetComponentsInChildren<RotatingWall>(true);
        for (int i = 0; i < rotatingWalls.Length; i++)
        {
            RotatingWall wall = rotatingWalls[i];
            float sign = wall.speedDegPerSec < 0f ? -1f : 1f;
            float speed = Mathf.Lerp(50f, 90f, progress) + (i * 6f);
            wall.speedDegPerSec = sign * Mathf.Clamp(speed, 45f, 105f);
            EditorUtility.SetDirty(wall);
        }

        MovingWall[] movingWalls = root.GetComponentsInChildren<MovingWall>(true);
        for (int i = 0; i < movingWalls.Length; i++)
        {
            MovingWall wall = movingWalls[i];
            wall.cycleSeconds = Mathf.Clamp(Mathf.Lerp(3.0f, 1.65f, progress) + (i * 0.12f), 1.5f, 3.2f);
            EditorUtility.SetDirty(wall);
        }

        foreach (BucaWindZone wind in root.GetComponentsInChildren<BucaWindZone>(true))
        {
            wind.forceMagnitude = Mathf.Lerp(4f, 9.5f, progress);
            EditorUtility.SetDirty(wind);
        }

        foreach (GravityWell well in root.GetComponentsInChildren<GravityWell>(true))
        {
            well.strength = Mathf.Lerp(8f, 14f, progress);
            EditorUtility.SetDirty(well);
        }

        foreach (KickerBumper bumper in root.GetComponentsInChildren<KickerBumper>(true))
        {
            bumper.kickSpeed = Mathf.Lerp(9f, 12f, progress);
            bumper.restitutionGain = Mathf.Lerp(1.05f, 1.20f, progress);
            bumper.maxSpeed = 14f;
            EditorUtility.SetDirty(bumper);
        }

        foreach (SpeedBoost boost in root.GetComponentsInChildren<SpeedBoost>(true))
        {
            boost.boostAmount = Mathf.Lerp(7f, 10.5f, progress);
            boost.maxSpeedAfter = 14f;
            EditorUtility.SetDirty(boost);
        }

        foreach (IcePatch surface in root.GetComponentsInChildren<IcePatch>(true))
        {
            bool isMud = surface.name.IndexOf("Mud", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         surface.patchDamping > 1f;
            surface.patchDamping = isMud
                ? Mathf.Lerp(4f, 7f, progress)
                : Mathf.Lerp(0.10f, 0.035f, progress);
            EditorUtility.SetDirty(surface);
        }
    }

    static void CleanGeneratedObjects(GameObject root)
    {
        // DP_ includes visual guides and any leftovers from the cancelled
        // Goal Key idea. Rebuild only the non-gameplay visual guides below.
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            if (child.name.StartsWith(GeneratedPrefix, StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item.gameObject);
    }

    static void MakeHazardsHonest(GameObject root)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (!item.name.StartsWith("NM3_TrapRing", StringComparison.Ordinal)) continue;
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null && _dangerMaterial != null)
                renderer.sharedMaterial = _dangerMaterial;
        }
    }

    static void AddMechanicTelegraphs(GameObject root)
    {
        BucaWindZone[] winds = root.GetComponentsInChildren<BucaWindZone>(true);
        for (int i = 0; i < winds.Length; i++)
            CreateWindArrows(root, winds[i], i + 1);

        GravityWell[] wells = root.GetComponentsInChildren<GravityWell>(true);
        for (int i = 0; i < wells.Length; i++)
            CreateGravityRing(root, wells[i], i + 1);

        Teleporter[] teleporters = root.GetComponentsInChildren<Teleporter>(true);
        if (teleporters.Length >= 2)
            CreateTeleporterLink(root, teleporters[0].transform, teleporters[1].transform);
    }

    static void CreateWindArrows(GameObject root, BucaWindZone wind, int index)
    {
        GameObject arrows = new GameObject($"{GeneratedPrefix}WindTelegraph_{index}");
        arrows.transform.SetParent(root.transform, false);
        arrows.transform.position = new Vector3(wind.transform.position.x, 0.035f, wind.transform.position.z);
        arrows.transform.rotation = Quaternion.Euler(0f, wind.transform.eulerAngles.y, 0f);

        for (int row = -1; row <= 1; row++)
        {
            float z = row * 0.62f;
            CreateArrowBar(arrows.transform, $"Arrow_{row}_L", new Vector3(-0.16f, 0f, z), 42f);
            CreateArrowBar(arrows.transform, $"Arrow_{row}_R", new Vector3(0.16f, 0f, z), -42f);
        }
    }

    static void CreateArrowBar(Transform parent, string name, Vector3 localPosition, float yaw)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = localPosition;
        bar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        bar.transform.localScale = new Vector3(0.075f, 0.025f, 0.48f);
        UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
        if (_telegraphMaterial != null)
            bar.GetComponent<Renderer>().sharedMaterial = _telegraphMaterial;
    }

    static void CreateGravityRing(GameObject root, GravityWell well, int index)
    {
        GameObject ringObject = new GameObject($"{GeneratedPrefix}GravityRange_{index}");
        ringObject.transform.SetParent(root.transform, false);
        ringObject.transform.position = new Vector3(well.transform.position.x, 0.04f, well.transform.position.z);

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 48;
        line.widthMultiplier = 0.045f;
        line.sharedMaterial = _telegraphMaterial;
        line.startColor = new Color(0.75f, 0.30f, 1f, 0.85f);
        line.endColor = line.startColor;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        float radius = Mathf.Clamp(well.range, 0.6f, 3.2f);
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    static void CreateTeleporterLink(GameObject root, Transform a, Transform b)
    {
        GameObject linkObject = new GameObject($"{GeneratedPrefix}TeleporterLink");
        linkObject.transform.SetParent(root.transform, false);
        LineRenderer line = linkObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, a.position + Vector3.up * 0.04f);
        line.SetPosition(1, b.position + Vector3.up * 0.04f);
        line.widthMultiplier = 0.035f;
        line.sharedMaterial = _telegraphMaterial;
        line.startColor = new Color(0.1f, 0.85f, 1f, 0.55f);
        line.endColor = line.startColor;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    static int CreateAndWireLevelSettings()
    {
        EnsureFolder("Assets/Settings", "Levels");
        LevelSettings[] settings = new LevelSettings[LevelCount];
        int issues = 0;

        for (int i = 0; i < LevelCount; i++)
        {
            string path = $"{LevelSettingsFolder}/Level_{i + 1:D2}_Settings.asset";
            LevelSettings setting = AssetDatabase.LoadAssetAtPath<LevelSettings>(path);
            if (setting == null)
            {
                setting = ScriptableObject.CreateInstance<LevelSettings>();
                AssetDatabase.CreateAsset(setting, path);
            }

            setting.timeLimit = FixedTimeLimit;
            setting.threeStarStrokes = ThreeStarStrokes[i];
            setting.maxLives = MaxLives[i];
            setting.displayName = LevelNames[i];
            EditorUtility.SetDirty(setting);
            settings[i] = setting;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        LevelManager manager = UnityEngine.Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError("[DifficultyRebalance] Game.unity has no LevelManager.");
            return 1;
        }

        manager.levelSettings = settings;
        manager.defaultTimeLimit = FixedTimeLimit;
        manager.maxLives = 6;
        manager.useProgressiveDifficulty = true;
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return issues;
    }

    static int ValidateLevel(GameObject root, int level)
    {
        int issues = 0;
        if (root.transform.Find("PuckStart") == null || root.transform.Find("Hole") == null || root.transform.Find("Hole_Ring") == null)
        {
            Debug.LogError($"[DifficultyRebalance] Level {level:D2} is missing PuckStart/Hole/Hole_Ring.");
            issues++;
        }

        LevelDifficultyProfile profile = root.GetComponent<LevelDifficultyProfile>();
        if (profile == null || profile.levelNumber != level)
        {
            Debug.LogError($"[DifficultyRebalance] Level {level:D2} has invalid difficulty metadata.");
            issues++;
        }

        int windCount = root.GetComponentsInChildren<BucaWindZone>(true).Length;
        int windTelegraphs = 0;
        for (int i = 0; i < root.transform.childCount; i++)
            if (root.transform.GetChild(i).name.StartsWith($"{GeneratedPrefix}WindTelegraph_", StringComparison.Ordinal))
                windTelegraphs++;
        if (windCount != windTelegraphs)
        {
            Debug.LogError($"[DifficultyRebalance] Level {level:D2} has {windCount} wind zone(s) but {windTelegraphs} telegraph(s).");
            issues++;
        }

        return issues;
    }

    static void RemoveLegacyGoalKeyAssets()
    {
        string[] legacyPaths =
        {
            $"{MaterialFolder}/Difficulty_GoalLocked.mat",
            $"{MaterialFolder}/Difficulty_GoalUnlocked.mat",
            $"{MaterialFolder}/Difficulty_GoalKey.mat"
        };
        foreach (string path in legacyPaths)
            AssetDatabase.DeleteAsset(path);
    }

    static void EnsureMaterials()
    {
        _defaultRing = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Hole_Ring.mat");
        _dangerMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Deadly_Pink.mat");
        _telegraphMaterial = MakeMaterial("Difficulty_Telegraph", new Color(0.95f, 0.58f, 0.08f), new Color(1.7f, 0.62f, 0.06f), 0.35f, 0.70f);
        if (_dangerMaterial == null)
            _dangerMaterial = MakeMaterial("Difficulty_Danger", new Color(0.78f, 0.03f, 0.20f), new Color(2.2f, 0.04f, 0.36f), 0.25f, 0.72f);
    }

    static Material MakeMaterial(string name, Color baseColor, Color emission, float metallic, float smoothness)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;
        if (isNew)
        {
            Material template = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Rail_White.mat");
            material = template != null
                ? new Material(template)
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        SetColor(material, "_BaseColor", baseColor);
        SetColor(material, "_Color", baseColor);
        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);
        SetFloat(material, "_Glossiness", smoothness);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
        }
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        if (isNew) AssetDatabase.CreateAsset(material, path);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    static void SetColor(Material material, string property, Color value)
    {
        if (material != null && material.HasProperty(property)) material.SetColor(property, value);
    }

    static void SetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }
}
#endif
