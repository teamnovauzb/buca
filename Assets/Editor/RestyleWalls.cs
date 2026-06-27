#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT: restyle every maze/boundary/corner wall in ALL 30 levels from the
/// plain white neon tube to a "Gunmetal + Electric-Cyan Groove" look — a dark
/// satin-steel body with a vivid cyan emissive glow.
///
///   RealBuca ▸ Restyle Walls (Gunmetal + Cyan)
///
/// HOW IT REACHES ALL 30 LEVELS: every wall, boundary rail, moving/rotating wall,
/// and corner shares ONE material asset — Rail_White.mat. Recoloring that single
/// asset instantly restyles the walls in every level. The RailLight component now
/// derives its hit-flash from the material's emission, so touched walls flare a
/// brighter cyan instead of reverting to white.
///
/// AFTER running you can DELETE THIS FILE (Assets/Editor/RestyleWalls.cs).
/// KEEP RailLight.cs (it's the permanent component that drives the glow).
///
/// Does NOT touch: the gold banking rail (Mechanic_BankGold), pink deadly walls
/// (Deadly_Pink), brass scenery, pads, holes, or teleporters — only Rail_White.
/// </summary>
public static class RestyleWalls
{
    const string RailMat = "Assets/Materials/Rail_White.mat";

    [MenuItem("RealBuca/Restyle Walls (Gunmetal + Cyan)")]
    static void Apply()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(RailMat);
        if (m == null)
        {
            EditorUtility.DisplayDialog("Rail_White not found",
                $"Couldn't load {RailMat}. Nothing changed.", "OK");
            return;
        }

        // Gunmetal body — dark cool satin steel
        SetCol(m, "_BaseColor", new Color(0.16f, 0.18f, 0.21f));
        SetCol(m, "_Color",     new Color(0.16f, 0.18f, 0.21f));
        SetF(m, "_Metallic",   0.90f);
        SetF(m, "_Smoothness", 0.70f);
        SetF(m, "_Glossiness", 0.70f);   // built-in fallback name, harmless on URP

        // Electric-cyan glow (HDR so it blooms); RailLight derives the hit-flash from this
        if (m.HasProperty("_EmissionColor"))
        {
            m.SetColor("_EmissionColor", new Color(0.9f, 3.2f, 4.2f));
            m.EnableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Walls restyled ✓",
            "All walls, boundaries, and corners across ALL 30 levels are now dark\n" +
            "satin-steel with an electric-cyan glow — and they flare brighter cyan\n" +
            "when the puck hits them.\n\n" +
            "• Press ▶ Play to see it (any level).\n" +
            "• You can now DELETE this file (Assets/Editor/RestyleWalls.cs).\n" +
            "• KEEP RailLight.cs — it drives the glow + hit-flash.\n\n" +
            "Your gold banking rail and pink deadly walls are untouched, so the\n" +
            "mechanics still read as distinct.",
            "OK");
    }

    static void SetCol(Material m, string prop, Color c) { if (m.HasProperty(prop)) m.SetColor(prop, c); }
    static void SetF(Material m, string prop, float v) { if (m.HasProperty(prop)) m.SetFloat(prop, v); }
}
#endif
