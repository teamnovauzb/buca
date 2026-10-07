using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Editor-only surface maps, studio reflection and a dedicated menu renderer.</summary>
internal static class ToyBoxSurfaceBaker
{
    const string Folder = "Assets/ToyBoxMenu/Materials/";

    internal static Texture2D WoodGrain(bool normal, bool painted = false)
    {
        const int size = 512;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, normal)
        { name = normal ? "WoodMicroNormal" : painted ? "PaintGrain" : "BirchGrain", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float u = x / (float)size, v = y / (float)size;
            float height = Grain(u, v);
            if (normal)
            {
                float dx = (Grain(u + 1f / size, v) - Grain(u - 1f / size, v)) * .8f;
                float dy = (Grain(u, v + 1f / size) - Grain(u, v - 1f / size)) * .8f;
                Vector3 n = new Vector3(-dx, -dy, 1).normalized;
                pixels[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
            }
            else
            {
                float value = painted ? .94f + height * .055f : .93f + height * .055f;
                pixels[y * size + x] = new Color(value, value * .985f, value * .945f, 1);
            }
        }
        texture.SetPixels(pixels); texture.Apply(true, false);
        return ToyBoxGeometry.Save(texture, Folder + texture.name + ".asset");
    }

    static float Grain(float u, float v)
    {
        float warp = Mathf.PerlinNoise(u * 2.5f, v * 3.5f) * 3;
        float longFibres = Mathf.PerlinNoise(u * 6, v * 145 + warp);
        float pores = Mathf.PerlinNoise(u * 165, v * 190);
        float waves = Mathf.Sin(v * 90 + Mathf.Sin(u * 6) * 3.5f + warp * 6) * .5f + .5f;
        return longFibres * .45f + waves * .35f + pores * .20f;
    }

    internal static Cubemap StudioReflection()
    {
        const int size = 128;
        var cube = new Cubemap(size, TextureFormat.RGBAHalf, true) { name = "BakedStudioReflection" };
        Vector3 key = new Vector3(-.55f, .9f, -.45f).normalized;
        Vector3 rim = new Vector3(.8f, .4f, .7f).normalized;
        for (int face = 0; face < 6; face++)
        {
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1;
                Vector3 d = face == 0 ? new Vector3(1, -v, -u) : face == 1 ? new Vector3(-1, -v, u) :
                    face == 2 ? new Vector3(u, 1, v) : face == 3 ? new Vector3(u, -1, -v) :
                    face == 4 ? new Vector3(u, -v, 1) : new Vector3(-u, -v, -1);
                d.Normalize();
                Color room = Color.Lerp(new Color(.11f, .085f, .055f), new Color(.53f, .49f, .42f), d.y * .5f + .5f);
                float window = Mathf.Pow(Mathf.Max(0, Vector3.Dot(d, key)), 24) * 3.5f;
                float bounce = Mathf.Pow(Mathf.Max(0, Vector3.Dot(d, rim)), 18) * 1.8f;
                pixels[y * size + x] = room + new Color(1, .94f, .83f) * window + new Color(.75f, .86f, 1) * bounce;
            }
            cube.SetPixels(pixels, (CubemapFace)face);
        }
        cube.Apply(true, false);
        return ToyBoxGeometry.Save(cube, Folder + cube.name + ".asset");
    }

    static UniversalRendererData CreateRenderer(string path, string sourcePath)
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if (renderer == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(sourcePath);
            renderer = Object.Instantiate(source); renderer.name = System.IO.Path.GetFileNameWithoutExtension(path);
            renderer.rendererFeatures.Clear();
            AssetDatabase.CreateAsset(renderer, path);
        }
        var occlusion = renderer.rendererFeatures.Find(feature => feature is ScreenSpaceAmbientOcclusion) as ScreenSpaceAmbientOcclusion;
        if (occlusion == null)
        {
            occlusion = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            occlusion.name = "ToyBoxContactShadows";
            AssetDatabase.AddObjectToAsset(occlusion, renderer);
            renderer.rendererFeatures.Add(occlusion);
        }
        var settings = new SerializedObject(occlusion);
        settings.FindProperty("m_Settings.Intensity").floatValue = 1.05f;
        settings.FindProperty("m_Settings.Radius").floatValue = .28f;
        settings.FindProperty("m_Settings.DirectLightingStrength").floatValue = .45f;
        settings.FindProperty("m_Settings.Samples").enumValueIndex = 0;
        settings.FindProperty("m_Settings.Downsample").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        occlusion.SetActive(true); renderer.SetDirty(); EditorUtility.SetDirty(renderer);
        return renderer;
    }

    internal static int EnsureMenuRenderer()
    {
        var renderer = CreateRenderer("Assets/ToyBoxMenu/ToyBoxRenderer.asset", "Assets/Settings/PC_Renderer.asset");
        var mobileRenderer = CreateRenderer("Assets/ToyBoxMenu/ToyBoxMobileRenderer.asset", "Assets/Settings/Mobile_Renderer.asset");

        // Each quality tier retains its rendering mode at the same menu index.
        // Gameplay keeps its original default renderer and feature settings.
        var pipelines = new List<SerializedObject>();
        int index = 1;
        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" }))
        {
            var pipeline = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid)));
            var list = pipeline.FindProperty("m_RendererDataList");
            bool found = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer || list.GetArrayElementAtIndex(i).objectReferenceValue == mobileRenderer)
                { index = Mathf.Max(index, i); found = true; }
            if (!found) index = Mathf.Max(index, list.arraySize);
            pipelines.Add(pipeline);
        }
        foreach (var pipeline in pipelines)
        {
            var list = pipeline.FindProperty("m_RendererDataList");
            int oldSize = list.arraySize;
            if (oldSize <= index) list.arraySize = index + 1;
            for (int i = oldSize; i < index; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = list.GetArrayElementAtIndex(0).objectReferenceValue;
            var defaultRenderer = list.GetArrayElementAtIndex(pipeline.FindProperty("m_DefaultRendererIndex").intValue).objectReferenceValue as UniversalRendererData;
            list.GetArrayElementAtIndex(index).objectReferenceValue = defaultRenderer != null && defaultRenderer.renderingMode == RenderingMode.Forward ? mobileRenderer : renderer;
            pipeline.ApplyModifiedPropertiesWithoutUndo();
        }
        return index;
    }
}
