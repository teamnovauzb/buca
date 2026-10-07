#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes the approved image into a reusable overlay without editing a scene.</summary>
public static class HoneyComparisonTutorialAuthoring
{
    const string ImagePath = "Assets/Tutorials/Honey/ComparisonAnimationPlate.png";
    const string PrefabPath = "Assets/Resources/Tutorials/HoneyComparisonTutorial.prefab";

    [InitializeOnLoadMethod]
    static void ScheduleInstall() { EditorApplication.delayCall += EnsureInstalled; }

    static void EnsureInstalled()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += EnsureInstalled; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath) != null) Build();
    }

    [MenuItem("RealBuca/Toy Box 3D/Install Approved Honey Tutorial Picture")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before baking.");
        var importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Approved tutorial picture is missing.");
        importer.textureType = TextureImporterType.Default;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Tutorials"))
            AssetDatabase.CreateFolder("Assets/Resources", "Tutorials");
        var root = new GameObject("HoneyComparisonTutorial", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(HoneyComparisonTutorial));
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1536, 1024);
            scaler.matchWidthOrHeight = .5f;
            var backdrop = Child(root.transform, "Letterbox");
            Stretch(backdrop, Vector2.zero, Vector2.one);
            backdrop.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.black;
            var picture = Child(root.transform, "UnchangedApprovedPicture");
            picture.anchorMin = picture.anchorMax = new Vector2(.5f, .5f);
            picture.sizeDelta = new Vector2(texture.width, texture.height);
            var image = picture.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            var fitter = picture.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)texture.width / texture.height;
            var hit = Child(picture, "PrintedPlayButtonHitArea");
            // Exact location of the printed mint button in the 1536 x 1024 artwork.
            Stretch(hit, new Vector2(424f / 1536, 48f / 1024), new Vector2(1114f / 1536, 177f / 1024));
            var target = hit.gameObject.AddComponent<UnityEngine.UI.Image>();
            target.color = Color.clear;
            var button = hit.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = target;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var view = root.GetComponent<HoneyComparisonTutorial>();
            view.artwork = image;
            view.playButton = button;
            view.weakPuck = Child(picture, "AnimatedWeakPuck");
            view.weakPuck.gameObject.AddComponent<HoneyDemoPuckGraphic>().raycastTarget = false;
            view.strongPuck = Child(picture, "AnimatedStrongPuck");
            view.strongPuck.gameObject.AddComponent<HoneyDemoPuckGraphic>().raycastTarget = false;
            view.weakPower = Power(picture, "WeakCharge", 328, 631, new Color(1,.35f,.25f));
            view.strongPower = Power(picture, "StrongCharge", 900, 1208, new Color(.22f,.86f,.64f));
            view.SampleDemonstration(0);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("HONEY_COMPARISON_INSTALLED: original artwork, aspect-fit layout, transparent Play hit area.");
    }

    static UnityEngine.UI.Image Power(Transform parent, string name, float left, float right, Color color)
    {
        var rect = Child(parent, name);
        Stretch(rect, new Vector2(left/1536f, 289f/1024f), new Vector2(right/1536f, 315f/1024f));
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = UnityEngine.UI.Image.Type.Filled;
        image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        image.fillOrigin = 0; image.fillAmount = 0; image.color = color; image.raycastTarget = false;
        return image;
    }

    static RectTransform Child(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    [MenuItem("RealBuca/Toy Box 3D/Validate Approved Honey Tutorial Picture")]
    public static void Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new Exception("Tutorial prefab missing.");
        var view = prefab.GetComponent<HoneyComparisonTutorial>();
        if (view == null || view.artwork == null || view.playButton == null)
            throw new Exception("Tutorial references are incomplete.");
        if (AssetDatabase.GetAssetPath(view.artwork.texture) != ImagePath)
            throw new Exception("Tutorial must use the approved original image.");
        var aspect = view.artwork.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        if (aspect.aspectMode != UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent ||
            Mathf.Abs(aspect.aspectRatio - 1.5f) > .001f)
            throw new Exception("Image must fit without cropping or stretching.");
        if (view.playButton.targetGraphic.color.a != 0 ||
            view.playButton.transition != UnityEngine.UI.Selectable.Transition.None)
            throw new Exception("Button must not paint over the approved artwork.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");
        int eventSystems = 0;
        if (scene.isLoaded)
            foreach (var root in scene.GetRootGameObjects())
                eventSystems += root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true).Length;
        if (scene.isLoaded && eventSystems != 1) throw new Exception("Expected one Game EventSystem, got " + eventSystems);
        if (view.weakPuck == null || view.strongPuck == null || view.weakPower.sprite == null || view.strongPower.sprite == null)
            throw new Exception("Animated demonstration references missing.");
        var copy = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var demo = copy.GetComponent<HoneyComparisonTutorial>();
            demo.SampleDemonstration(0);
            Vector2 start = demo.weakPuck.anchorMin;
            demo.SampleDemonstration(4);
            Vector2 stopped = demo.weakPuck.anchorMin;
            if (Vector2.Distance(start,stopped)<.03f) throw new Exception("Weak puck did not move.");
            demo.SampleDemonstration(6);
            if (Vector2.Distance(stopped,demo.weakPuck.anchorMin)>.001f || demo.strongPuck.localScale.sqrMagnitude>.001f)
                throw new Exception("Expected weak shot stopped and strong shot sunk.");
            demo.SampleDemonstration(8);
            if (Vector2.Distance(start,demo.weakPuck.anchorMin)>.001f) throw new Exception("Loop did not reset.");
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
        // Check the fit and the printed button alignment at landscape, original and portrait sizes.
        foreach (var size in new[] { new Vector2(1920,1080), new Vector2(1536,1024), new Vector2(1080,1920) })
        {
            float scale = Mathf.Min(size.x / 1536, size.y / 1024);
            Vector2 fit = new Vector2(1536,1024) * scale;
            if (fit.x > size.x + .01f || fit.y > size.y + .01f || Mathf.Abs(fit.x / fit.y - 1.5f) > .001f)
                throw new Exception("Aspect-fit check failed.");
        }
        Debug.Log("HONEY_COMPARISON_VALIDATED: artwork identity, uncropped 3:2 fitting at landscape/portrait, transparent button, references, Game EventSystem.");
    }
}
#endif
