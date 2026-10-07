using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Saved rounded ice art and a puck-height sliding trigger.</summary>
public static class SnowflakeIceBaker
{
    const string Folder="Assets/Art/SnowflakeIce";
    [MenuItem("RealBuca/Prebuild/Snowflake Ice Tiles")]
    public static void Bake()
    {
        string texturePath=Folder+"/SnowflakeFrost.png";AssetDatabase.ImportAsset(texturePath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.anisoLevel=8;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/FrostedIce.mat");
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Folder+"/FrostedIce.mat");}
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.65f);mat.SetFloat("_Metallic",.05f);EditorUtility.SetDirty(mat);
        foreach(var directory in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{directory}).Select(AssetDatabase.GUIDToAssetPath))
        {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<IcePatch>(true).Any(p=>p.patchDamping<=1))continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var patch in root.GetComponentsInChildren<IcePatch>(true).Where(p=>p.patchDamping<=1))Apply(patch,mat);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    static void Apply(IcePatch patch,Material material)
    {
        var scale=patch.transform.lossyScale;
        // The art stays thin; the authored trigger covers the puck's travel plane.
        var trigger=patch.GetComponent<BoxCollider>();
        if(trigger!=null) {
            var size=trigger.size;size.y=.7f/Mathf.Abs(scale.y);trigger.size=size;
            var center=trigger.center;center.y=.30f/scale.y;trigger.center=center;
            trigger.isTrigger=true;
        }
        patch.patchDamping=.05f;
        float width=Mathf.Abs(scale.x),length=Mathf.Abs(scale.z);
        string meshPath=Folder+"/Tile_"+width.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"_"+length.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(mesh==null){mesh=Tile(width,length);AssetDatabase.CreateAsset(mesh,meshPath);}
        var renderer=patch.GetComponent<MeshRenderer>();if(renderer!=null)renderer.enabled=false;
        var old=patch.transform.Find("SnowflakeIceTile");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var go=new GameObject("SnowflakeIceTile",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(patch.transform,false);
        go.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    static Mesh Tile(float width,float length)
    {
        var outline=new List<Vector2>();float radius=Mathf.Min(width,length)*.085f;
        for(int corner=0;corner<4;corner++)
        {
            float angle=corner*90;float cx=corner==0||corner==3?width*.5f-radius:-width*.5f+radius;float cy=corner<2?length*.5f-radius:-length*.5f+radius;
            for(int j=0;j<=12;j++){float a=(angle+j*90f/12)*Mathf.Deg2Rad;outline.Add(new Vector2(cx+Mathf.Cos(a)*radius,cy+Mathf.Sin(a)*radius));}
        }
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int n=outline.Count;
        float[] sizes={1,1,.988f,.975f},heights={-.009f,.008f,.025f,.032f};
        for(int ring=0;ring<4;ring++)foreach(var p in outline){vertices.Add(new Vector3(p.x*sizes[ring],heights[ring],p.y*sizes[ring]));uv.Add(new Vector2(p.x/width+.5f,p.y/length+.5f));}
        void Tri(int a,int b,int c,Vector3 normal){if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normal)>=0)triangles.AddRange(new[]{a,b,c});else triangles.AddRange(new[]{a,c,b});}
        for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++){int j=(i+1)%n;var e=outline[j]-outline[i];var normal=new Vector3(e.y,0,-e.x);Tri(ring*n+i,ring*n+j,(ring+1)*n+i,normal);Tri(ring*n+j,(ring+1)*n+j,(ring+1)*n+i,normal);}
        int center=vertices.Count;vertices.Add(new Vector3(0,.032f,0));uv.Add(Vector2.one*.5f);for(int i=0;i<n;i++)Tri(center,3*n+i,3*n+(i+1)%n,Vector3.up);
        var mesh=new Mesh{name="Rounded frosted ice tile"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
