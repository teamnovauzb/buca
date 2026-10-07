using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
 public static void GenerateBlueTrajectory()
 {
  PrepareFolders();ToyBoxGeometry.Initialize();
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
  var puck=Object.FindAnyObjectByType<PuckController>(FindObjectsInactive.Include);
  var old=Object.FindAnyObjectByType<TrajectoryMarkers>(FindObjectsInactive.Include);if(old!=null)Object.DestroyImmediate(old.gameObject);
  var root=Group("SavedBlueTrajectoryMarkers",null);var markers=root.gameObject.AddComponent<TrajectoryMarkers>();markers.path=puck.previewLine;
  var blue=Material("TrajectorySkyBlue",new Color(.13f,.58f,.95f),.25f);
  var navy=Material("TrajectoryNavy",new Color(.015f,.055f,.16f),.25f);
  // Unlit colors remain legible in the board's cast shadows.
  foreach(var mat in new[]{blue,navy}) {mat.shader=Shader.Find("Universal Render Pipeline/Unlit");mat.SetColor("_BaseColor",mat==blue?new Color(.13f,.58f,.95f):new Color(.015f,.055f,.16f));EditorUtility.SetDirty(mat);}
  var mesh=ClearArcadeArrowMesh();markers.arrows=new Transform[3];
  for(int i=0;i<3;i++) {
   var arrow=Group("DirectionMarker"+i,root);markers.arrows[i]=arrow;
   var edge=MeshObject("NavyBorder",arrow,Vector3.zero,mesh,navy);edge.transform.localScale=new Vector3(2.1f,.12f,2.1f);
   var fill=MeshObject("BlueFace",arrow,new Vector3(0,.006f,0),mesh,blue);fill.transform.localScale=new Vector3(1.65f,.12f,1.65f);
   arrow.gameObject.SetActive(false);
  }
  markers.endpoint=Group("Endpoint",root);
  Disc("NavyEdge",markers.endpoint,Vector3.zero,.09f,.008f,navy,.002f);
  Disc("BlueFace",markers.endpoint,new Vector3(0,.007f,0),.065f,.008f,blue,.002f);
  markers.endpoint.gameObject.SetActive(false);
  var line=puck.previewLine;line.widthMultiplier=1;line.widthCurve=AnimationCurve.Linear(0,.065f,1,.065f);line.startColor=Color.white;line.endColor=Color.white;line.textureMode=LineTextureMode.Stretch;line.numCapVertices=8;line.numCornerVertices=6;
  line.sharedMaterial.SetTexture("_MainTex",null);EditorUtility.SetDirty(line.sharedMaterial);
  // Remove the overlapping short aim stroke; the predicted path is the visible guide.
  puck.aimLine.widthMultiplier=0;
  EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("BLUE_TRAJECTORY_SAVED");
 }
}
