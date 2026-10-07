#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
public static class HoneyTutorialAuthoring
{
    static GameObject Part(Transform p,string n,Mesh mesh,Material mat,Vector3 pos)
    {var t=p.Find(n);var g=t!=null?t.gameObject:new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(p,false);g.layer=30;g.transform.localPosition=pos;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;}
    static Material Mat(string n,Color c)
    {string path="Assets/Materials/TrainLeaderboard3D/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Smoothness",.55f);EditorUtility.SetDirty(m);return m;}
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode only");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var intro=Object.FindFirstObjectByType<ObstacleIntroController>(FindObjectsInactive.Include);
        if(intro.honeyBoard!=null)return "Already authored";
        ToyBoxGeometry.Initialize();
        var root=new GameObject("HoneyTutorial",typeof(HoneyTutorialBoard));var b=root.GetComponent<HoneyTutorialBoard>();
        var canvasGO=new GameObject("TutorialCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));canvasGO.transform.SetParent(root.transform,false);
        var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=400;
        var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        b.group=canvasGO.GetComponent<CanvasGroup>();b.group.alpha=0;b.group.blocksRaycasts=false;
        var bg=new GameObject("Blur",typeof(RectTransform),typeof(RawImage));bg.transform.SetParent(canvasGO.transform,false);b.backdrop=bg.GetComponent<RawImage>();var br=(RectTransform)bg.transform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;b.backdrop.color=new Color(.8f,.8f,.8f,1);
        var card=new GameObject("AnimatedBoard",typeof(RectTransform),typeof(RawImage));card.transform.SetParent(canvasGO.transform,false);b.card=(RectTransform)card.transform;b.card.sizeDelta=new Vector2(1400,730);
        var rt=new RenderTexture(1920,1000,24);rt.antiAliasing=2;AssetDatabase.CreateAsset(rt,"Assets/Materials/TrainLeaderboard3D/HoneyDemo.renderTexture");card.GetComponent<RawImage>().texture=rt;
        b.stage=new GameObject("HoneyDemoStage");b.stage.transform.SetParent(root.transform,false);b.stage.transform.position=new Vector3(0,4500,0);var stage=b.stage.transform;
        var maple=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TrainLeaderboard3D/CreamMaple.mat");var teal=Mat("HoneyDemoTeal",new Color(.07f,.34f,.33f));var honeyMat=Mat("HoneyDemoAmber",new Color(1,.53f,.045f));var gold=Mat("HoneyDemoGold",new Color(1,.73f,.15f));var navy=Mat("HoneyDemoNavy",new Color(.01f,.06f,.13f));
        b.lit=gold;b.dim=teal;
        Part(stage,"Frame",ToyBoxGeometry.RoundedBox(new Vector3(10.8f,5.3f,.5f),.3f),teal,Vector3.zero);
        Part(stage,"Maple",ToyBoxGeometry.RoundedBox(new Vector3(10.5f,5,.3f),.25f),maple,new Vector3(0,0,-.3f));
        for(int row=-1;row<=1;row++)for(int col=-1;col<=1;col++)
        {var cell=Part(stage,"HoneyCell"+row+col,ToyBoxGeometry.Disc(.38f,.09f,.025f),honeyMat,new Vector3(col*.59f+(row%2)*.22f,-.4f+row*.52f,-.5f));cell.transform.localRotation=Quaternion.Euler(90,0,0);}
        var trigger=new GameObject("HoneyPhysics",typeof(BoxCollider),typeof(IcePatch));trigger.transform.SetParent(stage,false);trigger.transform.localPosition=new Vector3(0,-.4f,-.65f);var box=trigger.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(1.6f,2,1);b.honey=trigger.GetComponent<IcePatch>();b.honey.patchDamping=6;
        var puck=Part(stage,"Puck",ToyBoxGeometry.Disc(.35f,.18f,.05f),gold,new Vector3(-3.8f,-.4f,-.65f));puck.transform.localRotation=Quaternion.Euler(90,0,0);var sphere=puck.AddComponent<SphereCollider>();sphere.radius=.35f;b.demoPuck=puck.AddComponent<Rigidbody>();b.demoPuck.useGravity=false;b.demoPuck.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;b.demoPuck.isKinematic=true;puck.AddComponent<TutorialPracticePuck>();
        var goal=Part(stage,"Goal",ToyBoxGeometry.Disc(.48f,.07f,.02f),navy,new Vector3(3.7f,-.4f,-.5f));goal.transform.localRotation=Quaternion.Euler(90,0,0);
        var rim=Part(stage,"GoalRim",ToyBoxGeometry.Torus(.48f,.065f),teal,new Vector3(3.7f,-.4f,-.57f));rim.transform.localRotation=Quaternion.Euler(90,0,0);
        b.chargeDots=new Renderer[3];for(int i=0;i<3;i++){var dot=Part(stage,"Charge"+i,ToyBoxGeometry.Disc(.13f,.06f,.02f),teal,new Vector3(-.42f+i*.42f,-1.9f,-.55f));dot.transform.localRotation=Quaternion.Euler(90,0,0);b.chargeDots[i]=dot.GetComponent<Renderer>();}
        var text=new GameObject("Caption",typeof(TextMeshPro));text.layer=30;text.transform.SetParent(stage,false);text.transform.localPosition=new Vector3(0,1.55f,-.55f);b.caption=text.GetComponent<TextMeshPro>();b.caption.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Roboto-Bold SDF.asset");b.caption.fontSize=5;b.caption.color=new Color(.025f,.08f,.14f);b.caption.alignment=TextAlignmentOptions.Center;b.caption.rectTransform.sizeDelta=new Vector2(9,1);b.caption.text="HONEY SLOWS YOU";
        var cam=new GameObject("DemoCamera",typeof(Camera));cam.transform.SetParent(stage,false);cam.transform.localPosition=new Vector3(0,0,-15);var c=cam.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=2.95f;c.cullingMask=1<<30;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.clear;c.targetTexture=rt;
        var lamp=new GameObject("SoftLight",typeof(Light));lamp.transform.SetParent(stage,false);lamp.transform.localPosition=new Vector3(-3,4,-5);var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.range=20;light.intensity=4;light.cullingMask=1<<30;
        b.stage.SetActive(false);intro.honeyBoard=b;EditorUtility.SetDirty(intro);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Honey board authored and linked to Level 7 intro";
    }
}
#endif
