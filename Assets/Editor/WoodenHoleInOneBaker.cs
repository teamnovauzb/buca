#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Bakes the selected wooden celebration into the existing saved win prefab.</summary>
public static class WoodenHoleInOneBaker
{
    const string Folder="Assets/ToyBoxMenu/HoleInOne";
    const string Prefab="Assets/ToyBoxMenu/Prefabs/PremiumWinCelebration.prefab";
    static Material Material(string name,Color tint,float metal=0,float smooth=.4f)
    {
        string path=Folder+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",tint);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);mat.enableInstancing=true;
        EditorUtility.SetDirty(mat);return mat;
    }
    static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 position)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.receiveShadows=false;renderer.shadowCastingMode=ShadowCastingMode.Off;return go;
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
                var n=(p-q).normalized;p=q+n*radius;
                float slope=-2*curve*p.x/(half.x*half.x);p.y+=curve*(1-Mathf.Pow(p.x/half.x,2));
                vertices.Add(p);normals.Add(new Vector3(n.x-slope*n.y,n.y,n.z).normalized);uv.Add(new Vector2(p.x,p.y)*.2f);
                if(x<nx&&y<ny){int start=first+y*(nx+1)+x;triangles.AddRange(new[]{start,start+1,start+nx+2,start,start+nx+2,start+nx+1});}
            }
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
        return ToyBoxGeometry.Save(mesh,Folder+"/"+name+"_Final.asset");
    }

    static void Label(Transform parent,string name,string words,float height,float width,float y,float z,float arch,Material material)
    {
        var mesh=UnityEngine.Object.Instantiate(ToyBoxGeometry.Text(words,.17f,.028f,name=="RaisedCreamTitle"?.065f:.025f));
        float scale=Mathf.Min(height/mesh.bounds.size.y,width/mesh.bounds.size.x);
        var vertices=mesh.vertices;
        for(int i=0;i<vertices.Length;i++)
        {
            vertices[i]*=scale;
            vertices[i].y+=arch*(1-Mathf.Pow(vertices[i].x/(width*.5f),2));
        }
        mesh.name=name;mesh.vertices=vertices;mesh.RecalculateBounds();
        Part(parent,name,ToyBoxGeometry.Save(mesh,Folder+"/"+name+"_Final.asset"),material,new Vector3(0,y,z));
    }
    static ParticleSystem Effects(Transform parent,string name,Vector3 pos,Material mat,Mesh mesh,bool fountain,uint seed)
    {
        var go=new GameObject(name,typeof(ParticleSystem));go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(-90,0,0);
        var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=seed;
        var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=1.65f;main.useUnscaledTime=true;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.startLifetime=new ParticleSystem.MinMaxCurve(fountain?.35f:1.6f,fountain?.65f:2.7f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(fountain?3.6f:3f,fountain?6.8f:5.5f);
        main.startSize=new ParticleSystem.MinMaxCurve(fountain?.045f:.15f,fountain?.075f:.24f);
        main.gravityModifier=fountain?.7f:.24f;main.maxParticles=fountain?130:60;
        main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
        if(!fountain){main.startRotation3D=true;main.startRotationX=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationY=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationZ=new ParticleSystem.MinMaxCurve(0,6.28f);}
        var emission=ps.emission;emission.rateOverTime=fountain?150:0;
        if(!fountain)emission.SetBursts(new[]{new ParticleSystem.Burst(.05f,22),new ParticleSystem.Burst(.4f,14)});
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=fountain?13:42;shape.radius=fountain?.035f:.5f;
        var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(1,.7f),new GradientAlphaKey(0,1)});color.color=gradient;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        if(fountain){renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=2.5f;renderer.velocityScale=.05f;}
        else {renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=mesh;var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.separateAxes=true;rotation.x=2;rotation.y=3;rotation.z=4;}
        return ps;
    }
    [MenuItem("RealBuca/Bake Wooden Hole In One")]
    public static string Bake()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before baking.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/ToyBoxMenu","HoleInOne");
        ToyBoxGeometry.Initialize();
        foreach(var glyph in ToyBoxGeometry.Glyphs.Values)glyph.width+=.10f;
        ToyBoxGeometry.Glyphs['!']=new ToyBoxGeometry.Glyph {character="!",width=.35f,contours=new[]{new ToyBoxGeometry.Contour{points=new[]{new Vector2(.09f,.30f),new Vector2(.25f,.30f),new Vector2(.29f,1),new Vector2(.05f,1)}},new ToyBoxGeometry.Contour{points=new[]{new Vector2(.06f,0),new Vector2(.28f,0),new Vector2(.28f,.19f),new Vector2(.06f,.19f)}}}};
        ToyBoxGeometry.Glyphs['.']=new ToyBoxGeometry.Glyph {character=".",width=.25f,contours=new[]{new ToyBoxGeometry.Contour{points=new[]{new Vector2(.03f,0),new Vector2(.22f,0),new Vector2(.22f,.19f),new Vector2(.03f,.19f)}}}};
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var view=root.GetComponent<ToyBoxWinCelebration>();
            foreach(string name in new[]{"WoodenCelebrationBanner","TwinFountains"}) {var old=view.presentation.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var teal=Material("TealLacquer",new Color(.025f,.24f,.24f),.12f,.48f);
            var edge=Material("TealEdge",new Color(.045f,.38f,.36f),.12f,.45f);
            var maple=Material("CarvedMaple",new Color(.92f,.81f,.64f),0,.28f);
            maple.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));EditorUtility.SetDirty(maple);
            var cream=Material("CreamLetters",new Color(1,.89f,.64f),.03f,.35f);
            var navy=Material("Ink",new Color(.012f,.064f,.062f),0,.28f);
            var brass=Material("BrassPins",new Color(.92f,.52f,.1f),.65f,.53f);
            var banner=new GameObject("WoodenCelebrationBanner").transform;banner.SetParent(view.presentation,false);view.woodenBanner=banner;
            Part(banner,"TealCarvedFrame",Arch("TealCarvedFrame",new Vector3(8.8f,1.8f,.36f),.52f),teal,Vector3.zero);
            Part(banner,"RaisedEdge",Arch("RaisedEdge",new Vector3(8.55f,1.60f,.16f),.52f),edge,new Vector3(0,0,-.22f));
            Part(banner,"InnerTealRecess",Arch("InnerTealRecess",new Vector3(8.33f,1.43f,.13f),.52f),teal,new Vector3(0,0,-.33f));
            Part(banner,"MapleFace",Arch("MapleFace",new Vector3(8.1f,1.29f,.18f),.52f),maple,new Vector3(0,0,-.42f));
            Label(banner,"RaisedCreamTitle","HOLE IN ONE!",.80f,7.15f,.02f,-.58f,.44f,cream);
            // Matching smaller capsule below the main banner.
            Part(banner,"SubtitleTealFrame",ToyBoxGeometry.RoundedBox(new Vector3(5.15f,.81f,.38f),.18f),teal,new Vector3(0,-.92f,-.25f));
            Part(banner,"SubtitleMaple",ToyBoxGeometry.RoundedBox(new Vector3(4.87f,.59f,.25f),.12f),maple,new Vector3(0,-.90f,-.48f));
            Label(banner,"Subtitle","ONE SHOT. PERFECT!",.25f,4.15f,-.90f,-.64f,0,navy);
            foreach(float x in new[]{-3.86f,3.86f})Part(banner,"BrassPin",ToyBoxGeometry.Disc(.09f,.07f,.025f),brass,new Vector3(x,.05f,-.57f)).transform.localRotation=Quaternion.Euler(90,0,0);
            foreach(float x in new[]{-2.24f,2.24f})Part(banner,"SubtitlePin",ToyBoxGeometry.Disc(.055f,.045f,.018f),brass,new Vector3(x,-.90f,-.64f)).transform.localRotation=Quaternion.Euler(90,0,0);
            var jets=new GameObject("TwinFountains").transform;jets.SetParent(view.presentation,false);view.fountainRoot=jets.gameObject;
            var sparks=Material("WarmSpark",new Color(1,.84f,.39f),0,.2f);
            sparks.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");sparks.SetColor("_BaseColor",new Color(5,3.2f,.7f));
            sparks.SetFloat("_Surface",1);sparks.SetFloat("_Blend",2);sparks.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);sparks.SetFloat("_DstBlend",(float)BlendMode.One);sparks.SetFloat("_ZWrite",0);sparks.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");sparks.renderQueue=3000;
            var glow=new Texture2D(32,32,TextureFormat.RGBA32,false);glow.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;glow.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),1.5f)));}glow.Apply();
            sparks.SetTexture("_BaseMap",ToyBoxGeometry.Save(glow,Folder+"/SparkGlow.asset"));EditorUtility.SetDirty(sparks);
            var effects=new List<ParticleSystem>();
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 at=new Vector3(side*1.35f,.05f,0);
                Part(jets,"BrassFountainBase",ToyBoxGeometry.Disc(.13f,.08f,.025f),brass,at);
                effects.Add(Effects(jets,"SparkFountain"+side,at+Vector3.up*.05f,sparks,null,true,(uint)(100+side)));
            }
            var confettiMesh=ToyBoxGeometry.RoundedBox(new Vector3(1,.68f,.035f),.016f);
            var colors=new[]{new Color(.05f,.55f,.48f),new Color(1,.88f,.62f),new Color(1,.66f,.19f)};
            for(int i=0;i<3;i++)
            {
                var mat=Material("Confetti"+i,colors[i],i==2?.35f:0,.3f);mat.shader=Shader.Find("Universal Render Pipeline/Unlit");EditorUtility.SetDirty(mat);
                effects.Add(Effects(jets,"FallingConfetti"+i,new Vector3((i-1)*1.7f,.25f,-.4f),mat,confettiMesh,false,(uint)(300+i)));
            }
            view.celebrationEffects=effects.ToArray();banner.gameObject.SetActive(false);jets.gameObject.SetActive(false);
            view.presentation.gameObject.SetActive(false);
            WoodenFireworksBaker.Configure(view);
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{Folder}))AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid),ImportAssetOptions.ForceUpdate);return "Saved wooden banner, cream title, subtitle, confetti and twin fountains.";
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
