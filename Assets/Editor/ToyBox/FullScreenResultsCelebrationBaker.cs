#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FullScreenResultsCelebrationBaker
{
    public const string PrefabPath="Assets/ToyBoxMenu/Prefabs/ResultsAndLeaderboard.prefab";
    const string Folder="Assets/ToyBoxMenu/ResultsCelebration";
    const string AudioPath="Assets/Audio/Celebration/ResultsFireworksFanfare.wav";

    [MenuItem("RealBuca/Toy Box 3D/Bake Full Screen Results Celebration")]
    public static void BakeMenu() { Debug.Log(Bake()); }

    public static string Bake()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before baking.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/ToyBoxMenu","ResultsCelebration");
        ToyBoxGeometry.Initialize();
        var importer=(AudioImporter)AssetImporter.GetAtPath(AudioPath);
        if(importer==null)throw new InvalidOperationException("Saved celebration audio is missing.");
        var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.90f;
        importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Configure(root.GetComponent<ToyBoxResults3D>());
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            AssetDatabase.SaveAssets();
            return "Saved full-screen celebration: 144 confetti pieces, 80 firework rays, stereo booms and victory chime.";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void Configure(ToyBoxResults3D view)
    {
        foreach(string oldName in new[]{"HappyStarFountains","FullScreenCelebration"})
        {
            var old=view.presentation.transform.Find(oldName);
            if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var stage=new GameObject("FullScreenCelebration").transform;
        stage.SetParent(view.presentation.transform,false);
        var colors=new[]{new Color(1,.70f,.09f),new Color(.035f,.80f,.67f),new Color(1,.91f,.63f),new Color(1,.30f,.17f)};
        var mats=new Material[colors.Length];
        for(int i=0;i<mats.Length;i++)
        {
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Confetti"+i,enableInstancing=true};
            mat.SetColor("_BaseColor",colors[i]);mat.SetFloat("_Metallic",i==0?.45f:.1f);mat.SetFloat("_Smoothness",.42f);
            mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",colors[i]*.24f);
            mats[i]=ToyBoxGeometry.Save(mat,Folder+"/Confetti"+i+".mat");
        }
        var star=ToyBoxGeometry.MapPrism(10,1,.16f,.03f,true);
        var flake=Flake();
        view.celebrationStars=new Transform[144];
        for(int i=0;i<view.celebrationStars.Length;i++)
            view.celebrationStars[i]=Piece("Confetti"+i,stage,i%4==0?star:flake,mats[(i+i/7)%4]);
        var glow=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/HoleInOne/WarmSpark.mat")){name="ResultsFireworkGlow",enableInstancing=true};
        glow.SetColor("_BaseColor",new Color(3.2f,1.8f,.48f,1));
        glow=ToyBoxGeometry.Save(glow,Folder+"/FireworkGlow.mat");
        var streak=Streak();
        view.celebrationRays=new Transform[80];
        for(int i=0;i<view.celebrationRays.Length;i++)view.celebrationRays[i]=Piece("FireworkRay"+i,stage,streak,glow);
        view.holeInOneFanfare=AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
        view.happyCheer=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/HappyYeah.wav");
        view.cheerSource=rootAudio(view.gameObject);
        view.Fit();view.SampleCelebration(0);
    }
    static AudioSource rootAudio(GameObject root)
    {
        var source=root.GetComponent<AudioSource>();if(source==null)source=root.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.volume=1;source.pitch=1;source.priority=64;source.dopplerLevel=0;
        return source;
    }
    static Transform Piece(string name,Transform parent,Mesh mesh,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        go.SetActive(false);return go.transform;
    }
    static Mesh Streak()
    {
        var mesh=new Mesh{name="SavedFireworkStreak"};
        mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
        mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Folder+"/FireworkStreak.asset");
    }
    static Mesh Flake()
    {
        // A bent, two-sided ribbon: eight triangles, no runtime mesh construction.
        var mesh=new Mesh{name="SavedConfettiRibbon"};
        mesh.vertices=new[]{new Vector3(-.40f,0,-.8f),new Vector3(.40f,0,-.8f),new Vector3(-.40f,.18f,0),new Vector3(.40f,.18f,0),new Vector3(-.40f,0,.8f),new Vector3(.40f,0,.8f),
            new Vector3(-.40f,0,-.8f),new Vector3(.40f,0,-.8f),new Vector3(-.40f,.18f,0),new Vector3(.40f,.18f,0),new Vector3(-.40f,0,.8f),new Vector3(.40f,0,.8f)};
        mesh.triangles=new[]{0,2,1,1,2,3,2,4,3,3,4,5,6,7,8,7,9,8,8,9,10,9,11,10};
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Folder+"/ConfettiRibbon.asset");
    }
}
#endif
