#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT: recolor the in-game effect materials to the neon theme so sparks,
/// win bursts, and the aim line match the menu/HUD.
///
/// Run it:  RealBuca ▸ Theme Game FX
///
/// Asset-only (no scene edit), so it works even while Play mode is running.
///   • Particle_Yellow (wall sparks) → cyan
///   • WinRingMat (win ring)         → cyan
///   • WinBurstMat (win burst)       → magenta
///   • AimLine (aim guide)           → cyan
/// Edit the hex values below and re-run to retune.
/// </summary>
public static class ThemeGameFX
{
    static readonly (string mat, string hex)[] Targets =
    {
        ("Particle_Yellow", "#2DE2FF"), // wall sparks → cyan
        ("WinRingMat",      "#2DE2FF"), // win ring → cyan
        ("WinBurstMat",     "#FF3D9E"), // win burst → magenta
        ("AimLine",         "#2DE2FF"), // aim guide → cyan
    };

    static readonly string[] ColorProps =
    { "_TintColor", "_BaseColor", "_Color", "_EmissionColor" };

    [MenuItem("RealBuca/Theme Game FX")]
    static void Apply()
    {
        int n = 0;
        foreach (var (matName, hex) in Targets)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{matName}.mat");
            if (mat == null) { Debug.LogWarning($"[ThemeGameFX] Material not found: {matName}"); continue; }
            if (!ColorUtility.TryParseHtmlString(hex, out var c)) continue;

            foreach (var p in ColorProps)
            {
                if (!mat.HasProperty(p)) continue;
                // Emission gets an HDR-bright version so it still glows under bloom.
                mat.SetColor(p, p == "_EmissionColor" ? c * 2.2f : KeepAlpha(mat, p, c));
            }
            if (mat.HasProperty("_EmissionColor")) mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            n++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Theme Game FX ✓",
            $"Recolored {n} effect material(s) to neon (sparks/aim = cyan, win burst = magenta).\n\n" +
            "Press Play to see them. Edit the hex values in the script and re-run to retune.", "OK");
    }

    static Color KeepAlpha(Material m, string prop, Color c)
    {
        c.a = m.GetColor(prop).a; // preserve additive/particle alpha
        return c;
    }
}
#endif
