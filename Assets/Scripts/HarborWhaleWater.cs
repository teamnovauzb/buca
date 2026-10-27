using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HarborWhaleWater : MaskableGraphic
{
    public float seconds,landing;public Vector2 center=new Vector2(425,-130),spout;public float rainbow;
    public void Sample(float time,float splashTime,Vector2 blowhole,float rainbowAmount){seconds=time;landing=splashTime;spout=blowhole;rainbow=rainbowAmount;SetVerticesDirty();}
    static float Seed(int i)=>Mathf.Repeat(Mathf.Sin(i*77.31f+21.4f)*4211.17f,1);
    protected override void OnPopulateMesh(VertexHelper v)
    {
        v.Clear();
        for(int i=0;i<5;i++)
        {
            float age=Mathf.Repeat(seconds*.65f+i*.22f,1);
            Ring(v,center,new Vector2(25+age*65,7+age*15),2.5f,new Color(.55f,.92f,1,(1-age)*.55f));
        }
        Burst(v,seconds-.65f,1);Burst(v,seconds-landing,2);
        if(rainbow>.01f)
        {
            Color[] colors={new Color(1,.27f,.43f),new Color(1,.72f,.2f),new Color(1,1,.45f),new Color(.3f,1,.78f),new Color(.3f,.7f,1),new Color(.75f,.5f,1)};
            for(int band=0;band<6;band++)for(int i=0;i<36;i++)
            {
                float a=i/35f;var p=spout+new Vector2(-a*155,Mathf.Sin(a*Mathf.PI)*100+band*3);
                var color=colors[band];color.a=rainbow*.75f;Disc(v,p,new Vector2(3.4f,3.4f),color);
            }
            for(int i=0;i<28;i++)
            {
                float u=Mathf.Repeat(seconds*.65f+i*.137f,1);var p=spout+new Vector2(-155*u,Mathf.Sin(u*Mathf.PI)*115);
                Disc(v,p,new Vector2(2,4),new Color(.85f,1,1,rainbow));
            }
        }
    }
    void Burst(VertexHelper v,float age,int seed)
    {
        if(age<0||age>2.3f)return;
        for(int i=0;i<65;i++)
        {
            float s=Seed(i+seed*71);float x=(s-.5f)*2;float a=age*(.8f+Seed(i+31)*.5f);
            var p=center+new Vector2(x*(20+a*65),a*(75+Seed(i+11)*80)-145*a*a);
            if(p.y<center.y-28)continue;
            float alpha=Mathf.Clamp01((2.3f-age)*1.5f);
            Disc(v,p,new Vector2(2+s*3,4+s*6),new Color(.23f,.71f,1,alpha));
            Disc(v,p+new Vector2(-1,2),new Vector2(1+s,2+s*2),new Color(.9f,1,1,alpha));
        }
        Ring(v,center,new Vector2(30+age*32,8+age*10),4*(1-age/2.3f),new Color(.8f,.98f,1,1-age/2.3f));
    }
    static void Ring(VertexHelper v,Vector2 c,Vector2 r,float width,Color color)
    {
        for(int i=0;i<64;i++)
        {
            float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;int n=v.currentVertCount;
            foreach(var p in new[]{new Vector2(Mathf.Cos(a)*r.x,Mathf.Sin(a)*r.y),new Vector2(Mathf.Cos(b)*r.x,Mathf.Sin(b)*r.y),new Vector2(Mathf.Cos(b)*(r.x-width),Mathf.Sin(b)*(r.y-width)),new Vector2(Mathf.Cos(a)*(r.x-width),Mathf.Sin(a)*(r.y-width))})v.AddVert(c+p,color,Vector2.zero);
            v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);
        }
    }
    static void Disc(VertexHelper v,Vector2 c,Vector2 r,Color color)
    {
        int n=v.currentVertCount;v.AddVert(c,color,Vector2.zero);
        for(int i=0;i<=12;i++){float a=i*Mathf.PI/6;v.AddVert(c+new Vector2(Mathf.Cos(a)*r.x,Mathf.Sin(a)*r.y),color,Vector2.zero);if(i>0)v.AddTriangle(n,n+i,n+i+1);}
    }
}
