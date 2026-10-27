using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    static void PolishEveningStation(EveningGameplayTheme theme,Transform env,Material navy,Material gold,Material ivory,Material slate,Material stone,Material glass,Material brown)
    {
        navy.SetColor("_BaseColor",new Color(.15f,.245f,.37f));navy.SetFloat("_Metallic",.12f);navy.SetFloat("_Smoothness",.46f);
        gold.SetColor("_BaseColor",new Color(.88f,.67f,.36f));gold.SetFloat("_Metallic",.48f);gold.SetFloat("_Smoothness",.56f);
        ivory.SetColor("_BaseColor",new Color(.72f,.65f,.51f));ivory.SetFloat("_Smoothness",.28f);
        glass.SetColor("_BaseColor",new Color(1,.83f,.57f));glass.SetColor("_EmissionColor",new Color(4.2f,2.5f,.9f));
        brown.SetColor("_BaseColor",new Color(.27f,.13f,.065f));brown.SetFloat("_Smoothness",.35f);
        slate.SetColor("_BaseColor",new Color(.10f,.13f,.18f));slate.SetFloat("_Smoothness",.53f);
        var tex=EveningSurfaceTexture();foreach(var mat in new[]{slate,ivory,brown}){mat.SetTexture("_BaseMap",tex);mat.SetTextureScale("_BaseMap",new Vector2(4,4));EditorUtility.SetDirty(mat);}
        navy.SetTexture("_BaseMap",null);
        var rivet=StationSphere("BumperBrassStud",null,Vector3.zero,new Vector3(.115f,.115f,.055f),gold);
        theme.rivetPrefab=PrefabUtility.SaveAsPrefabAsset(rivet,Root+"/Prefabs/EveningBumperStud.prefab");Object.DestroyImmediate(rivet);
        theme.goldPuck=gold;theme.goalBlue=StationMaterial("EveningGoalCobalt",new Color(.025f,.13f,.50f),.4f);
        theme.blackCap=StationMaterial("EveningShootBlack",new Color(.10f,.13f,.17f),.08f);
        theme.blackCap.SetFloat("_Smoothness",.38f);
        theme.bumper=StationMaterial("EveningBumperEnamel",new Color(.19f,.32f,.47f),.12f);
        theme.bumper.SetFloat("_Smoothness",.42f);
        // Same controls and interaction geometry, integrated into one low cabinet deck.
        Box("IntegratedControlDeck",env,new Vector3(0,.25f,-7.95f),new Vector3(10.35f,.35f,1.90f),navy,.18f);
        Box("DeckLeadingGoldLine",env,new Vector3(0,.44f,-8.86f),new Vector3(10.02f,.04f,.04f),gold,.015f);
        Box("FrontCabinetFascia",env,new Vector3(0,-.53f,-8.30f),new Vector3(10.3f,.98f,.24f),navy,.16f);
        foreach(float y in new[]{-.92f,-.1f})Box("FasciaPinstripe",env,new Vector3(0,y,-8.44f),new Vector3(9.8f,.035f,.04f),gold,.014f);
        foreach(int side in new[]{-1,1})
        {
            Box("BroadBrassRail",env,new Vector3(side*5.14f,.43f,0),new Vector3(.16f,.085f,14.5f),gold,.035f);
            Box("InnerBrassRail",env,new Vector3(side*4.55f,.40f,0),new Vector3(.055f,.075f,14.5f),gold,.02f);
            foreach(float z in new[]{-6.9f,6.9f})Disc("RailBrassRivet",env,new Vector3(side*4.95f,.57f,z),.095f,.045f,gold,.016f);
        }
        foreach(float x in new[]{-3.15f,0f,3.15f})
        {
            var frame=Group("FramedHudSign",env,new Vector3(x,.92f,7));frame.localRotation=Quaternion.Euler(12,0,0);
            float width=x==0?3.45f:2.5f;
            foreach(int side in new[]{-1,1})
            {
                Box("GoldSignSide",frame,new Vector3(side*(width*.5f+.025f),0,-.10f),new Vector3(.055f,.99f,.09f),gold,.02f);
                Box("GoldSignEdge",frame,new Vector3(0,side*.48f,-.10f),new Vector3(width+.10f,.045f,.09f),gold,.018f);
            }
        }
        // Vertical masonry details avoid the flat box skyline of the initial pass.
        var dim=StationMaterial("EveningWindowDim",new Color(.22f,.20f,.17f),.2f);
        var ledge=StationMaterial("EveningStoneTrim",new Color(.27f,.32f,.38f),.08f);
        foreach(Transform house in env)
        {
            if(house.name!="CityFacade")continue;
            var building=house.Find("Building").GetComponent<MeshRenderer>().bounds;float h=building.size.y;
            for(int side=-1;side<=1;side+=2)Box("MasonryPilaster",house,new Vector3(side*2.19f,h*.5f,-1.58f),new Vector3(.24f,h,.22f),ledge,.04f);
            for(int row=0;row<3;row++)
            {
                Box("StoreyLedge",house,new Vector3(0,.58f+row*1.9f,-1.60f),new Vector3(4.7f,.12f,.28f),ledge,.035f);
                for(int col=-1;col<=1;col++)
                {
                    Vector3 at=new Vector3(col*1.3f,1.2f+row*1.9f,-1.64f);
                    foreach(int side in new[]{-1,1})Box("WindowReveal",house,at+Vector3.right*side*.37f,new Vector3(.12f,1.18f,.15f),navy,.025f);
                    Box("WindowMullion",house,at,new Vector3(.05f,1,.1f),navy,.012f);
                    Box("WindowTransom",house,at+Vector3.up*.12f,new Vector3(.75f,.04f,.1f),navy,.01f);
                    Box("WindowSill",house,at+Vector3.down*.58f,new Vector3(.9f,.13f,.25f),ledge,.025f);
                }
            }
        }
        SignalLetter("BUCA",env,new Vector3(0,-.48f,-8.49f),.43f,gold,2.3f);
        foreach(int side in new[]{-1,1})Box("BrandRule",env,new Vector3(side*3,-.48f,-8.48f),new Vector3(1.8f,.035f,.04f),gold,.015f);
        // Fine slate joints and occasional brass strips give the platform a believable scale.
        foreach(Transform item in env)if(item.name=="Paving")item.gameObject.SetActive(false);
        var slateAlt=StationMaterial("EveningSlateAlternate",new Color(.13f,.17f,.22f),.15f);slateAlt.SetFloat("_Smoothness",.5f);slateAlt.SetTexture("_BaseMap",tex);
        for(int x=-9;x<=9;x++)for(int z=-7;z<=12;z++)Box("CutSlatePaver",env,new Vector3(x*2.2f+(z%2)*1.1f,-3.14f,z*2.6f),new Vector3(2.18f,.08f,2.58f),(x+z)%3==0?slateAlt:slate,.015f);
        // More substantial station lantern bases and arms.
        foreach(Transform lamp in env)
        {
            if(lamp.name!="PlatformLantern")continue;
            Disc("FlutedLowerShaft",lamp,new Vector3(0,.65f,0),.17f,1.3f,navy,.035f);
            foreach(float y in new[]{.16f,.35f,1.30f,4.9f,5.1f})Disc("TurnedBrassCollar",lamp,new Vector3(0,y,0),y<.4f?.26f:.145f,.10f,gold,.025f);
            var pool=lamp.Find("AmberPool").GetComponent<Light>();pool.intensity=55;pool.range=9;
            StationSphere("LanternFinial",lamp,new Vector3(0,6.13f,0),Vector3.one*.20f,gold);
        }
        // A station clock placed at the edge of the readable game view.
        var clock=env.Find("StationClock");clock.localPosition=new Vector3(8,2,7.3f);
        Disc("ClockPedestal",env,new Vector3(8,-3.02f,7.3f),.48f,.18f,navy,.04f);
        Disc("ClockColumn",env,new Vector3(8,-.6f,7.4f),.12f,4.8f,gold,.03f);
        // Small framed departure boards fill the side spaces without covering the field.
        foreach(int side in new[]{-1,1})
        {
            var sign=Group("StationDepartureBoard",env,new Vector3(side*9.2f,-.4f,.5f));
            Box("BrassFrame",sign,Vector3.zero,new Vector3(2.5f,3.6f,.20f),gold,.09f);
            Box("NavyFace",sign,new Vector3(0,0,-.12f),new Vector3(2.32f,3.42f,.07f),navy,.055f);
            StationText(side<0?"BUCA":"WORLD 2",sign,new Vector3(0,.95f,-.20f),.31f,ivory,false,2.1f);
            StationText(side<0?"EXPRESS":"NIGHT CITY",sign,new Vector3(0,.32f,-.20f),.23f,gold,false,2.1f);
            Box("SignRule",sign,new Vector3(0,-.12f,-.19f),new Vector3(1.8f,.025f,.03f),gold,.01f);
            StationText("PLATFORM 02",sign,new Vector3(0,-.6f,-.20f),.17f,ivory,false,2.1f);
            for(int leg=-1;leg<=1;leg+=2)Box("SignLeg",sign,new Vector3(leg*.8f,-2,0),new Vector3(.12f,1.8f,.14f),gold,.02f);
        }
        var roomPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ToyPlayroom.prefab");
        var plantSource=roomPrefab!=null?roomPrefab.transform.Find("DottedPotPlant"):null;
        if(plantSource!=null)foreach(int side in new[]{-1,1})
        {
            var plant=Object.Instantiate(plantSource.gameObject,env);plant.name="StationPlanter";plant.transform.localPosition=new Vector3(side*10,-2.7f,-5);plant.transform.localScale=Vector3.one*1.5f;
        }
        // Reflection capture supplied as a saved studio environment, not generated at runtime.
        var probe=Group("StationReflection",env,new Vector3(0,2,0)).gameObject.AddComponent<ReflectionProbe>();
        probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=EveningReflection();probe.size=new Vector3(80,35,80);probe.boxProjection=false;probe.intensity=.85f;probe.importance=10;
        var key=env.Find("WarmBoardKey").GetComponent<Light>();key.intensity=1.3f;key.color=new Color(1,.92f,.82f);key.shadowStrength=.72f;
        key.transform.rotation=Quaternion.Euler(42,-38,0);key.shadowBias=.025f;key.shadowNormalBias=.18f;
        env.Find("EveningSkyFill").GetComponent<Light>().intensity=.38f;
        var front=Group("CabinetSoftFill",env).gameObject.AddComponent<Light>();front.type=LightType.Directional;
        front.color=new Color(.77f,.86f,1);front.intensity=.48f;front.transform.rotation=Quaternion.Euler(18,22,0);
        var volume=Group("EveningStationGrade",env).gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=70;volume.gameObject.layer=29;
        string profilePath=Root+"/Materials/EveningGameplayGrade.asset";
        var previous=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);if(previous!=null){foreach(var c in previous.components)Object.DestroyImmediate(c,true);previous.components.Clear();}
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="EveningGameplayGrade";
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        var color=profile.Add<ColorAdjustments>(true);color.contrast.Override(3);color.saturation.Override(0);color.postExposure.Override(0);
        var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.15f);bloom.intensity.Override(.30f);bloom.scatter.Override(.65f);
        var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.10f);vignette.smoothness.Override(.55f);
        profile.Add<DepthOfField>(true).mode.Override(DepthOfFieldMode.Off);
        volume.sharedProfile=ToyBoxGeometry.Save(profile,profilePath);foreach(var c in volume.sharedProfile.components)if(!AssetDatabase.Contains(c))AssetDatabase.AddObjectToAsset(c,volume.sharedProfile);
        var data=theme.gameplayCamera.GetComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;data.volumeLayerMask|=1<<29;theme.gameplayCamera.allowHDR=true;
        AddEveningSolidDetails(env,navy,gold,ivory,brown);
        EditorUtility.SetDirty(volume.sharedProfile);
    }
    static Texture2D EveningSurfaceTexture()
    {
        const int n=128;var t=new Texture2D(n,n,TextureFormat.RGB24,false);t.name="EveningFineGrain";var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++){float value=.91f+.07f*Mathf.PerlinNoise(x*.36f,y*.36f)+.02f*Mathf.Sin(y*.9f);pixels[y*n+x]=new Color(value,value,value);}
        t.SetPixels(pixels);t.Apply();t.wrapMode=TextureWrapMode.Repeat;return ToyBoxGeometry.Save(t,Root+"/Materials/EveningFineGrain.asset");
    }
    static Cubemap EveningReflection()
    {
        const int n=64;var cube=new Cubemap(n,TextureFormat.RGBAHalf,true);cube.name="EveningStationReflection";
        for(int face=0;face<6;face++)
        {
            var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=(x+.5f)/n,v=(y+.5f)/n;Color c=Color.Lerp(new Color(.13f,.16f,.22f),new Color(.45f,.55f,.72f),v);
                if(face==2)c=new Color(.65f,.70f,.8f);if(face==3)c=new Color(.08f,.065f,.05f);
                float panel=Mathf.Exp(-Mathf.Pow((u-.25f)*18,2)-Mathf.Pow((v-.6f)*3,8));c+=new Color(2.8f,2.4f,1.8f)*panel;
                pixels[y*n+x]=c;
            }cube.SetPixels(pixels,(CubemapFace)face);
        }cube.Apply();return ToyBoxGeometry.Save(cube,Root+"/Materials/EveningStationReflection.asset");
    }
}
