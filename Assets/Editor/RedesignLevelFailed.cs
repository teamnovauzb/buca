#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ONE-SHOT: rebuild the "LEVEL FAILED" screen with a premium design —
///   • dark framed card (cyan edge + drop shadow + red accent bar)
///   • a row of dim "spent" hearts up top (reuses the heart sprite)
///   • a vivid neon-red title that shakes on entry (LevelFailedPanel.FadeIn)
///   • teal RETRY + muted EXIT buttons that stagger-pop in
///
///   RealBuca ▸ Redesign Level Failed Screen
///
/// Re-wires LevelManager.levelFailedPanel + the new FX hooks (titleRect, popInOrder).
/// AFTER running you can DELETE THIS FILE. KEEP LevelFailedPanel.cs.
/// </summary>
public static class RedesignLevelFailed
{
    const string GameScene = "Assets/Scenes/Game.unity";
    const string FailCanvas = "LevelFailedCanvas";
    const string HeartPath  = "Assets/Textures/Heart.png";

    // palette
    static readonly Color CardCol   = new Color(0.05f, 0.07f, 0.13f, 0.99f);
    static readonly Color FrameCol  = new Color(0.18f, 0.88f, 1f, 0.85f);   // cyan board accent
    static readonly Color ShadowCol = new Color(0f, 0f, 0f, 0.45f);
    static readonly Color AccentRed = new Color(1f, 0.22f, 0.30f, 1f);
    static readonly Color HeartDim  = new Color(1f, 0.19f, 0.24f, 0.22f);
    static readonly Color RetryCol  = new Color(0.12f, 0.66f, 0.58f, 1f);   // teal
    static readonly Color ExitCol   = new Color(0.40f, 0.16f, 0.20f, 1f);   // muted red

    [MenuItem("RealBuca/Redesign Level Failed Screen")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
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
            EditorUtility.DisplayDialog("LevelManager not found",
                "Couldn't find LevelManager in the Game scene. Nothing changed.", "OK");
            return;
        }

        var font = FindFont(scene);
        EnsureEventSystem(scene);
        var heart = AssetDatabase.LoadAssetAtPath<Sprite>(HeartPath);

        // ── rebuild canvas ──
        RemoveRoot(scene, FailCanvas);
        var failGo = MakeCanvas(FailCanvas, 1001);
        var cg = failGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;

        // scrim
        var scrim = MakeImage(failGo.transform, "Scrim", new Color(0.02f, 0.02f, 0.05f, 0.86f));
        Stretch(scrim.rectTransform);

        const float CW = 860f, CH = 640f;

        // drop shadow → cyan frame → card  (back to front)
        var shadow = MakeImage(failGo.transform, "Shadow", ShadowCol);
        shadow.sprite = UISprite(); shadow.type = Image.Type.Sliced;
        Center(shadow.rectTransform, 0f, -16f, CW + 44f, CH + 44f);

        var frame = MakeImage(failGo.transform, "Frame", FrameCol);
        frame.sprite = UISprite(); frame.type = Image.Type.Sliced;
        Center(frame.rectTransform, 0f, 0f, CW + 8f, CH + 8f);

        var card = MakeImage(failGo.transform, "Card", CardCol);
        card.sprite = UISprite(); card.type = Image.Type.Sliced;
        Center(card.rectTransform, 0f, 0f, CW, CH);
        var cardT = card.transform;

        // red accent bar near the top
        var bar = MakeImage(cardT, "AccentBar", AccentRed);
        Place(bar.rectTransform, 298f, 760f, 8f);

        // dim "spent" hearts
        RectTransform h0 = null, h1 = null, h2 = null;
        if (heart != null)
        {
            const float HS = 58f, GAP = 22f;
            float[] xs = { -(HS + GAP), 0f, (HS + GAP) };
            var hs = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var h = MakeImage(cardT, "SpentHeart" + i, HeartDim);
                h.sprite = heart; h.preserveAspect = true; h.raycastTarget = false;
                Center(h.rectTransform, xs[i], 212f, HS, HS);
                hs[i] = h.rectTransform;
            }
            h0 = hs[0]; h1 = hs[1]; h2 = hs[2];
        }

        // title (shaken on entry)
        var title = MakeText(cardT, "Title", "LEVEL FAILED", 92f, AccentRed, font);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, 95f, 800f, 130f);

        var sub = MakeText(cardT, "Sub", "Out of lives", 32f, new Color(1f, 1f, 1f, 0.62f), font);
        Place(sub.rectTransform, 18f, 600f, 50f);

        // buttons
        var retry = MakeButton(cardT, "RetryButton", "RETRY", -110f, new Vector2(540f, 112f), RetryCol, font);
        var exit  = MakeButton(cardT, "ExitButton", "EXIT",  -242f, new Vector2(540f, 112f), ExitCol, font);

        // ── wire LevelFailedPanel ──
        var panel = failGo.AddComponent<LevelFailedPanel>();
        panel.group = cg;
        panel.card = card.rectTransform;
        panel.retryButton = retry;
        panel.exitButton = exit;
        panel.titleRect = title.rectTransform;

        var pop = new System.Collections.Generic.List<RectTransform>();
        if (h0 != null) { pop.Add(h0); pop.Add(h1); pop.Add(h2); }
        pop.Add(retry.GetComponent<RectTransform>());
        pop.Add(exit.GetComponent<RectTransform>());
        panel.popInOrder = pop.ToArray();

        lm.levelFailedPanel = panel;

        EditorUtility.SetDirty(lm);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Level Failed redesigned ✓",
            "Rebuilt the LEVEL FAILED screen:\n" +
            "• framed card (cyan edge + shadow + red accent bar)\n" +
            "• dim 'spent' hearts up top\n" +
            "• neon-red title that shakes on entry\n" +
            "• teal RETRY + muted EXIT that stagger-pop in\n\n" +
            "Press ▶ Play and lose all 3 lives to see it. You can DELETE this file (keep LevelFailedPanel.cs).",
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
        colors.highlightedColor = new Color(bg.r * 1.25f + 0.12f, bg.g * 1.25f + 0.12f, bg.b * 1.25f + 0.12f, 1f);
        colors.pressedColor = new Color(bg.r * 0.8f, bg.g * 0.8f, bg.b * 0.8f, 1f);
        btn.colors = colors;

        var lbl = MakeText(go.transform, "Label", label, 50f, Color.white, font);
        lbl.fontStyle = FontStyles.Bold;
        var lrt = lbl.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        return btn;
    }

    // centered at (x,y) with explicit size
    static void Center(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void Place(RectTransform rt, float y, float w, float h) => Center(rt, 0f, y, w, h);

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
}
#endif
