using UnityEngine;
using UnityEngine.UI;

/// <summary>Soft contact under the toy wheels, drawn as inexpensive UI geometry.</summary>
public sealed class ForestHarborTrackShadow : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        mesh.AddVert(rect.center,new Color(0.025f,.055f,.065f,.27f),Vector2.one*.5f);
        const int segments = 40;
        for(int i=0;i<=segments;i++)
        {
            float a=i*Mathf.PI*2/segments;
            mesh.AddVert(rect.center+new Vector2(Mathf.Cos(a)*rect.width*.5f,Mathf.Sin(a)*rect.height*.5f),Color.clear,Vector2.zero);
            if(i>0)mesh.AddTriangle(0,i,i+1);
        }
    }
}
