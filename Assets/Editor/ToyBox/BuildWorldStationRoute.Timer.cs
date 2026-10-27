#if UNITY_EDITOR
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    static void BuildSignalTimer(WorldStationRoute3D view, Transform bonus)
    {
        var root = Group("RailwaySignalTimer", bonus, new Vector3(-.3f,.78f,-.72f));
        root.localScale=new Vector3(1.25f,1f,.65f);
        root.localRotation=Quaternion.Euler(32,0,0);
        var timer = root.gameObject.AddComponent<StationSignalTimer3D>();
        view.signalTimer = timer;
        var navy = StationMaterial("SignalNavy", new Color(.035f,.065f,.10f), .65f);
        var cream = StationMaterial("SignalIvory", new Color(1f,.95f,.81f));
        StationBox("BrassHousing", root, new Vector3(0,.67f,0), new Vector3(7.4f,1.10f,.62f),stationGold,.12f);
        StationBox("EnamelHousing", root,new Vector3(0,.67f,-.075f),new Vector3(7.26f,.96f,.64f),navy,.10f);
        foreach(float x in new[]{-3.45f,0f,3.45f})
        {
            StationBox("SupportPost",root,new Vector3(x,-.4f,.20f),new Vector3(.19f,1f,.23f),stationGold,.035f);
            StationBox("Foot",root,new Vector3(x,.10f,0),new Vector3(.43f,.2f,.86f),navy,.08f);
            foreach(float y in new[]{.26f,1.08f})StationSphere("Rivet",root,new Vector3(x,y,-.41f),Vector3.one*.11f,stationGold);
        }
        timer.lenses=new Renderer[3];timer.lit=new Material[3];timer.unlit=new Material[3];
        var colors=new[]{new Color(.1f,1,.30f),new Color(1,.52f,.05f),new Color(1,.06f,.025f)};
        for(int i=0;i<3;i++)
        {
            float x=-2.9f+i*.91f;
            var rim=Disc("LensBrassRim",root,new Vector3(x,.68f,-.43f),.34f,.12f,stationGold,.035f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
            timer.unlit[i]=StationMaterial("SignalOff"+i,colors[i]*.18f,.45f);
            timer.lit[i]=StationMaterial("SignalOn"+i,colors[i],.4f);
            timer.lit[i].SetColor("_EmissionColor",colors[i]*3.5f);
            timer.lenses[i]=StationSphere("GlassLens"+i,root,new Vector3(x,.68f,-.53f),new Vector3(.59f,.59f,.23f),timer.unlit[i]).GetComponent<Renderer>();
        }
        StationBox("LabelBezel",root,new Vector3(.55f,.67f,-.42f),new Vector3(2.05f,.70f,.13f),stationGold,.06f);
        StationBox("CreamLabel",root,new Vector3(.55f,.67f,-.50f),new Vector3(1.94f,.59f,.06f),cream,.045f);
        SignalLetter("START IN",root,new Vector3(.55f,.67f,-.56f),.29f,navy,1.78f);
        timer.numerals=new GameObject[20];timer.tiles=new Transform[2];
        for(int d=0;d<2;d++)
        {
            float x=2.14f+d*.76f;
            StationBox("DigitSocket",root,new Vector3(x,.68f,-.43f),new Vector3(.76f,.87f,.13f),stationGold,.045f);
            var tile=Group("NumeralTile"+d,root,new Vector3(x,.68f,-.53f));timer.tiles[d]=tile;
            StationBox("IvoryTile",tile,Vector3.zero,new Vector3(.65f,.77f,.09f),cream,.035f);
            for(int n=0;n<10;n++)
            {
                SignalLetter(n.ToString(),tile,new Vector3(0,0,-.07f),.65f,navy,.53f);
                timer.numerals[d*10+n]=tile.Find(n.ToString()).gameObject;
            }
            foreach(int side in new[]{-1,1})StationSphere("TileHinge",root,new Vector3(x+side*.34f,.68f,-.59f),Vector3.one*.075f,stationGold);
        }
        timer.Display(30);
    }
    static void SignalLetter(string text,Transform parent,Vector3 at,float height,Material mat,float width)
    {
        var letters=Text(text,parent,at,1,mat,false,.07f,.008f,.006f);
        var bounds=letters.GetComponent<MeshFilter>().sharedMesh.bounds;
        letters.transform.localScale=Vector3.one*Mathf.Min(height/bounds.size.y,width/bounds.size.x);
        var t=parent.Find(text);var b=t.GetComponent<MeshFilter>().sharedMesh.bounds;
        t.localPosition-=new Vector3(b.center.x*t.localScale.x,b.center.y*t.localScale.y,0);
    }
}
#endif
