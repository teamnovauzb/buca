using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class MoonlitReplayPreview
{
 public static string Capture()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
  var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var old=LevelManager.Instance;
  var prop=typeof(LevelManager).GetProperty("Instance",BindingFlags.Public|BindingFlags.Static);
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
  var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene!=scene&&c.CompareTag("MainCamera")).ToArray();
  try {
   foreach(var c in cameras)c.tag="Untagged";
   var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LevelManager>(true)).First();prop.SetValue(null,manager);
   typeof(LevelManager).GetField("_currentIndex",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,30);
   var board=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[30],scene);
   var theme=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EveningGameplayTheme>(true)).First();theme.Apply(true,board);
   var v=manager.bestShotReplay;var cam=Camera.main;
   BestShotReplayPreview.BuildPreviewRecording(v,board,manager.puck,cam,31);
   if(!v.TryShow(cam,()=>{}))throw new Exception("Replay failed to open");
   v.SampleEntrance(1);v.SampleReplay(.5f);
   BestShotReplayPreview.CaptureFrame(v,1920,1080,"MoonlitSteel");
   BestShotReplayPreview.CaptureFrame(v,900,1600,"MoonlitSteel-Portrait");
   v.Hide(false);return "Rendered MoonlitSteel landscape and portrait";
  }finally{prop.SetValue(null,old);foreach(var c in cameras)if(c!=null)c.tag="MainCamera";EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
 }
}
