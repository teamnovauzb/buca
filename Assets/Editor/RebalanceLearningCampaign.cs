#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Repeatable tuning of the existing 30 layouts, in source and saved skin prefabs.</summary>
public static class RebalanceLearningCampaign
{
    public static readonly int[] Seconds={50,50,48,50,55,55,65,65,55,65,65,70,75,65,85,70,70,75,70,80,80,80,85,75,75,85,85,90,90,100};
    public static readonly int[] Par={1,2,2,3,3,3,3,3,3,3,3,3,3,3,4,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3};
    static readonly string[] Focus={
        "Learn aim and power with time to retry.","Learn one rebound.","Practice the corner route.","Plan alternating banks.","Control power through the narrow exit.","Combine two banks.",
        "Learn mud and guard pegs with a gentler slowdown.","Learn slow rotation and a small, slow-moving goal.","Choose the safer branch.","Learn ice and a controlled boost; pickups are optional.","Learn to avoid hazards without rushing.","Learn slow sliding gates and the conveyor.",
        "Learn light wind with familiar moving gates.","Practice hazard avoidance.","Learn gravity and bounce pads; familiar forces and gates stay gentle.","Practice quicker sliding gates.","Learn predictable kicker rebounds.","Combine faster spinners and moving goal.",
        "Combine ice with stronger wind.","Learn linked portals with moderate mud.","Combine existing launch pads and kicker rebounds.",
        "Time a safe moving-wall gap.","Plan around gravity and conveyors.","Keep clear of the hazard route.","Control ice and boost together.","Time launch pads and rotation.","Thread mud, rotation and hazards.","Wait for conveyor and wall alignment.","Bank through wind and moving gaps.","Final combination: portals, gravity, wind and rotation."
    };
    [MenuItem("RealBuca/Toy Box 3D/8 - Balance Learning Levels 1-30")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var report=new StringBuilder("# Difficulty progression: levels 1–30\n\nExisting geometry, hazard positions, launch-pad trajectories and tutorial triggers are preserved. Introductory mechanics get gentler forces and longer response windows. Timer budgets now reach runtime (previously hardcoded to 30 seconds). Later combinations receive longer planning windows while the 5/4/3 heart rule is preserved. Three-star targets never exceed the heart budget.\n\n| Level | Chapter | Seconds | Par | Focus |\n|---|---|---|---|---|\n");
        for(int level=1;level<=30;level++)
        {
            foreach(string folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
            {
                string path=$"{folder}/Level_{level:00}.prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null) throw new Exception("Missing "+path);
                var root=PrefabUtility.LoadPrefabContents(path);
                try { Tune(root,level); PrefabUtility.SaveAsPrefabAsset(root,path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var settings=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/Settings/Levels/Level_{level:00}_Settings.asset");
            if(settings==null) throw new Exception("Missing settings "+level);
            settings.timeLimit=Seconds[level-1]; settings.threeStarStrokes=Par[level-1]; EditorUtility.SetDirty(settings);
            report.AppendLine($"| {level} | {(level-1)/6+1} | {Seconds[level-1]} | {Par[level-1]} | {Focus[level-1]} |");
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("output/toy-box"); File.WriteAllText("output/toy-box/Difficulty-Levels-01-30.md",report.ToString());
        Validate();
        Debug.Log("LEARNING_CAMPAIGN_BAKED: 30 source and 30 skinned prefabs plus 30 settings.");
    }
    static void Tune(GameObject root,int level)
    {
        var profile=root.GetComponent<LevelDifficultyProfile>();
        if(profile==null) profile=root.AddComponent<LevelDifficultyProfile>();
        profile.balanceVersion=4; profile.levelNumber=level; profile.chapter=(level-1)/6+1;
        profile.difficultyRating=profile.chapter; profile.difficultyIndex=level; profile.mechanicHint=Focus[level-1];
        int index=0;
        foreach(var wall in root.GetComponentsInChildren<RotatingWall>(true))
        {
            float speed=level==8?28:level==15?30:level>=21?36:38;
            wall.speedDegPerSec=(wall.speedDegPerSec<0?-1:1)*(speed+index++*4);
        }
        index=0;
        foreach(var wall in root.GetComponentsInChildren<MovingWall>(true))
            wall.cycleSeconds=(level==12?4.4f:level==13?4.2f:level==15?5.0f:level>=21?4.8f:4.2f)+index++*.15f;
        foreach(var goal in root.GetComponentsInChildren<OrbitingHole>(true))
        { goal.radius=level==8?.45f:.65f; goal.cycleSeconds=level==8?8f:6.5f; }
        foreach(var wind in root.GetComponentsInChildren<BucaWindZone>(true))
            wind.forceMagnitude=level==13?3f:level==15?3.2f:3.6f;
        foreach(var gravity in root.GetComponentsInChildren<GravityWell>(true)) gravity.strength=5.5f;
        foreach(var patch in root.GetComponentsInChildren<IcePatch>(true))
        {
            bool mud=patch.name.IndexOf("Mud",StringComparison.OrdinalIgnoreCase)>=0 || patch.patchDamping>1;
            patch.patchDamping=mud?8f:(level==10?.22f:.14f);
        }
        foreach(var boost in root.GetComponentsInChildren<SpeedBoost>(true)) { boost.boostAmount=4.5f; boost.maxSpeedAfter=10f; }
        foreach(var kicker in root.GetComponentsInChildren<KickerBumper>(true))
        { kicker.kickSpeed=level==17?8f:9.5f; kicker.restitutionGain=level==17?1.02f:1.08f; kicker.maxSpeed=12f; }
    }
    public static void Validate()
    {
        for(int level=1;level<=30;level++)
        {
            var settings=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/Settings/Levels/Level_{level:00}_Settings.asset");
            if(settings.timeLimit!=Seconds[level-1] || settings.threeStarStrokes!=Par[level-1]) throw new Exception("Wrong authored learning budget: "+level);
            foreach(string folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
            {
                string path=$"{folder}/Level_{level:00}.prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    // Idempotency also checks that all serialized gameplay components match the policy.
                    string before=Snapshot(root); Tune(root,level);
                    if(before!=Snapshot(root)) throw new Exception("Difficulty differs from authored policy: "+path);
                    if(root.transform.Find("Hole")==null || root.transform.Find("PuckStart")==null) throw new Exception("Missing goal/start: "+path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        ToyBoxSkinValidation.CheckCampaign();
        Debug.Log("LEARNING_CAMPAIGN_VALIDATION_PASSED: settings, 60 prefab profiles/mechanic sets, idempotency and unchanged collision geometry across source/skins.");
    }
    static string Snapshot(GameObject root)
    {
        var result=new StringBuilder();
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            if(component is LevelDifficultyProfile || component is MovingWall || component is RotatingWall || component is OrbitingHole || component is BucaWindZone || component is GravityWell || component is IcePatch || component is SpeedBoost || component is KickerBumper)
                result.Append(EditorJsonUtility.ToJson(component));
        return result.ToString();
    }
}
#endif
