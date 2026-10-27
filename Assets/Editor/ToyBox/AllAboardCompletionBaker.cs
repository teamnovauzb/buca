#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Authors the actual solid station diorama; no full-screen illustration or runtime mesh generation.</summary>
public static partial class AllAboardCompletionBaker
{
    const string Folder="Assets/ToyBoxMenu/AllAboard";
    static Material teal,maple,walnut,brass,cream,ink,mint,red,rubber,glass,glow,shadow;
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static Transform G(string n,Transform p,Vector3 v){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=v;return t;}
    static GameObject P(Transform p,string n,Mesh mesh,Material m,Vector3 v)
    {var g=G(n,p,v).gameObject;g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return g;}
    static Mesh Shared(string key,Func<Mesh> create)
    {if(meshes.TryGetValue(key,out var m))return m;m=AssetDatabase.LoadAssetAtPath<Mesh>(ToyBoxGeometry.Folder+"/"+key+".asset")??create();meshes[key]=m;return m;}
    static GameObject B(Transform p,string n,Vector3 v,Vector3 s,Material m,float r=.08f)
        =>P(p,n,Shared($"Box_{s.x:F3}_{s.y:F3}_{s.z:F3}_{r:F3}",()=>ToyBoxGeometry.RoundedBox(s,r)),m,v);
    static GameObject C(Transform p,string n,Vector3 v,float radius,float height,Material m,Vector3 axis)
    {
        var g=P(p,n,Shared($"Disc_{radius:F3}_{height:F3}_0.035",()=>ToyBoxGeometry.Disc(radius,height,.035f)),m,v);
        g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,axis);return g;
    }
    static GameObject Ring(Transform p,string n,Vector3 v,float radius,float tube,Material m,Vector3 axis)
    {var g=P(p,n,Shared($"Torus_{radius:F3}_{tube:F3}",()=>ToyBoxGeometry.Torus(radius,tube)),m,v);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,axis);return g;}
    static Material M(string n,Color c,float metal=0,float smooth=.45f)
    {var m=new Material(Shader.Find("BUCA/Station Toy"));m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);return ToyBoxGeometry.Save(m,Folder+"/"+n+".mat");}
    static GameObject L(Transform p,string n,string text,Vector3 v,float height,float width,Material m)
    {
        string key="Text_"+text.Replace(' ','_')+"_0.210_Bevel_0.018_Weight_0.045";
        var mesh=UnityEngine.Object.Instantiate(Shared(key,()=>ToyBoxGeometry.Text(text,.21f,.018f,.045f)));
        var a=mesh.vertices;var center=mesh.bounds.center;float s=Mathf.Min(height/mesh.bounds.size.y,width/mesh.bounds.size.x);
        for(int i=0;i<a.Length;i++)a[i]=(a[i]-center)*s;mesh.vertices=a;mesh.RecalculateBounds();
        return P(p,n,ToyBoxGeometry.Save(mesh,Folder+"/"+n+".asset"),m,v);
    }
    // Rounded face in XY with a full depth bevel, independent of the corner radius.
    static Mesh Slab(Vector3 size,float radius)
    {
        string key=$"Slab_{size.x:F3}_{size.y:F3}_{size.z:F3}_{radius:F3}";
        if(meshes.TryGetValue(key,out var cached))return cached;
        radius=Mathf.Min(radius,Mathf.Min(size.x,size.y)*.49f);
        var outline=new List<Vector2>();
        for(int corner=0;corner<4;corner++)for(int j=0;j<=10;j++)
        {float a=(corner*90+j*9)*Mathf.Deg2Rad;outline.Add(new Vector2((corner==0||corner==3?1:-1)*(size.x*.5f-radius)+Mathf.Cos(a)*radius,(corner<2?1:-1)*(size.y*.5f-radius)+Mathf.Sin(a)*radius));}
        var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();int count=outline.Count;float bevel=Mathf.Min(.075f,size.z*.28f);
        for(int ring=0;ring<6;ring++)foreach(var p in outline)
        {
            float angle=(ring<3?ring:5-ring)*Mathf.PI/4;
            float inset=bevel*(1-Mathf.Sin(angle));float z=(size.z*.5f-bevel+bevel*Mathf.Cos(angle))*(ring<3?-1:1);
            var q=new Vector3(p.x*(1-inset/(size.x*.5f)),p.y*(1-inset/(size.y*.5f)),z);vs.Add(q);uv.Add(new Vector2(q.x,q.y)*.2f);
        }
        for(int ring=0;ring<5;ring++)for(int j=0;j<count;j++)
        {int a=ring*count+j,b=ring*count+(j+1)%count,c=b+count,d=a+count;ts.AddRange(new[]{a,b,c,a,c,d});}
        for(int side=0;side<2;side++)
        {
            int center=vs.Count;float z=(side==0?-1:1)*size.z*.5f;vs.Add(new Vector3(0,0,z));uv.Add(Vector2.zero);
            foreach(var p in outline){var q=new Vector3(p.x*(1-bevel/(size.x*.5f)),p.y*(1-bevel/(size.y*.5f)),z);vs.Add(q);uv.Add(new Vector2(q.x,q.y)*.2f);}
            for(int j=0;j<count;j++){int a=center+1+j,b=center+1+(j+1)%count;ts.AddRange(side==0?new[]{center,b,a}:new[]{center,a,b});}
        }
        var mesh=new Mesh{name=key};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        meshes[key]=ToyBoxGeometry.Save(mesh,Folder+"/"+key+".asset");return meshes[key];
    }
    static GameObject S(Transform p,string n,Vector3 v,Vector3 size,float radius,Material m)=>P(p,n,Slab(size,radius),m,v);
    static void Beam(Transform p,string n,Vector3 a,Vector3 b,float width,Material m)
    {var g=B(p,n,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),m,.03f);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Bolt(Transform p,Vector3 v,float radius=.065f)
    {C(p,"BrassBolt",v,radius,.06f,brass,Vector3.forward);B(p,"BoltSlot",v+Vector3.back*.035f,new Vector3(radius*.9f,.013f,.014f),walnut,.006f);}
    static void Plaque(Transform p,string n,Vector3 v,Vector3 size,Material face)
    {
        S(p,n+"Wood",v,size,.19f,walnut);
        S(p,n+"Brass",v+Vector3.back*(size.z*.5f+.025f),new Vector3(size.x-.16f,size.y-.16f,.12f),.16f,brass);
        S(p,n+"Face",v+Vector3.back*(size.z*.5f+.10f),new Vector3(size.x-.28f,size.y-.28f,.10f),.12f,face);
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})Bolt(p,v+new Vector3(x*(size.x*.5f-.20f),y*(size.y*.5f-.20f),-size.z*.5f-.18f));
    }
    static void Contact(Transform p,string n,Vector3 v,float width,float depth)
    {
        const string key="ContactQuad";
        if(!meshes.TryGetValue(key,out var mesh))
        {
            mesh=new Mesh{name=key};mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[key]=mesh=ToyBoxGeometry.Save(mesh,Folder+"/ContactQuad.asset");
        }
        P(p,n,mesh,shadow,v).transform.localScale=new Vector3(width,1,depth);
    }
    static Vector3 Track(float x)=>new Vector3(x,-2.63f,-1.35f-.36f*(x+3)+.055f*(x+3)*(x+3));
    static void Rails(Transform s)
    {
        for(int i=0;i<40;i++)
        {
            float x=-8.3f+i*.425f;Vector3 p=Track(x);float yaw=Mathf.Atan(.36f-.11f*(x+3))*Mathf.Rad2Deg;
            var t=G("TrackSection",s,p);t.localRotation=Quaternion.Euler(0,yaw,0);
            B(t,"WalnutSleeper",new Vector3(0,-.04f,0),new Vector3(.24f,.18f,2.38f),walnut,.035f);
            foreach(int side in new[]{-1,1})
            {
                B(t,"RailFoot",new Vector3(0,.08f,side*.83f),new Vector3(.47f,.065f,.20f),ink,.025f);
                B(t,"PolishedRail",new Vector3(0,.14f,side*.83f),new Vector3(.49f,.12f,.09f),brass,.035f);
                C(t,"RailPin",new Vector3(0,.07f,side*1.05f),.045f,.08f,brass,Vector3.up);
            }
        }
    }
    static void Station(AllAboardCompletion3D view,Transform s)
    {
        var building=G("TimberStation",s,new Vector3(2.45f,0,2.35f));building.localRotation=Quaternion.Euler(0,-6,0);
        // Solid back wall, corner posts, deep eaves and exposed rafters.
        for(int i=0;i<19;i++)B(building,"RearTimber",new Vector3(-4.15f+i*.46f,.26f,1.45f),new Vector3(.44f,5.8f,.20f),maple,.025f);
        foreach(float x in new[]{-4.45f,4.45f})foreach(float z in new[]{-.45f,1.7f})
        {
            B(building,"TimberPost",new Vector3(x,.15f,z),new Vector3(.40f,5.9f,.42f),maple,.055f);
            B(building,"PostFoot",new Vector3(x,-2.40f,z),new Vector3(.62f,.55f,.64f),walnut);
            B(building,"PostCollar",new Vector3(x,2.68f,z),new Vector3(.56f,.28f,.60f),brass,.04f);
        }
        foreach(float z in new[]{-.7f,1.9f})
        {
            B(building,"CrossBeam",new Vector3(0,2.94f,z),new Vector3(9.5f,.36f,.38f),walnut);
            foreach(int side in new[]{-1,1})Beam(building,"DiagonalBrace",new Vector3(side*4.4f,1.68f,z),new Vector3(side*3.25f,2.82f,z),.22f,maple);
        }
        foreach(int side in new[]{-1,1})
        {
            var roof=G("PitchedRoof",building,new Vector3(side*2.55f,3.72f,.5f));roof.localRotation=Quaternion.Euler(0,0,-side*16);
            B(roof,"RoofStructure",Vector3.zero,new Vector3(5.50f,.28f,3.9f),walnut,.07f);
            for(int row=0;row<9;row++)B(roof,"TealRoofPlank",new Vector3(0,.21f,-1.72f+row*.43f),new Vector3(5.65f,.19f,.42f),teal,.04f);
            B(roof,"FrontFascia",new Vector3(0,-.04f,-2),new Vector3(5.70f,.36f,.15f),maple,.035f);
            B(roof,"FasciaInlay",new Vector3(0,.11f,-2.095f),new Vector3(5.6f,.055f,.055f),brass,.02f);
        }
        B(building,"RidgeCap",new Vector3(0,4.54f,.5f),new Vector3(.28f,.22f,4.14f),brass,.08f);
        Plaque(building,"StationName",new Vector3(0,4.33f,-1.62f),new Vector3(6.4f,.97f,.35f),teal);
        L(building,"BucaStation","BUCA STATION",new Vector3(0,4.33f,-1.985f),.42f,5.65f,cream);
        Plaque(building,"CompleteSign",new Vector3(0,1.73f,-.99f),new Vector3(7.6f,1.92f,.37f),ink);
        view.firstTitle=L(building,"CompleteTitle","30 LEVELS",new Vector3(0,2.11f,-1.37f),.48f,6.65f,cream);
        view.bonusTitle=L(building,"BonusTitle","BONUS BOARDS",new Vector3(0,2.11f,-1.37f),.48f,6.65f,cream);view.bonusTitle.SetActive(false);
        L(building,"CompleteSubtitle","COMPLETE!",new Vector3(0,1.39f,-1.37f),.63f,6.65f,cream);
        foreach(int side in new[]{-1,1})
        {
            C(building,"SignHanger",new Vector3(side*2.8f,2.8f,-.97f),.045f,.55f,brass,Vector3.up);
            Ring(building,"HangerRing",new Vector3(side*2.8f,2.60f,-1),.09f,.025f,brass,Vector3.forward);
        }
        Plaque(building,"Counter",new Vector3(0,-.35f,-1.04f),new Vector3(7.1f,1.75f,.43f),teal);
        L(building,"TotalScore","TOTAL SCORE",new Vector3(0,.16f,-1.47f),.27f,4.5f,cream);
        view.numeralMeshes=new Mesh[10];view.digits=new MeshFilter[6];
        for(int i=0;i<10;i++)
        {
            var glyph=L(building,"Numeral"+i,i.ToString(),Vector3.zero,.63f,.47f,cream);view.numeralMeshes[i]=glyph.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(glyph);
        }
        for(int i=0;i<6;i++)
        {
            float x=(i-2.5f)*.83f;
            S(building,"DigitSocket",new Vector3(x,-.57f,-1.44f),new Vector3(.77f,1.06f,.15f),.045f,brass);
            S(building,"FlipTile",new Vector3(x,-.56f,-1.545f),new Vector3(.70f,.99f,.11f),.035f,ink);
            B(building,"TileSplit",new Vector3(x,-.56f,-1.608f),new Vector3(.65f,.012f,.008f),rubber,.003f);
            view.digits[i]=P(building,"ScoreDigit"+i,view.numeralMeshes[0],cream,new Vector3(x,-.56f,-1.652f)).GetComponent<MeshFilter>();
        }
        Plaque(building,"NewJourney",new Vector3(0,-1.64f,-.8f),new Vector3(5.70f,.65f,.23f),maple);
        view.bonusHint=L(building,"NextHint","20 NEW BONUS BOARDS",new Vector3(0,-1.64f,-1.09f),.23f,5.05f,ink);
        // A pendant with a real bell-shaped shade, rim and illuminated bulb.
        C(building,"LampStem",new Vector3(0,3.23f,-1.28f),.05f,.58f,brass,Vector3.up);
        var shade=C(building,"LampShade",new Vector3(0,2.98f,-1.28f),.29f,.18f,teal,Vector3.up);
        Ring(building,"LampRim",new Vector3(0,2.87f,-1.28f),.30f,.035f,brass,Vector3.up);
        C(building,"LampBulb",new Vector3(0,2.84f,-1.28f),.14f,.18f,glow,Vector3.up);
        Contact(s,"StationContact",new Vector3(2.4f,-2.655f,2.2f),11,5.1f);
    }
    static void Clock(Transform s)
    {
        var clock=G("StationClock",s,new Vector3(-5.9f,2.14f,2.3f));clock.localRotation=Quaternion.Euler(0,-12,0);
        B(clock,"CarvedClockPost",new Vector3(0,-2.48f,.3f),new Vector3(.38f,5.1f,.42f),walnut,.06f);
        C(clock,"DeepClockCase",Vector3.zero,.97f,.32f,teal,Vector3.forward);
        Ring(clock,"ClockBrassBezel",new Vector3(0,0,-.19f),.88f,.055f,brass,Vector3.forward);
        C(clock,"ClockIvoryFace",new Vector3(0,0,-.2f),.83f,.08f,cream,Vector3.forward);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;var t=B(clock,"ClockHourMark",new Vector3(Mathf.Sin(a)*.69f,Mathf.Cos(a)*.69f,-.255f),new Vector3(.042f,.13f,.025f),ink,.01f);t.transform.localRotation=Quaternion.Euler(0,0,-i*30);
        }
        Beam(clock,"HourHand",new Vector3(0,0,-.28f),new Vector3(-.30f,.25f,-.28f),.055f,ink);
        Beam(clock,"MinuteHand",new Vector3(0,0,-.30f),new Vector3(.27f,.46f,-.30f),.04f,ink);
        Bolt(clock,new Vector3(0,0,-.34f),.08f);
        Plaque(clock,"ClockName",new Vector3(0,-1.31f,.02f),new Vector3(2.35f,.58f,.2f),teal);
        L(clock,"AllAboard","ALL ABOARD",new Vector3(0,-1.31f,-.255f),.23f,2,cream);
    }
    static void Train(AllAboardCompletion3D view,Transform s)
    {
        view.trainRest=new Vector3(-3.35f,-1.83f,-1.24f);view.trainHeading=21;
        view.train=G("HeroLocomotive",s,view.trainRest);var t=view.train;t.localRotation=Quaternion.Euler(0,view.trainHeading,0);
        Contact(t,"TrainContact",new Vector3(0,-.79f,0),6.8f,3.9f);
        B(t,"BlackUndercarriage",new Vector3(-.18f,.03f,0),new Vector3(4.65f,.35f,1.48f),ink,.10f);
        B(t,"TealRunningBoard",new Vector3(-.12f,.40f,0),new Vector3(4.92f,.25f,1.98f),teal,.09f);
        B(t,"BrassRunningBoardTrim",new Vector3(-.12f,.31f,0),new Vector3(4.96f,.08f,2.03f),brass,.025f);
        // The cab is a built-up frame with recessed windows on both sides.
        B(t,"Cab",new Vector3(-1.64f,1.54f,0),new Vector3(1.53f,2.1f,1.8f),teal,.16f);
        foreach(int side in new[]{-1,1})
        {
            S(t,"CabWindowBrass",new Vector3(-1.64f,1.99f,side*.913f),new Vector3(1.06f,.92f,.08f),.13f,brass);
            S(t,"CabWindowDarkRecess",new Vector3(-1.64f,1.99f,side*.972f),new Vector3(.92f,.78f,.045f),.10f,ink);
            S(t,"CabWindowGlass",new Vector3(-1.64f,1.99f,side*1.003f),new Vector3(.77f,.66f,.025f),.07f,glass);
            B(t,"CabMullion",new Vector3(-1.66f,1.99f,side*1.026f),new Vector3(.075f,.74f,.05f),teal,.025f);
            B(t,"CabHandle",new Vector3(-2.13f,1.24f,side*.958f),new Vector3(.06f,.27f,.07f),brass,.025f);
            B(t,"CabStep",new Vector3(-1.8f,.11f,side*1.12f),new Vector3(1.02f,.16f,.32f),walnut,.04f);
            var name=G("BucaNamePlate",t,new Vector3(-1.65f,.95f,side*.93f));if(side==1)name.localRotation=Quaternion.Euler(0,180,0);
            S(name,"NameRim",Vector3.zero,new Vector3(1.04f,.36f,.07f),.07f,brass);
            L(name,"TrainBuca"+side,"BUCA",new Vector3(0,0,-.06f),.22f,.88f,ink);
        }
        B(t,"CabRoofUnderside",new Vector3(-1.68f,2.68f,0),new Vector3(1.98f,.23f,2.28f),walnut,.11f);
        B(t,"CabCurvedRoof",new Vector3(-1.68f,2.83f,0),new Vector3(2.07f,.24f,2.36f),teal,.115f);
        B(t,"RoofEdgeInlay",new Vector3(-1.68f,2.70f,-1.18f),new Vector3(1.98f,.065f,.05f),brass,.02f);
        C(t,"LacqueredBoiler",new Vector3(.54f,1.28f,0),.79f,2.63f,teal,Vector3.right);
        foreach(float x in new[]{-.42f,.63f,1.61f})Ring(t,"BoilerBrassBand",new Vector3(x,1.28f,0),.793f,.035f,brass,Vector3.right);
        C(t,"FrontBrassRim",new Vector3(1.94f,1.28f,0),.83f,.20f,brass,Vector3.right);
        C(t,"FrontDoor",new Vector3(2.07f,1.28f,0),.69f,.17f,teal,Vector3.right);
        Ring(t,"FrontDoorInlay",new Vector3(2.16f,1.28f,0),.59f,.025f,brass,Vector3.right);
        for(int i=0;i<12;i++)
        {float a=i*Mathf.PI/6;C(t,"BoilerRivet",new Vector3(2.06f,1.28f+Mathf.Sin(a)*.756f,Mathf.Cos(a)*.756f),.038f,.075f,walnut,Vector3.right);}
        // Crown emblem built from solid brass strokes on the boiler door.
        var crown=G("CrownBadge",t,new Vector3(2.19f,1.30f,0));crown.localRotation=Quaternion.Euler(0,-90,0);
        Beam(crown,"CrownBase",new Vector3(-.23f,-.12f,0),new Vector3(.23f,-.12f,0),.065f,brass);
        foreach(int side in new[]{-1,1})
        {Beam(crown,"CrownSide",new Vector3(side*.23f,-.12f,0),new Vector3(side*.29f,.14f,0),.065f,brass);Beam(crown,"CrownPeak",new Vector3(side*.21f,-.015f,0),new Vector3(0,.23f,0),.065f,brass);}
        C(t,"ChimneyBase",new Vector3(1.2f,2.07f,0),.36f,.16f,brass,Vector3.up);
        C(t,"ChimneyStem",new Vector3(1.2f,2.43f,0),.26f,.65f,teal,Vector3.up);
        C(t,"ChimneyCap",new Vector3(1.2f,2.78f,0),.40f,.15f,brass,Vector3.up);
        C(t,"ChimneyOpening",new Vector3(1.2f,2.86f,0),.30f,.025f,ink,Vector3.up);
        C(t,"SteamDome",new Vector3(.04f,2.13f,0),.30f,.44f,brass,Vector3.up);
        C(t,"DomeCap",new Vector3(.04f,2.37f,0),.29f,.10f,cream,Vector3.up);
        C(t,"HeadlampHousing",new Vector3(1.98f,2.20f,0),.30f,.34f,brass,Vector3.right);
        C(t,"HeadlampLens",new Vector3(2.18f,2.20f,0),.235f,.075f,glow,Vector3.right);
        Ring(t,"HeadlampLip",new Vector3(2.22f,2.20f,0),.247f,.03f,cream,Vector3.right);
        foreach(int side in new[]{-1,1})
        {
            C(t,"SideHandrail",new Vector3(.55f,1.46f,side*.86f),.035f,2.68f,brass,Vector3.right);
            foreach(float x in new[]{-.70f,1.73f})C(t,"RailStand",new Vector3(x,1.46f,side*.79f),.055f,.23f,brass,Vector3.forward);
            C(t,"PistonCylinder",new Vector3(1.68f,.34f,side*.78f),.20f,.80f,teal,Vector3.right);
        }
        var wheels=new List<Transform>();
        foreach(int side in new[]{-1,1})foreach(float x in new[]{-1.64f,-.22f,1.18f})
        {
            var w=G("WheelAssembly",t,new Vector3(x,-.01f,side*.91f));wheels.Add(w);
            C(w,"DarkTyre",Vector3.zero,.62f,.28f,rubber,Vector3.forward);
            Ring(w,"WheelBrassRim",new Vector3(0,0,side*.16f),.535f,.065f,brass,Vector3.forward);
            C(w,"RecessedWheelHub",new Vector3(0,0,side*.14f),.46f,.095f,teal,Vector3.forward);
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI/5;var spoke=B(w,"CastWheelSpoke",new Vector3(Mathf.Sin(a)*.26f,Mathf.Cos(a)*.26f,side*.208f),new Vector3(.055f,.35f,.065f),brass,.023f);spoke.transform.localRotation=Quaternion.Euler(0,0,-i*36);
            }
            C(w,"AxleCap",new Vector3(0,0,side*.25f),.14f,.12f,brass,Vector3.forward);
            C(w,"AxleBolt",new Vector3(0,0,side*.32f),.064f,.06f,cream,Vector3.forward);
        }
        view.wheels=wheels.ToArray();
        foreach(int side in new[]{-1,1})
        {
            B(t,"ConnectingRod",new Vector3(-.2f,-.16f,side*1.29f),new Vector3(3.17f,.13f,.10f),brass,.045f);
            foreach(float x in new[]{-1.64f,-.22f,1.18f})C(t,"ConnectingPin",new Vector3(x,-.16f,side*1.35f),.09f,.065f,cream,Vector3.forward);
        }
        B(t,"FrontBumper",new Vector3(2.30f,.31f,0),new Vector3(.40f,.35f,2.12f),teal,.10f);
        for(int i=0;i<7;i++)Beam(t,"CowcatcherSlat",new Vector3(2.40f,.26f,-.88f+i*.293f),new Vector3(2.76f,-.45f,-.88f+i*.293f),.105f,teal);
        B(t,"CowcatcherFoot",new Vector3(2.76f,-.45f,0),new Vector3(.12f,.12f,2.05f),brass,.045f);
    }
    static Transform Button(Transform p,string n,Vector3 v,float width,Material face,string text,int number,out Collider hit)
    {
        var t=G(n,p,v);t.localRotation=Quaternion.Euler(24,-9,0);
        S(t,"WalnutButtonSocket",Vector3.zero,new Vector3(width+.40f,1.3f,.43f),.31f,walnut);
        S(t,"GoldButtonBezel",new Vector3(0,.06f,-.27f),new Vector3(width+.15f,1.07f,.17f),.40f,brass);
        S(t,"DarkButtonGasket",new Vector3(0,.06f,-.38f),new Vector3(width+.03f,.98f,.17f),.40f,ink);
        var cap=G("PressableCap",t,new Vector3(0,.08f,-.52f));
        S(cap,"ThickPolishedCap",Vector3.zero,new Vector3(width,.95f,.35f),.42f,face);
        L(cap,n+"Letters",text,new Vector3(0,.03f,-.225f),.39f,width-.5f,n=="NextStop"?cream:ink);
        var collider=cap.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(width,1,.50f);hit=collider;
        foreach(int side in new[]{-1,1})Bolt(t,new Vector3(side*(width*.5f+.065f),-.47f,-.245f),.064f);
        var legend=G(n+"ArcadeLabel",t,new Vector3(0,-1.01f,-.70f));
        S(legend,"CreamLabel",Vector3.zero,new Vector3(width-.20f,.43f,.13f),.17f,cream);
        C(legend,"NumberRing",new Vector3(-(width-.20f)*.5f+.23f,0,-.10f),.17f,.06f,brass,Vector3.forward);
        C(legend,"NumberBlack",new Vector3(-(width-.20f)*.5f+.23f,0,-.14f),.142f,.045f,number==1?ink:red,Vector3.forward);
        L(legend,n+"Number",number.ToString(),new Vector3(-(width-.20f)*.5f+.23f,0,-.185f),.205f,.19f,cream);
        L(legend,n+"Legend",number==1?"CONTINUE":"BACK",new Vector3(.14f,0,-.106f),.19f,width-.86f,ink);
        return cap;
    }
    static void Luggage(Transform s,Vector3 v,Vector3 size,string label)
    {
        var t=G("WoodenTravelCase",s,v);t.localRotation=Quaternion.Euler(0,-12,0);
        B(t,"Suitcase",Vector3.zero,size,walnut,.10f);
        foreach(int side in new[]{-1,1})B(t,"LeatherStrap",new Vector3(side*size.x*.31f,0,0),new Vector3(.105f,size.y+.03f,size.z+.03f),teal,.025f);
        B(t,"TopHandle",new Vector3(0,size.y*.5f+.1f,0),new Vector3(.44f,.09f,.09f),brass,.035f);
        foreach(int side in new[]{-1,1})C(t,"HandleSupport",new Vector3(side*.2f,size.y*.5f+.04f,0),.03f,.14f,brass,Vector3.up);
        L(t,"CaseLabel"+label,label,new Vector3(0,-.02f,-size.z*.5f-.03f),.20f,size.x*.49f,cream);
    }
    // Bake the small construction parts into one mesh per material. Moving parts and
    // switchable labels remain separate, so animation never rebuilds geometry.
    static void Batch(Transform scope,params Transform[] exclusions)
    {
        var groups=new Dictionary<Material,List<MeshFilter>>();
        foreach(var f in scope.GetComponentsInChildren<MeshFilter>(true))
        {
            bool excluded=false;
            foreach(var x in exclusions)if(x!=null&&(f.transform==x||f.transform.IsChildOf(x))){excluded=true;break;}
            if(excluded)continue;
            var r=f.GetComponent<MeshRenderer>();if(r==null||f.sharedMesh==null)continue;
            if(!groups.TryGetValue(r.sharedMaterial,out var list))groups.Add(r.sharedMaterial,list=new List<MeshFilter>());
            list.Add(f);
        }
        int group=0;
        foreach(var pair in groups)
        {
            if(pair.Value.Count<2)continue;
            var pieces=new CombineInstance[pair.Value.Count];
            for(int i=0;i<pieces.Length;i++)pieces[i]=new CombineInstance{mesh=pair.Value[i].sharedMesh,transform=scope.worldToLocalMatrix*pair.Value[i].transform.localToWorldMatrix};
            var mesh=new Mesh{name=scope.name+"_"+group,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pieces,true,true);mesh.RecalculateBounds();
            ToyBoxMeshOptimizer.Weld(mesh);
            mesh=ToyBoxGeometry.Save(mesh,Folder+"/Batched_"+scope.name+"_"+group+".asset");
            P(scope,"SavedBatch"+group,mesh,pair.Key,Vector3.zero);group++;
            foreach(var f in pair.Value){UnityEngine.Object.DestroyImmediate(f.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(f);}
        }
    }
    [MenuItem("RealBuca/Toy Box 3D/Bake All Aboard Completion")]
    public static void BakeMenu()=>Debug.Log(Bake());
    public static string Bake()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Clean edit mode required.");
        string restore=SceneManager.GetActiveScene().path;meshes.Clear();cityLights.Clear();ToyBoxGeometry.Initialize();
        teal=M("Teal",new Color(.085f,.39f,.39f),.22f,.56f);maple=M("Maple",new Color(.95f,.9f,.79f),0,.25f);
        walnut=M("Walnut",new Color(.48f,.29f,.12f),0,.3f);brass=M("Brass",new Color(.82f,.58f,.25f),.83f,.6f);
        cream=M("Cream",new Color(1,.94f,.77f),.02f,.34f);ink=M("Ink",new Color(.038f,.086f,.09f),.05f,.35f);
        mint=M("Mint",new Color(.17f,.78f,.50f),.14f,.65f);red=M("Coral",new Color(.76f,.16f,.07f),.1f,.45f);
        rubber=M("Rubber",new Color(.025f,.039f,.04f),0,.22f);glass=M("Glass",new Color(.12f,.30f,.32f),.38f,.8f);
        glow=M("LampGlow",new Color(1,.94f,.72f));glow.SetColor("_EmissionColor",new Color(9,5.5f,1.6f));EditorUtility.SetDirty(glow);
        foreach(var m in new[]{maple,walnut,teal}){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));m.SetTextureScale("_BaseMap",new Vector2(.7f,.7f));EditorUtility.SetDirty(m);}
        teal.SetFloat("_TextureTint",0);teal.SetFloat("_GrainStrength",.38f);maple.SetFloat("_TextureTint",.65f);
        shadow=ToyBoxGeometry.Save(new Material(Shader.Find("BUCA/Station Contact")),Folder+"/Contact.mat");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var old=UnityEngine.Object.FindAnyObjectByType<AllAboardCompletion3D>(FindObjectsInactive.Include);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("SavedAllAboardCompletion");root.transform.position=new Vector3(950,0,0);var view=root.AddComponent<AllAboardCompletion3D>();
            var stage=G("Presentation",root.transform,Vector3.zero);view.presentation=stage.gameObject;view.station=G("Station",stage,Vector3.zero);var s=view.station;
            BuildCity(view,stage);
            Contact(stage,"DioramaContact",new Vector3(0,-3.52f,0),24,17);
            B(s,"DeepTealPlinth",new Vector3(0,-3.13f,.4f),new Vector3(17.5f,.62f,9.9f),teal,.24f);
            B(s,"PlatformBrassInlay",new Vector3(0,-2.96f,.4f),new Vector3(17.42f,.10f,9.84f),brass,.045f);
            for(int i=0;i<25;i++)B(s,"SolidMapleDeck",new Vector3(-8.38f+i*.698f,-2.79f,.4f),new Vector3(.68f,.24f,9.67f),maple,.035f);
            Rails(s);Station(view,s);Clock(s);Train(view,s);
            CityLight(s,new Vector3(2.2f,2.6f,-.9f),2.8f,8);
            CityLight(s,new Vector3(-5.5f,2.1f,-5),2.3f,9);
            Luggage(s,new Vector3(-7.02f,-2.16f,2.1f),new Vector3(1.55f,1.02f,.91f),"BUCA");
            Luggage(s,new Vector3(-6.91f,-1.40f,2.22f),new Vector3(1.34f,.48f,.83f),"GO!");
            Contact(s,"LuggageContact",new Vector3(-7.0f,-2.66f,2.1f),2.5f,1.8f);
            // A substantial desk at the front supports the two tactile controls.
            B(s,"ControlDesk",new Vector3(2.75f,-2.4f,-3.21f),new Vector3(8.25f,.52f,2.01f),walnut,.18f);
            B(s,"ControlDeskTop",new Vector3(2.75f,-2.1f,-3.21f),new Vector3(8.30f,.17f,2.04f),maple,.075f);
            view.nextCap=Button(s,"NextStop",new Vector3(1.38f,-1.89f,-3.33f),4.30f,mint,"NEXT STOP",1,out view.nextHit);
            view.menuCap=Button(s,"Menu",new Vector3(5.25f,-1.89f,-3.33f),2.18f,cream,"MENU",2,out view.menuHit);
            view.nextLabel=view.nextCap.Find("NextStopLetters").gameObject;
            view.replayLabel=L(view.nextCap,"ReplayLetters","PLAY AGAIN",new Vector3(0,.03f,-.225f),.37f,3.85f,cream);view.replayLabel.SetActive(false);
            view.sparks=new Transform[128];var sparkMesh=Shared("Box_1.000_1.000_1.000_0.100",()=>ToyBoxGeometry.RoundedBox(Vector3.one,.1f));
            for(int i=0;i<view.sparks.Length;i++)view.sparks[i]=P(stage,"FireworkSpark"+i,sparkMesh,i%3==0?mint:i%3==1?brass:red,Vector3.zero).transform;
            view.stationCamera=G("StationCamera",stage,new Vector3(5.4f,6.6f,-21.1f)).gameObject.AddComponent<Camera>();
            view.cameraFocus=new Vector3(0,-.05f,.5f);view.cameraPosition=view.stationCamera.transform.localPosition;
            view.stationCamera.transform.localRotation=Quaternion.LookRotation(view.cameraFocus-view.cameraPosition);view.stationCamera.orthographic=false;view.stationCamera.fieldOfView=33.5f;
            view.stationCamera.depth=150;view.stationCamera.nearClipPlane=.1f;view.stationCamera.farClipPlane=500;view.stationCamera.backgroundColor=new Color(.065f,.17f,.17f);view.stationCamera.clearFlags=CameraClearFlags.SolidColor;
            var cameraData=view.stationCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();cameraData.SetRenderer(1);cameraData.renderPostProcessing=false;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            CityPostProcessing(view,stage);
            view.sound=root.AddComponent<AudioSource>();view.sound.playOnAwake=false;view.sound.spatialBlend=0;
            view.fanfare=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/AllAboardArrival.wav");view.fireworks=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/HoleInOneTwinFireworks.wav");
            view.tick=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ScoreCountTick.ogg");view.chime=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ScoreCountChime.wav");
            var moving=new List<Transform>{view.train,view.nextCap,view.menuCap,view.firstTitle.transform,view.bonusTitle.transform,view.bonusHint.transform};
            foreach(var digit in view.digits)moving.Add(digit.transform);
            Batch(s,moving.ToArray());Batch(view.train,view.wheels);
            // Each wheel keeps its baked parts under its rotating hub.
            for(int i=0;i<view.wheels.Length;i++){view.wheels[i].name="Wheel"+i;Batch(view.wheels[i]);}
            Batch(view.nextCap,view.nextLabel.transform,view.replayLabel.transform);Batch(view.menuCap);
            BakeCityLight(stage);
            stage.gameObject.SetActive(false);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/AllAboardCompletion.prefab");UnityEngine.Object.DestroyImmediate(root);
            var saved=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();manager.allAboardCompletion=saved.GetComponent<AllAboardCompletion3D>();EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{Folder}))
            {var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Low);EditorUtility.SetDirty(mesh);}
            AssetDatabase.SaveAssets();return "Solid station diorama saved, with perspective camera, detailed locomotive and readable tactile controls.";
        }
        finally{if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);}
    }
}
#endif
