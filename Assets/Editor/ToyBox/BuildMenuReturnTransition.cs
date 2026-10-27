#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class BuildMenuReturnTransition
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode only");
        var menu=Object.FindAnyObjectByType<ToyBoxMenuController>(FindObjectsInactive.Include);
        if(menu==null || menu.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new System.Exception("MainMenu required");
        var transition=menu.GetComponent<MenuReturnTransition>();if(transition==null)transition=menu.gameObject.AddComponent<MenuReturnTransition>();
        menu.menuReturnTransition=transition;transition.homeCamera=menu.menuCamera;
        var overlay=menu.transform.Find("MenuReturnCurtain");
        if(overlay==null){overlay=new GameObject("MenuReturnCurtain",typeof(RectTransform)).transform;overlay.SetParent(menu.transform,false);}
        overlay.gameObject.layer=5;
        var canvas=overlay.GetComponent<Canvas>();if(canvas==null)canvas=overlay.gameObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;transition.overlay=canvas;
        if(overlay.GetComponent<UnityEngine.UI.GraphicRaycaster>()==null)overlay.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var fill=overlay.Find("SoftNavyVeil") as RectTransform;
        if(fill==null){fill=new GameObject("SoftNavyVeil",typeof(RectTransform)).GetComponent<RectTransform>();fill.SetParent(overlay,false);}
        fill.gameObject.layer=5;fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
        var image=fill.GetComponent<UnityEngine.UI.Image>();if(image==null)image=fill.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color=new Color(.018f,.035f,.055f,0);image.raycastTarget=true;transition.curtain=image;
        foreach(string name in new[]{"RailwayLeverMechanism"})
        {
            var path="Assets/Audio/UI/"+name+".wav";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
        }
        var lever=menu.twilightMap.menuLever;lever.returnTransition=transition;
        lever.pullSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/RailwayLeverMechanism.wav");
        transition.sweepSound=null; // The lever mechanism is the only return sound.
        overlay.gameObject.SetActive(false);lever.ResetLever();EditorUtility.SetDirty(lever);EditorUtility.SetDirty(menu);EditorUtility.SetDirty(transition);
        PolishRailwayMenuLever.Apply();
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);EditorSceneManager.SaveScene(menu.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved centered smaller lettering, layered latch audio and smooth full-screen return to Home.";
    }
}
#endif
