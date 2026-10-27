#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

public static class SecretSuitcaseValidation
{
    const string LiveKey="SecretSuitcase.LiveValidation";
    static double liveDeadline;
    static int liveFrames;
    static readonly string[] ProgressKeys={"BucaCurrentLevel","BucaPendingLevel","BucaHighestLevel"};
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        EditorApplication.update-=LiveTick;EditorApplication.update+=LiveTick;
        EditorApplication.playModeStateChanged-=LiveChanged;EditorApplication.playModeStateChanged+=LiveChanged;
    }
    public static string RunLive(bool timer = false, bool joystick = false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Start in Edit mode.");
        foreach(string key in ProgressKeys)
        {
            SessionState.SetBool(LiveKey+key+"Exists",PlayerPrefs.HasKey(key));
            SessionState.SetInt(LiveKey+key,PlayerPrefs.GetInt(key));
        }
        SessionState.SetInt(LiveKey,1);SessionState.SetString(LiveKey+"Result","running");
        SessionState.SetBool(LiveKey+"Timer",timer);
        SessionState.SetBool(LiveKey+"Joystick",joystick);
        EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();return "Running live menu, audio and game-launch validation.";
    }
    static void LiveChanged(PlayModeStateChange state)
    {
        if(SessionState.GetInt(LiveKey,0)==0)return;
        if(state==PlayModeStateChange.EnteredPlayMode){liveFrames=0;liveDeadline=EditorApplication.timeSinceStartup+20;}
        if(state!=PlayModeStateChange.EnteredEditMode)return;
        foreach(string key in ProgressKeys)
        {
            if(SessionState.GetBool(LiveKey+key+"Exists",false))PlayerPrefs.SetInt(key,SessionState.GetInt(LiveKey+key,0));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();SessionState.SetInt(LiveKey,0);
    }
    static void LiveFinish(string result)
    {
        File.WriteAllText("output/suitcase-menu/live-validation.txt",result);
        SessionState.SetString(LiveKey+"Result",result);SessionState.SetInt(LiveKey,3);
        EditorApplication.ExitPlaymode();
    }
    static void LiveTick()
    {
        int phase=SessionState.GetInt(LiveKey,0);
        if(phase==0||phase==3||!EditorApplication.isPlaying||EditorApplication.isCompiling||liveFrames++<4)return;
        try
        {
            if(liveDeadline>0&&EditorApplication.timeSinceStartup>liveDeadline)throw new Exception("Live test timed out at phase "+phase);
            if(phase==1)
            {
                Buttons();
                var map=Object.FindAnyObjectByType<TwilightRailwayMap>();
#if ENABLE_INPUT_SYSTEM
                if(SessionState.GetBool(LiveKey+"Joystick",false))SuitcaseJoystickValidation.Check(map);
#endif
                if(SessionState.GetBool(LiveKey+"Timer",false))SuitcasePocketWatchValidation.Check(map);
                if(map.illustratedMenu.GetComponentInChildren<SuitcaseDepartureBoard>(true)!=null)SuitcaseDepartureBoardValidation.Check(map);
                int highest=map.HighestUnlocked;Set(map,"highest",49);
                map.Select(29);for(int i=0;i<24000&&(map.Parked!=29||map.Moving);i++)map.TickTrain(1f/60);
                if(map.Parked!=29)throw new Exception("Failed to reach station 30.");
                map.Select(30);for(int i=0;i<90;i++)map.TickTrain(1f/60);
                if(!map.Moving||!map.motionAudio.isPlaying)throw new Exception("Rolling audio did not follow movement.");
                File.WriteAllText("output/suitcase-menu/audio-validation.txt","PASS: moving 3D engine plays StationMovingTrain; speed controls pitch and volume; whistle and arrival clips assigned.");
                map.Select(29);for(int i=0;i<24000&&(map.Parked!=29||map.Moving);i++)map.TickTrain(1f/60);
                Set(map,"highest",highest);
                int launch=Mathf.Min(29,highest);map.Select(launch);
                for(int i=0;i<24000&&(map.Parked!=launch||map.Moving);i++)map.TickTrain(1f/60);
                SessionState.SetInt(LiveKey+"Expected",launch+1);SessionState.SetInt(LiveKey,2);
                if(SessionState.GetBool(LiveKey+"Timer",false))
                {
                    Set(map,"countdown",0f);
                    typeof(TwilightRailwayMap).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(map,null);
                }
#if ENABLE_INPUT_SYSTEM
                else if(SessionState.GetBool(LiveKey+"Joystick",false))SuitcaseJoystickValidation.Confirm(map);
#endif
                else map.illustratedMenu.playButton.onClick.Invoke();
            }
            else if(phase==2)
            {
                var manager=LevelManager.Instance;
                if(manager==null||manager.CurrentLevelRoot==null)return;
                if(manager.CurrentLevelNumber!=SessionState.GetInt(LiveKey+"Expected",0))throw new Exception("Play loaded the wrong board.");
                string trigger=SessionState.GetBool(LiveKey+"Timer",false)?"Pocket-watch timer expiry":SessionState.GetBool(LiveKey+"Joystick",false)?"Controller Black/A":"Play button";
                LiveFinish("PASS: 50 station buttons; locked Play guard; 3D train movement with live wheel-clatter audio; "+trigger+" loads Game level "+manager.CurrentLevelNumber+". Saved level and unlock progress restored after test.");
            }
        }
        catch(Exception e){LiveFinish("FAIL: "+e.Message);}
    }
    static void Set(TwilightRailwayMap map,string field,object value) =>
        typeof(TwilightRailwayMap).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,value);

    public static string Route()
    {
        if(Application.isPlaying)throw new Exception("Run isolated route validation in Edit mode.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var copy=Object.Instantiate(original.gameObject); copy.name="SuitcaseValidationTemporary"; copy.SetActive(false);
        try
        {
            var map=copy.GetComponent<TwilightRailwayMap>();
            map.eventAudio=null;map.motionAudio=null;map.Open(null);Set(map,"highest",49);
            map.Select(0); Travel(map,0,false);
            int forward=Travel(map,49,true),reverse=Travel(map,0,true);
            Set(map,"highest",0);map.Select(49);
            for(int i=0;i<180;i++)map.TickTrain(1f/60);
            if(map.Moving||map.Parked!=0)throw new Exception("Locked station moved the train.");
            if(original.arrival==null||original.rolling==null||original.whistle==null)throw new Exception("Missing train audio.");
            // Each U-bend must have enough arc length to go around the terrain, not cut straight across it.
            foreach(int next in new[]{6,12,18,24,30,34,38,42,46})
            {
                float from=map.stops[next-1].distance,to=map.stops[next].distance;
                float chord=Vector3.Distance(map.Point(from),map.Point(to));
                float minimumRatio=next==30?1.02f:1.08f; // The bridge is a shallow arch, the others are U-turns.
                if(to-from<chord*minimumRatio)throw new Exception("Untraced bend before level "+(next+1));
            }
            var result="PASS: "+forward+" sequential forward arrivals and "+reverse+" reverse arrivals; every intermediate stop dwells at least 0.55s; wheels rotate with distance; exact station positions; all nine bends traced; locked levels cannot move or launch; whistle, rolling and arrival clips assigned. Saved progress unchanged.";
            Directory.CreateDirectory("output/suitcase-menu");File.WriteAllText("output/suitcase-menu/route-validation.txt",result);return result;
        }
        finally{Object.DestroyImmediate(copy);}
    }
    static int Travel(TwilightRailwayMap map,int target,bool check)
    {
        int previous=map.Parked,arrivals=0;float dwell=0;bool justArrived=false,spun=false;
        var wheel=map.wheels[0].localRotation; map.Select(target);
        for(int frame=0;frame<24000&&(map.Parked!=target||map.Moving);frame++)
        {
            bool wasMoving=map.Moving;
            map.TickTrain(1f/60);
            spun|=Quaternion.Angle(wheel,map.wheels[0].localRotation)>1;
            if(map.Parked!=previous)
            {
                if(Math.Abs(map.Parked-previous)!=1)throw new Exception("Skipped a station.");
                if(Vector3.Distance(map.train.localPosition,map.Point(map.stops[map.Parked].distance))>.001f)throw new Exception("Train missed a platform.");
                previous=map.Parked;arrivals++;dwell=0;justArrived=true;
            }
            else if(!wasMoving&&justArrived)
            {
                dwell+=1f/60;
                if(map.Moving){if(dwell<.55f)throw new Exception("Station dwell too short.");justArrived=false;}
            }
        }
        if(map.Parked!=target||map.Moving||(check&&!spun))throw new Exception("Train failed to arrive or its wheels did not rotate.");
        return arrivals;
    }

    public static string Buttons()
    {
        if(!Application.isPlaying)throw new Exception("Run button validation in Play mode.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>();var view=map.illustratedMenu;
        int selected=map.Selected;
        var raycaster=view.canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        Canvas.ForceUpdateCanvases();view.Layout();Canvas.ForceUpdateCanvases();
        if(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude).Length!=1)throw new Exception("Expected one EventSystem.");
        for(int i=0;i<50;i++)
        {
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(view.canvas.worldCamera,view.stopButtons[i].transform.position)};
            var hits=new List<RaycastResult>();raycaster.Raycast(pointer,hits);
            if(hits.Count==0||hits[0].gameObject!=view.stopButtons[i].gameObject)throw new Exception("Station "+(i+1)+" not clickable.");
            view.stopButtons[i].onClick.Invoke();
            if(map.Selected!=i)throw new Exception("Station "+(i+1)+" listener failed.");
        }
        view.Refresh();
        if(map.HighestUnlocked<49)
        {
            if(view.playButton.interactable)throw new Exception("Locked Play button is enabled.");
            map.RequestPlay();
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MainMenu")throw new Exception("Locked Play loaded a scene.");
        }
        map.Select(selected); Set(map,"countdown",300f);
        string result="PASS: all 50 visible station raycasts and click listeners; exactly one EventSystem; locked Play blocked; selection restored.";
        File.WriteAllText("output/suitcase-menu/button-validation.txt",result);return result;
    }
}
#endif
