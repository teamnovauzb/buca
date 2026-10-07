#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class WoodenCelebrationValidation
{
    const string Key="WoodenCelebrationValidation";
    static ToyBoxWinCelebration view;static float began;static int phase;
    static WoodenCelebrationValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));BootFromMainMenu.SetEnabled(false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var go=new GameObject("Main Camera",typeof(Camera));go.AddComponent<AudioListener>();go.tag="MainCamera";go.transform.position=new Vector3(0,9,-12);go.transform.LookAt(Vector3.zero);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Prefabs/PremiumWinCelebration.prefab"));
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","running");phase=0;EditorApplication.EnterPlaymode();return "running";
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try
        {
            Time.timeScale=0;
            if(phase==0){view=UnityEngine.Object.FindAnyObjectByType<ToyBoxWinCelebration>();view.Play(Vector3.zero,true);began=Time.unscaledTime;phase=1;}
            else if(phase==1&&Time.unscaledTime-began>.8f)
            {
                Check(view.IsPlaying&&view.woodenBanner.gameObject.activeInHierarchy,"Banner missing");
                Check(!view.goldRing.gameObject.activeSelf,"Legacy effect overlaps banner");
                Check(view.celebrationEffects.Length==9,"Missing fountains or confetti");
                foreach(var ps in view.celebrationEffects)if(ps.name.StartsWith("Firework")||ps.name.StartsWith("FallingConfetti"))Check(ps.particleCount>0,"No particles from "+ps.name);
                Check(view.fireworkBoom!=null&&view.fireworkAudio.isPlaying,"Boom audio missing or not playing");
                Check(Mathf.Abs(view.Duration-2.85f)<.01f,"Wrong duration");phase=2;
            }
            else if(phase==2&&Time.unscaledTime-began>3.05f)
            {
                Check(!view.IsPlaying&&!view.presentation.gameObject.activeSelf&&!view.fireworkAudio.isPlaying,"Effect or audio did not clear");
                view.Play(Vector3.zero,false);began=Time.unscaledTime;phase=3;
            }
            else if(phase==3&&Time.unscaledTime-began>.3f)
            {
                Check(!view.woodenBanner.gameObject.activeSelf&&!view.fountainRoot.activeSelf,"Ordinary win shows special banner");
                Check(view.goldRing.gameObject.activeSelf&&!view.fireworkAudio.isPlaying,"Ordinary win effect or audio incorrect");
                view.Hide();view.Play(Vector3.zero,true);view.enabled=false;
                Check(!view.IsPlaying&&!view.presentation.gameObject.activeSelf,"Disable left visuals active");
                SessionState.SetString(Key+"Result","PASS: hole-in-one banner, two fireworks with synced boom audio, nine particle systems, 2.85s unscaled playback, cleanup, normal-win separation, interrupted cleanup.");
                Time.timeScale=1;EditorApplication.ExitPlaymode();phase=4;
            }
        }
        catch(Exception e){SessionState.SetString(Key+"Result","FAIL: "+e.Message);Time.timeScale=1;EditorApplication.ExitPlaymode();phase=4;}
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
    }
}
#endif
