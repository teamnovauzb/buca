using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>All tutorial geometry, sprites and renderer slots are authored before entering Play Mode.</summary>
public static class TutorialContentBaker
{
    const string BoardPath="Assets/Resources/Tutorials/BoardCoachTutorial.prefab";
    [MenuItem("RealBuca/Prebuild/Bake Tutorial Content")]
    public static void Bake()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Bake in Edit Mode.");
        EditorApplication.ExecuteMenuItem("RealBuca/Toy Box 3D/Polish Board Coach Text");
        var root=PrefabUtility.LoadPrefabContents(BoardPath);
        try
        {
            var board=root.GetComponent<BoardCoachTutorial>();board.ApplyArcadePresentation();
            foreach(var face in root.GetComponentsInChildren<ArcadeButtonFace>(true))
            {
                face.raisedSprite=TutorialSpriteBaker.Face(face.capColor,false,face.arcadeButton+"Raised");
                face.pressedSprite=TutorialSpriteBaker.Face(face.capColor,true,face.arcadeButton+"Pressed");
                face.sprite=face.raisedSprite;face.color=Color.white;face.type=UnityEngine.UI.Image.Type.Simple;
            }
            var cinematic=root.GetComponent<CinematicMechanicTutorial>();
            if(cinematic==null)cinematic=root.AddComponent<CinematicMechanicTutorial>();cinematic.enabled=false;
            if(board.demoPuck==null)
            {
                board.demoPuck=new GameObject("SavedDemoPuckRenderers");board.demoPuck.transform.SetParent(root.transform,false);
                board.demoRenderers=new MeshRenderer[16];
                for(int i=0;i<board.demoRenderers.Length;i++)
                {
                    var slot=new GameObject("RendererSlot"+i,typeof(MeshFilter),typeof(MeshRenderer));
                    slot.transform.SetParent(board.demoPuck.transform,false);board.demoRenderers[i]=slot.GetComponent<MeshRenderer>();
                }
                board.demoPuck.SetActive(false);
            }
            if(board.demoGuide==null)
            {
                var guide=new GameObject("SavedTutorialGuide",typeof(LineRenderer));guide.transform.SetParent(root.transform,false);
                board.demoGuide=guide.GetComponent<LineRenderer>();
            }
            const string matPath="Assets/Materials/Prebuilt/TutorialGuide.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=new Color(.3f,.95f,.78f);AssetDatabase.CreateAsset(mat,matPath);}
            board.demoGuide.sharedMaterial=mat;board.demoGuide.positionCount=2;
            board.demoGuide.useWorldSpace=true;board.demoGuide.startWidth=.045f;board.demoGuide.endWidth=.025f;board.demoGuide.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,BoardPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        const string honeyPath="Assets/Resources/Tutorials/HoneyComparisonTutorial.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(honeyPath)!=null)
        {
            var honey=PrefabUtility.LoadPrefabContents(honeyPath);
            try
            {
                var view=honey.GetComponent<HoneyComparisonTutorial>();
                if(view.artwork.GetComponent<CanvasGroup>()==null)view.artwork.gameObject.AddComponent<CanvasGroup>();
                var sprite=TutorialSpriteBaker.Honey();
                foreach(var puck in honey.GetComponentsInChildren<HoneyDemoPuckGraphic>(true)){puck.sprite=sprite;puck.color=Color.white;}
                PrefabUtility.SaveAsPrefabAsset(honey,honeyPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(honey);}
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach(string path in new[]{"Assets/Scenes/Game.unity","Assets/Scenes/MainMenu.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                bool hasCoroutineManager=false;
                foreach(var obj in scene.GetRootGameObjects())
                    if(obj.GetComponentInChildren<Luxodd.Game.Scripts.HelpersAndUtils.CoroutineManager>(true)!=null)hasCoroutineManager=true;
                if(!hasCoroutineManager)new GameObject("CoroutineManager",typeof(Luxodd.Game.Scripts.HelpersAndUtils.CoroutineManager));
                foreach(var obj in scene.GetRootGameObjects())
                {
                    foreach(var legacy in obj.GetComponentsInChildren<HoneyTutorialBoard>(true))
                    {
                        legacy.blurTexture=Texture("TutorialBlur");legacy.blurScratch=Texture("TutorialBlurScratch");
                        const string blurPath="Assets/Materials/Prebuilt/TutorialBlur.mat";
                        legacy.blurMaterial=AssetDatabase.LoadAssetAtPath<Material>(blurPath);
                        if(legacy.blurMaterial==null){legacy.blurMaterial=new Material(Shader.Find("Hidden/Buca/GaussianBlur"));AssetDatabase.CreateAsset(legacy.blurMaterial,blurPath);}
                    }
                    foreach(var module in obj.GetComponentsInChildren<Luxodd.Game.EventSystemInputModuleSwitcher>(true))
                    {
#if ENABLE_INPUT_SYSTEM
                        if(module.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>()==null)module.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                        if(module.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>()==null)module.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                    }
                }
                if(path.EndsWith("Game.unity"))
                {
                    LevelManager manager=null;
                    foreach(var item in scene.GetRootGameObjects())
                        if(manager==null)manager=item.GetComponentInChildren<LevelManager>(true);
                    if(manager!=null && manager.puck!=null)
                    {
                        var saved=PrefabUtility.LoadPrefabContents(BoardPath);
                        try
                        {
                            var board=saved.GetComponent<BoardCoachTutorial>();int slot=0;
                            foreach(var source in manager.puck.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                var filter=source.GetComponent<MeshFilter>();if(filter==null)continue;
                                if(slot>=board.demoRenderers.Length)throw new Exception("Not enough baked puck renderer slots");
                                var dest=board.demoRenderers[slot++];
                                dest.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;dest.sharedMaterials=source.sharedMaterials;
                                dest.transform.localPosition=manager.puck.transform.InverseTransformPoint(source.transform.position);
                                dest.transform.localRotation=Quaternion.Inverse(manager.puck.transform.rotation)*source.transform.rotation;
                                dest.transform.localScale=source.transform.lossyScale;
                                dest.enabled=source.enabled;
                            }
                            PrefabUtility.SaveAsPrefabAsset(saved,BoardPath);
                        }
                        finally{PrefabUtility.UnloadPrefabContents(saved);}
                    }
                }
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally{if(Array.Exists(setup,x=>x.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        AssetDatabase.SaveAssets();
        PrebuiltContentValidation.Validate();
        Debug.Log("TUTORIAL_CONTENT_BAKED: saved button sprites, renderer slots, guide, components and legacy render targets.");
    }
    static RenderTexture Texture(string name)
    {
        string path="Assets/Tutorials/BakedControls/"+name+".renderTexture";
        var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if(texture==null){texture=new RenderTexture(960,540,0);texture.name=name;AssetDatabase.CreateAsset(texture,path);}
        return texture;
    }
}
