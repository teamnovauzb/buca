#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GrandTrainWelcomePreview
{
    public static string Render(bool sequence=false, string prefabPath=null, string outputFolder="output/grand-train-welcome")
    {
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        Directory.CreateDirectory(outputFolder+"/frames");
        try
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath??BuildGrandTrainWelcome.Path));
            SceneManager.MoveGameObjectToScene(go,scene);
            var reward=go.GetComponent<GrandTrainWelcome>();
            var cameraObject=new GameObject("PreviewCamera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=5;camera.nearClipPlane=.01f;camera.farClipPlane=30;
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.targetTexture=target;camera.aspect=1280f/720;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
            go.SetActive(true);
            var portrait=go.GetComponent<AnimalGiftPortrait>();
            if(portrait!=null)typeof(AnimalGiftPortrait).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(portrait,null);
            int count=sequence?150:5;
            float[] moments={.45f,1.5f,4.6f,7.5f,12f};
            for(int i=0;i<count;i++)
            {
                float t=sequence?i/12f:moments[i];
                Canvas.ForceUpdateCanvases();reward.Sample(t);Canvas.ForceUpdateCanvases();
                foreach(var text in go.GetComponentsInChildren<TMPro.TMP_Text>())text.ForceMeshUpdate();
                foreach(var portraitCamera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if(portraitCamera.name=="Portrait camera" && portraitCamera.targetTexture!=null)
                        RenderPipeline.SubmitRenderRequest(portraitCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=portraitCamera.targetTexture});
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                File.WriteAllBytes(outputFolder+"/"+(sequence?"frames/"+i.ToString("D4"):"pose-"+i)+".png",pixels.EncodeToPNG());
            }
            reward.Sample(0);
            if(reward.controls.blocksRaycasts)throw new Exception("Early continue enabled");
            reward.Sample(12);
            if(reward.controls.blocksRaycasts==reward.automaticJourney || reward.animals.Length!=5 || reward.tickets.Length!=5)throw new Exception("Final state invalid");
            if(reward.firstLine==null || reward.firstLine.length<1 || reward.welcomeLine==null || reward.welcomeLine.length<1)throw new Exception("Missing voice audio");
            return "PASS: arrival, five friends, five tickets, final input gate and voice clips";
        }
        finally{RenderTexture.active=previous;EditorSceneManager.ClosePreviewScene(scene);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
#endif
