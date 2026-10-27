#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class PolishRailwayMenuLever
{
    const string Folder="Assets/TwilightMap/ForestHarbor/MenuLever/";
    static Transform Part(Transform p,string name,Mesh mesh,Vector3 pos,Material mat)
    {
        var t=p.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(p,false);}
        t.gameObject.layer=25;t.localPosition=pos;
        var filter=t.GetComponent<MeshFilter>();if(filter==null)filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        var renderer=t.GetComponent<MeshRenderer>();if(renderer==null)renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;
        return t;
    }
    static Transform Box(Transform p,string n,Vector3 pos,Vector3 size,float r,Material mat)=>Part(p,n,ToyBoxGeometry.RoundedBox(size,r),pos,mat);
    static Material Set(string name,Color color,float gloss,float metal=0)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat");m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",gloss);m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode only");
        ToyBoxGeometry.Initialize();
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);var lever=map.menuLever;var rig=lever.handle.parent;
        var wood=Set("Maple",new Color(.97f,.76f,.48f),.42f);
        wood.SetFloat("_GrainStrength",.42f);wood.SetTextureScale("_BaseMap",new Vector2(1.8f,1.8f));
        var navy=Set("NavyEnamel",new Color(.09f,.23f,.34f),.38f,.06f);
        var red=Set("RedHandle",new Color(.88f,.18f,.075f),.34f);
        red.SetTexture("_BaseMap",wood.GetTexture("_BaseMap"));red.SetFloat("_GrainStrength",.12f);red.SetFloat("_TextureTint",0);
        var brass=Set("Brass",new Color(.98f,.73f,.36f),.6f,.65f);
        var ivory=Set("Ivory",new Color(1,.94f,.77f),.3f);
        var rim=Part(rig,"Pivot brass rim",ToyBoxGeometry.Disc(.36f,.045f,.015f),new Vector3(0,1.12f,-.207f),brass);rim.localRotation=Quaternion.Euler(90,0,0);
        var face=Part(rig,"Pivot enamel inset",ToyBoxGeometry.Disc(.315f,.053f,.015f),new Vector3(0,1.12f,-.234f),navy);face.localRotation=Quaternion.Euler(90,0,0);
        rig.Find("Brass axle").localPosition=new Vector3(0,1.12f,-.10f);
        Box(rig,"Ivory inset edge",new Vector3(0,.68f,-.35f),new Vector3(.90f,.55f,.045f),.085f,brass);
        rig.Find("Front navy inset").localPosition=new Vector3(0,.68f,-.389f);
        foreach(Transform t in rig)if(t.name=="Raised menu bar")t.localPosition=new Vector3(t.localPosition.x,t.localPosition.y,-.437f);
        rig.Find("MENU lettering").gameObject.SetActive(false);
        lever.transform.Find("Instruction").gameObject.SetActive(false);
        var plaque=rig.Find("Wooden instruction plaque");if(plaque==null){plaque=new GameObject("Wooden instruction plaque").transform;plaque.SetParent(rig,false);}
        plaque.gameObject.layer=25;plaque.localPosition=new Vector3(-.10f,-.22f,-.98f);
        plaque.localRotation=lever.objectCamera.transform.localRotation;
        Box(plaque,"Rounded honey edge",Vector3.zero,new Vector3(2.58f,.67f,.20f),.12f,wood);
        Box(plaque,"Cream maple face",new Vector3(0,0,-.112f),new Vector3(2.42f,.51f,.065f),.09f,ivory);
        var title=Part(plaque,"Menu title",ToyBoxGeometry.Text("MENU",.025f),new Vector3(0,.085f,-.155f),navy);title.localScale=Vector3.one*.165f;
        var hint=Part(plaque,"Push left hint",ToyBoxGeometry.Text("PUSH LEFT",.022f),new Vector3(0,-.105f,-.155f),navy);hint.localScale=Vector3.one*.10f;
        var a=Box(plaque,"Arrow upper",new Vector3(-.91f,.055f,-.16f),new Vector3(.18f,.055f,.03f),.018f,navy);a.localRotation=Quaternion.Euler(0,0,40);
        var b=Box(plaque,"Arrow lower",new Vector3(-.91f,-.055f,-.16f),new Vector3(.18f,.055f,.03f),.018f,navy);b.localRotation=Quaternion.Euler(0,0,-40);
        Box(plaque,"Arrow stem",new Vector3(-.82f,0,-.16f),new Vector3(.24f,.05f,.03f),.018f,navy);
        var rect=(RectTransform)lever.transform;rect.sizeDelta=new Vector2(245,225);rect.anchoredPosition=new Vector2(285-836,470.5f-811);
        var cam=lever.objectCamera;cam.orthographicSize=1.80f*rig.lossyScale.x;cam.transform.localPosition=new Vector3(2.3f,3.35f,-7);cam.aspect=245f/225;
        lever.ResetLever();EditorUtility.SetDirty(lever);EditorUtility.SetDirty(map);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved polished colors, wood grain, brass pivot trim and solid rounded MENU / PUSH LEFT plaque.";
    }
}
#endif
