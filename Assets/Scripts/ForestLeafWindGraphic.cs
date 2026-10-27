using UnityEngine;
/// <summary>Soft air ribbons and tumbling leaves confined to woodland clearings.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ForestLeafWindGraphic : UnityEngine.UI.MaskableGraphic
{
    float seconds;
    static readonly Vector3[] Lanes={new(310,356,390),new(310,431,390),new(260,582,435),new(35,57,270)};
    public void Pose(float time){seconds=time;SetVerticesDirty();}
    void Update(){if(Application.isPlaying)Pose(Time.unscaledTime);}
    static Vector2 Art(Vector2 p)=>new Vector2(p.x-836,470.5f-p.y);
    static Vector2 Path(Vector3 lane,float u,int index)
        =>Art(new Vector2(lane.x+lane.z*(1-u),lane.y+Mathf.Sin(u*Mathf.PI*2+index*.8f)*13+Mathf.Sin(u*Mathf.PI)*6));
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        float t=Mathf.Repeat(seconds,16);
        float pressure=t<5?Mathf.SmoothStep(.12f,1,t/5):t<8?1:t<15?Mathf.SmoothStep(1,.12f,(t-8)/7):.12f;
        for(int laneIndex=0;laneIndex<Lanes.Length;laneIndex++)
        {
            var lane=Lanes[laneIndex];
            float head=Mathf.Repeat(seconds/8+laneIndex*.23f,1.4f);
            for(int ribbon=0;ribbon<2;ribbon++)
            for(int j=1;j<=54;j++)
            {
                float a=head-.44f+(j-1)*.44f/54,b=head-.44f+j*.44f/54;
                if(a<0||b>1)continue;
                float taper=Mathf.Sin((float)j/55*Mathf.PI);
                float edge=Mathf.SmoothStep(0,1,a/.12f)*Mathf.SmoothStep(0,1,(1-b)/.12f);
                var delta=new Vector2(0,ribbon*3);
                Stroke(mesh,Path(lane,a,laneIndex)+delta,Path(lane,b,laneIndex)+delta,1.6f,
                    new Color(1,.97f,.77f,taper*edge*(.30f+.36f*pressure)*(ribbon==0?1:.45f)));
            }
            for(int leaf=0;leaf<6;leaf++)
            {
                float age=Mathf.Repeat(seconds/8+leaf/6f+laneIndex*.17f,1);
                float alpha=Mathf.SmoothStep(0,1,age/.12f)*Mathf.SmoothStep(0,1,(1-age)/.12f);
                var p=Path(lane,age,laneIndex)+new Vector2(0,Mathf.Sin(age*6.283f+leaf)*13);
                float angle=age*6.283f+leaf*1.7f;
                float size=6f+(leaf%3)*.7f;
                Leaf(mesh,p,angle,size,alpha*(.55f+.4f*pressure),(leaf+laneIndex)%3==0);
            }
        }
    }
    static void Leaf(UnityEngine.UI.VertexHelper mesh,Vector2 p,float angle,float size,float alpha,bool gold)
    {
        Vector2 axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
        Vector2 side=new Vector2(-axis.y,axis.x);
        float width=size*(.36f+.18f*Mathf.Abs(Mathf.Sin(angle*.7f)));
        Color light=gold?new Color(.94f,.77f,.15f,alpha):new Color(.48f,.72f,.12f,alpha);
        Color dark=gold?new Color(.61f,.40f,.05f,alpha):new Color(.13f,.38f,.06f,alpha);
        int first=mesh.currentVertCount;
        mesh.AddVert(p-axis*size,light,Vector2.zero);mesh.AddVert(p+side*width,light,Vector2.zero);
        mesh.AddVert(p+axis*size,light,Vector2.zero);mesh.AddVert(p,light,Vector2.zero);
        mesh.AddVert(p-side*width,dark,Vector2.zero);
        mesh.AddTriangle(first,first+1,first+3);mesh.AddTriangle(first+1,first+2,first+3);
        mesh.AddTriangle(first,first+3,first+4);mesh.AddTriangle(first+3,first+2,first+4);
        Stroke(mesh,p-axis*size*1.12f,p+axis*size*.8f,.3f,new Color(.88f,.88f,.4f,alpha*.6f));
    }
    static void Stroke(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
    {
        Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*width;
        int n=mesh.currentVertCount;var edge=tint;edge.a=0;
        mesh.AddVert(a-normal,edge,Vector2.zero);mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(a+normal,edge,Vector2.zero);
        mesh.AddVert(b-normal,edge,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);mesh.AddVert(b+normal,edge,Vector2.zero);
        mesh.AddTriangle(n,n+1,n+4);mesh.AddTriangle(n,n+4,n+3);
        mesh.AddTriangle(n+1,n+2,n+5);mesh.AddTriangle(n+1,n+5,n+4);
    }
}
