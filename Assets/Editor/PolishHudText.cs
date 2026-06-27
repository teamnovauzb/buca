#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ONE-SHOT: give the in-game HUD text (LEVEL / STROKES / SCORE / TIMER / hints)
/// a crisp dark OUTLINE + a soft DROP-SHADOW so it reads cleanly against the busy
/// wood-and-neon board — the "professionally laid out" look.
///
///   RealBuca ▸ Polish HUD Text
///
/// It does this with a per-font MATERIAL PRESET (a copy of the font material with
/// outline + underlay enabled) assigned to the HUD texts — so the menu's text and
/// the font asset itself are NOT affected. PRESENTATION ONLY: no numbers, layout,
/// or logic change — only legibility/styling.
///
/// AFTER running you can DELETE THIS FILE. (The preset materials + saved scene keep it.)
/// </summary>
public static class PolishHudText
{
    const string GameScene = "Assets/Scenes/Game.unity";
    const string PresetDir = "Assets/Materials";

    [MenuItem("RealBuca/Polish HUD Text")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits the Game scene + creates material presets. Click ■ Stop, then run again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        // Prefer the GameHUD subtree; fall back to every TMP text in the scene.
        Transform hud = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = FindByName(root.transform, "GameHUD");
            if (t != null) { hud = t; break; }
        }

        var texts = new List<TMP_Text>();
        if (hud != null) texts.AddRange(hud.GetComponentsInChildren<TMP_Text>(true));
        else foreach (var root in scene.GetRootGameObjects()) texts.AddRange(root.GetComponentsInChildren<TMP_Text>(true));

        var cache = new Dictionary<Material, Material>();
        int n = 0;
        foreach (var t in texts)
        {
            var src = t.fontSharedMaterial;
            if (src == null || !src.HasProperty(ShaderUtilities.ID_OutlineWidth)) continue; // only SDF fonts
            if (!cache.TryGetValue(src, out var preset)) { preset = MakePreset(src); cache[src] = preset; }
            t.fontSharedMaterial = preset;
            EditorUtility.SetDirty(t);
            n++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("HUD text polished ✓",
            $"Applied a crisp outline + soft drop-shadow to {n} HUD text element(s).\n\n" +
            "• Press ▶ Play — LEVEL / STROKES / SCORE / TIMER now read cleanly over the board.\n" +
            "• You can DELETE this file now.\n\n" +
            "Only the text styling changed — every number/label and the layout are identical.",
            "OK");
    }

    static Material MakePreset(Material src)
    {
        string path = $"{PresetDir}/{Sanitize(src.name)}_HUD.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
        else m.CopyPropertiesFromMaterial(src);

        m.EnableKeyword(ShaderUtilities.Keyword_Outline);    // OUTLINE_ON
        m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.20f);
        m.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.85f));

        m.EnableKeyword(ShaderUtilities.Keyword_Underlay);   // UNDERLAY_ON (drop shadow)
        m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.70f));
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1.0f);
        m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1.0f);
        m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.30f);

        EditorUtility.SetDirty(m);
        return m;
    }

    static string Sanitize(string s)
    {
        foreach (var ch in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
        return string.IsNullOrEmpty(s) ? "Font" : s;
    }

    static Transform FindByName(Transform p, string n)
    {
        if (p.name == n) return p;
        foreach (Transform c in p) { var r = FindByName(c, n); if (r != null) return r; }
        return null;
    }
}
#endif
