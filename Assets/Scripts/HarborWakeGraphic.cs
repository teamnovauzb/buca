using UnityEngine;
using UnityEngine.UI;

/// <summary>Water contact and a soft, widening stern wake in the boat's direction of travel.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HarborWakeGraphic : MaskableGraphic
{
    public float phase;
    public Vector2 direction=Vector2.left;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var size=rectTransform.rect.size;
        var forward=direction.sqrMagnitude>.01f?direction.normalized:Vector2.left;
        // A shallow water-plane projection prevents the trail rising into the cabin on turns.
        forward=new Vector2(forward.x,forward.y*.38f).normalized;
        var side=new Vector2(-forward.y,forward.x);
        float halfHull=size.x*.35f;
        Ellipse(mesh,new Vector2(0,0),halfHull,size.y*.23f,new Color(.01f,.09f,.16f,.35f));
        for(int edge=-1;edge<=1;edge+=2)
        {
            Vector2 previous=Vector2.zero;
            for(int step=0;step<=24;step++)
            {
                float age=step/24f;
                float width=size.y*(.09f+age*.42f);
                Vector2 point=-forward*(halfHull*.65f+age*size.x*.62f)+side*(edge*width);
                point.y+=Mathf.Sin(age*18-phase*Mathf.PI*2)*.65f*age;
                if(step>0)
                {
                    float alpha=Mathf.Pow(1-age,1.5f)*(.42f+.1f*Mathf.Sin(age*31-phase*6.283f));
                    Stroke(mesh,previous,point,1.6f+age*2.2f,new Color(.65f,.86f,.94f,alpha));
                }
                previous=point;
            }
        }
        // Low bow ripples, tight to the hull, fade as they move outward.
        for(int ring=0;ring<3;ring++)
        {
            float age=Mathf.Repeat(phase+ring/3f,1);
            float rx=halfHull+age*8,ry=size.y*(.14f+age*.14f);
            Vector2 offset=forward*(2+age*4);
            for(int j=0;j<48;j++)
            {
                float a=j*Mathf.PI*2/48,b=(j+1)*Mathf.PI*2/48;
                var p=offset+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry);
                var q=offset+new Vector2(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry);
                if(Vector2.Dot((p-offset).normalized,forward)<-.15f||(j+ring*3)%13<3)continue;
                Stroke(mesh,p,q,.85f,new Color(.72f,.89f,.96f,(1-age)*.5f));
            }
        }
    }
    static void Ellipse(VertexHelper mesh,Vector2 centre,float rx,float ry,Color tint)
    {
        int first=mesh.currentVertCount;mesh.AddVert(centre,tint,Vector2.zero);
        var edge=tint;edge.a=0;
        for(int j=0;j<=48;j++)
        {
            float a=j*Mathf.PI*2/48;mesh.AddVert(centre+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),edge,Vector2.zero);
            if(j>0)mesh.AddTriangle(first,first+j,first+j+1);
        }
    }
    static void Stroke(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color color)
    {
        var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width;
        int first=mesh.currentVertCount;var edge=color;edge.a=0;
        mesh.AddVert(a-normal,edge,Vector2.zero);mesh.AddVert(a,color,Vector2.zero);mesh.AddVert(a+normal,edge,Vector2.zero);
        mesh.AddVert(b-normal,edge,Vector2.zero);mesh.AddVert(b,color,Vector2.zero);mesh.AddVert(b+normal,edge,Vector2.zero);
        mesh.AddTriangle(first,first+1,first+4);mesh.AddTriangle(first,first+4,first+3);
        mesh.AddTriangle(first+1,first+2,first+5);mesh.AddTriangle(first+1,first+5,first+4);
    }
}
