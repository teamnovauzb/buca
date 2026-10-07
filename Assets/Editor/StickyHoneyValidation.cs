using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class StickyHoneyValidation
{
    const string Key="BucaStickyHoneyValidation";
    static double started;
    static int phase;
    static LevelManager manager;
    static ScorePickup star;
    static ToyBoxGameplayHud hud;
    static StickyHoneyValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    static void Set(string name,object value)=>typeof(LevelManager).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,value);
    public static void Run()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new Exception("Validation needs clean edit scene");
        phase=0;
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
        BootFromMainMenu.SetEnabled(false);
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)) component.enabled=false;
        var fx=UnityEngine.Object.FindAnyObjectByType<HeartRewardPresentation>(FindObjectsInactive.Include);fx.enabled=true;
        SessionState.SetBool(Key,true); SessionState.SetString(Key+"Result","running");
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false))
        {
            SessionState.SetBool(Key,false);BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));
            EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
        }
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static Rigidbody rb;static IcePatch patch;static float began;static int index;static int[] levels;static GameObject levelRoot;static string results;
    static void Tick() {
      if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
      try {
        Time.timeScale=1;
        if(phase==0) {
          manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
          if(levels==null){levels=Enumerable.Range(0,manager.levelPrefabs.Length).Where(i=>manager.levelPrefabs[i].GetComponentsInChildren<IcePatch>(true).Any(p=>p.patchDamping>1)).ToArray();index=0;results="";}
          levelRoot=UnityEngine.Object.Instantiate(manager.levelPrefabs[levels[index]]);
          patch=levelRoot.GetComponentsInChildren<IcePatch>().First(p=>p.patchDamping>1);
          rb=manager.puckRigidbody;manager.puck.transform.localScale=Vector3.one*manager.puckSize;
          rb.isKinematic=false;rb.useGravity=false;rb.linearDamping=.9f;
          rb.position=new Vector3(patch.transform.position.x,manager.puckSize*.5f,patch.transform.position.z);
          rb.linearVelocity=Vector3.right*3;rb.WakeUp();Physics.SyncTransforms();began=Time.fixedTime;phase=1;
        }else if(phase==1 && Time.fixedTime-began>.25f) {
          Check(rb.linearDamping==8f,"Honey trigger did not activate");
          Check(rb.linearVelocity.magnitude<.6f,"Honey not slow enough: "+rb.linearVelocity.magnitude);
          results+="Level "+(levels[index]+1)+": 3m/s slowed to "+rb.linearVelocity.magnitude.ToString("F2")+"m/s. ";
          rb.position+=Vector3.up*3;rb.linearVelocity=Vector3.zero;began=Time.fixedTime;phase=2;
        }else if(phase==2 && Time.fixedTime-began>.1f) {
          Check(Mathf.Abs(rb.linearDamping-.9f)<.001f,"Normal drag not restored after honey");
          UnityEngine.Object.Destroy(levelRoot);index++;
          if(index<levels.Length)phase=0;
          else{SessionState.SetString(Key+"Result","PASS: "+results+"Normal drag restored on every exit.");phase=3;EditorApplication.ExitPlaymode();}
        }
      }catch(Exception e){SessionState.SetString(Key+"Result","FAIL: "+e.Message);phase=3;EditorApplication.ExitPlaymode();}
    }
}
