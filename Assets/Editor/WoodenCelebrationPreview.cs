#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class WoodenCelebrationPreview
{
    public static string Capture()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var original=SceneManager.GetActiveScene();if(original.isDirty)throw new Exception("Save the current scene before previewing");
        string restore=original.path;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        RenderTexture rt=null;Texture2D output=null;var previous=RenderTexture.active;
        try
        {
            var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
            var camera=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
            var level=(GameObject)PrefabUtility.InstantiatePrefab(manager.levelPrefabs[0],scene);
            var hole=level.transform.Find("Hole");
            var view=manager.premiumWinCelebration;
            if(view.woodenBanner==null)throw new Exception("Scene is missing saved wooden celebration");
            view.Play(hole.position+Vector3.up*.1f,true,camera);view.Sample(.95f);
            foreach(var ps in view.celebrationEffects)ps.Simulate(.95f,true,true,true);
            rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            output=new Texture2D(1920,1080,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1920,1080),0,0);output.Apply();
            System.IO.Directory.CreateDirectory("output/celebration");System.IO.File.WriteAllBytes("output/celebration/Wooden-Hole-In-One.png",output.EncodeToPNG());
            camera.targetTexture=null;view.Hide();
            return "output/celebration/Wooden-Hole-In-One.png";
        }
        finally
        {
            RenderTexture.active=previous;if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(output!=null)UnityEngine.Object.DestroyImmediate(output);
            EditorSceneManager.OpenScene(restore);
        }
    }
}
#endif
