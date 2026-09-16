#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Recovers the real MainMenu asset when Unity restores an empty in-memory
/// backup after Play Mode or a failed low-disk scene backup. Editor-only.
/// </summary>
[InitializeOnLoad]
public static class MainMenuSceneRecovery
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static MainMenuSceneRecovery()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        EditorApplication.delayCall += RecoverEmptyMainMenu;
    }

    static void RecoverEmptyMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || active.rootCount != 0 || active.isDirty) return;

        string path = (active.path ?? string.Empty).Replace('\\', '/');
        bool isBrokenMainMenu = active.name == "MainMenu"
            || path == MainMenuPath
            || path.StartsWith("Temp/__Backupscenes/", StringComparison.OrdinalIgnoreCase);
        if (!isBrokenMainMenu) return;

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (sceneAsset == null)
        {
            Debug.LogError($"[MainMenuSceneRecovery] Scene asset not found: {MainMenuPath}");
            return;
        }

        Scene recovered = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        if (!recovered.IsValid() || recovered.rootCount == 0)
        {
            Debug.LogError("[MainMenuSceneRecovery] MainMenu reload completed but the scene is still empty.");
            return;
        }

        Selection.activeObject = sceneAsset;
        Debug.Log($"[MainMenuSceneRecovery] Restored {recovered.rootCount} MainMenu root objects from disk.");
    }
}
#endif
