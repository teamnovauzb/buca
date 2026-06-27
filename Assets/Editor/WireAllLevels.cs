#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ONE-SHOT: finish the 30-level setup in a single click. Run AFTER
/// "Generate All Levels (30)".
///
///   RealBuca ▸ Wire All Levels   (Play mode must be STOPPED)
///
/// It does BOTH wiring steps:
///   1. Assigns Level_01..30 prefabs to LevelManager.levelPrefabs (Game scene).
///   2. Rebuilds the level-select picker to 30 tiles via MenuSetupHelper
///      (MainMenu scene), re-wiring every tile + the LEVELS button.
/// </summary>
public static class WireAllLevels
{
    const string GameScene = "Assets/Scenes/Game.unity";
    const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    const string PrefabFolder = "Assets/Prefabs/Levels";
    const int Count = 30;
    const int Columns = 5;

    [MenuItem("RealBuca/Wire All Levels")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "Wire All Levels edits scenes. Click ■ Stop, then run it again.", "OK");
            return;
        }

        // ── 1) Load prefabs ──────────────────────────────────────
        var prefabs = new List<GameObject>();
        int missing = 0;
        for (int i = 1; i <= Count; i++)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Level_{i:D2}.prefab");
            if (p != null) prefabs.Add(p);
            else { missing++; Debug.LogWarning($"[WireAllLevels] Missing Level_{i:D2}.prefab — run 'Generate All Levels (30)' first."); }
        }

        // ── 2) Wire prefabs into LevelManager (Game scene) ───────
        var gameScene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        var lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (lm != null)
        {
            lm.levelPrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(lm);
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
        }
        else Debug.LogWarning("[WireAllLevels] No LevelManager found in the Game scene.");

        // ── 3) Rebuild the level-select grid to 30 (MainMenu) ────
        var menuScene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        var helper = Object.FindFirstObjectByType<MenuSetupHelper>(FindObjectsInactive.Include);
        bool temp = false;
        if (helper == null)
        {
            helper = new GameObject("_TempMenuSetup").AddComponent<MenuSetupHelper>();
            temp = true;
        }
        helper.levelCount = Count;
        helper.columnsPerRow = Columns;
        helper.SpawnLevelSelectPanel(); // rebuilds the grid (replaces the old 15-tile panel)
        helper.WireLevelsButton();      // re-points LEVELS → the new panel
        if (temp) Object.DestroyImmediate(helper.gameObject);
        EditorSceneManager.MarkSceneDirty(menuScene);
        EditorSceneManager.SaveScene(menuScene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Wire All Levels ✓",
            $"• {prefabs.Count} level prefabs wired to LevelManager" +
            (missing > 0 ? $" ({missing} missing — generate them first)" : "") + "\n" +
            $"• Level picker rebuilt to {Count} tiles ({Columns}×{Mathf.CeilToInt(Count / (float)Columns)})\n\n" +
            "Press Play (MainMenu) → LEVELS to see all 30.", "OK");
    }
}
#endif
