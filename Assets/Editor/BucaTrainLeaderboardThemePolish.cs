#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>In-place theme corrections using the same geometry as BUCA's cabinet.</summary>
public static class BucaTrainLeaderboardThemePolish
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
    public static string CapturePreview()
    {
        var original=Object.FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.path=="Assets/Scenes/Game.unity");
        var view=original.GetComponent<BucaTrainLeaderboard3D>();
        var sourceCamera=original.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
        var cameraGO=new GameObject("LeaderboardPreviewCamera",typeof(Camera));var camera=cameraGO.GetComponent<Camera>();camera.CopyFrom(sourceCamera);camera.transform.SetPositionAndRotation(sourceCamera.transform.position,sourceCamera.transform.rotation);
        var canvasGO=new GameObject("LeaderboardPreviewCanvas",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));var canvas=canvasGO.GetComponent<Canvas>();
        var texture=new RenderTexture(1920,1080,24);camera.targetTexture=texture;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=200;
        var scaler=canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();var oldScaler=original.GetComponentInParent<Canvas>().GetComponent<UnityEngine.UI.CanvasScaler>();
        EditorUtility.CopySerialized(oldScaler,scaler);
        var clone=Object.Instantiate(original.gameObject,canvasGO.transform);var panel=clone.GetComponent<LeaderboardPanel>();
        bool stageActive=view.stage.activeSelf;
        var active=view.carriages.Select(c=>c.activeSelf).ToArray();var materials=view.faces.Select(f=>f.sharedMaterial).ToArray();
        var previous=RenderTexture.active;Texture2D output=null;
        try
        {
            clone.SetActive(true);panel.enabled=false;clone.GetComponent<BucaTrainLeaderboard3D>().enabled=false;
            string[] names={"MAYA","LEO","YOU","AMIR","LILY"};int[] scores={2450,2180,1980,1760,1520};
            var entries=Enumerable.Range(0,5).Select(i=>new LeaderboardPanel.LeaderboardData{rank=i+1,playerName=names[i],score=scores[i]}).ToArray();
            typeof(LeaderboardPanel).GetMethod("Populate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(panel,new object[]{entries,3,1980,"YOU"});
            panel.titleText.text="TIME'S UP!";panel.myRankText.text="SAMPLE SCORES - VISUAL CHECK";panel.group.alpha=1;panel.card.localScale=Vector3.one;
            view.stage.SetActive(true);for(int i=0;i<5;i++){view.carriages[i].SetActive(true);view.faces[i].sharedMaterial=i==2?view.mint:view.maple;view.playerEdges[i].SetActive(i==2);}
            view.stage.GetComponentInChildren<Camera>().Render();
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
            output=new Texture2D(1920,1080,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1920,1080),0,0);output.Apply();
            System.IO.Directory.CreateDirectory("output/leaderboard");System.IO.File.WriteAllBytes("output/leaderboard/BUCA-Train-3D-Preview.png",output.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(canvasGO);Object.DestroyImmediate(cameraGO);texture.Release();Object.DestroyImmediate(texture);if(output!=null)Object.DestroyImmediate(output);
            for(int i=0;i<5;i++){view.carriages[i].SetActive(active[i]);view.faces[i].sharedMaterial=materials[i];view.playerEdges[i].SetActive(false);}view.stage.SetActive(stageActive);
        }
        return "output/leaderboard/BUCA-Train-3D-Preview.png";
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Apply only in Edit Mode.");
        var panel=Object.FindObjectsByType<LeaderboardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.path=="Assets/Scenes/Game.unity");
        var view=panel.GetComponent<BucaTrainLeaderboard3D>();var root=view.stage.transform;
        ToyBoxGeometry.Initialize();
        var maple=Material("Maple",new Color(.63f,.72f,.83f),0,.32f,true);
        var face=Material("CreamMaple",new Color(.75f,.83f,.93f),0,.38f,true);
        var teal=Material("PaintedTeal",new Color(.06f,.24f,.23f),.06f,.4f);
        var mint=Material("MintPlayer",new Color(.25f,.47f,.40f),.02f,.4f);
        var walnut=Material("WalnutWheels",new Color(.35f,.27f,.18f),0,.38f,true);
        var gold=Material("Gold",new Color(.61f,.40f,.15f),.45f,.48f);
        var silver=Material("Silver",new Color(.48f,.51f,.51f),.5f,.48f);
        var bronze=Material("Bronze",new Color(.46f,.25f,.13f),.38f,.42f);
        var navy=Material("RaisedNavyLetters",new Color(.014f,.075f,.17f),.04f,.38f);
        var glow=Material("PlayerEdge",new Color(.12f,.58f,.50f),.08f,.45f);
        var shadows=ShadowMaterial();
        foreach(var material in new[]{maple,face,mint})
        {material.SetFloat("_ReceiveShadows",0);material.EnableKeyword("_RECEIVE_SHADOWS_OFF");EditorUtility.SetDirty(material);}
        navy.EnableKeyword("_EMISSION");navy.SetColor("_EmissionColor",new Color(.004f,.012f,.03f));

        foreach(string board in new[]{"TealArchedBoard","MapleArchedInset"})root.Find(board).GetComponent<Renderer>().receiveShadows=false;
        Lettering(root,"SculptedBUCA","BUCA",new Vector3(0,4.53f,-.42f),1.08f,navy);
        Lettering(root,"SculptedChampions","CHAMPIONS",new Vector3(0,3.48f,-.34f),.46f,navy);
        for(int side=-1;side<=1;side+=2)
        {
            Part(root,"TitleDashLong"+side,ToyBoxGeometry.RoundedBox(new Vector3(.55f,.1f,.10f),.045f),gold,new Vector3(side*3.15f,4.50f,-.27f));
            Part(root,"TitleDashShort"+side,ToyBoxGeometry.RoundedBox(new Vector3(.38f,.1f,.10f),.045f),gold,new Vector3(side*3.22f,4.23f,-.27f));
        }
        view.playerEdges=new GameObject[5];
        for(int i=0;i<5;i++)
        {
            var car=view.carriages[i].transform;
            Part(car,"CarTrim"+i,ToyBoxGeometry.RoundedBox(new Vector3(6.60f,1.08f,.36f),.14f),i==0?gold:teal,new Vector3(.57f,0,-.12f));
            view.faces[i]=Part(car,"CarFace"+i,ToyBoxGeometry.RoundedBox(new Vector3(6.40f,.87f,.24f),.105f),face,new Vector3(.57f,.025f,-.34f)).GetComponent<Renderer>();
            Disc(car,"Medal"+i,new Vector3(-3.59f,0,-.23f),.53f,.28f,i==0?gold:i==1?silver:i==2?bronze:maple);
            Ring(car,"MedalRim"+i,new Vector3(-3.59f,0,-.388f),.455f,.025f,i==0?gold:i==1?silver:i==2?bronze:maple);
            var edge=Part(car,"PlayerOutline",ToyBoxGeometry.RoundedBox(new Vector3(6.75f,1.20f,.16f),.075f),glow,new Vector3(.57f,0,.02f));
            view.playerEdges[i]=edge;edge.SetActive(false);
            for(int wheel=0;wheel<2;wheel++)
            {
                float x=wheel==0?-2.13f:3.26f;
                Disc(car,"Wheel"+i+x,new Vector3(x,-.49f,-.46f),.29f,.28f,walnut);
                Ring(car,"WheelGroove"+wheel,new Vector3(x,-.49f,-.608f),.22f,.018f,bronze);
                Disc(car,"Hub"+i+x,new Vector3(x,-.49f,-.63f),.12f,.09f,maple);
            }
            for(int j=0;j<3;j++) Disc(car,"CouplerCollar"+j,new Vector3(-2.85f-j*.14f,0,-.15f),.17f,.1f,bronze);
            Shadow(car,"CarContact",new Vector3(.60f,-.11f,.075f),new Vector2(7.05f,1.58f),shadows);
            Shadow(car,"MedalContact",new Vector3(-3.56f,-.09f,.075f),new Vector2(1.30f,1.30f),shadows);
        }
        view.maple=face;view.mint=mint;
        panel.sculptedHeading=true;
        var title=panel.titleText;title.rectTransform.anchoredPosition=new Vector2(0,304);title.rectTransform.sizeDelta=new Vector2(800,45);title.fontSize=27;title.lineSpacing=0;title.text="TIME'S UP!";
        foreach(var row in panel.rows){row.nameText.fontStyle=TMPro.FontStyles.Bold;row.scoreText.fontStyle=TMPro.FontStyles.Bold;row.rankText.fontStyle=TMPro.FontStyles.Bold;}
        view.stage.SetActive(false);EditorUtility.SetDirty(panel);EditorUtility.SetDirty(view);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);
        return "Updated existing train with BUCA lettering, cabinet meshes, maple photograph and contact shadows.";
    }
}
#endif
