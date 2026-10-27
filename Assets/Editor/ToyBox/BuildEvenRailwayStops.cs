#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Resamples level positions on the existing track without changing its shape.</summary>
public static class BuildEvenRailwayStops
{
    public const float GapPixels = 64f;
    public const string Output = "output/even-level-spacing";
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("RealBuca/Workshop Railway/Even Level Spacing In Both Worlds")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || map.gameObject.scene.path != "Assets/Scenes/MainMenu.unity")
            throw new Exception("Open the existing MainMenu railway first.");
        Directory.CreateDirectory(Output);
        string backup = Output + "/MainMenu.before-even-spacing.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(map.gameObject.scene, backup, true);
        Arrange(map, true);
        Validate(map);
        // Keep the current train parked at its selected stop after the distances change.
        int parked = map.Parked;
        typeof(TwilightRailwayMap).GetField("distance", Private).SetValue(map, map.stops[parked].distance);
        typeof(TwilightRailwayMap).GetMethod("Pose", Private).Invoke(map, null);
        map.illustratedMenu.PoseTrain();
        map.GetComponent<SuitcaseLocomotiveView>().Frame();
        map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().Refresh();
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        EditorSceneManager.SaveScene(map.gameObject.scene); AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();
        return "Saved 64-pixel center-to-center level spacing in both worlds, following the existing rails.";
    }

    public static void Arrange(TwilightRailwayMap map, bool undo)
    {
        var view = map.illustratedMenu;
        if (map.stops.Length != 50 || view.stopRects.Length != 50 ||
            !view.GetComponent<ForestHarborMapPresentation>().workshopRailway)
            throw new Exception("Existing 50-stop workshop railway required.");
        var lengths = new float[map.railway.Length];
        for (int i = 1; i < lengths.Length; i++)
            lengths[i] = lengths[i - 1] + Vector3.Distance(map.railway[i - 1], map.railway[i]);
        // Equal visible gaps need extra rail length around tight bends. Use chord
        // distance in the artwork plane, rather than crowding labels on those bends.
        var forest = Plan(map, lengths, 0, 29);
        var harbor = Plan(map, lengths, 30, 49);
        if (forest[29] >= harbor[0]) throw new Exception("World boundary would overlap.");
        if (undo) Undo.RecordObjects(new Object[] { map, view }, "Even railway level spacing");
        for (int i = 0; i < 50; i++)
        {
            float distance = i < 30 ? forest[i] : harbor[i - 30];
            var point = map.Point(distance);
            var rect = view.stopRects[i];
            if (undo) Undo.RecordObject(rect, "Move level " + (i + 1));
            map.stops[i].distance = distance;
            view.route[i] = new Vector2(point.x, point.z) * 20;
            rect.anchoredPosition = view.route[i];
            if (undo) EditorUtility.SetDirty(rect);
        }
        if (undo) { EditorUtility.SetDirty(map); EditorUtility.SetDirty(view); }
    }

    static float[] Plan(TwilightRailwayMap map, float[] lengths, int first, int last)
    {
        float center = (map.stops[first].distance + map.stops[last].distance) * .5f;
        float low = 0, high = center;
        int count = last - first + 1;
        float[] positions = null;
        // Preserve each world's occupied route center. Reapplying is idempotent.
        for (int iteration = 0; iteration < 32; iteration++)
        {
            float start = (low + high) * .5f;
            if (!TryPositions(map, lengths, start, count, out positions) ||
                (start + positions[count - 1]) * .5f > center) high = start;
            else low = start;
        }
        if (!TryPositions(map, lengths, low, count, out positions))
            throw new Exception("Not enough existing rail for evenly spaced levels.");
        return positions;
    }

    static bool TryPositions(TwilightRailwayMap map, float[] lengths, float start, int count, out float[] positions)
    {
        positions = new float[count]; positions[0] = start;
        float gap = GapPixels / 20;
        for (int n = 1; n < count; n++)
        {
            float from = positions[n - 1];
            var origin = map.Point(from);
            int index = Array.BinarySearch(lengths, from);
            index = index < 0 ? ~index : index + 1;
            while (index < lengths.Length && FlatDistance(origin, map.railway[index]) < gap) index++;
            if (index == lengths.Length) return false;
            float low = Mathf.Max(from, lengths[index - 1]), high = lengths[index];
            for (int i = 0; i < 24; i++)
            {
                float mid = (low + high) * .5f;
                if (FlatDistance(origin, map.Point(mid)) < gap) low = mid;
                else high = mid;
            }
            positions[n] = (low + high) * .5f;
        }
        return true;
    }

    static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

    public static void Validate(TwilightRailwayMap map)
    {
        var view = map.illustratedMenu;
        for (int i = 0; i < 50; i++)
        {
            var point = map.Point(map.stops[i].distance);
            var position = new Vector2(point.x, point.z) * 20;
            if (Vector2.Distance(position, view.stopRects[i].anchoredPosition) > .02f ||
                Vector2.Distance(position, view.route[i]) > .02f)
                throw new Exception("Marker or train stop off the rail: " + (i + 1));
            if (i == 0) continue;
            if (map.stops[i].distance <= map.stops[i - 1].distance)
                throw new Exception("Level order broken.");
            // The bridge between worlds is a scenic transition, without extra levels.
            if (i == 30) continue;
            float gap = Vector2.Distance(view.route[i - 1], position);
            if (Mathf.Abs(gap - GapPixels) > .03f)
                throw new Exception("Unequal level gap before " + (i + 1) + ": " + gap);
        }
    }
}
#endif
