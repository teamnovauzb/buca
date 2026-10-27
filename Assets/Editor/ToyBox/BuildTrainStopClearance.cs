#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Parks the engine over its hidden ticket.</summary>
public static class BuildTrainStopClearance
{
    public const float StopOffset = 0f;
    public const string Output = "output/train-ticket-clearance";

    public static void Configure(TwilightRailwayMap map)
    {
        map.trainNoseOffset = StopOffset;
        var view = map.illustratedMenu;
        view.GetComponent<ForestHarborMapPresentation>()?.ResetTicketClearance();
        int lastStop = view.stopRects.Max(stop => stop.GetSiblingIndex());
        if (view.locomotive.GetSiblingIndex() < lastStop)
            view.locomotive.SetSiblingIndex(lastStop);
    }

    [MenuItem("RealBuca/Workshop Railway/Park Train On Level Tickets")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || map.gameObject.scene.path != "Assets/Scenes/MainMenu.unity")
            throw new Exception("Open the existing railway menu first.");
        Directory.CreateDirectory(Output);
        string backup = Output + "/MainMenu.before-ticket-clearance.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(map.gameObject.scene, backup, true);
        Undo.RecordObjects(new Object[] { map, map.train, map.illustratedMenu.locomotive }, "Park train on level tickets");
        Configure(map);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(TwilightRailwayMap).GetField("distance", flags).SetValue(map, map.stops[map.Parked].distance);
        typeof(TwilightRailwayMap).GetMethod("Pose", flags).Invoke(map, null);
        map.illustratedMenu.PoseTrain(); map.GetComponent<SuitcaseLocomotiveView>().Frame();
        map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().Refresh();
        EditorUtility.SetDirty(map);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        if (!EditorSceneManager.SaveScene(map.gameObject.scene)) throw new Exception("Scene save failed.");
        AssetDatabase.SaveAssets(); EditorApplication.QueuePlayerLoopUpdate();
        return "Saved centered tickets, hidden beneath the parked train and restored on departure.";
    }
}
#endif
