#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT: restyle the four plain white corner balls (Corner_TL/TR/BL/BR) in
/// ALL 30 levels into polished BRASS caps — matching the table's brass frame
/// brackets + posts — and size them up a touch so they read as deliberate caps,
/// not tiny balls.
///
///   RealBuca ▸ Premium Corner Caps (Brass)
///
/// Reuses the scenery's Brass_Trim material (creates it if missing) so the corners,
/// brackets, posts, and frame all speak one premium material language. Because the
/// caps now use Brass_Trim instead of Rail_White, the Gunmetal+Cyan wall restyle no
/// longer touches them — they stay brass, a warm accent on the cool steel rails.
///
/// AFTER running you can DELETE THIS FILE (Assets/Editor/PremiumCornerCaps.cs).
/// Re-running is safe (idempotent). Note: re-running "Generate All Levels (30)"
/// rebuilds the prefabs and reverts the caps to white — just run this again.
/// </summary>
public static class PremiumCornerCaps
{
    const string PrefabFolder   = "Assets/Prefabs/Levels";
    const string MaterialFolder = "Assets/Materials";
    const string BrassName      = "Brass_Trim";   // reuse the scenery brass for cohesion
    const float  CapScale       = 0.34f;          // up from 0.24 so they read as caps

    [MenuItem("RealBuca/Premium Corner Caps (Brass)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves level prefabs. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var brass = EnsureBrass();

        int levels = 0, caps = 0;
        for (int i = 1; i <= 30; i++)
        {
            string path = $"{PrefabFolder}/Level_{i:D2}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogWarning($"[PremiumCornerCaps] missing {path}"); continue; }
            try
            {
                int n = 0;
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!mr.gameObject.name.StartsWith("Corner_")) continue;
                    mr.sharedMaterial = brass;
                    mr.transform.localScale = Vector3.one * CapScale;
                    n++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                levels++; caps += n;
            }
            catch (System.Exception e) { Debug.LogError($"[PremiumCornerCaps] {path}: {e}"); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Premium corner caps ✓",
            $"Restyled {caps} corner caps across {levels} levels to polished brass.\n\n" +
            "• Press ▶ Play to see them.\n" +
            "• You can now DELETE this file (Assets/Editor/PremiumCornerCaps.cs).\n\n" +
            "They now match your brass frame, and stay brass even after the\n" +
            "Gunmetal+Cyan wall restyle.",
            "OK");
    }

    // Load the scenery brass; create it (same recipe) if it isn't there yet.
    static Material EnsureBrass()
    {
        string path = $"{MaterialFolder}/{BrassName}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        var lit = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Rail_White.mat");
        var shader = lit != null ? lit.shader : Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(shader);
        if (lit != null && m.shader == lit.shader) m.CopyPropertiesFromMaterial(lit);
        SetCol(m, "_BaseColor", new Color(0.72f, 0.55f, 0.25f));
        SetCol(m, "_Color",     new Color(0.72f, 0.55f, 0.25f));
        SetF(m, "_Metallic", 0.85f); SetF(m, "_Smoothness", 0.70f);
        if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", new Color(0.25f, 0.18f, 0.06f)); m.EnableKeyword("_EMISSION"); }
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void SetCol(Material m, string p, Color c) { if (m.HasProperty(p)) m.SetColor(p, c); }
    static void SetF(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
}
#endif
