using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor-only utility that builds Levels 06–15 as prefab assets.
/// Each level introduces a new gameplay mechanic and ramps the
/// difficulty over the previous one. Run via the menu:
///
///   RealBuca → Generate Levels 06-15
///
/// Output goes to Assets/Prefabs/Levels/Level_NN.prefab. Existing
/// files with the same names are overwritten — re-run safely after
/// tweaking any of the BuildLevelXX methods.
///
/// After generation, drag the new prefabs into LevelManager.levelPrefabs
/// so they appear in the Game scene.
/// </summary>
public static class LevelPrefabGenerator
{
    // ─────────────────────────────────────────────────────────
    // Field constants — must match what LevelManager expects
    // ─────────────────────────────────────────────────────────
    const float FieldW = 4.5f;
    const float FieldL = 7f;
    const float TubeRadius = 0.12f;
    const float TubeY = 0.22f;

    const string PrefabFolder = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";

    // Material handles loaded once per generation
    static Material _railMat, _deadlyMat, _padMat, _holeMat, _ringMat, _bevelMat;
    static Material[] _floorPalette;

    [MenuItem("RealBuca/Generate Levels 06-15")]
    public static void GenerateAll()
    {
        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        LoadMaterials();

        BuildAndSave("Level_06", BuildLevel06_BouncePadIntro);
        BuildAndSave("Level_07", BuildLevel07_MovingWall);
        BuildAndSave("Level_08", BuildLevel08_SpeedBoost);
        BuildAndSave("Level_09", BuildLevel09_Windmill);
        BuildAndSave("Level_10", BuildLevel10_WindZone);
        BuildAndSave("Level_11", BuildLevel11_PadWindCombo);
        BuildAndSave("Level_12", BuildLevel12_Teleporter);
        BuildAndSave("Level_13", BuildLevel13_GravityWell);
        BuildAndSave("Level_14", BuildLevel14_GateTiming);
        BuildAndSave("Level_15", BuildLevel15_Boss);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Level Generator",
            "Generated Level_06 through Level_15 in Assets/Prefabs/Levels.\n\n" +
            "Drag them into LevelManager.levelPrefabs (in order) to wire them up.",
            "OK");
    }

    static void LoadMaterials()
    {
        _railMat   = LoadMat("Rail_White");
        _deadlyMat = LoadMat("Deadly_Pink");
        _padMat    = LoadMat("Pad_Green");
        _holeMat   = LoadMat("Hole_Dark");
        _ringMat   = LoadMat("Hole_Ring");
        _bevelMat  = LoadMat("Table_Edge");
        _floorPalette = new Material[]
        {
            LoadMat("Floor_Teal"),
            LoadMat("Floor_Magenta"),
            LoadMat("Floor_Orange"),
            LoadMat("Floor_Blue"),
            LoadMat("Floor_Green"),
            LoadMat("Floor_Purple"),
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
        var root = new GameObject(name);
        builder(root);
        string path = $"{PrefabFolder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[LevelPrefabGenerator] Saved {path}");
    }

    // ═══════════════════════════════════════════════════════════
    // LEVELS — each level is a single static method that builds
    // the GameObject hierarchy under `root`. The prefab is saved
    // by BuildAndSave after the builder returns.
    // ═══════════════════════════════════════════════════════════

    /// <summary>Level 06 — Bounce Pad introduction. Easy.</summary>
    static void BuildLevel06_BouncePadIntro(GameObject root)
    {
        BuildBase(root, _floorPalette[0]);
        BuildPuckStart(root, new Vector3(0f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(2.5f, 0.06f, 5f), 1.15f);

        // Single bounce pad pointing toward the hole's quadrant
        Vector3 padPos = new Vector3(-2.0f, 0.05f, 0f);
        Vector3 padForward = (new Vector3(2.5f, 0.05f, 5f) - padPos).normalized;
        BuildBouncePad(root, padPos, padForward, 14f);

        // One simple wall to teach there's a route to plan
        AddTube(root, "Wall_1", new Vector3(0f, TubeY, 2.5f), Quaternion.Euler(0,0,90), 2.4f, _railMat, false);
    }

    /// <summary>Level 07 — Moving Wall introduction. Easy-medium.</summary>
    static void BuildLevel07_MovingWall(GameObject root)
    {
        BuildBase(root, _floorPalette[1]);
        BuildPuckStart(root, new Vector3(0f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(0f, 0.06f, 5.5f), 1.1f);

        // One moving wall slides L-R across the middle of the field
        BuildMovingWall(root, new Vector3(0f, TubeY, 1.5f),
                        lengthAlongLocalY: 2.6f,
                        axis: Vector3.right,
                        distance: 3.5f,
                        cycleSeconds: 2.6f,
                        phase: 0f);

        // A static frame bracket so the field reads as "thread the gap"
        AddTube(root, "Wall_Side_L", new Vector3(-2.8f, TubeY, -1f), Quaternion.Euler(90,0,0), 2.4f, _railMat, false);
        AddTube(root, "Wall_Side_R", new Vector3( 2.8f, TubeY, -1f), Quaternion.Euler(90,0,0), 2.4f, _railMat, false);
    }

    /// <summary>Level 08 — Speed Boost intro. Hole far away, boost makes it reachable.</summary>
    static void BuildLevel08_SpeedBoost(GameObject root)
    {
        BuildBase(root, _floorPalette[2]);
        BuildPuckStart(root, new Vector3(-3f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(3.2f, 0.06f, 5.2f), 1.05f);

        // Boost ring pointing toward upper-right (the hole)
        Vector3 boostPos = new Vector3(0f, 0.18f, -1.5f);
        Vector3 boostFwd = (new Vector3(3.2f, 0.18f, 5.2f) - boostPos).normalized;
        BuildSpeedBoost(root, boostPos, boostFwd, boostAmount: 9f);

        // Two cosmetic walls flanking the boost so the player knows to enter
        AddTube(root, "Wall_BoostL", new Vector3(-1.6f, TubeY, -1.5f), Quaternion.Euler(90,0,0), 1.4f, _railMat, false);
        AddTube(root, "Wall_BoostR", new Vector3( 1.6f, TubeY, -1.5f), Quaternion.Euler(90,0,0), 1.4f, _railMat, false);

        // A wall partway up to require the boost (otherwise the puck stops short)
        AddTube(root, "Wall_Block", new Vector3(0f, TubeY, 3.5f), Quaternion.Euler(0,0,90), 3.5f, _railMat, false);
    }

    /// <summary>Level 09 — Rotating windmill in center. Medium.</summary>
    static void BuildLevel09_Windmill(GameObject root)
    {
        BuildBase(root, _floorPalette[3]);
        BuildPuckStart(root, new Vector3(0f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(0f, 0.06f, 5.5f), 1.05f);

        // Windmill — long capsule that rotates around Y
        BuildRotatingWall(root, new Vector3(0f, TubeY, 0f), length: 3.2f,
                          rotationSpeed: 65f);

        // A pair of corner walls to channel the puck toward the windmill
        AddTube(root, "Wall_NEL", new Vector3(-2.8f, TubeY,  2.2f), Quaternion.Euler(0, 30, 90), 2.0f, _railMat, false);
        AddTube(root, "Wall_NER", new Vector3( 2.8f, TubeY,  2.2f), Quaternion.Euler(0,-30, 90), 2.0f, _railMat, false);
    }

    /// <summary>Level 10 — Wind Zone. Sideways wind pushes shots off course.</summary>
    static void BuildLevel10_WindZone(GameObject root)
    {
        BuildBase(root, _floorPalette[4]);
        BuildPuckStart(root, new Vector3(0f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(-3f, 0.06f, 5f), 1.0f);

        // Wind zone covering the middle third, blowing right (+X)
        BuildWindZone(root, new Vector3(0f, 0.4f, 1f),
                      size: new Vector3(8.5f, 1.0f, 4.0f),
                      forward: Vector3.right,
                      forceMagnitude: 7f);

        // Visual marker bars indicating wind direction
        AddTube(root, "Wall_Stop", new Vector3(2.5f, TubeY, 4.0f), Quaternion.Euler(90,0,0), 1.6f, _railMat, false);
    }

    /// <summary>Level 11 — Wind + chained Bounce Pads. Medium-hard.</summary>
    static void BuildLevel11_PadWindCombo(GameObject root)
    {
        BuildBase(root, _floorPalette[5]);
        BuildPuckStart(root, new Vector3(-3f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(3f, 0.06f, 5f), 0.95f);

        // Wind zone blowing left (-X) — fights the puck's natural arc
        BuildWindZone(root, new Vector3(0f, 0.4f, 0f),
                      size: new Vector3(9.0f, 1.0f, 6.0f),
                      forward: -Vector3.right,
                      forceMagnitude: 8f);

        // Two pads chained: pad 1 redirects toward pad 2, pad 2 toward hole
        Vector3 pad1 = new Vector3(-2.5f, 0.05f, -2f);
        Vector3 pad2 = new Vector3( 2.0f, 0.05f,  1f);
        BuildBouncePad(root, pad1, (pad2 - pad1).normalized, 13f);
        Vector3 holePos = new Vector3(3f, 0.05f, 5f);
        BuildBouncePad(root, pad2, (holePos - pad2).normalized, 13f);

        // One deadly wall to punish a wide shot
        AddTube(root, "Deadly_1", new Vector3(0f, TubeY, 3.5f), Quaternion.Euler(0,0,90), 3.0f, _deadlyMat, true);
    }

    /// <summary>Level 12 — Teleporter pair, hole behind a wall. Medium-hard.</summary>
    static void BuildLevel12_Teleporter(GameObject root)
    {
        BuildBase(root, _floorPalette[6]);
        BuildPuckStart(root, new Vector3(-3f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(0f, 0.06f, 4f), 1.0f);

        // Hole is walled off behind a horizontal barrier with no gap
        AddTube(root, "Wall_HoleGuard1", new Vector3(-2f, TubeY, 2f), Quaternion.Euler(0,0,90), 2.4f, _railMat, false);
        AddTube(root, "Wall_HoleGuard2", new Vector3( 2f, TubeY, 2f), Quaternion.Euler(0,0,90), 2.4f, _railMat, false);
        AddTube(root, "Wall_HoleGuardC", new Vector3( 0f, TubeY, 2.2f), Quaternion.Euler(0,0,90), 1.6f, _deadlyMat, true);

        // Entry teleporter — puck side
        var tpA = BuildTeleporter(root, "Teleport_A", new Vector3(2.5f, 0.18f, -2f),
                                  facing: Vector3.forward);
        // Exit teleporter — beyond the wall, facing toward hole
        var tpB = BuildTeleporter(root, "Teleport_B", new Vector3(0f, 0.18f, 3.2f),
                                  facing: Vector3.forward);
        // Wire the pair
        var tpAComp = tpA.GetComponent<Teleporter>();
        var tpBComp = tpB.GetComponent<Teleporter>();
        tpAComp.partner = tpBComp;
        tpBComp.partner = tpAComp;
    }

    /// <summary>Level 13 — Gravity Well + scattered deadlies. Hard.</summary>
    static void BuildLevel13_GravityWell(GameObject root)
    {
        BuildBase(root, _floorPalette[0]);
        BuildPuckStart(root, new Vector3(0f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(2.5f, 0.06f, 4.5f), 0.95f);

        // Gravity well at midfield — bends shots toward center
        BuildGravityWell(root, new Vector3(-0.5f, 0.18f, 1f), range: 3f, strength: 11f);

        // Three deadly bars the puck has to thread between
        AddTube(root, "Deadly_1", new Vector3(-3f, TubeY, 0f), Quaternion.Euler(0,0,90), 2.0f, _deadlyMat, true);
        AddTube(root, "Deadly_2", new Vector3( 3f, TubeY, 2.0f), Quaternion.Euler(0,0,90), 2.0f, _deadlyMat, true);
        AddTube(root, "Deadly_3", new Vector3(-1.0f, TubeY, 4.0f), Quaternion.Euler(0,0,90), 2.4f, _deadlyMat, true);
    }

    /// <summary>Level 14 — Three alternating disappearing wall gates. Hard.</summary>
    static void BuildLevel14_GateTiming(GameObject root)
    {
        BuildBase(root, _floorPalette[1]);
        BuildPuckStart(root, new Vector3(0f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(0f, 0.06f, 5.5f), 0.95f);

        // 3 disappearing gates at z = -1, 1.5, 4 — phased so only one
        // line through is open at any moment
        BuildDisappearingGate(root, "Gate_1", zPos: -1f, phase: 0f,    onDur: 1.4f, offDur: 1.4f);
        BuildDisappearingGate(root, "Gate_2", zPos:  1.5f, phase: 1.0f, onDur: 1.4f, offDur: 1.4f);
        BuildDisappearingGate(root, "Gate_3", zPos:  4f,  phase: 2.0f, onDur: 1.4f, offDur: 1.4f);

        // Static side rails to channel the puck
        AddTube(root, "Wall_SideL", new Vector3(-1.5f, TubeY, 0f), Quaternion.Euler(90,0,0), 6.0f, _railMat, false);
        AddTube(root, "Wall_SideR", new Vector3( 1.5f, TubeY, 0f), Quaternion.Euler(90,0,0), 6.0f, _railMat, false);
    }

    /// <summary>Level 15 — Boss level: small hole, all mechanics, deadly walls. Very hard.</summary>
    static void BuildLevel15_Boss(GameObject root)
    {
        BuildBase(root, _floorPalette[2]);
        BuildPuckStart(root, new Vector3(0f, 0.08f, -5.5f));
        BuildHole(root, new Vector3(-3f, 0.06f, 5f), 0.7f);   // very small hole

        // Wind from the right pushing left — combats every shot
        BuildWindZone(root, new Vector3(1f, 0.4f, 0f),
                      size: new Vector3(7f, 1f, 8f),
                      forward: -Vector3.right,
                      forceMagnitude: 6f);

        // Moving deadly wall sliding L-R near the hole
        var movingDeadly = BuildMovingWall(root, new Vector3(0f, TubeY, 3.0f),
                                           lengthAlongLocalY: 2.4f,
                                           axis: Vector3.right,
                                           distance: 4.5f,
                                           cycleSeconds: 2.0f,
                                           phase: 0f);
        // Re-tag as deadly: replace material + add DeadlyTrigger
        var mr = movingDeadly.GetComponent<MeshRenderer>();
        if (mr != null && _deadlyMat != null) mr.sharedMaterial = _deadlyMat;
        movingDeadly.GetComponent<Collider>().isTrigger = true;
        var rl = movingDeadly.GetComponent<RailLight>();
        if (rl != null) Object.DestroyImmediate(rl);
        movingDeadly.AddComponent<DeadlyTrigger>();

        // Bounce pad to redirect against the wind
        Vector3 padPos = new Vector3(2.5f, 0.05f, -2f);
        Vector3 holePos = new Vector3(-3f, 0.05f, 5f);
        BuildBouncePad(root, padPos, (holePos - padPos).normalized, 16f);

        // Rotating windmill blocking the direct line
        BuildRotatingWall(root, new Vector3(-1.5f, TubeY, 1f), length: 2.6f, rotationSpeed: 80f);

        // Two static deadlies for extra danger
        AddTube(root, "Deadly_Wall1", new Vector3(2.5f, TubeY, 4f), Quaternion.Euler(0,0,90), 2.0f, _deadlyMat, true);
        AddTube(root, "Deadly_Wall2", new Vector3(-1.0f, TubeY, -2f), Quaternion.Euler(90,0,0), 2.0f, _deadlyMat, true);
    }

    // ═══════════════════════════════════════════════════════════
    // Primitive builders — same hierarchy contract LevelManager
    // expects (Floor, Hole, Hole_Ring, PuckStart child names).
    // ═══════════════════════════════════════════════════════════
    static void BuildBase(GameObject root, Material floorMat)
    {
        BuildFloor(root, floorMat);
        BuildBoundary(root);
    }

    static void BuildFloor(GameObject root, Material floorMat)
    {
        float w = (FieldW * 2) + 1.4f;
        float l = (FieldL * 2) + 1.4f;

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
        AddTube(root, "Bound_Top", new Vector3(0f, TubeY,  l), Quaternion.Euler(0,0,90), w * 2, _railMat, false);
        AddTube(root, "Bound_Bot", new Vector3(0f, TubeY, -l), Quaternion.Euler(0,0,90), w * 2, _railMat, false);
        AddTube(root, "Bound_L",   new Vector3(-w, TubeY, 0f), Quaternion.Euler(90,0,0), l * 2, _railMat, false);
        AddTube(root, "Bound_R",   new Vector3( w, TubeY, 0f), Quaternion.Euler(90,0,0), l * 2, _railMat, false);
        AddCorner(root, "Corner_TL", new Vector3(-w, TubeY,  l));
        AddCorner(root, "Corner_TR", new Vector3( w, TubeY,  l));
        AddCorner(root, "Corner_BL", new Vector3(-w, TubeY, -l));
        AddCorner(root, "Corner_BR", new Vector3( w, TubeY, -l));
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
        else
        {
            var rl = tube.AddComponent<RailLight>();
            rl.targetRenderer = tube.GetComponent<Renderer>();
        }
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
        // Rotate so transform.forward points along `forward`
        if (forward.sqrMagnitude > 0.001f)
            pad.transform.localRotation = Quaternion.LookRotation(new Vector3(forward.x, 0, forward.z), Vector3.up);
        pad.transform.localScale = new Vector3(1.2f, 0.08f, 0.4f);
        if (_padMat != null) pad.GetComponent<MeshRenderer>().sharedMaterial = _padMat;
        var col = pad.GetComponent<Collider>();
        col.isTrigger = true;
        var bp = pad.AddComponent<BouncePad>();
        bp.launchSpeed = launchSpeed;
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
        var col = ring.GetComponent<Collider>();
        col.isTrigger = true;
        var sb = ring.AddComponent<SpeedBoost>();
        sb.boostAmount = boostAmount;
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
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var mover = tube.AddComponent<MovingWall>();
        mover.axis = axis;
        mover.distance = distance;
        mover.cycleSeconds = cycleSeconds;
        mover.phase = phase;

        var rl = tube.AddComponent<RailLight>();
        rl.targetRenderer = tube.GetComponent<Renderer>();
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
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var rot = tube.AddComponent<RotatingWall>();
        rot.speedDegPerSec = rotationSpeed;

        var rl = tube.AddComponent<RailLight>();
        rl.targetRenderer = tube.GetComponent<Renderer>();
        return tube;
    }

    static GameObject BuildWindZone(GameObject root, Vector3 pos, Vector3 size,
                                    Vector3 forward, float forceMagnitude)
    {
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "WindZone";
        zone.transform.SetParent(root.transform);
        zone.transform.localPosition = pos;
        if (forward.sqrMagnitude > 0.001f)
            zone.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);
        zone.transform.localScale = size;
        // Make invisible — wind zones are felt, not seen as solid
        var mr = zone.GetComponent<MeshRenderer>();
        if (mr != null) Object.DestroyImmediate(mr);
        var mf = zone.GetComponent<MeshFilter>();
        if (mf != null) Object.DestroyImmediate(mf);
        var col = zone.GetComponent<Collider>();
        col.isTrigger = true;
        var wz = zone.AddComponent<BucaWindZone>();
        wz.forceMagnitude = forceMagnitude;
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
        var col = disc.GetComponent<Collider>();
        col.isTrigger = true;
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
        // No collider — pure point attractor
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        var gw = marker.AddComponent<GravityWell>();
        gw.range = range;
        gw.strength = strength;
        return marker;
    }

    static void BuildDisappearingGate(GameObject root, string name, float zPos, float phase,
                                      float onDur, float offDur)
    {
        var gate = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        gate.name = name;
        gate.transform.SetParent(root.transform);
        gate.transform.localPosition = new Vector3(0f, TubeY, zPos);
        gate.transform.localRotation = Quaternion.Euler(0, 0, 90);
        gate.transform.localScale = new Vector3(TubeRadius * 2, 1.5f, TubeRadius * 2);
        if (_railMat != null) gate.GetComponent<MeshRenderer>().sharedMaterial = _railMat;
        gate.GetComponent<Collider>().isTrigger = false;
        var dw = gate.AddComponent<DisappearingWall>();
        dw.onDuration = onDur;
        dw.offDuration = offDur;
        dw.phase = phase;
        var rl = gate.AddComponent<RailLight>();
        rl.targetRenderer = gate.GetComponent<Renderer>();
    }
}
