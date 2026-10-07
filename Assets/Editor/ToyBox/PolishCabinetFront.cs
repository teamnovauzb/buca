using UnityEditor;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
    static void BuildCleanCabinetFront(Transform root,ToyBoxGameplayHud hud)
    {
        var old=root.Find("InsetCabinetScoreboard");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var oldTrim=root.Find("CleanCabinetTrim");if(oldTrim!=null)Object.DestroyImmediate(oldTrim.gameObject);
        hud.strokeDigits=new MeshFilter[0];hud.parDigits=new MeshFilter[0];
        var trim=Group("CleanCabinetTrim",root,new Vector3(1.25f,-.96f,-7.82f));
        var paint=Material("CabinetTrimSatin",new Color(.24f,.48f,.47f),.30f);
        Box("UpperFineTrim",trim,new Vector3(0,.20f,0),new Vector3(6.1f,.035f,.025f),paint,.012f);
        Box("LowerFineTrim",trim,new Vector3(0,-.20f,0),new Vector3(6.1f,.035f,.025f),paint,.012f);
    }
    public static void PolishCabinetFront()
    {
        PrepareFolders();ToyBoxGeometry.Initialize();
        var path=Root+"/Prefabs/SolidGameplayHud.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{BuildCleanCabinetFront(root.transform,root.GetComponent<ToyBoxGameplayHud>());PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("CLEAN_CABINET_SAVED");
    }
}
