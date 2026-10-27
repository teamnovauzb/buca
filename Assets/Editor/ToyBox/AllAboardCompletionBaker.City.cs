#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class AllAboardCompletionBaker
{
    static Material stone,stoneLight,stoneBlue,roofBlue,cityDark,windowWarm,windowSoft,leaves,leavesLight,paving,cloud;
    static Mesh cityCube,sphere,archWindow,roofMesh;
    static GameObject Q(Transform p,string n,Vector3 v,Vector3 size,Material m)
    {var g=P(p,n,cityCube,m,v);g.transform.localScale=size;return g;}
    static GameObject Ball(Transform p,string n,Vector3 v,Vector3 size,Material m)
    {var g=P(p,n,sphere,m,v);g.transform.localScale=size;return g;}
    static Mesh CityMesh(string n,List<Vector3> v,List<int> t,List<Vector2> uv=null)
    {
        var m=new Mesh{name=n,indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetTriangles(t,0);
        if(uv!=null)m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();if(uv!=null)m.RecalculateTangents();
        return ToyBoxGeometry.Save(m,Folder+"/City_"+n+".asset");
    }
    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
    {
        int i=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>0)t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});else t.AddRange(new[]{i,i+2,i+1,i,i+3,i+2});
    }
    static Mesh Sphere()
    {
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();const int rings=12,segments=20;
        for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++)
        {
            float a=x*Mathf.PI*2/segments,b=y*Mathf.PI/rings;
            v.Add(new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))*.5f);uv.Add(new Vector2(x/(float)segments,y/(float)rings));
            if(y<rings&&x<segments){int i=y*(segments+1)+x;t.AddRange(new[]{i,i+1,i+segments+2,i,i+segments+2,i+segments+1});}
        }
        var mesh=CityMesh("Sphere",v,t,uv);var normals=new Vector3[v.Count];for(int i=0;i<v.Count;i++)normals[i]=v[i].normalized;mesh.normals=normals;EditorUtility.SetDirty(mesh);return mesh;
    }
    static Mesh CityCube()
    {
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();var normals=new List<Vector3>();
        float[] samples={-.5f,-.46f,.46f,.5f};
        foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            var u=n.x!=0?Vector3.forward:Vector3.right;var w=Vector3.Cross(n,u);int first=v.Count;
            for(int y=0;y<4;y++)for(int x=0;x<4;x++)
            {
                var p=n*.5f+u*samples[x]+w*samples[y];var q=new Vector3(Mathf.Clamp(p.x,-.46f,.46f),Mathf.Clamp(p.y,-.46f,.46f),Mathf.Clamp(p.z,-.46f,.46f));
                var normal=(p-q).normalized;v.Add(q+normal*.04f);normals.Add(normal);uv.Add(new Vector2(samples[x],samples[y]));
                if(x<3&&y<3){int a=first+y*4+x;t.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
            }
        }
        var mesh=CityMesh("BeveledCube",v,t,uv);mesh.SetNormals(normals);EditorUtility.SetDirty(mesh);return mesh;
    }
    static Mesh WindowArch()
    {
        var outline=new List<Vector2>{new Vector2(-.5f,-.8f),new Vector2(.5f,-.8f)};
        for(int i=0;i<=16;i++){float a=i*Mathf.PI/16;outline.Add(new Vector2(Mathf.Cos(a)*.5f,.3f+Mathf.Sin(a)*.5f));}
        var v=new List<Vector3>();var t=new List<int>();int count=outline.Count;
        foreach(var p in outline)v.Add(new Vector3(p.x,p.y,-.05f));foreach(var p in outline)v.Add(new Vector3(p.x,p.y,.05f));
        for(int i=1;i<count-1;i++){t.AddRange(new[]{0,i+1,i,count,count+i,count+i+1});}
        for(int i=0;i<count;i++){int j=(i+1)%count;t.AddRange(new[]{i,j,count+j,i,count+j,count+i});}
        return CityMesh("ArchedWindow",v,t);
    }
    static Mesh Mansard()
    {
        var v=new List<Vector3>();var t=new List<int>();var bottom=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
        for(int i=0;i<4;i++){int j=(i+1)%4;var a=bottom[i];var b=bottom[j];var c=b*.64f+Vector3.up;var d=a*.64f+Vector3.up;Quad(v,t,a,b,c,d,((a+b)*.5f+Vector3.up*.2f).normalized);}
        Quad(v,t,bottom[0]*.64f+Vector3.up,bottom[1]*.64f+Vector3.up,bottom[2]*.64f+Vector3.up,bottom[3]*.64f+Vector3.up,Vector3.up);
        return CityMesh("MansardRoof",v,t);
    }
    static void Window(Transform face,Vector3 at,float w,float h,bool lit)
    {
        P(face,"CarvedWindowArch",archWindow,stoneLight,at).transform.localScale=new Vector3(w,h/1.6f,1.6f);
        P(face,"WindowRecess",archWindow,cityDark,at+Vector3.back*.10f).transform.localScale=new Vector3(w*.81f,h*.86f/1.6f,1);
        P(face,"AmberWindow",archWindow,lit?windowWarm:windowSoft,at+new Vector3(0,.01f,-.16f)).transform.localScale=new Vector3(w*.67f,h*.75f/1.6f,.7f);
        Q(face,"WindowMullion",at+new Vector3(0,-.05f,-.205f),new Vector3(.045f,h*.70f,.04f),stoneLight);
        Q(face,"WindowCrossbar",at+new Vector3(0,-h*.1f,-.205f),new Vector3(w*.69f,.055f,.04f),stoneLight);
        Q(face,"StoneWindowSill",at+new Vector3(0,-h*.5f,-.16f),new Vector3(w+.22f,.10f,.36f),stoneLight);
        if(lit)CityLight(face,at+Vector3.back*.55f,2.8f,2.6f);
    }
    static void House(Transform city,float x,float z,float width,float height,float depth,float yaw,int style)
    {
        var h=G("Townhouse",city,new Vector3(x,-3.55f,z));h.localRotation=Quaternion.Euler(0,yaw,0);
        var mat=style%3==0?stone:style%3==1?stoneBlue:stoneLight;
        Q(h,"MasonryBody",new Vector3(0,height*.5f,0),new Vector3(width,height,depth),mat);
        Q(h,"Foundation",new Vector3(0,.22f,0),new Vector3(width+.18f,.45f,depth+.16f),stone);
        Q(h,"Cornice",new Vector3(0,height-.02f,0),new Vector3(width+.30f,.24f,depth+.30f),stoneLight);
        for(int row=0;row<height/.42f;row++)
        {
            float y=.28f+row*.42f;
            Q(h,"MasonryCourse",new Vector3(0,y,-depth*.5f-.018f),new Vector3(width-.05f,.016f,.015f),stoneBlue);
            for(int col=0;col<width/.78f;col++)
            {float jointX=-width*.5f+.35f+col*.78f+(row%2)*.37f;if(jointX<width*.5f-.08f)Q(h,"BrickJoint",new Vector3(jointX,y+.21f,-depth*.5f-.018f),new Vector3(.015f,.40f,.015f),stoneBlue);}
        }
        var roof=P(h,"Mansard",roofMesh,roofBlue,new Vector3(0,height,0));roof.transform.localScale=new Vector3(width+.45f,1.24f,depth+.42f);
        Q(h,"Ridge",new Vector3(0,height+1.25f,0),new Vector3(width*.69f,.15f,depth*.67f),roofBlue);
        int floors=Mathf.Clamp(Mathf.FloorToInt(height/1.65f),2,4);int cols=width>3.9f?3:2;
        for(int floor=0;floor<floors;floor++)
        {
            float y=1.02f+floor*(height-.7f)/floors;
            if(floor>0)Q(h,"FloorMolding",new Vector3(0,y-.71f,0),new Vector3(width+.08f,.10f,depth+.08f),stoneLight);
            for(int col=0;col<cols;col++)Window(h,new Vector3((col-(cols-1)*.5f)*width/(cols+.25f),y,-depth*.5f-.06f),.63f,1.02f,(floor+col+style)%4!=0);
        }
        var side=G("SideFacade",h,new Vector3(width*.5f+.05f,0,0));side.localRotation=Quaternion.Euler(0,-90,0);
        for(int floor=0;floor<floors;floor++)for(int col=0;col<2;col++)Window(side,new Vector3((col-.5f)*depth*.49f,1.02f+floor*(height-.7f)/floors,0),.61f,1.02f,(floor+col+style)%3!=0);
        foreach(int sign in new[]{-1,1})for(int floor=0;floor<floors*2;floor++)Q(h,"CornerStone",new Vector3(sign*(width*.5f-.07f),.52f+floor*(height-.5f)/(floors*2),-depth*.5f-.05f),new Vector3(.29f,.20f,.17f),stoneLight);
        // A dormer and chimney make the roof a silhouette with real volume.
        Q(h,"Dormer",new Vector3(0,height+.46f,-depth*.39f),new Vector3(.82f,.85f,.65f),mat);
        Window(h,new Vector3(0,height+.45f,-depth*.39f-.37f),.48f,.63f,true);
        Q(h,"DormerCap",new Vector3(0,height+.93f,-depth*.39f),new Vector3(1,.15f,.87f),roofBlue);
        Q(h,"Chimney",new Vector3(width*.28f,height+1.37f,depth*.15f),new Vector3(.36f,.85f,.41f),stone);
        Q(h,"ChimneyLip",new Vector3(width*.28f,height+1.81f,depth*.15f),new Vector3(.47f,.13f,.53f),stoneLight);
        if(style%2==0)
        {
            var awning=G("ShopCanopy",h,new Vector3(0,1.8f,-depth*.5f-.48f));awning.localRotation=Quaternion.Euler(17,0,0);
            for(int strip=0;strip<8;strip++)Q(awning,"CanvasStripe",new Vector3((strip-3.5f)*width/9,0,0),new Vector3(width/9+.005f,.12f,.96f),strip%2==0?roofBlue:stoneLight);
        }
        Contact(city,"HouseContact",new Vector3(x,-3.535f,z),width+1,depth+1);
    }
    static void Dome(Transform p,Vector3 at,float radius,float height)
    {
        Ball(p,"CopperDome",at+Vector3.up*(height*.31f),new Vector3(radius*2,height*1.30f,radius*2),roofBlue);
        C(p,"DomeDrum",at+Vector3.up*.1f,radius,.38f,stoneLight,Vector3.up);
        Ring(p,"DomeBaseBand",at+Vector3.up*.30f,radius,.045f,brass,Vector3.up);
        C(p,"DomeFinial",at+Vector3.up*(height+.12f),.055f,.5f,brass,Vector3.up);
        Ball(p,"DomeFinialBall",at+Vector3.up*(height+.40f),Vector3.one*.19f,brass);
    }
    static void Tower(Transform city,Vector3 pos,float height,float width)
    {
        var t=G("CityClockTower",city,pos+Vector3.up*-3.55f);
        Q(t,"TowerBody",new Vector3(0,height*.5f,0),new Vector3(width,height,width),stone);
        foreach(float y in new[]{.4f,height*.4f,height*.75f,height})Q(t,"TowerMolding",new Vector3(0,y,0),new Vector3(width+.24f,.22f,width+.24f),stoneLight);
        for(int f=0;f<3;f++)Window(t,new Vector3(0,1.3f+f*1.9f,-width*.5f-.05f),.7f,1.18f,true);
        C(t,"CityClockRim",new Vector3(0,height-.84f,-width*.5f-.16f),.61f,.16f,brass,Vector3.forward);
        C(t,"CityClockFace",new Vector3(0,height-.84f,-width*.5f-.26f),.53f,.075f,windowWarm,Vector3.forward);
        Beam(t,"TowerClockHand",new Vector3(0,height-.84f,-width*.5f-.32f),new Vector3(-.28f,height-.58f,-width*.5f-.32f),.04f,cityDark);
        Beam(t,"TowerClockHand",new Vector3(0,height-.84f,-width*.5f-.33f),new Vector3(.22f,height-.60f,-width*.5f-.33f),.04f,cityDark);
        Dome(t,new Vector3(0,height+.15f,0),width*.57f,1.35f);
    }
    static void Tree(Transform city,Vector3 pos,float size)
    {
        var t=G("SculptedTree",city,pos);t.localScale=Vector3.one*size;
        C(t,"TreeTrunk",new Vector3(0,.85f,0),.12f,1.7f,walnut,Vector3.up);
        Ball(t,"RoundTreeCrown",new Vector3(0,1.92f,0),new Vector3(1.40f,1.80f,1.30f),leaves);
        Ball(t,"LeafCluster",new Vector3(-.40f,1.70f,-.23f),new Vector3(.86f,1.12f,.85f),leavesLight);
        Ball(t,"LeafCluster",new Vector3(.42f,2.12f,.12f),new Vector3(.80f,1.10f,.80f),leaves);
        Ball(t,"LeafCluster",new Vector3(-.11f,2.72f,.02f),new Vector3(.83f,.88f,.83f),leavesLight);
        C(t,"Planter",new Vector3(0,.22f,0),.58f,.44f,stone,Vector3.up);Ring(t,"PlanterLip",new Vector3(0,.43f,0),.57f,.055f,stoneLight,Vector3.up);
        Contact(t,"TreeContact",new Vector3(0,.015f,0),2.5f,2.2f);
    }
    static void Lamp(Transform city,Vector3 at,float scale=1)
    {
        var l=G("AmberStreetLamp",city,at);l.localScale=Vector3.one*scale;
        C(l,"LampBase",new Vector3(0,.15f,0),.26f,.30f,roofBlue,Vector3.up);
        C(l,"LampFoot",new Vector3(0,.36f,0),.16f,.40f,roofBlue,Vector3.up);
        C(l,"LampPost",new Vector3(0,1.70f,0),.065f,2.6f,roofBlue,Vector3.up);
        foreach(float y in new[]{.45f,2.79f})Ring(l,"BrassCollar",new Vector3(0,y,0),.11f,.035f,brass,Vector3.up);
        C(l,"LanternBottom",new Vector3(0,2.97f,0),.27f,.16f,roofBlue,Vector3.up);
        Ball(l,"OpalLanternGlobe",new Vector3(0,3.31f,0),Vector3.one*.61f,glow);
        CityLight(l,new Vector3(0,3.31f,0),7.5f,7.0f);
        LightPool(city,l.TransformPoint(new Vector3(0,.025f,0)),3.9f*scale,2.0f*scale,.32f);
        var cap=P(l,"LanternRoof",roofMesh,roofBlue,new Vector3(0,3.65f,0));cap.transform.localScale=new Vector3(.66f,.21f,.66f);
        Ball(l,"LanternFinial",new Vector3(0,3.99f,0),Vector3.one*.15f,brass);
        Contact(l,"LampContact",new Vector3(0,.015f,0),1.3f,1.3f);
    }
    static Mesh Arch(float radius,float depth,float thickness)
    {
        string key=$"CityArch_{radius:F2}_{depth:F2}_{thickness:F2}";if(meshes.TryGetValue(key,out var cached))return cached;
        var v=new List<Vector3>();var t=new List<int>();const int count=24;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI/count,b=(i+1)*Mathf.PI/count;
            Vector3 pa=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),pb=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0),z=Vector3.forward*depth*.5f;
            Quad(v,t,pa*radius-z,pb*radius-z,pb*(radius+thickness)-z,pa*(radius+thickness)-z,Vector3.back);
            Quad(v,t,pa*radius+z,pa*(radius+thickness)+z,pb*(radius+thickness)+z,pb*radius+z,Vector3.forward);
            Quad(v,t,pa*radius-z,pa*radius+z,pb*radius+z,pb*radius-z,-(pa+pb));
            Quad(v,t,pa*(radius+thickness)-z,pb*(radius+thickness)-z,pb*(radius+thickness)+z,pa*(radius+thickness)+z,pa+pb);
        }
        meshes[key]=CityMesh(key,v,t);return meshes[key];
    }
    static void Viaduct(Transform city)
    {
        var viaduct=G("RailwayViaduct",city,new Vector3(-3,0,18));
        for(int i=0;i<9;i++)
        {
            float x=-24+i*6;
            P(viaduct,"StoneArch",Arch(2.47f,2.3f,.39f),stone,new Vector3(x,-1.1f,0));
            Q(viaduct,"ArchPier",new Vector3(x-3,-2.32f,0),new Vector3(.94f,2.5f,2.48f),stone);
            Q(viaduct,"PierCap",new Vector3(x-3,-1.1f,0),new Vector3(1.13f,.22f,2.62f),stoneLight);
            // Solid voussoirs around the curved opening.
            for(int j=0;j<11;j++)
            {float a=(j+.5f)*Mathf.PI/11;var b=Q(viaduct,"ArchStone",new Vector3(x+Mathf.Cos(a)*2.68f,-1.1f+Mathf.Sin(a)*2.68f,-1.18f),new Vector3(.24f,.30f,.12f),stoneLight);b.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90);}
        }
        Q(viaduct,"ViaductDeck",new Vector3(0,1.68f,0),new Vector3(55,.4f,2.55f),stoneLight);
        foreach(int side in new[]{-1,1})
        {
            Q(viaduct,"TrackRail",new Vector3(0,1.93f,side*.55f),new Vector3(55,.07f,.08f),brass);
            Q(viaduct,"BalustradeRail",new Vector3(0,2.33f,side*1.29f),new Vector3(55,.075f,.075f),roofBlue);
            for(int i=0;i<64;i++)Q(viaduct,"Baluster",new Vector3(-27+i*.85f,2.09f,side*1.29f),new Vector3(.055f,.47f,.055f),roofBlue);
        }
    }
    static void CanalBridge(Transform city)
    {
        var bridge=G("CanalFootbridge",city,new Vector3(15.3f,-2.85f,5.5f));
        P(bridge,"ArchedBridge",Arch(2.6f,2.20f,.50f),stoneLight,Vector3.zero);
        foreach(int side in new[]{-1,1})
        {
            Q(bridge,"BridgeAbutment",new Vector3(side*3.1f,-.25f,0),new Vector3(1.2f,2.1f,2.5f),stone);
            for(int i=0;i<20;i++)
            {float x=-3.5f+i*.37f;float y=.9f+1.8f*Mathf.Sqrt(Mathf.Clamp01(1-x*x/(3.6f*3.6f)));Q(bridge,"BridgeRailingPost",new Vector3(x,y,side*1.21f),new Vector3(.09f,.72f,.09f),roofBlue);}
        }
    }
    static void BackgroundTrain(AllAboardCompletion3D view,Transform city)
    {
        view.cityTrain=G("EveningCityTrain",city,new Vector3(-12,2.03f,18));view.cityTrainRest=view.cityTrain.localPosition;
        for(int car=0;car<4;car++)
        {
            var t=G("WarmCarriage",view.cityTrain,new Vector3(-car*2.8f,0,0));
            Q(t,"CarriageBody",new Vector3(0,.54f,0),new Vector3(2.49f,.88f,1.05f),roofBlue);
            Q(t,"CarriageRoof",new Vector3(0,1.04f,0),new Vector3(2.63f,.19f,1.19f),teal);
            Q(t,"CoachStripe",new Vector3(0,.32f,-.542f),new Vector3(2.39f,.065f,.025f),brass);
            for(int j=0;j<4;j++)Q(t,"TrainWindow",new Vector3(-.88f+j*.59f,.70f,-.543f),new Vector3(.33f,.37f,.04f),windowWarm);
            foreach(float x in new[]{-.8f,.8f})foreach(int side in new[]{-1,1})C(t,"CoachWheel",new Vector3(x,.08f,side*.43f),.19f,.13f,cityDark,Vector3.forward);
        }
        Batch(view.cityTrain);
    }
    static void BuildCity(AllAboardCompletion3D view,Transform stage)
    {
        cityCube=CityCube();
        sphere=Sphere();archWindow=WindowArch();roofMesh=Mansard();
        stone=M("CitySandstone",new Color(.49f,.41f,.31f),0,.13f);stoneLight=M("CityCreamStone",new Color(.61f,.56f,.43f),0,.16f);
        stoneBlue=M("CityBlueStone",new Color(.38f,.43f,.46f),0,.15f);roofBlue=M("CityTealCopper",new Color(.07f,.22f,.25f),.24f,.40f);
        cityDark=M("CityWindowRecess",new Color(.035f,.072f,.102f));paving=M("CityPaving",new Color(.33f,.31f,.29f),0,.10f);
        windowWarm=M("CityAmberWindow",new Color(1,.69f,.31f));windowWarm.SetColor("_EmissionColor",new Color(5.4f,2.6f,.60f));EditorUtility.SetDirty(windowWarm);
        windowSoft=M("CityDimWindow",new Color(.24f,.31f,.37f),.1f,.4f);
        leaves=M("CityLeaves",new Color(.095f,.22f,.16f));leavesLight=M("CityLeafTips",new Color(.18f,.28f,.17f));cloud=M("CityCloud",new Color(.25f,.33f,.44f),0,0);
        foreach(var material in new[]{stone,stoneLight,stoneBlue,roofBlue,cityDark,windowWarm,windowSoft,leaves,leavesLight,paving,cloud})
        {material.SetFloat("_CityAtmosphere",1);EditorUtility.SetDirty(material);}
        foreach(var material in new[]{stone,stoneLight,paving})
        {material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));material.SetFloat("_TextureTint",0);material.SetFloat("_GrainStrength",.3f);EditorUtility.SetDirty(material);}
        var city=G("CityLightsBackground",stage,Vector3.zero);
        var sky=ToyBoxGeometry.Save(new Material(Shader.Find("BUCA/Station Sky")),Folder+"/CitySky.mat");
        Q(city,"BlueHourSky",new Vector3(0,20,85),new Vector3(260,130,1),sky);
        Q(city,"TownGround",new Vector3(0,-4.50f,15),new Vector3(160,.4f,130),cityDark);
        Q(city,"StationPlaza",new Vector3(0,-4.03f,-2.0f),new Vector3(24,.9f,19),paving);
        Q(city,"FarCityQuay",new Vector3(0,-4.0f,29),new Vector3(110,1,28),paving);
        Q(city,"RightCityQuay",new Vector3(30,-4.0f,6),new Vector3(24,1,24),paving);
        Q(city,"LeftCityQuay",new Vector3(-20,-4.0f,4),new Vector3(16,1,22),paving);
        var water=ToyBoxGeometry.Save(new Material(Shader.Find("BUCA/Station Canal")),Folder+"/CityWater.mat");
        Q(city,"BackCanal",new Vector3(0,-3.90f,11),new Vector3(65,.05f,8),water);
        Q(city,"RightCanal",new Vector3(15,-3.90f,-1),new Vector3(6,.05f,22),water);
        // Warm stone coping and small paving blocks on the quay edges.
        for(int i=0;i<32;i++)
        {
            float x=-22+i*1.45f;
            Q(city,"CanalCoping",new Vector3(x,-3.39f,7.30f),new Vector3(1.4f,.32f,.55f),stoneLight);
            Q(city,"RearCanalCoping",new Vector3(x,-3.35f,15.0f),new Vector3(1.4f,.32f,.55f),stoneLight);
        }
        for(int i=0;i<24;i++)Q(city,"SideQuayCoping",new Vector3(11.89f,-3.39f,-10+i*.76f),new Vector3(.52f,.30f,.72f),stoneLight);
        for(int side=0;side<2;side++)for(int row=0;row<8;row++)for(int col=0;col<4;col++)
        {float x=(side==0?-1:1)*(9.55f+col*.65f);Q(city,"PlazaPaver",new Vector3(x,-3.55f,-6+row*1.1f),new Vector3(.60f,.05f,1.03f),row%2==0?stone:stoneBlue);}
        House(city,-13.5f,1.8f,4.4f,6.4f,3.8f,12,0);
        House(city,-16,7.6f,3.9f,7.4f,3.8f,8,1);
        House(city,-21,8.4f,4.2f,6.5f,3.5f,6,2);
        House(city,14.8f,9.2f,3.7f,6.1f,3.3f,-12,0);
        House(city,21,6.6f,4.3f,7.1f,3.8f,-8,1);
        for(int i=0;i<11;i++)House(city,-32+i*6.1f,25+(i%3)*2.8f,3.8f+(i%2)*.6f,4.4f+(i%3)*.7f,3.8f,(i%3-1)*5,i);
        Tower(city,new Vector3(-17,0,27),7.35f,2.25f);Tower(city,new Vector3(12,0,30),7.4f,2.5f);
        for(int i=0;i<5;i++)
        {float x=-29+i*13.2f;Dome(city,new Vector3(x,3.8f,38),1.3f,1.35f);Q(city,"DistantDomeBuilding",new Vector3(x,-.1f,38),new Vector3(3.2f,7.3f,3.2f),stoneBlue);}
        Viaduct(city);CanalBridge(city);BackgroundTrain(view,city);
        foreach(var p in new[]{new Vector3(-10.7f,-3.55f,-4),new Vector3(-10.3f,-3.55f,2.5f),new Vector3(10.7f,-3.55f,-4),new Vector3(10.9f,-3.55f,3.8f),new Vector3(-8,-3.55f,6.7f),new Vector3(7,-3.55f,6.7f)})Lamp(city,p,1.1f);
        for(int i=0;i<12;i++){Lamp(city,new Vector3(-31+i*5.8f,-3.5f,15.3f),.85f);Tree(city,new Vector3(-32+i*5.8f,-3.55f,21.4f),.9f+(i%3)*.12f);}
        foreach(var p in new[]{new Vector3(-11.6f,-3.55f,-1),new Vector3(-9.2f,-3.55f,5),new Vector3(10.4f,-3.55f,6),new Vector3(10.6f,-3.55f,-6),new Vector3(-10.6f,-3.55f,-6.5f),new Vector3(19.4f,-3.55f,5)})Tree(city,p,1.25f);
        // Soft sculpted clouds at skyline distance, not painted cutouts.
        for(int i=0;i<6;i++)
        {float x=-42+i*16;for(int j=0;j<3;j++)Ball(city,"EveningCloud",new Vector3(x+j*2.2f,4.8f+(i%2)*1.1f+j*.25f,51),new Vector3(5.5f,1.5f+(j%2)*.5f,2.0f),cloud);}
        Batch(city,view.cityTrain);
    }
    static void CityPostProcessing(AllAboardCompletion3D view,Transform stage)
    {
        const string path=Folder+"/CityEveningProfile.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
        bloom.threshold.Override(.95f);bloom.intensity.Override(.8f);bloom.scatter.Override(.72f);bloom.highQualityFiltering.Override(true);
        if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}
        tone.mode.Override(TonemappingMode.Neutral);EditorUtility.SetDirty(tone);
        if(!profile.TryGet<DepthOfField>(out var dof)){dof=profile.Add<DepthOfField>();AssetDatabase.AddObjectToAsset(dof,profile);}
        dof.mode.Override(DepthOfFieldMode.Off); // Keep text crisp; the city shader supplies distance haze.
        if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}
        vignette.intensity.Override(.12f);vignette.smoothness.Override(.40f);
        EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(dof);EditorUtility.SetDirty(vignette);EditorUtility.SetDirty(profile);
        var g=G("StationEveningVolume",stage,Vector3.zero).gameObject;g.layer=30;
        var collider=g.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=Vector3.one*160;
        view.presentationVolume=g.AddComponent<Volume>();view.presentationVolume.sharedProfile=profile;view.presentationVolume.isGlobal=false;view.presentationVolume.priority=100;
        var data=view.stationCamera.GetComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.volumeLayerMask=1<<30;data.requiresDepthTexture=false;
        view.stationCamera.allowHDR=true;
    }
}
#endif
