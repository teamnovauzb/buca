#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ValidateTimeUpPersistence
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var resultPanels = UnityEngine.Object.FindObjectsByType<LeaderboardPanel>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var choicePanels = UnityEngine.Object.FindObjectsByType<LevelFailedPanel>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (resultPanels.Length == 0 || choicePanels.Length == 0)
            throw new Exception("Game scene needs both a result panel and a Continue/End panel.");

        var classify = typeof(LevelFailedPanel).GetMethod("IsTimeUpReason",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (classify == null)
            throw new Exception("Time-up choice policy missing.");
        if (!(bool)classify.Invoke(null, new object[] { "TIME'S UP!" })
            || !(bool)classify.Invoke(null, new object[] { "TIME IS OVER" })
            || (bool)classify.Invoke(null, new object[] { "YOU LOST" }))
            throw new Exception("Time-up choice policy classified an outcome incorrectly.");

        if (resultPanels[0].continueHintText == null)
            throw new Exception("The result screen has no visible press-button hint.");
        Debug.Log("BUCA_TIME_UP_PERSISTENCE_PASS result and choice panels wired; " +
                  "time-up outcome requires an explicit choice.");
    }
}
#endif
