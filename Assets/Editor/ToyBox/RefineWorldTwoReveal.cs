#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RefineWorldTwoReveal
{
    public static string Apply()
    {
        var root=PrefabUtility.LoadPrefabContents(BuildWorldTwoHarborReveal.Path);
        try
        {
            var reveal=root.GetComponent<WorldTwoHarborReveal>();reveal.duration=18f;reveal.revealStartSeconds=7f;
            var viewport=root.transform.Find("MapViewport") as RectTransform;
            if(viewport==null)
            {
                viewport=new GameObject("MapViewport",typeof(RectTransform)).GetComponent<RectTransform>();
                viewport.SetParent(root.transform,false);
            }
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;
            viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            viewport.SetSiblingIndex(1);
            reveal.artwork.transform.SetParent(viewport,false);
            reveal.artwork.uvRect=new Rect(0,0,1,1);
            reveal.FitMapToScreen();
            if(reveal.trainUpgrade!=null)reveal.trainUpgrade.wholeMap=true;
            var sourceMap=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
            if(reveal.ticketLayer==null)
            {
                var layer=new GameObject("LevelTickets",typeof(RectTransform));layer.transform.SetParent(reveal.artwork.transform,false);
                reveal.ticketLayer=(RectTransform)layer.transform;reveal.ticketLayer.sizeDelta=new Vector2(1672,941);
                reveal.ticketLayer.SetAsFirstSibling();reveal.worldTwoTickets=new CanvasGroup[20];
                for(int i=0;i<50;i++)
                {
                    if(reveal.trainUpgrade!=null&&(i==29||i==30))continue;
                    var ticket=UnityEngine.Object.Instantiate(sourceMap.illustratedMenu.stopRects[i].gameObject,layer.transform,false);
                    var group=ticket.GetComponent<CanvasGroup>();if(group==null)group=ticket.AddComponent<CanvasGroup>();
                    group.alpha=1;group.blocksRaycasts=false;
                    foreach(var button in ticket.GetComponentsInChildren<Button>(true))button.onClick=new Button.ButtonClickedEvent();
                    foreach(var graphic in ticket.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
                    var badge=ticket.GetComponentInChildren<WorkshopStationBadge>(true);
                    if(badge!=null){badge.SetTrainClearance(Vector2.zero);badge.SetLocked(i>30);badge.SetState(false,false);}
                    if(i>=30)reveal.worldTwoTickets[i-30]=group;
                }
            }
            var audio=reveal.GetComponent<AudioSource>();if(audio==null)audio=reveal.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake=false;audio.spatialBlend=0;reveal.chimeSource=audio;
            reveal.revealChime=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ScoreCountChime.wav");
            if(root.GetComponent<GraphicRaycaster>()==null)root.AddComponent<GraphicRaycaster>();
            if(root.transform.Find("RevealFooter")==null)
            {
                var footer=new GameObject("RevealFooter",typeof(RectTransform),typeof(Image));footer.transform.SetParent(root.transform,false);
                var rect=(RectTransform)footer.transform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(0,210);rect.anchoredPosition=Vector2.zero;
                footer.GetComponent<Image>().color=new Color(.015f,.035f,.055f);
                var title=new GameObject("Title",typeof(RectTransform),typeof(TextMeshProUGUI));title.transform.SetParent(footer.transform,false);
                var label=title.GetComponent<TextMeshProUGUI>();label.rectTransform.anchoredPosition=new Vector2(0,40);label.rectTransform.sizeDelta=new Vector2(600,50);
                label.font=sourceMap.illustratedMenu.GetComponent<ForestHarborMapPresentation>().destination.font;
                label.text="WORLD 2";label.fontSize=28;label.color=new Color(1,.85f,.45f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            }
            if(reveal.continueButton==null)
            {
                var go=new GameObject("PlayLevel31",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(root.transform,false);
                var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);
                rect.anchoredPosition=new Vector2(0,28);rect.sizeDelta=new Vector2(420,76);
                var face=go.GetComponent<Image>();face.color=new Color(.025f,.035f,.055f,.98f);
                var outline=go.AddComponent<Outline>();outline.effectColor=new Color(1,.79f,.3f);outline.effectDistance=new Vector2(2,-2);
                var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);
                var label=text.GetComponent<TextMeshProUGUI>();label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
                var map=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
                label.font=map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().destination.font;
                label.text="1   PLAY LEVEL 31";label.fontSize=30;label.color=new Color(1,.94f,.76f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
                reveal.continueButton=go.GetComponent<Button>();reveal.continueButton.targetGraphic=face;
            }
            reveal.continueButton.gameObject.SetActive(false);
            reveal.continueButton.transform.SetAsLastSibling();
            var footerRect=root.transform.Find("RevealFooter") as RectTransform;
            if(footerRect!=null)footerRect.sizeDelta=new Vector2(0,152);
            var footerTitle=root.transform.Find("RevealFooter/Title") as RectTransform;
            if(footerTitle!=null)footerTitle.anchoredPosition=new Vector2(0,56);
            var playLabel=reveal.continueButton.GetComponentInChildren<TMP_Text>(true);
            if(playLabel!=null)playLabel.text="PLAY LEVEL 31";
            PrefabUtility.SaveAsPrefabAsset(root,BuildWorldTwoHarborReveal.Path);AssetDatabase.SaveAssets();
            return "18-second full-map reveal: seven-second train transformation, slow World 2 opening during travel, then Play Level 31.";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
