#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ONE-SHOT: reskin the game's TABLE + FLOOR with the real CC0 wood-floor
/// textures in Assets/Textures/WoodFloor007 (ambientCG "Wood Floor 007",
/// CC0 / public-domain — free for commercial use, no attribution).
///
/// Run it:  RealBuca ▸ Apply Realistic Wood Table
///
/// TEXTURES ONLY — no meshes, colliders or physics touched, gameplay is identical:
///   • Imports the 3 maps correctly (Color = sRGB, Normal = NormalMap, AO = linear).
///   • Builds a realistic URP/Lit wood look (base + normal + AO + subtle gloss).
///   • Light wood on every Floor_* material (all 15 levels) + darker stained
///     wood on the table edge/bevel, tiled so it looks like planks.
///
/// Tune TilingX/TilingZ / Smoothness below and re-run to change the look.
/// </summary>
public static class ApplyRealisticTable
{
    const string TexFolder = "Assets/Textures/WoodFloor007";
    const string MatFolder = "Assets/Materials";

    const float TilingX = 3f;
    const float TilingZ = 4f;
    const float Smoothness = 0.35f;

    [MenuItem("RealBuca/Apply Realistic Wood Table")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Apply realistic wood table",
            "Reskins the floor + table edge with the downloaded CC0 wood textures " +
            "(textures only — no gameplay change). Back up first.\n\nContinue?",
            "Apply", "Cancel")) return;

        AssetDatabase.Refresh();

        var color  = LoadConfigured("WoodFloor_Color.jpg",  isNormal: false, sRGB: true);
        var normal = LoadConfigured("WoodFloor_Normal.jpg", isNormal: true,  sRGB: false);
        var ao     = LoadConfigured("WoodFloor_AO.jpg",     isNormal: false, sRGB: false);

        if (color == null)
        {
            EditorUtility.DisplayDialog("Missing textures",
                $"Couldn't find WoodFloor_Color.jpg in {TexFolder}.\n" +
                "Make sure the WoodFloor007 folder is in the project, then try again.", "OK");
            return;
        }

        int floors = 0;
        var lightWood = new Color(1f, 0.96f, 0.90f, 1f);
        var darkWood  = new Color(0.42f, 0.28f, 0.18f, 1f);

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            bool isFloor = mat.name.StartsWith("Floor");
            bool isEdge  = mat.name == "Table_Edge";
            if (!isFloor && !isEdge) continue;

            DressAsWood(mat, color, normal, ao, isEdge ? darkWood : lightWood);
            floors++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Reskinned {floors} material(s) with realistic wood:\n" +
                     "• Floors → light parquet wood\n• Table edge → dark stained wood\n\n" +
                     "Press Play (from MainMenu) to see it. Adjust TilingX/TilingZ or Smoothness " +
                     "in the script and re-run to fine-tune.";
        Debug.Log("[ApplyRealisticTable] " + msg.Replace("\n", " "));
        EditorUtility.DisplayDialog("Realistic table applied ✓", msg, "OK");
    }

    static void DressAsWood(Material mat, Texture2D color, Texture2D normal, Texture2D ao, Color tint)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null && mat.shader != lit) mat.shader = lit;

        var tiling = new Vector2(TilingX, TilingZ);

        if (mat.HasProperty("_BaseMap"))   { mat.SetTexture("_BaseMap", color); mat.SetTextureScale("_BaseMap", tiling); }
        if (mat.HasProperty("_MainTex"))   { mat.SetTexture("_MainTex", color); mat.SetTextureScale("_MainTex", tiling); }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", tint);

        if (normal != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetTextureScale("_BumpMap", tiling);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (ao != null && mat.HasProperty("_OcclusionMap"))
        {
            mat.SetTexture("_OcclusionMap", ao);
            mat.SetTextureScale("_OcclusionMap", tiling);
            if (mat.HasProperty("_OcclusionStrength")) mat.SetFloat("_OcclusionStrength", 1f);
            mat.EnableKeyword("_OCCLUSIONMAP");
        }

        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", Smoothness);

        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
        mat.DisableKeyword("_EMISSION");

        EditorUtility.SetDirty(mat);
    }

    static Texture2D LoadConfigured(string file, bool isNormal, bool sRGB)
    {
        string path = $"{TexFolder}/{file}";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;

        bool changed = false;
        var wantType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (imp.textureType != wantType) { imp.textureType = wantType; changed = true; }
        if (!isNormal && imp.sRGBTexture != sRGB) { imp.sRGBTexture = sRGB; changed = true; }
        if (changed) { imp.SaveAndReimport(); }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif
