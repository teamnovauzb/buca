using UnityEngine;

/// <summary>Shows the approved railway artwork only in a feathered strip along the forest rails.</summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ForestHarborRailArtwork : UnityEngine.UI.MaskableGraphic
{
    public Texture2D artworkTexture;
    public Vector2[] pixelPath;
    public float solidHalfWidth = 34;
    public float featherHalfWidth = 42;
    public override Texture mainTexture => artworkTexture != null ? artworkTexture : s_WhiteTexture;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        if (pixelPath == null || pixelPath.Length < 2) return;
        float[] offsets = { -featherHalfWidth, -solidHalfWidth, solidHalfWidth, featherHalfWidth };
        for (int i = 0; i < pixelPath.Length; i++)
        {
            var tangent = (pixelPath[Mathf.Min(i + 1, pixelPath.Length - 1)] -
                pixelPath[Mathf.Max(0, i - 1)]).normalized;
            var normal = new Vector2(-tangent.y, tangent.x);
            float endFade = Mathf.Clamp01(Mathf.Min(
                Vector2.Distance(pixelPath[i], pixelPath[0]),
                Vector2.Distance(pixelPath[i], pixelPath[pixelPath.Length - 1])) / 18);
            for (int lane = 0; lane < 4; lane++)
            {
                var pixel = pixelPath[i] + normal * offsets[lane];
                var vertex = UIVertex.simpleVert;
                vertex.position = new Vector3(pixel.x - 836, 470.5f - pixel.y, 0);
                vertex.uv0 = new Vector2(pixel.x / 1672, 1 - pixel.y / 941);
                var tint = color;
                tint.a *= lane == 0 || lane == 3 ? 0 : endFade;
                vertex.color = tint;
                mesh.AddVert(vertex);
            }
            if (i == 0) continue;
            for (int lane = 0; lane < 3; lane++)
            {
                int a = (i - 1) * 4 + lane, b = i * 4 + lane;
                mesh.AddTriangle(a, b, b + 1);
                mesh.AddTriangle(a, b + 1, a + 1);
            }
        }
    }
}
