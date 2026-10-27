#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
public static class BuildTrainLevelBadge
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var map=UnityEngine.Object.FindFirstObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.illustratedMenu==null||map.illustratedMenu.artwork==null)throw new Exception("Map artwork missing");
        var existing=map.illustratedMenu.artwork.Find("EmeraldTrainLevelBadge");
        var root=existing!=null?(RectTransform)existing:new GameObject("EmeraldTrainLevelBadge",typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(map.illustratedMenu.artwork,false);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(86,60);root.SetAsLastSibling();
        if(root.GetComponent<CanvasRenderer>()==null)root.gameObject.AddComponent<CanvasRenderer>();
        var graphic=root.GetComponent<EmeraldTrainBadgeGraphic>()??root.gameObject.AddComponent<EmeraldTrainBadgeGraphic>();graphic.raycastTarget=false;graphic.SetAllDirty();
        var controller=root.GetComponent<TrainLevelBadge>()??root.gameObject.AddComponent<TrainLevelBadge>();controller.map=map;
        var font=map.illustratedMenu.numbers[0].font;
        Label(root,"Heading","LEVEL",font,11,new Vector2(0,14),new Vector2(60,15));
        controller.number=Label(root,"Number","1",font,30,new Vector2(0,-7),new Vector2(58,36));
        controller.Refresh();EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved emerald level badge: follows engine, uses reached level, no raycast blocking.";
    }
    static TMP_Text Label(RectTransform parent,string name,string text,TMP_FontAsset font,float size,Vector2 position,Vector2 dimensions)
    {
        var found=parent.Find(name);var label=found!=null?found.GetComponent<TextMeshProUGUI>():new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent,false);label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(.5f,.5f);
        label.rectTransform.anchoredPosition=position;label.rectTransform.sizeDelta=dimensions;label.font=font;label.fontSize=size;label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;
        label.text=text;label.color=new Color(1,.97f,.78f);label.raycastTarget=false;label.enableWordWrapping=false;return label;
    }
}
#endif
