#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT injector. Stamps the 4 NEW mechanics — Kicker Bumper, Banking Rail,
/// Orbiting Hole, Ice Patch — as PREGENERATED prefab pieces into a curated set of
/// the 30 level prefabs, and creates their wood-friendly materials.
///
///   RealBuca ▸ Add New Mechanics (one-shot)      (Play mode must be STOPPED)
///
/// AFTER running you can DELETE THIS FILE (Assets/Editor/AddNewMechanics.cs).
/// KEEP the 4 gameplay component scripts in Assets/Scripts — the pieces need them,
/// exactly like RailLight.cs does:
///     KickerBumper.cs · BankingRail.cs · OrbitingHole.cs · IcePatch.cs
///
/// Re-running is SAFE: it first removes anything it added before (objects whose
/// name starts with "NM_") so it never stacks duplicates.
///
/// ⚠ These mechanics live in the LEVEL PREFABS. If you later re-run
/// "RealBuca ▸ Generate All Levels (30)" it rebuilds the prefabs from scratch and
/// REMOVES them — just run this injector again afterwards.
/// </summary>
public static class AddNewMechanics
{
    const string PrefabFolder   = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";
    const float  TubeRadius     = 0.12f;
    const float  TubeY          = 0.22f;
    const string Tag            = "NM_";   // name prefix for everything this tool adds

    static Material _brass, _gold, _ice;

    [MenuItem("RealBuca/Add New Mechanics (one-shot)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves level prefabs. Click ■ Stop, then run it again.", "OK");
            return;
        }

        BuildMaterials();

        int n = 0;
        n += Process("Level_03", InjectL03);
        n += Process("Level_06", InjectL06);
        n += Process("Level_08", InjectL08);
        n += Process("Level_10", InjectL10);
        n += Process("Level_17", InjectL17);
        n += Process("Level_18", InjectL18);
        n += Process("Level_19", InjectL19);
        n += Process("Level_21", InjectL21);
        n += Process("Level_25", InjectL25);
        n += Process("Level_26", InjectL26);
        n += Process("Level_29", InjectL29);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("New mechanics added ✓",
            $"Injected the 4 new mechanics into {n} level prefabs:\n\n" +
            "• Kicker Bumper  →  L17, L21, L26\n" +
            "• Banking Rail   →  L3, L6, L29\n" +
            "• Ice Patch      →  L10, L19, L25\n" +
            "• Orbiting Hole  →  L8, L18\n\n" +
            "You can now DELETE this file (Assets/Editor/AddNewMechanics.cs).\n" +
            "KEEP the 4 component scripts in Assets/Scripts (the pieces need them).\n\n" +
            "Note: re-running 'Generate All Levels (30)' rebuilds the prefabs and\n" +
            "removes these — just run this tool again if you ever do that.",
            "OK");
    }

    // ── per-level injections (coords verified against each level's open space) ──
    static void InjectL03(GameObject r) { BankRailV(r, 3.9f,  0.5f, 4.0f); }                 // bank up the right wall into the corner pocket
    static void InjectL06(GameObject r) { BankRailV(r, 4.0f,  0.0f, 5.0f); }                 // live right wall for the zig-zag banks
    static void InjectL08(GameObject r) { Orbit(r, 0.7f, 5.5f, 0f); }                        // drifting goal at the top of the snake
    static void InjectL10(GameObject r) { Ice(r, -2.3f, 0.3f, 1.6f, 3.2f); }                 // slick the safe left lane (vs the boosted right)
    static void InjectL17(GameObject r) { Bumper(r, 0f, -0.8f); Bumper(r, 0f, 2.0f); }       // two pops down the bumper alley
    static void InjectL18(GameObject r) { Orbit(r, 0.8f, 5.0f, 1.5f); }                       // moving goal above the counter-spinners
    static void InjectL19(GameObject r) { Ice(r, 0f, -1.0f, 2.0f, 3.0f); }                    // ice + the wind that follows = a long slide
    static void InjectL21(GameObject r) { Bumper(r, 0f, 0.3f); }                              // a central pop in the bounce-pad chain
    static void InjectL25(GameObject r) { Ice(r, 0f, -0.8f, 1.3f, 2.2f); }                    // extends the boost into a glide
    static void InjectL26(GameObject r) { Bumper(r, 0f, 2.6f); }                              // pop bumper at the top of the pinball table
    static void InjectL29(GameObject r) { BankRailV(r, 4.0f, 0.5f, 4.0f); }                   // live right wall to bank across the gauntlet

    // ── load → clean → inject → save one prefab ──
    static int Process(string name, System.Action<GameObject> inject)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogWarning($"[AddNewMechanics] missing {path}"); return 0; }
        try
        {
            CleanInjected(root);
            inject(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return 1;
        }
        catch (System.Exception e) { Debug.LogError($"[AddNewMechanics] {name}: {e}"); return 0; }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void CleanInjected(GameObject root)
    {
        // remove previously-injected pieces so re-running never duplicates
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var c = root.transform.GetChild(i);
            if (c.name.StartsWith(Tag)) Object.DestroyImmediate(c.gameObject);
        }
        // remove any kinematic Rigidbody we previously added to the hole
        var hole = root.transform.Find("Hole");
        if (hole != null)
        {
            var rb = hole.GetComponent<Rigidbody>();
            if (rb != null) Object.DestroyImmediate(rb);
        }
    }

    // ── piece builders (mirror LevelPrefabGenerator's style) ──
    static void Bumper(GameObject root, float x, float z)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = Tag + "KickerBumper";
        b.transform.SetParent(root.transform);
        b.transform.localPosition = new Vector3(x, 0.30f, z);
        b.transform.localScale = Vector3.one * 0.60f;
        if (_brass != null) b.GetComponent<MeshRenderer>().sharedMaterial = _brass;
        b.GetComponent<Collider>().isTrigger = false;                 // solid → puck physically rebounds
        AddKinematicRb(b);                                            // guarantees OnCollisionEnter fires
        var rl = b.AddComponent<RailLight>(); rl.targetRenderer = b.GetComponent<Renderer>();
        b.AddComponent<KickerBumper>();
    }

    static void BankRailV(GameObject root, float x, float z, float len)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        t.name = Tag + "BankRail";
        t.transform.SetParent(root.transform);
        t.transform.localPosition = new Vector3(x, TubeY, z);
        t.transform.localRotation = Quaternion.Euler(90, 0, 0);       // along Z, like the WZ walls
        t.transform.localScale = new Vector3(TubeRadius * 2, len * 0.5f, TubeRadius * 2);
        if (_gold != null) t.GetComponent<MeshRenderer>().sharedMaterial = _gold;
        t.GetComponent<Collider>().isTrigger = false;
        AddKinematicRb(t);                                            // guarantees OnCollisionEnter fires
        var rl = t.AddComponent<RailLight>(); rl.targetRenderer = t.GetComponent<Renderer>();
        t.AddComponent<BankingRail>();
    }

    static void AddKinematicRb(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    static void Ice(GameObject root, float x, float z, float sx, float sz)
    {
        var ice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ice.name = Tag + "IcePatch";
        ice.transform.SetParent(root.transform);
        ice.transform.localPosition = new Vector3(x, 0.012f, z);      // flat inlay just above the floor
        ice.transform.localScale = new Vector3(sx, 0.04f, sz);
        if (_ice != null) ice.GetComponent<MeshRenderer>().sharedMaterial = _ice;
        ice.GetComponent<Collider>().isTrigger = true;
        var ip = ice.AddComponent<IcePatch>();
        ip.patchDamping = 0.05f;
        ip.restoreDamping = 0.9f;                                     // the puck's normal drag (from Game.unity)
    }

    static void Orbit(GameObject root, float radius, float cycle, float phase)
    {
        var hole = root.transform.Find("Hole");
        if (hole == null) { Debug.LogWarning("[AddNewMechanics] no 'Hole' to orbit"); return; }

        var rb = hole.GetComponent<Rigidbody>();
        if (rb == null) rb = hole.gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;

        var orb = new GameObject(Tag + "Hole_Orbiter");
        orb.transform.SetParent(root.transform);
        orb.transform.localPosition = Vector3.zero;
        var oc = orb.AddComponent<OrbitingHole>();
        oc.radius = radius; oc.cycleSeconds = cycle; oc.phase = phase;
    }

    // ── wood-friendly materials, saved as real assets ──
    static void BuildMaterials()
    {
        var template = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Rail_White.mat");
        _brass = MakeMat("Mechanic_Brass",    template, new Color(0.72f, 0.50f, 0.18f), 0.85f, 0.62f, new Color(1.40f, 0.85f, 0.30f));
        _gold  = MakeMat("Mechanic_BankGold", template, new Color(0.86f, 0.66f, 0.26f), 1.00f, 0.88f, new Color(1.10f, 0.78f, 0.24f));
        _ice   = MakeMat("Mechanic_Ice",      template, new Color(0.62f, 0.84f, 1.00f), 0.00f, 0.95f, new Color(0.22f, 0.42f, 0.60f));
    }

    static Material MakeMat(string name, Material template, Color baseCol, float metallic, float smooth, Color emission)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = m == null;
        if (isNew)
        {
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader);
        }
        if (template != null && m.shader == template.shader) m.CopyPropertiesFromMaterial(template);
        SetCol(m, "_BaseColor", baseCol); SetCol(m, "_Color", baseCol);
        SetF(m, "_Metallic", metallic); SetF(m, "_Smoothness", smooth); SetF(m, "_Glossiness", smooth);
        if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", emission); m.EnableKeyword("_EMISSION"); }
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        if (isNew) AssetDatabase.CreateAsset(m, path);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void SetCol(Material m, string p, Color c) { if (m.HasProperty(p)) m.SetColor(p, c); }
    static void SetF(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
}
#endif
