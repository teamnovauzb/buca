#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Applies the selected World 2 mood without replacing the approved map geometry or hit targets.</summary>
public static class BuildMistyHarbor
{
    const string Folder="Assets/TwilightMap/WorkshopRailway";
    const string Output="output/misty-harbor";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static Material MaterialAsset(string name,string shaderName)
    {
        var shader=Shader.Find(shaderName);
        Require(shader!=null&&!ShaderUtil.ShaderHasError(shader),"Shader failed: "+shaderName);
        string path=Folder+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        else material.shader=shader;
        return material;
    }
    public static string Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode required.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        Require(map!=null&&map.gameObject.scene.path=="Assets/Scenes/MainMenu.unity","MainMenu required.");
        Require(map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().workshopRailway,"Workshop map required.");
        var art=map.illustratedMenu.artwork;
        var background=art.GetComponent<UnityEngine.UI.RawImage>();
        var beam=art.Find("RotatingLighthouseBeam").GetComponent<UnityEngine.UI.RawImage>();
        Require(background!=null&&background.texture!=null&&beam!=null,"Existing artwork and beacon required.");
        Directory.CreateDirectory(Output);
        if(!File.Exists(Output+"/MainMenu.before-mist.unity"))File.Copy(map.gameObject.scene.path,Output+"/MainMenu.before-mist.unity");
        var mist=MaterialAsset("MistyHarbor","BUCA/Workshop Misty Harbor");
        mist.SetFloat("_EveningStrength",1);mist.SetFloat("_MistDensity",1);mist.SetFloat("_FlowTime",0);
        background.material=mist;
        background.GetComponent<ForestHarborWaterFlow>().Configure(background);
        var light=MaterialAsset("MistyHarborBeacon","BUCA/Lighthouse Beacon");
        light.SetFloat("_FlowTime",0);light.SetFloat("_Period",6);light.SetFloat("_Intensity",1.6f);
        light.SetFloat("_Range",380);light.SetFloat("_Projection",.55f);
        light.SetVector("_BeamSize",new Vector4(840,460,0,0));
        light.SetVector("_ArtOrigin",new Vector4(1498,278,0,0));light.SetFloat("_ClipToWorkshop",1);
        beam.material=light;beam.raycastTarget=false;
        beam.rectTransform.sizeDelta=new Vector2(840,460);beam.rectTransform.localScale=Vector3.one;
        beam.rectTransform.anchoredPosition=BuildForestHarborMap.Art(new Vector2(1498,278));
        beam.transform.SetAsFirstSibling(); // Light is above the painted harbor but below labels, train and buttons.
        beam.GetComponent<ForestHarborWaterFlow>().Configure(beam);
        EditorUtility.SetDirty(mist);EditorUtility.SetDirty(light);EditorUtility.SetDirty(background);EditorUtility.SetDirty(beam);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved World 2 evening lighting, drifting sea mist and a stronger six-second lighthouse revolution; original artwork, all 50 stops and navigation retained.";
    }

    public static string CheckAndRender()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();
        Material mist=null,light=null;
        try
        {
            var copy=Object.Instantiate(original.gameObject);copy.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",Private).SetValue(map,49);
            var view=map.illustratedMenu;var background=view.artwork.GetComponent<UnityEngine.UI.RawImage>();
            var beam=view.artwork.Find("RotatingLighthouseBeam").GetComponent<UnityEngine.UI.RawImage>();
            mist=new Material(background.material);background.material=mist;
            light=new Material(beam.material);beam.material=light;
            Require(!ShaderUtil.ShaderHasError(mist.shader)&&!ShaderUtil.ShaderHasError(light.shader),"Effect shader compile error.");
            Require(!beam.raycastTarget&&beam.transform.GetSiblingIndex()<view.stopRects[30].GetSiblingIndex(),"Beacon blocks level controls.");
            Require(light.HasProperty("_MainTex")&&light.GetFloat("_Period")==6,"Beacon UI texture or rotation setup missing.");
            Require(background.GetComponent<ForestHarborWaterFlow>().enabled&&beam.GetComponent<ForestHarborWaterFlow>().enabled,"Animation driver disabled.");
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            copy.SetActive(true);Directory.CreateDirectory(Output);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))
                if(camera.gameObject.activeInHierarchy&&camera.targetTexture!=null)
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture});
            var render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            for(int frame=0;frame<5;frame++)
            {
                light.SetFloat("_FlowTime",frame*1.5f);mist.SetFloat("_FlowTime",frame*1.5f);
                render.Invoke(null,new object[]{map,31,1672,941,Output+"/Harbor-"+frame+".png"});
            }
            light.SetFloat("_FlowTime",3);mist.SetFloat("_FlowTime",3);
            render.Invoke(null,new object[]{map,11,1672,941,Output+"/Controls.png"}); // Existing 50-stop and footer raycast checks.
            render.Invoke(null,new object[]{map,31,2560,1080,Output+"/Ultrawide.png"});
            return "PASS: both shaders compile; stronger six-second rotating beacon is below level labels and train; animation drivers assigned; all 50 level and footer raycasts pass; rendered four quarter-turns, loop endpoint and ultrawide framing. Edit mode only.";
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if(mist!=null)Object.DestroyImmediate(mist);if(light!=null)Object.DestroyImmediate(light);
        }
    }
}
#endif
