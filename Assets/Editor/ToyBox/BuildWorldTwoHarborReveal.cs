#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildWorldTwoHarborReveal
{
    public const string Path="Assets/TwilightMap/WorkshopRailway/WorldTwoHarborReveal.prefab";
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var map=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var source=map.illustratedMenu.artwork.GetComponent<RawImage>();
        if(source.material==null||!source.material.HasProperty("_WorldReveal"))throw new Exception("Harbor reveal shader missing.");
        var root=new GameObject("WorldTwoHarborReveal",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        try
        {
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
            var backdrop=new GameObject("Backdrop",typeof(RectTransform),typeof(Image));backdrop.transform.SetParent(root.transform,false);
            var rect=(RectTransform)backdrop.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            backdrop.GetComponent<Image>().color=new Color(.015f,.035f,.055f);
            var art=new GameObject("ExistingWorldTwoHarbor",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));art.transform.SetParent(root.transform,false);
            var image=art.GetComponent<RawImage>();image.texture=source.texture;image.material=source.material;image.raycastTarget=false;
            image.uvRect=new Rect(820f/1672,181f/941,852f/1672,590f/941);
            var fit=art.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=852f/590;
            var reveal=root.AddComponent<WorldTwoHarborReveal>();reveal.artwork=image;reveal.fade=root.GetComponent<CanvasGroup>();reveal.duration=4.5f;
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        var game=SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");bool opened=!game.isLoaded;
        if(opened)game=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        try
        {
            LevelManager manager=null;
            foreach(var item in game.GetRootGameObjects()){manager=item.GetComponentInChildren<LevelManager>(true);if(manager!=null)break;}
            if(manager==null)throw new Exception("Gameplay level manager missing.");
            manager.worldTwoHarborRevealPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path).GetComponent<WorldTwoHarborReveal>();
            EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(game);EditorSceneManager.SaveScene(game);
            AssetDatabase.SaveAssets();
        }
        finally{if(opened)EditorSceneManager.CloseScene(game,true);}
        return "Saved actual-harbor reveal after level 30, followed by level 31 in the same game session.";
    }
}
#endif
