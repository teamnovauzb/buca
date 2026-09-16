#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT: fill the bare wood with PREMIUM decorative table markings — like a real
/// crokinole / shuffleboard board. PURELY COSMETIC: every piece is a FLAT decal with
/// NO collider, so the puck rolls right over it and gameplay/difficulty are unchanged.
///
///   RealBuca ▸ Add Table Markings (decorative)
///
/// Per level it adds, on the floor:
///   • Target rings around the hole (dark-wood + brass concentric circles)
///   • Two faint lane-border lines down the long sides
///
/// Wood-and-brass tones match the table frame. Names prefixed "NM4_" (idempotent).
/// AFTER running you can DELETE THIS FILE.
/// </summary>
public static class AddTableMarkings
{
    const string PrefabFolder   = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";
    const string Tag            = "NM4_";

    static Material _inlay, _brass;

    [MenuItem("RealBuca/Add Table Markings (decorative)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves level prefabs. Click ■ Stop, then run it again.", "OK");
            return;
        }

        _inlay = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Table_Edge.mat"); // dark stained wood
        _brass = EnsureBrass();

        int n = 0;
        for (int i = 1; i <= 30; i++)
            n += Process($"Level_{i:D2}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Table markings added ✓",
            $"Added premium wood + brass markings (target rings and lane lines) to {n} levels.\n\n" +
            "All FLAT with NO colliders — gameplay and difficulty are 100% unchanged.\n\n" +
            "Press ▶ Play to see the board look finished. You can DELETE this file now.",
            "OK");
    }

    static int Process(string name)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogWarning($"[Markings] missing {path}"); return 0; }
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var c = root.transform.GetChild(i);
                if (c.name.StartsWith(Tag)) Object.DestroyImmediate(c.gameObject);
            }

            // Target rings around the hole
            var hole = root.transform.Find("Hole");
            float hx = 0f, hz = 5f;
            if (hole != null) { hx = hole.localPosition.x; hz = hole.localPosition.z; }
            Disc(root, "RingOuter", hx, hz, 2.5f, 0.011f, _inlay);
            Disc(root, "RingInner", hx, hz, 1.85f, 0.014f, _brass);

            // Faint lane-border lines down the long sides (just inside the rails)
            Strip(root, "LaneL", -3.85f, 0f, 13f);
            Strip(root, "LaneR",  3.85f, 0f, 13f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return 1;
        }
        catch (System.Exception e) { Debug.LogError($"[Markings] {name}: {e}"); return 0; }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Disc(GameObject root, string name, float x, float z, float dia, float y, Material mat)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = Tag + name;
        d.transform.SetParent(root.transform);
        d.transform.localPosition = new Vector3(x, y, z);
        d.transform.localScale = new Vector3(dia, 0.012f, dia);     // flat disc
        if (mat != null) d.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(d.GetComponent<Collider>());         // purely decorative
    }

    static void Strip(GameObject root, string name, float x, float z, float len)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
        s.name = Tag + name;
        s.transform.SetParent(root.transform);
        s.transform.localPosition = new Vector3(x, 0.011f, z);
        s.transform.localScale = new Vector3(0.10f, 0.02f, len);     // thin floor line along Z
        if (_inlay != null) s.GetComponent<MeshRenderer>().sharedMaterial = _inlay;
        Object.DestroyImmediate(s.GetComponent<Collider>());
    }

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
