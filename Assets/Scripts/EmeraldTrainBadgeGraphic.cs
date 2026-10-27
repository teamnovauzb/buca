using UnityEngine;

/// <summary>Resolution-independent emerald enamel, beveled brass rim, rivets and pointer.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class EmeraldTrainBadgeGraphic : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        var r=rectTransform.rect;
        var gold=new Color(1,.72f,.22f);
        Triangle(mesh,new Vector2(-12,r.yMin+2),new Vector2(12,r.yMin+2),new Vector2(0,r.yMin-17),new Color(.43f,.21f,.025f));
        Triangle(mesh,new Vector2(-10,r.yMin+2),new Vector2(10,r.yMin+2),new Vector2(0,r.yMin-14),gold);
        Triangle(mesh,new Vector2(-6.5f,r.yMin+1),new Vector2(6.5f,r.yMin+1),new Vector2(0,r.yMin-9),new Color(1,.98f,.72f));
        Round(mesh,new Rect(r.x+1,r.y-3,r.width,r.height),12,new Color(.38f,.18f,.03f),new Color(.15f,.07f,.015f,.7f));
        Round(mesh,r,12,new Color(1,.96f,.65f),new Color(.68f,.34f,.055f));
        Round(mesh,Inset(r,1.5f),10.5f,new Color(.92f,.57f,.12f),new Color(1,.83f,.39f));
        Round(mesh,Inset(r,3),9,new Color(1,1,.85f),gold);
        Round(mesh,Inset(r,4.5f),7.5f,new Color(.025f,.11f,.07f),new Color(.01f,.06f,.035f));
        Round(mesh,Inset(r,6),6,new Color(.055f,.38f,.22f),new Color(.008f,.16f,.085f));
        Round(mesh,new Rect(r.x+12,r.yMax-9,r.width-24,1.2f),.6f,new Color(.43f,.85f,.56f,.65f),new Color(.43f,.85f,.56f,.2f));
        foreach(float side in new[]{-1f,1f}){
            var p=new Vector2(side*(r.width*.5f-11),-2);
            Disc(mesh,p+Vector2.down*.7f,3.3f,new Color(.4f,.2f,.025f),new Color(.22f,.08f,.01f));
            Disc(mesh,p,2.7f,new Color(1,.98f,.67f),new Color(.77f,.41f,.05f));
            Disc(mesh,p+new Vector2(-.6f,.7f),.8f,Color.white,new Color(1,.9f,.45f));
        }
    }
    static Rect Inset(Rect r,float n)=>new Rect(r.x+n,r.y+n,r.width-2*n,r.height-2*n);
    static void Triangle(UnityEngine.UI.VertexHelper m,Vector2 a,Vector2 b,Vector2 c,Color tint)
    {int i=m.currentVertCount;m.AddVert(a,tint,Vector2.zero);m.AddVert(b,tint,Vector2.zero);m.AddVert(c,tint,Vector2.zero);m.AddTriangle(i,i+1,i+2);}
    static void Round(UnityEngine.UI.VertexHelper mesh,Rect r,float radius,Color top,Color bottom)
    {
        radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
        int first=mesh.currentVertCount;mesh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
        const int steps=6;int count=0;
        for(int corner=0;corner<4;corner++)for(int j=0;j<=steps;j++)
        {
            float angle=(corner*90+j*90f/steps)*Mathf.Deg2Rad;
            var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,
                corner<2?r.yMax-radius:r.yMin+radius);
            var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
            mesh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);
            if(count>0)mesh.AddTriangle(first,first+count,first+count+1);count++;
        }
        mesh.AddTriangle(first,first+count,first+1);
    }
    static void Disc(UnityEngine.UI.VertexHelper mesh,Vector2 center,float radius,Color top,Color bottom)
    {
        int first=mesh.currentVertCount;
        mesh.AddVert(center,Color.Lerp(bottom,top,.5f),Vector2.zero);
        const int sides=64;
        for(int i=0;i<=sides;i++)
        {
            float a=i*Mathf.PI*2/sides;
            var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            mesh.AddVert(center+p*radius,Color.Lerp(bottom,top,p.y*.5f+.5f),Vector2.zero);
            if(i>0)mesh.AddTriangle(first,first+i,first+i+1);
        }
    }
}
