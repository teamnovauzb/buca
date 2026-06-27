#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// ONE-SHOT: set the generated neon synthwave image as the full-screen menu
/// background (Assets/Textures/Menu/NeonBackground.png).
///
/// Run it:  RealBuca ▸ Apply Neon Background   (Play mode must be STOPPED)
///
/// Adds (or refreshes) a stretched "NeonBackground" Image as the FIRST child of
/// MainMenuCanvas, so it sits behind the title + buttons (which stay on top).
/// raycastTarget is off so it never blocks button clicks. Re-run safe.
/// </summary>
public static class ApplyNeonBackground
{
    const string ScenePath  = "Assets/Scenes/MainMenu.unity";
    const string SpritePath = "Assets/Textures/Menu/NeonBackground.png";
    const string CanvasName = "MainMenuCanvas";

    [MenuItem("RealBuca/Apply Neon Background")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "Apply Neon Background edits the MainMenu scene, which can't be done while playing.\n\n" +
                "Click the ■ Stop button, then run it again.", "OK");
            return;
        }

        var sprite = ConfigureSprite(SpritePath);
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Missing background",
                $"Couldn't find {SpritePath}. Make sure the image is in the project.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var canvas = FindInScene(scene, CanvasName);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas not found",
                $"Couldn't find '{CanvasName}' in the MainMenu scene.", "OK");
            return;
        }

        var existing = canvas.transform.Find("NeonBackground");
        GameObject bg = existing != null ? existing.gameObject : null;
        if (bg == null)
        {
            bg = new GameObject("NeonBackground", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(canvas.transform, false);
        }
        bg.transform.SetSiblingIndex(0); // behind title + buttons

        var rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.pivot = new Vector2(0.5f, 0.5f);

        var img = bg.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.type = Image.Type.Simple;
        img.preserveAspect = false; // fill the whole screen
        img.raycastTarget = false;  // don't block button clicks

        EditorUtility.SetDirty(bg);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Neon background ✓",
            "The neon synthwave image is now the menu background (behind the buttons).\n\n" +
            "Press Play from MainMenu to see it. Want a different background? Tell me the vibe " +
            "and I'll generate another and swap it in.", "OK");
    }

    static Sprite ConfigureSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        bool changed = false;
        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
        if (imp.maxTextureSize < 2048) { imp.maxTextureSize = 2048; changed = true; }
        if (changed) imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
