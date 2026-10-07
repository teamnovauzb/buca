using System;
using System.Reflection;
using UnityEngine;

public static class FeedbackGameplayValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(LevelManager manager,string name,object value) => typeof(LevelManager).GetField(name,Private).SetValue(manager,value);
    public static void Check(LevelManager manager)
    {
        if(LuxoddGameBridge.MergeBestStrokes(3,0)!=3 || LuxoddGameBridge.MergeBestStrokes(0,4)!=4 || LuxoddGameBridge.MergeBestStrokes(3,2)!=2 || LuxoddGameBridge.MergeBestStrokes(2,5)!=2)
            throw new Exception("Personal best must use fewer shots and tolerate old saves.");
        int level=manager.CurrentLevelIndex;
        Set(manager,"_timeRemaining",17f); Set(manager,"_shotCount",5);
        Set(manager,"_timerActive",true); Set(manager,"_isTransitioning",false);
        Set(manager,"_levelFailed",false); Set(manager,"_timeUpTriggered",false); Set(manager,"_obstacleIntroActive",false);
        manager.RestartCurrentLevel();
        if(manager.CurrentLevelIndex!=level || manager.CurrentShotCount!=0 || Mathf.Abs(manager.CurrentTimeRemaining-17)>0.01f)
            throw new Exception("Restart must reset shots on this level without granting time.");
        Set(manager,"_timeRemaining",0f);
        typeof(LevelManager).GetMethod("ContinueCurrentLevelWithFreshTimer",Private).Invoke(manager,null);
        if(manager.CurrentLevelIndex!=level || manager.CurrentShotCount!=0 || manager.CurrentTimeRemaining<30)
            throw new Exception("Continue must restore a fresh attempt on the same level.");
        var puck=manager.puck.GetComponent<PuckController>();
        // A first-time Level 1 restart can open the basic lesson. Finish its
        // presentation before checking gameplay rendering, without saving it seen.
        var lesson=UnityEngine.Object.FindAnyObjectByType<WatchCopyTutorial3D>();
        if(lesson!=null && lesson.IsShowing)
            typeof(WatchCopyTutorial3D).GetMethod("Close",Private).Invoke(lesson,new object[]{true});
        if(puck.fineAimRotationSpeed>=puck.arcadeAimRotationSpeed || puck.fineAimRotationSpeed<=0)
            throw new Exception("Fine aiming is not slower.");
        // Regression: joystick aim must be visible even before Shoot is held.
        typeof(PuckController).GetField("_isDragging",Private).SetValue(puck,true);
        typeof(PuckController).GetField("_arcadeAimDir",Private).SetValue(puck,Vector3.forward);
        typeof(PuckController).GetField("_arcadePower",Private).SetValue(puck,0f);
        typeof(PuckController).GetField("_arcadeFireWasHeld",Private).SetValue(puck,false);
        typeof(PuckController).GetMethod("UpdateArcadeInput",Private).Invoke(puck,null);
        if(!puck.aimLine.enabled || puck.aimLine.widthMultiplier<=0 || Vector3.Distance(puck.aimLine.GetPosition(0),puck.aimLine.GetPosition(1))<.1f)
            throw new Exception("Uncharged joystick aim guide is invisible.");
        puck.ResetInputState();
        var hud=UnityEngine.Object.FindAnyObjectByType<ToyBoxGameplayHud>();
        Physics.SyncTransforms();
        var grip=hud.joystickPivot.Find("RedBallGrip");
        var pointer=Camera.main.WorldToScreenPoint(grip.position);
        var ray=Camera.main.ScreenPointToRay(pointer);
        if(!hud.joystickHit.Raycast(ray,out var joystickHit,100f)) throw new Exception("Visible red joystick grip cannot be pressed.");
        foreach(Vector2 direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right,new Vector2(1,1).normalized})
        {
            hud.HandlePanelPointer(ray,pointer,true,true);
            hud.HandlePanelPointer(ray,(Vector2)pointer+direction*70,false,true);
            typeof(PuckController).GetField("_cam",Private).SetValue(puck,Camera.main);
            typeof(PuckController).GetMethod("UpdateArcadeInput",Private).Invoke(puck,null);
            var aim=(Vector3)typeof(PuckController).GetField("_arcadeAimDir",Private).GetValue(puck);
            var expected=(Vector3.ProjectOnPlane(Camera.main.transform.right,Vector3.up).normalized*direction.x+
                Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized*direction.y).normalized;
            if(Vector3.Dot(aim,expected)<.99f) throw new Exception("Joystick drag and aim disagree: "+direction);
            hud.HandlePanelPointer(ray,pointer,false,false);
            if(ToyBoxGameplayHud.PanelStick!=Vector2.zero) throw new Exception("Joystick failed to release.");
            puck.ResetInputState();
        }
        Debug.Log("JOYSTICK_POINTER_PASSED: red grip hit, four directions, diagonal and release.");
        float previousPower=0;
        foreach(float distance in new[]{.3f,.6f,1f})
        {
            hud.HandlePanelPointer(ray,pointer,true,true);
            hud.HandlePanelPointer(ray,(Vector2)pointer+Vector2.up*70*distance,false,true);
            typeof(PuckController).GetField("_nextArcadePreviewTime",Private).SetValue(puck,0f);
            typeof(PuckController).GetMethod("UpdateArcadeInput",Private).Invoke(puck,null);
            float power=(float)typeof(PuckController).GetField("_arcadePower",Private).GetValue(puck);
            if(power<=previousPower || !puck.previewLine.enabled || puck.previewLine.positionCount<2)
                throw new Exception("Stick displacement failed to grow power or show prediction.");
            previousPower=power;
            hud.HandlePanelPointer(ray,pointer,false,false);
            typeof(PuckController).GetMethod("UpdateArcadeInput",Private).Invoke(puck,null);
            if((float)typeof(PuckController).GetField("_arcadePower",Private).GetValue(puck)!=power)
                throw new Exception("Releasing joystick lost selected strength.");
        }
        if(previousPower<.99f) throw new Exception("Full stick failed to select full power.");
        puck.ResetInputState();
        Debug.Log("STICK_POWER_PREVIEW_PASSED: light, medium, full displacement and retained power.");

        // A brief panel press without prior aim must still launch on release.
        var panelFire=typeof(ToyBoxGameplayHud).GetField("<PanelShoot>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
        var body=puck.GetComponent<Rigidbody>();
        panelFire.SetValue(null,true);
        try
        {
            typeof(PuckController).GetMethod("UpdateArcadeInput",Private).Invoke(puck,null);
            panelFire.SetValue(null,false);
            typeof(PuckController).GetMethod("UpdateArcadeInput",Private).Invoke(puck,null);
            if(body.linearVelocity.sqrMagnitude<.001f) throw new Exception("A quick Shoot press failed to launch.");
        }
        finally { panelFire.SetValue(null,false); body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero; puck.ResetInputState(); }
        Debug.Log("QUICK_SHOOT_PASSED: first press supplies aim and release launches.");
        bool rebound=false;
        for(int angle=0;angle<360 && !rebound;angle+=20)
        {
            float radians=angle*Mathf.Deg2Rad;
            var drag=new Vector3(Mathf.Cos(radians),0,Mathf.Sin(radians))*puck.maxDragDistance;
            typeof(PuckController).GetMethod("UpdatePreview",Private).Invoke(puck,new object[]{drag});
            rebound=puck.previewCoverageLine.enabled && puck.previewCoverageLine.positionCount>1;
        }
        if(!rebound || puck.previewCoverageLine.widthCurve.Evaluate(1)>.4f || puck.previewCoverageLine.colorGradient.Evaluate(0).a>.3f)
            throw new Exception("Post-bounce guide must be present, narrow and translucent.");
        var endOfLine=puck.previewLine.GetPosition(puck.previewLine.positionCount-1);
        if(Vector3.Distance(endOfLine,puck.previewCoverageLine.GetPosition(0))>.02f) throw new Exception("Estimate does not begin at the first rebound.");
        ToyBoxMenuValidation.RenderCamera(Camera.main,"Gameplay-Aim-Estimate",1600,1000);
        typeof(PuckController).GetMethod("HideTrajectoryPreview",Private).Invoke(puck,null);
        Debug.Log("FEEDBACK_GUIDE_PASSED: preview switches to a bounded translucent estimate after rebound.");
        bool retry=false,end=false;
        var panel=manager.levelFailedPanel;
        panel.ShowTransactionRetry(()=>retry=true,()=>end=true);
        if(panel.retryButtonText.text!="TRY AGAIN" || !(bool)typeof(LevelFailedPanel).GetField("_timeUpSessionPrompt",Private).GetValue(panel))
            throw new Exception("Connection failure must offer retry with no automatic end.");
        typeof(LevelFailedPanel).GetField("_choicesReady",Private).SetValue(panel,true);
        typeof(LevelFailedPanel).GetMethod("Retry",Private).Invoke(panel,null);
        if(!retry || end) throw new Exception("Connection retry ended the session.");
        panel.StopAllCoroutines();
        retry=false;end=false;
        panel.ShowContinueChoice("TIME'S UP!",()=>retry=true,()=>end=true);
        var hanging=panel.hangingTimeUp;
        if(hanging==null || !hanging.IsShowing || hanging.Ready)throw new Exception("Hanging sign failed to start its entrance.");
        hanging.Activate();if(retry || end)throw new Exception("Hanging sign accepted input before settling.");
        for(float sample=0;sample<=1.4f;sample+=.025f)
        {
            hanging.SampleReveal(sample);
            if(hanging.header.localPosition.y-hanging.continueSign.localPosition.y<2.2f ||
                hanging.continueSign.localPosition.y-hanging.endSign.localPosition.y<1.4f)
                throw new Exception("Hanging signs overlap during entrance.");
        }
        hanging.SampleReveal(.62f);
        ToyBoxMenuValidation.RenderCamera(Camera.main,"Hanging-Time-Up-Entrance",1600,900);
        hanging.SampleReveal(1.4f);
        typeof(LevelFailedPanel).GetField("_choicesReady",Private).SetValue(panel,true);
        Physics.SyncTransforms();
        ToyBoxMenuValidation.RenderCamera(Camera.main,"Hanging-Time-Up",1600,1000);
        hanging.Select(0);hanging.Activate();
        if(!retry || end || hanging.IsShowing)throw new Exception("Hanging Continue action failed.");
        panel.StopAllCoroutines();
        panel.ShowContinueChoice("TIME'S UP!",()=>retry=true,()=>end=true);
        hanging.SampleReveal(1.4f);
        typeof(LevelFailedPanel).GetField("_choicesReady",Private).SetValue(panel,true);
        hanging.Select(1);hanging.Activate();
        if(!end || hanging.IsShowing)throw new Exception("Hanging End action failed.");
        panel.StopAllCoroutines();
        Debug.Log("HANGING_TIME_UP_PASSED: entrance input guard, Continue and End callbacks, hide and replay.");
        Debug.Log("FEEDBACK_ATTEMPTS_PASSED: restart preserves time, continue restores same level, fine aim is slower, connection retry does not end run.");
    }
}
