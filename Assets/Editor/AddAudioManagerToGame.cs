#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// ONE-SHOT: ensure the Game scene has its own AudioManager so sound plays even
/// when you press Play directly in Game.unity (instead of launching from MainMenu).
///
/// Safe with the existing MainMenu AudioManager: AudioManager is a DontDestroyOnLoad
/// singleton, so when you DO launch from MainMenu, that persistent instance wins and
/// this Game-scene copy destroys itself in Awake. Either entry point → audio works.
///
///   RealBuca ▸ Add AudioManager to Game Scene
///
/// It also AUTO-WIRES every clip slot from Assets/Audio/. AFTER running you can
/// DELETE THIS FILE (keep AudioManager.cs).
/// </summary>
public static class AddAudioManagerToGame
{
    const string GameScene = "Assets/Scenes/Game.unity";

    [MenuItem("RealBuca/Add AudioManager to Game Scene")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        AudioManager am = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            am = root.GetComponentInChildren<AudioManager>(true);
            if (am != null) break;
        }

        bool created = false;
        if (am == null)
        {
            var go = new GameObject("AudioManager");
            am = go.AddComponent<AudioManager>();
            created = true;
        }

        // Wire (or top up) every clip slot from the Assets/Audio/ folder structure.
        am.AutoWireFromAssetsFolder();

        EditorUtility.SetDirty(am);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("AudioManager ready ✓",
            (created ? "Added an AudioManager to the Game scene and auto-wired every clip"
                     : "Game scene already had an AudioManager — re-wired every clip")
            + " from Assets/Audio/.\n\n" +
            "Press ▶ Play directly in Game.unity and you'll now hear music + SFX.\n" +
            "(Launching from MainMenu still works — its persistent AudioManager takes over.)\n\n" +
            "You can DELETE this tool file (keep AudioManager.cs).",
            "OK");
    }
}
#endif
