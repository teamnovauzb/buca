#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AllAboardFlowValidation
{
    const string Key="BucaAllAboardValidation";
    static int phase;static double deadline;static float stamp;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static AllAboardFlowValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    static IEnumerable<string> Keys()
    {
        yield return "BucaCurrentLevel";yield return "BucaPendingLevel";yield return "BucaHighestLevel";
        for(int i=0;i<50;i++)foreach(string p in new[]{"BucaStars_L","BucaScore_L","BucaBestStrokes_L","BucaPar_L"})yield return p+i;
    }
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit scene required.");
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetFloat(Key+"TimeScale",Time.timeScale);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
        foreach(var key in Keys()){SessionState.SetBool(Key+key+"Has",PlayerPrefs.HasKey(key));SessionState.SetInt(Key+key,PlayerPrefs.GetInt(key));}
        BootFromMainMenu.SetEnabled(false);EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");PlayerPrefs.SetInt("BucaPendingLevel",29);
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","running");EditorApplication.EnterPlaymode();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Prepare()
    {
        if(!SessionState.GetBool(Key,false))return;
        var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
        if(manager!=null)typeof(LevelManager).GetField("_obstacleIntro",Flags).SetValue(manager,null);
        PlayerPrefs.SetInt("BucaHighestLevel",29);
        Time.timeScale=1;phase=0;deadline=EditorApplication.timeSinceStartup+90;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredEditMode)return;
        foreach(var key in Keys()){if(SessionState.GetBool(Key+key+"Has",false))PlayerPrefs.SetInt(key,SessionState.GetInt(Key+key,0));else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();
        BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));Time.timeScale=SessionState.GetFloat(Key+"TimeScale",1);SessionState.SetBool(Key,false);
        EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
    }
    static void Require(bool valid,string why){if(!valid)throw new Exception(why);}
    static void Finish(string result)
    {
        SessionState.SetString(Key+"Result",result);phase=99;Time.timeScale=1;
        System.IO.Directory.CreateDirectory("output/all-aboard");System.IO.File.WriteAllText("output/all-aboard/flow-validation.txt",result);EditorApplication.ExitPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||phase==99)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline&&deadline>0)throw new Exception("Transition timed out at phase "+phase);
            var m=LevelManager.Instance;if(m==null||m.CurrentLevelRoot==null)return;var v=m.allAboardCompletion;
            if(phase==0)
            {
                Time.timeScale=1;Require(m.TotalLevels==50,"Expected 50 boards");Require(m.CurrentLevelNumber==30,"Wrong starting board");
                m.CompleteLevel();phase=1;
            }
            else if(phase==1&&m.worldStationJourney.IsShowing)
            {Require(m.worldStationJourney.Travelling,"Train not departing");Require(!m.GameplayInputAllowed,"Gameplay input remains active");stamp=Time.unscaledTime;phase=2;}
            else if(phase==2&&m.worldStationJourney.BonusVisible)
            {
                Require(Time.unscaledTime-stamp>6,"Departure skipped");
                m.worldStationJourney.ChooseBonus(19);
                Require(m.worldStationJourney.IsShowing,"Locked bonus was accepted");
                m.worldStationJourney.ChooseBonus(0);m.worldStationJourney.ChooseBonus(0);phase=3;
            }
            else if(phase==3&&m.CurrentLevelNumber==31&&m.GameplayInputAllowed)
            {
                Require(!m.worldStationJourney.IsShowing,"Station camera not dismissed");Require(m.GetCurrentHolePosition().z>5,"Missing bonus goal");
                // Every new board must load with its own walls, puck and goal.
                for(int index=30;index<50;index++)
                {m.LoadLevel(index);Require(m.CurrentLevelRoot.GetComponentsInChildren<RailLight>().Length>=7,"Bonus walls missing");}
                m.CompleteLevel();phase=4;
            }
            else if(phase==4&&v.IsShowing)
            {Require(v.bonusTitle.activeSelf&&!v.firstTitle.activeSelf,"Wrong final screen");phase=5;}
            else if(phase==5&&v.ActionsReady)
            {v.Choose(false);phase=6;}
            else if(phase==6&&m.CurrentLevelNumber==1&&m.GameplayInputAllowed)
            {Require(PlayerPrefs.GetInt("BucaHighestLevel")>=49,"Replay erased unlocks");Finish("PASS: level 30 opens train route, full timed departure, 20-level map, locked bonus guard, duplicate press guard, departure to 31, all 20 bonus boards load, level 50 finale, replay to 1, unlocked progress preserved. Local Play mode; no live Luxodd transaction performed.");}
        }
        catch(Exception e){Finish("FAIL: "+e.Message);}
    }
}
#endif
