#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class BestShotReplayValidation
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
        var restore=SceneManager.GetActiveScene().path;
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            var replay=manager.bestShotReplay;var source=Camera.main;
            Check(replay!=null,"Game scene missing replay");
            Check(!replay.TryShow(source,null),"Empty run must omit replay");
            var board=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[1],scene);
            replay.BeginShot(board,manager.puck,1,source.transform.position,source.transform.rotation,38);
            Check(!replay.IsRecording,"Level 1 must not record");
            replay.CommitGoal(1);
            Check(!replay.HasBestShot,"Level 1 must not qualify");
            BestShotReplayPreview.BuildPreviewRecording(replay,board,manager.puck,source);
            Check(replay.HasBestShot && replay.BestStrokeCount==1 && replay.BestFrameCount>100,"Completed shot was not retained");
            int frames=replay.BestFrameCount;
            replay.BeginShot(board,manager.puck,2,source.transform.position,source.transform.rotation,38);
            replay.CancelRecording();replay.CommitGoal(1);
            Check(replay.BestFrameCount==frames && replay.BestLevelNumber==2,"Canceled shot replaced best");
            UnityEngine.Object.DestroyImmediate(board);
            int callbacks=0;bool enabled=source.enabled;
            Check(replay.TryShow(source,()=>callbacks++),"Replay must survive original board destruction");
            Check(!source.enabled && replay.IsShowing,"Camera handoff failed");
            replay.SampleReplay(0);
            var starts=new Vector3[replay.savedMeshes.Length];
            for(int i=0;i<starts.Length;i++)starts[i]=replay.savedMeshes[i].transform.localPosition;
            replay.SampleReplay(.6f);bool moved=false;
            for(int i=0;i<starts.Length;i++)if(Vector3.Distance(starts[i],replay.savedMeshes[i].transform.localPosition)>.1f)moved=true;
            Check(moved && Mathf.Abs(replay.Progress-.6f)<.001f,"Playback did not advance");
            Check(replay.recordedStage.GetComponentsInChildren<Collider>(true).Length==0,"Replay must have no physics colliders");
            replay.Skip();replay.Skip();replay.Tick(.2f);replay.Tick(20);replay.Hide(true);
            Check(callbacks==1 && !replay.IsShowing && source.enabled==enabled,"Skip must finish exactly once and restore camera");
            Check(replay.TryShow(source,()=>callbacks++),"Replay reopen failed");
            replay.Tick(20);
            Check(callbacks==2 && !replay.IsShowing && source.enabled==enabled,"Automatic completion failed");
            ValidateCitySelection(manager, replay, source, scene);
            replay.ClearRun();Check(!replay.HasBestShot,"New run retained previous player shot");
            var result="PASS: saved scene wiring; empty-run omission; successful recording; canceled-shot exclusion; replay survives board destruction; moving playback; collider-free playback; Skip once; automatic completion; camera restoration; World 1 to World 2 isolation; current-city-level attempts; city scenery retained; nearest-goal attempt selection; Undo restores previous attempt; next-level and retry isolation; no-shot omission; new-run reset. Editor fixture validation (not a physical gameplay test).";
            System.IO.Directory.CreateDirectory("output/replay");
            System.IO.File.WriteAllText("output/replay/validation.txt",result);return result;
        }
        finally{EditorSceneManager.OpenScene(restore);}
    }

    static void ValidateCitySelection(LevelManager manager, BestShotReplay replay, Camera source, Scene scene)
    {
        Check(replay.BestLevelNumber==2,"World 1 fixture was lost");
        Check(!replay.TryShow(source,null,31),"World 2 accepted a World 1 highlight");
        replay.BeginLevel(31);
        Check(!replay.HasBestShot && !replay.TryShow(source,null,31),"City level with no shots must omit replay");
        var board=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[30],scene);
        var theme=UnityEngine.Object.FindAnyObjectByType<EveningGameplayTheme>();
        Check(theme!=null && theme.station!=null,"City theme missing");
        var sky=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
        try
        {
            theme.Apply(true,board);
            RecordAttempt(replay,board,manager.puck,source,31,1,.45f);
            Check(replay.HasBestShotForLevel(31) && replay.BestLevelNumber==31,"First city attempt did not replace the room highlight");
            Check(replay.TryShow(source,null,31),"City attempt replay failed");
            var cityMeshes=theme.station.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).ToArray();
            Check(replay.savedMeshes.Any(f=>f.GetComponent<Renderer>().enabled && cityMeshes.Contains(f.sharedMesh)),"Replay omitted recorded city scenery");
            Check(!theme.room.activeSelf,"Room should not appear in city capture");
            replay.Hide(false);
            RecordAttempt(replay,board,manager.puck,source,31,2,.8f);
            Check(replay.BestStrokeCount==2,"Closer city attempt should rank first");
            replay.DiscardShotsAfter(1);
            Check(replay.BestStrokeCount==1,"Undo did not restore previous best attempt");
            replay.DiscardShotsAfter(0);
            Check(!replay.HasBestShot,"Undone attempt remained eligible");
            RecordAttempt(replay,board,manager.puck,source,31,1,.65f);
            replay.BeginShot(board,manager.puck,31,source.transform.position,source.transform.rotation,38);
            replay.CancelRecording();replay.CommitGoal(1);
            Check(replay.BestStrokeCount==1 && replay.HasBestShotForLevel(31),"Canceled shot lost the valid city attempt");
            Check(!replay.TryShow(source,null,32),"Level 32 accepted level 31's replay");
            replay.BeginLevel(32);
            Check(!replay.HasBestShot,"City level transition retained the previous level");
            RecordAttempt(replay,board,manager.puck,source,32,1,.6f);
            replay.BeginLevel(32);
            Check(!replay.HasBestShot,"Retry retained a previous attempt");
            replay.BeginLevel(30);
            Check(!replay.HasBestShot,"Returning to World 1 retained a city clip");
        }
        finally
        {
            replay.Hide(false);theme.Apply(false,board);
            RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            UnityEngine.Object.DestroyImmediate(board);
        }
    }

    // Deterministic recording fixture; the runtime records actual player physics.
    static void RecordAttempt(BestShotReplay replay,GameObject board,GameObject puck,Camera camera,int level,int stroke,float fraction)
    {
        var from=board.transform.Find("PuckStart").position;
        var to=board.transform.Find("Hole").position;
        from.y=to.y=.22f;
        puck.SetActive(true);puck.transform.localScale=Vector3.one*.46f;puck.transform.position=from;
        replay.BeginShot(board,puck,level,camera.transform.position,camera.transform.rotation,camera.fieldOfView);
        var capture=typeof(BestShotReplay).GetMethod("CaptureFrame",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int i=1;i<=60;i++)
        {
            puck.transform.position=Vector3.Lerp(from,to,fraction*i/60f);
            capture.Invoke(replay,new object[]{i/30f});
        }
        typeof(BestShotReplay).GetField("_recordStarted",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(replay,Time.time-2f);
        replay.CommitAttempt(stroke);
    }
}
#endif
