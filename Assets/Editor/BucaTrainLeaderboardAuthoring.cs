#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Styles the existing authored leaderboard without replacing its references.</summary>
public static class BucaTrainLeaderboardAuthoring
{
    static readonly Color Navy = new Color(.035f,.12f,.20f);
    static readonly Color Teal = new Color(.13f,.40f,.42f);
    static readonly Color Wood = new Color(.96f,.84f,.63f);
    static Sprite rounded, circle;
    static void Place(RectTransform r, float x,float y,float w,float h)
    {
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); r.localScale=Vector3.one;
    }
    static UnityEngine.UI.Image Image(Transform parent,string name,float x,float y,float w,float h,Color color,bool round=false)
    {
        var t=parent.Find(name);
        var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent,false);
        var image=go.GetComponent<UnityEngine.UI.Image>()??go.AddComponent<UnityEngine.UI.Image>();
        Place((RectTransform)go.transform,x,y,w,h);
        image.sprite=round?circle:rounded; image.type=round?UnityEngine.UI.Image.Type.Simple:UnityEngine.UI.Image.Type.Sliced;
        image.color=color; image.raycastTarget=false; return image;
    }
    static void Text(TMP_Text text,Transform parent,float x,float y,float w,float h,float size)
    {
        text.transform.SetParent(parent,false); Place(text.rectTransform,x,y,w,h);
        text.color=Navy; text.fontSize=size; text.enableAutoSizing=false;
        text.enableVertexGradient=false; text.outlineWidth=0; text.characterSpacing=1;
        text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
        text.textWrappingMode=TextWrappingModes.NoWrap;
        text.overflowMode=TextOverflowModes.Ellipsis;
    }
    static Sprite MakeSprite(string name,bool disc)
    {
        const int n=128;
        var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);
        for(int y=0;y<n;y++) for(int x=0;x<n;x++)
        {
            float dx=Mathf.Abs(x-63.5f),dy=Mathf.Abs(y-63.5f);
            float distance=disc?Mathf.Sqrt(dx*dx+dy*dy)-61f:
                new Vector2(Mathf.Max(dx-43f,0),Mathf.Max(dy-43f,0)).magnitude-19f;
            float alpha=Mathf.Clamp01(-distance+.5f);
            float bevel=Mathf.Clamp01(-distance/6f);
            float shade=Mathf.Lerp(.66f,1f,bevel)*( .94f+.06f*y/127f);
            texture.SetPixel(x,y,new Color(shade,shade,shade,alpha));
        }
        texture.Apply();
        string path="Assets/Textures/"+name+".png";
        System.IO.File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spriteBorder=disc?Vector4.zero:new Vector4(24,24,24,24);
        importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        var panel=Object.FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .First(p=>p.gameObject.scene.path=="Assets/Scenes/Game.unity");
        rounded=MakeSprite("BucaTrainRounded",false); circle=MakeSprite("BucaTrainMedal",true);
        var card=panel.card;
        var frame=panel.transform.Find("TrainPresentation") as RectTransform;
        if(frame==null){frame=(RectTransform)new GameObject("TrainPresentation",typeof(RectTransform)).transform;frame.SetParent(panel.transform,false);}
        Place(frame,0,0,960,1110);frame.localScale=Vector3.one*.78f;
        card.SetParent(frame,false); Place(card,0,0,960,1110);
        var bg=card.GetComponent<UnityEngine.UI.Image>(); bg.sprite=rounded;bg.type=UnityEngine.UI.Image.Type.Sliced;bg.color=Teal;
        foreach(string name in new[]{"BorderOutline","HeaderStrip","Divider","LB_AccentBar","GlowHalo"})
        {var t=card.Find(name);if(t!=null)t.gameObject.SetActive(false);}
        var fx=card.GetComponent<LeaderboardCardFX>();if(fx!=null)fx.enabled=false;
        Image(card,"MapleFace",0,0,922,1072,Wood).transform.SetAsFirstSibling();
        // Fine horizontal maple grain is baked into the scene, never rebuilt during play.
        var grain=Image(card,"WoodGrain",0,0,900,1050,new Color(1,1,1,0)).transform;
        for(int i=0;i<34;i++) Image(grain,"Grain"+i,Mathf.Sin(i*3.1f)*30,-496+i*30,780+Mathf.Sin(i*2f)*70,1,new Color(.48f,.29f,.10f,.075f));
        grain.SetSiblingIndex(1);
        for(int i=0;i<4;i++) Image(card,"Pin"+i,i%2==0?-420:420,i<2?490:-490,22,22,new Color(.89f,.63f,.25f),true);
        Text(panel.titleText,card,0,413,830,180,65);
        panel.titleText.text="BUCA CHAMPIONS\n<size=50%>TIME'S UP!</size>";
        panel.titleText.lineSpacing=10;
        var rowsRoot=panel.rows[0].transform.parent as RectTransform; Place(rowsRoot,0,0,860,680);
        var all=panel.rows; panel.rows=all.Take(5).ToArray();
        for(int i=0;i<all.Length;i++)
        {
            var row=all[i]; row.gameObject.SetActive(i<5);if(i>=5)continue;
            var r=(RectTransform)row.transform; Place(r,0,237-i*139,860,110);
            Image(r,"CarRim",57,0,660,108,Teal).transform.SetAsFirstSibling();
            Place(row.rowBackground.rectTransform,57,0,646,94);
            row.rowBackground.sprite=rounded; row.rowBackground.type=UnityEngine.UI.Image.Type.Sliced;
            row.rowBackground.color=new Color(.97f,.87f,.68f);row.rowBackground.transform.SetSiblingIndex(1);
            Image(r,"Coupler",-292,0,70,22,new Color(.52f,.34f,.17f)).transform.SetAsFirstSibling();
            row.rankBadge=Image(r,"RankBadge",-359,0,106,106,new Color(.92f,.70f,.34f),true);
            Text(row.rankText,r,-359,0,82,82,49);
            Text(row.nameText,r,-35,5,410,72,43);row.nameText.alignment=TextAlignmentOptions.MidlineLeft;
            Text(row.scoreText,r,268,5,178,72,43);row.scoreText.alignment=TextAlignmentOptions.MidlineRight;
            for(int wheel=0;wheel<2;wheel++)
            {
                float x=wheel==0?-213:326;
                Image(r,"Wheel"+wheel,x,-49,46,46,new Color(.53f,.32f,.14f),true);
                Image(r,"Hub"+wheel,x,-49,22,22,new Color(.80f,.58f,.31f),true);
            }
            row.rankText.transform.SetAsLastSibling();
            row.playerRowTintColor=new Color(.60f,.85f,.77f);
            row.Populate(i+1,"",0,Navy,false); row.Clear();EditorUtility.SetDirty(row);
        }
        Text(panel.myRankText,card,0,-441,830,70,30);panel.myRankText.text="";
        if(panel.continueHintText!=null)panel.continueHintText.gameObject.SetActive(false);
        panel.normalRowColor=panel.myRowColor=Navy;panel.autoAdvanceSeconds=5;panel.fadeInDuration=.3f;
        panel.group.alpha=0;panel.group.blocksRaycasts=false;panel.group.interactable=false;
        var dim=panel.transform.Find("Dim")?.GetComponent<UnityEngine.UI.Image>();if(dim!=null)dim.color=new Color(.025f,.07f,.08f,.38f);
        EditorUtility.SetDirty(panel);EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        EditorSceneManager.SaveScene(panel.gameObject.scene);
        return "Saved wooden train leaderboard with five dynamic rows; no action buttons.";
    }
}
#endif
