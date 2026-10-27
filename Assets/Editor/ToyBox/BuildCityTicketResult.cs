using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    public static string BuildCityTicketResult()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        const string path="Assets/ToyBoxMenu/Prefabs/ResultsAndLeaderboard.prefab";
        Directory.CreateDirectory("output/city-ticket");
        if(!File.Exists("output/city-ticket/before.prefab"))File.Copy(path,"output/city-ticket/before.prefab");
        PrepareFolders();ToyBoxGeometry.Initialize();MakeMaterials();
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var results=root.GetComponent<ToyBoxResults3D>();
            if(results.worldOneWellDone==null)results.worldOneWellDone=results.wellDone;
            if(results.worldTwoWellDone!=null)UnityEngine.Object.DestroyImmediate(results.worldTwoWellDone.gameObject);
            var city=UnityEngine.Object.Instantiate(results.worldOneWellDone,results.worldOneWellDone.transform.parent);
            city.name="World2FirstClassTicket";results.worldTwoWellDone=city;results.wellDone=results.worldOneWellDone;
            var emerald=NextMaterial("TicketEmerald",new Color(.014f,.105f,.070f),.22f,.38f);
            var edge=NextMaterial("TicketEdge",new Color(.023f,.22f,.15f),.28f,.42f);
            var brass=NextMaterial("TicketBrass",new Color(.70f,.43f,.12f),.63f,.43f);
            var cream=NextMaterial("TicketIvory",new Color(.93f,.84f,.62f),.06f,.30f);
            foreach(var r in city.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n=r.name;
                if(n.Contains("Letters")||n.StartsWith("Digit")||n.StartsWith("SavedGlyph")||n=="NEXT LEVEL"||n=="1"||n.StartsWith("CChevron"))r.sharedMaterial=cream;
                else if(n.Contains("Pin")||n.Contains("Brass")||n.Contains("Gold")||n=="TealRibbonBody"||n=="TealTailRim"||n=="SubtitleWoodRim"||n=="CMapleSocket")r.sharedMaterial=brass;
                else if(n=="SubtitleCreamFace")r.sharedMaterial=cream;
                else if(n.Contains("Highlight")||n.Contains("Shadow"))r.sharedMaterial=edge;
                else r.sharedMaterial=emerald;
            }
            var subtitle=city.banner.Find("LevelCompleteLetters");subtitle.GetComponent<MeshRenderer>().sharedMaterial=emerald;
            // Gold pinstripe just behind the emerald ribbon face.
            var ribbon=city.banner.Find("MapleRibbonFace");
            var stripe=UnityEngine.Object.Instantiate(ribbon.gameObject,ribbon.parent);stripe.name="TicketRibbonPinstripe";
            stripe.transform.localPosition=ribbon.localPosition+Vector3.forward*.06f;stripe.transform.localScale=new Vector3(1.018f,1.08f,1);stripe.GetComponent<MeshRenderer>().sharedMaterial=brass;
            foreach(var child in city.scorePlaque.Cast<Transform>().ToArray())
                if(child.name!="SavedScoreDigits" && child.name!="TotalLetters")UnityEngine.Object.DestroyImmediate(child.gameObject);
            TicketLayer(city.scorePlaque,"TicketGoldBacking",5.5f,2.5f,.23f,0,brass);
            TicketLayer(city.scorePlaque,"TicketEmeraldBody",5.35f,2.35f,.20f,-.15f,emerald);
            TicketLayer(city.scorePlaque,"TicketFineGoldLine",5.12f,2.12f,.06f,-.28f,brass);
            TicketLayer(city.scorePlaque,"TicketInsetFace",5.05f,2.05f,.06f,-.33f,emerald);
            city.scorePlaque.Find("TotalLetters").GetComponent<MeshRenderer>().sharedMaterial=cream;
            foreach(int side in new[]{-1,1})foreach(float y in new[]{-.87f,.87f})
            {
                var pin=Disc("TicketBrassRivet",city.scorePlaque,new Vector3(side*2.22f,y,-.40f),.065f,.06f,brass,.018f);pin.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            // Small raised locomotive crest, built as real saved mesh parts.
            var crest=Group("StationTrainCrest",city.banner,new Vector3(0,1.94f,-.38f));
            var rim=Disc("CrestBrass",crest,Vector3.zero,.48f,.12f,brass,.04f);rim.transform.localRotation=Quaternion.Euler(90,0,0);
            var face=Disc("CrestEmerald",crest,new Vector3(0,0,-.09f),.413f,.10f,emerald,.035f);face.transform.localRotation=Quaternion.Euler(90,0,0);
            PodiumSlab("EngineCab",crest,new Vector3(0,.10f,-.18f),.31f,.30f,.055f,.07f,cream);
            PodiumSlab("EngineWindow",crest,new Vector3(0,.15f,-.22f),.17f,.11f,.03f,.025f,emerald);
            PodiumSlab("EngineRoof",crest,new Vector3(0,.28f,-.20f),.39f,.065f,.06f,.025f,brass);
            var boiler=Disc("Boiler",crest,new Vector3(0,-.08f,-.22f),.13f,.05f,brass,.014f);boiler.transform.localRotation=Quaternion.Euler(90,0,0);
            foreach(int side in new[]{-1,1})
            {
                var wheel=Disc("Wheel",crest,new Vector3(side*.13f,-.24f,-.20f),.055f,.05f,cream,.012f);wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                var rail=PodiumSlab("Rail",crest,new Vector3(side*.18f,-.29f,-.18f),.04f,.16f,.04f,.015f,brass);rail.transform.localRotation=Quaternion.Euler(0,0,side*23);
            }
            var pieces=new List<Transform>(city.confetti);
            int originalCount=pieces.Count;
            for(int i=originalCount;i<120;i++)
            {
                var piece=UnityEngine.Object.Instantiate(pieces[i%originalCount],pieces[i%originalCount].parent);
                piece.name="CityRibbon"+i;pieces.Add(piece);
            }
            var mint=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/WellDone/MintButton.mat");
            for(int i=0;i<pieces.Count;i++)
                foreach(var renderer in pieces[i].GetComponentsInChildren<MeshRenderer>(true))
                    renderer.sharedMaterial=i%3==0?brass:i%3==1?cream:mint;
            city.confetti=pieces.ToArray();city.richConfetti=true;
            city.gameObject.SetActive(false);results.worldOneWellDone.gameObject.SetActive(false);
            results.solidLabels=results.presentation.GetComponentsInChildren<ToyBoxPodiumLabel>(true);
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        return "World 2 First Class Ticket saved; World 1 original retained.";
    }
    static void TicketLayer(Transform parent,string name,float width,float height,float depth,float z,Material mat)
    {
        // Clipped corners and half-round ticket punches on both sides.
        var p=new List<Vector2>();float x=width/2,y=height/2,c=.23f,r=.24f;
        p.Add(new Vector2(-x+c,y));p.Add(new Vector2(x-c,y));p.Add(new Vector2(x,y-c));p.Add(new Vector2(x,r));
        for(int i=1;i<=16;i++){float a=Mathf.PI/2+i*Mathf.PI/16;p.Add(new Vector2(x+Mathf.Cos(a)*r,Mathf.Sin(a)*r));}
        p.Add(new Vector2(x,-y+c));p.Add(new Vector2(x-c,-y));p.Add(new Vector2(-x+c,-y));p.Add(new Vector2(-x,-y+c));p.Add(new Vector2(-x,-r));
        for(int i=1;i<=16;i++){float a=-Mathf.PI/2+i*Mathf.PI/16;p.Add(new Vector2(-x+Mathf.Cos(a)*r,Mathf.Sin(a)*r));}
        p.Add(new Vector2(-x,y-c));
        var v=new List<Vector3>();var t=new List<int>();int n=p.Count;
        for(int ring=0;ring<4;ring++)foreach(var a in p){float s=ring==0||ring==3?.985f:1;v.Add(new Vector3(a.x*s,a.y*s,ring==0?-depth/2:ring==1?-depth/2+.025f:ring==2?depth/2-.025f:depth/2));}
        for(int k=0;k<3;k++)for(int i=0;i<n;i++){int a=k*n+i,b=k*n+(i+1)%n;t.AddRange(new[]{a,b,b+n,a,b+n,a+n});}
        for(int side=0;side<2;side++){int center=v.Count;v.Add(new Vector3(0,0,(side==0?-1:1)*depth/2));for(int i=0;i<n;i++){int a=(side==0?0:3)*n+i,b=(side==0?0:3)*n+(i+1)%n;t.AddRange(side==0?new[]{center,a,b}:new[]{center,b,a});}}
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        MeshObject(name,parent,new Vector3(0,0,z),ToyBoxGeometry.Save(mesh,Root+"/Meshes/"+name+".asset"),mat);
    }
}
