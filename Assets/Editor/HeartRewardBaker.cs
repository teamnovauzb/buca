using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HeartRewardBaker
{
    // Scattered, authored rewards: no random generation during gameplay.
    public static bool IsRewardLevel(int number) => number==21 || number==24 || number==26 || number==29 || number==30;
    const string Folder="Assets/Art/HeartReward";
    const string StarPath="Assets/Prefabs/Prebuilt/PreferredRouteMarker.prefab";
    [MenuItem("RealBuca/Prebuild/Heart Reward Stars")]
    public static void Bake()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art","HeartReward");
        WoodenStarPickupBaker.Bake();
        BakeLevelPlacements();
        string hudPath="Assets/ToyBoxMenu/Prefabs/SolidGameplayHud.prefab";
        var hudRoot=PrefabUtility.LoadPrefabContents(hudPath);
        try{ExpandHearts(hudRoot.GetComponent<ToyBoxGameplayHud>());PrefabUtility.SaveAsPrefabAsset(hudRoot,hudPath);}
        finally{PrefabUtility.UnloadPrefabContents(hudRoot);}
        var rounded=Sprite("RoundedCard",false); var heart=Sprite("DomedHeart",true);
        var rootUI=new GameObject("HeartRewardPresentation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(HeartRewardPresentation));
        var canvas=rootUI.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=220;
        var scaler=rootUI.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var effect=rootUI.GetComponent<HeartRewardPresentation>();effect.canvasRect=rootUI.GetComponent<RectTransform>();effect.cards=new HeartRewardPresentation.Card[8];
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CoachDisplay.asset");
        for(int i=0;i<effect.cards.Length;i++)
        {
            var card=new GameObject("HeartRewardCard"+i,typeof(RectTransform),typeof(CanvasGroup));card.transform.SetParent(rootUI.transform,false);
            var rect=card.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(280,112);
            Image(rect,"Shadow",rounded,new Vector2(0,-8),new Vector2(286,118),new Color(.01f,.07f,.075f,.35f));
            Image(rect,"TealRim",rounded,Vector2.zero,new Vector2(280,112),new Color(.025f,.26f,.28f));
            Image(rect,"CreamFace",rounded,new Vector2(0,3),new Vector2(270,102),new Color(1,.9f,.69f));
            Image(rect,"Heart",heart,new Vector2(46,16),new Vector2(66,64),Color.white);
            Label(rect,"+1",font,new Vector2(-26,15),new Vector2(100,68),58);
            Label(rect,"EXTRA HEART",font,new Vector2(0,-32),new Vector2(240,28),24);
            effect.cards[i]=new HeartRewardPresentation.Card{rect=rect,group=card.GetComponent<CanvasGroup>()};
            card.GetComponent<CanvasGroup>().blocksRaycasts=false;card.GetComponent<CanvasGroup>().interactable=false;card.SetActive(false);
        }
        string effectPath="Assets/Prefabs/Prebuilt/HeartRewardPresentation.prefab";
        var saved=PrefabUtility.SaveAsPrefabAsset(rootUI,effectPath);UnityEngine.Object.DestroyImmediate(rootUI);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        try
        {
            var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LevelManager>(true)).First();
            var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ToyBoxGameplayHud>(true)).First();ExpandHearts(hud);
            foreach(var old in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HeartRewardPresentation>(true)).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(saved,scene);
            var fx=instance.GetComponent<HeartRewardPresentation>();fx.hud=hud;manager.heartRewardPresentation=fx;
            PrefabUtility.RecordPrefabInstancePropertyModifications(fx);EditorUtility.SetDirty(manager);
            EditorSceneManager.SaveScene(scene);
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();
        Debug.Log("HEART_REWARDS_BAKED: selected hard levels 21, 24, 26, 29, 30, 8 heart slots, 8 prebuilt animation cards");
    }
    public static void BakeLevelPlacements()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        var star=AssetDatabase.LoadAssetAtPath<GameObject>(StarPath);
        foreach(string directory in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        for(int number=1;number<=30;number++)
        {
            string path=$"{directory}/Level_{number:00}.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var existing=root.GetComponentsInChildren<ScorePickup>(true).Where(p=>p.grantsHeart).OrderBy(p=>p.transform.position.z).ToList();
                if(!IsRewardLevel(number))
                {
                    if(existing.Count>0){foreach(var pickup in existing)UnityEngine.Object.DestroyImmediate(pickup.gameObject);PrefabUtility.SaveAsPrefabAsset(root,path);}
                    continue;
                }
                if(existing.Count>2)
                {
                    while(existing.Count>2){UnityEngine.Object.DestroyImmediate(existing[1].gameObject);existing.RemoveAt(1);}
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                if(IsRewardLevel(number) && !root.GetComponentsInChildren<ScorePickup>(true).Any(p=>p.grantsHeart))
                {
                    Physics.SyncTransforms();
                    var start=root.GetComponentsInChildren<Transform>().First(t=>t.name=="PuckStart").position;
                    var colliders=root.GetComponentsInChildren<Collider>(true);
                    Vector3 chosen=Vector3.zero; bool found=false;
                    foreach(float distance in new[]{2.5f,2f,1.6f,3f})
                    {
                        foreach(float angle in new[]{20f,-20f,0f,40f,-40f,60f,-60f,90f,-90f,130f,-130f,180f})
                        {
                            Vector3 direction=Quaternion.Euler(0,angle,0)*Vector3.forward;
                            var point=start+direction*distance; point.y=.22f;
                            if(Mathf.Abs(point.x)>3.8f||Mathf.Abs(point.z)>6.2f)continue;
                            bool clear=true;
                            foreach(var collider in colliders)
                            {
                                if(!collider.enabled||!collider.gameObject.activeInHierarchy||collider.bounds.max.y<.12f||collider.GetComponent<ScorePickup>()!=null)continue;
                                for(int step=1;step<=12;step++)
                                {
                                    var sample=Vector3.Lerp(new Vector3(start.x,.22f,start.z),point,step/12f);
                                    if((collider.ClosestPoint(sample)-sample).sqrMagnitude<.42f*.42f){clear=false;break;}
                                }
                                if(!clear)break;
                            }
                            if(clear){chosen=point;found=true;break;}
                        }
                        if(found)break;
                    }
                    if(!found)throw new InvalidOperationException("No clear reward path in "+path);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(star,root.transform);
                    instance.name="HardLevel_HeartStar"; instance.transform.position=chosen;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    static void ExpandHearts(ToyBoxGameplayHud hud)
    {
        var slots=hud.heartSlots.ToList();var fills=hud.heartFills.ToList();
        while(slots.Count<8)
        {
            var slot=UnityEngine.Object.Instantiate(slots[0],slots[0].transform.parent);slot.name="HeartSlot"+slots.Count;
            slots.Add(slot);fills.Add(slot.GetComponentsInChildren<Transform>(true).First(t=>t.name=="RedHeart").gameObject);
        }
        hud.heartSlots=slots.ToArray();hud.heartFills=fills.ToArray();hud.RefreshHearts(5,5);EditorUtility.SetDirty(hud);
        if(PrefabUtility.IsPartOfPrefabInstance(hud))PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
    }
    static void Image(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
        var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;image.type=name=="Heart"?UnityEngine.UI.Image.Type.Simple:UnityEngine.UI.Image.Type.Sliced;
        image.rectTransform.anchoredPosition=position;image.rectTransform.sizeDelta=size;
    }
    static void Label(Transform parent,string text,TMP_FontAsset font,Vector2 position,Vector2 size,float fontSize)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var tmp=go.GetComponent<TextMeshProUGUI>();tmp.font=font;tmp.text=text;tmp.fontSize=fontSize;tmp.alignment=TextAlignmentOptions.Center;tmp.color=new Color(.02f,.17f,.19f);tmp.raycastTarget=false;
        tmp.rectTransform.anchoredPosition=position;tmp.rectTransform.sizeDelta=size;
    }
    static Sprite Sprite(string name,bool isHeart)
    {
        const int size=256;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float alpha=0;
            for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++)
            {
                float px=(x+(sx+.5f)/2)/size*2-1,py=(y+(sy+.5f)/2)/size*2-1;
                bool inside;
                if(isHeart){float a=px*1.22f,b=py*1.22f;inside=Mathf.Pow(a*a+b*b-1,3)-a*a*b*b*b<=0;}
                else {float dx=Mathf.Max(Mathf.Abs(px)-.75f,0),dy=Mathf.Max(Mathf.Abs(py)-.75f,0);inside=dx*dx+dy*dy<.24f*.24f;}
                if(inside)alpha+=.25f;
            }
            float shine=Mathf.Clamp01((y/(float)size-.4f)*1.6f);
            var color=isHeart?Color.Lerp(new Color(.65f,.04f,.07f),new Color(1,.36f,.33f),shine):Color.white;color.a=alpha;texture.SetPixel(x,y,color);
        }
        texture.Apply();string path=Folder+"/"+name+".png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=isHeart?Vector4.zero:new Vector4(40,40,40,40);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
