// Offline capture of the saved World Station prefab. No scene assets or progress are saved.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class CaptureLuxoddWorlds
{
    const string Out="output/buca-luxodd-attract/worlds";
    static Scene previous, capture;
    static GameObject root;
    static WorldStationRoute3D view;
    static RenderTexture target;
    static Texture2D pixels;
    static readonly List<Light> lights=new List<Light>();
    static int frame;
    const int Total=870; // map 5s, milestone 5s, journey 12s, welcome 4s, bonus 3s
    public static string Main(bool preview=false)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        previous=SceneManager.GetActiveScene();
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.enabled){lights.Add(l);l.enabled=false;}
        capture=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(capture);
        root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/WorldStation/WorldStationJourney.prefab"));
        root.transform.position=new Vector3(1400,0,0);root.SetActive(true);view=root.GetComponent<WorldStationRoute3D>();
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.49f,.50f);RenderSettings.ambientEquatorColor=new Color(.37f,.35f,.31f);RenderSettings.ambientGroundColor=new Color(.23f,.21f,.18f);
        view.SetUnlocked(true);view.ResetWorld();view.viewCamera.aspect=16f/9;view.Fit(16f/9);
        target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};target.Create();
        pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        Directory.CreateDirectory(Out+"/frames");
        AssemblyReloadEvents.beforeAssemblyReload+=End;
        if(preview){try{foreach(float t in new[]{2f,7f,11.5f,15f,19.5f,24f,28f}){Sample(t);Save(Out+"/preview-"+t.ToString("0.0")+".jpg");}}finally{End();}return "World shots previewed";}
        frame=0;EditorApplication.update+=Tick;return "Rendering 870 frames of the actual world maps and celebrations";
    }
    static void Sample(float t)
    {
        if(t<10){if(view.BonusVisible)view.ResetWorld();view.Sample(t);view.victory.gameObject.SetActive(t>=5);if(t>=5)view.victory.Sample(t-5+.15f);}
        else if(t<22){view.victory.Stop();float j=(t-10)/1.6f;view.Sample(t,j);view.SampleTransition(j);view.Sample(t,j);}
        else{if(!view.BonusVisible)view.ShowBonus();view.Sample(t);view.SampleTransition(7.4f);view.welcome.gameObject.SetActive(t<26);if(t<26)view.welcome.Sample(t-22+.15f);}
        Physics.SyncTransforms();VolumeManager.instance.Update(view.viewCamera.transform,1<<29);
    }
    static void Save(string file)
    {
        var req=new UniversalRenderPipeline.SingleCameraRequest{destination=target};RenderPipeline.SubmitRenderRequest(view.viewCamera,req);
        var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();RenderTexture.active=old;
        File.WriteAllBytes(file,pixels.EncodeToJPG(94));
    }
    static void Tick()
    {
        try{Sample(frame/30f);Save(Out+"/frames/"+frame.ToString("D4")+".jpg");frame++;
            if(frame%30==0)File.WriteAllText(Out+"/progress.txt",frame+" / "+Total);
            if(frame>=Total){File.WriteAllText(Out+"/done.txt","870 frames; 1920x1080; 30 fps; actual saved game assets and runtime animation samples.");End();}}
        catch(Exception e){File.WriteAllText(Out+"/error.txt",e.ToString());End();throw;}
    }
    static void End()
    {
        EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=End;
        if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
        if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);
        if(previous.IsValid())SceneManager.SetActiveScene(previous);
        if(capture.IsValid())EditorSceneManager.CloseScene(capture,true);
        foreach(var l in lights)if(l!=null)l.enabled=true;lights.Clear();
    }
}
