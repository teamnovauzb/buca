using System.Collections.Generic;
using UnityEngine;

internal static partial class ToyBoxGeometry
{
    internal static Mesh StandingCapsule(float radius,float length,float height)
    {
        string key=$"StandingRail_{radius:F3}_{length:F3}_{height:F3}";
        if(Cache.TryGetValue(key,out var cached)) return cached;
        var v=new List<Vector3>(); var t=new List<int>(); var uv=new List<Vector2>();
        const int n=64;
        float bevel=Mathf.Min(radius*.45f,.07f),straight=Mathf.Max(0,length*.5f-radius);
        float[] ys={0,bevel,height-bevel,height};
        float[] rs={radius-bevel,radius,radius,radius-bevel};
        for(int ring=0;ring<4;ring++) for(int i=0;i<n;i++)
        {
            float a=i*Mathf.PI*2/n;
            var p=new Vector3(Mathf.Cos(a)*rs[ring],ys[ring],Mathf.Sin(a)*rs[ring]+(i<n/2?straight:-straight));
            v.Add(p);uv.Add(new Vector2(p.x,p.z)*.2f);
        }
        for(int ring=0;ring<3;ring++) for(int i=0;i<n;i++)
        {
            int a=ring*n+i,b=ring*n+(i+1)%n,c=b+n,d=a+n;
            t.AddRange(new[]{a,c,b,a,d,c});
        }
        int top=v.Count; v.Add(new Vector3(0,height,0));uv.Add(Vector2.zero);
        for(int i=0;i<n;i++) t.AddRange(new[]{top,3*n+(i+1)%n,3*n+i});
        return Finish(key,v,t,uv);
    }
    internal static Mesh RecessedBoard(Vector2 hole,float radius)
    {
        string key=$"RecessedBoard_{hole.x:F3}_{hole.y:F3}_{radius:F3}";
        if(Cache.TryGetValue(key,out var cached)) return cached;
        var angles=new List<float>();
        for(int i=0;i<144;i++) angles.Add(i*Mathf.PI*2/144);
        foreach(float x in new[]{-5.15f,5.15f}) foreach(float z in new[]{-7.65f,7.65f})
            angles.Add(Mathf.Repeat(Mathf.Atan2(z-hole.y,x-hole.x),Mathf.PI*2));
        angles.Sort(); var v=new List<Vector3>();var t=new List<int>();
        Vector3 Inner(float a,float y)=>new Vector3(hole.x+Mathf.Cos(a)*radius,y,hole.y+Mathf.Sin(a)*radius);
        Vector3 Outer(float a)
        {
            float x=Mathf.Cos(a),z=Mathf.Sin(a);
            float tx=(x>=0?5.15f-hole.x:-5.15f-hole.x)/x;
            float tz=(z>=0?7.65f-hole.y:-7.65f-hole.y)/z;
            float d=Mathf.Min(Mathf.Abs(x)<.00001f?float.MaxValue:tx,Mathf.Abs(z)<.00001f?float.MaxValue:tz);
            return new Vector3(hole.x+x*d,-.01f,hole.y+z*d);
        }
        for(int i=0;i<angles.Count;i++)
        {
            float a=angles[i],b=angles[(i+1)%angles.Count];
            Quad(v,t,Inner(a,-.01f),Inner(b,-.01f),Outer(b),Outer(a),Vector3.up);
            Vector3 inward=new Vector3(-Mathf.Cos(a),0,-Mathf.Sin(a));
            Quad(v,t,Inner(a,-.01f),Inner(a,-.40f),Inner(b,-.40f),Inner(b,-.01f),inward);
        }
        var uv=new List<Vector2>();foreach(var p in v)uv.Add(new Vector2(p.x,p.z)*.18f);
        return Finish(key,v,t,uv);
    }
}
