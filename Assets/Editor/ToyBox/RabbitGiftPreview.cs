#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RabbitGiftPreview
{
    public static string Render(bool sequence=false, string prefabPath=null, string outputFolder="output/rabbit-gift")
    {
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        Directory.CreateDirectory(outputFolder+"/frames");
        try
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath??BuildRabbitGiftReward.Path));
            SceneManager.MoveGameObjectToScene(go,scene);
            var reward=go.GetComponent<RabbitGiftReward>();
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
            int count=sequence?126:5;
            float[] moments={.5f,1.5f,2.35f,3.4f,4.2f};
            for(int i=0;i<count;i++)
            {
                float t=sequence?i/24f:moments[i];
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
            if(reward.fade.alpha!=0||reward.controls.blocksRaycasts)throw new Exception("Initial visibility/input gate failed");
            reward.Sample(2.35f);
            if(reward.heading.alpha!=0||reward.controls.alpha!=0)throw new Exception("Reveal text/button appeared before greeting");
            reward.Sample(4.2f);
            if(reward.heading.alpha!=1||reward.controls.alpha!=1)throw new Exception("Final content missing");
            if(reward.revealSound==null||reward.continueButton==null||reward.ticketLayer.childCount!=50)throw new Exception("Missing reward references");
            if(portrait!=null)typeof(AnimalGiftPortrait).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(portrait,null);
            return "PASS: five poses, reverse sampling, input gate, sound and 50 map stops; output/rabbit-gift";
        }
        finally{RenderTexture.active=previous;EditorSceneManager.ClosePreviewScene(scene);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
#endif
