using UnityEngine;

/// <summary>Native level plaque so the map artwork never contains baked or duplicate numbers.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class WorkshopStationBadge : UnityEngine.UI.MaskableGraphic
{
    public bool stationPlaque;
    public bool ticketPlaque;
    [SerializeField, HideInInspector] Vector2 trainClearance;
    public Vector2 TrainClearance => trainClearance;
    bool selected,arrived,locked=true;
    // Move the number, face, click target and decorations together, leaving the rail stop fixed.
    // Serialize the applied offset with the child positions so editor reloads cannot accumulate it.
    public void SetTrainClearance(Vector2 offset)
    {
        var delta=offset-trainClearance;
        if(delta.sqrMagnitude<.000001f)return;
        foreach(Transform child in transform.parent)
            if(child is RectTransform rect)rect.anchoredPosition+=delta;
        trainClearance=offset;
    }
    public void SetLocked(bool isLocked)
    {
        if(locked==isLocked)return;
        locked=isLocked;SetVerticesDirty();
    }
    public void SetState(bool isSelected,bool hasArrived)
    {
        if(selected==isSelected&&arrived==hasArrived)return;
        selected=isSelected;arrived=hasArrived;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        if(ticketPlaque){Ticket(mesh);return;}
        if(stationPlaque){Plaque(mesh);return;}
        float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        Disc(mesh,new Vector2(0,-radius*.10f),radius,new Color(.20f,.10f,.025f,.42f),new Color(.20f,.10f,.025f,.42f));
        Disc(mesh,Vector2.zero,radius,new Color(1,.88f,.48f),new Color(.65f,.34f,.055f));
        Disc(mesh,Vector2.zero,radius*.92f,new Color(1,.97f,.68f),new Color(.95f,.65f,.18f));
        Disc(mesh,Vector2.zero,radius*.81f,new Color(1,.98f,.84f),new Color(1,.90f,.68f));
    }
    void Ticket(UnityEngine.UI.VertexHelper mesh)
    {
        var r=rectTransform.rect;float w=r.width,h=r.height,s=w/50f;
        var front=new Rect(r.xMin,r.yMin+17,w-3,h-19);
        if(selected || arrived)
        {
            // Use the same notched silhouette as the ticket, never a rectangular selector.
            for(int layer=3;layer>=1;layer--)
            {
                float spread=layer*1.4f*s;
                var halo=Inset(front,-spread);
                var glow=new Color(.38f,1,.14f,arrived?.07f:.12f);
                TicketShape(mesh,halo,5*s+spread,glow,glow);
            }
        }
        // Layered maple bevel and real side cut-outs, with no raster texture to upscale.
        var back=new Rect(front.x+2,front.y-2.5f,front.width,front.height);
        TicketShape(mesh,back,5*s,new Color(.60f,.27f,.055f),new Color(.32f,.13f,.025f));
        TicketShape(mesh,front,5*s,selected&&!arrived?new Color(.75f,1,.38f):new Color(1,.86f,.48f),selected&&!arrived?new Color(.25f,.85f,.12f):new Color(.82f,.43f,.10f));
        var bevel=Inset(front,1.5f*s);
        TicketShape(mesh,bevel,4.7f*s,new Color(1,.97f,.75f),new Color(.98f,.69f,.27f));
        var face=Inset(front,3.2f*s);
        var faceTop=arrived?new Color(.04f,.29f,.14f):selected?new Color(.76f,1,.48f):new Color(1,.99f,.88f);
        var faceBottom=arrived?new Color(.015f,.16f,.075f):selected?new Color(.42f,.91f,.24f):new Color(1,.91f,.69f);
        TicketShape(mesh,face,4.4f*s,faceTop,faceBottom);
        var stripe=arrived?new Color(.05f,.32f,.16f):new Color(.08f,.43f,.45f);
        var stripeTop=arrived?new Color(.08f,.40f,.21f):selected?new Color(.18f,.72f,.65f):new Color(.24f,.63f,.61f);
        float bandWidth=2.2f*s;
        TicketShape(mesh,face,4.4f*s,stripeTop,stripe,face.xMin+4*s,face.xMin+4*s+bandWidth);
        TicketShape(mesh,face,4.4f*s,stripeTop,stripe,face.xMax-4*s-bandWidth,face.xMax-4*s);
        if(!locked)return;
        // The closed brass padlock hangs below locked tickets so every number stays clear.
        Vector2 center=new Vector2(-1.5f,r.yMin);
        Disc(mesh,center+new Vector2(0,18.5f*s),2.3f*s,new Color(.59f,.30f,.06f),new Color(.32f,.14f,.025f));
        Shackle(mesh,center+new Vector2(1,-.7f)*s,s,3.3f,new Color(.45f,.20f,.02f),new Color(.30f,.12f,.01f));
        Shackle(mesh,center,s,2.6f,new Color(1,.93f,.56f),new Color(.82f,.48f,.055f));
        Shackle(mesh,center+new Vector2(-.35f,.25f)*s,s,1.0f,new Color(1,1,.86f),new Color(1,.77f,.22f));
        Round(mesh,new Rect(center.x-6.5f*s,center.y,15*s,12*s),2.1f*s,new Color(.62f,.30f,.025f),new Color(.35f,.15f,.01f));
        Round(mesh,new Rect(center.x-7.3f*s,center.y+1.4f*s,14*s,12*s),2*s,new Color(1,.89f,.28f),new Color(.91f,.52f,.04f));
        Round(mesh,new Rect(center.x-5.8f*s,center.y+3*s,10.8f*s,9*s),1.1f*s,new Color(1,.96f,.52f),new Color(.97f,.68f,.09f));
        var ink=new Color(.15f,.105f,.025f);
        Disc(mesh,center+new Vector2(-.3f,8.2f)*s,1.7f*s,ink,ink);
        Round(mesh,new Rect(center.x-1.3f*s,center.y+4.3f*s,2*s,4*s),.45f*s,ink,ink);
    }
    static Rect Inset(Rect r,float amount)=>new Rect(r.x+amount,r.y+amount,r.width-amount*2,r.height-amount*2);
    static float TicketInset(Rect r,float notch,float y)
    {
        float edge=Mathf.Min(y-r.yMin,r.yMax-y),radius=Mathf.Min(3,r.height*.15f);
        float corner=edge<radius?radius-Mathf.Sqrt(Mathf.Max(0,radius*radius-(radius-edge)*(radius-edge))):0;
        float dy=Mathf.Abs(y-r.center.y);
        return Mathf.Max(corner,dy<notch?Mathf.Sqrt(notch*notch-dy*dy):0);
    }
    static void TicketShape(UnityEngine.UI.VertexHelper mesh,Rect r,float notch,Color top,Color bottom,
        float clipLeft=float.NegativeInfinity,float clipRight=float.PositiveInfinity)
    {
        // Horizontal strips preserve the concave ticket notches instead of filling them with a triangle fan.
        const int steps=48;
        for(int i=0;i<steps;i++)
        {
            float y0=Mathf.Lerp(r.yMin,r.yMax,i/(float)steps),y1=Mathf.Lerp(r.yMin,r.yMax,(i+1f)/steps);
            float a=TicketInset(r,notch,y0),b=TicketInset(r,notch,y1);
            float l0=Mathf.Max(r.xMin+a,clipLeft),r0=Mathf.Min(r.xMax-a,clipRight);
            float l1=Mathf.Max(r.xMin+b,clipLeft),r1=Mathf.Min(r.xMax-b,clipRight);
            if(l0>=r0||l1>=r1)continue;
            int first=mesh.currentVertCount;
            var c0=Color.Lerp(bottom,top,i/(float)steps);var c1=Color.Lerp(bottom,top,(i+1f)/steps);
            mesh.AddVert(new Vector2(l0,y0),c0,Vector2.zero);mesh.AddVert(new Vector2(r0,y0),c0,Vector2.zero);
            mesh.AddVert(new Vector2(r1,y1),c1,Vector2.zero);mesh.AddVert(new Vector2(l1,y1),c1,Vector2.zero);
            mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
        }
    }
    static void Shackle(UnityEngine.UI.VertexHelper mesh,Vector2 origin,float scale,float thickness,Color top,Color bottom)
    {
        float radius=4.3f*scale,half=thickness*.5f*scale;
        var center=origin+Vector2.up*16.5f*scale;
        Round(mesh,new Rect(center.x-radius-half,origin.y+10*scale,half*2,6.5f*scale),half,top,bottom);
        Round(mesh,new Rect(center.x+radius-half,origin.y+10*scale,half*2,6.5f*scale),half,top,bottom);
        int first=mesh.currentVertCount;
        const int steps=24;
        for(int i=0;i<=steps;i++)
        {
            float angle=i*Mathf.PI/steps;var dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            var tint=Color.Lerp(bottom,top,dir.y);
            mesh.AddVert(center+dir*(radius+half),tint,Vector2.zero);
            mesh.AddVert(center+dir*(radius-half),tint,Vector2.zero);
            if(i==0)continue;int p=first+(i-1)*2;
            mesh.AddTriangle(p,p+2,p+1);mesh.AddTriangle(p+1,p+2,p+3);
        }
    }
    void Plaque(UnityEngine.UI.VertexHelper mesh)
    {
        var r=rectTransform.rect;float w=r.width,h=r.height;
        // The base touches the actual railway stop; the numbered face stands above it.
        Round(mesh,new Rect(r.xMin+2,r.yMin-1,w-2,7),3,new Color(.16f,.08f,.02f,.25f),new Color(.16f,.08f,.02f,0));
        Round(mesh,new Rect(-w*.28f,r.yMin+1,w*.60f,10),2,new Color(.66f,.29f,.055f),new Color(.85f,.43f,.08f));
        Round(mesh,new Rect(-w*.28f,r.yMin+4,w*.60f,7),2,new Color(1,.81f,.36f),new Color(.92f,.54f,.13f));
        Round(mesh,new Rect(r.xMin+2,r.yMin+9,w-2,h-10),6,new Color(.65f,.30f,.07f),new Color(.51f,.22f,.045f));
        var front=new Rect(r.xMin,r.yMin+11,w-3,h-13);
        Round(mesh,front,6,selected?new Color(1,.90f,.47f):new Color(1,.83f,.43f),new Color(.85f,.47f,.12f));
        var face=new Rect(front.x+2.4f,front.y+2.4f,front.width-4.8f,front.height-4.8f);
        Round(mesh,face,4,arrived?new Color(.80f,1,.69f):new Color(1,.98f,.85f),
            arrived?new Color(.34f,.88f,.36f):new Color(1,.89f,.67f));
        if(arrived)
        {
            Round(mesh,new Rect(-1,r.yMax-1,2,5),.6f,new Color(.12f,.44f,.19f),new Color(.10f,.31f,.14f));
            Disc(mesh,new Vector2(0,r.yMax+5),4.5f,new Color(.44f,1,.54f,.22f),new Color(.15f,1,.28f,.10f));
            Disc(mesh,new Vector2(0,r.yMax+5),2.5f,new Color(.80f,1,.68f),new Color(.05f,.72f,.2f));
        }
    }
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
