using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-built portal art; teleport triggers and partner wiring are preserved.</summary>
public static class BrassPortalBaker
{
    const string Folder="Assets/Art/BrassPortal";
    public static string Bake()
    {
        int pads=0,levels=0;
        foreach(var dir in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{dir}).Select(AssetDatabase.GUIDToAssetPath))
        {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Teleporter>(true).Any())continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{
                foreach(var pad in root.GetComponentsInChildren<Teleporter>(true)){Apply(pad);pads++;}
                foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="DP_TeleporterLink"))t.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,path);levels++;
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return $"Saved {pads} brass portals across {levels} level prefabs";
    }
    static Material Mat(string name,Color color,float metallic,float gloss)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",gloss);EditorUtility.SetDirty(m);return m;
    }
    public static void Apply(Teleporter pad)
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art","BrassPortal");
        ToyBoxGeometry.Initialize();
        var brass=Mat("SatinBrass",new Color(.80f,.49f,.13f),.72f,.56f);
        var teal=Mat("DeepTealEnamel",new Color(.008f,.17f,.16f),.17f,.5f);
        var mint=Mat("MintInlay",new Color(.12f,.83f,.67f),.12f,.45f);
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/Materials/NaturalBirch.mat");
        var old=pad.transform.Find("BrassPortalVisual");if(old!=null)Object.DestroyImmediate(old.gameObject);
        foreach(var r in pad.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        var visual=new GameObject("BrassPortalVisual").transform;
        visual.SetParent(pad.transform.parent,false);visual.position=new Vector3(pad.transform.position.x,.015f,pad.transform.position.z);visual.rotation=pad.transform.rotation;visual.SetParent(pad.transform,true);
        GameObject Part(string n,Vector3 pos,Mesh mesh,Material mat){var go=new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(visual,false);go.transform.localPosition=pos;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;}
        Part("MapleBase",new Vector3(0,.065f,0),ToyBoxGeometry.Disc(.51f,.13f,.025f),wood);
        Part("BrassBand",new Vector3(0,.095f,0),ToyBoxGeometry.Ring(.475f,.065f,.08f),brass);
        Part("TealWell",new Vector3(0,.137f,0),ToyBoxGeometry.Disc(.421f,.032f,.01f),teal);
        Part("RoundedBrassRim",new Vector3(0,.17f,0),ToyBoxGeometry.Torus(.455f,.047f),brass);
        Part("MintInnerRim",new Vector3(0,.168f,0),ToyBoxGeometry.Torus(.403f,.013f),mint);
        Part("Spiral",new Vector3(0,.174f,0),Spiral(),mint);
        for(int i=0;i<2;i++){
            var pin=Part("PairPin"+i,new Vector3((i-.5f)*.11f,.085f,-.492f),ToyBoxGeometry.Disc(.026f,.023f,.009f),brass);pin.transform.localRotation=Quaternion.Euler(90,0,0);
        }
    }
    static Mesh Spiral()
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Spiral.asset");if(existing!=null)return existing;
        var vertices=new List<Vector3>();var triangles=new List<int>();const int segments=100,sides=8;
        for(int i=0;i<=segments;i++){
            float t=(float)i/segments,a=t*Mathf.PI*3.4f,r=Mathf.Lerp(.035f,.285f,t);
            var center=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
            var tangent=new Vector3(.25f*Mathf.Cos(a)-r*3.4f*Mathf.PI*Mathf.Sin(a),0,.25f*Mathf.Sin(a)+r*3.4f*Mathf.PI*Mathf.Cos(a)).normalized;
            var side=Vector3.Cross(tangent,Vector3.up);
            for(int j=0;j<sides;j++){float b=j*Mathf.PI*2/sides;vertices.Add(center+(side*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b))*.019f);}
            if(i==0)continue;
            for(int j=0;j<sides;j++){int a0=(i-1)*sides+j,b0=(i-1)*sides+(j+1)%sides,c=i*sides+j,d=i*sides+(j+1)%sides;triangles.AddRange(new[]{a0,c,b0,b0,c,d});}
        }
        var mesh=new Mesh{name="Raised mint spiral"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return ToyBoxGeometry.Save(mesh,Folder+"/Spiral.asset");
    }
}
