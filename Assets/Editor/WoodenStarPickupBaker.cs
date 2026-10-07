using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Builds saved meshes/materials only in the Editor; gameplay loads the authored prefab.</summary>
public static class WoodenStarPickupBaker
{
    const string Folder = "Assets/Art/WoodenStarPickup";
    const string PrefabPath = "Assets/Prefabs/Prebuilt/PreferredRouteMarker.prefab";

    [MenuItem("RealBuca/Prebuild/Wooden Star Bonuses")]
    public static void Bake()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art", "WoodenStarPickup");
        var wood = Material("Natural wood edges", new Color(.72f,.47f,.23f), .27f);
        var paint = Material("Golden yellow paint", new Color(1f,.64f,.025f), .39f);
        var teal = Material("Teal enamel center", new Color(.015f,.52f,.53f), .58f);
        var grain = new Texture2D(128,128,TextureFormat.RGBA32,false);
        grain.name="Subtle wood grain"; grain.wrapMode=TextureWrapMode.Repeat;
        for(int y=0;y<128;y++) for(int x=0;x<128;x++)
        {
            float wave=Mathf.Sin(y*.65f+Mathf.PerlinNoise(x*.018f,y*.014f)*5f);
            float value=.91f+.06f*wave+.03f*Mathf.PerlinNoise(x*.3f,y*.3f);
            grain.SetPixel(x,y,new Color(value,value,value,1));
        }
        grain.Apply(); Save(grain,Folder+"/WoodGrain.asset");
        wood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WoodGrain.asset"));
        EditorUtility.SetDirty(wood);
        var outline=Outline();
        var shell=Solid(outline,new float[]{.88f,.96f,1,1,.96f,.88f},new float[]{-.24f,-.22f,-.15f,.15f,.22f,.24f},"Rounded wooden star");
        var face=Solid(outline,new float[]{.84f,.89f,.91f},new float[]{-.275f,-.265f,-.225f},"Inset yellow star face");
        Save(shell,Folder+"/WoodenStar.asset"); Save(face,Folder+"/PaintedFace.asset");
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach(var child in new List<Transform>(Children(root.transform))) Object.DestroyImmediate(child.gameObject);
            var filter=root.GetComponent<MeshFilter>(); if(filter) Object.DestroyImmediate(filter);
            var renderer=root.GetComponent<MeshRenderer>(); if(renderer) Object.DestroyImmediate(renderer);
            MeshPart(root,"WoodenStar",AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/WoodenStar.asset"),wood);
            MeshPart(root,"YellowPaint",AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/PaintedFace.asset"),paint);
            var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere); dot.name="TealCenter";
            dot.transform.SetParent(root.transform,false);
            dot.transform.localPosition=new Vector3(0,.25f,-.29f);
            dot.transform.localScale=new Vector3(.34f,.34f,.14f);
            Object.DestroyImmediate(dot.GetComponent<Collider>());
            dot.GetComponent<MeshRenderer>().sharedMaterial=teal;
            var pickup=root.GetComponent<ScorePickup>();
            pickup.grantsHeart=true; pickup.idleSpinSpeed=0; pickup.idleBobAmplitude=.015f;
            // Preserve root scale, trigger radius, score and Undo behavior.
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    static IEnumerable<Transform> Children(Transform t) { foreach(Transform c in t) yield return c; }
    static void MeshPart(GameObject root,string name,Mesh mesh,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(root.transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    static Material Material(string name,Color color,float smooth)
    {
        string path=Folder+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",smooth); mat.SetFloat("_Metallic",0);
        EditorUtility.SetDirty(mat); return mat;
    }
    static void Save(Object value,string path)
    {
        var old=AssetDatabase.LoadMainAssetAtPath(path);
        if(old) { EditorUtility.CopySerialized(value,old); Object.DestroyImmediate(value); EditorUtility.SetDirty(old); }
        else AssetDatabase.CreateAsset(value,path);
    }
    static List<Vector2> Outline()
    {
        var points=new Vector2[10];
        for(int i=0;i<10;i++) { float a=(90-i*36)*Mathf.Deg2Rad; points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?1.05f:.53f); }
        var result=new List<Vector2>();
        for(int i=0;i<10;i++)
        {
            Vector2 p=points[i],a=Vector2.Lerp(p,points[(i+9)%10],.22f),b=Vector2.Lerp(p,points[(i+1)%10],.22f);
            for(int j=0;j<=6;j++) { float t=j/6f; result.Add((1-t)*(1-t)*a+2*(1-t)*t*p+t*t*b); }
        }
        return result;
    }
    static Mesh Solid(List<Vector2> outline,float[] sizes,float[] depths,string name)
    {
        var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>(); int n=outline.Count;
        for(int r=0;r<sizes.Length;r++) foreach(var p in outline)
        { vertices.Add(new Vector3(p.x*sizes[r],p.y*sizes[r]+.25f,depths[r])); uv.Add(p*.5f+Vector2.one*.5f); }
        for(int r=0;r<sizes.Length-1;r++) for(int i=0;i<n;i++)
        { int a=r*n+i,b=r*n+(i+1)%n,c=(r+1)*n+i,d=(r+1)*n+(i+1)%n; triangles.AddRange(new[]{a,c,b,b,c,d}); }
        // Clockwise outline gives the front cap its -Z normal.
        int front=vertices.Count; vertices.Add(new Vector3(0,.25f,depths[0])); uv.Add(Vector2.one*.5f);
        int back=vertices.Count; vertices.Add(new Vector3(0,.25f,depths[depths.Length-1])); uv.Add(Vector2.one*.5f);
        for(int i=0;i<n;i++) { triangles.AddRange(new[]{front,i,(i+1)%n}); int a=(sizes.Length-1)*n; triangles.AddRange(new[]{back,a+(i+1)%n,a+i}); }
        var mesh=new Mesh {name=name}; mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
