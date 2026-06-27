using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Runtime: themes the in-game HUD text to the neon palette so the gameplay
/// screen matches the menu (cyan primary, magenta accents). Runs automatically
/// when the Game scene loads — no editor tool, no "stop play" needed.
///
/// Only RGB is changed; each text's existing alpha is preserved (so faded hints
/// like the drag prompt still fade in/out correctly).
/// </summary>
public static class GameNeonTheme
{
    const string GameScene = "Game";

    static readonly Color Cyan     = new Color(0.18f, 0.88f, 1.00f, 1f); // score / timer
    static readonly Color CyanSoft = new Color(0.64f, 0.90f, 1.00f, 1f); // strokes / hints
    static readonly Color Magenta  = new Color(1.00f, 0.32f, 0.64f, 1f); // bonus / combo pops

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
        if (scene.name != GameScene) return;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // NOTE: the timer + combo texts drive their OWN color every frame
                // (TimerDisplay / FloatingComboText), so recoloring them here would
                // be a silent no-op — we only theme the static HUD labels.
                string n = t.name.ToLowerInvariant();
                if (n.Contains("score"))                        Set(t, Cyan);
                else if (n.Contains("stroke"))                  Set(t, CyanSoft);
                else if (n.Contains("drag") || n.Contains("hint")) Set(t, CyanSoft);
                else if (n.Contains("bonus"))                   Set(t, Magenta);
            }
        }
    }

    static void Set(TMP_Text t, Color c)
    {
        c.a = t.color.a; // keep the text's current alpha (drag hint fades, etc.)
        t.color = c;
    }
}
