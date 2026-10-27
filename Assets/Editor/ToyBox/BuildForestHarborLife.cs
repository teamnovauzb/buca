#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Adds harbor life to the existing map without rebuilding its controls or railway.</summary>
public static class BuildForestHarborLife
{
    const string Folder="Assets/TwilightMap/ForestHarbor/HarborLife";
    static Vector2 P(float x,float y)=>new Vector2(x,y);
    static Vector2[] OpenWaterTugRoute()=>new[]{P(1555,887),P(1573,882),P(1580,889),P(1572,899),P(1549,901),P(1532,897),P(1530,888),P(1541,882)};

    [MenuItem("RealBuca/Forest Harbor/Move Tugboat to Open Water")]
    public static string MoveTugboatToOpenWater()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("Open MainMenu first.");
        var life=Object.FindAnyObjectByType<ForestHarborLife>(FindObjectsInactive.Include);
        if(life?.tugboat?.picture==null)throw new Exception("Existing tugboat required.");
        Undo.RecordObject(life,"Move tugboat into open water");
        var boat=life.tugboat;boat.route=OpenWaterTugRoute();boat.lapSeconds=40;
        // The route cache is not serialized; discard it when changing the authored path in a live Editor.
        typeof(ForestHarborLife.Boat).GetField("samples",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(boat,null);
        Undo.RecordObject(boat.picture,"Fit tugboat in open water");boat.picture.sizeDelta=P(144,96);
        Undo.RecordObject(boat.root,"Fit tugboat in open water");boat.root.sizeDelta=P(144,96);
        if(boat.reflection!=null){Undo.RecordObject(boat.reflection,"Fit boat reflection");boat.reflection.sizeDelta=P(144*.96f,96*.22f);}
        if(boat.wake!=null){Undo.RecordObject(boat.wake.rectTransform,"Fit boat wake");boat.wake.rectTransform.sizeDelta=P(144*1.2f,32);}
        life.Pose(0);EditorUtility.SetDirty(life);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Tugboat moved to the lower-right open water. Its complete loop stays below the harbor columns and clear of the screen button.";
    }
    [MenuItem("RealBuca/Forest Harbor/Add Water and Moving Boats")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("Open MainMenu first.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||!map.illustratedMenu.forestHarborPresentation)throw new Exception("Forest-harbor map is required.");
        var view=map.illustratedMenu;
        Directory.CreateDirectory("output/harbor-animation");
        const string backup="output/harbor-animation/MainMenu.before-harbor-life.unity";
        if(!File.Exists(backup))File.Copy(scene.path,backup);
        var plate=Texture("WaterWithoutBoats",false);
        var sail=Texture("Sailboat",true);var tug=Texture("TugboatClean",true);
        // A masked repair overlays only the two original painted boats, leaving every level pixel intact.
        var repair=Rect(view.artwork,"HarborWaterRepair",new Vector2(1672,941));repair.SetAsFirstSibling();
        var repairImage=Get<UnityEngine.UI.RawImage>(repair.gameObject);repairImage.texture=plate;repairImage.raycastTarget=false;
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/HarborWaterRepair.mat");
        if(material==null){material=new Material(Shader.Find("BUCA/Harbor Water Repair"));AssetDatabase.CreateAsset(material,Folder+"/HarborWaterRepair.mat");}
        repairImage.material=material;
        var root=Rect(view.artwork,"HarborLife",new Vector2(1672,941));
        root.SetSiblingIndex(repair.GetSiblingIndex()+1);
        var life=Get<ForestHarborLife>(root.gameObject);
        life.sailboat=Boat(root,"Sailboat",sail,P(92,120),new[]{P(1065,704),P(1070,718),P(1100,735),P(1130,744),P(1125,724),P(1095,700)},56,1);
        life.tugboat=Boat(root,"Tugboat",tug,P(144,96),OpenWaterTugRoute(),40,-1);
        const string audioPath="Assets/Audio/Ambience/HarborRockWaves.wav";
        AssetDatabase.ImportAsset(audioPath,ImportAssetOptions.ForceSynchronousImport);
        var audioImporter=(AudioImporter)AssetImporter.GetAtPath(audioPath);
        var settings=audioImporter.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;
        settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;audioImporter.defaultSampleSettings=settings;
        audioImporter.forceToMono=false;audioImporter.loadInBackground=true;audioImporter.SaveAndReimport();
        var sound=root.Find("EveningHarborWater");
        if(sound==null){sound=new GameObject("EveningHarborWater").transform;sound.SetParent(root,false);}
        life.waterAudio=Get<AudioSource>(sound.gameObject);life.waterAudio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
        life.waterAudio.loop=true;life.waterAudio.playOnAwake=false;life.waterAudio.spatialBlend=0;
        life.waterAudio.volume=0;life.waterAudio.priority=180;life.waterAudio.dopplerLevel=0;
        var audio=Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
        if(audio!=null)life.waterAudio.outputAudioMixerGroup=audio.sfxGroup;
        life.waterVolume=.42f;
        ConfigureRiverSound(life);
        BuildHarborBoatSurface.Configure(life);
        life.Pose(0);
        EditorUtility.SetDirty(life);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved: two independent sailing routes with bobbing/wakes; map-only stereo harbor water loop, volume settings, focus and exit cleanup. Edit mode only.";
    }
    [MenuItem("RealBuca/Forest Harbor/Add Woodland River Sound")]
    public static string ApplyRiverSound()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("Open MainMenu first.");
        var life=Object.FindAnyObjectByType<ForestHarborLife>(FindObjectsInactive.Include);
        if(life==null)throw new Exception("Existing harbor life required.");
        Undo.RecordObject(life,"Add woodland river ambience");
        ConfigureRiverSound(life);
        EditorUtility.SetDirty(life);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved: natural 24-second woodland stream recording with a smooth loop, gentle fade-in and shared sound settings.";
    }
    static void ConfigureRiverSound(ForestHarborLife life)
    {
        const string path="Assets/Audio/Ambience/WoodlandRiverWater.wav";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(path) as AudioImporter;
        if(importer==null)throw new Exception("Generate WoodlandRiverWater.wav first.");
        var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;
        settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings=settings;importer.forceToMono=false;
        importer.loadInBackground=true;importer.SaveAndReimport();
        var sound=life.transform.Find("WoodlandRiverWater");
        if(sound==null)
        {
            var go=new GameObject("WoodlandRiverWater");
            Undo.RegisterCreatedObjectUndo(go,"Add river sound");
            sound=go.transform;sound.SetParent(life.transform,false);
        }
        life.riverAudio=Get<AudioSource>(sound.gameObject);
        life.riverAudio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        life.riverAudio.loop=true;life.riverAudio.playOnAwake=false;life.riverAudio.spatialBlend=0;
        life.riverAudio.volume=0;life.riverAudio.priority=185;life.riverAudio.dopplerLevel=0;
        var audio=Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
        if(audio!=null)life.riverAudio.outputAudioMixerGroup=audio.sfxGroup;
        life.riverVolume=.34f;
    }
    static ForestHarborLife.Boat Boat(Transform parent,string name,Texture2D texture,Vector2 size,Vector2[] route,float seconds,float facing)
    {
        var root=Rect(parent,name,size);
        var wakeRect=Rect(root,"Wake",P(size.x*1.2f,32));wakeRect.anchoredPosition=P(0,-10);wakeRect.SetAsFirstSibling();
        var wake=Get<HarborWakeGraphic>(wakeRect.gameObject);wake.raycastTarget=false;
        var picture=Rect(root,"Picture",size);picture.pivot=P(.5f,.12f);
        var image=Get<UnityEngine.UI.RawImage>(picture.gameObject);image.texture=texture;image.raycastTarget=false;
        image.uvRect=VisibleBounds(texture);
        return new ForestHarborLife.Boat{root=root,picture=picture,wake=wake,route=route,lapSeconds=seconds,nativeFacing=facing,bobPixels=name=="Sailboat"?1.4f:.85f};
    }
    static Rect VisibleBounds(Texture2D texture)
    {
        var pixels=texture.GetPixels32();int left=texture.width,right=0,bottom=texture.height,top=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
            if(pixels[y*texture.width+x].a>25){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
        if(right<=left||top<=bottom)throw new Exception("Boat cutout has no visible pixels.");
        left=Math.Max(0,left-3);bottom=Math.Max(0,bottom-3);right=Math.Min(texture.width-1,right+3);top=Math.Min(texture.height-1,top+3);
        return new Rect(left/(float)texture.width,bottom/(float)texture.height,(right-left+1)/(float)texture.width,(top-bottom+1)/(float)texture.height);
    }
    static Texture2D Texture(string name,bool alpha)
    {
        string path=Folder+"/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception("Missing "+path);
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=alpha;importer.isReadable=alpha;
        importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static T Get<T>(GameObject go)where T:Component{var component=go.GetComponent<T>();return component!=null?component:go.AddComponent<T>();}
    static RectTransform Rect(Transform parent,string name,Vector2 size)
    {
        var rect=parent.Find(name) as RectTransform;
        if(rect==null){var go=new GameObject(name,typeof(RectTransform));go.layer=5;rect=(RectTransform)go.transform;rect.SetParent(parent,false);}
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=Vector2.zero;rect.sizeDelta=size;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
        return rect;
    }
}
#endif
