#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor convenience: force Unity's ▶ Play button to ALWAYS start from the
/// Main Menu, no matter which scene is currently open. (By default ▶ plays
/// whatever scene is open, so opening the Game scene and pressing ▶ skips the
/// menu and drops you straight into a level.)
///
///   RealBuca ▸ Boot From Main Menu (editor ▶)   ← toggles it on/off (shows a ✓)
///
/// Editor-only — has NO effect on the actual build (the build already boots from
/// the first scene in Build Settings, which is MainMenu). Keep this file; it's a
/// handy testing toggle. The setting resets when you restart Unity — just click
/// the menu item again to re-enable.
/// </summary>
public static class BootFromMainMenu
{
    const string MenuPath = "RealBuca/Boot From Main Menu (editor ▶)";
    const string MainMenuScene = "Assets/Scenes/MainMenu.unity";

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        if (EditorSceneManager.playModeStartScene != null)
        {
            EditorSceneManager.playModeStartScene = null;
            EditorUtility.DisplayDialog("Boot From Main Menu: OFF",
                "Unity's ▶ now plays whatever scene is currently open (Unity default).", "OK");
        }
        else
        {
            var s = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScene);
            if (s == null)
            {
                EditorUtility.DisplayDialog("Scene not found",
                    "Couldn't find " + MainMenuScene + ". Nothing changed.", "OK");
                return;
            }
            EditorSceneManager.playModeStartScene = s;
            EditorUtility.DisplayDialog("Boot From Main Menu: ON",
                "Now Unity's ▶ ALWAYS starts from the Main Menu, no matter which scene is open.\n\n" +
                "Press ▶ → the menu appears → click PLAY → Level 1.\n\n" +
                "(This resets if you restart Unity — just click the menu item again.)", "OK");
        }
    }

    // Adds a ✓ next to the menu item when the override is active.
    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, EditorSceneManager.playModeStartScene != null);
        return true;
    }
}
#endif
