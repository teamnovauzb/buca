#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// ONE-SHOT premium polish for the PUCK (the hero object) + its feedback FX.
/// PRESENTATION ONLY — no physics, collider, mass, launch/aim, speed cap, or
/// scoring is touched. It edits render materials + cosmetic PuckDynamics fields.
///
///   RealBuca ▸ Polish Puck & FX
///
/// • Puck_Yellow.mat   → true mirror chrome (Metallic 1, Smoothness 0.93) + a faint warm rim glow.
/// • PuckTrailMat.mat   → brighter, glowier trail.
/// • Particle_Yellow.mat→ punchier cyan wall-spark.
/// • PuckShadow.mat     → softer, less-harsh contact shadow.
/// • PuckDynamics (Game scene) → thicker trail + denser idle glow + brighter speed flare.
///
/// AFTER running you can DELETE THIS FILE. (Materials + the saved scene keep the look.)
/// </summary>
public static class PolishPuckAndFX
{
    const string GameScene = "Assets/Scenes/Game.unity";
    const string Mats = "Assets/Materials";

    [MenuItem("RealBuca/Polish Puck & FX")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits materials + the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        // ── Materials (render-only) ──
        var puck = Load("Puck_Yellow");
        if (puck != null)
        {
            SetF(puck, "_Metallic", 1.0f);
            SetF(puck, "_Smoothness", 0.93f);
            SetC(puck, "_EmissionColor", new Color(0.15f, 0.15f, 0.20f));
            SetC(puck, "_SpecColor", new Color(0.40f, 0.40f, 0.45f));
            if (puck.HasProperty("_EmissionColor")) puck.EnableKeyword("_EMISSION");
            Dirty(puck);
        }
        var trail = Load("PuckTrailMat");
        if (trail != null) { var c = new Color(1.6f, 1.2f, 0.15f); SetC(trail, "_BaseColor", c); SetC(trail, "_Color", c); Dirty(trail); }

        var spark = Load("Particle_Yellow");
        if (spark != null) { SetC(spark, "_EmissionColor", new Color(0.6f, 2.4f, 2.8f)); if (spark.HasProperty("_EmissionColor")) spark.EnableKeyword("_EMISSION"); Dirty(spark); }

        var shadow = Load("PuckShadow");
        if (shadow != null) { var c = new Color(0.8f, 0.6f, 0.1f, 0.22f); SetC(shadow, "_BaseColor", c); SetC(shadow, "_Color", c); Dirty(shadow); }

        AssetDatabase.SaveAssets();

        // ── PuckDynamics cosmetic fields (Game scene) ──
        int tuned = 0;
        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            var pd = root.GetComponentInChildren<PuckDynamics>(true);
            if (pd == null) continue;
            pd.trailWidthSlow  = 0.25f;   // was 0.18 — more presence at rest
            pd.trailWidthFast  = 1.10f;   // was 0.75 — real weight at speed
            pd.idleGlowRateSlow = 22f;    // was 14  — fuller "ready" halo
            pd.idleGlowRateFast = 70f;    // was 46  — denser energy when flying
            pd.emissionSlow     = 1.2f;   // was 1.0 — brighter resting glow
            pd.emissionFast     = 2.1f;   // was 1.55 — flares on a fast shot
            EditorUtility.SetDirty(pd);
            tuned++;
            break;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Puck & FX polished ✓",
            "The puck is now mirror chrome with a richer trail + glow, sparks punch harder, and the\n" +
            "contact shadow is softer.\n\n" +
            (tuned > 0 ? "PuckDynamics feel values updated on the puck.\n\n" : "(No PuckDynamics found in the Game scene — materials still updated.)\n\n") +
            "• Press ▶ Play and take a shot.\n" +
            "• You can DELETE this file now.\n\n" +
            "Gameplay is unchanged — these are render + cosmetic values only.",
            "OK");
    }

    static Material Load(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/{n}.mat");
    static void SetF(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
    static void SetC(Material m, string p, Color c) { if (m.HasProperty(p)) m.SetColor(p, c); }
    static void Dirty(Material m) { EditorUtility.SetDirty(m); }
}
#endif
