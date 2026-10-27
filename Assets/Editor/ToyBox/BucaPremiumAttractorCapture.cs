#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

// Offline promotional capture only. Never changes scenes on disk or player progress.
public static class BucaPremiumAttractorCapture
{
    const string DefaultOut="output/buca-attractor-2026/capture";
    static string Out=DefaultOut;
    static int lastJob=3;
    static bool hideTitles;
    const int Fps=30, Width=1920, Height=1080;
    static readonly string[] Jobs={"party","whale","reveal","journey"};
    static readonly float[] Durations={12,7.2f,22,8};
    static int job,frame;
    static Scene scene;
    static GameObject root;
    static Camera camera;
    static RenderTexture target;
    static Texture2D pixels;
    static GrandTrainWelcome party;
    static HarborWhaleEncounter whale;
    static WorldTwoHarborReveal reveal;
    static TwilightRailwayMap map;
    static AnimalTrainCarriages cars;
    static Material mapMaterial;
    static bool active;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;

    public static string Start(int firstJob=0,int finalJob=3,bool cleanTitles=false,string outputFolder=null)
    {
        if(active) return "Capture already active";
        if(EditorApplication.isPlaying)throw new Exception("Capture requires Edit Mode");
        Out=outputFolder??DefaultOut;lastJob=finalJob;hideTitles=cleanTitles;
        Directory.CreateDirectory(Out);
        job=firstJob;frame=0;active=true;Setup();
        AssemblyReloadEvents.beforeAssemblyReload+=Stop;
        EditorApplication.update+=Tick;
        return "Capturing current BUCA animations at 1920x1080 / 30fps in isolated preview scenes";
    }
    static void Setup()
    {
        scene=EditorSceneManager.NewPreviewScene();
        target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32){antiAliasing=4};
        target.Create();pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);
        Directory.CreateDirectory(Out+"/"+Jobs[job]);
        if(job==3)
        {
            var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
            root=Object.Instantiate(original.gameObject);root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root,scene);
            map=root.GetComponent<TwilightRailwayMap>();map.eventAudio=map.motionAudio=null;
            map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",Flags).SetValue(map,49);
            cars=root.GetComponent<AnimalTrainCarriages>()??root.AddComponent<AnimalTrainCarriages>();cars.map=map;
            typeof(AnimalTrainCarriages).GetMethod("Start",Flags).Invoke(cars,null);
            foreach(var friend in AnimalTrainCollection.Load().friends)
                typeof(AnimalTrainCarriages).GetMethod("Add",Flags).Invoke(cars,new object[]{friend});
            camera=map.viewCamera;camera.targetTexture=target;camera.aspect=(float)Width/Height;
            map.illustratedMenu.canvas.worldCamera=camera;map.illustratedMenu.ShowImmediately();
            var artwork=map.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>();
            mapMaterial=new Material(artwork.material);artwork.material=mapMaterial;
            root.SetActive(true);
        }
        else
        {
            string path=job==0?BuildGrandTrainWelcome.Path:job==1?BuildHarborWhale.Path:BuildWorldTwoHarborReveal.Path;
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            SceneManager.MoveGameObjectToScene(root,scene);
            var go=new GameObject("Attractor capture camera");SceneManager.MoveGameObjectToScene(go,scene);
            camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;
            camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.055f,.08f);
            camera.targetTexture=target;camera.aspect=(float)Width/Height;camera.cullingMask=~(1<<28);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;canvas.planeDistance=5;root.SetActive(true);
            party=root.GetComponent<GrandTrainWelcome>();whale=root.GetComponent<HarborWhaleEncounter>();reveal=root.GetComponent<WorldTwoHarborReveal>();
        }
        foreach(var cam in root.GetComponentsInChildren<Camera>(true)){cam.enabled=false;cam.scene=scene;}
        camera.enabled=false;camera.scene=scene;
        frame=0;
    }
    static void Tick()
    {
        if(!active)return;
        try
        {
            for(int batch=0;batch<2&&active;batch++)
            {
                float t=frame/(float)Fps;
                Canvas.ForceUpdateCanvases();
                if(job==0)party.Sample(t);
                else if(job==1){whale.Sample(t);if(hideTitles)whale.greeting.gameObject.SetActive(false);}
                else if(job==2)reveal.Sample(t);
                else
                {
                    float route=Mathf.Lerp(map.stops[5].distance,map.stops[28].distance,t/Durations[job]);
                    int selected=5;while(selected<28&&map.stops[selected+1].distance<route)selected++;
                    foreach(string field in new[]{"selected","parked","destination"})typeof(TwilightRailwayMap).GetField(field,Flags).SetValue(map,selected);
                    typeof(TwilightRailwayMap).GetField("distance",Flags).SetValue(map,route);
                    typeof(TwilightRailwayMap).GetMethod("Pose",Flags).Invoke(map,null);
                    typeof(AnimalTrainCarriages).GetMethod("LateUpdate",Flags).Invoke(cars,null);
                    map.illustratedMenu.Refresh();map.illustratedMenu.PoseTrain();
                    map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().Refresh();
                    mapMaterial.SetFloat("_FlowTime",t);mapMaterial.SetFloat("_WorldReveal",1);
                }
                Canvas.ForceUpdateCanvases();
                foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))if(text.gameObject.activeInHierarchy)text.ForceMeshUpdate();
                foreach(var cam in root.GetComponentsInChildren<Camera>(true))
                    if(cam!=camera&&cam.targetTexture!=null)RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=cam.targetTexture});
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                var old=RenderTexture.active;RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply();RenderTexture.active=old;
                File.WriteAllBytes(Out+"/"+Jobs[job]+"/"+frame.ToString("D4")+".jpg",pixels.EncodeToJPG(95));
                frame++;
                if(frame%30==0)File.WriteAllText(Out+"/progress.txt",Jobs[job]+" "+frame+"/"+Mathf.CeilToInt(Durations[job]*Fps));
                if(frame>=Mathf.CeilToInt(Durations[job]*Fps))
                {
                    Cleanup();job++;
                    if(job>lastJob){Stop();File.WriteAllText(Out+"/done.txt","Complete: requested captures at 30fps, clean titles="+hideTitles);}
                    else Setup();
                }
            }
        }
        catch(Exception e){File.WriteAllText(Out+"/error.txt",e.ToString());Stop();Debug.LogException(e);}
    }
    static void Cleanup()
    {
        if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
        if(target!=null){target.Release();Object.DestroyImmediate(target);}
        if(pixels!=null)Object.DestroyImmediate(pixels);
        if(mapMaterial!=null)Object.DestroyImmediate(mapMaterial);
        root=null;camera=null;target=null;pixels=null;mapMaterial=null;map=null;cars=null;party=null;whale=null;reveal=null;
    }
    public static void Stop()
    {
        EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Stop;
        if(active){active=false;Cleanup();}
    }
}
#endif
