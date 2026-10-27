using UnityEngine;

/// <summary>Persistent grassy World 1 reminders for each animal milestone.</summary>
[ExecuteAlways]
public sealed class AnimalSurpriseMarkers : MonoBehaviour
{
    public RectTransform[] markers;
    public string[] friendIds={"rabbit","fox","bear","panda","penguin"};
    public int[] levels={5,10,15,20,25};
    TwilightIllustratedMenu view;
    public bool showAllForPreview;
    static readonly Vector2[] GrassSpots={new Vector2(-560,-225),new Vector2(-245,-225),new Vector2(-50,-210),new Vector2(-330,110),new Vector2(-200,250)};
    public static void Ensure(TwilightIllustratedMenu menu)
    {
        if(menu.artwork==null)return;
        var existing=menu.artwork.GetComponentInChildren<AnimalSurpriseMarkers>(true);
        if(existing!=null){existing.view=menu;existing.Refresh();return;}
        var prefab=Resources.Load<AnimalSurpriseMarkers>("AnimalSurpriseMarkers");
        if(prefab==null)return;
        var instance=Instantiate(prefab,menu.artwork,false);instance.view=menu;
        foreach(var child in instance.GetComponentsInChildren<Transform>(true))child.gameObject.layer=menu.artwork.gameObject.layer;
        // Above scenery, below the train and controls.
        if(menu.locomotive!=null)
        {
            Transform trainLayer=menu.locomotive;
            while(trainLayer.parent!=null && trainLayer.parent!=menu.artwork)trainLayer=trainLayer.parent;
            instance.transform.SetSiblingIndex(trainLayer.GetSiblingIndex());
        }
        instance.Refresh();
    }
    void OnEnable(){view=GetComponentInParent<TwilightIllustratedMenu>();Refresh();}
    public void Refresh()
    {
        if(view==null)return;
        for(int i=0;i<markers.Length;i++)
        {
            int index=levels[i]-1;
            if(index>=view.route.Length)continue;
            bool visible=showAllForPreview || i>=friendIds.Length || !AnimalTrainCollection.IsRiding(friendIds[i])
                || AnimalTrainCollection.IsWaitingToBoard(friendIds[i]);
            markers[i].gameObject.SetActive(visible);
            if(!visible)continue;
            markers[i].localScale=Vector3.one;
            markers[i].anchoredPosition=GrassSpots[i];
            markers[i].localRotation=Quaternion.identity;
        }
    }
}
