#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT: stamp the 6 authentic "starter batch" mechanics (grounded in real
/// wood-table disc games) into a curated set of level prefabs.
///
///   RealBuca ▸ Add Starter Mechanics (crokinole/sjoelen batch)
///
///   L1  Guard Pegs   (crokinole) — posts ringing the hole; thread between them
///   L2  Funnel Rails (crokinole) — a brass V that channels a rough shot to the hole
///   L4  Pinch Gate   (pucket)    — one gap in a full-width wall; thread it
///   L5  Rim Rebound  (crokinole) — brass side lips that bank cleanly, keeping speed
///   L9  Narrow Gate  (sjoelen)   — a slot just wider than the puck, framing the hole
///   L11 Gutter Edge  (crokinole) — a ditch along the edge that swallows wide shots
///
/// Pieces reuse your existing parts: pegs/gates are walls + RailLight, the rim/funnel
/// use the new RimRebound.cs component, the gutter reuses DeadlyTrigger. Names are
/// prefixed "NM2_" so re-running never duplicates.
///
/// AFTER running you can DELETE THIS FILE. KEEP RimRebound.cs (a piece needs it).
/// ⚠ Re-running "Generate All Levels (30)" rebuilds the prefabs and removes these —
/// just run this again afterwards.
/// </summary>
public static class AddStarterMechanics
{
    const string PrefabFolder   = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";
    const float  TubeRadius     = 0.12f;
    const float  TubeY          = 0.22f;
    const string Tag            = "NM2_";

    static Material _brass, _rail, _deadly;

    [MenuItem("RealBuca/Add Starter Mechanics (crokinole-sjoelen batch)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves level prefabs. Click ■ Stop, then run it again.", "OK");
            return;
        }

        _brass  = EnsureBrass();
        _rail   = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Rail_White.mat");
        _deadly = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Deadly_Pink.mat");

        int n = 0;
        n += Process("Level_01", r => { });              // L1 stays a CLEAN, easy intro (pegs removed)
        n += Process("Level_07", InjectL07_GuardPegs);   // pegs moved here — a fair mid-game precision test
        n += Process("Level_02", InjectL02_Funnel);
        n += Process("Level_04", InjectL04_PinchGate);
        n += Process("Level_05", InjectL05_RimRebound);
        n += Process("Level_09", InjectL09_NarrowGate);
        n += Process("Level_11", InjectL11_Gutter);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Starter mechanics added ✓",
            $"Injected 6 authentic mechanics into {n} levels:\n\n" +
            "• L1  Guard Pegs\n• L2  Funnel Rails\n• L4  Pinch Gate\n" +
            "• L5  Rim Rebound\n• L9  Narrow Gate\n• L11 Gutter Edge\n\n" +
            "Press ▶ Play those levels to try them.\n" +
            "You can DELETE this file now — KEEP RimRebound.cs.",
            "OK");
    }

    // ── per-level placements (verified against each level's open space) ──
    static void InjectL07_GuardPegs(GameObject r)   // pegs guarding the hole at (0, 5.4) — mid-game precision
    {
        Peg(r, -0.70f, 4.70f); Peg(r, 0f, 4.58f); Peg(r, 0.70f, 4.70f);
    }
    static void InjectL02_Funnel(GameObject r)       // a brass V channeling up to the hole (0, 5.3)
    {
        RimRail(r, -1.55f, 3.6f, 2.6f, Quaternion.Euler(0f,  32f, 90f));
        RimRail(r,  1.55f, 3.6f, 2.6f, Quaternion.Euler(0f, -32f, 90f));
    }
    static void InjectL04_PinchGate(GameObject r)    // full-width wall + one centre gap at z = 1.5
    {
        Wall(r, "PinchL", -2.60f, 1.5f, 3.8f, true);
        Wall(r, "PinchR",  2.60f, 1.5f, 3.8f, true);
    }
    static void InjectL05_RimRebound(GameObject r)   // brass side lips that bank cleanly
    {
        RimRail(r, -4.0f, 2.0f, 3.0f, Quaternion.Euler(90f, 0f, 0f));
        RimRail(r,  4.0f, 2.0f, 3.0f, Quaternion.Euler(90f, 0f, 0f));
    }
    static void InjectL09_NarrowGate(GameObject r)   // a narrow slot framing the hole (0, 5.3)
    {
        Wall(r, "NarrowL", -2.45f, 4.5f, 4.1f, true);
        Wall(r, "NarrowR",  2.45f, 4.5f, 4.1f, true);
    }
    static void InjectL11_Gutter(GameObject r)       // a ditch along the left edge that swallows wide shots
    {
        Gutter(r, -4.2f, 0f, 6.0f, true);
    }

    // ── load → clean → inject → save ──
    static int Process(string name, System.Action<GameObject> inject)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogWarning($"[AddStarterMechanics] missing {path}"); return 0; }
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var c = root.transform.GetChild(i);
                if (c.name.StartsWith(Tag)) Object.DestroyImmediate(c.gameObject);
            }
            inject(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return 1;
        }
        catch (System.Exception e) { Debug.LogError($"[AddStarterMechanics] {name}: {e}"); return 0; }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ── piece builders ──
    static void Peg(GameObject root, float x, float z)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = Tag + "GuardPeg";
        p.transform.SetParent(root.transform);
        p.transform.localPosition = new Vector3(x, TubeY, z);
        p.transform.localScale = new Vector3(0.22f, 0.20f, 0.22f);
        if (_brass != null) p.GetComponent<MeshRenderer>().sharedMaterial = _brass;
        p.GetComponent<Collider>().isTrigger = false;                  // solid deflector
        var rl = p.AddComponent<RailLight>(); rl.targetRenderer = p.GetComponent<Renderer>();
    }

    static void RimRail(GameObject root, float x, float z, float len, Quaternion rot)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        t.name = Tag + "RimRail";
        t.transform.SetParent(root.transform);
        t.transform.localPosition = new Vector3(x, TubeY, z);
        t.transform.localRotation = rot;
        t.transform.localScale = new Vector3(TubeRadius * 2, len * 0.5f, TubeRadius * 2);
        if (_brass != null) t.GetComponent<MeshRenderer>().sharedMaterial = _brass;
        t.GetComponent<Collider>().isTrigger = false;
        AddKinematicRb(t);                                            // guarantees OnCollisionEnter
        var rl = t.AddComponent<RailLight>(); rl.targetRenderer = t.GetComponent<Renderer>();
        t.AddComponent<RimRebound>();
    }

    static void Wall(GameObject root, string name, float x, float z, float len, bool alongX)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        t.name = Tag + name;
        t.transform.SetParent(root.transform);
        t.transform.localPosition = new Vector3(x, TubeY, z);
        t.transform.localRotation = alongX ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(90, 0, 0);
        t.transform.localScale = new Vector3(TubeRadius * 2, len * 0.5f, TubeRadius * 2);
        if (_rail != null) t.GetComponent<MeshRenderer>().sharedMaterial = _rail;
        t.GetComponent<Collider>().isTrigger = false;
        var rl = t.AddComponent<RailLight>(); rl.targetRenderer = t.GetComponent<Renderer>();
    }

    static void Gutter(GameObject root, float x, float z, float len, bool alongZ)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = Tag + "Gutter";
        g.transform.SetParent(root.transform);
        g.transform.localPosition = new Vector3(x, -0.02f, z);        // sunken, flat at floor
        g.transform.localScale = alongZ ? new Vector3(0.5f, 0.06f, len) : new Vector3(len, 0.06f, 0.5f);
        if (_deadly != null) g.GetComponent<MeshRenderer>().sharedMaterial = _deadly;
        g.GetComponent<Collider>().isTrigger = true;                  // a swallow zone
        g.AddComponent<DeadlyTrigger>();
    }

    static void AddKinematicRb(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // ── brass material (reuse the scenery brass; create if missing) ──
    static Material EnsureBrass()
    {
        string path = $"{MaterialFolder}/Brass_Trim.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        var lit = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Rail_White.mat");
        var shader = lit != null ? lit.shader : Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(shader);
        if (lit != null && m.shader == lit.shader) m.CopyPropertiesFromMaterial(lit);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.72f, 0.55f, 0.25f));
        if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.72f, 0.55f, 0.25f));
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.85f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.70f);
        if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", new Color(0.25f, 0.18f, 0.06f)); m.EnableKeyword("_EMISSION"); }
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
#endif
