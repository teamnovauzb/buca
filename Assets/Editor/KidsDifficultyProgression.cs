#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Difficulty-only pass. Keeps level order, obstacle geometry, art and tutorials.</summary>
public static class KidsDifficultyProgression
{
    public static int Seconds(int n)=>20+(n-1)*2;
    public static int Hearts(int n)=>Mathf.Max(5,n);
    [MenuItem("RealBuca/Balance Campaign/Apply Move Progression Only")]
    public static string ApplyMovesOnly()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var settings=new LevelSettings[30];
        for(int n=1;n<=30;n++)
        {
            settings[n-1]=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/Settings/Levels/Level_{n:00}_Settings.asset");
            if(settings[n-1]==null)throw new Exception("Missing level settings: "+n);
        }
        string backup="output/balance/moves-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        var report=new StringBuilder("Level,PreviousMoves,Moves\n");
        for(int n=1;n<=30;n++)
        {
            string path=AssetDatabase.GetAssetPath(settings[n-1]);
            File.Copy(path,Path.Combine(backup,Path.GetFileName(path)),false);
            report.AppendLine($"{n},{settings[n-1].maxLives},{Hearts(n)}");
            settings[n-1].maxLives=Hearts(n);
            EditorUtility.SetDirty(settings[n-1]);AssetDatabase.SaveAssetIfDirty(settings[n-1]);
        }
        File.WriteAllText("output/balance/move-progression.csv",report.ToString());
        return "Saved move allowances: levels 1–5 = 5, then +1 per level through level 30 = 30. Backup: "+backup;
    }
    [MenuItem("RealBuca/Balance Campaign/Apply Timer Progression Only")]
    public static string ApplyTimersOnly()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var settings=new LevelSettings[30];
        for(int n=1;n<=30;n++)
        {
            settings[n-1]=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/Settings/Levels/Level_{n:00}_Settings.asset");
            if(settings[n-1]==null)throw new Exception("Missing level settings: "+n);
        }
        string backup="output/balance/timer-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        var report=new StringBuilder("Level,PreviousSeconds,Seconds\n");
        for(int n=1;n<=30;n++)
        {
            string path=AssetDatabase.GetAssetPath(settings[n-1]);
            File.Copy(path,Path.Combine(backup,Path.GetFileName(path)),false);
            report.AppendLine($"{n},{settings[n-1].timeLimit},{Seconds(n)}");
            settings[n-1].timeLimit=Seconds(n);
            EditorUtility.SetDirty(settings[n-1]);AssetDatabase.SaveAssetIfDirty(settings[n-1]);
        }
        File.WriteAllText("output/balance/timer-progression.csv",report.ToString());
        return "Saved all 30 level timers: level 1 = 20 seconds, +2 per level, level 30 = 78 seconds. Backup: "+backup;
    }
    static void Tune(GameObject root,int n)
    {
        var profile=root.GetComponent<LevelDifficultyProfile>();
        if(profile==null)profile=root.AddComponent<LevelDifficultyProfile>();
        profile.balanceVersion=5;profile.levelNumber=n;profile.difficultyIndex=n;
        profile.chapter=(n-1)/6+1;profile.difficultyRating=profile.chapter;
        if(n==1){var start=root.transform.Find("PuckStart");start.localPosition=new Vector3(0,start.localPosition.y,-2.5f);}
        foreach(var wall in root.GetComponentsInChildren<RotatingWall>(true))
            wall.speedDegPerSec=Mathf.Sign(wall.speedDegPerSec)*Mathf.Lerp(18,38,Mathf.InverseLerp(8,30,n));
        foreach(var wall in root.GetComponentsInChildren<MovingWall>(true))
            wall.cycleSeconds=Mathf.Lerp(6.5f,4.6f,Mathf.InverseLerp(12,30,n));
        foreach(var goal in root.GetComponentsInChildren<OrbitingHole>(true))
        {goal.radius=Mathf.Lerp(.30f,.55f,Mathf.InverseLerp(8,30,n));goal.cycleSeconds=Mathf.Lerp(10,7.5f,Mathf.InverseLerp(8,30,n));}
        foreach(var wind in root.GetComponentsInChildren<BucaWindZone>(true))
        {
            // Preserve conveyor transport and launch trajectories. Tune crosswind only.
            if(!wind.conveyor)wind.forceMagnitude=Mathf.Lerp(1.5f,3.5f,Mathf.InverseLerp(13,30,n));
        }
        // Honey remains strongly sticky as requested; pads, traps and portal behavior stay intact.
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        string backup="output/balance/kids-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        foreach(var folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels","Assets/Settings/Levels"})
        {Directory.CreateDirectory(backup+"/"+folder);foreach(var f in Directory.GetFiles(folder))File.Copy(f,backup+"/"+f,false);}
        var report=new StringBuilder("# Kids difficulty progression\n\nOnly difficulty values, level 1 start distance, and heart-star placements changed. Existing order, art, tutorials, trap behavior and launch trajectories retained.\n\nTimers start at 20 seconds and increase by 2 seconds per level, reaching 78 seconds at level 30. Moving mechanics start slower and become moderately faster. Honey retains its strong slowdown. Stars grant one heart and appear only in five hard levels. This is an authored balance pass; player testing is needed to assess the full difficulty curve.\n\n| Level | Seconds | Starting hearts | Heart stars |\n|---|---|---|---|\n");
        for(int n=1;n<=30;n++)
        {
            foreach(var dir in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
            {
                string path=$"{dir}/Level_{n:00}.prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{Tune(root,n);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var settings=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/Settings/Levels/Level_{n:00}_Settings.asset");
            settings.timeLimit=Seconds(n);settings.maxLives=Hearts(n);EditorUtility.SetDirty(settings);
        }
        HeartRewardBaker.BakeLevelPlacements();
        for(int n=1;n<=30;n++){
            var r=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ToyBoxMenu/Levels/Level_{n:00}.prefab");
            report.AppendLine($"| {n} | {Seconds(n)} | {Hearts(n)} | {r.GetComponentsInChildren<ScorePickup>(true).Count(p=>p.grantsHeart)} |");}
        Directory.CreateDirectory("output/balance");File.WriteAllText("output/balance/kids-progression.md",report.ToString()+"\nBackup: "+backup);
        AssetDatabase.SaveAssets();return Validate();
    }
    public static string Validate()
    {
        foreach(var dir in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"}){
            int rewardLevels=0;
            for(int n=1;n<=30;n++){
                var root=AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Level_{n:00}.prefab");
                var settings=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/Settings/Levels/Level_{n:00}_Settings.asset");
                if(settings.timeLimit!=Seconds(n)||settings.maxLives!=Hearts(n))throw new Exception("Wrong budget: "+n);
                int stars=root.GetComponentsInChildren<ScorePickup>(true).Count(p=>p.grantsHeart);
                if(stars>0)rewardLevels++;
                if(HeartRewardBaker.IsRewardLevel(n)?stars<1||stars>2:stars!=0)throw new Exception("Wrong reward count: "+dir+" "+n);
                if(root.GetComponent<LevelDifficultyProfile>().balanceVersion!=5)throw new Exception("Not balanced: "+n);
            }
            if(rewardLevels!=5)throw new Exception("Expected exactly 5 reward levels");
        }
        return "PASS: 60 level prefabs, 30 timer/move settings, levels 1–5 = 5 moves, then +1 each level; exactly five reward levels in each set, 1–2 stars each.";
    }
}
#endif
