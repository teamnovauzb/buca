#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public static class BuildCompletionRailwayMap
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
        var map=Object.FindFirstObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null)throw new System.Exception("Open MainMenu to bake the real map");
        System.IO.Directory.CreateDirectory("Assets/Resources");AssetDatabase.Refresh();
        var copy=Object.Instantiate(map.gameObject);copy.SetActive(false);
        try{
            var m=copy.GetComponent<TwilightRailwayMap>();m.lockLevelsAfterFirstForTesting=false;
            foreach(var listener in copy.GetComponentsInChildren<AudioListener>(true))Object.DestroyImmediate(listener);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))if(camera.targetTexture==null)camera.depth=100;
            PrefabUtility.SaveAsPrefabAsset(copy,"Assets/Resources/CompletionRailwayMap.prefab");AssetDatabase.SaveAssets();
        }finally{Object.DestroyImmediate(copy);}
        return "Saved actual map for in-session completion journeys.";
    }
}
#endif
