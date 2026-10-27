#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class BuildMidnightBrassLeaderboard
{
    const string Folder = "Assets/Art/MidnightBrassLeaderboard";
    static readonly Color Ivory = new Color(.95f,.90f,.78f);
    static readonly Color Gold = new Color(.87f,.65f,.30f);
    static Material navy, edge, brass, ivory, mint, teal;
    static List<GameObject> cityObjects;
    static List<BucaLeaderboardWorldTheme.Surface> surfaces;
    static List<BucaLeaderboardWorldTheme.Shape> shapes;
    static List<BucaLeaderboardWorldTheme.Pose> poses;

    [MenuItem("RealBuca/Build Midnight Brass Leaderboard")]
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean edit mode required.");
        string restore = SceneManager.GetActiveScene().path;
        Directory.CreateDirectory("output/leaderboard");
        File.Copy("Assets/Scenes/Game.unity", "output/leaderboard/Game-before-midnight-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var view = Object.FindAnyObjectByType<BucaArcadeResult3D>(FindObjectsInactive.Include);
            var table = Object.FindAnyObjectByType<BucaResultStandings>(FindObjectsInactive.Include);
            if (view == null || table == null || table.rows.Length != 10) throw new Exception("Saved ten-row result stage is missing.");
            var theme = view.GetComponent<BucaLeaderboardWorldTheme>();
            if (theme != null) theme.Apply(false);
            else theme = view.gameObject.AddComponent<BucaLeaderboardWorldTheme>();
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh(); ToyBoxGeometry.Initialize();
            navy = Mat("MidnightEnamel", new Color(.11f,.155f,.235f), .16f, .34f);
            edge = Mat("DeepNavyEdges", new Color(.022f,.037f,.065f), .23f, .40f);
            brass = Mat("WarmBrass", new Color(.78f,.57f,.28f), .78f, .48f);
            ivory = Mat("RaisedIvory", new Color(.88f,.80f,.62f), .06f, .32f);
            mint = Mat("PlayerMint", new Color(.13f,.71f,.55f), .15f, .34f);
            teal = Mat("PlayerTeal", new Color(.014f,.16f,.15f), .12f, .30f);
            cityObjects = new List<GameObject>(); surfaces = new List<BucaLeaderboardWorldTheme.Surface>();
            shapes = new List<BucaLeaderboardWorldTheme.Shape>(); poses = new List<BucaLeaderboardWorldTheme.Pose>();
            theme.standings = table;
            theme.levelManager = Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
            theme.roomInk = table.ink; theme.roomIvory = table.ivory;
            theme.roomRow = table.normalFace; theme.roomPlayer = table.playerFace; theme.roomMedals = table.rankMaterials;
            theme.cityRow = navy; theme.cityPlayer = teal; theme.cityBadge = Mat("GoldRankMedals", new Color(.88f,.65f,.29f), .22f, .35f); theme.cityInk = Ivory;
            theme.roomRankSize = table.rankLabels[0].rectTransform.sizeDelta;

            var cabinet = view.cabinet;
            var left = CityRoot(cabinet, "MidnightBrassCabinet");
            Slab(left,"CabinetDepth",new Vector3(0,1.15f,.11f),10.05f,5.38f,.70f,.66f,edge);
            Slab(left,"OuterBrassBevel",new Vector3(0,1.15f,-.22f),9.88f,5.20f,.21f,.60f,brass);
            Slab(left,"NavyShoulder",new Vector3(0,1.15f,-.34f),9.65f,4.97f,.18f,.52f,navy);
            Slab(left,"FineBrassLine",new Vector3(0,1.15f,-.435f),9.39f,4.71f,.07f,.43f,brass);
            Slab(left,"EnamelFace",new Vector3(0,1.15f,-.48f),9.29f,4.61f,.07f,.40f,navy);
            Slab(left,"BrandBrassBezel",new Vector3(0,3.72f,-.30f),4.08f,1.17f,.42f,.24f,brass);
            Slab(left,"HeadingRule",new Vector3(0,1.99f,-.57f),7.9f,.045f,.035f,.015f,brass).SetActive(false);
            foreach (string n in new[] {"BrandTab","TealDisplay","CounterRecess"}) Bind(cabinet,n,navy);
            foreach (string n in new[] {"Brand","HeartsHeading","TimeHeading","ScoreLabel"}) Bind(cabinet,n,ivory);
            Shape(cabinet.Find("Brand").GetComponent<MeshFilter>(),ToyBoxGeometry.Text("BUCA",.20f,.03f,.065f));
            Shape(view.heartsHeading.GetComponent<MeshFilter>(),ToyBoxGeometry.Text("OUT OF HEARTS",.23f,.03f,.062f));
            Shape(view.timeHeading.GetComponent<MeshFilter>(),ToyBoxGeometry.Text("TIME'S UP",.23f,.03f,.062f));
            foreach (string n in new[] {"DisplayTrim","ScoreAccent-1","ScoreAccent1","CornerBolt0","CornerBolt1","CornerBolt2","CornerBolt3"}) Bind(cabinet,n,brass);
            foreach (var digit in view.digits)
            {
                Bind(digit.transform.parent.GetComponent<Renderer>(), ivory);
                Bind(digit.GetComponent<Renderer>(), edge);
                Shape(digit.transform.parent.GetComponent<MeshFilter>(),SlabMesh(.50f,1.2f,.26f,.10f,false));
                Pose(digit.transform,new Vector3(0,0,-.16f));
            }

            var right = CityRoot(table.transform,"MidnightBrassStandings");
            Slab(right,"CrownedCabinet",new Vector3(0,0,.08f),8.18f,6.95f,.60f,.45f,edge,true);
            Slab(right,"CrownedBrassBevel",new Vector3(0,0,-.22f),8.02f,6.80f,.20f,.41f,brass,true);
            Slab(right,"CrownedNavyShoulder",new Vector3(0,0,-.33f),7.83f,6.61f,.15f,.35f,navy,true);
            Slab(right,"CrownedFineBrass",new Vector3(0,0,-.408f),7.59f,6.37f,.055f,.29f,edge,true);
            Slab(right,"CrownedEnamelFace",new Vector3(0,0,-.439f),7.51f,6.29f,.045f,.27f,navy,true);
            // The title remains the original extruded mesh with its existing proportions.
            Bind(table.transform,"StandingsTitle",ivory);
            Slab(right,"TitleRecess",new Vector3(0,2.64f,-.476f),6.9f,.78f,.06f,.16f,navy).SetActive(false);
            Shape(table.transform.Find("StandingsTitle").GetComponent<MeshFilter>(),ToyBoxGeometry.Text("LEADERBOARD",.22f,.03f,.052f));
            foreach (int side in new[] {-1,1})
            {
                Slab(right,"SubtitleRule"+side,new Vector3(side*2.25f,2.04f,-.49f),1.85f,.035f,.025f,.01f,brass);
                foreach (float y in new[] {-3.03f,2.94f}) Disc(right,"CornerBolt"+side+y,new Vector3(side*3.64f,y,-.50f),.105f,.085f,brass);
            }
            TrainCrest(right);

            table.playerOutlines = new GameObject[table.rows.Length];
            var textSizes=new List<BucaLeaderboardWorldTheme.TextSize>();
            for(int i=0;i<table.rows.Length;i++)
            {
                var row = table.rows[i].transform;
                var trim = CityRoot(row,"MidnightRowTrim");
                Shape(row.GetComponent<MeshFilter>(),SlabMesh(6.85f,.39f,.17f,.095f,false));
                Pose(row,new Vector3(0,1.39f-i*.445f,-.49f));
                Pose(table.labels[i].transform,new Vector3(-.53f,0,-.115f));
                Pose(table.scoreLabels[i].transform,new Vector3(2.18f,0,-.115f));
                Pose(table.rankLabels[i].transform,new Vector3(-2.92f,0,-.16f));
                Pose(table.rankBadges[i].transform,new Vector3(-2.92f,0,-.105f));
                Shape(table.rankBadges[i].GetComponent<MeshFilter>(),ToyBoxGeometry.Disc(.166f,.055f,.018f));
                Slab(trim,"BrassRowBorder",new Vector3(0,0,.045f),6.93f,.425f,.09f,.10f,edge);
                table.playerOutlines[i] = Slab(trim,"PlayerOutline",new Vector3(0,0,.02f),6.96f,.43f,.07f,.105f,mint);
                table.playerOutlines[i].SetActive(false);
                foreach(var label in new[]{table.labels[i],table.scoreLabels[i]})
                    textSizes.Add(new BucaLeaderboardWorldTheme.TextSize {label=label,roomMax=label.fontSizeMax,roomMin=label.fontSizeMin,cityMax=3.05f,cityMin=label.fontSizeMin});
            }
            var colors = new List<BucaLeaderboardWorldTheme.LetterColor>();
            foreach (string name in new[] {"Subtitle","RankHeading","PlayerHeading","ScoreHeading","Footer","NoScores"})
            {
                var label = table.transform.Find(name).GetComponent<TMP_Text>();
                colors.Add(new BucaLeaderboardWorldTheme.LetterColor {label=label,room=label.color,city=name=="NoScores"?Ivory:Gold});
            }
            theme.lettering = colors.ToArray(); theme.surfaces = surfaces.ToArray(); theme.cityObjects = cityObjects.ToArray();
            theme.shapes = shapes.ToArray(); theme.poses = poses.ToArray();
            theme.textSizes=textSizes.ToArray();
            theme.card=view.panel.card;theme.roomCardSize=view.panel.card.sizeDelta;
            theme.rotations=new[]{
                new BucaLeaderboardWorldTheme.Rotation{transform=cabinet,room=cabinet.localRotation,city=Quaternion.Euler(0,5,0)},
                new BucaLeaderboardWorldTheme.Rotation{transform=table.transform,room=table.transform.localRotation,city=Quaternion.Euler(0,-4,0)}
            };
            var stageCamera=view.stage.GetComponentInChildren<Camera>(true);
            stageCamera.allowMSAA=true;
            var stageTarget=stageCamera.targetTexture;
            stageTarget.Release();stageTarget.antiAliasing=4;stageTarget.filterMode=FilterMode.Bilinear;stageTarget.Create();
            EditorUtility.SetDirty(stageTarget);
            Pose(stageCamera.transform,stageCamera.transform.localPosition+Vector3.up*.75f);
            theme.poses=poses.ToArray();
            theme.roomObjects = new[] {"Cabinet","InsetBrass","MapleInset"}.Select(n=>cabinet.Find(n).gameObject)
                .Concat(new[] {"StandingsFrame","StandingsMaple","TitlePlaque","Footer"}.Select(n=>table.transform.Find(n).gameObject)).ToArray();
            view.worldTheme = theme;
            BuildBackdrop(theme,view.panel);
            theme.Apply(true); theme.Apply(false);
            view.stage.SetActive(false);
            EditorUtility.SetDirty(view); EditorUtility.SetDirty(table); EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(view.gameObject.scene); EditorSceneManager.SaveScene(view.gameObject.scene);
            File.WriteAllText("output/leaderboard/midnight-build.txt","Saved World 2 Midnight Brass. World 1 retained.\n");
            return "Saved World 2 Midnight Brass leaderboard and score cabinet.";
        }
        finally { EditorSceneManager.OpenScene(restore); }
    }

    static Material Mat(string name,Color color,float metallic,float smoothness)
    {
        var material=new Material(AssetDatabase.LoadAssetAtPath<Shader>(Folder+"/MidnightDisplay.shader")){name=name,enableInstancing=true};
        material.SetColor("_BaseColor",color); material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness);
        return ToyBoxGeometry.Save(material,Folder+"/"+name+".mat");
    }
    static void BuildBackdrop(BucaLeaderboardWorldTheme theme,LeaderboardPanel panel)
    {
        theme.gameplayCamera = Camera.main;
        RenderTexture Target(string name,int depth)
        {
            string path=Folder+"/"+name+".renderTexture";
            var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if(texture==null)
            {
                texture=new RenderTexture(960,540,depth,RenderTextureFormat.ARGB32){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                AssetDatabase.CreateAsset(texture,path);
            }
            return texture;
        }
        theme.levelCapture=Target("CityCapture",24);theme.blurScratch=Target("CityBlurScratch",0);theme.blurredLevel=Target("CityBlurredBackdrop",0);
        theme.blurMaterial=ToyBoxGeometry.Save(new Material(Resources.Load<Shader>("BucaGaussianBlur")){name="CityLeaderboardBlur"},Folder+"/CityLeaderboardBlur.mat");
        var child=panel.transform.Find("MidnightCityBackdrop");
        if(child==null){child=new GameObject("MidnightCityBackdrop",typeof(RectTransform),typeof(UnityEngine.UI.RawImage)).transform;child.SetParent(panel.transform,false);}
        var rect=(RectTransform)child;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        child.SetAsFirstSibling();child.gameObject.layer=panel.gameObject.layer;
        theme.cityBackdrop=child.GetComponent<UnityEngine.UI.RawImage>();theme.cityBackdrop.texture=theme.blurredLevel;
        theme.cityBackdrop.color=new Color(.72f,.72f,.72f,1);theme.cityBackdrop.raycastTarget=false;
    }
    static Transform CityRoot(Transform parent,string name)
    {
        var t=parent.Find(name);
        if(t==null){t=new GameObject(name).transform;t.SetParent(parent,false);}
        t.gameObject.layer=31;cityObjects.Add(t.gameObject);return t;
    }
    static GameObject Part(Transform parent,string name,Vector3 position,Mesh mesh,Material material)
    {
        var t=parent.Find(name);
        if(t==null){t=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)).transform;t.SetParent(parent,false);}
        t.gameObject.layer=31;t.localPosition=position;
        t.GetComponent<MeshFilter>().sharedMesh=mesh;t.GetComponent<MeshRenderer>().sharedMaterial=material;
        return t.gameObject;
    }
    static void Bind(Transform root,string name,Material material)
    {
        var target=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        if(target==null)throw new Exception("Missing result surface: "+name);
        Bind(target.GetComponent<Renderer>(),material);
    }
    static void Bind(Renderer renderer,Material city)
    {surfaces.Add(new BucaLeaderboardWorldTheme.Surface {renderer=renderer,room=renderer.sharedMaterial,city=city});}
    static void Shape(MeshFilter filter,Mesh city)
    {shapes.Add(new BucaLeaderboardWorldTheme.Shape {filter=filter,room=filter.sharedMesh,city=city});}
    static void Pose(Transform transform,Vector3 city)
    {poses.Add(new BucaLeaderboardWorldTheme.Pose {transform=transform,roomPosition=transform.localPosition,cityPosition=city});}
    static GameObject Slab(Transform parent,string name,Vector3 pos,float w,float h,float d,float radius,Material material,bool crown=false)
    {return Part(parent,name,pos,SlabMesh(w,h,d,radius,crown),material);}
    static Mesh SlabMesh(float w,float h,float d,float radius,bool crown)
    {
        string key=$"SoftBevel_{w:F3}_{h:F3}_{d:F3}_{radius:F3}_{crown}";
        string path=Folder+"/"+key+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var outline=new List<Vector2>();
            for(int corner=0;corner<4;corner++)
            {
                for(int j=0;j<=12;j++)
                {
                    float a=(corner*90+j*7.5f)*Mathf.Deg2Rad;
                    float x=(corner==0||corner==3?1:-1)*(w*.5f-radius)+Mathf.Cos(a)*radius;
                    float y=(corner<2?1:-1)*(h*.5f-radius)+Mathf.Sin(a)*radius;
                    outline.Add(new Vector2(x,y));
                }
                if(corner==0)for(int j=1;j<64;j++)outline.Add(new Vector2(Mathf.Lerp(w*.5f-radius,-w*.5f+radius,j/64f),h*.5f));
            }
            if(crown)for(int i=0;i<outline.Count;i++)
            {var p=outline[i];if(p.y>0)p.y+=.57f*Mathf.Exp(-Mathf.Pow(p.x/(w*.215f),4))*Mathf.Clamp01(p.y/(h*.4f));outline[i]=p;}
            var vertices=new List<Vector3>();var triangles=new List<int>();int n=outline.Count;
            float bevel=Mathf.Min(.11f,d*.40f);
            for(int ring=0;ring<4;ring++)foreach(var p in outline)
            {
                float inset=ring==0||ring==3?bevel:0;
                vertices.Add(new Vector3(p.x*(1-inset*2/w),p.y*(1-inset*2/h),ring==0?-d/2:ring==1?-d/2+bevel:ring==2?d/2-bevel:d/2));
            }
            for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++)
            {int a=ring*n+i,b=ring*n+(i+1)%n;triangles.AddRange(new[]{a,b,b+n,a,b+n,a+n});}
            int center=vertices.Count;vertices.Add(new Vector3(0,0,-d/2));
            for(int i=0;i<n;i++)triangles.AddRange(new[]{center,(i+1)%n,i});
            mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var normals=mesh.normals;for(int i=0;i<n;i++)normals[i]=Vector3.back;normals[center]=Vector3.back;mesh.normals=normals;
            AssetDatabase.CreateAsset(mesh,path);
        }
        return mesh;
    }
    static void Disc(Transform parent,string name,Vector3 position,float radius,float depth,Material material)
    {Part(parent,name,position,ToyBoxGeometry.Disc(radius,depth,Mathf.Min(.025f,depth*.4f)),material).transform.localRotation=Quaternion.Euler(90,0,0);}
    static void TrainCrest(Transform parent)
    {
        var crest=CityRoot(parent,"BrassLocomotiveCrest");crest.localPosition=new Vector3(0,3.43f,-.52f);
        // Rebuild only this builder-owned badge, including obsolete crest pieces.
        while(crest.childCount>0)Object.DestroyImmediate(crest.GetChild(0).gameObject);
        Disc(crest,"MedallionRim",Vector3.zero,.355f,.09f,brass);
        Disc(crest,"MedallionEnamel",new Vector3(0,0,-.055f),.307f,.045f,edge);
        Slab(crest,"CabRoof",new Vector3(0,.195f,-.10f),.33f,.055f,.055f,.02f,ivory);
        Slab(crest,"Cab",new Vector3(0,.075f,-.10f),.245f,.22f,.06f,.025f,ivory);
        Slab(crest,"Windshield",new Vector3(0,.10f,-.139f),.16f,.10f,.025f,.015f,navy);
        Disc(crest,"BoilerRim",new Vector3(0,-.075f,-.15f),.135f,.075f,brass);
        Disc(crest,"Headlamp",new Vector3(0,-.065f,-.195f),.052f,.025f,ivory);
        foreach(int side in new[]{-1,1})
            Disc(crest,"Wheel"+side,new Vector3(side*.12f,-.215f,-.11f),.047f,.055f,ivory);
    }
}
#endif
