using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/Polish Gameplay Board and Rugs")]
    public static void PolishGameplayBoard()
    {
        if(EditorApplication.isPlaying) return;
        PrepareFolders(); ToyBoxGeometry.Initialize();
        var trim=Material("BoardDepthTrim",new Color(.035f,.12f,.16f),.25f);
        foreach(var path in Directory.GetFiles(Root+"/Levels","*.prefab"))
        {
            var level=PrefabUtility.LoadPrefabContents(path);
            try {
                var shell=level.transform.Find("SavedToyBoardShell"); if(shell==null) continue;
                var old=shell.Find("DepthDetails");if(old!=null) Object.DestroyImmediate(old.gameObject);
                var details=Group("DepthDetails",shell);
                foreach(int side in new[]{-1,1}) {
                    Box("InsetSideSeam",details,new Vector3(side*5.36f,-.43f,0),new Vector3(.045f,.09f,14.55f),trim,.02f);
                    Box("InsetEndSeam",details,new Vector3(0,-.43f,side*7.86f),new Vector3(9.8f,.09f,.045f),trim,.02f);
                    Box("InnerRailFoot",details,new Vector3(side*4.51f,-.05f,0),new Vector3(.10f,.10f,14.2f),trim,.025f);
                }
                PrefabUtility.SaveAsPrefabAsset(level,path);
            } finally { PrefabUtility.UnloadPrefabContents(level); }
        }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach(var root in scene.GetRootGameObjects()) if(root.name=="SavedToyPlayroom") MirrorRug(root.transform);
        EditorSceneManager.SaveScene(scene);
        var room=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/ToyPlayroom.prefab");
        try {MirrorRug(room.transform);PrefabUtility.SaveAsPrefabAsset(room,Root+"/Prefabs/ToyPlayroom.prefab");}
        finally {PrefabUtility.UnloadPrefabContents(room);}
        AssetDatabase.SaveAssets();
        Debug.Log("BOARD_POLISH_SAVED: dimensional trim across level prefabs and matching left rug.");
    }
    static void MirrorRug(Transform room)
    {
        var rug=room.Find("StripedRug");if(rug==null || room.Find("StripedRugLeft")!=null)return;
        var left=Object.Instantiate(rug.gameObject,room);left.name="StripedRugLeft";
        var p=rug.localPosition;left.transform.localPosition=new Vector3(-p.x,p.y,p.z);
    }
}
