#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class BuildRabbitGiftReward
{
    public const string Path="Assets/Art/RabbitGift/RabbitGiftReward.prefab";
    const string Atlas="Assets/Art/RabbitGift/RabbitGiftAtlas.png";
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=pos;return r;
    }
    static UnityEngine.UI.Image Picture(string name,Transform parent,Sprite sprite,Vector2 size,Vector2 pos)
    {
        var r=Rect(name,parent,size,pos);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite=sprite;image.raycastTarget=false;return image;
    }
    static Sprite Slice(Texture2D texture,string name,int x,int top,int w,int h)
    {
        // Sprite rects slice the atlas without modifying the generated artwork.
        string path="Assets/Art/RabbitGift/"+name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(old!=null)return old;
        var sprite=Sprite.Create(texture,new UnityEngine.Rect(x,texture.height-top-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    static void Label(Transform parent,string text,float size,Vector2 dimensions,Vector2 pos,TMP_FontAsset font)
    {
        var r=Rect("Label",parent,dimensions,pos);var label=r.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font;label.text=text;label.fontSize=size;label.fontStyle=FontStyles.Normal;
        label.alignment=TextAlignmentOptions.Center;label.color=new Color(.19f,.095f,.025f);label.raycastTarget=false;
    }
    static void ConfigureCelebration(RabbitGiftReward reward)
    {
        if(reward.celebration==null)
        {
            var rect=Rect("ColorfulCelebration",reward.transform,new Vector2(1920,1080),Vector2.zero);
            reward.celebration=rect.gameObject.AddComponent<RabbitGiftCelebration>();
            
            rect.SetSiblingIndex(reward.transform.Find("RewardTitle").GetSiblingIndex());
        }
        BuildRabbitGift3D.Configure(reward);
        if(reward.cheerSource==null)
        {
            var audio=Rect("RabbitCheer",reward.transform,Vector2.zero,Vector2.zero);
            reward.cheerSource=audio.gameObject.AddComponent<AudioSource>();
        }
        reward.cheerSource.playOnAwake=false;reward.cheerSource.spatialBlend=0;
        reward.happyCheer=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/HappyYeah.wav");
    }
    public static string AddCelebration()
    {
        var root=PrefabUtility.LoadPrefabContents(Path);
        try{ConfigureCelebration(root.GetComponent<RabbitGiftReward>());PrefabUtility.SaveAsPrefabAsset(root,Path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        return "Saved colorful confetti, golden stars and synchronized cheer";
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var source=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(source==null)throw new Exception("Open MainMenu first");
        var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
        importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
        var box=Slice(texture,"GiftBox",30,92,508,432);
        var front=Slice(texture,"GiftFront",30,246,508,278);
        var top=Slice(texture,"GiftLid",534,174,526,278);
        var body=Slice(texture,"RabbitBody",1080,66,399,494);
        var earA=Slice(texture,"LeftEar",32,558,240,398);
        var earB=Slice(texture,"RightEar",286,558,236,398);
        var arm=Slice(texture,"WavingPaw",610,566,326,394);
        var plaque=Slice(texture,"WoodPlaque",942,672,586,188);
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Oswald Bold SDF.asset");
        var root=Rect("RabbitGiftReward",null,new Vector2(1920,1080),Vector2.zero).gameObject;
        try
        {
            var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30001;
            var scaler=root.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var reward=root.AddComponent<RabbitGiftReward>();reward.fade=root.AddComponent<CanvasGroup>();
            var back=Picture("Backdrop",root.transform,null,Vector2.zero,Vector2.zero);back.color=new Color(.025f,.045f,.065f);back.raycastTarget=true;
            back.rectTransform.anchorMin=Vector2.zero;back.rectTransform.anchorMax=Vector2.one;back.rectTransform.offsetMin=back.rectTransform.offsetMax=Vector2.zero;
            var map=Rect("YourLevelMap",root.transform,Vector2.zero,Vector2.zero);
            var raw=map.gameObject.AddComponent<UnityEngine.UI.RawImage>();raw.texture=source.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>().texture;raw.raycastTarget=false;raw.material=source.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>().material;
            var fitter=map.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fitter.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fitter.aspectRatio=1672f/941;
            reward.artwork=raw;
            var layer=Rect("LevelTickets",map,new Vector2(1672,941),Vector2.zero);reward.ticketLayer=layer;
            for(int i=0;i<source.illustratedMenu.stopRects.Length;i++)
            {
                var original=source.illustratedMenu.stopRects[i];if(original==null)continue;
                var stop=UnityEngine.Object.Instantiate(original.gameObject,layer,false);
                var cg=stop.GetComponent<CanvasGroup>();if(cg!=null){cg.alpha=1;cg.blocksRaycasts=false;}
                foreach(var g in stop.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))g.raycastTarget=false;
                foreach(var b in stop.GetComponentsInChildren<UnityEngine.UI.Button>(true))UnityEngine.Object.DestroyImmediate(b);
                var badge=stop.GetComponentInChildren<WorkshopStationBadge>(true);
                if(badge!=null){badge.SetTrainClearance(Vector2.zero);badge.SetLocked(i>5);badge.SetState(false,false);}
            }
            var veil=Picture("SoftVeil",root.transform,null,Vector2.zero,Vector2.zero);veil.color=new Color(.035f,.07f,.11f,.23f);
            veil.rectTransform.anchorMin=Vector2.zero;veil.rectTransform.anchorMax=Vector2.one;veil.rectTransform.offsetMin=veil.rectTransform.offsetMax=Vector2.zero;
            var title=Picture("RewardTitle",root.transform,plaque,new Vector2(430,130),new Vector2(0,355));
            reward.heading=title.gameObject.AddComponent<CanvasGroup>();
            Label(title.transform,"LEVEL 5 COMPLETE",30,new Vector2(370,55),new Vector2(0,13),font);
            Label(title.transform,"RABBIT UNLOCKED",22,new Vector2(350,36),new Vector2(0,-25),font);
            reward.gift=Rect("GiftRig",root.transform,new Vector2(700,750),new Vector2(0,-82));
            Picture("BoxInterior",reward.gift,box,new Vector2(420,357),new Vector2(0,-80));
            reward.lid=Picture("OpeningLid",reward.gift,top,new Vector2(435,230),new Vector2(0,36)).rectTransform;
            var mask=Rect("InsideBoxMask",reward.gift,new Vector2(760,620),new Vector2(0,280));
            mask.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            // The mask's bottom is at the front rim, so the rabbit emerges from inside the gift.
            var origin=Rect("RabbitOrigin",mask,Vector2.zero,new Vector2(0,-280));
            reward.rabbit=Rect("Rabbit",origin,Vector2.zero,Vector2.zero);
            reward.leftEar=Picture("LeftEar",reward.rabbit,earA,new Vector2(98,180),new Vector2(-57,205)).rectTransform;
            reward.rightEar=Picture("RightEar",reward.rabbit,earB,new Vector2(98,180),new Vector2(57,205)).rectTransform;
            reward.leftEar.pivot=reward.rightEar.pivot=new Vector2(.5f,0);
            Picture("RabbitFaceAndScarf",reward.rabbit,body,new Vector2(260,322),new Vector2(0,83));
            reward.leftPaw=Picture("RestingPaw",reward.rabbit,arm,new Vector2(104,132),new Vector2(-76,48)).rectTransform;
            reward.leftPaw.pivot=new Vector2(.72f,.17f);
            reward.paw=Picture("GreetingPaw",reward.rabbit,arm,new Vector2(114,145),new Vector2(76,48)).rectTransform;
            reward.paw.pivot=new Vector2(.72f,.17f);
            reward.paw.localScale=new Vector3(-1,1,1);
            // Bury the shoulder ends beneath the torso; rotate at the actual arm roots.
            reward.leftPaw.SetSiblingIndex(2);
            reward.paw.SetSiblingIndex(3);
            // Exact atlas crop of the front face overlays the rabbit, with no duplicate box edge.
            Picture("BoxFront",reward.gift,front,new Vector2(420,357f*278/432),new Vector2(0,-80-357f/2+357f*278/432/2));
            var buttonFace=Picture("Continue",root.transform,plaque,new Vector2(340,92),new Vector2(0,-410));buttonFace.raycastTarget=true;
            reward.controls=buttonFace.gameObject.AddComponent<CanvasGroup>();
            reward.continueButton=buttonFace.gameObject.AddComponent<UnityEngine.UI.Button>();reward.continueButton.targetGraphic=buttonFace;
            var colors=reward.continueButton.colors;colors.highlightedColor=new Color(1,.94f,.77f);colors.pressedColor=new Color(.8f,.74f,.6f);reward.continueButton.colors=colors;
            Label(buttonFace.transform,"CONTINUE  ›",32,new Vector2(300,70),Vector2.zero,font);
            reward.sound=root.AddComponent<AudioSource>();reward.sound.playOnAwake=false;reward.sound.spatialBlend=0;
            reward.revealSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/RabbitGiftReveal.wav");
            ConfigureCelebration(reward);
            reward.Sample(0);root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,Path);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        try
        {
            LevelManager manager=null;
            foreach(var item in scene.GetRootGameObjects()){manager=item.GetComponentInChildren<LevelManager>(true);if(manager!=null)break;}
            if(manager==null)throw new Exception("Game LevelManager missing");
            manager.rabbitGiftRewardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path).GetComponent<RabbitGiftReward>();
            EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();return "Rabbit gift prefab built and connected to level 5 completion.";
    }
}
#endif
