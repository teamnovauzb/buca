using System.Collections.Generic;
using UnityEngine;

// Offline outline reduction: at most 0.0005 of the normalized glyph height.
// Keeps corners, counter contours, winding, and the original text advances.
internal static class ToyBoxGlyphSimplifier
{
    internal const float Tolerance=.0005f;
    internal static Vector2[] Simplify(Vector2[] points)
    {
        if(points.Length<5)return points;
        int split=1;float far=0;
        for(int i=1;i<points.Length;i++)if((points[i]-points[0]).sqrMagnitude>far){far=(points[i]-points[0]).sqrMagnitude;split=i;}
        var closed=new Vector2[points.Length+1];points.CopyTo(closed,0);closed[points.Length]=points[0];
        var keep=new bool[closed.Length];keep[0]=keep[split]=keep[points.Length]=true;
        Reduce(closed,0,split,keep);Reduce(closed,split,points.Length,keep);
        var result=new List<Vector2>();for(int i=0;i<points.Length;i++)if(keep[i])result.Add(points[i]);
        return result.Count>=3?result.ToArray():points;
    }
    static void Reduce(Vector2[] points,int start,int end,bool[] keep)
    {
        if(end-start<2)return;
        float error=Tolerance*Tolerance;int pivot=-1;
        for(int i=start+1;i<end;i++)
        {
            float distance=DistanceSquared(points[i],points[start],points[end]);
            if(distance>error){error=distance;pivot=i;}
        }
        if(pivot<0)return;keep[pivot]=true;Reduce(points,start,pivot,keep);Reduce(points,pivot,end,keep);
    }
    internal static float DistanceSquared(Vector2 p,Vector2 a,Vector2 b)
    {
        var d=b-a;float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;
        return (p-a-d*t).sqrMagnitude;
    }
}
