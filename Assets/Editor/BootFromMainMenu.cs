#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

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
/// handy testing toggle. The preference is remembered per project and defaults
/// to MainMenu, including when Unity opens an empty Untitled scene.
/// </summary>
[InitializeOnLoad]
public static class BootFromMainMenu
{
    const string MenuPath = "RealBuca/Boot From Main Menu (editor ▶)";
    const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    static string PreferenceKey => "RealBuca.BootFromMainMenu." + Application.dataPath;

    static BootFromMainMenu()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += InitializeEditorScene;
    }

    static void InitializeEditorScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InitializeEditorScene;
            return;
        }
        RestoreStartupScene();
        if (!Application.isBatchMode) TryOpenMainMenuIfEmpty();
    }

    // The Play override alone leaves the edit-mode preview showing an empty sky.
    // Recover only the untouched default scene; never replace unsaved user work.
    public static bool TryOpenMainMenuIfEmpty()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorPrefs.GetBool(PreferenceKey, true) || SceneManager.sceneCount != 1) return false;
        var current = SceneManager.GetActiveScene();
        if (!current.IsValid() || !current.isLoaded || current.isDirty || !string.IsNullOrEmpty(current.path)) return false;
        foreach (var root in current.GetRootGameObjects())
        {
            if (root.transform.childCount != 0) return false;
            if (root.name != "Main Camera" && root.name != "Directional Light") return false;
            foreach (var component in root.GetComponents<Component>())
                if (!(component is Transform) && !(component is Camera) && !(component is Light) && !(component is AudioListener)) return false;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScene) == null) return false;
        EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        Debug.Log("BUCA_EDITOR_MAIN_MENU_OPENED: replaced untouched empty startup scene.");
        return true;
    }

    [MenuItem("RealBuca/Open Main Menu Scene")]
    public static void OpenMainMenuScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        SetEnabled(true);
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingEditMode)
            RestoreStartupScene();
    }

    public static void RestoreStartupScene()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorPrefs.GetBool(PreferenceKey, true))
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScene);
        if (scene != null) EditorSceneManager.playModeStartScene = scene;
    }

    public static void SetEnabled(bool enabled)
    {
        EditorPrefs.SetBool(PreferenceKey, enabled);
        RestoreStartupScene();
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        if (EditorPrefs.GetBool(PreferenceKey, true))
        {
            SetEnabled(false);
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
            SetEnabled(true);
            EditorUtility.DisplayDialog("Boot From Main Menu: ON",
                "Now Unity's ▶ ALWAYS starts from the Main Menu, no matter which scene is open.\n\n" +
                "Press ▶ → the menu appears → click PLAY → Level 1.\n\n" +
                "This preference is remembered for this project.", "OK");
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
