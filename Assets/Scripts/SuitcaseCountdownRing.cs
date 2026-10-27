using UnityEngine;
using UnityEngine.UI;

/// <summary>A smooth clockwise countdown arc, drawn over the illustrated watch dial.</summary>
[AddComponentMenu("BUCA/Suitcase Countdown Ring")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SuitcaseCountdownRing : MaskableGraphic
{
    [SerializeField, Range(0, 1)] float fill = 1;
    public float thickness = 4.5f;
    public float Fill
    {
        get => fill;
        set
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(fill, value)) return;
            fill = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (fill <= 0) return;
        var rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(rect.width, rect.height) * .5f - 1;
        float inner = Mathf.Max(0, radius - thickness);
        int segments = Mathf.Max(1, Mathf.CeilToInt(160 * fill));
        var transparent = new Color(color.r, color.g, color.b, 0);
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.PI * .5f - Mathf.PI * 2 * fill * i / segments;
            float b = Mathf.PI * .5f - Mathf.PI * 2 * fill * (i + 1) / segments;
            Band(mesh, rect.center, a, b, inner - 1, inner, transparent, color);
            Band(mesh, rect.center, a, b, inner, radius, color, color);
            Band(mesh, rect.center, a, b, radius, radius + 1, color, transparent);
        }
    }

    static void Band(VertexHelper mesh, Vector2 center, float a, float b,
        float inner, float outer, Color inside, Color outside)
    {
        var da = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        var db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
        int start = mesh.currentVertCount;
        mesh.AddVert(center + da * inner, inside, Vector2.zero);
        mesh.AddVert(center + da * outer, outside, Vector2.zero);
        mesh.AddVert(center + db * outer, outside, Vector2.zero);
        mesh.AddVert(center + db * inner, inside, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start, start + 2, start + 3);
    }
}
