using UnityEngine;

/// <summary>Continuous shaded chassis, axle supports and couplings behind the train artwork.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GrandTrainUndercarriage : UnityEngine.UI.MaskableGraphic
{
    static readonly Color Navy=new Color(.025f,.075f,.14f), Steel=new Color(.12f,.22f,.3f), Gold=new Color(.72f,.37f,.055f), LightGold=new Color(1,.83f,.37f);
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        // Connectors overlap the frame ends, so the joints stay attached during the entrance.
        Coupler(vh,-400,-360);Coupler(vh,32,100);Coupler(vh,500,562);
        Frame(vh,-810,-365,-110);Frame(vh,-397,57,-185);Frame(vh,72,527,-185);Frame(vh,535,805,-185);
        foreach(float x in new[]{-750f,-540f,-340f,-10f,130f,460f,590f,750f})
        {
            Box(vh,x-28,-241,56,42,Navy,Steel);
            Box(vh,x-22,-232,44,25,Gold,LightGold);
            Disc(vh,x,-234,16,Navy);Disc(vh,x,-234,10,LightGold);
        }
        // The engine's running board and side rod anchor the wheel assembly to the boiler.
        Box(vh,-804,-184,438,12,Gold,LightGold);
        Box(vh,-753,-239,216,10,Gold,LightGold);
    }
    static void Frame(UnityEngine.UI.VertexHelper vh,float left,float right,float top)
    {
        Box(vh,left+5,-255,right-left-10,13,new Color(0,0,0,.22f),new Color(0,0,0,.05f));
        Box(vh,left,-238,right-left,top+238,Navy,Steel);
        Box(vh,left,-208,right-left,9,Gold,LightGold);
        Box(vh,left+9,-233,right-left-18,6,new Color(.015f,.03f,.06f),Steel);
        for(float x=left+20;x<right-10;x+=54){Disc(vh,x,-217,4,Gold);Disc(vh,x-1,-216,2,LightGold);}
    }
    static void Coupler(UnityEngine.UI.VertexHelper vh,float a,float b)
    {
        Box(vh,a,-225,b-a,20,Navy,Steel);
        Box(vh,a,-216,b-a,9,Gold,LightGold);
        for(int i=0;i<5;i++)Box(vh,Mathf.Lerp(a,b,(i+1)/6f)-2,-224,4,23,Gold,LightGold);
        Disc(vh,a+5,-214,13,Gold);Disc(vh,b-5,-214,13,Gold);
        Disc(vh,a+5,-212,6,LightGold);Disc(vh,b-5,-212,6,LightGold);
    }
    static void Box(UnityEngine.UI.VertexHelper vh,float x,float y,float w,float h,Color bottom,Color top)
    {
        int n=vh.currentVertCount;vh.AddVert(new Vector3(x,y),bottom,Vector2.zero);vh.AddVert(new Vector3(x+w,y),bottom,Vector2.zero);vh.AddVert(new Vector3(x+w,y+h),top,Vector2.zero);vh.AddVert(new Vector3(x,y+h),top,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
    }
    static void Disc(UnityEngine.UI.VertexHelper vh,float x,float y,float radius,Color color)
    {
        int n=vh.currentVertCount;vh.AddVert(new Vector3(x,y),color,Vector2.zero);
        for(int i=0;i<=20;i++){float a=i*Mathf.PI/10;vh.AddVert(new Vector3(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),color,Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1);}
    }
}
