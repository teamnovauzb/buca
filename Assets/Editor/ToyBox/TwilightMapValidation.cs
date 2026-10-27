#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
public static class TwilightMapValidation
{
    public static string Check()
    {
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.stops.Length!=50)throw new Exception("Expected 50 stops.");
        float prior=-1,clearance=float.MaxValue;
        for(int i=0;i<50;i++)
        {
            var s=map.stops[i];if(s.hit==null||s.face==null||s.platform.name!="LevelStop_"+(i+1).ToString("00"))throw new Exception("Invalid stop "+i);
            if(s.distance<=prior)throw new Exception("Nonsequential route "+i);prior=s.distance;
            foreach(var p in map.railway){var d=new Vector2(p.x-s.platform.localPosition.x,p.z-s.platform.localPosition.z).magnitude;clearance=Mathf.Min(clearance,d);}
        }
        if(clearance<1.35f)throw new Exception("Platform too close to track: "+clearance);
        if(map.wheels.Length!=6||map.whistle==null||map.rolling==null)throw new Exception("Train/audio references missing.");
        var result="PASS: 50 sequential stops; minimum platform-centre / rail-centre clearance "+clearance.ToString("F2")+"; six wheel pivots; train and audio references assigned.";
        Directory.CreateDirectory("output/twilight-map");File.WriteAllText("output/twilight-map/validation.txt",result);return result;
    }
    public static string CheckTravel()
    {
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var go=Object.Instantiate(original.gameObject);go.name="TwilightValidationTemporary";
        try
        {
            var map=go.GetComponent<TwilightRailwayMap>();map.Open(null);map.eventAudio=null;map.motionAudio=null;
            var field=typeof(TwilightRailwayMap).GetField("highest",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);field.SetValue(map,49);
            int start=map.Parked;var seen=new System.Collections.Generic.HashSet<int>{start};var wheel=map.wheels[0].localRotation;bool spun=false;
            map.Select(49);
            for(int i=0;i<24000&&map.Parked!=49;i++){map.TickTrain(1f/60);seen.Add(map.Parked);spun|=Quaternion.Angle(wheel,map.wheels[0].localRotation)>.5f;}
            if(map.Parked!=49||map.Moving||seen.Count!=50-start||!spun)throw new Exception("Forward stop traversal failed.");
            map.Select(0);seen.Clear();seen.Add(49);
            for(int i=0;i<24000&&map.Parked!=0;i++){map.TickTrain(1f/60);seen.Add(map.Parked);}
            if(map.Parked!=0||map.Moving||seen.Count!=50)throw new Exception("Reverse stop traversal failed.");
            field.SetValue(map,0);map.Select(49);for(int i=0;i<120;i++)map.TickTrain(1f/60);
            if(map.Parked!=0||map.Moving)throw new Exception("Locked stop should not move the train.");
            string result="PASS: forward travel to level 50; all 50 reverse stops; wheel rotation; no travel to locked levels. Player progress unchanged.";
            File.WriteAllText("output/twilight-map/travel-validation.txt",result);return result;
        }
        finally{Object.DestroyImmediate(go);}
    }
    public static string Render(int width=1920,int height=1080)
    {
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);bool active=map.gameObject.activeSelf;
        var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include).Where(l=>!l.transform.IsChildOf(map.transform)&&l.enabled).ToArray();foreach(var l in lights)l.enabled=false;
        var ambientMode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.42f,.45f,.5f);RenderSettings.ambientEquatorColor=new Color(.28f,.29f,.32f);RenderSettings.ambientGroundColor=new Color(.16f,.17f,.2f);
        var camera=map.viewCamera;var prev=camera.targetTexture;var rect=camera.rect;var old=RenderTexture.active;
        var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            map.gameObject.SetActive(true);map.Open(null);foreach(var label in map.GetComponentsInChildren<TMPro.TMP_Text>())label.ForceMeshUpdate(true);map.Fit(width/(float)height);camera.rect=new Rect(0,0,1,1);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
            string path="output/twilight-map/Twilight-"+width+"x"+height+".png";File.WriteAllBytes(path,tex.EncodeToPNG());return Path.GetFullPath(path);
        }
        finally
        {
            camera.targetTexture=prev;camera.rect=rect;RenderTexture.active=old;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);map.gameObject.SetActive(active);foreach(var l in lights)if(l!=null)l.enabled=true;
            RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
        }
    }
}
#endif
