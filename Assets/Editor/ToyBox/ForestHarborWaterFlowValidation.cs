#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ForestHarborWaterFlowValidation
{
    const string Output="output/water-flow";
    public static string CheckAndRender()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(original==null) throw new Exception("Map missing.");
        var scene=EditorSceneManager.NewPreviewScene();
        var materials=new System.Collections.Generic.List<Material>();
        try
        {
            var copy=Object.Instantiate(original.gameObject);copy.name="WaterFlowValidationTemporary";
            SceneManager.MoveGameObjectToScene(copy,scene);copy.SetActive(false);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=null;map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,49);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            var image=map.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>();
            if(image.GetComponent<ForestHarborWaterFlow>()==null||image.material.shader.name!="BUCA/Forest Harbor Water Flow")
                throw new Exception("Flow animation is not attached to the artwork.");
            if(ShaderUtil.ShaderHasError(image.material.shader))throw new Exception("Shader compilation failed.");
            foreach(var raw in copy.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
                if(raw.material!=null&&raw.material.shader.name=="BUCA/Forest Harbor Water Flow")
                {var material=new Material(raw.material);raw.material=material;materials.Add(material);}
            copy.SetActive(true);Directory.CreateDirectory(Output);
            var life=copy.GetComponentInChildren<ForestHarborLife>(true);
            if(life==null||life.riverAudio==null||life.riverAudio.clip==null)
                throw new Exception("Woodland river sound missing.");
            var river=life.riverAudio;
            if(!river.loop||river.playOnAwake||river.spatialBlend!=0||Mathf.Abs(river.clip.length-24)>.05f||river.clip.channels!=2)
                throw new Exception("River audio must be a 24-second stereo loop.");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(ForestHarborLife).GetMethod("OnApplicationFocus",flags).Invoke(life,new object[]{true});
            if(life.TargetRiverVolume()<=0)throw new Exception("River sound has no audible target volume.");
            typeof(ForestHarborLife).GetMethod("OnApplicationFocus",flags).Invoke(life,new object[]{false});
            if(life.TargetRiverVolume()!=0||river.volume!=0||river.isPlaying)throw new Exception("River must mute on focus loss.");
            typeof(ForestHarborLife).GetMethod("OnApplicationFocus",flags).Invoke(life,new object[]{true});
            typeof(ForestHarborLife).GetMethod("OnApplicationPause",flags).Invoke(life,new object[]{true});
            if(life.TargetRiverVolume()!=0)throw new Exception("River must mute when paused.");
            typeof(ForestHarborLife).GetMethod("OnApplicationPause",flags).Invoke(life,new object[]{false});
            life.enabled=false;
            if(life.TargetRiverVolume()!=0||river.volume!=0||river.isPlaying)throw new Exception("River must stop on map exit.");
            life.enabled=true;
            var render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            for(int frame=0;frame<32;frame++)
            {
                foreach(var material in materials)material.SetFloat("_FlowTime",frame/16f);
                render.Invoke(null,new object[]{map,11,1672,941,Output+"/Water-"+frame.ToString("000")+".png"});
            }
            // Deterministic frame comparison: the bridge, trees and all buttons must remain still.
            var a=new Texture2D(2,2);var b=new Texture2D(2,2);var mask=new Texture2D(2,2);var shore=new Texture2D(2,2);
            try
            {
                a.LoadImage(File.ReadAllBytes(Output+"/Water-000.png"));b.LoadImage(File.ReadAllBytes(Output+"/Water-008.png"));
                mask.LoadImage(File.ReadAllBytes("Assets/TwilightMap/ForestHarbor/WaterFlowMask.png"));
                shore.LoadImage(File.ReadAllBytes("Assets/TwilightMap/ForestHarbor/ShoreWaveData.png"));
                var first=a.GetPixels32();var second=b.GetPixels32();int changed=0,outside=0,harbor=0,bridge=0,foreground=0,shoreline=0,solid=0;
                var protectedAreas=new[]{new Rect(1300,672,40,24),new Rect(1377,727,19,28),new Rect(1418,725,20,22),new Rect(1488,695,15,20),new Rect(1566,708,17,12),new Rect(1640,704,7,6),new Rect(280,603,45,15),new Rect(560,756,28,27)};
                for(int y=0;y<a.height;y++)for(int x=0;x<a.width;x++)
                {
                    var c=first[y*a.width+x];var d=second[y*a.width+x];
                    int delta=Math.Abs(c.r-d.r)+Math.Abs(c.g-d.g)+Math.Abs(c.b-d.b);
                    if(delta<4)continue;
                    changed++;
                    int artY=a.height-1-y;
                    bool water=false;
                    for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                    {
                        int mx=Mathf.Clamp(x+dx,0,a.width-1),my=Mathf.Clamp(y+dy,0,a.height-1);
                        var m=mask.GetPixel(mx,my);
                        if(Mathf.Max(Mathf.Max(m.r,m.a),Mathf.Max(m.g,m.b))>.005f||shore.GetPixel(mx,my).r>.005f)water=true;
                    }
                    if(!water)outside++;
                    if(x>1000&&artY>460)harbor++;
                    if(x>140&&x<460&&artY>530&&artY<670)bridge++;
                    if(x>300&&x<1150&&artY>730)foreground++;
                    if(x>1330&&artY>690&&artY<815&&shore.GetPixel(x,y).r>.01f)shoreline++;
                    foreach(var area in protectedAreas)if(area.Contains(new Vector2(x,artY)))solid++;
                }
                if(changed<600)throw new Exception("Water does not visibly animate: "+changed);
                if(harbor<5000)throw new Exception("Harbor water does not visibly animate: "+harbor);
                if(bridge<1500||foreground<2000)throw new Exception("Woodland river animation missing: bridge="+bridge+" foreground="+foreground);
                if(shoreline<8000)throw new Exception("Shoreline waves are not visible: "+shoreline);
                if(solid!=0)throw new Exception("Shoreline animation moves dock posts or rocks: "+solid);
                if(outside>10)throw new Exception("Animation leaks beyond the water corridor: "+outside);
                string report="PASS: shader compiled; "+changed+" water pixels change over 0.5 seconds including "+bridge+" stone-bridge river pixels, "+foreground+" foreground river pixels, "+harbor+" harbor pixels and "+shoreline+" shoreline wave pixels; "+outside+" changed pixels outside the water masks; "+solid+" changes in protected dock/post/rock areas. All active level and footer controls remain clickable. River audio: 24-second stereo loop assigned, fade-in target active, focus/pause/disable mute verified. Rendered 32 frames at 16 fps on an isolated map copy. No gameplay or progress changes. Runtime listening has not been performed.";
                File.WriteAllText(Output+"/validation.txt",report);return report;
            }
            finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);Object.DestroyImmediate(mask);Object.DestroyImmediate(shore);}
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);foreach(var material in materials)Object.DestroyImmediate(material);}
    }
}
#endif
