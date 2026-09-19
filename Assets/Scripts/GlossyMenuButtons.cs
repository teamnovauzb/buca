#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime: restyles the MainMenu buttons (PLAY / LEVELS / QUIT) from flat
/// rectangles into premium GLOSSY rounded buttons — a procedurally-built
/// rounded sprite with a vertical gradient + top gloss highlight, tinted by
/// each button's existing neon color, plus a soft drop shadow for depth.
///
/// Runs automatically when the MainMenu scene loads (no editor tool, no
/// "stop play" needed). Tweak the look via the constants below.
/// </summary>
public static class GlossyMenuButtons
{
    const string MenuSceneName = "MainMenu";
    static readonly string[] ButtonNames = { "PlayButton", "LEVELSButton", "QuitButton" };

    // Look tuning
    const int   SpriteSize   = 128;   // texture resolution
    const int   CornerRadius = 34;    // rounded-corner radius (px)
    const float BottomBright = 0.72f; // darkness at the bottom (glass body)
    const float TopBright    = 0.99f; // brightness at the top (reflection)
    const float CornerScale  = 3.0f;  // pixelsPerUnitMultiplier — higher = smaller corners

    static Sprite _gloss;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Apply(SceneManager.GetActiveScene());
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => Apply(s);

    static void Apply(Scene scene)
    {
        if (scene.name != MenuSceneName) return;
        var sprite = GetGlossSprite();

        foreach (var name in ButtonNames)
        {
            var go = Find(name);
            if (go == null) continue;

            var btn = go.GetComponent<Button>();
            Image img = btn != null ? btn.targetGraphic as Image : go.GetComponent<Image>();
            if (img == null) img = go.GetComponent<Image>();
            if (img == null) continue;

            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = CornerScale;
            // keep img.color — its neon tint shows through the white gloss

            // Soft drop shadow — ONLY add one if the button has no Shadow-derived
            // effect yet. UnityEngine.UI.Outline derives from Shadow, so the neon
            // glow edge baked by "Vibrant Neon Menu" IS a Shadow to GetComponent.
            // Overwriting it would silently turn the neon edge into a black drop
            // shadow, so leave any existing effect (the neon edge) untouched.
            if (go.GetComponent<Shadow>() == null)
            {
                var sh = go.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.55f);
                sh.effectDistance = new Vector2(0f, -7f);
            }
        }
    }

    static Sprite GetGlossSprite()
    {
        if (_gloss != null) return _gloss;

        int s = SpriteSize, r = CornerRadius;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[s * s];

        for (int y = 0; y < s; y++)
        {
            float t = (float)y / (s - 1);                 // 0 bottom → 1 top
            float g = Mathf.SmoothStep(0f, 1f, t);
            float bright = Mathf.Lerp(BottomBright, TopBright, g);
            if (t > 0.86f) bright = Mathf.Min(1f, bright + (t - 0.86f) / 0.14f * 0.10f); // top gloss edge

            for (int x = 0; x < s; x++)
            {
                float a = RoundedAlpha(x, y, s, s, r);
                byte b = (byte)(Mathf.Clamp01(bright) * 255f);
                px[y * s + x] = new Color32(b, b, b, (byte)(Mathf.Clamp01(a) * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        float border = r + 3f;
        _gloss = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        return _gloss;
    }

    /// <summary>Anti-aliased coverage for a rounded rectangle at (x,y).</summary>
    static float RoundedAlpha(int x, int y, int w, int h, int r)
    {
        float dx = Mathf.Max(Mathf.Abs(x - (w - 1) * 0.5f) - (w * 0.5f - r), 0f);
        float dy = Mathf.Max(Mathf.Abs(y - (h - 1) * 0.5f) - (h * 0.5f - r), 0f);
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        return Mathf.Clamp01(r - dist + 0.5f); // 1px anti-aliased edge
    }

    static GameObject Find(string name)
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            var t = FindRec(canvas.transform, name);
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
