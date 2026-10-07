using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
    static void StandTeddyBesideBasket(Transform basket)
    {
        if(basket.Find("StandingTeddy")!=null)return;
        var teddy=new GameObject("StandingTeddy").transform;
        teddy.SetParent(basket,false);
        var parts=new System.Collections.Generic.List<Transform>();
        foreach(Transform child in basket)if(child.name.StartsWith("Teddy"))parts.Add(child);
        foreach(var child in parts)child.SetParent(teddy,false);
        teddy.localPosition=new Vector3(2.1f,-.87f,-.4f);
    }
    [MenuItem("RealBuca/Toy Box 3D/Stand Bear Beside Basket")]
    public static void PlaceStandingTeddy()
    {
        if(EditorApplication.isPlaying)return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        foreach(var root in scene.GetRootGameObjects())
            if(root.name=="SavedToyPlayroom")StandTeddyBesideBasket(root.transform.Find("BasketAndTeddy"));
        EditorSceneManager.SaveScene(scene);
        var room=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/ToyPlayroom.prefab");
        try {StandTeddyBesideBasket(room.transform.Find("BasketAndTeddy"));PrefabUtility.SaveAsPrefabAsset(room,Root+"/Prefabs/ToyPlayroom.prefab");}
        finally {PrefabUtility.UnloadPrefabContents(room);}
        AssetDatabase.SaveAssets();Debug.Log("STANDING_TEDDY_SAVED");
    }
}
