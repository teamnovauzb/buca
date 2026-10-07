#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
public static class HoneyReferencePolish
{
 static Mesh Save(string n,List<Vector3> v,List<int> t,List<Vector2> uv=null){var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);if(uv!=null)m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();return ToyBoxGeometry.Save(m,"Assets/ToyBoxMenu/Meshes/"+n+".asset");}
 static Vector3 Outline(float a,float w,float h,float r,float z){float x=Mathf.Cos(a),y=Mathf.Sin(a);return new Vector3(Mathf.Sign(x)*(w/2-r)+r*x,Mathf.Sign(y)*(h/2-r)+r*y,z);}
 static Mesh Tray()
 {
  var v=new List<Vector3>();var t=new List<int>();const int N=128;
  float[] widths={9.15f,9.4f,9.4f,9.28f,8.94f,8.82f,8.82f};float[] heights={4.15f,4.4f,4.4f,4.28f,3.94f,3.82f,3.82f};float[] depths={.32f,.20f,-.54f,-.64f,-.64f,-.54f,-.39f};
  for(int j=0;j<widths.Length;j++)for(int i=0;i<N;i++)v.Add(Outline((i+.5f)*Mathf.PI*2/N,widths[j],heights[j],j<4?.64f:.40f,depths[j]));
  for(int j=0;j<widths.Length-1;j++)for(int i=0;i<N;i++){int a=j*N+i,b=j*N+(i+1)%N,c=(j+1)*N+i,d=(j+1)*N+(i+1)%N;t.AddRange(new[]{a,c,b,b,c,d});}
  return Save("HoneyRoundedTray",v,t);
 }
 static float Sdf(Vector2 p){Vector2 q=new Vector2(Mathf.Abs(p.x),Mathf.Abs(p.y))-new Vector2(4.41f-.4f,1.91f-.4f);return new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-.4f;}
 static Mesh Floor()
 {
  var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();const int N=192;
  for(int i=0;i<N;i++){float a=i*Mathf.PI*2/N;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float lo=.56f,hi=10;for(int k=0;k<22;k++){float mid=(lo+hi)/2;if(Sdf(new Vector2(3.1f,0)+dir*mid)>0)hi=mid;else lo=mid;}foreach(float radius in new[]{.56f,lo}){var p=new Vector2(3.1f,0)+dir*radius;v.Add(new Vector3(p.x,p.y,-.405f));uv.Add(new Vector2(p.x/9+.5f,p.y/4+.5f));}}
  for(int i=0;i<N;i++){int a=i*2,b=((i+1)%N)*2;t.AddRange(new[]{a,b,a+1,b,b+1,a+1});}return Save("HoneyMapleCutout",v,t,uv);
 }
 static GameObject Part(Transform root,string n,Mesh mesh,Material mat,Vector3 p){var tr=root.Find(n);var g=tr?tr.gameObject:new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=mat;g.SetActive(true);return g;}
 public static string Apply()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");var b=Object.FindFirstObjectByType<HoneyTutorialBoard>();var stage=b.stage.transform;var root=stage.Find("MiniatureBoard");ToyBoxGeometry.Initialize();
  var teal=b.dim;teal.color=new Color(.16f,.40f,.41f);teal.SetFloat("_Smoothness",.36f);teal.EnableKeyword("_EMISSION");teal.SetColor("_EmissionColor",new Color(.018f,.055f,.056f));teal.SetFloat("_Cull",0);EditorUtility.SetDirty(teal);
  var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TrainLeaderboard3D/CreamMaple.mat");
  foreach(string n in new[]{"Body","Maple","BackRail","FrontRail","SideRail0","SideRail1","GoalWell","GoalLip","ShootMount"})root.Find(n).gameObject.SetActive(false);
  Part(root,"SculptedTray",Tray(),teal,Vector3.zero);var floor=Part(root,"CutoutMaple",Floor(),wood,Vector3.zero);floor.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  var well=Part(root,"RecessedCup",ToyBoxGeometry.Ring(.55f,.035f,.36f),teal,new Vector3(3.1f,0,-.405f));well.transform.localRotation=Quaternion.Euler(-90,0,0);
  var bottom=Part(root,"CupBottom",ToyBoxGeometry.Disc(.535f,.035f,.01f),teal,new Vector3(3.1f,0,-.035f));bottom.transform.localRotation=Quaternion.Euler(90,0,0);
  var rim=Part(root,"CupRim",ToyBoxGeometry.Torus(.56f,.035f),teal,new Vector3(3.1f,0,-.415f));rim.transform.localRotation=Quaternion.Euler(90,0,0);
  b.shootButton.localPosition=new Vector3(3.1f,-1.22f,-.53f);b.shootButton.localScale=Vector3.one*.78f;
  for(int i=0;i<b.chargeDots.Length;i++){float a=Mathf.Lerp(15,165,i/11f)*Mathf.Deg2Rad;b.chargeDots[i].transform.localPosition=new Vector3(3.1f+Mathf.Cos(a)*.46f,-1.22f+Mathf.Sin(a)*.46f,-.49f);b.chargeDots[i].transform.localScale=Vector3.one*.65f;}
  var badge=Part(root,"WoodBrandBadge",ToyBoxGeometry.RoundedBox(new Vector3(2.8f,.12f,.55f),.05f),wood,new Vector3(-.2f,-2.21f,-.08f));
  root.Find("Brand").localPosition=new Vector3(-.2f,-2.28f,-.08f);root.Find("Brand").GetComponent<TMPro.TMP_Text>().color=new Color(.18f,.08f,.025f);root.Find("Brand").gameObject.SetActive(false);
  var letters=Part(root,"CarvedBrand",ToyBoxGeometry.Text("BUCA",.025f),b.dim,new Vector3(-.2f,-2.295f,-.22f));letters.transform.localRotation=Quaternion.Euler(-90,0,0);letters.transform.localScale=Vector3.one*.34f;
  var liquid=root.Find("Liquid");liquid.localPosition=new Vector3(0,0,-.42f);liquid.localScale=new Vector3(1,1,.65f);
  var amber=liquid.GetComponent<Renderer>().sharedMaterial;amber.SetColor("_BaseColor",new Color(1,.48f,.025f,.72f));amber.SetFloat("_Smoothness",.94f);amber.EnableKeyword("_EMISSION");amber.SetColor("_EmissionColor",new Color(.19f,.065f,.002f));EditorUtility.SetDirty(amber);
  var gold=b.demoPuck.GetComponent<Renderer>().sharedMaterial;gold.SetColor("_BaseColor",new Color(.83f,.49f,.10f));gold.SetFloat("_Metallic",.58f);gold.SetFloat("_Smoothness",.58f);EditorUtility.SetDirty(gold);
  var pour=b.pouringDipper;var syrup=b.pourStream.GetComponent<Renderer>().sharedMaterial;syrup.SetColor("_BaseColor",new Color(1,.48f,.035f));syrup.SetFloat("_Metallic",0);syrup.SetFloat("_Smoothness",.86f);syrup.EnableKeyword("_EMISSION");syrup.SetColor("_EmissionColor",new Color(.38f,.15f,.008f));EditorUtility.SetDirty(syrup);
  for(int i=0;i<6;i++){var head=pour.Find("Head"+i);head.GetComponent<Renderer>().sharedMaterial=syrup;}
  b.pourStream.localScale=new Vector3(1.4f,1,1.4f);
  var camera=stage.GetComponentInChildren<Camera>(true);camera.transform.localPosition=new Vector3(2.8f,-11,-12);camera.transform.LookAt(stage.TransformPoint(new Vector3(0,.1f,0)));camera.orthographicSize=3.55f;
  var fill=stage.Find("HoneyFill").GetComponent<Light>();fill.transform.localPosition=new Vector3(0,-5,-4);fill.intensity=5;
  var front=stage.Find("SoftFrontFill");if(!front){front=new GameObject("SoftFrontFill",typeof(Light)).transform;front.SetParent(stage,false);}front.localPosition=new Vector3(-2,-6,-7);var fl=front.GetComponent<Light>();fl.type=LightType.Point;fl.color=new Color(.85f,.94f,1);fl.range=24;fl.intensity=13;fl.cullingMask=1<<30;fl.shadows=LightShadows.None;
  EditorUtility.SetDirty(b);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);return "Rounded tray, maple cutout, real cup and inset controls saved";
 }
}
#endif
