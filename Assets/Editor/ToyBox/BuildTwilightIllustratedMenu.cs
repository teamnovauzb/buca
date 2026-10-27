#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class BuildTwilightIllustratedMenu
{
    const string Folder = "Assets/TwilightMap/Artwork";
    static readonly Color Ink = new Color(.16f,.09f,.035f);
    static TMP_FontAsset font;
    static Sprite ivory, locked, selected, glow, play, small;

    [MenuItem("RealBuca/Twilight Railway/Install Illustrated Fullscreen Menu")]
    public static string Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play mode before installing.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity" || scene.isDirty) throw new Exception("Open the saved MainMenu scene first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || map.stops.Length != 50) throw new Exception("The existing 50-stop railway is required.");
        if (map.illustratedMenu != null) return "Illustrated menu is already installed.";
        Directory.CreateDirectory("output/twilight-fullscreen");
        File.Copy(scene.path, "output/twilight-fullscreen/MainMenu-before-illustrated.unity", false);
        ImportTexture(Folder + "/TwilightCrossingBackground.png", false);
        ImportTexture(Folder + "/JourneyLocomotive.png", true);
        font = MakeFont();
        ivory = MakePlaque("IvoryStop", new Color(1,.89f,.65f), new Color(.8f,.57f,.28f), false);
        locked = MakePlaque("LockedStop", new Color(.27f,.34f,.35f), new Color(.1f,.17f,.2f), false);
        selected = MakePlaque("SelectedStop", new Color(.62f,1,.85f), new Color(.08f,.58f,.5f), false);
        glow = MakeGlow();
        play = MakePlaque("PlayPlaque", new Color(1,.88f,.62f), new Color(.79f,.48f,.19f), true);
        small = MakePlaque("TealPlaque", new Color(.065f,.19f,.20f), new Color(.018f,.075f,.085f), true);

        var presentationSpace = new GameObject("UnscaledPresentation").transform;
        presentationSpace.SetParent(map.transform, false);
        var inheritedScale = map.transform.lossyScale;
        presentationSpace.localScale = new Vector3(1/inheritedScale.x, 1/inheritedScale.y, 1/inheritedScale.z);
        var root = Rect("IllustratedFullscreenMenu", presentationSpace, Vector2.zero, Vector2.zero);
        var view = root.gameObject.AddComponent<TwilightIllustratedMenu>();
        view.enabled = false;
        view.map = map;
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = map.viewCamera;
        canvas.planeDistance = 1;
        canvas.sortingOrder = 100;
        view.canvas = canvas;
        var scaler = root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672,941);
        scaler.matchWidthOrHeight = .5f;
        root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var background = Rect("FullBleedBackdrop", root, Vector2.zero, Vector2.zero);
        Stretch(background);
        var backgroundImage = background.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        backgroundImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/TwilightCrossingBackground.png");
        backgroundImage.color = new Color(.36f,.39f,.40f,1);
        backgroundImage.raycastTarget = false;
        var art = Rect("ArtAndRailway", root, Vector2.zero, new Vector2(1672,941));
        view.artwork = art;
        var artImage = art.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        artImage.texture = backgroundImage.texture;
        artImage.raycastTarget = false;
        view.route = Route();
        view.stopRects = new RectTransform[50];
        view.stopButtons = new UnityEngine.UI.Button[50];
        view.faces = new UnityEngine.UI.Image[50];
        view.glows = new UnityEngine.UI.Image[50];
        view.numbers = new TMP_Text[50];
        view.locks = new GameObject[50];
        view.awards = new GameObject[50];
        view.ivoryFace = ivory; view.lockedFace = locked; view.selectedFace = selected;
        for (int i = 0; i < 50; i++)
        {
            float y = 470.5f - view.route[i].y;
            float width = Mathf.Lerp(41,66,Mathf.InverseLerp(180,680,y));
            if (i >= 30) width *= .82f;
            var stop = Rect("Level_" + (i+1).ToString("00"), art, view.route[i], new Vector2(width,width*.74f));
            var halo = Image("SelectionGlow", stop, Vector2.zero, new Vector2(width*1.5f,width*1.2f), glow);
            halo.color = new Color(.3f,1,.82f,.8f);
            var face = Image("IvoryPuck", stop, Vector2.zero, stop.sizeDelta, ivory);
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = face;
            var colors = button.colors;
            colors.highlightedColor = new Color(1,1,.86f);
            colors.pressedColor = new Color(.7f,.9f,.8f);
            colors.fadeDuration = .09f;
            button.colors = colors;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var label = Text("Number", stop, (i+1).ToString(), new Vector2(0,3), stop.sizeDelta, width*.45f, Ink);
            var lockMark = Rect("Lock", stop, new Vector2(width*.3f,-width*.18f), new Vector2(9,10));
            var shackle = Image("Shackle",lockMark,new Vector2(0,3),new Vector2(6,6),null);
            shackle.color = new Color(.85f,.73f,.45f);
            var hole = Image("Opening",lockMark,new Vector2(0,3),new Vector2(3,4),null);
            hole.color = new Color(.12f,.21f,.22f);
            var body = Image("Body",lockMark,new Vector2(0,-1),new Vector2(8,5),null);
            body.color = shackle.color;
            var award = Text("Completed",stop,"•",new Vector2(-width*.31f,-width*.17f),new Vector2(14,14),15,new Color(.12f,.45f,.28f));
            view.stopRects[i]=stop;view.stopButtons[i]=button;view.faces[i]=face;view.glows[i]=halo;
            view.numbers[i]=label;view.locks[i]=lockMark.gameObject;view.awards[i]=award.gameObject;
        }
        var engine = Image("JourneyLocomotive", art, Vector2.zero, new Vector2(132,88), AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/JourneyLocomotive.png"));
        engine.preserveAspect = true;
        view.locomotive = engine.rectTransform;
        var controls = Rect("SafeControls",root,new Vector2(0,26),new Vector2(1300,155));
        controls.anchorMin=controls.anchorMax=new Vector2(.5f,0); controls.pivot=new Vector2(.5f,0);
        view.controls=controls;
        view.statusText=Text("Chapter",controls,"THE TWILIGHT CROSSING",new Vector2(0,57),new Vector2(820,28),19,new Color(.95f,.82f,.56f));
        view.statusText.characterSpacing=2;
        view.playButton=Button("Play",controls,new Vector2(0,4),new Vector2(440,87),play);
        view.playText=Text("PlayLabel",view.playButton.transform,"PLAY LEVEL 1  ›",Vector2.zero,new Vector2(410,72),41,Ink);
        view.backButton=Button("Back",controls,new Vector2(-535,-3),new Vector2(132,44),small);
        Text("BackLabel",view.backButton.transform,"‹  MENU",Vector2.zero,new Vector2(125,38),20,new Color(.97f,.85f,.61f));
        view.fullscreenButton=Button("Fullscreen",controls,new Vector2(535,-3),new Vector2(148,44),small);
        view.fullscreenText=Text("FullscreenLabel",view.fullscreenButton.transform,"FULL SCREEN",Vector2.zero,new Vector2(140,38),18,new Color(.97f,.85f,.61f));
        view.countdownText=Text("Departure",controls,"DEPARTING IN  30",new Vector2(0,-53),new Vector2(400,23),15,new Color(.95f,.84f,.63f));
        view.countdownText.characterSpacing=2;
        Text("KeyboardHint",controls,"←  SELECT LEVEL  →",new Vector2(-535,-38),new Vector2(225,22),13,new Color(.88f,.79f,.6f));

        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        map.illustratedMenu=view;
        map.viewCamera.cullingMask=1<<5;
        map.viewCamera.rect=new UnityEngine.Rect(0,0,1,1);
        var cameraData=map.viewCamera.GetComponent<UniversalAdditionalCameraData>();
        if(cameraData!=null)cameraData.renderPostProcessing=false;
        view.enabled=true;
        bool active=map.gameObject.activeSelf;
        map.gameObject.SetActive(true); map.Open(null);
        Canvas.ForceUpdateCanvases();view.Layout();view.Refresh();view.PoseTrain();
        map.gameObject.SetActive(active);
        PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
        EditorUtility.SetDirty(map);EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Installed full-screen illustrated menu with 50 live stops, train, Play, Menu and fullscreen controls.";
    }

    static TMP_FontAsset MakeFont()
    {
        const string path=Folder+"/Fonts/CormorantMenu SDF.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(existing!=null){ArcadeFontClarity.Configure(existing);return existing;}
        var source=AssetDatabase.LoadAssetAtPath<Font>(Folder+"/Fonts/CormorantGaramond-Bold.ttf");
        var asset=TMP_FontAsset.CreateFontAsset(source,64,7,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
        asset.name="CormorantMenu SDF";
        asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 /·›‹←→•:.-");
        AssetDatabase.CreateAsset(asset,path);
        AssetDatabase.AddObjectToAsset(asset.material,asset);
        foreach(var atlas in asset.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,asset);
        asset.atlasPopulationMode=AtlasPopulationMode.Static;
        ArcadeFontClarity.Configure(asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;
        var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Sprite sprite)
    {var image=Rect(name,parent,pos,size).gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.raycastTarget=false;return image;}
    static TMP_Text Text(string name,Transform parent,string text,Vector2 pos,Vector2 size,float fontSize,Color color)
    {
        var label=Rect(name,parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font;label.text=text;label.fontSize=fontSize;label.color=color;
        label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;
        label.raycastTarget=false;label.overflowMode=TextOverflowModes.Overflow;return label;
    }
    static UnityEngine.UI.Button Button(string name,Transform parent,Vector2 pos,Vector2 size,Sprite sprite)
    {
        var image=Image(name,parent,pos,size,sprite);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
        button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
        var colors=button.colors;colors.highlightedColor=new Color(1,1,.88f);colors.pressedColor=new Color(.8f,.8f,.65f);colors.disabledColor=new Color(.55f,.6f,.57f);button.colors=colors;
        return button;
    }
    static void ImportTexture(string path,bool sprite)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;
        importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=sprite;
        importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
    }
    // Small native UI assets, baked once in the Editor. The generated scene artwork is left intact.
    static Sprite MakePlaque(string name,Color top,Color bottom,bool wide)
    {
        int w=wide?640:256,h=wide?136:192;
        var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
        var pixels=new Color[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float px=x-w*.5f,py=y-h*.5f;
            float radius=wide?58:45;
            float qx=Mathf.Abs(px)-(w*.5f-radius-5),qy=Mathf.Abs(py)-(h*.5f-radius-5);
            float sdf=new Vector2(Mathf.Max(qx,0),Mathf.Max(qy,0)).magnitude+Mathf.Min(Mathf.Max(qx,qy),0)-radius;
            if(sdf>0)continue;
            var color=Color.Lerp(bottom,top,y/(float)h);
            if(sdf> -3)color=new Color(.28f,.12f,.035f);
            else if(sdf> -7)color=Color.Lerp(new Color(.58f,.3f,.09f),new Color(1,.88f,.57f),y/(float)h);
            else if(sdf> -10)color=new Color(1,.88f,.62f);
            else if(sdf> -14)color=new Color(.62f,.34f,.12f);
            else if(sdf> -17)color=Color.Lerp(bottom,Color.white,.45f);
            else if(!wide&&y<h*.22f)color=Color.Lerp(new Color(.56f,.30f,.10f),bottom,.5f);
            color.a=Mathf.Clamp01(-sdf);pixels[y*w+x]=color;
        }
        texture.SetPixels(pixels);texture.Apply();
        string path=Folder+"/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);ImportTexture(path,true);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite MakeGlow()
    {
        const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {float radius=new Vector2((x-63.5f)/63.5f,(y-63.5f)/63.5f).magnitude;pixels[y*size+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),1.5f));}
        texture.SetPixels(pixels);texture.Apply();string path=Folder+"/SelectionGlow.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);ImportTexture(path,true);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Vector2[] Route()
    {
        var left=new[]{new Vector2(252,423),new Vector2(161,448),new Vector2(111,500),new Vector2(116,557),new Vector2(190,612),new Vector2(310,651),new Vector2(470,675),new Vector2(628,654),new Vector2(761,604),new Vector2(844,546),new Vector2(859,489),new Vector2(829,431),new Vector2(771,385),new Vector2(687,348),new Vector2(587,319),new Vector2(476,290),new Vector2(432,262),new Vector2(441,225),new Vector2(486,199),new Vector2(559,180),new Vector2(650,180),new Vector2(746,193),new Vector2(827,222),new Vector2(900,254),new Vector2(963,275)};
        var right=new[]{new Vector2(1030,285),new Vector2(1110,303),new Vector2(1196,326),new Vector2(1286,353),new Vector2(1377,385),new Vector2(1463,421),new Vector2(1535,464),new Vector2(1578,511),new Vector2(1575,554),new Vector2(1540,586),new Vector2(1484,609),new Vector2(1418,620),new Vector2(1350,620)};
        var route=new List<Vector2>();route.AddRange(Resample(left,30));route.AddRange(Resample(right,20));
        for(int i=0;i<route.Count;i++)route[i]=new Vector2(route[i].x-836,470.5f-route[i].y);
        return route.ToArray();
    }
    static IEnumerable<Vector2> Resample(Vector2[] keys,int count)
    {
        var path=new List<Vector2>();
        for(int i=0;i<keys.Length-1;i++)for(int j=0;j<16;j++)
        {
            float t=j/16f;var a=keys[Mathf.Max(0,i-1)];var b=keys[i];var c=keys[i+1];var d=keys[Mathf.Min(keys.Length-1,i+2)];
            path.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
        }
        path.Add(keys[keys.Length-1]);var length=new float[path.Count];for(int i=1;i<length.Length;i++)length[i]=length[i-1]+Vector2.Distance(path[i-1],path[i]);
        for(int n=0;n<count;n++){float distance=length[length.Length-1]*n/(count-1);int i=1;while(i<length.Length-1&&length[i]<distance)i++;yield return Vector2.Lerp(path[i-1],path[i],Mathf.InverseLerp(length[i-1],length[i],distance));}
    }
}
#endif
