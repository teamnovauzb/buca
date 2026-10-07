#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BestShotReplayPreview
{
    public static string Capture()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean edit mode required.");
        string restore=SceneManager.GetActiveScene().path;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            var view=manager.bestShotReplay;
            if(view==null)throw new Exception("Best shot replay is not baked into Game.");
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var board=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[0],scene);
            var cameraSource=Camera.main;
            BuildPreviewRecording(view,board,manager.puck,cameraSource);
            if(!view.TryShow(cameraSource,()=>{}))throw new Exception("Preview recording did not open.");
            view.SampleEntrance(1);view.SampleReplay(.50f);
            CaptureFrame(view,1920,1080,"BestShot-WoodenFrame");
            CaptureFrame(view,900,1600,"BestShot-WoodenFrame-Portrait");
            view.Hide(false);
            return "Saved landscape and portrait replay-frame previews in output/replay.";
        }
        finally{EditorSceneManager.OpenScene(restore);}
    }
    // Editorial preview only. Runtime always records the player's physics motion.
    public static void BuildPreviewRecording(BestShotReplay view,GameObject board,GameObject puck,Camera source)
    {
        var start=board.transform.Find("PuckStart");var hole=board.transform.Find("Hole");
        Vector3 from=start!=null?start.position:new Vector3(0,.22f,-5);
        Vector3 to=hole!=null?hole.position:new Vector3(0,.22f,6);
        from.y=to.y=.22f;
        puck.SetActive(true);puck.transform.position=from;puck.transform.localScale=Vector3.one*.46f;
        var scale=puck.transform.localScale;
        view.BeginShot(board,puck,1,source.transform.position,source.transform.rotation,source.fieldOfView);
        var capture=typeof(BestShotReplay).GetMethod("CaptureFrame",BindingFlags.NonPublic|BindingFlags.Instance);
        for(int i=1;i<=120;i++)
        {
            float t=i/120f;
            float flight=Mathf.Clamp01(t/.9f);
            puck.transform.position=Vector3.Lerp(from,to,flight);
            if(t>.9f)puck.transform.localScale=scale*(1-(t-.9f)/.1f);
            capture.Invoke(view,new object[]{t*4});
        }
        typeof(BestShotReplay).GetField("_recordStarted",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,Time.time-4f);
        view.CommitGoal(1);
    }
    public static void CaptureFrame(BestShotReplay view,int width,int height,string name)
    {
        var camera=view.frameCamera;var previous=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
        var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            view.playbackCamera.Render();
            camera.targetTexture=rt;camera.aspect=(float)width/height;view.Fit();camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            System.IO.Directory.CreateDirectory("output/replay");System.IO.File.WriteAllBytes("output/replay/"+name+".png",image.EncodeToPNG());
        }
        finally{camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif
