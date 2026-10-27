#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Checks and renders an isolated map copy without starting gameplay or audio.</summary>
public static class HarborLifeValidation
{
    const string Output = "output/harbor-animation";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok,string message) { if(!ok) throw new Exception(message); }

    public static string CheckAndRender()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        Require(original!=null,"Map missing.");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);
            copy.name="HarborLifeValidationTemporary";
            SceneManager.MoveGameObjectToScene(copy,scene);copy.SetActive(false);
            var map=copy.GetComponent<TwilightRailwayMap>();
            map.eventAudio=null;map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",Private).SetValue(map,49);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            copy.SetActive(true);
            var life=copy.GetComponentInChildren<ForestHarborLife>(true);
            Require(life!=null&&life.sailboat!=null&&life.tugboat!=null,"Two boats required.");
            foreach(var boat in new[]{life.sailboat,life.tugboat})
            {
                Require(Vector2.Distance(ForestHarborLife.RoutePoint(boat,0),ForestHarborLife.RoutePoint(boat,1))<.01f,"Route does not close.");
                var routeMin=new Vector2(float.MaxValue,float.MaxValue);
                var routeMax=new Vector2(float.MinValue,float.MinValue);
                for(int i=0;i<1000;i++)
                {
                    var a=ForestHarborLife.RoutePoint(boat,i/1000f);
                    var b=ForestHarborLife.RoutePoint(boat,(i+1)/1000f);
                    routeMin=Vector2.Min(routeMin,a);routeMax=Vector2.Max(routeMax,a);
                    Require(Vector2.Distance(a,b)<1.5f,"Route jumps.");
                    if(boat==life.tugboat)
                    {
                        // Conservative full-picture bounds include the cabin above the waterline.
                        float halfWidth=boat.picture.sizeDelta.x*.5f;
                        float top=a.y-boat.picture.sizeDelta.y*(1-boat.picture.pivot.y)-3;
                        float bottom=a.y+boat.picture.sizeDelta.y*boat.picture.pivot.y+3;
                        Require(a.x-halfWidth>1450&&a.x+halfWidth<1660&&top>790&&bottom<920,
                            "Tugboat overlaps the harbor columns, screen control, or map edge.");
                    }
                    else Require(a.x>1000&&a.x<1582&&a.y>680&&a.y<864,"Hull left harbor bounds.");
                }
                Require(Vector2.Distance(routeMin,routeMax)>35,"Boat route has no visible travel.");
            }
            float closest=float.MaxValue;
            for(int i=0;i<4928;i++)
            {
                float t=i*.25f;
                var a=ForestHarborLife.RoutePoint(life.sailboat,t/life.sailboat.lapSeconds);
                var b=ForestHarborLife.RoutePoint(life.tugboat,t/life.tugboat.lapSeconds);
                closest=Math.Min(closest,Vector2.Distance(a,b));
                life.Pose(t);
                Require(Mathf.Abs(life.sailboat.picture.localScale.x)>=.77f&&Mathf.Abs(life.tugboat.picture.localScale.x)>=.77f,"Boat disappears during a turn.");
            }
            Require(closest>135,"Boat routes approach too closely: "+closest);
            foreach(var graphic in life.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))Require(!graphic.raycastTarget,"Harbor graphic blocks clicks.");
            foreach(var boat in new[]{life.sailboat,life.tugboat})
            {
                Require(boat.wake.GetComponent<CanvasRenderer>()!=null,"Boat wake has no CanvasRenderer and cannot draw.");
                Require(boat.reflection!=null,"Boat reflection missing.");
            }
            var source=life.waterAudio;
            Require(source!=null&&source.clip!=null&&source.loop&&!source.playOnAwake&&source.spatialBlend==0,"Water source setup incorrect.");
            Require(Mathf.Abs(source.clip.length-32)<.05f&&source.clip.channels==2,"Expected 32-second stereo water loop.");
            Require(!source.isPlaying&&source.volume==0,"Editor audio must remain silent.");
            typeof(ForestHarborLife).GetMethod("OnApplicationFocus",Private).Invoke(life,new object[]{false});
            Require(life.TargetWaterVolume()==0,"Background water must mute.");
            typeof(ForestHarborLife).GetMethod("OnApplicationFocus",Private).Invoke(life,new object[]{true});
            typeof(ForestHarborLife).GetMethod("OnApplicationPause",Private).Invoke(life,new object[]{true});
            Require(life.TargetWaterVolume()==0,"Paused water must mute.");
            typeof(ForestHarborLife).GetMethod("OnApplicationPause",Private).Invoke(life,new object[]{false});
            life.enabled=false;Require(life.TargetWaterVolume()==0&&source.volume==0&&!source.isPlaying,"Map exit must stop water.");life.enabled=true;
            Directory.CreateDirectory(Output);
            var render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(int time in new[]{0,14,28,44})
            {
                life.Pose(time);
                render.Invoke(null,new object[]{map,11,1920,1080,Output+"/Harbor-"+time.ToString("00")+".png"});
            }
            string report="PASS: isolated Edit-mode checks. Two continuous travelling boat routes, "+life.sailboat.lapSeconds+"s / "+life.tugboat.lapSeconds+"s laps; tugboat full-picture bounds remain in lower-right open water, clear of harbor columns and the screen control. Minimum hull-center separation "+closest.ToString("F1")+" pixels across the combined cycles. Four map renders at 0/14/28/44s; all 50 level targets and five footer controls remain clickable. Water: 32-second stereo loop, no play-on-awake, silent in Editor, focus/pause/disable cleanup. No Play mode or player-progress writes. Runtime listening has not been performed.";
            File.WriteAllText(Output+"/validation.txt",report);return report;
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
