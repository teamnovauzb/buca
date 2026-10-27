using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public sealed class GrandAnimalFace : BaseMeshEffect
{
    public Vector2 leftEye,rightEye,mouth;
    public int friendIndex;
    public RectTransform[] extraEyes;
    float blink;
    public void Sample(float t)
    {
        float phase=Mathf.Repeat(t+friendIndex*.43f,3.2f);
        blink=1-Mathf.SmoothStep(0,1,Mathf.Abs(phase-1.65f)/.14f);
        if(extraEyes!=null)foreach(var eye in extraEyes)eye.localScale=new Vector3(1,1-.96f*blink,1);
        graphic.SetVerticesDirty();
    }
    Vector2 Warp(Vector2 p,Vector2 center,float scale,Vector2 radius)
    {
        var d=p-center;
        float x=Mathf.Abs(d.x)/radius.x,y=Mathf.Abs(d.y)/radius.y;
        float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,x)))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,y)));
        return p+new Vector2(0,d.y*(scale-1)*weight);
    }
    public override void ModifyMesh(VertexHelper vh)
    {
        if(!IsActive())return;
        var im=GetComponent<UnityEngine.UI.Image>();if(im.sprite==null)return;
        var r=((RectTransform)transform).rect;var uv=DataUtility.GetOuterUV(im.sprite);vh.Clear();
        const int cols=48,rows=80;
        for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
        {
            float u=x/(float)cols,v=y/(float)rows;var p=new Vector2(r.xMin+u*r.width,r.yMin+v*r.height);
            if(friendIndex!=4){p=Warp(p,leftEye,1-.97f*blink,new Vector2(19,23));p=Warp(p,rightEye,1-.97f*blink,new Vector2(19,23));}
            vh.AddVert(p,im.color,new Vector2(Mathf.Lerp(uv.x,uv.z,u),Mathf.Lerp(uv.y,uv.w,v)));
            if(x<cols&&y<rows){int n=y*(cols+1)+x;vh.AddTriangle(n,n+1,n+cols+2);vh.AddTriangle(n,n+cols+2,n+cols+1);}
        }
    }
}

