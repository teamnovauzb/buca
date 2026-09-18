#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildBucaQuickPreview
{
    public static void EnterGame()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        Debug.Log("BUCA_ENTERING_PLAY_MODE");
        EditorApplication.isPlaying = true;
    }

    public static void BuildBatch()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes were found in Build Settings.");

        var output = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "Builds", "Buca-Quick-Preview-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(output);

        var previousOptimization = UserBuildSettings.codeOptimization;
        try
        {
            UserBuildSettings.codeOptimization = WasmCodeOptimization.BuildTimes;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Quick preview failed: " + report.summary.result);
            Debug.Log("BUCA_QUICK_PREVIEW=" + output);
        }
        finally
        {
            UserBuildSettings.codeOptimization = previousOptimization;
        }
    }
}
#endif
