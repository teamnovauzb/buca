#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Seats the existing boat cutouts in the water without changing their routes.</summary>
public static class BuildHarborBoatSurface
{
    const string Folder="Assets/TwilightMap/ForestHarbor/HarborLife";
    [MenuItem("RealBuca/Forest Harbor/Seat Boats in Water")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("Open MainMenu first.");
        var life=Object.FindAnyObjectByType<ForestHarborLife>(FindObjectsInactive.Include);
        if(life==null)throw new Exception("Harbor boats missing.");
        Directory.CreateDirectory("output/boat-water-contact");
        const string backup="output/boat-water-contact/MainMenu.before-boat-contact.unity";
        if(!File.Exists(backup))File.Copy(scene.path,backup);
        Configure(life);life.Pose(0);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved both boats with immersed hull edges, contact foam, soft reflections, directional wakes and gentle bobbing.";
    }
    public static void Configure(ForestHarborLife life)
    {
        var shader=Shader.Find("BUCA/Harbor Boat Surface");
        if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Boat surface shader failed to compile.");
        foreach(var boat in new[]{life.sailboat,life.tugboat})
        {
            if(boat?.picture==null||boat.wake==null)throw new Exception("Boat image or wake missing.");
            Get<CanvasRenderer>(boat.wake.gameObject);
            var image=boat.picture.GetComponent<UnityEngine.UI.RawImage>();
            var texture=image.texture as Texture2D;
            if(texture==null||!texture.isReadable)throw new Exception("Readable boat texture required.");
            string name=boat.root.name;
            var waterline=BakeWaterline(texture,name);
            var mat=Material(name+"Surface",shader);
            mat.SetTexture("_Waterline",waterline);mat.SetVector("_UVRect",UV(image.uvRect));
            mat.SetFloat("_Reflection",0);mat.SetColor("_Tint",new Color(.84f,.91f,1,1));
            image.material=mat;image.raycastTarget=false;
            Get<ForestHarborWaterFlow>(image.gameObject).Configure(image);
            var reflection=boat.root.Find("WaterReflection") as RectTransform;
            if(reflection==null)
            {
                var go=new GameObject("WaterReflection",typeof(RectTransform));go.layer=image.gameObject.layer;
                reflection=(RectTransform)go.transform;reflection.SetParent(boat.root,false);
            }
            reflection.anchorMin=reflection.anchorMax=new Vector2(.5f,.5f);reflection.pivot=new Vector2(.5f,1);
            reflection.sizeDelta=new Vector2(boat.picture.sizeDelta.x*.96f,boat.picture.sizeDelta.y*.22f);
            reflection.SetAsFirstSibling();
            var reflected=Get<UnityEngine.UI.RawImage>(reflection.gameObject);reflected.texture=texture;
            reflected.uvRect=new Rect(image.uvRect.x,image.uvRect.yMax,image.uvRect.width,-image.uvRect.height);
            reflected.raycastTarget=false;
            var reflectionMaterial=Material(name+"Reflection",shader);
            reflectionMaterial.SetVector("_UVRect",UV(image.uvRect));reflectionMaterial.SetFloat("_Reflection",1);
            reflected.material=reflectionMaterial;
            Get<ForestHarborWaterFlow>(reflection.gameObject).Configure(reflected);
            boat.reflection=reflection;boat.bobPixels=name=="Sailboat"?.55f:.35f;
            boat.wake.rectTransform.sizeDelta=new Vector2(boat.picture.sizeDelta.x*1.2f,32);
            boat.wake.SetVerticesDirty();
            EditorUtility.SetDirty(mat);EditorUtility.SetDirty(reflectionMaterial);EditorUtility.SetDirty(image);
        }
        EditorUtility.SetDirty(life);
    }
    static Vector4 UV(Rect r)=>new Vector4(r.x,r.y,r.width,r.height);
    static T Get<T>(GameObject go)where T:Component
    {
        var component=go.GetComponent<T>();
        return component!=null?component:go.AddComponent<T>();
    }
    static Material Material(string name,Shader shader)
    {
        string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;return mat;
    }
    static Texture2D BakeWaterline(Texture2D image,string name)
    {
        var pixels=image.GetPixels32();var data=new Color32[image.width];
        for(int x=0;x<image.width;x++)
        {
            int bottom=-1;
            for(int y=0;y<image.height;y++)if(pixels[y*image.width+x].a>180){bottom=y;break;}
            data[x]=bottom<0?Color.clear:new Color((bottom+.5f)/image.height,1,0,1);
        }
        var line=new Texture2D(image.width,1,TextureFormat.RGBA32,false,true);
        line.SetPixels32(data);line.Apply();string path=Folder+"/"+name+"Waterline.png";
        File.WriteAllBytes(path,line.EncodeToPNG());Object.DestroyImmediate(line);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
        importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif
