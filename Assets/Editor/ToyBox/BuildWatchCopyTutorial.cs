using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    internal static readonly string[] TutorialKeys={"BASIC","RIM","BANK","MUD","MOVE","ICE","FAST","+PTS","!","PUSH","WIND","PULL","JUMP","KICK","WARP","SLIDE","SPIN","GATE","PEGS"};
    static readonly string[] TutorialTitles={"YOUR FIRST SHOT","BOUNCE OFF THE EDGE","BOUNCE OFF THE WALL","MUD SLOWS YOU DOWN","A MOVING GOAL","ICE KEEPS YOU SLIDING","RIDE THE BOOST","COLLECT THE GOLD","GO AROUND THE TRAPS","A MOVING FLOOR","AIM INTO THE WIND","CURVE AROUND THE PULL","BOUNCE PAD - CHANGE DIRECTION","BUMPER BOOST","IN HERE - OUT THERE","WAIT FOR A GAP","WATCH THE SPIN","WAIT UNTIL IT OPENS","GUARD PEGS - AIM THROUGH THE GAP"};

    [MenuItem("RealBuca/Toy Box 3D/5 - Generate Watch Then Try Tutorials")]
    public static void GenerateWatchCopyTutorials()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders(); ToyBoxGeometry.Initialize(); MakeMaterials(); MakeSkinMaterials();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Single);
        InstallWatchCopy(scene);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("WATCH_COPY_BAKED: 19 saved 3D lessons and animation clips assigned in Game.");
    }

    static void InstallWatchCopy(Scene scene)
    {
        if(!AssetDatabase.IsValidFolder(Root+"/Animations")) AssetDatabase.CreateFolder(Root,"Animations");
        WatchCopyTutorial3D old=null;
        ObstacleIntroController intro=null; LevelManager manager=null; Camera gameCamera=null; ToyBoxGameplayHud hud=null;
        foreach(var root in scene.GetRootGameObjects())
        {
            if(root.GetComponent<WatchCopyTutorial3D>() is WatchCopyTutorial3D found) old=found;
            if(root.GetComponentInChildren<ObstacleIntroController>(true) is ObstacleIntroController i) intro=i;
            if(root.GetComponentInChildren<LevelManager>(true) is LevelManager m) manager=m;
            if(root.GetComponentInChildren<ToyBoxGameplayHud>(true) is ToyBoxGameplayHud h) hud=h;
            var c=root.GetComponent<Camera>(); if(c!=null && c.CompareTag("MainCamera")) gameCamera=c;
        }
        if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        if(intro==null || manager==null || gameCamera==null) throw new InvalidOperationException("Gameplay tutorial services missing.");
        var host=new GameObject("SavedWatchThenTryTutorial");
        SceneManager.MoveGameObjectToScene(host,scene);
        host.transform.position=new Vector3(60,0,0);
        var view=host.AddComponent<WatchCopyTutorial3D>();
        var stage=Group("Presentation",host.transform); view.presentation=stage.gameObject;
        var common=Group("Common",stage);
        Box("BlueCabinet",common,new Vector3(0,-.48f,0),new Vector3(11.2f,.96f,11.9f),blue,.24f);
        Box("MapleBoard",common,new Vector3(0,.02f,0),new Vector3(10.4f,.18f,11.2f),surface,.09f);
        foreach(int side in new[]{-1,1})
        {
            Box("SideRail",common,new Vector3(side*5.4f,.32f,0),new Vector3(.48f,.62f,11.5f),blue,.2f);
            Box("EndRail",common,new Vector3(0,.32f,side*5.6f),new Vector3(11.2f,.62f,.48f),blue,.2f);
            foreach(int end in new[]{-1,1}) Disc("CornerPlug",common,new Vector3(side*5.37f,.65f,end*5.57f),.22f,.08f,yellow,.03f);
        }
        Box("ControlShelf",common,new Vector3(0,-.35f,-6.8f),new Vector3(11,.65f,2.2f),wood,.25f);
        Box("TitleFrame",common,new Vector3(0,1.8f,5.5f),new Vector3(10.5f,.8f,.22f),wood,.13f);
        Text("WATCH AND LEARN",common,new Vector3(0,1.86f,5.35f),.51f,blue,false,.13f,.01f);
        Box("ActionPlaque",common,new Vector3(0,.72f,-5.94f),new Vector3(6.2f,.68f,.20f),wood,.10f);
        view.messages=new GameObject[9];
        string[] labels={"WATCH ME PLAY","1  MOVE THE JOYSTICK","2  HOLD SHOOT","3  LET GO TO SHOOT","READY TO PLAY","1  MOVE THE JOYSTICK","2  HOLD SHOOT","3  LET GO TO SHOOT","YOUR TURN - AIM AND SHOOT"};
        for(int i=0;i<9;i++) view.messages[i]=Text(labels[i],common,new Vector3(0,.80f,-6.07f),i==8?.25f:.31f,blue);
        view.watchLabel=Text("PAUSE",common,new Vector3(-3.7f,1.12f,-5.89f),.25f,mint);
        view.yourTurnLabel=Text("DONE",common,new Vector3(-3.7f,1.12f,-5.89f),.25f,mint);
        view.pausedLabel=Text("RESUME",common,new Vector3(-3.7f,1.12f,-5.89f),.25f,mint);
        var pauseTarget=Group("PausePlayback",common,new Vector3(-3.7f,1.12f,-5.89f));
        var pauseCollider=pauseTarget.gameObject.AddComponent<BoxCollider>();
        pauseCollider.size=new Vector3(1.5f,.5f,.35f); view.pauseHit=pauseCollider;
        Box("PlaybackTrack",common,new Vector3(0,1.37f,5.34f),new Vector3(8.1f,.09f,.08f),blue,.03f);
        view.playbackFill=Box("PlaybackProgress",common,new Vector3(-4,1.37f,5.29f),new Vector3(8f,.06f,.08f),mint,.02f).transform;
        view.progressLights=new GameObject[3];
        for(int i=0;i<3;i++)
        {
            Disc("ProgressSocket",common,new Vector3(1.05f+i*.65f,.08f,-7.7f),.20f,.13f,wood,.03f);
            view.progressLights[i]=Disc("ProgressLight"+i,common,new Vector3(1.05f+i*.65f,.18f,-7.7f),.14f,.08f,glow,.03f);
            view.progressLights[i].transform.localRotation=Quaternion.identity;
        }
        var joystickBase=Group("JoystickBase",common,new Vector3(-1.85f,.02f,-6.9f));
        Disc("Socket",joystickBase,Vector3.zero,.70f,.20f,blue,.06f);
        view.joystick=Group("Joystick",joystickBase,new Vector3(0,.12f,0));
        Disc("Stick",view.joystick,new Vector3(0,.30f,0),.10f,.60f,wood,.03f);
        var ball=Box("JoystickBall",view.joystick,new Vector3(0,.67f,0),Vector3.one*.66f,coral,.325f);
        view.aimHit=ball.AddComponent<SphereCollider>(); ((SphereCollider)view.aimHit).radius=.50f;
        view.aimGlow=MeshObject("AimLight",joystickBase,new Vector3(0,.12f,0),ToyBoxGeometry.Torus(.76f,.06f),glow);
        var fire=Button(common,"",new Vector3(1.7f,.02f,-6.9f),.61f,black,.2f,false);
        view.fireCap=fire.cap; view.fireHit=fire.hit; view.fireGlow=fire.selectedRing;

        var replay=Button(common,"",new Vector3(-4.15f,.02f,-6.95f),.70f,mint,.17f,false);
        replay.cap.parent.name="Replay_Button";
        view.replayLabel=Text("REPEAT",replay.cap,new Vector3(0,.20f,0),.17f,cream,true,.22f,.024f,.04f);
        view.retryLabel=Text("RETRY",replay.cap,new Vector3(0,.20f,0),.17f,cream,true,.22f,.024f,.04f);
        var skip=Button(common,"",new Vector3(4.2f,.02f,-6.95f),.80f,yellow,.21f,false);
        skip.cap.parent.name="Continue_Button";
        view.skipLabel=Text("SKIP",skip.cap,new Vector3(0,.26f,0),.26f,cream,true,.22f,.024f,.04f);
        view.letsPlayLabel=Text("CONTINUE",skip.cap,new Vector3(0,.26f,0),.17f,cream,true,.22f,.024f,.04f);
        view.nextLessonLabel=Text("CONTINUE",skip.cap,new Vector3(0,.26f,0),.17f,cream,true,.22f,.024f,.04f);
        view.controlSelectors=new[]{replay.selectedRing,
            Box("PauseSelector",common,new Vector3(-3.7f,.91f,-5.96f),new Vector3(1.5f,.06f,.08f),glow,.025f),skip.selectedRing};
        view.repeatCap=replay.cap; view.continueCap=skip.cap;
        view.repeatRest=replay.cap.localPosition; view.continueRest=skip.cap.localPosition;
        view.countdownLabels=new GameObject[5];
        for(int seconds=1;seconds<=5;seconds++)
            view.countdownLabels[seconds-1]=Text("CONTINUE IN "+seconds,common,new Vector3(0,.80f,-6.07f),.35f,blue);
        view.replayHit=replay.hit; view.skipHit=skip.hit;

        view.hand=Group("Glove",common,new Vector3(-1.85f,1.2f,-7.1f));
        view.hand.localRotation=Quaternion.Euler(-15,0,-12);
        view.hand.localScale=Vector3.one*.48f;
        view.hand.gameObject.SetActive(false);
        Box("SoftPalm",view.hand,Vector3.zero,new Vector3(.93f,.45f,1.04f),cream,.22f);
        for(int f=0;f<4;f++)
        {
            var finger=Box("Finger"+f,view.hand,new Vector3((f-1.5f)*.23f,-.06f,.58f-(f==3?.10f:0)),
                new Vector3(.235f,.34f,f==0?.79f:.60f),cream,.115f);
            finger.transform.localRotation=Quaternion.Euler(f==0?-12:20,0,0);
        }
        var thumb=Box("Thumb",view.hand,new Vector3(.57f,-.06f,.10f),new Vector3(.38f,.38f,.62f),cream,.18f);
        thumb.transform.localRotation=Quaternion.Euler(0,-35,0);
        Box("GloveCuff",view.hand,new Vector3(0,0,-.65f),new Vector3(.99f,.55f,.30f),cream,.14f);

        view.lessons=new WatchCopyTutorial3D.Lesson[TutorialKeys.Length];
        for(int i=0;i<TutorialKeys.Length;i++) BuildLesson(stage,view,i);
        var roomAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ToyPlayroom.prefab");
        if(roomAsset!=null)
        {
            var room=(GameObject)PrefabUtility.InstantiatePrefab(roomAsset,stage);
            room.transform.localPosition=new Vector3(0,-.72f,0);
            // Reuse Game's directional lights; no duplicate sun or global volume.
            foreach(var light in room.GetComponentsInChildren<Light>(true)) light.enabled=false;
            foreach(var volume in room.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)) volume.enabled=false;
        }
        var camera=Group("TutorialCamera",stage).gameObject;
        view.lessonCamera=camera.AddComponent<Camera>();
        view.lessonCamera.fieldOfView=38; view.lessonCamera.nearClipPlane=.1f; view.lessonCamera.farClipPlane=100;
        camera.transform.localRotation=Quaternion.Euler(34,0,0);
        var data=camera.AddComponent<UniversalAdditionalCameraData>();
        data.SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer()); data.renderPostProcessing=true;
        data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        BuildTeddyCoach(view);
        view.FitCameraAspect(1.6f);
        stage.gameObject.SetActive(false);
        var prefab=PrefabUtility.SaveAsPrefabAsset(host,Root+"/Prefabs/WatchThenTryTutorial.prefab");
        UnityEngine.Object.DestroyImmediate(host);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        view=instance.GetComponent<WatchCopyTutorial3D>();
        view.gameplayCamera=gameCamera; view.gameplayHud=hud!=null?hud.gameObject:null;
        intro.watchCopy=view; EditorUtility.SetDirty(intro);
        PrefabUtility.RecordPrefabInstancePropertyModifications(intro);
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        if(manager.tutorial!=null)
        {
            manager.tutorial.gameObject.SetActive(false);
            manager.tutorial=null;
            EditorUtility.SetDirty(manager);
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
        }
    }

    static void BuildLesson(Transform stage,WatchCopyTutorial3D view,int index)
    {
        string key=TutorialKeys[index];
        var lesson=Group("Lesson_"+key.Replace("+","Plus").Replace("!","Hazard"),stage);
        Text(TutorialTitles[index],lesson,new Vector3(0,1.15f,5.27f),.29f,ink);
        var puck=Group("Puck",lesson,new Vector3(-1,.24f,-3.8f));
        Disc("CoralPuck",puck,Vector3.zero,.43f,.26f,coral,.07f);
        Ring("CreamInlay",puck,new Vector3(0,.138f,0),.30f,.045f,cream,.016f);
        var goal=Group("Goal",lesson,new Vector3(0,.16f,3.9f));
        Disc("DarkRecess",goal,new Vector3(0,.005f,0),.64f,.09f,black,.02f);
        MeshObject("MintRim",goal,new Vector3(0,.055f,0),ToyBoxGeometry.Torus(.64f,.10f),mint);
        var goalGlow=MeshObject("GoalLight",goal,new Vector3(0,.055f,0),ToyBoxGeometry.Torus(.83f,.045f),glow);
        goalGlow.SetActive(false);
        var prop=Group("Mechanic",lesson);
        switch(key)
        {
            case "RIM": case "BANK":
                Box("BounceWall",prop,new Vector3(1.85f,.55f,0),new Vector3(.34f,.88f,3.0f),key=="BANK"?blue:yellow,.14f); break;
            case "MUD": case "ICE": case "FAST": case "PUSH": case "WIND": case "JUMP":
                Box("SurfacePatch",prop,new Vector3(0,.14f,0),new Vector3(3.3f,.08f,2.3f),key=="MUD"?pucks[2]:key=="ICE"?mint:yellow,.035f);
                if(key!="MUD" && key!="ICE")
                    for(int a=0;a<3;a++)
                        foreach(int side in new[]{-1,1})
                        {
                            var arrow=Box("ArrowPeg",prop,new Vector3((a-1)*.85f+side*.10f,.22f,0),new Vector3(.10f,.08f,.36f),blue,.035f);
                            arrow.transform.localRotation=Quaternion.Euler(0,side*-40,0);
                        }
                if(key=="JUMP") prop.Find("SurfacePatch").localRotation=Quaternion.Euler(0,145,0);
                break;
            case "!":
                for(int i=0;i<3;i++) Disc("AvoidTrap"+i,prop,new Vector3((i-1)*.6f,.30f,0),.26f,.40f,rails[3],.08f);
                break;
            case "PULL":
                Box("GravityBall",prop,new Vector3(-1.3f,.70f,.2f),Vector3.one*.95f,rails[3],.47f);
                Ring("GravityField",prop,new Vector3(-1.3f,.14f,.2f),1.25f,.05f,rails[3]); break;
            case "KICK": Disc("Kicker",prop,new Vector3(.55f,.42f,0),.55f,.62f,yellow,.14f); break;
            case "WARP":
                foreach(float z in new[]{-1.2f,1.7f})
                {
                    Disc("PortalWell",prop,new Vector3(0,.16f,z),.62f,.10f,black,.03f);
                    MeshObject("PortalRing",prop,new Vector3(0,.23f,z),ToyBoxGeometry.Torus(.62f,.10f),z<0?rails[3]:mint);
                }
                break;
            case "SLIDE": case "SPIN": case "GATE":
                Box("MovingWall",prop,new Vector3(0,.49f,0),new Vector3(3,.76f,.34f),blue,.14f); break;
            case "PEGS":
                for(int i=0;i<3;i++)
                    Disc("GuardPeg"+i,prop,new Vector3((i-1)*1.35f,.54f,i==1?2.28f:2.4f),.16f,.85f,yellow,.045f);
                Text("AIM BETWEEN THE POSTS",prop,new Vector3(0,.20f,1.1f),.22f,blue,true);
                break;
            case "+PTS":
                for(int i=0;i<3;i++) Disc("Gold"+i,prop,new Vector3(-.65f+i*.3f,.36f,-1.4f+i*1.3f),.17f,.18f,yellow,.06f); break;
        }
        var clip=new AnimationClip { name="WatchCopy_"+index, legacy=true, wrapMode=WrapMode.ClampForever };
        string lp=lesson.name;
        Vector3 start=new Vector3(key=="PEGS"?-3.45f:-1,.24f,-3.8f),finish=key=="JUMP"?new Vector3(2.4f,.24f,-3f):new Vector3(0,.24f,3.9f);
        if(key=="JUMP") goal.localPosition=new Vector3(finish.x,.16f,finish.z);
        puck.localPosition=start;
        var path=new List<Vector3>{start};
        var times=new List<float>{4f};
        if(key=="BANK" || key=="RIM") { path.Add(new Vector3(1.23f,.24f,0)); times.Add(5.5f); }
        else if(key=="!") { path.Add(new Vector3(2,.24f,0)); times.Add(5.5f); }
        else if(key=="JUMP") { path.Add(new Vector3(0,.24f,0)); times.Add(5.55f); }
        else if(key=="PULL") { path.Add(new Vector3(-.35f,.24f,.2f)); times.Add(5.5f); }
        else if(key=="PUSH" || key=="WIND") { path.Add(new Vector3(1.45f,.24f,.4f)); times.Add(5.6f); }
        else if(key=="KICK") { path.Add(new Vector3(-.45f,.24f,0)); times.Add(5.7f); }
        else if(key=="MUD") { path.Add(new Vector3(-.45f,.24f,-1.1f)); times.Add(4.6f); path.Add(new Vector3(-.25f,.24f,.8f)); times.Add(6.55f); }
        else if(key=="ICE" || key=="FAST") { path.Add(new Vector3(-.45f,.24f,-1.1f)); times.Add(5.1f); path.Add(new Vector3(-.15f,.24f,2.8f)); times.Add(5.55f); }
        else if(key=="SLIDE" || key=="SPIN" || key=="GATE") { path.Add(start); times.Add(4.7f); }
        if(key=="WARP")
        {
            path.Add(new Vector3(0,.24f,-1.2f));times.Add(5f);
            path.Add(new Vector3(0,.24f,1.7f));times.Add(5.3f);
            Curve(clip,lp+"/Puck","localScale.x",new[]{0f,4.95f,5.02f,5.28f,5.35f,7.5f},new[]{1f,1f,0f,0f,1f,1f});
            Curve(clip,lp+"/Puck","localScale.y",new[]{0f,4.95f,5.02f,5.28f,5.35f,7.5f},new[]{1f,1f,0f,0f,1f,1f});
            Curve(clip,lp+"/Puck","localScale.z",new[]{0f,4.95f,5.02f,5.28f,5.35f,7.5f},new[]{1f,1f,0f,0f,1f,1f});
        }
        var aimArrow=Group("AimArrow",lesson,new Vector3(start.x,.16f,start.z+.4f));
        Vector3 firstTarget=path.Count>1?path[1]:finish;
        Vector3 direction=firstTarget-start; direction.y=0;
        if(direction.sqrMagnitude<.01f) direction=finish-start;
        aimArrow.localRotation=Quaternion.LookRotation(direction);
        float aimYaw=aimArrow.localEulerAngles.y;
        Curve(clip,lp+"/AimArrow","localEulerAnglesRaw.y",new[]{0f,1f,2f,7.5f},new[]{aimYaw,aimYaw+30f,aimYaw,aimYaw});
        Box("ArrowShaft",aimArrow,new Vector3(0,0,.55f),new Vector3(.10f,.06f,.95f),mint,.025f);
        foreach(int side in new[]{-1,1})
        {
            var wing=Box("ArrowHead",aimArrow,new Vector3(side*.13f,0,.96f),new Vector3(.10f,.06f,.40f),mint,.025f);
            wing.transform.localRotation=Quaternion.Euler(0,side*-45,0);
        }
        foreach(string axis in new[]{"x","y","z"})
            Curve(clip,lp+"/AimArrow","localScale."+axis,new[]{0f,3.9f,4f,7.5f},new[]{1f,1f,0f,0f});
        path.Add(finish);times.Add(7f); path.Add(new Vector3(finish.x,.04f,finish.z));times.Add(7.5f);
        for(int axis=0;axis<3;axis++)
        {
            var values=new List<float>{start[axis]}; var t=new List<float>{0};t.AddRange(times);
            foreach(var point in path) values.Add(point[axis]);
            Curve(clip,lp+"/Puck","localPosition."+"xyz"[axis],t.ToArray(),values.ToArray());
        }
        if(key=="MOVE") Curve(clip,lp+"/Goal","localPosition.x",new[]{0f,1.5f,3f,4.5f,6f,7f,7.5f},new[]{0f,1.1f,0f,-1.1f,.7f,0f,0f});
        if(key=="SLIDE") Curve(clip,lp+"/Mechanic","localPosition.x",new[]{0f,2f,4f,5f,7.5f},new[]{0f,1.5f,0f,3.1f,3.1f});
        if(key=="SPIN") Curve(clip,lp+"/Mechanic","localEulerAnglesRaw.y",new[]{0f,2f,4f,5f,7.5f},new[]{0f,90f,180f,90f,90f});
        if(key=="GATE") Curve(clip,lp+"/Mechanic","localScale.y",new[]{0f,3.5f,4f,4.7f,7.5f},new[]{1f,1f,0f,0f,0f});
        if(key=="+PTS")
            for(int i=0;i<3;i++) Curve(clip,lp+"/Mechanic/Gold"+i,"localScale.y",new[]{0f,4.7f+i*.55f,4.9f+i*.55f,7.5f},new[]{1f,1f,0f,0f});
        Curve(clip,"Common/JoystickBase/Joystick","localEulerAnglesRaw.z",new[]{0f,1f,2f,3f,7.5f},new[]{15f,-18f,0f,0f,0f});
        for(int axis=0;axis<3;axis++)
        {
            Vector3 a=new Vector3(-1.85f,.99f,-7.12f),b=new Vector3(1.7f,.64f,-7.15f);
            Curve(clip,"Common/Glove","localPosition."+"xyz"[axis],new[]{0f,1.5f,2.2f,3.6f,4f,7.5f},
                new[]{a[axis],a[axis],b[axis],b[axis],b[axis]+(axis==1?.65f:0),b[axis]+(axis==1?.65f:0)});
        }
        Curve(clip,"Common/_Button/MovingCap","localPosition.y",new[]{0f,2.2f,2.5f,3.6f,4f,7.5f},new[]{.3f,.3f,.18f,.18f,.3f,.3f});
        clip=ToyBoxGeometry.Save(clip,Root+"/Animations/WatchCopy_"+index.ToString("00")+".anim");
        view.lessons[index]=new WatchCopyTutorial3D.Lesson{key=key,root=lesson.gameObject,demonstration=clip,aimArrow=aimArrow,goalGlow=goalGlow};
        lesson.gameObject.SetActive(false);
    }

    static void Curve(AnimationClip clip,string path,string property,float[] times,float[] values)
    {
        var curve=new AnimationCurve();
        for(int i=0;i<times.Length;i++) curve.AddKey(new Keyframe(times[i],values[i]));
        for(int i=0;i<curve.length;i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
        }
        clip.SetCurve(path,typeof(Transform),property,curve);
    }
}
