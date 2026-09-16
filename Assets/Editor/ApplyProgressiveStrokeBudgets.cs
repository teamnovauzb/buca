#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the tester-requested progressive missed-shot budgets without changing
/// the fixed 30-second timer, level layouts, gameplay mechanics, or star targets.
///
/// Run: RealBuca -> Apply Progressive Stroke Budgets
///
/// The command also rebuilds the lives HUD with six heart slots. At runtime,
/// LevelManager hides slots that are not used by the current level.
/// </summary>
public static class ApplyProgressiveStrokeBudgets
{
    const int LevelCount = 30;
    const float FixedTimeLimit = 30f;
    const string SettingsFolder = "Assets/Settings/Levels";
    const string GameScenePath = "Assets/Scenes/Game.unity";

    static readonly int[] StrokeBudgets =
    {
        6, 6, 6, 6, 6, 6,
        5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5,
        4, 4, 4, 4, 4, 4,
        3, 3, 3, 3, 3, 3
    };

    [MenuItem("RealBuca/Apply Progressive Stroke Budgets")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play Mode",
                "Stop Play Mode, then run the stroke-budget command again.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureSettingsFolder();
        var settings = new LevelSettings[LevelCount];

        for (int i = 0; i < LevelCount; i++)
        {
            string path = $"{SettingsFolder}/Level_{i + 1:D2}_Settings.asset";
            LevelSettings setting = AssetDatabase.LoadAssetAtPath<LevelSettings>(path);
            if (setting == null)
            {
                setting = ScriptableObject.CreateInstance<LevelSettings>();
                AssetDatabase.CreateAsset(setting, path);
            }

            // Preserve threeStarStrokes and displayName: this tool changes only
            // the actual attempt budget and enforces the existing timer promise.
            setting.timeLimit = FixedTimeLimit;
            setting.maxLives = StrokeBudgets[i];
            EditorUtility.SetDirty(setting);
            settings[i] = setting;
        }

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        LevelManager manager = Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            EditorUtility.DisplayDialog("LevelManager not found",
                "Game.unity has no LevelManager. No scene wiring was changed.", "OK");
            return;
        }

        manager.levelSettings = settings;
        manager.defaultTimeLimit = FixedTimeLimit;
        manager.maxLives = 6;
        manager.useProgressiveDifficulty = true;
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        bool oldSilent = BucaBatch.Silent;
        bool hudUpdated;
        BucaBatch.Silent = true;
        try
        {
            hudUpdated = EditorApplication.ExecuteMenuItem("RealBuca/Add Lives System");
        }
        finally
        {
            BucaBatch.Silent = oldSilent;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            hudUpdated ? "Stroke budgets applied" : "Budgets applied — HUD needs attention",
            "Timer: 30 seconds on every level\n\n" +
            "Levels 1–6: 6 attempts\n" +
            "Levels 7–18: 5 attempts\n" +
            "Levels 19–24: 4 attempts\n" +
            "Levels 25–30: 3 attempts\n\n" +
            (hudUpdated
                ? "The heart HUD was rebuilt with six responsive slots."
                : "Run RealBuca > Add Lives System once to rebuild the heart HUD."),
            "OK");
    }

    static void EnsureSettingsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(SettingsFolder))
            AssetDatabase.CreateFolder("Assets/Settings", "Levels");
    }
}
#endif
