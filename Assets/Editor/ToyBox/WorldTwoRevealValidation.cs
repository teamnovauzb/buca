#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WorldTwoRevealValidation
{
    const string Key="WorldTwoRevealTest";
    static readonly string[] Prefs={"BucaCurrentLevel","BucaPendingLevel","BucaHighestLevel","BucaStars_L29","BucaScore_L29","BucaBestStrokes_L29","BucaPar_L29",TutorialController.PrefSeen};
    static int phase;
    static double start;
    static bool sawReveal;
    static float lastProgress;
    static double waitingAt;
    static WorldTwoRevealValidation(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        phase=0;start=0;sawReveal=false;lastProgress=0;waitingAt=0;
        foreach(var pref in Prefs){SessionState.SetBool(Key+pref+"Exists",PlayerPrefs.HasKey(pref));SessionState.SetInt(Key+pref,PlayerPrefs.GetInt(pref));}
        SessionState.SetBool(Key+"Background",Application.runInBackground);
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","Running");
        Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(start==0)start=EditorApplication.timeSinceStartup;
        try
        {
            if(EditorApplication.timeSinceStartup-start>60)throw new Exception("Completion reveal timed out.");
            if(phase==0)
            {
                if(!GameScenePreloader.Ready)return;
                var map=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
                map.gameObject.SetActive(true);map.Open(null);map.Select(29);map.RequestPlay();phase=1;return;
            }
            var manager=LevelManager.Instance;if(manager==null||manager.CurrentLevelRoot==null)return;
            if(phase==1)
            {
                if(manager.CurrentLevelNumber!=30)throw new Exception("Did not open level 30.");
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                if((bool)typeof(LevelManager).GetField("_obstacleIntroActive",flags).GetValue(manager))return;
                manager.luxoddBridge=null; // Test scores must never be sent to the platform.
                manager.CompleteLevel();phase=2;
            }
            var reveal=UnityEngine.Object.FindAnyObjectByType<WorldTwoHarborReveal>();
            if(reveal!=null)
            {
                sawReveal=true;
                float progress=reveal.artwork.material.GetFloat("_WorldReveal");
                if(progress<lastProgress-.001f)throw new Exception("Reveal moved backwards.");
                lastProgress=progress;
                if(reveal.WaitingForPlay)
                {
                    if(waitingAt==0)
                    {
                        waitingAt=EditorApplication.timeSinceStartup;
                        System.IO.Directory.CreateDirectory("output/world-two-reveal");
                        ScreenCapture.CaptureScreenshot("output/world-two-reveal/FullMap-Ready.png");
                        if((int)typeof(WorldTwoHarborReveal).GetField("nextChime",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reveal)!=7)
                            throw new Exception("Not all seven chimes played.");
                    }
                    if(reveal.duration<10||reveal.artwork.uvRect!=new Rect(0,0,1,1)||reveal.revealChime==null)
                        throw new Exception("Slow full-map chime setup missing.");
                    if(EditorApplication.timeSinceStartup-waitingAt>2)
                        reveal.continueButton.onClick.Invoke();
                }
                if(manager.CurrentLevelNumber!=30)throw new Exception("Gameplay advanced before reveal ended.");
            }
            if(phase==2&&manager.CurrentLevelNumber==31)
            {
                if(!sawReveal||lastProgress<.99f)throw new Exception("Skipped complete harbor reveal.");
                if(waitingAt==0)throw new Exception("Level 31 auto-started without Play.");
                Finish("PASS: slow full-map reveal with chime asset; stays on map after completion; Play 31 opens level 31 in the same scene.");
            }
        }
        catch(Exception ex){Finish("FAIL: "+ex);}
    }
    static void Finish(string result)
    {
        SessionState.SetBool(Key,false);SessionState.SetString(Key+"Result",result);
        Time.timeScale=0;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;
    }
    public static string Restore()
    {
        if(EditorApplication.isPlaying)return SessionState.GetString(Key+"Result","Running");
        foreach(var pref in Prefs){if(SessionState.GetBool(Key+pref+"Exists",false))PlayerPrefs.SetInt(pref,SessionState.GetInt(Key+pref,0));else PlayerPrefs.DeleteKey(pref);}
        PlayerPrefs.Save();Application.runInBackground=SessionState.GetBool(Key+"Background",false);Time.timeScale=1;
        return SessionState.GetString(Key+"Result","Not run");
    }
}
#endif
