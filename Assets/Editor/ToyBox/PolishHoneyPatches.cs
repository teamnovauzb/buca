#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static partial class BuildToyBoxMainMenu
{
    public static void PolishHoneyPatches()
    {
        const string texPath="Assets/Textures/Honey/HoneyPuddle.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.alphaIsTransparency=true; importer.mipmapEnabled=true;
        importer.wrapMode=TextureWrapMode.Clamp; importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mechanic_Mud.mat");
        mat.shader=Shader.Find("Universal Render Pipeline/Unlit");
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetColor("_BaseColor",Color.white);
        mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetTextureOffset("_BaseMap",Vector2.zero);
        mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);
        mat.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3000;
        EditorUtility.SetDirty(mat);
        const string meshPath="Assets/ToyBoxMenu/Meshes/HoneyFlatSurface.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null){mesh=new Mesh{name="HoneyFlatSurface"};AssetDatabase.CreateAsset(mesh,meshPath);}
        // Flat silhouette follows the painted rim; no rectangular background
        // is drawn. UVs retain the original artwork without modifying the image.
        var contour=new[]{new Vector2(80,240),new Vector2(160,153),new Vector2(310,109),new Vector2(435,111),new Vector2(600,148),new Vector2(760,160),new Vector2(940,126),new Vector2(1120,120),new Vector2(1320,129),new Vector2(1403,177),new Vector2(1443,256),new Vector2(1441,350),new Vector2(1456,446),new Vector2(1480,600),new Vector2(1473,731),new Vector2(1406,827),new Vector2(1270,880),new Vector2(1140,905),new Vector2(990,910),new Vector2(810,873),new Vector2(671,855),new Vector2(518,871),new Vector2(370,893),new Vector2(238,878),new Vector2(123,817),new Vector2(70,723),new Vector2(60,625),new Vector2(94,511),new Vector2(110,431),new Vector2(91,343)};
        var uv=new System.Collections.Generic.List<Vector2>{new Vector2(.5f,.5f)};
        for(int i=0;i<contour.Length;i++)for(int j=0;j<5;j++){
            float t=j/5f;var a=contour[(i+contour.Length-1)%contour.Length];var b=contour[i];var c=contour[(i+1)%contour.Length];var d=contour[(i+2)%contour.Length];
            var p=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
            uv.Add(new Vector2(p.x/1536f,1-p.y/1024f));
        }
        var verts=new Vector3[uv.Count];for(int i=0;i<verts.Length;i++)verts[i]=new Vector3(uv[i].x-.5f,.55f,uv[i].y-.5f);
        var tris=new int[(uv.Count-1)*3];for(int i=1;i<uv.Count;i++){int k=(i-1)*3;tris[k]=0;tris[k+1]=i;tris[k+2]=i==uv.Count-1?1:i+1;}
        mesh.Clear();mesh.vertices=verts;mesh.uv=uv.ToArray();mesh.triangles=tris;
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        int count=0;
        foreach(var folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{folder}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bool has=false;foreach(var patch in asset.GetComponentsInChildren<IcePatch>(true))if(patch.patchDamping>1)has=true;
            if(!has)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{
                foreach(var patch in root.GetComponentsInChildren<IcePatch>(true)){
                    if(patch.patchDamping<=1)continue;
                    var filter=patch.GetComponent<MeshFilter>();var renderer=patch.GetComponent<MeshRenderer>();
                    if(filter==null||renderer==null)continue;
                    filter.sharedMesh=mesh;renderer.sharedMaterial=mat;renderer.enabled=true;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                    count++;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        PolishTeddyCoachTutorial();
        Debug.Log("HONEY_PATCHES_SAVED: "+count);
        System.IO.File.WriteAllText("/tmp/buca-honey-result.txt","Saved "+count+" flat honey patches and matching tutorial.");
    }
}
#endif
