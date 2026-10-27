using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static partial class BuildToyBoxMainMenu
{
    const string TwilightFolder="Assets/TwilightMap";
    static Material twWood,twNavy,twGold,twIvory,twInk,twTeal,twStone,twWater,twLeaves,twGlow,twSleeper;
    static Transform twStatic;
    static int twMeshSerial;
    static string twMeshFolder;
    static Material TwMat(string name,Color color,float smooth=.35f,float metal=0)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.enableInstancing=true;m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);
        return ToyBoxGeometry.Save(m,TwilightFolder+"/Materials/"+name+".mat");
    }
    [MenuItem("RealBuca/Twilight Railway/Build Two World Map")]
    public static string BuildTwilight()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=MenuScene||scene.isDirty)throw new Exception("Open clean MainMenu first.");
        var menu=Object.FindAnyObjectByType<ToyBoxMenuController>(FindObjectsInactive.Include);
        if(menu==null)throw new Exception("ToyBox menu missing.");
        var source=menu.worldStation;
        if(source==null)source=menu.GetComponentsInChildren<WorldStationRoute3D>(true).FirstOrDefault();
        if(source==null)throw new Exception("Existing train scenery required.");
        Directory.CreateDirectory(TwilightFolder+"/Meshes");Directory.CreateDirectory(TwilightFolder+"/Materials");
        ToyBoxGeometry.Initialize();twMeshSerial=0;twMeshFolder=TwilightFolder+"/Meshes/Build"+DateTime.UtcNow.ToString("yyyyMMddHHmmss");Directory.CreateDirectory(twMeshFolder);
        if(menu.twilightMap!=null)Object.DestroyImmediate(menu.twilightMap.gameObject);
        var map=Group("TwilightCrossing",menu.transform);map.localPosition=new Vector3(0,0,180);map.localScale=Vector3.one*.35f;
        var runtime=map.gameObject.AddComponent<TwilightRailwayMap>();runtime.gameScene=menu.gameScene;
        twWood=TwMat("Maple",new Color(.78f,.65f,.47f));twNavy=TwMat("MidnightEnamel",new Color(.035f,.085f,.14f),.42f,.2f);
        twGold=TwMat("BrushedBrass",new Color(.76f,.49f,.16f),.5f,.65f);twIvory=TwMat("Ivory",new Color(.98f,.86f,.61f));twInk=TwMat("Ink",new Color(.025f,.055f,.07f));
        twTeal=TwMat("Teal",new Color(.035f,.27f,.27f));twStone=TwMat("BlueStone",new Color(.12f,.19f,.27f),.16f);
        twWater=TwMat("MoonlitWater",new Color(.025f,.16f,.23f),.88f,.4f);twLeaves=TwMat("Pine",new Color(.13f,.27f,.16f));twSleeper=TwMat("WalnutSleepers",new Color(.23f,.12f,.055f));
        twGlow=TwMat("LampGlass",new Color(1,.72f,.32f));twGlow.EnableKeyword("_EMISSION");twGlow.SetColor("_EmissionColor",new Color(1,.58f,.14f)*1.4f);
        var selectedMat=TwMat("SelectedMint",new Color(.06f,.8f,.66f));selectedMat.EnableKeyword("_EMISSION");selectedMat.SetColor("_EmissionColor",new Color(.05f,.8f,.63f));
        runtime.ivory=twIvory;runtime.lockedFace=TwMat("LockedIvory",new Color(.48f,.51f,.53f));
        twWood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));
        twStatic=Group("Diorama",map);
        Box("DisplayBase",twStatic,new Vector3(0,-2.9f,0),new Vector3(63,1.4f,36),twNavy,1);
        Box("BrassBaseInlay",twStatic,new Vector3(0,-2.15f,0),new Vector3(62.7f,.12f,35.7f),twGold,.05f);
        Box("River",twStatic,new Vector3(0,-2,0),new Vector3(62,.15f,35),twWater,.06f);
        Island("PlayroomIsland",-16,twWood,false);Island("CityIsland",16,twStone,true);
        // Gently elevated organic switchbacks. Same sampled centreline builds rails and drives the engine.
        var left=Curve(new[]{new Vector3(-26,0,-7),new Vector3(-24,0,-12),new Vector3(-16,0,-14),new Vector3(-7,0,-12),new Vector3(-6.2f,0,-7),new Vector3(-8,0,-2),new Vector3(-14,0,1),new Vector3(-23,0,3),new Vector3(-25,0,7),new Vector3(-21,0,11),new Vector3(-13,0,12),new Vector3(-5,0,10)},false);
        var right=Curve(new[]{new Vector3(5,0,10),new Vector3(13,0,11),new Vector3(23,0,8),new Vector3(26,0,3),new Vector3(23,0,-2),new Vector3(15,0,-4),new Vector3(7,0,-6),new Vector3(6,0,-10),new Vector3(13,0,-13),new Vector3(23,0,-12),new Vector3(26,0,-8)},true);
        var all=new List<Vector3>(left);for(int i=1;i<=40;i++){float t=i/40f;var p=Vector3.Lerp(left.Last(),right[0],t);p.y+=Mathf.Sin(t*Mathf.PI)*.8f;all.Add(p);}all.AddRange(right.Skip(1));runtime.railway=all.ToArray();
        float leftLength=Length(left),bridgeLength=Length(all.Take(left.Length+40).ToArray())-leftLength;
        var stops=new List<TwilightRailwayMap.Stop>();
        AddStops(map,stops,left,0,30,0,selectedMat);
        AddStops(map,stops,right,30,20,leftLength+bridgeLength,selectedMat);runtime.stops=stops.ToArray();
        Railway(runtime.railway);Bridge(left.Last(),right[0]);
        Scenery(source,map);
        var train=Object.Instantiate(source.train.gameObject,map).transform;train.name="JourneyLocomotive";train.gameObject.SetActive(true);train.localScale=Vector3.one*.9f;
        foreach(var c in train.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        runtime.train=train;runtime.wheels=train.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Wheel")&&t.name.Length==6).ToArray();
        var smoke=new List<Transform>();for(int i=0;i<5;i++)smoke.Add(Sphere("Steam",train,new Vector3(1,3+i*.3f,0),Vector3.one*.3f,twIvory));runtime.steam=smoke.ToArray();
        runtime.motionAudio=map.gameObject.AddComponent<AudioSource>();runtime.motionAudio.playOnAwake=false;runtime.motionAudio.spatialBlend=0;
        runtime.eventAudio=map.gameObject.AddComponent<AudioSource>();runtime.eventAudio.playOnAwake=false;runtime.eventAudio.spatialBlend=0;runtime.whistle=source.arrivalWhistle;runtime.rolling=source.movingTrain;
        var camera=Group("TwilightCamera",map).gameObject;runtime.viewCamera=camera.AddComponent<Camera>();camera.AddComponent<AudioListener>();runtime.viewCamera.nearClipPlane=.3f;runtime.viewCamera.farClipPlane=240;runtime.viewCamera.backgroundColor=new Color(.025f,.045f,.08f);runtime.viewCamera.clearFlags=CameraClearFlags.SolidColor;
        var data=camera.AddComponent<UniversalAdditionalCameraData>();data.SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var key=Group("WarmSoftbox",map).gameObject.AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(44,-35,0);key.color=new Color(1,.86f,.68f);key.intensity=1.3f;key.shadows=LightShadows.Soft;key.shadowStrength=.85f;
        var fill=Group("MoonFill",map).gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(48,140,0);fill.color=new Color(.54f,.71f,1);fill.intensity=.36f;
        var reflection=Group("Reflection",map).gameObject.AddComponent<ReflectionProbe>();reflection.mode=ReflectionProbeMode.Custom;reflection.customBakedTexture=ToyBoxSurfaceBaker.StudioReflection();reflection.size=Vector3.one*100;reflection.intensity=.65f;
        var ui=Group("Signs",map);
        Plaque(ui,"BUCA",new Vector3(0,17.3f,18.5f),new Vector3(7,2,.45f),1.15f,twTeal);
        Plaque(ui,"WORLD 1",new Vector3(-21,16,18.5f),new Vector3(11,2,.4f),1.0f,twWood);
        Plaque(ui,"WORLD 2",new Vector3(21,18,18.5f),new Vector3(11,2,.4f),1.0f,twNavy);
        var play=Plaque(ui,"",new Vector3(0,-.2f,-18),new Vector3(16,2.2f,.6f),1,twIvory);play.localRotation=Quaternion.Euler(34,0,0);runtime.playHit=play.gameObject.AddComponent<BoxCollider>();((BoxCollider)runtime.playHit).size=new Vector3(16,2.2f,.8f);
        runtime.playLabel=Label(play,"PLAY LEVEL 1",new Vector3(0,0,-.65f),1.1f,twInk.color,15);
        runtime.playWords=Text("PLAY LEVEL",play,new Vector3(-1.1f,0,-.72f),.95f,twInk,false,.14f,.025f,.02f);
        runtime.lockedWords=Text("LOCKED",play,new Vector3(0,0,-.72f),.95f,twInk,false,.14f,.025f,.02f);
        runtime.playNumberDisplays=new GameObject[50];for(int i=0;i<50;i++)runtime.playNumberDisplays[i]=Text((i+1).ToString(),play,new Vector3(4.8f,0,-.72f),1.05f,twInk,false,.14f,.025f,.02f);
        runtime.playLabel.gameObject.SetActive(false);
        runtime.statusLabel=Label(ui,"THE TWILIGHT CROSSING",new Vector3(0,1,-16),.45f,twIvory.color,25);runtime.statusLabel.transform.localRotation=Quaternion.Euler(34,0,0);
        var back=Plaque(ui,"BACK",new Vector3(-26,-.2f,-17),new Vector3(5,1.6f,.4f),.65f,twNavy);back.localRotation=Quaternion.Euler(34,0,0);runtime.backHit=back.gameObject.AddComponent<BoxCollider>();((BoxCollider)runtime.backHit).size=new Vector3(5,1.6f,.5f);
        runtime.countdownLabel=Label(ui,"START IN 30",new Vector3(25,.1f,-17),.52f,twIvory.color,9);runtime.countdownLabel.transform.localRotation=Quaternion.Euler(34,0,0);
        PolishTwilight(map);
        runtime.Fit(16f/9);runtime.Open(null);
        foreach(var t in ui.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate(true);
        // Keep authored transforms intact for the visual inspection pass.
        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAsset(map.gameObject,TwilightFolder+"/TwilightCrossing.prefab");
        if(menu.levelsRoot!=null)menu.levelsRoot.SetActive(false);menu.twilightMap=runtime;menu.levelsRoot=map.gameObject;menu.mapCamera=runtime.viewCamera;menu.worldStation=null;
        map.gameObject.SetActive(false);EditorUtility.SetDirty(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Installed TwilightCrossing: 50 platform stops, connected 3D rails and two worlds.";
    }
    static float Height(Vector3 p,bool city)=>.45f+3*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-5,0,p.z))+3*Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,10,p.z))+(city?.15f:0);
    static float GroundTilt(Vector3 p)=>-Mathf.Atan((Height(p+Vector3.forward*.1f,p.x>0)-Height(p-Vector3.forward*.1f,p.x>0))/.2f)*Mathf.Rad2Deg;
    static Vector3[] Curve(Vector3[] anchors,bool city)
    {
        var points=new List<Vector3>();for(int i=0;i<anchors.Length-1;i++)for(int j=0;j<28;j++)
        {float t=j/28f;var a=anchors[Mathf.Max(0,i-1)];var b=anchors[i];var c=anchors[i+1];var d=anchors[Mathf.Min(anchors.Length-1,i+2)];var p=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);p.y=Height(p,city);points.Add(p);}
        var last=anchors.Last();last.y=Height(last,city);points.Add(last);return points.ToArray();
    }
    static float Length(Vector3[] p){float d=0;for(int i=1;i<p.Length;i++)d+=Vector3.Distance(p[i-1],p[i]);return d;}
    static Vector3 At(Vector3[] points,float distance)
    {for(int i=1;i<points.Length;i++){float d=Vector3.Distance(points[i-1],points[i]);if(distance<=d)return Vector3.Lerp(points[i-1],points[i],distance/d);distance-=d;}return points.Last();}
    static void AddStops(Transform map,List<TwilightRailwayMap.Stop> stops,Vector3[] path,int start,int count,float offset,Material selectedMat)
    {
        float total=Length(path);
        for(int i=0;i<count;i++)
        {
            float d=Mathf.Lerp(1,total-1,i/(float)(count-1));var p=At(path,d);var tangent=(At(path,d+.1f)-At(path,d-.1f)).normalized;var side=Vector3.Cross(Vector3.up,tangent).normalized;
            // All platforms stand outside the train's swept corridor, never between rails.
            // Alternate the middle station at the tight upper bend to keep 20–22 readable.
            float platformSide=start+i==20?-1.85f:1.85f;
            var platformPosition=p+side*platformSide;platformPosition.y=Height(platformPosition,start>0)+.14f;
            var platform=Group("LevelStop_"+(start+i+1).ToString("00"),map,platformPosition);
            Box("PlatformFoot",platform,new Vector3(0,-.2f,0),new Vector3(1.8f,.45f,1.8f),start==0?twWood:twStone,.15f);
            var plate=Group("InclinedFace",platform,Vector3.up*.12f);plate.localRotation=Quaternion.Euler(-25,0,0);
            Disc("BrassCollar",plate,Vector3.zero,.89f,.18f,twGold,.04f);
            var face=Disc("LevelButton",plate,new Vector3(0,.15f,0),.80f,.25f,twIvory,.075f);
            var hit=face.AddComponent<BoxCollider>();hit.size=new Vector3(1.7f,.5f,1.7f);
            Text((start+i+1).ToString(),plate,new Vector3(0,.36f,0),.73f,twInk,true,.12f,.012f,.018f);
            var ring=Ring("Selected",plate,new Vector3(0,.32f,0),.88f,.07f,selectedMat,.055f);ring.SetActive(false);
            var complete=Disc("Completed",plate,new Vector3(.59f,.39f,-.53f),.16f,.06f,twGold,.01f);complete.SetActive(false);
            var locked=Box("Locked",plate,new Vector3(-.62f,.34f,-.53f),new Vector3(.17f,.06f,.17f),twInk,.025f);locked.SetActive(false);
            stops.Add(new TwilightRailwayMap.Stop{platform=platform,hit=hit,face=face.GetComponent<Renderer>(),selected=ring,completed=complete,locked=locked,distance=offset+d});
        }
    }
    static void Railway(Vector3[] points)
    {
        var rails=Group("ContinuousRailway",twStatic);
        foreach(int side in new[]{-1,1})
        {var edge=new Vector3[points.Length];for(int i=0;i<points.Length;i++){var t=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;edge[i]=points[i]+Vector3.Cross(Vector3.up,t)*side*.48f+Vector3.up*.08f;}Tube("ContinuousRail"+side,rails,edge,.065f,twGold);}
        float total=Length(points);for(float d=0;d<total;d+=.44f)
        {var p=At(points,d);var forward=At(points,Mathf.Min(d+.1f,total))-At(points,Mathf.Max(0,d-.1f));var tie=Box("Sleeper",rails,p-Vector3.up*.03f,new Vector3(1.35f,.12f,.20f),twSleeper,.025f);tie.transform.rotation=Quaternion.LookRotation(forward);}
    }
    static void Bridge(Vector3 a,Vector3 b)
    {
        var g=Group("TwilightBridge",twStatic);
        var path=new List<Vector3>();for(int i=0;i<=40;i++){float t=i/40f;var p=Vector3.Lerp(a,b,t);p.y+=Mathf.Sin(t*Mathf.PI)*.8f;path.Add(p);}
        for(int i=0;i<40;i++)
        {var p=(path[i]+path[i+1])*.5f;Box("BridgeDeck",g,p-Vector3.up*.18f,new Vector3(.27f,.30f,1.7f),twWood,.025f);}
        foreach(int side in new[]{-1,1})
        {
            var arch=new List<Vector3>();for(int i=0;i<=40;i++){float t=i/40f;arch.Add(new Vector3(Mathf.Lerp(a.x,b.x,t),-1.75f+Mathf.Sin(t*Mathf.PI)*(a.y+1.1f),a.z+side*.85f));}Tube("Arch",g,arch.ToArray(),.22f,twGold);
            var rim=path.Select(p=>p+new Vector3(0,.8f,side*.8f)).ToArray();Tube("Handrail",g,rim,.06f,twGold);
            for(int i=0;i<=10;i++){var p=path[i*4]+new Vector3(0,.4f,side*.8f);Box("Baluster",g,p,new Vector3(.07f,.8f,.07f),twGold,.015f);}
        }
    }
    static void Island(string name,float center,Material material,bool city)
    {
        var v=new List<Vector3>();var tri=new List<int>();var uv=new List<Vector2>();const int n=96,rings=28;
        var contour=new List<Vector3>();
        for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;float x=Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.36f)*13.5f;float z=Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.4f)*17.2f;contour.Add(new Vector3(center+x,0,z));}
        for(int ring=0;ring<=rings;ring++)for(int i=0;i<n;i++)
        {var p=Vector3.Lerp(new Vector3(center,0,0),contour[i],ring/(float)rings);p.y=Height(p,city)-.18f;v.Add(p);uv.Add(new Vector2(p.x*.08f,p.z*.08f));}
        for(int ring=0;ring<rings;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n,c=a+n,d=b+n;tri.AddRange(new[]{a,b,c,b,d,c});}
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        mesh=ToyBoxGeometry.Save(mesh,twMeshFolder+"/"+name+".asset");MeshObject(name,twStatic,Vector3.zero,mesh,material);
        v.Clear();tri.Clear();uv.Clear();
        for(int i=0;i<n;i++){var a=contour[i];var b=contour[(i+1)%n];a.y=Height(a,city)-.18f;b.y=Height(b,city)-.18f;int k=v.Count;v.AddRange(new[]{a,b,new Vector3(a.x,-2,a.z),new Vector3(b.x,-2,b.z)});tri.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});uv.AddRange(new[]{new Vector2(i*.2f,a.y*.2f),new Vector2((i+1)*.2f,b.y*.2f),new Vector2(i*.2f,0),new Vector2((i+1)*.2f,0)});}
        mesh=new Mesh{name=name+"Edge"};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=ToyBoxGeometry.Save(mesh,twMeshFolder+"/"+name+"Edge.asset");MeshObject(name+"Edge",twStatic,Vector3.zero,mesh,material);
    }
    static void Tube(string name,Transform parent,Vector3[] points,float radius,Material mat)
    {
        var v=new List<Vector3>();var tr=new List<int>();const int sides=8;
        for(int i=0;i<points.Length;i++)
        {var dir=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)]).normalized;var side=Vector3.Cross(Vector3.up,dir).normalized;var up=Vector3.Cross(dir,side);
            for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;v.Add(points[i]+radius*(side*Mathf.Cos(a)+up*Mathf.Sin(a)));}
            if(i>0)for(int j=0;j<sides;j++){int a=(i-1)*sides+j,b=(i-1)*sides+(j+1)%sides,c=i*sides+j,d=i*sides+(j+1)%sides;tr.AddRange(new[]{a,c,b,b,c,d});}}
        name=name+"_"+(twMeshSerial++);var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=ToyBoxGeometry.Save(mesh,twMeshFolder+"/"+name+".asset");MeshObject(name,parent,Vector3.zero,mesh,mat);
    }
    static Transform Sphere(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());return go.transform;}
    static Transform Plaque(Transform parent,string text,Vector3 pos,Vector3 size,float letter,Material face)
    {
        var g=Group(string.IsNullOrEmpty(text)?"PlayPlaque":text,parent,pos);Box("BrassRim",g,Vector3.zero,size,twGold,.18f);Box("Enamel",g,new Vector3(0,0,-size.z*.5f),size-new Vector3(.22f,.22f,size.z*.6f),face,.14f);
        if(text.Length>0)Text(text,g,new Vector3(0,0,-size.z*.8f),letter,twIvory,false,.11f,.012f,.02f);return g;
    }
    static TMP_Text Label(Transform parent,string text,Vector3 pos,float size,Color color,float width)
    {var go=Group("Label",parent,pos).gameObject;var t=go.AddComponent<TextMeshPro>();t.text=text;t.fontSize=size*12;t.color=color;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,2);t.enableAutoSizing=false;t.textWrappingMode=TextWrappingModes.NoWrap;t.ForceMeshUpdate(true);return t;}
    static void Scenery(WorldStationRoute3D source,Transform map)
    {
        var decor=Group("StoryScenery",twStatic);
        // Reuse recognizable authored BUCA props rather than replacing the game's visual identity.
        TeddyAndBooks(decor);
        CloneProp(source.transform.Find("WorldOneRoom/Bookshelf"),decor,new Vector3(-28,1,5),5.3f);
        CloneProp(source.transform.Find("WorldOneRoom/WarmRoomLamp"),decor,new Vector3(-27,2,12),6.5f);
        var rug=Disc("RoundPlayroomRug",decor,new Vector3(-15,Height(new Vector3(0,0,-9),false)-.04f,-9),3.5f,.07f,twTeal,.02f);
        Ring("RugGoldBorder",decor,rug.transform.localPosition+Vector3.up*.05f,3.25f,.15f,twGold,.03f);
        var star=MeshObject("RugStar",decor,rug.transform.localPosition+Vector3.up*.06f,ToyBoxGeometry.MapPrism(10,1.45f,.035f,.01f,true),twIvory);
        foreach(var part in new[]{rug.transform,star.transform,decor.Find("RugGoldBorder")})part.localRotation=Quaternion.Euler(GroundTilt(part.localPosition),0,0);
        var table=Group("ChildTable",decor,new Vector3(-16,Height(new Vector3(0,0,-6),false),-6));
        Disc("RoundTable",table,new Vector3(0,1.7f,0),1.65f,.30f,twWood,.08f);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box("TableLeg",table,new Vector3(x*.9f,.8f,z*.9f),new Vector3(.22f,1.6f,.22f),twWood,.04f);
        for(int i=0;i<3;i++){var chair=Group("LittleChair",decor,new Vector3(-19+i*2.4f,Height(new Vector3(0,0,-8),false),-8));Box("Seat",chair,new Vector3(0,.5f,0),new Vector3(.95f,.2f,.95f),twWood,.07f);Box("ChairBack",chair,new Vector3(0,1.1f,.4f),new Vector3(.95f,1.2f,.15f),twWood,.05f);foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box("Leg",chair,new Vector3(x*.35f,.2f,z*.35f),new Vector3(.14f,.65f,.14f),twWood,.025f);}
        var coral=TwMat("ToyCoral",new Color(.7f,.22f,.12f));var sage=TwMat("ToySage",new Color(.25f,.47f,.32f));
        for(int i=0;i<6;i++){var p=new Vector3(-9+(i%2)*1.2f,Height(new Vector3(0,0,-5.5f),false)+.55f+(i/2)*1.1f,-5.5f);Box("AlphabetBlock",decor,p,Vector3.one*1.05f,i%2==0?coral:sage,.09f);Text(((char)('A'+i)).ToString(),decor,p+new Vector3(0,0,-.54f),.56f,twIvory);}
        // Wooden tent and a small house inside the left loop.
        House(decor,new Vector3(-13.8f,1.6f,5),2.7f,twWood,false);
        var tent=Group("PlayTent",decor,new Vector3(-11,Height(new Vector3(0,0,-9),false),-9));foreach(int side in new[]{-1,1}){var panel=Box("Canvas",tent,new Vector3(side*.85f,1.25f,0),new Vector3(.1f,3.2f,2.5f),twIvory,.04f);panel.transform.localRotation=Quaternion.Euler(0,0,side*32);}Box("TentFloor",tent,Vector3.zero,new Vector3(3,.1f,2.6f),twTeal,.04f);
        // Skyline has warm windows, tiered roofs and brass needles without covering the level path.
        House(decor,new Vector3(12,2.5f,14),3.4f,twNavy,true);House(decor,new Vector3(17,2.5f,15),3.5f,twStone,true);House(decor,new Vector3(23,2.5f,14),2.9f,twNavy,true);
        House(decor,new Vector3(6,1.2f,19),1.9f,twNavy,false);House(decor,new Vector3(12,1.2f,19),1.9f,twStone,false);House(decor,new Vector3(27,1.2f,19),1.6f,twNavy,false);
        Station(decor,new Vector3(13,0,2));
        for(int i=0;i<7;i++){float x=8+i*2.9f;House(decor,new Vector3(x,.5f,-9),1.9f,i%2==0?twNavy:twStone,false);}
        var random=new System.Random(41);
        var runtime=map.GetComponent<TwilightRailwayMap>();
        for(int i=0;i<125;i++)
        {
            bool city=i>=70;float x=(city?4:-28)+(float)random.NextDouble()*24;float z=-14+(float)random.NextDouble()*28;var p=new Vector3(x,Height(new Vector3(x,0,z),city)-.1f,z);
            if(runtime.railway.Any(q=>new Vector2(q.x-p.x,q.z-p.z).sqrMagnitude<3.5f))continue;
            if(runtime.stops.Any(s=>new Vector2(s.platform.localPosition.x-p.x,s.platform.localPosition.z-p.z).sqrMagnitude<2.7f))continue;
            if(!city&&x>-24&&x<-8&&z>-12&&z<0)continue;
            if(city&&x>9&&x<24&&z>-1&&z<8)continue;
            Tree(decor,p,.65f+(float)random.NextDouble()*.75f);
        }
        for(int i=0;i<runtime.stops.Length;i+=3)
        {var p=runtime.stops[i].platform.localPosition;Lamp(decor,p+new Vector3(i<30?-.95f:.95f,0,.35f),i<30?1.5f:1.9f);}
        Boat(decor,new Vector3(0,-1.6f,-6),1.4f);Boat(decor,new Vector3(.3f,-1.6f,2),1);
        for(int i=0;i<22;i++){float z=-14+i*1.2f;Box("WaterGlint",decor,new Vector3(Mathf.Sin(i*2.3f)*.9f,-1.83f,z),new Vector3(.3f+i%3*.18f,.015f,.06f),twGold,.005f);}
        var moonMat=TwMat("Moon",new Color(.68f,.81f,.95f));moonMat.EnableKeyword("_EMISSION");moonMat.SetColor("_EmissionColor",new Color(.2f,.32f,.5f));
        Sphere("Moon",decor,new Vector3(30,17.3f,20),new Vector3(3.6f,3.6f,.55f),moonMat);
        for(int i=0;i<32;i++){float x=3+(float)random.NextDouble()*26;float y=11+(float)random.NextDouble()*9;Sphere("NightStar",decor,new Vector3(x,y,18),Vector3.one*(i%4==0?.10f:.05f),twGlow);}
        var cloud=TwMat("BlueCloud",new Color(.22f,.34f,.46f));for(int i=0;i<12;i++)Sphere("Cloud",decor,new Vector3(3+i*2.1f,10.1f+Mathf.Sin(i)*.4f,17.5f),new Vector3(3,1.4f,1.2f),cloud);
    }
    static void TeddyAndBooks(Transform decor)
    {
        var fur=TwMat("TeddyFur",new Color(.48f,.27f,.12f),.12f);
        var bear=Group("StoryTeddy",decor,new Vector3(-23,Height(new Vector3(0,0,14),false),14));bear.localScale=Vector3.one*2.8f;
        Sphere("Body",bear,new Vector3(0,.75f,0),new Vector3(1.65f,1.9f,1.15f),fur);
        Sphere("Head",bear,new Vector3(0,2.13f,0),new Vector3(1.57f,1.53f,1.18f),fur);
        foreach(int side in new[]{-1,1})
        {
            Sphere("Ear",bear,new Vector3(side*.7f,2.69f,0),Vector3.one*.56f,fur);
            Sphere("Foot",bear,new Vector3(side*.59f,.1f,-.45f),new Vector3(.74f,.63f,.95f),fur);
            Sphere("Arm",bear,new Vector3(side*.89f,.95f,0),new Vector3(.54f,1.05f,.56f),fur);
            Sphere("Eye",bear,new Vector3(side*.29f,2.31f,-.57f),Vector3.one*.11f,twInk);
            Sphere("Bow",bear,new Vector3(side*.22f,1.47f,-.6f),new Vector3(.49f,.36f,.16f),twNavy);
        }
        Sphere("Muzzle",bear,new Vector3(0,1.99f,-.57f),new Vector3(.7f,.51f,.34f),twIvory);
        Sphere("Nose",bear,new Vector3(0,2.13f,-.75f),new Vector3(.22f,.15f,.1f),twInk);
        for(int i=0;i<5;i++)
        {var p=new Vector3(-28+i*.75f,Height(new Vector3(0,0,13.5f),false)+1.6f,13.5f);Box("StoryBook",decor,p,new Vector3(.63f,3.2f+(i%2)*.5f,1.4f),i%2==0?twNavy:twTeal,.05f);foreach(float y in new[]{-1.15f,1.15f})Box("SpineInlay",decor,p+new Vector3(0,y,-.71f),new Vector3(.45f,.07f,.04f),twGold,.01f);}
        var lamp=Group("WarmReadingLamp",decor,new Vector3(-28,Height(new Vector3(0,0,8),false),8));Disc("Base",lamp,new Vector3(0,.1f,0),.9f,.2f,twGold,.04f);Box("Stem",lamp,new Vector3(0,1.8f,0),new Vector3(.12f,3.4f,.12f),twGold,.03f);Disc("LinenShade",lamp,new Vector3(0,3.8f,0),1.2f,1.7f,twIvory,.09f);Disc("WarmGlow",lamp,new Vector3(0,2.92f,0),1.05f,.04f,twGlow,.01f);
        for(int i=0;i<32;i++){float a=i*Mathf.PI/16;Box("Pleat",lamp,new Vector3(Mathf.Cos(a)*1.21f,3.8f,Mathf.Sin(a)*1.21f),new Vector3(.025f,1.5f,.025f),twGold,.005f);}
        var wall=TwMat("PlayroomWall",new Color(.30f,.21f,.13f));
        Box("RoomBackdrop",decor,new Vector3(-35,12,21),new Vector3(70,40,.4f),wall,.1f);
        Box("NightBackdrop",decor,new Vector3(35,12,34),new Vector3(70,40,.4f),twNavy,.1f);
        for(int i=0;i<12;i++)Box("CurtainFold",decor,new Vector3(-1.8f+i*.32f,11,20.6f),new Vector3(.22f,22,.6f),twTeal,.09f);
        Box("PresentationTable",decor,new Vector3(0,-3.9f,0),new Vector3(110,.5f,80),twWood,.2f);
        // Playroom joinery, inset window and warm decorative wall lamps.
        for(int i=0;i<8;i++)Box("WallPanelMoulding",decor,new Vector3(-30+i*3.8f,10,20.72f),new Vector3(.12f,21,.12f),twWood,.02f);
        foreach(float y in new[]{4f,19f})Box("DadoRail",decor,new Vector3(-16,y,20.6f),new Vector3(30,.18f,.16f),twGold,.025f);
        var window=Group("PlayroomWindow",decor,new Vector3(-11,12,20.35f));
        Box("WindowFrame",window,Vector3.zero,new Vector3(7.6f,7,.3f),twWood,.16f);
        Box("TwilightGlass",window,Vector3.back*.2f,new Vector3(7,6.4f,.1f),twStone,.08f);
        Box("VerticalMullion",window,Vector3.back*.3f,new Vector3(.2f,6.5f,.16f),twIvory,.03f);
        Box("HorizontalMullion",window,Vector3.back*.3f,new Vector3(7,.2f,.16f),twIvory,.03f);
        Box("Windowsill",window,new Vector3(0,-3.6f,-.5f),new Vector3(8,.3f,1.3f),twWood,.08f);
        // Foreground brass station clock, like the selected concept.
        var clock=Group("BrassClock",decor,new Vector3(26,1.4f,-16));clock.localRotation=Quaternion.Euler(-65,0,0);
        Disc("ClockRim",clock,Vector3.zero,1.6f,.2f,twGold,.04f);Disc("ClockFace",clock,new Vector3(0,.13f,0),1.4f,.07f,twIvory,.025f);
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var mark=Box("Hour",clock,new Vector3(Mathf.Sin(a)*1.15f,.19f,Mathf.Cos(a)*1.15f),new Vector3(.06f,.035f,.2f),twInk,.005f);mark.transform.localRotation=Quaternion.Euler(0,i*30,0);}
        Box("MinuteHand",clock,new Vector3(0,.22f,.4f),new Vector3(.07f,.04f,.9f),twInk,.01f);var hand=Box("HourHand",clock,new Vector3(.23f,.24f,0),new Vector3(.60f,.04f,.10f),twInk,.01f);
    }
    static void CloneProp(Transform source,Transform parent,Vector3 position,float height)
    {
        if(source==null)return;var g=Group(source.name,parent,position);var copy=Object.Instantiate(source.gameObject,g).transform;copy.gameObject.SetActive(true);copy.localPosition=Vector3.zero;copy.localRotation=Quaternion.identity;copy.localScale=Vector3.one;
        var rr=copy.GetComponentsInChildren<Renderer>();if(rr.Length==0)return;var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);float scale=height/Mathf.Max(.1f,b.size.y);copy.localPosition-=g.InverseTransformPoint(new Vector3(b.center.x,b.min.y,b.center.z));g.localScale=Vector3.one*scale;
        foreach(var c in copy.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);foreach(var l in copy.GetComponentsInChildren<Light>())Object.DestroyImmediate(l);
    }
    static void Tree(Transform parent,Vector3 p,float size)
    {
        var g=Group("CarvedPine",parent,p);Box("Trunk",g,new Vector3(0,.4f*size,0),new Vector3(.2f,.8f,.2f)*size,twSleeper,.03f);
        for(int i=0;i<3;i++)MeshObject("PineTier",g,new Vector3(0,(.7f+i*.45f)*size,0),ToyBoxGeometry.MapCone((.65f-i*.13f)*size,1.1f*size),twLeaves);
    }
    static void House(Transform parent,Vector3 p,float size,Material face,bool tower)
    {
        p.y=Height(p,p.x>0);var g=Group(tower?"CityHouse":"ToyHouse",parent,p);g.localRotation=Quaternion.Euler(0,p.x>0?-9:12,0);float h=tower?size*1.5f:size*.9f;
        Box("Walls",g,new Vector3(0,h*.5f,0),new Vector3(size,h,size*.7f),face,.09f);
        foreach(int side in new[]{-1,1}){var roof=Box("Roof",g,new Vector3(side*size*.25f,h+.28f*size,0),new Vector3(size*.7f,.15f,size*.9f),twNavy,.04f);roof.transform.localRotation=Quaternion.Euler(0,0,-side*40);}
        for(int row=0;row<(tower?3:1);row++)for(int col=0;col<3;col++)
        {var pos=new Vector3((col-1)*size*.27f,.5f+row*size*.38f,-size*.356f);Box("WindowBrass",g,pos,new Vector3(size*.19f,size*.28f,.055f),twGold,.025f);Box("WindowLight",g,pos+Vector3.back*.035f,new Vector3(size*.14f,size*.23f,.04f),twGlow,.015f);Box("Mullion",g,pos+Vector3.back*.065f,new Vector3(.035f,size*.25f,.03f),face,.005f);}
        // Deep cornices, corner masonry and recessed doorways make each miniature read as a solid building.
        Box("Foundation",g,new Vector3(0,.12f,0),new Vector3(size*1.08f,.24f,size*.78f),twStone,.07f);
        Box("Eaves",g,new Vector3(0,h,0),new Vector3(size*1.12f,.16f,size*.87f),twGold,.035f);
        for(int side=-1;side<=1;side+=2)
        {
            Box("CornerPier",g,new Vector3(side*size*.47f,h*.5f,-size*.36f),new Vector3(size*.08f,h,.10f),twStone,.025f);
            for(int row=0;row<(tower?7:3);row++)Box("CornerStone",g,new Vector3(side*size*.47f,.25f+row*h/(tower?7:3),-size*.42f),new Vector3(size*.14f,.13f,.14f),twIvory,.025f);
        }
        Box("DoorTrim",g,new Vector3(0,size*.23f,-size*.38f),new Vector3(size*.25f,size*.47f,.14f),twGold,.065f);
        Box("DoorInset",g,new Vector3(0,size*.21f,-size*.46f),new Vector3(size*.19f,size*.40f,.05f),twTeal,.045f);
        Sphere("DoorKnob",g,new Vector3(size*.06f,size*.21f,-size*.51f),Vector3.one*.07f,twGold);
        var chimney=Box("Chimney",g,new Vector3(-size*.30f,h+size*.54f,.12f),new Vector3(size*.15f,size*.42f,size*.18f),twStone,.04f);
        Box("ChimneyCap",g,chimney.transform.localPosition+Vector3.up*size*.23f,new Vector3(size*.20f,.1f,size*.24f),twIvory,.025f);
        for(int i=0;i<5;i++)
        {
            float x=(i-2)*size*.20f;float y=h+size*.54f-Mathf.Abs(x)*.80f;
            Box("RoofSeam",g,new Vector3(x,y,0),new Vector3(.035f,.035f,size*.9f),twStone,.009f);
        }
        if(tower&&size>3.4f){var t=Group("Spire",g,new Vector3(size*.35f,h,0));Box("Tower",t,new Vector3(0,size*.3f,0),new Vector3(size*.4f,size,size*.4f),face,.04f);MeshObject("SpireRoof",t,new Vector3(0,size,0),ToyBoxGeometry.MapCone(size*.36f,size),twNavy);Sphere("Finial",t,new Vector3(0,size*1.55f,0),Vector3.one*.14f,twGold);}
    }
    static void Station(Transform parent,Vector3 p)
    {
        p.y=Height(p,true);var g=Group("MoonlitStation",parent,p);g.localScale=Vector3.one*1.1f;Box("StationBase",g,new Vector3(0,.1f,0),new Vector3(7,.3f,3.7f),twStone,.15f);Box("StationBack",g,new Vector3(0,1.5f,1.2f),new Vector3(6.7f,3,1),twNavy,.1f);
        for(int i=-3;i<=3;i++)Box("LitStationWindow",g,new Vector3(i*.86f,1.6f,.65f),new Vector3(.6f,1.9f,.07f),twGlow,.09f);
        for(int i=0;i<7;i++)
        {float z=-1.5f+i*.48f;var arch=new List<Vector3>();for(int j=0;j<=30;j++){float a=j*Mathf.PI/30;arch.Add(new Vector3(Mathf.Cos(a)*3.6f,1.8f+Mathf.Sin(a)*2.2f,z));}Tube("CanopyRib"+i,g,arch.ToArray(),.065f,twGold);}
        for(int x=-3;x<=3;x++)Box("RoofGlassSlat",g,new Vector3(x,3.4f-Mathf.Abs(x)*.28f,0),new Vector3(.88f,.10f,3.2f),twNavy,.035f);
        Plaque(g,"BUCA STATION",new Vector3(0,1.6f,-1.75f),new Vector3(6.4f,.85f,.22f),.43f,twNavy);
    }
    static void Lamp(Transform parent,Vector3 p,float h)
    {
        var g=Group("BrassLamp",parent,p);Disc("Foot",g,new Vector3(0,.05f,0),.25f,.12f,twNavy,.02f);Box("Pole",g,new Vector3(0,h*.5f,0),new Vector3(.07f,h,.07f),twGold,.015f);Box("Lantern",g,new Vector3(0,h,0),new Vector3(.3f,.48f,.3f),twGlow,.04f);Box("Cap",g,new Vector3(0,h+.28f,0),new Vector3(.43f,.12f,.43f),twNavy,.04f);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("LanternPost",g,new Vector3(x*.15f,h,z*.15f),new Vector3(.035f,.5f,.035f),twGold,.008f);
        Sphere("Finial",g,new Vector3(0,h+.4f,0),Vector3.one*.12f,twGold);
    }
    static void Boat(Transform parent,Vector3 p,float scale)
    {
        var g=Group("RiverSailboat",parent,p);g.localScale=Vector3.one*scale;Box("Hull",g,Vector3.zero,new Vector3(.85f,.35f,1.8f),twWood,.16f);Box("Mast",g,new Vector3(0,1.1f,0),new Vector3(.06f,2.2f,.06f),twGold,.01f);var sail=Box("Sail",g,new Vector3(.28f,1.25f,0),new Vector3(.6f,1.4f,.07f),twIvory,.025f);sail.transform.localRotation=Quaternion.Euler(0,20,-15);
    }
    static void Combine(Transform root)
    {
        // Batch decorative meshes by material. Keep level hits, animated train and control labels separate.
        var groups=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.gameObject.activeInHierarchy&&r.sharedMaterials.Length==1&&r.GetComponent<MeshFilter>()!=null).GroupBy(r=>r.sharedMaterial).ToArray();
        int index=0;foreach(var group in groups)
        {
            var rs=group.ToArray();if(rs.Length<2)continue;var combines=rs.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{name="TwilightBatch"+index,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();mesh=ToyBoxGeometry.Save(mesh,TwilightFolder+"/Meshes/Batch"+(index++)+".asset");MeshObject(mesh.name,root,Vector3.zero,mesh,group.Key);
            foreach(var r in rs){Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);}
        }
    }
}
