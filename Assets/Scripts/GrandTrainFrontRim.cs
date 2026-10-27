using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

/// <summary>Preserves the illustrated rim contour instead of cutting the front wall horizontally.</summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public sealed class GrandTrainFrontRim : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper vh)
    {
        if(!IsActive())return;
        var image=GetComponent<UnityEngine.UI.Image>();if(image.sprite==null)return;
        var rect=((RectTransform)transform).rect;
        var uv=DataUtility.GetOuterUV(image.sprite);
        // Points follow the left side rail, corner cap, and gently rising front rail.
        float[] xs={0, .06f, .17f, .205f, .25f, .86f, .91f, 1};
        float[] tops={.12f,.12f,.19f,.195f,.224f,.174f,.157f,.157f};
        vh.Clear();
        for(int i=0;i<xs.Length-1;i++)
        {
            int n=vh.currentVertCount;
            Add(vh,rect,uv,xs[i],0,image.color);Add(vh,rect,uv,xs[i+1],0,image.color);
            Add(vh,rect,uv,xs[i+1],1-tops[i+1],image.color);Add(vh,rect,uv,xs[i],1-tops[i],image.color);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
    static void Add(VertexHelper vh,Rect r,Vector4 uv,float x,float y,Color color)
    {
        vh.AddVert(new Vector3(r.xMin+x*r.width,r.yMin+y*r.height),color,new Vector2(Mathf.Lerp(uv.x,uv.z,x),Mathf.Lerp(uv.y,uv.w,y)));
    }
}
