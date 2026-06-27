using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor-only utility that builds ALL 15 Buca levels as maze-style prefab
/// assets, named and difficulty-ramped:
///
///   RealBuca → Generate 15 Maze Levels
///
/// Output overwrites Assets/Prefabs/Levels/Level_01..Level_15.prefab in place,
/// so the existing LevelManager.levelPrefabs wiring keeps working. Each level is
/// a compact method using the WX/WZ/WA wall helpers + mechanic helpers, so the
/// layouts are easy to read and tweak. Re-run safely after editing any LNN().
///
/// Field: interior x in [-4.5, 4.5], z in [-7, 7]; boundary rails auto-added.
/// </summary>
public static class LevelPrefabGenerator
{
    const float FieldW = 4.5f;
    const float FieldL = 7f;
    const float TubeRadius = 0.12f;
    const float TubeY = 0.22f;

    const string PrefabFolder = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";

    static Material _railMat, _deadlyMat, _padMat, _holeMat, _ringMat, _bevelMat;
    static Material[] _floorPalette;
    static int _levelNum = 1; // current level (1..30) — drives the difficulty ramp

    [MenuItem("RealBuca/Generate All Levels (30)")]
    public static void GenerateAll()
    {
        if (!Directory.Exists(PrefabFolder)) Directory.CreateDirectory(PrefabFolder);
        LoadMaterials();

        BuildAndSave("Level_01", L1_FirstShot);
        BuildAndSave("Level_02", L2_EasyCurve);
        BuildAndSave("Level_03", L3_CornerPocket);
        BuildAndSave("Level_04", L4_ZigZag);
        BuildAndSave("Level_05", L5_NarrowEscape);
        BuildAndSave("Level_06", L6_DoubleBounce);
        BuildAndSave("Level_07", L7_TheFunnel);
        BuildAndSave("Level_08", L8_SnakePath);
        BuildAndSave("Level_09", L9_Crossroads);
        BuildAndSave("Level_10", L10_Labyrinth);
        BuildAndSave("Level_11", L11_PrecisionRun);
        BuildAndSave("Level_12", L12_SharpAngles);
        BuildAndSave("Level_13", L13_GravityTest);
        BuildAndSave("Level_14", L14_FinalMaze);
        BuildAndSave("Level_15", L15_MasterBuca);
        // ── Expansion: 15 harder levels (16–30) ──
        BuildAndSave("Level_16", L16_TwinGaps);
        BuildAndSave("Level_17", L17_BumperAlley);
        BuildAndSave("Level_18", L18_SpinningGauntlet);
        BuildAndSave("Level_19", L19_WindTunnel);
        BuildAndSave("Level_20", L20_TeleportMaze);
        BuildAndSave("Level_21", L21_BounceChain);
        BuildAndSave("Level_22", L22_MovingCross);
        BuildAndSave("Level_23", L23_GravityBend);
        BuildAndSave("Level_24", L24_DeadlyCorridor);
        BuildAndSave("Level_25", L25_SpeedRun);
        BuildAndSave("Level_26", L26_Pinball);
        BuildAndSave("Level_27", L27_TwinWindmills);
        BuildAndSave("Level_28", L28_TheVault);
        BuildAndSave("Level_29", L29_HazardGauntlet);
        BuildAndSave("Level_30", L30_GrandFinale);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Level Generator",
            "Rebuilt all 30 levels (Level_01–Level_30) in Assets/Prefabs/Levels.\n\n" +
            "Next: run RealBuca → Wire All Levels (assigns the 30 prefabs to LevelManager),\n" +
            "then RealBuca → Rebuild Level Grid (30) to grow the picker to 30 tiles.",
            "OK");
    }

    static void LoadMaterials()
    {
        _railMat = LoadMat("Rail_White");
        _deadlyMat = LoadMat("Deadly_Pink");
        _padMat = LoadMat("Pad_Green");
        _holeMat = LoadMat("Hole_Dark");
        _ringMat = LoadMat("Hole_Ring");
        _bevelMat = LoadMat("Table_Edge");
        _floorPalette = new[]
        {
            LoadMat("Floor_Teal"), LoadMat("Floor_Magenta"), LoadMat("Floor_Orange"),
            LoadMat("Floor_Blue"), LoadMat("Floor_Green"), LoadMat("Floor_Purple"),
            LoadMat("Floor_DarkPurple"),
        };
    }

    static Material LoadMat(string name)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        if (m == null) Debug.LogWarning($"[LevelPrefabGenerator] Missing material: {name}");
        return m;
    }

    static void BuildAndSave(string name, System.Action<GameObject> builder)
    {
        int.TryParse(name.Substring(name.Length - 2), out _levelNum); // "Level_07" → 7
        var root = new GameObject(name);
        builder(root);
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");
        Object.DestroyImmediate(root);
    }

    // ═══════════════════════════════════════════════════════════
    // Compact helpers — keep the level layouts readable
    // ═══════════════════════════════════════════════════════════
    static void Base(GameObject r, int floor) { BuildFloor(r, _floorPalette[floor]); BuildBoundary(r); }
    static void Puck(GameObject r, float x, float z) => BuildPuckStart(r, new Vector3(x, 0.08f, z));
    static void Hole(GameObject r, float x, float z, float ring)
    {
        // Smooth difficulty ramp across all 30 levels: the hole shrinks
        // linearly from L1 (forgiving) to L30 (tight) — easy → hard, never
        // brutal. The per-level 'ring' is kept as a small relative nudge so
        // precision levels still read a touch tighter than their neighbors.
        float curve = Mathf.Lerp(1.16f, 0.78f, (Mathf.Clamp(_levelNum, 1, 30) - 1) / 29f);
        float finalRing = Mathf.Clamp(curve + (ring - 0.9f) * 0.25f, 0.72f, 1.18f);
        BuildHole(r, new Vector3(x, 0.06f, z), finalRing);
    }

    static void WX(GameObject r, string n, float x, float z, float len, bool deadly = false) =>
        AddTube(r, n, new Vector3(x, TubeY, z), Quaternion.Euler(0, 0, 90), len, deadly ? _deadlyMat : _railMat, deadly);
    static void WZ(GameObject r, string n, float x, float z, float len, bool deadly = false) =>
        AddTube(r, n, new Vector3(x, TubeY, z), Quaternion.Euler(90, 0, 0), len, deadly ? _deadlyMat : _railMat, deadly);
    static void WA(GameObject r, string n, float x, float z, float len, float angle, bool deadly = false) =>
        AddTube(r, n, new Vector3(x, TubeY, z), Quaternion.Euler(0, angle, 90), len, deadly ? _deadlyMat : _railMat, deadly);

    static void Pad(GameObject r, float x, float z, float tx, float tz, float speed)
    { var p = new Vector3(x, 0.05f, z); BuildBouncePad(r, p, (new Vector3(tx, 0.05f, tz) - p).normalized, speed); }
    static void Boost(GameObject r, float x, float z, float tx, float tz, float amt)
    { var p = new Vector3(x, 0.18f, z); BuildSpeedBoost(r, p, (new Vector3(tx, 0.18f, tz) - p).normalized, amt); }
    static void MovWall(GameObject r, float x, float z, float len, char axis, float dist, float cycle, float phase, bool deadly = false)
    { var w = BuildMovingWall(r, new Vector3(x, TubeY, z), len, axis == 'X' ? Vector3.right : Vector3.forward, dist, cycle, phase); if (deadly) MakeDeadly(w); }
    static void RotWall(GameObject r, float x, float z, float len, float speed) => BuildRotatingWall(r, new Vector3(x, TubeY, z), len, speed);
    static void Wind(GameObject r, float x, float z, float sx, float sz, float tx, float tz, float force) =>
        BuildWindZone(r, new Vector3(x, 0.4f, z), new Vector3(sx, 1f, sz), new Vector3(tx, 0, tz).normalized, force);
    static void Tele(GameObject r, float ax, float az, float bx, float bz)
    {
        var a = BuildTeleporter(r, "Teleport_A", new Vector3(ax, 0.18f, az), Vector3.forward);
        var b = BuildTeleporter(r, "Teleport_B", new Vector3(bx, 0.18f, bz), Vector3.forward);
        a.GetComponent<Teleporter>().partner = b.GetComponent<Teleporter>();
        b.GetComponent<Teleporter>().partner = a.GetComponent<Teleporter>();
    }
    static void Grav(GameObject r, float x, float z, float range, float strength) => BuildGravityWell(r, new Vector3(x, 0.18f, z), range, strength);

    static void MakeDeadly(GameObject w)
    {
        var mr = w.GetComponent<MeshRenderer>();
        if (mr != null && _deadlyMat != null) mr.sharedMaterial = _deadlyMat;
        w.GetComponent<Collider>().isTrigger = true;
        var rl = w.GetComponent<RailLight>();
        if (rl != null) Object.DestroyImmediate(rl);
        w.AddComponent<DeadlyTrigger>();
    }

    // ═══════════════════════════════════════════════════════════
    // THE 15 LEVELS  (coordinates from the verified design pass)
    // ═══════════════════════════════════════════════════════════

    // Balanced "not too easy / not too hard" pass — each level a distinct
    // strategy, holes kept fair (0.8–1.05), a gentle ramp 1→15.

    static void L1_FirstShot(GameObject r)        // 1. Straight shot (with a guide funnel)
    {
        Base(r, 0); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.4f, 1.05f);
        WA(r, "Guide_L", -1.5f, 4.1f, 2.6f, 35f);
        WA(r, "Guide_R", 1.5f, 4.1f, 2.6f, -35f);
        WX(r, "Nudge_L", -2.9f, -0.5f, 1.8f);
        WX(r, "Nudge_R", 2.9f, 1.5f, 1.8f);
    }

    static void L2_EasyCurve(GameObject r)        // 2. Single obstacle — go around the block
    {
        Base(r, 2); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.3f, 1.0f);
        WX(r, "Block", 0f, 0.3f, 4f);
        WX(r, "Lower_Nudge", -1.6f, -2.6f, 2.2f);
    }

    static void L3_CornerPocket(GameObject r)     // 3. Angled reflection — bank the shot
    {
        Base(r, 4); Puck(r, -2.8f, -5.5f); Hole(r, 2.8f, 5.0f, 1.05f);
        WA(r, "Reflector", 0.3f, 0.8f, 5.5f, -42f);
        WZ(r, "Hole_Backstop", 3.6f, 4.2f, 3f);
        WX(r, "Lower_Wall", -2.2f, -2.6f, 2.6f);
    }

    static void L4_ZigZag(GameObject r)           // 4. Narrow gap — thread two offset gaps
    {
        Base(r, 3); Puck(r, 0f, -5.6f); Hole(r, 0f, 5.4f, 1.0f);
        WX(r, "Gap1_L", -2.6f, -1f, 4f);
        WX(r, "Gap1_R", 2.6f, -1f, 4f);
        WX(r, "Gap2_L", -1.9f, 3f, 5.2f);
        WX(r, "Gap2_R", 3.4f, 3f, 2f);
    }

    static void L5_NarrowEscape(GameObject r)     // 5. Multiple barriers — a field of pillars
    {
        Base(r, 4); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.3f, 0.95f);
        WZ(r, "P1", -2.2f, -2.5f, 2.2f);
        WX(r, "P2", 1.3f, -1.3f, 2.2f);
        WZ(r, "P3", 2.4f, 1f, 2.2f);
        WX(r, "P4", -1.6f, 2f, 2.4f);
        WZ(r, "P5", 0.4f, 3.6f, 1.8f);
    }

    static void L6_DoubleBounce(GameObject r)     // 6. Zig-zag path
    {
        Base(r, 5); Puck(r, -3f, -5.5f); Hole(r, 3f, 5.3f, 0.95f);
        WX(r, "Z1", -1f, -3f, 5.2f);
        WX(r, "Z2", 1f, 0f, 5.2f);
        WX(r, "Z3", -1f, 3f, 5.2f);
    }

    static void L7_TheFunnel(GameObject r)        // 7. Precision shot — funnel into a chute
    {
        Base(r, 6); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.4f, 0.85f);
        WA(r, "Mouth_L", -1.9f, -2.6f, 2.6f, 48f);
        WA(r, "Mouth_R", 1.9f, -2.6f, 2.6f, -48f);
        WZ(r, "Chute_L", -0.85f, 1.5f, 6f);
        WZ(r, "Chute_R", 0.85f, 1.5f, 6f);
    }

    static void L8_SnakePath(GameObject r)        // 8. Rotating obstacle — time the windmill
    {
        Base(r, 0); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.3f, 0.95f);
        WZ(r, "Channel_L", -2.0f, 0.5f, 5f);
        WZ(r, "Channel_R", 2.0f, 0.5f, 5f);
        RotWall(r, 0f, 0.5f, 3.0f, 65f);
        WA(r, "Top_Funnel_L", -1.4f, 4.3f, 2.2f, 35f);
        WA(r, "Top_Funnel_R", 1.4f, 4.3f, 2.2f, -35f);
    }

    static void L9_Crossroads(GameObject r)       // 9. Cross-shaped obstacle
    {
        Base(r, 1); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.3f, 0.95f);
        WZ(r, "Cross_S", 0f, -2.4f, 2.4f);
        WZ(r, "Cross_N", 0f, 2.6f, 2.4f);
        WX(r, "Cross_W", -2.4f, 0.2f, 2.4f);
        WX(r, "Cross_E", 2.4f, 0.2f, 2.4f);
        WA(r, "Pocket_SE", 2.3f, -2.3f, 1.6f, 45f);
        WA(r, "Pocket_NW", -2.3f, 2.3f, 1.6f, 45f);
    }

    static void L10_Labyrinth(GameObject r)       // 10. Multiple routes — safe vs boosted
    {
        Base(r, 2); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.2f, 0.9f);
        WZ(r, "Divider", 0f, -0.5f, 6f);
        WX(r, "Left_Bump", -2.5f, 2.6f, 2f);
        WX(r, "Right_Gate", 2.3f, 4.2f, 2.2f);
        Boost(r, 2.3f, -2f, 2.3f, 3.5f, 9f);
    }

    static void L11_PrecisionRun(GameObject r)    // 11. Tight maze (3 turns)
    {
        Base(r, 3); Puck(r, 0f, -5.5f); Hole(r, 3.2f, 5.2f, 0.9f);
        WX(r, "M1", -0.75f, -3f, 6f);
        WX(r, "M2", 0.9f, -0.2f, 6f);
        WX(r, "M3", -0.9f, 2.6f, 6f);
        WZ(r, "Pocket_R", 3.3f, 3.9f, 2.4f);
        WZ(r, "Start_Nub", -2.6f, -4.6f, 1.6f);
    }

    static void L12_SharpAngles(GameObject r)     // 12. Moving blocker — two timed gates
    {
        Base(r, 4); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.4f, 0.9f);
        WZ(r, "Chute_L", -1.6f, -1f, 7f);
        WZ(r, "Chute_R", 1.6f, -1f, 7f);
        MovWall(r, 0f, -0.5f, 2.4f, 'X', 3f, 2.2f, 0f);
        MovWall(r, 0f, 2.5f, 2.4f, 'X', 3f, 2.2f, 1.1f);
        WA(r, "Top_Funnel_L", -1f, 4.6f, 2f, 35f);
        WA(r, "Top_Funnel_R", 1f, 4.6f, 2f, -35f);
    }

    static void L13_GravityTest(GameObject r)     // 13. Combination — moving + wind + funnel
    {
        Base(r, 5); Puck(r, 0f, -6f); Hole(r, 0f, 5.8f, 0.9f);
        WZ(r, "Chute_L", -1.5f, -2.5f, 5f);
        WZ(r, "Chute_R", 1.5f, -2.5f, 5f);
        MovWall(r, 0f, 1f, 2.4f, 'X', 3f, 2.4f, 0f);
        Wind(r, 0f, 3.5f, 3.5f, 2.5f, 1f, 0f, 6f);
        WA(r, "Exit_L", -1.2f, 4.8f, 2.2f, 35f);
        WA(r, "Exit_R", 1.2f, 4.8f, 2.2f, -35f);
    }

    static void L14_FinalMaze(GameObject r)       // 14. Expert precision — deadly slalom
    {
        Base(r, 6); Puck(r, 0f, -5.5f); Hole(r, 2.6f, 5.2f, 0.85f);
        WZ(r, "Corridor_L", -1.5f, -0.5f, 7f);
        WZ(r, "Corridor_R", 1.5f, -0.5f, 7f);
        WX(r, "Deadly_1", -0.8f, 0.3f, 1.6f, true);
        WX(r, "Deadly_2", 0.8f, 2.5f, 1.6f, true);
        WA(r, "Mouth_L", -2.4f, -4.2f, 2.2f, 35f);
        WA(r, "Mouth_R", 2.4f, -4.2f, 2.2f, -35f);
        WX(r, "Exit_Lip", -0.5f, 4.4f, 2f);
    }

    static void L15_MasterBuca(GameObject r)      // 15. Final boss — all mechanics (fair)
    {
        Base(r, 6); Puck(r, 2.8f, -5.6f); Hole(r, -3f, 5f, 0.8f);
        WX(r, "Deadly_Guard", -1.4f, 4f, 2.2f, true);
        WZ(r, "Hole_Lip", -1.4f, 5f, 1.6f);
        WX(r, "Lower_Channel", 0f, -3.2f, 2f);
        Wind(r, 0.5f, 1.5f, 8f, 7f, -1f, 0f, 6f);
        Pad(r, 2.6f, -2.2f, -3f, 4.5f, 15f);
        RotWall(r, -0.6f, 0.6f, 2.6f, 70f);
        MovWall(r, -1.3f, 2.8f, 2.4f, 'X', 3.2f, 2.2f, 0f);
    }

    // ═══════════════════════════════════════════════════════════
    // EXPANSION — 15 harder levels (16–30). Smaller holes, combined
    // mechanics, tighter timing. Still fair: a clear path always exists.
    // ═══════════════════════════════════════════════════════════

    static void L16_TwinGaps(GameObject r)        // 16. narrowing iris funnel + a moving wall
    {
        Base(r, 0); Puck(r, 0f, -5.6f); Hole(r, 0f, 5.4f, 0.85f);
        WA(r, "Iris_L1", -2.9f, -3f, 2.6f, 58f);
        WA(r, "Iris_R1", 2.9f, -3f, 2.6f, -58f);
        WA(r, "Iris_L2", -1.7f, 0.3f, 2.2f, 70f);
        WA(r, "Iris_R2", 1.7f, 0.3f, 2.2f, -70f);
        MovWall(r, 0f, 3.6f, 2f, 'X', 2.2f, 2.4f, 0f);
    }

    static void L17_BumperAlley(GameObject r)     // 17. slalom of angled bumpers
    {
        Base(r, 1); Puck(r, 0f, -5.6f); Hole(r, 0f, 5.4f, 0.85f);
        WA(r, "Bump1", -1.8f, -3f, 2.6f, 40f);
        WA(r, "Bump2", 1.8f, -0.8f, 2.6f, -40f);
        WA(r, "Bump3", -1.8f, 1.4f, 2.6f, 40f);
        WA(r, "Bump4", 1.8f, 3.6f, 2.6f, -40f);
    }

    static void L18_SpinningGauntlet(GameObject r) // 18. two counter-spinning windmills
    {
        Base(r, 2); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.4f, 0.8f);
        WZ(r, "Ch_L", -1.8f, 0.5f, 5f);
        WZ(r, "Ch_R", 1.8f, 0.5f, 5f);
        RotWall(r, 0f, -1.5f, 2.6f, 75f);
        RotWall(r, 0f, 2.5f, 2.6f, -75f);
    }

    static void L19_WindTunnel(GameObject r)      // 19. strong wind toward an offset hole
    {
        Base(r, 3); Puck(r, 0f, -5.5f); Hole(r, -2.5f, 5.2f, 0.8f);
        WZ(r, "Corr_L", -1.5f, -1f, 6f);
        WZ(r, "Corr_R", 1.5f, -1f, 6f);
        Wind(r, 0f, 2f, 3f, 3f, -1f, 0.3f, 7f);
        WX(r, "Exit_Lip", 0.6f, 4.4f, 2f);
    }

    static void L20_TeleportMaze(GameObject r)    // 20. split corridors linked by a teleporter
    {
        Base(r, 4); Puck(r, -2.5f, -5.6f); Hole(r, 2.5f, 5.3f, 0.8f);
        WZ(r, "Divider", 0f, -0.5f, 8f);
        WX(r, "Cap_Top", 0f, 5f, 4f);
        WX(r, "Cap_Bot", 0f, -5f, 4f);
        Tele(r, -2.5f, 0f, 2.5f, 0f);
    }

    static void L21_BounceChain(GameObject r)     // 21. chain of three bounce pads
    {
        Base(r, 5); Puck(r, 0f, -5.6f); Hole(r, 0f, 5.3f, 0.8f);
        WX(r, "Block_L", -1.8f, -1f, 3.4f);
        WX(r, "Block_R", 1.8f, 2f, 3.4f);
        Pad(r, -2.8f, -3f, 2.5f, 0f, 14f);
        Pad(r, 2.8f, 0f, -2.5f, 3f, 14f);
        Pad(r, -2.5f, 3f, 0f, 5.3f, 13f);
    }

    static void L22_MovingCross(GameObject r)     // 22. twin moving gates with deadly side rails
    {
        Base(r, 6); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.3f, 0.8f);
        MovWall(r, -0.7f, -0.5f, 2.6f, 'X', 2.6f, 2f, 0f);
        MovWall(r, 0.7f, 2.5f, 2.6f, 'X', 2.6f, 2f, 1f);
        WZ(r, "D_L", -2.4f, 1f, 4f, true);
        WZ(r, "D_R", 2.4f, 1f, 4f, true);
    }

    static void L23_GravityBend(GameObject r)     // 23. gravity well bends past a deadly wall
    {
        Base(r, 0); Puck(r, 0f, -5.5f); Hole(r, 2.8f, 5.2f, 0.8f);
        WZ(r, "Wall_L", -1.5f, 0f, 6f);
        WX(r, "Deadly", -0.5f, 3f, 2f, true);
        WA(r, "Exit", 1.5f, 4.5f, 2.5f, -35f);
        Grav(r, 1.5f, 1f, 3f, 13f);
    }

    static void L24_DeadlyCorridor(GameObject r)  // 24. weave around a deadly diamond
    {
        Base(r, 1); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.4f, 0.78f);
        WA(r, "Dia_BL", -1.2f, -1.2f, 2.2f, 45f, true);
        WA(r, "Dia_BR", 1.2f, -1.2f, 2.2f, -45f, true);
        WA(r, "Dia_TL", -1.2f, 1.8f, 2.2f, -45f, true);
        WA(r, "Dia_TR", 1.2f, 1.8f, 2.2f, 45f, true);
        WZ(r, "Lane_L", -2.7f, 0.3f, 4f);
        WZ(r, "Lane_R", 2.7f, 0.3f, 4f);
    }

    static void L25_SpeedRun(GameObject r)        // 25. boost through obstacles at speed
    {
        Base(r, 2); Puck(r, 0f, -5.5f); Hole(r, 0f, 5.6f, 0.78f);
        Boost(r, 0f, -3f, 0f, 2f, 11f);
        WX(r, "Block_L", -1.5f, -0.5f, 3f);
        WX(r, "Block_R", 1.8f, 2f, 3.2f);
        WA(r, "Top_L", -1f, 4.6f, 2f, 35f);
        WA(r, "Top_R", 1f, 4.6f, 2f, -35f);
    }

    static void L26_Pinball(GameObject r)         // 26. flippers, twin spinners, launch pad
    {
        Base(r, 3); Puck(r, 0f, -5.6f); Hole(r, 0f, 5.4f, 0.78f);
        WA(r, "Flip_L", -2.6f, -3.6f, 2.6f, 55f);
        WA(r, "Flip_R", 2.6f, -3.6f, 2.6f, -55f);
        Pad(r, 0f, -2f, 0f, 3.5f, 12f);
        RotWall(r, -1.9f, 1f, 1.8f, 90f);
        RotWall(r, 1.9f, 1f, 1.8f, -90f);
        WA(r, "Pocket_L", -1.4f, 4.3f, 2f, 40f);
        WA(r, "Pocket_R", 1.4f, 4.3f, 2f, -40f);
    }

    static void L27_TwinWindmills(GameObject r)   // 27. one giant sweeping windmill arena
    {
        Base(r, 4); Puck(r, 0f, -5.6f); Hole(r, 0f, 5.4f, 0.75f);
        RotWall(r, 0f, 0f, 4f, 52f);
        WX(r, "Guard_L", -2.9f, 2.6f, 2.4f, true);
        WX(r, "Guard_R", 2.9f, 2.6f, 2.4f, true);
        WZ(r, "Hole_Lip_L", -1f, 5f, 1.6f);
        WZ(r, "Hole_Lip_R", 1f, 5f, 1.6f);
    }

    static void L28_TheVault(GameObject r)        // 28. two-room vault — doors + a deadly mover between
    {
        Base(r, 5); Puck(r, 0f, -5.6f); Hole(r, 0f, 5.3f, 0.75f);
        WX(r, "Lower_Divide", -1.3f, -1.5f, 6f);  // doorway on the right
        WX(r, "Upper_Divide", 1.3f, 2f, 6f);       // doorway on the left
        MovWall(r, 0f, 0.3f, 2.2f, 'X', 2.5f, 2.2f, 0f, true);
        WA(r, "Hole_Funnel_L", -1.2f, 4.4f, 2f, 35f);
        WA(r, "Hole_Funnel_R", 1.2f, 4.4f, 2f, -35f);
    }

    static void L29_HazardGauntlet(GameObject r)  // 29. open gauntlet — sweeping wall + crosswind
    {
        Base(r, 6); Puck(r, -2.8f, -5.6f); Hole(r, 2.8f, 5.4f, 0.72f);
        MovWall(r, 0f, 1f, 3.6f, 'X', 3f, 2.6f, 0f);
        Wind(r, 0f, 3.8f, 8f, 2f, 1f, 0f, 7f);
        WA(r, "D_Bar", 0f, -1.8f, 3f, 35f, true);
        WZ(r, "Edge_L", -4f, 1f, 3f);
    }

    static void L30_GrandFinale(GameObject r)     // 30. the finale — spinners, gravity, teleport, hazards
    {
        Base(r, 6); Puck(r, 0f, -5.8f); Hole(r, 0f, 5.6f, 0.7f);
        RotWall(r, -1.6f, -1f, 2.2f, 70f);
        RotWall(r, 1.6f, 1.5f, 2.2f, -70f);
        Grav(r, 0f, 3.8f, 2.6f, 11f);
        WX(r, "D_L", -1.3f, 4.6f, 1.8f, true);
        WX(r, "D_R", 1.3f, 4.6f, 1.8f, true);
        Tele(r, -3.4f, -3f, 3.2f, 0.5f);
        Wind(r, 0f, 1.5f, 7f, 2f, 1f, 0f, 5f);
    }

    // ═══════════════════════════════════════════════════════════
    // Primitive builders — the hierarchy contract LevelManager expects
    // ═══════════════════════════════════════════════════════════
    static void BuildFloor(GameObject root, Material floorMat)
    {
        float w = (FieldW * 2) + 1.4f, l = (FieldL * 2) + 1.4f;

        var bevel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bevel.name = "Table_Bevel";
        bevel.transform.SetParent(root.transform);
        bevel.transform.localPosition = new Vector3(0f, -0.32f, 0f);
        bevel.transform.localScale = new Vector3(w + 0.35f, 0.5f, l + 0.35f);
        if (_bevelMat != null) bevel.GetComponent<MeshRenderer>().sharedMaterial = _bevelMat;
        Object.DestroyImmediate(bevel.GetComponent<Collider>());

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform);
        floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(w, 0.08f, l);
        if (floorMat != null) floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
        Object.DestroyImmediate(floor.GetComponent<Collider>());
    }

    static void BuildBoundary(GameObject root)
    {
        float w = FieldW, l = FieldL;
        AddTube(root, "Bound_Top", new Vector3(0f, TubeY, l), Quaternion.Euler(0, 0, 90), w * 2, _railMat, false);
        AddTube(root, "Bound_Bot", new Vector3(0f, TubeY, -l), Quaternion.Euler(0, 0, 90), w * 2, _railMat, false);
        AddTube(root, "Bound_L", new Vector3(-w, TubeY, 0f), Quaternion.Euler(90, 0, 0), l * 2, _railMat, false);
        AddTube(root, "Bound_R", new Vector3(w, TubeY, 0f), Quaternion.Euler(90, 0, 0), l * 2, _railMat, false);
        AddCorner(root, "Corner_TL", new Vector3(-w, TubeY, l));
        AddCorner(root, "Corner_TR", new Vector3(w, TubeY, l));
        AddCorner(root, "Corner_BL", new Vector3(-w, TubeY, -l));
        AddCorner(root, "Corner_BR", new Vector3(w, TubeY, -l));
    }

    static void BuildPuckStart(GameObject root, Vector3 pos)
    {
        var marker = new GameObject("PuckStart");
        marker.transform.SetParent(root.transform);
        marker.transform.localPosition = pos;
    }

    static void BuildHole(GameObject root, Vector3 pos, float ringScale)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Hole_Ring";
        ring.transform.SetParent(root.transform);
        ring.transform.localPosition = new Vector3(pos.x, 0.05f, pos.z);
        ring.transform.localScale = new Vector3(ringScale, 0.04f, ringScale);
        if (_ringMat != null) ring.GetComponent<MeshRenderer>().sharedMaterial = _ringMat;
        Object.DestroyImmediate(ring.GetComponent<Collider>());

        var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hole.name = "Hole";
        hole.transform.SetParent(root.transform);
        hole.transform.localPosition = new Vector3(pos.x, 0.06f, pos.z);
        float innerScale = ringScale * 0.74f;
        hole.transform.localScale = new Vector3(innerScale, 0.05f, innerScale);
        if (_holeMat != null) hole.GetComponent<MeshRenderer>().sharedMaterial = _holeMat;
        Object.DestroyImmediate(hole.GetComponent<Collider>());
        var col = hole.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = innerScale * 0.59f;
        hole.AddComponent<HoleTrigger>();
    }

    static GameObject AddTube(GameObject root, string name, Vector3 pos, Quaternion rot,
                              float length, Material mat, bool deadly)
    {
        var tube = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tube.name = name;
        tube.transform.SetParent(root.transform);
        tube.transform.localPosition = pos;
        tube.transform.localRotation = rot;
        tube.transform.localScale = new Vector3(TubeRadius * 2, length * 0.5f, TubeRadius * 2);
        if (mat != null) tube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        tube.GetComponent<Collider>().isTrigger = deadly;
        if (deadly) tube.AddComponent<DeadlyTrigger>();
        else { var rl = tube.AddComponent<RailLight>(); rl.targetRenderer = tube.GetComponent<Renderer>(); }
        return tube;
    }

    static void AddCorner(GameObject root, string name, Vector3 pos)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = name;
        s.transform.SetParent(root.transform);
        s.transform.localPosition = pos;
        s.transform.localScale = Vector3.one * (TubeRadius * 2);
        if (_railMat != null) s.GetComponent<MeshRenderer>().sharedMaterial = _railMat;
    }

    // ─── Mechanic primitive builders ──────────────────────────
    static GameObject BuildBouncePad(GameObject root, Vector3 pos, Vector3 forward, float launchSpeed)
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "BouncePad";
        pad.transform.SetParent(root.transform);
        pad.transform.localPosition = pos;
        if (forward.sqrMagnitude > 0.001f)
            pad.transform.localRotation = Quaternion.LookRotation(new Vector3(forward.x, 0, forward.z), Vector3.up);
        pad.transform.localScale = new Vector3(1.2f, 0.08f, 0.4f);
        if (_padMat != null) pad.GetComponent<MeshRenderer>().sharedMaterial = _padMat;
        pad.GetComponent<Collider>().isTrigger = true;
        pad.AddComponent<BouncePad>().launchSpeed = launchSpeed;
        return pad;
    }

    static GameObject BuildSpeedBoost(GameObject root, Vector3 pos, Vector3 forward, float boostAmount)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ring.name = "SpeedBoost";
        ring.transform.SetParent(root.transform);
        ring.transform.localPosition = pos;
        if (forward.sqrMagnitude > 0.001f)
            ring.transform.localRotation = Quaternion.LookRotation(new Vector3(forward.x, 0, forward.z), Vector3.up);
        ring.transform.localScale = new Vector3(0.3f, 0.4f, 1.4f);
        if (_ringMat != null) ring.GetComponent<MeshRenderer>().sharedMaterial = _ringMat;
        ring.GetComponent<Collider>().isTrigger = true;
        ring.AddComponent<SpeedBoost>().boostAmount = boostAmount;
        return ring;
    }

    static GameObject BuildMovingWall(GameObject root, Vector3 basePos, float lengthAlongLocalY,
                                      Vector3 axis, float distance, float cycleSeconds, float phase)
    {
        var tube = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tube.name = "MovingWall";
        tube.transform.SetParent(root.transform);
        tube.transform.localPosition = basePos;
        tube.transform.localRotation = Quaternion.Euler(0, 0, 90);
        tube.transform.localScale = new Vector3(TubeRadius * 2, lengthAlongLocalY * 0.5f, TubeRadius * 2);
        if (_railMat != null) tube.GetComponent<MeshRenderer>().sharedMaterial = _railMat;
        tube.GetComponent<Collider>().isTrigger = false;
        var rb = tube.AddComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
        var mover = tube.AddComponent<MovingWall>();
        mover.axis = axis; mover.distance = distance; mover.cycleSeconds = cycleSeconds; mover.phase = phase;
        var rl = tube.AddComponent<RailLight>(); rl.targetRenderer = tube.GetComponent<Renderer>();
        return tube;
    }

    static GameObject BuildRotatingWall(GameObject root, Vector3 pos, float length, float rotationSpeed)
    {
        var tube = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tube.name = "RotatingWall";
        tube.transform.SetParent(root.transform);
        tube.transform.localPosition = pos;
        tube.transform.localRotation = Quaternion.Euler(0, 0, 90);
        tube.transform.localScale = new Vector3(TubeRadius * 2, length * 0.5f, TubeRadius * 2);
        if (_railMat != null) tube.GetComponent<MeshRenderer>().sharedMaterial = _railMat;
        tube.GetComponent<Collider>().isTrigger = false;
        var rb = tube.AddComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
        tube.AddComponent<RotatingWall>().speedDegPerSec = rotationSpeed;
        var rl = tube.AddComponent<RailLight>(); rl.targetRenderer = tube.GetComponent<Renderer>();
        return tube;
    }

    static GameObject BuildWindZone(GameObject root, Vector3 pos, Vector3 size, Vector3 forward, float forceMagnitude)
    {
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "WindZone";
        zone.transform.SetParent(root.transform);
        zone.transform.localPosition = pos;
        if (forward.sqrMagnitude > 0.001f)
            zone.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);
        zone.transform.localScale = size;
        Object.DestroyImmediate(zone.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(zone.GetComponent<MeshFilter>());
        zone.GetComponent<Collider>().isTrigger = true;
        zone.AddComponent<BucaWindZone>().forceMagnitude = forceMagnitude;
        return zone;
    }

    static GameObject BuildTeleporter(GameObject root, string name, Vector3 pos, Vector3 facing)
    {
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;
        disc.transform.SetParent(root.transform);
        disc.transform.localPosition = pos;
        if (facing.sqrMagnitude > 0.001f)
            disc.transform.localRotation = Quaternion.LookRotation(facing, Vector3.up);
        disc.transform.localScale = new Vector3(1.0f, 0.05f, 1.0f);
        if (_ringMat != null) disc.GetComponent<MeshRenderer>().sharedMaterial = _ringMat;
        disc.GetComponent<Collider>().isTrigger = true;
        disc.AddComponent<Teleporter>();
        return disc;
    }

    static GameObject BuildGravityWell(GameObject root, Vector3 pos, float range, float strength)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "GravityWell";
        marker.transform.SetParent(root.transform);
        marker.transform.localPosition = pos;
        marker.transform.localScale = Vector3.one * 0.3f;
        if (_holeMat != null) marker.GetComponent<MeshRenderer>().sharedMaterial = _holeMat;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        var gw = marker.AddComponent<GravityWell>();
        gw.range = range; gw.strength = strength;
        return marker;
    }
}
