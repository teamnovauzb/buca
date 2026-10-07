#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class WoodenWellDoneBaker
{
    const string Folder="Assets/ToyBoxMenu/WellDone";
    const string PrefabPath="Assets/ToyBoxMenu/Prefabs/ResultsAndLeaderboard.prefab";
    static Material teal,edge,maple,cream,brass,mint,ink;

    [MenuItem("RealBuca/Toy Box 3D/Bake Wooden Well Done")]
    public static void BakeMenu() { Debug.Log(Bake()); }
    public static string Bake()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before baking.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/ToyBoxMenu","WellDone");
        ToyBoxGeometry.Initialize();
        ToyBoxGeometry.Glyphs['!']=new ToyBoxGeometry.Glyph{character="!",width=.35f,contours=new[]{
            new ToyBoxGeometry.Contour{points=new[]{new Vector2(.09f,.30f),new Vector2(.25f,.30f),new Vector2(.29f,1),new Vector2(.05f,1)}},
            new ToyBoxGeometry.Contour{points=new[]{new Vector2(.06f,0),new Vector2(.28f,0),new Vector2(.28f,.19f),new Vector2(.06f,.19f)}}}};
        teal=Mat("DeepTeal",new Color(.018f,.19f,.18f),.1f,.4f);
        edge=Mat("TealEdge",new Color(.04f,.36f,.32f),.12f,.46f);
        maple=Mat("WarmMaple",new Color(.98f,.87f,.68f),0,.32f);
        maple.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));EditorUtility.SetDirty(maple);
        cream=Mat("Ivory",new Color(1,.89f,.66f),.04f,.38f);
        brass=Mat("Brass",new Color(.92f,.58f,.17f),.63f,.58f);
        mint=Mat("MintButton",new Color(.13f,.65f,.40f),.08f,.5f);
        ink=Mat("TealLetters",new Color(.013f,.12f,.115f),.15f,.4f);
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var results=root.GetComponent<ToyBoxResults3D>();
            var old=results.presentation.transform.Find("WoodenWellDone");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var parent=Group("WoodenWellDone",results.presentation.transform,Vector3.zero);
            var view=parent.gameObject.AddComponent<ToyBoxWellDone3D>();results.wellDone=view;
            view.banner=Group("CurvedWoodenRibbon",parent,new Vector3(0,1.85f,0));view.bannerRest=view.banner.localPosition;
            foreach(int side in new[]{-1,1})
            {
                var wing=Group("FoldedRibbonTail"+side,view.banner,new Vector3(side*4.55f,-.26f,.15f));
                wing.localRotation=Quaternion.Euler(0,0,side*-17);
                var tail=Tail();
                var back=Part(wing,"TealTailRim",tail,teal,Vector3.zero);back.transform.localScale=new Vector3(side*1.8f,1.64f,1);
                var face=Part(wing,"MapleTailFace",tail,maple,new Vector3(0,0,-.11f));face.transform.localScale=new Vector3(side*1.60f,1.42f,.60f);
                Pin(wing,new Vector3(side*.24f,0,-.21f),.105f);
            }
            Part(view.banner,"TealRibbonBody",Arch("RibbonRim",new Vector3(9.05f,1.83f,.43f),.77f),teal,Vector3.zero);
            Part(view.banner,"RibbonHighlight",Arch("RibbonLip",new Vector3(8.87f,1.65f,.22f),.77f),edge,new Vector3(0,0,-.22f));
            Part(view.banner,"MapleRibbonFace",Arch("RibbonMaple",new Vector3(8.65f,1.44f,.25f),.77f),maple,new Vector3(0,0,-.34f));
            Label(view.banner,"WellDoneLetters","WELL DONE!",.93f,7.6f,new Vector3(0,.02f,-.65f),.69f,ink);
            Part(view.banner,"SubtitleWoodRim",Arch("SubtitleRim",new Vector3(4.98f,.81f,.30f),.10f),maple,new Vector3(0,-.43f,-.42f));
            Part(view.banner,"SubtitleCreamFace",Arch("SubtitleFace",new Vector3(4.75f,.63f,.20f),.10f),cream,new Vector3(0,-.42f,-.60f));
            Label(view.banner,"LevelCompleteLetters","LEVEL COMPLETE",.32f,4.30f,new Vector3(0,-.40f,-.73f),.07f,ink);
            Box(view.banner,"ShotsWoodTag",new Vector3(0,-1.03f,-.38f),new Vector3(2.90f,.63f,.26f),.18f,maple);
            view.shotsLabel=LiveLabel(view.banner,"ShotCount",new Vector3(0,-1.035f,-.55f),2.5f,.31f,results.golfSummary.font,ink,12);

            view.scorePlaque=Group("SeparateScorePlaque",parent,new Vector3(0,-.93f,-.12f));view.scoreRest=view.scorePlaque.localPosition;
            Box(view.scorePlaque,"TealScoreBacking",Vector3.zero,new Vector3(5.45f,2.53f,.44f),.44f,teal);
            Box(view.scorePlaque,"RaisedMapleScoreFace",new Vector3(0,.04f,-.24f),new Vector3(5.20f,2.29f,.33f),.37f,maple);
            Box(view.scorePlaque,"ScoreBrassInset",new Vector3(0,-.25f,-.44f),new Vector3(4.78f,1.53f,.13f),.24f,brass);
            Box(view.scorePlaque,"DeepTealScoreFace",new Vector3(0,-.25f,-.53f),new Vector3(4.62f,1.38f,.12f),.21f,teal);
            Label(view.scorePlaque,"TotalLetters","TOTAL",.39f,2.3f,new Vector3(0,.79f,-.46f),0,ink);
            foreach(int side in new[]{-1,1})Pin(view.scorePlaque,new Vector3(side*1.95f,.77f,-.47f),.12f);
            var digits=Group("SavedScoreDigits",view.scorePlaque,new Vector3(0,-.23f,-.84f));
            view.digitMeshes=new Mesh[10];for(int i=0;i<10;i++)view.digitMeshes[i]=ToyBoxGeometry.Text(i.ToString(),.241f,.035f,.035f);
            view.scoreDigits=new MeshFilter[10];
            for(int i=0;i<10;i++)view.scoreDigits[i]=Part(digits,"Digit"+i,view.digitMeshes[0],cream,Vector3.zero).GetComponent<MeshFilter>();

            view.nextButton=Group("WoodenNextButton",parent,new Vector3(0,-3.12f,-.16f));view.buttonRest=view.nextButton.localPosition;
            Box(view.nextButton,"MapleButtonSocket",Vector3.zero,new Vector3(5.45f,1.36f,.53f),.36f,maple);
            Box(view.nextButton,"TealButtonInset",new Vector3(0,.05f,-.28f),new Vector3(4.91f,1.09f,.18f),.29f,teal);
            Box(view.nextButton,"BrassButtonRim",new Vector3(0,.05f,-.38f),new Vector3(4.76f,.99f,.12f),.25f,brass);
            view.nextCap=Group("PressableMintNext",view.nextButton,new Vector3(0,.05f,-.43f));view.capRest=view.nextCap.localPosition;
            Box(view.nextCap,"MintNextCap",Vector3.zero,new Vector3(4.57f,.85f,.21f),.24f,mint);
            Label(view.nextCap,"NextLetters","NEXT",.49f,2.7f,new Vector3(0,0,-.23f),0,cream);
            foreach(int side in new[]{-1,1})Pin(view.nextCap,new Vector3(side*1.93f,0,-.14f),.09f);
            var hit=view.nextCap.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(4.6f,.90f,.3f);view.nextHit=hit;
            Box(view.nextButton,"CountdownTrack",new Vector3(0,-.51f,-.255f),new Vector3(3.15f,.045f,.025f),.01f,teal);
            view.countdownFill=Box(view.nextButton,"CountdownFill",new Vector3(0,-.51f,-.27f),new Vector3(3.1f,.045f,.025f),.01f,mint).transform;

            var confetti=Group("LightConfetti",parent,Vector3.zero);
            var star=ToyBoxGeometry.MapPrism(10,1,.16f,.03f,true);
            var ribbon=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ToyBoxMenu/ResultsCelebration/ConfettiRibbon.asset");
            view.confetti=new Transform[30];var colors=new[]{brass,edge,cream};
            for(int i=0;i<30;i++)view.confetti[i]=Part(confetti,"SavedConfetti"+i,i%3==0?star:ribbon,colors[i%3],Vector3.zero).transform;
            var shortcut=results.presentation.transform.Find("ScorecardShortcut");
            if(shortcut==null)
            {
                shortcut=Group("ScorecardShortcut",results.presentation.transform,Vector3.zero);
                results.scorecardHit.transform.SetParent(shortcut,true);
                var label=results.presentation.transform.Find("Text_GREEN_-_MY_SHOTS_-_BACK");
                if(label==null)label=results.presentation.transform.Cast<Transform>().FirstOrDefault(t=>t.name.Contains("GREEN") && t.name.Contains("MY"));
                if(label!=null)label.SetParent(shortcut,true);
            }
            results.scorecardShortcut=shortcut.gameObject;
            view.Prepare(3);view.SetTotal(840);view.SampleEntrance(1.8f);view.StopConfetti();parent.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            return "Saved wooden Well Done ribbon, live score/shots, working Next, 30 confetti pieces.";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static Transform Group(string name,Transform parent,Vector3 position)
    { var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t; }
    static Material Mat(string name,Color tint,float metallic,float smooth)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        mat.SetColor("_BaseColor",tint);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smooth);
        return ToyBoxGeometry.Save(mat,Folder+"/"+name+".mat");
    }
    static GameObject Part(Transform parent,string name,Mesh mesh,Material mat,Vector3 pos)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=pos;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;return go;
    }
    static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,float radius,Material mat)
    {return Part(parent,name,Slab(name,size,radius),mat,pos);}
    static void Pin(Transform parent,Vector3 position,float radius)
    {Part(parent,"BrassPin",ToyBoxGeometry.Disc(radius,.06f,.025f),brass,position).transform.localRotation=Quaternion.Euler(90,0,0);}
    static void Label(Transform parent,string name,string words,float height,float width,Vector3 pos,float arch,Material mat)
    {
        var mesh=UnityEngine.Object.Instantiate(ToyBoxGeometry.Text(words,.241f,.035f,.035f));
        float scale=Mathf.Min(height/mesh.bounds.size.y,width/mesh.bounds.size.x);var vertices=mesh.vertices;var normals=mesh.normals;var center=mesh.bounds.center;
        for(int i=0;i<vertices.Length;i++)
        {
            vertices[i]=new Vector3((vertices[i].x-center.x)*scale,(vertices[i].y-center.y)*scale,vertices[i].z*scale);
            vertices[i].y+=arch*(1-Mathf.Pow(vertices[i].x/(width*.5f),2));
            float slope=-2*arch*vertices[i].x/(width*width*.25f);
            normals[i]=new Vector3(normals[i].x-slope*normals[i].y,normals[i].y,normals[i].z).normalized;
        }
        mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();mesh.RecalculateTangents();
        Part(parent,name,ToyBoxGeometry.Save(mesh,Folder+"/"+name+".asset"),mat,pos);
    }
    static ToyBoxPodiumLabel LiveLabel(Transform parent,string name,Vector3 pos,float width,float height,TMP_FontAsset font,Material material,int count)
    {
        var go=Group(name,parent,pos).gameObject;var text=go.AddComponent<TextMeshPro>();text.font=font;text.fontSize=3;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(width,.5f);text.text="";
        var label=go.AddComponent<ToyBoxPodiumLabel>();label.source=text;label.height=height;label.characters="0123456789SHOTS";
        label.glyphs=new Mesh[label.characters.Length];for(int i=0;i<label.glyphs.Length;i++)label.glyphs[i]=ToyBoxGeometry.Text(label.characters[i].ToString(),.241f,.035f,.035f);
        label.places=new MeshFilter[count];for(int i=0;i<count;i++)label.places[i]=Part(go.transform,"SavedGlyph"+i,label.glyphs[0],material,Vector3.zero).GetComponent<MeshFilter>();
        return label;
    }
    static Mesh Tail()
    {
        ToyBoxGeometry.Glyphs['§']=new ToyBoxGeometry.Glyph{character="§",width=1,contours=new[]{new ToyBoxGeometry.Contour{points=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(.76f,.5f),new Vector2(1,1),new Vector2(0,1)}}}};
        return ToyBoxGeometry.Text("§",.241f,.035f,0);
    }
    static Mesh Slab(string name,Vector3 size,float radius)
    {
        radius=Mathf.Min(radius,Mathf.Min(size.x,size.y)*.49f);
        var outline=new List<Vector2>();
        for(int corner=0;corner<4;corner++)for(int j=0;j<=12;j++)
        {
            float a=(corner*90+j*7.5f)*Mathf.Deg2Rad;
            outline.Add(new Vector2((corner==0||corner==3?1:-1)*(size.x*.5f-radius)+Mathf.Cos(a)*radius,(corner<2?1:-1)*(size.y*.5f-radius)+Mathf.Sin(a)*radius));
        }
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int count=outline.Count;
        float bevel=Mathf.Min(.085f,size.z*.30f);
        for(int ring=0;ring<4;ring++)foreach(var p in outline)
        {
            float inset=ring==0||ring==3?bevel:0;
            var q=new Vector3(p.x*(1-inset/(size.x*.5f)),p.y*(1-inset/(size.y*.5f)),ring==0?-size.z*.5f:ring==1?-size.z*.5f+bevel:ring==2?size.z*.5f-bevel:size.z*.5f);
            vertices.Add(q);uv.Add(new Vector2(q.x,q.y)*.2f);
        }
        for(int ring=0;ring<3;ring++)for(int j=0;j<count;j++)
        {int a=ring*count+j,b=ring*count+(j+1)%count,c=b+count,d=a+count;triangles.AddRange(new[]{a,b,c,a,c,d});}
        for(int side=0;side<2;side++)
        {
            int center=vertices.Count;float z=(side==0?-1:1)*size.z*.5f;vertices.Add(new Vector3(0,0,z));uv.Add(Vector2.zero);
            foreach(var p in outline){var q=new Vector3(p.x*(1-bevel/(size.x*.5f)),p.y*(1-bevel/(size.y*.5f)),z);vertices.Add(q);uv.Add(new Vector2(q.x,q.y)*.2f);}
            for(int j=0;j<count;j++){int a=center+1+j,b=center+1+(j+1)%count;triangles.AddRange(side==0?new[]{center,b,a}:new[]{center,a,b});}
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        return ToyBoxGeometry.Save(mesh,Folder+"/Slab_"+name+".asset");
    }
    static Mesh Arch(string name,Vector3 size,float curve)
    {
        float radius=Mathf.Min(size.z*.45f,.14f);Vector3 half=size*.5f,inner=half-Vector3.one*radius;
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            var u=normal.x!=0?Vector3.forward:Vector3.right;var v=Vector3.Cross(normal,u);
            int nx=u.x!=0?64:12,ny=v.x!=0?64:12;int first=vertices.Count;
            for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
            {
                var p=Vector3.Scale(normal+u*(2f*x/nx-1)+v*(2f*y/ny-1),half);
                var q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                var n=(p-q).normalized;p=q+n*radius;float slope=-2*curve*p.x/(half.x*half.x);p.y+=curve*(1-Mathf.Pow(p.x/half.x,2));
                vertices.Add(p);normals.Add(new Vector3(n.x-slope*n.y,n.y,n.z).normalized);uv.Add(new Vector2(p.x,p.y)*.2f);
                if(x<nx&&y<ny){int s=first+y*(nx+1)+x;triangles.AddRange(new[]{s,s+1,s+nx+2,s,s+nx+2,s+nx+1});}
            }
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
        return ToyBoxGeometry.Save(mesh,Folder+"/"+name+".asset");
    }
}
#endif
