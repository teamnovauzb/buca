#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Buca.Prototypes;
using Object = UnityEngine.Object;

public static class BucaLightingPrototypeBuilder
{
    public const string Root = "Assets/Prototypes/WebLighting";
    public const string ScenePath = Root + "/LightingComparison.unity";
    public const string Output = "Builds/BucaLightingPrototype";

    [MenuItem("RealBuca/Lighting Prototype/Prepare isolated comparison")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before baking the prototype.");
        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (original.isDirty) throw new InvalidOperationException("Save the active scene before creating the isolated prototype.");
        Directory.CreateDirectory(Root);
        BucaLightingGraphBuilder.Create(Root + "/PuckStylized.shadergraph");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/PuckStylized.shadergraph");
        var source = PrefabUtility.LoadPrefabContents("Assets/ToyBoxMenu/Prefabs/FullSizeSkinPreview.prefab");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        try
        {
            var stage = new GameObject("SavedBoardContext").transform;
            var board = source.transform.Find("ActualBoardPreview");
            foreach (var r in board.GetComponentsInChildren<MeshRenderer>(true))
                CopyRenderer(r, stage, r.transform.position, r.sharedMaterials);
            var binding = source.transform.Find("ActualPlayerPuckPreview").GetComponent<BucaSkinBinding>();
            var a = new GameObject("CurrentWebPuck");
            var b = new GameObject("StylizedWebPuck");
            var aPuck = new GameObject("PuckVisual").transform; aPuck.SetParent(a.transform, false);
            var bPuck = new GameObject("PuckVisual").transform; bPuck.SetParent(b.transform, false);
            aPuck.localPosition = bPuck.localPosition = new Vector3(0,.11f,-4.1f);
            foreach (var surface in binding.surfaces)
            {
                var material = surface.variants[4]; // The gold Ocean Club puck shown in the game screenshots.
                var copy = new Material(shader) { name = material.name + "_Stylized" };
                copy.SetColor("_BaseColor", material.GetColor("_BaseColor"));
                copy.SetTexture("_BaseMap", material.GetTexture("_BaseMap"));
                copy.SetFloat("_ShadeStrength", .30f);
                copy.SetFloat("_HighlightStrength", material.name.Contains("Navy") ? .035f : .20f);
                copy.SetFloat("_RimStrength", .065f);
                copy = Save(copy, Root + "/" + copy.name + ".mat");
                Vector3 local = surface.target.transform.position - binding.transform.position;
                var ra = CopyRenderer((MeshRenderer)surface.target, aPuck, local, new[] { material });
                var rb = CopyRenderer((MeshRenderer)surface.target, bPuck, local, new[] { copy });
                if (surface.target.GetComponent<MeshFilter>() == binding.motif)
                {
                    ra.GetComponent<MeshFilter>().sharedMesh = binding.motifMeshes[4];
                    rb.GetComponent<MeshFilter>().sharedMesh = binding.motifMeshes[4];
                }
            }

            // Preserve the existing baseline disc's actual geometry, size and material.
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "ExistingYellowShadow"; disc.transform.SetParent(a.transform, false);
            disc.transform.localPosition = new Vector3(0,.015f,-4.1f);
            disc.transform.localScale = new Vector3(.6f * 1.9f,.004f,.6f * 1.9f);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PuckShadow.mat");
            NoShadows(disc.GetComponent<Renderer>());

            var shadow = new GameObject("BakedSoftContactShadow", typeof(MeshFilter), typeof(MeshRenderer));
            shadow.transform.SetParent(b.transform, false);
            shadow.transform.localPosition = new Vector3(.035f,.007f,-4.065f);
            shadow.GetComponent<MeshFilter>().sharedMesh = Save(ShadowQuad(), Root + "/ContactShadowQuad.asset");
            var blob = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "SoftContactShadow" };
            blob.SetFloat("_Surface", 1); blob.SetFloat("_Blend", 0); blob.SetFloat("_ZWrite", 0);
            blob.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); blob.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            blob.SetFloat("_Cull", (float)CullMode.Off); blob.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            blob.SetOverrideTag("RenderType", "Transparent"); blob.renderQueue = 3000;
            blob.SetColor("_BaseColor", new Color(.13f,.075f,.035f,.65f));
            blob.SetTexture("_BaseMap", CreateBlob());
            blob.SetShaderPassEnabled("ShadowCaster", false); blob.SetShaderPassEnabled("DepthOnly", false);
            shadow.GetComponent<MeshRenderer>().sharedMaterial = Save(blob, Root + "/SoftContactShadow.mat");
            NoShadows(shadow.GetComponent<Renderer>());

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.52f,.57f,.63f);
            RenderSettings.ambientEquatorColor = new Color(.23f,.20f,.17f);
            RenderSettings.ambientGroundColor = new Color(.10f,.08f,.06f);
            RenderSettings.fog = false;
            var key = Light("WarmWindowLight", new Vector3(42,312,0), new Color(1,.96f,.89f), 1.35f);
            RenderSettings.sun = key;
            Light("SoftRoomFill", new Vector3(40,145,0), new Color(.90f,.94f,1), .22f);
            var camera = new GameObject("ComparisonCamera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = .05f; camera.farClipPlane = 70; camera.fieldOfView = 35;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.06f,.12f,.13f);
            var data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(1); data.renderPostProcessing = false; data.renderShadows = false;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var comparison = new GameObject("LightingComparison").AddComponent<BucaLightingComparison>();
            comparison.current = a; comparison.prototype = b;
            comparison.currentPuck = aPuck; comparison.prototypePuck = bPuck;
            comparison.comparisonCamera = camera;
            comparison.webPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Web_RPAsset.asset");
            comparison.prototypePipeline = CreatePrototypePipeline();
            comparison.closePosition = new Vector3(0,1.3f,-6.3f);
            comparison.closeTarget = new Vector3(0,.10f,-4.1f);
            comparison.widePosition = new Vector3(0,10f,-18.81f);
            comparison.wideTarget = new Vector3(0,0,0);
            comparison.SetView("close"); comparison.SetMode("current");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("BUCA_LIGHTING_PROTOTYPE_SAVED=" + ScenePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
            EditorSceneManager.CloseScene(scene, true);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
        }
    }

    static T Save<T>(T value, string path) where T : Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(value, path); return value; }
        EditorUtility.CopySerialized(value, existing); Object.DestroyImmediate(value); EditorUtility.SetDirty(existing); return existing;
    }
    static UniversalRenderPipelineAsset CreatePrototypePipeline()
    {
        string rendererPath = Root + "/PrototypeRenderer.asset", pipelinePath = Root + "/PrototypeWebPipeline.asset";
        if (!AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath))
            AssetDatabase.CopyAsset("Assets/ToyBoxMenu/ToyBoxMobileRenderer.asset", rendererPath);
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
        foreach (var feature in renderer.rendererFeatures)
            if (feature != null && feature.GetType().Name == "ScreenSpaceAmbientOcclusion") { feature.SetActive(false); EditorUtility.SetDirty(feature); }
        EditorUtility.SetDirty(renderer);
        if (!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath))
            AssetDatabase.CopyAsset("Assets/Settings/Web_RPAsset.asset", pipelinePath);
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        var settings = new SerializedObject(pipeline);
        var renderers = settings.FindProperty("m_RendererDataList");
        for (int i=0;i<renderers.arraySize;i++) renderers.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
        settings.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
        return pipeline;
    }
    static void NoShadows(Renderer r) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
    static MeshRenderer CopyRenderer(MeshRenderer source, Transform parent, Vector3 position, Material[] materials)
    {
        var go = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.transform.localRotation = source.transform.rotation; go.transform.localScale = source.transform.lossyScale;
        go.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        var r = go.GetComponent<MeshRenderer>(); r.sharedMaterials = materials; NoShadows(r); return r;
    }
    static Light Light(string name, Vector3 euler, Color color, float intensity)
    {
        var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(euler);
        light.color = color; light.intensity = intensity; light.shadows = LightShadows.None; return light;
    }
    static Mesh ShadowQuad()
    {
        var mesh = new Mesh { name = "ContactShadowQuad" };
        const float r = .56f;
        mesh.vertices = new[] { new Vector3(-r,0,-r), new Vector3(-r,0,r), new Vector3(r,0,r), new Vector3(r,0,-r) };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0,1,2,0,2,3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    static Texture2D CreateBlob()
    {
        const int size = 128;
        var texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
        for (int y=0; y<size; y++) for (int x=0; x<size; x++)
        {
            float r = new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1).magnitude;
            float a = (1-Mathf.SmoothStep(.34f,1,r)) * .90f;
            texture.SetPixel(x,y,new Color(1,1,1,a));
        }
        texture.Apply(); string path = Root + "/SoftContactShadow.png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = true; importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.isReadable = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    [MenuItem("RealBuca/Lighting Prototype/Build WebGL comparison")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Editor must be idle before building.");
        var template = PlayerSettings.WebGL.template;
        try
        {
            PlayerSettings.WebGL.template = "PROJECT:BucaLightingComparison";
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = BuildTarget.WebGL,
                locationPathName = Output, options = BuildOptions.None
            });
            Directory.CreateDirectory("output/lighting-prototype");
            File.WriteAllText("output/lighting-prototype/build-result.txt", report.summary.result +
                "\nErrors=" + report.summary.totalErrors + "\nBytes=" + report.summary.totalSize +
                "\nDuration=" + report.summary.totalTime + "\nUnity=" + Application.unityVersion);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Prototype build failed.");
        }
        finally { PlayerSettings.WebGL.template = template; AssetDatabase.SaveAssets(); }
    }
}
#endif
