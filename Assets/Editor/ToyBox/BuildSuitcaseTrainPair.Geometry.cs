#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class BuildSuitcaseTrainPair
{
    static Mesh Finish(string key,List<Vector3> v,List<int> triangles,List<Vector3> normals=null,List<Vector2> uv=null)
    {
        var mesh=new Mesh{name=key,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);
        if(normals==null)mesh.RecalculateNormals();else mesh.SetNormals(normals);
        if(uv!=null){mesh.SetUVs(0,uv);mesh.RecalculateTangents();}
        mesh.RecalculateBounds();mesh=Save(mesh,Folder+"/Meshes/"+key+".asset");meshes[key]=mesh;return mesh;
    }
    static string Key(string name,IEnumerable<float> values)=>name+"-"+string.Join("_",values.Select(v=>v.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)));
    static Mesh BoxMesh(Vector3 size,float radius)
    {
        string key=Key("BevelBox",new[]{size.x,size.y,size.z,radius});if(meshes.TryGetValue(key,out var cached))return cached;
        radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.49f);
        var half=size*.5f;var inner=half-Vector3.one*radius;
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
        foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            Vector3 u=normal.x!=0?Vector3.forward:Vector3.right,w=Vector3.Cross(normal,u);
            float fu=radius/Vector3.Dot(half,new Vector3(Mathf.Abs(u.x),Mathf.Abs(u.y),Mathf.Abs(u.z)));
            float fw=radius/Vector3.Dot(half,new Vector3(Mathf.Abs(w.x),Mathf.Abs(w.y),Mathf.Abs(w.z)));
            int start=v.Count;
            for(int y=0;y<=6;y++)for(int x=0;x<=6;x++)
            {
                var p=Vector3.Scale(normal+u*Edge(x,fu)+w*Edge(y,fw),half);
                var q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                var outward=(p-q).normalized;v.Add(q+outward*radius);n.Add(outward);uv.Add(new Vector2(Vector3.Dot(p,u),Vector3.Dot(p,w)));
                if(x<6&&y<6){int a=start+y*7+x;tr.AddRange(new[]{a,a+1,a+8,a,a+8,a+7});}
            }
        }
        return Finish(key,v,tr,n,uv);
    }
    static float Edge(int i,float fraction)
    {if(i==0)return -1;if(i==1)return -1+fraction*.3f;if(i==2)return -1+fraction;if(i==3)return 0;if(i==4)return 1-fraction;if(i==5)return 1-fraction*.3f;return 1;}
    static Mesh TurnedMesh(Vector2[] profile)
    {
        string key=Key("Turned",profile.SelectMany(p=>new[]{p.x,p.y}));if(meshes.TryGetValue(key,out var cached))return cached;
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int sides=56;
        for(int row=0;row<profile.Length;row++)
        {
            Vector2 tangent=profile[Mathf.Min(row+1,profile.Length-1)]-profile[Mathf.Max(row-1,0)];
            for(int i=0;i<=sides;i++)
            {
                float a=i*Mathf.PI*2/sides;float c=Mathf.Cos(a),s=Mathf.Sin(a);
                v.Add(new Vector3(c*profile[row].x,profile[row].y,s*profile[row].x));
                n.Add(new Vector3(tangent.y*c,-tangent.x,tangent.y*s).normalized);uv.Add(new Vector2(i/(float)sides,profile[row].y));
                if(row>0&&i<sides){int q=row*(sides+1)+i,p=q-sides-1;tr.AddRange(new[]{p,q,q+1,p,q+1,p+1});}
            }
        }
        // Hash long lathe profiles to keep mesh filenames inside platform path limits.
        key="Turned-"+Hash128.Compute(key).ToString();
        if(meshes.TryGetValue(key,out cached))return cached;
        return Finish(key,v,tr,n,uv);
    }
    static Mesh TorusMesh(float radius,float tube)
    {
        string key=Key("RoundRim",new[]{radius,tube});if(meshes.TryGetValue(key,out var cached))return cached;
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int segments=56,sides=10;
        for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++)
        {
            float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;
            var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var normal=radial*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b);
            v.Add(radial*radius+normal*tube);n.Add(normal);uv.Add(new Vector2(i/(float)segments,j/(float)sides));
            if(i<segments&&j<sides){int p=i*(sides+1)+j,q=p+sides+1;tr.AddRange(new[]{p,p+1,q+1,p,q+1,q});}
        }
        return Finish(key,v,tr,n,uv);
    }
    static Mesh SphereMesh()
    {
        const string key="SmoothSphere";if(meshes.TryGetValue(key,out var cached))return cached;
        var temporary=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh=Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(temporary);
        mesh.name=key;mesh=Save(mesh,Folder+"/Meshes/"+key+".asset");meshes[key]=mesh;return mesh;
    }
    static Mesh Extrude(Vector2[] profile,float depth)
    {
        string key="Profile-"+Hash128.Compute(Key("P",profile.SelectMany(p=>new[]{p.x,p.y}).Concat(new[]{depth}))).ToString();
        if(meshes.TryGetValue(key,out var cached))return cached;
        var v=new List<Vector3>();var tr=new List<int>();
        foreach(float side in new[]{-1f,1f})
        {
            int start=v.Count;v.Add(new Vector3(0,0,side*depth/2));
            foreach(var p in profile)v.Add(new Vector3(p.x,p.y,side*depth/2));
            for(int i=0;i<profile.Length;i++)
            {int a=start+1+i,b=start+1+(i+1)%profile.Length;if(side>0)tr.AddRange(new[]{start,a,b});else tr.AddRange(new[]{start,b,a});}
        }
        for(int i=0;i<profile.Length;i++)
        {
            var a=profile[i];var b=profile[(i+1)%profile.Length];int p=v.Count;
            v.Add(new Vector3(a.x,a.y,-depth/2));v.Add(new Vector3(b.x,b.y,-depth/2));v.Add(new Vector3(b.x,b.y,depth/2));v.Add(new Vector3(a.x,a.y,depth/2));
            tr.AddRange(new[]{p,p+1,p+2,p,p+2,p+3});
        }
        return Finish(key,v,tr);
    }
    static Mesh Star()
    {
        var points=new Vector2[16];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI/8;float r=i%2==0?(i%4==0?1:.65f):.22f;points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}
        return Extrude(points,.10f);
    }
    static Mesh Crescent()
    {
        const string key="Crescent";if(meshes.TryGetValue(key,out var cached))return cached;
        var v=new List<Vector3>();var tr=new List<int>();
        for(int i=0;i<=32;i++)
        {
            float y=Mathf.Lerp(-1,1,i/32f),r=Mathf.Sqrt(Mathf.Max(0,1-y*y));
            v.Add(new Vector3(-r,y,-.06f));v.Add(new Vector3(-.25f*r,y,-.06f));
            v.Add(new Vector3(-r,y,.06f));v.Add(new Vector3(-.25f*r,y,.06f));
            if(i<32){int p=i*4;tr.AddRange(new[]{p,p+4,p+5,p,p+5,p+1,p+2,p+3,p+7,p+2,p+7,p+6,p,p+2,p+6,p,p+6,p+4,p+1,p+5,p+7,p+1,p+7,p+3});}
        }
        return Finish(key,v,tr);
    }
    static void Roof(Transform parent,Vector3 position,float length,float width,Material material)
    {
        string key=Key("ArchedCabRoof",new[]{length,width});
        if(!meshes.TryGetValue(key,out var mesh))
        {
            var v=new List<Vector3>();var tr=new List<int>();var uv=new List<Vector2>();
            for(int i=0;i<=24;i++)
            {
                float z=Mathf.Lerp(-width/2,width/2,i/24f),y=.22f*(1-z*z/(width*width/4));
                v.Add(new Vector3(-length/2,y,z));v.Add(new Vector3(length/2,y,z));
                v.Add(new Vector3(-length/2,y-.07f,z));v.Add(new Vector3(length/2,y-.07f,z));
                uv.Add(new Vector2(0,i/24f));uv.Add(new Vector2(1,i/24f));uv.Add(new Vector2(0,i/24f));uv.Add(new Vector2(1,i/24f));
                if(i<24){int p=i*4;tr.AddRange(new[]{p,p+4,p+5,p,p+5,p+1,p+2,p+3,p+7,p+2,p+7,p+6,p,p+2,p+6,p,p+6,p+4,p+1,p+5,p+7,p+1,p+7,p+3});}
            }
            mesh=Finish(key,v,tr,null,uv);
        }
        MeshObject("ArchedCabRoof",parent,position,mesh,material);
        foreach(float side in new[]{-1f,1f})Bar("RoofSideLip",parent,position+new Vector3(-length/2,0,side*width/2),position+new Vector3(length/2,0,side*width/2),.027f,brass);
    }
}
#endif
