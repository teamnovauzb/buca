#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class BuildForestLeafWind
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode only");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var art=map.illustratedMenu.artwork;var child=art.Find("ForestLeafSwirls");
        if(child==null){child=new GameObject("ForestLeafSwirls",typeof(RectTransform)).transform;child.SetParent(art,false);}
        child.gameObject.layer=art.gameObject.layer;child.SetAsLastSibling();
        var rect=(RectTransform)child;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(1672,941);
        var graphic=child.GetComponent<ForestLeafWindGraphic>();if(graphic==null)graphic=child.gameObject.AddComponent<ForestLeafWindGraphic>();
        graphic.raycastTarget=false;graphic.Pose(4);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);
        return "Saved leaf swirls: 24 drifting leaves and soft ribbons in four woodland clearings. No click interception.";
    }
}
#endif
