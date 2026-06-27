#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT: turn the puck into a polished CHROME / STEEL ball — shiny,
/// reflective, premium, and high-contrast against the wood table.
///
/// Run it:  RealBuca ▸ Make Chrome Ball
///
/// MATERIALS ONLY — no mesh/collider/physics touched. A sphere takes a chrome
/// material perfectly, so this is reliable (no UV/decal issues). The ball keeps
/// rolling as it moves (that's what a steel ball should do).
///
///   • Puck_Yellow (body) → bright brushed-steel metal with a soft glow
///     (a touch of emission keeps it visible against the dark space backdrop,
///      where a pure mirror would just reflect darkness and look dull).
///   • PuckGlowMat        → cool white halo for extra visibility.
///
/// Too dark / too shiny? Tell me and I'll tune Metallic / Smoothness / Emission.
/// </summary>
public static class MakeChromeBall
{
    static readonly Color Steel    = new Color(0.85f, 0.86f, 0.90f, 1f);
    static readonly Color GlowCool = new Color(0.70f, 0.82f, 1.00f, 1f);

    [MenuItem("RealBuca/Make Chrome Ball")]
    static void Apply()
    {
        var body = LoadMat("Puck_Yellow");
        if (body != null)
        {
            ForceLit(body);
            // Clear any leftover texture/alpha-clip from earlier puck attempts.
            if (body.HasProperty("_BaseMap"))    { body.SetTexture("_BaseMap", null); body.SetTextureScale("_BaseMap", Vector2.one); }
            if (body.HasProperty("_MainTex"))      body.SetTexture("_MainTex", null);
            if (body.HasProperty("_EmissionMap"))  body.SetTexture("_EmissionMap", null);
            if (body.HasProperty("_AlphaClip"))    body.SetFloat("_AlphaClip", 0f);
            body.DisableKeyword("_ALPHATEST_ON");
            body.renderQueue = -1;

            SetColor(body, Steel);
            if (body.HasProperty("_Metallic"))   body.SetFloat("_Metallic", 0.9f);
            if (body.HasProperty("_Smoothness")) body.SetFloat("_Smoothness", 0.85f);
            // Subtle cool emission base → the ball stays bright/visible on the
            // dark scene, and PuckDynamics brightens it as it speeds up.
            if (body.HasProperty("_EmissionColor")) body.SetColor("_EmissionColor", new Color(0.06f, 0.08f, 0.13f));
            body.EnableKeyword("_EMISSION");
            body.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(body);
        }

        var glow = LoadMat("PuckGlowMat");
        if (glow != null)
        {
            SetColor(glow, GlowCool);
            if (glow.HasProperty("_TintColor"))     glow.SetColor("_TintColor", GlowCool);
            if (glow.HasProperty("_EmissionColor")) glow.SetColor("_EmissionColor", new Color(0.50f, 0.65f, 1.10f));
            glow.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(glow);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Chrome ball ✓",
            "The puck is now a polished chrome / steel ball with a soft glow.\n\n" +
            "Press Play from MainMenu to see it — it rolls and catches the lights.\n" +
            "If it looks too dark or too shiny, tell me and I'll tune it.", "OK");
    }

    static void ForceLit(Material m)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null && m.shader != lit) m.shader = lit;
    }

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
    }

    static Material LoadMat(string n)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{n}.mat");
        if (m == null) Debug.LogWarning($"[MakeChromeBall] Material not found: {n}");
        return m;
    }
}
#endif
