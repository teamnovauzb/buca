#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HoneyMiniTableAuthoring
{
 static Material Mat(string name,Color c,float smooth,Material source=null){string p="Assets/Materials/TrainLeaderboard3D/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=source?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.color=c;m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
 static GameObject Part(Transform root,string n,Mesh mesh,Material mat,Vector3 p,int layer=30){var t=root.Find(n);var g=t?t.gameObject:new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.layer=layer;g.transform.localPosition=p;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=mat;g.SetActive(true);return g;}
 static GameObject Box(Transform root,string n,Vector3 p,Vector3 size,Material mat,int layer=30)=>Part(root,n,ToyBoxGeometry.RoundedBox(size,Mathf.Min(.22f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.45f)),mat,p,layer);
 public static string Apply(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit play first");
 var existing=Object.FindFirstObjectByType<HoneyTutorialBoard>(); if(existing.pouringDipper)HoneyReferencePolish.Apply();ToyBoxGeometry.Initialize();var b=Object.FindFirstObjectByType<HoneyTutorialBoard>();var stage=b.stage.transform;var root=stage.Find("MiniatureBoard");
 var maple=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TrainLeaderboard3D/CreamMaple.mat");var oak=Mat("MiniTableWarmMaple",new Color(.83f,.59f,.34f),.32f,maple);var cream=Mat("MiniTableCream",new Color(.98f,.90f,.73f),.28f);var ink=Mat("MiniTableInk",new Color(.045f,.14f,.15f),.3f);var fabric=Mat("MiniTableRug",new Color(.82f,.75f,.61f),.05f);var coral=Mat("MiniTableCoral",new Color(.91f,.30f,.19f),.42f);
 b.dim.color=new Color(.22f,.42f,.43f);b.dim.SetFloat("_Smoothness",.3f);EditorUtility.SetDirty(b.dim);
 var shell=root.Find("SculptedTray").GetComponent<MeshFilter>();var mesh=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ToyBoxMenu/Meshes/HoneyRoundedTray.asset"));var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i].z=v[i].z*.5f-.32f;mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();shell.sharedMesh=ToyBoxGeometry.Save(mesh,"Assets/ToyBoxMenu/Meshes/MiniTableSlimRim.asset");
 Box(root,"MapleApron",new Vector3(0,0,.14f),new Vector3(9.2f,4.18f,.67f),oak);
 for(int i=0;i<4;i++){var leg=Part(root,"TableFoot"+i,ToyBoxGeometry.Disc(.29f,.9f,.12f),oak,new Vector3(i%2==0?-3.65f:3.65f,i<2?-1.4f:1.4f,.91f));leg.transform.localRotation=Quaternion.Euler(90,i%2==0?-10:10,0);}
 root.Find("WoodBrandBadge").gameObject.SetActive(false);var brand=root.Find("CarvedBrand");brand.localPosition=new Vector3(-2.9f,-2.145f,.03f);brand.localScale=Vector3.one*.22f;brand.GetComponent<Renderer>().sharedMaterial=ink;
 root.Find("CaptionPlaque").gameObject.SetActive(false);b.caption.gameObject.SetActive(false);
 if(b.pouringDipper)b.pouringDipper.gameObject.SetActive(false);b.pouringDipper=null;b.pourStream=null;
 b.shootButton.localPosition=new Vector3(.7f,-2.24f,.10f);b.shootButton.localRotation=Quaternion.identity;b.shootButton.localScale=Vector3.one*.9f;b.shootButton.GetComponent<MeshFilter>().sharedMesh=ToyBoxGeometry.Disc(.44f,.20f,.065f);b.shootButton.GetComponent<Renderer>().sharedMaterial=coral;
 Part(root,"ButtonWoodBezel",ToyBoxGeometry.Disc(.54f,.13f,.055f),oak,new Vector3(.7f,-2.16f,.1f));
 var shoot=Part(root,"ShootWord",ToyBoxGeometry.Text("SHOOT",.006f),ink,new Vector3(.7f,-2.35f,.08f));shoot.transform.localRotation=Quaternion.Euler(-90,0,0);shoot.transform.localScale=Vector3.one*.11f;
 Box(root,"InstructionBadge",new Vector3(2.8f,-2.145f,.1f),new Vector3(2.6f,.1f,.42f),cream);
 var txt=Part(root,"InstructionWord",ToyBoxGeometry.Text("HOLD - RELEASE",.006f),ink,new Vector3(2.8f,-2.21f,.08f));txt.transform.localRotation=Quaternion.Euler(-90,0,0);txt.transform.localScale=Vector3.one*.105f;
 for(int i=0;i<b.chargeDots.Length;i++){float a=Mathf.Lerp(10,170,i/11f)*Mathf.Deg2Rad;b.chargeDots[i].transform.localPosition=new Vector3(.7f+Mathf.Cos(a)*.57f,-2.235f,.1f-Mathf.Sin(a)*.38f);b.chargeDots[i].transform.localScale=Vector3.one*.4f;}
 b.buttonPressDirection=Vector3.up;
 var liquid=b.honeyVisual;liquid.localScale=new Vector3(1,1,.3f);b.honeyDepth=.3f;
 var amber=liquid.GetComponent<Renderer>().sharedMaterial;amber.SetColor("_BaseColor",new Color(1,.54f,.04f,.57f));amber.SetColor("_EmissionColor",new Color(.12f,.045f,0));amber.SetFloat("_Smoothness",.98f);EditorUtility.SetDirty(amber);
 for(int i=0;i<3;i++){var ripple=Part(liquid,"SurfaceRipple"+i,ToyBoxGeometry.Torus(.35f+i*.3f,.012f),amber,new Vector3(.15f,0,-.16f));ripple.transform.localRotation=Quaternion.Euler(90,0,0);ripple.transform.localScale=new Vector3(1.3f,.32f,.65f);ripple.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 root.Find("CupBottom").localPosition=new Vector3(3.1f,0,-.27f);
 var front=stage.Find("MiniTableSoftbox");if(!front){front=new GameObject("MiniTableSoftbox",typeof(Light)).transform;front.SetParent(stage,false);}front.localPosition=new Vector3(-3,-8,-5);front.LookAt(stage.position);var light=front.GetComponent<Light>();light.type=LightType.Directional;light.intensity=.32f;light.color=new Color(1,.97f,.91f);light.cullingMask=1<<30;light.shadows=LightShadows.None;
 var rug=Part(root,"SoftOvalRug",ToyBoxGeometry.Disc(1,.06f,.02f),fabric,new Vector3(0,0,1.47f));rug.transform.localRotation=Quaternion.Euler(90,0,0);rug.transform.localScale=new Vector3(5.6f,1,3.0f);
 for(int i=0;i<10;i++){var braid=Part(root,"RugBraid"+i,ToyBoxGeometry.Torus(1,.007f),i%3==0?b.dim:fabric,new Vector3(0,0,1.425f));braid.transform.localRotation=Quaternion.Euler(90,0,0);braid.transform.localScale=new Vector3(5.5f-i*.04f,1,2.95f-i*.04f);braid.gameObject.SetActive(i<3);}
 var camera=stage.GetComponentInChildren<Camera>(true);camera.transform.localPosition=new Vector3(.9f,-13,-10);camera.transform.LookAt(stage.TransformPoint(new Vector3(0,-.05f,.25f)));camera.orthographicSize=3.8f;
 var rt=camera.targetTexture;if(rt){rt.Release();rt.width=2560;rt.height=1440;rt.antiAliasing=4;rt.Create();EditorUtility.SetDirty(rt);}
 b.card.sizeDelta=new Vector2(1700,950);
 foreach(var line in b.aimGuide.GetComponentsInChildren<LineRenderer>(true))line.enabled=false;
 var sphereTemp=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=sphereTemp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(sphereTemp);
 for(int i=0;i<20;i++){var dot=Part(b.aimGuide.transform,"AimDot"+i,sphere,cream,new Vector3(-2.7f+i*.285f,0,-.57f));dot.transform.localScale=Vector3.one*.045f;}
 // The room is modeled, with toys and soft furnishings behind the demonstration.
 var room=b.roomStage.transform;var blue=Mat("MiniRoomTeal",new Color(.26f,.46f,.47f),.25f);var rust=Mat("MiniRoomCoral",new Color(.78f,.34f,.20f),.3f);
 for(int i=0;i<5;i++){var cube=Box(room,"ToyBlock"+i,new Vector3(-1.8f+i*.55f,-2.8f,2.2f+(i%2)*.4f),Vector3.one*(.4f+(i%2)*.13f),i%2==0?blue:oak,29);cube.transform.localRotation=Quaternion.Euler(0,i*19,0);}
 Box(room,"ToyWagon",new Vector3(2.1f,-2.65f,1.8f),new Vector3(1.35f,.45f,.7f),oak,29);
 for(int i=0;i<2;i++){var wheel=Part(room,"WagonWheel"+i,ToyBoxGeometry.Disc(.23f,.13f,.035f),blue,new Vector3(1.65f+i*.9f,-2.95f,1.36f),29);wheel.transform.localRotation=Quaternion.Euler(90,0,0);}
 var floorRug=Part(room,"PlayroomRug",ToyBoxGeometry.Disc(1,.05f,.015f),fabric,new Vector3(0,-3.10f,1),29);floorRug.transform.localScale=new Vector3(6.8f,1,4.5f);
 var source=b.GetComponent<AudioSource>();if(!source)source=b.gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.7f;b.tutorialAudio=source;
 b.voice=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Tutorial/HoneyGuide.wav");b.enterSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/PanelOpen.ogg");b.clickSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ButtonClick.ogg");b.launchSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Player/PuckLaunch.ogg");b.winSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Player/HoleSink.ogg");
 EditorUtility.SetDirty(b);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);return "Mini Play Table, 3D room and tutorial audio saved";
 }
}
#endif
