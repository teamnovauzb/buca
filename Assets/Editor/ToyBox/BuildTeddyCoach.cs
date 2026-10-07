using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/Polish Teddy Coach Tutorial")]
    public static void PolishTeddyCoachTutorial()
    {
        PrepareFolders(); ToyBoxGeometry.Initialize();
        const string path="Assets/ToyBoxMenu/Prefabs/WatchThenTryTutorial.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try { BuildTeddyCoach(root.GetComponent<WatchCopyTutorial3D>()); PrefabUtility.SaveAsPrefabAsset(root,path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    static void BuildTeddyCoach(WatchCopyTutorial3D view)
    {
        var stage=view.presentation.transform;
        var previous=stage.Find("TeddyCoach"); if(previous!=null) Object.DestroyImmediate(previous.gameObject);
        var coach=Group("TeddyCoach",stage);
        var fur=Material("CoachHoneyFur",new Color(.60f,.32f,.13f),.12f);
        var soft=Material("CoachCream",new Color(.98f,.86f,.62f),.25f);
        var teal=Material("CoachTeal",new Color(.045f,.39f,.42f),.35f);
        var dark=Material("CoachInk",new Color(.025f,.07f,.12f),.15f);
        var glint=Material("CoachEyeGlint",Color.white,.3f);
        view.coachBody=Group("Bear",coach,new Vector3(-7.55f,-2.05f,-.5f));
        var bear=view.coachBody;
        bear.localScale=Vector3.one*1.3f;
        Box("Body",bear,new Vector3(0,1.15f,0),new Vector3(1.7f,2.1f,1.2f),fur,.58f);
        var belly=Box("Belly",bear,new Vector3(0,1.1f,-.59f),new Vector3(1.05f,1.3f,1.05f),soft,.5f);
        belly.transform.localScale=new Vector3(1,1,.14f);
        var head=Group("Head",bear,new Vector3(0,2.6f,-.05f));
        Box("Head",head,Vector3.zero,new Vector3(1.85f,1.65f,1.25f),fur,.60f);
        view.coachEyes=new Transform[2];
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1:1;
            Box("Ear",head,new Vector3(side*.78f,.66f,0),Vector3.one*.66f,fur,.32f);
            Box("InnerEar",head,new Vector3(side*.78f,.66f,-.29f),new Vector3(.37f,.37f,.10f),soft,.049f);
            Box("Foot",bear,new Vector3(side*.49f,.18f,-.24f),new Vector3(.80f,.58f,1f),fur,.28f);
            var eye=Group("Eye"+i,head,new Vector3(side*.35f,.12f,-.635f));
            Box("Pupil",eye,Vector3.zero,new Vector3(.18f,.23f,.13f),dark,.064f);
            Box("Glint",eye,new Vector3(-.028f,.04f,-.069f),Vector3.one*.045f,glint,.022f);
            view.coachEyes[i]=eye;
        }
        Box("Muzzle",head,new Vector3(0,-.32f,-.67f),new Vector3(.83f,.57f,.32f),soft,.15f);
        Box("Nose",head,new Vector3(0,-.20f,-.865f),new Vector3(.28f,.18f,.12f),dark,.059f);
        Box("Smile",head,new Vector3(0,-.43f,-.837f),new Vector3(.29f,.055f,.035f),dark,.017f);
        Box("BowLeft",bear,new Vector3(-.19f,1.93f,-.67f),new Vector3(.39f,.32f,.15f),teal,.074f);
        Box("BowRight",bear,new Vector3(.19f,1.93f,-.67f),new Vector3(.39f,.32f,.15f),teal,.074f);
        Box("LeftArm",bear,new Vector3(-.97f,1.18f,0),new Vector3(.60f,1.22f,.62f),fur,.29f);
        view.coachArm=Group("PointingArm",bear,new Vector3(.79f,1.65f,0));
        Box("Arm",view.coachArm,new Vector3(.39f,0,0),new Vector3(1.15f,.57f,.60f),fur,.28f);
        var bubble=Group("Speech",coach,new Vector3(-7.35f,3.35f,-.2f));
        var panel=Box("CreamBubble",bubble,Vector3.zero,new Vector3(3.65f,1.3f,1.3f),soft,.59f);
        panel.transform.localScale=new Vector3(1,1,.16f);
        view.coachMessages=new GameObject[5];
        string[] words={"WATCH ME!","MOVE TO AIM","HOLD SHOOT","LET GO!","YOUR TURN!"};
        for(int i=0;i<words.Length;i++) {view.coachMessages[i]=Text(words[i],bubble,new Vector3(0,0,-.12f),.30f,dark);view.coachMessages[i].SetActive(i==0);}
        var common=stage.Find("Common");
        foreach(Transform child in common)
            if(child.GetComponent<MeshRenderer>()!=null && child.localPosition.y>1.7f && child.localPosition.z>5.2f && child.name!="TitleFrame") child.gameObject.SetActive(false);
        var title=common.Find("TeddyTitle"); if(title!=null) Object.DestroyImmediate(title.gameObject);
        Text("LET'S LEARN TOGETHER",common,new Vector3(0,1.86f,5.35f),.40f,dark).name="TeddyTitle";
        string[] prompts={"WATCH TEDDY","MOVE TO AIM","HOLD SHOOT","RELEASE!","YOUR TURN","MOVE TO AIM","HOLD SHOOT","RELEASE!","YOUR TURN"};
        for(int i=0;i<view.messages.Length;i++)
        {
            if(view.messages[i]!=null) Object.DestroyImmediate(view.messages[i]);
            view.messages[i]=Text(prompts[i],common,new Vector3(0,.80f,-6.07f),.30f,dark);
            view.messages[i].SetActive(i == 0);
        }
        foreach (var label in view.countdownLabels) label.SetActive(false);
        foreach(var renderer in common.GetComponentsInChildren<MeshRenderer>(true))
            if(renderer.name=="BlueCabinet" || renderer.name=="SideRail" || renderer.name=="EndRail") renderer.sharedMaterial=teal;
        var grip = view.joystick.Find("JoystickBall");
        var gripMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ClearArcadeRedGrip.mat");
        if (grip != null && gripMaterial != null) grip.GetComponent<Renderer>().sharedMaterial = gripMaterial;
        view.hand.gameObject.SetActive(false);
        var guide=Material("CoachAimBlue",new Color(.015f,.27f,.65f),.15f);
        guide.shader=Shader.Find("Universal Render Pipeline/Unlit"); guide.SetColor("_BaseColor",new Color(.015f,.27f,.65f)); EditorUtility.SetDirty(guide);
        foreach(var lesson in view.lessons)
            foreach(var renderer in lesson.aimArrow.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial=guide;
        foreach(var t in stage.GetComponentsInChildren<Transform>(true))
            if(t.name=="StandingTeddy") t.gameObject.SetActive(false);
        BuildLevelSevenLessons(view);
        view.FitCameraAspect(16f/9f);
    }
}
