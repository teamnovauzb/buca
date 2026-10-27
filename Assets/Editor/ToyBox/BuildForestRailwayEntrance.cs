#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class BuildForestRailwayEntrance
{
    const string TexturePath="Assets/TwilightMap/ForestHarbor/RailwayEntrance/WideEntrance.png";
    const string Output="output/railway-entrance";

    [MenuItem("RealBuca/Forest Harbor/Extend Wide Railway To Screen Edge")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null || map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu map required.");
        var art=map.illustratedMenu.artwork;
        var rail=art.Find("RoundForestRailway");
        if(rail==null)throw new Exception("Original railway layer required.");
        AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType=TextureImporterType.Default;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=false;
        importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;
        importer.SaveAndReimport();
        var node=art.Find("WideRailwayEntrance");
        if(node==null)
        {
            var go=new GameObject("WideRailwayEntrance",typeof(RectTransform),typeof(CanvasRenderer),typeof(ForestRailwayEntranceGraphic));
            go.layer=art.gameObject.layer;
            go.transform.SetParent(art,false);
            Undo.RegisterCreatedObjectUndo(go,"Extend original railway to screen edge");
            node=go.transform;
        }
        var rect=(RectTransform)node;
        Undo.RecordObject(rect,"Fit railway continuation");
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.anchoredPosition=new Vector2(180-836,470.5f-835.5f);
        rect.sizeDelta=new Vector2(240,211);
        rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
        rect.SetSiblingIndex(rail.GetSiblingIndex()+1);
        var graphic=node.GetComponent<ForestRailwayEntranceGraphic>();
        Undo.RecordObject(graphic,"Blend railway continuation");
        graphic.artworkTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        graphic.raycastTarget=false;graphic.sideBlend=16;graphic.topBlend=12;
        graphic.color=Color.white;graphic.SetAllDirty();
        EditorUtility.SetDirty(graphic);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        EditorSceneManager.SaveScene(map.gameObject.scene);
        AssetDatabase.SaveAssets();
        return "Saved full-width railway continuation below level 1 to the lower map edge.";
    }

    public static string CheckAndRender()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var source=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(copy,preview);copy.SetActive(false);
            var map=copy.GetComponent<TwilightRailwayMap>();
            map.eventAudio=null;map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(map,49);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=preview;}
            copy.SetActive(true);
            var entrance=map.illustratedMenu.artwork.Find("WideRailwayEntrance").GetComponent<ForestRailwayEntranceGraphic>();
            if(entrance.raycastTarget || entrance.artworkTexture==null)throw new Exception("Entrance graphic configuration incorrect.");
            var rect=entrance.rectTransform;
            if(Mathf.Abs(rect.anchoredPosition.y-rect.sizeDelta.y*.5f+470.5f)>.01f)
                throw new Exception("Railway does not meet the bottom edge.");
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))
                if(camera.targetTexture!=null)RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture});
            Directory.CreateDirectory(Output);
            var render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static);
            // The existing renderer verifies all 50 station and footer click targets at level 12.
            render.Invoke(null,new object[]{map,11,1672,941,Output+"/Map-controls-checked.png"});
            render.Invoke(null,new object[]{map,0,1672,941,Output+"/Wide-entrance-map.png"});
            CaptureDetail(map);
            const string result="PASS: continuation reaches map bottom; decorative layer does not intercept input; all 50 level buttons and footer click targets pass; captured level-1 entrance and complete map.";
            File.WriteAllText(Output+"/validation.txt",result);
            return result;
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }

    static void CaptureDetail(TwilightRailwayMap map)
    {
        var camera=map.viewCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(5016,2823,24);
        var image=new Texture2D(840,903,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.aspect=1672f/941;
            Canvas.ForceUpdateCanvases();map.illustratedMenu.Layout();Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;image.ReadPixels(new Rect(90,0,840,903),0,0);image.Apply();
            File.WriteAllBytes(Output+"/Wide-entrance-detail.png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previous;RenderTexture.active=active;
            rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        }
    }
}
#endif
