#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BuildSuitcaseDepartureBoard
{
    public const string BackgroundPath = "Assets/TwilightMap/Artwork/SecretSuitcaseDepartureBackground.png";
    public const string MarkerPath = "Assets/TwilightMap/Artwork/DepartureCanopy.png";
    static Vector2 Art(float x, float y) => new Vector2(x - 836, 470.5f - y);

    [MenuItem("RealBuca/Twilight Railway/Apply Departure Board Selector")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var view = Object.FindAnyObjectByType<TwilightIllustratedMenu>(FindObjectsInactive.Include);
        if (view == null || !view.suitcasePresentation || view.stopRects.Length != 50)
            throw new Exception("The existing 50-level suitcase menu is required.");
        Configure(view);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Departure selector installed: previous/next, live split-flap digits, movable station canopy, all 50 levels and existing travel/countdown.";
    }

    public static void Configure(TwilightIllustratedMenu view)
    {
        var background = Import(BackgroundPath, false);
        view.artwork.GetComponent<UnityEngine.UI.RawImage>().texture = background;
        view.transform.Find("FullBleedBackdrop").GetComponent<UnityEngine.UI.RawImage>().texture = background;
        foreach (var stop in view.stopRects)
            stop.Find("ForegroundSign/OriginalPlaque").GetComponent<UnityEngine.UI.RawImage>().texture = background;

        var root = Rect(view.controls, "DepartureBoard", Art(334,848), new Vector2(424,164));
        var board = root.GetComponent<SuitcaseDepartureBoard>();
        if (board == null) board = root.gameObject.AddComponent<SuitcaseDepartureBoard>();
        board.view = view;
        board.previousButton = Button(root, "PreviousLevel", new Vector2(-152,0), view.ivoryFace);
        board.nextButton = Button(root, "NextLevel", new Vector2(150,0), view.ivoryFace);
        var digits = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Roboto-Bold SDF.asset");
        if (digits == null) throw new Exception("The departure-board digit font is missing.");
        board.tens = Label(root, "Tens", new Vector2(-44,0), new Vector2(82,92), digits, 82);
        board.ones = Label(root, "Ones", new Vector2(44,0), new Vector2(82,92), digits, 82);
        board.tens.alignment = board.ones.alignment = TextAlignmentOptions.MidlineGeoAligned;
        board.caption = Label(root, "SelectedLevel", new Vector2(0,-57), new Vector2(182,25), view.playText.font, 18);
        // Lay the mechanical hinge over the live glyphs so each digit stays a split-flap tile.
        foreach (float x in new[] { -44f, 44f })
        {
            var seam = Rect(root, "Hinge" + x, new Vector2(x,1), new Vector2(79,1.7f));
            var ink = seam.GetComponent<UnityEngine.UI.Image>();
            if (ink == null) ink = seam.gameObject.AddComponent<UnityEngine.UI.Image>();
            ink.color = new Color(.015f,.02f,.018f,.88f);
            ink.raycastTarget = false;
        }

        var canopy = Import(MarkerPath, true);
        var marker = Rect(view.artwork, "SelectedStationCanopy", Vector2.zero, new Vector2(72,104));
        var art = marker.GetComponent<UnityEngine.UI.RawImage>();
        if (art == null) art = marker.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        art.texture = canopy;
        art.raycastTarget = false;
        art.uvRect = AlphaBounds(canopy);
        board.marker = marker;
        board.markerArt = art;
        marker.SetAsLastSibling();
        var importer = (TextureImporter)AssetImporter.GetAtPath(MarkerPath);
        importer.isReadable = false;
        importer.SaveAndReimport();

        ((RectTransform)view.backButton.transform).sizeDelta = new Vector2(104,35);
        view.backButton.GetComponentInChildren<TMP_Text>(true).text = "MENU";
        var hint = view.controls.Find("KeyboardHint");
        if (hint != null) hint.gameObject.SetActive(false);
        board.Refresh(true);
        view.Layout();
        board.LayoutMenu();
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(board);
    }

    static Texture2D Import(string path, bool readable)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new Exception("Missing artwork: " + path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.isReadable = readable;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static UnityEngine.Rect AlphaBounds(Texture2D image)
    {
        var pixels = image.GetPixels32();
        int minX=image.width,minY=image.height,maxX=0,maxY=0;
        for (int y=0;y<image.height;y++) for (int x=0;x<image.width;x++)
            if (pixels[y*image.width+x].a > 8)
            {minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        if(minX>maxX) throw new Exception("Canopy image is empty.");
        minX=Mathf.Max(0,minX-3);minY=Mathf.Max(0,minY-3);
        maxX=Mathf.Min(image.width-1,maxX+3);maxY=Mathf.Min(image.height-1,maxY+3);
        return new UnityEngine.Rect((float)minX/image.width,(float)minY/image.height,
            (float)(maxX-minX+1)/image.width,(float)(maxY-minY+1)/image.height);
    }

    static UnityEngine.UI.Button Button(Transform parent, string name, Vector2 position, Sprite shape)
    {
        var rect = Rect(parent,name,position,new Vector2(61,73));
        var image = rect.GetComponent<UnityEngine.UI.Image>();
        if(image==null)image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite=shape;image.color=Color.white;image.raycastTarget=true;
        var button=rect.GetComponent<UnityEngine.UI.Button>();
        if(button==null)button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic=image;
        button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
        button.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors=button.colors;
        colors.normalColor=Color.clear;
        colors.highlightedColor=new Color(1,.83f,.4f,.16f);
        colors.pressedColor=new Color(1,.83f,.4f,.3f);
        colors.selectedColor=colors.highlightedColor;
        colors.disabledColor=new Color(.02f,.03f,.02f,.52f);
        colors.fadeDuration=.08f;
        button.colors=colors;
        return button;
    }

    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var rect=parent.Find(name) as RectTransform;
        if(rect==null){rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);}
        rect.gameObject.layer=5;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;
        return rect;
    }

    static TMP_Text Label(Transform parent,string name,Vector2 position,Vector2 size,TMP_FontAsset font,float fontSize)
    {
        var rect=Rect(parent,name,position,size);
        var label=rect.GetComponent<TextMeshProUGUI>();
        if(label==null)label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font;label.fontSize=fontSize;label.fontStyle=FontStyles.Normal;
        label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=false;
        label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Overflow;
        label.color=new Color(1,.83f,.47f);label.raycastTarget=false;
        return label;
    }
}
#endif
