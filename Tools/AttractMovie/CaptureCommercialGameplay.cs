// Ephemeral recording utility: run with Unity Pipeline run_script, outside Assets.
// Uses the real game, real input code and PhysX; does not replace any game asset.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class CaptureCommercialGameplay
{
    const string BackupKey = "BucaCommercial.PrefsBackup";
    public static string Main(string action)
    {
        if(action=="prepare")
        {
            if(EditorApplication.isPlaying) throw new Exception("Prepare requires Edit Mode.");
            var scene=SceneManager.GetActiveScene();
            if(scene.isDirty) throw new Exception("Current scene has unsaved changes; left untouched.");
            var values=new Dictionary<string,int?>();
            foreach(string key in new[]{"BucaCurrentLevel","BucaHighestLevel","BucaPendingLevel","BucaWatchTutorialV2_BASIC"})
                values[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null;
            for(int n=0;n<30;n++) foreach(string prefix in new[]{"BucaStars_L","BucaScore_L","BucaBestStrokes_L","BucaPar_L"})
            { var key=prefix+n;values[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null; }
            if(!string.IsNullOrEmpty(SessionState.GetString(BackupKey,""))) throw new Exception("A recording backup already exists; restore it first.");
            SessionState.SetString(BackupKey,JsonConvert.SerializeObject(values));
            SessionState.SetString("BucaCommercial.Scene",scene.path);
            SessionState.SetBool("BucaCommercial.Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
            BootFromMainMenu.SetEnabled(false);
            PlayerPrefs.SetInt("BucaPendingLevel",0);
            PlayerPrefs.SetInt("BucaWatchTutorialV2_BASIC",1);
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Single);
            return "Prepared Game scene; original editor settings and progress backed up.";
        }
        if(action=="record")
        {
            if(!EditorApplication.isPlaying || LevelManager.Instance==null) throw new Exception("Game must be running.");
            if(UnityEngine.Object.FindAnyObjectByType<MallGameplayRecorder>()!=null) throw new Exception("Recording already running.");
            var manager=LevelManager.Instance;
            Application.runInBackground=true;
            EditorApplication.isPaused=false;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.QueuePlayerLoopUpdate();
            manager.luxoddBridge=null;
            manager.LoadLevel(0);
            new GameObject("Temporary Commercial Capture").AddComponent<MallGameplayRecorder>();
            return "Recording real level 1 aim, charge, launch and result at 1920x1080 / 30 fps.";
        }
        if(action=="restore")
        {
            if(EditorApplication.isPlaying) throw new Exception("Stop Play Mode before restoring.");
            string raw=SessionState.GetString(BackupKey,"");
            if(string.IsNullOrEmpty(raw)) throw new Exception("No saved recording state.");
            foreach(var item in JsonConvert.DeserializeObject<Dictionary<string,int?>>(raw))
                if(item.Value.HasValue)PlayerPrefs.SetInt(item.Key,item.Value.Value);else PlayerPrefs.DeleteKey(item.Key);
            PlayerPrefs.Save();
            BootFromMainMenu.SetEnabled(SessionState.GetBool("BucaCommercial.Boot",true));
            EditorSceneManager.OpenScene(SessionState.GetString("BucaCommercial.Scene","Assets/Scenes/MainMenu.unity"),OpenSceneMode.Single);
            SessionState.EraseString(BackupKey);
            return "Restored original scene, progress and Play Mode startup preference.";
        }
        throw new Exception("Action must be prepare, record or restore.");
    }
}

[DefaultExecutionOrder(-50)]
public sealed class MallGameplayRecorder:MonoBehaviour
{
    const string Folder="output/buca-mall-film/gameplay";
    static readonly PropertyInfo Stick=typeof(ToyBoxGameplayHud).GetProperty("PanelStick");
    static readonly PropertyInfo Shoot=typeof(ToyBoxGameplayHud).GetProperty("PanelShoot");
    static readonly PropertyInfo Active=typeof(ToyBoxGameplayHud).GetProperty("PanelInputActive");
    RenderTexture target;
    Texture2D pixels;
    Camera camera;
    int frame,oldRate;
    float oldAspect;
    bool finished;
    IEnumerator Start()
    {
        Directory.CreateDirectory(Folder+"/frames");
        oldRate=Time.captureFramerate;Time.captureFramerate=30;
        camera=Camera.main;oldAspect=camera.aspect;camera.aspect=16f/9f;
        target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();
        pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        while(frame<480)
        {
            yield return new WaitForEndOfFrame();
            var results=UnityEngine.Object.FindAnyObjectByType<ToyBoxResults3D>();
            var current=results!=null && results.IsShowing ? results.resultsCamera : camera;
            current.aspect=16f/9f;
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            RenderPipeline.SubmitRenderRequest(current,request);
            var previous=RenderTexture.active;RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Folder+"/frames/"+frame.ToString("D4")+".jpg",pixels.EncodeToJPG(95));
            if(frame%30==0) File.WriteAllText(Folder+"/progress.txt",frame+" / 480; level="+LevelManager.Instance.CurrentLevelNumber+"; puck="+LevelManager.Instance.puck.transform.position);
            frame++;
        }
        finished=true;
        File.WriteAllText(Folder+"/done.txt","480 frames, 1920x1080, 30 fps. Verify actual goal entry in picture before editing.");
        Cleanup();
        EditorApplication.isPaused=true;
    }
    void Update()
    {
        if(finished)return;
        float t=frame/30f;
        // Select a gentle initial aim, then build power through the actual Shoot input.
        Vector2 stick=t>=.5f && t<3f ? new Vector2(Mathf.Lerp(.10f,0,Mathf.Clamp01((t-.5f)/1.5f)),-.30f):Vector2.zero;
        bool held=t>=3f && t<4.066667f;
        Active.SetValue(null,true);Stick.SetValue(null,stick);Shoot.SetValue(null,held);
    }
    void OnDestroy(){Cleanup();}
    void Cleanup()
    {
        Time.captureFramerate=oldRate;
        Active.SetValue(null,false);Stick.SetValue(null,Vector2.zero);Shoot.SetValue(null,false);
        if(camera!=null)camera.aspect=oldAspect;
        if(target!=null){target.Release();Destroy(target);target=null;}
        if(pixels!=null){Destroy(pixels);pixels=null;}
    }
}
