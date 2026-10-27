#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BuildOpenRailwayEntrance
{
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || map.gameObject.scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("MainMenu required.");
        const string output = "output/open-railway-entrance";
        Directory.CreateDirectory(output);
        if (!File.Exists(output + "/MainMenu.before-entrance.unity")) File.Copy(map.gameObject.scene.path, output + "/MainMenu.before-entrance.unity");
        const string path = "Assets/TwilightMap/WorkshopRailway/OpenEntrance.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale = TextureImporterNPOTScale.None; importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        var view = map.illustratedMenu;
        // Use the exact approved full plate, with no blended replacement region.
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var background = view.artwork.GetComponent<UnityEngine.UI.RawImage>();
        background.texture = texture;
        var backdrop = view.transform.Find("FullBleedBackdrop").GetComponent<UnityEngine.UI.RawImage>();
        backdrop.texture = texture;
        var oldPatch = view.artwork.Find("OpenRailwayEntrance");
        if (oldPatch != null) oldPatch.gameObject.SetActive(false);
        typeof(BuildWorkshopRailway).GetMethod("BuildRoute", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]{map});
        for (int i=0;i<50;i++)
        {
            var point = BuildForestHarborMap.Art(BuildWorkshopRailway.Stations[i]);
            view.route[i] = point; view.stopRects[i].anchoredPosition = point;
        }
        // The original foreground tree now falls on the new rail edge; keep its rooted mesh beside the track.
        var grove = map.GetComponentInChildren<GroundedForestGrove>(true);
        if (grove != null && grove.roots.Length > 12)
        {
            grove.artworkRoots[12] = new Vector2(66,735);
            grove.roots[12].localPosition = new Vector3((66-425)/20f,0,(470.5f-735)/10f);
            grove.Pose(0); EditorUtility.SetDirty(grove);
        }
        bool active=map.gameObject.activeSelf; map.gameObject.SetActive(true);
        map.Open(null); view.Layout(); view.Refresh(); view.PoseTrain();
        view.GetComponent<ForestHarborMapPresentation>().Refresh();
        map.GetComponent<SuitcaseLocomotiveView>().Frame(); map.gameObject.SetActive(active);
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(view); EditorUtility.SetDirty(background); EditorUtility.SetDirty(backdrop);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene); EditorSceneManager.SaveScene(map.gameObject.scene);
        AssetDatabase.SaveAssets(); EditorApplication.QueuePlayerLoopUpdate();
        return "Saved cave-free single entrance curve and aligned train route and level targets.";
    }
}
#endif
