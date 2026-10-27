#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Installs just the dock crane; the railway, boats, trees and controls are preserved.</summary>
public static class BuildForestHarborCrane
{
    const string Folder = "Assets/TwilightMap/ForestHarbor/Crane";
    const int Layer = 27;

    [MenuItem("RealBuca/Forest Harbor/Add Working Cargo Crane")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map?.illustratedMenu == null || !map.illustratedMenu.forestHarborPresentation)
            throw new Exception("Forest harbor map required.");
        Directory.CreateDirectory("output/harbor-crane");
        const string backup = "output/harbor-crane/MainMenu.before-crane.unity";
        if (!File.Exists(backup)) File.Copy(scene.path,backup);
        var art = map.illustratedMenu.artwork;
        var oldRig = map.transform.Find("WorkingHarborCrane");
        if (oldRig != null) Object.DestroyImmediate(oldRig.gameObject);
        var oldArt = art.Find("WorkingHarborCraneArt");
        if (oldArt != null) Object.DestroyImmediate(oldArt.gameObject);

        string platePath = Folder+"/HarborWithoutCrane.png";
        AssetDatabase.ImportAsset(platePath,ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(platePath) as TextureImporter;
        if (importer == null) throw new Exception("Crane clean plate missing.");
        importer.textureType=TextureImporterType.Default; importer.mipmapEnabled=false;
        importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp; importer.SaveAndReimport();
        var root = Rect(art,"WorkingHarborCraneArt",new Vector2(1672,941),new Vector2(836,470.5f));
        root.SetSiblingIndex(art.Find("HarborLife").GetSiblingIndex()+1);
        var repair = Rect(root,"RestoredDock",new Vector2(1672,941),new Vector2(836,470.5f));
        var repairImage = repair.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        repairImage.raycastTarget=false; repairImage.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(platePath);
        var sourceMaterial = art.GetComponent<UnityEngine.UI.RawImage>().material;
        var repairMaterial = new Material(sourceMaterial) { name="CraneDockRepair" };
        repairMaterial.SetFloat("_RepairOnly",3); repairMaterial.SetFloat("_UseForestPlate",0);
        repairMaterial.SetFloat("_FlowTime",0);
        repairImage.material=ToyBoxGeometry.Save(repairMaterial,Folder+"/CraneDockRepair.mat");
        repair.gameObject.AddComponent<ForestHarborWaterFlow>().Configure(repairImage);

        Shadow(root,"BaseContact",new Vector2(1388,542),new Vector2(60,25),.23f);
        var loadShadow = Shadow(root,"LoadContact",new Vector2(1398,572),new Vector2(32,14),.2f);
        var rightCargo=Rect(root,"OriginalRightCargo",new Vector2(1672,941),new Vector2(836,470.5f))
            .gameObject.AddComponent<HarborCraneForeground>();
        rightCargo.artwork=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TwilightMap/ForestHarbor/ForestHarborBackground.png");
        rightCargo.leftCargo=false; rightCargo.raycastTarget=false;
        var pictureRect = Rect(root,"MovingCrane",new Vector2(300,240),new Vector2(1388,485.3f));
        var picture = pictureRect.gameObject.AddComponent<UnityEngine.UI.RawImage>(); picture.raycastTarget=false;
        var foreground=Rect(root,"OriginalForegroundCargo",new Vector2(1672,941),new Vector2(836,470.5f))
            .gameObject.AddComponent<HarborCraneForeground>();
        foreground.artwork=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TwilightMap/ForestHarbor/ForestHarborBackground.png");
        foreground.rightCargo=false; foreground.raycastTarget=false;
        var rig = Node(map.transform,"WorkingHarborCrane");
        rig.localPosition=new Vector3(0,-1000,0);
        var crane=rig.gameObject.AddComponent<ForestHarborCrane>(); crane.picture=picture; crane.cargoShadow=loadShadow;
        var cameraRoot=Node(rig,"CraneCamera");
        var camera=cameraRoot.gameObject.AddComponent<Camera>(); crane.craneCamera=camera;
        var offset=new Vector3(4,5,-8);
        cameraRoot.localPosition=new Vector3(0,1.4f,0)+offset;
        cameraRoot.localRotation=Quaternion.LookRotation(-offset);
        camera.orthographic=true; camera.orthographicSize=2.5f*rig.lossyScale.x;
        camera.nearClipPlane=.03f; camera.farClipPlane=20; camera.cullingMask=1<<Layer;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
        camera.allowHDR=false; camera.allowMSAA=true; camera.depth=-10;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(Folder+"/WorkingCrane.renderTexture");
        if(rt==null)
        {
            rt=new RenderTexture(900,720,24,RenderTextureFormat.ARGB32) { name="Working harbor crane",antiAliasing=4 };
            AssetDatabase.CreateAsset(rt,Folder+"/WorkingCrane.renderTexture");
        }
        camera.targetTexture=rt; camera.aspect=1.25f; picture.texture=rt;

        ToyBoxGeometry.Initialize();
        var navy=Material("CraneNavy",new Color(.035f,.27f,.57f),.4f);
        var blue=Material("CraneEdge",new Color(.055f,.36f,.69f),.4f);
        var steel=Material("Chain",new Color(.12f,.11f,.09f),.25f);
        var brass=Material("WarmBrass",new Color(.86f,.55f,.20f),.3f);
        var wood=Material("CrateWood",new Color(.84f,.52f,.21f),.12f);
        wood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));
        wood.SetFloat("_GrainStrength",.20f); wood.SetFloat("_TextureTint",0);
        wood.SetTextureScale("_BaseMap",new Vector2(3,3));
        var trim=Material("CrateEdges",new Color(.98f,.67f,.32f),.16f);
        Box(rig,"Fixed square footing",new Vector3(0,.10f,0),new Vector3(.94f,.2f,.82f),.06f,navy);
        Box(rig,"Fixed pedestal",new Vector3(0,.40f,0),new Vector3(.74f,.50f,.65f),.065f,navy);
        Box(rig,"Pedestal inset",new Vector3(0,.41f,-.331f),new Vector3(.46f,.31f,.025f),.012f,blue);
        Part(rig,"Turntable bearing",ToyBoxGeometry.Disc(.32f,.10f,.025f),new Vector3(0,.69f,0),Quaternion.identity,steel);
        var turret=Node(rig,"SwivellingJib"); crane.turntable=turret;
        Box(turret,"Blue upright",new Vector3(0,1.69f,0),new Vector3(.38f,2.02f,.34f),.075f,navy);
        Box(turret,"Upright edge",new Vector3(-.14f,1.69f,-.18f),new Vector3(.065f,1.91f,.025f),.01f,blue);
        var tip=new Vector3(1.10f,3.08f,0);
        Beam(turret,"Blue upper jib",new Vector3(0,2.62f,0),tip,.18f,.24f,navy);
        Beam(turret,"Golden jib inset",new Vector3(.10f,2.62f,-.135f),new Vector3(1.04f,3.02f,-.135f),.10f,.025f,brass);
        Beam(turret,"Diagonal brace",new Vector3(.10f,.94f,.015f),new Vector3(1.08f,2.99f,.015f),.105f,.14f,navy);
        Beam(turret,"Tension stay",new Vector3(.03f,2.21f,-.13f),new Vector3(1.06f,3.07f,-.13f),.032f,.025f,steel);
        var pulley=Node(turret,"WindingPulley"); pulley.localPosition=tip; crane.pulley=pulley;
        Part(pulley,"Blue sheave",ToyBoxGeometry.Disc(.205f,.16f,.025f),Vector3.zero,Quaternion.Euler(90,0,0),blue);
        Part(pulley,"Brass spindle",ToyBoxGeometry.Disc(.075f,.185f,.015f),Vector3.zero,Quaternion.Euler(90,0,0),brass);
        Box(pulley,"Pulley spoke",new Vector3(.10f,0,-.09f),new Vector3(.10f,.025f,.015f),.006f,brass);
        crane.boomTip=Node(turret,"CableExit"); crane.boomTip.localPosition=tip+new Vector3(0,-.12f,0);
        crane.cable=Part(rig,"HoistCable",ToyBoxGeometry.Disc(.012f,1,.003f),Vector3.zero,Quaternion.identity,steel);
        crane.chainLinks=new Transform[34];
        var linkMesh=ToyBoxGeometry.Torus(.027f,.008f);
        for(int i=0;i<crane.chainLinks.Length;i++)
            crane.chainLinks[i]=Part(rig,"ChainLink_"+i,linkMesh,Vector3.zero,Quaternion.identity,steel);
        var cargo=Node(rig,"SuspendedWoodenCrate"); crane.cargo=cargo;
        Box(cargo,"Wooden crate",Vector3.zero,new Vector3(.60f,.67f,.56f),.05f,wood);
        foreach(float side in new[]{-1f,1f})
        {
            foreach(float x in new[]{-.26f,.26f})
                Box(cargo,"Crate upright",new Vector3(x,0,side*.282f),new Vector3(.055f,.65f,.025f),.008f,trim);
            foreach(float y in new[]{-.29f,.29f})
                Box(cargo,"Crate crosspiece",new Vector3(0,y,side*.282f),new Vector3(.56f,.05f,.025f),.008f,trim);
            Beam(cargo,"Wood brace",new Vector3(-.23f,-.24f,side*.297f),new Vector3(.23f,.24f,side*.297f),.045f,.018f,wood);
        }
        Part(cargo,"Lifting eye",ToyBoxGeometry.Torus(.065f,.019f),new Vector3(0,.385f,0),Quaternion.Euler(90,0,0),brass);
        crane.hook=Node(cargo,"CableHook"); crane.hook.localPosition=new Vector3(0,.435f,0);
        crane.Pose(0);
        EditorUtility.SetDirty(crane); EditorUtility.SetDirty(wood);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved a working 3D harbor crane: 24-second hoist, slew, set-down and return cycle; linked cable, rotating pulley, restrained cargo sway and contact shadows.";
    }

    static Transform Node(Transform parent,string name)
    {
        var go=new GameObject(name); go.layer=Layer; go.transform.SetParent(parent,false); return go.transform;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 pixel)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.layer=5; go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform; rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.sizeDelta=size; rect.anchoredPosition=new Vector2(pixel.x-836,470.5f-pixel.y); return rect;
    }
    static HarborCraneShadow Shadow(Transform parent,string name,Vector2 pixel,Vector2 size,float alpha)
    {
        var shadow=Rect(parent,name,size,pixel).gameObject.AddComponent<HarborCraneShadow>();
        shadow.raycastTarget=false; shadow.color=new Color(.025f,.045f,.07f,alpha); return shadow;
    }
    static Material Material(string name,Color color,float smoothness)
    {
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name+".mat");
        if(mat==null){mat=new Material(Shader.Find("BUCA/Woodland Toy"));AssetDatabase.CreateAsset(mat,Folder+"/"+name+".mat");}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smoothness);mat.SetFloat("_GrainStrength",0);
        EditorUtility.SetDirty(mat);return mat;
    }
    static Transform Part(Transform parent,string name,Mesh mesh,Vector3 position,Quaternion rotation,Material material)
    {
        var t=Node(parent,name);t.localPosition=position;t.localRotation=rotation;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
    }
    static void Box(Transform parent,string name,Vector3 position,Vector3 size,float radius,Material material)
        => Part(parent,name,ToyBoxGeometry.RoundedBox(size,radius),position,Quaternion.identity,material);
    static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,float depth,Material material)
        => Part(parent,name,ToyBoxGeometry.RoundedBox(new Vector3(width,Vector3.Distance(a,b),depth),Mathf.Min(width,depth)*.2f),
            (a+b)*.5f,Quaternion.FromToRotation(Vector3.up,b-a),material);
}
#endif
