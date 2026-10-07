#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class TutorialLiveCheck
{
    [InitializeOnLoadMethod] static void ResumeTest()
    {
        EditorApplication.playModeStateChanged += state => {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BoardCoachRunTest",false))
            {SessionState.SetBool("BoardCoachRunTest",false);Run();}
        };
    }
    static int phase;
    static double deadline, sampleAt;
    static float timer;
    [MenuItem("RealBuca/Toy Box 3D/Test Level 7 Tutorial In Play Mode")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
            { SessionState.SetBool("BoardCoachRunTest",true); EditorApplication.isPlaying=true; return; }
        phase=-1; deadline=EditorApplication.timeSinceStartup+45;
        if (LevelManager.Instance == null) UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }
    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update-=Tick; return; }
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Live tutorial test timed out.");
            var manager=LevelManager.Instance;
            var picture=BoardCoachTutorial.IsActive ? UnityEngine.Object.FindAnyObjectByType<BoardCoachTutorial>() : null;
            var view=UnityEngine.Object.FindAnyObjectByType<WatchCopyTutorial3D>();
            if (manager == null) return;
            if (phase == -1)
            {
                if (manager.CurrentLevelNumber == 7) { phase=0; return; }
                if(picture!=null){picture.Continue();return;}
                if(view!=null && view.IsShowing){view.Skip();return;}
                manager.LoadLevel(6);phase=0;return;
            }
            if(phase==0)
            {
                if(picture!=null)
                {
                    if(manager.GameplayInputAllowed)throw new Exception("Gameplay active during honey tutorial.");
                    timer=manager.CurrentTimeRemaining;
                    sampleAt=EditorApplication.timeSinceStartup+5;phase=1;
                }
                else if(view!=null && view.IsShowing)view.Skip();
            }
            else if(phase==1 && EditorApplication.timeSinceStartup>=sampleAt)
            {
                if(picture==null || !picture.isActiveAndEnabled)throw new Exception("Board coach is not visible.");
                if(Mathf.Abs(timer-manager.CurrentTimeRemaining)>.01f)throw new Exception("Timer ran during tutorial.");
                if(picture.PlaybackTime<3.5f)throw new Exception("Puck animation did not advance.");
                picture.Continue();phase=2;
            }
            else if(phase==2)
            {
                if(picture!=null)
                {
                    if(manager.GameplayInputAllowed)throw new Exception("Input unlocked before handoff ended");
                    if(Mathf.Abs(timer-manager.CurrentTimeRemaining)>.01f)throw new Exception("Timer ran during handoff");
                    picture.Continue();return;
                }
                if(view!=null && view.IsShowing){view.Skip();return;}
                if(!manager.GameplayInputAllowed)return;
                var hud=UnityEngine.Object.FindAnyObjectByType<ToyBoxGameplayHud>(FindObjectsInactive.Include);
                if(hud==null || !hud.gameObject.activeInHierarchy || hud.hints==null || !hud.hints.activeInHierarchy)return;
                Debug.Log("TUTORIAL_LIVE_PASSED: level 7 honey displayed, animation moved, timer paused, queue completed, gameplay and joystick controls restored.");
                EditorApplication.update-=Tick;
            }
        }
        catch(Exception e){EditorApplication.update-=Tick;Debug.LogException(e);}
    }
}
#endif
