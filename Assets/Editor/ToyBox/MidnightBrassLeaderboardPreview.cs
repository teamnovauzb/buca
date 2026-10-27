#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class BuildMidnightBrassLeaderboard
{
    public static string Preview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean edit mode required.");
        string restore=SceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var view=Object.FindAnyObjectByType<BucaArcadeResult3D>(FindObjectsInactive.Include);
            var theme=view.worldTheme;
            var table=theme.standings;
            var panel=view.panel;
            view.stage.SetActive(true);view.cabinet.gameObject.SetActive(true);
            view.heartsHeading.SetActive(true);view.timeHeading.SetActive(false);view.SetScore(2050);
            var data=new[]{"MAYA","LEO","YOU","ALEX","LILY","NOAH","EMMA","ALI","SOFIA","MAX"}
                .Select((name,i)=>new LeaderboardPanel.LeaderboardData{rank=i+1,playerName=name,score=2450-i*200}).ToArray();
            var populate=typeof(LeaderboardPanel).GetMethod("Populate",BindingFlags.Instance|BindingFlags.NonPublic);
            populate.Invoke(panel,new object[]{data,3,2050,"YOU"});
            var index=typeof(LevelManager).GetField("_currentIndex",BindingFlags.Instance|BindingFlags.NonPublic);
            index.SetValue(theme.levelManager,29);theme.ApplyForCurrentWorld();table.Sync(panel);
            Require(!theme.IsCity && theme.roomObjects.All(o=>o.activeSelf),"World 1 wooden frame");
            Render(view,"Midnight-World1-Preserved",1920,1080);
            index.SetValue(theme.levelManager,30);theme.ApplyForCurrentWorld();table.Sync(panel);
            Require(theme.IsCity && theme.roomObjects.All(o=>!o.activeSelf),"World 2 theme selection at level 31");
            Require(table.rows.All(o=>o.activeSelf),"All ten rows visible");
            Require(table.playerOutlines[2].activeSelf && table.playerOutlines.Where((o,i)=>i!=2).All(o=>!o.activeSelf),"Only current-player row highlighted");
            Require(table.rankBadges.All(r=>r.sharedMaterial==theme.cityBadge),"Brass rank badges");
            Render(view,"Midnight-Brass",1920,1080);
            Render(view,"Midnight-Brass-Portrait",1080,1920);
            RenderContext(view);
            view.heartsHeading.SetActive(false);view.timeHeading.SetActive(true);view.SetScore(0);
            Render(view,"Midnight-Time-Up",1920,1080);
            populate.Invoke(panel,new object[]{Array.Empty<LeaderboardPanel.LeaderboardData>(),0,int.MaxValue,"A VERY LONG PLAYER NAME"});
            table.Sync(panel);view.SetScore(int.MaxValue);
            foreach(var text in table.GetComponentsInChildren<TMP_Text>(true))text.ForceMeshUpdate(true);
            Require(!table.scoreLabels[0].isTextTruncated,"Full score displayed");
            Require(table.empty.gameObject.activeSelf,"Unranked fallback shown");
            Require(view.digits.Count(d=>d.transform.parent.gameObject.activeSelf)==10,"Int32 score counter fits");
            theme.Apply(false);
            Require(theme.surfaces.All(s=>s.renderer.sharedMaterial==s.room),"Original World 1 materials restored");
            Require(theme.cityObjects.All(o=>!o.activeSelf),"City ornaments hidden in World 1");
            File.WriteAllText("output/leaderboard/midnight-validation.txt","PASS: level 30 uses wood; level 31 uses Midnight Brass; ten live rows; current-player highlight; brass badges; full Int32 score; unranked fallback; World 1 material restoration. Landscape, portrait and Time's Up previews rendered from scene meshes. Preview names and scores are sample data.\n");
            return "Midnight Brass previews and validation saved.";
        }
        finally {EditorSceneManager.OpenScene(restore);}
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void RenderContext(BucaArcadeResult3D view)
    {
        var manager=view.worldTheme.levelManager;
        var board=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[30]);
        var evening=Object.FindAnyObjectByType<EveningGameplayTheme>(FindObjectsInactive.Include);
        evening.Apply(true,board);
        view.worldTheme.CaptureBackdrop();
        var source=evening.gameplayCamera;
        var cameraObject=new GameObject("MidnightPreviewCamera",typeof(Camera));
        var camera=cameraObject.GetComponent<Camera>();camera.CopyFrom(source);
        camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var canvasObject=new GameObject("MidnightPreviewCanvas",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
        canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=200;
        EditorUtility.CopySerialized(view.GetComponentInParent<Canvas>().GetComponent<UnityEngine.UI.CanvasScaler>(),canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>());
        var clone=Object.Instantiate(view.gameObject,canvasObject.transform);
        var panel=clone.GetComponent<LeaderboardPanel>();clone.SetActive(true);panel.enabled=false;
        clone.GetComponent<BucaArcadeResult3D>().enabled=false;
        panel.group.alpha=1;panel.card.localScale=Vector3.one;
        var target=new RenderTexture(1920,1080,24){antiAliasing=4};
        var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.aspect=1920f/1080;
            view.stage.GetComponentInChildren<Camera>(true).Render();
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
            File.WriteAllBytes("output/leaderboard/Midnight-Brass-InGame.png",pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;
            Object.DestroyImmediate(canvasObject);Object.DestroyImmediate(cameraObject);
            target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);Object.DestroyImmediate(board);
            view.stage.SetActive(true);
        }
    }
    static void Render(BucaArcadeResult3D view,string name,int width,int height)
    {
        var camera=view.stage.GetComponentInChildren<Camera>(true);
        var target=camera.targetTexture;var previous=RenderTexture.active;
        float aspect=camera.aspect,size=camera.orthographicSize;
        Color background=camera.backgroundColor;
        var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=4};
        var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.aspect=(float)width/height;
            camera.orthographicSize=Mathf.Max(5.3f,9.2f/camera.aspect);
            camera.backgroundColor=new Color(.018f,.027f,.046f,1);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            foreach(var label in view.worldTheme.standings.GetComponentsInChildren<TMP_Text>(true))label.ForceMeshUpdate(true);
            camera.Render();RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            File.WriteAllBytes("output/leaderboard/"+name+".png",pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=target;camera.aspect=aspect;camera.orthographicSize=size;camera.backgroundColor=background;
            RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        }
    }
}
#endif
