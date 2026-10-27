using UnityEngine;
using UnityEditor;

public static partial class BuildToyBoxMainMenu
{
    static void AddEveningSolidDetails(Transform env,Material navy,Material gold,Material ivory,Material leather)
    {
        DarkenEveningClock(env);
        var ink=StationMaterial("EveningPlaqueInk",new Color(.018f,.038f,.07f));
        var lettering=StationMaterial("EveningWarmLettering",new Color(.96f,.89f,.72f));
        var green=StationMaterial("EveningControlGreen",new Color(.055f,.42f,.12f));
        var porcelain=StationMaterial("EveningPlaquePorcelain",new Color(.86f,.77f,.58f));
        porcelain.SetFloat("_Smoothness",.32f);
        // Raised enamel plates: readable text and a beveled perimeter that catches light.
        for(int i=0;i<3;i++)
        {
            var plate=Group("EveningControlPlaque",env,new Vector3((i-1)*2.3f,1.08f,-8.68f));
            plate.localRotation=Quaternion.Euler(65,0,0);
            Box("CastBrassBacking",plate,Vector3.zero,new Vector3(2.16f,.64f,.10f),gold,.045f);
            Box("IvoryEnamelFace",plate,new Vector3(0,0,-.063f),new Vector3(2.04f,.52f,.055f),porcelain,.024f);
            foreach(int side in new[]{-1,1})StationSphere("PlaqueScrew",plate,new Vector3(side*.96f,.22f,-.10f),new Vector3(.055f,.055f,.035f),gold);
            float textX=i==0?0:.20f;
            StationText(i==0?"JOYSTICK":i==1?"SHOOT":"UNDO",plate,new Vector3(textX,.105f,-.10f),.185f,ink,false,i==0?1.8f:1.4f);
            StationText(i==0?"PULL TO AIM":i==1?"HOLD THEN RELEASE":"LAST SHOT",plate,new Vector3(textX,-.13f,-.10f),.105f,ink,false,i==0?1.8f:1.4f);
            if(i>0)
            {
                var badge=Disc("ButtonNumberBezel",plate,new Vector3(-.76f,0,-.105f),.21f,.035f,gold,.012f);badge.transform.localRotation=Quaternion.Euler(90,0,0);
                var face=Disc("ButtonNumberFace",plate,new Vector3(-.76f,0,-.135f),.175f,.025f,i==1?ink:green,.010f);face.transform.localRotation=Quaternion.Euler(90,0,0);
                StationText(i==1?"1":"3",plate,new Vector3(-.76f,0,-.16f),.25f,lettering,false,.28f);
            }
        }
        foreach(int side in new[]{-1,1})
        {
            Box("FasciaCornerArmor",env,new Vector3(side*4.89f,-.52f,-8.46f),new Vector3(.28f,.78f,.10f),gold,.045f);
            foreach(float y in new[]{-.24f,-.79f})StationSphere("FasciaRaisedBolt",env,new Vector3(side*4.89f,y,-8.54f),new Vector3(.105f,.105f,.065f),gold);
        }
        // Give the carriage a readable roof, window recesses, doors and undercarriage.
        var train=env.Find("PassengerTrain");
        var windowTint=StationMaterial("EveningTrainWindowShade",new Color(.44f,.26f,.12f));
        foreach(Transform item in train)
            if(item.name=="WarmCarriageWindow")
            {
                Box("WindowUpperShade",item,new Vector3(0,.32f,-.04f),new Vector3(.84f,.16f,.025f),windowTint,.01f);
                Box("WindowCentralMullion",item,new Vector3(0,0,-.07f),new Vector3(.045f,.9f,.055f),gold,.012f);
            }
        for(int i=-11;i<=11;i+=2)Box("RoofSeam",train,new Vector3(i,1.37f,0),new Vector3(.035f,.045f,2.16f),gold,.014f);
        foreach(int side in new[]{-1,1})
        {
            Box("TrainDoorSurround",train,new Vector3(side*11.25f,0,-1.18f),new Vector3(.95f,1.85f,.10f),gold,.06f);
            Box("RecessedTrainDoor",train,new Vector3(side*11.25f,0,-1.25f),new Vector3(.80f,1.68f,.08f),navy,.04f);
            Box("DoorHandle",train,new Vector3(side*11.48f,-.18f,-1.33f),new Vector3(.045f,.25f,.06f),gold,.015f);
        }
        // Turn the simple slatted props into solid benches with armrests and feet.
        foreach(Transform bench in env)
        {
            if(bench.name!="StationBench")continue;
            foreach(int side in new[]{-1,1})
            {
                Box("BenchArm",bench,new Vector3(side*1.60f,1.56f,.04f),new Vector3(.18f,.16f,1.23f),navy,.075f);
                foreach(float z in new[]{-.40f,.5f})
                {
                    Box("ArmSupport",bench,new Vector3(side*1.60f,1.26f,z),new Vector3(.12f,.52f,.12f),navy,.04f);
                    Box("BenchShoe",bench,new Vector3(side*1.40f,.065f,z),new Vector3(.38f,.13f,.35f),navy,.045f);
                }
            }
        }
        // Rich green foliage and fluted station pots, instead of the pale room pots.
        var leaves=StationMaterial("EveningFoliage",new Color(.075f,.23f,.11f));
        var leafTips=StationMaterial("EveningFoliageTips",new Color(.18f,.34f,.13f));
        foreach(Transform plant in env)
        {
            if(plant.name!="StationPlanter")continue;
            plant.localPosition=new Vector3(Mathf.Sign(plant.localPosition.x)*9.6f,-3.14f,-5.25f);plant.localScale=Vector3.one*1.25f;
            foreach(var r in plant.GetComponentsInChildren<MeshRenderer>())
            {
                if(r.name=="SolidLeaf")r.sharedMaterial=r.transform.GetSiblingIndex()%2==0?leaves:leafTips;
                else if(r.name.Contains("Dot"))r.enabled=false;
                else if(!r.name.Contains("Soil")&&!r.name.Contains("Stem"))r.sharedMaterial=navy;
            }
            Ring("RaisedPlanterRim",plant,new Vector3(0,.92f,0),.58f,.035f,gold,.015f);
            Ring("RaisedPlanterFoot",plant,new Vector3(0,.08f,0),.57f,.035f,gold,.015f);
            for(int i=0;i<24;i++){float a=i*Mathf.PI/12;Disc("PlanterFluting",plant,new Vector3(Mathf.Sin(a)*.597f,.48f,Mathf.Cos(a)*.597f),.016f,.72f,navy,.008f);}
        }
        // Suitcase latches, a recessed handle and a raised luggage tag create near-field depth.
        foreach(Transform bag in env)
        {
            if(bag.name!="LeatherSuitcase")continue;
            foreach(int side in new[]{-1,1})Box("CaseLatch",bag,new Vector3(side*.60f,.08f,-.585f),new Vector3(.18f,.18f,.07f),gold,.03f);
            var handle=Ring("LeatherCarryHandle",bag,new Vector3(0,.40f,0),.23f,.045f,leather,.02f);handle.transform.localRotation=Quaternion.Euler(90,0,0);handle.transform.localScale=new Vector3(1.5f,1,1);
        }
    }

    static void DarkenEveningClock(Transform env)
    {
        var clock=env.Find("StationClock");
        if(clock==null)return;
        var dial=StationMaterial("EveningClockWarmDial",new Color(.61f,.54f,.41f));
        dial.SetFloat("_Metallic",0);dial.SetFloat("_Smoothness",.23f);
        var bezel=StationMaterial("EveningClockAntiqueBrass",new Color(.79f,.58f,.30f));
        bezel.SetFloat("_Metallic",.4f);bezel.SetFloat("_Smoothness",.36f);
        var marks=StationMaterial("EveningClockNavyMarks",new Color(.075f,.11f,.16f));
        marks.SetFloat("_Metallic",0);marks.SetFloat("_Smoothness",.30f);
        foreach(var r in clock.GetComponentsInChildren<MeshRenderer>())
        {
            if(r.name=="ClockFace")r.sharedMaterial=dial;
            else if(r.name=="ClockBezel")r.sharedMaterial=bezel;
            else if(r.name=="HourMark"||r.name=="MinuteHand"||r.name=="HourHand")r.sharedMaterial=marks;
        }
        var column=env.Find("ClockColumn");if(column!=null)column.GetComponent<MeshRenderer>().sharedMaterial=bezel;
        foreach(var material in new[]{dial,bezel,marks})EditorUtility.SetDirty(material);
    }
}
