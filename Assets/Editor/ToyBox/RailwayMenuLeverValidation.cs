#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class RailwayMenuLeverValidation
{
    public static string Check()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only");
        Directory.CreateDirectory("output/menu-lever");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(copy,scene);copy.SetActive(false);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=null;map.motionAudio=null;
            int returned=0;map.Open(()=>returned++);copy.SetActive(true);
            var lever=map.menuLever;
            lever.returnTransition=null; // Map-only clone must not call the original scene transition.
            if(lever.TickReturn(.1f))throw new Exception("Return started without a menu request");
            var rest=lever.handle.localRotation;
            map.RequestMenuReturn();
            if(!lever.TickReturn(.19f)||returned!=0)throw new Exception("Return must wait for animation");
            if(lever.pushButtonVisual!=null)
            {if(lever.pushButtonVisual.localScale.x>=.99f)throw new Exception("Button did not depress");}
            else if(Quaternion.Angle(rest,lever.handle.localRotation)<15)throw new Exception("Handle did not move");
            lever.TickReturn(.3f);
            if(returned!=1)throw new Exception("Return callback failed");
            lever.ResetLever();
            foreach(var c in copy.GetComponentsInChildren<Camera>(true))
            {
                c.enabled=false;c.scene=scene;
                if(c.targetTexture!=null)RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=c.targetTexture});
            }
            typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{map,11,1672,941,"output/menu-lever/Map.png"});
            var cam=lever.objectCamera;
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=cam.targetTexture});
            var before=RenderTexture.active;RenderTexture.active=cam.targetTexture;
            var image=new Texture2D(820,780,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,820,780),0,0);image.Apply();File.WriteAllBytes("output/menu-lever/Lever.png",image.EncodeToPNG());Object.DestroyImmediate(image);RenderTexture.active=before;
            return "PASS: explicit menu request, visible press motion, delayed single return; rendered map and lever. No Play mode.";
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
