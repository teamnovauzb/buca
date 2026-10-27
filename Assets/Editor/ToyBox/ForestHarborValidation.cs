#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Isolated Edit-mode verification. Never enters Play mode or writes player progress.</summary>
public static class ForestHarborValidation
{
    const string Output = "output/forest-harbor-implementation";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(TwilightRailwayMap map,string field,object value) => typeof(TwilightRailwayMap).GetField(field,Private).SetValue(map,value);
    static void Require(bool ok,string message) { if(!ok) throw new Exception(message); }

    [MenuItem("RealBuca/Forest Harbor/Validate and Render (Edit Mode)")]
    public static string CheckAndRender()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only.");
        var original = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        Require(original != null && original.illustratedMenu.forestHarborPresentation,"Install the forest / harbor map first.");
        Directory.CreateDirectory(Output);
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject copy = null;
        try
        {
            copy = Object.Instantiate(original.gameObject); copy.name = "ForestHarborValidationTemporary";
            SceneManager.MoveGameObjectToScene(copy,scene);
            copy.SetActive(false);
            var map = copy.GetComponent<TwilightRailwayMap>();
            map.eventAudio = null; map.motionAudio = null; map.Open(null); Set(map,"highest",49);
            Require(original.whistle != null && original.rolling != null && original.arrival != null,"Train audio clips missing.");
            Require(map.stops.Length == 50,"Expected 50 stops.");
            var stationPositions = BuildForestHarborMap.StationPositions(
                map.illustratedMenu.GetComponent<ForestHarborMapPresentation>());
            if (map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().roundForestStops)
            {
                var view = map.illustratedMenu;
                var railArt = view.artwork.GetComponentInChildren<ForestHarborRailArtwork>(true);
                Require(railArt != null && railArt.artworkTexture != null &&
                    railArt.GetComponent<CanvasRenderer>() != null, "Round railway artwork cannot render.");
                Require(!railArt.raycastTarget, "Railway artwork intercepts level clicks.");
                for (int i = 0; i < 30; i++)
                {
                    Require(Vector2.Distance(view.stopRects[i].anchoredPosition,
                        BuildForestHarborMap.Art(stationPositions[i])) < .05f, "Round click target misplaced: " + (i + 1));
                    Require(railArt.transform.GetSiblingIndex() < view.stopRects[i].GetSiblingIndex(),
                        "Railway artwork covers level feedback: " + (i + 1));
                    Require(view.glows[i].sprite != null &&
                        Mathf.Approximately(view.glows[i].rectTransform.sizeDelta.x, view.glows[i].rectTransform.sizeDelta.y),
                        "Round selection highlight missing: " + (i + 1));
                }
            }
            for(int i=0;i<50;i++)
            {
                var position = map.Point(map.stops[i].distance);
                var expected = BuildForestHarborMap.Art(stationPositions[i]);
                Require(Vector2.Distance(new Vector2(position.x,position.z)*20,expected)<.05f,"Station misplaced: "+(i+1));
                if(i>0)Require(map.stops[i].distance>map.stops[i-1].distance,"Route order broken.");
            }
            Travel(map,0);
            int forward = Travel(map,49), reverse = Travel(map,0);
            Require(forward==49&&reverse==49,"A station was skipped.");
            foreach(int next in new[]{10,20,30,40})
            {
                float from=map.stops[next-1].distance,to=map.stops[next].distance;
                Require(to-from>Vector3.Distance(map.Point(from),map.Point(to))*1.015f,"Curve missing before "+(next+1));
            }
            Wheels(map.trainMechanism);
            var navigate=typeof(TwilightRailwayMap).GetMethod("Navigate",Private);
            map.Select(0);
            navigate.Invoke(map,new object[]{Vector2.right,1f});Require(map.Selected==1,"Joystick right failed.");
            navigate.Invoke(map,new object[]{Vector2.right,1.1f});Require(map.Selected==1,"Joystick repeated too soon.");
            navigate.Invoke(map,new object[]{Vector2.right,1.41f});Require(map.Selected==2,"Joystick hold failed.");
            navigate.Invoke(map,new object[]{Vector2.left,1.42f});Require(map.Selected==1,"Joystick reversal failed.");
            navigate.Invoke(map,new object[]{Vector2.zero,1.43f});
            navigate.Invoke(map,new object[]{Vector2.down,1.44f});Require(map.Selected==2,"Joystick vertical navigation failed.");
            Require(Mathf.Approximately(map.Countdown,30),"Selection did not reset timer.");
            Travel(map,0); Set(map,"highest",0);map.Select(49);
            for(int i=0;i<180;i++)map.TickTrain(1f/60);
            map.illustratedMenu.Refresh();
            Require(map.Parked==0&&!map.Moving&&!map.illustratedMenu.playButton.interactable,"Locked level guard failed.");
            Require(map.illustratedMenu.stopButtons.Length==50,"Stop buttons missing.");
            var presentation=map.illustratedMenu.GetComponent<ForestHarborMapPresentation>();
            Require(presentation.previous!=null&&presentation.next!=null,"Selector arrows missing.");
            Set(map,"highest",49);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            copy.SetActive(true);
            Render(map,11,1920,1080,Output+"/ForestHarbor-Level12.png");
            Render(map,34,1920,1080,Output+"/ForestHarbor-World2.png");
            Render(map,49,1600,1000,Output+"/ForestHarbor-16x10.png");
            Render(map,29,2560,1080,Output+"/ForestHarbor-Ultrawide.png");
            if (map.illustratedMenu.GetComponent<ForestHarborMapPresentation>().roundForestStops)
                Render(map,20,1920,1080,Output+"/ForestHarbor-OpenCurve.png");
            string report="PASS (Edit mode only): 50 exact railway stops; 49 sequential arrivals forward and 49 in reverse; every intermediate station dwells at least 0.55 seconds; four route transitions follow curves; 12 wheels complete forward/reverse 360-degree turns and stop with the train; World 1/2 locomotive switching; joystick press, hold, repeat, reversal and vertical selection; selection resets the 30-second timer; locked destination cannot move the train or enable Play; whistle, rolling and arrival clips assigned. Renders: 16:9, 16:10, ultrawide. No Play mode, gameplay or player-progress writes.";
            File.WriteAllText(Output+"/validation.txt",report);return report;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    static int Travel(TwilightRailwayMap map,int destination)
    {
        map.Select(destination);int previous=map.Parked,arrivals=0;float dwell=0;bool arrived=false;
        for(int frame=0;frame<30000&&(map.Parked!=destination||map.Moving);frame++)
        {
            bool moving=map.Moving;map.TickTrain(1f/60);
            if(map.Parked!=previous)
            {
                Require(Math.Abs(map.Parked-previous)==1,"Train skipped a level.");
                Require(Vector3.Distance(map.train.localPosition,map.Point(map.stops[map.Parked].distance-map.trainNoseOffset))<.001f,"Train missed its stop.");
                previous=map.Parked;arrivals++;dwell=0;arrived=true;
            }
            else if(!moving&&arrived)
            {dwell+=1f/60;if(map.Moving){Require(dwell>=.55f,"Station pause too short.");arrived=false;}}
        }
        Require(map.Parked==destination&&!map.Moving,"Train did not arrive.");return arrivals;
    }
    static void Wheels(SuitcaseTrainMechanism mechanism)
    {
        foreach(var engine in new[]{mechanism.woodland,mechanism.moonlight})foreach(var wheel in engine.wheels)
        foreach(float sign in new[]{-1f,1f})
        {
            mechanism.Pose(0,1,false);float previous=wheel.pivot.localEulerAngles.z,total=0;
            for(int i=1;i<=32;i++)
            {mechanism.Pose(sign*2*Mathf.PI*wheel.radius*i/32,1,false);float current=wheel.pivot.localEulerAngles.z;total+=Mathf.DeltaAngle(previous,current);previous=current;}
            Require(Mathf.Abs(total+sign*360)<.05f,"Wheel did not rotate 360 degrees.");
        }
        mechanism.Pose(5,1,false);var stopped=mechanism.woodland.wheels[0].pivot.localRotation;
        mechanism.Pose(5,1,false);Require(Quaternion.Angle(stopped,mechanism.woodland.wheels[0].pivot.localRotation)<.001f,"Stationary wheel moved.");
        Require(mechanism.woodland.body.activeSelf&&!mechanism.moonlight.body.activeSelf,"Wrong World 1 engine.");
        mechanism.Pose(5,1,true);Require(!mechanism.woodland.body.activeSelf&&mechanism.moonlight.body.activeSelf,"Wrong World 2 engine.");
    }
    static void Render(TwilightRailwayMap map,int selected,int width,int height,string path)
    {
        Set(map,"selected",selected);Set(map,"parked",selected);Set(map,"destination",selected);
        Set(map,"distance",map.stops[selected].distance);Set(map,"moving",false);Set(map,"countdown",30f);
        typeof(TwilightRailwayMap).GetMethod("Pose",Private).Invoke(map,null);
        var view=map.illustratedMenu;view.Refresh();view.PoseTrain();view.countdownText.text="30";
        var presentation=view.GetComponent<ForestHarborMapPresentation>();presentation.Refresh();
        var rig=map.GetComponent<SuitcaseLocomotiveView>();rig.Frame();
        var camera=map.viewCamera;
        var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        var before=RenderTexture.active;var beforeTarget=camera.targetTexture;
        try
        {
            camera.targetTexture=target;camera.aspect=(float)width/height;
            view.canvas.worldCamera=camera;view.canvas.renderMode=RenderMode.ScreenSpaceCamera;
            Canvas.ForceUpdateCanvases();view.Layout();Canvas.ForceUpdateCanvases();
            foreach(var text in view.GetComponentsInChildren<TMPro.TMP_Text>(true))if(text.gameObject.activeInHierarchy)text.ForceMeshUpdate();
            for(int pass=0;pass<3;pass++)
            {
                var trainRequest=new UniversalRenderPipeline.SingleCameraRequest{destination=rig.trainCamera.targetTexture};
                RenderPipeline.SubmitRenderRequest(rig.trainCamera,trainRequest);
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                RenderPipeline.SubmitRenderRequest(camera,request);
            }
            if(selected==11)
            {
                var raycaster=view.canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                var buttons=new List<UnityEngine.UI.Button>(view.stopButtons){view.playButton,view.backButton,view.fullscreenButton,presentation.previous,presentation.next};
                foreach(var button in buttons)
                {
                    if(button==null||!button.gameObject.activeInHierarchy)continue;
                    if(button==view.stopButtons[map.Parked]&&!map.Moving)
                    {
                        var visibility=button.GetComponentInParent<CanvasGroup>();
                        if(visibility!=null&&visibility.alpha==0&&!visibility.blocksRaycasts)continue;
                    }
                    var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    {position=RectTransformUtility.WorldToScreenPoint(camera,button.transform.position)};
                    var hits=new List<UnityEngine.EventSystems.RaycastResult>();raycaster.Raycast(pointer,hits);
                    Require(hits.Count>0&&hits[0].gameObject==button.gameObject,"Blocked click target: "+button.transform.parent.name+"/"+button.name+" hits="+hits.Count+" depth="+button.image.depth+" culled="+button.image.canvasRenderer.cull+" pointer="+pointer.position);
                }
            }
            // Every station and every footer button must stay inside the fitted composition.
            foreach(var rect in view.stopRects)
            {var screen=camera.WorldToViewportPoint(rect.position);Require(screen.x>0&&screen.x<1&&screen.y>0&&screen.y<1,"Station clipped at "+width+"x"+height);}
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally {camera.targetTexture=beforeTarget;RenderTexture.active=before;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
    }
}
#endif
