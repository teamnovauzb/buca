using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/Apply Next Button Option A")]
    public static void ApplyNextOptionA()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        const string path="Assets/ToyBoxMenu/Prefabs/ResultsAndLeaderboard.prefab";
        Directory.CreateDirectory("output/next-control");
        if(!File.Exists("output/next-control/ResultsAndLeaderboard.before.prefab"))File.Copy(path,"output/next-control/ResultsAndLeaderboard.before.prefab");
        PrepareFolders();ToyBoxGeometry.Initialize();MakeMaterials();
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view=root.GetComponent<ToyBoxResults3D>();
            var font=view.countdownText.font;
            var maple=view.podiumBase.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            for(int i=view.nextRoot.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(view.nextRoot.GetChild(i).gameObject);
            var gold=NextMaterial("NextGold",new Color(.82f,.53f,.16f),.62f,.50f);
            var teal=NextMaterial("NextTeal",new Color(.018f,.24f,.23f),.20f,.40f);
            var dark=NextMaterial("NextDial",new Color(.018f,.025f,.028f),.12f,.32f);
            var ivory=NextMaterial("NextIvory",new Color(1f,.92f,.70f),.05f,.30f);
            var face=Group("OptionAFrame",view.nextRoot);face.localRotation=Quaternion.Euler(90,0,0);
            PodiumSlab("OptionAMapleSocket",face,Vector3.zero,6.75f,1.18f,.22f,.35f,maple);
            PodiumSlab("OptionAButtonShadow",face,new Vector3(0,0,-.14f),4.67f,1.04f,.12f,.30f,dark);
            view.selectedRing=PodiumSlab("OptionAGoldRim",face,new Vector3(0,0,-.22f),4.57f,.97f,.12f,.28f,gold);
            view.nextCap=Group("OptionATealCap",view.nextRoot,new Vector3(0,.30f,0));view.nextRest=view.nextCap.localPosition;
            var capFace=Group("Face",view.nextCap);capFace.localRotation=Quaternion.Euler(90,0,0);
            var cap=PodiumSlab("OptionATealButton",capFace,Vector3.zero,4.39f,.81f,.19f,.24f,teal);
            var hit=cap.AddComponent<BoxCollider>();hit.size=new Vector3(4.39f,.81f,.19f);view.nextHit=hit;
            var title=Text("NEXT LEVEL",capFace,new Vector3(0,0,-.12f),.39f,ivory,false,.16f,.018f);
            var bounds=title.GetComponent<MeshFilter>().sharedMesh.bounds;
            float scale=Mathf.Min(.39f,3.92f/bounds.size.x);title.transform.localScale=Vector3.one*scale;
            title.transform.localPosition-=new Vector3(bounds.center.x,bounds.center.y,0)*scale;
            foreach(float x in new[]{-2.86f,2.86f})
            {
                float y=x<0?0:-.12f;
                var dial=Group(x<0?"InputBadge":"CountdownDial",face,new Vector3(x,y,-.22f));
                var rim=Disc("GoldRim",dial,Vector3.zero,.39f,.12f,gold,.035f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
                var disc=Disc("DarkFace",dial,new Vector3(0,0,-.08f),.325f,.10f,dark,.025f);disc.transform.localRotation=Quaternion.Euler(90,0,0);
                if(x<0)Text("1",dial,new Vector3(0,0,-.15f),.43f,ivory,false,.16f,.015f);
                else
                {
                    Text("NEXT IN",face,new Vector3(x,.40f,-.15f),.12f,ink,false,.12f,.01f);
                    view.countdownText=ResultText("NextCountdown",dial,new Vector3(0,0,-.16f),new Vector2(.55f,.57f),font,5.1f,ivory.color,TextAlignmentOptions.Center);
                    foreach(var renderer in view.countdownText.GetComponentsInChildren<MeshRenderer>(true))if(renderer.GetComponent<TMP_Text>()==null)renderer.sharedMaterial=ivory;
                    view.countdownText.text="3";view.countdownText.GetComponent<ToyBoxPodiumLabel>().Refresh();
                }
            }
            view.countdownNumberOnly=true;
            view.solidLabels=view.presentation.GetComponentsInChildren<ToyBoxPodiumLabel>(true);
            PrefabUtility.SaveAsPrefabAsset(root,path);
            AssetDatabase.SaveAssets();
            // Inspect the actual saved geometry in isolation without changing the open game scene.
            view.presentation.SetActive(true);view.leftBoard.gameObject.SetActive(true);view.nextRoot.gameObject.SetActive(true);view.podiumBase.gameObject.SetActive(true);
            if(view.wellDone!=null)view.wellDone.gameObject.SetActive(false);
            view.resultsCamera.transform.position=view.nextRoot.position+new Vector3(0,.55f,-8);
            view.resultsCamera.transform.LookAt(view.nextRoot.position);
            view.resultsCamera.fieldOfView=36;
            view.resultsCamera.scene=root.scene;
            NextCapture(view.resultsCamera,"output/next-control/option-a.png",1600,650);
            Physics.SyncTransforms();
            if(!view.nextHit.Raycast(view.resultsCamera.ScreenPointToRay(view.resultsCamera.WorldToScreenPoint(view.nextHit.bounds.center)),out var result,100))throw new Exception("Next button hit target not reachable.");
            File.WriteAllText("output/next-control/applied.txt","Option A saved; gold-rimmed teal control, input 1 badge, separate countdown; collider raycast passed.");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        ValidateNextOptionA();
        Debug.Log("NEXT_OPTION_A_SAVED");
    }
    public static void ValidateNextOptionA()
    {
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        ToyBoxResults3D view=null;
        try
        {
            foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<ToyBoxResults3D>()!=null)view=root.GetComponent<ToyBoxResults3D>();
            if(view==null || !view.countdownNumberOnly)throw new Exception("Game scene has not inherited option A.");
            bool done=false;
            var score=new ScoreCalculator.ScoreBreakdown{basePoints=500,timeBonus=120,railBonus=0,strokeBonus=75,total=695,stars=3,strokesUsed=1};
            view.Show(score,()=>done=true);view.Tick(2);
            view.resultsCamera.aspect=16f/9f;view.Fit();
            NextCapture(view.resultsCamera,"output/next-control/full-results.png",1920,1080);
            view.resultsCamera.aspect=9f/16f;view.Fit();
            NextCapture(view.resultsCamera,"output/next-control/portrait-results.png",900,1600);
            view.Tick(1);
            if(view.countdownText.text.Contains("IN"))throw new Exception("Countdown is not numeric.");
            view.resultsCamera.transform.position=view.nextRoot.position+new Vector3(0,.55f,-5.2f);
            view.resultsCamera.transform.LookAt(view.nextRoot.position);view.resultsCamera.fieldOfView=36;
            NextCapture(view.resultsCamera,"output/next-control/option-a.png",1600,650);
            view.PressContinue();view.Tick(.17f);if(!done || view.IsShowing)throw new Exception("Manual advance failed.");
            done=false;view.Show(score,()=>done=true);view.Tick(10);if(!done || view.IsShowing)throw new Exception("Automatic advance failed.");
            File.WriteAllText("output/next-control/validation.txt","PASS: Game scene inherits Option A; manual advance and automatic countdown complete; landscape and portrait rendered.");
        }
        finally{if(view!=null)view.Hide(false);EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
    static Material NextMaterial(string name,Color color,float metal,float smooth)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
        material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metal);material.SetFloat("_Smoothness",smooth);
        return ToyBoxGeometry.Save(material,Root+"/Materials/"+name+".mat");
    }
    static void NextCapture(Camera camera,string path,int width,int height)
    {
        var target=new RenderTexture(width,height,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.aspect=(float)width/height;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
