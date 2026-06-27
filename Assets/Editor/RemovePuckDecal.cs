#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ONE-SHOT: remove the little orange "spin dot" (the SpinDecal sphere) stuck on top
/// of the puck. It only existed to show the BALL rolling; it just looks like a wart
/// now. This deletes the SpinDecal child + its driver component for a clean puck.
///
///   RealBuca ▸ Puck → Remove Spin Dot (clean look)
///
/// Touches only that decal — the puck's mesh, material, trail, glow, aim line, and
/// physics are untouched. AFTER running you can DELETE THIS FILE.
/// </summary>
public static class RemovePuckDecal
{
    const string GameScene = "Assets/Scenes/Game.unity";

    [MenuItem("RealBuca/Puck → Remove Spin Dot (clean look)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        GameObject puck = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var pc = root.GetComponentInChildren<PuckController>(true);
            if (pc != null) { puck = pc.gameObject; break; }
        }
        if (puck == null)
        {
            EditorUtility.DisplayDialog("Puck not found",
                "Couldn't find the PuckController in the Game scene. Nothing changed.", "OK");
            return;
        }

        int removed = 0;

        // The little orange sphere child.
        var decal = FindChild(puck.transform, "SpinDecal");
        if (decal != null) { Object.DestroyImmediate(decal.gameObject); removed++; }

        // The component that drove it (so nothing tries to use the deleted decal).
        var spin = puck.GetComponent<PuckSpinDecal>();
        if (spin != null) { Object.DestroyImmediate(spin); removed++; }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog(removed > 0 ? "Spin dot removed ✓" : "Nothing to remove",
            removed > 0
                ? "The little orange sphere on top of the puck is gone — clean puck now.\n\n" +
                  "• Press ▶ Play to see it.\n• You can DELETE this file.\n\n" +
                  "Everything else about the puck is unchanged."
                : "No 'SpinDecal' found on the puck — it may already be removed.",
            "OK");
    }

    static Transform FindChild(Transform p, string n)
    {
        foreach (Transform c in p)
        {
            if (c.name == n) return c;
            var r = FindChild(c, n);
            if (r != null) return r;
        }
        return null;
    }
}
#endif
