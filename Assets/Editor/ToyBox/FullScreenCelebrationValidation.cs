#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class FullScreenCelebrationValidation
{
    const string Key="FullScreenCelebrationValidation";
    static ToyBoxResults3D view;
    static float began;
    static int phase,callbacks;
    static bool testing;
    static FullScreenCelebrationValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));BootFromMainMenu.SetEnabled(false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var go=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));go.tag="MainCamera";
        go.transform.position=new Vector3(0,9,-12);go.transform.LookAt(Vector3.zero);
        go.GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing=false;
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FullScreenResultsCelebrationBaker.PrefabPath));
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","running");phase=0;callbacks=0;
        EditorApplication.EnterPlaymode();return "running";
    }
    static ScoreCalculator.ScoreBreakdown Score(int shots=1) => new ScoreCalculator.ScoreBreakdown{basePoints=500,timeBonus=200,total=1000,stars=3,strokesUsed=shots};
    static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||testing)return;
        testing=true;
        try
        {
            Time.timeScale=0;
            if(phase==0)
            {
                view=UnityEngine.Object.FindAnyObjectByType<ToyBoxResults3D>();
                began=Time.unscaledTime;phase=1;
            }
            else if(phase==1 && Time.unscaledTime-began>.25f)
            {
                view.Show(Score(),()=>callbacks++);began=Time.unscaledTime;phase=2;
            }
            else if(phase==2 && Time.unscaledTime-began>.95f)
            {
                Check(view.IsShowing && view.Remaining==5,"Countdown did not wait for score entrance");
                Check(view.cheerSource.isPlaying && view.holeInOneFanfare!=null,"Fanfare did not play");
                Check(view.celebrationRays.Count(r=>r.gameObject.activeSelf)==80,"Missing firework bursts while timeScale is zero");
                foreach(float aspect in new[]{16f/9,21f/9,9f/16})
                {
                    view.resultsCamera.aspect=aspect;view.Fit();view.SampleCelebration(1.1f);
                    var points=view.celebrationStars.Where(p=>p.gameObject.activeSelf).Select(p=>view.resultsCamera.WorldToViewportPoint(p.position)).ToArray();
                    Check(points.Min(p=>p.x)<.1f && points.Max(p=>p.x)>.9f,"Confetti failed to span viewport at "+aspect);
                    var center=view.resultsCamera.WorldToViewportPoint(view.leftBoard.position);
                    Check(points.All(p=>p.z>center.z),"Confetti could obscure score board");
                }
                view.resultsCamera.ResetAspect();view.Fit();phase=3;
            }
            else if(phase==3 && Time.unscaledTime-began>2f)
            {
                Check(view.scoreValues[4].text=="1,000","Score count did not finish");
                view.ToggleScorecard();view.Tick(10);
                Check(view.ScorecardShowing && view.Remaining==5 && callbacks==0,"Scorecard did not pause result countdown");
                Check(!view.cheerSource.isPlaying && view.celebrationStars.All(p=>!p.gameObject.activeSelf) && view.celebrationRays.All(p=>!p.gameObject.activeSelf),"Scorecard retained celebration");
                view.ToggleScorecard();view.Tick(.1f);
                Check(view.celebrationStars.All(p=>!p.gameObject.activeSelf),"Returning from scorecard replayed celebration");
                view.PressContinue();view.Tick(.17f);
                Check(!view.IsShowing && callbacks==1 && Camera.main.enabled,"Continue failed to restore camera or callback");
                view.Show(Score(3),()=>callbacks++);view.Tick(.9f);
                Check(view.celebrationRays.All(p=>!p.gameObject.activeSelf),"Normal win played hole-in-one fireworks");
                Check(view.ShowingWellDone && view.wellDone.gameObject.activeInHierarchy && !view.leftBoard.gameObject.activeSelf,"Normal win did not select wooden ribbon");
                Check(view.wellDone.shotsLabel.source.text=="3 SHOTS" && view.wellDone.confetti.Count(p=>p.gameObject.activeSelf)==30,"Normal win shot count or light confetti incorrect");
                Check(view.cheerSource.isPlaying,"Normal win lost the kids cheer");
                view.Tick(.9f);
                Check(view.wellDone.scoreDigits.Count(p=>p.gameObject.activeSelf)==4 && view.wellDone.scoreDigits[0].sharedMesh==view.wellDone.digitMeshes[1],"Wooden total did not count to 1000");
                foreach(var mesh in view.wellDone.GetComponentsInChildren<MeshFilter>(true))
                    if(mesh.GetComponent<TMPro.TMP_Text>()==null)Check(AssetDatabase.Contains(mesh.sharedMesh),"Unsaved Well Done mesh");
                foreach(float aspect in new[]{16f/9,9f/16})
                {
                    view.resultsCamera.aspect=aspect;view.Fit();Physics.SyncTransforms();
                    var point=view.resultsCamera.WorldToViewportPoint(view.ActiveNextHit.bounds.center);
                    Check(point.x>0 && point.x<1 && point.y>0 && point.y<1 && view.ActiveNextHit.Raycast(view.resultsCamera.ViewportPointToRay(point),out var hit,100),"Wooden Next not clickable at "+aspect);
                }
                view.resultsCamera.ResetAspect();view.Fit();
                view.ToggleScorecard();Check(!view.wellDone.gameObject.activeInHierarchy,"Ribbon overlapped My Shots");
                view.ToggleScorecard();Check(view.wellDone.gameObject.activeInHierarchy && !view.leftBoard.gameObject.activeSelf,"Returning from My Shots lost normal result layout");
                view.PressContinue();view.Tick(.17f);Check(!view.IsShowing && callbacks==2,"Wooden Next did not advance exactly once");
                view.Hide(false);Check(!view.cheerSource.isPlaying,"Interrupted audio remains");
                view.Show(Score(),()=>callbacks++);began=Time.unscaledTime;phase=4;
            }
            else if(phase==4 && Time.unscaledTime-began>5.5f)
            {
                Check(view.IsShowing && callbacks==2,"Premature auto-advance");
                Check(!view.cheerSource.isPlaying && view.celebrationStars.All(p=>!p.gameObject.activeSelf) && view.celebrationRays.All(p=>!p.gameObject.activeSelf),"Effect failed to expire");
                // Advance the remaining countdown explicitly so Editor focus cannot interrupt this final check.
                view.Tick(2f);
                Check(!view.IsShowing && callbacks==3 && Camera.main.enabled,"Auto-advance did not restore gameplay exactly once");
                SessionState.SetString(Key+"Result","PASS: wooden normal-win ribbon, saved meshes, live shots/score, light confetti and kids cheer, Next raycasts in landscape/portrait, My Shots return, hole-in-one fireworks/audio, countdown and cleanup.");
                phase=6;Time.timeScale=1;EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception e){SessionState.SetString(Key+"Result","FAIL: "+e.Message);phase=6;Time.timeScale=1;EditorApplication.ExitPlaymode();}
        finally{testing=false;}
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        if(SessionState.GetString(Key+"Result","")=="running")
            SessionState.SetString(Key+"Result","INTERRUPTED: Play mode ended before validation completed (phase "+phase+").");
        SessionState.SetBool(Key,false);BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));
        EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
    }
}
#endif
