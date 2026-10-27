#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

public static class SuitcasePocketWatchValidation
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Check(TwilightRailwayMap map)
    {
        var watch = map.illustratedMenu.GetComponentInChildren<SuitcasePocketWatch>(true);
        if (watch == null) throw new Exception("Pocket watch is missing.");
        var countdown = typeof(TwilightRailwayMap).GetField("countdown", Private);
        float original = map.Countdown;
        int selected = map.Selected;
        try
        {
            if (watch.ring.GetComponent<CanvasRenderer>() == null) throw new Exception("The ring renderer is missing.");
            foreach (float remaining in new[] { 30f, 15f, .1f, 0f })
            {
                countdown.SetValue(map, remaining);
                watch.Refresh();
                string expected = "00:" + Mathf.CeilToInt(remaining).ToString("00");
                if (watch.digits.text != expected) throw new Exception("Incorrect watch digits at " + remaining);
                if (Mathf.Abs(watch.ring.Fill - remaining / 30f) > .0001f) throw new Exception("Ring does not match the countdown.");
                Canvas.ForceUpdateCanvases();
                var mesh = watch.ring.canvasRenderer.GetMesh();
                if ((mesh != null && mesh.vertexCount > 0) != (remaining > 0))
                    throw new Exception("Ring geometry does not match the remaining time.");
            }
            if (watch.ring.raycastTarget || watch.digits.raycastTarget || watch.caption.raycastTarget)
                throw new Exception("The watch can intercept menu clicks.");
            if (map.illustratedMenu.countdownText.gameObject.activeSelf)
                throw new Exception("The old duplicate countdown is still visible.");
            map.Select(selected);
            if (map.Countdown != 30f) throw new Exception("Selection did not reset the countdown.");
            if (map.HighestUnlocked > 0)
            {
                map.Select(map.Parked == 0 ? 1 : map.Parked - 1);
                typeof(TwilightRailwayMap).GetMethod("Update", Private).Invoke(map, null);
                watch.Refresh();
                if (!map.Travelling || map.Countdown != 30f || watch.caption.text != "TRAVELLING")
                    throw new Exception("The timer did not pause while the train travelled.");
                map.Select(selected);
                for (int i = 0; i < 24000 && (map.Moving || map.Parked != selected); i++) map.TickTrain(1f / 60);
                if (map.Moving || map.Parked != selected) throw new Exception("Train failed to return after timer check.");
            }
            countdown.SetValue(map, 15f);
            typeof(TwilightRailwayMap).GetMethod("Update", Private).Invoke(map, null);
            if (map.Countdown >= 15f) throw new Exception("The timer did not resume after arrival.");
            System.IO.File.WriteAllText("output/suitcase-menu/watch-validation.txt",
                "PASS: 00:30, 00:15, 00:01 and 00:00; matching full, half and empty rings; reset on selection; pause during travel; resume after arrival; no duplicate countdown or intercepted clicks.");
        }
        finally
        {
            map.Select(selected);
            countdown.SetValue(map, original);
            watch.Refresh();
        }
    }
}
#endif
