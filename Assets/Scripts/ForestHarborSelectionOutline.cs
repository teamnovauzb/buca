using UnityEngine;

/// <summary>A hollow gold rim that keeps the selected level number fully readable.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ForestHarborSelectionOutline : UnityEngine.UI.MaskableGraphic
{
    public bool circular;
    public bool arrived;
    const int Segments = 96;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
    {
        vertices.Clear();
        var size = rectTransform.rect.size;
        if(arrived)
        {
            Band(vertices,size,10,new Color(.1f,1,.25f,0),3,new Color(.1f,1,.25f,.55f));
            Band(vertices,size,3,new Color(.02f,.42f,.1f,1),0,new Color(.15f,1,.3f,1));
            Band(vertices,size,0,new Color(.8f,1,.66f,1),-2,new Color(.12f,.85f,.18f,1));
            return;
        }
        // Soft amber halo, dark outer edge, metallic gold, and a narrow cream glint.
        Band(vertices, size, 4, new Color(1, .62f, .05f, 0), .8f, new Color(1, .69f, .1f, .65f));
        Band(vertices, size, 1, new Color(.56f, .25f, .025f, 1), -1.8f, new Color(1, .68f, .12f, 1));
        Band(vertices, size, .4f, new Color(1, .81f, .25f, 1), -1.1f, new Color(1, .96f, .66f, 1));
        Band(vertices, size, -1.1f, new Color(1, .96f, .66f, 1), -2, new Color(.95f, .51f, .035f, 1));
    }

    void Band(UnityEngine.UI.VertexHelper vertices, Vector2 size,
        float outer, Color outerColor, float inner, Color innerColor)
    {
        int first = vertices.currentVertCount;
        for (int i = 0; i <= Segments; i++)
        {
            float angle = i * Mathf.PI * 2 / Segments;
            Add(vertices, Contour(size, outer, angle), outerColor);
            Add(vertices, Contour(size, inner, angle), innerColor);
            if (i == 0) continue;
            int previous = first + (i - 1) * 2;
            vertices.AddTriangle(previous, previous + 2, previous + 1);
            vertices.AddTriangle(previous + 1, previous + 2, previous + 3);
        }
    }

    Vector2 Contour(Vector2 size, float margin, float angle)
    {
        var half = size * .5f + Vector2.one * margin;
        var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        if (circular) return Vector2.Scale(direction, half);
        float radius = Mathf.Max(1, 5 + margin);
        var center = new Vector2(Mathf.Sign(direction.x) * (half.x - radius),
            Mathf.Sign(direction.y) * (half.y - radius));
        return center + direction * radius;
    }

    void Add(UnityEngine.UI.VertexHelper vertices, Vector2 position, Color tint)
    {
        var vertex = UnityEngine.UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = tint * color;
        vertices.AddVert(vertex);
    }
}
