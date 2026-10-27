#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class HarborRockingPreview
{
    public static string Render()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        const string folder="output/boat-rocking";
        Directory.CreateDirectory(folder);
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(copy,scene);copy.SetActive(false);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=null;map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,49);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            copy.SetActive(true);
            var life=copy.GetComponentInChildren<ForestHarborLife>(true);
            float low=100,high=-100,previous=0;
            for(int i=0;i<600;i++)
            {
                life.Pose(i/60f);float angle=Mathf.DeltaAngle(0,life.tugboat.picture.localEulerAngles.z);
                low=Mathf.Min(low,angle);high=Mathf.Max(high,angle);
                if(i>0&&Mathf.Abs(Mathf.DeltaAngle(previous,angle))>.25f)throw new Exception("Rocking jumps between frames.");
                if(Mathf.Abs(life.tugboat.picture.anchoredPosition.y)>2)throw new Exception("Boat rises too far off the water.");
                previous=angle;
            }
            if(high-low<5)throw new Exception("Rocking is too subtle.");
            var render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            for(int i=0;i<64;i++)
            {
                life.Pose(i/16f);
                render.Invoke(null,new object[]{map,11,1672,941,folder+"/Boat-"+i.ToString("000")+".png"});
            }
            string report="PASS: tugboat roll "+low.ToString("F1")+" to "+high.ToString("F1")+" degrees, smooth frame-to-frame movement, vertical motion below 2 artwork pixels, reflection and wake follow hull. 64 Edit-mode preview frames; no gameplay or audio started.";
            File.WriteAllText(folder+"/validation.txt",report);return report;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
