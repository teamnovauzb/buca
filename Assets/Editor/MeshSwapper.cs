using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Swaps the Unity-primitive meshes across all level prefabs + the open
/// scene for the custom Blender meshes in Assets/Models/, keeping every
/// collider, component, transform, and material untouched (except where
/// a mesh has a 2nd "Accent" submesh, which gets the shared dark accent
/// material in slot 2).
///
/// Run via menu:  RealBuca → Apply Custom Meshes
///
/// Design rules:
///   • VISUAL swap only — MeshFilter.sharedMesh is the only thing that
///     changes on level geometry. Colliders stay primitive (mesh
///     colliders on a fast puck = tunneling + WebGL perf death).
///   • Colors come from the materials ALREADY on each renderer (the
///     game's neon palette: Rail_White, Deadly_Pink, Hole_Ring, the
///     per-level floor colors...). That's why swapped levels are
///     colored, not grey like the raw OBJ previews.
///   • The puck additionally gets FreezeRotation X/Z: its visual is now
///     a flat disc, and the rolling sphere collider would make a disc
///     visibly tumble. Frozen X/Z keeps it spinning flat like a real
///     puck; the sphere collider's physics are otherwise unchanged.
/// </summary>
public static class MeshSwapper
{
    const string ModelsDir = "Assets/Models";
    const string PrefabsDir = "Assets/Prefabs/Levels";
    const string AccentMatPath = "Assets/Materials/Accent_Dark.mat";

    // ─── name → model file mapping ─────────────────────────────
    // Exact names first, then prefix rules (ORDER MATTERS: more
    // specific prefixes must come before general ones).
    static readonly Dictionary<string, string> Exact = new Dictionary<string, string>
    {
        { "Floor",       "Floor" },
        { "Table_Bevel", "TableBevel" },
        { "Hole",        "HoleWell" },
        { "Hole_Ring",   "HoleRing" },
        { "Pickup",      "Pickup" },
        { "BouncePad",   "BouncePad" },
        { "SpeedBoost",  "SpeedBoost" },
        { "MovingWall",  "Wall" },
        { "RotatingWall","Wall" },
        { "Puck",        "Puck" },
        { "Island",      "Bumper" },
        { "GravityWell", "Corner" },
        { "Pad",         "BouncePad" },
    };
    static readonly (string prefix, string model)[] Prefixes =
    {
        ("Wall_Bump", "Bumper"),     // before "Wall_"
        ("Teleport_", "Teleporter"),
        ("Corner_",   "Corner"),
        ("Bound_",    "Wall"),
        ("Wall_",     "Wall"),
        ("Deadly_",   "Wall"),
        ("Zig_",      "Wall"),
        ("Funnel_",   "Wall"),
        ("Side_",     "Wall"),
        ("Trap_",     "Wall"),
        ("Bar_",      "Wall"),
        ("Gate_",     "Wall"),
        ("Shelf",     "Wall"),
        ("Spiral_",   "Wall"),
        ("Cross_",    "Wall"),
    };

    [MenuItem("RealBuca/Apply Custom Meshes")]
    public static void ApplyAll()
    {
        var meshes = LoadMeshes();
        if (meshes.Count == 0)
        {
            Debug.LogError("[MeshSwapper] No meshes found in " + ModelsDir +
                           " — did Unity import the OBJs?");
            return;
        }
        var accent = FindOrCreateAccentMaterial();

        int prefabCount = 0, swapTotal = 0;

        // ── all saved level prefabs ──────────────────────────────
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsDir });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            int swaps = SwapInHierarchy(root.transform, meshes, accent);
            if (swaps > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabCount++;
                swapTotal += swaps;
                Debug.Log($"[MeshSwapper]   {System.IO.Path.GetFileName(path)}: {swaps} meshes swapped");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ── currently open scene (puck, any scene-built visuals) ─
        int sceneSwaps = 0;
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var rootGO in scene.GetRootGameObjects())
            sceneSwaps += SwapInHierarchy(rootGO.transform, meshes, accent);
        if (sceneSwaps > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[MeshSwapper]   scene '{scene.name}': {sceneSwaps} meshes swapped (save the scene!)");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MeshSwapper] ✔ Done — {swapTotal} swaps across {prefabCount} prefabs " +
                  $"+ {sceneSwaps} in the open scene.");
    }

    // ─────────────────────────────────────────────────────────────
    static Dictionary<string, Mesh> LoadMeshes()
    {
        var result = new Dictionary<string, Mesh>();
        string[] names = { "Puck", "Wall", "HoleRing", "HoleWell", "Corner",
                           "BouncePad", "SpeedBoost", "Pickup", "Bumper",
                           "Teleporter", "Floor", "TableBevel" };
        foreach (var n in names)
        {
            var mesh = AssetDatabase.LoadAllAssetsAtPath($"{ModelsDir}/{n}.obj")
                                    .OfType<Mesh>().FirstOrDefault();
            if (mesh != null) result[n] = mesh;
            else Debug.LogWarning($"[MeshSwapper] missing mesh: {n}.obj");
        }
        return result;
    }

    static Material FindOrCreateAccentMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(AccentMatPath);
        if (mat != null) return mat;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader) { name = "Accent_Dark" };
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.13f, 0.12f, 0.18f, 1f));
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", new Color(0.13f, 0.12f, 0.18f, 1f));
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.5f);
        AssetDatabase.CreateAsset(mat, AccentMatPath);
        Debug.Log("[MeshSwapper] created " + AccentMatPath);
        return mat;
    }

    static string MapName(string goName)
    {
        if (Exact.TryGetValue(goName, out var exact)) return exact;
        foreach (var (prefix, model) in Prefixes)
            if (goName.StartsWith(prefix)) return model;
        return null;
    }

    static int SwapInHierarchy(Transform root, Dictionary<string, Mesh> meshes, Material accent)
    {
        int swaps = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            string model = MapName(mf.gameObject.name);
            if (model == null || !meshes.TryGetValue(model, out var mesh)) continue;
            if (mf.sharedMesh == mesh) continue; // already swapped

            mf.sharedMesh = mesh;
            swaps++;

            var mr = mf.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mats = mr.sharedMaterials;
                if (mesh.subMeshCount == 2)
                {
                    // Slot 1 keeps the existing game material (the neon
                    // color), slot 2 gets the shared dark accent.
                    var first = mats != null && mats.Length > 0 ? mats[0] : null;
                    mr.sharedMaterials = new[] { first, accent };
                }
                else if (mats != null && mats.Length > 1)
                {
                    mr.sharedMaterials = new[] { mats[0] };
                }
            }

            // Puck special-case: flat disc must not tumble on the rolling
            // sphere collider — freeze X/Z rotation, keep Y spin.
            if (model == "Puck")
            {
                var rb = mf.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.constraints = RigidbodyConstraints.FreezeRotationX |
                                     RigidbodyConstraints.FreezeRotationZ;
            }
        }
        return swaps;
    }
}
