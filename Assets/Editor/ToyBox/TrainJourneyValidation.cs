#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Runs the real map in an isolated Play session and restores player preferences afterwards.</summary>
[InitializeOnLoad]
public static class TrainJourneyValidation
{
    const string Key="BucaTrainJourneyValidation", Folder="output/train-journey";
    [Serializable] class SavedPrefs { public List<string> keys=new List<string>(); public List<int> values=new List<int>(); public List<bool> exists=new List<bool>(); }
    static TwilightRailwayMap map;
    static AnimalTrainCarriages cars;
    static AnimalTrainCollection rewards;
    static int phase, milestone, plays, firstFrame;
    static float began, boardingBegan, parkedDistance;
    static bool capturedJump, capturedSeat;
    static TrainJourneyValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}

    public static string Run()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode required");
        Check(!SceneManager.GetActiveScene().isDirty,"Save scene changes before validation");
        var saved=new SavedPrefs();
        var keys=new List<string>{"BucaCurrentLevel","BucaHighestLevel",LevelSelectController.PendingLevelKey};
        foreach(var friend in AnimalTrainCollection.Load().friends)keys.Add("BucaAnimal_"+friend.id);
        foreach(string key in keys){saved.keys.Add(key);saved.exists.Add(PlayerPrefs.HasKey(key));saved.values.Add(PlayerPrefs.GetInt(key,0));}
        SessionState.SetString(Key+"Prefs",JsonUtility.ToJson(saved));
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
        SessionState.SetBool(Key+"Background",Application.runInBackground);
        PlayerPrefs.SetInt("BucaHighestLevel",29);PlayerPrefs.SetInt("BucaCurrentLevel",17);
        foreach(var friend in AnimalTrainCollection.Load().friends)PlayerPrefs.DeleteKey("BucaAnimal_"+friend.id);
        BootFromMainMenu.SetEnabled(false);Application.runInBackground=true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Directory.CreateDirectory(Folder);
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","running");
        EditorApplication.EnterPlaymode();
        return "Started: entrance, five wagons, all five pickups, duplicate prevention, scene recreation, fresh journey and saved-progress restoration.";
    }

    static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
        {phase=0;milestone=0;plays=0;began=Time.realtimeSinceStartup;firstFrame=Time.frameCount;}
        if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key,false))return;
        var saved=JsonUtility.FromJson<SavedPrefs>(SessionState.GetString(Key+"Prefs",""));
        for(int i=0;i<saved.keys.Count;i++)
            if(saved.exists[i])PlayerPrefs.SetInt(saved.keys[i],saved.values[i]);else PlayerPrefs.DeleteKey(saved.keys[i]);
        PlayerPrefs.Save();AnimalTrainCollection.BeginJourney();
        BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));
        Application.runInBackground=SessionState.GetBool(Key+"Background",true);
        SessionState.SetBool(Key,false);
        EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
        File.WriteAllText(Folder+"/validation.txt",SessionState.GetString(Key+"Result","missing"));
    }

    static void Spawn()
    {
        if(map!=null){map.gameObject.SetActive(false);Object.Destroy(map.gameObject);}
        map=Object.Instantiate(Resources.Load<TwilightRailwayMap>("CompletionRailwayMap"));
        map.gameScene=string.Empty;map.gameObject.SetActive(true);map.enabled=false;
        map.viewCamera.rect=new Rect(0,0,1,1);map.viewCamera.enabled=true;
        map.illustratedMenu.canvas.worldCamera=map.viewCamera;
        map.illustratedMenu.canvas.renderMode=RenderMode.ScreenSpaceCamera;
        map.motionAudio.volume=map.eventAudio.volume=0;
    }
    static void AttachCars()
    {
        cars=map.GetComponent<AnimalTrainCarriages>();cars.enabled=false;cars.Tick(0);
        map.illustratedMenu.ShowImmediately();map.illustratedMenu.Layout();map.illustratedMenu.PoseTrain();
    }
    static void WinMilestone()
    {
        int level=(milestone+1)*5;
        var routine=rewards.RewardLevel(level);
        Check(!routine.MoveNext(),"Test rewards should have no full-screen presentation");
        Spawn();map.OpenAfterCompletion(level-1,level,index=>{Check(index==level,"Wrong next level");plays++;});
        AttachCars();map.RequestPlay();
        Check(cars.Boarding && !map.Moving,"Friend must board while the train is parked");
        Check(plays==milestone+1,"Play skipped boarding");
        Check(cars.CarriageCount==5,"Wagons duplicated or missing");
        parkedDistance=map.RouteDistance;boardingBegan=Time.unscaledTime;capturedJump=capturedSeat=false;
    }

    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            if(Time.realtimeSinceStartup-began>75)throw new Exception("Journey validation timed out");
            if(phase==0)
            {
                rewards=ScriptableObject.CreateInstance<AnimalTrainCollection>();
                var catalog=AnimalTrainCollection.Load();rewards.friends=new AnimalTrainCollection.Friend[catalog.friends.Length];
                for(int i=0;i<rewards.friends.Length;i++)rewards.friends[i]=new AnimalTrainCollection.Friend{id=catalog.friends[i].id,rewardLevel=catalog.friends[i].rewardLevel,passenger=catalog.friends[i].passenger};
                Spawn();
                var menu=new GameObject("New game entry validation").AddComponent<ToyBoxMenuController>();menu.enabled=false;
                menu.homeRoot=new GameObject("Validation home");menu.levelsRoot=map.gameObject;
                menu.resumeRoot=new GameObject("Validation resume");menu.twilightMap=map;menu.gameScene=string.Empty;
                typeof(ToyBoxMenuController).GetMethod("StartLevel",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(menu,new object[]{0});
                AttachCars();
                Check(map.Selected==0 && map.Parked==-1 && map.RouteDistance<map.stops[0].distance,"Train did not start before level 1");
                Check(cars.CarriageCount==5 && cars.PassengerCount==0,"Journey must begin with five empty wagons");
                map.PlayInCurrentSession=index=>{Check(index==0,"Entrance did not lead to level 1");plays++;};
                map.RequestPlay();Check(plays==0,"Play skipped entrance");
                Capture("01-approach");phase=1;return;
            }
            float dt=Time.unscaledDeltaTime;
            bool wasBoarding=cars.Boarding;
            map.TickTrain(dt);cars.Tick(dt);
            if(wasBoarding)Check(Mathf.Abs(map.RouteDistance-parkedDistance)<.0001f,"Train departed before boarding finished");
            if(phase==1 && plays==1)
            {
                Check(Time.frameCount>firstFrame+2,"Player loop did not advance");
                Check(map.Parked==0 && !map.Moving,"Train missed level 1");Capture("02-level-one-empty-wagons");
                WinMilestone();phase=2;return;
            }
            if(phase==2)
            {
                float age=Time.unscaledTime-boardingBegan;
                if(milestone==0 && age>1 && !capturedJump){Capture("03-rabbit-boarding");capturedJump=true;}
                if(milestone==0 && age>1.9f && !capturedSeat){Capture("04-rabbit-seated");capturedSeat=true;}
                if(plays!=milestone+2)return;
                Check(cars.PassengerCount==milestone+1 && !cars.Boarding,"Friend did not remain aboard");
                Check(!AnimalTrainCollection.IsWaitingToBoard(rewards.friends[milestone].id),"Pending boarding not cleared");
                if(++milestone<5){WinMilestone();return;}
                Capture("05-all-five-friends");
                var replay=rewards.RewardLevel(25);Check(!replay.MoveNext(),"Repeat reward unexpectedly animated");
                map.OpenAfterCompletion(24,25,_=>{});cars.Tick(0);
                Check(!cars.Boarding && cars.PassengerCount==5,"Duplicate passenger on replay");
                map.Open(null);cars.Tick(0);
                Check(map.Parked==-1 && cars.PassengerCount==0 && cars.CarriageCount==5,"New journey did not reset its train");
                Check(PlayerPrefs.GetInt("BucaHighestLevel")==29 && PlayerPrefs.GetInt("BucaCurrentLevel")==17,"Journey reset saved level progress");
                SessionState.SetString(Key+"Result","PASS: live frames advanced; New Game/home countdown route through the entrance before level 1; five empty connected wagons; early Play waits; rabbit/fox/bear/panda/penguin board after levels 5/10/15/20/25; train stays parked during boarding; passengers survive map recreation; no duplicates; a new journey starts empty without resetting level progress. Original preferences restored after test.");
                phase=3;EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception e){SessionState.SetString(Key+"Result","FAIL: "+e);phase=3;EditorApplication.ExitPlaymode();}
    }

    static void Capture(string name)
    {
        var camera=map.viewCamera;var original=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();
        var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;camera.aspect=16f/9;Canvas.ForceUpdateCanvases();
            map.illustratedMenu.Layout();map.illustratedMenu.PoseTrain();cars.Tick(0);map.GetComponent<SuitcaseLocomotiveView>().Frame();
            foreach(var nested in map.GetComponentsInChildren<Camera>(true))
                if(nested!=camera && nested.targetTexture!=null)RenderPipeline.SubmitRenderRequest(nested,new UniversalRenderPipeline.SingleCameraRequest{destination=nested.targetTexture});
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
            File.WriteAllBytes(Folder+"/"+name+".png",pixels.EncodeToPNG());
        }
        finally{camera.targetTexture=original;RenderTexture.active=active;target.Release();Object.Destroy(target);Object.Destroy(pixels);}
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
#endif
