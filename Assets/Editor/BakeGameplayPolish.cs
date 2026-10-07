using UnityEditor;
using UnityEngine;

public static class BakeGameplayPolish
{
    [MenuItem("RealBuca/Prebuild/Bake Gameplay Polish")]
    public static void Run()
    {
        SaveShot("Assets/ToyBoxMenu/Levels/Level_02.prefab","RIM",49,1,1);
        SaveShot("Assets/ToyBoxMenu/Levels/Level_11.prefab","!",-107,1,1);
        SaveShot("Assets/ToyBoxMenu/Levels/Level_13.prefab","WIND",-10,1,1);
        EditorApplication.ExecuteMenuItem("RealBuca/Toy Box 3D/Polish Board Coach Text");
        AssetDatabase.SaveAssets();
        PrebuiltContentValidation.Validate();
        VerifyLuxoddArcadeInput.Run();
        Debug.Log("GAMEPLAY_POLISH_BAKED: authored demo shots, saved caption layout, arcade mapping verified.");
    }
    static void SaveShot(string path,string mechanic,float yaw,float first,float second)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var setup=root.GetComponent<TutorialShotSetup>();
            if(setup==null)setup=root.AddComponent<TutorialShotSetup>();
            setup.shots=new[]{new TutorialShotSetup.Shot{mechanic=mechanic,yaw=yaw,firstPower=first,secondPower=second}};
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
