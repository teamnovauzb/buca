#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ONE-SHOT: dress the bare Game board with PREGENERATED scenery so the table no
/// longer floats in a void — a warm wooden frame + felt surround + brass corner
/// brackets + side posts, a warm fill light, soft shadows, and a gentle post
/// grade (warm color + soft vignette + light bloom).
///
///   RealBuca ▸ Add Board Scenery        (Play mode must be STOPPED)
///
/// EVERYTHING decorative lives under ONE object named "BoardScenery", so you can
/// find + delete it in the Hierarchy with a single keystroke if you don't like it.
/// Re-running is SAFE: it rebuilds BoardScenery from scratch each time.
///
/// AFTER running you can DELETE THIS FILE (Assets/Editor/AddBoardScenery.cs).
///
/// SAFE BY DESIGN: every prop has NO collider, sits OUTSIDE the play rectangle
/// (x[-4.5,4.5], z[-7,7]) and below puck height, so nothing can block a shot or
/// the view. The camera, puck, HUD, managers, and per-level table are untouched.
/// </summary>
public static class AddBoardScenery
{
    const string GameScene      = "Assets/Scenes/Game.unity";
    const string MaterialFolder = "Assets/Materials";
    const string GradePath      = "Assets/Settings/BucaBoardGrade.asset";
    const string Root           = "BoardScenery";

    static Material _wood, _felt, _brass;

    [MenuItem("RealBuca/Add Board Scenery")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        BuildMaterials();
        var grade = BuildGradeProfile();

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        // ── idempotent: wipe any previous run ──
        var old = FindRoot(scene, Root);
        if (old != null) Object.DestroyImmediate(old);

        var sceneryGo = new GameObject(Root);
        var S = sceneryGo.transform;

        // ── wooden frame (4 aprons around the table edge) ──
        Box(S, "Apron_Left",  new Vector3( 6.0f, -0.15f,  0f),   new Vector3(1.2f, 0.25f, 16f), _wood);
        Box(S, "Apron_Right", new Vector3(-6.0f, -0.15f,  0f),   new Vector3(1.2f, 0.25f, 16f), _wood);
        Box(S, "Apron_Front", new Vector3( 0f,   -0.15f, -8.6f), new Vector3(12f,  0.25f, 1.2f), _wood);
        Box(S, "Apron_Back",  new Vector3( 0f,   -0.15f,  8.6f), new Vector3(12f,  0.25f, 1.2f), _wood);

        // ── felt surround under/around the table (biggest "finished" upgrade) ──
        Box(S, "FeltSurround", new Vector3(0f, -0.20f, 0f), new Vector3(14f, 0.04f, 18f), _felt);

        // ── brass corner brackets (catch the warm key + neon rim light) ──
        Box(S, "Corner_FL", new Vector3(-5.8f, -0.10f, -8.4f), new Vector3(0.7f, 0.4f, 0.7f), _brass);
        Box(S, "Corner_FR", new Vector3( 5.8f, -0.10f, -8.4f), new Vector3(0.7f, 0.4f, 0.7f), _brass);
        Box(S, "Corner_BL", new Vector3(-5.8f, -0.10f,  8.4f), new Vector3(0.7f, 0.4f, 0.7f), _brass);
        Box(S, "Corner_BR", new Vector3( 5.8f, -0.10f,  8.4f), new Vector3(0.7f, 0.4f, 0.7f), _brass);

        // ── short side posts with brass caps (vertical rhythm) ──
        Post(S, "Post_L1", new Vector3(-6.0f, 0.20f, -4.5f));
        Post(S, "Post_L2", new Vector3(-6.0f, 0.20f,  4.5f));
        Post(S, "Post_R1", new Vector3( 6.0f, 0.20f, -4.5f));
        Post(S, "Post_R2", new Vector3( 6.0f, 0.20f,  4.5f));

        // ── subtle low crates at the back corners (depth behind the table) ──
        Box(S, "Crate_BL", new Vector3(-7.2f, 0.0f, 10.5f), new Vector3(1.6f, 0.7f, 1.6f), _wood);
        Box(S, "Crate_BR", new Vector3( 7.2f, 0.0f, 10.5f), new Vector3(1.6f, 0.7f, 1.6f), _wood);

        // ── warm fill light (counterbalances the cool cyan fill) ──
        var lgo = new GameObject("Warm Table Glow");
        lgo.transform.SetParent(S);
        lgo.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        var L = lgo.AddComponent<Light>();
        L.type = LightType.Point;
        L.color = new Color(1.0f, 0.86f, 0.62f);
        L.intensity = 1.1f;
        L.range = 16f;
        L.shadows = LightShadows.None;

        // ── self-contained global volume carrying the warm grade ──
        var vgo = new GameObject("BoardGradeVolume");
        vgo.transform.SetParent(S);
        var vol = vgo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = grade;

        // ── soft shadows on the existing warm key light (steep angle → falls outward) ──
        var dir = FindRoot(scene, "Directional Light");
        if (dir != null)
        {
            var dl = dir.GetComponent<Light>();
            if (dl != null) { dl.shadows = LightShadows.Soft; dl.shadowStrength = 0.4f; }
        }

        // ── nudge the ambient ground slightly warm so the felt isn't cold-black ──
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.04f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Board scenery added ✓",
            "The Game scene now has a wooden frame, felt surround, brass brackets, side posts,\n" +
            "a warm fill light, soft shadows, and a gentle warm post-grade — all under one\n" +
            "object called \"BoardScenery\".\n\n" +
            "• Press ▶ Play to see it.\n" +
            "• Don't like a piece? Select \"BoardScenery\" in the Hierarchy and delete it (or just that child).\n" +
            "• When happy, you can DELETE this file (Assets/Editor/AddBoardScenery.cs).\n\n" +
            "Nothing has a collider and nothing sits in the play area, so gameplay is unchanged.",
            "OK");
    }

    // ── builders ─────────────────────────────────────────────
    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        StripCollider(go);
        return go;
    }

    static void Post(Transform parent, string name, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f); // r≈0.3, height ≈0.6
        if (_wood != null) go.GetComponent<MeshRenderer>().sharedMaterial = _wood;
        StripCollider(go);

        var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = name + "_Cap";
        cap.transform.SetParent(parent);                         // parent to root → uniform scale
        cap.transform.localPosition = new Vector3(pos.x, pos.y + 0.30f, pos.z);
        cap.transform.localScale = Vector3.one * 0.35f;
        if (_brass != null) cap.GetComponent<MeshRenderer>().sharedMaterial = _brass;
        StripCollider(cap);
    }

    static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    static GameObject FindRoot(Scene s, string name)
    {
        foreach (var g in s.GetRootGameObjects())
            if (g.name == name) return g;
        return null;
    }

    // ── materials (wood-friendly, saved as assets) ───────────
    static void BuildMaterials()
    {
        var woodTemplate = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Table_Edge.mat");
        var litTemplate  = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Rail_White.mat");
        var litShader    = litTemplate != null ? litTemplate.shader : Shader.Find("Universal Render Pipeline/Lit");

        // Wood_Frame: clone the realistic wood Table_Edge (keeps its WoodFloor007 maps), darker stain
        _wood = LoadOrCreate("Wood_Frame", () => woodTemplate != null ? new Material(woodTemplate) : new Material(litShader));
        SetCol(_wood, "_BaseColor", new Color(0.30f, 0.20f, 0.13f));
        SetCol(_wood, "_Color",     new Color(0.30f, 0.20f, 0.13f));
        SetF(_wood, "_Metallic", 0f); SetF(_wood, "_Smoothness", 0.40f);
        EditorUtility.SetDirty(_wood);

        // Felt_Surround: matte dark teal
        _felt = LoadOrCreate("Felt_Surround", () => new Material(litShader));
        if (litTemplate != null && _felt.shader == litTemplate.shader) _felt.CopyPropertiesFromMaterial(litTemplate);
        SetCol(_felt, "_BaseColor", new Color(0.06f, 0.16f, 0.16f));
        SetCol(_felt, "_Color",     new Color(0.06f, 0.16f, 0.16f));
        SetF(_felt, "_Metallic", 0f); SetF(_felt, "_Smoothness", 0.15f);
        if (_felt.HasProperty("_EmissionColor")) _felt.SetColor("_EmissionColor", Color.black);
        _felt.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(_felt);

        // Brass_Trim: warm metallic with a faint glint
        _brass = LoadOrCreate("Brass_Trim", () => new Material(litShader));
        if (litTemplate != null && _brass.shader == litTemplate.shader) _brass.CopyPropertiesFromMaterial(litTemplate);
        SetCol(_brass, "_BaseColor", new Color(0.72f, 0.55f, 0.25f));
        SetCol(_brass, "_Color",     new Color(0.72f, 0.55f, 0.25f));
        SetF(_brass, "_Metallic", 0.85f); SetF(_brass, "_Smoothness", 0.70f);
        if (_brass.HasProperty("_EmissionColor")) { _brass.SetColor("_EmissionColor", new Color(0.25f, 0.18f, 0.06f)); _brass.EnableKeyword("_EMISSION"); }
        _brass.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(_brass);
    }

    static Material LoadOrCreate(string name, System.Func<Material> create)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = create(); AssetDatabase.CreateAsset(m, path); }
        return m;
    }

    // ── post-processing grade (own profile, no DoF so the board stays sharp) ──
    static VolumeProfile BuildGradeProfile()
    {
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradePath);
        if (prof == null) { prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, GradePath); }

        var ca = GetOrAdd<ColorAdjustments>(prof);
        ca.active = true;
        SetVF(ca.postExposure, 0.08f); SetVF(ca.contrast, 6f); SetVF(ca.saturation, 8f);
        SetVC(ca.colorFilter, new Color(1.0f, 0.95f, 0.88f));

        var vig = GetOrAdd<Vignette>(prof);
        vig.active = true;
        SetVF(vig.intensity, 0.28f); SetVF(vig.smoothness, 0.5f);
        SetVC(vig.color, new Color(0.02f, 0.02f, 0.03f));

        var bloom = GetOrAdd<Bloom>(prof);
        bloom.active = true;
        SetVF(bloom.intensity, 0.9f); SetVF(bloom.threshold, 1.0f); SetVF(bloom.scatter, 0.6f);
        SetVC(bloom.tint, new Color(1f, 0.97f, 1f));

        EditorUtility.SetDirty(prof);
        return prof;
    }

    static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
    {
        if (p.Has<T>()) { p.TryGet<T>(out var c); return c; }
        return p.Add<T>(true);
    }
    static void SetVF(VolumeParameter<float> p, float v) { p.overrideState = true; p.value = v; }
    static void SetVC(VolumeParameter<Color> p, Color v) { p.overrideState = true; p.value = v; }
    static void SetCol(Material m, string prop, Color c) { if (m.HasProperty(prop)) m.SetColor(prop, c); }
    static void SetF(Material m, string prop, float v) { if (m.HasProperty(prop)) m.SetFloat(prop, v); }
}
#endif
