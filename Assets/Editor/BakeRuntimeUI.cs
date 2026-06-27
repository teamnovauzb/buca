#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

/// <summary>
/// ONE-SHOT: bake the three RUNTIME UI scripts' effects permanently into the
/// saved scenes, so you can DELETE those scripts and the UI stays.
///
///   RealBuca ▸ Bake Runtime UI Into Scenes   (Play mode must be STOPPED)
///
/// Bakes:
///   • NeonMenuBackground → adds NeonBackground + NeonScrim to MainMenuCanvas
///   • GlossyMenuButtons  → glossy sprite (saved as an asset) + shadow on buttons
///   • GameNeonTheme      → neon HUD text colors in the Game scene
///
/// After running, delete these (the UI persists):
///   Assets/Scripts/NeonMenuBackground.cs
///   Assets/Scripts/GlossyMenuButtons.cs
///   Assets/Scripts/GameNeonTheme.cs
///
/// KEEP LevelSelectController's lock overlay — that's gameplay, not a theme, and
/// must stay runtime (it depends on the player's save).
/// </summary>
public static class BakeRuntimeUI
{
    const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    const string GameScene     = "Assets/Scenes/Game.unity";
    const string CanvasName    = "MainMenuCanvas";
    const string NeonTexPath   = "Assets/Resources/NeonBackground.png";
    const string GlossPath     = "Assets/Textures/Menu/GlossyButton.png";
    static readonly string[] ButtonNames = { "PlayButton", "LEVELSButton", "QuitButton" };

    static readonly Color Cyan     = new Color(0.18f, 0.88f, 1.00f, 1f);
    static readonly Color CyanSoft = new Color(0.64f, 0.90f, 1.00f, 1f);
    static readonly Color Magenta  = new Color(1.00f, 0.32f, 0.64f, 1f);

    [MenuItem("RealBuca/Bake Runtime UI Into Scenes")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "Bake Runtime UI edits + saves scenes. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var gloss = BuildGlossSpriteAsset();

        // ── MainMenu: background + veil + glossy buttons ──
        var menu = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        var canvas = FindInScene(menu, CanvasName);
        if (canvas != null)
        {
            BakeNeonBackground(canvas.transform);
            BakeGlossyButtons(canvas.transform, gloss);
        }
        else Debug.LogWarning("[BakeRuntimeUI] MainMenuCanvas not found.");
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        // ── Game: neon HUD text colors ──
        var game = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        int recolored = BakeHudColors(game);
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Baked into scenes ✓",
            $"Neon background + veil, glossy buttons, and {recolored} HUD text colors are now saved INTO the scenes.\n\n" +
            "You can now DELETE these runtime scripts — the UI will stay:\n" +
            "• NeonMenuBackground.cs\n• GlossyMenuButtons.cs\n• GameNeonTheme.cs\n\n" +
            "(Keep LevelSelectController's lock overlay — it must stay runtime.)", "OK");
    }

    // ── Background + veil (mirrors NeonMenuBackground) ──────────────
    static void BakeNeonBackground(Transform canvas)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NeonTexPath);
        if (tex == null) { Debug.LogWarning($"[BakeRuntimeUI] {NeonTexPath} not found."); return; }

        if (canvas.Find("NeonBackground") == null)
        {
            var bg = NewUI("NeonBackground", canvas, typeof(RawImage));
            bg.transform.SetSiblingIndex(0);
            var raw = bg.GetComponent<RawImage>();
            raw.texture = tex; raw.color = Color.white; raw.raycastTarget = false;
        }
        if (canvas.Find("NeonScrim") == null)
        {
            var scrim = NewUI("NeonScrim", canvas, typeof(Image));
            scrim.transform.SetSiblingIndex(1);
            var img = scrim.GetComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.06f, 0.52f); img.raycastTarget = false;
        }
    }

    // ── Glossy buttons (mirrors GlossyMenuButtons) ──────────────────
    static void BakeGlossyButtons(Transform canvas, Sprite gloss)
    {
        foreach (var name in ButtonNames)
        {
            var t = FindRec(canvas, name);
            if (t == null) continue;
            var btn = t.GetComponent<Button>();
            Image img = btn != null ? btn.targetGraphic as Image : t.GetComponent<Image>();
            if (img == null) img = t.GetComponent<Image>();
            if (img != null && gloss != null)
            {
                img.sprite = gloss; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 3f;
                EditorUtility.SetDirty(img);
            }
            // Drop shadow ONLY if no Shadow/Outline exists (don't clobber the neon edge).
            if (t.GetComponent<Shadow>() == null)
            {
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.55f);
                sh.effectDistance = new Vector2(0f, -7f);
            }
        }
    }

    // ── HUD colors (mirrors GameNeonTheme) ──────────────────────────
    static int BakeHudColors(Scene scene)
    {
        int n = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string nm = t.name.ToLowerInvariant();
                Color? c = null;
                if (nm.Contains("score")) c = Cyan;
                else if (nm.Contains("stroke")) c = CyanSoft;
                else if (nm.Contains("drag") || nm.Contains("hint")) c = CyanSoft;
                else if (nm.Contains("bonus")) c = Magenta;
                if (c.HasValue) { var col = c.Value; col.a = t.color.a; t.color = col; EditorUtility.SetDirty(t); n++; }
            }
        return n;
    }

    // ── Glossy sprite → saved as a real asset (9-sliced) ────────────
    static Sprite BuildGlossSpriteAsset()
    {
        const int S = 128, R = 34, Border = 37;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            float ty = (float)y / (S - 1);
            float g = Mathf.SmoothStep(0f, 1f, ty);
            float bright = Mathf.Lerp(0.72f, 0.99f, g);
            if (ty > 0.86f) bright = Mathf.Min(1f, bright + (ty - 0.86f) / 0.14f * 0.10f);
            for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - (S - 1) * 0.5f) - (S * 0.5f - R), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - (S - 1) * 0.5f) - (S * 0.5f - R), 0f);
                float a = Mathf.Clamp01(R - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                byte b = (byte)(Mathf.Clamp01(bright) * 255f);
                px[y * S + x] = new Color32(b, b, b, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px); tex.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(GlossPath));
        File.WriteAllBytes(GlossPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(GlossPath, ImportAssetOptions.ForceUpdate);

        var imp = (TextureImporter)AssetImporter.GetAtPath(GlossPath);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spriteBorder = new Vector4(Border, Border, Border, Border);
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(GlossPath);
    }

    // ── helpers ─────────────────────────────────────────────────────
    static GameObject NewUI(string name, Transform parent, System.Type graphic)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), graphic);
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        return go;
    }

    static GameObject FindInScene(Scene s, string name)
    {
        foreach (var r in s.GetRootGameObjects())
        {
            var t = FindRec(r.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindRec(Transform p, string n)
    {
        if (p.name == n) return p;
        foreach (Transform c in p) { var r = FindRec(c, n); if (r != null) return r; }
        return null;
    }
}
#endif
