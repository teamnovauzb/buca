#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ONE-SHOT: turn the main menu into a vibrant NEON ARCADE screen.
///
/// Run it:  RealBuca ▸ Vibrant Neon Menu   (Play mode must be STOPPED)
///
///   • Post-processing (BucaMenuVolume): adds/cranks BLOOM so every bright
///     element glows, plus a VIGNETTE for mood and punchier saturation.
///   • Buttons → vibrant neon colors (hot-pink / electric-cyan / violet) with
///     a glowing outline edge.
///   • Brightens the cyan + pink rim lights so the backdrop pops.
///
/// Tweak the numbers/colors below and re-run to taste.
/// </summary>
public static class VibrantNeonMenu
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string MenuVolumePath = "Assets/Settings/BucaMenuVolume.asset";

    static readonly (string name, string hex)[] Buttons =
    {
        ("PlayButton",   "#FF2E88"), // hot pink
        ("LEVELSButton", "#19D3FF"), // electric cyan
        ("QuitButton",   "#A24BFF"), // neon violet
    };

    [MenuItem("RealBuca/Vibrant Neon Menu")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "Vibrant Neon Menu edits the MainMenu scene, which can't be done while playing.\n\n" +
                "Click the ■ Stop button, then run it again.", "OK");
            return;
        }

        // 1) Post-processing — the neon GLOW.
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MenuVolumePath);
        if (profile != null)
        {
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            SetF(bloom.intensity, 1.4f);
            SetF(bloom.threshold, 0.82f);
            SetF(bloom.scatter, 0.68f);
            SetC(bloom.tint, new Color(1f, 0.96f, 1f));

            var vig = GetOrAdd<Vignette>(profile);
            vig.active = true;
            SetF(vig.intensity, 0.42f);
            SetF(vig.smoothness, 0.45f);

            var ca = GetOrAdd<ColorAdjustments>(profile);
            ca.active = true;
            SetF(ca.saturation, 22f);
            SetF(ca.contrast, 12f);
            SetF(ca.postExposure, 0.12f);

            EditorUtility.SetDirty(profile);
        }
        else Debug.LogWarning($"[VibrantNeonMenu] Volume profile not found at {MenuVolumePath}.");

        // 2) Scene — vibrant buttons + glow + brighter neon lights.
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int btnCount = 0;
        foreach (var (name, hex) in Buttons)
        {
            var go = FindInScene(scene, name);
            if (go == null || !ColorUtility.TryParseHtmlString(hex, out var col)) continue;

            var btn = go.GetComponent<Button>();
            Image img = btn != null ? btn.targetGraphic as Image : go.GetComponent<Image>();
            if (img == null) img = go.GetComponent<Image>();
            if (img != null) { img.color = col; EditorUtility.SetDirty(img); }

            // Glowing neon edge (works on overlay UI, no bloom needed).
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(col.r, col.g, col.b, 0.65f);
            outline.effectDistance = new Vector2(5f, -5f);
            EditorUtility.SetDirty(outline);
            btnCount++;
        }

        foreach (var lname in new[] { "Fill Light Cyan", "Rim Light Pink" })
        {
            var lgo = FindInScene(scene, lname);
            var li = lgo != null ? lgo.GetComponent<Light>() : null;
            if (li != null) { li.intensity = Mathf.Max(li.intensity, 1.4f); EditorUtility.SetDirty(li); }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Vibrant neon menu ✓",
            $"Neon glow applied:\n• Bloom + vignette + punchier color\n• {btnCount} buttons recolored neon + glow edge\n• Brighter cyan/pink lights\n\n" +
            "Press Play from MainMenu to see it. Tell me what to push further.", "OK");
    }

    static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
    {
        if (p.Has<T>()) { p.TryGet<T>(out var c); return c; }
        return p.Add<T>(true);
    }

    static void SetF(VolumeParameter<float> p, float v) { p.overrideState = true; p.value = v; }
    static void SetC(VolumeParameter<Color> p, Color v) { p.overrideState = true; p.value = v; }

    static GameObject FindInScene(Scene s, string name)
    {
        foreach (var r in s.GetRootGameObjects())
        {
            var t = FindRec(r.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindRec(Transform p, string n)
    {
        if (p.name == n) return p;
        foreach (Transform c in p) { var r = FindRec(c, n); if (r != null) return r; }
        return null;
    }
}
#endif
