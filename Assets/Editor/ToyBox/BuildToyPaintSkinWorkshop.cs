using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
public static partial class BuildToyBoxMainMenu
{
 [MenuItem("RealBuca/Toy Box 3D/14 - Generate Paint Workshop Skins")]
 public static void GeneratePaintWorkshop()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  PrepareFolders();ToyBoxGeometry.Initialize();LoadWorkshopMaterials();
  surface=ExistingWorkshopMaterial("MaplePlayingSurface");glow=ExistingWorkshopMaterial("MintLight");
  rails=new[]{blue,ExistingWorkshopMaterial("SkinSage"),yellow,ExistingWorkshopMaterial("SkinPlum"),ExistingWorkshopMaterial("SkinOcean")};
  pucks=new[]{coral,ExistingWorkshopMaterial("SkinDeepTeal"),ExistingWorkshopMaterial("SkinCocoa"),ExistingWorkshopMaterial("SkinRaspberry"),yellow};
  accents=new[]{cream,cream,yellow,cream,ink};
  var root=PrefabUtility.LoadPrefabContents(PrefabPath);
  try {var menu=root.GetComponent<ToyBoxMenuController>();UnityEngine.Object.DestroyImmediate(menu.skinsRoot);BuildPaintWorkshop(root.transform,menu);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}
  finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();RenderPaintWorkshop();
 }
 static void BuildPaintWorkshop(Transform root,ToyBoxMenuController menu)
 {
  var woodOriginal=wood;var surfaceOriginal=surface;var inkOriginal=ink;var glowOriginal=glow;var pucksOriginal=pucks;
  ink=Material("ArcadeNavyInk",new Color(.012f,.032f,.065f),.35f);
  wood=Material("ArcadeSatinWhite",new Color(.48f,.52f,.58f),.38f);
  surface=Material("PaintWorkshopMaple",new Color(.60f,.55f,.47f),.30f,WorkshopTexture("WorkshopMaplePhoto"));
  var normal=ToyBoxSurfaceBaker.WoodGrain(true);surface.SetTexture("_BumpMap",normal);surface.SetFloat("_BumpScale",.12f);surface.EnableKeyword("_NORMALMAP");
  var brass=Material("ArcadeBrushedSilver",new Color(.68f,.74f,.81f),.76f);brass.SetFloat("_Metallic",.78f);
  var paper=Material("ArcadeIvoryLabels",new Color(.71f,.74f,.77f),.38f);
  glow=Material("ArcadeCyanGlow",new Color(.025f,.68f,.85f),.65f);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.015f,.8f,1f)*2.4f);
  var railOriginal=rails;rails=(Material[])rails.Clone();pucks=(Material[])pucks.Clone();
  Color[] enamel={new Color(.018f,.24f,.64f),new Color(.18f,.61f,.48f),new Color(.95f,.59f,.035f),new Color(.65f,.035f,.23f),new Color(.012f,.47f,.59f)};
  for(int i=0;i<5;i++){
   rails[i]=Material("ArcadeEnamel"+i,enamel[i],.30f);
   // Keep each enamel color intact; studio reflections otherwise wash the bevels white.
   rails[i].SetFloat("_Metallic",0f);
   rails[i].SetFloat("_SpecularHighlights",0f);rails[i].EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
   rails[i].SetFloat("_EnvironmentReflections",0f);rails[i].EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
   var puckPaint=new Material(pucksOriginal[i]);puckPaint.name="ArcadePuck"+i;puckPaint.SetTexture("_BaseMap",null);puckPaint.SetTexture("_BumpMap",null);puckPaint.DisableKeyword("_NORMALMAP");puckPaint.SetFloat("_Smoothness",.78f);puckPaint.SetFloat("_Metallic",.18f);pucks[i]=ToyBoxGeometry.Save(puckPaint,Root+"/Materials/ArcadePuck"+i+".mat");
  }
  var room=Group("PaintWorkshop",root);menu.skinsRoot=room.gameObject;
  var selector=room.gameObject.AddComponent<ToyPaintSkinSelector>();menu.paintWorkshop=selector;selector.menu=menu;
  selector.buttons=new ToyBoxMenuController.PuckButton[7];selector.potRings=new GameObject[5];
  Box("SolidWorkbench",room,new Vector3(0,-.4f,0),new Vector3(36,.7f,23),wood,.18f);
  BuildWorkshopBackdrop(room,wood,paper,brass);
  Box("HangingSign",room,new Vector3(0,5.25f,6.4f),new Vector3(10.4f,1.2f,.38f),paper,.16f);
  Text("PICK YOUR COLORS",room,new Vector3(0,5.25f,6.18f),.48f,ink,false,.17f);
  for(int side=-1;side<=1;side+=2){var hook=MeshObject("HangingBrassLoop",room,new Vector3(side*4.1f,5.95f,6.3f),ToyBoxGeometry.Torus(.15f,.035f),brass);hook.transform.localRotation=Quaternion.Euler(90,0,0);
   for(int n=0;n<7;n++){float a=n*2.4f;var drop=Disc("SignPaintSplash",room,new Vector3(side*4.7f+Mathf.Cos(a)*.19f,5.25f+Mathf.Sin(a)*.25f,6.19f),n==0?.19f:.075f,.015f,side<0?blue:coral,.006f);drop.transform.localRotation=Quaternion.Euler(90,0,0);}}

  var board=Group("FullBoardPreview",room,new Vector3(0,1.40f,3.5f));var surfaces=new List<BucaSkinBinding.Surface>();
  MeshObject("SolidMapleWithHole",board,Vector3.zero,ToyBoxGeometry.WorkshopDeck(),surface);
  Bind(surfaces,MeshObject("ContinuousRoundedCabinet",board,Vector3.zero,ToyBoxGeometry.WorkshopRoundedFrame(),rails[0]).GetComponent<Renderer>(),rails);
  var navyBase=Material("RoundedDisplayNavy",new Color(.016f,.035f,.105f),.25f);
  navyBase.SetFloat("_SpecularHighlights",0f);navyBase.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
  MeshObject("LayeredNavyBase",board,Vector3.zero,ToyBoxGeometry.WorkshopDisplayBase(),navyBase);
  var rubberFoot=Material("RoundedDisplayFeet",new Color(.025f,.035f,.045f),.22f);
  for(int side=-1;side<=1;side+=2){
   var foot=Disc("LowDisplayFoot",board,new Vector3(side*5.65f,-1.08f,-.75f),.84f,.74f,rubberFoot,.18f);foot.transform.localScale=new Vector3(1.35f,1,1);
   Disc("SkinFootAccent",board,new Vector3(side*5.65f,-.67f,-.75f),.80f,.12f,navyBase,.045f).transform.localScale=new Vector3(1.35f,1,1);
  }
  var dark=Material("WorkshopRecess",new Color(.025f,.02f,.015f),.12f);
  Disc("SingleRecessedGoal",board,new Vector3(0,-.095f,.7f),.56f,.025f,dark,.002f);
  Text("BUCA",board,new Vector3(0,.135f,-1.25f),.42f,ink,true);
  selector.board=board.gameObject.AddComponent<BucaSkinBinding>();selector.board.surfaces=surfaces.ToArray();
  var puck=Disc("PreviewPuck",board,new Vector3(3.5f,.33f,-.6f),.62f,.38f,pucks[0],.085f);
  var motif=MeshObject("PreviewInlay",puck.transform,new Vector3(0,.20f,0),PuckMotif(0,.62f),accents[0]);
  selector.puck=puck.AddComponent<BucaSkinBinding>();selector.puck.surfaces=new[]{new BucaSkinBinding.Surface{target=puck.GetComponent<Renderer>(),variants=pucks},new BucaSkinBinding.Surface{target=motif.GetComponent<Renderer>(),variants=accents}};selector.puck.motif=motif.GetComponent<MeshFilter>();selector.puck.motifMeshes=new Mesh[5];
  string[] names={"CLASSIC","MINT","HONEY","BERRY","OCEAN"},second={"TOY BOX","GARDEN","BEE","CREAM","CLUB"};
  for(int i=0;i<5;i++){
   selector.puck.motifMeshes[i]=PuckMotif(i,.62f);
   var pot=Group(SkinNames[i]+" PaintPot",room,new Vector3((i-2)*3.45f,.97f,-2.5f));
   MeshObject("EnamelDisplayCan",pot,Vector3.zero,ToyBoxGeometry.WorkshopCanBody(),rails[i]);Disc("EnamelLid",pot,new Vector3(0,.86f,0),1.40f,.16f,rails[i],.055f);
   Ring("MetalFoot",pot,new Vector3(0,-.82f,0),1.40f,.10f,brass,.075f);
   Disc("SilverBaseBand",pot,new Vector3(0,-.72f,0),1.40f,.20f,brass,.035f);
   Ring("LidRim",pot,new Vector3(0,.90f,0),1.32f,.045f,brass,.035f);
   Ring("CanFootRim",pot,new Vector3(0,-.78f,0),1.39f,.045f,brass,.075f);

   Box("NamePlaque",pot,new Vector3(0,.02f,-1.45f),new Vector3(2.10f,.77f,.10f),paper,.10f);
   Text(names[i],pot,new Vector3(0,.16f,-1.51f),.23f,ink);Text(second[i],pot,new Vector3(0,-.12f,-1.51f),.23f,ink);
   var sample=Group("TiltedRailSample",pot,new Vector3(0,1.28f,.59f));sample.localRotation=Quaternion.Euler(0,-7,-5);
   Box("RailSample",sample,Vector3.zero,new Vector3(2.15f,.62f,.38f),rails[i],.12f);
   AddWorkshopSkinEmblem(pot,i,rails[i],brass);
   for(int s=-1;s<=1;s+=2){var screw=Disc("BrassScrew",sample,new Vector3(s*.89f,0,-.205f),.055f,.025f,cream,.01f);screw.transform.localRotation=Quaternion.Euler(90,0,0);}
   for(int side=-1;side<=1;side+=2){var rivet=Disc("LabelRivet",pot,new Vector3(side*.91f,.27f,-1.51f),.04f,.03f,brass,.008f);rivet.transform.localRotation=Quaternion.Euler(90,0,0);}
   var b=Button(pot,"",new Vector3(0,.83f,-.2f),.56f,pucks[i],.2f,false);selector.buttons[i]=b;
   var hitbox=(BoxCollider)b.hit;hitbox.size=new Vector3(2.7f,2.0f,2.7f);hitbox.center=new Vector3(0,-.7f,.2f);
   MeshObject("PuckPattern",b.cap,new Vector3(0,.15f,0),PuckMotif(i,.56f),accents[i]);
   foreach(Transform part in pot)if(part.name=="NamePlaque"||part.name==names[i]||part.name==second[i]||part.name=="LabelRivet")CurveWorkshopLabel(part,pot,i);
   pot.localRotation=Quaternion.Euler(0,new[]{-4f,3f,-2f,3f,5f}[i],0);
   selector.potRings[i]=MeshObject("SelectedPotHalo",pot,new Vector3(0,.98f,0),ToyBoxGeometry.Torus(1.43f,.045f),glow);
  }
  selector.buttons[5]=WorkshopAction(room,"BACK",new Vector3(-6.6f,.1f,-5.5f),paper);
  selector.buttons[6]=WorkshopAction(room,"USE SKIN",new Vector3(6.6f,.1f,-5.5f),glow);
  menu.skinButtons=new ToyBoxMenuController.PuckButton[5];Array.Copy(selector.buttons,menu.skinButtons,5);menu.equippedMarkers=selector.potRings;menu.skinsBackButton=selector.buttons[5];
  WorkshopProps(room,wood,paper,brass);
  var cam=Group("WorkshopCamera",room).gameObject;selector.viewCamera=cam.AddComponent<Camera>();cam.AddComponent<AudioListener>();cam.tag="MainCamera";selector.viewCamera.fieldOfView=38;selector.viewCamera.farClipPlane=150;cam.transform.rotation=Quaternion.Euler(31,0,0);selector.viewCamera.backgroundColor=new Color(.63f,.70f,.79f);selector.viewCamera.clearFlags=CameraClearFlags.SolidColor;
  var data=cam.AddComponent<UniversalAdditionalCameraData>();data.SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
  var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="WorkshopFinishing";
  var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
  var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.26f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.5f);
  var grading=profile.Add<ColorAdjustments>(true);grading.contrast.Override(5);grading.saturation.Override(3);grading.postExposure.Override(0f);
  string profilePath=Root+"/Materials/WorkshopFinishing.asset";
  var existingProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
  if(existingProfile==null){AssetDatabase.CreateAsset(profile,profilePath);foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);}
  else {foreach(var component in existingProfile.components)UnityEngine.Object.DestroyImmediate(component,true);existingProfile.components.Clear();foreach(var component in profile.components){existingProfile.components.Add(component);AssetDatabase.AddObjectToAsset(component,existingProfile);}UnityEngine.Object.DestroyImmediate(profile);profile=existingProfile;EditorUtility.SetDirty(profile);}
  var volume=Group("WorkshopFinish",room).gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=30;volume.sharedProfile=profile;
  var reflection=Group("SavedStudioReflections",room).gameObject.AddComponent<ReflectionProbe>();reflection.mode=UnityEngine.Rendering.ReflectionProbeMode.Custom;reflection.customBakedTexture=ToyBoxSurfaceBaker.StudioReflection();reflection.size=new Vector3(60,40,60);reflection.intensity=.8f;
  var rim=Group("WarmWindowRim",room).gameObject.AddComponent<Light>();rim.type=LightType.Directional;rim.transform.rotation=Quaternion.Euler(25,155,0);rim.intensity=.38f;rim.color=new Color(.75f,.86f,1f);
  var key=Group("SoftKey",room).gameObject.AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(48,-32,0);key.intensity=.90f;key.shadows=LightShadows.Soft;key.shadowBias=.025f;key.shadowNormalBias=.22f;key.shadowStrength=.60f;
  var fill=Group("SoftFill",room).gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(35,140,0);fill.intensity=.36f;
  var framing=new List<Vector3>();
  void Frame(Vector3 center,Vector3 size){foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})framing.Add(center+Vector3.Scale(size,new Vector3(x,y,z))*.5f);}
  Frame(new Vector3(0,1.4f,-2.5f),new Vector3(16.8f,2.8f,2.9f));
  Frame(new Vector3(0,.30f,-5.5f),new Vector3(17.4f,.7f,1.3f));
  Frame(new Vector3(0,5.3f,6.3f),new Vector3(10.5f,1.45f,.4f));
  Frame(new Vector3(0,1.7f,3.5f),new Vector3(15.8f,1.8f,6.9f));selector.framingPoints=framing.ToArray();
  rails=railOriginal;pucks=pucksOriginal;glow=glowOriginal;wood=woodOriginal;surface=surfaceOriginal;ink=inkOriginal;selector.Fit(16f/9f);ApplyPremiumWorkshop(room);SavePart(room,"PaintWorkshopSkins");room.gameObject.SetActive(false);
 }
 static void AddWorkshopSkinEmblem(Transform pot,int index,Material color,Material gold)
 {
  var emblem=Group("RaisedSkinEmblem",pot,new Vector3(0,-.60f,-1.435f));
  if(index==0){var dot=Disc("PaintSwatch",emblem,Vector3.zero,.10f,.025f,color,.01f);dot.transform.localRotation=Quaternion.Euler(90,0,0);dot.transform.localScale=new Vector3(2.3f,1,1);}
  else if(index==1){for(int side=-1;side<=1;side+=2){var leaf=MeshObject("Leaf",emblem,new Vector3(side*.1f,.03f,0),PlayroomLeafMesh(),mint);leaf.transform.localScale=new Vector3(.65f,.24f,.55f);leaf.transform.localRotation=Quaternion.Euler(90,0,side*45);}}
  else if(index==2){for(int k=0;k<7;k++){float a=k*Mathf.PI/3;var cell=MeshObject("HoneyCell",emblem,k==6?Vector3.zero:new Vector3(Mathf.Cos(a)*.16f,Mathf.Sin(a)*.16f,0),ToyBoxGeometry.MapPrism(6,.095f,.025f,.01f),yellow);cell.transform.localRotation=Quaternion.Euler(90,0,0);}}
  else if(index==3){for(int side=-1;side<=1;side+=2){var berry=Disc("Berry",emblem,new Vector3(side*.11f,0,0),.13f,.07f,coral,.033f);berry.transform.localRotation=Quaternion.Euler(90,0,side*18);}}
  else{for(int k=0;k<3;k++){var wave=MeshObject("Wave",emblem,new Vector3((k-1)*.18f,0,0),ToyBoxGeometry.Torus(.1f,.025f),color);wave.transform.localRotation=Quaternion.Euler(90,0,0);wave.transform.localScale=new Vector3(1,.4f,1);}}
 }
 static Texture2D WorkshopTexture(string name)
 {
  string path=Root+"/Materials/"+name+".png";
  var importer=AssetImporter.GetAtPath(path) as TextureImporter;
  if(importer==null)throw new InvalidOperationException("Missing workshop material texture: "+path);
  importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
  return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 static void CurveWorkshopLabel(Transform part,Transform pot,int skin,float radius=1.46f)
 {
  var mf=part.GetComponent<MeshFilter>();if(mf==null)return;
  var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);var vertices=mesh.vertices;
  for(int n=0;n<vertices.Length;n++){var v=pot.InverseTransformPoint(part.TransformPoint(vertices[n]));float angle=v.x/radius;vertices[n]=new Vector3(Mathf.Sin(angle)*radius,v.y,-Mathf.Cos(angle)*radius+(v.z+radius));}
  mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
  // Rivets have separate positions and must not share the same baked mesh asset.
  string key="CurvedCanLabel_"+skin+"_"+part.name.Replace(" ","_")+"_"+part.localPosition.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
  mf.sharedMesh=ToyBoxGeometry.Save(mesh,Root+"/Meshes/"+key+".asset");part.localPosition=Vector3.zero;part.localRotation=Quaternion.identity;part.localScale=Vector3.one;
 }
 static Texture2D WorkshopOakGrain()
 {
  var texture=new Texture2D(512,512,TextureFormat.RGBA32,true){name="WorkshopOakGrain",wrapMode=TextureWrapMode.Repeat,anisoLevel=8};
  var pixels=new Color[512*512];
  for(int y=0;y<512;y++)for(int x=0;x<512;x++){float u=x/512f,v=y/512f;float warp=Mathf.PerlinNoise(u*3,v*4)*2.0f;float fibre=Mathf.PerlinNoise(u*5,v*120+warp);float cloud=Mathf.PerlinNoise(u*6,v*14+warp);float tone=.86f+fibre*.12f+cloud*.06f;pixels[y*512+x]=new Color(tone,tone*.975f,tone*.925f);}
  texture.SetPixels(pixels);texture.Apply();return ToyBoxGeometry.Save(texture,Root+"/Materials/WorkshopOakGrain.asset");
 }
 // Every showroom prop is authored here and serialized into the prefab, never built at runtime.
 static void BuildWorkshopBackdrop(Transform room,Material white,Material label,Material metal)
 {
  var backdrop=Group("ArcadeShowroomBackdrop",room);
  var wall=Material("ArcadeCoolPlaster",new Color(.49f,.56f,.65f),.18f);
  var panel=Material("ArcadeWallPanels",new Color(.56f,.63f,.72f),.28f);
  Box("CoolGrayWall",backdrop,new Vector3(0,4.5f,8.2f),new Vector3(36,13,.4f),wall,.08f);
  Box("CentralWallPanel",backdrop,new Vector3(0,4.4f,7.85f),new Vector3(15.1f,10,.22f),panel,.10f);
  for(int side=-1;side<=1;side+=2){
   Box("MetalLightChannel",backdrop,new Vector3(side*7.65f,4.4f,7.66f),new Vector3(.24f,10,.17f),metal,.045f);
   Box("CyanWallLight",backdrop,new Vector3(side*7.65f,4.4f,7.55f),new Vector3(.075f,10,.08f),glow,.032f);
  }
  var brand=Group("EnamelBucaPlaque",backdrop,new Vector3(-9.8f,4.55f,6.5f));brand.localRotation=Quaternion.Euler(0,8,-3);
  Box("SilverPlaqueFrame",brand,Vector3.zero,new Vector3(4.05f,2.6f,.30f),metal,.15f);
  Box("BlueEnamelFace",brand,new Vector3(0,0,-.17f),new Vector3(3.85f,2.4f,.10f),rails[0],.10f);
  Text("BUCA",brand,new Vector3(0,.26f,-.25f),.77f,label,false,.30f,.025f);
  Text("ROLL  AIM  SCORE",brand,new Vector3(0,-.65f,-.25f),.17f,label);
  var sign=Group("ArcadeLightSign",backdrop,new Vector3(10,4.5f,6.9f));sign.localRotation=Quaternion.Euler(0,-6,2);
  Box("SilverSignFrame",sign,Vector3.zero,new Vector3(3.5f,3.3f,.30f),metal,.10f);
  Box("CyanSignBorder",sign,new Vector3(0,0,-.17f),new Vector3(3.25f,3.05f,.08f),glow,.055f);
  Box("DarkEnamelSign",sign,new Vector3(0,0,-.23f),new Vector3(3.05f,2.85f,.08f),ink,.045f);
  string[] words={"GOOD","PUCKS","GOOD","TIMES"};for(int i=0;i<4;i++)Text(words[i],sign,new Vector3(0,.94f-i*.61f,-.285f),.37f,label);
 }
 static void WorkshopProps(Transform room,Material white,Material label,Material metal)
 {
  var leafPaint=Material("ArcadeLeafGreen",new Color(.055f,.37f,.18f),.40f);
  var plant=Group("ShowroomPlant",room,new Vector3(-10.4f,1.05f,3.4f));Disc("CeramicPot",plant,Vector3.zero,.75f,1.75f,white,.14f);Disc("DarkSoil",plant,new Vector3(0,.885f,0),.64f,.025f,ink,.008f);
  for(int i=0;i<7;i++){float angle=i*2.4f;var leaf=MeshObject("SolidLeaf",plant,new Vector3(Mathf.Cos(angle)*.36f,1.2f+i*.16f,Mathf.Sin(angle)*.36f),PlayroomLeafMesh(),leafPaint);leaf.transform.localScale=new Vector3(1.65f,1.15f,2.0f);leaf.transform.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,40);}
  var block=Box("BlueSculpture",room,new Vector3(10.65f,1.0f,3.1f),new Vector3(1.45f,1.7f,1.45f),rails[0],.13f);block.transform.localRotation=Quaternion.Euler(0,28,-8);
  Disc("ChromePebble",room,new Vector3(9.25f,.59f,2.5f),.59f,1.18f,metal,.56f);
 }
 static ToyBoxMenuController.PuckButton WorkshopAction(Transform parent,string label,Vector3 pos,Material color)
 {
  var baseT=Group(label,parent,pos);Box("SatinSocket",baseT,Vector3.zero,new Vector3(3.9f,.24f,1.15f),wood,.18f);
  var cap=Group("MovingCap",baseT,new Vector3(0,.18f,0));var face=Box("Button",cap,Vector3.zero,new Vector3(3.7f,.25f,1.05f),color,.15f);var hit=face.AddComponent<BoxCollider>();Text(label,cap,new Vector3(0,.135f,0),.36f,ink,true);
  var ring=Group("Focus",baseT);Box("Highlight",ring,new Vector3(0,-.04f,0),new Vector3(4.02f,.12f,1.28f),glow,.16f);ring.gameObject.SetActive(false);
  return new ToyBoxMenuController.PuckButton{cap=cap,hit=hit,face=face.GetComponent<MeshRenderer>(),selectedRing=ring.gameObject,restPosition=cap.localPosition};
 }
 public static void RenderPaintWorkshop()
 {
  EditorSceneManager.OpenScene(MenuScene);var menu=UnityEngine.Object.FindAnyObjectByType<ToyBoxMenuController>();
  typeof(ToyBoxMenuController).GetMethod("ShowPage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(menu,new[]{Enum.Parse(typeof(ToyBoxMenuController).GetNestedType("Page",System.Reflection.BindingFlags.NonPublic),"Skins")});
  var s=menu.paintWorkshop;s.viewCamera.aspect=16f/9f;s.Focus(0);s.Fit(16f/9f);ToyBoxMenuValidation.RenderCamera(s.viewCamera,"Paint-Workshop-Skins",1920,1080);
  int before=BucaSkinBinding.Selected;for(int i=0;i<5;i++){s.Focus(i);if(BucaSkinBinding.Selected!=before)throw new Exception("Preview changed equipped skin");if(s.board.surfaces[0].target.sharedMaterial!=s.board.surfaces[0].variants[i])throw new Exception("Board preview failed");}
  Physics.SyncTransforms();foreach(var b in s.buttons){var ray=s.viewCamera.ScreenPointToRay(s.viewCamera.WorldToScreenPoint(b.hit.bounds.center));if(!b.hit.Raycast(ray,out var hit,150))throw new Exception("Button not visible");}
  foreach(float aspect in new[]{16f/9f,4f/3f,9f/16f}){s.viewCamera.aspect=aspect;s.Fit(aspect);foreach(var point in s.framingPoints){var v=s.viewCamera.WorldToViewportPoint(point);if(v.x<0||v.x>1||v.y<0||v.y>1||v.z<0)throw new Exception("Workshop framing clipped at "+aspect);}}
  s.viewCamera.aspect=16f/9f;s.Fit(16f/9f);
  Debug.Log("PAINT_WORKSHOP_VALIDATED: five saved board/puck previews, non-destructive selection, seven hittable controls.");
 }
}
