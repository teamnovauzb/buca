#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Applies the approved 30 round stops to the existing forest map and its train route.</summary>
public static class BuildForestHarborRoundStops
{
    const string Folder = "Assets/TwilightMap/ForestHarbor/RoundStops";
    public const string Output = "output/round-stops-implementation";
    static Vector2 P(float x, float y) => new Vector2(x, y);
    public static readonly Vector2[] ForestStations = {
        P(159,711),P(230,688),P(310,689),P(389,697),P(465,700),
        P(534,699),P(595,692),P(656,681),P(730,651),P(780,606),
        P(780,524),P(709,486),P(638,500),P(577,518),P(516,526),
        P(452,519),P(369,502),P(279,479),P(180,449),P(98,392),
        P(81,319),P(102,246),P(153,187),P(266,201),P(347,234),
        P(438,261),P(530,277),P(616,286),P(684,286),P(756,279)
    };

    public static void AddCurveKeys(List<Vector2> keys, int station)
    {
        if (station == 9) keys.Add(P(763,633));
        if (station == 10) keys.Add(P(794,566));
        if (station == 11) keys.Add(P(752,494));
        if (station == 20) keys.AddRange(new[] { P(73,357), P(69,340) });
        if (station == 21) keys.AddRange(new[] { P(103,290), P(109,267) });
        if (station == 22) keys.AddRange(new[] { P(96,221), P(114,200) });
    }

    [MenuItem("RealBuca/Forest Harbor/Apply 30 Round Stops")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var view = map != null ? map.illustratedMenu : null;
        if (view == null || !view.forestHarborPresentation || view.stopRects.Length != 50)
            throw new Exception("Existing forest / harbor map required.");
        var presentation = view.GetComponent<ForestHarborMapPresentation>();
        if (presentation == null) throw new Exception("Existing map presentation required.");
        Directory.CreateDirectory(Output);
        string backup = Output + "/MainMenu.before-round-stops.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);

        const string texturePath = Folder + "/RoundStopsApproved.png";
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) throw new Exception("Copy the approved round-marker preview first.");
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        var circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luxodd.Game/Example/Sprites/white_circle.png");
        if (circle == null) throw new Exception("Circular highlight sprite missing.");

        Undo.RecordObject(map, "Redistribute 30 railway stops");
        Undo.RecordObject(view, "Move forest level buttons");
        Undo.RecordObject(presentation, "Use round forest stops");
        presentation.roundForestStops = true;
        presentation.forestOpenCurve = true;
        BuildForestHarborMap.BuildRoute(map, presentation);
        for (int i = 0; i < 30; i++)
        {
            var rect = view.stopRects[i];
            Undo.RecordObject(rect, "Move level " + (i + 1));
            view.route[i] = BuildForestHarborMap.Art(ForestStations[i]);
            rect.anchoredPosition = view.route[i]; rect.sizeDelta = P(48,48);
            rect.localRotation = Quaternion.identity;
            foreach (var graphic in new[] { view.faces[i], view.glows[i] })
            {
                Undo.RecordObject(graphic, "Round marker feedback");
                Undo.RecordObject(graphic.rectTransform, "Round marker feedback");
                graphic.sprite = circle; graphic.type = UnityEngine.UI.Image.Type.Simple;
                graphic.rectTransform.anchoredPosition = Vector2.zero;
                graphic.rectTransform.sizeDelta = P(42,42);
                graphic.rectTransform.localRotation = Quaternion.identity;
            }
            view.glows[i].color = new Color(.1f,1,.37f,.3f);
            view.glows[i].raycastTarget = false;
            view.faces[i].raycastTarget = true;
            Undo.RecordObject(view.locks[i].transform, "Place marker lock");
            Undo.RecordObject(view.awards[i].transform, "Place marker award");
            view.locks[i].transform.localPosition = new Vector3(18,-17,0);
            view.awards[i].transform.localPosition = new Vector3(-18,-17,0);
        }

        var layer = view.artwork.Find("RoundForestRailway") as RectTransform;
        if (layer == null)
        {
            var go = new GameObject("RoundForestRailway", typeof(RectTransform), typeof(CanvasRenderer), typeof(ForestHarborRailArtwork));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Apply approved railway artwork");
            layer = (RectTransform)go.transform;
            layer.SetParent(view.artwork, false);
        }
        if (layer.GetComponent<CanvasRenderer>() == null)
            Undo.AddComponent<CanvasRenderer>(layer.gameObject);
        Undo.RecordObject(layer, "Fit railway artwork");
        layer.anchorMin = layer.anchorMax = layer.pivot = Vector2.one * .5f;
        layer.anchoredPosition = Vector2.zero; layer.sizeDelta = P(1672,941);
        layer.localScale = Vector3.one; layer.localRotation = Quaternion.identity;
        int firstStopIndex = view.stopRects[0].GetSiblingIndex();
        if (layer.GetSiblingIndex() < firstStopIndex) firstStopIndex--;
        layer.SetSiblingIndex(firstStopIndex);
        var graphicLayer = layer.GetComponent<ForestHarborRailArtwork>();
        Undo.RecordObject(graphicLayer, "Fit approved railway strip");
        graphicLayer.artworkTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        graphicLayer.raycastTarget = false;
        var strip = new List<Vector2>();
        float end = map.stops[29].distance + 1.7f;
        for (float distance = 0; distance < end; distance += .2f)
        {
            var point = map.Point(distance);
            strip.Add(P(point.x * 20 + 836, 470.5f - point.z * 20));
        }
        var finish = map.Point(end);
        strip.Add(P(finish.x * 20 + 836, 470.5f - finish.z * 20));
        graphicLayer.pixelPath = strip.ToArray();
        graphicLayer.SetAllDirty();

        // Refit the current selection to the new distance table; do not write player progress.
        map.Open(null); view.Refresh(); view.PoseTrain(); presentation.Refresh();
        map.GetComponent<SuitcaseLocomotiveView>().Frame();
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(presentation); EditorUtility.SetDirty(graphicLayer);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved 30 round World 1 markers with matching click targets, circular highlights and sequential train stops.";
    }
}
#endif
