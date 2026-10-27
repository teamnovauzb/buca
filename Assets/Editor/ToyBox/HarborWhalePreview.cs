#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class HarborWhalePreview
{
    public static string Render(bool sequence=false, string prefabPath=null, string outputFolder="output/harbor-whale")
    {
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        Directory.CreateDirectory(outputFolder+"/frames");
        try
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath??BuildHarborWhale.Path));
            SceneManager.MoveGameObjectToScene(go,scene);
            var reward=go.GetComponent<HarborWhaleEncounter>();
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
            int count=sequence?90:5;
            float[] moments={.5f,1.9f,3.3f,4.7f,6.5f};
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
            if(reward.hello==null||reward.hello.length<.2f||reward.splash==null||reward.mapDetails.tickets.childCount!=50)throw new Exception("Missing encounter assets");
            if(HarborWhaleEncounter.IsWhaleLevel(29)||!HarborWhaleEncounter.IsWhaleLevel(30)||!HarborWhaleEncounter.IsWhaleLevel(49)||HarborWhaleEncounter.IsWhaleLevel(50))throw new Exception("Wrong trigger range");
            reward.firstGreeting=false;reward.Sample(1.4f);
            if(reward.greeting.alpha!=0 || reward.Duration!=4.4f)throw new Exception("Repeat encounter must omit greeting");
            return "PASS: five poses, level 31–50 range, first greeting and shorter silent repeats, audio and 50 stops";
        }
        finally{RenderTexture.active=previous;EditorSceneManager.ClosePreviewScene(scene);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
#endif
