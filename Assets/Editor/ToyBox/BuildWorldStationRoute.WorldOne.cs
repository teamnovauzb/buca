#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
    static void BuildOriginalWorldOneRoom(Transform map)
    {
        var room=Group("WorldOneRoom",map);
        // Background is modeled at full depth, behind the playable tabletop.
        var wall=map.Find("QuietWall");if(wall!=null)wall.localPosition=new Vector3(0,6,14.5f);
        StationBox("Bookshelf",room,new Vector3(-10.0f,2.3f,11.8f),new Vector3(11,.35f,2.6f),stationMaple,.14f);
        foreach(float x in new[]{-15.1f,-4.9f})StationBox("ShelfLeg",room,new Vector3(x,.5f,12.3f),new Vector3(.4f,3.6f,1.5f),stationMaple,.12f);
        foreach(int i in Enumerable.Range(0,5))
        {
            var book=Group("StoryBook",room,new Vector3(-10.3f+i*.67f,3.9f,11.6f));var mat=new[]{stationBlue,stationCream,stationTeal,stationGold,coral}[i];
            StationBox("BookCover",book,Vector3.zero,new Vector3(.55f,2.8f+i%2*.5f,1.1f),mat,.05f);
            foreach(float y in new[]{-1.05f,1.05f})StationBox("SpineStripe",book,new Vector3(0,y,-.57f),new Vector3(.41f,.08f,.04f),stationCream,.015f);
        }
        var fur=StationMaterial("TeddyHoney",new Color(.48f,.27f,.105f),0,.28f);
        var bear=Group("ShelfTeddyBear",room,new Vector3(-13.4f,2.55f,11.45f));
        StationSphere("TeddyBody",bear,new Vector3(0,.73f,0),new Vector3(1.65f,1.9f,1.15f),fur);
        StationSphere("TeddyHead",bear,new Vector3(0,2.13f,0),new Vector3(1.57f,1.53f,1.18f),fur);
        foreach(int side in new[]{-1,1})
        {
            StationSphere("TeddyEar",bear,new Vector3(side*.70f,2.69f,0),Vector3.one*.56f,fur);
            StationSphere("TeddyFoot",bear,new Vector3(side*.59f,.10f,-.45f),new Vector3(.74f,.63f,.95f),fur);
            StationSphere("TeddyArm",bear,new Vector3(side*.89f,.95f,0),new Vector3(.54f,1.05f,.56f),fur);
            StationSphere("TeddyEye",bear,new Vector3(side*.29f,2.31f,-.57f),Vector3.one*.105f,stationDark);
            var bow=StationSphere("BlueBow",bear,new Vector3(side*.22f,1.47f,-.6f),new Vector3(.49f,.36f,.16f),stationBlue);bow.transform.localRotation=Quaternion.Euler(0,0,side*20);
        }
        StationSphere("Muzzle",bear,new Vector3(0,1.99f,-.57f),new Vector3(.70f,.51f,.34f),stationMaple);
        StationSphere("Nose",bear,new Vector3(0,2.13f,-.75f),new Vector3(.22f,.15f,.10f),stationDark);
        var plant=Group("ShelfPlant",room,new Vector3(-6.0f,2.5f,11.65f));
        Disc("PlantPot",plant,new Vector3(0,.46f,0),.59f,.90f,stationCream,.08f);
        foreach(int i in Enumerable.Range(0,7))
        {
            float a=i*2.4f;Vector3 p=new Vector3(Mathf.Sin(a)*.68f,1.2f+i*.12f,Mathf.Cos(a)*.37f);
            WorkshopRod("PlantStem",plant,new Vector3(0,.8f,0),p,.035f,stationGreen);
            var leaf=StationSphere("Leaf",plant,p,new Vector3(.60f,.86f,.12f),stationGreen);leaf.transform.localRotation=Quaternion.Euler(15,i*34,-30+i*11);
        }
        var poster=Group("SmallStepsFramedPrint",room,new Vector3(10.2f,5.35f,14.15f));
        StationBox("MapleFrame",poster,Vector3.zero,new Vector3(3.4f,4.7f,.26f),stationMaple,.10f);
        StationBox("CreamPrint",poster,new Vector3(0,0,-.18f),new Vector3(2.96f,4.25f,.10f),stationCream,.05f);
        string[] lines={"SMALL","STEPS","BIG","WORLDS"};for(int i=0;i<4;i++)StationText(lines[i],poster,new Vector3(0,1.25f-i*.73f,-.26f),.40f,stationBlue);
        var lamp=Group("WarmRoomLamp",room,new Vector3(19.4f,-.75f,10.6f));
        Disc("LampBase",lamp,new Vector3(0,.14f,0),1.2f,.30f,stationMaple,.09f);Disc("LampStem",lamp,new Vector3(0,3.5f,0),.17f,6.7f,stationMaple,.045f);
        Disc("PleatedLampShade",lamp,new Vector3(0,6.7f,0),1.45f,2.2f,stationCream,.08f);
        for(int i=0;i<36;i++){float a=i*Mathf.PI*2/36;WorkshopRod("LinenPleat",lamp,new Vector3(Mathf.Cos(a)*1.46f,5.66f,Mathf.Sin(a)*1.46f),new Vector3(Mathf.Cos(a)*1.46f,7.75f,Mathf.Sin(a)*1.46f),.025f,stationMaple);}
        Disc("WarmShadeOpening",lamp,new Vector3(0,5.60f,0),1.30f,.035f,stationGlow,.015f);
        var wallPaint=StationMaterial("WorldOneWarmWall",new Color(.56f,.53f,.45f),0,0);
        StationBox("RoomWall",room,new Vector3(0,7,15),new Vector3(100,25,.4f),wallPaint,.04f);
        StationBox("RoomWoodFloor",room,new Vector3(0,-.96f,0),new Vector3(140,.2f,120),stationMaple,.04f);
        var light=Group("WorldOneWarmKey",room).gameObject.AddComponent<Light>();
        light.type=LightType.Directional;light.color=new Color(1,.91f,.76f);light.intensity=1.1f;light.shadows=LightShadows.Soft;light.transform.localRotation=Quaternion.Euler(48,-35,0);

        BuildCozyWindowCorner(room);
        BatchStation(room);
    }
}
#endif
