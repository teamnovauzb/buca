#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the responsive Level Select layout used by the verified Web build.
/// Run from RealBuca > Apply Responsive Level Screen while not in Play mode.
/// </summary>
public static class ApplyResponsiveLevelScreen
{
    const string MenuScene = "Assets/Scenes/MainMenu.unity";

    [MenuItem("RealBuca/Apply Responsive Level Screen")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "Stop Play mode, then run this command again.", "OK");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(MenuScene);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        }

        LevelSelectController controller = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            controller = root.GetComponentInChildren<LevelSelectController>(true);
            if (controller != null) break;
        }

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Level Select not found",
                "MainMenu.unity has no LevelSelectController.", "OK");
            return;
        }

        RectTransform panel = controller.transform as RectTransform;
        RectTransform dim = FindRect(panel, "Dim");
        RectTransform card = controller.panelCard != null
            ? controller.panelCard
            : FindRect(panel, "Card");

        if (panel == null || dim == null || card == null)
        {
            EditorUtility.DisplayDialog("Level Select hierarchy incomplete",
                "Expected LevelSelectPanel/Dim/Card objects were not found.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Apply Responsive Level Screen");
        int undoGroup = Undo.GetCurrentGroup();

        Record(dim);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.pivot = new Vector2(0.5f, 0.5f);
        dim.offsetMin = Vector2.zero;
        dim.offsetMax = Vector2.zero;

        Record(card);
        SetWidth(card, 1400f);
        card.anchoredPosition = Vector2.zero;
        card.localScale = Vector3.one;

        SafeAreaFitter fitter = card.GetComponent<SafeAreaFitter>();
        if (fitter == null) fitter = Undo.AddComponent<SafeAreaFitter>(card.gameObject);
        Record(fitter);
        fitter.target = card;
        fitter.area = panel;
        fitter.designSize = new Vector2(1400f, 1520f);
        fitter.animScale = 1f;
        Record(controller);
        controller.cardFitter = fitter;
        controller.columnsPerRow = 5;

        SetWidth(RequireRect(card, "Title"), 1000f);
        SetWidth(RequireRect(card, "ProgressTrack"), 900f);

        RectTransform grid = RequireRect(card, "Grid");
        SetWidth(grid, 1250f);
        for (int level = 1; level <= 30; level++)
        {
            RectTransform tile = RequireRect(grid, $"Level_{level}");
            Record(tile);
            SetWidth(tile, 220f);
            Vector2 position = tile.anchoredPosition;
            position.x = (((level - 1) % 5) - 2) * 250f;
            tile.anchoredPosition = position;
        }

        RectTransform footer = RequireRect(card, "FooterStats");
        SetWidth(footer, 1250f);
        SetX(RequireRect(footer, "Completed"), -430f);
        SetX(RequireRect(footer, "StarsTotal"), 0f);
        SetX(RequireRect(footer, "Score"), 430f);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneView.RepaintAll();

        EditorUtility.DisplayDialog("Responsive Level Select applied",
            "The full-screen dim layer and widened responsive card are saved.\n\n" +
            "Press Play, then click LEVELS to preview it.", "OK");
    }

    static RectTransform RequireRect(Transform parent, string name)
    {
        RectTransform result = FindRect(parent, name);
        if (result == null) throw new InvalidOperationException($"Missing UI object: {name}");
        return result;
    }

    static RectTransform FindRect(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (RectTransform rect in parent.GetComponentsInChildren<RectTransform>(true))
            if (rect != parent && rect.name == name) return rect;
        return null;
    }

    static void SetWidth(RectTransform rect, float width)
    {
        Record(rect);
        Vector2 size = rect.sizeDelta;
        size.x = width;
        rect.sizeDelta = size;
    }

    static void SetX(RectTransform rect, float x)
    {
        Record(rect);
        Vector2 position = rect.anchoredPosition;
        position.x = x;
        rect.anchoredPosition = position;
    }

    static void Record(UnityEngine.Object target)
    {
        Undo.RecordObject(target, "Apply Responsive Level Screen");
        EditorUtility.SetDirty(target);
    }
}
#endif
