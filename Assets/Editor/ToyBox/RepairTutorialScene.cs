#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    // Replace the presentation only: older scene overrides reference obsolete
    // instruction meshes, leaving the prefab's new labels uncontrolled.
    public static void RepairTutorialScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Stop Play mode first.");
        const string scenePath = "Assets/Scenes/Game.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Prefabs/WatchThenTryTutorial.prefab");
            foreach (var root in scene.GetRootGameObjects())
            {
                var old = root.GetComponent<WatchCopyTutorial3D>();
                if (old == null) continue;
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                replacement.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
                replacement.transform.localScale = root.transform.localScale;
                var view = replacement.GetComponent<WatchCopyTutorial3D>();
                view.gameplayCamera = old.gameplayCamera;
                view.gameplayHud = old.gameplayHud;
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);
                foreach (var sceneRoot in scene.GetRootGameObjects())
                    foreach (var intro in sceneRoot.GetComponentsInChildren<ObstacleIntroController>(true))
                        if (intro.watchCopy == old)
                        {
                            intro.watchCopy = view;
                            EditorUtility.SetDirty(intro);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(intro);
                        }
                Object.DestroyImmediate(root);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
#endif
