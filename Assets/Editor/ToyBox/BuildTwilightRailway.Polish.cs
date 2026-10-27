using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    static Material twContact,twRock,twRockLight;

    static void PolishTwilight(Transform map)
    {
        var decor=map.Find("Diorama/StoryScenery");
        twContact=ToyBoxGeometry.Save(new Material(Shader.Find("BUCA/Station Contact")){name="TwilightContact",color=new Color(.012f,.018f,.028f,.65f)},TwilightFolder+"/Materials/Contact.mat");
        twRock=TwMat("RiverSlate",new Color(.12f,.18f,.24f),.2f);
        twRockLight=TwMat("RiverSlateEdge",new Color(.22f,.29f,.35f),.25f);
        var sandstone=TwMat("Sandstone",new Color(.53f,.37f,.21f),.2f);
        var rng=new System.Random(271);
        // Actual cliff volumes make the railway islands stand above the water.
        for(int side=-1;side<=1;side+=2)
        for(int i=0;i<26;i++)
        {
            float z=-14+i*1.12f;
            var p=new Vector3(side*(2.8f+(float)rng.NextDouble()*.22f),-1.9f,z);
            float h=Height(p,side>0)+1.9f;
            Rock(decor,p,.75f+(float)rng.NextDouble()*.3f,h,side>0?(i%3==0?twRockLight:twRock):sandstone,rng);
        }
        // Terraced stone edging and warm station paving stay inside the tracks.
        for(int row=0;row<7;row++)for(int col=0;col<10;col++)
        {
            var p=new Vector3(8+col*1.45f+(row%2)*.65f,0,-10.6f+row*.63f);p.y=Height(p,true)-.12f;
            var tile=Box("StationPaver",decor,p,new Vector3(1.36f,.08f,.56f),row%3==0?twRockLight:twRock,.06f);
            tile.transform.localRotation=Quaternion.Euler(GroundTilt(p),0,0);
        }
        foreach(Transform prop in decor)
        {
            if(prop.name=="CityHouse"||prop.name=="ToyHouse"||prop.name=="MoonlitStation"||prop.name=="StoryTeddy"||prop.name=="ChildTable")
            {
                var p=prop.localPosition;p.y=Height(p,p.x>0)-.12f;
                Contact(decor,p,prop.name=="StoryTeddy"?6:4.8f,3.5f);
            }
        }
        PlaygroundDetails(decor);
        ClockTower(decor,new Vector3(8,Height(new Vector3(8,0,16),true),16));
        // A layered skyline supplies depth behind the foreground playable route.
        for(int row=0;row<2;row++)for(int i=0;i<7;i++)
        {
            var p=new Vector3(6+i*4.1f+row*1.3f,0,22+row*4);
            House(decor,p,2.5f+(i%3)*.33f,(i+row)%2==0?twStone:twNavy,true);
        }
        foreach(var c in decor.GetComponentsInChildren<Transform>().Where(t=>t.name=="Cloud").ToArray())UnityEngine.Object.DestroyImmediate(c.gameObject);
        var cloud=TwMat("LayeredCloud",new Color(.18f,.27f,.40f),.1f);
        for(int i=0;i<7;i++)for(int j=0;j<3;j++)Sphere("DistantCloud",decor,new Vector3(4+i*4.5f+j*.8f,9.5f+(i%3)*.8f+j*.35f,29),new Vector3(3.8f,1.6f+j*.2f,2.6f),cloud);
        CurvedFascia(map);
        // Warm pools at the station and reading lamp add localized colour separation.
        GlowLight(map,"ReadingGlow",new Vector3(-23,10,6),new Color(1,.58f,.24f),3.1f,8);
        GlowLight(map,"StationGlow",new Vector3(13,7,-1),new Color(1,.65f,.30f),2.5f,6);
        GlowLight(map,"MoonRim",new Vector3(25,16,8),new Color(.36f,.58f,1),2.6f,10);
        // Distinct brass-lit trusses and masonry under the crossing.
        var bridge=map.Find("Diorama/TwilightBridge");
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<20;i++)
            {
                float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;
                var p=new Vector3(Mathf.Cos((a+b)*.5f)*4.75f,Mathf.Sin((a+b)*.5f)*6.9f-1.8f,10+side*.91f);
                var block=Box("ArchStone",bridge,p,new Vector3(.8f,.72f,.42f),sandstone,.08f);
                block.transform.localRotation=Quaternion.Euler(0,0,-(a+b)*.5f*Mathf.Rad2Deg);
            }
        }
        var signs=map.Find("Signs");
        foreach(Transform sign in signs)
            if(sign.name=="WORLD 1"||sign.name=="WORLD 2"||sign.name=="BUCA")
            {
                sign.localRotation=Quaternion.Euler(12,0,0);
                foreach(var text in sign.GetComponentsInChildren<MeshRenderer>())text.receiveShadows=true;
            }
        PostTwilight(map);
    }

    static void CurvedFascia(Transform map)
    {
        var root=map.Find("Diorama");
        var v=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        var upper=new List<Vector3>();var lower=new List<Vector3>();
        for(int i=0;i<=96;i++)
        {
            float x=-31+i*62f/96;float z=-18.3f+Mathf.Cos(x/31*Mathf.PI)*.55f;
            float y=-1.0f+Mathf.Cos(x/31*Mathf.PI)*.25f;
            upper.Add(new Vector3(x,y,z));lower.Add(new Vector3(x,y-1.25f,z));
            v.Add(upper[i]);v.Add(lower[i]);uv.Add(new Vector2(i*.1f,1));uv.Add(new Vector2(i*.1f,0));
            if(i<96){int a=i*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
        mesh=ToyBoxGeometry.Save(mesh,twMeshFolder+"/CurvedFascia.asset");MeshObject("CurvedEnamelFascia",root,Vector3.zero,mesh,twTeal);
        Tube("FasciaUpperBrass",root,upper.ToArray(),.085f,twGold);Tube("FasciaLowerBrass",root,lower.ToArray(),.055f,twGold);
    }

    static void Contact(Transform root,Vector3 position,float w,float d)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ToyBoxMenu/AllAboard/ContactQuad.asset");
        if(mesh==null)return;
        var o=MeshObject("SoftContact",root,position+Vector3.up*.015f,mesh,twContact);
        o.transform.localScale=new Vector3(w,1,d);o.transform.localRotation=Quaternion.Euler(GroundTilt(position),0,0);
        o.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void GlowLight(Transform map,string name,Vector3 position,Color color,float intensity,float range)
    {
        var l=Group(name,map,position).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range*map.lossyScale.x;l.shadows=LightShadows.None;
    }
    static void Rock(Transform root,Vector3 p,float width,float height,Material material,System.Random random)
    {
        var v=new List<Vector3>();var tr=new List<int>();const int n=7;
        for(int row=0;row<3;row++)for(int i=0;i<n;i++)
        {
            float a=i*Mathf.PI*2/n;float r=width*(.77f+(float)random.NextDouble()*.27f);
            v.Add(new Vector3(Mathf.Cos(a)*r,height*row*.5f+(row==2?(float)random.NextDouble()*.15f:0),Mathf.Sin(a)*r));
        }
        for(int row=0;row<2;row++)for(int i=0;i<n;i++){int a=row*n+i,b=row*n+(i+1)%n;tr.AddRange(new[]{a,a+n,b,b,a+n,b+n});}
        v.Add(new Vector3(0,height,0));for(int i=0;i<n;i++)tr.AddRange(new[]{21,14+(i+1)%n,14+i});
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        mesh=ToyBoxGeometry.Save(mesh,twMeshFolder+"/Rock"+(twMeshSerial++)+".asset");MeshObject("RiverCliff",root,p,mesh,material);
    }
    static void PlaygroundDetails(Transform decor)
    {
        var poolP=new Vector3(-21,0,-9);poolP.y=Height(poolP,false)+.05f;
        var pool=Group("ToyHarbour",decor,poolP);pool.localRotation=Quaternion.Euler(GroundTilt(poolP),0,0);
        Disc("PoolStone",pool,Vector3.zero,2.4f,.28f,twIvory,.09f);Disc("PoolWater",pool,Vector3.up*.16f,2.15f,.06f,twWater,.02f);
        Ring("HarbourRim",pool,Vector3.up*.22f,2.27f,.15f,twGold,.06f);
        for(int i=0;i<3;i++)
        {
            var p=new Vector3(-.9f+i*.7f,.30f,Mathf.Sin(i)*.7f);
            Sphere("RubberDuck",pool,p,new Vector3(.45f,.35f,.65f),twGold);Sphere("DuckHead",pool,p+new Vector3(0,.28f,-.20f),Vector3.one*.33f,twGold);
            Sphere("Beak",pool,p+new Vector3(0,.27f,-.42f),new Vector3(.17f,.08f,.15f),twWood);
        }
        // A toy balloon and cloth pennants add playroom scale without obstructing the levels.
        var balloon=Group("ToyBalloon",decor,new Vector3(-28,Height(new Vector3(-28,0,1),false),1));
        Sphere("Envelope",balloon,new Vector3(0,4.7f,0),new Vector3(2.3f,3,2.3f),twIvory);
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Sphere("BalloonPanel",balloon,new Vector3(Mathf.Cos(a)*.96f,4.7f,Mathf.Sin(a)*.96f),new Vector3(.44f,2.65f,.44f),i%2==0?twNavy:twGold);}
        Box("Basket",balloon,new Vector3(0,2.25f,0),new Vector3(.8f,.65f,.8f),twWood,.12f);
        for(int side=-1;side<=1;side+=2)Box("Rope",balloon,new Vector3(side*.3f,2.95f,0),new Vector3(.045f,1.2f,.045f),twGold,.01f);
        var tent=decor.Find("PlayTent");
        if(tent!=null)for(int i=0;i<9;i++){float x=-1.45f+i*.36f;Sphere("TentFairyLight",tent,new Vector3(x,2.75f-Mathf.Abs(x)*1.4f,-1.29f),Vector3.one*.11f,twGlow);}
        // Little fences give the inset play space a miniature-diorama scale.
        for(int i=0;i<10;i++)
        {
            var p=new Vector3(-23+i*.8f,0,-1);p.y=Height(p,false);
            Box("FencePicket",decor,p+Vector3.up*.45f,new Vector3(.10f,.9f,.10f),twIvory,.03f);
            if(i<9)Box("FenceRail",decor,p+new Vector3(.4f,.6f,0),new Vector3(.8f,.10f,.08f),twWood,.02f);
        }
    }
    static void ClockTower(Transform root,Vector3 p)
    {
        var g=Group("MoonlitClockTower",root,p);
        Box("Foundation",g,Vector3.up*.15f,new Vector3(2.9f,.3f,2.5f),twRockLight,.08f);
        Box("Tower",g,Vector3.up*4,new Vector3(2.1f,8,1.8f),twNavy,.12f);
        for(int i=0;i<4;i++)Box("Cornice",g,new Vector3(0,1.6f+i*1.65f,0),new Vector3(2.35f,.15f,2),twGold,.04f);
        var face=Group("ClockFace",g,new Vector3(0,6.5f,-1));face.localRotation=Quaternion.Euler(-90,0,0);
        Disc("BrassBezel",face,Vector3.zero,.86f,.12f,twGold,.04f);Disc("IvoryDial",face,Vector3.up*.08f,.73f,.05f,twGlow,.02f);
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tick=Box("HourMark",face,new Vector3(Mathf.Sin(a)*.61f,.13f,Mathf.Cos(a)*.61f),new Vector3(.035f,.02f,.12f),twInk,.005f);tick.transform.localRotation=Quaternion.Euler(0,i*30,0);}
        Box("MinuteHand",face,new Vector3(0,.16f,.23f),new Vector3(.04f,.02f,.48f),twInk,.005f);
        Box("HourHand",face,new Vector3(.17f,.17f,0),new Vector3(.38f,.02f,.05f),twInk,.005f);
        MeshObject("Spire",g,new Vector3(0,9.2f,0),ToyBoxGeometry.MapCone(1.65f,3),twNavy);Sphere("SpireTip",g,new Vector3(0,10.75f,0),Vector3.one*.22f,twGold);
    }
    static void PostTwilight(Transform map)
    {
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(TwilightFolder+"/MoonlitProfile.asset");
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,TwilightFolder+"/MoonlitProfile.asset");}
        if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.Neutral);
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.threshold.Override(1.1f);bloom.intensity.Override(.25f);bloom.scatter.Override(.55f);
        if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}color.contrast.Override(12);color.saturation.Override(6);color.postExposure.Override(.2f);
        if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}vignette.intensity.Override(.13f);vignette.smoothness.Override(.45f);
        foreach(var c in profile.components)EditorUtility.SetDirty(c);EditorUtility.SetDirty(profile);
        var go=Group("TwilightLighting",map).gameObject;go.layer=30;var volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.priority=150;volume.sharedProfile=profile;
        var cam=map.GetComponent<TwilightRailwayMap>().viewCamera;var data=cam.GetComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.volumeLayerMask=1<<30;cam.allowHDR=true;
    }
}
