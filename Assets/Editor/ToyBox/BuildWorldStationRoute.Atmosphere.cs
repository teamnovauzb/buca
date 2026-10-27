#if UNITY_EDITOR
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    static void BuildStationRoomDepth(Transform room)
    {
        var navy=StationMaterial("EveningPanelBlue",new Color(.035f,.064f,.12f),.08f,.17f);
        var trim=StationMaterial("EveningPanelTrim",new Color(.075f,.12f,.20f),.12f,.12f);
        var sky=StationMaterial("EveningSky",new Color(.025f,.065f,.15f),0,0);
        var distant=StationMaterial("EveningTownBlue",new Color(.065f,.11f,.17f),0,.15f);
        var map=room.parent;
        foreach(string n in new[]{"QuietWall","QuietBackdrop"}){var old=map.Find(n);if(old!=null)old.gameObject.SetActive(false);}
        foreach(var oldLight in map.GetComponentsInChildren<Light>(true))if(oldLight.type==LightType.Directional)oldLight.enabled=false;
        var key=Group("EveningArchitecturalKey",room).gameObject.AddComponent<Light>();
        key.type=LightType.Directional;key.color=new Color(1f,.79f,.57f);key.intensity=1.08f;key.shadows=LightShadows.Soft;key.shadowStrength=.68f;key.shadowBias=.025f;key.transform.localRotation=Quaternion.Euler(48,-35,0);
        var fill=Group("BlueHourFill",room).gameObject.AddComponent<Light>();
        fill.type=LightType.Directional;fill.color=new Color(.43f,.61f,1f);fill.intensity=.20f;fill.transform.localRotation=Quaternion.Euler(28,135,0);
        var paving=StationMaterial("CitySlatePaving",new Color(.14f,.19f,.26f),.18f,.08f);
        var stone=StationMaterial("CityLimestone",new Color(.32f,.37f,.43f),.05f,.14f);
        var brick=StationMaterial("CityWarmBrick",new Color(.30f,.22f,.19f),0,.18f);
        var cityBlue=StationMaterial("CityBlueFacades",new Color(.17f,.28f,.38f),.05f,.1f);
        var roofMat=StationMaterial("CitySlateRoofs",new Color(.085f,.15f,.24f),.38f,.04f);
        ApplyCityStoneSurface(paving);
        var pavingAlt=StationMaterial("CitySlatePavingVariation",new Color(.155f,.20f,.265f),.18f,.08f);ApplyCityStoneSurface(pavingAlt);
        var distantWindow=StationMaterial("CityWindowWarmDim",new Color(.48f,.30f,.13f),0,0);
        distantWindow.SetColor("_EmissionColor",new Color(.38f,.16f,.035f));
        var darkWindow=StationMaterial("CityWindowNightGlass",new Color(.035f,.075f,.13f),.25f,0);
        sky.shader=Shader.Find("Universal Render Pipeline/Unlit");sky.SetColor("_BaseColor",new Color(.045f,.085f,.16f));
        var square=Group("OutdoorStationSquare",room);
        StationBox("SquareFoundation",square,new Vector3(0,-1.05f,2),new Vector3(100,.3f,85),paving,.04f);
        for(int x=0;x<24;x++)for(int z=0;z<19;z++)
            StationBox("StonePavingSlab",square,new Vector3(-35+x*3.2f+(z%2)*1.6f,-.86f,-25+z*3.2f),new Vector3(3.15f,.08f,3.15f),(x+z)%5==0?pavingAlt:paving,.025f);
        StationBox("BlueHourSky",room,new Vector3(0,30,48),new Vector3(180,110,.4f),sky,.03f);
        var town=Group("EveningCityArchitecture",room,new Vector3(0,0,3f));town.localScale=new Vector3(1,.54f,.65f);
        // Two staggered streets, with modeled reveals, cornices, balconies and chimneys.
        for(int row=0;row<2;row++)for(int i=0;i<12;i++)
        {
            float x=-33+i*5.8f+(row==1?2.6f:0),h=4.7f+((i*7+row*3)%5)*.48f;
            float z=row==0?18.8f:31f;
            var house=Group("CityBuilding_"+row+"_"+i,town,new Vector3(x,-.75f,z+(i%3)*.7f));
            house.localScale=new Vector3(.88f+(i%3)*.055f,1f,1f);
            var facade=(i%3==0?brick:(i%3==1?cityBlue:stone));
            StationBox("MasonryFacade",house,new Vector3(0,h*.5f,0),new Vector3(4.9f,h,4.2f),facade,.09f);
            foreach(float y in new[]{.25f,h*.48f,h-.15f})StationBox("ProjectingCornice",house,new Vector3(0,y,-.12f),new Vector3(5.15f,.18f,4.5f),stone,.045f);
            foreach(int side in new[]{-1,1})
            {
                var roof=StationBox("SlateRoof",house,new Vector3(side*1.30f,h+.72f,0),new Vector3(3.02f,.22f,4.6f),roofMat,.04f);
                roof.transform.localRotation=Quaternion.Euler(0,0,-side*30);
                StationBox("CornerPilaster",house,new Vector3(side*2.28f,h*.5f,-2.16f),new Vector3(.23f,h,.23f),stone,.035f);
            }
            if(i%3==1)
            {
                StationBox("BalconyStoneLedge",house,new Vector3(0,h*.48f-.2f,-2.5f),new Vector3(3.5f,.16f,.85f),stone,.035f);
                WorkshopRod("BalconyTopRail",house,new Vector3(-1.65f,h*.48f+.55f,-2.88f),new Vector3(1.65f,h*.48f+.55f,-2.88f),.045f,roofMat);
                for(int bar=0;bar<9;bar++)WorkshopRod("BalconyBaluster",house,new Vector3(-1.65f+bar*.41f,h*.48f-.1f,-2.88f),new Vector3(-1.65f+bar*.41f,h*.48f+.55f,-2.88f),.035f,roofMat);
            }
            StationBox("Chimney",house,new Vector3(1.45f,h+1.4f,.8f),new Vector3(.55f,1.5f,.65f),brick,.04f);
            StationBox("ChimneyCap",house,new Vector3(1.45f,h+2.15f,.8f),new Vector3(.74f,.18f,.82f),stone,.025f);
            for(int level=0;level<3;level++)for(int col=0;col<3;col++)
            {
                Vector3 at=new Vector3(-1.45f+col*1.45f,.9f+level*(h-1.3f)/3,-2.16f);
                StationBox("DeepWindowReveal",house,at,new Vector3(.96f,1.20f,.2f),roofMat,.04f);
                StationBox("WarmWindowGlass",house,at+Vector3.back*.12f,new Vector3(.73f,.97f,.055f),(i+level*2+col+row)%5==0?darkWindow:((i+col)%3==0?distantWindow:stationGlow),.025f);
                StationBox("WindowSill",house,at+new Vector3(0,-.64f,-.12f),new Vector3(1.1f,.13f,.38f),stone,.025f);
                StationBox("WindowMullion",house,at+Vector3.back*.17f,new Vector3(.06f,1.03f,.04f),stationGold,.01f);
                StationBox("WindowTransom",house,at+Vector3.back*.17f,new Vector3(.8f,.06f,.04f),stationGold,.01f);
            }
        }
        var bridge=Group("RaisedCityRailwayBridge",room,new Vector3(0,-.75f,12.5f));
        bridge.localScale=new Vector3(1,.7f,.9f);
        for(int i=0;i<13;i++)
        {
            float x=-34+i*5.5f;
            StationBox("StonePier",bridge,new Vector3(x,1.35f,0),new Vector3(.8f,2.7f,2.4f),stone,.07f);
            StationBox("PierCapital",bridge,new Vector3(x,2.55f,0),new Vector3(1.12f,.22f,2.65f),stone,.045f);
            for(int joint=0;joint<4;joint++)StationBox("PierStoneJoint",bridge,new Vector3(x,.45f+joint*.55f,-1.215f),new Vector3(.78f,.024f,.025f),roofMat,.004f);
            MeshObject("ViaductArch",bridge,new Vector3(x+2.75f,1.3f,0),StationArch(2.38f,.38f,2.3f),stone);
        }
        StationBox("BridgeDeck",bridge,new Vector3(0,3.98f,0),new Vector3(78,.42f,2.8f),stone,.065f);
        foreach(float z in new[]{-1.28f,1.28f})
        {
            foreach(float y in new[]{4.35f,4.9f})WorkshopRod("BridgeBrassRail",bridge,new Vector3(-39,y,z),new Vector3(39,y,z),.045f,stationGold);
            for(int i=0;i<53;i++)WorkshopRod("RailingUpright",bridge,new Vector3(-39+i*1.5f,4.18f,z),new Vector3(-39+i*1.5f,4.93f,z),.045f,roofMat);
        }
        foreach(float x in new[]{-26f,-15f,-4f,7f,24f})BuildEveningStandingLantern(room,new Vector3(x,2.20f,12.5f),.70f);
        // Real barrel-vault canopy with deep roof ribs and brass posts.
        var canopy=Group("EveningStationCanopy",room,new Vector3(14.3f,.15f,10.3f));
        var roofMesh=StationArch(4.4f,.16f,4.2f);
        var roofObj=MeshObject("BarrelRoof",canopy,new Vector3(0,2.15f,0),roofMesh,navy);roofObj.transform.localScale=new Vector3(1,.48f,1);
        foreach(float z in new[]{-2.15f,-1.1f,0,1.1f,2.15f})
        {
            var rib=MeshObject("BrassRoofRib",canopy,new Vector3(0,2.15f,z),StationArch(4.53f,.12f,.11f),stationGold);rib.transform.localScale=new Vector3(1,.48f,1);
        }
        for(int seam=1;seam<14;seam++)
        {
            float a=seam*Mathf.PI/14;
            float x=Mathf.Cos(a)*4.45f,y=2.15f+Mathf.Sin(a)*4.45f*.48f;
            WorkshopRod("RoofStandingSeam",canopy,new Vector3(x,y,-2.08f),new Vector3(x,y,2.08f),.027f,trim);
            foreach(float z in new[]{-2.18f,2.18f})StationSphere("CanopyBrassRivet",canopy,new Vector3(x,y+.025f,z),Vector3.one*.085f,stationGold);
        }
        foreach(int side in new[]{-1,1})foreach(float z in new[]{-1.95f,1.95f})
        {
            Vector3 p=new Vector3(side*4.33f,0,z);
            StationBox("PostFoot",canopy,p+Vector3.up*.13f,new Vector3(.48f,.26f,.48f),stationBlue,.06f);
            StationBox("CanopyPost",canopy,p+Vector3.up*1.16f,new Vector3(.17f,2.1f,.17f),stationGold,.04f);
        }
        StationBox("StationRearWall",canopy,new Vector3(0,1.1f,1.84f),new Vector3(8.6f,2.2f,.23f),stationDark,.08f);
        foreach(float x in new[]{-3.1f,-1.8f,1.8f,3.1f})
        {
            StationBox("StationWindowBezel",canopy,new Vector3(x,1.2f,1.67f),new Vector3(.93f,1.25f,.17f),stationGold,.09f);
            StationBox("StationAmberGlass",canopy,new Vector3(x,1.2f,1.55f),new Vector3(.77f,1.08f,.09f),stationGlow,.07f);
            StationBox("WindowCrossbar",canopy,new Vector3(x,1.2f,1.48f),new Vector3(.81f,.065f,.06f),stationDark,.015f);
            StationBox("WindowUpright",canopy,new Vector3(x,1.2f,1.48f),new Vector3(.065f,1.10f,.06f),stationDark,.015f);
        }
        foreach(int side in new[]{-1,1})
        {
            StationBox("StationStoneWing",canopy,new Vector3(side*3.6f,1.15f,0),new Vector3(1.35f,2.3f,3.6f),brick,.06f);
            StationBox("StationWingCornice",canopy,new Vector3(side*3.6f,2.25f,0),new Vector3(1.55f,.16f,3.8f),stationGold,.025f);
            StationBox("StationFrontWindowFrame",canopy,new Vector3(side*3.6f,1.2f,-1.88f),new Vector3(.95f,1.5f,.15f),stationGold,.045f);
            StationBox("StationFrontAmberGlass",canopy,new Vector3(side*3.6f,1.2f,-1.98f),new Vector3(.77f,1.30f,.06f),stationGlow,.025f);
            StationBox("StationFrontWindowCross",canopy,new Vector3(side*3.6f,1.2f,-2.03f),new Vector3(.81f,.08f,.05f),stationDark,.01f);
            StationBox("StationFrontWindowUpright",canopy,new Vector3(side*3.6f,1.2f,-2.03f),new Vector3(.06f,1.35f,.05f),stationDark,.01f);
        }
        BuildEveningStandingLantern(room,new Vector3(7.4f,-.72f,10.7f),1f);
        BuildEveningStandingLantern(room,new Vector3(20.1f,-.72f,5.6f),1.15f);
        BuildEveningStandingLantern(room,new Vector3(-18f,-.72f,6f),1.15f);
        var bench=Group("CityStationBench",room,new Vector3(-15f,-.70f,8.8f));
        foreach(int side in new[]{-1,1})
        {
            StationBox("BenchIronLeg",bench,new Vector3(side*1.2f,.43f,0),new Vector3(.13f,.86f,.70f),roofMat,.035f);
            StationBox("BenchBackSupport",bench,new Vector3(side*1.2f,1.03f,.30f),new Vector3(.10f,1.05f,.10f),roofMat,.02f);
        }
        for(int slat=0;slat<4;slat++)StationBox("BenchSeatSlat",bench,new Vector3(0,.89f,-.30f+slat*.20f),new Vector3(3.1f,.12f,.17f),stationDark,.025f);
        for(int slat=0;slat<3;slat++)StationBox("BenchBackSlat",bench,new Vector3(0,1.12f+slat*.18f,.36f),new Vector3(3.1f,.14f,.11f),stationDark,.025f);
        BuildStationSquareEdges(room,stone,roofMat);
        var luggage=Group("StationLuggage",room,new Vector3(18.4f,.16f,1.3f));
        for(int i=0;i<3;i++)
        {
            var bag=Group("LeatherSuitcase"+i,luggage,new Vector3((i%2)*1.0f,(i==2?.64f:0)+.3f,(i%2)*.35f));bag.localRotation=Quaternion.Euler(0,i*12-12,0);
            StationBox("RoundedCase",bag,Vector3.zero,new Vector3(1.25f,.56f,.81f),stationDark,.10f);
            foreach(int side in new[]{-1,1})StationBox("BrassStrap",bag,new Vector3(side*.37f,0,-.415f),new Vector3(.07f,.51f,.03f),stationGold,.01f);
            WorkshopRod("LuggageHandle",bag,new Vector3(-.2f,.34f,0),new Vector3(.2f,.34f,0),.045f,stationGold);
        }
        // A sculpted station clock, including raised hands and tick markers.
        var clock=Group("BrassStationClock",room,new Vector3(19.2f,2.9f,3.0f));
        var dial=Disc("ClockBrassCase",clock,Vector3.zero,.65f,.20f,stationGold,.05f);dial.transform.localRotation=Quaternion.Euler(90,0,0);
        var face=Disc("ClockIvoryFace",clock,new Vector3(0,0,-.13f),.56f,.035f,stationCream,.02f);face.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tick=StationBox("HourMark",clock,new Vector3(Mathf.Sin(a)*.47f,Mathf.Cos(a)*.47f,-.16f),new Vector3(.035f,.10f,.025f),stationInk,.009f);tick.transform.localRotation=Quaternion.Euler(0,0,-i*30);}
        WorkshopRod("ClockMinuteHand",clock,new Vector3(0,0,-.19f),new Vector3(.29f,.27f,-.19f),.027f,stationInk);
        WorkshopRod("ClockHourHand",clock,new Vector3(0,0,-.20f),new Vector3(-.21f,.15f,-.20f),.035f,stationInk);
        WorkshopRod("ClockStand",room,new Vector3(19.2f,.2f,3),new Vector3(19.2f,2.3f,3),.09f,stationGold);
    }
    static void BuildStationSquareEdges(Transform room,Material stone,Material iron)
    {
        var foliage=StationMaterial("CityPlanterFoliage",new Color(.055f,.19f,.13f),0,.1f);
        var rim=StationMaterial("CityPlanterRim",new Color(.27f,.32f,.36f),.2f,0);
        // Keep decorations outside the board and turnout interaction bounds.
        foreach(var at in new[]{new Vector3(-12f,-.75f,-1.7f),new Vector3(-9f,-.75f,-9.2f),new Vector3(16.7f,-.75f,-6.8f)})
        {
            var planter=Group("SquareStonePlanter",room,at);
            StationBox("PlanterFoot",planter,new Vector3(0,.12f,0),new Vector3(2.6f,.24f,1.65f),iron,.09f);
            StationBox("CarvedStoneBody",planter,new Vector3(0,.59f,0),new Vector3(2.5f,.85f,1.55f),stone,.10f);
            StationBox("RaisedStoneRim",planter,new Vector3(0,1.03f,0),new Vector3(2.72f,.16f,1.76f),rim,.05f);
            StationBox("DarkSoil",planter,new Vector3(0,1.13f,0),new Vector3(2.38f,.06f,1.43f),stationDark,.04f);
            for(int leaf=0;leaf<15;leaf++)
            {
                float x=-.93f+(leaf%5)*.46f,z=-.45f+(leaf/5)*.44f;
                var shrub=StationSphere("DenseEvergreen",planter,new Vector3(x,1.34f+.09f*Mathf.Sin(leaf*2),z),new Vector3(.72f,.59f,.65f),foliage);
            }
            foreach(int side in new[]{-1,1})StationBox("PlanterBrassInlay",planter,new Vector3(side*.85f,.58f,-.79f),new Vector3(.04f,.57f,.025f),stationGold,.008f);
        }
        BuildEveningStandingLantern(room,new Vector3(-11.8f,-.72f,-5.4f),1.25f);
        BuildEveningStandingLantern(room,new Vector3(18.4f,-.72f,-3.5f),1.25f);
        var waiting=Group("ForegroundWaitingBench",room,new Vector3(4f,-.72f,-9.7f));
        foreach(int side in new[]{-1,1})
        {
            StationBox("IronBenchLeg",waiting,new Vector3(side*1.6f,.42f,0),new Vector3(.16f,.84f,.80f),iron,.035f);
            StationBox("BackSupport",waiting,new Vector3(side*1.6f,1.05f,-.35f),new Vector3(.12f,1.1f,.12f),iron,.025f);
        }
        for(int slat=0;slat<4;slat++)StationBox("WalnutSeat",waiting,new Vector3(0,.86f,-.3f+slat*.2f),new Vector3(3.8f,.13f,.17f),stationDark,.03f);
        for(int slat=0;slat<3;slat++)StationBox("WalnutBack",waiting,new Vector3(0,1.13f+slat*.19f,-.39f),new Vector3(3.8f,.14f,.12f),stationDark,.025f);
        // Inlaid stone border gives the open forecourt an intentional structure.
        foreach(float z in new[]{-11.5f,-11.8f})StationBox("ForecourtStoneInlay",room,new Vector3(1f,-.802f,z),new Vector3(28,.018f,.09f),rim,.006f);
        foreach(float x in new[]{-12f,21.8f})StationBox("SidewalkStoneInlay",room,new Vector3(x,-.802f,-2),new Vector3(.09f,.018f,19),rim,.006f);
    }
    static void ApplyCityStoneSurface(Material material)
    {
        const int size=256;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="StationSlatePavers",wrapMode=TextureWrapMode.Repeat,anisoLevel=8};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            int row=y/32,shift=(row%2)*32,col=((x+shift)%256)/64;
            int ux=(x+shift)%64,uy=y%32;
            float noise=Mathf.PerlinNoise(x*.17f,y*.17f);
            float value=.76f+.16f*noise+.055f*Mathf.Sin(row*8.9f+col*3.7f);
            if(ux<2||uy<2)value*=.43f;
            else if(ux==2||uy==2)value*=1.08f;
            pixels[y*size+x]=new Color(value,value,value,.50f+.10f*noise);
        }
        texture.SetPixels(pixels);texture.Apply();texture=ToyBoxGeometry.Save(texture,StationFolder+"/StationSlatePavers.asset");
        material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",.52f);
        UnityEditor.EditorUtility.SetDirty(material);
    }
    static void BuildEveningStandingLantern(Transform room,Vector3 at,float scale)
    {
        var lamp=Group("EveningBrassLantern",room,at);lamp.localScale=Vector3.one*scale;
        Disc("LanternBase",lamp,new Vector3(0,.10f,0),.42f,.20f,stationBlue,.05f);
        Disc("BrassStem",lamp,new Vector3(0,1.32f,0),.07f,2.5f,stationGold,.025f);
        BuildStationLantern(lamp,new Vector3(0,2.65f,.25f));
        StationFloorShade("LanternWarmPool",lamp,new Vector3(0,.006f,0),4.3f,3.8f,stationWarmPool);
        var light=Group("WarmLanternLight",lamp,new Vector3(0,2.55f,0)).gameObject.AddComponent<Light>();
        light.type=LightType.Point;light.color=new Color(1,.64f,.30f);light.intensity=5f;light.range=8;light.shadows=LightShadows.None;
    }
}
#endif
