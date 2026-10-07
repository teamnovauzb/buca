using System.Collections.Generic;
using UnityEngine;

internal static partial class ToyBoxGeometry
{
    // A rounded deck with one genuinely open goal, triangulated from its rim outward.
    internal static Mesh WorkshopDeck()
    {
        var v=new List<Vector3>();var t=new List<int>();const int n=256;
        Vector3 Inner(float a,float y,float r)=>new Vector3(Mathf.Cos(a)*r,y,.7f+Mathf.Sin(a)*r);
        Vector3 Outer(float a)
        {
            var direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float lo=0,hi=20;
            for(int step=0;step<24;step++){
                float distance=(lo+hi)*.5f;Vector2 point=new Vector2(0,.7f)+direction*distance;
                Vector2 q=new Vector2(Mathf.Abs(point.x)-5.24f,Mathf.Abs(point.y)-.79f);
                float sdf=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-2.12f;
                if(sdf>0)hi=distance;else lo=distance;
            }
            var p=new Vector2(0,.7f)+direction*lo;return new Vector3(p.x,.12f,p.y);
        }
        for(int i=0;i<n;i++){
            float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
            Quad(v,t,Inner(a,.12f,.64f),Inner(b,.12f,.64f),Outer(b),Outer(a),Vector3.up);
            Quad(v,t,Inner(a,.12f,.64f),Inner(a,.04f,.56f),Inner(b,.04f,.56f),Inner(b,.12f,.64f),Vector3.up);
            Quad(v,t,Inner(a,.04f,.56f),Inner(a,-.40f,.56f),Inner(b,-.40f,.56f),Inner(b,.04f,.56f),new Vector3(-Mathf.Cos(a),0,-Mathf.Sin(a)));
        }
        var uv=new List<Vector2>();foreach(var p in v)uv.Add(new Vector2(p.x*.15f,p.z*.35f));
        return Finish("RoundedDisplayDeckSingleGoal",v,t,uv);
    }
    internal static Mesh WorkshopRoundedFrame()=>RoundedDisplayProfile("RoundedDisplaySatinRim",new[]{
        new Vector4(7.76f,3.31f,2.52f,-.16f),new Vector4(7.90f,3.45f,2.66f,-.04f),
        new Vector4(7.90f,3.45f,2.66f,.34f),new Vector4(7.84f,3.39f,2.60f,.46f),
        new Vector4(7.72f,3.27f,2.48f,.51f),new Vector4(7.40f,2.95f,2.16f,.51f),
        new Vector4(7.29f,2.84f,2.05f,.42f),new Vector4(7.29f,2.84f,2.05f,.02f)},false);
    internal static Mesh WorkshopDisplayBase()=>RoundedDisplayProfile("RoundedDisplayNavyBase",new[]{
        new Vector4(7.62f,3.17f,2.38f,-.57f),new Vector4(7.82f,3.37f,2.58f,-.45f),
        new Vector4(7.82f,3.37f,2.58f,-.19f),new Vector4(7.73f,3.28f,2.49f,-.12f)},true);
    static Mesh RoundedDisplayProfile(string name,Vector4[] profile,bool cap)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        const int cornerSegments=32,n=4*(cornerSegments+1);
        foreach(var p in profile)for(int corner=0;corner<4;corner++)for(int j=0;j<=cornerSegments;j++){
            float a=(corner*90+j*90f/cornerSegments)*Mathf.Deg2Rad;
            float cx=(corner==0||corner==3?1:-1)*(p.x-p.z),cz=(corner<2?1:-1)*(p.y-p.z);
            vertices.Add(new Vector3(cx+Mathf.Cos(a)*p.z,p.w,cz+Mathf.Sin(a)*p.z));uv.Add(new Vector2((corner*(cornerSegments+1)+j)/(float)n,p.w));
        }
        for(int row=0;row<profile.Length-1;row++)for(int j=0;j<n;j++){int a=row*n+j,b=row*n+(j+1)%n,c=b+n,d=a+n;triangles.AddRange(new[]{a,c,b,a,d,c});}
        if(cap){int center=vertices.Count;vertices.Add(new Vector3(0,profile[profile.Length-1].w,0));uv.Add(Vector2.zero);int start=(profile.Length-1)*n;for(int j=0;j<n;j++)triangles.AddRange(new[]{center,start+(j+1)%n,start+j});}
        return Finish(name,vertices,triangles,uv);
    }
    internal static Mesh WorkshopCanBody()
    {
        var source=Disc(1.38f,1.75f,.16f);var mesh=Object.Instantiate(source);var v=mesh.vertices;var uv=mesh.uv;
        for(int i=0;i<v.Length;i++){
            float height=(v[i].y+.875f)/1.75f;float fullness=1f+.018f*Mathf.Sin(height*Mathf.PI);
            v[i].x*=fullness;v[i].z*=fullness;
            uv[i]=new Vector2(Mathf.Atan2(v[i].z,v[i].x)/(Mathf.PI*2)+.5f,height*.70f);
        }
        mesh.vertices=v;mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        return Save(mesh,Folder+"/WorkshopBarrelCan.asset");
    }
    internal static Mesh WorkshopPaintBand(int seed)
    {
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();const int n=192;
        for(int row=0;row<4;row++)for(int i=0;i<=n;i++)
        {
            float a=i*Mathf.PI*2/n;
            float drips=Mathf.Pow(Mathf.Max(0,Mathf.Sin(a*7+seed)),4)*(.35f+.18f*Mathf.Sin(a*3+seed));
            float lower=.61f-drips;float y=row==0?.92f:row==1?.87f:row==2?lower+.035f:lower;
            float r=row==0?1.37f:row==3?1.387f:1.416f;
            v.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));uv.Add(new Vector2(i/(float)n*3,y));
        }
        for(int row=0;row<3;row++)for(int i=0;i<n;i++){int a=row*(n+1)+i,b=a+1,c=b+n+1,d=a+n+1;t.AddRange(new[]{a,b,c,a,c,d});}
        return Finish("WorkshopFlowingPaint"+seed,v,t,uv);
    }
}
