#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class FullScreenCelebrationPreview
{
    public static string Capture(bool wellDone=false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required.");
        string restore=SceneManager.GetActiveScene().path;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
            // Edit-mode snapshots are taken before the scene's volume stack has
            // received its first frame. Leave post-processing to the live game.
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            PrefabUtility.InstantiatePrefab(manager.levelPrefabs[0],scene);
            var view=manager.levelCompletePanel.premiumResults;
            if(view.celebrationStars.Length!=144 || view.celebrationRays.Length!=80)throw new Exception("Game scene does not use the saved celebration.");
            foreach(var mesh in view.presentation.transform.Find("FullScreenCelebration").GetComponentsInChildren<MeshFilter>(true))
                if(!AssetDatabase.Contains(mesh.sharedMesh))throw new Exception("Unsaved mesh: "+mesh.name);
            view.Show(new ScoreCalculator.ScoreBreakdown{basePoints=500,timeBonus=242,railBonus=0,strokeBonus=75,total=wellDone?840:1226,stars=3,strokesUsed=wellDone?3:1},()=>{});
            view.Tick(1.8f);
            if(wellDone)
            {
                CaptureFrame(view,1.2f,1920,1080,"WellDone-WoodenRibbon");
                CaptureFrame(view,1.2f,900,1600,"WellDone-Portrait");
            }
            else
            {
                CaptureFrame(view,.92f,1920,1080,"Results-Fireworks");
                CaptureFrame(view,2.35f,1920,1080,"Results-Confetti");
                CaptureFrame(view,1.1f,2560,1080,"Results-Wide");
                CaptureFrame(view,1.1f,900,1600,"Results-Portrait");
            }
            view.Hide(false);
            return "Saved four previews to output/celebration; scene uses saved effect meshes and audio.";
        }
        finally {EditorSceneManager.OpenScene(restore);}
    }
    static void CaptureFrame(ToyBoxResults3D view,float time,int width,int height,string name)
    {
        var camera=view.resultsCamera;var target=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
        var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.aspect=(float)width/height;view.Fit();view.SampleCelebration(time);
            camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            System.IO.Directory.CreateDirectory("output/celebration");System.IO.File.WriteAllBytes("output/celebration/"+name+".png",image.EncodeToPNG());
        }
        finally {camera.targetTexture=target;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif
