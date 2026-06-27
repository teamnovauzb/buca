#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// ONE CLICK: applies the ENTIRE premium setup — every visual + mechanic tool — in
/// sequence, with a single confirmation at the end. Use this to make sure your whole
/// game is fully premium and nothing is half-applied.
///
///   RealBuca ▸ ★★ Set Up EVERYTHING Premium (one click)
///
/// Runs: wall restyle, chrome puck, clean puck, post-processing, HUD polish, board
/// scenery, brass corners, menu button, all 3 mechanic batches, table markings, and
/// unlock-all. Then: open MainMenu → Play. Delete the tool files whenever you're done.
/// </summary>
public static class BuildEverything
{
    static readonly string[] Steps =
    {
        // ── visuals / premium look ──
        "RealBuca/Restyle Walls (Gunmetal + Cyan)",
        "RealBuca/Polish Puck & FX",
        "RealBuca/Puck → Remove Spin Dot (clean look)",
        "RealBuca/Bake Volume Profiles (Post FX)",
        "RealBuca/Polish HUD Text",
        "RealBuca/Add Board Scenery",
        "RealBuca/Premium Corner Caps (Brass)",
        "RealBuca/Add Main Menu Button",
        "RealBuca/Premium Victory Celebration (confetti)",
        "RealBuca/Add Table Markings (decorative)",
        // ── mechanics ──
        "RealBuca/Add New Mechanics (one-shot)",
        "RealBuca/Add Starter Mechanics (crokinole-sjoelen batch)",
        "RealBuca/Add Mechanics Wave 2 (fill all levels)",
        // ── testing ──
        "RealBuca/Unlock All Levels (testing)",
    };

    [MenuItem("RealBuca/★★ Set Up EVERYTHING Premium (one click)")]
    static void All()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves your scenes and prefabs. Click ■ Stop, then run it again.", "OK");
            return;
        }

        int done = 0, failed = 0;
        string missing = "";
        BucaBatch.Silent = true;            // suppress each tool's individual pop-up
        try
        {
            foreach (var step in Steps)
            {
                if (EditorApplication.ExecuteMenuItem(step)) done++;
                else { failed++; missing += "\n• " + step; }
            }
        }
        finally { BucaBatch.Silent = false; }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(failed == 0 ? "Everything applied ✓" : "Applied — with warnings",
            $"Ran {done} premium setup steps in one click:\n" +
            "walls · chrome puck · post-FX · HUD · scenery · brass corners · menu button ·\n" +
            "table markings · all mechanics · unlock.\n\n" +
            "Now: open Assets/Scenes/MainMenu.unity → ▶ Play → click LEVELS → pick any level.\n\n" +
            (failed == 0 ? "Your whole game is now at its premium baseline." :
             $"⚠ {failed} step(s) didn't run (their tool file may be deleted):{missing}\n" +
             "That's fine if you intentionally removed those tools."),
            "OK");
    }
}
#endif
