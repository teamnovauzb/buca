#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ONE-SHOT: make the LEVEL SELECT screen respect the safe area on every
/// resolution / aspect ratio. Attaches a <see cref="SafeAreaFitter"/> to the
/// panel's Card so the whole card is uniform-scaled to fit the visible/safe
/// viewport and re-centered between the device insets — the top can no longer
/// be clipped by the screen border, and nothing overflows the edges.
///
///   RealBuca ▸ Fix Level Select Safe Area
///
/// Safe to run repeatedly. Edits the baked object in place (no rebuild).
/// </summary>
public static class FixLevelSelectSafeArea
{
    const string MenuScene = "Assets/Scenes/MainMenu.unity";

    [MenuItem("RealBuca/Fix Level Select Safe Area")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the MainMenu scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);

        LevelSelectController ctrl = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            ctrl = root.GetComponentInChildren<LevelSelectController>(true);
            if (ctrl != null) break;
        }
        if (ctrl == null)
        {
            EditorUtility.DisplayDialog("Level select not found",
                "Couldn't find a LevelSelectController in the MainMenu scene. Nothing changed.\n\n" +
                "Run 'Spawn Level Select Panel' from MenuSetupHelper first.", "OK");
            return;
        }

        var root2 = (RectTransform)ctrl.transform;                // LevelSelectPanel (stretch-stretch)
        RectTransform card = ctrl.panelCard != null ? ctrl.panelCard : FindChild(root2, "Card") as RectTransform;
        if (card == null)
        {
            EditorUtility.DisplayDialog("Card not found",
                "The LevelSelectController has no 'Card' to fit. Nothing changed.", "OK");
            return;
        }

        Vector2 ds = card.sizeDelta;
        if (ds.x <= 0f || ds.y <= 0f) ds = card.rect.size;

        var fitter = card.GetComponent<SafeAreaFitter>();
        if (fitter == null) fitter = card.gameObject.AddComponent<SafeAreaFitter>();
        fitter.target = card;
        fitter.area = root2;
        fitter.designSize = ds;
        fitter.animScale = 1f;
        ctrl.cardFitter = fitter;   // so the open/close pop composes with the fit

        // Reset any stale transform the fitter will now own, so the scene
        // preview matches its runtime, unclipped state.
        card.localScale = Vector3.one;
        card.anchoredPosition = Vector2.zero;

        EditorUtility.SetDirty(fitter);
        EditorUtility.SetDirty(ctrl);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Level select safe-area fixed ✓",
            $"Attached SafeAreaFitter to the level-select Card (design size {ds.x:0}×{ds.y:0}).\n\n" +
            "At runtime the card now:\n" +
            "• scales uniformly to fit the visible/safe viewport\n" +
            "• re-centers between the device safe-area insets\n" +
            "• never clips the title / pill / back button off any edge\n\n" +
            "Press ▶ Play → LEVELS to check. Rebuild WebGL for the cabinet.",
            "OK");
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t != parent && t.name == name) return t;
        return null;
    }
}
#endif
