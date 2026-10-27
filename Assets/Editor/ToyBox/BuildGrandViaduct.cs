#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class BuildGrandViaduct
{
 const string TexturePath="Assets/TwilightMap/WorkshopRailway/GrandViaduct.png";
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static Vector3 World(Vector2 p){var a=BuildForestHarborMap.Art(p);return new Vector3(a.x/20,0,a.y/20);}
 public static string Apply()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  var map=Object.FindFirstObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
  if(map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu required");
  System.IO.Directory.CreateDirectory("output/grand-viaduct");
  if(!System.IO.File.Exists("output/grand-viaduct/MainMenu.before.unity"))EditorSceneManager.SaveScene(map.gameObject.scene,"output/grand-viaduct/MainMenu.before.unity",true);
  AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath);importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);var view=map.illustratedMenu;
  var old=view.artwork.GetComponent<RawImage>().texture;
  foreach(var image in map.GetComponentsInChildren<RawImage>(true))if(image.texture==old)image.texture=tex;
  float oldLength=(map.stops[49].distance-map.stops[30].distance)*20;
  // Retain the exact World 1 route and entry bridge, replacing only the harbor.
  var points=new List<Vector3>();float sum=0;
  for(int i=0;i<map.railway.Length;i++){
   if(i>0)sum+=Vector3.Distance(map.railway[i-1],map.railway[i]);
   if(sum>map.stops[29].distance&&map.railway[i].x*20+836>=970)break;
   points.Add(map.railway[i]);
  }
  var last=points[points.Count-1];
  var keys=new List<Vector2>{new Vector2(last.x*20+836,470.5f-last.z*20),new Vector2(1005,326),new Vector2(1050,316),new Vector2(1100,315),new Vector2(1170,325),new Vector2(1260,344),new Vector2(1360,365),new Vector2(1450,385),new Vector2(1520,418),new Vector2(1570,465),new Vector2(1595,520),new Vector2(1594,565),new Vector2(1572,610),new Vector2(1520,648),new Vector2(1450,672),new Vector2(1370,677),new Vector2(1295,666),new Vector2(1210,638),new Vector2(1130,606),new Vector2(1050,576),new Vector2(1015,562)};
  for(int k=0;k<keys.Count-1;k++){
   var a=keys[Math.Max(0,k-1)];var b=keys[k];var c=keys[k+1];var d=keys[Math.Min(keys.Count-1,k+2)];int n=Math.Max(8,Mathf.CeilToInt(Vector2.Distance(b,c)));
   for(int j=1;j<=n;j++){float t=(float)j/n;points.Add(World(.5f*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)));}
  }
  map.railway=points.ToArray();typeof(TwilightRailwayMap).GetMethod("Cache",Private).Invoke(map,null);
  float total=0,start=0,best=float.MaxValue;var target=World(new Vector2(1025,321));
  for(int i=1;i<points.Count;i++){total+=Vector3.Distance(points[i-1],points[i]);float delta=(points[i]-target).sqrMagnitude;if(delta<best){best=delta;start=total;}}
  float end=total-.4f;
  for(int i=30;i<50;i++){
   map.stops[i].distance=Mathf.Lerp(start,end,(i-30)/19f);var p=map.Point(map.stops[i].distance);view.route[i]=new Vector2(p.x,p.z)*20;view.stopRects[i].anchoredPosition=view.route[i];EditorUtility.SetDirty(view.stopRects[i]);
  }
  var presentation=view.GetComponent<ForestHarborMapPresentation>();presentation.harborTunnelEntrance=presentation.harborTunnelExit=-100;
  var beam=view.artwork.Find("RotatingLighthouseBeam") as RectTransform;
  if(beam!=null){beam.anchoredPosition=BuildForestHarborMap.Art(new Vector2(1510,235));beam.GetComponent<RawImage>().material.SetVector("_ArtOrigin",new Vector4(1510,235,0,0));EditorUtility.SetDirty(beam.GetComponent<RawImage>().material);}
  EditorUtility.SetDirty(map);EditorUtility.SetDirty(view);EditorUtility.SetDirty(presentation);
  typeof(TwilightRailwayMap).GetField("distance",Private).SetValue(map,map.stops[map.Parked].distance);typeof(TwilightRailwayMap).GetMethod("Pose",Private).Invoke(map,null);
  EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);
  var root=PrefabUtility.LoadPrefabContents(BuildWorldTwoHarborReveal.Path);
  try{
   var reveal=root.GetComponent<WorldTwoHarborReveal>();reveal.artwork.texture=tex;reveal.FitMapToScreen();
   foreach(var image in root.GetComponentsInChildren<RawImage>(true))if(image.texture==old)image.texture=tex;
   for(int i=0;i<reveal.ticketLayer.childCount;i++){
    var t=reveal.ticketLayer.GetChild(i) as RectTransform;int level=i+(i>=29?2:0);if(level<50)t.anchoredPosition=view.route[level];
   }
   var up=reveal.trainUpgrade;
   for(int i=0;i<up.route.Length;i++){float distance=Mathf.Lerp(map.stops[29].distance,map.stops[30].distance,(float)i/(up.route.Length-1));up.route[i]=map.Point(distance);up.wheelTravel[i]=map.WheelDistanceAt(distance)-map.WheelDistanceAt(map.stops[29].distance);}
   PrefabUtility.SaveAsPrefabAsset(root,BuildWorldTwoHarborReveal.Path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  BuildCompletionRailwayMap.Apply();AssetDatabase.SaveAssets();
  return $"Grand viaduct installed. World 2 travel {oldLength:F1} -> {(end-start)*20:F1} pixels; 20 equally spaced stops; World 1 retained.";
 }
 public static string Preview()
 {
  var source=Object.FindFirstObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);var scene=EditorSceneManager.NewPreviewScene();GameObject copy=null;
  try{
   copy=Object.Instantiate(source.gameObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);var map=copy.GetComponent<TwilightRailwayMap>();
   foreach(var camera in copy.GetComponentsInChildren<Camera>(true))camera.scene=scene;
   copy.SetActive(true);var art=map.illustratedMenu.artwork.GetComponent<RawImage>();var material=new Material(art.material);art.material=material;material.SetFloat("_WorldReveal",1);
   try{foreach(int level in new[]{30,39,49})typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{map,level,2560,1440,$"output/grand-viaduct/level-{level+1}.png"});}finally{Object.DestroyImmediate(material);}
   for(int i=30;i<50;i++){var p=map.Point(map.stops[i].distance);if(Vector2.Distance(new Vector2(p.x,p.z)*20,map.illustratedMenu.route[i])>.01f)throw new Exception("Stop not on rail");if(i>30&&map.stops[i].distance<=map.stops[i-1].distance)throw new Exception("Stop order");}
   return "PASS: World 2 stop alignment and order; renders at 31, 40, 50.";
  }finally{if(copy!=null)Object.DestroyImmediate(copy);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
#endif
