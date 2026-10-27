#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    static void PrepareStationPolishMaterials()
    {
        stationInk=StationMaterial("StationMidnight",new Color(.012f,.045f,.06f),.13f,0);
        stationSteel=StationMaterial("StationRailSilver",new Color(.59f,.68f,.70f),.82f,0);
        stationDoorInset=StationMaterial("StationDoorRecess",new Color(.022f,.17f,.15f),.10f,.16f);
        var shade=new Material(Shader.Find("BUCA/Station Contact"));shade.SetColor("_BaseColor",new Color(.045f,.023f,.012f,.62f));
        stationContact=ToyBoxGeometry.Save(shade,StationFolder+"/StationSoftContact.mat");
        var pool=new Material(Shader.Find("BUCA/Station Light Pool"));pool.SetColor("_Color",new Color(1,.57f,.20f,.32f));
        stationWarmPool=ToyBoxGeometry.Save(pool,StationFolder+"/StationWarmPool.mat");
    }
    static void StationFloorShade(string name,Transform p,Vector3 at,float width,float depth,Material material=null)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ToyBoxMenu/AllAboard/ContactQuad.asset");
        var go=MeshObject(name,p,at,mesh,material??stationContact);go.transform.localScale=new Vector3(width,1,depth);
        go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    static void BuildStationPlatform(Transform ext)
    {
        StationBox("BluePlatformPlinth",ext,new Vector3(13.5f,-.39f,2.85f),new Vector3(9.2f,.60f,9.65f),stationBlue,.28f);
        StationBox("BrassPlatformInlay",ext,new Vector3(13.5f,-.12f,2.85f),new Vector3(9.10f,.13f,9.55f),stationGold,.18f);
        StationBox("MapleStationExtension",ext,new Vector3(13.5f,.01f,2.85f),new Vector3(8.96f,.21f,9.42f),stationMaple,.20f);
        StationFloorShade("StationGrounding",ext,new Vector3(14.35f,.121f,4.55f),6.9f,6.2f);
        // A wood-paved threshold keeps the track inside the station visibly grounded.
        for(int i=0;i<9;i++)StationBox("ThresholdMapleTile",ext,new Vector3(14.25f,.145f,2.85f+i*.47f),new Vector3(3.05f,.06f,.445f),stationCream,.025f);
    }
    static void BuildPolishedStationRailway(WorldStationRoute3D view,Transform ext)
    {
        var track=Group("RightHandTurnout",ext);const int count=100;
        for(int i=0;i<count;i++)
        {
            Vector3 a=StationPath(i/(float)count),b=StationPath((i+1)/(float)count),direction=(b-a).normalized;
            var across=Vector3.Cross(Vector3.up,direction);
            // Match the larger engine's wheel gauge and blend into the original oval.
            float gauge=Mathf.Lerp(.36f,.64f,Mathf.SmoothStep(0,1,i/20f));
            if(i%5==0)
            {
                var tie=StationBox("WalnutSleeper",track,a-Vector3.up*.075f,new Vector3(gauge*2+.55f,.13f,.25f),stationDark,.035f);
                tie.transform.localRotation=Quaternion.LookRotation(direction);
                foreach(int side in new[]{-1,1})
                {
                    var plate=StationBox("BrassRailChair",track,a+across*side*gauge-Vector3.up*.01f,new Vector3(.29f,.045f,.22f),stationGold,.015f);
                    plate.transform.localRotation=Quaternion.LookRotation(direction);
                    Disc("SleeperBolt",track,a+across*side*(gauge+.18f)+Vector3.up*.005f,.055f,.065f,stationSteel,.015f);
                }
            }
            foreach(int side in new[]{-1,1})
            {
                WorkshopRod("DarkRailWeb",track,a+across*side*gauge,b+across*side*gauge,.066f,stationInk);
                WorkshopRod("PolishedRailCrown",track,a+across*side*gauge+Vector3.up*.05f,b+across*side*gauge+Vector3.up*.05f,.042f,stationSteel);
            }
        }
        // Park farther back along the same rails, leaving the front turnout clear.
        view.route=Enumerable.Range(0,101).Select(i=>StationPath(.95f-.87f*i/100)+Vector3.up*.46f).ToArray();
        StationBox("SwitchBase",ext,new Vector3(9.35f,.29f,-2.05f),new Vector3(.8f,.18f,.80f),stationBlue,.10f);
        Disc("SwitchBrassSocket",ext,new Vector3(9.35f,.42f,-2.05f),.23f,.07f,stationGold,.02f);
        view.switchLever=Group("AnimatedSwitchLever",ext,new Vector3(9.35f,.43f,-2.05f));
        Disc("SwitchRod",view.switchLever,new Vector3(0,.32f,0),.065f,.65f,stationSteel,.02f);
        StationSphere("TealSwitchKnob",view.switchLever,new Vector3(0,.68f,0),Vector3.one*.34f,stationTeal);
    }
    static void StationArchLayer(Transform gate,string name,float radius,float width,float depth,float z,Material material)
    {
        foreach(int side in new[]{-1,1})StationBox(name+"Column",gate,new Vector3(side*radius,1.40f,z),new Vector3(width,2.8f,depth),material,Mathf.Min(.12f,width*.45f));
        MeshObject(name+"Arch",gate,new Vector3(0,2.8f,z),StationArch(radius,width,depth),material);
    }
    static void StationHeroPlaque(Transform p,string text,Vector3 at,float width,float height,float textHeight,bool dark)
    {
        var board=Group(text+"Sign",p,at);
        StationBox("DeepWalnutBacking",board,Vector3.zero,new Vector3(width+.18f,height+.12f,.32f),stationDark,.13f);
        StationBox("PolishedBrassBezel",board,new Vector3(0,.015f,-.18f),new Vector3(width,height,.16f),stationGold,.075f);
        StationBox("InsetEnamelFace",board,new Vector3(0,.015f,-.29f),new Vector3(width-.15f,height-.15f,.14f),dark?stationBlue:stationCream,.065f);
        // Full raised geometry with shallow extrusion: readable at the gameplay camera angle.
        StationText(text,board,new Vector3(0,.015f,-.405f),textHeight,dark?stationCream:stationInk,false,width-.66f);
        foreach(int side in new[]{-1,1})
        {
            StationSphere("BrassSignStud",board,new Vector3(side*(width*.5f-.15f),.015f,-.39f),Vector3.one*.14f,stationGold);
            StationBox("StudSlot",board,new Vector3(side*(width*.5f-.15f),.015f,-.462f),new Vector3(.065f,.014f,.008f),stationDark,.003f);
        }
    }
    static void BuildPolishedStationGateway(WorldStationRoute3D view,Transform ext)
    {
        var gate=Group("WorldTwoStationDoors",ext,new Vector3(14.25f,.16f,4.40f));gate.localScale=new Vector3(1.10f,1.10f,1);
        // Multiple solid contours, including a visible outer edge and a deep inner jamb.
        StationArchLayer(gate,"MidnightStructure",1.98f,.66f,1.0f,.08f,stationBlue);
        StationArchLayer(gate,"OuterBrassInlay",2.23f,.065f,.10f,-.45f,stationGold);
        StationArchLayer(gate,"CarvedMapleReveal",1.65f,.24f,.80f,-.10f,stationMaple);
        StationArchLayer(gate,"InnerTealJamb",1.48f,.14f,.40f,.02f,stationDoorInset);
        foreach(int side in new[]{-1,1})
        {
            StationBox("ColumnPedestal",gate,new Vector3(side*1.96f,.21f,-.015f),new Vector3(.96f,.42f,1.24f),stationBlue,.10f);
            StationBox("PedestalBrassCap",gate,new Vector3(side*1.96f,.43f,-.035f),new Vector3(.99f,.09f,1.27f),stationGold,.035f);
            StationBox("ColumnFrontInlay",gate,new Vector3(side*1.99f,1.75f,-.465f),new Vector3(.14f,2.43f,.07f),stationGold,.035f);
            StationFloorShade("ColumnContact",gate,new Vector3(side*1.96f,-.03f,0),1.5f,1.8f);
        }
        var firstLabel=Group("WorldOneLabel",gate);view.worldOneLabel=firstLabel.gameObject;
        var secondLabel=Group("WorldTwoLabel",gate);view.worldTwoLabel=secondLabel.gameObject;
        StationHeroPlaque(firstLabel,"WORLD 1",new Vector3(0,4.39f,-.65f),4.95f,1.12f,.58f,true);
        StationHeroPlaque(firstLabel,"30 LEVELS",new Vector3(0,3.46f,-.65f),3.9f,.70f,.30f,false);
        StationHeroPlaque(secondLabel,"WORLD 2",new Vector3(0,4.39f,-.65f),4.95f,1.12f,.58f,true);
        StationHeroPlaque(secondLabel,"20 LEVELS",new Vector3(0,3.46f,-.65f),3.9f,.70f,.30f,false);
        // A small brass crown detail finishes the outline without adding tiny copy.
        StationSphere("GoldenFinial",gate,new Vector3(0,5.19f,0),Vector3.one*.34f,stationGold);
        Disc("FinialBase",gate,new Vector3(0,5.02f,0),.22f,.14f,stationGold,.04f);

        // Receding structural ribs create real depth; the back is dark enough to read it.
        foreach(int side in new[]{-1,1})StationBox("DeepTunnelWall",gate,new Vector3(side*1.57f,1.40f,1.18f),new Vector3(.22f,2.8f,2.50f),stationDoorInset,.08f);
        MeshObject("VaultedTunnelRoof",gate,new Vector3(0,2.72f,1.15f),StationArch(1.53f,.18f,2.60f),stationDoorInset);
        StationBox("FarTunnelRecess",gate,new Vector3(0,1.52f,2.63f),new Vector3(3.05f,3.0f,.18f),stationInk,.14f);
        for(int i=0;i<3;i++)StationArchLayer(gate,"RecedingVault"+i,1.43f,.09f,.12f,.60f+i*.75f,stationMaple);
        var lighting=Group("WarmStationInterior",gate);view.gatewayGlow=lighting.gameObject;
        // The lit rear window sits behind the ribs; its trim gives the light a
        // physical source rather than filling the whole doorway with a flat glow.
        StationBox("RearWindowBrassFrame",lighting,new Vector3(0,.88f,2.15f),new Vector3(1.65f,1.15f,.16f),stationGold,.10f);
        StationBox("RearAmberWindow",lighting,new Vector3(0,.88f,2.045f),new Vector3(1.40f,.90f,.07f),stationGlow,.07f);
        StationBox("RearWindowMullion",lighting,new Vector3(0,.88f,1.985f),new Vector3(.075f,.93f,.07f),stationDoorInset,.025f);
        StationBox("RearWindowCrossbar",lighting,new Vector3(0,.88f,1.985f),new Vector3(1.43f,.075f,.07f),stationDoorInset,.025f);
        foreach(int side in new[]{-1,1})StationBox("WarmJambLight",lighting,new Vector3(side*1.39f,1.43f,.20f),new Vector3(.042f,2.62f,.035f),stationGlow,.015f);
        MeshObject("WarmArchLight",lighting,new Vector3(0,2.76f,.20f),StationArch(1.40f,.048f,.05f),stationGlow);
        for(int i=0;i<2;i++)
        {
            var lamp=Group("VaultPendant"+i,lighting,new Vector3(0,2.66f,.65f+i*1.12f));
            Disc("PendantBrassShade",lamp,Vector3.zero,.30f,.14f,stationGold,.05f);
            StationSphere("WarmPearlBulb",lamp,new Vector3(0,-.12f,0),new Vector3(.28f,.22f,.28f),stationGlow);
        }
        StationFloorShade("ThresholdWarmLight",lighting,new Vector3(0,.004f,.70f),3.1f,3.9f,stationWarmPool);
        foreach(int side in new[]{-1,1})
        {
            var door=Group(side<0?"LeftDoorHinge":"RightDoorHinge",gate,new Vector3(side*1.43f,.05f,-.20f));
            if(side<0)view.leftDoor=door;else view.rightDoor=door;
            float x=-side*.70f;
            StationBox("SolidDoor",door,new Vector3(x,1.35f,0),new Vector3(1.41f,2.7f,.30f),stationTeal,.10f);
            foreach(int face in new[]{-1,1})
            {
                // Both door faces have raised panels, so neither becomes a blank slab when open.
                StationBox("InsetPanelShadow",door,new Vector3(x,1.40f,face*.172f),new Vector3(1.11f,2.15f,.065f),stationDoorInset,.07f);
                foreach(float y in new[]{.64f,1.70f})
                {
                    StationBox("RaisedDoorPanel",door,new Vector3(x,y,face*.219f),new Vector3(.94f,y<1? .52f:1.12f,.09f),stationTeal,.065f);
                    StationBox("PanelTopBeading",door,new Vector3(x,y+(y<1?.23f:.53f),face*.269f),new Vector3(.85f,.035f,.03f),stationGold,.013f);
                }
                StationBox("DoorKickPlate",door,new Vector3(x,.23f,face*.184f),new Vector3(1.14f,.18f,.05f),stationGold,.023f);
                StationBox("HandleBackplate",door,new Vector3(-side*1.17f,1.18f,face*.212f),new Vector3(.14f,.45f,.06f),stationGold,.025f);
                WorkshopRod("BrassPullHandle",door,new Vector3(-side*1.17f,1.05f,face*.32f),new Vector3(-side*1.17f,1.31f,face*.32f),.047f,stationGold);
            }
            foreach(float y in new[]{.40f,1.35f,2.30f})
            {Disc("BrassHinge",door,new Vector3(0,y,0),.08f,.31f,stationGold,.025f);StationSphere("HingeCap",door,new Vector3(0,y+.17f,0),Vector3.one*.15f,stationGold);}
        }
        // Status is above the opening, never across the rails in front of the train.
        view.openSign=Group("UnlockedPlaque",gate).gameObject;
        view.lockedSign=Group("LockedPlaque",gate).gameObject;
        StationHeroPlaque(view.lockedSign.transform,"FINISH LEVEL 30",new Vector3(0,2.40f,-.67f),3.32f,.63f,.245f,false);
        var hit=gate.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,2.30f,-.52f);hit.size=new Vector3(5.1f,4.9f,2.1f);view.gateHit=hit;
        view.gateSelection=Group("StationSelectionRing",gate).gameObject;
        var ring=Ring("MintSelection",view.gateSelection.transform,new Vector3(0,.025f,-.3f),2.43f,.045f,glow);ring.transform.localScale=new Vector3(1,1,.52f);
        foreach(int side in new[]{-1,1})BuildStationLantern(gate,new Vector3(side*2.43f,2.0f,-.04f));
        var stars=new List<Transform>();
        for(int i=0;i<4;i++)
        {
            var star=Group("GoldenSparkle"+i,ext,new Vector3(11.9f+(i%2)*4.7f,4.6f+(i/2)*1.15f,4.1f));stars.Add(star);
            StationBox("SparkleVertical",star,Vector3.zero,new Vector3(.13f,1.15f,.13f),stationGlow,.06f);
            StationBox("SparkleHorizontal",star,Vector3.zero,new Vector3(.8f,.13f,.13f),stationGlow,.06f);
        }
        view.sparkles=stars.ToArray();
        BuildStationPlanter(ext,new Vector3(16.95f,.16f,1.85f));
        BuildStationPlanter(ext,new Vector3(11.43f,.16f,3.15f));
    }
    static void BuildStationLantern(Transform gate,Vector3 at)
    {
        var p=Group("BrassWallLantern",gate,at);
        StationBox("WallBracket",p,new Vector3(0,.30f,.05f),new Vector3(.16f,.17f,.64f),stationGold,.04f);
        StationBox("LanternRoof",p,new Vector3(0,.45f,-.25f),new Vector3(.58f,.14f,.54f),stationBlue,.06f);
        StationBox("LanternFoot",p,new Vector3(0,-.32f,-.25f),new Vector3(.49f,.13f,.45f),stationBlue,.05f);
        StationBox("AmberGlass",p,new Vector3(0,.05f,-.25f),new Vector3(.33f,.57f,.30f),stationGlow,.07f);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})WorkshopRod("LanternCage",p,new Vector3(x*.19f,-.26f,-.25f+z*.17f),new Vector3(x*.19f,.40f,-.25f+z*.17f),.025f,stationGold);
        StationSphere("LanternFinial",p,new Vector3(0,.57f,-.25f),Vector3.one*.16f,stationGold);
    }
    static void BuildStationPlanter(Transform ext,Vector3 at)
    {
        var p=Group("RoundStationPlanter",ext,at);
        StationFloorShade("PlanterContact",p,Vector3.up*.001f,1.2f,1.2f);
        Disc("BluePlanterPot",p,new Vector3(0,.22f,0),.40f,.40f,stationBlue,.08f);
        Ring("BrassPotRim",p,new Vector3(0,.44f,0),.42f,.05f,stationGold,.07f);
        Disc("DarkEarth",p,new Vector3(0,.425f,0),.34f,.025f,stationDark,.01f);
        StationSphere("CarvedTopiary",p,new Vector3(0,.91f,0),new Vector3(.85f,.96f,.85f),stationGreen);
        foreach(var off in new[]{new Vector3(-.25f,.17f,0),new Vector3(.19f,-.11f,-.21f),new Vector3(.17f,.10f,.20f)})
            StationSphere("TopiaryLobe",p,new Vector3(0,.9f,0)+off,Vector3.one*.52f,stationGreen);
    }
    static void PolishStationLocomotive(WorldStationRoute3D view)
    {
        var train=view.train;
        // Cream roof and cab trim make the little blue engine legible from the map camera.
        StationBox("IvoryLacqueredRoof",train,new Vector3(-1.68f,2.99f,0),new Vector3(2.14f,.22f,2.41f),stationCream,.105f);
        foreach(int side in new[]{-1,1})StationBox("RoofBrassEdge",train,new Vector3(-1.68f,2.92f,side*1.20f),new Vector3(2.08f,.065f,.065f),stationGold,.025f);
        StationFloorShade("SoftGroundContact",train,new Vector3(0,-.971f,0),6.3f,3.65f);
    }
}
#endif
