#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public static class BoardCoachTutorialAuthoring
{
    const string Path="Assets/Resources/Tutorials/BoardCoachTutorial.prefab";
    static TMP_FontAsset font;
    static Sprite rounded;
    static readonly Color Cream=new Color(.95f,.86f,.64f),Teal=new Color(.035f,.16f,.17f,.96f),Mint=new Color(.35f,.83f,.65f);
    [InitializeOnLoadMethod] static void Install(){EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode && AssetDatabase.LoadAssetAtPath<GameObject>(Path)==null)Build();};}
    [MenuItem("RealBuca/Toy Box 3D/Build Board Coach Tutorial")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Roboto-Bold SDF.asset");
        rounded=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var root=new GameObject("BoardCoachTutorial",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(CanvasGroup),typeof(BoardCoachTutorial));
        try
        {
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var view=root.GetComponent<BoardCoachTutorial>();view.group=root.GetComponent<CanvasGroup>();
            view.header=Panel(root.transform,"Header",new Vector2(.25f,.79f),new Vector2(.75f,.985f),Color.clear);
            var title=Panel(view.header,"WoodTitle",new Vector2(.10f,.58f),new Vector2(.90f,1),Cream);
            Label(title,"WATCH, THEN PLAY",32,Teal);
            view.stepFaces=new UnityEngine.UI.Image[3];
            string[] steps={"1  AIM","2  CHARGE","3  RELEASE"};
            for(int i=0;i<3;i++)
            {
                var step=Panel(view.header,"Step"+i,new Vector2(i/3f,.16f),new Vector2((i+1)/3f-.012f,.49f),Teal);
                view.stepFaces[i]=step.GetComponent<UnityEngine.UI.Image>();Label(step,steps[i],25,Cream);
            }
            view.footer=Panel(root.transform,"PlaybackFooter",Vector2.zero,new Vector2(1,.085f),Teal);
            view.pause=Button(view.footer,"Pause",new Vector2(.012f,.18f),new Vector2(.065f,.84f),"II",Cream,Teal);
            view.pauseLabel=view.pause.GetComponentInChildren<TMP_Text>();
            var timer=Panel(view.footer,"Time",new Vector2(.077f,.19f),new Vector2(.31f,.84f),Color.clear);
            view.clock=Label(timer,"DEMO PLAYING · 0:00 / 0:07",20,Cream);
            var track=Panel(view.footer,"Track",new Vector2(.32f,.42f),new Vector2(.64f,.52f),new Color(.32f,.38f,.36f));
            view.progress=Fill(track,"Progress",Mint);
            view.replay=Button(view.footer,"Replay",new Vector2(.68f,.16f),new Vector2(.825f,.86f),"WATCH AGAIN",Cream,Teal);
            view.play=Button(view.footer,"YourTurn",new Vector2(.84f,.16f),new Vector2(.988f,.86f),"YOUR TURN  >",Mint,Teal);
            view.chargeAnchor=Panel(root.transform,"ChargeCallout",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Teal);
            view.chargeAnchor.sizeDelta=new Vector2(370,78);
            view.caption=Label(view.chargeAnchor,"HOLD FOR MORE POWER",20,Cream);
            view.caption.rectTransform.anchorMin=new Vector2(0,.32f);
            var meter=Panel(view.chargeAnchor,"ChargeTrack",new Vector2(.08f,.10f),new Vector2(.92f,.25f),new Color(.25f,.32f,.3f));
            view.charge=Fill(meter,"Charge",Mint);
            var handObject=new GameObject("DemoHand",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
            view.hand=(RectTransform)handObject.transform;view.hand.SetParent(root.transform,false);
            view.hand.anchorMin=view.hand.anchorMax=new Vector2(.5f,.5f);view.hand.pivot=new Vector2(.17f,.87f);view.hand.sizeDelta=new Vector2(220,220);
            var handImage=handObject.GetComponent<UnityEngine.UI.RawImage>();handImage.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tutorials/Honey/CoachHand.png");handImage.raycastTarget=false;
            PrefabUtility.SaveAsPrefabAsset(root,Path);AssetDatabase.SaveAssets();
            Debug.Log("BOARD_COACH_BUILT: live-board tutorial, three steps, replay, pause and Your Turn.");
        }
        finally{Object.DestroyImmediate(root);}
    }
    static RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=false;return r;
    }
    static TMP_Text Label(RectTransform parent,string text,float size,Color color)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        var t=go.GetComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMin=size*.75f;t.fontSizeMax=size;return t;
    }
    static UnityEngine.UI.Button Button(Transform parent,string name,Vector2 min,Vector2 max,string text,Color bg,Color fg)
    {
        var r=Panel(parent,name,min,max,bg);r.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=r.GetComponent<UnityEngine.UI.Image>();Label(r,text,22,fg);return b;
    }
    static UnityEngine.UI.Image Fill(Transform parent,string name,Color color)
    {
        var r=Panel(parent,name,Vector2.zero,Vector2.one,color);var image=r.GetComponent<UnityEngine.UI.Image>();image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.fillAmount=0;return image;
    }
}
#endif
