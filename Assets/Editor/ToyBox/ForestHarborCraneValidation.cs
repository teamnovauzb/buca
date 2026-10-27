#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ForestHarborCraneValidation
{
    const string Output = "output/harbor-crane";
    static void Require(bool value,string message) { if(!value) throw new Exception(message); }

    public static string CheckAndRender(int frames = 0)
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only.");
        var source = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene = EditorSceneManager.NewPreviewScene();
        var materials = new List<Material>();
        RenderTexture craneTarget = null;
        try
        {
            var copy = Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(copy,scene); copy.SetActive(false);
            var map = copy.GetComponent<TwilightRailwayMap>();
            map.eventAudio=null; map.motionAudio=null; map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,49);
            var crane = copy.GetComponentInChildren<ForestHarborCrane>(true);
            Require(crane != null && crane.craneCamera != null && crane.picture != null,"Crane references missing.");
            Require(crane.craneCamera.cullingMask == 1<<27,"Crane camera must render only its own layer.");
            foreach(var graphic in map.illustratedMenu.artwork.Find("WorkingHarborCraneArt").GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                Require(!graphic.raycastTarget,"Crane intercepts map input: "+graphic.name);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)) { camera.enabled=false; camera.scene=scene; }
            foreach(var image in copy.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
            {
                if(image.material == null || !image.material.HasProperty("_FlowTime")) continue;
                var material = new Material(image.material); image.material=material; materials.Add(material);
            }
            craneTarget = new RenderTexture(crane.craneCamera.targetTexture);
            crane.craneCamera.targetTexture=craneTarget; crane.picture.texture=craneTarget;
            copy.SetActive(true); crane.enabled=false;
            CheckMechanism(crane);
            Directory.CreateDirectory(Output);
            var render = typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            var times = new[]{0f,3f,6f,10.9f,16.5f};
            for(int i=0;i<times.Length;i++)
            {
                crane.Pose(times[i]);
                foreach(var material in materials) material.SetFloat("_FlowTime",times[i]);
                DrawCrane(crane);
                render.Invoke(null,new object[]{map,11,1672,941,Output+"/Crane-"+i+".png"});
            }
            if(frames>0) RenderAnimation(map,crane,materials,Mathf.Clamp(frames,8,192));
            string report="PASS: hoist/turn/set-down/return cycle; cable joins both attachment points in 1,441 sampled poses; dock contact at rest; safe turning clearance; stationary base; continuous loop; dedicated camera layer; decorative graphics do not intercept any of the 50 level buttons or footer controls. Captured five poses"+(frames>0?" and "+frames+" animation frames":"")+".";
            File.WriteAllText(Output+"/validation.txt",report);
            return report;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            foreach(var material in materials) Object.DestroyImmediate(material);
            if(craneTarget!=null) { craneTarget.Release(); Object.DestroyImmediate(craneTarget); }
        }
    }

    static void CheckMechanism(ForestHarborCrane crane)
    {
        Vector3 fixedPosition=crane.transform.position;
        Vector3 previous=Vector3.zero;
        for(int i=0;i<=1440;i++)
        {
            float time=i/60f; crane.Pose(time);
            Vector3 top=crane.cable.TransformPoint(Vector3.down*.5f);
            Vector3 bottom=crane.cable.TransformPoint(Vector3.up*.5f);
            Require(Vector3.Distance(top,crane.boomTip.position)<.0001f,"Cable detached from pulley.");
            Require(Vector3.Distance(bottom,crane.hook.position)<.0001f,"Cable detached from cargo.");
            Require(crane.cable.localScale.y>.01f,"Cable inverted.");
            Require(crane.transform.position==fixedPosition,"Crane footing drifted.");
            Require(crane.cargo.localPosition.y>=.389f,"Load went through the dock.");
            if(crane.Phase.StartsWith("Turning")) Require(crane.cargo.localPosition.y>=2.39f,"Load turns without lifting clear.");
            if(i>0) Require(Vector3.Distance(previous,crane.cargo.localPosition)<.04f,"Cargo jumps between phases.");
            previous=crane.cargo.localPosition;
        }
        crane.Pose(0); Vector3 start=crane.cargo.localPosition;
        crane.Pose(crane.cycleSeconds-.001f);
        Require(Vector3.Distance(start,crane.cargo.localPosition)<.001f,"Loop does not join.");
        crane.Pose(11);
        Require(Mathf.Abs(crane.cargo.localPosition.y-.39f)<.0001f,"Dock rest height wrong.");
    }

    static void DrawCrane(ForestHarborCrane crane)
        => RenderPipeline.SubmitRenderRequest(crane.craneCamera,
            new UniversalRenderPipeline.SingleCameraRequest { destination=crane.craneCamera.targetTexture });

    static void RenderAnimation(TwilightRailwayMap map,ForestHarborCrane crane,List<Material> materials,int frames)
    {
        // Render the real menu at 2x resolution, recording only the harbor work area.
        var camera=map.viewCamera;
        var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
        var target=new RenderTexture(3344,1882,24,RenderTextureFormat.ARGB32);
        var frame=new Texture2D(480,470,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target; camera.aspect=1672f/941;
            var view=map.illustratedMenu;
            view.canvas.renderMode=RenderMode.ScreenSpaceCamera; view.canvas.worldCamera=camera;
            Canvas.ForceUpdateCanvases();view.Layout();Canvas.ForceUpdateCanvases();
            Directory.CreateDirectory(Output+"/frames");
            for(int i=0;i<frames;i++)
            {
                float seconds=i*crane.cycleSeconds/frames;
                crane.Pose(seconds);
                foreach(var material in materials) material.SetFloat("_FlowTime",seconds);
                DrawCrane(crane); Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;
                frame.ReadPixels(new Rect(2540,652,480,470),0,0);frame.Apply();
                File.WriteAllBytes(Output+"/frames/Crane-"+i.ToString("000")+".png",frame.EncodeToPNG());
            }
        }
        finally
        {
            camera.targetTexture=oldTarget;RenderTexture.active=oldActive;target.Release();
            Object.DestroyImmediate(target);Object.DestroyImmediate(frame);
        }
    }
}
#endif
