using System.Collections.Generic;
using UnityEngine;
public static class CarriageCraftMesh
{
    static Mesh rounded;
    public static Mesh RoundedBox
    {
        get{
            if(rounded!=null)return rounded;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
                var u=Vector3.Cross(normal,Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up);var v=Vector3.Cross(normal,u);int start=vertices.Count;
                const int steps=8;
                for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++){
                    var p=normal*.5f+u*(x/(float)steps-.5f)+v*(y/(float)steps-.5f);
                    var inner=new Vector3(Mathf.Clamp(p.x,-.40f,.40f),Mathf.Clamp(p.y,-.40f,.40f),Mathf.Clamp(p.z,-.40f,.40f));var roundedNormal=(p-inner).normalized;vertices.Add(inner+roundedNormal*.10f);normals.Add(roundedNormal);
                }
                for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){
                    int a=start+y*(steps+1)+x,b=a+1,c=a+steps+1,d=c+1;triangles.AddRange(new[]{a,b,d,a,d,c});
                }
            }
            rounded=new Mesh{name="Rounded carriage joinery"};rounded.SetVertices(vertices);rounded.SetTriangles(triangles,0);rounded.SetNormals(normals);rounded.RecalculateBounds();return rounded;
        }
    }
}
