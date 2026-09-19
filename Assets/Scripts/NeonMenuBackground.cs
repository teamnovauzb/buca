#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime: drops the neon synthwave image (Assets/Resources/NeonBackground.png)
/// in as a full-screen background on the MainMenu, BEHIND the title + buttons.
///
/// This runs automatically when the MainMenu scene loads — no editor menu, no
/// "stop play first" needed. It loads the texture from Resources and adds a
/// stretched RawImage as the first child of MainMenuCanvas (so it sits behind
/// everything and never blocks clicks).
/// </summary>
public static class NeonMenuBackground
{
    const string MenuSceneName = "MainMenu";
    const string CanvasName = "MainMenuCanvas";
    const string TextureName = "NeonBackground"; // file in any Resources/ folder

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply(SceneManager.GetActiveScene()); // handle the scene already open
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

    static void Apply(Scene scene)
    {
        if (scene.name != MenuSceneName) return;
        var canvas = FindCanvas();
        if (canvas == null) return;
        if (canvas.Find("NeonBackground") != null) return; // already added

        var tex = Resources.Load<Texture2D>(TextureName);
        if (tex == null)
        {
            Debug.LogWarning($"[NeonMenuBackground] '{TextureName}' not found in a Resources folder.");
            return;
        }

        var go = new GameObject("NeonBackground",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(canvas, false);
        go.transform.SetSiblingIndex(0); // behind title + buttons

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        var raw = go.GetComponent<RawImage>();
        raw.texture = tex;
        raw.color = Color.white;
        raw.raycastTarget = false; // never block button clicks

        // Dark veil ON TOP of the background but BELOW the title + buttons, so
        // the bright synthwave image doesn't wash out the UI. Raise the alpha
        // (DimAmount) for a darker background, lower it for a brighter one.
        const float DimAmount = 0.52f;
        var scrim = new GameObject("NeonScrim",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrim.transform.SetParent(canvas, false);
        scrim.transform.SetSiblingIndex(1); // above bg (0), below title/buttons
        var srt = (RectTransform)scrim.transform;
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        srt.localScale = Vector3.one;
        var veil = scrim.GetComponent<Image>();
        veil.color = new Color(0.02f, 0.02f, 0.06f, DimAmount);
        veil.raycastTarget = false;

        Debug.Log("[NeonMenuBackground] Neon background + dim veil applied to the menu.");
    }

    static Transform FindCanvas()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
            if (c.name == CanvasName) return c.transform;
        return null; // only apply to the real MainMenuCanvas, never an arbitrary one
    }
}
#endif
