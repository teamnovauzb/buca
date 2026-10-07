using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class HeartRewardValidation
{
    const string Key="BucaHeartRewardValidation";
    static double started;
    static int phase;
    static LevelManager manager;
    static ScorePickup star;
    static ToyBoxGameplayHud hud;
    static HeartRewardValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    static void Set(string name,object value)=>typeof(LevelManager).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,value);
    public static void Run()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new Exception("Validation needs clean edit scene");
        phase=0;
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
        BootFromMainMenu.SetEnabled(false);
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)) component.enabled=false;
        var fx=UnityEngine.Object.FindAnyObjectByType<HeartRewardPresentation>(FindObjectsInactive.Include);fx.enabled=true;
        SessionState.SetBool(Key,true); SessionState.SetString(Key+"Result","running");
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false))
        {
            SessionState.SetBool(Key,false);BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));
            EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
        }
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try
        {
            Time.timeScale=1;
            if(phase==0)
            {
                manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                hud=UnityEngine.Object.FindAnyObjectByType<ToyBoxGameplayHud>();
                Check(manager!=null && manager.heartRewardPresentation!=null,"Missing saved manager / reward connection");
                var root=UnityEngine.Object.Instantiate(manager.levelPrefabs[20]);Set("_currentInstance",root);
                Set("_lives",5);Set("_currentMaxLives",5);Set("_timerActive",true);Set("_timeRemaining",60f);
                Set("_isTransitioning",false);Set("_obstacleIntroActive",false);Set("_levelFailed",false);
                manager.useShotLives=true;manager.puckRigidbody.isKinematic=false;manager.puckRigidbody.useGravity=false;
                manager.CaptureUndoShot();
                star=root.GetComponentsInChildren<ScorePickup>().First(p=>p.grantsHeart);
                var collider=manager.puck.GetComponent<Collider>();
                star.SendMessage("OnTriggerEnter",collider);star.SendMessage("OnTriggerEnter",collider);
                Check(manager.CurrentLives==6 && manager.CurrentMaxLives==6,"Full health reward / duplicate trigger failed");
                Check(star.Collected,"Star not collected");
                hud.RefreshHearts(manager.CurrentLives,manager.CurrentMaxLives);
                Check(hud.heartFills.Count(f=>f.activeSelf)==6,"Sixth heart not visible");
                Check(manager.heartRewardPresentation.cards.Count(c=>c.active)==1,"Reward card missing / duplicated");
                foreach(var remaining in root.GetComponentsInChildren<ScorePickup>().Where(p=>p!=star && p.grantsHeart))remaining.SendMessage("OnTriggerEnter",collider);
                Check(manager.CurrentLives==7,"Two rapid stars should grant two hearts");
                hud.RefreshHearts(8,8);Check(hud.heartFills.Count(f=>f.activeSelf)==8,"Eighth heart not visible");
                started=EditorApplication.timeSinceStartup;phase=1;
            }
            else if(phase==1 && EditorApplication.timeSinceStartup-started>.5)
            {
                Check(manager.heartRewardPresentation.cards.Any(c=>c.active && c.rect.localScale.x>.5f),"Animation did not appear");
                manager.puckRigidbody.linearVelocity=Vector3.zero;Set("_shotInProgress",false);
                Check(manager.CanUndoShot,"Undo unavailable");manager.UndoLastShot();
                started=EditorApplication.timeSinceStartup;phase=2;
            }
            else if(phase==2 && EditorApplication.timeSinceStartup-started>2)
            {
                Check(!manager.IsRewindingShot,"Rewind did not complete");
                Check(manager.CurrentLives==5 && manager.CurrentMaxLives==5,"Undo did not restore heart allowance");
                Check(!star.Collected && star.gameObject.activeSelf && star.GetComponent<Collider>().enabled,"Undo did not restore star");
                Check(!manager.heartRewardPresentation.cards.Any(c=>c.active),"Undo did not clear reward animation");
                Set("_lives",2); manager.AddStarHeart(Vector3.zero);Check(manager.CurrentLives==3 && manager.CurrentMaxLives==5,"Missing-heart recovery failed");
                SessionState.SetString(Key+"Result","PASS: full-health +1, duplicate guard, rapid 2-star pickup, 8 visible hearts, animated cards, Undo restoration, missing-heart recovery");
                EditorApplication.ExitPlaymode();phase=3;
            }
        }
        catch(Exception e){SessionState.SetString(Key+"Result","FAIL: "+e);Debug.LogException(e);EditorApplication.ExitPlaymode();phase=3;}
    }
}
