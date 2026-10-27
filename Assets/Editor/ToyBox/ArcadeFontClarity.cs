#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Keeps arcade fonts on the shader matching Unity 6 TMP vertex data.</summary>
public static class ArcadeFontClarity
{
    public const string ShaderPath = "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader";
    static readonly string[] FontPaths =
    {
        "Assets/Fonts/CoachDisplay.asset",
        "Assets/Art/PolishedStandings/StandingsFont.asset",
        "Assets/TwilightMap/Artwork/Fonts/CormorantMenu SDF.asset",
        "Assets/TwilightMap/ForestHarbor/BucaRoundedMenu SDF.asset",
        "Assets/TwilightMap/ForestHarbor/BucaDigitalTimer SDF.asset"
    };

    public static void Configure(TMP_FontAsset font)
    {
        if (font == null || font.material == null)
            throw new InvalidOperationException("The arcade font and its material are required.");
        // Shader.Find is ambiguous: the SDK sample includes an older shader with
        // the same name that reads scale from UV1.y instead of Unity 6's UV0.w.
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("The current TextMeshPro mobile SDF shader must compile.");
        var material = font.material;
        if (material.shader == shader && material.GetFloat("_OutlineSoftness") == 0) return;
        material.shader = shader;
        material.SetFloat("_OutlineSoftness", 0);
        EditorUtility.SetDirty(material);
    }

    [MenuItem("RealBuca/Repair Arcade Font Clarity")]
    public static void RepairSavedFonts()
    {
        foreach (var path in FontPaths)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Configure(font);
            AssetDatabase.SaveAssetIfDirty(font.material);
        }
    }
}
#endif
