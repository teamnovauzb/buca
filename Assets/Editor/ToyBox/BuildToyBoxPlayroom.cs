using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class BuildToyBoxMainMenu
{
    static void InstallPlayroom(Scene game)
    {
        Scene previous=SceneManager.GetActiveScene(); SceneManager.SetActiveScene(game);
        foreach(var obj in game.GetRootGameObjects())
        {
            var levelManager=obj.GetComponent<LevelManager>(); if(levelManager!=null) levelManager.followStrength=0f;
            if(obj.name=="SavedToyPlayroom") { UnityEngine.Object.DestroyImmediate(obj); continue; }
            else if(obj.name=="Backdrop" || obj.name=="Backdrop Glow" || obj.name=="Starfield" ||
                obj.name=="Ambient Dust" || obj.name=="BoardScenery" || obj.name=="Global Volume") obj.SetActive(false);
            foreach(var light in obj.GetComponentsInChildren<Light>(true)) light.enabled=false;
        }
        var room=Group("SavedToyPlayroom",null,new Vector3(0,-1.7f,0));
        var oak=Material("PlayroomOakFloor",new Color(.65f,.51f,.35f),.24f,ToyBoxSurfaceBaker.WoodGrain(false));
        Box("RoomFloor",room,new Vector3(0,-1.63f,0),new Vector3(70,.16f,70),oak,.04f);
        var floorGrain=ToyBoxSurfaceBaker.WoodGrain(false);
        Material[] planks=new Material[4];
        for(int i=0;i<4;i++) planks[i]=Material("OakPlank"+i,new Color(.55f+i*.018f,.40f+i*.015f,.26f+i*.012f),.32f,floorGrain);
        for(int row=-22;row<=22;row++) for(int column=-3;column<=4;column++)
            Box("IndividualOakPlank",room,new Vector3(row*1.35f,-1.52f,column*5.6f+(row%2)*2.8f),new Vector3(1.325f,.08f,5.57f),planks[Mathf.Abs(row+column)%4],.018f);
        Box("WarmWall",room,new Vector3(0,4.5f,12),new Vector3(100,12,.25f),roomPaint,.04f);
        Box("Skirting",room,new Vector3(0,-1.25f,11.8f),new Vector3(100,.55f,.15f),cream,.035f);
        var shelf=Group("BirchBookshelf",room,new Vector3(-8,-1.5f,7.3f));
        for(int side=-1;side<=1;side+=2)
            Box("Side",shelf,new Vector3(side*1.65f,1.9f,0),new Vector3(.18f,3.8f,1.4f),wood,.06f);
        Box("Back",shelf,new Vector3(0,1.9f,.66f),new Vector3(3.3f,3.8f,.12f),wood,.03f);
        for(int row=0;row<3;row++)
        {
            Box("Shelf",shelf,new Vector3(0,.2f+row*1.7f,0),new Vector3(3.35f,.16f,1.4f),wood,.04f);
            if(row==2) continue;
            for(int b=0;b<7;b++)
            {
                float h=1.0f+(b%3)*.13f;
                var book=Box("PaintedBook",shelf,new Vector3(-1.33f+b*.41f,.30f+row*1.7f+h*.5f,-.08f),new Vector3(.32f,h,.9f),new[]{blue,mint,yellow,coral,cream}[b%5],.025f);
                book.transform.localRotation=Quaternion.Euler(0,0,b==6?-8:0);
            }
        }
        var plant=Group("DottedPotPlant",room,new Vector3(-7.8f,2.25f,7.2f));
        Disc("CreamPot",plant,new Vector3(0,.48f,0),.60f,.95f,cream,.10f);
        Disc("Soil",plant,new Vector3(0,.96f,0),.49f,.04f,ink,.01f);
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI*.2f;
            var dot=Disc("GreenPotDot",plant,new Vector3(Mathf.Sin(a)*.595f,.38f+(i%2)*.31f,Mathf.Cos(a)*.595f),.085f,.015f,mint,.006f);
            dot.transform.localRotation=Quaternion.FromToRotation(Vector3.up,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)));
            var leaf=Box("SolidLeaf",plant,new Vector3(Mathf.Sin(a)*.57f,1.3f+(i%3)*.26f,Mathf.Cos(a)*.57f),Vector3.one,mint,.49f);
            leaf.transform.localScale=new Vector3(.48f,.10f,1.5f);
            leaf.transform.localRotation=Quaternion.Euler(-32,i*36,0);
        }
        Box("YellowToyBall",room,new Vector3(-6.8f,-.99f,4.8f),Vector3.one*1.05f,yellow,.52f);
        var rug=Group("StripedRug",room,new Vector3(7.3f,-1.50f,.6f));
        Box("CreamRug",rug,Vector3.zero,new Vector3(3.8f,.055f,8),cream,.027f);
        for(int i=0;i<6;i++) Box("HoneyStripe",rug,new Vector3(0,.032f,-3.5f+i*1.4f),new Vector3(3.75f,.012f,.55f),yellow,.005f);
        MirrorRug(room);
        var basket=Group("BasketAndTeddy",room,new Vector3(8,-1.5f,7.4f));
        Disc("Basket",basket,new Vector3(0,.65f,0),1.02f,1.3f,wood,.12f);
        for(int i=0;i<8;i++) Ring("WovenBand",basket,new Vector3(0,.12f+i*.15f,0),1.02f,.065f,wood,.07f);
        for(int i=0;i<24;i++)
        {
            float a=i*Mathf.PI/12;
            var weave=Box("BasketWeave",basket,new Vector3(Mathf.Sin(a)*1.02f,.66f,Mathf.Cos(a)*1.02f),new Vector3(.065f,1.12f,.065f),cream,.025f);
        }
        var plush=Material("TeddyFabric",new Color(.57f,.36f,.19f),.08f);
        Box("TeddyBody",basket,new Vector3(0,1.5f,0),new Vector3(1.25f,1.65f,.9f),plush,.44f);
        Box("TeddyHead",basket,new Vector3(0,2.6f,-.12f),Vector3.one*1.22f,plush,.59f);
        for(int side=-1;side<=1;side+=2)
        {
            var arm=Box("TeddyArm",basket,new Vector3(side*.65f,1.76f,-.08f),Vector3.one,plush,.49f);
            arm.transform.localScale=new Vector3(.50f,.95f,.48f); arm.transform.localRotation=Quaternion.Euler(0,0,side*24);
            Box("TeddyFoot",basket,new Vector3(side*.43f,1.12f,-.5f),new Vector3(.60f,.50f,.72f),plush,.24f);
            Box("TeddyEar",basket,new Vector3(side*.53f,3.08f,-.1f),Vector3.one*.44f,plush,.21f);
            Box("TeddyEye",basket,new Vector3(side*.24f,2.70f,-.68f),Vector3.one*.09f,ink,.044f);
        }
        Box("TeddyMuzzle",basket,new Vector3(0,2.43f,-.70f),new Vector3(.56f,.37f,.19f),cream,.09f);
        Box("TeddyNose",basket,new Vector3(0,2.52f,-.81f),new Vector3(.18f,.13f,.09f),ink,.044f);
        StandTeddyBesideBasket(basket);
        Box("WallPrintFrame",room,new Vector3(7.5f,3.4f,11.76f),new Vector3(3.2f,4.2f,.20f),wood,.08f);
        Box("WallPrintPaper",room,new Vector3(7.5f,3.4f,11.64f),new Vector3(2.94f,3.94f,.04f),cream,.015f);
        string[] words={"PLAY","MORE","GOOD","GAMES"};
        for(int i=0;i<4;i++) Text(words[i],room,new Vector3(7.5f,4.6f-i*.7f,11.60f),.36f,yellow);
        var window=Group("WindowAlcove",room,new Vector3(-3.5f,4.1f,11.70f));
        Box("RecessFrame",window,Vector3.zero,new Vector3(4.8f,4.0f,.35f),cream,.07f);
        var glass=Material("SoftDaylightGlass",new Color(.63f,.79f,.83f),.65f);
        glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(.35f,.48f,.51f)*.55f); EditorUtility.SetDirty(glass);
        Box("DaylightPane",window,new Vector3(0,0,-.19f),new Vector3(4.48f,3.68f,.04f),glass,.015f);
        Box("WindowMullion",window,new Vector3(0,0,-.24f),new Vector3(.10f,3.75f,.14f),cream,.02f);
        Box("WindowCrossbar",window,new Vector3(0,0,-.24f),new Vector3(4.55f,.10f,.14f),cream,.02f);
        Box("WindowSill",window,new Vector3(0,-2.02f,-.18f),new Vector3(5.0f,.16f,.70f),wood,.05f);
        var key=Group("WarmWindowLight",room).gameObject.AddComponent<Light>();
        key.type=LightType.Directional; key.transform.rotation=Quaternion.Euler(42,-48,0);
        key.color=new Color(1,.96f,.89f); key.intensity=1.35f; key.shadows=LightShadows.Soft;
        key.shadowStrength=.70f; key.shadowBias=.015f; key.shadowNormalBias=.04f;
        var fill=Group("SoftRoomFill",room).gameObject.AddComponent<Light>();
        fill.type=LightType.Directional; fill.transform.rotation=Quaternion.Euler(40,145,0);
        fill.color=new Color(.90f,.94f,1); fill.intensity=.22f;
        var rim=Group("WindowEdgeBounce",room,new Vector3(5,7,5)).gameObject.AddComponent<Light>();
        rim.type=LightType.Point; rim.color=new Color(.78f,.88f,1); rim.intensity=8; rim.range=18;
        var probe=Group("SavedStudioReflection",room).gameObject.AddComponent<ReflectionProbe>();
        probe.mode=ReflectionProbeMode.Custom; probe.customBakedTexture=ToyBoxSurfaceBaker.StudioReflection();
        probe.size=new Vector3(50,30,50); probe.intensity=1.05f;
        var volume=Group("WarmGrade",room).gameObject.AddComponent<Volume>();
        volume.isGlobal=true; volume.priority=60;
        // Use the saved menu grade without adding a runtime postprocess asset.
        var grade=new VolumeProfile { name="PlayroomGrade" };
        grade.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        var color=grade.Add<ColorAdjustments>(true);
        color.postExposure.Override(-.12f); color.saturation.Override(-8); color.contrast.Override(10);
        var focus=grade.Add<DepthOfField>(true); focus.mode.Override(DepthOfFieldMode.Gaussian);
        focus.gaussianMaxRadius.Override(1.1f); focus.highQualitySampling.Override(true);
        var oldGrade=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Materials/PlayroomGrade.asset");
        if(oldGrade!=null) { foreach(var part in oldGrade.components) UnityEngine.Object.DestroyImmediate(part,true); oldGrade.components.Clear(); }
        volume.sharedProfile=ToyBoxGeometry.Save(grade,Root+"/Materials/PlayroomGrade.asset");
        foreach(var component in volume.sharedProfile.components)
            if(!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
        EditorUtility.SetDirty(volume.sharedProfile);
        foreach(var r in game.GetRootGameObjects())
        {
            var camera=r.GetComponent<Camera>(); if(camera==null) continue;
            ConfigureComfortableCamera(camera);
            focus.active=false; // Keep the whole playable board crisp.
            EditorUtility.SetDirty(focus);
            camera.backgroundColor=new Color(.83f,.75f,.62f);
            camera.GetUniversalAdditionalCameraData().SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());
        }
        ConfigureEnvironment();
        RenderSettings.ambientSkyColor=new Color(.28f,.30f,.34f);
        RenderSettings.ambientEquatorColor=new Color(.23f,.20f,.17f);
        RenderSettings.ambientGroundColor=new Color(.12f,.10f,.08f);
        SavePart(room,"ToyPlayroom");
        SceneManager.SetActiveScene(previous);
    }
    static void ConfigureComfortableCamera(Camera camera)
    {
        Vector3 target=new Vector3(0,-.25f,.25f);
        camera.transform.position=new Vector3(0,10.1f,-19);
        camera.transform.LookAt(target); camera.fieldOfView=38;
        float distance=0,tan=Mathf.Tan(19*Mathf.Deg2Rad);
        // Frame the playing area and cabinet controls, rather than the surrounding room.
        foreach(float x in new[]{-5.4f,5.4f}) foreach(var edge in new[]{new Vector2(-1.55f,-8.0f),new Vector2(.70f,-7.7f),new Vector2(1.4f,7.65f)})
        {
            Vector3 p=Quaternion.Inverse(camera.transform.rotation)*(new Vector3(x,edge.x,edge.y)-target);
            distance=Mathf.Max(distance,Mathf.Abs(p.y)/tan-p.z,Mathf.Abs(p.x)/(tan*1.6f)-p.z);
        }
        camera.transform.position=target-camera.transform.forward*(distance*1.015f);
    }

    [UnityEditor.MenuItem("RealBuca/Toy Box 3D/4 - Apply Comfortable Gameplay Camera")]
    public static void ApplyComfortableCamera()
    {
        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(!Application.isBatchMode && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach(var root in scene.GetRootGameObjects())
        {
            var manager=root.GetComponent<LevelManager>(); if(manager!=null) manager.followStrength=0f;
            var camera=root.GetComponent<Camera>(); if(camera!=null) ConfigureComfortableCamera(camera);
        }
        foreach(var root in scene.GetRootGameObjects())
        {
            var hud=root.GetComponent<ToyBoxGameplayHud>();
            if(hud==null || hud.hints==null) continue;
            hud.hints.transform.localPosition=new Vector3(0,.52f,-7.45f);
            hud.hints.transform.localRotation=Quaternion.Euler(90,0,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(hud.hints.transform);
        }
        var grade=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Materials/PlayroomGrade.asset");
        if(grade!=null && grade.TryGet<DepthOfField>(out var focus)) { focus.active=false; EditorUtility.SetDirty(focus); }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("COMFORTABLE_CAMERA_APPLIED: centered, lower perspective, tighter board framing, depth blur disabled.");
    }

}
