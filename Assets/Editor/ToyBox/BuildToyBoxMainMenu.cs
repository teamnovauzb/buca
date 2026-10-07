using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit, repeatable editor command. Nothing runs automatically on import.
/// Builds solid 3D content, saves nested prefabs, and optionally installs the
/// result in MainMenu while preserving the Luxodd and audio service objects.
/// </summary>
public static partial class BuildToyBoxMainMenu
{
    const string Root = "Assets/ToyBoxMenu";
    const string MenuScene = "Assets/Scenes/MainMenu.unity";
    const string PrefabPath = Root + "/Prefabs/ToyBoxMainMenu.prefab";
    const string BackupPath = Root + "/Backup/MainMenu-BeforeToyBox.unity";
    static Material blue, coral, yellow, mint, wood, cream, ink, glow, black, locked, surface, roomPaint, floorPaint;
    static int prefabSerial;

    [MenuItem("RealBuca/Toy Box 3D/1 - Generate and Assign Main Menu")]
    public static void GenerateAndAssign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before generating the menu.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders();
        // Keep the first pre-redesign version, even when the generator is rerun.
        if (!File.Exists(BackupPath) && !AssetDatabase.CopyAsset(MenuScene, BackupPath))
            throw new IOException("Could not back up MainMenu. No scene was replaced.");
        GameObject prefab = GeneratePrefab();
        Scene scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<ToyBoxMenuController>() != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                continue;
            }
            DisableLegacyPresentation(root);
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        ConfigureEnvironment();
        Validate(instance);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        InstallGameplaySkins();
        BootFromMainMenu.SetEnabled(true);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = instance;
        Debug.Log("[ToyBox] MainMenu rebuilt and wired. Original scene: " + BackupPath +
            ". Solid meshes and prefabs: " + Root + ". Five board and puck skins are assigned in Game.");
    }

    [MenuItem("RealBuca/Toy Box 3D/2 - Generate Separate Preview Scene")]
    public static void GeneratePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders();
        GameObject prefab = GeneratePrefab();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        ConfigureEnvironment();
        Validate(instance);
        EditorSceneManager.SaveScene(scene, Root + "/ToyBoxPreview.unity");
        BootFromMainMenu.SetEnabled(false);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = instance;
        Debug.Log("[ToyBox] Separate preview generated. MainMenu was not changed.");
    }

    [MenuItem("RealBuca/Toy Box 3D/3 - Restore Original Main Menu")]
    public static void Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(BackupPath)) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene backup = EditorSceneManager.OpenScene(BackupPath, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(backup, MenuScene);
        BootFromMainMenu.SetEnabled(true);
        Debug.Log("[ToyBox] Original MainMenu restored from the saved backup.");
    }

    static void PrepareFolders()
    {
        foreach (string folder in new[] { Root, Root+"/Meshes", Root+"/Materials", Root+"/Prefabs", Root+"/Backup" })
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\','/'), Path.GetFileName(folder));
    }

    static void DisableLegacyPresentation(GameObject root)
    {
        // Keep objects and serialized service references in place for recovery.
        bool containsService = false;
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (IsService(behaviour)) { containsService = true; break; }
        if (!containsService) { root.SetActive(false); return; }
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
        foreach (Camera camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
        foreach (Light light in root.GetComponentsInChildren<Light>(true)) light.enabled = false;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Volume volume in root.GetComponentsInChildren<Volume>(true)) volume.enabled = false;
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            if (IsService(behaviour)) continue;
            behaviour.enabled = false;
        }
    }

    static bool IsService(MonoBehaviour behaviour)
    {
        if (behaviour == null) return false;
        return behaviour is AudioManager || behaviour is LuxoddGameBridge || behaviour is LuxoddPersistor
            || behaviour is BucaInGameTransactionController || (behaviour.GetType().FullName ?? "").StartsWith("Luxodd.");
    }

    internal static GameObject GeneratePrefab()
    {
        PrepareFolders(); ToyBoxGeometry.Initialize(); prefabSerial = 0;
        MakeMaterials(); MakeSkinMaterials();
        Scene staging = EditorSceneManager.NewPreviewScene();
        GameObject root = new GameObject("ToyBoxMainMenu");
        SceneManager.MoveGameObjectToScene(root, staging);
        try
        {
            var controller = root.AddComponent<ToyBoxMenuController>();
            controller.lockedMaterial = locked; controller.levelMaterial = yellow;
            BuildTable(root.transform, controller);
            BuildLogo(root.transform);
            BuildRoom(root.transform);
            controller.menuFramingPadding = 1.32f;
            controller.menuVerticalFramingOffset = -.30f;
            BuildMenuControls(root.transform, controller);
            BuildCameraAndLights(root.transform, controller);
            PolishWorkshopLighting(root.transform, controller);
            BuildSkinPreview(root.transform, controller);
            var framing = new List<Vector3>();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform.IsChildOf(root.transform.Find("ToyRoomScenery")) || renderer.transform.IsChildOf(controller.previewRoot.transform) || renderer.transform.IsChildOf(controller.levelsRoot.transform)) continue;
                Bounds bounds = renderer.bounds;
                for (int i=0;i<8;i++) framing.Add(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            }
            controller.framingPoints=framing.ToArray();
            controller.FitHomeCamera(16f/9f);
            controller.SetClock(30);
            AddSurfaceShine(root.transform,controller);
            Validate(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            return prefab;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(staging);
        }
    }

    static void MakeMaterials()
    {
        Texture2D grain = ToyBoxSurfaceBaker.WoodGrain(false);
        Texture2D paintGrain = ToyBoxSurfaceBaker.WoodGrain(false, true);
        Texture2D normal = ToyBoxSurfaceBaker.WoodGrain(true);
        blue=Material("CobaltPaint",new Color(.105f,.28f,.59f),.52f,paintGrain);
        coral=Material("CoralPaint",new Color(.84f,.32f,.245f),.52f,paintGrain);
        yellow=Material("HoneyYellow",new Color(.92f,.65f,.16f),.49f,paintGrain);
        mint=Material("MintPaint",new Color(.32f,.61f,.48f),.49f,paintGrain);
        wood=Material("NaturalBirch",new Color(.88f,.73f,.52f),.31f,grain);
        surface=Material("MaplePlayingSurface",new Color(.68f,.56f,.39f),.18f);
        surface.SetTexture("_BaseMap",null);
        roomPaint=Material("WarmRoomPaint",new Color(.88f,.82f,.72f),.12f);
        floorPaint=Material("WarmFloorPaint",new Color(.66f,.61f,.53f),.16f);
        cream=Material("Ivory",new Color(.93f,.88f,.75f),.34f);
        foreach(var material in new[]{blue,coral,yellow,mint,wood,surface})
        {
            material.SetTexture("_BumpMap",normal); material.SetFloat("_BumpScale",material==surface?.035f:material==wood?.5f:.24f);
            material.EnableKeyword("_NORMALMAP"); EditorUtility.SetDirty(material);
        }
        ink=Material("NavyLettering",new Color(.035f,.095f,.18f),.25f);
        black=Material("ClockRecess",new Color(.014f,.035f,.031f),.32f);
        locked=Material("LockedLevel",new Color(.30f,.37f,.42f),.2f);
        glow=Material("MintLight",new Color(.34f,1f,.77f),.35f);
        glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor",new Color(.22f,1f,.65f)*1.5f);
        EditorUtility.SetDirty(glow);
    }

    static Material Material(string name, Color color, float smoothness, Texture2D texture=null)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) throw new InvalidOperationException("The URP Lit shader is required.");
        var material=new Material(shader) { name=name };
        material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",smoothness);
        if(texture!=null) material.SetTexture("_BaseMap",texture);
        return ToyBoxGeometry.Save(material,Root+"/Materials/"+name+".mat");
    }

    static Transform Group(string name, Transform parent, Vector3 position=default)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; return go.transform;
    }
    static GameObject MeshObject(string name,Transform parent,Vector3 position,Mesh mesh,Material material)
    {
        var go=Group(name,parent,position).gameObject;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.On; renderer.receiveShadows=true;
        return go;
    }
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,float bevel=.08f)
        => MeshObject(name,parent,position,ToyBoxGeometry.RoundedBox(size,bevel),material);
    static GameObject Disc(string name,Transform parent,Vector3 position,float radius,float height,Material material,float bevel=.06f)
        => MeshObject(name,parent,position,ToyBoxGeometry.Disc(radius,height,bevel),material);
    static GameObject Ring(string name,Transform parent,Vector3 position,float radius,float width,Material material,float height=.02f,float arc=360)
        => MeshObject(name,parent,position,ToyBoxGeometry.Ring(radius,width,height,arc),material);
    static GameObject Text(string text,Transform parent,Vector3 position,float height,Material material,bool onTable=false,float depth=.1f,float bevel=0,float weight=.025f)
    {
        var go=MeshObject(text,parent,position,ToyBoxGeometry.Text(text,depth,bevel,weight),material);
        go.transform.localScale=Vector3.one*height;
        if(onTable) go.transform.localRotation=Quaternion.Euler(90,0,0);
        return go;
    }
    static void SavePart(Transform root,string name)
        => PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Root+"/Prefabs/"+name+".prefab",InteractionMode.AutomatedAction);

    static void BuildTable(Transform root,ToyBoxMenuController controller)
    {
        var table=Group("ToyTable",root);
        Box("BlueCabinet",table,new Vector3(0,1.12f,0),new Vector3(12.5f,1.46f,7.8f),blue,.24f);
        Box("LowerCabinetTrim",table,new Vector3(0,.51f,0),new Vector3(12.34f,.13f,7.69f),blue,.06f);
        Box("BirchPlayingSurface",table,new Vector3(0,1.90f,0),new Vector3(11.65f,.15f,6.95f),surface,.07f);
        Box("LeftRail",table,new Vector3(-6,2.02f,0),new Vector3(.5f,.45f,7.4f),blue,.18f);
        Box("RightRail",table,new Vector3(6,2.02f,0),new Vector3(.5f,.45f,7.4f),blue,.18f);
        Box("FrontRail",table,new Vector3(0,2.02f,-3.62f),new Vector3(12.25f,.45f,.52f),blue,.18f);
        Box("RearRail",table,new Vector3(0,2.02f,3.62f),new Vector3(12.25f,.45f,.52f),blue,.18f);
        for(int side=-1;side<=1;side+=2) for(int end=-1;end<=1;end+=2)
        {
            Box("RoundedCorner",table,new Vector3(side*5.95f,1.40f,end*3.54f),new Vector3(.97f,1.70f,.97f),blue,.29f);
            Disc("BirchLeg",table,new Vector3(side*5.25f,.20f,end*3.35f),.48f,1.5f,wood,.07f);
            Disc("BlueFoot",table,new Vector3(side*5.25f,-.55f,end*3.35f),.51f,.18f,blue,.05f);
            var bolt=Disc("MintCornerPlug",table,new Vector3(side*5.95f,1.3f,end*4.052f),.23f,.10f,mint,.04f);
            bolt.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        Box("CentreInlay",table,new Vector3(0,1.981f,0),new Vector3(.055f,.012f,6.55f),coral,.004f);
        Ring("CentreCircle",table,new Vector3(0,1.988f,.95f),.91f,.055f,coral,.008f);
        for(int side=-1;side<=1;side+=2)
            Ring("SideInlay",table,new Vector3(side*5.74f,1.989f,.1f),.70f,.045f,coral,.008f);
        for(int i=0;i<3;i++)
            Box("SpeakerRecess",table,new Vector3(-4.25f,1.4f-i*.18f,-3.912f),new Vector3(1.15f,.065f,.018f),ink,.03f);
        Text("SMALL",table,new Vector3(4.28f,1.51f,-3.918f),.20f,cream);
        Text("PUCKS",table,new Vector3(4.28f,1.26f,-3.918f),.20f,cream);
        Text("BIG FUN",table,new Vector3(4.28f,1.01f,-3.918f),.20f,cream);
        Box("ClockBirchFrame",table,new Vector3(0,1.26f,-3.975f),new Vector3(3.9f,.88f,.25f),wood,.16f);
        Box("ClockDarkFace",table,new Vector3(0,1.26f,-4.115f),new Vector3(3.4f,.55f,.035f),black,.09f);
        for(int side=-1;side<=1;side+=2)
        {
            var bolt=Disc("ClockFramePlug",table,new Vector3(side*1.78f,1.26f,-4.14f),.105f,.08f,mint,.025f);
            bolt.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        Text("AUTO START",table,new Vector3(-.57f,1.26f,-4.14f),.19f,glow);
        controller.clockSegments=new GameObject[14];
        for(int digit=0;digit<2;digit++)
        {
            float x=.85f+digit*.4f;
            Vector2[] pos={new Vector2(0,.185f),new Vector2(.12f,.09f),new Vector2(.12f,-.09f),new Vector2(0,-.185f),new Vector2(-.12f,-.09f),new Vector2(-.12f,.09f),Vector2.zero};
            for(int s=0;s<7;s++)
            {
                bool horizontal=s==0||s==3||s==6;
                controller.clockSegments[digit*7+s]=Box("Clock_"+digit+"_Segment_"+s,table,
                    new Vector3(x+pos[s].x,1.26f+pos[s].y,-4.148f),
                    horizontal?new Vector3(.20f,.034f,.025f):new Vector3(.034f,.14f,.025f),glow,.015f);
            }
        }
        SavePart(table,"ToyTableAndClock");
    }

    static void BuildLogo(Transform root)
    {
        var logo=Group("RaisedBucaSign",root);
        Box("BirchSignPlinth",logo,new Vector3(0,2.53f,2.92f),new Vector3(9.35f,.76f,.64f),wood,.16f);
        Text("PUCKS - SKILLS - GOOD TIMES",logo,new Vector3(0,2.49f,2.575f),.245f,ink);
        string word="BUCA"; Material[] colors={blue,coral,yellow,mint};
        float[] xs={-3.26f,-1.09f,1.09f,3.26f};
        for(int i=0;i<4;i++)
        {
            var letter=Group("SolidLetter_"+word[i],logo,new Vector3(xs[i],4.03f,2.53f));
            letter.localRotation=Quaternion.Euler(0,0,i==0?-3:i==3?3:0);
            float weight=i==0?.067f:i==3?.060f:.10f;
            var face=Text(word[i].ToString(),letter,Vector3.zero,1,colors[i],false,.28f,.035f,weight);
            Bounds bounds=face.GetComponent<MeshFilter>().sharedMesh.bounds;
            face.transform.localScale=new Vector3(2.08f/bounds.size.x,2.20f/bounds.size.y,1.75f);
            face.transform.localPosition=-Vector3.Scale(new Vector3(bounds.center.x,bounds.center.y,0),face.transform.localScale);
            var back=Text(word[i].ToString(),letter,new Vector3(0,0,.38f),1,wood,false,.20f,.04f,weight);
            back.transform.localScale=new Vector3(face.transform.localScale.x*1.065f,face.transform.localScale.y*1.065f,1.5f);
            back.transform.localPosition=new Vector3(0,0,.38f)-Vector3.Scale(new Vector3(bounds.center.x,bounds.center.y,0),back.transform.localScale);
        }
        for(int s=-1;s<=1;s+=2)
        {
            var plug=Disc("SignPlug",logo,new Vector3(s*4.26f,2.5f,2.54f),.17f,.10f,mint,.03f);
            plug.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        SavePart(logo,"RaisedBucaSign");
    }

    static ToyBoxMenuController.PuckButton Button(Transform parent,string label,Vector3 position,float radius,Material color,float letterHeight,bool save=true)
    {
        var root=Group(label+"_Button",parent,position);
        Disc("CobaltSocket",root,Vector3.zero,radius+.16f,.19f,blue,.055f);
        Disc("BirchCollar",root,new Vector3(0,.13f,0),radius+.075f,.14f,wood,.03f);
        float puckHeight=radius>.7f?.38f:.28f;
        var cap=Group("MovingCap",root,new Vector3(0,.30f+(puckHeight-.28f)*.5f,0));
        GameObject face=Disc("PaintedPuck",cap,Vector3.zero,radius,puckHeight,color,.085f);
        var hit=face.AddComponent<BoxCollider>(); hit.size=new Vector3(radius*1.85f,puckHeight+.04f,radius*1.85f);
        // A rounded light tube under the cap and a raised ivory marker make
        // selection physical while keeping the label unobscured.
        var ring=Group("SelectedMintRing",cap).gameObject;
        MeshObject("SolidLightTube",ring.transform,new Vector3(0,-puckHeight*.5f+.035f,0),
            ToyBoxGeometry.Torus(radius+.065f,.05f),glow);
        float markerSize=radius>.7f?.21f:.11f;
        var marker=Box("SelectionDiamond",ring.transform,new Vector3(0,puckHeight*.5f+.015f,-radius-.035f),
            new Vector3(markerSize,.045f,markerSize),cream,.018f);
        marker.transform.localRotation=Quaternion.Euler(0,45,0);
        // Extrusion points down into the puck, so raise its front face by the
        // full physical thickness to expose the letter sides and their shadows.
        if (!string.IsNullOrEmpty(label)) Text(label,cap,new Vector3(0,puckHeight*.5f+.01f+letterHeight*.22f,0),letterHeight,cream,true,.22f,.024f,.04f);
        ring.SetActive(label=="PLAY");
        if(save) SavePart(root,"Button_"+label+"_"+(prefabSerial++));
        return new ToyBoxMenuController.PuckButton { cap=cap,hit=hit,face=face.GetComponent<MeshRenderer>(),selectedRing=ring,restPosition=cap.localPosition };
    }

    static void BuildMenuControls(Transform root,ToyBoxMenuController controller)
    {
        var home=Group("HomeControls",root); controller.homeRoot=home.gameObject;
        controller.homeButtons=new[] {
            Button(home,"PLAY",new Vector3(-3.2f,2.06f,-.65f),.96f,coral,.32f),
            Button(home,"LEVELS",new Vector3(0,2.06f,-.65f),.96f,yellow,.25f),
            Button(home,"SKINS",new Vector3(3.2f,2.06f,-.65f),.96f,mint,.27f) };
        SavePart(home,"HomeControls");
        BuildSkinsTray(root, controller);

        BuildChapterMap(root, controller);

        var resume=Group("ReturningPlayerTray",root); controller.resumeRoot=resume.gameObject;
        BuildResumeExplanation(resume);

        controller.resumeButtons=new[] {
            Button(resume,"LEVEL 1",new Vector3(-3.5f,2.06f,-.85f),1.17f,yellow,.27f),
            Button(resume,"CONTINUE",new Vector3(0,2.06f,-.85f),1.17f,coral,.245f),
            Button(resume,"BACK",new Vector3(3.5f,2.06f,-.85f),1.17f,mint,.35f) };
        controller.resumeButtons[0].selectedRing.SetActive(true);
        SavePart(resume,"ReturningPlayerTray"); resume.gameObject.SetActive(false);
    }

    static void BuildRoom(Transform root)
    {
        var room=Group("ToyRoomScenery",root,new Vector3(0,-.5f,0));
        BuildBrightPlayroom(room);
        SavePart(room,"ToyRoomScenery");
    }

    static void BuildCameraAndLights(Transform root,ToyBoxMenuController controller)
    {
        var rig=Group("CameraAndLighting",root);
        var camera=Group("ToyBoxCamera",rig,new Vector3(1.05f,14.0f,-19.7f)).gameObject;
        camera.tag="MainCamera";
        camera.transform.LookAt(new Vector3(0,2.15f,.2f));
        controller.menuCamera=camera.AddComponent<Camera>();
        controller.menuCamera.fieldOfView=34; controller.menuCamera.nearClipPlane=.1f; controller.menuCamera.farClipPlane=100;
        controller.menuCamera.clearFlags=CameraClearFlags.SolidColor;
        controller.menuCamera.backgroundColor=new Color(.79f,.72f,.60f);
        camera.AddComponent<AudioListener>();
        var data=camera.AddComponent<UniversalAdditionalCameraData>();
        data.SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());
        data.renderPostProcessing=true; data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var lightGo=Group("WarmKey",rig).gameObject;
        lightGo.transform.rotation=Quaternion.Euler(52,-38,0);
        var light=lightGo.AddComponent<Light>(); light.type=LightType.Directional;
        light.color=new Color(1,.94f,.84f); light.intensity=.95f;
        light.shadows=LightShadows.Soft; light.shadowBias=.012f; light.shadowNormalBias=.04f;
        light.shadowStrength=.78f;
        var fillGo=Group("CoolFill",rig).gameObject;
        fillGo.transform.rotation=Quaternion.Euler(33,140,0);
        var fill=fillGo.AddComponent<Light>(); fill.type=LightType.Directional;
        fill.color=new Color(.89f,.92f,1); fill.intensity=.28f; fill.shadows=LightShadows.None;
        var probe=Group("BakedStudioReflection",rig,new Vector3(0,3,0)).gameObject.AddComponent<ReflectionProbe>();
        probe.mode=ReflectionProbeMode.Custom; probe.customBakedTexture=ToyBoxSurfaceBaker.StudioReflection();
        probe.size=new Vector3(50,30,50); probe.blendDistance=2; probe.intensity=.7f; probe.boxProjection=false;
        var profile=new VolumeProfile { name="ToyBoxGrade" };
        var bloom=profile.Add<Bloom>(true); bloom.intensity.Override(.10f); bloom.threshold.Override(1.2f);
        var grading=profile.Add<ColorAdjustments>(true); grading.saturation.Override(-5); grading.contrast.Override(7);
        grading.postExposure.Override(-.10f);
        var tone=profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
        var focus=profile.Add<DepthOfField>(true); focus.mode.Override(DepthOfFieldMode.Gaussian);
        focus.gaussianStart.Override(29); focus.gaussianEnd.Override(42); focus.gaussianMaxRadius.Override(.65f);
        focus.highQualitySampling.Override(true);
        string path=Root+"/Materials/ToyBoxGrade.asset";
        var existing=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(existing==null)
        {
            AssetDatabase.CreateAsset(profile,path);
            foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
        }
        else
        {
            foreach(var component in existing.components) UnityEngine.Object.DestroyImmediate(component,true);
            existing.components.Clear();
            foreach(var component in profile.components)
            {
                AssetDatabase.AddObjectToAsset(component,existing);
                existing.components.Add(component);
            }
            profile.components.Clear();
            UnityEngine.Object.DestroyImmediate(profile); profile=existing;
            EditorUtility.SetDirty(profile);
        }
        var volume=Group("ToyBoxColorGrade",rig).gameObject.AddComponent<Volume>();
        volume.isGlobal=true; volume.priority=50; volume.sharedProfile=profile;
        SavePart(rig,"CameraAndLighting");
    }

    static void ConfigureEnvironment()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.51f,.50f,.48f);
        RenderSettings.ambientEquatorColor=new Color(.43f,.39f,.33f);
        RenderSettings.ambientGroundColor=new Color(.27f,.23f,.18f);
        RenderSettings.ambientIntensity=1; RenderSettings.fog=false; RenderSettings.skybox=null;
    }

    public static void Validate(GameObject root)
    {
        var c=root.GetComponent<ToyBoxMenuController>();
        if(c==null||c.menuCamera==null||c.homeButtons.Length!=3||c.skinButtons.Length!=5||c.equippedMarkers.Length!=5||c.skinsRoot==null||c.levelButtons.Length!=30||c.resumeButtons.Length!=3||c.clockSegments.Length!=14)
            throw new InvalidOperationException("Toy Box prefab wiring is incomplete.");
        if(root.GetComponentsInChildren<Canvas>(true).Length!=0 || root.GetComponentsInChildren<TMPro.TMP_Text>(true).Length!=0)
            throw new InvalidOperationException("The Toy Box prefab must contain only solid mesh lettering, not Canvas or TMP UI.");
        foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))
            if(mesh.sharedMesh==null||!AssetDatabase.Contains(mesh.sharedMesh))
                throw new InvalidOperationException("A mesh was not saved: "+mesh.name);
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            if(renderer.sharedMaterial==null||!AssetDatabase.Contains(renderer.sharedMaterial))
                throw new InvalidOperationException("A material was not saved: "+renderer.name);
        foreach(var buttons in new[]{c.homeButtons,c.levelButtons,c.resumeButtons,c.skinButtons,new[]{c.backButton,c.skinsBackButton}})
            foreach(var b in buttons)
                if(b.cap==null||b.hit==null||b.face==null||b.selectedRing==null)
                    throw new InvalidOperationException("A button is missing an assigned reference: cap="+b.cap+" hit="+b.hit+" face="+b.face+" ring="+b.selectedRing);
    }
}
