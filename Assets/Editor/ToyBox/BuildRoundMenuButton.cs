#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class BuildRoundMenuButton
{
    const string Folder="Assets/TwilightMap/WorkshopRailway/MenuButton";
    const string Output="output/round-menu-button";
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu required.");
        var control=map.menuLever;
        if(control==null)throw new Exception("Existing menu return control missing.");
        Directory.CreateDirectory(Output);
        if(!File.Exists(Output+"/MainMenu.before-button.unity"))File.Copy(map.gameObject.scene.path,Output+"/MainMenu.before-button.unity");
        string path=Folder+"/RoundMenuRed.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var root=(RectTransform)control.transform;
        root.anchorMin=root.anchorMax=root.pivot=Vector2.one*.5f;
        root.anchoredPosition=BuildForestHarborMap.Art(new Vector2(168,843));
        root.sizeDelta=new Vector2(142,146);root.localScale=Vector3.one;
        var old=root.GetComponent<UnityEngine.UI.RawImage>();old.texture=null;old.color=Color.clear;old.raycastTarget=true;
        if(control.handle!=null)control.handle.parent.gameObject.SetActive(false);
        if(control.objectCamera!=null)control.objectCamera.enabled=false;
        var instruction=root.Find("Instruction");if(instruction!=null)instruction.gameObject.SetActive(false);
        var picture=root.Find("RoundMenuArtwork") as RectTransform;
        if(picture==null)
        {
            picture=new GameObject("RoundMenuArtwork",typeof(RectTransform)).GetComponent<RectTransform>();
            picture.gameObject.layer=5;picture.SetParent(root,false);
        }
        picture.anchorMin=picture.anchorMax=picture.pivot=Vector2.one*.5f;
        picture.anchoredPosition=Vector2.zero;picture.localScale=Vector3.one;
        picture.sizeDelta=new Vector2(154f*texture.width/texture.height,154);
        var image=picture.GetComponent<UnityEngine.UI.RawImage>()??picture.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.texture=texture;image.color=Color.white;image.raycastTarget=false;
        control.pushButtonVisual=picture;control.ResetLever();
        AddArcadeBadge(picture,map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().destination.font);
        EditorUtility.SetDirty(control);EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved approved round menu button, retaining click, controller return, sound and transition.";
    }

    static void AddArcadeBadge(RectTransform picture,TMPro.TMP_FontAsset font)
    {
        var badge=picture.Find("ArcadeButton2") as RectTransform;
        if(badge==null){badge=new GameObject("ArcadeButton2",typeof(RectTransform)).GetComponent<RectTransform>();badge.gameObject.layer=5;badge.SetParent(picture,false);}
        badge.anchorMin=badge.anchorMax=badge.pivot=Vector2.one*.5f;
        badge.anchoredPosition=new Vector2(45,49);badge.sizeDelta=new Vector2(28,28);
        var rim=badge.GetComponent<UnityEngine.UI.Image>()??badge.gameObject.AddComponent<UnityEngine.UI.Image>();
        rim.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luxodd.Game/Example/Sprites/white_circle.png");
        rim.color=new Color(1,.86f,.52f);rim.raycastTarget=false;
        var face=badge.Find("RedFace") as RectTransform;
        if(face==null){face=new GameObject("RedFace",typeof(RectTransform)).GetComponent<RectTransform>();face.gameObject.layer=5;face.SetParent(badge,false);}
        face.anchorMin=face.anchorMax=face.pivot=Vector2.one*.5f;face.anchoredPosition=Vector2.zero;face.sizeDelta=new Vector2(24,24);
        var image=face.GetComponent<UnityEngine.UI.Image>()??face.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=rim.sprite;image.color=new Color(.77f,.035f,.025f);image.raycastTarget=false;
        var digit=face.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if(digit==null){var go=new GameObject("Number",typeof(RectTransform));go.layer=5;go.transform.SetParent(face,false);digit=go.AddComponent<TMPro.TextMeshProUGUI>();}
        digit.rectTransform.anchorMin=Vector2.zero;digit.rectTransform.anchorMax=Vector2.one;digit.rectTransform.offsetMin=digit.rectTransform.offsetMax=Vector2.zero;
        digit.font=font;digit.text=LuxoddNumberedLabels.DisplayNumber(ArcadeInputAdapter.Button.Red).ToString();
        digit.fontSize=17;digit.fontStyle=TMPro.FontStyles.Bold;digit.alignment=TMPro.TextAlignmentOptions.Center;digit.color=new Color(1,.96f,.82f);digit.raycastTarget=false;
    }

    public static string CheckAndRender()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);copy.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=null;map.motionAudio=null;
            int returned=0;map.Open(()=>returned++);copy.SetActive(true);
            var control=map.menuLever;control.returnTransition=null;
            if(control.pushButtonVisual==null)throw new Exception("Button art missing.");
            if(control.handle.parent.gameObject.activeSelf)throw new Exception("Old lever still visible.");
            var navigate=typeof(TwilightRailwayMap).GetMethod("Navigate",BindingFlags.Instance|BindingFlags.NonPublic);
            map.Select(2);
            navigate.Invoke(map,new object[]{Vector2.left,0f});
            if(map.Selected!=1||control.TickReturn(.49f)||control.Returning||returned!=0)throw new Exception("Left must select the previous level without returning to menu.");
            navigate.Invoke(map,new object[]{Vector2.left,.5f});
            if(map.Selected!=0||control.TickReturn(.49f)||returned!=0)throw new Exception("Held left must continue level navigation.");
            navigate.Invoke(map,new object[]{Vector2.left,1f});
            if(map.Selected!=0||control.Returning||returned!=0)throw new Exception("Left at the first level must stay on the map.");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            control.OnPointerClick(pointer);control.TickReturn(.19f);
            if(returned!=0||control.pushButtonVisual.localScale.x>=.99f)throw new Exception("Press feedback missing.");
            control.TickReturn(.30f);control.TickReturn(.30f);
            if(returned!=1)throw new Exception("Click return must fire once.");
            control.ResetLever();map.RequestMenuReturn();map.RequestMenuReturn();
            control.TickReturn(.19f);
            if(returned!=1||!control.Returning||control.pushButtonVisual.localScale.x>=.99f)throw new Exception("Arcade return bypasses button feedback.");
            control.TickReturn(.3f);control.TickReturn(.3f);
            if(returned!=2)throw new Exception("Arcade return must fire exactly once.");
            var badge=control.pushButtonVisual.Find("ArcadeButton2");
            if(badge==null||badge.GetComponentInChildren<TMPro.TMP_Text>().text!="2")throw new Exception("Red arcade number missing.");
            control.ResetLever();
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled=false;camera.scene=scene;
                if(camera.gameObject.activeInHierarchy&&camera.targetTexture!=null)
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture});
            }
            typeof(TwilightRailwayMap).GetField("highest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,49);
            typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{map,11,1672,941,Output+"/Updated-map.png"});
            var view=map.illustratedMenu;
            var hits=new System.Collections.Generic.List<RaycastResult>();
            pointer.position=RectTransformUtility.WorldToScreenPoint(view.canvas.worldCamera,control.transform.position);
            view.canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().Raycast(pointer,hits);
            if(hits.Count==0||hits[0].gameObject!=control.gameObject)throw new Exception("Menu button click is blocked.");
            string result="PASS: red arcade button 2 label; menu button raycast; press animation; delayed single click return; arcade/Escape return uses the same animation and single callback; left and held-left select previous levels without returning, including at level 1; all 50 level and footer button raycasts. Isolated Edit-mode checks, no physical arcade hardware verification or player progress changes.";
            File.WriteAllText(Output+"/validation.txt",result);return result;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
