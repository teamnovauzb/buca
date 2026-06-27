#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// ONE-SHOT: correctly AUTHOR the post-processing profiles and bind them so the
/// game finally has working Bloom / Vignette / Color-grade / Tonemapping.
///
/// WHY THIS EXISTS: VolumeProfile components must be saved as SUB-ASSETS
/// (AssetDatabase.AddObjectToAsset). Earlier scripts added them only in memory,
/// so BucaMenuVolume + BucaBoardGrade ended up with empty/dangling slots — i.e.
/// the game had NO post-processing at all. This builds them the right way and
/// ensures each scene has a global Volume + a post-enabled camera.
///
///   RealBuca ▸ Bake Volume Profiles (Post FX)
///
/// PRESENTATION ONLY — post-process is a final compositing layer; no gameplay,
/// geometry, collision, or rules are touched. AFTER running you can DELETE THIS FILE.
/// </summary>
public static class BakeVolumeProfiles
{
    const string MenuScene = "Assets/Scenes/MainMenu.unity";
    const string GameScene = "Assets/Scenes/Game.unity";
    const string MenuProfile = "Assets/Settings/BucaMenuVolume.asset";
    const string BoardProfile = "Assets/Settings/BucaBoardGrade.asset";

    [MenuItem("RealBuca/Bake Volume Profiles (Post FX)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits profiles + both scenes. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var menu = AuthorMenu();
        var board = AuthorBoard();

        // Bind a global Volume + enable camera post in each scene.
        BindSceneVolume(MenuScene, "Menu Post Volume", menu);
        BindSceneVolume(GameScene, "BoardGradeVolume", board);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Post-processing baked ✓",
            "Working Bloom + Vignette + Color-grade + Tonemapping are now authored into\n" +
            "BucaMenuVolume + BucaBoardGrade, with a global Volume bound and the camera\n" +
            "set to render post-processing in BOTH the menu and the game.\n\n" +
            "• Press ▶ Play (start from MainMenu) — the neon menu now glows, and the\n" +
            "  wood board has a cinematic graded frame.\n" +
            "• You can DELETE this file now.\n\n" +
            "Pure compositing layer — gameplay is unchanged.",
            "OK");
    }

    // ── Menu grade: vivid synthwave neon glow ──
    static VolumeProfile AuthorMenu()
    {
        var p = LoadOrCreate(MenuProfile);
        ClearProfile(p);

        var tm = Add<Tonemapping>(p); tm.active = true; tm.mode.overrideState = true; tm.mode.value = TonemappingMode.Neutral;
        var bl = Add<Bloom>(p); bl.active = true; SetF(bl.threshold, 0.80f); SetF(bl.intensity, 1.10f); SetF(bl.scatter, 0.70f); SetC(bl.tint, new Color(1f, 0.98f, 1f));
        var vg = Add<Vignette>(p); vg.active = true; SetF(vg.intensity, 0.30f); SetF(vg.smoothness, 0.50f); SetC(vg.color, new Color(0.02f, 0.01f, 0.04f));
        var ca = Add<ColorAdjustments>(p); ca.active = true; SetF(ca.postExposure, 0.05f); SetF(ca.contrast, 8f); SetF(ca.saturation, 18f);

        EditorUtility.SetDirty(p);
        return p;
    }

    // ── Board grade: warm cinematic wood-table frame ──
    static VolumeProfile AuthorBoard()
    {
        var p = LoadOrCreate(BoardProfile);
        ClearProfile(p);

        var tm = Add<Tonemapping>(p); tm.active = true; tm.mode.overrideState = true; tm.mode.value = TonemappingMode.Neutral;
        var bl = Add<Bloom>(p); bl.active = true; SetF(bl.threshold, 0.80f); SetF(bl.intensity, 1.00f); SetF(bl.scatter, 0.60f); SetC(bl.tint, new Color(1f, 0.97f, 1f));
        var vg = Add<Vignette>(p); vg.active = true; SetF(vg.intensity, 0.20f); SetF(vg.smoothness, 0.65f); SetC(vg.color, new Color(0.08f, 0.05f, 0.06f));
        var ca = Add<ColorAdjustments>(p); ca.active = true; SetF(ca.postExposure, 0.05f); SetF(ca.contrast, 8f); SetF(ca.saturation, 12f); SetC(ca.colorFilter, new Color(1f, 0.96f, 0.90f));

        EditorUtility.SetDirty(p);
        return p;
    }

    // ── profile authoring helpers ──
    static VolumeProfile LoadOrCreate(string path)
    {
        var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
        return p;
    }

    // Wipe existing components AND any orphaned sub-assets (the dangling {fileID:0}).
    static void ClearProfile(VolumeProfile p)
    {
        var listed = p.components.ToArray();
        foreach (var c in listed) if (c != null) Object.DestroyImmediate(c, true);
        p.components.Clear();

        string path = AssetDatabase.GetAssetPath(p);
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is VolumeComponent vc) Object.DestroyImmediate(vc, true);
    }

    // Create a component AND register it as a sub-asset so it actually persists.
    static T Add<T>(VolumeProfile p) where T : VolumeComponent
    {
        var c = ScriptableObject.CreateInstance<T>();
        c.name = typeof(T).Name;
        c.hideFlags = HideFlags.HideInHierarchy;
        p.components.Add(c);
        AssetDatabase.AddObjectToAsset(c, p);
        return c;
    }

    static void SetF(VolumeParameter<float> p, float v) { p.overrideState = true; p.value = v; }
    static void SetC(VolumeParameter<Color> p, Color v) { p.overrideState = true; p.value = v; }

    // ── scene binding: ensure a global Volume points at the profile + camera renders post ──
    static void BindSceneVolume(string scenePath, string preferredName, VolumeProfile profile)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Volume target = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var v = root.GetComponentInChildren<Volume>(true);
            if (v == null) continue;
            target = v;
            if (v.gameObject.name == preferredName) break;   // prefer the intended one
        }
        if (target == null)
            target = new GameObject(preferredName).AddComponent<Volume>();

        target.isGlobal = true;
        if (target.priority < 1f) target.priority = 1f;
        target.sharedProfile = profile;
        EditorUtility.SetDirty(target);

        // Camera must render post-processing for any of this to show.
        Camera cam = Camera.main;
        if (cam == null)
            foreach (var root in scene.GetRootGameObjects())
            {
                var c = root.GetComponentInChildren<Camera>(true);
                if (c != null) { cam = c; break; }
            }
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) { data.renderPostProcessing = true; EditorUtility.SetDirty(data); }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
