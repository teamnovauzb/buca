using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shows the real railway between boards without destroying the current score/session.</summary>
public static class LevelMapJourney
{
    public static IEnumerator Play(int completed,int next,Action<int> selected)
    {
        var prefab=Resources.Load<TwilightRailwayMap>("CompletionRailwayMap");
        if(prefab==null){Debug.LogError("Completion railway map is missing.");yield break;}
        var hidden=new List<Canvas>();
        var cameras=new List<Camera>();
        var gameplayScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if(canvas.enabled&&canvas.gameObject.scene==gameplayScene){hidden.Add(canvas);canvas.enabled=false;}
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if(camera.enabled&&camera.targetTexture==null&&camera.gameObject.scene==gameplayScene){cameras.Add(camera);camera.enabled=false;}
        TwilightRailwayMap map=null;
        try
        {
            map=UnityEngine.Object.Instantiate(prefab);
            map.gameObject.SetActive(true);
            int choice=-1;
            map.OpenAfterCompletion(completed,next,index=>choice=index);
            map.viewCamera.enabled=true;
            map.viewCamera.rect=new Rect(0,0,1,1);
            map.viewCamera.aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            map.illustratedMenu.canvas.enabled=true;
            map.illustratedMenu.canvas.worldCamera=map.viewCamera;
            map.illustratedMenu.canvas.renderMode=RenderMode.ScreenSpaceCamera;
            while(choice<0)yield return null;
            selected(choice);
        }
        finally
        {
            if(map!=null){map.gameObject.SetActive(false);UnityEngine.Object.Destroy(map.gameObject);}
            foreach(var canvas in hidden)if(canvas!=null)canvas.enabled=true;
            foreach(var camera in cameras)if(camera!=null)camera.enabled=true;
        }
    }
}
