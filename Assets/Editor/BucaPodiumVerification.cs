#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
public static class BucaPodiumVerification
{
    static double started;static bool captured;static int phase;static LeaderboardPanel panel;
    public static string Begin()
    {
        if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Verification requires Play Mode; scene objects must not be changed in Edit Mode.");
        panel=Object.FindFirstObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);
        var manager=Object.FindFirstObjectByType<LevelManager>();if(manager!=null){manager.StopAllCoroutines();manager.enabled=false;}
        panel.StopAllCoroutines();panel.gameObject.SetActive(false);
        typeof(LeaderboardPanel).GetField("_isAnimating",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(panel,false);
        Time.timeScale=0;phase=0;Run();EditorApplication.update-=Tick;EditorApplication.update+=Tick;return "Testing timeout, then lives-out at timeScale zero.";
    }
    static void Run()
    {
        captured=false;started=EditorApplication.timeSinceStartup;
        var entries=Enumerable.Range(0,5).Select(i=>new LeaderboardPanel.LeaderboardData{rank=i+1,playerName=new[]{"MAYA","LEO","YOU","AMIR","LILY"}[i],score=2450-i*270}).ToArray();
        panel.Show(phase==0?entries:null,phase==0?3:0,1910,"YOU",phase==0?"TIME'S UP!":"OUT OF HEARTS!",()=>{
            System.IO.File.AppendAllText("output/leaderboard/verification.txt",(phase==0?"Timeout":"Lives out")+" callback after "+(EditorApplication.timeSinceStartup-started).ToString("F3")+" seconds\n");
            phase++;});
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
        if(!captured&&EditorApplication.timeSinceStartup-started>1.5)
        {
            captured=true;ScreenCapture.CaptureScreenshot("output/leaderboard/BUCA-Podium-Live-"+phase+".png");
            var v=panel.GetComponent<BucaPodiumLeaderboard3D>();
            System.IO.File.AppendAllText("output/leaderboard/verification.txt","Visible stage="+v.stage.activeSelf+", first name mesh="+(v.names[0].GetChild(0).GetComponent<MeshFilter>().sharedMesh!=null)+", status="+panel.titleText.text+"\n");
        }
        if(phase==1&&!panel.gameObject.activeSelf)Run();
        if(phase>=2){Time.timeScale=1;EditorApplication.update-=Tick;}
    }
}
#endif
