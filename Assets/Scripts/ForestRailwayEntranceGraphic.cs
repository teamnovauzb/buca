using UnityEngine;

/// <summary>Blends the wide railway continuation into the map, reaching its lower edge.</summary>
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class ForestRailwayEntranceGraphic : UnityEngine.UI.MaskableGraphic
{
    public Texture2D artworkTexture;
    public float sideBlend = 16;
    public float topBlend = 12;
    public override Texture mainTexture => artworkTexture != null ? artworkTexture : s_WhiteTexture;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        float sx = Mathf.Clamp01(sideBlend / rect.width);
        float sy = Mathf.Clamp01(topBlend / rect.height);
        float[] x = {0, sx, 1-sx, 1};
        float[] y = {0, 1-sy, 1};
        for(int row=0;row<y.Length;row++)
        for(int column=0;column<x.Length;column++)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(rect.xMin+x[column]*rect.width, rect.yMin+y[row]*rect.height);
            vertex.uv0 = new Vector2(x[column],y[row]);
            var tint = color;
            tint.a *= column==0 || column==3 || row==2 ? 0 : 1;
            vertex.color = tint;
            mesh.AddVert(vertex);
            if(row==0 || column==0) continue;
            int i=row*4+column;
            mesh.AddTriangle(i-5,i-4,i);
            mesh.AddTriangle(i-5,i,i-1);
        }
    }
}
