using UnityEngine;
using UnityEngine.UI;

/// <summary>Soft contact shadow drawn as UI geometry; no generated texture is needed.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HarborCraneShadow : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        const int sides = 40;
        Vector2 half = rectTransform.rect.size*.5f;
        vh.AddVert(Vector3.zero, color, Vector2.zero);
        for (int ring=0; ring<2; ring++)
            for (int i=0; i<sides; i++)
            {
                float a = i*Mathf.PI*2/sides, radius = ring == 0 ? .38f : 1;
                Color c = color; if (ring == 1) c.a = 0;
                vh.AddVert(new Vector3(Mathf.Cos(a)*half.x*radius,Mathf.Sin(a)*half.y*radius),c,Vector2.zero);
            }
        for (int i=0; i<sides; i++)
        {
            int a=1+i, b=1+(i+1)%sides;
            vh.AddTriangle(0,b,a);
            vh.AddTriangle(a,b,b+sides); vh.AddTriangle(a,b+sides,a+sides);
        }
    }
}
