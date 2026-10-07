using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class ArcadeResultValidation
{
    const string Key="BucaArcadeResultValidation";
    static int phase, callbacks;
    static float began;
    static LeaderboardPanel panel;
    static BucaArcadeResult3D view;
    static ArcadeResultValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
        BootFromMainMenu.SetEnabled(false);EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))component.enabled=false;
        UnityEngine.Object.FindAnyObjectByType<LeaderboardPanel>(FindObjectsInactive.Include).enabled=true;
        UnityEngine.Object.FindAnyObjectByType<BucaArcadeResult3D>(FindObjectsInactive.Include).enabled=true;
        UnityEngine.Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include).enabled=true;
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))camera.enabled=false;
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","running");phase=0;callbacks=0;EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/Game.unity"));
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try
        {
            Time.timeScale=0; // UI must work through a frozen failure screen.
            if(phase==0)
            {
                panel=UnityEngine.Object.FindAnyObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);view=panel.GetComponent<BucaArcadeResult3D>();
                Check(view.finalChime!=null && AudioManager.Instance.scoreCountTickSfx!=null,"Missing audio assets");
                panel.Show(null,0,932,"YOU","OUT OF HEARTS",()=>callbacks++);panel.ChooseRetry();
                began=Time.unscaledTime;phase=1;
            }
            else if(phase==1 && Time.unscaledTime-began>1.3f)
            {
                Check(callbacks==0,"Early input skipped count");Check(view.stage.activeSelf,"Cabinet invisible");
                Check(view.DisplayedScore>0 && view.DisplayedScore<932,"Count not animated: displayed="+view.DisplayedScore+" target="+panel.PlayerScore+" elapsed="+(Time.unscaledTime-began));
                Check(Vector3.Distance(view.cabinet.localPosition,new Vector3(-4.7f,0,0))<.01f,"Entrance did not settle");
                panel.RefreshEntries(panel.PresentationId,null,0,932,"YOU");phase=2;
            }
            else if(phase==2 && Time.unscaledTime-began>4.3f)
            {
                Check(view.ShowingLeaderboard && view.cabinet.gameObject.activeSelf,"Leaderboard phase missing");
                Check(callbacks==0,"Transaction called before leaderboard duration");
                Check(panel.card.Find("RetryHit")==null && panel.card.Find("LevelsHit")==null,"Buttons still present");
                phase=3;
            }
            else if(phase==3 && Time.unscaledTime-began>8.3f)
            {
                Check(callbacks==1,"Handoff not called exactly once");
                panel.Show(null,0,932,"YOU","TIME UP",()=>callbacks++);phase=4;began=Time.unscaledTime;
            }
            else if(phase==4 && Time.unscaledTime-began>1.3f)
            {
                Check(view.timeHeading.activeSelf&&!view.heartsHeading.activeSelf,"Time-up reason missing");
                Check(view.ShowingLeaderboard,"Repeat started in wrong phase");phase=5;
            }
            else if(phase==5 && Time.unscaledTime-began>4.3f)
            {
                Check(view.ShowingLeaderboard&&callbacks==1,"Time-up leaderboard phase incorrect");phase=6;
            }
            else if(phase==6 && Time.unscaledTime-began>8.3f)
            {
                Check(callbacks==2,"Time-up handoff incorrect");
                SessionState.SetString(Key+"Result","PASS: both death reasons, 8-second combined result and leaderboard, buttons removed, one handoff each, frozen gameplay time.");
                Time.timeScale=1;EditorApplication.ExitPlaymode();phase=7;
            }
        }
        catch(Exception e){SessionState.SetString(Key+"Result","FAIL: "+e);Time.timeScale=1;EditorApplication.ExitPlaymode();phase=7;}
    }
}
