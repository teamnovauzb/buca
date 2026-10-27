using UnityEngine;

public sealed partial class ForestHarborMapPresentation
{
    CanvasGroup[] stopVisibility;

    public void ResetTicketClearance()
    {
        if(stationPlaques==null)return;
        foreach(var badge in stationPlaques)
            if(badge!=null)badge.SetTrainClearance(Vector2.zero);
    }

    void RefreshTicketClearance()
    {
        if(view==null||view.map==null||view.stopRects==null)return;
        if(stopVisibility==null||stopVisibility.Length!=view.stopRects.Length)
            stopVisibility=new CanvasGroup[view.stopRects.Length];
        var map=view.map;
        for(int i=0;i<view.stopRects.Length;i++)
        {
            var stop=view.stopRects[i];
            if(stop==null)continue;
            // Every ticket stays on its authored rail stop, whether selected or travelling.
            if(workshopRailway&&view.route!=null&&i<view.route.Length)
                stop.anchoredPosition=view.route[i];
            if(stationPlaques!=null&&i<stationPlaques.Length)
            {
                var badge=stationPlaques[i];
                if(badge!=null)badge.SetTrainClearance(Vector2.zero);
            }
            // Hide the occupied stop marker consistently, including level 31.
            var group=stopVisibility[i];
            if(group==null)
            {
                group=stop.GetComponent<CanvasGroup>();
                if(group==null)group=stop.gameObject.AddComponent<CanvasGroup>();
                stopVisibility[i]=group;
            }
            bool covered=!map.Moving&&i==map.Parked;
            group.alpha=covered?0:1;
            group.blocksRaycasts=!covered;
        }
    }
}
