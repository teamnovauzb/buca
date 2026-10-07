using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class WatchCopyTutorialValidation
{
    public static void Check(LevelManager manager)
    {
        var saved=new System.Collections.Generic.Dictionary<string,int>();
        foreach(string key in BuildToyBoxMainMenu.TutorialKeys)
        {
            string pref=ObstacleIntroController.SeenPreferencePrefix+key;
            saved[pref]=PlayerPrefs.GetInt(pref,-1); PlayerPrefs.DeleteKey(pref);
        }
        try { CheckPlayback(manager); }
        finally
        {
            foreach(var entry in saved)
                if(entry.Value<0) PlayerPrefs.DeleteKey(entry.Key); else PlayerPrefs.SetInt(entry.Key,entry.Value);
            PlayerPrefs.Save();
        }
    }

    static void CheckPlayback(LevelManager manager)
    {
        var view=UnityEngine.Object.FindAnyObjectByType<WatchCopyTutorial3D>();
        var intro=UnityEngine.Object.FindAnyObjectByType<ObstacleIntroController>(FindObjectsInactive.Include);
        if(view==null || intro==null || intro.watchCopy!=view) throw new Exception("Watch/Copy not assigned.");
        if(view.lessons.Length!=19) throw new Exception("Missing tutorial mechanics.");
        if(view.IsShowing) { view.Skip(); }
        PlayerPrefs.DeleteKey(ObstacleIntroController.SeenPreferencePrefix+"BASIC");
        var plain=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_01.prefab");
        var rebound=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_02.prefab");
        if(!intro.TryShowForLevel(plain,1,()=>{}) || view.CurrentMechanic!="BASIC") throw new Exception("Level 1 basic tutorial missing.");
        view.Tick(14.1f,false,false);view.Tick(5f,false,false);
        if(view.IsShowing || intro.IsShowing) throw new Exception("Basic tutorial did not close.");
        if(intro.TryShowForLevel(plain,1,()=>{})) throw new Exception("Watched basic tutorial repeated.");
        ObstacleIntroController.ResetTutorialsForNewRun();
        if(!intro.TryShowForLevel(plain,1,()=>{})) throw new Exception("New run did not restore basic tutorial.");
        view.Skip();
        if(!intro.TryShowForLevel(rebound,2,()=>{}) || view.CurrentMechanic!="RIM")
            throw new Exception("New rebound mechanic missing its tutorial.");
        view.Tick(14.1f,false,false); view.Tick(5f,false,false);
        if(view.IsShowing || intro.IsShowing) throw new Exception("Finished tutorial did not automatically resume game.");
        if(intro.TryShowForLevel(rebound,2,()=>{})) throw new Exception("Familiar mechanic repeats.");
        var multi=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_08.prefab");
        if(!intro.TryShowForLevel(multi,8,()=>{}) || view.CurrentMechanic!="MOVE") throw new Exception("New mechanic queue missing.");
        view.Skip();
        if(PlayerPrefs.GetInt(ObstacleIntroController.SeenPreferencePrefix+"SPIN",0)!=0)
            throw new Exception("Skipping current tutorial marks unwatched tutorials seen.");
        if(!view.IsShowing || !intro.IsShowing || view.CurrentMechanic!="SPIN") throw new Exception("Skip bypassed the next new mechanic.");
        intro.SkipTutorial();
        var pegLevel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_07.prefab");
        PlayerPrefs.DeleteKey(ObstacleIntroController.SeenPreferencePrefix+"MUD");
        PlayerPrefs.DeleteKey(ObstacleIntroController.SeenPreferencePrefix+"PEGS");
        bool levelSevenReady=false;
        if(!intro.TryShowForLevel(pegLevel,7,()=>levelSevenReady=true) || view.CurrentMechanic!="MUD") throw new Exception("Level 7 mud lesson missing.");
        view.Skip();
        if(levelSevenReady || !view.IsShowing || view.CurrentMechanic!="PEGS") throw new Exception("Level 7 began before guard-peg lesson.");
        view.Tick(14.1f,false,false);view.Tick(5f,false,false);
        if(!levelSevenReady || view.IsShowing) throw new Exception("Level 7 did not resume after both lessons.");
        PlayerPrefs.DeleteKey(ObstacleIntroController.SeenPreferencePrefix+"PEGS");
        PlayerPrefs.SetInt(ObstacleIntroController.SeenPreferencePrefix+"MUD",1);
        if(!intro.TryShowForLevel(pegLevel,7,()=>{}) || view.CurrentMechanic!="PEGS") throw new Exception("Guard pegs missing their first-encounter tutorial.");
        view.Skip();
        if(intro.TryShowForLevel(pegLevel,7,()=>{})) throw new Exception("Guard peg tutorial repeats after introduction.");
        // Full campaign: teach a mechanic once, not once per level or restart.
        ObstacleIntroController.ResetTutorialsForNewRun();
        var taught=new System.Collections.Generic.HashSet<string>();
        for(int level=1;level<=30;level++)
        {
            var course=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ToyBoxMenu/Levels/Level_{level:00}.prefab");
            intro.TryShowForLevel(course,level,()=>{});
            int guard=0;
            while(view.IsShowing)
            {
                if(++guard>19 || !taught.Add(view.CurrentMechanic)) throw new Exception("Familiar mechanic shown again at level "+level);
                view.Tick(14.1f,false,false); view.Tick(5f,false,false);
            }
            if(intro.TryShowForLevel(course,level,()=>{})) throw new Exception("Restart repeats a tutorial at level "+level);
        }
        for(int level=1;level<=30;level++)
            if(intro.TryShowForLevel(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ToyBoxMenu/Levels/Level_{level:00}.prefab"),level,()=>{}))
                throw new Exception("Replaying campaign repeats learned mechanics.");
        Debug.Log("TUTORIAL_FIRST_ENCOUNTER_PASSED: all 30 levels, restarts and campaign replay.");
        var keys=new System.Collections.Generic.HashSet<string>();
        foreach(var lesson in view.lessons)
        {
            if(!keys.Add(lesson.key) || !AssetDatabase.Contains(lesson.demonstration)) throw new Exception("Missing saved lesson.");
            bool done=false;
            view.Begin(new[]{lesson.key},()=>done=true);
            if(view.gameplayCamera.enabled || !view.lessonCamera.isActiveAndEnabled) throw new Exception("Tutorial camera isolation failed.");
            if(!view.skipLabel.activeInHierarchy || !view.controlSelectors[0].activeSelf) throw new Exception("Replay should be selected while the tutorial plays.");
            view.Tick(4,false,false);
            view.SelectControl(1); view.ActivateSelectedControl(); view.Tick(.17f,false,false);
            float progress=view.PlaybackProgress;
            view.Tick(30,false,false);
            if(!view.IsPaused || view.PlaybackProgress!=progress) throw new Exception("Selected Pause failed.");
            view.ActivateSelectedControl(); view.Tick(.17f,false,false);
            view.SelectControl(0); view.ActivateSelectedControl(); view.Tick(.17f,false,false);
            if(view.PlaybackProgress!=0 || view.IsPaused) throw new Exception("Selected Replay failed.");
            view.Tick(3,false,false);
            if(lesson.key=="WARP" || lesson.key=="MUD" || lesson.key=="PEGS")
            {
                view.lessonCamera.aspect=1.6f; view.FitCameraAspect(1.6f);
                view.SelectControl(2);
                ToyBoxMenuValidation.RenderCamera(view.lessonCamera,"Tutorial-WatchOnly-"+lesson.key,1600,1000);
            }
            if(lesson.key=="PEGS")
            {
                var puck=lesson.root.transform.Find("Puck");
                for(float t=4;t<=7;t+=.025f)
                {
                    lesson.demonstration.SampleAnimation(view.presentation,t);
                    foreach(Transform peg in lesson.root.transform.Find("Mechanic"))
                    {
                        if(!peg.name.StartsWith("GuardPeg")) continue;
                        Vector3 delta=puck.position-peg.position; delta.y=0;
                        if(delta.magnitude<.59f) throw new Exception("Tutorial shot clips a guard peg.");
                    }
                }
            }
            view.Tick(12,false,false);
            if(view.Step!=1 || view.ChoiceRemaining!=5 || done) throw new Exception("No full five-second choice window.");
            if(!view.countdownLabels[4].activeSelf || !view.letsPlayLabel.activeSelf || !view.replayLabel.activeSelf)
                throw new Exception("Countdown, Repeat or Continue missing.");
            view.SelectControl(0); view.ActivateSelectedControl();
            if(view.repeatCap.localPosition.y>=view.repeatRest.y) throw new Exception("Repeat button did not depress visibly.");
            view.Tick(.17f,false,false);
            if(view.Step!=0 || view.PlaybackProgress!=0) throw new Exception("Repeat did not restart video.");
            view.Tick(14f,false,false);
            if(lesson.key=="WARP") ToyBoxMenuValidation.RenderCamera(view.lessonCamera,"Tutorial-Five-Second-Choice",1600,1000);
            view.Tick(4.9f,false,false);
            if(!view.IsShowing || done || !view.countdownLabels[0].activeSelf) throw new Exception("Countdown advanced early.");
            view.Tick(.11f,false,false);
            if(!done || view.IsShowing || !view.gameplayCamera.enabled) throw new Exception("Watch-only tutorial did not resume gameplay.");
            foreach(var practice in view.GetComponentsInChildren<TutorialPractice3D>(true))
                if(practice.gameObject.activeInHierarchy) throw new Exception("Tutorial opened unwanted practice.");
        }
        // Audit the actual campaign using the same detection method as runtime.
        var kindType=typeof(ObstacleIntroController).GetNestedType("MechanicKind",BindingFlags.NonPublic);
        var detect=typeof(ObstacleIntroController).GetMethod("LevelContains",BindingFlags.Static|BindingFlags.NonPublic);
        string[] mechanicKeys={"RIM","BANK","MUD","MOVE","ICE","FAST","+PTS","!","PUSH","WIND","PULL","JUMP","KICK","WARP","PEGS"};
        var report=new System.Text.StringBuilder();
        for(int number=1;number<=30;number++)
        {
            var levelRoot=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_"+number.ToString("00")+".prefab");
            if(levelRoot==null) throw new Exception("Campaign level missing.");
            var present=new System.Collections.Generic.List<string>();
            for(int kind=0;kind<mechanicKeys.Length;kind++)
                if((bool)detect.Invoke(null,new object[]{levelRoot,Enum.ToObject(kindType,kind)})) present.Add(mechanicKeys[kind]);
            if(levelRoot.GetComponentInChildren<MovingWall>(true)!=null) present.Add("SLIDE");
            if(levelRoot.GetComponentInChildren<RotatingWall>(true)!=null) present.Add("SPIN");
            if(levelRoot.GetComponentInChildren<DisappearingWall>(true)!=null) present.Add("GATE");
            foreach(string key in present) if(!keys.Contains(key)) throw new Exception("No saved lesson for "+key);
            report.AppendLine("Level "+number.ToString("00")+": "+string.Join(", ",present));
        }
        System.IO.Directory.CreateDirectory("output/toy-box");
        System.IO.File.WriteAllText("output/toy-box/tutorial-mechanic-coverage.txt",report.ToString());
        view.Begin(new[]{"RIM","WARP"},()=>{});
        view.Tick(14.1f,false,false);
        view.SelectControl(2); view.ActivateSelectedControl(); view.Tick(.17f,false,false);
        if(view.CurrentMechanic!="WARP" || !view.IsShowing) throw new Exception("Tutorial queue did not advance automatically.");
        view.lessonCamera.aspect=1.6f; view.FitCameraAspect(1.6f);
        Physics.SyncTransforms();
        Collider[] controls={view.replayHit,view.pauseHit,view.skipHit};
        for(int i=0;i<controls.Length;i++)
        {
            Vector3 point=view.lessonCamera.WorldToScreenPoint(controls[i].bounds.center);
            if(!view.HandlePointer(point,false) || view.SelectedControl!=i || !view.controlSelectors[i].activeSelf)
                throw new Exception("Tutorial pointer selector failed: "+i);
        }
        Vector3 skip=view.lessonCamera.WorldToScreenPoint(view.skipHit.bounds.center);
        if(!view.HandlePointer(skip,true)) throw new Exception("Visible Skip button failed.");
        view.Tick(.17f,false,false);
        if(view.IsShowing) throw new Exception("Skip did not finish after press feedback.");
        Debug.Log("WATCH_COPY_VALIDATION_PASSED: all 19 watch-only clips, five-second end countdown, automatic queue and gameplay return, visible Repeat/Continue/Skip, button depression feedback, mouse/keyboard selection actions, Pause/Repeat, unseen-mechanic scheduling; no practice step.");
    }
}
