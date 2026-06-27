#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// ONE-SHOT: bake an always-on-top "MENU" button into the Game scene so the
/// player can return to the main menu at ANY time — during play, on the
/// level-complete panel, and on the game-over screen.
///
///   RealBuca ▸ Add Main Menu Button
///
/// Also ENSURES the Game scene has an EventSystem (it currently has none, and the
/// MainMenu's isn't carried over — so in-game UI clicks don't work without it).
///
/// The button lives on its own high-sorting "MenuButtonCanvas", so it renders +
/// stays clickable above every panel. Everything is under that one object plus the
/// EventSystem, so it's easy to find/remove. Re-running is safe (idempotent).
///
/// AFTER running you can DELETE THIS FILE (Assets/Editor/AddMainMenuButton.cs).
/// KEEP ReturnToMenu.cs (it's the component the button calls).
/// </summary>
public static class AddMainMenuButton
{
    const string GameScene = "Assets/Scenes/Game.unity";
    const string CanvasName = "MenuButtonCanvas";

    [MenuItem("RealBuca/Add Main Menu Button")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        // ── Ensure an EventSystem (so UI is clickable in-game) ──
        bool hasES = false;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<EventSystem>(true) != null) { hasES = true; break; }
        if (!hasES)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ── Idempotent: remove any previous MenuButtonCanvas ──
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == CanvasName) { Object.DestroyImmediate(root); break; }

        // ── Borrow a TMP font from the existing HUD so the label matches ──
        TMP_FontAsset font = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = root.GetComponentInChildren<TMP_Text>(true);
            if (t != null && t.font != null) { font = t.font; break; }
        }
        if (font == null) font = TMP_Settings.defaultFontAsset;

        // ── Overlay canvas, high sort order so it sits above all panels ──
        var canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var rtm = canvasGo.AddComponent<ReturnToMenu>();

        // ── The button (top-left corner) ──
        var btnGo = new GameObject("MenuButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)btnGo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);   // top-RIGHT (left side has the HUD stats)
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-34f, -30f);
        rt.sizeDelta = new Vector2(232f, 84f);

        var img = btnGo.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = new Color(0.06f, 0.09f, 0.16f, 0.92f);     // dark navy, slightly see-through

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.16f, 0.55f, 1f, 1f);
        colors.pressedColor = new Color(0.10f, 0.35f, 0.7f, 1f);
        btn.colors = colors;

        // ── Label ──
        var lblGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        lblGo.transform.SetParent(btnGo.transform, false);
        var lrt = (RectTransform)lblGo.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        lbl.text = "‹  MENU";
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontSize = 40f;
        lbl.color = new Color(0.60f, 0.90f, 1f, 1f);           // cyan, matches the theme
        if (font != null) lbl.font = font;

        // ── Bake the onClick → ReturnToMenu.GoToMainMenu (persistent, no runtime wiring) ──
        UnityEventTools.AddPersistentListener(btn.onClick, rtm.GoToMainMenu);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!BucaBatch.Silent) EditorUtility.DisplayDialog("Main Menu button added ✓",
            "A 'MENU' button now sits in the top-left of the Game scene, above every panel —\n" +
            "so the player can return to the main menu during play, on level-complete, and on game-over.\n" +
            (hasES ? "" : "\nAn EventSystem was also added (the scene had none) so in-game UI is now clickable.\n") +
            "\n• Press ▶ Play and click MENU (or press Escape) to test.\n" +
            "• You can now DELETE this file (Assets/Editor/AddMainMenuButton.cs).\n" +
            "• KEEP ReturnToMenu.cs (the button calls it).",
            "OK");
    }
}
#endif
