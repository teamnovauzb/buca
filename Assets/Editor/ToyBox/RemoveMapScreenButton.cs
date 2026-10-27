#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class RemoveMapScreenButton
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||!map.illustratedMenu.forestHarborPresentation)throw new Exception("Forest harbor map required.");
        var scene=map.gameObject.scene;
        if(scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu required.");
        Directory.CreateDirectory("output/remove-screen-button");
        const string backup="output/remove-screen-button/MainMenu.before.unity";
        if(!File.Exists(backup))File.Copy(scene.path,backup);
        const string path="Assets/TwilightMap/ForestHarbor/HarborLife/WithoutScreenButton.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new Exception("Repaired artwork missing.");
        importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
        importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var view=map.illustratedMenu;
        var patch=view.artwork.Find("ScreenButtonRemoval") as RectTransform;
        if(patch==null){var go=new GameObject("ScreenButtonRemoval",typeof(RectTransform));go.layer=5;patch=(RectTransform)go.transform;patch.SetParent(view.artwork,false);}
        patch.anchorMin=patch.anchorMax=patch.pivot=Vector2.one*.5f;
        patch.anchoredPosition=Vector2.zero;patch.sizeDelta=new Vector2(1672,941);patch.SetSiblingIndex(1);
        var image=patch.GetComponent<UnityEngine.UI.RawImage>()??patch.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);image.raycastTarget=false;
        const string materialPath="Assets/TwilightMap/ForestHarbor/HarborLife/ScreenButtonRemoval.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(view.artwork.GetComponent<UnityEngine.UI.RawImage>().material);AssetDatabase.CreateAsset(material,materialPath);}
        material.SetFloat("_RepairOnly",2);image.material=material;
        (patch.GetComponent<ForestHarborWaterFlow>()??patch.gameObject.AddComponent<ForestHarborWaterFlow>()).Configure(image);
        view.fullscreenButton.interactable=false;view.fullscreenButton.gameObject.SetActive(false);
        view.fullscreenText.gameObject.SetActive(false);
        EditorUtility.SetDirty(material);EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved: SCREEN artwork, label and click target removed.";
    }
    public static string Preview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(copy,scene);copy.SetActive(false);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=null;map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,49);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            copy.SetActive(true);
            if(map.illustratedMenu.fullscreenButton.gameObject.activeInHierarchy||map.illustratedMenu.fullscreenText.gameObject.activeInHierarchy)throw new Exception("SCREEN still visible.");
            typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{map,11,1672,941,"output/remove-screen-button/Map.png"});
            return "Preview rendered; SCREEN inactive; remaining level and footer click targets passed. No Play mode.";
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
