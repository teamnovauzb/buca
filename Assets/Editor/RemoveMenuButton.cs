#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// ONE-SHOT: remove the in-game "‹ MENU" button from the Game scene. (The hearts /
/// lives counter and everything else stay.) Exit-to-menu is still available on the
/// Level Failed screen's EXIT button.
///
///   RealBuca ▸ Remove In-Game Menu Button
///
/// AFTER running you can DELETE THIS FILE.
/// </summary>
public static class RemoveMenuButton
{
    const string GameScene = "Assets/Scenes/Game.unity";

    [MenuItem("RealBuca/Remove In-Game Menu Button")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        int removed = 0;
        // The button lives on its own "MenuButtonCanvas"; also catch any stray
        // object literally named "MenuButton".
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "MenuButtonCanvas" || root.name == "MenuButton")
            {
                Object.DestroyImmediate(root);
                removed++;
            }
        }
        // Fallback: a MenuButton nested inside another canvas.
        if (removed == 0)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var t = FindRec(root.transform, "MenuButton");
                if (t != null) { Object.DestroyImmediate(t.gameObject); removed++; break; }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(removed > 0 ? "Menu button removed ✓" : "No menu button found",
            removed > 0
                ? "The in-game MENU button is gone. Press ▶ Play to confirm.\nYou can DELETE this file.\n\n" +
                  "(Exit-to-menu is still on the Level Failed screen's EXIT button.)"
                : "Couldn't find a 'MenuButtonCanvas' / 'MenuButton' in the Game scene — it may already be removed.",
            "OK");
    }

    static Transform FindRec(Transform p, string n)
    {
        if (p.name == n) return p;
        foreach (Transform c in p) { var r = FindRec(c, n); if (r != null) return r; }
        return null;
    }
}
#endif
