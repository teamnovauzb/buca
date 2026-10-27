#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class BuildRailwayMenuLever
{
    const string Folder="Assets/TwilightMap/ForestHarbor/MenuLever";
    const int Layer=25;
    static Transform Node(Transform p,string n){var g=new GameObject(n);g.layer=Layer;g.transform.SetParent(p,false);return g.transform;}
    static Transform Part(Transform p,string n,Mesh mesh,Vector3 pos,Material mat)
    {var t=Node(p,n);t.localPosition=pos;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
    static Transform Box(Transform p,string n,Vector3 pos,Vector3 size,float r,Material mat)=>Part(p,n,ToyBoxGeometry.RoundedBox(size,r),pos,mat);
    static Material Mat(string name,Color color,float gloss)
    {
        var m=new Material(Shader.Find("BUCA/Woodland Toy"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",gloss);m.SetFloat("_GrainStrength",.08f);
        return ToyBoxGeometry.Save(m,Folder+"/"+name+".mat");
    }
    static RectTransform Rect(Transform p,string name,Vector2 pos,Vector2 size)
    {
        var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.gameObject.layer=5;t.SetParent(p,false);
        t.anchorMin=t.anchorMax=t.pivot=Vector2.one*.5f;t.sizeDelta=size;t.anchoredPosition=pos;return t;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu required");
        if(map.menuLever!=null)return "Lever already installed";
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();ToyBoxGeometry.Initialize();
        var view=map.illustratedMenu;
        var root=Rect(view.artwork,"RailwayMenuLever",new Vector2(285-836,470.5f-810),new Vector2(205,195));
        var lever=root.gameObject.AddComponent<RailwayMenuLever>();lever.map=map;map.menuLever=lever;
        var image=root.gameObject.AddComponent<UnityEngine.UI.RawImage>();image.raycastTarget=true;
        var rig=Node(map.transform,"MenuLeverGeometry");rig.localPosition=new Vector3(0,-1600,0);
        var wood=Mat("Maple",new Color(.83f,.56f,.26f),.3f);
        wood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));wood.SetFloat("_GrainStrength",.2f);wood.SetFloat("_TextureTint",0);
        var navy=Mat("NavyEnamel",new Color(.025f,.10f,.22f),.5f);
        var red=Mat("RedHandle",new Color(.72f,.055f,.025f),.48f);
        var gold=Mat("Brass",new Color(.93f,.61f,.22f),.65f);
        var cream=Mat("Ivory",new Color(1,.91f,.67f),.25f);
        Box(rig,"Maple plinth",new Vector3(0,.12f,0),new Vector3(1.6f,.24f,1.05f),.10f,wood);
        Box(rig,"Navy foot",new Vector3(0,.30f,0),new Vector3(1.37f,.17f,.85f),.07f,navy);
        Box(rig,"Wood pedestal",new Vector3(0,.66f,0),new Vector3(1.1f,.65f,.67f),.10f,wood);
        Box(rig,"Front navy inset",new Vector3(0,.68f,-.35f),new Vector3(.82f,.47f,.065f),.08f,navy);
        for(int i=0;i<3;i++)Box(rig,"Raised menu bar",new Vector3(0,.81f-i*.13f,-.396f),new Vector3(.42f,.052f,.035f),.02f,cream);
        var housing=Part(rig,"Round pivot housing",ToyBoxGeometry.Disc(.42f,.38f,.05f),new Vector3(0,1.12f,0),navy);housing.localRotation=Quaternion.Euler(90,0,0);
        var pin=Part(rig,"Brass axle",ToyBoxGeometry.Disc(.14f,.44f,.04f),new Vector3(0,1.12f,0),gold);pin.localRotation=Quaternion.Euler(90,0,0);
        var handle=Node(rig,"MovingHandle");handle.localPosition=new Vector3(0,1.12f,.04f);lever.handle=handle;
        Box(handle,"Lever arm",new Vector3(0,.43f,0),new Vector3(.20f,.86f,.20f),.055f,navy);
        Box(handle,"Brass collar",new Vector3(0,.79f,0),new Vector3(.25f,.16f,.24f),.04f,gold);
        Box(handle,"Rounded red grip",new Vector3(0,1.04f,0),new Vector3(.43f,.57f,.36f),.17f,red);
        for(int i=0;i<2;i++)
        {var bolt=Part(rig,"Foot bolt",ToyBoxGeometry.Disc(.065f,.035f,.01f),new Vector3(i==0?-.64f:.64f,.26f,-.34f),gold);}
        var word=Part(rig,"MENU lettering",ToyBoxGeometry.Text("MENU",.045f),new Vector3(0,.13f,-.54f),navy);word.localScale=Vector3.one*.17f;
        var camNode=Node(rig,"LeverCamera");var camera=camNode.gameObject.AddComponent<Camera>();lever.objectCamera=camera;
        var offset=new Vector3(2.3f,2.4f,-7);camNode.localPosition=new Vector3(0,1.1f,0)+offset;camNode.localRotation=Quaternion.LookRotation(-offset);
        camera.orthographic=true;camera.orthographicSize=1.55f*rig.lossyScale.x;camera.nearClipPlane=.03f;camera.farClipPlane=30;
        camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.depth=-12;camera.allowHDR=false;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=new RenderTexture(820,780,24,RenderTextureFormat.ARGB32){name="Menu lever live 3D",antiAliasing=4};AssetDatabase.CreateAsset(rt,Folder+"/Lever.renderTexture");camera.targetTexture=rt;image.texture=rt;camera.aspect=820f/780;
        var light=Node(rig,"Lever key light").gameObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<Layer;light.intensity=1.4f;light.transform.localRotation=Quaternion.Euler(35,-35,0);
        var label=Rect(root,"Instruction",new Vector2(0,-92),new Vector2(215,40));
        var plaque=label.gameObject.AddComponent<UnityEngine.UI.Image>();plaque.color=new Color(.96f,.81f,.53f);plaque.raycastTarget=false;
        var textRect=Rect(label,"Text",Vector2.zero,new Vector2(210,38));var text=textRect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        text.font=view.playText.font;text.text="PUSH LEFT\nFOR MENU";text.fontSize=13;text.fontStyle=TMPro.FontStyles.Bold;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(.015f,.055f,.13f);text.raycastTarget=false;
        lever.ResetLever();BootFromMainMenu.SetEnabled(true);
        EditorUtility.SetDirty(map);EditorUtility.SetDirty(wood);EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved mesh railway lever, live camera, joystick return and instruction. Main menu startup enabled.";
    }
}
#endif
