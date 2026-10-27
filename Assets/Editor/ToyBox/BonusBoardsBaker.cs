#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Twenty separately authored bank-shot layouts. Original campaign assets are untouched.</summary>
public static class BonusBoardsBaker
{
    const string Folder="Assets/ToyBoxMenu/BonusBoards";
    // x, z, length, angle around Y. All lanes leave room for the puck.
    static readonly Vector4[][] Layouts={
        new[]{new Vector4(-1.5f,-2,4.8f,0),new Vector4(1.5f,1,4.8f,0),new Vector4(-1.6f,3.5f,3.2f,0)},
        new[]{new Vector4(0,-2,3.8f,30),new Vector4(-2,1,3.2f,-30),new Vector4(2,3,3.2f,30)},
        new[]{new Vector4(-2,-2,3.6f,0),new Vector4(2,-.5f,3.6f,0),new Vector4(-2,1.5f,3.6f,0),new Vector4(2,3.7f,3.6f,0)},
        new[]{new Vector4(-1.2f,-1.5f,4.5f,90),new Vector4(1.3f,1.8f,4.5f,90),new Vector4(-2.7f,3.8f,2.4f,20)},
        new[]{new Vector4(0,-2,4.0f,-20),new Vector4(-2.5f,.3f,2.8f,35),new Vector4(1.9f,2.4f,3.8f,-25),new Vector4(-1.4f,4,2,0)},
        new[]{new Vector4(-2.2f,-3,3.5f,0),new Vector4(1.5f,-.9f,4.8f,0),new Vector4(-1.5f,1.5f,4.8f,0),new Vector4(2.1f,3.8f,3.5f,0)},
        new[]{new Vector4(-2,-2,3.5f,35),new Vector4(2,-2,3.5f,-35),new Vector4(0,.9f,3.4f,0),new Vector4(-2.3f,3.3f,3,30),new Vector4(2.3f,3.3f,3,-30)},
        new[]{new Vector4(-1.8f,-2.5f,4.1f,0),new Vector4(1.4f,-.2f,4.8f,0),new Vector4(-1.5f,2.1f,4.5f,0),new Vector4(2,4,3.6f,0)},
        new[]{new Vector4(-2,-2,4,60),new Vector4(1.5f,-.5f,4,-60),new Vector4(-1.7f,2,3.5f,25),new Vector4(1.7f,3.9f,3.2f,-20)},
        new[]{new Vector4(-1.8f,-3,4.5f,0),new Vector4(1.8f,-1.2f,4.5f,0),new Vector4(-1.8f,.8f,4.5f,0),new Vector4(1.8f,2.8f,4.5f,0),new Vector4(-2.3f,4.5f,2.8f,0)},
        new[]{new Vector4(1.8f,-3,4.3f,-8),new Vector4(-1.7f,-.8f,4.4f,8),new Vector4(1.6f,1.6f,4.2f,-8),new Vector4(-2,3.9f,3.7f,8)},
        new[]{new Vector4(-2,-2,3.8f,55),new Vector4(2,-2,3.8f,-55),new Vector4(0,1.0f,3.6f,0),new Vector4(-2.4f,3.7f,2.6f,-25),new Vector4(2.4f,3.7f,2.6f,25)},
        new[]{new Vector4(-1.4f,-2.2f,4.7f,0),new Vector4(1.1f,.2f,4.6f,90),new Vector4(-1.6f,3.2f,4.6f,0),new Vector4(2.7f,4.6f,2,15)},
        new[]{new Vector4(0,-3,3.3f,0),new Vector4(-2.4f,-.8f,3.1f,30),new Vector4(2.4f,-.8f,3.1f,-30),new Vector4(0,1.7f,3.3f,0),new Vector4(-2.4f,4,3,0)},
        new[]{new Vector4(1.5f,-3.1f,4.6f,0),new Vector4(-1.3f,-.9f,4.5f,0),new Vector4(1.7f,1.4f,4.3f,0),new Vector4(-1.8f,3.7f,4.0f,0)},
        new[]{new Vector4(-2.2f,-2.8f,3.2f,45),new Vector4(1.7f,-1.1f,4,25),new Vector4(-1.6f,1.2f,4.3f,-20),new Vector4(2.2f,3.3f,3.2f,-45),new Vector4(-2.3f,4.5f,2.3f,0)},
        new[]{new Vector4(-1.4f,-2,4.5f,90),new Vector4(1.4f,.6f,4.8f,90),new Vector4(-2.8f,3.2f,2.2f,0),new Vector4(2.5f,4,2.8f,0)},
        new[]{new Vector4(-1.8f,-3.1f,4.6f,0),new Vector4(1.6f,-1.1f,4.6f,0),new Vector4(-1.6f,1.1f,4.6f,0),new Vector4(1.8f,3.2f,4.6f,0),new Vector4(-2.4f,4.8f,2.6f,0)},
        new[]{new Vector4(-2.1f,-2.5f,3.6f,35),new Vector4(2.1f,-2.5f,3.6f,-35),new Vector4(-1.5f,.2f,4.3f,0),new Vector4(1.6f,2.5f,4.4f,0),new Vector4(-2.3f,4.5f,2.7f,0)},
        new[]{new Vector4(1.7f,-3.3f,4.6f,0),new Vector4(-1.7f,-1.4f,4.6f,0),new Vector4(1.7f,.5f,4.6f,0),new Vector4(-1.7f,2.4f,4.6f,0),new Vector4(1.7f,4.3f,4.6f,0)}
    };
    static readonly string[] Names={"THE SWITCHBACK","BRASS BANKS","FOUR CORNERS","LONG WAY ROUND","DIAMOND EXPRESS","CROSS COUNTRY","DOUBLE FUNNEL","NIGHT EXPRESS","LAST CONNECTION","HALFWAY EXPRESS","SWITCH TRACKS","MOUNTAIN PASS","SIGNAL BOX","CLOVER JUNCTION","THE LONG CROSSING","BRASS VIADUCT","NARROW GAUGE","MIDNIGHT SPECIAL","HOME STRETCH","GRAND TERMINUS"};
    public static string Bake()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Clean edit mode required.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/ToyBoxMenu","BonusBoards");
        ToyBoxGeometry.Initialize();var prefabs=new GameObject[20];var settings=new LevelSettings[20];
        for(int i=0;i<20;i++)
        {
            var root=PrefabUtility.LoadPrefabContents("Assets/ToyBoxMenu/Levels/Level_01.prefab");
            try
            {
                root.name=$"Bonus_{i+31:00}_{Names[i]}";
                var source=root.transform.Find("Guide_L");var material=source.GetComponent<Renderer>().sharedMaterial;
                var physicsMaterial=source.GetComponent<Collider>().sharedMaterial;
                foreach(string name in new[]{"Guide_L","Guide_R","Nudge_L","Nudge_R"})UnityEngine.Object.DestroyImmediate(root.transform.Find(name).gameObject);
                root.transform.Find("PuckStart").localPosition=new Vector3(0,.08f,-5.4f);
                for(int j=0;j<Layouts[i].Length;j++)
                {
                    var a=Layouts[i][j];var g=new GameObject("BonusBank"+j,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider),typeof(RailLight));
                    g.transform.SetParent(root.transform,false);g.transform.localPosition=new Vector3(a.x,.28f,a.y);g.transform.localRotation=Quaternion.Euler(0,a.w,0);
                    Vector3 size=new Vector3(a.z,.55f,.28f);g.GetComponent<MeshFilter>().sharedMesh=ToyBoxGeometry.RoundedBox(size,.10f);g.GetComponent<MeshRenderer>().sharedMaterial=material;
                    g.GetComponent<BoxCollider>().size=size;g.GetComponent<BoxCollider>().sharedMaterial=physicsMaterial;
                }
                prefabs[i]=PrefabUtility.SaveAsPrefabAsset(root,$"{Folder}/Level_{i+31:00}.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var config=ScriptableObject.CreateInstance<LevelSettings>();config.displayName=Names[i];config.timeLimit=117+i*3;config.maxLives=3;config.threeStarStrokes=5+i/2;
            settings[i]=ToyBoxGeometry.Save(config,$"{Folder}/Level_{i+31:00}_Settings.asset");
        }
        string restore=SceneManager.GetActiveScene().path;var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            manager.levelPrefabs=manager.levelPrefabs.Take(30).Concat(prefabs).ToArray();manager.levelSettings=manager.levelSettings.Take(30).Concat(settings).ToArray();
            EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        finally{if(!string.IsNullOrEmpty(restore))EditorSceneManager.OpenScene(restore);}
        return "Saved twenty bonus boards, levels 31–50; original 30 prefabs/settings retained.";
    }
}
#endif
