#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// TESTING HELPER: unlock every level so you can jump straight to any mechanic via
/// the LEVELS picker (instead of beating them in order). Plus a reset to go back to
/// a fresh "only Level 1 unlocked" start.
///
///   RealBuca ▸ Unlock All Levels (testing)
///   RealBuca ▸ Reset Progress (fresh start)
///
/// These just set the saved progress (PlayerPrefs). Delete this file whenever.
/// </summary>
public static class UnlockAllLevels
{
    [MenuItem("RealBuca/Unlock All Levels (testing)")]
    static void Unlock()
    {
        PlayerPrefs.SetInt("BucaHighestLevel", 29);   // all 30 (index 0..29) playable
        PlayerPrefs.Save();
        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("All levels unlocked ✓",
            "Every level is now selectable. In Play mode, click LEVELS and pick any level " +
            "to try its mechanic.\n\nUse 'Reset Progress (fresh start)' to go back to Level-1-only.",
            "OK");
    }

    [MenuItem("RealBuca/Reset Progress (fresh start)")]
    static void Reset()
    {
        PlayerPrefs.SetInt("BucaHighestLevel", 0);    // only Level 1 unlocked
        PlayerPrefs.DeleteKey("BucaCurrentLevel");
        PlayerPrefs.DeleteKey("BucaPendingLevel");

        // A fresh test profile must also forget old results. Otherwise the
        // level picker keeps showing pre-balance high scores/stars because
        // gameplay deliberately stores the best result ever achieved.
        for (int i = 0; i < 30; i++)
        {
            PlayerPrefs.DeleteKey(LevelManager.PrefLevelStars + i);
            PlayerPrefs.DeleteKey(LevelManager.PrefLevelScore + i);
        }
        PlayerPrefs.DeleteKey(TutorialController.PrefSeen);
        PlayerPrefs.Save();
        EditorUtility.DisplayDialog("Progress reset ✓",
            "Local progress, stars, scores, and tutorial state were cleared. " +
            "Only Level 1 is unlocked.", "OK");
    }
}
#endif
