#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ONE-SHOT: turn the win burst into a premium CONFETTI celebration. PRESENTATION
/// ONLY — it just reconfigures the existing `winBurst` ParticleSystem that the win
/// sequence already plays, so no code/gameplay/scoring changes.
///
///   RealBuca ▸ Premium Victory Celebration (confetti)
///
/// Multi-color, explosive outward, then falls + lingers in staggered bursts — so a
/// sink ends in a real "you nailed it" pop instead of a small puff.
///
/// AFTER running you can DELETE THIS FILE.
/// </summary>
public static class PremiumVictoryCelebration
{
    const string GameScene = "Assets/Scenes/Game.unity";

    [MenuItem("RealBuca/Premium Victory Celebration (confetti)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        // Find the win burst — prefer LevelManager.winBurst, else a PS named "win…".
        ParticleSystem ps = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var lm = root.GetComponentInChildren<LevelManager>(true);
            if (lm != null && lm.winBurst != null) { ps = lm.winBurst; break; }
        }
        if (ps == null)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
                    if (p.name.ToLowerInvariant().Contains("win")) { ps = p; break; }
                if (ps != null) break;
            }
        }
        if (ps == null)
        {
            if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Win burst not found",
                "Couldn't find the win ParticleSystem in the Game scene. Nothing changed.", "OK");
            return;
        }

        ConfigureConfetti(ps);
        EditorUtility.SetDirty(ps);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Victory celebration ✓",
            "Sinking the puck now triggers a multi-color confetti burst that pops out, falls, and lingers.\n\n" +
            "• Press ▶ Play and sink a shot.\n• You can DELETE this file.\n\n" +
            "Pure visual — scoring, rules, and physics are unchanged.",
            "OK");
    }

    static void ConfigureConfetti(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 1.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.283f);
        main.gravityModifier = 0.5f;                                  // confetti falls
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Random bright confetti colors.
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.30f, 0.45f), 0.00f),
                new GradientColorKey(new Color(1f, 0.85f, 0.25f), 0.33f),
                new GradientColorKey(new Color(0.30f, 0.92f, 1f), 0.66f),
                new GradientColorKey(new Color(0.70f, 0.45f, 1f), 1.00f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        var startCol = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
        main.startColor = startCol;

        // Staggered bursts = a sustained pop, not one puff.
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0.00f, (short)70),
            new ParticleSystem.Burst(0.18f, (short)35),
            new ParticleSystem.Burst(0.36f, (short)35),
        });

        // Explode outward in all directions.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        // Fade out at the end of life.
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(fade);
    }
}
#endif
