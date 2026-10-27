#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildWorkshopFlipSelector
{
    const string ArtworkPath = "Assets/TwilightMap/WorkshopRailway/FlipSelectorCentered.png";

    [MenuItem("RealBuca/Workshop Railway/Apply Centered Flip Selector")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required.");
        var presentation = UnityEngine.Object.FindAnyObjectByType<ForestHarborMapPresentation>(FindObjectsInactive.Include);
        if (presentation == null || presentation.gameObject.scene.path != "Assets/Scenes/MainMenu.unity")
            throw new InvalidOperationException("Open the workshop MainMenu scene first.");
        Configure(presentation.view, presentation);
        EditorSceneManager.MarkSceneDirty(presentation.gameObject.scene);
        EditorSceneManager.SaveScene(presentation.gameObject.scene);
        return "Number-only hinged flip selector saved with synchronized card-turn Foley.";
    }

    public static void Configure(TwilightIllustratedMenu view, ForestHarborMapPresentation presentation)
    {
        AssetDatabase.ImportAsset(ArtworkPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(ArtworkPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Flip selector artwork is missing.");
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        var board = Rect(view.controls, "WorkshopFlipSelector", new Vector2(-278, -372.5f), new Vector2(202, 114));
        var image = board.GetComponent<UnityEngine.UI.RawImage>() ?? board.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
        image.raycastTarget = false;
        var heading = Rect(board, "LevelHeading", new Vector2(0, 27), new Vector2(155, 24));
        var label = heading.GetComponent<TextMeshProUGUI>() ?? heading.gameObject.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Roboto-Bold SDF.asset");
        if (font == null) font = presentation.destination.font;
        Style(label, font, 17);
        label.text = string.Empty;
        heading.gameObject.SetActive(false);

        Undo.RecordObject(presentation.destination.transform, "Center selector number");
        var number = presentation.destination.rectTransform;
        number.SetParent(board, false);
        number.anchorMin = number.anchorMax = number.pivot = Vector2.one * .5f;
        number.anchoredPosition = new Vector2(0, 2);
        number.sizeDelta = new Vector2(91, 46);
        number.localScale = Vector3.one;
        number.localRotation = Quaternion.identity;
        Style(presentation.destination, font, 44);
        presentation.destination.alignment = TextAlignmentOptions.MidlineGeoAligned;
        number.gameObject.SetActive(true);
        presentation.progress.gameObject.SetActive(false);
        view.playText.gameObject.SetActive(false);
        board.SetAsLastSibling();
        ConfigureFlip(board, view, presentation.destination, image.texture, font);
        presentation.Refresh();
        EditorUtility.SetDirty(presentation);
    }

    static void ConfigureFlip(RectTransform board, TwilightIllustratedMenu view, TMP_Text number, Texture artwork, TMP_FontAsset font)
    {
        var flip = board.GetComponent<WorkshopFlipDisplay>() ?? board.gameObject.AddComponent<WorkshopFlipDisplay>();
        flip.view = view; flip.number = number;
        var leaves = Rect(board, "TurningLeaves", new Vector2(0, 2), new Vector2(91, 46));
        flip.leaves = leaves.gameObject;
        // Crop the existing navy card surface, retaining the original stationary brass frame and hinges.
        var card = new UnityEngine.Rect(.276f, .317f, .448f, .408f);
        flip.upper = Half(leaves, "UpperNew", true, artwork, card, font);
        flip.lower = Half(leaves, "LowerOld", false, artwork, card, font);
        var shade = Rect(flip.lower.root, "LeafShadow", Vector2.zero, flip.lower.root.sizeDelta);
        shade.anchorMin = shade.anchorMax = new Vector2(.5f, .5f);
        flip.shadow = shade.GetComponent<UnityEngine.UI.Image>() ?? shade.gameObject.AddComponent<UnityEngine.UI.Image>();
        flip.shadow.color = Color.clear; flip.shadow.raycastTarget = false;
        flip.turningUpper = Half(leaves, "TurningUpper", true, artwork, card, font);
        flip.turningLower = Half(leaves, "TurningLower", false, artwork, card, font);
        var seam = Rect(board, "CardHingeLine", new Vector2(0, 2), new Vector2(91, .65f));
        var line = seam.GetComponent<UnityEngine.UI.Image>() ?? seam.gameObject.AddComponent<UnityEngine.UI.Image>();
        line.color = new Color(.005f, .012f, .025f, .84f); line.raycastTarget = false;
        seam.SetAsLastSibling();
        const string soundPath = "Assets/Audio/UI/WorkshopCardFlip.wav";
        AssetDatabase.ImportAsset(soundPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(soundPath) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("Card-turn Foley is missing.");
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = false; importer.SaveAndReimport();
        flip.flipSound = AssetDatabase.LoadAssetAtPath<AudioClip>(soundPath);
        var source = board.GetComponent<AudioSource>();
        if (source == null) source = board.gameObject.AddComponent<AudioSource>();
        flip.flipAudio = source;
        flip.flipAudio.playOnAwake = false; flip.flipAudio.loop = false; flip.flipAudio.spatialBlend = 0;
        flip.SetNumber(view.map.Selected + 1, true);
        EditorUtility.SetDirty(flip);
    }

    static WorkshopFlipDisplay.Half Half(RectTransform parent, string name, bool top, Texture artwork, UnityEngine.Rect card, TMP_FontAsset font)
    {
        var root = Rect(parent, name, Vector2.zero, new Vector2(91, 23));
        root.pivot = new Vector2(.5f, top ? 0 : 1);
        root.localRotation = Quaternion.identity;
        if (root.GetComponent<UnityEngine.UI.RectMask2D>() == null) root.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var faceRect = Rect(root, "Card", Vector2.zero, root.sizeDelta);
        var face = faceRect.GetComponent<UnityEngine.UI.RawImage>() ?? faceRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        face.texture = artwork; face.raycastTarget = false; face.color = Color.white;
        face.uvRect = new UnityEngine.Rect(card.x, card.y + (top ? card.height * .5f : 0), card.width, card.height * .5f);
        var glyphRect = Rect(root, "Number", Vector2.zero, new Vector2(91, 46));
        glyphRect.anchorMin = glyphRect.anchorMax = new Vector2(.5f, top ? 0 : 1);
        var glyph = glyphRect.GetComponent<TextMeshProUGUI>() ?? glyphRect.gameObject.AddComponent<TextMeshProUGUI>();
        Style(glyph, font, 44); glyph.alignment = TextAlignmentOptions.MidlineGeoAligned;
        return new WorkshopFlipDisplay.Half { root = root, face = face, label = glyph };
    }

    static void Style(TMP_Text text, TMP_FontAsset font, float size)
    {
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = new Color32(255, 239, 202, 255);
        text.margin = Vector4.zero;
        text.characterSpacing = 0;
        text.raycastTarget = false;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create flip selector");
            rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
        }
        rect.gameObject.layer = 5;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
    }
}
#endif
