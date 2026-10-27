#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
public static class PolishCompletionMap
{
    public static string Apply()
    {
        var map=Object.FindFirstObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        string path="Assets/TwilightMap/WorkshopRailway/WorldTwoHarborReveal.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var upgrade=root.GetComponentInChildren<MoonPuffToyUpgrade>(true);
            for(int i=29;i<=30;i++){
                var old=i==29?upgrade.ticket30:upgrade.ticket31;
                var ticket=Object.Instantiate(map.illustratedMenu.stopRects[i].gameObject,old.parent,false).GetComponent<RectTransform>();
                ticket.name="MapTicket"+(i+1);ticket.anchoredPosition=old.anchoredPosition;
                var group=ticket.GetComponent<CanvasGroup>();if(group!=null){group.alpha=1;group.blocksRaycasts=false;}
                foreach(var graphic in ticket.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
                foreach(var button in ticket.GetComponentsInChildren<UnityEngine.UI.Button>(true))Object.DestroyImmediate(button);
                var badge=ticket.GetComponentInChildren<WorkshopStationBadge>();if(badge!=null){badge.SetLocked(false);badge.SetState(false,false);}
                foreach(var number in ticket.GetComponentsInChildren<TMP_Text>(true))number.color=new Color(.025f,.09f,.22f);
                if(i==29)upgrade.ticket30=ticket;else upgrade.ticket31=ticket;
                Object.DestroyImmediate(old.gameObject);
            }
            upgrade.ticket31Face=null;
            if(upgrade.emeraldLevelBadge==null){
                var source=map.illustratedMenu.artwork.GetComponentInChildren<TrainLevelBadge>(true);
                var marker=Object.Instantiate(source.gameObject,upgrade.trainImage.transform.parent,false);
                upgrade.emeraldLevelBadge=(RectTransform)marker.transform;upgrade.emeraldLevelNumber=marker.GetComponent<TrainLevelBadge>().number;
                Object.DestroyImmediate(marker.GetComponent<TrainLevelBadge>());marker.transform.SetAsLastSibling();
            }
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
                if(text.name=="SelectedLevel"||text.name=="Caption"||text.name=="Time"||text.name=="Title"||text.name=="Label"&&text.text=="PLAY LEVEL 31")text.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        // Remove leftover legacy labels from the ordinary map too.
        foreach(var text in map.GetComponentsInChildren<TMP_Text>(true))
            if(text.name=="SelectedLevel"||text.name=="Caption"||text.name=="Time")text.gameObject.SetActive(false);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(map.gameObject.scene);
        BuildCompletionRailwayMap.Apply();
        return "Consistent ticket shapes and emerald train marker; legacy footer labels hidden; completion map refreshed.";
    }
}
#endif
