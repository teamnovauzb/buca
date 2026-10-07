using System;
using UnityEditor;
using UnityEngine;
public static class ToyBoxMeshOptimizerValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static float Area(Vector2[] p){float a=0;for(int i=0;i<p.Length;i++){var b=p[(i+1)%p.Length];a+=p[i].x*b.y-b.x*p[i].y;}return a*.5f;}
    public static string Run()
    {
        var key=ToyBoxMeshOptimizer.TextKey.Match("Text_BUCA_0.160_Bevel_0.020_Weight_0.025");
        Check(key.Success && key.Groups[1].Value=="BUCA" && key.Groups[2].Value=="0.160" && key.Groups[3].Value=="0.020", "Text asset parameters parsed as visible text");
        var font=JsonUtility.FromJson<ToyBoxGeometry.FontData>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ToyBox/RoundedGlyphContours.json").text);
        int before=0,after=0;float maxError=0;
        foreach(var glyph in font.glyphs)foreach(var contour in glyph.contours)
        {
            var p=contour.points;var q=ToyBoxGlyphSimplifier.Simplify(p);before+=p.Length;after+=q.Length;
            Check(Mathf.Sign(Area(p))==Mathf.Sign(Area(q)),"Contour winding changed: "+glyph.character);
            Check(Mathf.Abs(Area(p)-Area(q))<.005f,"Glyph area changed: "+glyph.character);
            foreach(var point in p)
            {
                float nearest=float.MaxValue;for(int i=0;i<q.Length;i++)nearest=Mathf.Min(nearest,ToyBoxGlyphSimplifier.DistanceSquared(point,q[i],q[(i+1)%q.Length]));
                maxError=Mathf.Max(maxError,Mathf.Sqrt(nearest));
                Check(nearest<=ToyBoxGlyphSimplifier.Tolerance*ToyBoxGlyphSimplifier.Tolerance*1.01f,"Outline exceeded tolerance");
            }
        }
        var mesh=new Mesh();
        try
        {
            mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.zero,Vector3.right,Vector3.up,Vector3.zero};
            mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back,Vector3.back,Vector3.back,Vector3.forward};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.zero,Vector2.right,Vector2.one,Vector2.zero};
            mesh.triangles=new[]{0,1,2,3,4,5,6,1,2};var positions=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var indices=mesh.triangles;
            Check(ToyBoxMeshOptimizer.Weld(mesh),"Duplicate welding did not run");Check(mesh.vertexCount==5,"UV seam or hard normal lost");
            var actual=mesh.triangles;
            for(int i=0;i<indices.Length;i++)Check(mesh.vertices[actual[i]]==positions[indices[i]] && mesh.normals[actual[i]]==normals[indices[i]] && mesh.uv[actual[i]]==uv[indices[i]],"Triangle attributes changed");
            Check(!ToyBoxMeshOptimizer.Weld(mesh),"Welding is not idempotent");
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);}
        string result=$"PASS: all glyph winding/area/error checks; outline points {before} -> {after}; maximum outline error {maxError:F7}. Exact vertex welding preserves triangle positions, hard normals and UV seams; second pass is unchanged.";
        System.IO.File.WriteAllText("output/geometry-optimization/validation.txt",result);return result;
    }
}
