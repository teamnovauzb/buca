using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Loads and activates gameplay behind the menu, keeping its roots asleep until Play.</summary>
public static class GameScenePreloader
{
    static AsyncOperation pending;
    static string pendingScene;
    static Scene preparedScene;
    static readonly List<GameObject> activeRoots = new List<GameObject>();
    static bool openRequested;

    public static bool IsPreparing(Scene scene) => scene.name == pendingScene &&
        SceneManager.GetActiveScene() != scene;
    public static bool Ready => preparedScene.IsValid() && preparedScene.isLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        SceneManager.sceneLoaded -= OnPrepared;
        pending = null;
        pendingScene = null;
        preparedScene = default;
        activeRoots.Clear();
        openRequested = false;
    }

    public static void Prepare(string scene)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(scene)) return;
        if (SceneManager.GetActiveScene().name == scene) return;
        if (Ready || (pending != null && !pending.isDone)) return;
        if (!Application.CanStreamedLevelBeLoaded(scene)) return;
        pendingScene = scene;
        openRequested = false;
        SceneManager.sceneLoaded -= OnPrepared;
        SceneManager.sceneLoaded += OnPrepared;
        pending = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
    }

    static void OnPrepared(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != pendingScene || mode != LoadSceneMode.Additive) return;
        SceneManager.sceneLoaded -= OnPrepared;
        preparedScene = scene;
        activeRoots.Clear();
        // sceneLoaded runs before Start: no countdown, physics, input or game session
        // starts while the player is still choosing a ticket in the menu.
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.activeSelf) continue;
            activeRoots.Add(root);
            root.SetActive(false);
        }
        if (openRequested) Reveal();
    }

    public static void Open(string scene)
    {
        Prepare(scene);
        if (pendingScene != scene)
        {
            SceneManager.LoadScene(scene);
            return;
        }
        openRequested = true;
        if (Ready) Reveal();
        else if (pending == null) SceneManager.LoadScene(scene);
    }

    static void Reveal()
    {
        var menu = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(preparedScene);
        // Hide menu cameras/input first; unloading its assets is not on the Play path.
        foreach (var root in menu.GetRootGameObjects()) root.SetActive(false);
        foreach (var root in activeRoots) if (root != null) root.SetActive(true);
        if (LuxoddGameBridge.Instance != null)
            LuxoddGameBridge.Instance.ActivatePreparedScene(preparedScene);
        if (AudioManager.Instance != null)
            AudioManager.Instance.ActivatePreparedScene(preparedScene);
        activeRoots.Clear();
        pendingScene = null;
        preparedScene = default;
        pending = null;
        openRequested = false;
        SceneManager.UnloadSceneAsync(menu);
    }
}
