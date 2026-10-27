#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CompletionJourneyValidation
{
    public static string Check()
    {
        var prefab=Resources.Load<TwilightRailwayMap>("CompletionRailwayMap");
        if(prefab==null)throw new Exception("Missing map prefab");
        var scene=EditorSceneManager.NewPreviewScene();
        try{
            var map=Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(map.gameObject,scene);
            map.eventAudio=map.motionAudio=null;
            if(map.viewCamera==null||map.illustratedMenu==null||map.GetComponent<SuitcaseLocomotiveView>()==null)throw new Exception("Map references missing");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            int count=0;
            foreach(int completed in new[]{0,4,9,14,19,24,30,48}){
                int choice=-1,next=completed+1;
                map.OpenAfterCompletion(completed,next,index=>choice=index);
                typeof(TwilightRailwayMap).GetField("highest",flags).SetValue(map,49);map.Select(next);
                if(map.Parked!=completed||Mathf.Abs(map.RouteDistance-map.stops[completed].distance)>.001f)throw new Exception("Skipped completed stop");
                map.RequestPlay();if(choice!=-1)throw new Exception("Play skipped train travel");
                bool travelled=false;
                for(int i=0;i<600&&choice<0;i++){map.TickTrain(1f/60);travelled|=map.Moving;}
                if(!travelled||choice!=next||map.Parked!=next)throw new Exception("Journey did not arrive at "+(next+1));
                count++;
            }
            return "PASS: "+count+" completion journeys, including 5→6 and 49→50; correct starting stop, visible movement, early Play waits for arrival, same-session callback. No progress writes.";
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
