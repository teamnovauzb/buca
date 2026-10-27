#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

// Read-only promotional sampling in isolated preview scenes. No saved scenes or preferences changed.
public static partial class BucaFeatureFilmCapture
{
 const string DefaultFolder="output/buca-attractor-2026/feature-film/capture";
 static string Folder=DefaultFolder;
 static int lastJob=3;static bool hideCaptureTickets;
 const int Width=2560,Height=1440,Fps=30;
 static readonly string[] Jobs={"skins","travel","upgrade","boards"};
 static readonly float[] Duration={8,14,22,6};
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static Scene scene;static GameObject root;static Camera camera;static RenderTexture target;static Texture2D pixels;
 static ToyPaintSkinSelector skins;static TwilightRailwayMap map;static AnimalTrainCarriages cars;static WorldTwoHarborReveal reveal;
 static Material mapMaterial;static GameObject board;static int boardIndex=-1,job,frame;static bool active,preview;
 static Vector3 cameraRest;static Quaternion cameraRotation;static int beforeSkin,beforeHighest,beforeCurrent;
 static readonly int[] Levels={1,7,12,18,24,30};
 public static string Start(bool samples=true,int firstJob=0,int finalJob=3,bool hideTickets=false,string outputFolder=null,bool animalPickups=false,bool clearMilestonePickups=false)
 {
  if(active)throw new Exception("Already capturing");if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
  beforeSkin=BucaSkinBinding.Selected;beforeHighest=PlayerPrefs.GetInt("BucaHighestLevel",-1);beforeCurrent=PlayerPrefs.GetInt("BucaCurrentLevel",-1);
  preview=samples;job=firstJob;frame=0;lastJob=finalJob;hideCaptureTickets=hideTickets;pickupJourney=animalPickups;stationPickups=clearMilestonePickups;Folder=outputFolder??DefaultFolder;Directory.CreateDirectory(Folder);active=true;Setup();
  AssemblyReloadEvents.beforeAssemblyReload+=Stop;EditorApplication.update+=Tick;return samples?"Sampling all four feature scenes":"Capturing skins, level travel, train upgrade and level layouts at 2560x1440";
 }
 static GameObject Spawn(string path){var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));SceneManager.MoveGameObjectToScene(g,scene);g.SetActive(true);return g;}
 static void Setup()
 {
  scene=EditorSceneManager.NewPreviewScene();target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);Directory.CreateDirectory(Folder+"/"+Jobs[job]);
  if(job==0)
  {
   root=Spawn("Assets/ToyBoxMenu/Prefabs/PaintWorkshopSkins.prefab");skins=root.GetComponent<ToyPaintSkinSelector>();camera=skins.viewCamera;skins.Fit(16f/9);skins.Focus(0);
  }
  else if(job==1)
  {
   var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);root=Object.Instantiate(original.gameObject);root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);
   map=root.GetComponent<TwilightRailwayMap>();map.eventAudio=map.motionAudio=null;map.Open(null);typeof(TwilightRailwayMap).GetField("highest",Flags).SetValue(map,29);
   cars=root.GetComponent<AnimalTrainCarriages>()??root.AddComponent<AnimalTrainCarriages>();cars.map=map;typeof(AnimalTrainCarriages).GetMethod("Start",Flags).Invoke(cars,null);
   if(pickupJourney)SetupPickups();
   camera=map.viewCamera;map.illustratedMenu.canvas.worldCamera=camera;map.illustratedMenu.ShowImmediately();
   var art=map.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>();mapMaterial=new Material(art.material);art.material=mapMaterial;root.SetActive(true);
  }
  else if(job==2)
  {
   root=Spawn(BuildWorldTwoHarborReveal.Path);reveal=root.GetComponent<WorldTwoHarborReveal>();var go=new GameObject("Feature capture camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(0,0,-10);camera.nearClipPlane=.01f;camera.farClipPlane=2000;camera.cullingMask=~(1<<28);
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
  }
  else
  {
   root=Spawn("Assets/ToyBoxMenu/Prefabs/FullSizeSkinPreview.prefab");camera=root.GetComponentInChildren<Camera>(true);
   var old=root.transform.Find("ActualBoardPreview");if(old!=null)old.gameObject.SetActive(false);
   foreach(var tr in root.GetComponentsInChildren<Transform>(true))if(tr.name=="SkinNamePlaque"||tr.name=="EQUIPPED  -  BOARD + PUCK"||tr.name=="ActualPlayerPuckPreview"||tr.name.EndsWith("Control")||tr.name.Contains("CLASSIC TOY BOX")||tr.name.Contains("MINT GARDEN")||tr.name.Contains("HONEY BEE")||tr.name.Contains("BERRY CREAM")||tr.name.Contains("OCEAN CLUB"))tr.gameObject.SetActive(false);
   foreach(var label in root.GetComponentsInChildren<TMPro.TMP_Text>(true))label.gameObject.SetActive(false);
   foreach(var label in root.GetComponentsInChildren<TextMesh>(true))label.gameObject.SetActive(false);
   foreach(var lamp in root.GetComponentsInChildren<Light>(true))lamp.enabled=false;
   var volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10000;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/ToyBoxMenu/Materials/PlayroomGrade.asset");
   Light key=new GameObject("Capture key").AddComponent<Light>();SceneManager.MoveGameObjectToScene(key.gameObject,scene);key.type=LightType.Directional;key.intensity=.65f;key.transform.rotation=Quaternion.Euler(50,-35,0);key.shadows=LightShadows.Soft;
   Light fill=new GameObject("Capture fill").AddComponent<Light>();SceneManager.MoveGameObjectToScene(fill.gameObject,scene);fill.type=LightType.Directional;fill.intensity=.22f;fill.transform.rotation=Quaternion.Euler(25,140,0);
   boardIndex=-1;
  }
  camera.targetTexture=target;camera.aspect=16f/9;camera.rect=new Rect(0,0,1,1);camera.scene=scene;camera.enabled=false;
  cameraRest=camera.transform.position;cameraRotation=camera.transform.rotation;
  foreach(var g in scene.GetRootGameObjects())foreach(var c in g.GetComponentsInChildren<Camera>(true)){c.scene=scene;c.enabled=false;}
  frame=0;
 }
 static void Sample(float t)
 {
  if(job==0)
  {
   skins.Focus(Mathf.Clamp(Mathf.FloorToInt(t/1.6f),0,4));float phase=t/8;
   camera.transform.position=cameraRest+root.transform.right*Mathf.Sin(phase*Mathf.PI*2)*.8f;camera.transform.rotation=cameraRotation;
   camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,1.7f,1.8f)));
  }
  else if(job==1)
  {
   float route=pickupJourney?PickupRoute(t):Mathf.Lerp(map.stops[0].distance,map.stops[29].distance,t/14f);int selected=0;while(selected<29&&map.stops[selected+1].distance<=route+.001f)selected++;
   foreach(string field in new[]{"selected","parked","destination"})typeof(TwilightRailwayMap).GetField(field,Flags).SetValue(map,selected);
   typeof(TwilightRailwayMap).GetField("distance",Flags).SetValue(map,route);typeof(TwilightRailwayMap).GetMethod("Pose",Flags).Invoke(map,null);typeof(AnimalTrainCarriages).GetMethod("LateUpdate",Flags).Invoke(cars,null);
   map.illustratedMenu.Refresh();map.illustratedMenu.PoseTrain();map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().Refresh();mapMaterial.SetFloat("_FlowTime",t);mapMaterial.SetFloat("_WorldReveal",0);
   if(pickupJourney)SamplePickups(t);
  }
  else if(job==2)
  {
   reveal.Sample(t);
   if(hideCaptureTickets)
   {
    reveal.trainUpgrade.ticket30.gameObject.SetActive(false);
    reveal.trainUpgrade.ticket31.gameObject.SetActive(false);
   }
  }
  else
  {
   int n=Mathf.Min(5,Mathf.FloorToInt(t));if(n!=boardIndex)
   {
    if(board!=null)Object.DestroyImmediate(board);board=Spawn("Assets/ToyBoxMenu/Levels/Level_"+Levels[n].ToString("D2")+".prefab");board.transform.SetParent(root.transform,false);
    foreach(var script in board.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
    var binding=board.GetComponent<BucaSkinBinding>();if(binding!=null)binding.Apply(n%5);boardIndex=n;
   }
   float u=t-Mathf.Floor(t);Vector3 pivot=root.transform.TransformPoint(new Vector3(0,-.2f,.25f));camera.transform.position=pivot+Quaternion.Euler(0,Mathf.Lerp(-8,8,u),0)*(cameraRest-pivot)*.94f;camera.transform.LookAt(pivot);
  }
  Canvas.ForceUpdateCanvases();foreach(var label in root.GetComponentsInChildren<TMPro.TMP_Text>(true))if(label.gameObject.activeInHierarchy)label.ForceMeshUpdate();
  // MoonPuff.Initialize detaches its render stage. Scan preview-scene roots, not just the UI hierarchy.
  foreach(var g in scene.GetRootGameObjects())foreach(var c in g.GetComponentsInChildren<Camera>(true))
   if(c!=camera&&c.targetTexture!=null){c.enabled=false;c.scene=scene;RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=c.targetTexture});}
  RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
 }
 static void Tick()
 {
  try
  {
   for(int b=0;b<2&&active;b++)
   {
    float t=preview?(pickupJourney&&job==1?PickupSampleTimes[frame]:Duration[job]*new[]{.05f,.25f,.5f,.73f,.95f}[frame]):frame/(float)Fps;Sample(t);
    var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply();RenderTexture.active=old;
    File.WriteAllBytes(Folder+"/"+Jobs[job]+"/"+(preview?"sample-":"")+frame.ToString("D4")+".jpg",pixels.EncodeToJPG(96));
    if(job==1||job==2)
    {
     var tr=job==1?map.illustratedMenu.locomotive:reveal.trainUpgrade.trainImage.rectTransform;
     var point=pickupJourney&&job==1?PickupFocus():camera.WorldToScreenPoint(tr.position);File.AppendAllText(Folder+"/"+Jobs[job]+(preview?"-samples":"")+".csv",frame+","+point.x.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+(Height-point.y).ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");
     if(pickupJourney&&job==1)RecordPickup(t);
    }
    frame++;if(frame%30==0||preview)File.WriteAllText(Folder+"/progress.txt",Jobs[job]+" "+frame);
    if(frame>=(preview?(pickupJourney&&job==1?PickupSampleTimes.Length:5):Mathf.CeilToInt((pickupJourney&&job==1?PickupDuration:Duration[job])*Fps)))
    {
     if(pickupJourney&&job==1)ValidatePickupPreferences();
     Cleanup();job++;if(job>lastJob){Stop();if(beforeSkin!=BucaSkinBinding.Selected||beforeHighest!=PlayerPrefs.GetInt("BucaHighestLevel",-1)||beforeCurrent!=PlayerPrefs.GetInt("BucaCurrentLevel",-1))throw new Exception("Preferences unexpectedly changed");File.WriteAllText(Folder+(preview?"/samples-done.txt":"/done.txt"),"PASS: requested feature captures, skin, level and animal preferences unchanged");}else Setup();
    }
   }
  }
  catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());Stop();Debug.LogException(e);}
 }
 static void Cleanup(){if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);if(target!=null){target.Release();Object.DestroyImmediate(target);}if(pixels!=null)Object.DestroyImmediate(pixels);if(mapMaterial!=null)Object.DestroyImmediate(mapMaterial);CleanupPickups();root=null;camera=null;target=null;pixels=null;skins=null;map=null;cars=null;reveal=null;mapMaterial=null;board=null;}
 public static void Stop(){EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Stop;if(active){active=false;Cleanup();}}
}
#endif
