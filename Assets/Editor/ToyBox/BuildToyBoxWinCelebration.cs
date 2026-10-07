using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/6 - Generate Premium Win Celebration")]
    public static void GenerateWinCelebration()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders(); ToyBoxGeometry.Initialize(); MakeMaterials(); MakeSkinMaterials();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Single);
        InstallWinCelebration(scene);
        WoodenHoleInOneBaker.Bake();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("PREMIUM_WIN_BAKED");
    }

    static void InstallWinCelebration(Scene scene)
    {
        LevelManager manager=null;
        foreach(var obj in scene.GetRootGameObjects())
        {
            if(obj.GetComponent<ToyBoxWinCelebration>()!=null) UnityEngine.Object.DestroyImmediate(obj);
            else if(obj.GetComponentInChildren<LevelManager>(true)!=null) manager=obj.GetComponentInChildren<LevelManager>(true);
        }
        if(manager==null) throw new System.InvalidOperationException("LevelManager missing.");
        var root=new GameObject("PremiumWinCelebration"); SceneManager.MoveGameObjectToScene(root,scene);
        var view=root.AddComponent<ToyBoxWinCelebration>();
        var stage=Group("SavedHoleSparkle",root.transform); view.presentation=stage;
        var gold=Material("HoleSparkleGold",new Color(1f,.76f,.20f),.72f);
        gold.SetFloat("_Metallic",.22f); gold.EnableKeyword("_EMISSION"); gold.SetColor("_EmissionColor",new Color(1,.56f,.10f)*.9f); EditorUtility.SetDirty(gold);
        view.goldRing=MeshObject("SmallGoldRing",stage,new Vector3(0,.30f,0),ToyBoxGeometry.Torus(.84f,.065f),gold).transform;
        var diamond=new Mesh {name="HoleSparkleDiamond"};
        diamond.vertices=new[]{new Vector3(0,.30f,0),new Vector3(0,-.30f,0),new Vector3(-.15f,0,0),new Vector3(.15f,0,0),new Vector3(0,0,-.15f),new Vector3(0,0,.15f)};
        diamond.triangles=new[]{0,4,2,0,3,4,0,5,3,0,2,5,1,2,4,1,4,3,1,3,5,1,5,2};
        diamond.RecalculateNormals(); diamond.RecalculateBounds();
        diamond=ToyBoxGeometry.Save(diamond,Root+"/Meshes/HoleSparkleDiamond.asset");
        view.sparkles=new Transform[6];
        for(int i=0;i<view.sparkles.Length;i++)
            view.sparkles[i]=MeshObject("GoldSparkle"+i,stage,Vector3.zero,diamond,gold).transform;
        stage.gameObject.SetActive(false);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/PremiumWinCelebration.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        manager.premiumWinCelebration=instance.GetComponent<ToyBoxWinCelebration>();
        EditorUtility.SetDirty(manager); PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
    }
}
