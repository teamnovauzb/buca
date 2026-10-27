#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
public static class AnimalPassengerValidation
{
    public static string Check(bool fullMap = false)
    {
        var catalog=AnimalTrainCollection.Load();if(catalog==null||catalog.friends.Length<1)throw new Exception("Animal collection required");
        for(int i=0;i<catalog.friends.Length;i++)if(catalog.friends[i].rewardLevel!=(i+1)*5||catalog.friends[i].passenger==null||catalog.friends[i].celebration==null)throw new Exception("Invalid milestone");
        const string test="__validation_only";string key="BucaAnimal_"+test;
        if(PlayerPrefs.HasKey(key))throw new Exception("Test key occupied");
        try{if(!AnimalTrainCollection.Collect(test)||AnimalTrainCollection.Collect(test))throw new Exception("Duplicate collection");}finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        var original=Object.FindFirstObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();GameObject copy=null;
        try{
            copy=Object.Instantiate(original.gameObject);copy.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=map.motionAudio=null;map.Open(null);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var cars=copy.AddComponent<AnimalTrainCarriages>();cars.map=map;typeof(AnimalTrainCarriages).GetMethod("Start",flags).Invoke(cars,null);
            foreach(var friend in catalog.friends)typeof(AnimalTrainCarriages).GetMethod("Add",flags).Invoke(cars,new object[]{friend});
            var capture=map.GetComponent<SuitcaseLocomotiveView>().trainCamera.targetTexture;
            var trainImage=map.illustratedMenu.locomotive.GetComponent<UnityEngine.UI.RawImage>();
            if(capture.width<1440||capture.width>3072||trainImage.texture!=capture)
                throw new Exception("Passenger capture resolution or UI texture is incorrect");
            for(int level=0;level<map.stops.Length;level++)
            {
                typeof(TwilightRailwayMap).GetField("distance",flags).SetValue(map,map.stops[level].distance);
                typeof(TwilightRailwayMap).GetMethod("Pose",flags).Invoke(map,null);
                typeof(AnimalTrainCarriages).GetMethod("LateUpdate",flags).Invoke(cars,null);
                foreach(var portrait in copy.GetComponentsInChildren<PremiumPassengerPortrait>(true))
                {
                    if(Vector3.Dot(portrait.transform.up,Vector3.up)<.999f)
                        throw new Exception($"Passenger tilted into wagon at level {level+1}");
                    var camera=portrait.renderingCamera;
                    var origin=camera.WorldToScreenPoint(portrait.transform.position);
                    var right=camera.WorldToScreenPoint(portrait.transform.TransformPoint(Vector3.right))-origin;
                    var up=camera.WorldToScreenPoint(portrait.transform.TransformPoint(Vector3.up))-origin;
                    float ratio=new Vector2(right.x,right.y).magnitude/new Vector2(up.x,up.y).magnitude;
                    if(Mathf.Abs(ratio-1)>.015f)throw new Exception($"Passenger stretched at level {level+1}: {ratio}");
                }
            }
            typeof(TwilightRailwayMap).GetField("distance",flags).SetValue(map,map.stops[25].distance);
            typeof(TwilightRailwayMap).GetMethod("Pose",flags).Invoke(map,null);typeof(AnimalTrainCarriages).GetMethod("LateUpdate",flags).Invoke(cars,null);
            var rig=map.GetComponent<SuitcaseLocomotiveView>();rig.Frame();copy.SetActive(true);
            var rt=new RenderTexture(1800,1200,24){antiAliasing=4};rt.Create();
            try{
                RenderPipeline.SubmitRenderRequest(rig.trainCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                var old=RenderTexture.active;RenderTexture.active=rt;var pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();RenderTexture.active=old;
                System.IO.Directory.CreateDirectory("output/animal-passengers");System.IO.File.WriteAllBytes("output/animal-passengers/six-carriages.png",pixels.EncodeToPNG());Object.DestroyImmediate(pixels);
                RenderTexture.active=rt;
                var detail=new Texture2D(720,380,TextureFormat.RGBA32,false);
                detail.ReadPixels(new Rect(330,380,720,380),0,0);detail.Apply();
                System.IO.File.WriteAllBytes("output/animal-passengers/polished-train-detail.png",detail.EncodeToPNG());
                Object.DestroyImmediate(detail);RenderTexture.active=old;
            }finally{rt.Release();Object.DestroyImmediate(rt);}
            if(fullMap)
            {
                typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic)
                    .Invoke(null,new object[]{map,25,2560,1440,"output/animal-passengers/actual-map-six-carriages.png"});
            }
            return $"PASS: {catalog.friends.Length} passengers upright at all {map.stops.Length} levels, {capture.width}x{capture.height} capture wired to UI, duplicate collection blocked, route preview rendered; player collection unchanged.";
        }finally{if(copy!=null)Object.DestroyImmediate(copy);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
