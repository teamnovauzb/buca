#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AllAboardValidation
{
    public static string BuildLightingPreview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new Exception("Clean edit mode required.");
        string restore=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        const string scenePath="Assets/ToyBoxMenu/AllAboard/CityLightingPreview.unity";
        const string destination="output/all-aboard/web-lighting-preview";
        var template=PlayerSettings.WebGL.template;var compression=PlayerSettings.WebGL.compressionFormat;
        var optimization=UnityEditor.WebGL.UserBuildSettings.codeOptimization;
        try
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/AllAboard/AllAboardCompletion.prefab"));
            var view=root.GetComponent<AllAboardCompletion3D>();view.Show(12450,null,null);view.Sample(8);view.enabled=false;
            view.stationCamera.gameObject.AddComponent<AudioListener>();EditorSceneManager.SaveScene(scene,scenePath);
            PlayerSettings.WebGL.template="APPLICATION:Default";PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            UnityEditor.WebGL.UserBuildSettings.codeOptimization=UnityEditor.WebGL.WasmCodeOptimization.BuildTimes;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{scenePath},target=BuildTarget.WebGL,locationPathName=destination,options=BuildOptions.Development});
            string result=report.summary.result+"; errors="+report.summary.totalErrors+"; bytes="+report.summary.totalSize+"; duration="+report.summary.totalTime;
            System.IO.File.WriteAllText("output/all-aboard/web-lighting-build.txt",result);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception(result);
            return result;
        }
        finally
        {
            PlayerSettings.WebGL.template=template;PlayerSettings.WebGL.compressionFormat=compression;
            UnityEditor.WebGL.UserBuildSettings.codeOptimization=optimization;
            EditorSceneManager.OpenScene(restore);AssetDatabase.DeleteAsset(scenePath);
        }
    }
    public static string Validate()
    {
        var messages=new List<string>();
        for(int n=31;n<=50;n++)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ToyBoxMenu/BonusBoards/Level_{n:00}.prefab");
            if(p==null)throw new Exception("Missing board "+n);
            var settings=AssetDatabase.LoadAssetAtPath<LevelSettings>($"Assets/ToyBoxMenu/BonusBoards/Level_{n:00}_Settings.asset");
            if(settings.timeLimit!=30+(n-2)*3||settings.maxLives!=(n-1)*5)throw new Exception("Incorrect allowance "+n);
            var walls=p.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("BonusBank")).ToArray();
            // Flood fill with an inflated wall footprint: puck-radius clearance, not point clearance.
            const int w=83,h=129;const float step=.1f;
            var seen=new bool[w,h];var q=new Queue<Vector2Int>();q.Enqueue(new Vector2Int(41,10));seen[41,10]=true;bool goal=false;
            while(q.Count>0)
            {
                var v=q.Dequeue();float x=(v.x-41)*step,z=-6.4f+v.y*step;
                if(Vector2.Distance(new Vector2(x,z),new Vector2(0,5.4f))<.3f){goal=true;break;}
                foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                {
                    var k=v+d;if(k.x<0||k.x>=w||k.y<0||k.y>=h||seen[k.x,k.y])continue;
                    Vector3 point=new Vector3((k.x-41)*step,.28f,-6.4f+k.y*step);bool blocked=false;
                    foreach(var wall in walls)
                    {
                        var local=Quaternion.Inverse(wall.transform.localRotation)*(point-wall.transform.localPosition);
                        if(Mathf.Abs(local.x)<wall.size.x*.5f+.30f&&Mathf.Abs(local.z)<wall.size.z*.5f+.30f){blocked=true;break;}
                    }
                    seen[k.x,k.y]=true;if(!blocked)q.Enqueue(k);
                }
            }
            if(!goal)throw new Exception("No puck-width route through bonus board "+n);
            messages.Add(n+": route and allowances PASS");
        }
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/AllAboard/AllAboardCompletion.prefab");
        var root=UnityEngine.Object.Instantiate(asset);
        try
        {
            var view=root.GetComponent<AllAboardCompletion3D>();
            if(view.fanfare==null||view.tick==null||view.chime==null||view.fireworks==null)throw new Exception("Missing sound");
            int calls=0;view.Show(12450,()=>calls++,()=>calls++);view.Choose(false);
            if(calls!=0)throw new Exception("Early input accepted");
            view.Sample(1.4f);if(view.DisplayedScore!=0)throw new Exception("Count did not start at zero");
            view.Sample(3);if(view.DisplayedScore<=0||view.DisplayedScore>=12450)throw new Exception("No intermediate count");
            view.Sample(5.3f);if(view.DisplayedScore!=12450)throw new Exception("Wrong final score");
            if(view.sparks.Count(t=>t.gameObject.activeSelf)==0)throw new Exception("Fireworks absent");
            view.Show(50000,null,null,true);if(!view.bonusTitle.activeSelf||view.firstTitle.activeSelf||!view.replayLabel.activeSelf)throw new Exception("Wrong bonus finale title");
            foreach(float aspect in new[]{16f/9f,4f/3f,21f/9f})
            {
                view.stationCamera.aspect=aspect;view.Sample(8);
                if(view.stationCamera.orthographic)throw new Exception("Station must use a perspective camera");
                foreach(var hit in new[]{view.nextHit,view.menuHit})
                {
                    var box=(BoxCollider)hit;
                    foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
                    {
                        Vector3 corner=box.transform.TransformPoint(box.center+Vector3.Scale(box.size*.5f,new Vector3(x,y,-1)));
                        var screen=view.stationCamera.WorldToViewportPoint(corner);
                        if(screen.z<=0||screen.x<.02f||screen.x>.98f||screen.y<.02f||screen.y>.98f)throw new Exception("Control cropped at aspect "+aspect);
                    }
                    var ray=view.stationCamera.ViewportPointToRay(view.stationCamera.WorldToViewportPoint(hit.bounds.center));
                    if(!hit.Raycast(ray,out _,100))throw new Exception("Button raycast misses visible control");
                }
                foreach(var digit in view.digits)
                {var point=view.stationCamera.WorldToViewportPoint(digit.transform.position);if(point.x<0||point.x>1||point.y<0||point.y>1)throw new Exception("Counter cropped");}
            }
            view.Hide();if(view.IsShowing)throw new Exception("Hide failed");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        return string.Join("\n",messages)+"\nPASS score animation, early-input gate, saved audio, fireworks, finale title, cleanup, perspective framing and clickable controls at 16:9, 4:3 and 21:9.";
    }
    public static string Render(string path,int width=2560,int height=1440,float seconds=5.7f,bool bonus=false)
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/AllAboard/AllAboardCompletion.prefab"));
        var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};var tex=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try
        {
            var view=root.GetComponent<AllAboardCompletion3D>();view.Show(12450,null,null,bonus);view.stationCamera.targetTexture=rt;view.Sample(seconds);
            Physics.SyncTransforms();UnityEngine.Rendering.VolumeManager.instance.Update(view.stationCamera.transform,1<<30);
            view.stationCamera.Render();RenderTexture.active=rt;
            tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());return path;
        }
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
#endif
