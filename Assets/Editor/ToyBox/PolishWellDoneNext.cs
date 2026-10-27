using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/Apply Well Done Button Option C")]
    public static void ApplyWellDoneOptionC()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        const string path="Assets/ToyBoxMenu/Prefabs/ResultsAndLeaderboard.prefab";
        Directory.CreateDirectory("output/well-done-button");
        if(!File.Exists("output/well-done-button/before.prefab"))File.Copy(path,"output/well-done-button/before.prefab");
        PrepareFolders();ToyBoxGeometry.Initialize();MakeMaterials();
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var results=root.GetComponent<ToyBoxResults3D>();var view=results.wellDone;
            var maple=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/WellDone/WarmMaple.mat");
            if(maple==null)throw new Exception("Missing saved maple material.");
            for(int i=view.nextButton.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(view.nextButton.GetChild(i).gameObject);
            var gold=NextMaterial("WellDoneNextGold",new Color(.82f,.53f,.16f),.62f,.5f);
            var green=NextMaterial("WellDoneNextEmerald",new Color(.018f,.48f,.19f),.08f,.48f);
            var dark=NextMaterial("WellDoneNextDark",new Color(.014f,.055f,.041f),.1f,.35f);
            var ivory=NextMaterial("WellDoneNextCream",new Color(1f,.91f,.70f),.04f,.35f);
            PodiumSlab("CMapleSocket",view.nextButton,Vector3.zero,5.9f,1.36f,.45f,.38f,maple);
            PodiumSlab("CShadowInset",view.nextButton,new Vector3(0,0,-.25f),5.64f,1.17f,.14f,.33f,dark);
            PodiumSlab("CGoldRim",view.nextButton,new Vector3(0,0,-.34f),5.52f,1.10f,.13f,.30f,gold);
            view.nextCap=Group("EmeraldNextCap",view.nextButton,new Vector3(0,0,-.43f));view.capRest=view.nextCap.localPosition;
            PodiumSlab("CEmeraldFace",view.nextCap,Vector3.zero,5.35f,.94f,.22f,.27f,green);
            var title=Text("NEXT LEVEL",view.nextCap,new Vector3(.02f,0,-.16f),.37f,ivory,false,.16f,.018f);
            var b=title.GetComponent<MeshFilter>().sharedMesh.bounds;float scale=Mathf.Min(.37f,3.62f/b.size.x);
            title.transform.localScale=Vector3.one*scale;title.transform.localPosition-=new Vector3(b.center.x,b.center.y,0)*scale;
            var badge=Group("InputButton1",view.nextCap,new Vector3(-2.19f,0,-.17f));
            var rim=Disc("GoldRim",badge,Vector3.zero,.33f,.10f,gold,.025f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
            var disc=Disc("BlackInput",badge,new Vector3(0,0,-.06f),.281f,.09f,dark,.025f);disc.transform.localRotation=Quaternion.Euler(90,0,0);
            Text("1",badge,new Vector3(0,0,-.13f),.39f,ivory,false,.14f,.012f);
            var arrow=Group("CreamChevron",view.nextCap,new Vector3(2.22f,0,-.18f));
            for(int i=0;i<2;i++)
            {
                var arm=PodiumSlab("CChevron"+i,arrow,new Vector3(0,i==0?.12f:-.12f,0),.17f,.41f,.10f,.07f,ivory);
                arm.transform.localRotation=Quaternion.Euler(0,0,i==0?42:-42);
            }
            var hit=view.nextCap.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(5.35f,.94f,.30f);view.nextHit=hit;
            // Keep the existing auto-advance clock without adding a new visual to the approved design.
            view.countdownFill=Group("CountdownState",view.nextButton);view.countdownFill.gameObject.SetActive(false);
            results.solidLabels=results.presentation.GetComponentsInChildren<ToyBoxPodiumLabel>(true);
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        ValidateWellDoneOptionC();
    }
    public static void ValidateWellDoneOptionC()
    {
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var existing=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");
        if(existing.isLoaded)throw new Exception("Close the Game scene before isolated preview validation.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        ToyBoxResults3D results=null;
        try
        {
            foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<ToyBoxResults3D>()!=null)results=root.GetComponent<ToyBoxResults3D>();
            var score=new ScoreCalculator.ScoreBreakdown{total=909,stars=2,strokesUsed=2};bool done=false;
            results.Show(score,()=>done=true);results.Tick(2);
            if(!results.ShowingWellDone)throw new Exception("Expected Well Done view.");
            results.resultsCamera.aspect=16f/9f;results.Fit();NextCapture(results.resultsCamera,"output/well-done-button/landscape.png",1920,1080);
            results.resultsCamera.aspect=9f/16f;results.Fit();NextCapture(results.resultsCamera,"output/well-done-button/portrait.png",900,1600);
            var view=results.wellDone;
            results.resultsCamera.transform.position=view.nextButton.position+new Vector3(0,.15f,-5);
            results.resultsCamera.transform.LookAt(view.nextButton.position);results.resultsCamera.fieldOfView=33;
            NextCapture(results.resultsCamera,"output/well-done-button/button.png",1500,450);
            Physics.SyncTransforms();
            if(!view.nextHit.Raycast(results.resultsCamera.ScreenPointToRay(results.resultsCamera.WorldToScreenPoint(view.nextHit.bounds.center)),out var hit,100))throw new Exception("Button raycast failed.");
            results.PressContinue();if(view.nextCap.localPosition!=view.capRest+Vector3.forward*.08f)throw new Exception("Press animation failed.");
            results.Tick(.17f);if(!done || results.IsShowing)throw new Exception("Manual next failed.");
            done=false;results.Show(score,()=>done=true);results.Tick(10);if(!done || results.IsShowing)throw new Exception("Auto advance failed.");
            File.WriteAllText("output/well-done-button/validation.txt","PASS: Option C saved and inherited by Game scene. Landscape/portrait rendered. Mouse target, press animation, manual advance, automatic advance verified.");
        }
        finally{if(results!=null)results.Hide(false);EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
}
