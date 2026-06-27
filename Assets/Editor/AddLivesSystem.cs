#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

/// <summary>
/// ONE-SHOT: bake the UI for the lives system into the Game scene and wire it to
/// LevelManager — the logic itself lives in LevelManager (works for ALL levels):
///   • a HUD "LIVES ● ● ●" counter (top-center)
///   • a "LEVEL FAILED" screen with RETRY + EXIT buttons (hidden until lives hit 0)
///
///   RealBuca ▸ Add Lives System
///
/// Both are self-contained overlay canvases (easy to find/delete), and it also makes
/// sure the scene has an EventSystem so the buttons are clickable. AFTER running you
/// can DELETE THIS FILE. KEEP LevelFailedPanel.cs + the LevelManager changes.
/// </summary>
public static class AddLivesSystem
{
    const string GameScene  = "Assets/Scenes/Game.unity";
    const string LivesCanvas = "LivesHUD";
    const string FailCanvas  = "LevelFailedCanvas";

    [MenuItem("RealBuca/Add Lives System")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        LevelManager lm = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            lm = root.GetComponentInChildren<LevelManager>(true);
            if (lm != null) break;
        }
        if (lm == null)
        {
            if (!BucaBatch.Silent) EditorUtility.DisplayDialog("LevelManager not found",
                "Couldn't find LevelManager in the Game scene. Nothing changed.", "OK");
            return;
        }

        var font = FindFont(scene);
        EnsureEventSystem(scene);

        // ── HUD lives = red HEART icons, top-right (replacing the menu button) ──
        RemoveRoot(scene, "MenuButtonCanvas");   // the hearts take the menu button's spot
        RemoveRoot(scene, LivesCanvas);
        var heart = EnsureHeartSprite();
        var livesCanvas = MakeCanvas(LivesCanvas, 998);
        int hearts = Mathf.Max(1, lm.maxLives);
        const float HS = 58f, GAP = 12f;
        var icons = new Image[hearts];
        for (int i = 0; i < hearts; i++)
        {
            var h = MakeImage(livesCanvas.transform, "Heart" + i, lm.lifeFullColor);
            h.sprite = heart; h.type = Image.Type.Simple; h.preserveAspect = true; h.raycastTarget = false;
            var hrt = h.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(1f, 1f);
            // rightmost heart sits where the menu button was; others step left.
            hrt.anchoredPosition = new Vector2(-30f - (hearts - 1 - i) * (HS + GAP), -28f);
            hrt.sizeDelta = new Vector2(HS, HS);
            icons[i] = h;
        }
        lm.lifeIcons = icons;
        lm.livesDisplay = null;

        // ── Level Failed panel ──
        RemoveRoot(scene, FailCanvas);
        var failGo = MakeCanvas(FailCanvas, 1001);
        var cg = failGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        var scrim = MakeImage(failGo.transform, "Scrim", new Color(0f, 0f, 0f, 0.82f));
        Stretch(scrim.rectTransform);

        var card = MakeImage(failGo.transform, "Card", new Color(0.06f, 0.08f, 0.16f, 0.98f));
        card.sprite = UISprite(); card.type = Image.Type.Sliced;
        Place(card.rectTransform, 0f, 760f, 540f);

        var title = MakeText(card.transform, "Title", "LEVEL FAILED", 80f, new Color(1f, 0.30f, 0.36f), font);
        title.fontStyle = FontStyles.Bold; title.alignment = TextAlignmentOptions.Center;
        Place(title.rectTransform, 185f, 700f, 110f);

        var sub = MakeText(card.transform, "Sub", "Out of lives", 34f, new Color(1f, 1f, 1f, 0.7f), font);
        sub.alignment = TextAlignmentOptions.Center;
        Place(sub.rectTransform, 108f, 600f, 50f);

        var retry = MakeButton(card.transform, "RetryButton", "RETRY", -15f, new Vector2(440f, 110f), new Color(0.12f, 0.55f, 0.35f, 1f), font);
        var exit  = MakeButton(card.transform, "ExitButton", "EXIT", -150f, new Vector2(440f, 110f), new Color(0.50f, 0.16f, 0.20f, 1f), font);

        var panel = failGo.AddComponent<LevelFailedPanel>();
        panel.group = cg;
        panel.card = card.rectTransform;
        panel.retryButton = retry;
        panel.exitButton = exit;
        lm.levelFailedPanel = panel;

        EditorUtility.SetDirty(lm);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Lives system added ✓",
            "• HUD now shows LIVES ● ● ● (top-center).\n" +
            "• A 'LEVEL FAILED' screen with RETRY / EXIT appears when lives hit 0.\n" +
            "• Works on ALL levels (logic is in LevelManager).\n\n" +
            "Press ▶ Play and miss 3 shots to test. You can DELETE this file (keep LevelFailedPanel.cs).",
            "OK");
    }

    // ── helpers ──
    static GameObject MakeCanvas(string name, int sort)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sort;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight = 0.5f;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        if (font != null) t.font = font;
        return t;
    }

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static Button MakeButton(Transform parent, string name, string label, float y, Vector2 size, Color bg, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, y, size.x, size.y);
        var img = go.GetComponent<Image>();
        img.sprite = UISprite(); img.type = Image.Type.Sliced; img.color = bg;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(bg.r * 1.3f + 0.1f, bg.g * 1.3f + 0.1f, bg.b * 1.3f + 0.1f, 1f);
        btn.colors = colors;

        var lbl = MakeText(go.transform, "Label", label, 46f, Color.white, font);
        var lrt = lbl.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        return btn;
    }

    static void Place(RectTransform rt, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Sprite UISprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    static TMP_FontAsset FindFont(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = root.GetComponentInChildren<TMP_Text>(true);
            if (t != null && t.font != null) return t.font;
        }
        return TMP_Settings.defaultFontAsset;
    }

    static void EnsureEventSystem(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<EventSystem>(true) != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static void RemoveRoot(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) { Object.DestroyImmediate(root); return; }
    }

    // Procedural white heart sprite (tinted red/faint at runtime by LevelManager).
    static Sprite EnsureHeartSprite()
    {
        const string path = "Assets/Textures/Heart.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int S = 96;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float u = (x + 0.25f + sx * 0.5f) / S;
                        float v = (y + 0.25f + sy * 0.5f) / S;
                        float hx = (u - 0.5f) * 2.6f;          // heart x
                        float hy = -1.3f + v * 2.5f;           // heart y (point near bottom)
                        float a = hx * hx + hy * hy - 1f;
                        if (a * a * a - hx * hx * hy * hy * hy <= 0f) hits++;   // inside the heart curve
                    }
                byte al = (byte)(hits / 4f * 255f);            // 2×2 anti-alias
                px[y * S + x] = new Color32(255, 255, 255, al);
            }
        tex.SetPixels32(px); tex.Apply();

        Directory.CreateDirectory("Assets/Textures");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
