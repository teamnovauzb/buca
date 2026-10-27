#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public static partial class BuildToyBoxMainMenu
{
    static void BuildWorldWelcome(WorldStationRoute3D view)
    {
        var old=view.viewCamera.transform.Find("World2Welcome");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var go=Object.Instantiate(view.victory.gameObject,view.viewCamera.transform,false);go.name="World2Welcome";
        var v=go.GetComponent<WorldVictoryCelebration3D>();view.welcome=v;v.arrivalTicket=true;
        var p=v.presentation;for(int i=p.childCount-1;i>=0;i--)Object.DestroyImmediate(p.GetChild(i).gameObject);
        v.rays=new Transform[0];
        var gold=StationMaterial("WelcomeTicketGold",new Color(1,.69f,.22f),.65f);gold.SetFloat("_Smoothness",.42f);
        var navy=StationMaterial("WelcomeTicketNavy",new Color(.02f,.065f,.15f),.3f);
        var cream=StationMaterial("WelcomeTicketCream",new Color(1,.91f,.70f));
        var mesh=WelcomeTicketMesh();
        MeshObject("TicketGoldEdge",p,Vector3.zero,mesh,gold);
        var face=MeshObject("TicketNavyFace",p,new Vector3(0,0,-.12f),mesh,navy);face.transform.localScale=new Vector3(.97f,.94f,.8f);
        StationBox("WelcomeCreamPanel",p,new Vector3(0,.70f,-.24f),new Vector3(5.5f,.65f,.09f),cream,.07f);
        SignalLetter("WELCOME TO",p,new Vector3(0,.70f,-.31f),.40f,navy,5.0f);
        SignalLetter("WORLD 2",p,new Vector3(0,-.12f,-.29f),.90f,gold,5.2f);
        StationBox("CityCreamPanel",p,new Vector3(0,-.91f,-.24f),new Vector3(5.5f,.51f,.09f),cream,.07f);
        SignalLetter("NIGHT CITY",p,new Vector3(0,-.91f,-.31f),.31f,navy,4.5f);
        var stars=VictoryStarMesh();v.stars=new Transform[4];
        for(int i=0;i<4;i++)
        {v.stars[i]=Group("TicketStar"+i,p,new Vector3(i%2==0?-3.02f:3.02f,i<2?.92f:-.92f,-.28f));var star=MeshObject("GoldCornerStar",v.stars[i],Vector3.zero,stars,gold);star.transform.localScale=Vector3.one*.18f;}
        var medallion=Disc("LocomotiveSeal",p,new Vector3(0,1.35f,.02f),.65f,.15f,gold,.06f);medallion.transform.localRotation=Quaternion.Euler(90,0,0);
        var inset=Disc("SealNavy",p,new Vector3(0,1.35f,-.09f),.57f,.07f,navy,.04f);inset.transform.localRotation=Quaternion.Euler(90,0,0);
        var train=Group("RaisedLocomotiveEmblem",p,new Vector3(0,1.34f,-.21f));
        StationBox("Boiler",train,new Vector3(.08f,0,0),new Vector3(.58f,.20f,.10f),gold,.07f);
        StationBox("Cab",train,new Vector3(-.30f,.08f,0),new Vector3(.26f,.34f,.10f),gold,.035f);
        StationBox("CabWindow",train,new Vector3(-.30f,.13f,-.07f),new Vector3(.13f,.13f,.025f),navy,.02f);
        StationBox("Chimney",train,new Vector3(.25f,.20f,0),new Vector3(.10f,.23f,.10f),gold,.02f);
        foreach(float x in new[]{-.30f,0f,.26f}){var wheel=Disc("EmblemWheel",train,new Vector3(x,-.18f,0),.10f,.1f,gold,.015f);wheel.transform.localRotation=Quaternion.Euler(90,0,0);}
        for(int i=0;i<3;i++)StationSphere("EmblemSteam",train,new Vector3(.18f-i*.12f,.38f+i*.035f,0),new Vector3(.17f,.13f,.07f),cream);
        foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>(true)){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
        v.celebrationSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/World2Welcome.wav");
        v.cheeringSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/HappyYeah.wav");
        go.SetActive(false);
    }
    static Mesh WelcomeTicketMesh()
    {
        var outline=new List<Vector3>{new Vector3(-3.5f,-1.3f,0),new Vector3(3.5f,-1.3f,0),new Vector3(3.5f,-.45f,0)};
        for(int i=1;i<=12;i++){float a=-Mathf.PI*.5f-i*Mathf.PI/12;outline.Add(new Vector3(3.5f+Mathf.Cos(a)*.43f,Mathf.Sin(a)*.45f,0));}
        outline.Add(new Vector3(3.5f,1.3f,0));outline.Add(new Vector3(-3.5f,1.3f,0));outline.Add(new Vector3(-3.5f,.45f,0));
        for(int i=1;i<=12;i++){float a=Mathf.PI*.5f-i*Mathf.PI/12;outline.Add(new Vector3(-3.5f+Mathf.Cos(a)*.43f,Mathf.Sin(a)*.45f,0));}
        int n=outline.Count;var vertices=new Vector3[n*2+2];var indices=new List<int>();
        for(int i=0;i<n;i++){vertices[i]=outline[i]+Vector3.back*.08f;vertices[n+i]=outline[i]+Vector3.forward*.08f;}
        vertices[2*n]=Vector3.back*.08f;vertices[2*n+1]=Vector3.forward*.08f;
        for(int i=0;i<n;i++){int j=(i+1)%n;indices.AddRange(new[]{2*n,j,i,2*n+1,n+i,n+j,i,j,n+i,j,n+j,n+i});}
        var mesh=new Mesh{name="NotchedArrivalTicket"};mesh.vertices=vertices;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return ToyBoxGeometry.Save(mesh,StationFolder+"/NotchedArrivalTicket.asset");
    }
}
#endif
