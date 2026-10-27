#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Authors the chosen woodland and moonlight miniature engines, including real crank gear.</summary>
public static partial class BuildSuitcaseTrainPair
{
    const string Folder = "Assets/TwilightMap/Locomotives";
    const int Layer = 28;
    static Material brass, gold, dark, green, blue, red, oak, copper, glow, steel, coal;
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static Quaternion AlongX = Quaternion.Euler(0, 0, -90), AlongZ = Quaternion.Euler(90, 0, 0);

    [MenuItem("RealBuca/Twilight Railway/Build Woodland and Moonlight Trains")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Author trains in Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || map.illustratedMenu == null || map.train == null) throw new Exception("Existing suitcase railway required.");
        Directory.CreateDirectory(Folder + "/Meshes"); Directory.CreateDirectory(Folder + "/Materials");
        meshes.Clear(); Materials();
        Transform old = map.train;
        var root = Group("WoodlandAndMoonlightLocomotives", map.transform);
        root.localPosition = old.localPosition; root.localRotation = old.localRotation; root.localScale = old.localScale;
        var mechanism = root.gameObject.AddComponent<SuitcaseTrainMechanism>();
        try
        {
            mechanism.woodland = Engine(root, false);
            mechanism.moonlight = Engine(root, true);
            var steam = new List<Transform>();
            for (int i = 0; i < 5; i++)
            {
                var puff = Sphere("Steam" + i, root, Vector3.zero, Vector3.one * .2f, Mat("Steam", new Color(.78f,.8f,.76f), 0, .1f));
                puff.gameObject.SetActive(false); steam.Add(puff);
            }
            map.train = root; map.trainMechanism = mechanism;
            map.wheels = mechanism.woodland.wheels.Concat(mechanism.moonlight.wheels).Select(w => w.pivot).ToArray();
            map.steam = steam.ToArray(); map.steamOrigin = new Vector3(1.05f, 3.25f, 0);
            float crossing = (map.stops[29].distance + map.stops[30].distance) * .5f;
            mechanism.Pose(map.RouteDistance, root.localScale.x, map.RouteDistance >= crossing);
            old.gameObject.SetActive(false);
            if (old.GetComponent<SuitcaseTrainMechanism>() != null) Object.DestroyImmediate(old.gameObject);
            else old.name = "LegacyJourneyLocomotive";
            var rig = map.GetComponent<SuitcaseLocomotiveView>();
            rig.verticalSize = 2.8f;
            rig.centerOffset = new Vector3(-.38f, 1.2f, 0);
            rig.cameraOffset = new Vector3(2.2f, 5.2f, -10.3923f);
            rig.Frame();
            var texture = rig.trainCamera.targetTexture;
            texture.Release(); texture.width = 900; texture.height = 600; texture.antiAliasing = 4; texture.Create();
            EditorUtility.SetDirty(texture);
            map.illustratedMenu.locomotive.sizeDelta = new Vector2(192,128);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/WorldTrainPair.prefab");
            EditorUtility.SetDirty(map); EditorUtility.SetDirty(rig); EditorUtility.SetDirty(map.illustratedMenu);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "Saved two detailed 3D trains, 20 distance-driven spoked wheels and four complete crank/link/piston mechanisms. Play mode was not entered.";
        }
        catch
        {
            if (map.train != root) Object.DestroyImmediate(root.gameObject);
            throw;
        }
    }

    static void Materials()
    {
        brass=Mat("AntiqueBrass",new Color(.69f,.38f,.095f),.82f,.66f);
        gold=Mat("PolishedGold",new Color(.94f,.65f,.25f),.78f,.7f);
        dark=Mat("BlackenedSteel",new Color(.035f,.028f,.023f),.65f,.4f);
        green=Mat("WoodlandEnamel",new Color(.027f,.16f,.085f),.3f,.58f);
        blue=Mat("MidnightEnamel",new Color(.018f,.055f,.16f),.4f,.63f);
        red=Mat("OxbloodSpokes",new Color(.28f,.035f,.018f),.25f,.48f);
        oak=Mat("HoneyOak",new Color(.4f,.16f,.045f),.05f,.38f);
        var grain=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png");
        if(grain!=null){oak.SetTexture("_BaseMap",grain); oak.SetTextureScale("_BaseMap",new Vector2(.8f,3));EditorUtility.SetDirty(oak);}
        copper=Mat("CopperFunnel",new Color(.54f,.19f,.055f),.8f,.62f);
        glow=Mat("AmberLampGlass",new Color(1,.51f,.10f),.08f,.52f);
        glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.46f,.06f)*1.6f);EditorUtility.SetDirty(glow);
        steel=Mat("PolishedPistonSteel",new Color(.64f,.68f,.7f),.9f,.66f);
        coal=Mat("Coal",new Color(.018f,.021f,.026f),.05f,.18f);
    }
    static Material Mat(string name,Color color,float metallic,float smoothness)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);
        material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }

    static SuitcaseTrainMechanism.Engine Engine(Transform parent,bool city)
    {
        var root=Group(city?"World2MoonlightExpress":"World1WoodlandSteamer",parent);
        var body=Group("StaticDetails",root);
        Material enamel=city?blue:green, chassis=city?blue:red;
        // Rounded layered footplate, cylindrical boiler and riveted smokebox.
        Box("Footplate",body,new Vector3(-.15f,1.02f,0),new Vector3(4.6f,.22f,1.38f),.06f,chassis);
        Box("BrassFootplateLip",body,new Vector3(-.15f,1.15f,0),new Vector3(4.65f,.07f,1.43f),.025f,brass);
        Disc("Boiler",body,new Vector3(.45f,1.7f,0),.58f,2.5f,enamel,AlongX,.08f);
        foreach(float x in new[]{-.73f,-.08f,.65f,1.45f})
            Torus("BoilerBand",body,new Vector3(x,1.7f,0),.585f,.033f,gold,AlongX);
        Disc("Smokebox",body,new Vector3(1.7f,1.7f,0),.61f,.24f,dark,AlongX,.035f);
        Disc("FrontDoor",body,new Vector3(1.85f,1.7f,0),.51f,.11f,enamel,AlongX,.035f);
        Torus("FrontDoorBrass",body,new Vector3(1.93f,1.7f,0),.50f,.045f,brass,AlongX);
        for(int i=0;i<14;i++)
        {float a=i*Mathf.PI*2/14;Sphere("DoorRivet",body,new Vector3(1.935f,1.7f+Mathf.Cos(a)*.54f,Mathf.Sin(a)*.54f),Vector3.one*.055f,gold);}
        // Turned flared chimney, open black mouth, and a domed steam collector.
        var chimney=Lathe("FlaredChimney",new[]{V(.18f,0),V(.19f,.08f),V(.13f,.15f),V(.14f,.42f),V(.30f,.72f),V(.33f,.78f),V(.29f,.85f)},city?blue:copper);
        Place(chimney,body,new Vector3(1.05f,2.23f,0));
        foreach(var rim in new[]{V(.20f,.06f),V(.325f,.77f),V(.29f,.85f)})
            Torus("ChimneyRim",body,new Vector3(1.05f,2.23f+rim.y,0),rim.x,.025f,gold,Quaternion.identity);
        Disc("OpenChimney",body,new Vector3(1.05f,3.07f,0),.255f,.016f,dark,Quaternion.identity,.003f);
        var dome=Lathe("SteamDome",new[]{V(.29f,0),V(.30f,.06f),V(.25f,.11f),V(.25f,.32f),V(.22f,.44f),V(.13f,.53f),V(0,.57f)},brass);
        Place(dome,body,new Vector3(-.12f,2.22f,0));
        Torus("DomeCollar",body,new Vector3(-.12f,2.3f,0),.27f,.026f,gold,Quaternion.identity);
        if(!city)
        {
            // Acorn cap and its diagonal engraved lattice identify the woodland engine.
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                Bar("AcornCapEngraving",body,new Vector3(-.12f+Mathf.Cos(a)*.21f,2.66f,Mathf.Sin(a)*.21f),
                    new Vector3(-.12f+Mathf.Cos(a+.65f)*.1f,2.76f,Mathf.Sin(a+.65f)*.1f),.014f,copper);
            }
        }
        Disc("DomeFinial",body,new Vector3(-.12f,2.83f,0),.055f,.12f,gold,Quaternion.identity,.022f);
        // Arched cab with inset illuminated windows, roof wood/enamel, and fine brass framing.
        Box("CabLower",body,new Vector3(-1.63f,1.48f,0),new Vector3(1.37f,.62f,1.35f),.075f,enamel);
        Box("CabBack",body,new Vector3(-2.27f,2.05f,0),new Vector3(.12f,1.52f,1.34f),.035f,enamel);
        foreach(float side in new[]{-1f,1f})
        {
            float z=side*.67f;
            Box("CabWindowWall",body,new Vector3(-1.62f,2.13f,z),new Vector3(1.37f,1.25f,.10f),.035f,enamel);
            Pane("CabWindowBezel",body,new Vector3(-1.62f,2.21f,z+side*.07f),.91f,1.02f,.045f,gold);
            Pane("CabWindowShadow",body,new Vector3(-1.62f,2.21f,z+side*.10f),.79f,.90f,.035f,dark);
            Pane("CabAmberWindow",body,new Vector3(-1.62f,2.21f,z+side*.13f),.67f,.78f,.025f,glow);
            Box("WindowMullion",body,new Vector3(-1.62f,2.18f,z+side*.15f),new Vector3(.035f,.77f,.035f),.012f,brass);
            Box("WindowCrossbar",body,new Vector3(-1.62f,2.19f,z+side*.16f),new Vector3(.65f,.03f,.03f),.01f,brass);
            Box("CabBaseInlay",body,new Vector3(-1.62f,1.24f,z+side*.03f),new Vector3(1.22f,.035f,.04f),.01f,gold);
            foreach(float x in new[]{-2.22f,-1.02f})
                Box("CabBrassUpright",body,new Vector3(x,2.01f,z+side*.06f),new Vector3(.045f,1.5f,.045f),.016f,brass);
            Emblem(body,new Vector3(-1.62f,1.47f,z+side*.08f),.20f,city,side);
            Box("CabStep",body,new Vector3(-1.9f,.84f,side*.84f),new Vector3(.72f,.08f,.32f),.025f,dark);
            Box("CabStepLip",body,new Vector3(-1.9f,.9f,side*.99f),new Vector3(.72f,.04f,.04f),.014f,gold);
            // Handrails follow the boiler sides rather than floating above it.
            Bar("BoilerHandrail",body,new Vector3(-.82f,1.91f,side*.61f),new Vector3(1.48f,1.91f,side*.61f),.025f,gold);
            foreach(float x in new[]{-.75f,.2f,1.4f})
                Bar("HandrailStanchion",body,new Vector3(x,1.91f,side*.53f),new Vector3(x,1.91f,side*.65f),.022f,brass);
            Disc("PistonCylinder",body,new Vector3(1.68f,.65f,side*.58f),.22f,.65f,chassis,AlongX,.04f);
            foreach(float x in new[]{1.37f,1.98f})Torus("PistonCylinderBand",body,new Vector3(x,.65f,side*.58f),.22f,.03f,brass,AlongX);
            Bar("SliderGuide",body,new Vector3(1.17f,.49f,side*.86f),new Vector3(2.18f,.49f,side*.86f),.025f,steel);
            Bar("SliderGuide",body,new Vector3(1.17f,.81f,side*.86f),new Vector3(2.18f,.81f,side*.86f),.025f,steel);
            for(int i=0;i<16;i++)Sphere("FootplateRivet",body,new Vector3(-2.27f+i*.285f,1.065f,side*.70f),Vector3.one*.04f,gold);
        }
        Roof(body,new Vector3(-1.65f,2.79f,0),1.65f,1.65f,city?blue:oak);
        foreach(float x in new[]{-2.47f,-.83f})
        {
            Vector3 last=new Vector3(x,2.79f,-.83f);
            for(int i=1;i<=20;i++)
            {float z=Mathf.Lerp(-.83f,.83f,i/20f);var p=new Vector3(x,2.79f+.22f*(1-z*z/(.83f*.83f)),z);Bar("RoofBrassEdge",body,last,p,.027f,gold);last=p;}
        }
        // Front headlamp and the small roof bell provide a clear front/back silhouette.
        Disc("HeadlampHousing",body,new Vector3(1.94f,2.18f,0),.22f,.32f,brass,AlongX,.035f);
        Disc("HeadlampLens",body,new Vector3(2.115f,2.18f,0),.175f,.025f,glow,AlongX,.01f);
        Torus("HeadlampRim",body,new Vector3(2.13f,2.18f,0),.19f,.025f,gold,AlongX);
        var bell=Lathe("Bell",new[]{V(.16f,0),V(.14f,.035f),V(.07f,.15f),V(.06f,.22f),V(0,.25f)},gold);
        Place(bell,body,new Vector3(-.66f,2.4f,0));
        Bar("BellBracket",body,new Vector3(-.66f,2.39f,-.20f),new Vector3(-.66f,2.78f,-.20f),.025f,brass);
        Bar("BellBracket",body,new Vector3(-.66f,2.78f,-.20f),new Vector3(-.66f,2.78f,.20f),.025f,brass);
        // Slatted, tapered cowcatcher with bumper beams.
        for(int i=0;i<9;i++)
        {
            float z=Mathf.Lerp(-.62f,.62f,i/8f);
            Bar("CowcatcherSlat",body,new Vector3(1.96f,.91f,z*.75f),new Vector3(2.56f,.28f,z),.037f,city?gold:red);
        }
        Bar("CowcatcherLowerRim",body,new Vector3(2.56f,.28f,-.69f),new Vector3(2.56f,.28f,.69f),.05f,brass);
        foreach(float side in new[]{-1f,1f})Disc("Buffer",body,new Vector3(2.08f,1.01f,side*.52f),.105f,.22f,brass,AlongX,.03f);
        // Short tender, carrying cut logs in World 1 and coal beneath stars in World 2.
        Box("TenderFrame",body,new Vector3(-3.05f,.70f,0),new Vector3(1.15f,.18f,1.25f),.045f,chassis);
        Box("TenderBody",body,new Vector3(-3.05f,1.15f,0),new Vector3(1.07f,.77f,1.12f),.065f,enamel);
        Box("TenderTopLip",body,new Vector3(-3.05f,1.55f,0),new Vector3(1.15f,.08f,1.22f),.025f,brass);
        Box("TenderLoadShadow",body,new Vector3(-3.05f,1.6f,0),new Vector3(.98f,.06f,1.02f),.025f,dark);
        Bar("TenderCoupling",body,new Vector3(-2.6f,.78f,0),new Vector3(-2.2f,.78f,0),.06f,steel);
        foreach(float side in new[]{-1f,1f})
        {
            Box("TenderGoldFrame",body,new Vector3(-3.05f,1.13f,side*.575f),new Vector3(.93f,.61f,.025f),.035f,gold);
            Box("TenderEnamelPanel",body,new Vector3(-3.05f,1.13f,side*.596f),new Vector3(.84f,.51f,.025f),.025f,enamel);
            Emblem(body,new Vector3(-3.05f,1.13f,side*.626f),.23f,city,side);
        }
        var random=new System.Random(city?210:110);
        for(int i=0;i<(city?22:7);i++)
        {
            if(city)Sphere("Coal",body,new Vector3(-3.45f+(float)random.NextDouble()*.8f,1.66f+(float)random.NextDouble()*.15f,-.44f+(float)random.NextDouble()*.88f),Vector3.one*(.14f+(float)random.NextDouble()*.08f),coal);
            else Disc("OakLog",body,new Vector3(-3.3f+(i%3)*.25f,1.7f+(i/3)*.13f,-.06f+(i%2)*.14f),.10f,.86f,oak,AlongZ,.02f);
        }
        if(city)
        {
            var moon=MeshObject("CrescentDoorEmblem",body,new Vector3(1.994f,1.65f,0),Crescent(),gold);
            moon.localRotation=Quaternion.Euler(0,90,0);moon.localScale=Vector3.one*.25f;
        }
        Combine(body,city?"MoonlightBody":"WoodlandBody");

        var wheels=new List<SuitcaseTrainMechanism.Wheel>();
        var gear=new List<SuitcaseTrainMechanism.SideGear>();
        foreach(float side in new[]{-1f,1f})
        {
            float phase=side<0?0:90;
            var rear=Wheel(root,wheels,new Vector3(-1.1f,.62f,side*.66f),.62f,phase,side,chassis,true);
            var front=Wheel(root,wheels,new Vector3(.35f,.62f,side*.66f),.62f,phase,side,chassis,true);
            Wheel(root,wheels,new Vector3(2.02f,.31f,side*.61f),.31f,phase,side,chassis,false);
            foreach(float x in new[]{-3.37f,-2.77f})Wheel(root,wheels,new Vector3(x,.27f,side*.59f),.27f,phase,side,chassis,false);
            var coupling=Box("CouplingRod",root,Vector3.zero,new Vector3(1,.09f,.055f),.024f,brass);
            var drive=Box("PistonConnectingRod",root,Vector3.zero,new Vector3(1,.07f,.045f),.02f,steel);
            var slider=Box("Crosshead",root,Vector3.zero,new Vector3(.20f,.18f,.11f),.03f,gold);
            gear.Add(new SuitcaseTrainMechanism.SideGear{rearPin=rear,frontPin=front,couplingRod=coupling,driveRod=drive,
                crosshead=slider,sliderHeight=.65f,rodLength=1.48f});
        }
        return new SuitcaseTrainMechanism.Engine{body=root.gameObject,wheels=wheels.ToArray(),gear=gear.ToArray()};
    }

    static Transform Wheel(Transform root,List<SuitcaseTrainMechanism.Wheel> wheels,Vector3 position,float radius,float phase,float side,Material paint,bool crank)
    {
        var pivot=Group("Wheel"+wheels.Count.ToString("00"),root);pivot.localPosition=position;
        var visual=Group("SpokedWheel",pivot);
        Torus("SteelTyre",visual,Vector3.zero,radius-.025f,.035f,steel,AlongZ);
        Torus("GoldWheelRim",visual,new Vector3(0,0,side*.045f),radius-.055f,.032f,gold,AlongZ);
        Torus("PaintedInnerRim",visual,Vector3.zero,radius-.095f,.038f,paint,AlongZ);
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI/5;
            var spoke=Box("Spoke",visual,new Vector3(Mathf.Cos(a)*radius*.49f,Mathf.Sin(a)*radius*.49f,0),new Vector3(radius*.72f,.055f,.07f),.02f,paint);
            spoke.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
        }
        Disc("AxleHub",visual,new Vector3(0,0,side*.04f),radius*.23f,.17f,brass,AlongZ,.03f);
        Disc("AxleCap",visual,new Vector3(0,0,side*.13f),radius*.14f,.045f,gold,AlongZ,.015f);
        Combine(visual,(paint==blue?"Blue":"Red")+"Wheel"+radius.ToString("F2")+(side<0?"Left":"Right"));
        wheels.Add(new SuitcaseTrainMechanism.Wheel{pivot=pivot,radius=radius,phaseDegrees=phase});
        if(!crank)return null;
        var pin=Group("CrankPin",pivot);pin.localPosition=new Vector3(.245f,0,side*.19f);
        Disc("CrankPinBoss",pin,Vector3.zero,.095f,.085f,gold,AlongZ,.023f);
        Disc("CrankPinCap",pin,new Vector3(0,0,side*.07f),.058f,.045f,brass,AlongZ,.016f);
        return pin;
    }

    static void Emblem(Transform parent,Vector3 p,float size,bool city,float side)
    {
        if(city)
        {
            var star=MeshObject("CompassStar",parent,p,Star(),gold);star.localScale=Vector3.one*size;
            return;
        }
        Bar("LeafStem",parent,p+Vector3.down*size*.7f,p+Vector3.up*size*.9f,.013f,gold);
        for(int i=0;i<5;i++)
        {
            float y=(i-2)*size*.30f;
            foreach(float sign in new[]{-1f,1f})
            {
                var leaf=Sphere("OakLeaf",parent,p+new Vector3(sign*size*.31f,y,0),new Vector3(size*.43f,size*.75f,.035f),gold);
                leaf.localRotation=Quaternion.Euler(0,0,-sign*42);
            }
        }
    }

    static Transform Group(string name,Transform parent)
    {var t=new GameObject(name).transform;t.gameObject.layer=Layer;t.SetParent(parent,false);return t;}
    static Transform MeshObject(string name,Transform parent,Vector3 p,Mesh mesh,Material material)
    {
        var t=Group(name,parent);t.localPosition=p;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
        return t;
    }
    static Transform Box(string name,Transform parent,Vector3 p,Vector3 size,float radius,Material material) => MeshObject(name,parent,p,BoxMesh(size,radius),material);
    static Transform Disc(string name,Transform parent,Vector3 p,float radius,float height,Material material,Quaternion rotation,float bevel)
    {
        var t=MeshObject(name,parent,p,TurnedMesh(new[]{V(0,-height/2),V(radius-bevel,-height/2),V(radius,-height/2+bevel),V(radius,height/2-bevel),V(radius-bevel,height/2),V(0,height/2)}),material);
        t.localRotation=rotation;return t;
    }
    static Transform Torus(string name,Transform parent,Vector3 p,float radius,float tube,Material material,Quaternion rotation)
    {var t=MeshObject(name,parent,p,TorusMesh(radius,tube),material);t.localRotation=rotation;return t;}
    static Transform Lathe(string name,Vector2[] profile,Material material) => MeshObject(name,null,Vector3.zero,TurnedMesh(profile),material);
    static void Place(Transform t,Transform parent,Vector3 p){t.SetParent(parent,false);t.localPosition=p;}
    static Transform Sphere(string name,Transform parent,Vector3 p,Vector3 size,Material material)
    {
        var t=MeshObject(name,parent,p,SphereMesh(),material);t.localScale=size;return t;
    }
    static void Bar(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material material)
    {var t=Disc(name,parent,(a+b)*.5f,radius,Vector3.Distance(a,b),material,Quaternion.FromToRotation(Vector3.up,b-a),radius*.4f);}
    static void Pane(string name,Transform parent,Vector3 p,float width,float height,float depth,Material material)
    {
        var points=new List<Vector2>{new Vector2(-width/2,-height/2),new Vector2(width/2,-height/2)};
        for(int i=0;i<=20;i++){float a=i*Mathf.PI/20;points.Add(new Vector2(Mathf.Cos(a)*width/2,height/2-width/2+Mathf.Sin(a)*width/2));}
        MeshObject(name,parent,p,Extrude(points.ToArray(),depth),material);
    }
    static Vector2 V(float radius,float height)=>new Vector2(radius,height);
    static void Combine(Transform root,string prefix)
    {
        var renderers=root.GetComponentsInChildren<MeshRenderer>();
        var groups=renderers.GroupBy(r=>r.sharedMaterial).ToArray();
        foreach(var group in groups)
        {
            var mesh=new Mesh{name=prefix+"-"+group.Key.name,indexFormat=IndexFormat.UInt32};
            var parts=group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
            mesh.CombineMeshes(parts,true,true);mesh.RecalculateBounds();
            mesh=Save(mesh,Folder+"/Meshes/"+mesh.name+".asset");
            MeshObject(mesh.name,root,Vector3.zero,mesh,group.Key);
        }
        // Only the temporary static parts are removed; wheel pivots and gear stay separate.
        foreach(var renderer in renderers)Object.DestroyImmediate(renderer.gameObject);
    }
    static T Save<T>(T value,string path) where T:Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing==null){AssetDatabase.CreateAsset(value,path);return value;}
        if(value is Mesh source && existing is Mesh target)
        {
            // CopySerialized alone can leave stale GPU buffers after the vertex count
            // changes. Rebuild the streams so rebakes render the saved geometry.
            target.Clear();target.name=source.name;target.indexFormat=source.indexFormat;
            target.vertices=source.vertices;target.normals=source.normals;
            target.tangents=source.tangents;target.uv=source.uv;target.uv2=source.uv2;
            target.colors32=source.colors32;target.subMeshCount=source.subMeshCount;
            for(int i=0;i<source.subMeshCount;i++)target.SetIndices(source.GetIndices(i),source.GetTopology(i),i,false);
            target.bounds=source.bounds;target.UploadMeshData(false);
        }
        else EditorUtility.CopySerialized(value,existing);
        Object.DestroyImmediate(value);EditorUtility.SetDirty(existing);return existing;
    }
}
#endif
