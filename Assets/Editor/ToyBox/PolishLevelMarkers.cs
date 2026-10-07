using UnityEditor;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
    static void AddLevelMarkers(ToyBoxMenuController.PuckButton button)
    {
        var green=Material("LevelCompleteGreen",new Color(.03f,.38f,.16f),.3f);
        var gold=Material("LevelCompleteGold",new Color(.95f,.68f,.16f),.3f);
        if(button.cap.Find("CompletedBadge")!=null)return;
        var badge=Group("CompletedBadge",button.cap,new Vector3(.37f,.23f,-.35f));
        Disc("GoldMedal",badge,Vector3.zero,.16f,.06f,gold,.018f);
        var a=Box("CheckShort",badge,new Vector3(-.045f,.04f,0),new Vector3(.045f,.025f,.12f),green,.01f);a.transform.localRotation=Quaternion.Euler(0,-40,0);
        var b=Box("CheckLong",badge,new Vector3(.035f,.04f,.025f),new Vector3(.045f,.025f,.20f),green,.01f);b.transform.localRotation=Quaternion.Euler(0,35,0);
        button.awards=new[]{badge.gameObject};badge.gameObject.SetActive(false);
        button.selectedRing.transform.localScale=new Vector3(1.16f,1.4f,1.16f);
    }
    public static void PolishLevelMarkers()
    {
        PrepareFolders();ToyBoxGeometry.Initialize();
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try {var menu=root.GetComponent<ToyBoxMenuController>();foreach(var b in menu.levelButtons)AddLevelMarkers(b);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}
        finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("LEVEL_MARKERS_SAVED");
    }
}
