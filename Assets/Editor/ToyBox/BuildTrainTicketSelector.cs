#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class BuildTrainTicketSelector
{
    static void Place(RectTransform rect,float x,float y,float w,float h)
    {
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.anchoredPosition=BuildForestHarborMap.Art(new Vector2(x,y));rect.sizeDelta=new Vector2(w,h);
        rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode only");
        const string path="Assets/TwilightMap/ForestHarbor/TrainTicketSelector.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var p=Object.FindAnyObjectByType<ForestHarborMapPresentation>(FindObjectsInactive.Include);
        var parent=p.destination.transform.parent;
        var child=parent.Find("TrainTicketArtwork");
        if(child==null){child=new GameObject("TrainTicketArtwork",typeof(RectTransform)).transform;child.SetParent(parent,false);}
        child.gameObject.layer=parent.gameObject.layer;child.SetAsFirstSibling();
        Place((RectTransform)child,568,855,310,128);
        var image=child.GetComponent<UnityEngine.UI.RawImage>();if(image==null)image=child.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);image.raycastTarget=false;
        Place(p.previous.GetComponent<RectTransform>(),455,848,60,64);
        Place(p.next.GetComponent<RectTransform>(),680,848,60,64);
        Place(p.destination.rectTransform,600,852,71,46);p.destination.fontSize=34;p.destination.enableAutoSizing=false;
        p.destination.alignment=TextAlignmentOptions.Center;p.destination.color=new Color(.025f,.08f,.18f);p.destination.raycastTarget=false;
        Place(p.progress.rectTransform,568,901,112,23);p.progress.fontSize=15;p.progress.alignment=TextAlignmentOptions.Center;
        var labelTransform=parent.Find("TicketLevelLabel");
        if(labelTransform==null){labelTransform=new GameObject("TicketLevelLabel",typeof(RectTransform)).transform;labelTransform.SetParent(parent,false);}
        labelTransform.gameObject.layer=parent.gameObject.layer;
        var label=labelTransform.GetComponent<TextMeshProUGUI>();if(label==null)label=labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=p.destination.font;label.fontSize=12;label.text="LEVEL";label.color=p.destination.color;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        Place(label.rectTransform,600,824,75,20);p.Refresh();
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved train-ticket selector; existing previous/next and controller selection retained.";
    }
}
#endif
