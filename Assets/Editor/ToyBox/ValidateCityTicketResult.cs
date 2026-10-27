using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
    public static string ValidateCityTicketResult()
    {
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var oldManager=LevelManager.Instance;
        var prop=typeof(LevelManager).GetProperty("Instance",BindingFlags.Public|BindingFlags.Static);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        ToyBoxResults3D results=null;
        var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene!=scene && c.CompareTag("MainCamera")).ToArray();
        try
        {
            foreach(var c in cameras)c.tag="Untagged";
            var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LevelManager>(true)).First();
            prop.SetValue(null,manager);
            var index=typeof(LevelManager).GetField("_currentIndex",BindingFlags.NonPublic|BindingFlags.Instance);
            results=manager.levelCompletePanel.premiumResults;
            var level=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[30],scene);
            var theme=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EveningGameplayTheme>(true)).First();theme.Apply(true,level);
            var score=new ScoreCalculator.ScoreBreakdown{total=1032,stars=2,strokesUsed=3};
            foreach(int n in new[]{1,30,31,50,1})
            {
                index.SetValue(manager,n-1);bool done=false;results.Show(score,()=>done=true);results.Tick(2);
                if(results.wellDone!=(n>30?results.worldTwoWellDone:results.worldOneWellDone))throw new Exception("Wrong theme for level "+n);
                if(n==31)
                {
                    results.resultsCamera.aspect=16f/9f;results.Fit();NextCapture(results.resultsCamera,"output/city-ticket/landscape.png",1920,1080);
                    results.resultsCamera.aspect=9f/16f;results.Fit();NextCapture(results.resultsCamera,"output/city-ticket/portrait.png",900,1600);
                    Physics.SyncTransforms();var hit=results.ActiveNextHit;
                    if(!hit.Raycast(results.resultsCamera.ScreenPointToRay(results.resultsCamera.WorldToScreenPoint(hit.bounds.center)),out var collision,100))throw new Exception("Next hit not visible");
                }
                results.PressContinue();results.Tick(.17f);if(!done)throw new Exception("Next failed");
            }
            index.SetValue(manager,30);bool auto=false;results.Show(score,()=>auto=true);results.Tick(10);if(!auto)throw new Exception("Countdown failed");
            File.WriteAllText("output/city-ticket/validation.txt","PASS: correct theme at levels 1,30,31,50 and return to 1; next button raycast/manual advance; automatic advance; landscape and portrait rendered.");
            return "City ticket validation passed";
        }
        finally{if(results!=null)results.Hide(false);prop.SetValue(null,oldManager);foreach(var c in cameras)if(c!=null)c.tag="MainCamera";EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
}
