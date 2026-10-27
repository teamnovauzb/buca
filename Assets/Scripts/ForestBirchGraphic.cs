using UnityEngine;
/// <summary>Rooted trunk arc with delayed branch response to sustained wind pressure.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ForestBirchGraphic : UnityEngine.UI.MaskableGraphic
{
    public Texture2D artwork;
    public Rect uvRegion=new Rect(0,0,1,1);
    public float phase;
    float seconds;
    public override Texture mainTexture => artwork!=null?artwork:Texture2D.whiteTexture;
    public void Pose(float time){seconds=time;SetVerticesDirty();}
    void Update(){if(Application.isPlaying)Pose(Time.unscaledTime);}
    static float Ease(float x){x=Mathf.Clamp01(x);return x*x*x*(x*(x*6-15)+10);}
    // A gust builds, holds its pressure, then slowly releases. No back-and-forth oscillator.
    static float Pressure(float t)
    {
        t=Mathf.Repeat(t,16);
        if(t<5)return Mathf.Lerp(.12f,1,Ease(t/5));
        if(t<8)return 1;
        if(t<15)return Mathf.Lerp(1,.12f,Ease((t-8)/7));
        return .12f;
    }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();var rect=GetPixelAdjustedRect();const int rows=24,columns=10;
        float time=seconds-Mathf.Sin(phase)*.35f;
        for(int row=0;row<=rows;row++)
        {
            float h=(float)row/rows;
            float pressure=Pressure(time-h*.85f);
            float angle=(.045f+pressure*.48f)*h;
            const float direction=-1f;
            // Constant-length curved trunk: the tip lowers naturally as it leans.
            float length=h*rect.height;
            float x=length*(1-Mathf.Cos(angle))/Mathf.Max(angle,.0001f);
            float y=length*Mathf.Sin(angle)/Mathf.Max(angle,.0001f);
            for(int col=0;col<=columns;col++)
            {
                float u=(float)col/columns;
                float branch=(u-.5f)*rect.width;
                float leafWeight=Ease((h-.23f)/.55f);
                float delayed=Pressure(time-h*.85f-Mathf.Abs(u-.5f)*2.4f);
                float branchAngle=direction*(angle+(delayed-pressure)*.36f*leafWeight);
                float branchFlex=delayed*leafWeight*Mathf.Pow(Mathf.Abs(u-.5f)*2,2)*rect.width*.055f;
                vh.AddVert(new Vector3(rect.center.x+direction*x+branch*Mathf.Cos(branchAngle)+direction*branchFlex,
                    rect.y+y-branch*Mathf.Sin(branchAngle),0),color,
                    new Vector2(uvRegion.x+u*uvRegion.width,uvRegion.y+h*uvRegion.height));
                if(row==0||col==0)continue;
                int b=(row-1)*(columns+1)+col-1;
                vh.AddTriangle(b,b+columns+1,b+1);vh.AddTriangle(b+1,b+columns+1,b+columns+2);
            }
        }
    }
}
