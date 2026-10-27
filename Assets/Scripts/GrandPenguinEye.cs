using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GrandPenguinEye : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Oval(vh,Vector2.zero,new Vector2(10,12),new Color(.08f,.045f,.025f));
        Oval(vh,new Vector2(0,-1),new Vector2(7,9),Color.black);
        Oval(vh,new Vector2(-3,4),new Vector2(3,3),Color.white);
    }
    static void Oval(VertexHelper vh,Vector2 center,Vector2 size,Color color)
    {
        int n=vh.currentVertCount;vh.AddVert(center,color,Vector2.zero);
        for(int i=0;i<=24;i++){float a=i*Mathf.PI/12;vh.AddVert(center+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y),color,Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}
    }
}
