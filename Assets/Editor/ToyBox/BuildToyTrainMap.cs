using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/13 - Generate Train Level Selector")]
    public static void GenerateTrainSelector()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders();ToyBoxGeometry.Initialize();LoadWorkshopMaterials();
        surface=ExistingWorkshopMaterial("MaplePlayingSurface");glow=ExistingWorkshopMaterial("MintLight");
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var menu=root.GetComponent<ToyBoxMenuController>();
            UnityEngine.Object.DestroyImmediate(menu.levelsRoot);
            BuildTrainMap(root.transform,menu);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        RenderTrainSelector();
    }
    public static void RenderTrainSelector()
    {
        EditorSceneManager.OpenScene(MenuScene);
        var menu=UnityEngine.Object.FindAnyObjectByType<ToyBoxMenuController>();
        typeof(ToyBoxMenuController).GetMethod("ShowPage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(menu,new[]{Enum.Parse(typeof(ToyBoxMenuController).GetNestedType("Page",System.Reflection.BindingFlags.NonPublic),"Levels")});
        menu.mapCamera.aspect=16f/9f;menu.FitMapCamera(16f/9f);
        ToyBoxMenuValidation.RenderCamera(menu.mapCamera,"Train-Level-Selector",1920,1080);
        Debug.Log("TRAIN_SELECTOR_SAVED: five carriages, thirty level pucks and fourteen saved timer segments.");
    }
    static void BuildTrainMap(Transform root,ToyBoxMenuController menu)
    {
        var map=Group("PuckExpress",root);menu.levelsRoot=map.gameObject;
        var maple=Material("TrainMaple",new Color(.76f,.62f,.45f),.25f,wood.GetTexture("_BaseMap") as Texture2D);
        var ivory=Material("TrainIvory",new Color(.88f,.81f,.66f),.30f);
        var muted=Material("TrainLockedIvory",new Color(.78f,.74f,.66f),.25f);
        var teal=Material("TrainTeal",new Color(.15f,.49f,.52f),.35f);
        var dark=Material("TrainTrackDark",new Color(.29f,.20f,.12f),.28f);
        var paints=new[]{blue,mint,yellow,coral,teal};
        menu.mapPuckMaterial=ivory;menu.mapLockedMaterial=muted;menu.chapterMaterials=paints;menu.currentLevelMaterial=ivory;
        menu.chapterLocks=new GameObject[5];menu.mapSelectionSeconds=30;
        Box("BlueTable",map,new Vector3(0,-.40f,0),new Vector3(20.5f,.85f,15.5f),blue,.28f);
        Box("MapleTop",map,new Vector3(0,.08f,0),new Vector3(20,.20f,15),maple,.17f);
        foreach(int side in new[]{-1,1})
        {
            Box("SideRail",map,new Vector3(side*10,.28f,0),new Vector3(.3f,.42f,15.2f),blue,.13f);
            Box("EndRail",map,new Vector3(0,.28f,side*7.5f),new Vector3(20,.42f,.3f),blue,.13f);
        }
        // One simple oval track, baked as solid ties and two rails.
        var path=new List<Vector3>();
        for(int i=0;i<=160;i++)
        {
            float a=i*Mathf.PI*2/160;
            path.Add(new Vector3(Mathf.Cos(a)*8.85f,.29f,Mathf.Sin(a)*4.45f));
        }
        for(int i=0;i<path.Count-1;i++)
        {
            var forward=(path[i+1]-path[i]).normalized;var side=Vector3.Cross(Vector3.up,forward);
            if(i%2==0){var tie=Box("RailwaySleeper",map,path[i]-Vector3.up*.055f,new Vector3(.95f,.11f,.20f),maple,.025f);tie.transform.rotation=Quaternion.LookRotation(forward);}
            foreach(int sign in new[]{-1,1}) WorkshopRod("WoodenRail",map,path[i]+side*sign*.36f,path[i+1]+side*sign*.36f,.047f,dark);
        }
        var positions=new[]{new Vector3(-5.6f,0,3.6f),new Vector3(0,0,3.6f),new Vector3(5.6f,0,3.6f),new Vector3(-3.2f,0,-4.65f),new Vector3(3.2f,0,-4.65f)};
        menu.levelButtons=new ToyBoxMenuController.PuckButton[30];
        for(int c=0;c<5;c++)
        {
            var car=Group("Chapter_"+(c+1),map,positions[c]);var paint=paints[c];
            Box("LowerChassis",car,new Vector3(0,.39f,0),new Vector3(4.85f,.22f,3.65f),maple,.09f);
            Box("CarriageBase",car,new Vector3(0,.65f,0),new Vector3(4.95f,.50f,3.75f),paint,.19f);
            Box("TrayFloor",car,new Vector3(0,.97f,0),new Vector3(4.6f,.20f,3.40f),paint,.10f);
            foreach(int sign in new[]{-1,1})
            {
                WorkshopRod("RoundedSideLip",car,new Vector3(sign*2.36f,1.57f,-1.7f),new Vector3(sign*2.36f,1.57f,1.7f),.12f,paint);
                WorkshopRod("RoundedEndLip",car,new Vector3(-2.3f,1.57f,sign*1.73f),new Vector3(2.3f,1.57f,sign*1.73f),.12f,paint);
                Box("SideWall",car,new Vector3(sign*2.36f,1.17f,0),new Vector3(.25f,.82f,3.70f),paint,.12f);
                Box("EndWall",car,new Vector3(0,1.17f,sign*1.73f),new Vector3(4.7f,.82f,.25f),paint,.12f);
                foreach(int axle in new[]{-1,1})
                {
                    var wheel=Disc("WoodenWheel",car,new Vector3(axle*1.56f,.53f,sign*1.99f),.48f,.25f,maple,.07f);wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                    var rim=Disc("RecessedWheelFace",car,new Vector3(axle*1.56f,.53f,sign*2.14f),.33f,.08f,paint,.035f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
                    var hub=Disc("WheelHub",car,new Vector3(axle*1.56f,.53f,sign*2.20f),.14f,.07f,ivory,.025f);hub.transform.localRotation=Quaternion.Euler(90,0,0);
                }
                WorkshopRod("Coupling",car,new Vector3(sign*2.47f,.69f,0),new Vector3(sign*2.72f,.69f,0),.14f,dark);
            }
            var plaque=Group("ChapterPlaque",car,new Vector3(0,1.13f,-1.89f));
            Box("WoodPlaque",plaque,Vector3.zero,new Vector3(3.5f,.55f,.13f),maple,.09f);
            Text("CHAPTER "+(c+1),plaque,new Vector3(-.15f,0,-.09f),.26f,ink,false,.08f,.01f);
            var chapterLock=Group("ChapterLock",plaque,new Vector3(1.40f,0,-.16f));
            Box("Lock",chapterLock,Vector3.zero,new Vector3(.23f,.22f,.08f),yellow,.035f);
            var shackle=Ring("Shackle",chapterLock,new Vector3(0,.12f,0),.095f,.035f,yellow,.07f,180);shackle.transform.localRotation=Quaternion.Euler(-90,0,0);
            menu.chapterLocks[c]=chapterLock.gameObject;
            for(int local=0;local<6;local++)
            {
                int index=c*6+local;
                var button=Button(car,(index+1).ToString("00"),new Vector3((local%3-1)*1.40f,1.04f,.79f-(local/3)*1.57f),.49f,ivory,.33f,false);
                UnityEngine.Object.DestroyImmediate(button.cap.Find((index+1).ToString("00")).gameObject);
                Text((index+1).ToString("00"),button.cap,new Vector3(0,.18f,0),.36f,ink,true,.06f,.008f,.005f);
                button.cap.parent.Find("CobaltSocket").GetComponent<Renderer>().sharedMaterial=paint;
                button.lockMark=Group("LockedState",button.cap).gameObject;
                AddLevelMarkers(button);menu.levelButtons[index]=button;
            }
            PrefabUtility.SaveAsPrefabAsset(car.gameObject,Root+"/Prefabs/TrainCarriage"+(c+1)+".prefab");
        }
        var engine=Group("SmallEngine",map,new Vector3(-8.0f,.32f,5.35f));engine.localScale=Vector3.one*.85f;engine.localRotation=Quaternion.Euler(0,90,0);
        Box("EngineBody",engine,new Vector3(0,.70f,0),new Vector3(1.3f,.7f,2.2f),blue,.16f);
        Box("Cab",engine,new Vector3(0,1.4f,.60f),new Vector3(1.30f,1.3f,.90f),blue,.13f);
        Box("Roof",engine,new Vector3(0,2.10f,.60f),new Vector3(1.6f,.22f,1.12f),maple,.10f);
        Box("CabWindow",engine,new Vector3(0,1.53f,.12f),new Vector3(.80f,.52f,.07f),dark,.08f);
        Disc("Chimney",engine,new Vector3(0,1.4f,-.75f),.23f,.63f,maple,.04f);
        foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
        {var w=Disc("EngineWheel",engine,new Vector3(side*.72f,.32f,end*.7f),.38f,.20f,maple,.05f);w.transform.localRotation=Quaternion.Euler(0,0,90);}
        var title=Group("TrainHeader",map,new Vector3(0,2.10f,6.65f));
        Box("HeaderPlaque",title,Vector3.zero,new Vector3(11,1.30f,.30f),maple,.20f);
        Text("CHOOSE A LEVEL",title,new Vector3(0,0,-.18f),.80f,blue,false,.14f,.018f);
        menu.backButton=Button(map,"BACK",new Vector3(-8.5f,.19f,-6.15f),.67f,ivory,.27f,false);
        menu.backButton.cap.Find("BACK").GetComponent<Renderer>().sharedMaterial=ink;
        Box("TimerPedestal",map,new Vector3(0,.50f,-1.20f),new Vector3(3.5f,.65f,.60f),maple,.12f);
        var timer=Group("SelectionTimer",map,new Vector3(0,1.1f,-1.25f));timer.localRotation=Quaternion.Euler(42,0,0);
        Box("TimerFrame",timer,Vector3.zero,new Vector3(6.3f,1.28f,.42f),maple,.13f);
        Box("TimerInset",timer,new Vector3(0,0,-.25f),new Vector3(5.95f,1.0f,.10f),blue,.08f);
        Text("START IN",timer,new Vector3(-1.0f,0,-.32f),.37f,ivory,false,.10f,.01f);
        menu.mapClockSegments=new GameObject[14];
        var spots=new[]{new Vector2(0,.23f),new Vector2(.15f,.115f),new Vector2(.15f,-.115f),new Vector2(0,-.23f),new Vector2(-.15f,-.115f),new Vector2(-.15f,.115f),Vector2.zero};
        for(int digit=0;digit<2;digit++)for(int s=0;s<7;s++)
        {
            bool horizontal=s==0||s==3||s==6;
            menu.mapClockSegments[digit*7+s]=Box("TimerDigit"+digit+"_"+s,timer,new Vector3(1.25f+digit*.70f+spots[s].x*1.35f,spots[s].y*1.35f,-.33f),horizontal?new Vector3(.34f,.085f,.045f):new Vector3(.085f,.23f,.045f),ivory,.02f);
        }
        var backdrop=Material("TrainBackdrop",new Color(.62f,.56f,.46f),.1f);
        Box("QuietBackdrop",map,new Vector3(0,-1,0),new Vector3(150,.15f,150),maple,.01f);
        Box("QuietWall",map,new Vector3(0,5,11),new Vector3(100,12,.20f),Material("TrainStationWall",new Color(.80f,.74f,.64f),.12f),.025f);
        BuildTrainStationDetails(map,maple,ivory);
        var camera=Group("TrainMapCamera",map).gameObject;menu.mapCamera=camera.AddComponent<Camera>();camera.tag="MainCamera";camera.AddComponent<AudioListener>();
        menu.mapCamera.fieldOfView=38;menu.mapCamera.nearClipPlane=.1f;menu.mapCamera.farClipPlane=150;camera.transform.rotation=Quaternion.Euler(42,0,0);
        menu.mapCamera.clearFlags=CameraClearFlags.SolidColor;menu.mapCamera.backgroundColor=new Color(.62f,.56f,.46f);
        var data=camera.AddComponent<UniversalAdditionalCameraData>();data.SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var key=Group("TrainSoftKey",map).gameObject.AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(48,-35,0);key.intensity=.95f;key.color=new Color(1,.97f,.91f);key.shadows=LightShadows.Soft;key.shadowStrength=.65f;
        var fill=Group("TrainFill",map).gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(40,145,0);fill.intensity=.34f;fill.color=new Color(.92f,.96f,1);
        var probe=Group("TrainReflection",map).gameObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=ToyBoxSurfaceBaker.StudioReflection();probe.size=Vector3.one*70;probe.intensity=.50f;
        menu.FitMapCamera(16f/9f);
        PrefabUtility.SaveAsPrefabAsset(map.gameObject,Root+"/Prefabs/TrainLevelSelector.prefab");map.gameObject.SetActive(false);
    }

    static void BuildTrainStationDetails(Transform map,Material maple,Material ivory)
    {
        var details=Group("ToyStationDetails",map);
        var station=Group("SmallWoodenStation",details,new Vector3(12.0f,-.90f,1.6f));
        Box("StationPlinth",station,new Vector3(0,.10f,0),new Vector3(2.1f,.20f,1.70f),maple,.10f);
        Box("StationWalls",station,new Vector3(0,.86f,0),new Vector3(1.75f,1.40f,1.40f),ivory,.07f);
        foreach(int side in new[]{-1,1})
        {
            var roof=Box("SlopedBlueRoof",station,new Vector3(side*.53f,1.73f,0),new Vector3(1.24f,.18f,1.80f),blue,.06f);
            roof.transform.localRotation=Quaternion.Euler(0,0,-side*24);
        }
        Box("Chimney",station,new Vector3(.43f,2.05f,.18f),new Vector3(.30f,.62f,.35f),maple,.035f);
        Box("BlueDoor",station,new Vector3(-.39f,.67f,-.72f),new Vector3(.47f,.95f,.07f),blue,.18f);
        Box("WindowFrame",station,new Vector3(.43f,.98f,-.74f),new Vector3(.53f,.51f,.08f),maple,.035f);
        Box("WindowGlass",station,new Vector3(.43f,.98f,-.79f),new Vector3(.42f,.40f,.035f),blue,.018f);
        Box("WindowVertical",station,new Vector3(.43f,.98f,-.82f),new Vector3(.04f,.40f,.035f),ivory,.01f);
        Box("WindowHorizontal",station,new Vector3(.43f,.98f,-.82f),new Vector3(.42f,.04f,.035f),ivory,.01f);
        var green=Material("TrainCarvedBush",new Color(.27f,.43f,.18f),.28f,wood.GetTexture("_BaseMap") as Texture2D);
        var stone=Material("TrainSmoothStone",new Color(.47f,.45f,.39f),.22f);
        var rounded=PlayroomLeafMesh();
        Vector3[] places={new Vector3(-11.0f,-.75f,1.2f),new Vector3(-11.7f,-.82f,-.3f),new Vector3(-8.5f,.20f,-3.4f)};
        for(int i=0;i<places.Length;i++)
        {
            var bush=MeshObject("CarvedWoodenBush",details,places[i],rounded,green);
            bush.transform.localScale=new Vector3(i==0?2.5f:1.8f,i==0?7:5,3.2f);
        }
        for(int i=0;i<2;i++)
        {
            var pebble=MeshObject("SmallStationStone",details,new Vector3(11.05f+i*.70f,-.82f,-.4f-i*.45f),rounded,i==0?stone:maple);
            pebble.transform.localScale=new Vector3(1.1f,2.2f,1.6f);
        }
        var badge=Group("FrontBucaBadge",details,new Vector3(0,.02f,-7.81f));
        Box("BadgeWood",badge,Vector3.zero,new Vector3(2.55f,.58f,.19f),maple,.13f);
        Text("BUCA",badge,new Vector3(0,0,-.12f),.33f,ink,false,.10f,.01f);
        foreach(int side in new[]{-1,1})
        {
            var bolt=Disc("BadgePeg",badge,new Vector3(side*1.04f,0,-.13f),.09f,.09f,ivory,.03f);
            bolt.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        PrefabUtility.SaveAsPrefabAsset(details.gameObject,Root+"/Prefabs/ToyStationDetails.prefab");
    }
}
