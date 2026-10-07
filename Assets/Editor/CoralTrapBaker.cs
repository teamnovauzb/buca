using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Saved coral trap-pocket artwork. Retains the authored damage trigger.</summary>
public static class CoralTrapBaker
{
    const string Folder="Assets/Art/CoralTrap";
    [MenuItem("RealBuca/Prebuild/Coral Trap Pockets")]
    public static void Bake()
    {
        foreach(var dir in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{dir}).Select(AssetDatabase.GUIDToAssetPath)) {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<DeadlyTrigger>(true).Any(p=>p.name.Contains("TrapPocket")))continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try {foreach(var p in root.GetComponentsInChildren<DeadlyTrigger>(true).Where(p=>p.name.Contains("TrapPocket")))Apply(p);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    public static void Apply(DeadlyTrigger pocket)
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art","CoralTrap");
        // The art is flattened; the gameplay trigger must still reach puck height.
        foreach(var c in pocket.GetComponents<Collider>())Object.DestroyImmediate(c);
        var trigger=pocket.gameObject.AddComponent<CapsuleCollider>();
        var scale=pocket.transform.lossyScale;
        trigger.isTrigger=true;trigger.direction=1;
        trigger.radius=.35f/Mathf.Max(.001f,Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)));
        trigger.height=1f/Mathf.Max(.001f,Mathf.Abs(scale.y));
        trigger.center=new Vector3(0,(.4f-pocket.transform.position.y)/Mathf.Max(.001f,scale.y),0);
        ToyBoxGeometry.Initialize();
        var coral=Mat("CoralEnamel",new Color(.83f,.18f,.105f),.48f);
        var burgundy=Mat("DarkBurgundy",new Color(.10f,.008f,.006f),.2f);
        var black=Mat("PocketDepth",new Color(.004f,.002f,.002f),.05f);
        var ivory=Mat("IvoryEmblem",new Color(1,.9f,.67f),.32f);
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/Materials/NaturalBirch.mat");
        var old=pocket.transform.Find("CoralTrapVisual");if(old!=null)Object.DestroyImmediate(old.gameObject);
        foreach(var r in pocket.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
        foreach(Transform sibling in pocket.transform.parent)if(sibling.name.Contains("TrapRing") && (sibling.position-pocket.transform.position).sqrMagnitude<.1f)foreach(var r in sibling.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
        var v=new GameObject("CoralTrapVisual").transform;v.SetParent(pocket.transform.parent,false);v.position=new Vector3(pocket.transform.position.x,0,pocket.transform.position.z);v.SetParent(pocket.transform,true);
        GameObject Part(string name,Vector3 pos,Mesh mesh,Material mat) {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(v,false);go.transform.localPosition=pos;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;}
        Part("MapleSurround",new Vector3(0,.018f,0),ToyBoxGeometry.Disc(.49f,.032f,.012f),wood);
        Part("DarkWell",new Vector3(0,.039f,0),ToyBoxGeometry.Disc(.354f,.012f,.004f),black);
        Part("InnerBurgundyLip",new Vector3(0,.052f,0),ToyBoxGeometry.Ring(.352f,.035f,.025f),burgundy);
        Part("RoundedCoralRim",new Vector3(0,.079f,0),ToyBoxGeometry.Torus(.40f,.072f),coral);
        var heart=Part("MinusHeart",new Vector3(0,.155f,-.40f),AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ToyBoxMenu/Meshes/DomedShotHeart.asset"),ivory);
        heart.transform.localRotation=Quaternion.Euler(90,0,0);heart.transform.localScale=new Vector3(.145f,.145f,.08f);
        Part("MinusMark",new Vector3(0,.174f,-.40f),ToyBoxGeometry.RoundedBox(new Vector3(.060f,.009f,.018f),.004f),coral);
    }
    static Material Mat(string name,Color color,float smooth) {string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");EditorUtility.SetDirty(m);return m;}
}
