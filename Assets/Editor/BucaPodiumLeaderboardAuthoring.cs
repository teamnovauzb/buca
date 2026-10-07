#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>In-place theme corrections using the same geometry as BUCA's cabinet.</summary>
public static class BucaPodiumLeaderboardAuthoring
{
    const string Dir="Assets/Materials/TrainLeaderboard3D/";
    static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 position)
    {
        var child=parent.Find(name);
        var go=child!=null?child.gameObject:new GameObject(name);
        go.transform.SetParent(parent,false);go.layer=31;go.transform.localPosition=position;
        go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
        var filter=go.GetComponent<MeshFilter>();if(filter==null)filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        var r=go.GetComponent<MeshRenderer>();if(r==null)r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        r.receiveShadows=false;
        var collider=go.GetComponent<Collider>();if(collider!=null)Object.DestroyImmediate(collider);
        return go;
    }
    static Material Material(string name,Color color,float metal=0,float gloss=.35f,bool photo=false)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+name+".mat");
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Dir+name+".mat");}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",gloss);
        m.SetTexture("_BaseMap",photo?AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"):null);
        m.SetTextureScale("_BaseMap",Vector2.one);
        EditorUtility.SetDirty(m);return m;
    }
    static void Disc(Transform parent,string name,Vector3 position,float radius,float depth,Material material)
    {
        var go=Part(parent,name,ToyBoxGeometry.Disc(radius,depth,Mathf.Min(.065f,depth*.45f)),material,position);
        go.transform.localRotation=Quaternion.Euler(90,0,0);
        var oldFace=parent.Find(name+"RoundedFace");if(oldFace!=null)oldFace.gameObject.SetActive(false);
    }
    static void Ring(Transform parent,string name,Vector3 position,float radius,float tube,Material material)
    {
        Part(parent,name,ToyBoxGeometry.Torus(radius,tube),material,position).transform.localRotation=Quaternion.Euler(90,0,0);
    }
    static void Lettering(Transform parent,string name,string value,Vector3 position,float height,Material material)
    {
        var go=Part(parent,name,ToyBoxGeometry.Text(value,.20f,.025f,.045f),material,position);
        go.transform.localScale=Vector3.one*height;
    }
    static Material ShadowMaterial()
    {
        var tex=new Texture2D(128,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<128;x++)
        {
            float dx=Mathf.Max(Mathf.Abs((x+.5f)/128f-.5f)-.34f,0)/.16f;
            float dy=Mathf.Max(Mathf.Abs((y+.5f)/64f-.5f)-.17f,0)/.33f;
            float a=Mathf.Exp(-3*(dx*dx+dy*dy))*.30f;
            a*=Mathf.Clamp01((.5f-Mathf.Abs((x+.5f)/128f-.5f))*20)*Mathf.Clamp01((.5f-Mathf.Abs((y+.5f)/64f-.5f))*20);
            tex.SetPixel(x,y,new Color(.11f,.065f,.025f,a));
        }
        tex.Apply();tex=ToyBoxGeometry.Save(tex,Dir+"SoftContactShadow.asset");
        var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+"SoftContactShadow.mat");
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,Dir+"SoftContactShadow.mat");}
        m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",Color.white);m.SetFloat("_Surface",1);
        m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;EditorUtility.SetDirty(m);return m;
    }
    static void Shadow(Transform parent,string name,Vector3 pos,Vector2 size,Material material)
    {
        var t=parent.Find(name);var go=t!=null?t.gameObject:GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;go.layer=31;go.transform.SetParent(parent,false);
        go.transform.localPosition=pos;go.transform.localRotation=Quaternion.identity;go.transform.localScale=new Vector3(size.x,size.y,1);
        if(go.GetComponent<Collider>()!=null)Object.DestroyImmediate(go.GetComponent<Collider>());
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
    }
    static Transform Field(Transform parent,string name,Vector3 position,Material material,int count=24)
    {
        var existing=parent.Find(name);var t=existing!=null?existing:new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;
        for(int i=0;i<count;i++)Part(t,"Glyph"+i,null,material,Vector3.zero);
        return t;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
        var panel=Object.FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.path=="Assets/Scenes/Game.unity");
        var old=panel.GetComponent<BucaTrainLeaderboard3D>();old.enabled=false;
        var view=panel.GetComponent<BucaPodiumLeaderboard3D>();if(view==null)view=panel.gameObject.AddComponent<BucaPodiumLeaderboard3D>();
        view.panel=panel;view.stage=old.stage;var root=old.stage.transform;
        ToyBoxGeometry.Initialize();
        foreach(Transform t in root)if(t.GetComponent<Camera>()==null&&t.GetComponent<Light>()==null&&t.name!="TealArchedBoard"&&t.name!="MapleArchedInset"&&t.name!="PuckPodiumPresentation")t.gameObject.SetActive(false);
        var found=root.Find("PuckPodiumPresentation");var content=found!=null?found:new GameObject("PuckPodiumPresentation").transform;content.SetParent(root,false);
        var cream=AssetDatabase.LoadAssetAtPath<Material>(Dir+"CreamMaple.mat");var maple=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Maple.mat");
        var teal=AssetDatabase.LoadAssetAtPath<Material>(Dir+"PaintedTeal.mat");var navy=AssetDatabase.LoadAssetAtPath<Material>(Dir+"RaisedNavyLetters.mat");
        var gold=Material("PodiumGold",new Color(.83f,.55f,.17f),.3f,.45f);
        var silver=Material("PodiumSilver",new Color(.66f,.72f,.73f),.35f,.45f);
        var bronze=Material("PodiumBronze",new Color(.65f,.37f,.20f),.3f,.4f);
        var mint=Material("PodiumMint",new Color(.42f,.78f,.62f),.05f,.4f);
        view.cream=cream;view.mint=mint;view.navy=navy;view.gold=gold;view.silver=silver;view.bronze=bronze;view.muted=teal;
        view.alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.,!'#()";
        view.glyphs=view.alphabet.Select(c=>ToyBoxGeometry.Text(c.ToString(),.20f,.025f,.045f)).ToArray();
        Lettering(content,"BUCA","BUCA",new Vector3(0,4.3f,-.4f),1.15f,navy);
        Lettering(content,"TopScores","TOP SCORES",new Vector3(0,3.3f,-.4f),.52f,navy);
        Part(content,"StatusPlaque",ToyBoxGeometry.RoundedBox(new Vector3(5.7f,.73f,.24f),.16f),cream,new Vector3(0,2.5f,-.4f));
        view.status=Field(content,"Status",new Vector3(.45f,2.5f,-.56f),navy);
        Ring(content,"ClockRim",new Vector3(-2.12f,2.5f,-.57f),.26f,.033f,navy);
        Part(content,"ClockHour",ToyBoxGeometry.RoundedBox(new Vector3(.045f,.19f,.04f),.02f),navy,new Vector3(-2.12f,2.57f,-.59f));
        Part(content,"ClockMinute",ToyBoxGeometry.RoundedBox(new Vector3(.17f,.045f,.04f),.02f),navy,new Vector3(-2.06f,2.5f,-.59f));
        Part(content,"Shelf",ToyBoxGeometry.RoundedBox(new Vector3(8.65f,.4f,2.8f),.14f),maple,new Vector3(0,-1.35f,-.55f));
        Part(content,"LowerCabinet",ToyBoxGeometry.RoundedBox(new Vector3(8.5f,2.25f,.5f),.2f),maple,new Vector3(0,-2.68f,-.3f));
        view.names=new Transform[5];view.scores=new Transform[5];view.ranks=new Transform[5];view.faces=new Renderer[5];view.bodies=new Renderer[3];view.caps=new Renderer[3];
        view.clockParts=new[]{content.Find("ClockRim").gameObject,content.Find("ClockHour").gameObject,content.Find("ClockMinute").gameObject};
        for(int i=0;i<5;i++)
        {
            if(i<3)
            {
                float x=i==0?0:i==1?-2.7f:2.7f;float h=i==0?2.25f:i==1?1.65f:1.45f;float bottom=-1.12f;
                Material metal=i==0?gold:i==1?silver:bronze;
                view.bodies[i]=Part(content,"Puck"+i,ToyBoxGeometry.Disc(1.28f,h,.12f),metal,new Vector3(x,bottom+h*.5f,-.55f)).GetComponent<Renderer>();
                view.caps[i]=Part(content,"PuckTop"+i,ToyBoxGeometry.Disc(1.20f,.10f,.04f),metal,new Vector3(x,bottom+h,-.55f)).GetComponent<Renderer>();
                view.faces[i]=Part(content,"Nameplate"+i,ToyBoxGeometry.RoundedBox(new Vector3(2.16f,1.12f,.16f),.17f),cream,new Vector3(x,bottom+.67f,-1.78f)).GetComponent<Renderer>();
                view.names[i]=Field(content,"Name"+i,new Vector3(x,bottom+.91f,-1.88f),navy);
                view.scores[i]=Field(content,"Score"+i,new Vector3(x,bottom+.43f,-1.88f),navy);
                float medalY=bottom+h+.51f;
                Disc(content,"Medal"+i,new Vector3(x,medalY,-.62f),.64f,.22f,metal);
                Ring(content,"MedalRim"+i,new Vector3(x,medalY,-.76f),.565f,.025f,cream);
                view.ranks[i]=Field(content,"Rank"+i,new Vector3(x,medalY,-.80f),navy,6);
            }
            else
            {
                float y=i==3?-2.1f:-3.13f;
                Part(content,"RowTrim"+i,ToyBoxGeometry.RoundedBox(new Vector3(7.45f,.89f,.22f),.15f),gold,new Vector3(0,y,-.7f));
                view.faces[i]=Part(content,"Row"+i,ToyBoxGeometry.RoundedBox(new Vector3(7.3f,.76f,.18f),.14f),cream,new Vector3(0,y,-.85f)).GetComponent<Renderer>();
                Disc(content,"Medal"+i,new Vector3(-3.04f,y,-.99f),.37f,.13f,cream);
                view.ranks[i]=Field(content,"Rank"+i,new Vector3(-3.04f,y,-1.09f),navy,6);
                view.names[i]=Field(content,"Name"+i,new Vector3(-.67f,y,-.96f),navy);
                view.scores[i]=Field(content,"Score"+i,new Vector3(2.6f,y,-.96f),navy);
            }
        }
        view.footer=Field(content,"Footer",new Vector3(0,-4.13f,-.4f),navy,80);
        view.dots=new Renderer[5];for(int i=0;i<5;i++){Disc(content,"Dot"+i,new Vector3((i-2)*.43f,-4.64f,-.4f),.115f,.09f,mint);view.dots[i]=content.Find("Dot"+i).GetComponent<Renderer>();}
        for(int i=0;i<4;i++)Disc(content,"Pin"+i,new Vector3(i%2==0?-4.2f:4.2f,i<2?3.93f:-4.8f,-.3f),.13f,.12f,gold);
        var camera=root.GetComponentInChildren<Camera>(true);camera.transform.localPosition=new Vector3(0,3.1f,-20);camera.transform.localRotation=Quaternion.Euler(8.8f,0,0);camera.orthographicSize=5.65f;
        foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))text.enabled=false;
        foreach(var row in panel.rows)row.Clear();panel.myRankText.text="";view.Sync(panel);view.stage.SetActive(false);EditorUtility.SetDirty(view);EditorUtility.SetDirty(old);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);
        return "Puck podium with real extruded lettering saved.";
    }
    public static string CapturePreview()
    {
        var original=Object.FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.path=="Assets/Scenes/Game.unity");
        var view=original.GetComponent<BucaPodiumLeaderboard3D>();
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
            clone.SetActive(true);panel.enabled=false;clone.GetComponent<BucaPodiumLeaderboard3D>().enabled=false;
            string[] names={"MAYA","LEO","YOU","AMIR","LILY"};int[] scores={2450,2180,1980,1760,1520};
            var entries=Enumerable.Range(0,5).Select(i=>new LeaderboardPanel.LeaderboardData{rank=i+1,playerName=names[i],score=scores[i]}).ToArray();
            typeof(LeaderboardPanel).GetMethod("Populate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(panel,new object[]{entries,3,1980,"YOU"});
            panel.titleText.text="TIME'S UP!";panel.myRankText.text="SAMPLE SCORES - VISUAL CHECK";panel.group.alpha=1;panel.card.localScale=Vector3.one;
            view.stage.SetActive(true);view.Sync(panel);
            view.stage.GetComponentInChildren<Camera>().Render();
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
            output=new Texture2D(1920,1080,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1920,1080),0,0);output.Apply();
            System.IO.Directory.CreateDirectory("output/leaderboard");System.IO.File.WriteAllBytes("output/leaderboard/BUCA-Podium-3D-Preview.png",output.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(canvasGO);Object.DestroyImmediate(cameraGO);texture.Release();Object.DestroyImmediate(texture);if(output!=null)Object.DestroyImmediate(output);
            view.Sync(original);view.stage.SetActive(stageActive);
        }
        return "output/leaderboard/BUCA-Podium-3D-Preview.png";
    }
}
#endif
