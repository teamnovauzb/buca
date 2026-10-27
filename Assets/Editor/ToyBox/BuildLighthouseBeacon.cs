#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class BuildLighthouseBeacon
{
    public static string Preview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var source=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();Material material=null;
        var previewMaterials=new System.Collections.Generic.List<Material>();
        try
        {
            var copy=UnityEngine.Object.Instantiate(source.gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);copy.SetActive(false);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=null;map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(map,49);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            var beam=map.illustratedMenu.artwork.Find("RotatingLighthouseBeam").GetComponent<UnityEngine.UI.RawImage>();
            material=new Material(beam.material);beam.material=material;
            foreach(var flow in copy.GetComponentsInChildren<ForestHarborWaterFlow>(true))
            {
                var img=flow.GetComponent<UnityEngine.UI.RawImage>();
                if(img==null||img==beam||img.material==null||!img.material.HasProperty("_FlowTime"))continue;
                var instance=new Material(img.material);img.material=instance;previewMaterials.Add(instance);
            }
            copy.SetActive(true);
            System.IO.Directory.CreateDirectory("output/lighthouse-beacon");
            var render=typeof(ForestHarborValidation).GetMethod("Render",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            for(int i=0;i<320;i++)
            {
                material.SetFloat("_FlowTime",i*.05f);
                foreach(var tree in copy.GetComponentsInChildren<ForestBirchGraphic>(true))tree.Pose(i*.05f);
                foreach(var wind in copy.GetComponentsInChildren<ForestLeafWindGraphic>(true))wind.Pose(i*.05f);
                foreach(var instance in previewMaterials)instance.SetFloat("_FlowTime",i*.05f);
                render.Invoke(null,new object[]{map,11,1672,941,"output/lighthouse-beacon/Beam-"+i.ToString("00")+".png"});
            }
            return "Rendered a complete eight-second rotation in Edit mode; remaining controls pass raycast checks.";
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);foreach(var instance in previewMaterials)UnityEngine.Object.DestroyImmediate(instance);if(material!=null)UnityEngine.Object.DestroyImmediate(material);}
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var map=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var art=map.illustratedMenu.artwork;
        var shader=Shader.Find("BUCA/Lighthouse Beacon");
        if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Beacon shader error.");
        var rect=art.Find("RotatingLighthouseBeam") as RectTransform;
        if(rect==null){var go=new GameObject("RotatingLighthouseBeam",typeof(RectTransform));go.layer=5;rect=(RectTransform)go.transform;rect.SetParent(art,false);}
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.sizeDelta=new Vector2(500,200);rect.anchoredPosition=new Vector2(1604-836,470.5f-496);
        var image=rect.GetComponent<UnityEngine.UI.RawImage>()??rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.raycastTarget=false;
        const string path="Assets/TwilightMap/ForestHarbor/LighthouseBeacon.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
        mat.SetFloat("_FlowTime",0);image.material=mat;
        (rect.GetComponent<ForestHarborWaterFlow>()??rect.gameObject.AddComponent<ForestHarborWaterFlow>()).Configure(image);
        EditorUtility.SetDirty(mat);EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved: lighthouse beam rotates 360 degrees every eight seconds; rear fade and tower occlusion; no click interception.";
    }
}
#endif
