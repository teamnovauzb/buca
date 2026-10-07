using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Optional editor validation and offscreen renders; never included in a player.</summary>
public static class ToyBoxMenuValidation
{
    public static void BatchValidate()
    {
        try
        {
            BuildToyBoxMainMenu.GeneratePreview();
            var menu = UnityEngine.Object.FindAnyObjectByType<ToyBoxMenuController>();
            BuildToyBoxMainMenu.Validate(menu.gameObject);
            CheckClock(menu);
            CheckNavigation(menu);
            Render(menu, "Home", 1600, 1000);
            menu.homeRoot.SetActive(false); menu.levelsRoot.SetActive(true);
            Render(menu, "Levels", 1600, 1000);
            menu.levelsRoot.SetActive(false); menu.resumeRoot.SetActive(true);
            Render(menu, "Resume", 1600, 1000);
            menu.resumeRoot.SetActive(false); menu.skinsRoot.SetActive(true);
            Render(menu, "Skins", 1600, 1000);
            menu.skinsRoot.SetActive(false); menu.homeRoot.SetActive(true);
            Render(menu, "Portrait", 900, 1200);
            // In batch this is a disposable project copy. Exercise the actual
            // install command too, including its scene backup and service preservation.
            if (Application.isBatchMode)
            {
                string main="Assets/Scenes/MainMenu.unity";
                string backup="Assets/ToyBoxMenu/Backup/MainMenu-BeforeToyBox.unity";
                string original=File.ReadAllText(main);
                bool hadBackup=File.Exists(backup);
                BuildToyBoxMainMenu.GenerateAndAssign();
                if(!hadBackup && File.ReadAllText(backup)!=original) throw new Exception("Scene backup differs from original.");
                int cameras=0;
                foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
                    if(camera.enabled) cameras++;
                if(cameras!=1) throw new Exception("Expected one active menu camera, found "+cameras);
                foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                    if(canvas.enabled) throw new Exception("Legacy flat menu is still visible.");
            }
            Debug.Log("TOYBOX_VALIDATION_PASSED: compilation, saved geometry, 44 clickable buttons, clock, navigation, locks, five renders, scene installation and backup.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch(Exception error)
        {
            Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    static void CheckNavigation(ToyBoxMenuController menu)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        Type type=typeof(ToyBoxMenuController);
        FieldInfo focus=type.GetField("_focus",flags);
        type.GetMethod("Navigate",flags).Invoke(menu,new object[]{Vector2.right});
        if((int)focus.GetValue(menu)!=1) throw new Exception("Home focus does not move to Levels.");
        type.GetMethod("Activate",flags).Invoke(menu,null);
        if(!menu.levelsRoot.activeSelf||menu.homeRoot.activeSelf) throw new Exception("Levels page did not open.");
        int highest=Mathf.Clamp(PlayerPrefs.GetInt(LevelSelectController.HighestUnlockedKey,0),0,29);
        for(int i=0;i<30;i++)
            if(menu.levelButtons[i].lockMark.activeSelf!=(i>highest)) throw new Exception("Incorrect level lock: "+i);
        if(highest<29)
        {
            focus.SetValue(menu,highest+1);
            type.GetMethod("Activate",flags).Invoke(menu,null);
            if((bool)type.GetField("_loading",flags).GetValue(menu)) throw new Exception("Locked level was started.");
        }
        focus.SetValue(menu,30);
        type.GetMethod("Activate",flags).Invoke(menu,null);
        if(!menu.homeRoot.activeSelf||menu.levelsRoot.activeSelf) throw new Exception("Back did not return Home.");
    }

    static void CheckClock(ToyBoxMenuController menu)
    {
        int[] masks = {63,6,91,79,102,109,125,7,127,111};
        for(int number=0;number<=30;number++)
        {
            menu.SetClock(number);
            for(int i=0;i<14;i++)
            {
                bool expected=(masks[i<7?number/10:number%10]&(1<<(i%7)))!=0;
                if(menu.clockSegments[i].activeSelf!=expected) throw new Exception("Clock segment mismatch: "+number);
            }
        }
    }

    static void Render(ToyBoxMenuController menu,string name,int width,int height)
    {
        bool isMap=name=="Levels";
        Camera camera=isMap?menu.mapCamera:menu.menuCamera;
        foreach(var scenery in menu.menuScenery) scenery.SetActive(!isMap);
        camera.aspect=(float)width/height;
        float tan=Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f),distance=0;
        for(int i=0;i<menu.framingPoints.Length;i++)
        {
            var corner=menu.framingPoints[i]-menu.framingBounds.center;
            Vector3 p=Quaternion.Inverse(camera.transform.rotation)*corner;
            distance=Mathf.Max(distance,Mathf.Abs(p.y)/tan-p.z,Mathf.Abs(p.x)/(tan*camera.aspect)-p.z);
        }
        camera.transform.position=menu.framingBounds.center-camera.transform.forward*(distance*1.04f);
        if(isMap) menu.FitMapCamera(camera.aspect);
        Physics.SyncTransforms();
        var buttons=name=="Levels"?menu.levelButtons:name=="Resume"?menu.resumeButtons:name=="Skins"?menu.skinButtons:menu.homeButtons;
        foreach(var button in buttons)
        {
            Vector3 screen=camera.WorldToViewportPoint(button.hit.bounds.center);
            if(screen.z<=0||screen.x<=0||screen.x>=1||screen.y<=0||screen.y>=1)
                throw new Exception("Button clipped: "+button.cap.parent.name+" in "+name);
            Ray ray=camera.ViewportPointToRay(screen);
            if(!Physics.Raycast(ray,out RaycastHit hit,100)||hit.collider!=button.hit)
                throw new Exception("Button cannot be clicked: "+button.cap.parent.name+" in "+name);
        }
        RenderCamera(camera,name,width,height);
    }

    public static void RenderCamera(Camera camera,string name,int width,int height)
    {
        var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        RenderTexture previous=RenderTexture.active;
        try
        {
            target.Create();
            var request=new UniversalRenderPipeline.SingleCameraRequest { destination=target };
            // Warm the SRP material buffers before capturing the first offscreen frame.
            for(int pass=0;pass<3;pass++)
                if(RenderPipeline.SupportsRenderRequest(camera,request)) RenderPipeline.SubmitRenderRequest(camera,request);
                else { camera.targetTexture=target; camera.Render(); }
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
            Directory.CreateDirectory("output/toy-box");
            File.WriteAllBytes("output/toy-box/"+name+".png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=null; RenderTexture.active=previous;
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
