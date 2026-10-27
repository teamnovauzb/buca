#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Replaces only the old forest tunnel corner with the approved open railway.</summary>
public static class BuildForestHarborOpenCurve
{
    const string Folder = "Assets/TwilightMap/ForestHarbor/OpenWoodlandCurve";

    [MenuItem("RealBuca/Forest Harbor/Apply Open Woodland Curve")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || !map.illustratedMenu.forestHarborPresentation) throw new Exception("Forest harbor map required.");
        var view = map.illustratedMenu;
        var presentation = view.GetComponent<ForestHarborMapPresentation>();
        if (presentation == null) throw new Exception("Existing map presentation required.");
        var shader = Shader.Find("BUCA/Open Woodland Curve");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Open curve shader did not compile.");
        Directory.CreateDirectory("output/open-woodland-curve");
        const string backup = "output/open-woodland-curve/MainMenu.before-open-curve.unity";
        if (!File.Exists(backup)) File.Copy(scene.path,backup);

        string texturePath = Folder + "/OpenCurve.png";
        AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/OpenCurveRepair.mat");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material,Folder + "/OpenCurveRepair.mat");
        }
        var patch = view.artwork.Find("OpenWoodlandCurve") as RectTransform;
        if (patch == null)
        {
            var go = new GameObject("OpenWoodlandCurve",typeof(RectTransform)); go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go,"Replace forest tunnel with open curve");
            patch = (RectTransform)go.transform; patch.SetParent(view.artwork,false);
        }
        patch.anchorMin = patch.anchorMax = patch.pivot = Vector2.one*.5f;
        patch.anchoredPosition = BuildForestHarborMap.Art(new Vector2(120,300));
        patch.sizeDelta = new Vector2(240,280); patch.localRotation = Quaternion.identity;
        patch.localScale = Vector3.one; patch.SetSiblingIndex(4);
        var image = patch.GetComponent<UnityEngine.UI.RawImage>();
        if (image == null) image = patch.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        image.material = material; image.raycastTarget = false;

        Undo.RecordObject(map,"Follow open woodland rails");
        Undo.RecordObject(presentation,"Keep forest train visible");
        presentation.forestOpenCurve = true;
        BuildForestHarborMap.BuildRoute(map,presentation);
        // Refit the current Edit mode pose to the revised distance table without touching saved progress.
        map.Open(null); view.PoseTrain(); presentation.Refresh();
        map.GetComponent<SuitcaseLocomotiveView>().Frame();
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(presentation);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved open woodland curve between levels 20 and 21 with low stone edging and a continuously visible train.";
    }
}
#endif
