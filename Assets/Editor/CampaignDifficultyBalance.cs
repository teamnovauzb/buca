#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Repeatable, layout-specific balance pass. Does not rebuild art or reorder tutorials.</summary>
public static class CampaignDifficultyBalance
{
    public const int Version = 3;
    public static readonly int[] Seconds = {
        50,55,55,60,60,65,70,70,65,70,
        75,75,75,70,85,70,75,75,75,80,
        80,80,80,80,80,85,85,90,90,95 };
    public static readonly int[] Par = {
        1,2,2,3,2,3,2,2,3,2,
        3,3,3,3,3,2,2,3,2,2,
        2,3,2,2,3,3,3,3,3,3 };
    public static readonly string[] Focus = {
        "Learn a straight shot and gentle power.",
        "Go around one broad blocker; discover forgiving rim rebounds.",
        "Aim diagonally and use a bank toward the corner goal.",
        "Line up two offset openings with room for correction.",
        "Choose a route between staggered short rails.",
        "Combine controlled banks through three alternating rails.",
        "Learn sticky honey with a passable approach between the goal posts.",
        "Watch one slow spinner and shoot through its opening.",
        "Choose a branch, then line up the wide exit opening.",
        "Learn ice on the safe route; boost is an optional bonus route.",
        "Plan short shots through the maze; the side gutter is avoidable.",
        "Learn conveyor drift with slow, offset sliding gates.",
        "Learn mild crosswind, with familiar honey and a slow gate.",
        "Steer through a gentle hazard slalom; avoid the false pocket.",
        "Learn mild gravity with slower familiar moving obstacles.",
        "Practice timing and conveyor correction through paired gaps.",
        "Learn two moderate kick bumpers with room to recover.",
        "Time two counter-rotating walls, one after the other.",
        "Combine familiar ice and crosswind toward the side goal.",
        "Learn linked portals with gentle honey before the entrance.",
        "Plan a route using familiar pads and one kick bumper.",
        "Time offset gates while staying away from the side hazards.",
        "Combine gravity and conveyor drift with a clear goal approach.",
        "Choose a safe route around the deadly diamond.",
        "Control boost and ice through staggered blockers.",
        "Combine two spinners and a kick bumper with an open finish.",
        "Time the larger spinner after honey, then aim between goal guards.",
        "Plan three shots through offset doors and a slow deadly gate.",
        "Combine a sliding wall, bank rail and stronger crosswind.",
        "Final mixed challenge: spinners, portals, wind and gravity." };

    [MenuItem("RealBuca/Balance Campaign/Apply All 30 Levels")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before balancing saved levels.");
        // Keep an exact restorable snapshot; never overwrite an earlier snapshot.
        string backup="output/balance/backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        foreach(string folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels","Assets/Settings/Levels"})
        {
            Directory.CreateDirectory(backup+"/"+folder);
            foreach(string file in Directory.GetFiles(folder))
                File.Copy(file,backup+"/"+file,false);
        }
        var report=new StringBuilder("# Campaign balance v3\n\n");
        report.AppendLine("30 levels; existing mechanic order and art retained. Hearts: 5 / 4 / 3 per ten levels.\n");
        report.AppendLine("| Level | Seconds | Three-star shots | Hearts | Focus |\n|---|---|---|---|---|");
        for(int n=1;n<=30;n++)
        {
            foreach(string folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
            {
                string path=$"{folder}/Level_{n:00}.prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try { Configure(root,n); PrefabUtility.SaveAsPrefabAsset(root,path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var setting=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/Settings/Levels/Level_{n:00}_Settings.asset");
            if(setting==null)throw new InvalidOperationException("Missing level settings "+n);
            setting.timeLimit=Seconds[n-1];setting.threeStarStrokes=Par[n-1];setting.maxLives=Lives(n);
            EditorUtility.SetDirty(setting);
            report.AppendLine($"| {n} | {Seconds[n-1]} | {Par[n-1]} | {Lives(n)} | {Focus[n-1]} |");
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("output/balance");
        File.WriteAllText("output/balance/campaign-v3.md",report.ToString());
        File.WriteAllText("output/balance/applied.txt","Updated 60 prefabs and 30 settings. Backup: "+backup);
        Debug.Log("[CampaignBalance] Updated all 30 source and playable levels.");
    }

    static int Lives(int level) => level<=10?5:level<=20?4:3;
    static void Configure(GameObject root,int n)
    {
        var profile=root.GetComponent<LevelDifficultyProfile>();
        if(profile==null)profile=root.AddComponent<LevelDifficultyProfile>();
        profile.balanceVersion=Version;profile.levelNumber=n;profile.chapter=(n-1)/6+1;
        profile.difficultyIndex=n;profile.difficultyRating=profile.chapter;profile.mechanicHint=Focus[n-1];
        // The toy cabinet's visible cutout has a 0.44-unit radius at every level.
        // Keep capture near that visible size instead of secretly shrinking it twice.
        var hole=root.transform.Find("Hole");var ring=root.transform.Find("Hole_Ring");
        if(hole==null||ring==null)throw new InvalidOperationException("Missing goal in "+root.name);
        ring.localScale=new Vector3(1.14f,.04f,1.14f);
        hole.localScale=new Vector3(.8436f,.05f,.8436f);
        var trigger=hole.GetComponent<SphereCollider>();
        trigger.radius=.40f/Mathf.Max(hole.lossyScale.x,hole.lossyScale.z);trigger.isTrigger=true;
        switch(n)
        {
            case 2: Length(root,"Block",1.55f);Position(root,"Lower_Nudge",-2.5f,-2.6f);break;
            case 3: Position(root,"Lower_Wall",-2.7f,-2.6f);break;
            case 4: Position(root,"Gap2_L",-2.2f,3f);break;
            case 5: Position(root,"P5",.8f,3.6f);break;
            case 6: Length(root,"Z1",2.1f);Length(root,"Z2",2.15f);Length(root,"Z3",2.1f);break;
            case 7:
                var pegs=Array.FindAll(root.GetComponentsInChildren<Transform>(true),t=>t.name=="NM2_GuardPeg");
                Array.Sort(pegs,(a,b)=>a.localPosition.x.CompareTo(b.localPosition.x));
                for(int i=0;i<pegs.Length;i++){var p=pegs[i].localPosition;p.x=(i-1)*1.12f;p.z=i==1?6.25f:4.65f;pegs[i].localPosition=p;}
                break;
            case 8: Position(root,"Top_Funnel_L",-1.65f,4.3f);Position(root,"Top_Funnel_R",1.65f,4.3f);break;
            case 9: Position(root,"NM2_NarrowL",-2.65f,4.5f);Position(root,"NM2_NarrowR",2.65f,4.5f);Length(root,"NM2_NarrowL",1.85f);Length(root,"NM2_NarrowR",1.85f);break;
            case 11:
                Length(root,"M1",2.25f);Length(root,"M2",2.25f);Length(root,"M3",2.25f);
                Position(root,"Start_Nub",-3.4f,-4.6f);Position(root,"Pocket_R",3.3f,3.5f);Length(root,"Pocket_R",.85f);break;
            case 12: Position(root,"Top_Funnel_L",-1.5f,4.6f);Position(root,"Top_Funnel_R",1.5f,4.6f);break;
            case 13: Position(root,"Exit_L",-1.5f,4.8f);Position(root,"Exit_R",1.5f,4.8f);break;
            case 14: Length(root,"Deadly_1",.65f);Length(root,"Deadly_2",.65f);break;
            case 23: Position(root,"Exit",1.25f,4.2f);Length(root,"Exit",1f);Position(root,"PuckStart",-2.4f,-5.5f);break;
            case 24: Position(root,"PuckStart",-2.2f,-5.5f);break;
            case 25: Position(root,"Top_L",-1.4f,4.6f);Position(root,"Top_R",1.4f,4.6f);break;
            case 26: Position(root,"Pocket_L",-1.65f,4.3f);Position(root,"Pocket_R",1.65f,4.3f);break;
            case 27: Position(root,"Hole_Lip_L",-1.15f,5f);Position(root,"Hole_Lip_R",1.15f,5f);break;
            case 28: Length(root,"Lower_Divide",2.3f);Length(root,"Upper_Divide",2.3f);break;
        }
        int index=0;
        foreach(var wall in root.GetComponentsInChildren<RotatingWall>(true))
        {
            float speed=n==8?24:n==15?28:n==18?34:n==26?42:n==27?44:46;
            wall.speedDegPerSec=(wall.speedDegPerSec<0?-1:1)*(speed+index++*4);
        }
        index=0;
        foreach(var wall in root.GetComponentsInChildren<MovingWall>(true))
        {
            float cycle=n==12?5:n==13?4.8f:n==15?5.5f:n==16?4.6f:n==22?4.4f:n==28?5:4.2f;
            wall.cycleSeconds=cycle+index++*.25f;
        }
        foreach(var wind in root.GetComponentsInChildren<BucaWindZone>(true))
            wind.forceMagnitude=n==12?1.8f:n==13?2.2f:n==15?2.4f:n==16?2.5f:n==19?3:n==23?2.7f:n==28?3:n==29?3.8f:4;
        foreach(var well in root.GetComponentsInChildren<GravityWell>(true))well.strength=n==15?3.5f:n==23?4.5f:5;
        foreach(var patch in root.GetComponentsInChildren<IcePatch>(true))
        {
            bool honey=patch.name.Contains("Mud")||patch.patchDamping>1;
            patch.patchDamping=honey?8f:n==10?.24f:.20f;
            patch.restoreDamping=.9f;
        }
        foreach(var boost in root.GetComponentsInChildren<SpeedBoost>(true)){boost.boostAmount=n==10?3:3.5f;boost.maxSpeedAfter=11;}
        foreach(var pad in root.GetComponentsInChildren<BouncePad>(true))pad.launchSpeed=n==15?10:n==21?11:12;
        foreach(var kick in root.GetComponentsInChildren<KickerBumper>(true)){kick.kickSpeed=n==17?6.5f:n==21?8:8.5f;kick.restitutionGain=n==17?1:1.05f;kick.maxSpeed=12;}
    }
    static void Position(GameObject root,string name,float x,float z)
    {
        var t=root.transform.Find(name);if(t==null)throw new InvalidOperationException("Missing "+name);
        t.localPosition=new Vector3(x,t.localPosition.y,z);
    }
    static void Length(GameObject root,string name,float halfLength)
    {
        var t=root.transform.Find(name);if(t==null)throw new InvalidOperationException("Missing "+name);
        var scale=t.localScale;scale.y=halfLength;t.localScale=scale;
    }
}
#endif
