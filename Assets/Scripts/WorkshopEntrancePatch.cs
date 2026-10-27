using UnityEngine;

/// <summary>Displays only the revised entrance region, preserving the rest of the map plate.</summary>
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class WorkshopEntrancePatch : UnityEngine.UI.MaskableGraphic
{
    public Texture2D texture;
    public Rect pixelRegion = new Rect(0, 465, 450, 310);
    public override Texture mainTexture => texture != null ? texture : s_WhiteTexture;
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        var r = rectTransform.rect;
        float[] xs = { 0, .94f, 1 };
        float[] ys = { 0, .04f, .96f, 1 };
        for (int y = 0; y < ys.Length; y++)
        for (int x = 0; x < xs.Length; x++)
        {
            var v = UIVertex.simpleVert;
            v.position = new Vector3(r.xMin + xs[x] * r.width, r.yMin + ys[y] * r.height);
            v.uv0 = new Vector2((pixelRegion.x + xs[x] * pixelRegion.width) / 1672f,
                1 - (pixelRegion.y + (1 - ys[y]) * pixelRegion.height) / 941f);
            var c = color;
            c.a *= x == 2 || y == 0 || y == 3 ? 0 : 1;
            v.color = c;
            mesh.AddVert(v);
            if (x == 0 || y == 0) continue;
            int i = y * 3 + x;
            mesh.AddTriangle(i - 4, i - 3, i);
            mesh.AddTriangle(i - 4, i, i - 1);
        }
    }
}
