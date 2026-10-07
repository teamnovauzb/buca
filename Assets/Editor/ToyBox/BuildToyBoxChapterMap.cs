using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    static void BuildChapterMap(Transform root, ToyBoxMenuController menu)
    {
        BuildTrainMap(root,menu);
    }
}

internal static partial class ToyBoxGeometry
{
    internal static Mesh MapPrism(int sides,float radius,float height,float bevel,bool star=false)
    {
        string key=$"MapPrism_{sides}_{radius:F3}_{height:F3}_{bevel:F3}_{star}";
        if(Cache.TryGetValue(key,out var cached)) return cached;
        var vertices=new List<Vector3>(); var indices=new List<int>(); var uv=new List<Vector2>();
        float[] radii={radius-bevel,radius,radius,radius-bevel};
        float[] ys={-height*.5f,-height*.5f+bevel,height*.5f-bevel,height*.5f};
        System.Func<int,int,Vector3> point=(ring,i)=>{
            float angle=i*2*Mathf.PI/sides+Mathf.PI/6;
            float r=radii[ring]*(star && i%2==1?.47f:1f);
            return new Vector3(Mathf.Cos(angle)*r,ys[ring],Mathf.Sin(angle)*r);
        };
        for(int i=0;i<sides;i++)
        {
            for(int ring=0;ring<3;ring++)
                Quad(vertices,indices,point(ring,i),point(ring,(i+1)%sides),point(ring+1,(i+1)%sides),point(ring+1,i),
                    new Vector3(point(1,i).x,0,point(1,i).z));
            Quad(vertices,indices,new Vector3(0,ys[3],0),point(3,i),point(3,(i+1)%sides),new Vector3(0,ys[3],0),Vector3.up);
            Quad(vertices,indices,new Vector3(0,ys[0],0),point(0,i),point(0,(i+1)%sides),new Vector3(0,ys[0],0),Vector3.down);
        }
        foreach(var v in vertices) uv.Add(new Vector2(v.x*.2f,v.z*.2f+v.y*.1f));
        return Finish(key,vertices,indices,uv);
    }
    internal static Mesh MapCone(float radius,float height)
    {
        string key=$"MapCone_{radius:F3}_{height:F3}";
        if(Cache.TryGetValue(key,out var cached)) return cached;
        var v=new List<Vector3>(); var t=new List<int>();
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;
            Vector3 p=new Vector3(Mathf.Cos(a)*radius,-height*.5f,Mathf.Sin(a)*radius);
            Vector3 q=new Vector3(Mathf.Cos(b)*radius,-height*.5f,Mathf.Sin(b)*radius);
            Quad(v,t,p,q,Vector3.up*height*.5f,Vector3.up*height*.5f,new Vector3(p.x,.1f,p.z));
            Quad(v,t,p,q,Vector3.down*height*.5f,Vector3.down*height*.5f,Vector3.down);
        }
        return Finish(key,v,t);
    }
}
