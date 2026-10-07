using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Prebuilt miniature conveyor artwork, aligned with the existing push direction.</summary>
public static class MiniConveyorBaker
{
    const string Folder="Assets/Art/MiniConveyor";
    [MenuItem("RealBuca/Prebuild/Mini Conveyors")]
    public static void Bake()
    {
        foreach(var dir in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{dir}).Select(AssetDatabase.GUIDToAssetPath)) {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<BucaWindZone>(true).Any(p=>p.name.Contains("Conveyor")))continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try {foreach(var p in root.GetComponentsInChildren<BucaWindZone>(true).Where(p=>p.name.Contains("Conveyor")))Apply(p);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    public static void Apply(BucaWindZone pad)
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art","MiniConveyor");
        ToyBoxGeometry.Initialize();
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/Materials/NaturalBirch.mat");
        var teal=Mat("RubberTeal",new Color(.018f,.23f,.24f),.32f,0);
        var dark=Mat("Recess",new Color(.012f,.035f,.034f),.18f,0);
        var brass=Mat("BrassRollers",new Color(.78f,.55f,.22f),.55f,.65f);
        var cream=Mat("CreamArrows",new Color(1,.91f,.69f),.3f,0);
        var previous=pad.transform.Find("MiniConveyorVisual");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
        foreach(var r in pad.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
        // Legacy direction markers were authored beside, rather than under, the pad.
        foreach(Transform sibling in pad.transform.parent)
            if(sibling.name.StartsWith("DP_WindTelegraph") &&
               Vector2.Distance(new Vector2(sibling.position.x,sibling.position.z),new Vector2(pad.transform.position.x,pad.transform.position.z))<.1f)
                sibling.gameObject.SetActive(false);
        var size=pad.transform.lossyScale;
        var trigger=pad.GetComponent<BoxCollider>();
        if(trigger!=null){var dimensions=trigger.size;dimensions.y=.7f/Mathf.Abs(size.y);trigger.size=dimensions;var center=trigger.center;center.y=.3f/size.y;trigger.center=center;trigger.isTrigger=true;}
        // Restore a clearly perceptible belt push, even on the introduction board.
        pad.forceMagnitude=Mathf.Max(pad.forceMagnitude,3.5f);
        pad.conveyor=true;pad.conveyorSpeed=3f;
        float width=Mathf.Abs(size.x),length=Mathf.Abs(size.z);
        var v=new GameObject("MiniConveyorVisual").transform;
        v.SetParent(pad.transform.parent,false);v.SetPositionAndRotation(new Vector3(pad.transform.position.x,.012f,pad.transform.position.z),pad.transform.rotation);v.SetParent(pad.transform,true);
        GameObject Part(string name,Vector3 p,Mesh mesh,Material mat) {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(v,false);go.transform.localPosition=p;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;}
        void Box(string name,Vector3 p,Vector3 s,Material m,float bevel=.025f)=>Part(name,p,ToyBoxGeometry.RoundedBox(s,bevel),m);
        Box("MapleFrame",new Vector3(0,.025f,0),new Vector3(width,.065f,length),wood,.03f);
        Box("BeltSocket",new Vector3(0,.062f,0),new Vector3(width-.10f,.025f,length-.10f),dark,.012f);
        Box("TealBelt",new Vector3(0,.083f,0),new Vector3(width-.20f,.035f,length-.25f),teal,.015f);
        int ribs=24;
        for(int i=0;i<ribs;i++)Box("BeltRib"+i,new Vector3(0,.107f,(i/(float)(ribs-1)-.5f)*(length-.32f)),new Vector3(width-.25f,.025f,.028f),teal,.012f);
        for(int side=-1;side<=1;side+=2) {
            var roller=Part("BrassRoller",new Vector3(0,.086f,side*(length*.5f-.13f)),ToyBoxGeometry.Disc(.075f,width-.18f,.012f),brass);roller.transform.localRotation=Quaternion.Euler(0,0,90);
        }
        var arrow=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/MintBoost/RoundedChevron.asset");
        for(int i=0;i<3;i++){var a=Part("DirectionArrow"+i,new Vector3(0,.126f,(i-1)*length*.23f),arrow,cream);a.transform.localScale=new Vector3(width*.26f,.25f,length*.34f);}
    }
    static Material Mat(string name,Color color,float smooth,float metal) {string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;}
}
