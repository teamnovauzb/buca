#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class AllAboardCompletionBaker
{
    struct CityIlluminant { public Vector3 position; public float power,range; }
    static readonly List<CityIlluminant> cityLights=new List<CityIlluminant>();
    static Material lightPool;

    static void CityLight(Transform parent,Vector3 at,float power,float range)
    {
        cityLights.Add(new CityIlluminant {position=parent.TransformPoint(at),power=power,range=range*parent.lossyScale.x});
    }
    static void LightPool(Transform parent,Vector3 world,float width,float depth,float intensity)
    {
        if(lightPool==null)lightPool=ToyBoxGeometry.Save(new Material(Shader.Find("BUCA/Station Light Pool")),Folder+"/LanternPool.mat");
        lightPool.SetColor("_Color",new Color(1,.49f,.12f,intensity));EditorUtility.SetDirty(lightPool);
        Contact(parent,"LanternLightOnPaving",parent.InverseTransformPoint(world),width*2,depth*2);
        parent.GetChild(parent.childCount-1).GetComponent<MeshRenderer>().sharedMaterial=lightPool;
    }

    // Persist illumination in UV3: no runtime light loop, shadow map, texture
    // lookup, or effect on the lighting of other game screens.
    static void BakeCityLight(Transform stage)
    {
        var done=new HashSet<Mesh>();
        var litMaterials=new Dictionary<Material,Material>();
        foreach(var f in stage.GetComponentsInChildren<MeshFilter>(true))
        {
            var r=f.GetComponent<MeshRenderer>();var mesh=f.sharedMesh;
            if(r==null||r.sharedMaterial.shader.name!="BUCA/Station Toy"||!f.name.StartsWith("SavedBatch")||!done.Add(mesh))continue;
            if(f.transform.parent.name=="CityLightsBackground")SubdivideCityFaces(mesh);
            var vertices=mesh.vertices;var normals=mesh.normals;var irradiance=new List<Vector3>(vertices.Length);
            var matrix=f.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 p=matrix.MultiplyPoint3x4(vertices[i]),n=normalMatrix.MultiplyVector(normals[i]).normalized;
                float energy=0;
                foreach(var lamp in cityLights)
                {
                    Vector3 d=lamp.position-p;float squared=d.sqrMagnitude,rangeSquared=lamp.range*lamp.range;
                    if(squared>=rangeSquared)continue;
                    float falloff=1-squared/rangeSquared;
                    float facing=Mathf.Clamp01(Vector3.Dot(n,d/Mathf.Sqrt(Mathf.Max(squared,.001f)))*.85f+.15f);
                    energy+=lamp.power*falloff*falloff*facing/(1+squared*.38f);
                }
                irradiance.Add(new Vector3(1,.51f,.16f)*Mathf.Min(energy,5));
            }
            mesh.SetUVs(2,irradiance);EditorUtility.SetDirty(mesh);
            var original=r.sharedMaterial;
            if(!litMaterials.TryGetValue(original,out var lit))
            {
                lit=new Material(original);lit.SetFloat("_BakedLight",1);
                string name=System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(original));
                lit=ToyBoxGeometry.Save(lit,Folder+"/"+name+"_Illuminated.mat");litMaterials.Add(original,lit);
            }
            r.sharedMaterial=lit;
        }
    }
    struct LightVertex {public Vector3 p,n;public Vector2 uv;}
    static LightVertex Mid(LightVertex a,LightVertex b)=>new LightVertex {p=(a.p+b.p)*.5f,n=(a.n+b.n).normalized,uv=(a.uv+b.uv)*.5f};
    static void SubdivideCityFaces(Mesh mesh)
    {
        var source=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var triangles=mesh.triangles;
        var vertices=new List<Vector3>();var outputNormals=new List<Vector3>();var outputUv=new List<Vector2>();var outputTriangles=new List<int>();
        void Triangle(LightVertex a,LightVertex b,LightVertex c,int depth)
        {
            float ab=(a.p-b.p).sqrMagnitude,bc=(b.p-c.p).sqrMagnitude,ca=(c.p-a.p).sqrMagnitude;
            // Split long planar spans so a lamp's falloff is smooth across the wall.
            if(depth<10&&Mathf.Max(ab,Mathf.Max(bc,ca))>2.25f)
            {
                if(ab>=bc&&ab>=ca){var m=Mid(a,b);Triangle(a,m,c,depth+1);Triangle(m,b,c,depth+1);}
                else if(bc>=ca){var m=Mid(b,c);Triangle(a,b,m,depth+1);Triangle(a,m,c,depth+1);}
                else {var m=Mid(c,a);Triangle(a,b,m,depth+1);Triangle(m,b,c,depth+1);}
                return;
            }
            foreach(var v in new[]{a,b,c}){outputTriangles.Add(vertices.Count);vertices.Add(v.p);outputNormals.Add(v.n);outputUv.Add(v.uv);}
        }
        LightVertex Vertex(int index)=>new LightVertex {p=source[index],n=normals[index],uv=uv.Length==source.Length?uv[index]:Vector2.zero};
        for(int i=0;i<triangles.Length;i+=3)Triangle(Vertex(triangles[i]),Vertex(triangles[i+1]),Vertex(triangles[i+2]),0);
        mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(outputNormals);mesh.SetUVs(0,outputUv);mesh.SetTriangles(outputTriangles,0);mesh.RecalculateBounds();
        ToyBoxMeshOptimizer.Weld(mesh);
    }
}
#endif
