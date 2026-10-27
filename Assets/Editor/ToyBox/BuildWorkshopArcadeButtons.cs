#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildWorkshopArcadeButtons
{
    const string Folder = "Assets/TwilightMap/WorkshopRailway/";

    [MenuItem("RealBuca/Workshop Railway/Apply Classic Arcade Buttons")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required.");
        var p = UnityEngine.Object.FindAnyObjectByType<ForestHarborMapPresentation>(FindObjectsInactive.Include);
        if (p == null || p.gameObject.scene.path != "Assets/Scenes/MainMenu.unity") throw new InvalidOperationException("Open MainMenu first.");
        Configure(p.view, p);
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        EditorSceneManager.SaveScene(p.gameObject.scene);
        return "Classic Arcade buttons installed with live press, hover, disabled and keyboard feedback.";
    }

    public static void Configure(TwilightIllustratedMenu view, ForestHarborMapPresentation p)
    {
        var atlas = Import("ClassicArcadeButtons.png", true);
        var clean = Import("CleanButtonDeck.png", false);
        // Only the footer region is replaced; all existing scenery retains its original texture.
        var patch = Rect(view.artwork, "CleanArcadeButtonDeck", new Vector2(-204, -360), new Vector2(616, 221));
        var art = patch.GetComponent<UnityEngine.UI.RawImage>() ?? patch.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        art.texture = clean; art.raycastTarget = false;
        art.uvRect = new Rect(324f / 1672, 0, 616f / 1672, 221f / 941);
        patch.SetAsFirstSibling();
        // Trim each frame's transparent padding so the generated atlas cannot shift a button.
        var bounds = CellBounds(atlas);
        var buttonSize = new Vector2(96, 100);
        // Equal 16-unit edge gaps around the 202-unit level board and between buttons.
        ConfigureButton(p.previous, atlas, bounds, -1, new Vector2(-443, -374.5f), buttonSize);
        ConfigureButton(p.next, atlas, bounds, 1, new Vector2(-113, -374.5f), buttonSize);
        ConfigureButton(view.playButton, atlas, bounds, 0, new Vector2(-1, -374.5f), buttonSize);
        ArcadeIdentity(p.previous, 3, new Color(.025f, .85f, .16f), p.destination.font);
        ArcadeIdentity(p.next, 5, new Color(.035f, .26f, 1f), p.destination.font);
        ArcadeIdentity(view.playButton, 1, new Color(.055f, .055f, .055f), p.destination.font);
        p.Refresh(); view.Refresh();
        foreach (var button in new[] { p.previous, p.next, view.playButton })
            button.GetComponent<WorkshopArcadeButton>().RefreshVisual(false);
    }

    static void ConfigureButton(UnityEngine.UI.Button button, Texture2D atlas, Rect[] crops, int direction, Vector2 position, Vector2 size)
    {
        Undo.RecordObject(button.transform, "Replace painted arcade buttons");
        var root = (RectTransform)button.transform;
        root.anchoredPosition = position; root.sizeDelta = size;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        button.image.color = Color.clear; button.image.raycastTarget = true;
        var rect = Rect(root, "ClassicArcadeFace", Vector2.zero, size);
        var image = rect.GetComponent<UnityEngine.UI.RawImage>() ?? rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture = atlas; image.raycastTarget = false;
        var behavior = button.GetComponent<WorkshopArcadeButton>() ?? button.gameObject.AddComponent<WorkshopArcadeButton>();
        behavior.face = image; behavior.direction = direction;
        int row = direction == 0 ? 0 : 2;
        behavior.raisedUV = crops[row];
        behavior.pressedUV = crops[row + 1];
        if (direction < 0)
        {
            var raised = behavior.raisedUV; raised.x += raised.width; raised.width *= -1; behavior.raisedUV = raised;
            var pressed = behavior.pressedUV; pressed.x += pressed.width; pressed.width *= -1; behavior.pressedUV = pressed;
        }
        behavior.RefreshVisual(false);
        EditorUtility.SetDirty(behavior);
    }

    static void ArcadeIdentity(UnityEngine.UI.Button button, int number, Color color, TMPro.TMP_FontAsset font)
    {
        string path=Folder+"ArcadeCap"+number+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null)
        {
            var shader=Shader.Find("Buca/UI/ArcadeCapColor");
            if(shader==null)throw new InvalidOperationException("Missing arcade cap shader.");
            material=new Material(shader);AssetDatabase.CreateAsset(material,path);
        }
        material.SetColor("_CapColor",color);EditorUtility.SetDirty(material);
        button.GetComponent<WorkshopArcadeButton>().face.material=material;
        var badge=Rect(button.transform,"ArcadeNumber",new Vector2(34,36),new Vector2(24,24));
        var rim=badge.GetComponent<UnityEngine.UI.Image>()??badge.gameObject.AddComponent<UnityEngine.UI.Image>();
        rim.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luxodd.Game/Example/Sprites/white_circle.png");
        rim.color=new Color(1,.86f,.52f);rim.raycastTarget=false;
        var inner=Rect(badge,"Face",Vector2.zero,new Vector2(20,20));
        var face=inner.GetComponent<UnityEngine.UI.Image>()??inner.gameObject.AddComponent<UnityEngine.UI.Image>();
        face.sprite=rim.sprite;face.color=color;face.raycastTarget=false;
        var digitRect=Rect(inner,"Number",Vector2.zero,new Vector2(20,22));
        var digit=digitRect.GetComponent<TMPro.TextMeshProUGUI>()??digitRect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        digit.font=font;digit.text=number.ToString();digit.fontSize=17;digit.alignment=TMPro.TextAlignmentOptions.Center;
        digit.color=Color.white;digit.raycastTarget=false;digit.fontStyle=TMPro.FontStyles.Bold;
        badge.SetAsLastSibling();
    }

    static Rect[] CellBounds(Texture2D texture)
    {
        int w = texture.width / 2, h = texture.height / 2;
        var bounds = new Rect[4];
        var pixels = texture.GetPixels32();
        for (int cell = 0; cell < 4; cell++)
        {
            int ox = (cell % 2) * w, oy = (cell / 2) * h;
            int left = w, bottom = h, right = 0, top = 0;
            for (int y = oy; y < oy + h; y++)
                for (int x = ox; x < ox + w; x++)
                    if (pixels[y * texture.width + x].a > 32)
                    {
                        int cx = x % w, cy = y % h;
                        left = Mathf.Min(left, cx); right = Mathf.Max(right, cx);
                        bottom = Mathf.Min(bottom, cy); top = Mathf.Max(top, cy);
                    }
            left = Mathf.Max(0, left - 3); bottom = Mathf.Max(0, bottom - 3);
            right = Mathf.Min(w - 1, right + 3); top = Mathf.Min(h - 1, top + 3);
            bounds[cell] = new Rect((float)(ox + left) / texture.width, (float)(oy + bottom) / texture.height,
                (float)(right - left + 1) / texture.width, (float)(top - bottom + 1) / texture.height);
        }
        return bounds;
    }

    static Texture2D Import(string name, bool readable)
    {
        string path = Folder + name;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing artwork: " + path);
        importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.isReadable = readable; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rt = parent.Find(name) as RectTransform;
        if (rt == null)
        {
            var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "Create arcade button art");
            rt = (RectTransform)go.transform; rt.SetParent(parent, false);
        }
        rt.gameObject.layer = 5;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * .5f;
        rt.anchoredPosition = position; rt.sizeDelta = size; rt.localScale = Vector3.one;
        return rt;
    }
}
#endif
