using UnityEngine;
using UnityEngine.UI;

/// <summary>The existing front cargo remains in front of the crane's stationary footing.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HarborCraneForeground : MaskableGraphic
{
    public Texture2D artwork;
    public bool leftCargo = true, rightCargo = true;
    public override Texture mainTexture => artwork != null ? artwork : base.mainTexture;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if(leftCargo) Polygon(vh,new[]{
            new Vector2(1351,535),new Vector2(1367,530),new Vector2(1391,540),
            new Vector2(1391,565),new Vector2(1376,571),new Vector2(1350,561)});
        if(rightCargo) Polygon(vh,new[]{
            new Vector2(1418,522),new Vector2(1431,524),new Vector2(1464,536),
            new Vector2(1467,543),new Vector2(1465,566),new Vector2(1450,573),new Vector2(1417,558)});
    }
    void Polygon(VertexHelper vh,Vector2[] points)
    {
        int first=vh.currentVertCount;
        foreach(var p in points)
            vh.AddVert(new Vector3(p.x-836,470.5f-p.y),color,new Vector2(p.x/1672,1-p.y/941));
        for(int i=1;i<points.Length-1;i++)vh.AddTriangle(first,first+i,first+i+1);
    }
}
