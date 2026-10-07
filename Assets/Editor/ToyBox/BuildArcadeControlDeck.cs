using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/15 - Generate Arcade Control Deck")]
    public static void GenerateArcadeControlDeck()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        PrepareFolders(); ToyBoxGeometry.Initialize();
        string path=Root+"/Prefabs/SolidGameplayHud.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud=root.GetComponent<ToyBoxGameplayHud>();
            if(hud.hints!=null) UnityEngine.Object.DestroyImmediate(hud.hints);
            BuildArcadeControlDeck(root.transform,hud);
            ValidateControlJoystick(hud);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("ARCADE_CONTROL_DECK_SAVED: three separate pods: joystick, Black Shoot and Green Undo.");
    }

    static void ValidateControlJoystick(ToyBoxGameplayHud hud)
    {
        var animate=typeof(ToyBoxGameplayHud).GetMethod("AnimateJoystick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var socket=hud.joystickPivot.parent;var rest=socket.localRotation;
        for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
        {
            if(x==0 && y==0)continue;
            for(int frame=0;frame<45;frame++)animate.Invoke(hud,new object[]{new Vector2(x,y),1f/60f});
            var tip=hud.joystickPivot.localRotation*Vector3.up;
            if((x!=0 && tip.x*x<.15f)||(y!=0 && tip.z*y<.15f)||Vector3.Angle(Vector3.up,tip)>38.1f)
                throw new InvalidOperationException("Joystick direction or tilt limit failed.");
            if(socket.localRotation!=rest)throw new InvalidOperationException("Joystick socket moved.");
        }
        for(int frame=0;frame<60;frame++)animate.Invoke(hud,new object[]{Vector2.zero,1f/60f});
        if(Quaternion.Angle(hud.joystickPivot.localRotation,Quaternion.identity)>.01f)throw new InvalidOperationException("Joystick failed to center.");
        Debug.Log("CONTROL_JOYSTICK_VALIDATED: eight directions, 38 degree tilt limit, fixed socket and release-to-center.");
    }

    static void BuildArcadeControlDeck(Transform root,ToyBoxGameplayHud hud)
    {
        var navy=Material("ControlDeckBlue",new Color(.018f,.12f,.32f),.55f);
        var silver=Material("ControlDeckSilver",new Color(0.43f,0.49f,0.54f),0.34f);silver.SetFloat("_Metallic",.10f);
        var white=Material("ControlDeckLettering",new Color(.90f,.92f,.88f),.25f);
        var black=Material("ControlDeckBlack",new Color(.27f,.29f,.32f),.38f);
        var green=Material("ControlDeckGreen",new Color(.018f,.66f,.065f),.66f);
        var cyan=Material("ControlDeckCyan",new Color(.018f,.55f,.75f),.45f);
        cyan.EnableKeyword("_EMISSION");cyan.SetColor("_EmissionColor",new Color(.01f,.45f,.7f)*1.4f);
        var hints=Group("PhysicalControlHints",root,new Vector3(0,.64f,-7.80f));
        hints.localRotation=Quaternion.Euler(90,0,0);
        var teal=Material("ControlPodTeal",new Color(0.27f,0.36f,0.42f),0.3f);
        var slate=Material("ControlPodSlate",new Color(0.56f,0.66f,0.75f),0.34f);
        for(int i=0;i<3;i++)
        {
            float x=(i-1f)*2.3f;
            var pod=Group("SeparateControlPod"+i,hints,new Vector3(x,0,.02f));
            pod.localRotation=Quaternion.Euler(-90,0,0);
            Disc("TealFoot",pod,Vector3.zero,1.02f,.19f,teal,.075f);
            Disc("NavyPod",pod,new Vector3(0,.20f,0),.94f,.38f,slate,.12f);
        }
        // Model axes face out of the panel. Geometry is baked into the saved prefab.
        var redGrip=Material("ClearArcadeRedGrip",new Color(0.88f,0.25f,0.18f),0.38f);
        var stem=Material("ClearArcadeSilverStem",new Color(.67f,.73f,.79f),.42f);stem.SetFloat("_Metallic",.25f);
        var basePaint=Material("ClearArcadeNavyBase",new Color(0.16f,0.23f,0.3f),0.32f);
        var rimPaint=Material("ClearArcadeCyanRim",new Color(0.4f,0.51f,0.59f),0.34f);
        var stick=Group("ClearArcadeJoystick",hints,new Vector3(-2.3f,.18f,-.39f));
        stick.localRotation=Quaternion.Euler(-90,0,0);
        Disc("SolidNavyBase",stick,Vector3.zero,.49f,.16f,basePaint,.045f);
        Ring("CyanPerimeter",stick,new Vector3(0,.091f,0),.465f,.026f,rimPaint,.025f);
        Disc("CentralSocket",stick,new Vector3(0,.14f,0),.165f,.13f,basePaint,.035f);
        hud.joystickDirectionDot=Disc("AimDirectionLight",stick,new Vector3(0,.14f,.42f),.06f,.035f,cyan,.015f).transform;
        hud.joystickDirectionDot.gameObject.SetActive(false);
        var upright=Group("UprightJoystickMount",stick,new Vector3(0,.18f,0));upright.localRotation=Quaternion.identity;
        hud.joystickPivot=Group("MovingJoystick",upright);
        Disc("SilverStem",hud.joystickPivot,new Vector3(0,.36f,0),.075f,.72f,stem,.018f);
        Box("RedBallGrip",hud.joystickPivot,new Vector3(0,.91f,0),Vector3.one*.67f,redGrip,.33f);
        hud.joystickHit=stick.gameObject.AddComponent<SphereCollider>(); ((SphereCollider)hud.joystickHit).radius=1.05f; ((SphereCollider)hud.joystickHit).center=new Vector3(0,.6f,0);
        Text("JOYSTICK",hints,new Vector3(-2.9f,-.60f,-.41f),.13f,white);
        string[] labels={"SHOOT","UNDO"};Material[] caps={black,green};
        for(int i=0;i<2;i++)
        {
            float x=i*2.3f;
            var socket=Group(labels[i]+"Socket",hints,new Vector3(x,.18f,-.39f));socket.localRotation=Quaternion.Euler(-90,0,0);
            float buttonScale=i==0?1.5f:1.15f;
            socket.localScale=Vector3.one*buttonScale;
            Disc("SilverButtonBezel",socket,Vector3.zero,.39f,.14f,silver,.025f);
            Disc("DarkSocketInset",socket,new Vector3(0,.09f,0),.335f,.075f,black,.013f);
            var cap=Group(labels[i]+"Cap",hints,new Vector3(x,.18f,-.55f));cap.localRotation=Quaternion.Euler(-90,0,0);
            cap.localScale=Vector3.one*buttonScale;
            var face=Disc(labels[i]+"Button",cap,Vector3.zero,.31f,.21f,caps[i],.078f);
            Text(labels[i],hints,new Vector3(x,-.60f,-.41f),.13f,white);
            if(i==0) { hud.shootHit=face.AddComponent<BoxCollider>(); ((BoxCollider)hud.shootHit).size=new Vector3(.78f,.25f,.78f); }
            if(i==1)
            {
                hud.undoCap=cap;
                hud.undoHit=face.AddComponent<BoxCollider>(); ((BoxCollider)hud.undoHit).size=new Vector3(.78f,.25f,.78f);
                Ring("UndoReady",socket,new Vector3(0,.17f,0),.37f,.025f,cyan,.025f);
                hud.undoHighlight=socket.Find("UndoReady").gameObject;
                hud.undoHighlight.SetActive(false);
            }

        }
        hud.hints=hints.gameObject;
        BuildReadableControlInstructions(hints);
        var oldAllowance=root.Find("ShotAllowance");if(oldAllowance!=null)UnityEngine.Object.DestroyImmediate(oldAllowance.gameObject);
        var allowance=Group("ShotAllowance",root,new Vector3(0,.92f,7f));allowance.localRotation=Quaternion.Euler(12,0,0);
        Box("AllowancePanel",allowance,Vector3.zero,new Vector3(3.45f,.92f,.24f),navy,.10f);
        var unlimited=Group("UnlimitedShots",allowance);hud.unlimitedShotsLabel=unlimited.gameObject;
        Text("UNLIMITED SHOTS",unlimited,new Vector3(0,.17f,-.15f),.20f,white);
        Text("WATCH THE TIMER",unlimited,new Vector3(0,-.20f,-.15f),.16f,white);
        BuildHeartIndicator(allowance,hud);
    }
    static Mesh ClearArcadeArrowMesh()
    {
        // Solid arrow with a rectangular tail and triangular head, generated only in editor.
        var p=new[]{new Vector2(-.036f,-.095f),new Vector2(.036f,-.095f),new Vector2(.036f,0),new Vector2(.085f,0),new Vector2(0,.105f),new Vector2(-.085f,0),new Vector2(-.036f,0)};
        var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
        for(int layer=0;layer<2;layer++)foreach(var point in p)v.Add(new Vector3(point.x,layer*.027f,point.y));
        int[] cap={0,1,2,0,2,6,6,2,4,2,3,4,6,4,5};
        for(int i=0;i<cap.Length;i+=3){t.AddRange(new[]{cap[i],cap[i+1],cap[i+2],cap[i]+7,cap[i+2]+7,cap[i+1]+7});}
        for(int i=0;i<7;i++){int j=(i+1)%7;t.AddRange(new[]{i,j+7,j,i,i+7,j+7});}
        var mesh=new Mesh{name="ClearArcadeDirectionArrow"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Root+"/Meshes/ClearArcadeDirectionArrow.asset");
    }

}
