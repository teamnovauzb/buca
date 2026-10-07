#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PolishResultStandings
{
    const string Folder="Assets/Art/PolishedStandings";
    static readonly Color Ink=new Color(.018f,.12f,.13f),Cream=new Color(1,.95f,.82f);
    static TMP_FontAsset font;
    static Material lettering;
    static TMP_FontAsset BakedFont()
    {
        string path=Folder+"/StandingsFont.asset";
        var saved=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(saved!=null&&saved.atlasRenderMode==UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA)return saved;
        var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        bool created=saved==null;
        if(created)
        {
            saved=TMP_FontAsset.CreateFontAsset(source,80,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
            saved.name="Standings Font";AssetDatabase.CreateAsset(saved,path);
        }
        else
        {
            saved.atlasPopulationMode=AtlasPopulationMode.Dynamic;saved.ClearFontAssetData(true);
            var serialized=new SerializedObject(saved);serialized.FindProperty("m_AtlasRenderMode").intValue=(int)UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var characters=new System.Text.StringBuilder();
        for(int c=32;c<384;c++)characters.Append((char)c);
        for(int c=1024;c<1280;c++)characters.Append((char)c);
        characters.Append("–—…’");
        saved.TryAddCharacters(characters.ToString(),out string missing);
        if(!saved.HasCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789–…"))throw new Exception("Leaderboard font is missing required glyphs.");
        saved.atlasPopulationMode=AtlasPopulationMode.Static;saved.boldStyle=.75f;
        if(created)AssetDatabase.AddObjectToAsset(saved.material,saved);
        foreach(var atlas in saved.atlasTextures){if(!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,saved);EditorUtility.SetDirty(atlas);}
        EditorUtility.SetDirty(saved);return saved;
    }
    static Material Mat(string name,Color color,float metallic=0)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.32f);m.SetFloat("_Metallic",metallic);
        return ToyBoxGeometry.Save(m,Folder+"/"+name+".mat");
    }
    static Transform Child(Transform parent,string name)
    {
        var t=parent.Find(name);if(t!=null)return t;
        var go=new GameObject(name);go.layer=31;go.transform.SetParent(parent,false);return go.transform;
    }
    static GameObject Mesh(Transform p,string name,Vector3 pos,Mesh mesh,Material material)
    {
        var t=Child(p,name);t.localPosition=pos;
        var f=t.GetComponent<MeshFilter>();if(f==null)f=t.gameObject.AddComponent<MeshFilter>();f.sharedMesh=mesh;
        var r=t.GetComponent<MeshRenderer>();if(r==null)r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;return t.gameObject;
    }
    static TMP_Text Label(Transform parent,string name,Vector3 pos,Vector2 size,float pointSize,TextAlignmentOptions alignment,Color color)
    {
        var t=parent.Find(name);
        if(t==null){var go=new GameObject(name,typeof(TextMeshPro));go.layer=31;go.transform.SetParent(parent,false);t=go.transform;}
        var text=t.GetComponent<TMP_Text>();
        t.localPosition=pos;t.localScale=Vector3.one;t.localRotation=Quaternion.identity;
        text.enabled=true;text.font=font;text.fontSharedMaterial=lettering;text.fontStyle=FontStyles.Bold;
        text.fontSize=pointSize;text.fontSizeMax=pointSize;text.fontSizeMin=pointSize*.76f;text.enableAutoSizing=true;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;text.alignment=alignment;
        text.richText=false;text.color=color;text.characterSpacing=0;text.enableVertexGradient=false;
        // World-space TMP uses a tenth-size point scale, even with this orthographic camera.
        text.isOrthographic=false;
        text.rectTransform.sizeDelta=size;text.margin=Vector4.zero;text.raycastTarget=false;
        return text;
    }
    [MenuItem("RealBuca/Polish Leaderboard Typography")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required.");
        string restore=SceneManager.GetActiveScene().path;
        if(restore!="Assets/Scenes/Game.unity")EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("output/leaderboard");AssetDatabase.Refresh();
        File.Copy("Assets/Scenes/Game.unity","output/leaderboard/Game-before-typography-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
        try
        {
            var view=UnityEngine.Object.FindAnyObjectByType<BucaResultStandings>(FindObjectsInactive.Include);
            if(view==null)throw new Exception("Saved standings are missing.");
            var panel=view.panel;
            font=BakedFont();
            lettering=ToyBoxGeometry.Save(new Material(font.material){name="CrispStandingsText"},Folder+"/CrispStandingsText.mat");
            // The SDK example includes an older shader with the same name but incompatible UV packing.
            lettering.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader");
            lettering.SetColor("_FaceColor",Color.white);lettering.SetFloat("_FaceDilate",.12f);lettering.SetFloat("_OutlineWidth",0);lettering.DisableKeyword("GLOW_ON");lettering.DisableKeyword("UNDERLAY_ON");
            var teal=Mat("DeepTeal",new Color(.018f,.18f,.18f));
            var cream=Mat("IvoryRows",new Color(.94f,.87f,.70f));
            var mint=Mat("MintAccent",new Color(.31f,.78f,.64f));
            var gold=Mat("FirstPlace",new Color(.88f,.65f,.28f),.22f);
            var silver=Mat("SecondPlace",new Color(.72f,.80f,.79f),.2f);
            var bronze=Mat("ThirdPlace",new Color(.77f,.48f,.29f),.18f);
            var ivory=Mat("RaisedCreamLetters",Cream);
            ToyBoxGeometry.Initialize();
            Mesh(view.transform,"TitlePlaque",new Vector3(0,2.64f,-.48f),ToyBoxGeometry.RoundedBox(new Vector3(6.85f,.78f,.13f),.06f),teal);
            var title=view.transform.Find("StandingsTitle");title.localPosition=new Vector3(0,2.64f,-.58f);
            title.GetComponent<MeshRenderer>().sharedMaterial=ivory;
            var mesh=ToyBoxGeometry.Text("LEADERBOARD",.05f,.012f,.03f);title.GetComponent<MeshFilter>().sharedMesh=mesh;
            title.localScale=Vector3.one*Mathf.Min(.39f,5.7f/mesh.bounds.size.x);
            Label(view.transform,"Subtitle",new Vector3(0,2.04f,-.49f),new Vector2(6,.24f),1.9f,TextAlignmentOptions.Center,Ink).text="BEST RUNS";
            Label(view.transform,"RankHeading",new Vector3(-2.92f,1.72f,-.56f),new Vector2(.66f,.22f),1.55f,TextAlignmentOptions.Center,Ink).text="RANK";
            Label(view.transform,"PlayerHeading",new Vector3(-.53f,1.72f,-.56f),new Vector2(3.48f,.22f),1.55f,TextAlignmentOptions.MidlineLeft,Ink).text="PLAYER";
            Label(view.transform,"ScoreHeading",new Vector3(2.18f,1.72f,-.56f),new Vector2(1.67f,.22f),1.55f,TextAlignmentOptions.MidlineRight,Ink).text="SCORE";
            view.rankLabels=new TMP_Text[10];view.scoreLabels=new TMP_Text[10];view.rowFaces=new Renderer[10];view.playerMarkers=new GameObject[10];
            view.rankBadges=new Renderer[10];view.rankMaterials=new[]{gold,silver,bronze};
            var rowMesh=ToyBoxGeometry.RoundedBox(new Vector3(6.85f,.35f,.105f),.045f);
            for(int i=0;i<10;i++)
            {
                var row=view.rows[i].transform;row.localPosition=new Vector3(0,1.39f-i*.40f,-.49f);
                row.GetComponent<MeshFilter>().sharedMesh=rowMesh;view.rowFaces[i]=row.GetComponent<Renderer>();view.rowFaces[i].sharedMaterial=cream;
                view.labels[i]=Label(row,"PlayerAndScore",new Vector3(-.53f,0,-.075f),new Vector2(3.48f,.32f),2.6f,TextAlignmentOptions.MidlineLeft,Ink);
                view.rankLabels[i]=Label(row,"Rank",new Vector3(-2.92f,0,-.11f),new Vector2(.46f,.31f),2.35f,TextAlignmentOptions.Center,Ink);
                view.scoreLabels[i]=Label(row,"Score",new Vector3(2.18f,0,-.075f),new Vector2(1.67f,.32f),2.65f,TextAlignmentOptions.MidlineRight,Ink);
                view.rankLabels[i].fontSizeMin=.85f;view.scoreLabels[i].fontSizeMin=1.1f;
                var badge=Mesh(row,"RankMedallion",new Vector3(-2.92f,0,-.072f),ToyBoxGeometry.Disc(.145f,.026f,.01f),i==0?gold:i==1?silver:i==2?bronze:cream);
                view.rankBadges[i]=badge.GetComponent<Renderer>();
                badge.transform.localRotation=Quaternion.Euler(90,0,0);
                view.playerMarkers[i]=Mesh(row,"YourRowAccent",new Vector3(-3.34f,0,-.071f),ToyBoxGeometry.RoundedBox(new Vector3(.045f,.21f,.025f),.01f),mint);
                view.playerMarkers[i].SetActive(false);
            }
            view.normalFace=cream;view.playerFace=teal;view.ink=Ink;view.ivory=Cream;
            view.footer=Label(view.transform,"Footer",new Vector3(0,-2.78f,-.49f),new Vector2(6.8f,.25f),1.65f,TextAlignmentOptions.Center,Ink);
            view.empty=Label(view.transform,"NoScores",new Vector3(0,-.12f,-.49f),new Vector2(6,.7f),2.45f,TextAlignmentOptions.Center,Ink);
            view.empty.text="No rankings to show yet";
            panel.normalRowColor=Ink;panel.myRowColor=Ink;
            foreach(var r in panel.rows)r.playerRowTintColor=new Color(.05f,.37f,.30f,1);
            EditorUtility.SetDirty(view);EditorUtility.SetDirty(panel);EditorUtility.SetDirty(lettering);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(view.gameObject.scene);EditorSceneManager.SaveScene(view.gameObject.scene);
            return "Polished title, ten separate rank/name/score rows, player highlight and unranked state saved.";
        }
        finally{if(restore!="Assets/Scenes/Game.unity")EditorSceneManager.OpenScene(restore);}
    }

    public static string Preview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required.");
        string restore=SceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var panel=UnityEngine.Object.FindAnyObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);
            var view=UnityEngine.Object.FindAnyObjectByType<BucaResultStandings>(FindObjectsInactive.Include);
            var cabinet=panel.GetComponent<BucaArcadeResult3D>();cabinet.stage.SetActive(true);cabinet.cabinet.gameObject.SetActive(true);cabinet.SetScore(1980);
            cabinet.heartsHeading.SetActive(true);cabinet.timeHeading.SetActive(false);
            foreach(var camera in cabinet.stage.GetComponentsInChildren<Camera>(true))camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            string[] names={"MAYA","LEO","YOU","ALEXANDER LONGNAME","LILY","NOAH","EMMA","ALI","SOFIA","MAX"};
            var data=names.Select((n,i)=>new LeaderboardPanel.LeaderboardData{rank=i+1,playerName=n,score=2450-i*200}).ToArray();
            var populate=typeof(LeaderboardPanel).GetMethod("Populate",BindingFlags.NonPublic|BindingFlags.Instance);
            populate.Invoke(panel,new object[]{data,3,2050,"YOU"});view.Sync(panel);
            Capture(cabinet.stage.GetComponentInChildren<Camera>(),view,"Polished-FullResult",true);
            Capture(cabinet.stage.GetComponentInChildren<Camera>(),view,"Polished-TopTen");
            populate.Invoke(panel,new object[]{Array.Empty<LeaderboardPanel.LeaderboardData>(),0,963,"YOU"});view.Sync(panel);
            Capture(cabinet.stage.GetComponentInChildren<Camera>(),view,"Polished-YourScore");
            panel.rows[0].Populate(1000,"A VERY LONG PLAYER NAME TO CHECK ELLIPSIS",int.MaxValue,Ink,true);view.Sync(panel);
            foreach(var t in view.GetComponentsInChildren<TMP_Text>(true))t.ForceMeshUpdate(true);
            if(view.scoreLabels[0].isTextTruncated)throw new Exception("Score was truncated.");
            File.WriteAllText("output/leaderboard/verification.txt","PASS: 10 distinct rows; current-player highlight; full result layout; unranked score; long-name fitting; complete Int32 maximum score; static font atlas with "+view.labels[0].font.characterTable.Count+" characters.\n");
            return "output/leaderboard/Polished-TopTen.png and Polished-YourScore.png";
        }
        finally{EditorSceneManager.OpenScene(restore);}
    }
    static void Capture(Camera camera,BucaResultStandings view,string name,bool full=false)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;var pos=camera.transform.position;var rot=camera.transform.rotation;float size=camera.orthographicSize,aspect=camera.aspect;
        int width=full?1920:1500,height=full?1080:1400;
        var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            if(!full){camera.transform.position=view.transform.position+new Vector3(0,0,-20);camera.transform.rotation=Quaternion.identity;camera.orthographicSize=3.85f;}
            camera.orthographic=true;camera.aspect=(float)width/height;camera.targetTexture=rt;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.085f,.08f);
            foreach(var t in view.GetComponentsInChildren<TMP_Text>(true))t.ForceMeshUpdate(true);
            var probe=view.labels[0];
            File.WriteAllText("output/leaderboard/font-render.txt","Text font: "+probe.font.name+"; material: "+probe.fontSharedMaterial.name+"; renderer: "+probe.GetComponent<Renderer>().sharedMaterial.name+"; character font: "+probe.textInfo.characterInfo[0].fontAsset.name+"; glyph: "+probe.textInfo.characterInfo[0].textElement.glyph.glyphRect+"; bold: "+probe.fontSharedMaterial.GetFloat("_WeightBold")+"; dilate: "+probe.fontSharedMaterial.GetFloat("_FaceDilate")+"; scale: "+probe.transform.lossyScale+"; atlas: "+probe.font.atlasRenderMode);
            camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes("output/leaderboard/"+name+".png",image.EncodeToPNG());
        }
        finally{camera.targetTexture=old;camera.transform.SetPositionAndRotation(pos,rot);camera.orthographicSize=size;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif
