using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/12 - Generate Bright Playroom")]
    public static void GenerateBrightPlayroom()
    {
        GenerateToyWorkshop();
        var menu=UnityEngine.Object.FindAnyObjectByType<ToyBoxMenuController>();
        var room=menu.transform.Find("ToyRoomScenery");
        foreach(string removed in new[]{"PerforatedMaplePegboard","SideWorkBench","WoodenToyTrain","WorkshopStool","WallPosterFrame","CrateFront","WallShapeToys"})
            if(room.Find(removed)!=null) throw new Exception("Old decoration remains: "+removed);
        foreach(string prop in new[]{"WoodenWallClock","PlantAndRainbowShelf","BraidedOvalRug","SmallToyBasket","ThreeStackedPucks","LowBookCubby","LampSideStand","FiveWoodenStars"})
            if(room.Find(prop)==null) throw new Exception("Missing selected playroom prop: "+prop);
        foreach(float aspect in new[]{16f/9f,4f/3f,9f/16f})
        {
            menu.menuCamera.aspect=aspect;
            menu.FitHomeCamera(aspect);
            foreach(var point in menu.framingPoints)
            {
                var view=menu.menuCamera.WorldToViewportPoint(point);
                if(view.z<=0 || view.x<0 || view.x>1 || view.y<0 || view.y>1)
                    throw new Exception("Menu framing clips at aspect "+aspect+": "+view);
            }
            if(Mathf.Abs(menu.menuCamera.WorldToViewportPoint(menu.framingBounds.center).x-.5f)>.001f)
                throw new Exception("Menu board is not horizontally centered.");
        }
        menu.menuCamera.aspect=16f/9f;
        menu.FitHomeCamera(16f/9f);
        menu.GetComponentInChildren<ToyBoxSurfaceShine>(true).Sample(.8f);
        ToyBoxMenuValidation.RenderCamera(menu.menuCamera,"Bright-Playroom",1920,1080);
        Debug.Log("BRIGHT_PLAYROOM_PASSED: eight saved 3D prop groups; centered board and unclipped framing at 16:9, 4:3 and 9:16.");
    }

    public static void ValidateBrightPlayroomBatch()
    {
        GenerateBrightPlayroom();
        ToyBoxStartupValidation.RunBatch();
    }

    static void BuildBrightPlayroom(Transform room)
    {
        var oak=Material("BrightPlayroomOak",new Color(.77f,.57f,.35f),.34f,wood.GetTexture("_BaseMap") as Texture2D);
        oak.SetTexture("_BumpMap",wood.GetTexture("_BumpMap"));oak.SetFloat("_BumpScale",.15f);oak.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(oak);
        var plaster=Material("BrightPlayroomCreamWall",new Color(.97f,.92f,.81f),.15f);
        var panel=Material("BrightPlayroomBluePanel",new Color(.53f,.64f,.67f),.29f);
        var seams=Material("BrightPlayroomFloorSeams",new Color(.35f,.24f,.14f),.18f);
        var leaf=Material("BrightPlayroomLeaf",new Color(.16f,.39f,.16f),.52f);
        var pot=Material("BrightPlayroomCeramic",new Color(.93f,.89f,.76f),.57f);

        Box("FloorBase",room,new Vector3(0,-.26f,0),new Vector3(46,.3f,40),seams,.02f);
        for(int row=0;row<29;row++)for(int col=0;col<8;col++)
            Box("OakFloorboard",room,new Vector3(-21+col*6+(row%2)*3,-.095f,-17+row*1.25f),new Vector3(5.985f,.12f,1.235f),oak,.018f);
        Box("CreamWall",room,new Vector3(0,6,8.10f),new Vector3(46,14,.30f),plaster,.05f);
        Box("BlueWainscot",room,new Vector3(0,1.45f,7.89f),new Vector3(45,3.05f,.22f),panel,.04f);
        for(int i=-22;i<=22;i++) Box("PanelStile",room,new Vector3(i,1.45f,7.72f),new Vector3(.065f,2.90f,.10f),panel,.018f);
        Box("OakChairRail",room,new Vector3(0,3.05f,7.65f),new Vector3(45,.19f,.38f),oak,.045f);
        Box("OakSkirting",room,new Vector3(0,.10f,7.65f),new Vector3(45,.25f,.35f),oak,.04f);

        var shelf=Group("PlantAndRainbowShelf",room,new Vector3(-8.4f,4.3f,6.8f));
        Box("RoundedShelf",shelf,Vector3.zero,new Vector3(5.65f,.25f,1.9f),oak,.11f);
        for(int side=-1;side<=1;side+=2)
        {
            Box("WoodBracket",shelf,new Vector3(side*2.15f,-.28f,.60f),new Vector3(.25f,.57f,.25f),oak,.07f);
            WorkshopRod("ShelfBrace",shelf,new Vector3(side*2.15f,-.47f,.55f),new Vector3(side*2.15f,-.13f,-.48f),.09f,oak);
        }
        var plant=Group("SolidPottedPlant",shelf,new Vector3(-1.35f,.14f,-.15f));
        MeshObject("GlazedPot",plant,Vector3.zero,PlayroomPotMesh(),pot);
        Disc("Soil",plant,new Vector3(0,.74f,0),.41f,.025f,seams,.01f);
        WorkshopRod("MainStem",plant,new Vector3(0,.70f,0),new Vector3(.03f,1.82f,0),.035f,leaf);
        var leaves=PlayroomLeafMesh();
        for(int i=0;i<7;i++)
        {
            float side=i%2==0?-1:1,y=.95f+i*.13f;
            var end=new Vector3(side*(.42f+(i%3)*.08f),y+.18f,(i%3-1)*.14f);
            WorkshopRod("LeafStem",plant,new Vector3(0,y-.10f,0),end,.018f,leaf);
            var l=MeshObject("SolidLeaf",plant,end,leaves,leaf);
            l.transform.localRotation=Quaternion.Euler(i*7,side*15,side*24);
        }
        var colors=new[]{blue,mint,yellow,coral};
        for(int i=0;i<4;i++)
        {
            var arch=Ring("WoodenRainbowBand",shelf,new Vector3(1.05f,.14f,-.25f),.36f+i*.22f,.21f,colors[i],.34f,180);
            arch.transform.localRotation=Quaternion.Euler(-90,0,0);
        }

        var clock=Group("WoodenWallClock",room,new Vector3(8.35f,4.9f,7.50f));
        var rim=Disc("RoundOakCase",clock,Vector3.zero,1.68f,.28f,oak,.10f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
        var face=Disc("RecessedBlueDial",clock,new Vector3(0,0,-.18f),1.48f,.10f,blue,.035f);face.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI/6;
            var dot=Disc("RaisedHourMarker",clock,new Vector3(Mathf.Sin(angle)*1.22f,Mathf.Cos(angle)*1.22f,-.28f),.085f,.075f,cream,.03f);
            dot.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        WorkshopRod("HourHand",clock,new Vector3(0,0,-.33f),new Vector3(-.68f,.57f,-.33f),.09f,cream);
        WorkshopRod("MinuteHand",clock,new Vector3(0,0,-.36f),new Vector3(.71f,.81f,-.36f),.07f,cream);
        var hub=Disc("ClockHub",clock,new Vector3(0,0,-.40f),.15f,.10f,cream,.045f);hub.transform.localRotation=Quaternion.Euler(90,0,0);
        BuildCozyPlayroomDetails(room,oak);
        CombineWorkshopMeshes(room,"CozyPlayroomBatch_");
    }

    static Mesh PlayroomPotMesh()
    {
        var profile=new[]{new Vector2(0,0),new Vector2(.35f,0),new Vector2(.48f,.10f),new Vector2(.54f,.35f),new Vector2(.53f,.59f),new Vector2(.46f,.86f),new Vector2(.41f,.87f),new Vector2(.39f,.79f),new Vector2(.45f,.53f),new Vector2(.39f,.12f),new Vector2(0,.12f)};
        var v=new List<Vector3>();var t=new List<int>();const int n=64;
        for(int row=0;row<profile.Length;row++)for(int i=0;i<=n;i++)
        {
            float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x));
            if(row>0&&i<n){int p=row*(n+1)+i,b=p-n-1;t.AddRange(new[]{b,p,p+1,b,p+1,b+1});}
        }
        var mesh=new Mesh{name="BrightPlayroomPot"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Root+"/Meshes/BrightPlayroomPot.asset");
    }

    static Mesh PlayroomLeafMesh()
    {
        var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<int>();const int rows=16,cols=32;
        var size=new Vector3(.34f,.10f,.19f);
        for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
        {
            float a=y*Mathf.PI/rows,b=x*Mathf.PI*2/cols;
            Vector3 p=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b));
            v.Add(Vector3.Scale(p,size));n.Add(new Vector3(p.x/size.x,p.y/size.y,p.z/size.z).normalized);
            if(y<rows&&x<cols){int i=y*(cols+1)+x;t.AddRange(new[]{i,i+cols+2,i+cols+1,i,i+1,i+cols+2});}
        }
        var mesh=new Mesh{name="BrightPlayroomSolidLeaf"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetNormals(n);mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Root+"/Meshes/BrightPlayroomSolidLeaf.asset");
    }
}
