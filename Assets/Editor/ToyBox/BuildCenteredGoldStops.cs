#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildCenteredGoldStops
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required");
        var map=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity") throw new Exception("MainMenu required");
        var view=map.illustratedMenu;
        typeof(BuildWorkshopRailway).GetMethod("BuildRoute",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{map});
        for(int i=0;i<50;i++)
        {
            var point=map.Point(map.stops[i].distance);
            var center=new Vector2(point.x,point.z)*20;
            view.stopRects[i].anchoredPosition=center;
            view.route[i]=center;
            float diameter=i<10?36:(i<30?30:29);
            var plaque=view.stopRects[i].Find("WorkshopPlaque") as RectTransform;
            plaque.GetComponent<WorkshopStationBadge>().SetTrainClearance(Vector2.zero);
            plaque.anchoredPosition=Vector2.zero;
            plaque.sizeDelta=Vector2.one*diameter;
            plaque.GetComponent<WorkshopStationBadge>().SetAllDirty();
            view.faces[i].rectTransform.sizeDelta=Vector2.one*(diameter-2);
            view.glows[i].rectTransform.sizeDelta=Vector2.one*(diameter-2);
            var number=view.numbers[i];
            number.rectTransform.anchoredPosition=Vector2.zero;
            number.rectTransform.sizeDelta=Vector2.one*diameter;
            number.fontSize=i<10?19:16;
            number.fontStyle=TMPro.FontStyles.Bold;
            view.locks[i].transform.localPosition=new Vector3(diameter*.33f,-diameter*.33f,0);
            view.awards[i].transform.localPosition=new Vector3(-diameter*.33f,-diameter*.33f,0);
        }
        view.Refresh();view.GetComponent<ForestHarborMapPresentation>().Refresh();
        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        EditorSceneManager.SaveScene(map.gameObject.scene);
        return "Applied 50 centered cream-and-gold rail tokens with navy numerals.";
    }
}
#endif
