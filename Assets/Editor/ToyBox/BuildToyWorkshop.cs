using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class BuildToyBoxMainMenu
{
    public static void ValidateWorkshopBatch()
    {
        GenerateToyWorkshop();
        ToyBoxStartupValidation.RunBatch();
    }
    [MenuItem("RealBuca/Toy Box 3D/10 - Regenerate Main Menu Room")]
    public static void GenerateToyWorkshop()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders(); ToyBoxGeometry.Initialize(); LoadWorkshopMaterials();
        var contents=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var old=contents.transform.Find("ToyRoomScenery");
            var controller=contents.GetComponent<ToyBoxMenuController>();
            var scenerySlots=new List<int>();
            if(controller.menuScenery!=null) for(int i=0;i<controller.menuScenery.Length;i++)
                if(controller.menuScenery[i]==null || (old!=null && controller.menuScenery[i]==old.gameObject)) scenerySlots.Add(i);
            if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            BuildRoom(contents.transform);
            foreach(int i in scenerySlots) controller.menuScenery[i]=contents.transform.Find("ToyRoomScenery").gameObject;
            controller.menuFramingPadding=1.32f;
            controller.menuVerticalFramingOffset=-.30f;
            PolishWorkshopLighting(contents.transform,controller);
            AddSurfaceShine(contents.transform,controller);
            controller.FitHomeCamera(16f/9f);
            PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScene);
        var menu=UnityEngine.Object.FindAnyObjectByType<ToyBoxMenuController>();
        Validate(menu.gameObject);
        var scenery=menu.transform.Find("ToyRoomScenery");
        int vertices=0;
        foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
        {
            if(!AssetDatabase.Contains(filter.sharedMesh)) throw new Exception("Unsaved workshop mesh.");
            vertices+=filter.sharedMesh.vertexCount;
        }
        menu.SetClock(30);
        ToyBoxMenuValidation.RenderCamera(menu.menuCamera,"Workshop-Main-Menu",1920,1080);
        Debug.Log("WORKSHOP_SAVED: solid scene geometry, "+scenery.GetComponentsInChildren<MeshRenderer>().Length+" material batches, "+vertices+" vertices; original controls preserved.");
    }

    static Material ExistingWorkshopMaterial(string name)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
        if(material==null) throw new InvalidOperationException("Generate the main menu first: missing "+name);
        return material;
    }
    static void PolishWorkshopLighting(Transform root,ToyBoxMenuController controller)
    {
        var rig=root.Find("CameraAndLighting");
        var key=rig.Find("WarmKey").GetComponent<Light>();
        key.transform.localRotation=Quaternion.Euler(48,-48,0);
        key.intensity=.85f; key.shadowStrength=.85f;
        rig.Find("CoolFill").GetComponent<Light>().intensity=.12f;
        rig.GetComponentInChildren<ReflectionProbe>().intensity=.45f;
        controller.menuCamera.transform.rotation=Quaternion.Euler(29,0,0);
    }
    static void LoadWorkshopMaterials()
    {
        blue=ExistingWorkshopMaterial("CobaltPaint"); coral=ExistingWorkshopMaterial("CoralPaint");
        yellow=ExistingWorkshopMaterial("HoneyYellow"); mint=ExistingWorkshopMaterial("MintPaint");
        wood=ExistingWorkshopMaterial("NaturalBirch"); cream=ExistingWorkshopMaterial("Ivory");
        ink=ExistingWorkshopMaterial("NavyLettering"); roomPaint=ExistingWorkshopMaterial("WarmRoomPaint");
        floorPaint=ExistingWorkshopMaterial("WarmFloorPaint");
    }

    static void BuildWorkshopDetails(Transform room)
    {
        var originalWood=wood;
        wood=Material("WorkshopHoneyOak",new Color(.76f,.56f,.35f),.34f,originalWood.GetTexture("_BaseMap") as Texture2D);
        wood.SetTexture("_BumpMap",originalWood.GetTexture("_BumpMap"));
        wood.SetFloat("_BumpScale",.20f); wood.EnableKeyword("_NORMALMAP"); EditorUtility.SetDirty(wood);
        UnityEngine.Object.DestroyImmediate(room.Find("WarmFloor").gameObject);
        UnityEngine.Object.DestroyImmediate(room.Find("WarmWall").gameObject);
        var panel=Material("WorkshopBluePanel",new Color(.40f,.51f,.53f),.25f);
        var darkWood=Material("WorkshopEndGrain",new Color(.37f,.23f,.115f),.22f);
        var leaf=Material("WorkshopLeaf",new Color(.17f,.34f,.14f),.30f);
        var bulb=Material("WorkshopWarmBulb",new Color(1,.87f,.55f),.25f);
        bulb.EnableKeyword("_EMISSION"); bulb.SetColor("_EmissionColor",new Color(1,.65f,.25f)*2.2f); EditorUtility.SetDirty(bulb);
        Box("WoodFloorBase",room,new Vector3(0,-.27f,0),new Vector3(45,.3f,40),darkWood);
        for(int row=0;row<29;row++) for(int col=0;col<8;col++)
            Box("IndividualFloorboard",room,new Vector3(-21+col*6+(row%2)*3,-.095f,-17+row*1.25f),new Vector3(5.975f,.12f,1.22f),wood,.025f);
        Box("WallBacking",room,new Vector3(0,6,8.15f),new Vector3(45,14,.25f),darkWood);
        Box("PaintedWainscot",room,new Vector3(0,1.45f,7.95f),new Vector3(44,3.05f,.22f),panel);
        for(int i=-22;i<=22;i++) Box("RaisedPanelStile",room,new Vector3(i,1.45f,7.77f),new Vector3(.065f,2.9f,.10f),panel,.018f);
        Box("MapleChairRail",room,new Vector3(0,3.05f,7.72f),new Vector3(44,.18f,.38f),wood,.035f);
        Box("MapleSkirting",room,new Vector3(0,.10f,7.72f),new Vector3(44,.24f,.35f),wood,.035f);
        MeshObject("PerforatedMaplePegboard",room,new Vector3(0,7.2f,7.80f),WorkshopPegboard(),wood);
        // Real raised wall toys, spaced above the existing logo.
        var shapes=Group("WallShapeToys",room,new Vector3(0,6.3f,7.15f));
        var circle=Disc("CoralCircle",shapes,new Vector3(-3.7f,0,0),.72f,.45f,coral,.10f); circle.transform.localRotation=Quaternion.Euler(90,0,0);
        var star=MeshObject("GoldStar",shapes,new Vector3(-1.25f,0,0),ToyBoxGeometry.MapPrism(10,.9f,.45f,.08f,true),yellow); star.transform.localRotation=Quaternion.AngleAxis(-24,Vector3.forward)*Quaternion.Euler(90,0,0);
        var triangle=MeshObject("BlueTriangle",shapes,new Vector3(1.3f,0,0),ToyBoxGeometry.MapPrism(3,.95f,.45f,.08f),blue); triangle.transform.localRotation=Quaternion.Euler(90,0,0);
        var green=Disc("MintCircle",shapes,new Vector3(3.8f,0,0),.76f,.45f,mint,.10f); green.transform.localRotation=Quaternion.Euler(90,0,0);
        WorkshopShelf(room,new Vector3(-8.6f,5.05f,6.8f),6.2f);
        WorkshopShelf(room,new Vector3(8.6f,5.6f,6.8f),6.5f);
        WorkshopTrain(room,new Vector3(6.3f,5.81f,6.50f));
        for(int i=0;i<3;i++)
        {
            var arch=Ring("ShelfRainbow",room,new Vector3(-8.3f,5.22f,6.5f),.55f+i*.18f,.15f,new[]{mint,yellow,coral}[i],.22f,180);
            arch.transform.localRotation=Quaternion.Euler(-90,0,0);
        }
        Box("ToyHouse",room,new Vector3(-10.2f,5.65f,6.5f),new Vector3(.85f,.85f,.7f),cream);
        var roof=MeshObject("ToyHouseRoof",room,new Vector3(-10.2f,6.25f,6.5f),ToyBoxGeometry.MapPrism(3,.70f,.80f,.035f),coral); roof.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int side=-1;side<=1;side+=2)
        {
            var table=Group("SideWorkBench",room,new Vector3(side*11.2f,0,2.6f));
            Box("ThickMapleTop",table,new Vector3(0,2.08f,0),new Vector3(4.0f,.32f,3.0f),wood,.12f);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Box("BenchLeg",table,new Vector3(x*1.6f,.95f,z*1.13f),new Vector3(.30f,2.1f,.30f),wood);
            WorkshopLamp(table,new Vector3(side*.65f,2.31f,.25f),side==1?mint:coral,bulb,side);
            WorkshopPlant(room,new Vector3(side*12.0f,7.5f,6.75f),leaf);
            WorkshopShelf(room,new Vector3(side*12.0f,7.06f,6.8f),1.65f);
        }
        // Hollow mug and individual wooden pencils on the right-hand bench.
        Ring("PencilCup",room,new Vector3(10.8f,2.8f,2.15f),.34f,.085f,cream,.85f);
        Disc("CupBottom",room,new Vector3(10.8f,2.41f,2.15f),.32f,.08f,cream);
        var handle=Ring("CupHandle",room,new Vector3(10.35f,2.8f,2.15f),.27f,.08f,cream,.11f); handle.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<6;i++)
        {
            var pencil=Disc("WoodenPencil",room,new Vector3(10.8f+(i%3-1)*.14f,3.22f,2.15f+(i/3)*.15f),.045f,1.25f,i%2==0?yellow:wood,.012f);
            pencil.transform.localRotation=Quaternion.Euler(i*3,0,(i-3)*6);
        }
        var stool=Group("WorkshopStool",room,new Vector3(12.0f,0,-1.6f));
        Disc("StoolSeat",stool,new Vector3(0,1.27f,0),.92f,.22f,wood);
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3;
            WorkshopRod("StoolLeg",stool,new Vector3(Mathf.Cos(a)*.55f,1.2f,Mathf.Sin(a)*.55f),new Vector3(Mathf.Cos(a)*.75f,0,Mathf.Sin(a)*.75f),.13f,wood);
        }
        CombineWorkshopMeshes(room);
        wood=originalWood;
    }

    static void WorkshopShelf(Transform parent,Vector3 at,float width)
    {
        var shelf=Group("MapleShelf",parent,at);
        Box("ShelfBoard",shelf,Vector3.zero,new Vector3(width,.32f,2.2f),wood,.10f);
        for(int side=-1;side<=1;side+=2)
        {
            Box("BracketBack",shelf,new Vector3(side*(width*.5f-.6f),-.43f,.65f),new Vector3(.20f,.8f,.22f),wood);
            WorkshopRod("DiagonalBracket",shelf,new Vector3(side*(width*.5f-.6f),-.75f,.55f),new Vector3(side*(width*.5f-.6f),-.16f,-.6f),.10f,wood);
        }
    }
    static void WorkshopTrain(Transform parent,Vector3 at)
    {
        var train=Group("WoodenToyTrain",parent,at);
        for(int car=0;car<3;car++)
        {
            var body=Group("TrainCar",train,new Vector3(car*1.65f,0,0));
            Box("Chassis",body,new Vector3(0,.25f,0),new Vector3(1.45f,.20f,.72f),wood);
            if(car==0)
            {
                var boiler=Disc("RoundEngineBoiler",body,new Vector3(-.22f,.66f,0),.35f,.95f,cream,.08f);
                boiler.transform.localRotation=Quaternion.Euler(0,0,90);
                var nose=Disc("EngineNose",body,new Vector3(-.73f,.66f,0),.28f,.07f,wood,.025f);
                nose.transform.localRotation=Quaternion.Euler(0,0,90);
            }
            else Box("CarBody",body,new Vector3(0,.65f,0),new Vector3(1.2f,.65f,.68f),car==1?blue:mint,.15f);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                var wheel=Disc("WoodenWheel",body,new Vector3(x*.46f,.20f,z*.43f),.23f,.14f,car==0?coral:yellow,.035f); wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                var hub=Disc("WheelHub",body,new Vector3(x*.46f,.20f,z*.51f),.075f,.025f,wood);hub.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            if(car==0)
            {
                Box("Cab",body,new Vector3(.28f,1.04f,0),new Vector3(.52f,.55f,.63f),cream);
                Box("Roof",body,new Vector3(.28f,1.36f,0),new Vector3(.75f,.15f,.90f),coral);
                Disc("Chimney",body,new Vector3(-.40f,1.05f,0),.13f,.42f,coral);
                Box("CabWindow",body,new Vector3(.28f,1.06f,-.327f),new Vector3(.27f,.27f,.025f),ink,.035f);
            }
        }
    }
    static void WorkshopRod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material material)
    {
        var rod=Disc(name,parent,(a+b)*.5f,radius,Vector3.Distance(a,b),material,.015f);
        rod.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void WorkshopLamp(Transform parent,Vector3 at,Material paint,Material bulb,int side)
    {
        var lamp=Group("ArticulatedDeskLamp",parent,at);
        Disc("WeightedBase",lamp,Vector3.zero,.6f,.16f,paint);
        Vector3 joint=new Vector3(side*.25f,1.35f,.1f),top=new Vector3(-side*.55f,2.1f,0);
        WorkshopRod("LowerArm",lamp,new Vector3(0,.1f,0),joint,.075f,wood);
        WorkshopRod("UpperArm",lamp,joint,top,.075f,wood);
        var screw=Disc("ArmJoint",lamp,joint,.16f,.18f,paint); screw.transform.localRotation=Quaternion.Euler(90,0,0);
        var shade=Group("HollowLampShade",lamp,top); shade.localRotation=Quaternion.Euler(-12,0,-side*22);
        MeshObject("MetalShade",shade,Vector3.zero,WorkshopShade(),paint);
        Disc("WarmDiffuser",shade,new Vector3(0,-.31f,0),.52f,.025f,bulb,.008f);
        var light=Group("WarmPoolLight",shade,new Vector3(0,-.48f,0)).gameObject.AddComponent<Light>();
        light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);
        light.spotAngle=85;light.innerSpotAngle=45;light.color=new Color(1,.77f,.46f);light.intensity=1.2f;light.range=5;light.shadows=LightShadows.None;
    }
    static void WorkshopPlant(Transform parent,Vector3 at,Material leaf)
    {
        var plant=Group("TrailingShelfPlant",parent,at);
        Ring("PlantPot",plant,Vector3.zero,.42f,.1f,cream,.65f);
        Disc("PotSoil",plant,new Vector3(0,.2f,0),.32f,.05f,ink);
        for(int stem=0;stem<3;stem++)
        {
            Vector3 last=new Vector3(0,.22f,0);
            for(int i=0;i<10;i++)
            {
                var p=new Vector3(Mathf.Sin(i*.65f+stem)*.28f+(stem-1)*.30f,.35f-i*.30f,-.12f-i*.055f);
                WorkshopRod("VineStem",plant,last,p,.025f,leaf);last=p;
                var l=MeshObject("CurvedSolidLeaf",plant,p+new Vector3((i%2==0?1:-1)*.16f,0,-.035f),WorkshopLeafMesh(),leaf);
                l.transform.localRotation=Quaternion.Euler(70+i*3,stem*21,i%2==0?35:-35);
            }
        }
    }

    static Mesh WorkshopShade()
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var normals=new List<Vector3>();
        for(int i=0;i<40;i++)
        {
            float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;
            for(int side=0;side<2;side++)
            {
                float r=.62f-side*.055f,t=.23f-side*.055f;
                var p=new[]{new Vector3(Mathf.Cos(a)*r,-.35f,Mathf.Sin(a)*r),new Vector3(Mathf.Cos(b)*r,-.35f,Mathf.Sin(b)*r),new Vector3(Mathf.Cos(b)*t,.35f,Mathf.Sin(b)*t),new Vector3(Mathf.Cos(a)*t,.35f,Mathf.Sin(a)*t)};
                WorkshopQuad(vertices,triangles,p[0],p[side==0?3:1],p[2],p[side==0?1:3]);
                for(int n=vertices.Count-4;n<vertices.Count;n++)
                {
                    var point=vertices[n];
                    normals.Add(new Vector3(point.x,new Vector2(point.x,point.z).magnitude*(.39f/.70f),point.z).normalized*(side==0?1:-1));
                }
            }
        }
        var mesh=new Mesh{name="WorkshopHollowLampShade"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetNormals(normals);mesh.RecalculateBounds();return ToyBoxGeometry.Save(mesh,Root+"/Meshes/WorkshopHollowLampShade.asset");
    }
    static Mesh WorkshopLeafMesh()
    {
        const string path=Root+"/Meshes/WorkshopCurvedLeaf.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null)return existing;
        var v=new List<Vector3>();var t=new List<int>();
        const int rows=12,columns=8;
        for(int side=0;side<2;side++)
        {
            int offset=v.Count;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float along=y/(float)rows,across=x/(float)columns*2-1;
                float width=Mathf.Sin(along*Mathf.PI)*.14f;
                v.Add(new Vector3(across*width,.075f*Mathf.Sin(along*Mathf.PI)+across*across*.045f+(side==0?.012f:-.012f),along*.48f-.24f));
            }
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                int a=offset+y*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;
                if(side==0)t.AddRange(new[]{a,c,b,b,c,d});else t.AddRange(new[]{a,b,c,b,d,c});
            }
        }
        var mesh=new Mesh{name="WorkshopCurvedLeaf"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,path);
    }
    static void WorkshopQuad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    { int start=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{start,start+1,start+2,start,start+2,start+3}); }
    static Mesh WorkshopPegboard()
    {
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
        const float step=.65f,r=.057f;
        for(int x=0;x<56;x++) for(int y=0;y<14;y++) for(int i=0;i<16;i++)
        {
            float a=(i*22.5f+45)*Mathf.Deg2Rad,b=((i+1)*22.5f+45)*Mathf.Deg2Rad;
            Vector2 da=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),db=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
            Vector2 ca=new Vector2((x-27.5f)*step,(y-6.5f)*step);
            Vector2 oa=ca+da*(step*.5f/Mathf.Max(Mathf.Abs(da.x),Mathf.Abs(da.y))),ob=ca+db*(step*.5f/Mathf.Max(Mathf.Abs(db.x),Mathf.Abs(db.y)));
            Vector2 ia=ca+da*r,ib=ca+db*r;
            WorkshopQuad(v,t,new Vector3(oa.x,oa.y,-.1f),new Vector3(ia.x,ia.y,-.1f),new Vector3(ib.x,ib.y,-.1f),new Vector3(ob.x,ob.y,-.1f));
            WorkshopQuad(v,t,new Vector3(ia.x,ia.y,-.1f),new Vector3(ia.x,ia.y,.1f),new Vector3(ib.x,ib.y,.1f),new Vector3(ib.x,ib.y,-.1f));
        }
        foreach(var p in v)uv.Add(new Vector2(p.x,p.y)*.2f);
        var mesh=new Mesh{name="WorkshopPerforatedPegboard",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return ToyBoxGeometry.Save(mesh,Root+"/Meshes/WorkshopPerforatedPegboard.asset");
    }
    static void CombineWorkshopMeshes(Transform room,string prefix="WorkshopBatch_")
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();
        var filters=room.GetComponentsInChildren<MeshFilter>();
        foreach(var filter in filters)
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(!groups.TryGetValue(renderer.sharedMaterial,out var list)) groups[renderer.sharedMaterial]=list=new List<CombineInstance>();
            list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=room.worldToLocalMatrix*filter.transform.localToWorldMatrix});
        }
        foreach(var pair in groups)
        {
            var mesh=new Mesh{name=prefix+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            mesh=ToyBoxGeometry.Save(mesh,Root+"/Meshes/"+mesh.name+".asset");MeshObject(mesh.name,room,Vector3.zero,mesh,pair.Key);
        }
        foreach(var filter in filters) { UnityEngine.Object.DestroyImmediate(filter.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(filter); }
    }
}
