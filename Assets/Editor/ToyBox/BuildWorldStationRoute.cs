#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    const string StationFolder="Assets/ToyBoxMenu/WorldStation";
    static Material stationBlue,stationTeal,stationMaple,stationGold,stationCream,stationDark,stationGlow,stationGreen;
    static Material stationInk,stationSteel,stationDoorInset,stationContact,stationWarmPool;
    static GameObject StationBox(string n,Transform p,Vector3 at,Vector3 size,Material m,float radius=.07f)=>Box(n,p,at,size,m,radius);
    static GameObject StationSphere(string n,Transform p,Vector3 at,Vector3 scale,Material m)
    {
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);var mesh=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
        var g=MeshObject(n,p,at,mesh,m);g.transform.localScale=scale;return g;
    }
    static Material StationMaterial(string n,Color c,float metal=0,float grain=.18f)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=n};m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal*.5f);m.SetFloat("_Smoothness",metal>.3f?.64f:.35f);
        m.EnableKeyword("_EMISSION");
        return ToyBoxGeometry.Save(m,StationFolder+"/"+n+".mat");
    }
    static void StationText(string text,Transform p,Vector3 at,float h,Material m,bool flat=false,float width=100)
    {
        var go=Text(text,p,at,1,m,flat,.15f,.017f,.027f);var b=go.GetComponent<MeshFilter>().sharedMesh.bounds;
        go.transform.localScale=Vector3.one*Mathf.Min(h/b.size.y,width/b.size.x);
    }
    static void StationPlaque(string text,Transform p,Vector3 at,Vector3 size,float h)
    {
        StationBox(text+"Frame",p,at,size,stationGold,.10f);
        StationBox(text+"Face",p,at+Vector3.back*.09f,size-new Vector3(.10f,.10f,.02f),stationMaple,.10f);
        StationText(text,p,at+Vector3.back*(size.z*.5f+.16f),h,stationBlue,false,size.x-.55f);
        var lettering=p.Find(text);
        if(lettering!=null)
        {
            var bounds=lettering.GetComponent<MeshFilter>().sharedMesh.bounds;
            lettering.localPosition-=new Vector3(bounds.center.x*lettering.localScale.x,bounds.center.y*lettering.localScale.y,0);
        }
        foreach(int side in new[]{-1,1})StationSphere("BrassPin",p,at+new Vector3(side*(size.x*.5f-.16f),0,-size.z*.5f-.11f),Vector3.one*.13f,stationGold);
    }
    [MenuItem("RealBuca/Toy Box 3D/Bake World Station Route")]
    public static void BakeWorldStation()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit scene required.");
        if(!AssetDatabase.IsValidFolder(StationFolder))AssetDatabase.CreateFolder(Root,"WorldStation");
        PrepareFolders();ToyBoxGeometry.Initialize();LoadWorkshopMaterials();glow=ExistingWorkshopMaterial("MintLight");
        stationBlue=StationMaterial("StationBlue",new Color(.025f,.13f,.33f),.28f,.12f);
        stationTeal=StationMaterial("StationTeal",new Color(.035f,.36f,.30f),.20f,.16f);
        stationMaple=StationMaterial("StationMaple",new Color(1f,.84f,.61f),0,.42f);
        stationGold=StationMaterial("StationBrass",new Color(.84f,.49f,.15f),.78f,0);
        stationCream=StationMaterial("StationIvory",new Color(1,.90f,.68f),0,.14f);
        stationDark=StationMaterial("StationWalnut",new Color(.14f,.08f,.035f),0,.4f);
        stationGreen=StationMaterial("StationLeaf",new Color(.26f,.44f,.12f),0,.16f);
        stationGlow=StationMaterial("StationGoldenLight",new Color(1,.77f,.43f),0,0);stationGlow.SetColor("_EmissionColor",new Color(1.25f,.65f,.22f));
        EditorUtility.SetDirty(stationGlow);
        PrepareStationPolishMaterials();
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var menu=prefab.GetComponent<ToyBoxMenuController>();var map=menu.levelsRoot.transform;
            foreach(var old in map.GetComponentsInChildren<WorldStationRoute3D>(true))UnityEngine.Object.DestroyImmediate(old);
            foreach(string name in new[]{"WorldStationExtension","BonusWorldMap","CozyStationRoom","WorldOneRoom","WorldTransitionCurtain","StationSmoke"})if(map.Find(name)!=null)UnityEngine.Object.DestroyImmediate(map.Find(name).gameObject);
            if(map.Find("SmallEngine")!=null)map.Find("SmallEngine").gameObject.SetActive(false);
            foreach(Transform child in map)if(child.name=="SideRail"&&child.localPosition.x>0)child.gameObject.SetActive(false);
            var view=map.gameObject.AddComponent<WorldStationRoute3D>();menu.worldStation=view;view.viewCamera=menu.mapCamera;view.ivory=menu.mapPuckMaterial;view.locked=menu.mapLockedMaterial;
            var content=new List<GameObject>();
            foreach(Transform c in map)if(c.name.StartsWith("Chapter_")||new[]{"TrainHeader","SelectionTimer","TimerPedestal","BACK_Button"}.Contains(c.name))content.Add(c.gameObject);
            view.firstWorldContent=content.ToArray();view.firstWorldButtons=menu.levelButtons;view.chapterLocks=menu.chapterLocks;
            var ext=Group("WorldStationExtension",map);
            // Open the right border where the turnout leaves the oval.
            StationBox("RightRailFront",ext,new Vector3(10,.28f,-4.60f),new Vector3(.30f,.42f,5.80f),blue,.12f);
            StationBox("RightRailBack",ext,new Vector3(10,.28f,4.90f),new Vector3(.30f,.42f,5.20f),blue,.12f);
            BuildStationPlatform(ext);
            BuildStationRailway(view,ext);BuildStationGateway(view,ext);BuildStationLocomotive(view,ext,map);
            BuildStationRoom(map);BuildOriginalWorldOneRoom(map);BuildBonusMap(view,map);
            view.worldOneRoom=map.Find("WorldOneRoom").gameObject;view.worldTwoCity=map.Find("CozyStationRoom").gameObject;
            view.steamAudio=Group("TrainMovementLoop",ext).gameObject.AddComponent<AudioSource>();view.steamAudio.playOnAwake=false;view.steamAudio.spatialBlend=0;view.steamAudio.volume=0;
            view.eventAudio=Group("StationWhistleAndDeparture",ext).gameObject.AddComponent<AudioSource>();view.eventAudio.playOnAwake=false;view.eventAudio.spatialBlend=0;
            view.movingTrain=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/StationMovingTrain.wav");
            view.departure=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/StationDeparture.wav");
            var details=map.Find("ToyStationDetails");if(details!=null)foreach(Transform detail in details)if(detail.name!="FrontBucaBadge")detail.gameObject.SetActive(false);
            view.framing=new Bounds(new Vector3(3.0f,.75f,1.2f),new Vector3(29.0f,4.5f,17.5f));
            menu.mapCamera.transform.localRotation=Quaternion.Euler(38,0,0);menu.mapCamera.fieldOfView=38;menu.mapCamera.allowHDR=true;
            var data=menu.mapCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1<<29;
            var curtainRoot=new GameObject("WorldTransitionCurtain",typeof(RectTransform),typeof(Canvas));
            curtainRoot.transform.SetParent(map,false);
            var curtainCanvas=curtainRoot.GetComponent<Canvas>();curtainCanvas.renderMode=RenderMode.ScreenSpaceCamera;curtainCanvas.worldCamera=menu.mapCamera;curtainCanvas.planeDistance=1;curtainCanvas.sortingOrder=30000;
            var curtain=new GameObject("FullScreenFade",typeof(RectTransform),typeof(UnityEngine.UI.Image));curtain.transform.SetParent(curtainRoot.transform,false);
            var rect=curtain.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            view.transitionCurtain=curtain.GetComponent<UnityEngine.UI.Image>();view.transitionCurtain.color=new Color(.025f,.045f,.085f,0);view.transitionCurtain.raycastTarget=false;
            BuildWorldVictory(view);
            BuildWorldWelcome(view);
            BuildStationVolume(ext);view.SetUnlocked(false);view.ResetWorld();menu.FitMapCamera(16f/9f);
            BatchStation(ext,view.train,view.leftDoor,view.rightDoor,view.switchLever,view.lockedSign.transform,view.openSign.transform,view.gateSelection.transform,view.gatewayGlow.transform,view.worldOneLabel.transform,view.worldTwoLabel.transform);
            view.SetGateSelected(false);
            var timerMethod=typeof(ToyBoxMenuController).GetMethod("SetMapClock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);timerMethod.Invoke(menu,new object[]{30});
            PrefabUtility.SaveAsPrefabAsset(map.gameObject,StationFolder+"/WorldStationJourney.prefab");
            PrefabUtility.SaveAsPrefabAsset(map.gameObject,Root+"/Prefabs/TrainLevelSelector.prefab");
            PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        string restore=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            if(manager.worldStationJourney!=null)UnityEngine.Object.DestroyImmediate(manager.worldStationJourney.gameObject);
            var journey=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StationFolder+"/WorldStationJourney.prefab"),scene);
            journey.name="WorldStationJourney";journey.transform.position=new Vector3(1400,0,0);
            manager.worldStationJourney=journey.GetComponent<WorldStationRoute3D>();manager.worldStationJourney.viewCamera.depth=110;
            journey.SetActive(false);EditorUtility.SetDirty(manager);EditorSceneManager.SaveScene(scene);
        }
        finally{EditorSceneManager.OpenScene(restore);}
        AssetDatabase.SaveAssets();Debug.Log("WORLD_STATION_SAVED: real 3D route, smoke, train, doors, room and 20 bonus selectors.");
    }
    static Vector3 StationPath(float t)
    {
        // Smooth turnout to the right followed by a turn into the rear station.
        var a=new Vector3(8.30f,.30f,-1.25f);var b=new Vector3(14.70f,.30f,-1.25f);var c=new Vector3(14.25f,.30f,6.50f);
        if(t<=.65f){float u=t/.65f;return (1-u)*(1-u)*a+2*(1-u)*u*b+u*u*new Vector3(14.25f,.30f,3.25f);}
        return Vector3.Lerp(new Vector3(14.25f,.30f,3.25f),c,(t-.65f)/.35f);
    }
    static Mesh StationArch(float radius,float width,float depth)
    {
        var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();const int length=72,around=24;
        for(int i=0;i<=length;i++)for(int j=0;j<=around;j++)
        {
            float a=i*Mathf.PI/length,b=j*Mathf.PI*2/around;
            float radial=width*.5f*Mathf.Sign(Mathf.Cos(b))*Mathf.Pow(Mathf.Abs(Mathf.Cos(b)),.45f);
            float z=depth*.5f*Mathf.Sign(Mathf.Sin(b))*Mathf.Pow(Mathf.Abs(Mathf.Sin(b)),.45f);
            vs.Add(new Vector3(Mathf.Cos(a)*(radius+radial),Mathf.Sin(a)*(radius+radial),z));uv.Add(new Vector2(i/(float)length,j/(float)around));
            if(i<length&&j<around){int k=i*(around+1)+j,n=k+around+1;ts.AddRange(new[]{k,n,k+1,k+1,n,n+1});}
        }
        var mesh=new Mesh{name="ContinuousArch"};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        return ToyBoxGeometry.Save(mesh,StationFolder+"/Arch_"+radius.ToString("F2")+"_"+width.ToString("F2")+"_"+depth.ToString("F2")+".asset");
    }
    static void BuildStationRailway(WorldStationRoute3D view,Transform ext) => BuildPolishedStationRailway(view,ext);
    static void BuildStationGateway(WorldStationRoute3D view,Transform ext) => BuildPolishedStationGateway(view,ext);
    static void BuildStationLocomotive(WorldStationRoute3D view,Transform ext,Transform map)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/AllAboard/AllAboardCompletion.prefab").GetComponent<AllAboardCompletion3D>();
        view.train=UnityEngine.Object.Instantiate(source.train,ext);view.train.name="WaitingSteamLocomotive";view.trainScale=Vector3.one*.66f;
        view.train.localScale=view.trainScale;
        foreach(var r in view.train.GetComponentsInChildren<MeshRenderer>(true))
        {
            string name=r.sharedMaterial.name;
            if(name=="Teal_Illuminated")r.sharedMaterial=stationBlue;
            else if(name=="Brass_Illuminated")r.sharedMaterial=stationGold;
        }
        foreach(int side in new[]{-1,1})
        {
            foreach(float y in new[]{1.27f,2.50f})StationBox("CreamCabTrim",view.train,new Vector3(-1.65f,y,side*1.014f),new Vector3(1.37f,.24f,.045f),stationCream,.025f);
            foreach(float x in new[]{-2.28f,-1.02f})StationBox("CreamCabPillar",view.train,new Vector3(x,1.96f,side*1.014f),new Vector3(.17f,1.2f,.045f),stationCream,.025f);
        }
        PolishStationLocomotive(view);
        view.wheels=view.train.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==view.train && t.name.StartsWith("Wheel")).ToArray();
        view.chimney=Group("SmokeOutlet",view.train,new Vector3(1.2f,2.95f,0));
        var white=StationMaterial("SoftSteam",new Color(.98f,.97f,.92f),0,0);
        var smokeRoot=Group("StationSmoke",map);view.smoke=new Transform[12];
        for(int i=0;i<view.smoke.Length;i++)
        {
            view.smoke[i]=Group("SteamPuff"+i,smokeRoot);
            StationSphere("SteamCloud",view.smoke[i],Vector3.zero,new Vector3(1.15f,1,1),white);
            StationSphere("SteamCloudLobe",view.smoke[i],new Vector3(.22f,.18f,.10f),Vector3.one*.85f,white);
            // Animate in map coordinates; the root has identity transform.
        }
    }
    static void BuildBonusMap(WorldStationRoute3D view,Transform map)
    {
        var bonus=Group("BonusWorldMap",map);view.bonusRoot=bonus.gameObject;
        var title=Group("WorldTwoHeader",bonus,new Vector3(0,2.3f,6.7f));StationPlaque("WORLD 2 - BONUS LEVELS",title,Vector3.zero,new Vector3(13.2f,1.25f,.35f),.55f);
        view.bonusButtons=new ToyBoxMenuController.PuckButton[20];
        for(int c=0;c<4;c++)
        {
            var car=Group("BonusCarriage"+c,bonus,new Vector3(c%2==0?-4.7f:3.9f,.24f,c<2?3.2f:-3.4f));var paint=new[]{blue,mint,yellow,coral}[c];
            StationBox("MapleChassis",car,new Vector3(0,.42f,0),new Vector3(7.1f,.32f,3.6f),stationMaple,.14f);
            StationBox("TrayFloor",car,new Vector3(0,.80f,0),new Vector3(7.2f,.44f,3.7f),paint,.16f);
            foreach(int side in new[]{-1,1})
            {
                StationBox("LongRail",car,new Vector3(0,1.12f,side*1.75f),new Vector3(7.1f,.7f,.22f),paint,.10f);
                StationBox("SideRail",car,new Vector3(side*3.48f,1.12f,0),new Vector3(.22f,.7f,3.5f),paint,.10f);
                foreach(float x in new[]{-2.5f,2.5f})
                {var wheel=Disc("CarriageWheel",car,new Vector3(x,.43f,side*1.91f),.47f,.23f,stationMaple,.06f);wheel.transform.localRotation=Quaternion.Euler(90,0,0);var hub=Disc("PaintedHub",car,new Vector3(x,.43f,side*2.05f),.29f,.1f,paint,.03f);hub.transform.localRotation=Quaternion.Euler(90,0,0);}
            }
            StationPlaque("BONUS "+(c*5+1)+" - "+(c*5+5),car,new Vector3(0,1.12f,-2.16f),new Vector3(3.35f,.64f,.16f),.24f);
            for(int j=0;j<5;j++)
            {
                int level=c*5+j+31;Vector3 at=j<3?new Vector3((j-1)*1.85f,1.03f,.72f):new Vector3((j-3.5f)*1.85f,1.03f,-.80f);
                var button=Button(car,level.ToString(),at,.57f,view.ivory,.43f,false);
                UnityEngine.Object.DestroyImmediate(button.cap.Find(level.ToString()).gameObject);
                Text(level.ToString(),button.cap,new Vector3(0,.19f,0),.39f,ink,true,.06f,.008f,.005f);
                button.lockMark=Group("Locked",button.cap,new Vector3(.38f,.20f,.34f)).gameObject;
                StationBox("LockBase",button.lockMark.transform,Vector3.zero,new Vector3(.16f,.07f,.18f),stationGold,.025f);
                AddLevelMarkers(button);view.bonusButtons[c*5+j]=button;
            }
        }
        view.bonusBack=Button(bonus,"BACK",new Vector3(-8.65f,.20f,-6.45f),.65f,view.ivory,.23f,false);view.bonusBack.cap.Find("BACK").GetComponent<Renderer>().sharedMaterial=ink;
        StationText("WORLD 1",bonus,new Vector3(-8.65f,.23f,-7.45f),.22f,ink,true);
        BuildSignalTimer(view,bonus);
        bonus.gameObject.SetActive(false);
    }
    static void BuildStationRoom(Transform map)
    {
        var room=Group("CozyStationRoom",map);
        BuildStationRoomDepth(room);
        BatchStation(room);
    }
    static void BuildStationVolume(Transform root)
    {
        var g=Group("StationBloom",root).gameObject;g.layer=29;var volume=g.AddComponent<Volume>();volume.isGlobal=false;volume.priority=90;
        var box=g.AddComponent<BoxCollider>();box.isTrigger=true;box.size=Vector3.one*160;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.05f);bloom.intensity.Override(.65f);bloom.scatter.Override(.62f);
        var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
        profile=ToyBoxGeometry.Save(profile,StationFolder+"/StationBloom.asset");volume.sharedProfile=profile;
        foreach(var component in profile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
    }
    static void BatchStation(Transform scope,params Transform[] exclusions)
    {
        var groups=new Dictionary<Material,List<MeshFilter>>();
        foreach(var f in scope.GetComponentsInChildren<MeshFilter>(true))
        {
            if(exclusions.Any(x=>x!=null&&(f.transform==x||f.transform.IsChildOf(x))))continue;
            if(f.name.StartsWith("Sparkle")||f.transform.parent.name.StartsWith("GoldenSparkle")||f.name=="SmoothCarvedArch"||f.name=="WORLD 2"||f.name=="20 NEW LEVELS")continue;
            var r=f.GetComponent<MeshRenderer>();if(r==null||f.sharedMesh==null)continue;
            if(!groups.ContainsKey(r.sharedMaterial))groups[r.sharedMaterial]=new List<MeshFilter>();groups[r.sharedMaterial].Add(f);
        }
        int index=0;
        foreach(var pair in groups)
        {
            var mesh=new Mesh{name=scope.name+index,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(pair.Value.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=scope.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            mesh=SaveStationBatch(mesh,scope.name,index);
            MeshObject("SavedBatch"+index,scope,Vector3.zero,mesh,pair.Key);index++;
            foreach(var f in pair.Value){UnityEngine.Object.DestroyImmediate(f.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(f);}
        }
    }
    // Content-addressed batches preserve every mesh channel and avoid mutating a
    // mesh that the open Editor is already using in its native render buffers.
    static Mesh SaveStationBatch(Mesh mesh,string scope,int index)
    {
        using(var stream=new System.IO.MemoryStream())
        using(var writer=new System.IO.BinaryWriter(stream))
        {
            foreach(var v in mesh.vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
            foreach(var n in mesh.normals){writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);}
            foreach(var uv in mesh.uv){writer.Write(uv.x);writer.Write(uv.y);}
            foreach(var i in mesh.triangles)writer.Write(i);
            writer.Flush();
            string hash;
            using(var sha=System.Security.Cryptography.SHA256.Create())hash=System.BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","").Substring(0,16);
            string path=StationFolder+"/Batch_"+scope+"_"+index+"_"+hash+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved!=null){UnityEngine.Object.DestroyImmediate(mesh);return saved;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
#endif
