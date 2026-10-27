using UnityEngine;

/// <summary>Places the railway signal beside the currently selected destination.</summary>
[ExecuteAlways]
public sealed class ForestHarborSignalSelector : MonoBehaviour
{
    public TwilightIllustratedMenu view;
    public ForestHarborMapPresentation presentation;
    public RectTransform signal;
    public ForestHarborSelectionOutline outline;
    public Vector2 signalOffset = new Vector2(-3.5f, 63f);
    int outlinedStop = -1;
    bool outlinedRound;
    static readonly Vector2[] BendOffsets = {
        new Vector2(-52, 45), new Vector2(52, 45),
        new Vector2(-58, 0), new Vector2(58, 0),
        new Vector2(-52, -45), new Vector2(52, -45)
    };

    void OnEnable() => Refresh();

    public void Refresh()
    {
        if (view == null || view.map == null || signal == null || outline == null ||
            view.stopRects == null || view.map.Selected < 0 || view.map.Selected >= view.stopRects.Length)
            return;

        int selected = view.map.Selected;
        var stop = view.stopRects[selected];
        bool round = presentation != null && presentation.roundForestStops && selected < 30;
        signal.anchoredPosition = stop.anchoredPosition + ClearOffset(selected);
        outline.rectTransform.anchoredPosition = stop.anchoredPosition;
        outline.rectTransform.localRotation = round ? Quaternion.identity :
            view.glows[selected].rectTransform.localRotation;
        if (outlinedStop != selected || outlinedRound != round)
        {
            outlinedStop = selected;
            outlinedRound = round;
            outline.circular = round;
            outline.rectTransform.sizeDelta = round ? new Vector2(46, 46) : new Vector2(60, 33);
            outline.SetVerticesDirty();
        }
    }

    Vector2 ClearOffset(int selected)
    {
        Vector2 best = signalOffset;
        float leastOverlap = Overlap(selected, best);
        if (leastOverlap == 0) return best;
        // At the tight vertical bends the next stop is directly above this one.
        // Tuck the signal beside the rail so it never hides another level number.
        foreach (var offset in BendOffsets)
        {
            float overlap = Overlap(selected, offset);
            if (overlap >= leastOverlap) continue;
            best = offset;
            leastOverlap = overlap;
            if (overlap == 0) break;
        }
        return best;
    }

    float Overlap(int selected, Vector2 offset)
    {
        var position = view.stopRects[selected].anchoredPosition + offset;
        var bounds = new Rect(position - signal.sizeDelta * .5f, signal.sizeDelta);
        if (bounds.xMin < -TwilightIllustratedMenu.ArtWidth * .5f ||
            bounds.xMax > TwilightIllustratedMenu.ArtWidth * .5f ||
            bounds.yMin < -TwilightIllustratedMenu.ArtHeight * .5f ||
            bounds.yMax > TwilightIllustratedMenu.ArtHeight * .5f) return float.MaxValue;
        float area = 0;
        for (int i = 0; i < view.stopRects.Length; i++)
        {
            var stop = view.stopRects[i];
            var half = stop.sizeDelta * .5f + Vector2.one * (i == selected ? 1 : 5);
            var center = stop.anchoredPosition;
            float width = Mathf.Max(0, Mathf.Min(bounds.xMax, center.x + half.x) -
                Mathf.Max(bounds.xMin, center.x - half.x));
            float height = Mathf.Max(0, Mathf.Min(bounds.yMax, center.y + half.y) -
                Mathf.Max(bounds.yMin, center.y - half.y));
            area += width * height;
        }
        return area;
    }
}
