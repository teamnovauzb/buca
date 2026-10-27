#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class BuildArrivalMarker
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode only");
        var p=Object.FindAnyObjectByType<ForestHarborMapPresentation>(FindObjectsInactive.Include);
        var marker=p.GetComponent<ForestHarborArrivalMarker>();if(marker==null)marker=p.gameObject.AddComponent<ForestHarborArrivalMarker>();
        var art=p.view.artwork;var child=art.Find("ArrivedLevelGlow");
        if(child==null){child=new GameObject("ArrivedLevelGlow",typeof(RectTransform)).transform;child.SetParent(art,false);}
        child.gameObject.layer=art.gameObject.layer;child.SetAsLastSibling();
        var rect=(RectTransform)child;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        var ring=child.GetComponent<ForestHarborSelectionOutline>();if(ring==null)ring=child.gameObject.AddComponent<ForestHarborSelectionOutline>();
        ring.raycastTarget=false;ring.arrived=true;marker.view=p.view;marker.presentation=p;marker.ring=ring;p.arrivalMarker=marker;marker.Refresh();
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
        return "Saved green arrival marker, tracking the train parked stop in both worlds.";
    }
}
#endif
