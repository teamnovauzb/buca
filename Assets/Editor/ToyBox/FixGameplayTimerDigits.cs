using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class FixGameplayTimerDigits
{
    static void Fix(ToyBoxGameplayHud hud)
    {
        if(hud.timeDigits.Length<3)
        {
            var original=hud.timeDigits;
            var digit=UnityEngine.Object.Instantiate(original[0].gameObject,original[0].transform.parent).GetComponent<MeshFilter>();
            digit.name="HundredsDigit";
            hud.timeDigits=new[]{digit,original[0],original[1]};
        }
        hud.timeFaces=hud.timeDigits.Select(d=>d.GetComponent<Renderer>()).ToArray();
        EditorUtility.SetDirty(hud);
        if(PrefabUtility.IsPartOfPrefabInstance(hud))PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        const string path="Assets/ToyBoxMenu/Prefabs/SolidGameplayHud.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{foreach(var hud in root.GetComponentsInChildren<ToyBoxGameplayHud>(true))Fix(hud);PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        var report=new StringBuilder();
        try
        {
            var hud=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ToyBoxGameplayHud>(true)).First();Fix(hud);
            var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LevelManager>(true)).First();
            var show=typeof(ToyBoxGameplayHud).GetMethod("Show",BindingFlags.Instance|BindingFlags.NonPublic);
            Action<int> check=value=>{
                show.Invoke(hud,new object[]{hud.timeDigits,value,true});
                string text=string.Concat(hud.timeDigits.Where(d=>d.gameObject.activeSelf).Select(d=>Array.IndexOf(hud.digitMeshes,d.sharedMesh).ToString()));
                if(text!=value.ToString("D2"))throw new Exception("Timer rendering mismatch: "+value+" -> "+text);
            };
            for(int i=0;i<manager.levelSettings.Length;i++)
            {
                var setting=manager.levelSettings[i];if(setting==null)throw new Exception("Missing setting "+(i+1));
                int seconds=Mathf.CeilToInt(setting.timeLimit);check(seconds);
                report.AppendLine("Level "+(i+1)+": "+seconds+" seconds; display verified");
            }
            foreach(int seconds in new[]{0,4,9,10,30,99,100,104,110,111,114,171})check(seconds);
            show.Invoke(hud,new object[]{hud.timeDigits,20,true});
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("output/timer-fix");File.WriteAllText("output/timer-fix/validation.txt",report+"PASS: all level budgets plus 99/100 boundary and leading-zero display.");
            return report.ToString();
        }
        finally{EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
    }
}
