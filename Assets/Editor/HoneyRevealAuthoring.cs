#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HoneyRevealAuthoring
{
    static Material M(string n,Color c){string path="Assets/Materials/TrainLeaderboard3D/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Smoothness",.35f);EditorUtility.SetDirty(m);return m;}
    static GameObject P(Transform root,string n,Mesh mesh,Material m,Vector3 p,int layer=29)
    {var t=root.Find(n);var g=t?t.gameObject:new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.layer=layer;g.transform.localPosition=p;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=m;g.SetActive(true);return g;}
    static GameObject Box(Transform r,string n,Vector3 p,Vector3 size,Material m,int l=29){return P(r,n,ToyBoxGeometry.RoundedBox(size,.06f),m,p,l);}
    public static string Apply()
    {
        HoneyMiniatureAuthoring.Apply();var b=Object.FindFirstObjectByType<HoneyTutorialBoard>();var stage=b.stage.transform;var board=stage.Find("MiniatureBoard");
        var amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TrainLeaderboard3D/HoneyDipperCoating.mat");var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TrainLeaderboard3D/CreamMaple.mat");
        foreach(Transform t in board)if(t.name.StartsWith("Dipper")||t.name.StartsWith("HoneyDipper")||t.name=="HoneyHangingDrip"||t.name=="HoneyDripBead")t.gameObject.SetActive(false);
        var pour=stage.Find("PouringDipper");if(!pour){pour=new GameObject("PouringDipper").transform;pour.SetParent(stage,false);}b.pouringDipper=pour;
        var handle=P(pour,"Handle",ToyBoxGeometry.Disc(.075f,1.5f,.035f),wood,new Vector3(.85f,.8f,-2.0f),30);handle.transform.localRotation=Quaternion.Euler(0,0,-65);
        for(int i=0;i<6;i++){var ring=P(pour,"Head"+i,ToyBoxGeometry.Disc(.24f,.075f,.025f),wood,new Vector3(-.25f+i*.09f,.29f+i*.043f,-2),30);ring.transform.localRotation=Quaternion.Euler(0,0,-65);}
        var strand=P(pour,"HoneyStream",ToyBoxGeometry.Disc(.045f,1.32f,.02f),amber,new Vector3(0,.4f,-1.26f),30);strand.transform.localRotation=Quaternion.Euler(90,0,0);b.pourStream=strand.transform;
        for(int i=0;i<3;i++){var swirl=P(board,"HoneySwirl"+i,ToyBoxGeometry.Torus(.1f+i*.1f,.023f),amber,new Vector3(0,.4f,-.615f),30);swirl.transform.localRotation=Quaternion.Euler(90,0,0);swirl.SetActive(false);}
        b.honeyVisual=board.Find("Liquid");
        // Keep the board's surface and physical controls, replacing the oversized header with a small sign.
        var plaque=board.Find("CaptionPlaque");plaque.localPosition=new Vector3(0,2.7f,-.45f);plaque.localScale=new Vector3(.65f,1,1);
        b.caption.transform.localPosition=new Vector3(0,2.7f,-.56f);b.caption.fontSize=2.65f;b.caption.text="STICKY HONEY";b.caption.rectTransform.anchoredPosition3D=new Vector3(0,2.7f,-.56f);b.caption.ForceMeshUpdate(true);
        var room=b.transform.Find("HoneyPlayroom");if(!room){room=new GameObject("HoneyPlayroom").transform;room.SetParent(b.transform,false);}room.position=stage.position+Vector3.right*30;b.roomStage=room.gameObject;
        var cream=M("RevealWall",new Color(.76f,.65f,.47f));var oak=M("RevealOak",new Color(.48f,.25f,.095f));var trim=M("RevealTrim",new Color(.96f,.83f,.60f));var green=M("RevealLeaves",new Color(.12f,.32f,.13f));var blue=M("RevealBookBlue",new Color(.08f,.30f,.36f));var red=M("RevealBookRust",new Color(.62f,.20f,.095f));var glass=M("RevealSky",new Color(.66f,.83f,.81f));
        Box(room,"Wall",new Vector3(0,0,5),new Vector3(18,10,.25f),cream);
        Box(room,"Floor",new Vector3(0,-3.25f,0),new Vector3(20,.2f,14),wood);
        Box(room,"Skirting",new Vector3(0,-3.0f,4.6f),new Vector3(18,.25f,.2f),trim);
        Box(room,"WindowLight",new Vector3(-4,1.3f,4.7f),new Vector3(3.4f,3.9f,.15f),glass);
        for(int i=0;i<3;i++)Box(room,"WindowVertical"+i,new Vector3(-5.7f+i*1.7f,1.3f,4.5f),new Vector3(.13f,4,.23f),trim);
        for(int i=0;i<3;i++)Box(room,"WindowHorizontal"+i,new Vector3(-4,-.65f+i*1.95f,4.5f),new Vector3(3.5f,.13f,.23f),trim);
        Box(room,"WindowSill",new Vector3(-4,-.8f,4.1f),new Vector3(3.9f,.2f,.8f),wood);
        for(int i=0;i<4;i++)Box(room,"Shelf"+i,new Vector3(4.5f,-2.6f+i*1.55f,3.7f),new Vector3(3.1f,.18f,1.1f),oak);
        for(int i=0;i<2;i++)Box(room,"ShelfSide"+i,new Vector3(3+i*3,0,3.7f),new Vector3(.18f,5.4f,1.1f),oak);
        for(int row=0;row<3;row++)for(int i=0;i<7;i++){float h=.75f+(i%3)*.12f;Box(room,"Book"+row+"_"+i,new Vector3(3.35f+i*.36f,-2.42f+row*1.55f+h/2,3.5f),new Vector3(.23f,h,.6f),i%3==0?blue:i%3==1?red:trim);}
        Box(room,"LeftTable",new Vector3(-4.5f,-1.8f,2.6f),new Vector3(3.6f,.2f,2),wood);
        for(int i=0;i<2;i++)Box(room,"TableLeg"+i,new Vector3(-5.8f+i*2.6f,-2.5f,2.6f),new Vector3(.18f,1.4f,.2f),oak);
        var temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
        var pot=P(room,"PlantPot",ToyBoxGeometry.Disc(.38f,.6f,.1f),trim,new Vector3(-4.7f,-1.42f,2.5f));
        for(int i=0;i<9;i++){float a=i*2.4f;var leaf=P(room,"Leaf"+i,sphere,green,new Vector3(-4.7f+Mathf.Sin(a)*.36f,-.75f+(i%3)*.2f,2.5f+Mathf.Cos(a)*.3f));leaf.transform.localScale=new Vector3(.28f,.72f,.14f);leaf.transform.localRotation=Quaternion.Euler(15,i*30,Mathf.Sin(a)*40);}
        var cameraT=room.Find("Camera");if(!cameraT){cameraT=new GameObject("Camera",typeof(Camera)).transform;cameraT.SetParent(room,false);}cameraT.localPosition=new Vector3(0,.0f,-12);cameraT.localRotation=Quaternion.Euler(5,0,0);var camera=cameraT.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3.6f;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.76f,.65f,.47f);
        string rtPath="Assets/Materials/TrainLeaderboard3D/HoneyRoom.renderTexture";var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);if(!rt){rt=new RenderTexture(1920,1080,24);AssetDatabase.CreateAsset(rt,rtPath);}camera.targetTexture=rt;b.roomTexture=rt;
        var lamp=room.Find("WarmLight");if(!lamp){lamp=new GameObject("WarmLight",typeof(Light)).transform;lamp.SetParent(room,false);}lamp.localPosition=new Vector3(-2,3,-3);var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.87f,.68f);light.range=22;light.intensity=7;light.cullingMask=1<<29;
        room.gameObject.SetActive(false);b.backdrop.color=Color.white;b.card.sizeDelta=new Vector2(1450,755);
        EditorUtility.SetDirty(b);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);return "Honey reveal and separate 3D playroom saved";
    }
}
#endif
