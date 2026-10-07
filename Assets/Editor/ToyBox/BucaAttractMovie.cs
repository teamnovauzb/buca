using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Offline cinematic capture. Does not alter a gameplay scene, input, or player progress.
public static partial class BuildToyBoxMainMenu
{
    static Scene movieScene, previousScene;
    static Light[] oldLights;
    static bool[] oldLightEnabled;
    static Camera movieCamera;
    static Transform moviePuck;
    static Transform[] racePucks;
    static DepthOfField movieFocus;
    static Vector3 fireRest;
    static GameObject movieGuide;
    static Transform[] movieEyes,movieStars;
    static GameObject movieTitle,movieInvite;
    static RenderTexture movieTarget;
    static Texture2D movieImage;
    static int movieFrame;
    const int MovieFrames=1050;
    const string MovieFolder="output/attract-race/frames";
    public static void StartAttractMovie(bool preview=false, int firstFrame=0)
    {
        if(movieCamera!=null) throw new Exception("Capture is already active");
        ToyBoxGeometry.Initialize();
        previousScene=SceneManager.GetActiveScene();
        oldLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
        oldLightEnabled=Array.ConvertAll(oldLights,l=>l.enabled); foreach(var l in oldLights)l.enabled=false;
        movieScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(movieScene);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.24f,.26f,.30f);RenderSettings.ambientIntensity=1;
        var stage=Group("BUCA Story Capture",null);
        var level=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_01.prefab"),movieScene);
        level.transform.SetParent(stage,false);
        foreach(var b in level.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled=false;
        foreach(var tr in level.GetComponentsInChildren<Transform>(true))
            if(tr.name=="Guide_L"||tr.name=="Guide_R"||tr.name=="Nudge_L"||tr.name=="Nudge_R")tr.gameObject.SetActive(false);
        var cream=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/CoachCream.mat");
        var teal=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/CoachTeal.mat");
        var gold=new Material(cream); gold.color=new Color(1,.67f,.12f);gold.SetFloat("_Smoothness",.62f);gold.SetFloat("_Metallic",.32f);
        var maple=new Material(cream);maple.color=new Color(.94f,.83f,.62f);maple.SetFloat("_Smoothness",.25f);
        maple.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Materials/WorkshopMaplePhoto.png"));
        maple.SetTextureScale("_BaseMap",new Vector2(2,3));
        foreach(var renderer in level.GetComponentsInChildren<MeshRenderer>(true))if(renderer.name=="MapleWithCutout")renderer.sharedMaterial=maple;
        var ink=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/CoachInk.mat");
        Box("StoryPlinth",stage,new Vector3(0,-.8f,0),new Vector3(10,.8f,15.4f),teal,.25f);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ToyRoomScenery.prefab"),movieScene);
        room.transform.SetParent(stage,false);room.transform.localPosition=new Vector3(0,-1.3f,0);
        foreach(var b in room.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
        foreach(var tr in room.GetComponentsInChildren<Transform>(true))if(tr.name=="StandingTeddy")tr.gameObject.SetActive(false);
        var coral=new Material(gold);coral.color=new Color(.94f,.28f,.16f);coral.SetFloat("_Metallic",.12f);
        var blue=new Material(gold);blue.color=new Color(.075f,.31f,.68f);blue.SetFloat("_Metallic",.16f);
        racePucks=new Transform[3];
        var colors=new[]{gold,coral,blue};
        for(int i=0;i<3;i++){
            racePucks[i]=Group("RacePuck"+i,stage);
            Disc("RoundedCap",racePucks[i],Vector3.zero,.37f,.25f,colors[i],.055f);
            Ring("NavyInset",racePucks[i],new Vector3(0,.131f,0),.285f,.027f,ink,.014f);
        }
        moviePuck=racePucks[0];
        var mint=new Material(teal);mint.color=new Color(.42f,.78f,.67f);mint.SetFloat("_Smoothness",.4f);
        foreach(var center in new[]{new Vector3(0,.32f,.8f),new Vector3(-2.8f,.32f,2.7f),new Vector3(2.8f,.32f,3.2f)}){
            Box("RoundedRaceBumper",stage,center,new Vector3(1.45f,.64f,.85f),teal,.18f);
            Box("MintBumperTop",stage,center+Vector3.up*.305f,new Vector3(1.22f,.10f,.64f),mint,.049f);
        }
        movieStars=new Transform[36];
        for(int i=0;i<movieStars.Length;i++)movieStars[i]=MeshObject("CelebrationStar",stage,Vector3.zero,ToyBoxGeometry.MapPrism(10,1,.22f,.04f,true),i%4==0?coral:i%3==0?teal:gold).transform;
        var go=new GameObject("StoryCamera");movieCamera=go.AddComponent<Camera>();
        movieCamera.clearFlags=CameraClearFlags.SolidColor;movieCamera.backgroundColor=new Color(.77f,.69f,.52f);
        movieCamera.fieldOfView=41;movieCamera.nearClipPlane=.1f;movieCamera.farClipPlane=100;movieCamera.cullingMask=1<<29;
        var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.volumeLayerMask=1<<29;
        var volume=new GameObject("CinematicFocus").AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;
        volume.sharedProfile=ScriptableObject.CreateInstance<VolumeProfile>();
        movieFocus=volume.sharedProfile.Add<DepthOfField>(true);movieFocus.mode.Override(DepthOfFieldMode.Bokeh);movieFocus.aperture.Override(3.5f);movieFocus.focalLength.Override(55f);
        var light=new GameObject("StorySoftbox").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.95f;light.color=new Color(1,.93f,.80f);light.transform.rotation=Quaternion.Euler(48,-35,0);light.shadows=LightShadows.Soft;light.shadowBias=.025f;light.shadowNormalBias=.25f;light.shadowStrength=.75f;light.cullingMask=1<<29;
        var fill=new GameObject("StoryFill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.transform.rotation=Quaternion.Euler(35,140,0);fill.cullingMask=1<<29;
        foreach(var root in movieScene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
        movieTarget=new RenderTexture(preview?960:1920,preview?540:1080,24);movieTarget.antiAliasing=4;movieTarget.Create();
        movieImage=new Texture2D(movieTarget.width,movieTarget.height,TextureFormat.RGB24,false);
        Directory.CreateDirectory(MovieFolder);
        if(preview){try{foreach(float t in new[]{1f,4f,7.3f,10f,15f,20f,24f,27f,31f}){SampleAttract(t);CaptureAttract("output/attract-race/preview-"+t+".jpg");}}finally{EndAttract();}return;}
        movieFrame=firstFrame;EditorApplication.update+=CaptureAttractTick;
        AssemblyReloadEvents.beforeAssemblyReload+=EndAttract;
        EditorApplication.playModeStateChanged+=MoviePlayModeChanged;
    }
    static float Smooth(float v)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(v));
    static void SampleAttract(float t)
    {
        // Timed character beats: line-up, start, comic collision, recovery, chase, rim suspense, payoff.
        for(int i=0;i<3;i++) {racePucks[i].localScale=Vector3.one;racePucks[i].localRotation=Quaternion.Euler(0,t*85+i*40,0);}
        Vector3 gold,red,blue;
        if(t<2.5f){
            gold=new Vector3(-1.1f,.18f,-4.8f);red=new Vector3(0,.18f,-4.8f);blue=new Vector3(1.1f,.18f,-4.8f);
            var arr=new[]{gold,red,blue};for(int i=0;i<3;i++){float jump=(t-.25f-i*.55f)/.45f;if(jump>0&&jump<1)arr[i].y+=Mathf.Sin(jump*Mathf.PI)*.15f;}
            gold=arr[0];red=arr[1];blue=arr[2];
        }else if(t<7.2f){
            float u=Smooth((t-2.5f)/4.7f);
            gold=Vector3.Lerp(new Vector3(-1.1f,.18f,-4.8f),new Vector3(-1.45f,.18f,.25f),u);
            red=Vector3.Lerp(new Vector3(0,.18f,-4.8f),new Vector3(.36f,.18f,-.08f),u);
            blue=Vector3.Lerp(new Vector3(1.1f,.18f,-4.8f),new Vector3(1.10f,.18f,-.08f),u);
        }else if(t<11f){
            float u=Smooth((t-7.2f)/3.8f);
            gold=Vector3.Lerp(new Vector3(-1.45f,.18f,.25f),new Vector3(-1.45f,.18f,2.5f),u);
            float bump=Mathf.Sin(Mathf.Min((t-7.2f)*7,Mathf.PI));
            red=new Vector3(.36f-bump*.32f,.18f+Mathf.Max(0,bump)*.08f,-.08f-.32f*u);
            blue=new Vector3(1.10f+bump*.45f,.18f,-.08f-.22f*u);
            racePucks[1].localRotation=Quaternion.Euler(0,t*400,Mathf.Sin((t-7.2f)*8)*12*(1-u));
            racePucks[2].localRotation=Quaternion.Euler(0,-t*450,-Mathf.Sin((t-7.2f)*8)*12*(1-u));
        }else if(t<18f){
            float u=Smooth((t-11)/7f);
            if(t<13f)gold=Vector3.Lerp(new Vector3(-1.45f,.18f,2.5f),new Vector3(1.1f,.18f,2.1f),Smooth((t-11)/2));
            else if(t<15f)gold=Vector3.Lerp(new Vector3(1.1f,.18f,2.1f),new Vector3(1.1f,.18f,4.5f),Smooth((t-13)/2));
            else if(t<16.3f)gold=Vector3.Lerp(new Vector3(1.1f,.18f,4.5f),new Vector3(4.0f,.18f,4.5f),Mathf.Clamp01((t-15)/1.3f));
            else gold=Vector3.Lerp(new Vector3(4.0f,.18f,4.5f),new Vector3(.6f,.18f,4.4f),Mathf.Clamp01((t-16.3f)/1.7f));
            if(u<.3f)red=Vector3.Lerp(new Vector3(.36f,.18f,-.4f),new Vector3(-1.3f,.18f,-.4f),u/.3f);
            else if(u<.7f)red=Vector3.Lerp(new Vector3(-1.3f,.18f,-.4f),new Vector3(-1.3f,.18f,2.2f),(u-.3f)/.4f);
            else red=Vector3.Lerp(new Vector3(-1.3f,.18f,2.2f),new Vector3(-.85f,.18f,3.8f),(u-.7f)/.3f);
            blue=RaceCurve(new Vector3(1.1f,.18f,-.3f),new Vector3(1.7f,.18f,1.6f),new Vector3(1.1f,.18f,3.65f),u);
        }else if(t<23f){
            float u=Smooth((t-18)/5f);
            gold=Vector3.Lerp(new Vector3(.6f,.18f,4.4f),new Vector3(.38f,.18f,5.4f),u);
            red=Vector3.Lerp(new Vector3(-.85f,.18f,3.8f),new Vector3(-.8f,.18f,4.55f),u);
            blue=Vector3.Lerp(new Vector3(1.1f,.18f,3.65f),new Vector3(.8f,.18f,4.55f),u);
        }else{
            float u=Mathf.Clamp01((t-23)/3f);float angle=u*Mathf.PI*4;float radius=.38f*(1-Smooth((u-.5f)*2));
            gold=new Vector3(Mathf.Cos(angle)*radius,.18f-Smooth((u-.7f)/.3f)*.6f,5.4f+Mathf.Sin(angle)*radius);
            red=new Vector3(-.8f,.18f,4.55f);blue=new Vector3(.8f,.18f,4.55f);
            racePucks[0].localRotation=Quaternion.Euler(0,angle*57,20*Mathf.Sin(angle));
            racePucks[0].localScale=Vector3.one*(1-Smooth((t-25.5f)/.5f));
            if(t>26){red.y+=Mathf.Max(0,Mathf.Sin((t-26)*6))*.17f;blue.y+=Mathf.Max(0,Mathf.Sin((t-26)*6+.7f))*.17f;}
        }
        racePucks[0].localPosition=gold;racePucks[1].localPosition=red;racePucks[2].localPosition=blue;
        for(int i=0;i<movieStars.Length;i++){
            float s=t-26-i*.012f;movieStars[i].gameObject.SetActive(s>=0&&s<3.0f);
            float a=i*2.39996f;
            movieStars[i].localPosition=new Vector3(Mathf.Cos(a)*s*1.8f,.4f+4*s-1.3f*s*s,5.4f+Mathf.Sin(a)*s*1.2f);
            movieStars[i].localScale=Vector3.one*(.13f+(i%3)*.04f);
            movieStars[i].localRotation=Quaternion.Euler(70+s*150,a*57,s*180);
        }
        Vector3 position,target;
        if(t<2.5f){position=Vector3.Lerp(new Vector3(2.9f,1.65f,-8.7f),new Vector3(2.1f,1.45f,-8.3f),Smooth(t/2.5f));target=new Vector3(0,.15f,-4.8f);}
        else if(t<5.5f){var center=(gold+red+blue)/3;position=center+new Vector3(.5f,2.4f,-4.1f);target=center+new Vector3(0,0,1.6f);}
        else if(t<8.3f){position=new Vector3(3.4f,2.6f,-3.6f);target=new Vector3(.05f,.18f,.05f);}
        else if(t<11f){position=gold+new Vector3(-2.3f,1.8f,-2.8f);target=gold+new Vector3(.2f,0,.6f);}
        else if(t<14.5f){position=new Vector3(1.4f,6.6f,-3.6f);target=new Vector3(0,0,1.3f);}
        else if(t<18f){position=gold+new Vector3(2.5f,2.2f,-3.4f);target=gold+new Vector3(0,0,.45f);}
        else if(t<21f){position=new Vector3(-2.5f,4.5f,.8f);target=new Vector3(0,.15f,4.9f);}
        else if(t<26f){position=Vector3.Lerp(new Vector3(1.7f,2.5f,2.2f),new Vector3(1.3f,1.7f,3f),Smooth((t-21)/5));target=new Vector3(0,.1f,5.4f);}
        else if(t<30f){position=Vector3.Lerp(new Vector3(3.3f,5.4f,-.7f),new Vector3(3.8f,6.2f,-1),Smooth((t-26)/4));target=new Vector3(0,1.0f,5.1f);}
        else{position=Vector3.Lerp(new Vector3(0,12,-15),new Vector3(0,11,-14),Smooth((t-30)/5));target=new Vector3(0,0,1);}
        movieCamera.transform.position=position;movieCamera.transform.LookAt(target);
        movieFocus.focusDistance.Override(Vector3.Distance(position,target));
    }
    static Vector3 RaceCurve(Vector3 a,Vector3 b,Vector3 c,float t)=>Vector3.Lerp(Vector3.Lerp(a,b,t),Vector3.Lerp(b,c,t),t);
    static void CaptureAttract(string path){
        var request=new UniversalRenderPipeline.SingleCameraRequest{destination=movieTarget};
        RenderPipeline.SubmitRenderRequest(movieCamera,request);
        var prior=RenderTexture.active;RenderTexture.active=movieTarget;
        movieImage.ReadPixels(new Rect(0,0,movieTarget.width,movieTarget.height),0,0);movieImage.Apply();RenderTexture.active=prior;
        File.WriteAllBytes(path,movieImage.EncodeToJPG(94));
    }
    static void CaptureAttractTick(){try{SampleAttract(movieFrame/30f);CaptureAttract(MovieFolder+"/"+movieFrame.ToString("D4")+".jpg");movieFrame++;if(movieFrame%30==0)File.WriteAllText("output/attract-race/progress.txt",movieFrame+" / "+MovieFrames);if(movieFrame>=MovieFrames){EndAttract();File.WriteAllText("output/attract-race/done.txt","1050 frames / 1920x1080 / 30 fps");}}catch(Exception e){EndAttract();Debug.LogException(e);}}
    static void MoviePlayModeChanged(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingEditMode)EndAttract();}
    static void EndAttract(){AssemblyReloadEvents.beforeAssemblyReload-=EndAttract;EditorApplication.playModeStateChanged-=MoviePlayModeChanged;EditorApplication.update-=CaptureAttractTick;if(movieTarget!=null){movieTarget.Release();UnityEngine.Object.DestroyImmediate(movieTarget);}if(movieImage!=null)UnityEngine.Object.DestroyImmediate(movieImage);EditorSceneManager.CloseScene(movieScene,true);SceneManager.SetActiveScene(previousScene);for(int i=0;i<oldLights.Length;i++)if(oldLights[i]!=null)oldLights[i].enabled=oldLightEnabled[i];movieCamera=null;}
}
