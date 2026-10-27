#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;
public static class BuildGrandTrainWelcome
{
 public const string Path="Assets/Art/GrandTrainWelcome/GrandTrainWelcome.prefab";
 static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
 static Sprite Slice(Texture2D tex,string name,int x,int y,int w,int h){string p="Assets/Art/GrandTrainWelcome/"+name+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(p);if(s!=null)return s;s=Sprite.Create(tex,new UnityEngine.Rect(x,tex.height-y-h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(s,p);return s;}
 static RectTransform Pic(string name,Transform parent,Sprite s,Vector2 size,Vector2 pos){var r=Rect(name,parent,size,pos);var im=r.gameObject.AddComponent<Image>();im.sprite=s;im.raycastTarget=false;return r;}
 public static void ConfigureChassis(GrandTrainWelcome reward)
 {
  for(int i=0;i<reward.animals.Length;i++)
  {
   var position=new Vector2(-285+i*235,40);
   reward.animalPositions[i]=position;
   reward.animals[i].anchoredPosition=position;
   reward.handPositions[i]=position+new Vector2(i==4?-86:91,i==0?23:i==2?55:5);
  }
  // Sandwich the passengers between the rear walls and the front panels.
  var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/GrandTrainWelcome/TrainAtlas.png");
  for(int i=0;i<3;i++)reward.train.Find("Carriage"+i).SetAsLastSibling();
  foreach(var animal in reward.animals)animal.SetAsLastSibling();
  for(int i=0;i<3;i++)
  {
   var wagon=(RectTransform)reward.train.Find("Carriage"+i);
   var name="CarriageFront"+i;
   var front=reward.train.Find(name) as RectTransform;
   var sprite=wagon.GetComponent<UnityEngine.UI.Image>().sprite;
   var size=wagon.sizeDelta;
   var pos=wagon.anchoredPosition;
   if(front==null)front=Pic(name,reward.train,sprite,size,pos);
   else{front.sizeDelta=size;front.anchoredPosition=pos;front.GetComponent<UnityEngine.UI.Image>().sprite=sprite;}
   if(front.GetComponent<GrandTrainFrontRim>()==null)front.gameObject.AddComponent<GrandTrainFrontRim>();
   front.SetAsLastSibling();
  }
  var locomotive=reward.train.Find("Locomotive") as RectTransform;
  if(locomotive!=null)
  {
   locomotive.anchoredPosition=new Vector2(-600,-15);
   // Keep the cab in front of the adjoining wagon instead of clipping it behind the rim.
   locomotive.SetAsLastSibling();
   foreach(var wheel in reward.wheels)if(wheel!=null)wheel.SetAsLastSibling();
  }
  if(reward.wheels!=null && reward.wheels.Length>=2)
  {
   reward.wheels[0].anchoredPosition=new Vector2(-750,-234);
   reward.wheels[1].anchoredPosition=new Vector2(-540,-234);
  }
  var existing=reward.train.Find("Undercarriage");
  var rect=existing!=null?(RectTransform)existing:Rect("Undercarriage",reward.train,new Vector2(1920,700),Vector2.zero);
  rect.SetAsFirstSibling();
  if(rect.GetComponent<CanvasRenderer>()==null)rect.gameObject.AddComponent<CanvasRenderer>();
  var graphic=rect.GetComponent<GrandTrainUndercarriage>();
  if(graphic==null)graphic=rect.gameObject.AddComponent<GrandTrainUndercarriage>();
  graphic.raycastTarget=false;
 }
 public static void ConfigureBigCelebration(GrandTrainWelcome reward)
 {
  var fx=reward.celebration;fx.continuousWaves=true;fx.starCount=64;
  var pieces=new Transform[800];
  for(int i=0;i<pieces.Length;i++)
  {
   var p=i<fx.pieces.Length?fx.pieces[i]:new GameObject("CelebrationPiece"+i,typeof(MeshFilter),typeof(MeshRenderer)).transform;
   p.SetParent(fx.stage,false);p.gameObject.layer=28;
   p.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/RabbitGift/"+(i<64?"CelebrationBeveledStar":"CelebrationCurledRibbon")+".asset");
   p.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/RabbitGift/CelebrationGloss"+(i<64?0:1+i%5)+".mat");
   pieces[i]=p;
  }
  fx.pieces=pieces;
  // Keep the dense ribbon shower behind the train and its passengers.
  fx.transform.SetSiblingIndex(reward.train.GetSiblingIndex());
 }
 public static string BoostCelebration()
 {
  var root=PrefabUtility.LoadPrefabContents(Path);
  try{ConfigureBigCelebration(root.GetComponent<GrandTrainWelcome>());PrefabUtility.SaveAsPrefabAsset(root,Path);}
  finally{PrefabUtility.UnloadPrefabContents(root);}
  return "800 3D pieces, including 64 gold stars, with staggered continuous waves.";
 }
 public static string ConfigureTicketHandoff()
 {
  var root=PrefabUtility.LoadPrefabContents(Path);
  try
  {
   var r=root.GetComponent<GrandTrainWelcome>();r.ticketGrips=new RectTransform[5];
   for(int i=0;i<5;i++)
   {
    var animal=r.animals[i];var source=animal.GetComponent<UnityEngine.UI.Image>().sprite;
    var hand=r.handPositions[i]-r.animalPositions[i];
    float px=source.rect.x+(hand.x/animal.sizeDelta.x+.5f)*source.rect.width;
    float py=source.rect.y+(hand.y/animal.sizeDelta.y+.5f)*source.rect.height;
    var sprite=Slice(source.texture,"TicketGrip"+i,Mathf.RoundToInt(px-26),Mathf.RoundToInt(source.texture.height-py-26),52,52);
    var existing=root.transform.Find("TicketGrip"+i) as RectTransform;
    var size=new Vector2(52*animal.sizeDelta.x/source.rect.width,52*animal.sizeDelta.y/source.rect.height);
    var grip=existing!=null?existing:Pic("TicketGrip"+i,root.transform,sprite,size,Vector2.zero);
    grip.SetAsLastSibling();r.ticketGrips[i]=grip;
   }
   PrefabUtility.SaveAsPrefabAsset(root,Path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  return "Five held tickets with paw grips and staggered forward handoff.";
 }
 public static string AddFaceAndTrainAudio()
 {
  var root=PrefabUtility.LoadPrefabContents(Path);
  try
  {
   var r=root.GetComponent<GrandTrainWelcome>();r.faces=new GrandAnimalFace[5];
   Vector2[] left={new Vector2(-14,48),new Vector2(-23,45),new Vector2(-42,77),new Vector2(-34,55),new Vector2(-32,34)};
   Vector2[] right={new Vector2(52,49),new Vector2(42,39),new Vector2(27,92),new Vector2(25,42),new Vector2(39,23)};
   Vector2[] mouths={new Vector2(20,12),new Vector2(6,4),new Vector2(2,50),new Vector2(-13,11),new Vector2(0,5)};
   for(int i=0;i<5;i++)
   {
    var face=r.animals[i].GetComponent<GrandAnimalFace>();if(face==null)face=r.animals[i].gameObject.AddComponent<GrandAnimalFace>();
    face.friendIndex=i;face.leftEye=left[i];face.rightEye=right[i];face.mouth=mouths[i];r.faces[i]=face;
    if(i==4)
    {
     face.extraEyes=new RectTransform[2];
     for(int e=0;e<2;e++)
     {
      string name="BlinkEye"+e;var eye=r.animals[i].Find(name) as RectTransform;
      if(eye==null){eye=Rect(name,r.animals[i],new Vector2(24,28),e==0?left[i]:right[i]);eye.gameObject.AddComponent<GrandPenguinEye>().raycastTarget=false;}
      face.extraEyes[e]=eye;
     }
    }
   }
   if(r.trainSound==null){var sound=new GameObject("ArrivalTrainSound");sound.transform.SetParent(root.transform,false);r.trainSound=sound.AddComponent<AudioSource>();}
   r.trainSound.playOnAwake=false;r.trainSound.spatialBlend=0;
   r.trainLoop=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/StationMovingTrain.wav");
   r.arrival=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/StationWhistle.wav");
   PrefabUtility.SaveAsPrefabAsset(root,Path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  return "Five blinking/talking faces and entrance train audio saved.";
 }
 public static string FixChassis()
 {
  var root=PrefabUtility.LoadPrefabContents(Path);
  try{ConfigureChassis(root.GetComponent<GrandTrainWelcome>());PrefabUtility.SaveAsPrefabAsset(root,Path);}
  finally{PrefabUtility.UnloadPrefabContents(root);}
  return "Connected all four bodies with chassis, axle supports and three couplings.";
 }
 public static string Apply()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
  foreach(string n in new[]{"TrainAtlas","FriendsAtlas"}){var imp=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/GrandTrainWelcome/"+n+".png");imp.textureType=TextureImporterType.Default;imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=true;imp.maxTextureSize=4096;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();}
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/GrandTrainWelcome/TrainAtlas.png");var friends=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/GrandTrainWelcome/FriendsAtlas.png");
  var root=PrefabUtility.LoadPrefabContents(BuildRabbitGiftReward.Path);
  try{
   root.name="GrandTrainWelcome";var old=root.GetComponent<RabbitGiftReward>();var r=root.AddComponent<GrandTrainWelcome>();
   r.fade=old.fade;r.controls=old.controls;r.artwork=old.artwork;r.celebration=old.celebration;r.continueButton=old.continueButton;r.voice=old.sound;r.effects=old.cheerSource;r.cheer=old.happyCheer;
   r.firstLine=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/GrandTrainWelcome/YouDidIt.wav");r.welcomeLine=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/GrandTrainWelcome/WelcomeWorldTwo.wav");r.arrival=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/StationDeparture.wav");
   Object.DestroyImmediate(old.gift.gameObject);Object.DestroyImmediate(old.ticketLayer.gameObject);
   var title=root.transform.Find("RewardTitle").GetComponent<RectTransform>();title.sizeDelta=new Vector2(630,140);title.anchoredPosition=new Vector2(0,370);old.heading.alpha=1;
   var labels=title.GetComponentsInChildren<TMP_Text>();r.headline=labels[0];r.subtitle=labels[1];foreach(var l in labels)l.rectTransform.sizeDelta=new Vector2(560,55);r.headline.fontSize=40;r.subtitle.fontSize=27;
   r.continueButton.GetComponentInChildren<TMP_Text>().text="PLAY LEVEL 31  ›";r.continueButton.GetComponent<RectTransform>().sizeDelta=new Vector2(380,92);
   r.train=Rect("ExpressTrain",root.transform,new Vector2(1800,700),new Vector2(0,-120));
   r.animals=new RectTransform[5];r.animalPositions=new Vector2[5];r.handPositions=new Vector2[5];r.tickets=new RectTransform[5];
   int[] xs={0,434,868,1302,1736};
   for(int i=0;i<5;i++){
    var sprite=Slice(friends,"Friend"+i,xs[i],0,i==4?436:434,724);
    var pos=new Vector2(-285+i*235,110);r.animalPositions[i]=pos;
    r.animals[i]=Pic("Friend"+i,r.train,sprite,new Vector2(250,417),pos);
    r.handPositions[i]=pos+new Vector2(i==4?-86:91,i==0?23:i==2?55:5);
   }
   var engine=Slice(tex,"Engine",0,60,620,520);var wagon=Slice(tex,"BlueWagon",620,255,450,280);var purple=Slice(tex,"PurpleWagon",1080,255,456,280);var wheel=Slice(tex,"Wheel",215,565,405,390);
   Pic("Locomotive",r.train,engine,new Vector2(490,411),new Vector2(-710,-15));
   for(int i=0;i<3;i++)Pic("Carriage"+i,r.train,i==2?purple:wagon,new Vector2(i==2?270:455,235),new Vector2(i==0?-170:i==1?300:670,-105));
   var wheels=new List<RectTransform>();
   foreach(float x in new[]{-860f,-650f,-340f,-10f,130f,460f,590f,750f})wheels.Add(Pic("GoldenWheel",r.train,wheel,new Vector2(122,118),new Vector2(x,-234)));
   r.wheels=wheels.ToArray();ConfigureChassis(r);ConfigureBigCelebration(r);
   var plaque=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RabbitGift/WoodPlaque.asset");
   for(int i=0;i<5;i++){
    var ticket=Rect("HandTicket"+i,root.transform,new Vector2(175,110),Vector2.zero);r.tickets[i]=ticket;var badge=ticket.gameObject.AddComponent<WorkshopStationBadge>();badge.ticketPlaque=true;badge.SetLocked(false);badge.raycastTarget=false;
    var txt=Rect("Destination",ticket,new Vector2(140,65),new Vector2(-2,7)).gameObject.AddComponent<TextMeshProUGUI>();txt.font=r.headline.font;txt.text="WORLD 2\n<size=32>31</size>";txt.fontSize=19;txt.color=new Color(.06f,.14f,.25f);txt.alignment=TextAlignmentOptions.Center;txt.raycastTarget=false;
   }
   r.celebration.transform.SetSiblingIndex(r.train.GetSiblingIndex());r.controls.transform.SetAsLastSibling();
   Object.DestroyImmediate(old);root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  var collection=AssetDatabase.LoadAssetAtPath<AnimalTrainCollection>("Assets/Resources/AnimalTrainCollection.asset");collection.friends=System.Array.FindAll(collection.friends,f=>f.id!="lion");EditorUtility.SetDirty(collection);
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
  try{foreach(var go in scene.GetRootGameObjects()){var m=go.GetComponentInChildren<LevelManager>(true);if(m==null)continue;m.grandTrainWelcomePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path).GetComponent<GrandTrainWelcome>();EditorUtility.SetDirty(m);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);break;}}
  finally{if(opened)EditorSceneManager.CloseScene(scene,true);}AssetDatabase.SaveAssets();return "Grand train welcome saved and linked to level 30; five friends, no lion.";
 }
}
#endif
