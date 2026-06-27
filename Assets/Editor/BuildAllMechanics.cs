#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// ONE CLICK: runs all the mechanic tools + unlock in sequence, with a single
/// confirmation at the end (no clicking OK on each one).
///
///   RealBuca ▸ ★ Add ALL Mechanics + Unlock (one click)
///
/// Runs: Add New Mechanics → Add Starter Mechanics → Add Mechanics Wave 2 →
/// Unlock All Levels. Then: open MainMenu, Play, LEVELS, pick any level.
/// Delete the tool files whenever you're done.
/// </summary>
public static class BuildAllMechanics
{
    [MenuItem("RealBuca/★ Add ALL Mechanics + Unlock (one click)")]
    static void All()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves level prefabs. Click ■ Stop, then run it again.", "OK");
            return;
        }

        bool ok = true;
        BucaBatch.Silent = true;        // suppress each tool's individual pop-up
        try
        {
            ok &= EditorApplication.ExecuteMenuItem("RealBuca/Add New Mechanics (one-shot)");
            ok &= EditorApplication.ExecuteMenuItem("RealBuca/Add Starter Mechanics (crokinole-sjoelen batch)");
            ok &= EditorApplication.ExecuteMenuItem("RealBuca/Add Mechanics Wave 2 (fill all levels)");
            ok &= EditorApplication.ExecuteMenuItem("RealBuca/Unlock All Levels (testing)");
        }
        finally { BucaBatch.Silent = false; }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(ok ? "All mechanics added ✓" : "Done — with a warning",
            "Ran in one click:\n" +
            "• Add New Mechanics  (bumpers, banking rails, ice, orbiting holes)\n" +
            "• Add Starter Mechanics  (pegs, funnel, gates, rim, gutter)\n" +
            "• Add Mechanics Wave 2  (mud, conveyor, trap pockets, vortex)\n" +
            "• Unlock All Levels\n\n" +
            "Now: open Assets/Scenes/MainMenu.unity → ▶ Play → click LEVELS → pick a level.\n" +
            "Try L5 (rim), L7 (pegs), L12 (conveyor), L15 (vortex).\n\n" +
            (ok ? "" : "⚠ One step didn't run — check the Console for which.\n"),
            "OK");
    }
}
#endif
