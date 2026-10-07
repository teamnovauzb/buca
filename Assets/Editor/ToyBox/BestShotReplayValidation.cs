#if UNITY_EDITOR
using System;
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
            var board=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[0],scene);
            BestShotReplayPreview.BuildPreviewRecording(replay,board,manager.puck,source);
            Check(replay.HasBestShot && replay.BestStrokeCount==1 && replay.BestFrameCount>100,"Completed shot was not retained");
            int frames=replay.BestFrameCount;
            replay.BeginShot(board,manager.puck,2,source.transform.position,source.transform.rotation,38);
            replay.CancelRecording();replay.CommitGoal(1);
            Check(replay.BestFrameCount==frames && replay.BestLevelNumber==1,"Canceled shot replaced best");
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
            replay.ClearRun();Check(!replay.HasBestShot,"New run retained previous player shot");
            var result="PASS: saved scene wiring; empty-run omission; successful recording; canceled-shot exclusion; replay survives board destruction; moving playback; collider-free playback; Skip once; automatic completion; camera restoration; new-run reset. Editor fixture validation (not a physical gameplay test).";
            System.IO.File.WriteAllText("output/replay/validation.txt",result);return result;
        }
        finally{EditorSceneManager.OpenScene(restore);}
    }
}
#endif
