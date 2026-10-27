#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Installs the selected pocket watch on the existing suitcase controls.</summary>
public static class BuildSuitcasePocketWatch
{
    public const string BackgroundPath = "Assets/TwilightMap/Artwork/SecretSuitcasePocketWatchBackground.png";

    [MenuItem("RealBuca/Twilight Railway/Apply Pocket Watch Timer")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var view = Object.FindAnyObjectByType<TwilightIllustratedMenu>(FindObjectsInactive.Include);
        if (view == null || !view.suitcasePresentation) throw new Exception("The suitcase menu is required.");
        Configure(view);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Pocket watch installed: live 00:30 countdown, smooth green ring, travel pause and selection reset.";
    }

    public static void Configure(TwilightIllustratedMenu view)
    {
        AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
        if (importer == null) throw new Exception("The pocket-watch artwork is missing.");
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        view.artwork.GetComponent<UnityEngine.UI.RawImage>().texture = background;
        view.transform.Find("FullBleedBackdrop").GetComponent<UnityEngine.UI.RawImage>().texture = background;
        foreach (var stop in view.stopRects)
            stop.Find("ForegroundSign/OriginalPlaque").GetComponent<UnityEngine.UI.RawImage>().texture = background;

        var watchRoot = Rect(view.controls, "PocketWatch", new Vector2(1275 - 836, 470.5f - 810), new Vector2(176, 176));
        var watch = watchRoot.GetComponent<SuitcasePocketWatch>();
        if (watch == null) watch = watchRoot.gameObject.AddComponent<SuitcasePocketWatch>();
        watch.map = view.map;
        var ringRect = Rect(watchRoot, "RemainingTime", Vector2.zero, new Vector2(170, 170));
        if (ringRect.GetComponent<CanvasRenderer>() == null) ringRect.gameObject.AddComponent<CanvasRenderer>();
        var ring = ringRect.GetComponent<SuitcaseCountdownRing>();
        if (ring == null) ring = ringRect.gameObject.AddComponent<SuitcaseCountdownRing>();
        ring.color = new Color(.28f, 1, .13f, .98f);
        ring.thickness = 4.5f;
        ring.raycastTarget = false;
        watch.ring = ring;
        watch.caption = Label(watchRoot, "Caption", new Vector2(0, 35), new Vector2(132, 22), view.playText.font, 14);
        watch.digits = Label(watchRoot, "Time", new Vector2(0, -3), new Vector2(144, 64), view.playText.font, 51);
        watch.digits.fontStyle = FontStyles.Normal;
        watch.Refresh();
        watchRoot.SetAsLastSibling();
        view.countdownText.gameObject.SetActive(false);
        var fullscreen = (RectTransform)view.fullscreenButton.transform;
        fullscreen.anchoredPosition = new Vector2(1510 - 836, 470.5f - 878);
        fullscreen.sizeDelta = new Vector2(148, 39);
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(watch);
        if (view.controls.Find("DepartureBoard") != null) BuildSuitcaseDepartureBoard.Configure(view);
    }

    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
        }
        rect.gameObject.layer = 5;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
    }

    static TMP_Text Label(Transform parent, string name, Vector2 position, Vector2 size, TMP_FontAsset font, float fontSize)
    {
        var rect = Rect(parent, name, position, size);
        var label = rect.GetComponent<TextMeshProUGUI>();
        if (label == null) label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = new Color(1, .80f, .39f);
        label.raycastTarget = false;
        return label;
    }
}
#endif
