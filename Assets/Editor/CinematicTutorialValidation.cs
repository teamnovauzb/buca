#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class CinematicTutorialValidation
{
    [InitializeOnLoadMethod] static void Resume()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CinematicValidate",false))
            {SessionState.SetBool("CinematicValidate",false);Run();}
        };
    }
    static double deadline;
    [MenuItem("RealBuca/Toy Box 3D/Validate Unified Cinematic Tutorials")]
    static void Run()
    {
        if(!EditorApplication.isPlaying){SessionState.SetBool("CinematicValidate",true);EditorApplication.isPlaying=true;return;}
        deadline=EditorApplication.timeSinceStartup+45;
        if(LevelManager.Instance==null)UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        EditorApplication.update-=Check;EditorApplication.update+=Check;
    }
    static void Check()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Check;return;}
        var manager=LevelManager.Instance;
        if(manager==null){if(EditorApplication.timeSinceStartup>deadline){EditorApplication.update-=Check;Debug.LogError("CINEMATIC_VALIDATION: game did not load");}return;}
        EditorApplication.update-=Check;
        try
        {
            var view=UnityEngine.Object.FindAnyObjectByType<WatchCopyTutorial3D>();
            for(int i=0;view.IsShowing && i<30;i++)view.Skip();
            int count=0;
            foreach(var lesson in view.lessons)
            {
                bool finished=false;
                view.Begin(new[]{lesson.key},()=>finished=true);
                var cinematic=UnityEngine.Object.FindObjectsByType<CinematicMechanicTutorial>(FindObjectsSortMode.None);
                CinematicMechanicTutorial active=null;
                foreach(var candidate in cinematic)if(candidate.Mechanic==lesson.key && candidate.gameObject.activeInHierarchy)active=candidate;
                if(active==null || !view.CinematicActive)throw new Exception("No cinematic overlay for "+lesson.key);
                foreach(float t in new[]{0f,2f,3.9f,5f,active.Duration})
                {
                    active.Sample(t);
                    var puck=lesson.root.transform.Find("Puck");
                    if(puck==null || float.IsNaN(puck.position.x))throw new Exception("Invalid puck pose for "+lesson.key);
                }
                var overlay=active.GetComponent<BoardCoachTutorial>();
                foreach(var button in new[]{overlay.play,overlay.replay,overlay.pause})
                    if(button.GetComponentInChildren<ArcadeButtonFace>()==null)
                        throw new Exception("Missing physical arcade button visual");
                active.TogglePause();
                if(overlay.pauseLabel.text!="RESUME")throw new Exception("Pause label did not update");
                if(!active.Paused)throw new Exception("Pause failed");
                active.Replay();if(active.PlaybackTime!=0 || active.Paused)throw new Exception("Replay failed");
                view.Skip();if(!finished || view.IsShowing)throw new Exception("Continue failed "+lesson.key);
                count++;
            }
            var intro=UnityEngine.Object.FindAnyObjectByType<ObstacleIntroController>(FindObjectsInactive.Include);
            int entries=0;
            for(int level=1;level<=manager.levelPrefabs.Length;level++)
            {
                var root=manager.levelPrefabs[level-1];
                var first=intro.SelectLessonsForLevel(root,level);
                var again=intro.SelectLessonsForLevel(root,level);
                if(string.Join(",",first)!=string.Join(",",again))throw new Exception("Repeat entry changed tutorial queue");
                foreach(string key in first)if(Array.FindIndex(view.lessons,x=>x.key==key)<0)throw new Exception("Missing lesson "+key);
                entries++;
            }
            Debug.Log("CINEMATIC_CAMPAIGN_PASSED: "+entries+" levels, repeat entry and mechanic coverage.");
            Debug.Log("CINEMATIC_ALL_PASSED: "+count+" mechanic lessons, sampled timelines, pause, replay and return callbacks.");
            manager.LoadLevel(1);
        }
        catch(Exception error){Debug.LogException(error);}
    }
}
#endif
