using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Saved wooden bridge hazard art; preserves the original deadly trigger.</summary>
public static class BrokenBridgeGutterBaker
{
    const string Folder="Assets/Art/BrokenBridge";
    [MenuItem("RealBuca/Prebuild/Broken Bridge Gutters")]
    public static void Bake()
    {
        foreach(var dir in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{dir}).Select(AssetDatabase.GUIDToAssetPath)) {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset.GetComponentsInChildren<DeadlyTrigger>(true).Any(p=>p.name.Contains("Gutter")))continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try {foreach(var p in root.GetComponentsInChildren<DeadlyTrigger>(true).Where(p=>p.name.Contains("Gutter")))Apply(p);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    public static void Apply(DeadlyTrigger gutter)
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art","BrokenBridge");
        ToyBoxGeometry.Initialize();
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/Materials/NaturalBirch.mat");
        var coral=Mat("Coral",new Color(.74f,.12f,.075f));var dark=Mat("Recess",new Color(.025f,.035f,.032f));var ivory=Mat("Ivory",new Color(1,.88f,.62f));
        var old=gutter.transform.Find("BrokenBridgeVisual");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var visual=new GameObject("BrokenBridgeVisual").transform;
        visual.SetParent(gutter.transform.parent,false);visual.position=new Vector3(gutter.transform.position.x,.012f,gutter.transform.position.z);
        var bounds=gutter.GetComponent<Collider>().bounds;
        float width=Mathf.Min(bounds.size.x,bounds.size.z),length=Mathf.Max(bounds.size.x,bounds.size.z);
        visual.rotation=Quaternion.Euler(0,bounds.size.x>bounds.size.z?90:0,0);
        visual.SetParent(gutter.transform,true);
        foreach(var r in gutter.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
        void Box(string name,Vector3 pos,Vector3 size,Material mat,float bevel=.025f) {Part(name,visual,pos,ToyBoxGeometry.RoundedBox(size,bevel),mat);}
        Box("DarkGutter",Vector3.zero,new Vector3(width,.035f,length),dark);
        for(int side=-1;side<=1;side+=2) {
            Box("MapleEdge",new Vector3(side*width*.46f,.035f,0),new Vector3(width*.08f,.08f,length),wood);
            Box("EndBlock",new Vector3(0,.06f,side*(length*.5f-.24f)),new Vector3(width,.12f,.48f),wood);
            for(int lane=-1;lane<=1;lane+=2) {
                float span=length*.5f-.7f;
                var plank=Part("BrokenPlank",visual,new Vector3(lane*width*.23f,.055f,side*(.25f+span*.5f)),BrokenPlank(width*.34f,span),wood);
                if(side<0)plank.transform.localRotation=Quaternion.Euler(0,180,0);
            }
        }
        for(int i=0;i<4;i++) {
            float z=(i-1.5f)*(length-.95f)/4;
            if(Mathf.Abs(z)<.35f)continue;
            Box("CoralBrace",new Vector3(0,.13f,z),new Vector3(width*.83f,.075f,.19f),coral);
            for(int side=-1;side<=1;side+=2)Part("WoodPin",visual,new Vector3(side*width*.29f,.178f,z),ToyBoxGeometry.Disc(.027f,.015f,.004f),ivory);
        }
        var heart=Part("LoseHeart",visual,new Vector3(0,.14f,-length*.5f+.24f),AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ToyBoxMenu/Meshes/DomedShotHeart.asset"),coral);
        heart.transform.localRotation=Quaternion.Euler(90,0,0);heart.transform.localScale=Vector3.one*(width*.63f);
        Box("Minus",new Vector3(0,.203f,-length*.5f+.24f),new Vector3(width*.32f,.012f,.035f),ivory,.005f);
        Box("WarningStem",new Vector3(0,.13f,length*.5f-.2f),new Vector3(.04f,.02f,.17f),coral,.008f);
        Box("WarningDot",new Vector3(0,.13f,length*.5f-.34f),new Vector3(.045f,.02f,.045f),coral,.008f);
    }
    static GameObject Part(string name,Transform parent,Vector3 pos,Mesh mesh,Material mat) {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
    }
    static Material Mat(string name,Color color) {var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.32f);return m;}
    static Mesh BrokenPlank(float width,float length) {
        var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
        var outline=new[]{new Vector2(-width/2,-length/2),new Vector2(-width*.18f,-length/2+.08f),new Vector2(width*.08f,-length/2-.03f),new Vector2(width*.30f,-length/2+.09f),new Vector2(width/2,-length/2),new Vector2(width/2,length/2),new Vector2(-width/2,length/2)};
        for(int i=0;i<outline.Length;i++){var a=outline[i];var b=outline[(i+1)%outline.Length];int n=v.Count;v.AddRange(new[]{new Vector3(a.x,0,a.y),new Vector3(b.x,0,b.y),new Vector3(b.x,.065f,b.y),new Vector3(a.x,.065f,a.y)});t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}
        for(int i=0;i<outline.Length;i++){int n=v.Count;var a=outline[i];var b=outline[(i+1)%outline.Length];v.AddRange(new[]{new Vector3(0,.065f,length*.2f),new Vector3(a.x,.065f,a.y),new Vector3(b.x,.065f,b.y)});t.AddRange(new[]{n,n+2,n+1});}
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return ToyBoxGeometry.Save(mesh,Folder+"/Plank_"+width.ToString("F3")+"_"+length.ToString("F3")+".asset");
    }
}
