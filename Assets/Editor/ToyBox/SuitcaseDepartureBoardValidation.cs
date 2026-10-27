#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public static class SuitcaseDepartureBoardValidation
{
    public static void Check(TwilightRailwayMap map)
    {
        var view=map.illustratedMenu;
        var board=view.GetComponentInChildren<SuitcaseDepartureBoard>(true);
        if(board==null)throw new Exception("Departure board missing.");
        int selected=map.Selected,highest=map.HighestUnlocked;
        var countdown=typeof(TwilightRailwayMap).GetField("countdown",BindingFlags.Instance|BindingFlags.NonPublic);
        float savedCountdown=map.Countdown;
        try
        {
            map.Select(0);Snapshot(board,0);
            if(board.previousButton.interactable)throw new Exception("Previous is enabled at level 1.");
            countdown.SetValue(map,9f);board.Previous();
            if(map.Selected!=0||map.Countdown!=9f)throw new Exception("Lower boundary changed selection or countdown.");
            for(int i=1;i<50;i++)
            {
                EventSystem.current.SetSelectedGameObject(board.nextButton.gameObject);
                board.nextButton.onClick.Invoke();
                Snapshot(board,i);
                if(map.Countdown!=TwilightRailwayMap.DepartureSeconds)throw new Exception("Arrow did not reset the departure timer.");
                if(EventSystem.current.currentSelectedGameObject!=null)throw new Exception("Arrow retained keyboard focus.");
            }
            if(board.nextButton.interactable)throw new Exception("Next is enabled at level 50.");
            board.Next();Snapshot(board,49);
            for(int i=48;i>=0;i--){board.previousButton.onClick.Invoke();Snapshot(board,i);}
            // Direct map clicks must drive the exact same number display and canopy.
            foreach(int i in new[]{8,9,28,29,30,49})
            {view.stopButtons[i].onClick.Invoke();Snapshot(board,i);}
            if(highest<49)
            {
                map.Select(map.Parked);map.Select(highest+1);
                float before=map.RouteDistance;
                map.TickTrain(1f/60);view.Refresh();board.Refresh(true);
                if(map.RouteDistance!=before||map.Moving||view.playButton.interactable)
                    throw new Exception("Locked destination moved or enabled Play.");
            }
            map.Select(Mathf.Min(1,highest));view.Refresh();board.Refresh(true);
            Canvas.ForceUpdateCanvases();view.Layout();board.LayoutMenu();Canvas.ForceUpdateCanvases();
            foreach(var button in new[]{board.previousButton,board.nextButton,view.backButton,view.fullscreenButton,view.playButton})
                Hit(view,button);
            if(board.markerArt.raycastTarget||board.tens.raycastTarget||board.ones.raycastTarget)
                throw new Exception("Selector art intercepts station clicks.");
            if(map.HighestUnlocked!=highest)throw new Exception("Selector changed unlocked progress.");
            System.IO.File.WriteAllText("output/suitcase-menu/departure-selector-validation.txt",
                "PASS: 49 forward and 49 reverse arrow selections; 1/50 boundaries; timer reset; direct station clicks including 9/10 and 30/31; both digits and canopy synchronized; locked levels cannot move or launch; all footer controls receive pointer hits; keyboard focus released; unlock progress unchanged.");
        }
        finally{map.Select(selected);countdown.SetValue(map,savedCountdown);view.Refresh();board.Refresh(true);}
    }

    static void Snapshot(SuitcaseDepartureBoard board,int expected)
    {
        var view=board.view;view.Refresh();board.Refresh(true);
        if(view.map.Selected!=expected)throw new Exception("Wrong selected index: "+expected);
        string digits=(expected+1).ToString("00");
        if(board.tens.text+board.ones.text!=digits)throw new Exception("Wrong departure digits: "+digits);
        var position=view.stopRects[expected].anchoredPosition+Vector2.up*24;
        if(Vector2.Distance(board.marker.anchoredPosition,position)>.01f)throw new Exception("Canopy missed station "+digits);
    }

    static void Hit(TwilightIllustratedMenu view,UnityEngine.UI.Button button)
    {
        var position=RectTransformUtility.WorldToScreenPoint(view.canvas.worldCamera,button.transform.position);
        if(!view.canvas.worldCamera.pixelRect.Contains(position))throw new Exception(button.name+" is offscreen.");
        var hits=new List<RaycastResult>();
        view.canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().Raycast(new PointerEventData(EventSystem.current){position=position},hits);
        if(hits.Count==0||hits[0].gameObject!=button.gameObject)throw new Exception(button.name+" is blocked.");
    }
}
#endif
