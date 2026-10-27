using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static partial class BuildToyBoxMainMenu
{
    public static void BakeEveningGameplay()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
        PrepareFolders();ToyBoxGeometry.Initialize();
        var previous=SceneManager.GetActiveScene().path;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var old=GameObject.Find("EveningGameplayTheme");if(old!=null)Object.DestroyImmediate(old);
        var root=Group("EveningGameplayTheme",null);var theme=root.gameObject.AddComponent<EveningGameplayTheme>();
        theme.manager=Object.FindAnyObjectByType<LevelManager>();theme.gameplayCamera=Camera.main;
        foreach(var g in scene.GetRootGameObjects())if(g.name=="SavedToyPlayroom")theme.room=g;
        var navy=StationMaterial("GameplayMidnightNavy",new Color(.025f,.07f,.135f),.35f,.34f);
        var gold=StationMaterial("GameplayChampagneBrass",new Color(.72f,.47f,.19f),.65f,.38f);
        var ivory=StationMaterial("GameplayWarmIvory",new Color(.78f,.70f,.53f),0,.18f);
        var slate=StationMaterial("GameplayStationSlate",new Color(.10f,.15f,.21f),.08f,.18f);
        var stone=StationMaterial("GameplayStationStone",new Color(.22f,.28f,.34f),0,.25f);
        var glass=StationMaterial("GameplayAmberGlass",new Color(1,.71f,.34f),0,.25f);glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(1.4f,.75f,.22f));
        var brown=StationMaterial("GameplayLuggageLeather",new Color(.18f,.08f,.035f),0,.25f);
        theme.navy=navy;theme.brass=gold;theme.ivory=ivory;
        var env=Group("EveningStationEnvironment",root);theme.station=env.gameObject;
        foreach(int side in new[]{-1,1}){Box("CabinetBrassEdge",env,new Vector3(side*5.32f,.38f,0),new Vector3(.045f,.055f,14.8f),gold,.018f);}
        Box("SlatePlatform",env,new Vector3(0,-3.3f,8),new Vector3(80,.2f,90),slate,.02f);
        for(int x=-6;x<=6;x++)for(int z=-5;z<=9;z++)Box("Paving",env,new Vector3(x*3.1f+(z%2)*1.5f,-3.15f,z*3.1f),new Vector3(3.04f,.08f,3.04f),slate,.025f);
        for(int i=-5;i<=5;i++)
        {
            float h=6+(i*i%4);var house=Group("CityFacade",env,new Vector3(i*5,-3.1f,22+Mathf.Abs(i)%2*4));
            Box("Building",house,new Vector3(0,h/2,0),new Vector3(4.7f,h,3),stone,.12f);
            Box("Cornice",house,new Vector3(0,h,0),new Vector3(4.95f,.22f,3.2f),navy,.05f);
            for(int row=0;row<3;row++)for(int col=-1;col<=1;col++)Box("LitWindow",house,new Vector3(col*1.3f,1.2f+row*1.9f,-1.53f),new Vector3(.6f,.95f,.06f),glass,.04f);
        }
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<3;i++)
            {
                var lamp=Group("PlatformLantern",env,new Vector3(side*(8+i*.5f),-3.1f,1+i*7));
                Disc("Foot",lamp,Vector3.zero,.42f,.18f,navy,.04f);Disc("BrassPost",lamp,new Vector3(0,2.7f,0),.08f,5.4f,gold,.02f);
                Box("LanternGlass",lamp,new Vector3(0,5.55f,0),new Vector3(.48f,.75f,.48f),glass,.09f);
                Box("LanternCap",lamp,new Vector3(0,5.98f,0),new Vector3(.75f,.12f,.75f),navy,.05f);
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box("LanternFrame",lamp,new Vector3(sx*.25f,5.55f,sz*.25f),new Vector3(.04f,.8f,.04f),gold,.01f);
                var light=Group("AmberPool",lamp,new Vector3(0,5.4f,0)).gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=9;light.intensity=2;light.color=new Color(1,.74f,.43f);
            }
            var bench=Group("StationBench",env,new Vector3(side*9,-3.1f,7));
            for(int i=0;i<4;i++){Box("SeatSlat",bench,new Vector3(0,1,-.5f+i*.3f),new Vector3(3.7f,.12f,.25f),brown,.035f);Box("BackSlat",bench,new Vector3(0,1.4f+i*.23f,.55f),new Vector3(3.7f,.15f,.1f),brown,.03f);}
            foreach(int s in new[]{-1,1})Box("BenchLeg",bench,new Vector3(s*1.4f,.5f,0),new Vector3(.18f,1,1.2f),navy,.035f);
            for(int i=0;i<2;i++)
            {
                var bag=Group("LeatherSuitcase",env,new Vector3(side*7.6f,-2.6f+i*.78f,-4));
                Box("Case",bag,Vector3.zero,new Vector3(1.9f,.78f,1.1f),brown,.15f);
                foreach(int s in new[]{-1,1})Box("BrassStrap",bag,new Vector3(s*.6f,0,0),new Vector3(.065f,.8f,1.12f),gold,.02f);
            }
        }
        // A real modeled station canopy and parked passenger carriage frame the view.
        for(int side=-1;side<=1;side+=2)
        {
            for(int row=0;row<2;row++)
            {
                var at=new Vector3(side*10,-3.1f,13+row*6);
                Box("CanopyColumn",env,at+Vector3.up*4.4f,new Vector3(.3f,8.8f,.3f),navy,.05f);
                Box("ColumnFoot",env,at+Vector3.up*.35f,new Vector3(.65f,.7f,.65f),gold,.06f);
                Box("Capital",env,at+Vector3.up*8.5f,new Vector3(.8f,.22f,.8f),gold,.04f);
            }
        }
        for(int row=0;row<2;row++)MeshObject("ArchedCanopyRib",env,new Vector3(0,5.4f,13+row*6),StationArch(10,.13f,.18f),gold);
        var carriage=Group("PassengerTrain",env,new Vector3(-2,-1.7f,17));
        Box("CarriageBody",carriage,Vector3.zero,new Vector3(24,2.1f,2.2f),navy,.22f);
        Box("ChampagneBelt",carriage,new Vector3(0,-.5f,-1.12f),new Vector3(23.7f,.065f,.04f),gold,.01f);
        Box("CarriageRoof",carriage,new Vector3(0,1.15f,0),new Vector3(24.3f,.45f,2.5f),navy,.2f);
        for(int i=-7;i<=7;i++)
        {
            Box("WindowBrassFrame",carriage,new Vector3(i*1.45f,.35f,-1.12f),new Vector3(1.02f,1.13f,.12f),gold,.09f);
            Box("WarmCarriageWindow",carriage,new Vector3(i*1.45f,.35f,-1.20f),new Vector3(.85f,.94f,.06f),glass,.07f);
        }
        foreach(float x in new[]{-9f,-7f,7f,9f}){var wheel=Disc("TrainWheel",carriage,new Vector3(x,-1.05f,-.9f),.45f,.2f,navy,.03f);wheel.transform.localRotation=Quaternion.Euler(90,0,0);}
        var clock=Group("StationClock",env,new Vector3(7,3.7f,12));
        var rim=Disc("ClockBezel",clock,Vector3.zero,1.15f,.2f,gold,.06f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
        var face=Disc("ClockFace",clock,new Vector3(0,0,-.14f),1.02f,.04f,ivory,.02f);face.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tick=Box("HourMark",clock,new Vector3(Mathf.Sin(a)*.85f,Mathf.Cos(a)*.85f,-.18f),new Vector3(.035f,.13f,.025f),navy,.01f);tick.transform.localRotation=Quaternion.Euler(0,0,-i*30);}
        Box("MinuteHand",clock,new Vector3(0,.3f,-.20f),new Vector3(.035f,.6f,.04f),navy,.01f);
        var hand=Box("HourHand",clock,new Vector3(.22f,0,-.22f),new Vector3(.45f,.04f,.04f),navy,.01f);hand.transform.localRotation=Quaternion.Euler(0,0,25);
        var key=Group("WarmBoardKey",env).gameObject.AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.color=new Color(1,.88f,.69f);key.shadows=LightShadows.Soft;key.shadowStrength=.55f;key.transform.rotation=Quaternion.Euler(55,-30,0);
        var fill=Group("EveningSkyFill",env).gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.color=new Color(.48f,.65f,1);fill.transform.rotation=Quaternion.Euler(35,145,0);
        PolishEveningStation(theme,env,navy,gold,ivory,slate,stone,glass,brown);
        env.gameObject.SetActive(false);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(previous)&&previous!=scene.path)EditorSceneManager.OpenScene(previous);
    }
    public static string RenderEveningGameplay()
    {
        var previous=SceneManager.GetActiveScene().path;var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var theme=Object.FindAnyObjectByType<EveningGameplayTheme>();var level=Object.Instantiate(theme.manager.levelPrefabs[30]);
        theme.Apply(true,level);var camera=theme.gameplayCamera;camera.aspect=16f/9;
        var hud=Object.FindAnyObjectByType<ToyBoxGameplayHud>();
        var show=typeof(ToyBoxGameplayHud).GetMethod("Show",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        show.Invoke(hud,new object[]{hud.levelDigits,31,true});show.Invoke(hud,new object[]{hud.timeDigits,30,true});hud.RefreshHearts(150,150);
        var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;
        UnityEngine.Rendering.VolumeManager.instance.Update(camera.transform,camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().volumeLayerMask);
        camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();
        string path=Path.GetFullPath("output/evening-gameplay-polished.png");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(level);
        EditorSceneManager.OpenScene(string.IsNullOrEmpty(previous)?"Assets/Scenes/MainMenu.unity":previous);return path;
    }
}
