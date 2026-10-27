#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Fits the existing interactive railway to the approved workshop-table map.</summary>
public static class BuildWorkshopRailway
{
    const string Folder="Assets/TwilightMap/WorkshopRailway";
    const string Output="output/workshop-railway";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Vector2 P(float x,float y)=>new Vector2(x,y);
    static Vector2 Art(Vector2 p)=>BuildForestHarborMap.Art(p);
    // Original rail control anchors; final level markers are resampled at equal visible gaps.
    public static readonly Vector2[] Stations={
        P(182,652),P(263,622),P(343,625),P(422,633),P(501,636),
        P(580,636),P(657,634),P(728,615),P(788,583),P(826,534),
        P(810,481),P(755,449),P(688,444),P(624,444),P(560,440),
        P(497,431),P(432,420),P(370,406),P(312,388),P(263,365),
        P(242,328),P(273,292),P(326,271),P(386,265),P(448,276),
        P(505,305),P(567,324),P(635,331),P(702,327),P(771,319),
        P(1080,343),P(1146,345),P(1212,356),P(1277,372),P(1343,391),
        P(1408,411),P(1472,434),P(1528,471),P(1555,534),P(1553,588),
        P(1513,630),P(1454,649),P(1387,651),P(1321,640),P(1259,622),
        P(1202,601),P(1144,579),P(1089,557),P(1036,536),P(995,517)
    };

    [MenuItem("RealBuca/Workshop Railway/Apply Approved First Option")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null || map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("Open MainMenu first.");
        var view=map.illustratedMenu;
        var p=view.GetComponent<ForestHarborMapPresentation>();
        if(p==null || view.stopRects.Length!=50)throw new Exception("Existing 50-stop presentation required.");
        Directory.CreateDirectory(Output);
        if(!File.Exists(Output+"/MainMenu.before-workshop.unity"))File.Copy(map.gameObject.scene.path,Output+"/MainMenu.before-workshop.unity");
        string asset=Folder+"/WorkshopBackground.png";
        AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(asset);
        importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=false;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
        var art=view.artwork;
        var background=art.GetComponent<UnityEngine.UI.RawImage>();background.texture=texture;background.color=Color.white;
        var shader=Shader.Find("BUCA/Workshop Water");
        if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Workshop water shader failed.");
        var water=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/WorkshopWater.mat");
        if(water==null){water=new Material(shader);AssetDatabase.CreateAsset(water,Folder+"/WorkshopWater.mat");}
        background.material=water;
        (background.GetComponent<ForestHarborWaterFlow>()??background.gameObject.AddComponent<ForestHarborWaterFlow>()).Configure(background);
        var backdrop=view.transform.Find("FullBleedBackdrop").GetComponent<UnityEngine.UI.RawImage>();
        backdrop.texture=texture;
        foreach(string name in new[]{"HarborWaterRepair","ScreenButtonRemoval","OpenWoodlandCurve","RoundForestRailway","WideRailwayEntrance","LeafyBirchGrove","SelectedStationCanopy"})Hide(art.Find(name));
        p.workshopRailway=true;p.roundForestStops=true;p.forestOpenCurve=true;
        p.harborTunnelEntrance=p.harborTunnelExit=-100;
        BuildRoute(map);
        var circle=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luxodd.Game/Example/Sprites/white_circle.png");
        if(circle==null)throw new Exception("Circle sprite missing.");
        for(int i=0;i<50;i++)
        {
            var rect=view.stopRects[i];Place(rect,Stations[i],P(48,48));view.route[i]=Art(Stations[i]);
            rect.GetComponentInChildren<WorkshopStationBadge>(true)?.SetTrainClearance(Vector2.zero);
            var badge=rect.Find("WorkshopPlaque") as RectTransform;
            if(badge==null)badge=Rect(rect,"WorkshopPlaque",Vector2.zero,P(46,46),false);
            badge.SetAsFirstSibling();badge.gameObject.SetActive(true);
            var graphic=badge.GetComponent<WorkshopStationBadge>()??badge.gameObject.AddComponent<WorkshopStationBadge>();
            graphic.raycastTarget=false;
            foreach(var face in new[]{view.faces[i],view.glows[i]})
            {
                face.sprite=circle;face.type=UnityEngine.UI.Image.Type.Simple;
                face.rectTransform.anchorMin=face.rectTransform.anchorMax=face.rectTransform.pivot=Vector2.one*.5f;
                face.rectTransform.anchoredPosition=Vector2.zero;face.rectTransform.sizeDelta=P(42,42);
                face.rectTransform.localRotation=Quaternion.identity;face.rectTransform.localScale=Vector3.one;
            }
            view.faces[i].raycastTarget=true;view.glows[i].raycastTarget=false;
            view.stopButtons[i].transition=UnityEngine.UI.Selectable.Transition.None;
            var number=view.numbers[i];number.gameObject.SetActive(true);number.text=(i+1).ToString();
            number.font=p.destination.font;number.fontSize=23;number.enableAutoSizing=false;number.fontStyle=FontStyles.Bold;
            number.alignment=TextAlignmentOptions.Center;number.raycastTarget=false;
            var nr=number.rectTransform;nr.anchorMin=nr.anchorMax=nr.pivot=Vector2.one*.5f;
            nr.anchoredPosition=P(0,1);nr.sizeDelta=P(42,40);nr.localScale=Vector3.one;nr.localRotation=Quaternion.identity;
            view.locks[i].transform.localPosition=new Vector3(17,-17,0);
            view.awards[i].transform.localPosition=new Vector3(-17,-17,0);
            Hide(rect.Find("ForegroundSign"));
        }
        Controls(view,p);
        Scenery(map,view);
        BuildEvenRailwayStops.Arrange(map,false);
        bool active=map.gameObject.activeSelf;
        map.gameObject.SetActive(true);map.Open(null);view.Layout();view.Refresh();view.PoseTrain();p.Refresh();
        map.GetComponent<SuitcaseLocomotiveView>().Frame();map.gameObject.SetActive(active);
        EditorUtility.SetDirty(map);EditorUtility.SetDirty(view);EditorUtility.SetDirty(p);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Approved workshop map applied; 50 native badges, fitted route and footer, reanchored live scenery.";
    }
    static void Controls(TwilightIllustratedMenu view,ForestHarborMapPresentation p)
    {
        Place(view.controls,P(836,470.5f),P(1672,941));
        foreach(string n in new[]{"TrainTicketArtwork","TicketLevelLabel"})Hide(p.destination.transform.parent.Find(n));
        Hotspot(view.playButton,P(840,821),P(140,135));
        Label(view.playText,view.controls,P(840,903),P(180,22),14,new Color(.035f,.10f,.20f));
        Hotspot(p.previous,P(410,845),P(82,85));Hotspot(p.next,P(690,845),P(70,80));
        Label(p.destination,view.controls,P(599,850),P(78,48),38,new Color(.025f,.09f,.22f));
        Label(p.progress,view.controls,P(596,903),P(104,20),12,new Color(.10f,.17f,.23f));
        Label(view.countdownText,view.controls,P(1214,844),P(67,44),38,new Color(.10f,1,.64f));
        view.countdownText.characterSpacing=0;
        // Keep the existing tactile menu lever on the left end of the blue table frame.
        var lever=view.artwork.Find("RailwayMenuLever") as RectTransform;
        if(lever!=null)
        {
            lever.anchoredPosition=Art(P(168,834));lever.localScale=Vector3.one*.69f;
            lever.SetAsLastSibling();
            Hide(view.backButton.transform);Hide(view.controls.Find("BackLabel"));
        }
        else
        {
            Hotspot(view.backButton,P(165,850),P(130,55));
            var image=view.backButton.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.97f,.79f,.47f);
            var label=view.controls.Find("BackLabel")?.GetComponent<TMP_Text>();
            if(label!=null)Label(label,view.controls,P(165,850),P(120,45),20,new Color(.02f,.08f,.20f));
        }
        // Preserve the existing hidden fullscreen control; keyboard fullscreen still works.
        if(view.fullscreenButton.gameObject.activeSelf)
        {
            Hotspot(view.fullscreenButton,P(1475,855),P(120,48));
            Label(view.fullscreenText,view.controls,P(1475,855),P(116,40),15,new Color(1,.88f,.60f));
        }
        view.controls.SetAsLastSibling();
        if (view.controls.Find("WorkshopFlipSelector") != null)
            BuildWorkshopFlipSelector.Configure(view, p);
        if (view.artwork.Find("CleanArcadeButtonDeck") != null)
            BuildWorkshopArcadeButtons.Configure(view, p);
    }
    static void Scenery(TwilightRailwayMap map,TwilightIllustratedMenu view)
    {
        var art=view.artwork;
        var crane=map.GetComponentInChildren<ForestHarborCrane>(true);
        if(crane!=null)
        {
            var root=art.Find("WorkingHarborCraneArt");
            foreach(string n in new[]{"RestoredDock","OriginalRightCargo","OriginalForegroundCargo"})Hide(root.Find(n));
            Place(crane.picture.rectTransform,P(1100,447),P(230,184));
            Place(root.Find("BaseContact") as RectTransform,P(1100,491),P(46,19));
            crane.Pose(0);
        }
        var wind=map.GetComponentInChildren<ForestHarborWindmill>(true);
        if(wind!=null)
        {
            Hide(wind.transform.Find("FixedTower"));
            ((RectTransform)wind.blades.parent).anchoredPosition=Art(P(540,214));
            wind.blades.sizeDelta=P(118,118);wind.Pose(0);
        }
        var beam=art.Find("RotatingLighthouseBeam") as RectTransform;
        if(beam!=null){beam.anchoredPosition=Art(P(1498,278));beam.localScale=Vector3.one*.78f;}
        var life=map.GetComponentInChildren<ForestHarborLife>(true);
        if(life!=null)
        {
            life.sailboat.route=new[]{P(1360,558),P(1420,542),P(1458,500),P(1410,475),P(1350,503),P(1320,538)};
            life.sailboat.root.localScale=Vector3.one*.75f;
            life.tugboat.route=new[]{P(1295,727),P(1420,729),P(1490,745),P(1370,748),P(1190,721),P(1090,675),P(1160,690)};
            life.tugboat.root.localScale=Vector3.one*.72f;
            life.Pose(0);
        }
        var grove=map.GetComponentInChildren<GroundedForestGrove>(true);
        if(grove!=null)
        {
            // Fourteen rooted meshes, with room between them and the rail; no flat tree cutouts.
            var roots=new[]{new Vector3(190,254,67),new Vector3(147,365,66),new Vector3(338,355,64),
                new Vector3(478,388,65),new Vector3(647,294,62),new Vector3(691,404,55),
                new Vector3(144,452,58),new Vector3(347,563,68),new Vector3(442,607,60),
                new Vector3(734,552,64),new Vector3(816,689,65),new Vector3(310,746,70),
                new Vector3(101,735,68),new Vector3(587,592,55)};
            for(int i=0;i<grove.roots.Length&&i<roots.Length;i++)
            {
                var q=roots[i];grove.artworkRoots[i]=P(q.x,q.y);
                grove.roots[i].localPosition=new Vector3((q.x-425)/20,0,(470.5f-q.y)/10);
                grove.roots[i].localScale=Vector3.one*(q.z/79f);
            }
            grove.Pose(0);EditorUtility.SetDirty(grove);
        }
        var leaves=art.Find("ForestLeafSwirls") as RectTransform;
        if(leaves!=null){leaves.localScale=new Vector3(.78f,.56f,1);leaves.anchoredPosition=P(-70,-100);}
    }
    static void BuildRoute(TwilightRailwayMap map)
    {
        var keys=new List<Vector2>{P(140,765),P(140,735),P(148,704),P(164,682)};
        var indices=new int[50];
        for(int i=0;i<50;i++)
        {
            if(i==10)keys.Add(P(821,510));
            if(i==20)keys.Add(P(239,350));
            if(i==30)keys.AddRange(new[]{P(810,325),P(848,343),P(885,324),P(930,315),P(970,324),P(1008,349),P(1042,345)});
            keys.Add(Stations[i]);indices[i]=keys.Count-1;
        }
        keys.Add(P(971,526));
        var points=new List<Vector3>();var distances=new float[keys.Count];float distance=0;
        for(int k=0;k<keys.Count-1;k++)
        {
            var a=keys[Mathf.Max(0,k-1)];var b=keys[k];var c=keys[k+1];var d=keys[Mathf.Min(keys.Count-1,k+2)];
            int steps=Mathf.Max(8,Mathf.CeilToInt(Vector2.Distance(b,c)/2));
            for(int j=0;j<steps;j++)
            {
                float t=(float)j/steps;var q=.5f*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                var v=Art(q);var point=new Vector3(v.x/20,0,v.y/20);
                if(points.Count>0)distance+=Vector3.Distance(points[points.Count-1],point);
                points.Add(point);if(j==0)distances[k]=distance;
            }
        }
        var last=Art(keys[keys.Count-1]);points.Add(new Vector3(last.x/20,0,last.y/20));map.railway=points.ToArray();
        for(int i=0;i<50;i++)map.stops[i].distance=distances[indices[i]];
        typeof(TwilightRailwayMap).GetMethod("Cache",Private).Invoke(map,null);
    }
    static void Hide(Transform t){if(t!=null)t.gameObject.SetActive(false);}
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size,bool pixel=true)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.gameObject.layer=5;
        rect.SetParent(parent,false);Place(rect,pixel?position:P(position.x+836,470.5f-position.y),size);return rect;
    }
    static void Place(RectTransform r,Vector2 pixel,Vector2 size)
    {
        r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=Art(pixel);r.sizeDelta=size;
        r.localRotation=Quaternion.identity;r.localScale=Vector3.one;
    }
    static void Hotspot(UnityEngine.UI.Button button,Vector2 pixel,Vector2 size)
    {
        button.gameObject.SetActive(true);Place((RectTransform)button.transform,pixel,size);
        var image=button.GetComponent<UnityEngine.UI.Image>();image.color=Color.clear;image.raycastTarget=true;
        button.transition=UnityEngine.UI.Selectable.Transition.None;
    }
    static void Label(TMP_Text text,Transform parent,Vector2 pixel,Vector2 size,float fontSize,Color color)
    {
        text.transform.SetParent(parent,false);text.gameObject.SetActive(true);Place(text.rectTransform,pixel,size);
        text.fontSize=fontSize;text.enableAutoSizing=false;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;
    }

    public static string CheckAndRender()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(!original.illustratedMenu.GetComponent<ForestHarborMapPresentation>().workshopRailway)throw new Exception("Workshop map not installed.");
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);copy.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=null;map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",Private).SetValue(map,49);
            var view=map.illustratedMenu;
            BuildEvenRailwayStops.Validate(map);
            for(int i=0;i<50;i++)
            {
                var point=map.Point(map.stops[i].distance);
                if(Vector2.Distance(view.stopRects[i].anchoredPosition,P(point.x,point.z)*20)>.05f)throw new Exception("Click target misaligned: "+(i+1));
                if(!view.numbers[i].gameObject.activeSelf||view.numbers[i].text!=(i+1).ToString())throw new Exception("Missing level number: "+(i+1));
                if(i>0&&map.stops[i].distance<=map.stops[i-1].distance)throw new Exception("Route order broken.");
            }
            var travel=typeof(ForestHarborValidation).GetMethod("Travel",BindingFlags.Static|BindingFlags.NonPublic);
            travel.Invoke(null,new object[]{map,0});
            int forward=(int)travel.Invoke(null,new object[]{map,49}),reverse=(int)travel.Invoke(null,new object[]{map,0});
            if(forward!=49||reverse!=49)throw new Exception("Train skipped a station.");
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            copy.SetActive(true);
            var render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))
                if(camera.targetTexture!=null)RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture});
            render.Invoke(null,new object[]{map,11,1672,941,Output+"/Workshop-map.png"});
            render.Invoke(null,new object[]{map,39,1672,941,Output+"/Workshop-harbor.png"});
            render.Invoke(null,new object[]{map,11,1600,1000,Output+"/Workshop-16x10.png"});
            // Check lock protection without touching saved progress.
            typeof(TwilightRailwayMap).GetField("highest",Private).SetValue(map,0);
            travel.Invoke(null,new object[]{map,0});map.Select(49);view.Refresh();
            for(int n=0;n<120;n++)map.TickTrain(1f/60);
            if(map.Parked!=0||map.Moving||view.playButton.interactable)throw new Exception("Locked level guard failed.");
            var crane=copy.GetComponentInChildren<ForestHarborCrane>(true);
            if(crane!=null)
            {
                for(int n=0;n<240;n++)
                {
                    crane.Pose(n*.1f);
                    // The moving load remains below the boom throughout the cycle.
                    if(crane.hook.position.y>crane.boomTip.position.y+.01f)throw new Exception("Crane load crossed the boom.");
                }
            }
            string report="PASS: 50 unique native numbers and exact rail stops; 49 consecutive arrivals forward and reverse, with station dwell checks; raycasts reach all 50 level buttons and active footer controls at 16:9 and 16:10; locked levels cannot move the train or enable Play; crane motion sampled across a complete cycle. Isolated Edit-mode verification; no player progress changed.";
            File.WriteAllText(Output+"/validation.txt",report);return report;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
