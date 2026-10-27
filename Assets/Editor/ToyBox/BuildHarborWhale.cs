#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class BuildHarborWhale
{
 public const string Path="Assets/Art/HarborWhale/HarborWhaleEncounter.prefab";
 static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
 static Sprite Slice(Texture2D tex,string name,int x,int top,int w,int h){string path="Assets/Art/HarborWhale/"+name+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(s!=null)return s;s=Sprite.Create(tex,new UnityEngine.Rect(x,tex.height-top-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(s,path);return s;}
 static RectTransform Pic(string name,Transform parent,Sprite sprite,Vector2 size,Vector2 pos){var r=Rect(name,parent,size,pos);var im=r.gameObject.AddComponent<UnityEngine.UI.Image>();im.sprite=sprite;im.raycastTarget=false;return r;}
 public static string AddWaterline()
 {
  var root=PrefabUtility.LoadPrefabContents(Path);
  try
  {
   var w=root.GetComponent<HarborWhaleEncounter>();
   if(w.waterMask==null)
   {
    w.waterMask=Rect("WaterSurfaceOcclusion",w.stage,new Vector2(1672,800),new Vector2(0,445));
    w.waterMask.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
    w.whale.SetParent(w.waterMask,false);
   }
   PrefabUtility.SaveAsPrefabAsset(root,Path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  return "Smaller centered whale, head-first dive and waterline occlusion saved.";
 }
 public static string Apply()
 {
  AssetDatabase.Refresh();var imp=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/HarborWhale/WhaleAtlas.png");imp.textureType=TextureImporterType.Default;imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/HarborWhale/WhaleAtlas.png");
  var root=PrefabUtility.LoadPrefabContents(BuildGrandTrainWelcome.Path);
  try
  {
   root.name="HarborWhaleEncounter";var old=root.GetComponent<GrandTrainWelcome>();var w=root.AddComponent<HarborWhaleEncounter>();
   w.fade=old.fade;w.artwork=old.artwork;w.mapDetails=root.GetComponent<CinematicMapDetails>();w.effects=old.effects;w.voice=old.voice;w.ambience=old.trainSound;
   w.hello=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/HarborWhale/Hello.wav");w.splash=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/HarborWhale/Splash.wav");w.waterLoop=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambience/EveningHarborWater.wav");
   foreach(var ticket in old.tickets)Object.DestroyImmediate(ticket.gameObject);
   foreach(var grip in old.ticketGrips)Object.DestroyImmediate(grip.gameObject);
   Object.DestroyImmediate(old.train.gameObject);Object.DestroyImmediate(old.celebration.gameObject);Object.DestroyImmediate(old.controls.gameObject);
   var title=root.transform.Find("RewardTitle");w.greeting=title.GetComponent<CanvasGroup>();
   var labels=title.GetComponentsInChildren<TMPro.TMP_Text>();labels[0].text="HELLO!";labels[1].text="YOUR NEW HARBOR FRIEND";
   w.stage=Rect("WhaleHarborStage",w.artwork.transform,new Vector2(1672,941),Vector2.zero);
   w.whale=Rect("WhaleRig",w.stage,new Vector2(500,450),Vector2.zero);w.whaleVisibility=w.whale.gameObject.AddComponent<CanvasGroup>();
   w.tail=Pic("Tail",w.whale,Slice(tex,"Tail",1045,85,491,510),new Vector2(165,172),new Vector2(137,-35));w.tail.pivot=new Vector2(.12f,.13f);
   w.flipper=Pic("WavingFlipper",w.whale,Slice(tex,"Flipper",1120,640,410,300),new Vector2(130,95),new Vector2(110,-32));w.flipper.pivot=new Vector2(.12f,.24f);
   Pic("WhaleBody",w.whale,Slice(tex,"Body",10,200,1025,710),new Vector2(350,242),Vector2.zero);
   var water=Rect("SplashAndRipples",w.stage,new Vector2(1672,941),Vector2.zero);w.water=water.gameObject.AddComponent<HarborWhaleWater>();w.water.raycastTarget=false;water.SetAsFirstSibling();
   var veil=root.transform.Find("SoftVeil");if(veil!=null)veil.gameObject.SetActive(false);
   Object.DestroyImmediate(old);root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
  try{foreach(var go in scene.GetRootGameObjects()){var manager=go.GetComponentInChildren<LevelManager>(true);if(manager==null)continue;manager.harborWhalePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path).GetComponent<HarborWhaleEncounter>();EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);break;}}
  finally{if(opened)EditorSceneManager.CloseScene(scene,true);}return "Whale encounter saved and linked to World 2 completions.";
 }
}
#endif
