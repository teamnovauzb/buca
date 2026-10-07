using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ConveyorValidation
{
    const string Key="BucaConveyorValidation";
    static double started;
    static int phase;
    static LevelManager manager;
    static ScorePickup star;
    static ToyBoxGameplayHud hud;
    static ConveyorValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    static void Set(string name,object value)=>typeof(LevelManager).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,value);
    public static void Run()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new Exception("Validation needs clean edit scene");
        phase=0;index=0;results="";
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
    static Rigidbody rb;static BucaWindZone pad;static Vector3 start;static float begun;static int index;static GameObject levelRoot;static string results;static readonly int[] levels={12,16,23,28};
    static void Tick() {
      if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
      try {
        Time.timeScale=1;
        if(phase==0) {
          manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
          var root=UnityEngine.Object.Instantiate(manager.levelPrefabs[levels[index]-1]);levelRoot=root;
          pad=root.GetComponentsInChildren<BucaWindZone>().First(p=>p.name.Contains("Conveyor"));
          rb=manager.puckRigidbody;manager.puck.transform.localScale=Vector3.one*manager.puckSize;
          rb.isKinematic=false;rb.useGravity=false;rb.linearDamping=.9f;
          start=new Vector3(pad.transform.position.x,manager.puckSize*.5f,pad.transform.position.z);
          rb.position=start;rb.linearVelocity=index%2==0?pad.transform.right*8f:-pad.transform.forward*8f;rb.WakeUp();Physics.SyncTransforms();
          SessionState.SetString(Key+"Bounds","puck="+rb.GetComponent<Collider>().bounds+" pad="+pad.GetComponent<Collider>().bounds);
          begun=Time.fixedTime;started=EditorApplication.timeSinceStartup;phase=1;
        } else if(phase==1 && Time.fixedTime-begun>.2f) {
          float travel=Vector3.Dot(rb.position-start,pad.transform.forward);
          Check(travel>.35f,"No conveyor push: travel="+travel+" "+SessionState.GetString(Key+"Bounds",""));
          Check(Mathf.Abs(Vector3.Dot(rb.linearVelocity,pad.transform.right))<.1f,"Belt failed to redirect incoming shot");
          begun=Time.fixedTime;phase=2;
        } else if(phase==2 && Time.fixedTime-begun>2f) {
          if(index==0)Check(rb.linearVelocity.magnitude<.05f,"Level 12 exit is still shaking: "+rb.linearVelocity);
          results+="Level "+levels[index]+": redirect passed, exit speed="+rb.linearVelocity.magnitude.ToString("F3")+". ";
          rb.position=start+Vector3.back*10;rb.linearVelocity=Vector3.zero;
          UnityEngine.Object.Destroy(levelRoot);index++;
          if(index<levels.Length)phase=0;
          else {SessionState.SetString(Key+"Result","PASS: "+results);phase=3;EditorApplication.ExitPlaymode();}
        } else if(EditorApplication.timeSinceStartup-started>12)throw new Exception("Physics timed out");
      }catch(Exception e){SessionState.SetString(Key+"Result","FAIL: "+e.Message);phase=2;EditorApplication.ExitPlaymode();}
    }
}
