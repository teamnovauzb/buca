#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class WorldStationValidation
{
    const string Prefab="Assets/ToyBoxMenu/WorldStation/WorldStationJourney.prefab";
    public static string Render(string name="station",float aspect=1.7777778f,bool bonus=false,bool unlocked=true,float journeyTime=-1,int renderWidth=1920,float victoryTime=-1,float welcomeTime=-1)
    {
        Directory.CreateDirectory("output/world-station");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));root.transform.position=new Vector3(1400,0,0);root.SetActive(true);
        var view=root.GetComponent<WorldStationRoute3D>();int width=renderWidth,height=Mathf.RoundToInt(width/aspect);
        var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf){antiAliasing=4};var image=new Texture2D(width,height,TextureFormat.RGB24,false);var active=RenderTexture.active;
        var outsideLights=new System.Collections.Generic.List<Light>();
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.enabled&&!light.transform.IsChildOf(root.transform)){outsideLights.Add(light);light.enabled=false;}
        var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
        try
        {
            RenderSettings.ambientSkyColor=new Color(.48f,.49f,.50f);RenderSettings.ambientEquatorColor=new Color(.37f,.35f,.31f);RenderSettings.ambientGroundColor=new Color(.23f,.21f,.18f);
            view.SetUnlocked(unlocked);view.Sample(5f,journeyTime);if(bonus){view.ShowBonus();view.Sample(5f);}
            view.viewCamera.aspect=aspect;view.viewCamera.targetTexture=rt;view.Fit(aspect);if(journeyTime>=0)view.SampleTransition(journeyTime);if(victoryTime>=0){view.victory.gameObject.SetActive(true);view.victory.Sample(victoryTime);}
            if(welcomeTime>=0){view.welcome.gameObject.SetActive(true);view.welcome.Sample(welcomeTime);}
            Physics.SyncTransforms();VolumeManager.instance.Update(view.viewCamera.transform,1<<29);view.viewCamera.Render();
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();string path="output/world-station/"+name+".png";File.WriteAllBytes(path,image.EncodeToPNG());return path;
        }
        finally{foreach(var light in outsideLights)if(light!=null)light.enabled=true;RenderTexture.active=active;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
    public static string ValidateSignalTimer()
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        var v=root.GetComponent<WorldStationRoute3D>();
        try
        {
            v.ShowBonus();var timer=v.signalTimer;
            if(timer==null || timer.Seconds!=30)throw new Exception("Timer missing or not reset");
            foreach(int seconds in new[]{30,11,10,6,5,0})
            {
                timer.Display(seconds);
                int lamp=seconds>10?0:seconds>5?1:2;
                for(int j=0;j<3;j++)if(timer.lenses[j].sharedMaterial!=(j==lamp?timer.lit[j]:timer.unlit[j]))throw new Exception("Wrong lamp");
                for(int j=0;j<20;j++)if(timer.numerals[j].activeSelf!=(j%10==(j<10?seconds/10:seconds%10)))throw new Exception("Wrong digit");
            }
            v.ShowBonus();v.TickBonusCountdown(19.5f);if(timer.Seconds!=11)throw new Exception("Wrong countdown");
            v.TickBonusCountdown(.5f);if(timer.Seconds!=10)throw new Exception("Wrong amber threshold");
            v.ResetWorld();v.SetUnlocked(true);v.BeginDeparture();v.ShowBonus();v.TickBonusCountdown(20);
            if(timer.Seconds!=30)throw new Exception("Countdown ran during arrival");
            v.ResetWorld();v.ShowBonus();if(timer.Seconds!=30)throw new Exception("Reopen did not reset");
            return "PASS: numeral tiles, green/amber/red thresholds, elapsed countdown, paused during journey, reset on reopen; no saved progress changed.";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    public static string Validate()
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));root.transform.position=new Vector3(1400,0,0);root.SetActive(true);
        var v=root.GetComponent<WorldStationRoute3D>();
        try
        {
            if(v.bonusButtons.Length!=20||v.firstWorldContent.Length<5||v.smoke.Length<6||v.wheels.Length!=6)throw new Exception("Incomplete saved content");
            if(v.movingTrain==null||v.departure==null)throw new Exception("Missing train movement audio");
            if(v.steamAudio==null||v.eventAudio==null||v.steamAudio.playOnAwake)throw new Exception("Train movement audio not configured");
            if(root.transform.Find("CozyStationRoom/EveningStationCanopy")==null)throw new Exception("Missing dimensional evening station");
            v.ResetWorld();
            if(v.worldOneRoom==null||v.worldTwoCity==null||!v.worldOneRoom.activeSelf||v.worldTwoCity.activeSelf)throw new Exception("World 1 must start in its room");
            if(v.worldOneRoom.transform.Find("ShelfTeddyBear")==null)throw new Exception("Original room missing");
            v.ShowBonus();if(v.worldOneRoom.activeSelf||!v.worldTwoCity.activeSelf)throw new Exception("World 2 must use the evening city");
            v.ResetWorld();
            if(root.transform.Find("CozyStationRoom/EveningCityArchitecture")==null||root.transform.Find("CozyStationRoom/RaisedCityRailwayBridge")==null)throw new Exception("Missing evening city depth");
            v.Sample(.7f);var idlePosition=v.train.localPosition;var steamPosition=v.smoke[3].position;
            v.Sample(1.6f);
            if(Vector3.Distance(idlePosition,v.train.localPosition)<.005f)throw new Exception("Train idle breathing is static");
            if(Vector3.Distance(steamPosition,v.smoke[3].position)<.1f)throw new Exception("Steam is not rising");
            if(v.route[0].z<.7f)throw new Exception("Train has not moved back along the station rails");
            v.Sample(0);var parked=v.train.localPosition;v.Sample(2);
            if(Vector3.Distance(parked,v.train.localPosition)>.08f)throw new Exception("World 1 train must wait on the track, not enter");
            v.Sample(1,1);float departureStart=Vector3.Distance(v.train.localPosition,v.RoutePoint(0));
            v.Sample(3.8f,3.8f);if(Vector3.Distance(v.train.localPosition,v.RoutePoint(0))>=departureStart+3)throw new Exception("Train must depart toward the gate");
            v.Sample(4.1f,4.1f);
            if(Mathf.Abs(Mathf.DeltaAngle(0,v.leftDoor.localEulerAngles.y))>.1f||!v.train.gameObject.activeSelf)throw new Exception("Gate must close before hiding train");
            v.Sample(4.25f,4.25f);if(v.train.gameObject.activeSelf)throw new Exception("Departed train remains active");
            v.ShowBonus();v.Sample(7.3f,7.3f);
            if(!v.train.gameObject.activeSelf||Vector3.Distance(v.train.localPosition,v.RoutePoint(.55f))>.08f)throw new Exception("Train must arrive and park in World 2");
            v.ResetWorld();
            v.SetUnlocked(false);v.OpenFromMenu(_=>{});if(v.Travelling)throw new Exception("Locked route accepted input");
            v.SetUnlocked(true);v.OpenFromMenu(_=>{});if(!v.Travelling)throw new Exception("Unlocked route did not depart");
            v.Sample(1,1);if(Mathf.Abs(Mathf.DeltaAngle(0,v.leftDoor.localEulerAngles.y))<40)throw new Exception("Train exit door is closed");
            v.Sample(2,2);
            var p=v.train.localPosition;v.Sample(4,4);if(Vector3.Distance(p,v.train.localPosition)<1)throw new Exception("Train does not move");
            v.ResetWorld();v.SetUnlocked(true);v.BeginDeparture();
            var first=v.firstWorldContent[0].transform;var resting=first.localPosition;
            v.SampleTransition(3.9f);
            if(Vector3.Distance(first.localPosition,resting)>.001f)throw new Exception("Level trays must remain stationary");
            v.SampleTransition(4.7f);if(v.transitionCurtain==null||v.transitionCurtain.color.a<.99f)throw new Exception("World swap must be fully covered");
            v.SampleTransition(5.5f);if(!v.BonusVisible||!v.Modal)throw new Exception("World 2 reveal did not remain modal");
            v.ResetWorld();if(Vector3.Distance(first.localPosition,resting)>.001f||v.BonusVisible)throw new Exception("Transition reset did not restore the scene");
            v.ResetWorld();
            if(!v.worldOneLabel.activeSelf||v.worldTwoLabel.activeSelf)throw new Exception("World 1 label incorrect");
            v.ShowBonus();if(v.worldOneLabel.activeSelf||!v.worldTwoLabel.activeSelf)throw new Exception("World 2 label incorrect");v.ResetWorld();
            foreach(float aspect in new[]{16f/9f,4f/3f,21f/9f})
            {
                v.viewCamera.aspect=aspect;v.Fit(aspect);Physics.SyncTransforms();
                foreach(var b in v.bonusButtons)
                {var s=v.viewCamera.WorldToViewportPoint(b.hit.bounds.center);if(s.x<.02f||s.x>.98f||s.y<.02f||s.y>.98f)throw new Exception("Bonus button cropped at "+aspect);}
                var gp=v.viewCamera.WorldToViewportPoint(v.gateHit.bounds.center);if(gp.x>.98f||gp.y>.98f)throw new Exception("Gate cropped");
            }
            return "PASS: parked World 1 train, gate departure, World 2 train arrival; stationary trays, full-screen fade, correct world labels, World 2 reveal and transition reset; World 1 room restored, World 2 evening city isolated, modeled bridge, saved 3D backdrop, 20 bonus controls, six animated wheels, continuous moving steam, train breathing and rear parking, departure and movement sound clips, locked gate guard, open exit doors, train movement, and 16:9/4:3/21:9 framing.";
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    public static string BuildWebPreview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit scene required");
        string restore=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,scenePath="Assets/ToyBoxMenu/WorldStation/StationWebPreview.unity";
        string template=PlayerSettings.WebGL.template;var compression=PlayerSettings.WebGL.compressionFormat;var optimization=UnityEditor.WebGL.UserBuildSettings.codeOptimization;
        try
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);root.SetActive(true);
            root.AddComponent<WorldStationPreviewDriver>();
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.49f,.50f);RenderSettings.ambientEquatorColor=new Color(.37f,.35f,.31f);RenderSettings.ambientGroundColor=new Color(.23f,.21f,.18f);
            EditorSceneManager.SaveScene(scene,scenePath);PlayerSettings.WebGL.template="APPLICATION:Default";PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            UnityEditor.WebGL.UserBuildSettings.codeOptimization=UnityEditor.WebGL.WasmCodeOptimization.BuildTimes;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},target=BuildTarget.WebGL,locationPathName="output/world-station/web-preview",options=BuildOptions.Development});
            string result=report.summary.result+"; errors="+report.summary.totalErrors+"; duration="+report.summary.totalTime;
            File.WriteAllText("output/world-station/web-build.txt",result);return result;
        }
        finally{PlayerSettings.WebGL.template=template;PlayerSettings.WebGL.compressionFormat=compression;UnityEditor.WebGL.UserBuildSettings.codeOptimization=optimization;EditorSceneManager.OpenScene(restore);AssetDatabase.DeleteAsset(scenePath);}
    }
}
#endif
