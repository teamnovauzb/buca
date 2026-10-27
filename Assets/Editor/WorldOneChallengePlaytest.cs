#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Runtime smoke test against the actual playable boards, without changing progress.</summary>
[InitializeOnLoad]
public static class WorldOneChallengePlaytest
{
    const string Key="WorldOneChallengePlaytest";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int index=-1;
    static float began;
    static GameObject board;
    static LevelManager manager;
    static PuckController puck;
    static Rigidbody body;
    static readonly StringBuilder report=new StringBuilder();
    static WorldOneChallengePlaytest(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required.");
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Boot",EditorPrefs.GetBool("RealBuca.BootFromMainMenu."+Application.dataPath,true));
        BootFromMainMenu.SetEnabled(false);
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach(var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))component.enabled=false;
        foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))camera.enabled=false;
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","running");
        index=-1;report.Clear();EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);
        BootFromMainMenu.SetEnabled(SessionState.GetBool(Key+"Boot",true));
        EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene","Assets/Scenes/MainMenu.unity"));
    }
    static void Set(string field,object value)=>typeof(LevelManager).GetField(field,Private).SetValue(manager,value);
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||index==int.MaxValue)return;
        try
        {
            Time.timeScale=1;
            if(index<0)
            {
                manager=Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
                puck=manager.puck.GetComponent<PuckController>();body=puck.GetComponent<Rigidbody>();
                foreach(var collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(collider.attachedRigidbody!=body)collider.enabled=false;
                Set("_isTransitioning",true);Set("_levelFailed",true);
                index=0;Next();return;
            }
            if(Time.unscaledTime-began<1.7f)return;
            int level=WorldOneChallengeBalance.ChallengeLevels[index];
            foreach(var gate in board.GetComponentsInChildren<MovingWall>())
            {
                var rb=gate.GetComponent<Rigidbody>();
                Check(rb.isKinematic&&rb.interpolation==RigidbodyInterpolation.Interpolate,"Gate interpolation");
                Check(Vector3.Distance(rb.position,gate.GetPredictedPosition(BoardMechanicClock.Time))<.25f,"Gate pose disagrees with prediction");
            }
            foreach(var spinner in board.GetComponentsInChildren<RotatingWall>())
            {
                var rb=spinner.GetComponent<Rigidbody>();
                Check(rb.isKinematic&&rb.interpolation==RigidbodyInterpolation.Interpolate,"Spinner interpolation");
                Check(Quaternion.Angle(rb.rotation,spinner.GetPredictedRotation(BoardMechanicClock.Time))<7,"Spinner pose disagrees with prediction");
            }
            Check(float.IsFinite(body.position.x)&&float.IsFinite(body.position.z)&&float.IsFinite(body.linearVelocity.magnitude),"Non-finite puck physics");
            Check(Mathf.Abs(body.position.x)<5&&Mathf.Abs(body.position.z)<7.5f,"Puck escaped the board");
            report.AppendLine($"Level {level}: playable prefab loaded; full timer/hearts; trajectory preview; live puck shot; moving-wall pose agreement PASS.");
            index++;
            if(index<WorldOneChallengeBalance.ChallengeLevels.Length){Next();return;}
            Finish("PASS: 19 changed playable boards; existing timer/heart budgets; aim prediction; live puck motion; moving and rotating wall prediction agreement. This is a runtime smoke test, not a complete human playthrough.");
        }
        catch(Exception error){Finish("FAIL at level index "+index+": "+error);}
    }
    static void Next()
    {
        if(board!=null){board.SetActive(false);Object.Destroy(board);}
        int level=WorldOneChallengeBalance.ChallengeLevels[index];
        Check(AssetDatabase.GetAssetPath(manager.levelPrefabs[level-1])==WorldOneChallengeBalance.PathFor("Assets/ToyBoxMenu/Levels",level),"Playable prefab mismatch");
        board=Object.Instantiate(manager.levelPrefabs[level-1]);
        Set("_currentInstance",board);Set("_currentIndex",level-1);
        var settings=manager.levelSettings[level-1];
        float timer=(float)typeof(LevelManager).GetMethod("GetTimeLimitForLevel",Private).Invoke(manager,new object[]{level-1});
        int hearts=(int)typeof(LevelManager).GetMethod("GetMaxLivesForLevel",Private).Invoke(manager,new object[]{level-1});
        Check(timer==settings.timeLimit&&timer==KidsDifficultyProgression.Seconds(level),"Timer budget changed");
        Check(hearts==KidsDifficultyProgression.Hearts(level),"Heart budget changed");
        typeof(LevelManager).GetMethod("PositionPuckAtStart",Private).Invoke(manager,null);
        body.isKinematic=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        Physics.SyncTransforms();
        var path=puck.GetTutorialPath(Vector3.forward,.65f);
        Check(path.Length>5,"Trajectory preview missing");
        foreach(var point in path)Check(float.IsFinite(point.x)&&float.IsFinite(point.z),"Invalid predicted trajectory");
        body.linearVelocity=new Vector3(.8f,0,7f);body.WakeUp();began=Time.unscaledTime;
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Finish(string result)
    {
        Directory.CreateDirectory(WorldOneChallengeBalance.Output);
        File.WriteAllText(WorldOneChallengeBalance.Output+"/playmode-validation.txt",result+"\n\n"+report);
        SessionState.SetString(Key+"Result",result);index=int.MaxValue;Time.timeScale=1;EditorApplication.ExitPlaymode();
    }
}
#endif
