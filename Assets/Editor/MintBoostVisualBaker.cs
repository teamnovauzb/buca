using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Authored mint/teal/brass boost inlays. All meshes and materials are saved before play.</summary>
public static class MintBoostVisualBaker
{
    const string Folder="Assets/Art/MintBoost";
    [MenuItem("RealBuca/Prebuild/Mint Boost Inlays")]
    public static void Bake()
    {
        foreach(var directory in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{directory}).Select(AssetDatabase.GUIDToAssetPath))
        {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<SpeedBoost>(true).Any())continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var boost in root.GetComponentsInChildren<SpeedBoost>(true))Apply(boost);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    public static void Apply(SpeedBoost boost)
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art","MintBoost");
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/RoundedChevron.asset");
        if(mesh==null){mesh=Chevron();AssetDatabase.CreateAsset(mesh,Folder+"/RoundedChevron.asset");}
        var brass=Material("BrushedBrass",new Color(.64f,.43f,.19f),.40f,.5f);
        var teal=Material("DeepTealEdge",new Color(.018f,.19f,.20f),.43f,.05f);
        var mint=Material("MintEnamel",new Color(.27f,.78f,.59f),.48f,0);
        var renderer=boost.GetComponent<MeshRenderer>();if(renderer!=null)renderer.enabled=false;
        var previous=boost.transform.Find("MintSpeedInlays");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        var visual=new GameObject("MintSpeedInlays").transform;visual.SetParent(boost.transform,false);
        visual.position=new Vector3(boost.transform.position.x,.012f,boost.transform.position.z);
        visual.rotation=Quaternion.Euler(0,boost.transform.eulerAngles.y,0);
        var scale=boost.transform.lossyScale;visual.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        for(int arrow=0;arrow<3;arrow++)
        for(int layer=0;layer<3;layer++)
        {
            var go=new GameObject(new[]{"BrassOutline","TealBevel","MintFace"}[layer]+arrow,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(visual,false);go.transform.localPosition=new Vector3(0,layer*.012f,(arrow-1)*.52f-.135f);
            float size=new[]{.84f,.77f,.65f}[layer];go.transform.localScale=new Vector3(size,layer==2?.65f:1,size);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=new[]{brass,teal,mint}[layer];
        }
        var box=boost.GetComponent<BoxCollider>();
        if(box!=null){var size=box.size;size.x=.84f/Mathf.Abs(scale.x);size.z=1.55f/Mathf.Abs(scale.z);box.size=size;box.isTrigger=true;}
    }
    static Material Material(string name,Color color,float smooth,float metal)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smooth);material.SetFloat("_Metallic",metal);EditorUtility.SetDirty(material);return material;
    }
    static Mesh Chevron()
    {
        var corners=new[]{new Vector2(-.5f,-.16f),new Vector2(0,.11f),new Vector2(.5f,-.16f),new Vector2(.5f,.13f),new Vector2(0,.43f),new Vector2(-.5f,.13f)};
        var outline=new List<Vector2>();
        for(int i=0;i<corners.Length;i++)
        {
            var p=corners[i];var a=Vector2.Lerp(p,corners[(i+5)%6],.13f);var b=Vector2.Lerp(p,corners[(i+1)%6],.13f);
            for(int j=0;j<=6;j++){float t=j/6f;outline.Add((1-t)*(1-t)*a+2*(1-t)*t*p+t*t*b);}
        }
        var vertices=new List<Vector3>();var triangles=new List<int>();int n=outline.Count;
        float[] sizes={1,1,.95f,.91f},heights={0,.012f,.027f,.032f};
        for(int ring=0;ring<4;ring++)foreach(var p in outline)vertices.Add(new Vector3(p.x*sizes[ring],heights[ring],(p.y-.18f)*sizes[ring]+.18f));
        void Tri(int a,int b,int c,Vector3 normal)
        {if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normal)>=0)triangles.AddRange(new[]{a,b,c});else triangles.AddRange(new[]{a,c,b});}
        for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++)
        {
            int j=(i+1)%n;var edge=outline[j]-outline[i];var outward=new Vector3(edge.y,0,-edge.x);
            Tri(ring*n+i,ring*n+j,(ring+1)*n+i,outward);Tri(ring*n+j,(ring+1)*n+j,(ring+1)*n+i,outward);
        }
        int center=vertices.Count;vertices.Add(new Vector3(0,.032f,.20f));
        for(int i=0;i<n;i++)Tri(center,3*n+i,3*n+(i+1)%n,Vector3.up);
        var mesh=new Mesh{name="Soft beveled chevron"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
