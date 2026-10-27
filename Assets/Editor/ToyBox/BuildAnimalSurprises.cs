#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class BuildAnimalSurprises
{
    const string Folder = "Assets/Art/AnimalSurprises/";
    const string PrefabPath = "Assets/Resources/AnimalSurpriseMarkers.prefab";

    public static string Apply()
    {
        var sprites = ImportColorfulBoxes();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Style(root.GetComponent<AnimalSurpriseMarkers>(), sprites);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var view = UnityEngine.Object.FindAnyObjectByType<TwilightIllustratedMenu>(FindObjectsInactive.Include);
        if (view != null)
        {
            AnimalSurpriseMarkers.Ensure(view);
            var markers = view.artwork.GetComponentInChildren<AnimalSurpriseMarkers>(true);
            Style(markers, sprites);
            markers.Refresh();
            foreach (var child in markers.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = view.artwork.gameObject.layer;
                PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            }
            foreach (var graphic in markers.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(graphic);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(view.gameObject.scene);
            }
        }
        return "Option B applied: mint rabbit 5, coral fox 10, blue bear 15, lavender panda 20, yellow penguin 25.";
    }

    static Sprite[] ImportColorfulBoxes()
    {
        string path = Folder + "ColorfulBoxesAtlas.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels = texture.GetPixels32();
        var sprites = new Sprite[5];
        for (int i = 0; i < sprites.Length; i++)
        {
            string spritePath = Folder + "ColorfulBox" + i + ".asset";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] != null) continue;
            int left = Mathf.RoundToInt(i * texture.width / 5f);
            int right = Mathf.RoundToInt((i + 1) * texture.width / 5f);
            int minX = right, minY = texture.height, maxX = left, maxY = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = left; x < right; x++)
            {
                if (pixels[y * texture.width + x].a <= 32) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
            if (minX > maxX) throw new InvalidOperationException("Missing colorful box " + i);
            minX = Mathf.Max(left, minX - 2); maxX = Mathf.Min(right - 1, maxX + 2);
            minY = Mathf.Max(0, minY - 2); maxY = Mathf.Min(texture.height - 1, maxY + 2);
            sprites[i] = Sprite.Create(texture, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            sprites[i].name = "ColorfulBox" + i;
            AssetDatabase.CreateAsset(sprites[i], spritePath);
        }
        return sprites;
    }

    static void Style(AnimalSurpriseMarkers markers, Sprite[] sprites)
    {
        if (markers == null || markers.markers.Length != 5)
            throw new InvalidOperationException("Expected the five existing animal reminders.");
        for (int i = 0; i < markers.markers.Length; i++)
        {
            var marker = markers.markers[i];
            var box = marker.GetComponent<UnityEngine.UI.Image>();
            var bubble = marker.Find("SpeechBubble") as RectTransform;
            var label = marker.Find("Message").GetComponent<TMPro.TextMeshProUGUI>();
            if (box == null || bubble == null || label == null)
                throw new InvalidOperationException("Incomplete reminder " + marker.name);
            box.sprite = sprites[i];
            box.color = Color.white;
            box.raycastTarget = false;
            marker.sizeDelta = new Vector2(80, 80 * sprites[i].rect.height / sprites[i].rect.width);
            marker.localScale = Vector3.one;
            marker.localRotation = Quaternion.identity;
            bubble.sizeDelta = new Vector2(125, 68);
            bubble.anchoredPosition = new Vector2(68, i < 2 ? 20 : 38);
            if (i == 3) bubble.anchoredPosition = new Vector2(85, -20);
            label.rectTransform.sizeDelta = new Vector2(96, 36);
            label.rectTransform.anchoredPosition = bubble.anchoredPosition + Vector2.up * 5;
            label.fontSize = 9.5f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8.5f;
            label.fontSizeMax = 9.5f;
            label.margin = new Vector4(2, 0, 2, 0);
            label.text = "Meet a new friend!\n<size=126%>LEVEL " + markers.levels[i] + "</size>";
            label.alignment = TMPro.TextAlignmentOptions.Center;
            ApplyLabelStyle(label);
            label.raycastTarget = false;
            label.overflowMode = TMPro.TextOverflowModes.Overflow;
        }
    }

    public static void ApplyLabelStyle(TMPro.TextMeshProUGUI label)
    {
        // The former font falls back to regular Liberation Sans for these letters.
        // Use that complete alphabet directly and give only the reminder labels a heavier face.
        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new InvalidOperationException("Missing reminder label font.");
        string path = Folder + "SurpriseLabelBlack.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(font.material) { name = "SurpriseLabelBlack" };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_FaceColor", Color.black);
        material.SetFloat("_FaceDilate", .12f);
        material.SetFloat("_OutlineWidth", 0);
        EditorUtility.SetDirty(material);
        label.font = font;
        label.fontSharedMaterial = material;
        label.fontStyle = TMPro.FontStyles.Bold;
        label.fontWeight = TMPro.FontWeight.Bold;
        label.color = Color.black;
        label.UpdateMeshPadding();
    }
}
#endif
