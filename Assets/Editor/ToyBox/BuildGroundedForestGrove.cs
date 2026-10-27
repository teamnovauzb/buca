#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Fourteen individually placed solid birches, replacing the sixty image cutouts.</summary>
public static class BuildGroundedForestGrove
{
    const string Folder="Assets/TwilightMap/ForestHarbor/GroundedTrees";
    const int Layer=26;
    // x/y are the ground-contact points in the map; z is approximate projected height.
    static readonly Vector3[] Planting={
        new(64,91,72),new(284,104,67),new(520,156,63),new(640,168,57),
        new(181,296,72),new(368,387,80),new(540,397,78),new(708,449,77),
        new(53,532,80),new(518,619,69),new(675,615,72),
        new(58,785,88),new(210,789,72),new(728,764,78)
    };

    [MenuItem("RealBuca/Forest Harbor/Replace Flat Trees With Grounded Grove")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu map required.");
        var treeShader=Shader.Find("BUCA/Grounded Birch");var shadowShader=Shader.Find("BUCA/Tree Ground Shadow");
        if(treeShader==null||shadowShader==null||ShaderUtil.ShaderHasError(treeShader)||ShaderUtil.ShaderHasError(shadowShader))
            throw new Exception("Tree shaders must compile before installation.");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("output/grounded-forest");
        var art=map.illustratedMenu.artwork;
        var flat=art.Find("LeafyBirchGrove");
        if(flat!=null){Undo.RecordObject(flat.gameObject,"Replace flat forest trees");flat.gameObject.SetActive(false);}
        var old=map.transform.Find("GroundedForestGrove");
        if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
        var oldPicture=art.Find("GroundedForestView");
        if(oldPicture!=null)Undo.DestroyObjectImmediate(oldPicture.gameObject);

        var root=Node(map.transform,"GroundedForestGrove");
        Undo.RegisterCreatedObjectUndo(root.gameObject,"Plant grounded forest");
        root.localPosition=new Vector3(0,-1600,0);
        var grove=root.gameObject.AddComponent<GroundedForestGrove>();
        var ui=new GameObject("GroundedForestView",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.RawImage));
        ui.layer=5;ui.transform.SetParent(art,false);
        Undo.RegisterCreatedObjectUndo(ui,"Show grounded forest");
        var rect=(RectTransform)ui.transform;
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.sizeDelta=new Vector2(850,941);rect.anchoredPosition=new Vector2(425-836,0);
        // Trees render behind the numbered stops and the travelling locomotive.
        rect.SetSiblingIndex(art.Find("RoundForestRailway").GetSiblingIndex()+1);
        grove.picture=ui.GetComponent<UnityEngine.UI.RawImage>();grove.picture.raycastTarget=false;

        var camera=Node(root,"ForestCamera").gameObject.AddComponent<Camera>();grove.forestCamera=camera;
        camera.transform.localPosition=new Vector3(0,40,-69.282032f);
        camera.transform.localRotation=Quaternion.LookRotation(-camera.transform.localPosition);
        camera.orthographic=true;camera.orthographicSize=941f/40*root.lossyScale.x;
        camera.nearClipPlane=.1f;camera.farClipPlane=80;camera.aspect=850f/941;
        camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.allowHDR=false;camera.allowMSAA=true;camera.depth=-11;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(Folder+"/GroundedForest.renderTexture");
        if(rt==null)
        {
            rt=new RenderTexture(1700,1882,24,RenderTextureFormat.ARGB32){name="Grounded forest",antiAliasing=4};
            AssetDatabase.CreateAsset(rt,Folder+"/GroundedForest.renderTexture");
        }
        camera.targetTexture=rt;grove.picture.texture=rt;
        var treeMaterial=Material("LivingBirch",treeShader);var shadowMaterial=Material("GroundContact",shadowShader);
        var models=new Mesh[4];for(int i=0;i<4;i++)models[i]=BuildTree(i);
        var shadow=BuildShadow();
        grove.roots=new Transform[Planting.Length];grove.trees=new MeshRenderer[Planting.Length];
        grove.artworkRoots=new Vector2[Planting.Length];
        for(int i=0;i<Planting.Length;i++)
        {
            var point=Planting[i];var tree=Node(root,"Birch_"+(i+1).ToString("00"));
            tree.localPosition=new Vector3((point.x-425)/20,0,(470.5f-point.y)/10);
            tree.localScale=Vector3.one*(point.z/79);
            tree.localRotation=Quaternion.Euler(0,(i*137.5f)%360,0);
            grove.roots[i]=tree;grove.artworkRoots[i]=new Vector2(point.x,point.y);
            var model=Node(tree,"SolidTrunkBranchesAndLeaves");
            model.gameObject.AddComponent<MeshFilter>().sharedMesh=models[i%4];
            var renderer=model.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=treeMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            grove.trees[i]=renderer;
            // Shadows are anchored in world direction, independent from each tree's rotation.
            var ground=Node(tree,"GroundContactAndCanopyShadow");
            ground.localRotation=Quaternion.Inverse(tree.localRotation);
            ground.gameObject.AddComponent<MeshFilter>().sharedMesh=shadow;
            ground.gameObject.AddComponent<MeshRenderer>().sharedMaterial=shadowMaterial;
        }
        grove.Pose(0);
        EditorUtility.SetDirty(grove);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return "Saved 14 solid 3D birches in place of 60 flat cutouts: flared roots, branching trunks, volumetric foliage, grass collars, fixed ground shadows and gentle crown movement.";
    }

    static Material Material(string name,Shader shader)
    {
        var path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader){name=name};AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;EditorUtility.SetDirty(material);return material;
    }
    static Transform Node(Transform parent,string name)
    {
        var go=new GameObject(name);go.layer=Layer;go.transform.SetParent(parent,false);return go.transform;
    }

    static Mesh BuildTree(int variant)
    {
        var b=new MeshBuilder();var random=new System.Random(1297+variant*101);
        float lean=(variant-1.5f)*.045f;
        Vector3[] trunk={new(0,-.04f,0),new(0,.18f,0),new(.02f,.8f,0),new(-.04f+lean,1.5f,.01f),
            new(.03f+lean,2.2f,.02f),new(.1f+lean,3,.04f),new(.14f+lean,3.8f,.05f)};
        b.Tube(trunk,new[]{.21f,.14f,.105f,.088f,.065f,.043f,.012f},new Color(.91f,.88f,.72f),0);
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.PI/3+variant*.4f;var direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            b.Tube(new[]{Vector3.up*.15f,direction*.19f+Vector3.up*.045f,direction*.40f-Vector3.up*.025f},
                new[]{.10f,.064f,.012f},new Color(.51f,.43f,.24f),0);
        }
        Vector3[] centers={new(-.62f,2.50f,.08f),new(.57f,2.75f,.05f),new(-.33f,3.32f,.20f),
            new(.32f,3.53f,-.15f),new(.08f,3.96f,.05f),new(.02f,2.99f,-.45f),new(-.28f,2.81f,.55f)};
        Vector3[] radii={new(.66f,.69f,.58f),new(.68f,.77f,.62f),new(.62f,.78f,.62f),
            new(.64f,.68f,.61f),new(.46f,.56f,.47f),new(.62f,.66f,.54f),new(.60f,.63f,.57f)};
        for(int i=0;i<centers.Length;i++)
        {
            var center=centers[i]+new Vector3(((float)random.NextDouble()-.5f)*.14f,0,0);
            var start=new Vector3(lean,(1.2f+i*.24f),.01f);
            b.Tube(new[]{start,Vector3.Lerp(start,center,.55f),center},
                new[]{.048f,.028f,.008f},new Color(.78f,.76f,.58f),0);
            Color green=Color.Lerp(new Color(.32f,.53f,.06f),new Color(.51f,.68f,.10f),(i%4)/3f);
            b.Lobe(center,radii[i],green,random.Next(1000),14,10);
            // Solid leaf buds break up the crown contour without alpha-cutout cards.
            for(int leaf=0;leaf<15;leaf++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                float y=(float)random.NextDouble()*1.6f-.8f;
                float radius=Mathf.Sqrt(1-y*y);
                var outward=new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);
                var position=center+Vector3.Scale(outward,radii[i])*.98f;
                var size=new Vector3(.12f,.055f,.075f)*(1+(float)random.NextDouble()*.3f);
                b.Lobe(position,size,green*1.10f,leaf+variant,6,4,Quaternion.FromToRotation(Vector3.up,outward));
            }
        }
        for(int i=0;i<24;i++)
        {
            float a=i*2.39996f;float r=.20f+(i%4)*.055f;
            var p=new Vector3(Mathf.Cos(a)*r,.01f,Mathf.Sin(a)*r);
            b.Tube(new[]{p,p+new Vector3(Mathf.Cos(a)*.065f,.18f+(i%3)*.045f,Mathf.Sin(a)*.065f)},
                new[]{.028f,.001f},new Color(.43f,.53f,.08f),1,5);
        }
        return b.Save(Folder+"/BirchVariant"+variant+".asset");
    }

    static Mesh BuildShadow()
    {
        var b=new MeshBuilder();
        b.Shadow(new Vector3(.55f,.003f,.50f),new Vector2(3.2f,2.4f),.32f);
        b.Shadow(new Vector3(.18f,.005f,.12f),new Vector2(1.25f,.95f),.25f);
        b.Shadow(new Vector3(0,.008f,0),new Vector2(.80f,.68f),.48f);
        return b.Save(Folder+"/TreeShadow.asset");
    }

    sealed class MeshBuilder
    {
        readonly List<Vector3> vertices=new(), normals=new();
        readonly List<Vector2> uv=new(), kinds=new();
        readonly List<Color> colors=new();
        readonly List<int> triangles=new();
        int Add(Vector3 p,Vector3 n,Color color,Vector2 tex,float kind)
        {
            int i=vertices.Count;vertices.Add(p);normals.Add(n);colors.Add(color);uv.Add(tex);kinds.Add(new Vector2(kind,0));return i;
        }
        void Tri(int a,int b,int c)
        {
            if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a]+normals[b]+normals[c])<0)
                (b,c)=(c,b);
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        }
        public void Tube(Vector3[] path,float[] radius,Color color,float kind,int sides=10)
        {
            int first=vertices.Count;
            for(int row=0;row<path.Length;row++)
            {
                Vector3 direction=(path[Mathf.Min(row+1,path.Length-1)]-path[Mathf.Max(0,row-1)]).normalized;
                Vector3 x=Vector3.Cross(direction,Vector3.forward).normalized;if(x.sqrMagnitude<.5f)x=Vector3.right;
                Vector3 z=Vector3.Cross(direction,x).normalized;
                for(int side=0;side<=sides;side++)
                {
                    float a=side*Mathf.PI*2/sides;
                    Vector3 normal=x*Mathf.Cos(a)+z*Mathf.Sin(a);
                    Color tint=kind<.5f?Color.Lerp(new Color(.43f,.37f,.20f),color,Mathf.SmoothStep(0,1,path[row].y/.38f)):color;
                    Add(path[row]+normal*radius[row],normal,tint,new Vector2(side/(float)sides,path[row].y),kind);
                    if(row==0||side==0)continue;
                    int v=first+row*(sides+1)+side;Tri(v-sides-2,v-1,v);Tri(v-sides-2,v,v-sides-1);
                }
            }
        }
        public void Lobe(Vector3 center,Vector3 radius,Color color,int seed,int longitude,int latitude,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            int first=vertices.Count;
            for(int y=0;y<=latitude;y++)for(int x=0;x<=longitude;x++)
            {
                float a=x*Mathf.PI*2/longitude,p=y*Mathf.PI/latitude;
                var n=new Vector3(Mathf.Cos(a)*Mathf.Sin(p),Mathf.Cos(p),Mathf.Sin(a)*Mathf.Sin(p));
                float bump=1+.065f*Mathf.Sin(a*3+seed)*Mathf.Sin(p*2)+.035f*Mathf.Cos(p*7+seed)*Mathf.Sin(p);
                var normal=new Vector3(n.x/radius.x,n.y/radius.y,n.z/radius.z).normalized;
                Color tint=color*Mathf.Lerp(.88f,1.1f,(n.y+1)*.5f);tint.a=1;
                Add(center+rotation*(Vector3.Scale(n,radius)*bump),rotation*normal,tint,new Vector2(x/(float)longitude,y/(float)latitude),1);
                if(x==0||y==0)continue;
                int v=first+y*(longitude+1)+x;Tri(v-longitude-2,v-1,v);Tri(v-longitude-2,v,v-longitude-1);
            }
        }
        public void Shadow(Vector3 center,Vector2 size,float alpha)
        {
            var color=new Color(.035f,.045f,.015f,alpha);
            int a=Add(center+new Vector3(-size.x/2,0,-size.y/2),Vector3.up,color,Vector2.zero,2);
            Add(center+new Vector3(size.x/2,0,-size.y/2),Vector3.up,color,Vector2.right,2);
            Add(center+new Vector3(size.x/2,0,size.y/2),Vector3.up,color,Vector2.one,2);
            Add(center+new Vector3(-size.x/2,0,size.y/2),Vector3.up,color,Vector2.up,2);
            Tri(a,a+1,a+2);Tri(a,a+2,a+3);
        }
        public Mesh Save(string path)
        {
            var mesh=new Mesh{name=Path.GetFileNameWithoutExtension(path),indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetUVs(1,kinds);
            mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            // Wind stays inside this expanded bound even at its maximum crown displacement.
            var bounds=mesh.bounds;bounds.Expand(.7f);mesh.bounds=bounds;
            return ToyBoxGeometry.Save(mesh,path);
        }
    }
}
#endif
