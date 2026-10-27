#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Applies the chosen suitcase artwork to the existing selector and traces its continuous railway.</summary>
public static class BuildSecretSuitcaseMenu
{
    const string Folder = "Assets/TwilightMap/Artwork";
    const int TrainLayer = 28;
    static Vector2 P(float x, float y) => new Vector2(x, y);
    static Vector2 Art(Vector2 p) => new Vector2(p.x - 836, 470.5f - p.y);

    // Plaque centres in the source artwork. Do not resample these: each is an actual selectable level.
    public static readonly Vector2[] Stations = {
        P(245,648),P(359,652),P(475,652),P(577,654),P(685,654),P(790,657),
        P(774,546),P(668,548),P(560,548),P(451,548),P(341,547),P(239,544),
        P(229,440),P(335,443),P(439,443),P(549,441),P(650,438),P(754,433),
        P(749,328),P(657,327),P(558,328),P(450,328),P(341,325),P(230,324),
        P(240,223),P(348,225),P(450,225),P(550,225),P(647,224),P(741,222),
        P(1077,225),P(1194,232),P(1303,234),P(1417,234),
        P(1436,336),P(1318,337),P(1205,336),P(1092,332),
        P(1118,444),P(1233,448),P(1348,449),P(1464,448),
        P(1473,553),P(1351,555),P(1230,552),P(1110,548),
        P(1129,655),P(1254,663),P(1382,665),P(1504,663)
    };
    static readonly Vector2[] RailStops = {
        P(245,676),P(359,679),P(475,680),P(577,680),P(685,683),P(790,682),
        P(774,572),P(668,574),P(560,574),P(451,575),P(341,574),P(239,570),
        P(229,466),P(335,470),P(439,469),P(549,467),P(650,462),P(754,455),
        P(749,348),P(657,351),P(558,351),P(450,351),P(341,350),P(230,347),
        P(240,248),P(348,251),P(450,250),P(550,250),P(647,248),P(741,244),
        P(1077,251),P(1194,258),P(1303,258),P(1417,257),
        P(1436,357),P(1318,361),P(1205,361),P(1092,356),
        P(1118,466),P(1233,469),P(1348,473),P(1464,472),
        P(1473,575),P(1351,581),P(1230,582),P(1110,575),
        P(1129,681),P(1254,689),P(1382,693),P(1504,689)
    };

    [MenuItem("RealBuca/Twilight Railway/Apply Secret Suitcase")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var view = map != null ? map.illustratedMenu : null;
        if (view == null || view.stopButtons.Length != 50) throw new Exception("The existing 50-level illustrated menu is required.");
        var events = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        if (events == null) throw new Exception("The menu EventSystem is missing.");
        events.gameObject.SetActive(true); events.enabled = true;
        var input = events.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (input == null) input = events.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        input.enabled = true;
        var path = Folder + "/SecretSuitcaseBackground.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        view.artwork.GetComponent<UnityEngine.UI.RawImage>().texture = background;
        view.transform.Find("FullBleedBackdrop").GetComponent<UnityEngine.UI.RawImage>().texture = background;
        view.suitcasePresentation = true;
        view.route = new Vector2[50];
        for (int i = 0; i < 50; i++)
        {
            view.route[i] = Art(Stations[i]);
            var rect = view.stopRects[i];
            rect.anchoredPosition = view.route[i]; rect.sizeDelta = new Vector2(58, 48);
            view.faces[i].rectTransform.sizeDelta = new Vector2(50, 39);
            view.faces[i].sprite = view.ivoryFace;
            view.faces[i].color = Color.clear;
            view.stopButtons[i].transition = UnityEngine.UI.Selectable.Transition.None;
            view.numbers[i].gameObject.SetActive(false); // Exact numbering is retained from the selected artwork.
            // The signs stand in front of the rails. Sample their existing artwork into a
            // foreground UI layer so the moving engine cannot hide another level's number.
            var foreground = rect.Find("ForegroundSign") as RectTransform;
            if(foreground==null)
            {
                foreground=(RectTransform)new GameObject("ForegroundSign",typeof(RectTransform)).transform;
                foreground.gameObject.layer=5;foreground.SetParent(rect,false);
                var maskImage=foreground.gameObject.AddComponent<UnityEngine.UI.Image>();
                maskImage.sprite=view.ivoryFace;maskImage.raycastTarget=false;
                foreground.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
                var sample=(RectTransform)new GameObject("OriginalPlaque",typeof(RectTransform),typeof(UnityEngine.UI.RawImage)).transform;
                sample.gameObject.layer=5;sample.SetParent(foreground,false);
            }
            foreground.sizeDelta=new Vector2(52,42);foreground.anchoredPosition=Vector2.zero;foreground.SetSiblingIndex(1);
            var plaque=foreground.GetComponentInChildren<UnityEngine.UI.RawImage>();
            plaque.rectTransform.sizeDelta=foreground.sizeDelta;plaque.raycastTarget=false;plaque.texture=background;
            plaque.uvRect=new UnityEngine.Rect((Stations[i].x-26)/1672f,1-(Stations[i].y+21)/941f,52/1672f,42/941f);
            view.glows[i].rectTransform.sizeDelta = new Vector2(92, 82);
            view.locks[i].transform.localPosition = new Vector3(18, -19, 0);
            view.awards[i].transform.localPosition = new Vector3(-18, -18, 0);
        }
        BuildRoute(map);
        map.stationPause = .6f;
        map.arrival = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/StationGateClose.wav");

        // Keep the live controls inside the exact brass button and lower leather panel in the artwork.
        view.controls.SetParent(view.artwork, false);
        view.controls.anchorMin = view.controls.anchorMax = view.controls.pivot = new Vector2(.5f, .5f);
        view.controls.anchoredPosition = Vector2.zero;
        view.controls.sizeDelta = new Vector2(1672, 941); view.controls.localScale = Vector3.one;
        SetRect(view.playButton.transform, P(836,849), P(422,68));
        view.playButton.image.sprite = null; view.playButton.image.color = Color.clear;
        view.playButton.transition = UnityEngine.UI.Selectable.Transition.None;
        ConfigurePlayLabel(view);
        SetRect(view.statusText.transform, P(836,917), P(660,19));
        view.statusText.fontSize = 12; view.statusText.characterSpacing = 2;
        view.statusText.gameObject.SetActive(false);
        SetRect(view.countdownText.transform, P(836,917), P(440,22));
        view.countdownText.fontSize = 14;
        SetRect(view.backButton.transform, P(330,858), P(120,39));
        SetRect(view.fullscreenButton.transform, P(1350,858), P(148,39));
        var keyboard = view.controls.Find("KeyboardHint");
        if (keyboard != null) SetRect(keyboard, P(330,892), P(230,22));
        // Reapplying the map must preserve an already-installed pocket-watch presentation.
        if (view.controls.Find("PocketWatch") != null) BuildSuitcasePocketWatch.Configure(view);

        ConfigureTrain(map, view);
        view.locomotive.SetAsFirstSibling();
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        bool active = map.gameObject.activeSelf; map.gameObject.SetActive(true);
        map.Open(null); Canvas.ForceUpdateCanvases(); view.Layout(); view.Refresh(); view.PoseTrain();
        map.GetComponent<SuitcaseLocomotiveView>().Frame();
        map.gameObject.SetActive(active);
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Secret Suitcase installed: all 50 original plaques, traced railway, live 3D locomotive, station dwell, movement audio and fullscreen controls.";
    }

    public static void ConfigurePlayLabel(TwilightIllustratedMenu view)
    {
        const string fontPath = Folder + "/Fonts/BucaPlaque SDF.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (font == null)
        {
            // The source keeps Cormorant's lettering but maps digits to its lining figures.
            var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/Fonts/BucaPlaque-Bold.ttf");
            if (source == null) throw new Exception("The BucaPlaque font source is missing.");
            font = TMP_FontAsset.CreateFontAsset(source, 80, 8,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            font.name = "BucaPlaque SDF";
            if (!font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 /·›‹:.-"))
                throw new Exception("The Play label font is missing required glyphs.");
            AssetDatabase.CreateAsset(font, fontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);
        }
        var label = view.playText;
        label.font = font;
        label.rectTransform.anchoredPosition = Vector2.zero;
        label.rectTransform.sizeDelta = new Vector2(412, 65);
        label.rectTransform.localScale = Vector3.one;
        label.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.35f);
        label.fontSize = 36;
        label.enableAutoSizing = false;
        label.alignment = TextAlignmentOptions.MidlineGeoAligned;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = new Color(1, .78f, .39f);
        EditorUtility.SetDirty(label);
    }

    static void SetRect(Transform transform, Vector2 pixelPosition, Vector2 size)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = Art(pixelPosition); rect.sizeDelta = size;
    }

    static void ConfigureTrain(TwilightRailwayMap map, TwilightIllustratedMenu view)
    {
        foreach (var t in map.train.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = TrainLayer;
        // Isolate the miniature's lighting from the much larger hidden diorama's lamps.
        foreach (var renderer in map.train.GetComponentsInChildren<Renderer>(true))
        {
            if (map.trainMechanism != null) continue; // Preserve the authored two-world enamel and brass.
            var materials = renderer.sharedMaterials;
            if(Array.Exists(materials,m=>m.shader.name=="BUCA/Station Contact")) renderer.enabled=false;
            for(int i=0;i<materials.Length;i++)
            {
                var source = materials[i];
                if(source.shader.name != "Universal Render Pipeline/Lit") continue;
                string assetPath = Folder + "/SuitcaseTrain-" + source.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if(material == null)
                {
                    material = new Material(Shader.Find("BUCA/Station Toy")) { name="SuitcaseTrain-"+source.name };
                    material.SetColor("_BaseColor",source.GetColor("_BaseColor"));
                    material.SetFloat("_Metallic",source.GetFloat("_Metallic"));
                    material.SetFloat("_Smoothness",.4f);
                    material.SetFloat("_GrainStrength",0);
                    if(source.name=="StationIvory") material.SetColor("_BaseColor",new Color(.035f,.12f,.25f));
                    AssetDatabase.CreateAsset(material,assetPath);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
        map.train.localScale = Vector3.one * .75f;
        var oldSprite = view.locomotive.GetComponent<UnityEngine.UI.Image>();
        if (oldSprite != null) Object.DestroyImmediate(oldSprite);
        var image = view.locomotive.GetComponent<UnityEngine.UI.RawImage>();
        if (image == null) image = view.locomotive.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.raycastTarget = false; view.locomotive.sizeDelta = new Vector2(180,120);
        const string texturePath = Folder + "/SuitcaseLocomotive.renderTexture";
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(texturePath);
        if (texture == null)
        {
            texture = new RenderTexture(540,360,24,RenderTextureFormat.ARGB32) { name="SuitcaseLocomotive", antiAliasing=4 };
            AssetDatabase.CreateAsset(texture, texturePath);
        }
        image.texture = texture;
        var rig = map.GetComponent<SuitcaseLocomotiveView>();
        if (rig == null) rig = map.gameObject.AddComponent<SuitcaseLocomotiveView>();
        rig.map = map;
        if (rig.trainCamera == null)
        {
            var go = new GameObject("SuitcaseLocomotiveCamera", typeof(Camera)); go.transform.SetParent(map.transform, false);
            rig.trainCamera = go.GetComponent<Camera>();
        }
        var camera = rig.trainCamera;
        camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0,0,0,0); camera.cullingMask = 1 << TrainLayer;
        camera.targetTexture = texture; camera.nearClipPlane = .03f; camera.farClipPlane = 100;
        camera.allowHDR = false; camera.allowMSAA = true; camera.depth = map.viewCamera.depth - 10;
        var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.renderShadows = true;
        data.requiresColorOption = CameraOverrideOption.Off; data.requiresDepthOption = CameraOverrideOption.Off;
        rig.Frame();
    }

    static void BuildRoute(TwilightRailwayMap map)
    {
        var points = new List<Vector2> { P(145,670) };
        var stationIndices = new int[50];
        for (int i=0; i<50; i++)
        {
            var a = points[points.Count-1]; var b = RailStops[i];
            switch (i)
            {
                case 6: Curve(points,a,P(880,690),P(870,578),b); break;
                case 12: Curve(points,a,P(82,580),P(82,461),b); break;
                case 18: Curve(points,a,P(827,435),P(800,344),b); break;
                case 24: Curve(points,a,P(92,327),P(89,239),b); break;
                case 30:
                    Curve(points,a,P(806,242),P(851,192),P(899,203));
                    Curve(points,P(899,203),P(954,202),P(1001,253),b); break;
                case 34: Curve(points,a,P(1547,235),P(1548,358),b); break;
                case 38: Curve(points,a,P(979,326),P(1004,447),b); break;
                case 42: Curve(points,a,P(1550,462),P(1578,578),b); break;
                case 46: Curve(points,a,P(1008,552),P(1020,669),b); break;
                default: Line(points,a,b); break;
            }
            stationIndices[i] = points.Count-1;
        }
        map.railway = new Vector3[points.Count];
        float length = 0; var distances = new float[points.Count];
        for (int i=0; i<points.Count; i++)
        {
            var p = Art(points[i])/20;
            map.railway[i] = new Vector3(p.x,0,p.y);
            if(i>0) length += Vector3.Distance(map.railway[i-1],map.railway[i]);
            distances[i] = length;
        }
        // Park the engine just before each plaque, leaving its number visible.
        for(int i=0;i<50;i++) map.stops[i].distance = distances[stationIndices[i]] - 3.8f;
    }
    static void Line(List<Vector2> points, Vector2 a, Vector2 b)
    {
        int count = Mathf.CeilToInt(Vector2.Distance(a,b)/5);
        for(int i=1;i<=count;i++) points.Add(Vector2.Lerp(a,b,i/(float)count));
    }
    static void Curve(List<Vector2> points, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        for(int i=1;i<=48;i++)
        {
            float t=i/48f,s=1-t;
            points.Add(s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d);
        }
    }
}
#endif
