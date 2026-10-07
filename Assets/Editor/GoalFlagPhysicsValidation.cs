#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Real Play mode contact checks, with restoration of the user's scene and boot preference.</summary>
[InitializeOnLoad]
public static class GoalFlagPhysicsValidation
{
    const string Key="BucaGoalFlagPhysicsCheck";
    static OrbitingGoalFlag flag;
    static Rigidbody puck;
    static int stage,index,hits;
    static float started;
    static Vector3 away,pausedPosition;
    static string report;
    static GoalFlagPhysicsValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit scene required.");
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
        BootFromMainMenu.SetEnabled(false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var goal=new GameObject("TestGoal");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/CreamGoalFlag/CreamGoalFlag.prefab"));
        instance.GetComponent<OrbitingGoalFlag>().goal=goal.transform;
        instance.transform.position=new Vector3(-.65f,.008f,.4f);
        var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.name="TestPuck";ball.transform.localScale=Vector3.one*.46f;
        var rb=ball.AddComponent<Rigidbody>();rb.useGravity=false;rb.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        ball.AddComponent<TutorialPracticePuck>();
        ball.transform.position=new Vector3(0,.23f,-5);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Editor/GoalFlagPhysicsFixture.unity");
        stage=0;flag=null;index=0;report="";
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","running");
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        SessionState.SetString(Key+"State",state.ToString());
        if(state!=PlayModeStateChange.EnteredEditMode)return;
        SessionState.SetBool(Key,false);BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));
        EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
        AssetDatabase.DeleteAsset("Assets/Editor/GoalFlagPhysicsFixture.unity");
    }
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static void Finish(string text)
    {
        BoardMechanicClock.SetPaused(false);Time.timeScale=1;
        SessionState.SetString(Key+"Result",text);Directory.CreateDirectory("output/goal-flag");File.WriteAllText("output/goal-flag/physics-validation.txt",text);
        stage=99;EditorApplication.ExitPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||stage==99)return;
        try
        {
            if(flag==null){Time.timeScale=1;BoardMechanicClock.SetPaused(false);flag=UnityEngine.Object.FindAnyObjectByType<OrbitingGoalFlag>();puck=UnityEngine.Object.FindAnyObjectByType<TutorialPracticePuck>().GetComponent<Rigidbody>();stage=0;index=0;report="";}
            if(stage==0)
            {
                flag.cycleSeconds=index==3?12:1000000;
                Vector3 pole=flag.GetPredictedPosition(BoardMechanicClock.Time);
                away=new Vector3(pole.x,0,pole.z).normalized;
                puck.position=pole+away*(index==0? .38f:.8f);puck.position=new Vector3(puck.position.x,.23f,puck.position.z);
                puck.linearVelocity=-away*new[]{.8f,4f,14f,4f}[index];puck.WakeUp();hits=flag.ImpactCount;
                started=Time.time;stage=1;
            }
            else if(stage==1&&flag.ImpactCount>hits)
            {
                Check(Vector3.Dot(puck.linearVelocity,away)>1,"Puck did not return away from pole");
                Check(puck.linearVelocity.magnitude<=8.05f,"Rebound exceeded speed cap");
                Check(Mathf.Abs(puck.linearVelocity.y)<.001f,"Flag lifted the puck");
                report+="PASS impact "+index+": speed "+puck.linearVelocity.magnitude.ToString("F2")+"; ";
                started=Time.time;stage=2;
            }
            else if(stage==1&&Time.time-started>2)throw new Exception("Missed pole collision in case "+index);
            else if(stage==2&&Time.time-started>.3f)
            {
                Check(flag.ImpactCount==hits+1,"Repeated hit/jitter");
                if(++index<4){stage=0;return;}
                puck.position=new Vector3(0,.23f,-5);puck.linearVelocity=Vector3.zero;
                // Orbit shape and a moving goal use the same prediction as the guide.
                float now=BoardMechanicClock.Time;
                Vector3 a=flag.GetPredictedPosition(now),b=flag.GetPredictedPosition(now+3);
                Check(Vector3.Distance(a,b)>.9f,"Flag did not orbit");
                var delta=new Vector3(1,0,.5f);flag.goal.position+=delta;
                Check((flag.GetPredictedPosition(now)-a-delta).magnitude<.001f,"Flag lost moving goal center");
                flag.goal.position-=delta;BoardMechanicClock.SetPaused(true);started=Time.time;stage=3;
            }
            else if(stage==3&&Time.time-started>.1f){pausedPosition=flag.GetComponent<Rigidbody>().position;started=Time.time;stage=4;}
            else if(stage==4&&Time.time-started>.3f)
            {
                Check(Vector3.Distance(pausedPosition,flag.GetComponent<Rigidbody>().position)<.001f,"Flag moves during tutorial pause");
                report+="PASS circular orbit, moving goal and paused clock. ";
                // Clear approach along a different angle must still reach the cup.
                Vector3 pole=flag.GetComponent<Rigidbody>().position;
                Vector3 side=Vector3.Cross(Vector3.up,new Vector3(pole.x,0,pole.z)).normalized;
                puck.position=side*1.5f+Vector3.up*.23f;puck.linearVelocity=-side*3;hits=flag.ImpactCount;started=Time.time;stage=5;
            }
            else if(stage==5&&Time.time-started>.46f)
            {
                Check(new Vector2(puck.position.x,puck.position.z).magnitude<.3f,"Clear shot failed to reach cup center");
                Check(flag.ImpactCount==hits,"Pole blocked a clear shot");
                Finish(report+"PASS unobstructed goal approach.");
            }
        }
        catch(Exception e){Finish("FAIL: "+e.Message+" "+report);}
    }
}
#endif
