#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class BucaArcadeResultBaker
{
    const string Dir="Assets/Materials/TrainLeaderboard3D/";
    static Material Mat(string name,Color color,float metal=0,float gloss=.4f)
    {
        string path=Dir+"Arcade"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",gloss);EditorUtility.SetDirty(m);return m;
    }
    static GameObject Part(Transform p,string n,Mesh mesh,Material mat,Vector3 pos)
    {
        var go=new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));go.layer=31;go.transform.SetParent(p,false);go.transform.localPosition=pos;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
    }
    static GameObject Box(Transform p,string n,Vector3 size,Vector3 pos,Material m,float bevel=.15f)
    {
        var meshMethod=typeof(BucaTrainLeaderboard3DBaker).GetMethod("Mesh",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        var mesh=(Mesh)meshMethod.Invoke(null,new object[]{size.x,size.y,size.z,Mathf.Min(n.StartsWith("DigitTile")? .07f:bevel*2.5f,Mathf.Min(size.x,size.y)*.45f),false});
        mesh=ToyBoxGeometry.Save(mesh,Dir+"MapleConsole_"+n+".asset");
        return Part(p,n,mesh,m,pos);
    }
    static GameObject Text(Transform p,string n,string text,Vector3 pos,float height,float width,Material mat)
    {
        var mesh=ToyBoxGeometry.Text(text,.09f,.025f,.055f);var go=Part(p,n,mesh,mat,pos);
        go.transform.localScale=Vector3.one*Mathf.Min(height,width/Mathf.Max(.01f,mesh.bounds.size.x));return go;
    }
    static void Disc(Transform p,string n,Vector3 pos,float r,float h,Material m)
    {Part(p,n,ToyBoxGeometry.Disc(r,h,.055f),m,pos).transform.localRotation=Quaternion.Euler(90,0,0);}
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
        var panel=Object.FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.path=="Assets/Scenes/Game.unity");
        if(panel.rows.Length<10)
        {
            var dataRows=panel.rows.ToList();
            while(dataRows.Count<10){var row=Object.Instantiate(panel.rows[0],panel.rows[0].transform.parent);row.name="EntryRow"+(dataRows.Count+1);row.gameObject.SetActive(false);dataRows.Add(row);}
            panel.rows=dataRows.ToArray();
        }
        ToyBoxGeometry.Initialize();
        var old=panel.GetComponent<BucaPodiumLeaderboard3D>();old.enabled=false;
        var train=panel.GetComponent<BucaTrainLeaderboard3D>();train.enabled=false;
        var stage=old.stage.transform;
        foreach(Transform child in stage)if(child.GetComponent<Camera>()==null&&child.GetComponent<Light>()==null)child.gameObject.SetActive(false);
        var existing=stage.Find("ArcadeResultCabinet");if(existing!=null)Object.DestroyImmediate(existing.gameObject);
        var root=new GameObject("ArcadeResultCabinet").transform;root.SetParent(stage,false);
        var view=panel.GetComponent<BucaArcadeResult3D>();if(view==null)view=panel.gameObject.AddComponent<BucaArcadeResult3D>();view.enabled=true;view.panel=panel;view.stage=stage.gameObject;view.cabinet=root;
        var teal=Mat("Teal",new Color(.035f,.23f,.23f),.10f,.4f);
        var cream=Mat("Enamel",new Color(.92f,.82f,.60f),.03f,.31f);
        var navy=Mat("Ink",new Color(.009f,.035f,.062f),.02f,.3f);
        var brass=Mat("Brass",new Color(.75f,.45f,.12f),.65f,.55f);
        var coral=Mat("Coral",new Color(.60f,.14f,.07f),.04f,.35f);
        var green=Mat("Green",new Color(.02f,.43f,.13f),.18f,.82f);
        var red=Mat("Red",new Color(.65f,.015f,.018f),.18f,.82f);
        var metal=Mat("Chrome",new Color(.45f,.52f,.53f),.75f,.65f);
        var maple=Mat("WarmMaple",new Color(.76f,.73f,.62f),0,.30f);
        maple.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));
        EditorUtility.SetDirty(maple);
        Box(root,"Cabinet",new Vector3(10.0f,5.3f,.65f),new Vector3(0,1.15f,.12f),teal,.45f);
        Box(root,"InsetBrass",new Vector3(9.45f,4.75f,.22f),new Vector3(0,1.2f,-.27f),brass,.36f);
        Box(root,"MapleInset",new Vector3(9.25f,4.55f,.25f),new Vector3(0,1.2f,-.42f),maple,.34f);
        Box(root,"BrandTab",new Vector3(3.9f,1.03f,.42f),new Vector3(0,3.72f,-.36f),maple,.22f);
        Text(root,"Brand","BUCA",new Vector3(0,3.72f,-.65f),.65f,2.8f,navy);
        view.heartsHeading=Text(root,"HeartsHeading","OUT OF HEARTS",new Vector3(0,2.45f,-.64f),.72f,8.1f,navy);
        view.timeHeading=Text(root,"TimeHeading","TIME'S UP",new Vector3(0,2.45f,-.64f),.70f,8.1f,navy);view.timeHeading.SetActive(false);
        var counter=new GameObject("ScoreCounter").transform;counter.SetParent(root,false);counter.localPosition=new Vector3(0,.6f,-.58f);view.scoreHousing=counter;
        Box(counter,"DisplayTrim",new Vector3(8.1f,2.5f,.22f),Vector3.zero,brass,.25f);
        Box(counter,"TealDisplay",new Vector3(7.94f,2.34f,.20f),new Vector3(0,0,-.14f),teal,.23f);
        Text(counter,"ScoreLabel","YOUR SCORE",new Vector3(0,.76f,-.28f),.38f,4.7f,cream);
        for(int side=-1;side<=1;side+=2)
            Box(counter,"ScoreAccent"+side,new Vector3(1.05f,.055f,.045f),new Vector3(side*2.9f,.76f,-.29f),brass,.02f);
        Box(counter,"CounterRecess",new Vector3(6.5f,1.4f,.08f),new Vector3(0,-.22f,-.24f),teal,.13f);
        // Ten slots cover every nonnegative Int32 score; leading slots are hidden below.
        view.numeralMeshes=Enumerable.Range(0,10).Select(i=>ToyBoxGeometry.Text(i.ToString(),.09f,.025f,.05f)).ToArray();
        view.digits=new MeshFilter[10];
        for(int i=0;i<10;i++)
        {
            var tile=Box(counter,"DigitTile"+i,new Vector3(.50f,1.20f,.19f),new Vector3((i-4.5f)*.55f,-.22f,-.34f),cream,.08f);
            var digit=Part(tile.transform,"Digit",view.numeralMeshes[0],navy,new Vector3(0,0,-.10f));digit.transform.localScale=Vector3.one*.52f;view.digits[i]=digit.GetComponent<MeshFilter>();
        }
        for(int i=0;i<4;i++)Disc(root,"CornerBolt"+i,new Vector3(i%2==0?-4.25f:4.25f,i<2?3.03f:-1.20f,-.60f),.12f,.11f,brass);
        var camera=stage.GetComponentInChildren<Camera>(true);camera.transform.localPosition=new Vector3(0,1.7f,-20);camera.transform.localRotation=Quaternion.Euler(4.86f,0,0);camera.orthographicSize=6.05f;
        panel.fadeInDuration=.7f;panel.autoAdvanceSeconds=6f;panel.arcadeResult=true;
        foreach(var t in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))t.enabled=false;
        foreach(var name in new[]{"RetryHit","LevelsHit"}){var t=panel.card.Find(name);if(t!=null)Object.DestroyImmediate(t.gameObject);}
        view.leaderboard=old;
        view.leaderboardObjects=new[]{stage.Find("TealArchedBoard").gameObject,stage.Find("MapleArchedInset").gameObject,stage.Find("PuckPodiumPresentation").gameObject};
        foreach(var item in view.leaderboardObjects)item.SetActive(false);
        panel.autoAdvanceSeconds=8f;
        var prior=stage.Find("CombinedStandings");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
        var standings=new GameObject("CombinedStandings").transform;standings.SetParent(stage,false);standings.localPosition=new Vector3(4.05f,.75f,0);
        Box(standings,"StandingsFrame",new Vector3(8.1f,6.9f,.45f),Vector3.zero,teal,.36f);
        Box(standings,"StandingsMaple",new Vector3(7.7f,6.5f,.24f),new Vector3(0,0,-.3f),maple,.30f);
        Text(standings,"StandingsTitle","LEADERBOARD",new Vector3(0,2.65f,-.48f),.44f,6.8f,navy);
        var updater=standings.gameObject.AddComponent<BucaResultStandings>();updater.panel=panel;updater.rows=new GameObject[10];updater.labels=new TMPro.TMP_Text[10];
        var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        for(int i=0;i<10;i++)
        {
            float y=1.90f-i*.51f;
            var row=Box(standings,"TopTenRow"+i,new Vector3(6.85f,.45f,.12f),new Vector3(0,y,-.49f),cream,.10f);updater.rows[i]=row;
            var label=new GameObject("PlayerAndScore",typeof(TMPro.TextMeshPro));label.layer=31;label.transform.SetParent(row.transform,false);label.transform.localPosition=new Vector3(0,0,-.12f);
            var text=label.GetComponent<TMPro.TextMeshPro>();text.font=font;text.fontSize=3.5f;text.color=new Color(.01f,.08f,.10f);text.alignment=TMPro.TextAlignmentOptions.MidlineLeft;text.rectTransform.sizeDelta=new Vector2(6.25f,.43f);text.enableWordWrapping=false;updater.labels[i]=text;
        }
        var emptyGO=new GameObject("NoScores",typeof(TMPro.TextMeshPro));emptyGO.layer=31;emptyGO.transform.SetParent(standings,false);emptyGO.transform.localPosition=new Vector3(0,0,-.5f);
        updater.empty=emptyGO.GetComponent<TMPro.TextMeshPro>();updater.empty.font=font;updater.empty.fontSize=3;updater.empty.color=new Color(.01f,.08f,.1f);updater.empty.alignment=TMPro.TextAlignmentOptions.Center;updater.empty.rectTransform.sizeDelta=new Vector2(6.5f,1.5f);updater.empty.text="NO SCORES YET";
        root.localPosition=new Vector3(-4.7f,0,0);root.localScale=Vector3.one*.75f;
        camera.orthographicSize=5.3f;
        var target=camera.targetTexture;target.Release();target.width=2400;target.height=1350;target.Create();EditorUtility.SetDirty(target);
        panel.card.sizeDelta=new Vector2(1720,960);
        view.finalChime=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level/StarReveal_2.ogg");
        stage.gameObject.SetActive(false);EditorUtility.SetDirty(view);EditorUtility.SetDirty(panel);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);return "Arcade result cabinet saved";
    }
    public static string CapturePreview()
    {
        var original=Object.FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.path=="Assets/Scenes/Game.unity");
        var view=original.GetComponent<BucaArcadeResult3D>();
        var sourceCamera=original.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
        var cameraGO=new GameObject("LeaderboardPreviewCamera",typeof(Camera));var camera=cameraGO.GetComponent<Camera>();camera.CopyFrom(sourceCamera);camera.transform.SetPositionAndRotation(sourceCamera.transform.position,sourceCamera.transform.rotation);
        var canvasGO=new GameObject("LeaderboardPreviewCanvas",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));var canvas=canvasGO.GetComponent<Canvas>();
        var texture=new RenderTexture(1920,1080,24);camera.targetTexture=texture;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=200;
        var scaler=canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();var oldScaler=original.GetComponentInParent<Canvas>().GetComponent<UnityEngine.UI.CanvasScaler>();
        EditorUtility.CopySerialized(oldScaler,scaler);
        var clone=Object.Instantiate(original.gameObject,canvasGO.transform);var panel=clone.GetComponent<LeaderboardPanel>();
        bool stageActive=view.stage.activeSelf;

        var previous=RenderTexture.active;Texture2D output=null;
        try
        {
            clone.SetActive(true);panel.enabled=false;clone.GetComponent<BucaArcadeResult3D>().enabled=false;
            string[] names={"MAYA","LEO","YOU","AMIR","LILY","NOAH","EMMA","ALI","SOFIA","MAX"};int[] scores={2450,2180,1980,1760,1520,1400,1250,1100,950,800};
            var entries=Enumerable.Range(0,10).Select(i=>new LeaderboardPanel.LeaderboardData{rank=i+1,playerName=names[i],score=scores[i]}).ToArray();
            typeof(LeaderboardPanel).GetMethod("Populate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(panel,new object[]{entries,3,1980,"YOU"});
            panel.titleText.text="OUT OF HEARTS!";panel.myRankText.text="SAMPLE SCORES - VISUAL CHECK";panel.group.alpha=1;panel.card.localScale=Vector3.one;
            view.stage.SetActive(true);view.SetScore(5481);
            view.stage.GetComponentInChildren<BucaResultStandings>(true).Sync(panel);
            view.stage.GetComponentInChildren<Camera>().Render();
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
            output=new Texture2D(1920,1080,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1920,1080),0,0);output.Apply();
            System.IO.Directory.CreateDirectory("output/leaderboard");System.IO.File.WriteAllBytes("output/leaderboard/BUCA-Arcade-Cabinet.png",output.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(canvasGO);Object.DestroyImmediate(cameraGO);texture.Release();Object.DestroyImmediate(texture);if(output!=null)Object.DestroyImmediate(output);
            view.SetScore(0);view.stage.SetActive(stageActive);
        }
        return "output/leaderboard/BUCA-Arcade-Cabinet.png";
    }

}
#endif
