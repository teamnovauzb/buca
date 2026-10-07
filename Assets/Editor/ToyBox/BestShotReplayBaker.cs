#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BestShotReplayBaker
{
    public const string Folder = "Assets/ToyBoxMenu/BestShotReplay";
    public const string PrefabPath = Folder + "/WoodenReplayFrame.prefab";
    static Material teal, edge, maple, brass, cream, ink, mint, black;

    [MenuItem("RealBuca/Toy Box 3D/Bake Best Shot Replay")]
    public static void BakeMenu() { Debug.Log(Bake()); }
    public static string Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean edit mode is required to bake the replay frame.");
        string restore = SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/ToyBoxMenu", "BestShotReplay");
            ToyBoxGeometry.Initialize();
            teal = Mat("DeepTeal", new Color(.017f,.19f,.18f), .12f,.42f);
            edge = Mat("TealLip", new Color(.04f,.35f,.32f), .15f,.48f);
            maple = Mat("WarmMaple", new Color(.98f,.86f,.66f), 0,.32f);
            maple.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));
            EditorUtility.SetDirty(maple);
            brass = Mat("Brass", new Color(.94f,.62f,.20f), .64f,.56f);
            cream = Mat("Ivory", new Color(1,.92f,.73f), .05f,.4f);
            ink = Mat("TealLetters", new Color(.012f,.105f,.10f), .10f,.38f);
            mint = Mat("MintProgress", new Color(.15f,.80f,.55f), .10f,.46f);
            black = Mat("BlackArcadeCap", new Color(.02f,.025f,.029f), .12f,.52f);
            var manager = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            var existing = UnityEngine.Object.FindAnyObjectByType<BestShotReplay>(FindObjectsInactive.Include);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var root = new GameObject("SavedBestShotReplay");
            root.transform.position = new Vector3(350,0,0);
            var view = root.AddComponent<BestShotReplay>();
            var stage = Group("Presentation", root.transform, Vector3.zero);
            view.presentation = stage.gameObject;
            view.frame = Group("WoodenVideoFrame", stage, Vector3.zero);
            view.frameRest = Vector3.zero;
            Box(view.frame,"TealOuterBody",Vector3.zero,new Vector3(16,8.65f,.56f),.62f,teal);
            Box(view.frame,"BrassOuterTrim",new Vector3(0,0,-.29f),new Vector3(15.77f,8.42f,.14f),.53f,brass);
            Box(view.frame,"TealHighlight",new Vector3(0,0,-.38f),new Vector3(15.61f,8.26f,.14f),.49f,edge);
            Box(view.frame,"MapleFrameFace",new Vector3(0,0,-.48f),new Vector3(15.38f,8.03f,.27f),.42f,maple);
            Box(view.frame,"VideoBrassBezel",new Vector3(0,.38f,-.65f),new Vector3(14.92f,6.31f,.13f),.31f,brass);
            Box(view.frame,"VideoInsetShadow",new Vector3(0,.38f,-.74f),new Vector3(14.72f,6.10f,.14f),.27f,teal);

            view.replayTexture = Target("ShotVideo",1600,650,24);
            var videoMat = ToyBoxGeometry.Save(new Material(Shader.Find("Buca/ReplayScreen")),Folder+"/VideoScreen.mat");
            videoMat.SetTexture("_MainTex",view.replayTexture); videoMat.SetFloat("_Aspect",14.55f/5.91f); videoMat.SetFloat("_Radius",.035f); EditorUtility.SetDirty(videoMat);
            var video = Part(view.frame,"LiveRecordedShot",Quad("VideoQuad"),videoMat,new Vector3(0,.38f,-.83f));
            video.transform.localScale = new Vector3(14.55f,5.91f,1);

            var title = Group("RaisedTitlePlaque",view.frame,new Vector3(0,3.83f,-.74f));
            Box(title,"TitleTealRim",Vector3.zero,new Vector3(8.8f,1.43f,.32f),.28f,teal);
            Box(title,"TitleBrassRim",new Vector3(0,0,-.18f),new Vector3(8.57f,1.23f,.14f),.23f,brass);
            Box(title,"TitleMapleFace",new Vector3(0,0,-.27f),new Vector3(8.40f,1.07f,.19f),.20f,maple);
            Label(title,"BestShotLetters","YOUR BEST SHOT",.56f,7.3f,new Vector3(0,.01f,-.47f),ink);
            foreach (int side in new[]{-1,1}) Pin(title,new Vector3(side*3.94f,0,-.39f),.10f);
            var replayTag = Group("ReplayTag",view.frame,new Vector3(-5.76f,2.79f,-.98f));
            Box(replayTag,"ReplayGoldRim",Vector3.zero,new Vector3(2.25f,.65f,.13f),.22f,brass);
            Box(replayTag,"ReplayTealInset",new Vector3(0,0,-.08f),new Vector3(2.1f,.52f,.13f),.18f,teal);
            Label(replayTag,"ReplayLetters","REPLAY",.27f,1.66f,new Vector3(0,0,-.18f),cream);

            var slow = Group("SlowMotionPlaque",view.frame,new Vector3(-5.63f,-3.36f,-.7f));
            Box(slow,"SlowBrassBorder",Vector3.zero,new Vector3(3.38f,.83f,.16f),.18f,brass);
            Box(slow,"SlowIvoryFace",new Vector3(0,0,-.1f),new Vector3(3.19f,.66f,.13f),.14f,cream);
            Label(slow,"SlowLetters","SLOW MOTION",.26f,2.58f,new Vector3(.16f,0,-.23f),ink);
            Disc(slow,"IndicatorSocket",new Vector3(-1.37f,0,-.23f),.13f,.06f,teal);
            Disc(slow,"MintIndicator",new Vector3(-1.37f,0,-.28f),.085f,.045f,mint);

            view.progressLeft=-3.25f;view.progressWidth=7.40f;
            Box(view.frame,"ProgressBrassSocket",new Vector3(.45f,-3.36f,-.73f),new Vector3(7.76f,.49f,.15f),.21f,brass);
            Box(view.frame,"ProgressTealChannel",new Vector3(.45f,-3.36f,-.83f),new Vector3(7.56f,.32f,.13f),.15f,teal);
            view.progressFill=Box(view.frame,"MintPlaybackFill",new Vector3(.45f,-3.36f,-.93f),new Vector3(view.progressWidth,.19f,.06f),.08f,mint).transform;
            view.progressKnob=Group("GoldPlaybackMarker",view.frame,new Vector3(view.progressLeft,-3.36f,-1.01f));
            Disc(view.progressKnob,"MarkerBrassRim",Vector3.zero,.23f,.1f,brass);
            Disc(view.progressKnob,"MarkerMapleFace",new Vector3(0,0,-.055f),.174f,.065f,cream);

            var skip = Group("BlackArcadeSkip",view.frame,new Vector3(5.27f,-3.36f,-.77f));
            Disc(skip,"SkipBrassSocket",Vector3.zero,.62f,.14f,brass);
            Disc(skip,"SkipTealMount",new Vector3(0,0,-.10f),.54f,.18f,teal);
            view.skipCap=Group("PressableBlackCap",skip,new Vector3(0,0,-.25f));view.skipRest=view.skipCap.localPosition;
            var button=Disc(view.skipCap,"PolishedBlackCap",Vector3.zero,.46f,.21f,black);
            var hit=button.AddComponent<SphereCollider>();hit.radius=.55f;view.skipHit=hit;
            var caption=Group("SkipNameplate",view.frame,new Vector3(6.72f,-3.36f,-.75f));
            Box(caption,"SkipBrassBorder",Vector3.zero,new Vector3(1.77f,.83f,.16f),.18f,brass);
            Box(caption,"SkipIvoryFace",new Vector3(0,0,-.1f),new Vector3(1.60f,.66f,.13f),.13f,cream);
            Label(caption,"SkipLetters","SKIP",.34f,1.24f,new Vector3(0,0,-.22f),ink);
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})Pin(view.frame,new Vector3(x*7.40f,y*3.82f,-.68f),.115f);

            view.frameCamera=Group("ReplayFrameCamera",stage,new Vector3(0,0,-22)).gameObject.AddComponent<Camera>();
            ConfigureCamera(view.frameCamera,29,100);
            view.captureTexture=Target("BackdropCapture",1600,900,24);
            view.blurScratch=Target("BackdropScratch",1600,900,0);
            view.blurredBackdrop=Target("BlurredBackdrop",1600,900,0);
            view.blurMaterial=ToyBoxGeometry.Save(new Material(Shader.Find("Hidden/Buca/GaussianBlur")),Folder+"/BackdropBlur.mat");
            var bg=ToyBoxGeometry.Save(new Material(Shader.Find("Buca/ResultsBackdrop")),Folder+"/Backdrop.mat");
            bg.SetTexture("_MainTex",view.blurredBackdrop);EditorUtility.SetDirty(bg);
            view.backdrop=Part(view.frameCamera.transform,"BlurredRoom",Quad("BackdropQuad"),bg,new Vector3(0,0,40)).transform;
            Lamp(stage,"FrameKey",new Vector3(-5,7,-7),12,new Color(1,.94f,.84f),true);
            Lamp(stage,"FrameFill",new Vector3(6,3,-5),5,new Color(.82f,.92f,1),false);
            view.replayAudio=root.AddComponent<AudioSource>();view.replayAudio.playOnAwake=false;view.replayAudio.spatialBlend=0;
            var audio=UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if(audio!=null)view.revealSound=audio.levelStartSfx;

            var recorded=Group("SavedRecordedWorld",root.transform,new Vector3(300,0,0));view.recordedStage=recorded.gameObject;
            view.savedMeshes=new MeshFilter[768];view.savedRenderers=new MeshRenderer[768];
            for(int i=0;i<768;i++)
            {
                var go=new GameObject("SavedVisualSlot"+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(recorded,false);
                view.savedMeshes[i]=go.GetComponent<MeshFilter>();view.savedRenderers[i]=go.GetComponent<MeshRenderer>();view.savedRenderers[i].enabled=false;
            }
            view.playbackCamera=Group("RecordedBoardCamera",recorded,new Vector3(0,10,-18.8f)).gameObject.AddComponent<Camera>();
            ConfigureCamera(view.playbackCamera,38,-50);view.playbackCamera.targetTexture=view.replayTexture;
            view.playbackCamera.transform.localRotation=Quaternion.Euler(28.265f,0,0);
            Lamp(recorded,"RecordedRoomFill",new Vector3(5,7,5),8,new Color(.78f,.88f,1),false);
            var probe=recorded.gameObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;
            probe.customBakedTexture=ToyBoxSurfaceBaker.StudioReflection();probe.size=new Vector3(100,50,100);probe.intensity=1.05f;
            recorded.gameObject.SetActive(false);stage.gameObject.SetActive(false);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);UnityEngine.Object.DestroyImmediate(root);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            view=instance.GetComponent<BestShotReplay>();
            view.environmentRoots=scene.GetRootGameObjects().Where(g=>g.name=="SavedToyPlayroom").Select(g=>g.transform).ToArray();
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            manager.bestShotReplay=view;EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return "Saved wooden replay frame, 768 prebuilt render slots, video targets, arcade Skip and Game scene wiring.";
        }
        finally {if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);}
    }
    static void ConfigureCamera(Camera camera,float fov,float depth)
    {
        camera.fieldOfView=fov;camera.depth=depth;camera.nearClipPlane=.1f;camera.farClipPlane=90;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.65f,.57f,.43f);
        var data=camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());data.renderPostProcessing=false;
    }
    static Transform Group(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static Material Mat(string name,Color color,float metallic,float smoothness)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smoothness);
        return ToyBoxGeometry.Save(mat,Folder+"/"+name+".mat");
    }
    static RenderTexture Target(string name,int width,int height,int depth)
    {return ToyBoxGeometry.Save(new RenderTexture(width,height,depth,RenderTextureFormat.ARGB32){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp},Folder+"/"+name+".renderTexture");}
    static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 position)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
    }
    static GameObject Disc(Transform parent,string name,Vector3 position,float radius,float depth,Material material)
    {
        var go=Part(parent,name,ToyBoxGeometry.Disc(radius,depth,Mathf.Min(.055f,depth*.3f)),material,position);
        go.transform.localRotation=Quaternion.Euler(90,0,0);return go;
    }
    static void Pin(Transform parent,Vector3 position,float radius) {Disc(parent,"BrassPin",position,radius,.08f,brass);}
    static void Lamp(Transform parent,string name,Vector3 position,float intensity,Color color,bool shadows)
    {var light=Group(name,parent,position).gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=30;light.intensity=intensity;light.color=color;light.shadows=shadows?LightShadows.Soft:LightShadows.None;}
    static void Label(Transform parent,string name,string text,float height,float width,Vector3 position,Material material)
    {
        var mesh=UnityEngine.Object.Instantiate(ToyBoxGeometry.Text(text,.242f,.035f,.035f));
        var vertices=mesh.vertices;var center=mesh.bounds.center;float scale=Mathf.Min(height/mesh.bounds.size.y,width/mesh.bounds.size.x);
        for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3((vertices[i].x-center.x)*scale,(vertices[i].y-center.y)*scale,vertices[i].z*scale);
        mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateTangents();
        Part(parent,name,ToyBoxGeometry.Save(mesh,Folder+"/"+name+".asset"),material,position);
    }
    static Mesh Quad(string name)
    {
        var mesh=new Mesh{name=name};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Folder+"/"+name+".asset");
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,float radius,Material material)
    {
        radius=Mathf.Min(radius,Mathf.Min(size.x,size.y)*.49f);
        var outline=new List<Vector2>();
        for(int corner=0;corner<4;corner++)for(int j=0;j<=12;j++)
        {
            float angle=(corner*90+j*7.5f)*Mathf.Deg2Rad;
            outline.Add(new Vector2((corner==0||corner==3?1:-1)*(size.x*.5f-radius)+Mathf.Cos(angle)*radius,(corner<2?1:-1)*(size.y*.5f-radius)+Mathf.Sin(angle)*radius));
        }
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int count=outline.Count;
        float bevel=Mathf.Min(.085f,size.z*.30f);
        for(int ring=0;ring<4;ring++)foreach(var p in outline)
        {
            float inset=ring==0||ring==3?bevel:0;
            var q=new Vector3(p.x*(1-inset/(size.x*.5f)),p.y*(1-inset/(size.y*.5f)),ring==0?-size.z*.5f:ring==1?-size.z*.5f+bevel:ring==2?size.z*.5f-bevel:size.z*.5f);
            vertices.Add(q);uv.Add(new Vector2(q.x,q.y)*.2f);
        }
        for(int ring=0;ring<3;ring++)for(int j=0;j<count;j++)
        {int a=ring*count+j,b=ring*count+(j+1)%count,c=b+count,d=a+count;triangles.AddRange(new[]{a,b,c,a,c,d});}
        for(int side=0;side<2;side++)
        {
            int center=vertices.Count;float z=(side==0?-1:1)*size.z*.5f;vertices.Add(new Vector3(0,0,z));uv.Add(Vector2.zero);
            foreach(var p in outline){var q=new Vector3(p.x*(1-bevel/(size.x*.5f)),p.y*(1-bevel/(size.y*.5f)),z);vertices.Add(q);uv.Add(new Vector2(q.x,q.y)*.2f);}
            for(int j=0;j<count;j++){int a=center+1+j,b=center+1+(j+1)%count;triangles.AddRange(side==0?new[]{center,b,a}:new[]{center,a,b});}
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        return Part(parent,name,ToyBoxGeometry.Save(mesh,Folder+"/"+name+".asset"),material,position);
    }
}
#endif
