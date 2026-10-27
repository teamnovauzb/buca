#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Installs the user's green railway signal without rebuilding the map or footer.</summary>
public static class BuildForestHarborSignalSelector
{
    public const string TexturePath = "Assets/TwilightMap/ForestHarbor/SignalSelector/GreenRailwaySignal.png";
    public const string Output = "output/signal-selector";

    [MenuItem("RealBuca/Forest Harbor/Apply Green Signal Selector")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var view = Object.FindAnyObjectByType<TwilightIllustratedMenu>(FindObjectsInactive.Include);
        if (view == null || !view.forestHarborPresentation || view.stopRects.Length != 50)
            throw new Exception("The existing forest and harbor map is required.");
        var presentation = view.GetComponent<ForestHarborMapPresentation>();
        if (presentation == null) throw new Exception("Map presentation is missing.");
        Directory.CreateDirectory(Output);
        var backup = Output + "/MainMenu.before-signal-selector.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);

        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) throw new Exception("The signal texture is missing.");
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.isReadable = true;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var uv = AlphaBounds(texture);

        var root = Rect(view.artwork, "RailwaySignalSelector", new Vector2(1672, 941));
        var selector = root.GetComponent<ForestHarborSignalSelector>();
        if (selector == null) selector = Undo.AddComponent<ForestHarborSignalSelector>(root.gameObject);
        Undo.RecordObject(selector, "Configure railway signal selector");
        selector.view = view;
        selector.presentation = presentation;

        var rim = Rect(root, "SelectedLevelGoldOutline", new Vector2(46, 46));
        var outline = rim.GetComponent<ForestHarborSelectionOutline>();
        if (outline == null) outline = Undo.AddComponent<ForestHarborSelectionOutline>(rim.gameObject);
        outline.color = Color.white;
        outline.raycastTarget = false;
        selector.outline = outline;

        float aspect = texture.width * uv.width / (texture.height * uv.height);
        var signal = Rect(root, "GreenSignal", new Vector2(72 * aspect, 72));
        var art = signal.GetComponent<UnityEngine.UI.RawImage>();
        if (art == null) art = Undo.AddComponent<UnityEngine.UI.RawImage>(signal.gameObject);
        Undo.RecordObject(art, "Assign green railway signal");
        art.texture = texture;
        art.uvRect = uv;
        art.color = Color.white;
        art.raycastTarget = false;
        selector.signal = signal;
        selector.signalOffset = new Vector2(-3.5f, 63);

        // Selection belongs to the new hollow rim; the existing glow objects still
        // follow the menu state and retain their button/layout references.
        foreach (var glow in view.glows)
        {
            Undo.RecordObject(glow, "Replace green wash with gold selection rim");
            glow.color = Color.clear;
        }
        Undo.RecordObject(presentation, "Connect railway selector");
        presentation.signalSelector = selector;
        root.SetAsLastSibling();
        selector.Refresh();
        importer.isReadable = false;
        importer.SaveAndReimport();
        EditorUtility.SetDirty(selector);
        EditorUtility.SetDirty(presentation);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved the green railway signal and gold selected-level outline for all 50 stops.";
    }

    static RectTransform Rect(Transform parent, string name, Vector2 size)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            rect = (RectTransform)new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).transform;
            rect.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(rect.gameObject, "Add railway signal selector");
        }
        Undo.RecordObject(rect, "Place railway signal selector");
        rect.gameObject.layer = 5;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    static Rect AlphaBounds(Texture2D texture)
    {
        var pixels = texture.GetPixels32();
        int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
            if (pixels[y * texture.width + x].a > 12)
            {
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
        if (minX > maxX) throw new Exception("The signal sprite is empty.");
        minX = Mathf.Max(0, minX - 3); minY = Mathf.Max(0, minY - 3);
        maxX = Mathf.Min(texture.width - 1, maxX + 3); maxY = Mathf.Min(texture.height - 1, maxY + 3);
        return new Rect((float)minX / texture.width, (float)minY / texture.height,
            (float)(maxX - minX + 1) / texture.width, (float)(maxY - minY + 1) / texture.height);
    }
}
#endif
