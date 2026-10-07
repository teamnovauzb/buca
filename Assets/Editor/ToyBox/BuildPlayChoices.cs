using UnityEditor;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
 static void BuildResumeExplanation(Transform parent)
 {
  var sign=Group("RaisedResumeExplanation",parent,new Vector3(0,3.05f,1.4f));
  Box("ResumePlaque",sign,Vector3.zero,new Vector3(8.2f,1.12f,.20f),wood,.10f);
  Text("CONTINUE",sign,new Vector3(0,.23f,-.115f),.31f,ink);
  Text("PLAY FROM YOUR SAVED LEVEL",sign,new Vector3(0,-.23f,-.115f),.25f,ink);
 }
 public static void GeneratePlayChoices()
 {
  PrepareFolders();ToyBoxGeometry.Initialize();LoadWorkshopMaterials();
  var root=PrefabUtility.LoadPrefabContents(PrefabPath);
  try {
   var menu=root.GetComponent<ToyBoxMenuController>();Object.DestroyImmediate(menu.resumeRoot);
   var tray=Group("ReturningPlayerTray",root.transform);menu.resumeRoot=tray.gameObject;
   BuildResumeExplanation(tray);

   menu.resumeButtons=new[]{Button(tray,"LEVEL 1",new Vector3(-3.5f,2.06f,-.85f),1.17f,yellow,.27f),Button(tray,"CONTINUE",new Vector3(0,2.06f,-.85f),1.17f,coral,.245f),Button(tray,"BACK",new Vector3(3.5f,2.06f,-.85f),1.17f,mint,.30f)};
   tray.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();Debug.Log("PLAY_CHOICES_SAVED");
 }
}
