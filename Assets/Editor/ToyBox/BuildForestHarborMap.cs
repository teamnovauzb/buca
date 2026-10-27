#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Installs the approved wooden forest / moonlit harbor landscape in Edit mode.</summary>
public static class BuildForestHarborMap
{
    const string Folder = "Assets/TwilightMap/ForestHarbor";
    const int TrainLayer = 28;
    static Vector2 P(float x, float y) => new Vector2(x, y);
    public static Vector2 Art(Vector2 p) => new Vector2(p.x - 836, 470.5f - p.y);
    public static readonly Vector2[] Stations = {
        P(174,704),P(241,689),P(309,689),P(377,699),P(445,702),P(508,699),P(572,694),P(633,683),P(690,671),P(750,640),
        P(744,495),P(670,496),P(598,514),P(535,525),P(474,522),P(407,511),P(344,495),P(277,477),P(212,456),P(152,438),
        P(154,188),P(225,188),P(280,209),P(334,229),P(395,249),P(453,265),P(517,278),P(582,283),P(650,286),P(714,285),
        P(1047,253),P(1099,264),P(1153,279),P(1206,297),P(1256,317),P(1313,335),P(1375,351),P(1435,365),P(1500,377),P(1570,384),
        P(1011,513),P(1064,538),P(1120,561),P(1176,583),P(1237,603),P(1298,622),P(1360,639),P(1436,649),P(1498,650),P(1553,641)
    };
    static TMP_FontAsset font;

    [MenuItem("RealBuca/Forest Harbor/Apply Selected Map")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var view = map != null ? map.illustratedMenu : null;
        if (view == null || view.stopRects.Length != 50) throw new Exception("Existing 50-stop map required.");
        var texturePath = Folder + "/ForestHarborBackground.png";
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None; importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        view.artwork.GetComponent<RawImage>().texture = background;
        var backdrop = view.transform.Find("FullBleedBackdrop").GetComponent<RawImage>();
        backdrop.texture = background; backdrop.color = Color.white;
        const string backdropPath = Folder + "/LandscapeMargins.mat";
        var margins = AssetDatabase.LoadAssetAtPath<Material>(backdropPath);
        if (margins == null) { margins = new Material(Shader.Find("BUCA/Landscape Margins")); AssetDatabase.CreateAsset(margins,backdropPath); }
        backdrop.material = margins;
        view.suitcasePresentation = false; view.forestHarborPresentation = true;
        foreach (var board in map.GetComponentsInChildren<SuitcaseDepartureBoard>(true))
        {
            board.enabled = false;
            if (board.marker != null) board.marker.gameObject.SetActive(false);
            if (board.tens != null) board.tens.transform.parent.gameObject.SetActive(false);
        }
        foreach (var watch in map.GetComponentsInChildren<SuitcasePocketWatch>(true)) watch.enabled = false;
        foreach (string name in new[] { "DepartureBoard", "PocketWatch", "KeyboardHint" })
        { var old = view.controls.Find(name); if (old != null) old.gameObject.SetActive(false); }
        var previousPresentation = view.GetComponent<ForestHarborMapPresentation>();
        if (previousPresentation != null) Object.DestroyImmediate(previousPresentation);
        var priorControls = view.controls.Find("ForestHarborControls");
        if (priorControls != null) Object.DestroyImmediate(priorControls.gameObject);
        var presentation = view.gameObject.AddComponent<ForestHarborMapPresentation>(); presentation.view = view;
        presentation.forestOpenCurve = view.artwork.Find("OpenWoodlandCurve") != null;
        BuildRoute(map, presentation);
        view.route = new Vector2[50];
        for (int i = 0; i < 50; i++)
        {
            view.route[i] = Art(Stations[i]);
            var rect = view.stopRects[i]; rect.anchoredPosition = view.route[i]; rect.sizeDelta = P(61,42);
            rect.localRotation = Quaternion.identity;
            var delta = map.Point(map.stops[i].distance + .08f) - map.Point(map.stops[i].distance - .08f);
            // Rotate the flat timber highlight with the local rail tangent, without turning labels upside down.
            float angle = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
            if (angle > 90) angle -= 180; if (angle < -90) angle += 180;
            view.faces[i].rectTransform.sizeDelta = P(53,27);
            view.faces[i].rectTransform.localRotation = Quaternion.Euler(0,0,angle);
            view.faces[i].sprite = null; view.faces[i].color = Color.clear;
            view.stopButtons[i].transition = Selectable.Transition.None;
            view.stopButtons[i].navigation = new Navigation { mode = Navigation.Mode.None };
            view.numbers[i].gameObject.SetActive(false);
            var foreground = rect.Find("ForegroundSign"); if (foreground != null) foreground.gameObject.SetActive(false);
            var glow = view.glows[i]; glow.sprite = null; glow.raycastTarget = false;
            glow.rectTransform.anchoredPosition = Vector2.zero; glow.rectTransform.sizeDelta = P(55,28);
            glow.rectTransform.localRotation = Quaternion.Euler(0,0,angle);
            glow.color = new Color(.1f,1,.37f,.4f);
            view.locks[i].transform.localPosition = new Vector3(24,-15,0);
            view.locks[i].transform.localScale = Vector3.one * .7f;
            view.awards[i].transform.localPosition = new Vector3(-22,-15,0);
            view.awards[i].transform.localScale = Vector3.one * .7f;
        }
        map.stationPause = .6f; map.trainProjectionDepth = 2; map.trainNoseOffset = 2.4f;
        font = BakeFont("BucaRoundedMenu", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 /-");
        if (font == null) throw new Exception("Menu font missing.");
        view.controls.SetParent(view.artwork, false);
        Place(view.controls, P(836,470.5f), P(1672,941));
        view.controls.localScale = Vector3.one;
        ConfigureButton(view.playButton, P(825,839), P(190,144));
        Label(view.playText, P(825,882), P(181,32), 17, view.controls);
        view.playText.gameObject.SetActive(false);
        ConfigureButton(view.backButton, P(289,864), P(96,118));
        var menuLabel = view.controls.Find("BackLabel");
        Label(menuLabel != null ? menuLabel.GetComponent<TMP_Text>() : view.backButton.GetComponentInChildren<TMP_Text>(true), P(288,902), P(88,26), 14, view.controls);
        var backLabel = view.controls.Find("BackLabel").GetComponent<TMP_Text>(); backLabel.text = "MENU";
        ConfigureButton(view.fullscreenButton, P(1383,865), P(108,120));
        Label(view.fullscreenText, P(1382,903), P(107,26), 14, view.controls);
        view.statusText.gameObject.SetActive(false);
        Label(view.countdownText, P(1168,849), P(66,42), 38, view.controls);
        var digital = BakeFont("BucaDigitalTimer", "0123456789");
        if (digital != null) view.countdownText.font = digital;
        view.countdownText.color = new Color(.28f,1,.67f); view.countdownText.text = "30";
        var root = Rect("ForestHarborControls", view.controls, P(836,470.5f), P(1672,941));
        presentation.previous = Hotspot("Previous", root, P(480,848), P(67,67));
        presentation.next = Hotspot("Next", root, P(656,848), P(67,67));
        presentation.destination = Text("Destination", root, P(568,850), P(94,47), 30);
        presentation.progress = Text("Progress", root, P(568,896), P(116,27), 14);
        presentation.progress.gameObject.SetActive(false);
        ConfigureTrain(map, view);
        var oldShadow = view.artwork.Find("TrainContactShadow"); if(oldShadow != null) Object.DestroyImmediate(oldShadow.gameObject);
        presentation.contactShadow = Rect("TrainContactShadow",view.artwork,P(836,470.5f),P(86,22));
        presentation.contactShadow.gameObject.AddComponent<ForestHarborTrackShadow>().raycastTarget=false;
        presentation.trainVisibility = view.locomotive.GetComponent<CanvasGroup>();
        if (presentation.trainVisibility == null) presentation.trainVisibility = view.locomotive.gameObject.AddComponent<CanvasGroup>();
        presentation.trainVisibility.blocksRaycasts = false; presentation.trainVisibility.interactable = false;
        view.locomotive.SetAsLastSibling(); view.controls.SetAsLastSibling();
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        bool active = map.gameObject.activeSelf; map.gameObject.SetActive(true);
        map.Open(null); Canvas.ForceUpdateCanvases(); view.Layout(); view.Refresh(); view.PoseTrain();
        presentation.Refresh(); map.GetComponent<SuitcaseLocomotiveView>().Frame();
        map.gameObject.SetActive(active);
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Forest Harbor installed: 50 on-track levels, curved railway, two toy locomotives, selector, timer and fullscreen controls. Edit mode only.";
    }

    public static void BuildRoute(TwilightRailwayMap map, ForestHarborMapPresentation presentation)
    {
        var stations = StationPositions(presentation);
        var keys = new List<Vector2> { P(140,785), P(143,750) };
        var stationKeys = new int[50]; int entrance = 0, exit = 0, forestEntrance=0, forestExit=0;
        for (int i=0; i<50; i++)
        {
            if (presentation.roundForestStops && i < 30)
                BuildForestHarborRoundStops.AddCurveKeys(keys, i);
            if (!presentation.roundForestStops && i==10) keys.AddRange(new[]{P(783,608),P(795,566),P(782,525)});
            if (!presentation.roundForestStops && i==20)
            {
                if (presentation.forestOpenCurve)
                {
                    // The train remains above ground on the painted open cutting between 20 and 21.
                    keys.AddRange(new[]{P(102,413),P(77,380),P(71,355),P(67,330),P(72,310),
                        P(89,290),P(107,267),P(104,242),P(97,220),P(103,204)});
                }
                else
                {
                    keys.AddRange(new[]{P(102,413),P(77,380),P(61,326),P(47,282)});forestEntrance=keys.Count-1;
                    keys.AddRange(new[]{P(72,270),P(111,289)});forestExit=keys.Count-1;
                    keys.AddRange(new[]{P(125,259),P(107,222),P(103,204)});
                }
            }
            if (i==30) keys.AddRange(new[]{P(768,275),P(812,254),P(854,237),P(890,234),P(937,239),P(988,247)});
            if (i==40)
            {
                keys.AddRange(new[]{P(1627,379),P(1642,355)}); entrance=keys.Count-1;
                keys.AddRange(new[]{P(1615,327),P(1545,331),P(1360,381),P(1150,398),P(1036,418),P(998,446)});
                exit=keys.Count-1; keys.Add(P(984,478));
            }
            keys.Add(stations[i]); stationKeys[i] = keys.Count-1;
        }
        keys.Add(P(1586,625));
        var points = new List<Vector3>(); var keyDistances = new float[keys.Count]; float distance = 0;
        for (int k=0; k<keys.Count-1; k++)
        {
            var a=keys[Mathf.Max(0,k-1)];var b=keys[k];var c=keys[k+1];var d=keys[Mathf.Min(keys.Count-1,k+2)];
            int steps=Mathf.Max(8, Mathf.CeilToInt(Vector2.Distance(b,c)/3));
            for(int j=0;j<steps;j++)
            {
                float t=(float)j/steps;
                var pixel=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                var art=Art(pixel);var point=new Vector3(art.x/20,0,art.y/20);
                if(points.Count>0)distance+=Vector3.Distance(points[points.Count-1],point);
                points.Add(point);if(j==0)keyDistances[k]=distance;
            }
        }
        var last=Art(keys[keys.Count-1]);points.Add(new Vector3(last.x/20,0,last.y/20));
        map.railway=points.ToArray();
        for(int i=0;i<50;i++)map.stops[i].distance=keyDistances[stationKeys[i]];
        presentation.harborTunnelEntrance=keyDistances[entrance];presentation.harborTunnelExit=keyDistances[exit];
        presentation.forestTunnelEntrance=keyDistances[forestEntrance];presentation.forestTunnelExit=keyDistances[forestExit];
        // Rebuild the private distance cache immediately, before fitting the stop hit areas.
        typeof(TwilightRailwayMap).GetMethod("Cache",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(map,null);
    }

    public static Vector2[] StationPositions(ForestHarborMapPresentation presentation)
    {
        var positions = (Vector2[])Stations.Clone();
        if (presentation != null && presentation.roundForestStops)
            Array.Copy(BuildForestHarborRoundStops.ForestStations, positions, 30);
        return positions;
    }

    static void ConfigureTrain(TwilightRailwayMap map, TwilightIllustratedMenu view)
    {
        var old=map.transform.Find("ForestHarborLocomotive");if(old!=null)Object.DestroyImmediate(old.gameObject);
        if(map.train!=null)map.train.gameObject.SetActive(false);
        ToyBoxGeometry.Initialize();
        var train=new GameObject("ForestHarborLocomotive");train.transform.SetParent(map.transform,false);train.layer=TrainLayer;
        var mechanism=train.AddComponent<SuitcaseTrainMechanism>();
        mechanism.woodland=Engine(train.transform,"Woodland",false);
        mechanism.moonlight=Engine(train.transform,"Harbor",true);
        map.train=train.transform;map.train.localScale=Vector3.one;map.trainMechanism=mechanism;
        map.wheels=Array.Empty<Transform>();map.steam=Array.Empty<Transform>();
        var rig=map.GetComponent<SuitcaseLocomotiveView>();
        rig.map=map;rig.verticalSize=2;rig.centerOffset=new Vector3(0,.9f,0);rig.cameraOffset=new Vector3(0,6,-10.3923f);
        var camera=rig.trainCamera; camera.cullingMask=1<<TrainLayer;
        camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        const string rtPath=Folder+"/ToyLocomotive.renderTexture";
        var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if(rt==null){rt=new RenderTexture(720,480,24,RenderTextureFormat.ARGB32){antiAliasing=4,name="ToyLocomotive"};AssetDatabase.CreateAsset(rt,rtPath);}
        camera.targetTexture=rt;
        view.locomotive.GetComponent<RawImage>().texture=rt;
        view.locomotive.GetComponent<RawImage>().color=Color.white;
        view.locomotive.sizeDelta=P(144,96);
        view.forestTrainGroundOffset=18.7f;
        var lightRoot=map.transform.Find("ForestHarborTrainLights");if(lightRoot!=null)Object.DestroyImmediate(lightRoot.gameObject);
        lightRoot=new GameObject("ForestHarborTrainLights").transform;lightRoot.SetParent(map.transform,false);
        KeyLight(lightRoot,"Warm daylight",new Vector3(40,-35,0),new Color(1,.88f,.68f),1.5f);
        KeyLight(lightRoot,"Sky fill",new Vector3(25,145,0),new Color(.68f,.82f,1),.7f);
        rig.Frame();
    }
    static SuitcaseTrainMechanism.Engine Engine(Transform parent,string name,bool city)
    {
        var root=new GameObject(name).transform;root.SetParent(parent,false);root.gameObject.layer=TrainLayer;
        var navy=Material("ToyNavy",new Color(.035f,.25f,.58f));
        var red=Material("ToyRed",new Color(.75f,.16f,.08f));
        var yellow=Material("ToyYellow",new Color(.98f,.61f,.12f));
        var green=Material("ToyGreen",new Color(.06f,.36f,.2f));
        var dark=Material("ToyRubber",new Color(.08f,.07f,.055f));
        var cream=Material("ToyMaple",new Color(1,.84f,.5f));
        Box(root,"Chassis",P3(0,.62f,0),P3(3.05f,.28f,1.08f),.12f,red);
        Part(root,"Boiler",ToyBoxGeometry.Disc(.53f,1.6f,.12f),P3(.38f,1.12f,0),Quaternion.Euler(0,0,90),navy);
        Part(root,"Boiler cap",ToyBoxGeometry.Disc(.5f,.17f,.07f),P3(1.2f,1.12f,0),Quaternion.Euler(0,0,90),city?yellow:red);
        Part(root,"Headlamp",ToyBoxGeometry.Disc(.19f,.13f,.04f),P3(1.31f,1.27f,0),Quaternion.Euler(0,0,90),cream);
        Box(root,"Cab",P3(-.91f,1.14f,0),P3(.88f,1.05f,.94f),.12f,city?green:navy);
        Box(root,"Cab roof",P3(-.93f,1.77f,0),P3(1.24f,.23f,1.24f),.1f,city?navy:red);
        foreach(float side in new[]{-1f,1f})
            Box(root,"Cab window",P3(-.9f,1.39f,.483f*side),P3(.51f,.43f,.06f),.06f,cream);
        Part(root,"Chimney",ToyBoxGeometry.Disc(.17f,.55f,.05f),P3(.8f,1.85f,0),Quaternion.identity,navy);
        Part(root,"Chimney lip",ToyBoxGeometry.Disc(.25f,.13f,.04f),P3(.8f,2.14f,0),Quaternion.identity,yellow);
        Part(root,"Steam dome",ToyBoxGeometry.Disc(.22f,.24f,.08f),P3(-.05f,1.67f,0),Quaternion.identity,yellow);
        Box(root,"Front bumper",P3(1.55f,.58f,0),P3(.25f,.3f,1.1f),.1f,yellow);
        var wheels=new List<SuitcaseTrainMechanism.Wheel>();
        foreach(float x in new[]{-1.01f, .02f,1.01f}) foreach(float side in new[]{-1f,1f})
        {
            var pivot=new GameObject("RollingWheel").transform;pivot.SetParent(root,false);pivot.localPosition=P3(x,.4f,side*.61f);pivot.gameObject.layer=TrainLayer;
            Part(pivot,"Tire",ToyBoxGeometry.Disc(.4f,.17f,.06f),Vector3.zero,Quaternion.Euler(90,0,0),dark);
            Part(pivot,"Red wooden wheel",ToyBoxGeometry.Disc(.32f,.19f,.055f),Vector3.zero,Quaternion.Euler(90,0,0),red);
            Part(pivot,"Axle hub",ToyBoxGeometry.Disc(.12f,.22f,.035f),Vector3.zero,Quaternion.Euler(90,0,0),yellow);
            // A real radial inlay makes the entire 360-degree rotation visible at menu scale.
            Box(pivot,"Wheel inlay",P3(.14f,0,side*.106f),P3(.19f,.06f,.025f),.012f,yellow);
            wheels.Add(new SuitcaseTrainMechanism.Wheel{pivot=pivot,radius=.4f});
        }
        return new SuitcaseTrainMechanism.Engine{body=root.gameObject,wheels=wheels.ToArray(),gear=Array.Empty<SuitcaseTrainMechanism.SideGear>()};
    }
    static Vector3 P3(float x,float y,float z)=>new Vector3(x,y,z);
    static Material Material(string name,Color color)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("BUCA/Woodland Toy")){name=name};AssetDatabase.CreateAsset(material,path);}
        material.shader=Shader.Find("BUCA/Woodland Toy");
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.28f);material.SetFloat("_Metallic",0);material.SetFloat("_GrainStrength",.035f);EditorUtility.SetDirty(material);return material;
    }
    static void Box(Transform p,string n,Vector3 pos,Vector3 size,float radius,Material m)=>Part(p,n,ToyBoxGeometry.RoundedBox(size,radius),pos,Quaternion.identity,m);
    static void Part(Transform p,string n,Mesh mesh,Vector3 pos,Quaternion rot,Material material)
    {
        var go=new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));go.layer=TrainLayer;go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localRotation=rot;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void KeyLight(Transform root,string name,Vector3 rotation,Color color,float intensity)
    {
        var go=new GameObject(name,typeof(Light));go.transform.SetParent(root,false);go.transform.localRotation=Quaternion.Euler(rotation);
        var light=go.GetComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;light.cullingMask=1<<TrainLayer;
    }
    static void Place(RectTransform rect,Vector2 pixel,Vector2 size)
    {
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=Art(pixel);rect.sizeDelta=size;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 pixel,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;Place(rect,pixel,size);return rect;
    }
    static void ConfigureButton(Button button,Vector2 pixel,Vector2 size)
    {
        Place((RectTransform)button.transform,pixel,size);button.gameObject.SetActive(true);
        button.image.sprite=null;button.image.color=Color.clear;button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};
    }
    static Button Hotspot(string name,Transform parent,Vector2 pixel,Vector2 size)
    {
        var rect=Rect(name,parent,pixel,size);var image=rect.gameObject.AddComponent<Image>();image.color=Color.clear;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};return button;
    }
    static void Label(TMP_Text label,Vector2 pixel,Vector2 size,float fontSize,Transform parent)
    {
        label.transform.SetParent(parent,false);Place(label.rectTransform,pixel,size);label.gameObject.SetActive(true);
        label.font=font;label.fontSize=fontSize;label.enableAutoSizing=false;label.color=new Color(.025f,.065f,.16f);label.alignment=TextAlignmentOptions.Center;
        label.fontStyle=FontStyles.Normal;label.characterSpacing=0;label.textWrappingMode=TextWrappingModes.NoWrap;label.raycastTarget=false;label.margin=Vector4.zero;
    }
    static TMP_Text Text(string name,Transform parent,Vector2 pixel,Vector2 size,float sizeText)
    {
        var rect=Rect(name,parent,pixel,size);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();Label(text,pixel,size,sizeText,parent);return text;
    }
    static TMP_FontAsset BakeFont(string name,string characters)
    {
        string path=Folder+"/"+name+" SDF.asset";var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(asset!=null){ArcadeFontClarity.Configure(asset);return asset;}
        var source=AssetDatabase.LoadAssetAtPath<Font>(Folder+"/"+name+".ttf");
        asset=TMP_FontAsset.CreateFontAsset(source,90,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
        asset.name=name+" SDF";RequireFont(asset.TryAddCharacters(characters),name);
        AssetDatabase.CreateAsset(asset,path);AssetDatabase.AddObjectToAsset(asset.material,asset);
        foreach(var atlas in asset.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,asset);
        asset.atlasPopulationMode=AtlasPopulationMode.Static;ArcadeFontClarity.Configure(asset);EditorUtility.SetDirty(asset);return asset;
    }
    static void RequireFont(bool ok,string name){if(!ok)throw new Exception("Missing glyph in "+name);}
}
#endif
