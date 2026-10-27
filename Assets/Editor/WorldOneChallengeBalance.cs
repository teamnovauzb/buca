#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>World 1 precision/timing pass. Never regenerates a board or changes budgets.</summary>
public static partial class WorldOneChallengeBalance
{
    public static readonly int[] ChallengeLevels = {3,4,5,6,9,14,16,18,19,21,22,23,24,25,26,27,28,29,30};
    public static readonly int[] IntroductionLevels = {1,2,7,8,10,11,12,13,15,17,20};
    public const string Output = "output/balance/world1-oct16";
    static readonly string[] Folders = {"Assets/Prefabs/Levels", "Assets/ToyBoxMenu/Levels"};
    static readonly Dictionary<int,string> Focus = new Dictionary<int,string> {
        {3,"Longer lower blocker asks for a deliberate corner bank."},
        {4,"Offset openings require two planned aim directions."},
        {5,"Staggered longer rails close the easy centre line."},
        {6,"Longer alternating rails reward controlled bank shots."},
        {9,"A tighter final opening rewards a precise approach."},
        {14,"Narrower corridor and longer exit lip demand controlled placement."},
        {16,"A quicker gate follows the tighter second iris."},
        {18,"Counter-rotating walls have different speeds and starting angles."},
        {19,"Stronger familiar wind and a longer lip demand a corrected bank."},
        {21,"Longer staggered blockers make the existing launch-pad route useful."},
        {22,"Two quicker gates open at different times instead of moving together."},
        {23,"A longer angled exit rail asks for a deliberate final approach."},
        {24,"Offset lanes break the easy straight route while leaving recovery space."},
        {25,"Longer blockers and a tighter finish require control after the boost."},
        {26,"Quicker staggered spinners lead to a tighter final opening."},
        {27,"Faster rotation and closer goal guards reward careful timing."},
        {28,"Longer alternating dividers require planning across both rooms."},
        {29,"A quicker wider gate and stronger familiar wind test aim and timing."},
        {30,"Longer staggered spinners and stronger wind make the finale a combined challenge."}
    };

    [MenuItem("RealBuca/Balance Campaign/Harden World 1 Challenges")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit mode required.");
        Directory.CreateDirectory(Output);
        var protectedHashes=ProtectedFiles().Distinct().ToDictionary(p=>p,Hash);
        var lessons=TutorialSequence();
        string backup=Output+"/backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var report=new StringBuilder("# World 1 challenge pass\n\n19 practice/challenge levels tightened. Introduction levels 1, 2, 7, 8, 10, 11, 12, 13, 15, 17 and 20 are unchanged. No changes to timers, hearts, par, tutorial selection, mechanic types, goal sizes or World 2.\n\n| Level | Change |\n|---|---|\n");
        // Keep exact source snapshots before any authoring. Restoring uses these prefabs.
        foreach(int n in ChallengeLevels)foreach(string folder in Folders)
        {
            string path=PathFor(folder,n), destination=backup+"/"+path;
            Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(path,destination);
        }
        File.WriteAllText(Output+"/latest-backup.txt",backup);
        foreach(int n in ChallengeLevels)
        {
            foreach(string folder in Folders)
            {
                string path=PathFor(folder,n);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string mechanics=MechanicSignature(root);
                    Tune(root,n);
                    if(mechanics!=MechanicSignature(root))throw new Exception("Mechanic types changed: "+path);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            report.AppendLine($"| {n} | {Focus[n]} |");
        }
        AssetDatabase.SaveAssets();
        foreach(var item in protectedHashes)if(Hash(item.Key)!=item.Value)throw new Exception("Protected content changed: "+item.Key);
        if(lessons!=TutorialSequence())throw new Exception("Tutorial introductions changed.");
        ToyBoxSkinValidation.CheckCampaign();
        File.WriteAllText(Output+"/changes.md",report+"\nBackup: "+backup+"\n");
        File.WriteAllText(Output+"/protected-files.txt",string.Join("\n",protectedHashes.Select(p=>p.Value+" "+p.Key)));
        File.WriteAllText(Output+"/authoring-validation.txt","PASS: 38 source/playable prefabs; tutorial sequence preserved; all introduction prefabs, settings, World 2 and tutorial assets byte-identical; source/skin collision agreement.\n");
        return "Saved 19 harder World 1 challenges. Introductions, budgets and World 2 retained.";
    }

    public static void Tune(GameObject root,int n)
    {
        if(!ChallengeLevels.Contains(n))return;
        switch(n)
        {
            case 3: Position(root,"Lower_Wall",-2.2f,-2.6f);Length(root,"Lower_Wall",1.45f);break;
            case 4:
                Length(root,"Gap1_L",1.675f);Length(root,"Gap1_R",2.475f);
                Position(root,"Gap2_L",-2f,3);Length(root,"Gap2_L",2.725f);
                Position(root,"Gap2_R",3.5f,3);Length(root,"Gap2_R",1.525f);break;
            case 5:
                Position(root,"P2",.75f,-1.3f);Length(root,"P2",1.35f);
                Position(root,"P4",-1.1f,2);Length(root,"P4",1.45f);Position(root,"P5",.4f,3.6f);break;
            case 6: Length(root,"Z1",2.45f);Length(root,"Z2",2.5f);Length(root,"Z3",2.45f);break;
            case 9: Length(root,"NM2_NarrowL",2f);Length(root,"NM2_NarrowR",2f);Length(root,"Cross_N",1.2f);break;
            case 14: Position(root,"Corridor_L",-1.3f,-.5f);Position(root,"Corridor_R",1.3f,-.5f);Position(root,"Exit_Lip",.1f,4.4f);Length(root,"Exit_Lip",1.45f);break;
            case 16: Position(root,"Iris_L2",-1.4f,.3f);Position(root,"Iris_R2",1.4f,.3f);SetGates(root,4.65f,0);break;
            case 18: SetSpinners(root,40,4,45);foreach(var goal in root.GetComponentsInChildren<OrbitingHole>(true))goal.cycleSeconds=6.6f;break;
            case 19: SetWind(root,3.25f);Position(root,"Exit_Lip",.05f,4.4f);Length(root,"Exit_Lip",1.35f);break;
            case 21: Length(root,"Block_L",2);Length(root,"Block_R",2);break;
            case 22: SetGates(root,4.4f,.5f);break;
            case 23: Position(root,"Exit",1,4.2f);Length(root,"Exit",1.4f);break;
            case 24: Position(root,"Lane_L",-1.6f,-.4f);Position(root,"Lane_R",1.6f,1.2f);Length(root,"Lane_L",2.35f);Length(root,"Lane_R",2.35f);break;
            case 25: Length(root,"Block_L",2);Length(root,"Block_R",2.05f);Position(root,"Top_L",-1.15f,4.6f);Position(root,"Top_R",1.15f,4.6f);break;
            case 26: SetSpinners(root,48,4,50);Position(root,"Pocket_L",-1.35f,4.3f);Position(root,"Pocket_R",1.35f,4.3f);break;
            case 27: SetSpinners(root,50,0,0);Position(root,"Hole_Lip_L",-.90f,5);Position(root,"Hole_Lip_R",.90f,5);break;
            case 28: Length(root,"Lower_Divide",2.7f);Length(root,"Upper_Divide",2.7f);break;
            case 29: SetGates(root,3.7f,0);Length(root,"MovingWall",2.05f);SetWind(root,4.2f);break;
            case 30:
                SetSpinners(root,56,4,55);SetWind(root,4.4f);
                foreach(var wall in root.GetComponentsInChildren<RotatingWall>(true))SetLength(wall.transform,1.35f);
                break;
        }
        var profile=root.GetComponent<LevelDifficultyProfile>();
        if(profile==null)profile=root.AddComponent<LevelDifficultyProfile>();
        profile.balanceVersion=6;profile.mechanicHint=Focus[n];
    }
    static void SetSpinners(GameObject root,float speed,float step,float phase)
    {
        int i=0;foreach(var wall in root.GetComponentsInChildren<RotatingWall>(true))
        {wall.speedDegPerSec=Mathf.Sign(wall.speedDegPerSec)*(speed+i*step);wall.phaseDeg=i*phase;i++;}
    }
    static void SetGates(GameObject root,float cycle,float step)
    {
        int i=0;foreach(var wall in root.GetComponentsInChildren<MovingWall>(true))
        {wall.cycleSeconds=cycle+i*step;wall.phase=i*.35f;i++;}
    }
    static void SetWind(GameObject root,float force)
    {foreach(var wind in root.GetComponentsInChildren<BucaWindZone>(true))if(!wind.conveyor)wind.forceMagnitude=force;}
    static Transform Required(GameObject root,string name)=>root.transform.Find(name)??throw new Exception("Missing "+root.name+"/"+name);
    static void Position(GameObject root,string name,float x,float z)
    {var t=Required(root,name);t.localPosition=new Vector3(x,t.localPosition.y,z);}
    static void Length(GameObject root,string name,float halfLength)=>SetLength(Required(root,name),halfLength);
    static void SetLength(Transform t,float halfLength)
    {var s=t.localScale;s.y=halfLength;t.localScale=s;}
    public static string PathFor(string folder,int n)=>$"{folder}/Level_{n:00}.prefab";
    static string MechanicSignature(GameObject root)=>string.Join("|",root.GetComponentsInChildren<MonoBehaviour>(true).Where(x=>x!=null&&!(x is LevelDifficultyProfile)).Select(x=>x.GetType().FullName+":"+AnimationUtility.CalculateTransformPath(x.transform,root.transform)));
    static IEnumerable<string> ProtectedFiles()
    {
        foreach(string folder in Folders)foreach(int n in IntroductionLevels)yield return PathFor(folder,n);
        foreach(string path in Directory.GetFiles("Assets/Settings/Levels"))yield return path;
        foreach(string path in Directory.GetFiles("Assets","*",SearchOption.AllDirectories))
            if((path.IndexOf("Tutorial",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("WatchCopy",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("ObstacleIntro",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("BonusBoards",StringComparison.OrdinalIgnoreCase)>=0)&&!path.EndsWith(".meta"))yield return path;
        foreach(string folder in Folders)for(int n=31;n<=50;n++)if(File.Exists(PathFor(folder,n)))yield return PathFor(folder,n);
    }
    static string Hash(string path)
    {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    static string TutorialSequence()
    {
        var go=new GameObject("WorldOneTutorialAudit"){hideFlags=HideFlags.HideAndDontSave};
        try
        {
            var intro=go.AddComponent<ObstacleIntroController>();
            var roots=Enumerable.Range(1,30).Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(Folders[1],n))).ToArray();
            return string.Join("|",roots.Select((r,i)=>string.Join(",",intro.SelectLessonsForLevel(r,i+1,roots))));
        }
        finally{Object.DestroyImmediate(go);}
    }
}
#endif
