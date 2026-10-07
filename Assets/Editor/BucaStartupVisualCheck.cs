#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
[InitializeOnLoad]
public static class BucaStartupVisualCheck
{
    static BucaStartupVisualCheck(){EditorApplication.update+=Check;}
    public static void Start()
    {
        SessionState.SetBool("Buca.StartupVisualCheck",true);
        SessionState.SetString("Buca.StartupOldScene",AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Game.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Check()
    {
        if(!SessionState.GetBool("Buca.StartupVisualCheck",false)||!EditorApplication.isPlaying)return;
        var m=Object.FindFirstObjectByType<LevelManager>();if(m==null)return;
        var level=typeof(LevelManager).GetField("_currentInstance",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m) as GameObject;
        if(level==null)return;
        SessionState.SetBool("Buca.StartupVisualCheck",false);
        System.IO.File.WriteAllText("output/leaderboard/startup-check.txt","LevelManager enabled="+m.enabled+"; loaded="+level.name+"; visible renderers="+level.GetComponentsInChildren<Renderer>().Length);
        ScreenCapture.CaptureScreenshot("output/leaderboard/Game-Restored.png");
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("Buca.StartupOldScene",""));
    }
}
#endif
