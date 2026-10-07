#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class HoneyTutorialCheck
{
    static double began;static int phase;
    static HoneyTutorialCheck(){EditorApplication.update+=Tick;}
    public static void Start(){phase=0;began=0;SessionState.SetBool("Honey.Check",true);SessionState.SetString("Honey.OldStart",AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Game.unity");EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("Honey.Check",false)||!EditorApplication.isPlaying)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Game"){UnityEngine.SceneManagement.SceneManager.LoadScene("Game");return;}
        var m=Object.FindFirstObjectByType<LevelManager>();var b=Object.FindFirstObjectByType<HoneyTutorialBoard>();if(m==null||b==null||m.CurrentLevelRoot==null)return;
        if(phase==0){m.LoadLevel(6);began=EditorApplication.timeSinceStartup;phase=1;}
        double elapsed=EditorApplication.timeSinceStartup-began;
        if(phase==1&&elapsed>.85){ScreenCapture.CaptureScreenshot("output/toy-box/Honey-Tutorial-Live.png");System.IO.File.WriteAllText("output/toy-box/Honey-check.txt","Level="+m.CurrentLevelNumber+" showing="+b.IsShowing+" inputAllowed="+m.GameplayInputAllowed+"\n");phase=2;}
        if(phase==2&&elapsed>3.35){ScreenCapture.CaptureScreenshot("output/toy-box/Honey-Miniature-Charge.png");phase=3;}
        if(phase==3&&elapsed>7){System.IO.File.AppendAllText("output/toy-box/Honey-check.txt","After 7s showing="+b.IsShowing+" inputAllowed="+m.GameplayInputAllowed+"\n");b.StartCoroutine(CheckHoney(m,b));SessionState.SetBool("Honey.Check",false);UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("Honey.OldStart",""));}
    }
    static System.Collections.IEnumerator CheckHoney(LevelManager m,HoneyTutorialBoard board)
    {
        var patch=m.CurrentLevelRoot.GetComponentInChildren<IcePatch>();
        var go=new GameObject("HoneyPhysicsProbe",typeof(SphereCollider),typeof(Rigidbody),typeof(TutorialPracticePuck));
        go.GetComponent<SphereCollider>().radius=.12f;var rb=go.GetComponent<Rigidbody>();rb.useGravity=false;rb.linearDamping=.9f;
        rb.position=patch.transform.position+Vector3.up*.3f;rb.linearVelocity=Vector3.right;
        yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        float inside=rb.linearDamping;float speed=rb.linearVelocity.magnitude;
        rb.position=patch.transform.position+Vector3.up*3;
        yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        System.IO.File.AppendAllText("output/toy-box/Honey-check.txt","Actual level trigger: damping inside="+inside+" expected="+patch.patchDamping+" speed="+speed+" damping after exit="+rb.linearDamping+"; demo weak x="+(board.singleBoard?board.LastWeakX:board.demoPuck.transform.localPosition.x)+" strong x="+(board.singleBoard?board.LastStrongX:board.secondPuck.transform.localPosition.x)+"\n");
        Object.Destroy(go);
    }
}
#endif
