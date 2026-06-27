#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT WAVE 2: add a fresh mechanic to every level that didn't already get one
/// — so ALL 30 levels have a distinctive mechanic — with PREMIUM, wood-matching
/// materials and an easy→hard intensity ramp.
///
///   RealBuca ▸ Add Mechanics Wave 2 (fill all levels)
///
/// SAFE: every piece is NON-BLOCKING (floor zones, fields, avoidable decoy pockets),
/// so no placement can make a level unwinnable. Reuses existing components (zero new
/// scripts): Sticky Mud = IcePatch (high damping), Conveyor = BucaWindZone, Trap
/// Pocket = DeadlyTrigger, Vortex = GravityWell.
///
/// PREMIUM LOOK (matches the wood table): Mud = dark stained WOOD (real wood grain),
/// Conveyor = brushed brass belt, Vortex = a glowing violet energy orb, Trap Pocket =
/// an exact copy of the real hole (honest deception).
///
/// DIFFICULTY: mud gets stickier and the conveyor pushes harder in later levels, on
/// top of the built-in hole-shrink ramp — a smooth climb, never a spike.
///
/// Covers L7 L12 L13 L14 L15 L16 L20 L22 L23 L24 L27 L28 L30. Names prefixed "NM3_"
/// (idempotent). AFTER running you can DELETE THIS FILE.
/// </summary>
public static class AddMechanicsWave2
{
    const string PrefabFolder   = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";
    const string Tag            = "NM3_";

    static Material _ringMat, _holeMat, _mud, _conveyor, _vortex;

    [MenuItem("RealBuca/Add Mechanics Wave 2 (fill all levels)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves level prefabs. Click ■ Stop, then run it again.", "OK");
            return;
        }

        _ringMat  = Load("Hole_Ring");
        _holeMat  = Load("Hole_Dark");
        _mud      = EnsureMud();
        _conveyor = EnsureConveyor();
        _vortex   = EnsureVortex();

        int n = 0;
        // Sticky Mud — damping ramps up with level (stickier later)
        n += Process("Level_07", r => Mud(r, 0f, -3f, 2.0f, 1.3f, 4.5f));
        n += Process("Level_13", r => Mud(r, 0f, -3.5f, 2.0f, 1.3f, 5.0f));
        n += Process("Level_20", r => Mud(r, -2.5f, -2.5f, 1.8f, 1.4f, 5.5f));
        n += Process("Level_27", r => Mud(r, 0f, -3.5f, 2.0f, 1.3f, 6.5f));
        // Conveyor — push force ramps up with level
        n += Process("Level_12", r => Conveyor(r, 0f, -3f, 2.0f, 1.5f, Vector3.right, 3.5f));
        n += Process("Level_16", r => Conveyor(r, 0f, -1.5f, 2.0f, 1.5f, Vector3.right, 4.0f));
        n += Process("Level_23", r => Conveyor(r, -0.5f, -2.5f, 2.0f, 1.5f, Vector3.right, 4.5f));
        n += Process("Level_28", r => Conveyor(r, 0f, -3.5f, 2.0f, 1.5f, Vector3.right, 5.5f));
        // Decoy Trap Pocket — a fake hole offset to the side
        n += Process("Level_14", r => TrapPocket(r, -2.6f, 5.0f));
        n += Process("Level_22", r => TrapPocket(r, 3.0f, 4.3f));
        n += Process("Level_24", r => TrapPocket(r, 2.5f, 4.6f));
        n += Process("Level_30", r => TrapPocket(r, 2.8f, 5.0f));
        // Vortex
        n += Process("Level_15", r => Vortex(r, 2.2f, -0.5f, 2.4f, 9f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Wave 2 added ✓",
            $"Added a premium, wood-matched mechanic to {n} more levels — all 30 now have one.\n\n" +
            "• Sticky Mud (dark wood) → L7, L13, L20, L27 — stickier each time\n" +
            "• Conveyor (brass belt)  → L12, L16, L23, L28 — pushes harder each time\n" +
            "• Trap Pocket (decoy)    → L14, L22, L24, L30\n" +
            "• Vortex (violet glow)   → L15\n\n" +
            "All non-blocking. You can DELETE this file now.",
            "OK");
    }

    static int Process(string name, System.Action<GameObject> inject)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogWarning($"[Wave2] missing {path}"); return 0; }
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
        catch (System.Exception e) { Debug.LogError($"[Wave2] {name}: {e}"); return 0; }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ── builders ──
    static void Mud(GameObject root, float x, float z, float sx, float sz, float damping)
    {
        var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m.name = Tag + "MudPatch";
        m.transform.SetParent(root.transform);
        m.transform.localPosition = new Vector3(x, 0.012f, z);
        m.transform.localScale = new Vector3(sx, 0.04f, sz);
        if (_mud != null) m.GetComponent<MeshRenderer>().sharedMaterial = _mud;
        m.GetComponent<Collider>().isTrigger = true;
        var ip = m.AddComponent<IcePatch>();
        ip.patchDamping = damping;
        ip.restoreDamping = 0.9f;
    }

    static void Conveyor(GameObject root, float x, float z, float sx, float sz, Vector3 drift, float force)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = Tag + "Conveyor";
        c.transform.SetParent(root.transform);
        c.transform.localPosition = new Vector3(x, 0.012f, z);
        if (drift.sqrMagnitude > 0.001f)
            c.transform.localRotation = Quaternion.LookRotation(drift.normalized, Vector3.up);
        c.transform.localScale = new Vector3(sx, 0.04f, sz);
        if (_conveyor != null) c.GetComponent<MeshRenderer>().sharedMaterial = _conveyor;
        c.GetComponent<Collider>().isTrigger = true;
        c.AddComponent<BucaWindZone>().forceMagnitude = force;
    }

    static void TrapPocket(GameObject root, float x, float z)
    {
        float ring = 0.95f;
        var r = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        r.name = Tag + "TrapRing";
        r.transform.SetParent(root.transform);
        r.transform.localPosition = new Vector3(x, 0.05f, z);
        r.transform.localScale = new Vector3(ring, 0.04f, ring);
        if (_ringMat != null) r.GetComponent<MeshRenderer>().sharedMaterial = _ringMat;
        Object.DestroyImmediate(r.GetComponent<Collider>());

        var h = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        h.name = Tag + "TrapPocket";
        h.transform.SetParent(root.transform);
        h.transform.localPosition = new Vector3(x, 0.06f, z);
        float inner = ring * 0.74f;
        h.transform.localScale = new Vector3(inner, 0.05f, inner);
        if (_holeMat != null) h.GetComponent<MeshRenderer>().sharedMaterial = _holeMat;
        Object.DestroyImmediate(h.GetComponent<Collider>());
        var col = h.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = inner * 0.59f;
        h.AddComponent<DeadlyTrigger>();
    }

    static void Vortex(GameObject root, float x, float z, float range, float strength)
    {
        var v = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        v.name = Tag + "Vortex";
        v.transform.SetParent(root.transform);
        v.transform.localPosition = new Vector3(x, 0.18f, z);
        v.transform.localScale = Vector3.one * 0.34f;
        if (_vortex != null) v.GetComponent<MeshRenderer>().sharedMaterial = _vortex;
        Object.DestroyImmediate(v.GetComponent<Collider>());
        var gw = v.AddComponent<GravityWell>();
        gw.range = range; gw.strength = strength;
    }

    static Material Load(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{n}.mat");

    // ── premium, wood-matching materials (refreshed every run) ──
    static Material EnsureMud()
    {
        // Dark stained WOOD — clones the table-edge wood so it carries real grain.
        string path = $"{MaterialFolder}/Mechanic_Mud.mat";
        var wood = Load("Table_Edge");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = wood != null ? new Material(wood) : NewLit(); AssetDatabase.CreateAsset(m, path); }
        else if (wood != null && m.shader == wood.shader) m.CopyPropertiesFromMaterial(wood);
        SetC(m, "_BaseColor", new Color(0.16f, 0.11f, 0.06f)); SetC(m, "_Color", new Color(0.16f, 0.11f, 0.06f));
        SetF(m, "_Metallic", 0f); SetF(m, "_Smoothness", 0.28f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m); return m;
    }

    static Material EnsureConveyor()
    {
        // Brushed brass belt — matches the table frame/brackets.
        var m = LoadOrNew("Mechanic_Conveyor");
        SetC(m, "_BaseColor", new Color(0.40f, 0.31f, 0.16f)); SetC(m, "_Color", new Color(0.40f, 0.31f, 0.16f));
        SetF(m, "_Metallic", 0.65f); SetF(m, "_Smoothness", 0.55f);
        if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", new Color(0.20f, 0.13f, 0.04f)); m.EnableKeyword("_EMISSION"); }
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m); return m;
    }

    static Material EnsureVortex()
    {
        // Glowing violet energy orb — interesting + premium under bloom.
        var m = LoadOrNew("Mechanic_Vortex");
        SetC(m, "_BaseColor", new Color(0.16f, 0.05f, 0.26f)); SetC(m, "_Color", new Color(0.16f, 0.05f, 0.26f));
        SetF(m, "_Metallic", 0f); SetF(m, "_Smoothness", 0.7f);
        if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", new Color(0.7f, 0.2f, 1.4f)); m.EnableKeyword("_EMISSION"); }
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m); return m;
    }

    static Material LoadOrNew(string name)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = NewLit(); AssetDatabase.CreateAsset(m, path); }
        return m;
    }

    static Material NewLit()
    {
        var lit = Load("Rail_White");
        var m = new Material(lit != null ? lit.shader : Shader.Find("Universal Render Pipeline/Lit"));
        if (lit != null && m.shader == lit.shader) m.CopyPropertiesFromMaterial(lit);
        return m;
    }

    static void SetC(Material m, string p, Color c) { if (m.HasProperty(p)) m.SetColor(p, c); }
    static void SetF(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
}
#endif
