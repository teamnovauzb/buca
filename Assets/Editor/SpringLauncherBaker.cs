using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Prebuilt spring launcher meshes; no runtime art generation.</summary>
public static class SpringLauncherBaker
{
    const string Folder="Assets/Art/SpringLauncher";
    [MenuItem("RealBuca/Prebuild/Spring Launchers")]
    public static void Bake()
    {
        foreach(var dir in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{dir}).Select(AssetDatabase.GUIDToAssetPath)) {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<BouncePad>(true).Any())continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var pad in root.GetComponentsInChildren<BouncePad>(true))Apply(pad);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    public static void Apply(BouncePad pad)
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art","SpringLauncher");
        ToyBoxGeometry.Initialize();
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/Materials/NaturalBirch.mat");
        var teal=Mat("TealBed",new Color(.015f,.22f,.23f),.34f,0);
        var coral=Mat("CoralPlate",new Color(.86f,.25f,.12f),.38f,0);
        var brass=Mat("BrassSpring",new Color(.78f,.54f,.20f),.55f,.65f);
        var ivory=Mat("IvoryArrow",new Color(1,.90f,.68f),.3f,0);
        var dark=Mat("SpringRecess",new Color(.015f,.045f,.04f),.2f,0);
        var old=pad.transform.Find("SpringLauncherVisual");if(old!=null)Object.DestroyImmediate(old.gameObject);
        foreach(var r in pad.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
        var v=new GameObject("SpringLauncherVisual").transform;v.SetParent(pad.transform.parent,false);v.SetPositionAndRotation(new Vector3(pad.transform.position.x,.012f,pad.transform.position.z),pad.transform.rotation);v.SetParent(pad.transform,true);
        GameObject Part(string name,Vector3 p,Mesh mesh,Material mat){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(v,false);go.transform.localPosition=p;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;}
        void Box(string name,Vector3 p,Vector3 s,Material m,float bevel=.03f)=>Part(name,p,ToyBoxGeometry.RoundedBox(s,bevel),m);
        Box("MapleBase",new Vector3(0,.04f,-.30f),new Vector3(1.2f,.10f,1.35f),wood,.045f);
        Box("TealBed",new Vector3(0,.095f,-.30f),new Vector3(1.02f,.04f,1.17f),teal,.018f);
        for(int side=-1;side<=1;side+=2)Box("MapleSide",new Vector3(side*.55f,.12f,-.30f),new Vector3(.10f,.15f,1.28f),wood,.04f);
        Box("SpringSocket",new Vector3(0,.119f,-.58f),new Vector3(.40f,.012f,.40f),dark,.005f);
        Part("CoiledBrassSpring",new Vector3(0,.19f,-.57f),Spring(),brass);
        Box("CoralPushPlate",new Vector3(0,.17f,-.83f),new Vector3(.90f,.16f,.23f),coral,.065f);
        var arrow=Part("LaunchDirection",new Vector3(0,.123f,-.06f),AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ToyBoxMenu/Meshes/ClearArcadeDirectionArrow.asset"),ivory);arrow.transform.localScale=new Vector3(3.5f,.3f,3.3f);
        // The artwork extends behind the original activation strip. Keep activation
        // at that strip so existing launch routes and timings are unchanged.
    }
    static Mesh Spring(){
        const int steps=192,sides=10;var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
        for(int i=0;i<=steps;i++){float a=i/(float)steps*Mathf.PI*2*5;var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var center=radial*.10f+Vector3.forward*((i/(float)steps-.5f)*.33f);for(int j=0;j<sides;j++){float b=j*Mathf.PI*2/sides;v.Add(center+.021f*(radial*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b)));}}
        for(int i=0;i<steps;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;t.AddRange(new[]{a,b,c,b,d,c});}
        var mesh=new Mesh{name="Brass coil"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return ToyBoxGeometry.Save(mesh,Folder+"/BrassCoil.asset");
    }
    static Material Mat(string name,Color color,float smooth,float metal){string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;}
}
