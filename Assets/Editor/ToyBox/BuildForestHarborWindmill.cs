#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BuildForestHarborWindmill
{
    const string Folder = "Assets/TwilightMap/ForestHarbor/Windmill";

    [MenuItem("RealBuca/Forest Harbor/Add Rotating Windmill")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || !map.illustratedMenu.forestHarborPresentation) throw new Exception("Forest harbor map required.");
        Directory.CreateDirectory("output/windmill-animation");
        const string backup = "output/windmill-animation/MainMenu.before-windmill.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        var tower = Texture("WindmillWithoutBlades", false);
        var blades = Texture("WindmillBlades", true);
        var shader = Shader.Find("BUCA/Windmill Repair");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Windmill repair shader is unavailable.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/WindmillRepair.mat");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, Folder + "/WindmillRepair.mat");
        }
        var root = Rect(map.illustratedMenu.artwork, "WindmillLife", new Vector2(1672,941));
        root.SetSiblingIndex(3);
        var repair = Rect(root, "FixedTower", new Vector2(200,200));
        repair.anchoredPosition = Art(410,100);
        var towerImage = Get<UnityEngine.UI.RawImage>(repair.gameObject);
        towerImage.texture = tower; towerImage.material = material; towerImage.raycastTarget = false;
        var axle = Rect(root, "FixedAxle", Vector2.zero);
        axle.anchoredPosition = Art(410,69);
        axle.localScale = new Vector3(.9f,1,1);
        var rotor = Rect(axle, "FourRotatingBlades", new Vector2(118,118));
        // The painted brass hub is slightly off the centre of the transparent image.
        rotor.pivot = new Vector2(640f/1254,1-607f/1254);
        var rotorImage = Get<UnityEngine.UI.RawImage>(rotor.gameObject);
        rotorImage.texture = blades; rotorImage.raycastTarget = false;
        var animation = Get<ForestHarborWindmill>(root.gameObject);
        animation.blades = rotor; animation.revolutionSeconds = 16; animation.Pose(0);
        EditorUtility.SetDirty(animation);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved four rotating blades with a fixed tower and centre axle. One clockwise revolution every 16 seconds.";
    }

    static Vector2 Art(float x,float y) => new Vector2(x-836,470.5f-y);
    static Texture2D Texture(string name,bool alpha)
    {
        string path = Folder + "/" + name + ".png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("Missing " + path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = alpha; importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static T Get<T>(GameObject go) where T:Component
    {
        var component = go.GetComponent<T>(); return component != null ? component : go.AddComponent<T>();
    }
    static RectTransform Rect(Transform parent,string name,Vector2 size)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            var go = new GameObject(name,typeof(RectTransform)); go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go,"Add rotating windmill");
            rect = (RectTransform)go.transform; rect.SetParent(parent,false);
        }
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one*.5f;
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
        return rect;
    }
}
#endif
