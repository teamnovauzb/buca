#if UNITY_EDITOR
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
    static void BuildCozyWindowCorner(Transform room)
    {
        var linen=StationMaterial("CozyLinen",new Color(.83f,.75f,.59f));
        var thread=StationMaterial("CozyThread",new Color(.94f,.87f,.71f));
        var curtain=StationMaterial("CozyBlueCurtain",new Color(.46f,.60f,.70f));
        var sky=BuildWindowLandscape();
        var rugLinen=StationMaterial("SageRugLinen",new Color(.40f,.49f,.33f));
        var rugThread=StationMaterial("SageRugThread",new Color(.55f,.63f,.46f));
        var rug=Group("WovenCreamRug",room,new Vector3(3,-.72f,0));
        StationBox("LinenBacking",rug,Vector3.zero,new Vector3(31,.20f,20.5f),rugLinen,.16f);
        // Actual raised weave catches the light around the board's perimeter.
        for(int z=0;z<103;z++)
            StationBox("WovenRow",rug,new Vector3(0,.12f,-10.1f+z*.2f),new Vector3(30.7f,.04f,.14f),rugThread,.028f);
        for(int x=0;x<154;x++)
            StationBox("WovenWarp",rug,new Vector3(-15.3f+x*.2f,.13f,0),new Vector3(.12f,.035f,20.2f),rugLinen,.028f);
        foreach(int side in new[]{-1,1})for(int i=0;i<100;i++)
            StationBox("SoftRugFringe",rug,new Vector3(side*15.6f,.03f,-10+i*.2f),new Vector3(.48f,.12f,.10f),rugThread,.045f);

        var window=Group("SunlitWindow",room,new Vector3(1.2f,5.65f,14.45f));
        StationBox("DeepWindowFrame",window,Vector3.zero,new Vector3(7.2f,6.1f,.45f),stationMaple,.12f);
        var vista=new Mesh {name="WindowVistaPlane"};
        vista.vertices=new[]{new Vector3(-3.325f,-2.775f,0),new Vector3(-3.325f,2.775f,0),new Vector3(3.325f,2.775f,0),new Vector3(3.325f,-2.775f,0)};
        vista.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};vista.triangles=new[]{0,1,2,0,2,3};vista.RecalculateNormals();vista.RecalculateBounds();
        MeshObject("GardenView",window,new Vector3(0,0,-.29f),vista,sky);
        foreach(float x in new[]{-3.25f,0,3.25f})StationBox("WindowMullion",window,new Vector3(x,0,-.42f),new Vector3(.17f,5.8f,.25f),stationCream,.035f);
        foreach(float y in new[]{-2.8f,0,2.8f})StationBox("WindowCrossbar",window,new Vector3(0,y,-.42f),new Vector3(6.7f,.18f,.25f),stationCream,.035f);
        StationBox("DeepCreamSill",window,new Vector3(0,-3.02f,-.60f),new Vector3(7.7f,.26f,1.0f),stationCream,.07f);
        WorkshopRod("CurtainRail",window,new Vector3(-4.6f,3.3f,-.65f),new Vector3(4.6f,3.3f,-.65f),.07f,stationGold);
        foreach(int side in new[]{-1,1})
        {
            var cloth=Group("BlueCurtain",window,new Vector3(side*3.65f,0,-.75f));
            for(int i=0;i<7;i++)
            {
                var fold=StationBox("SoftFabricFold",cloth,new Vector3((i-3)*.20f,-.15f,Mathf.Sin(i*2)*.08f),new Vector3(.29f,6.25f,.26f),curtain,.12f);
                fold.transform.localRotation=Quaternion.Euler(0,0,side*(i-3)*1.1f);
            }
            StationBox("GoldTieback",cloth,new Vector3(0,-.65f,-.19f),new Vector3(1.45f,.13f,.18f),stationGold,.06f);
            StationSphere("RailFinial",window,new Vector3(side*4.65f,3.3f,-.65f),Vector3.one*.25f,stationGold);
        }
        var glow=Group("WindowWarmFill",window,new Vector3(0,1,-1.3f)).gameObject.AddComponent<Light>();
        glow.type=LightType.Point;glow.color=new Color(1,.80f,.49f);glow.intensity=.45f;glow.range=15;glow.shadows=LightShadows.None;

        var plant=Group("LargeRoomPlant",room,new Vector3(-15.7f,-.85f,6.1f));
        StationSphere("CreamCeramicPot",plant,new Vector3(0,1.0f,0),new Vector3(2.55f,2.2f,2.55f),stationCream);
        Disc("PotRim",plant,new Vector3(0,1.9f,0),1.13f,.20f,stationMaple,.07f);
        Disc("Soil",plant,new Vector3(0,2.01f,0),1.0f,.04f,stationDark,.01f);
        var leafMat=StationMaterial("CozyLeafGreen",new Color(.19f,.34f,.085f));
        for(int i=0;i<15;i++)
        {
            float a=i*2.4f;var end=new Vector3(Mathf.Sin(a)*(1.2f+i%3*.30f),2.7f+i%5*.52f,Mathf.Cos(a)*(1.0f+i%3*.25f));
            WorkshopRod("LeafStem",plant,new Vector3(0,1.9f,0),end,.045f,leafMat);
            var leaf=StationSphere("SculptedLeaf",plant,end,new Vector3(.75f,1.6f,.18f),leafMat);
            leaf.transform.localRotation=Quaternion.Euler(25,i*137,Mathf.Sin(a)*48);
        }
        var pouf=Group("KnittedCreamPouf",room,new Vector3(21,-.85f,2.3f));
        StationSphere("SoftPouf",pouf,new Vector3(0,1.05f,0),new Vector3(3.7f,2.25f,3.7f),linen);
        for(int ring=0;ring<6;ring++)for(int i=0;i<32;i++)
        {
            float a=i*Mathf.PI*2/32;float r=.12f+ring*.19f;float y=1.05f+1.125f*Mathf.Sqrt(1-r*r/(1.85f*1.85f));
            var stitch=StationSphere("TopKnit",pouf,new Vector3(Mathf.Sin(a)*r,y,Mathf.Cos(a)*r),new Vector3(.16f,.12f,.25f),thread);
            stitch.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
        }
        for(int row=0;row<12;row++)
        {
            float y=.15f+row*.17f;float radius=1.84f*Mathf.Sqrt(Mathf.Max(.08f,1-Mathf.Pow((y-1.1f)/1.1f,2)));
            for(int i=0;i<52;i++)
            {
                float a=i*Mathf.PI*2/52+row*.035f;
                var stitch=StationSphere("KnitStitch",pouf,new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius),new Vector3(.16f,.26f,.13f),thread);
                stitch.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,(i%2==0?1:-1)*25);
            }
        }
    }
}
#endif
