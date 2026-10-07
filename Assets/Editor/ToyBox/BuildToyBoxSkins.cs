using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class BuildToyBoxMainMenu
{
    static readonly string[] SkinNames = { "CLASSIC TOY BOX", "MINT GARDEN", "HONEY BEE", "BERRY CREAM", "OCEAN CLUB" };
    static Material[] rails, plugs, bumpers, goals, pucks, accents;

    static Material SkinPaint(string name, Color color)
    {
        var mat = Material(name, color, .4f, ToyBoxSurfaceBaker.WoodGrain(false, true));
        mat.SetTexture("_BumpMap", ToyBoxSurfaceBaker.WoodGrain(true));
        mat.SetFloat("_BumpScale", .22f); mat.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(mat); return mat;
    }
    static void MakeSkinMaterials()
    {
        var sage = SkinPaint("SkinSage", new Color(.42f,.55f,.36f));
        var teal = SkinPaint("SkinDeepTeal", new Color(.08f,.34f,.34f));
        var cocoa = SkinPaint("SkinCocoa", new Color(.25f,.12f,.07f));
        var plum = SkinPaint("SkinPlum", new Color(.39f,.19f,.30f));
        var pink = SkinPaint("SkinDustyPink", new Color(.76f,.43f,.44f));
        var berry = SkinPaint("SkinRaspberry", new Color(.68f,.17f,.28f));
        var ocean = SkinPaint("SkinOcean", new Color(.16f,.43f,.51f));
        rails = new[] { blue,sage,yellow,plum,ocean };
        plugs = new[] { mint,cream,cream,pink,cream };
        bumpers = new[] { yellow,yellow,blue,cream,coral };
        goals = new[] { mint,coral,mint,mint,blue };
        pucks = new[] { coral,teal,cocoa,berry,yellow };
        accents = new[] { cream,cream,yellow,cream,ink };
    }
    static Mesh PuckMotif(int index, float radius)
    {
        // Ring, ring, bullseye, solid centre, ring. All meshes saved by the baker.
        return index == 3 ? ToyBoxGeometry.Disc(radius*.53f,.012f,.004f) :
            ToyBoxGeometry.Ring(radius*.73f, radius*(index==2?.20f:.12f),.012f);
    }
    static void BuildSkinsTray(Transform root, ToyBoxMenuController controller)
    {
        BuildPaintWorkshop(root,controller);
    }

    static void Bind(List<BucaSkinBinding.Surface> list, Renderer target, Material[] variants)
        => list.Add(new BucaSkinBinding.Surface { target=target, variants=variants });
    static Material[] Same(Material mat) => new[]{mat,mat,mat,mat,mat};

    static void InstallGameplaySkins()
    {
        const string gamePath="Assets/Scenes/Game.unity";
        string backup=Root+"/Backup/Game-BeforeSkins.unity";
        if(!File.Exists(backup) && !AssetDatabase.CopyAsset(gamePath,backup))
            throw new IOException("Could not back up Game before adding skins.");
        if(!AssetDatabase.IsValidFolder(Root+"/Levels")) AssetDatabase.CreateFolder(Root,"Levels");
        Scene game=EditorSceneManager.OpenScene(gamePath,OpenSceneMode.Additive);
        try
        {
            LevelManager manager=null;
            foreach(var r in game.GetRootGameObjects())
                if(r.GetComponentInChildren<LevelManager>(true) is LevelManager found) manager=found;
            if(manager==null) throw new InvalidOperationException("Game has no LevelManager.");
            for(int i=0;i<manager.levelPrefabs.Length;i++)
            {
                string source="Assets/Prefabs/Levels/Level_"+(i+1).ToString("00")+".prefab";
                var level=PrefabUtility.LoadPrefabContents(source);
                try
                {
                    BakeBoardSkin(level);
                    manager.levelPrefabs[i]=PrefabUtility.SaveAsPrefabAsset(level,Root+"/Levels/Level_"+(i+1).ToString("00")+".prefab");
                }
                finally { PrefabUtility.UnloadPrefabContents(level); }
            }
            InstallPlayroom(game);
            InstallSolidHud(game,manager);
            var puck=manager.puck;
            var binding=puck.GetComponent<BucaSkinBinding>() ?? puck.AddComponent<BucaSkinBinding>();
            var old=puck.transform.Find("SavedSkinInlay");
            if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var mf=puck.GetComponent<MeshFilter>();
            // Preserve physics radius and slide behaviour, improve just the mesh bevel.
            Bounds bounds=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/HockeyPuck.mesh").bounds;
            float radius=Mathf.Max(bounds.extents.x,bounds.extents.z);
            float puckHeight=bounds.size.y*1.8f;
            mf.sharedMesh=ToyBoxGeometry.Disc(radius,puckHeight,.065f);
            var motif=MeshObject("SavedSkinInlay",puck.transform,new Vector3(0,puckHeight*.5f+.007f,0),PuckMotif(0,radius),accents[0]);
            var surfaces=new List<BucaSkinBinding.Surface>();
            Bind(surfaces,puck.GetComponent<Renderer>(),pucks);
            Bind(surfaces,motif.GetComponent<Renderer>(),accents);
            binding.surfaces=surfaces.ToArray(); binding.motif=motif.GetComponent<MeshFilter>();
            binding.motifMeshes=new Mesh[5];
            for(int i=0;i<5;i++) binding.motifMeshes[i]=PuckMotif(i,radius);
            // Honey's central dot is included in its own saved mesh child.
            var centre=Disc("HoneyCentre",motif.transform,Vector3.zero,radius*.25f,.012f,accents[0],.003f);
            var centerColors=new[]{pucks[0],pucks[1],accents[2],accents[3],pucks[4]};
            surfaces.Add(new BucaSkinBinding.Surface{target=centre.GetComponent<Renderer>(),variants=centerColors});
            binding.surfaces=surfaces.ToArray(); binding.Apply(0);
            SavePart(motif.transform,"PlayerSkinInlay");
            InstallWatchCopy(game);
        InstallWinCelebration(game);
        InstallResultsLeaderboard(game);
            EditorSceneManager.MarkSceneDirty(game); EditorSceneManager.SaveScene(game);
        }
        finally { EditorSceneManager.CloseScene(game,true); }
    }

    static void BakeSolidRail(MeshRenderer original,Transform shell,List<BucaSkinBinding.Surface> surfaces,Material[] palette,float height)
    {
        var cap=original.GetComponent<CapsuleCollider>();
        if(cap==null) { Bind(surfaces,original,palette); return; }
        Vector3 axis=cap.direction==0?Vector3.right:cap.direction==1?Vector3.up:Vector3.forward;
        Vector3 scale=original.transform.lossyScale;
        float along=cap.direction==0?scale.x:cap.direction==1?scale.y:scale.z;
        float across=cap.direction==0?Mathf.Max(scale.y,scale.z):cap.direction==1?Mathf.Max(scale.x,scale.z):Mathf.Max(scale.x,scale.y);
        Vector3 direction=original.transform.TransformDirection(axis); direction.y=0;
        if(direction.sqrMagnitude<.1f) { Bind(surfaces,original,palette); return; }
        float radius=cap.radius*across,length=Mathf.Max(2*radius,cap.height*along);
        Vector3 center=shell.InverseTransformPoint(original.transform.TransformPoint(cap.center)); center.y=0;
        var solid=MeshObject(original.name+"_SolidWood",shell,center,ToyBoxGeometry.StandingCapsule(radius,length,height),palette[0]);
        solid.transform.rotation=Quaternion.LookRotation(direction.normalized,Vector3.up);
        // Keep authored moving/rotating walls and their visuals together.
        solid.transform.SetParent(original.transform,true);
        Bind(surfaces,solid.GetComponent<Renderer>(),palette);
        original.enabled=false;
    }

    static void BakeBoardSkin(GameObject level)
    {
        var surfaces=new List<BucaSkinBinding.Surface>();
        var shell=Group("SavedToyBoardShell",level.transform);
        foreach(var renderer in level.GetComponentsInChildren<MeshRenderer>(true))
        {
            string name=renderer.name;
            if(name=="Floor") renderer.enabled=false;
            else if(name=="Table_Bevel") renderer.enabled=false;
            else if(name.StartsWith("Bound_")) { BakeSolidRail(renderer,shell,surfaces,rails,.60f); }
            else if(name.StartsWith("Corner_")) renderer.enabled=false;
            else if(name=="Hole_Ring") renderer.enabled=false;
            else if(name=="Hole") renderer.enabled=false;
            else if(name.StartsWith("NM4_")) renderer.enabled=false;
            else if(renderer.GetComponent<RailLight>()!=null) BakeSolidRail(renderer,shell,surfaces,bumpers,.65f);
        }
        Bind(surfaces,Box("RoundedWoodCabinet",shell,new Vector3(0,-1.01f,0),new Vector3(10.40f,1.02f,15.45f),rails[0],.22f).GetComponent<Renderer>(),rails);
        var hole=level.transform.Find("Hole");
        Vector3 hp=hole.localPosition; float holeRadius=.44f;
        Bind(surfaces,MeshObject("MapleWithCutout",shell,Vector3.zero,ToyBoxGeometry.RecessedBoard(new Vector2(hp.x,hp.z),holeRadius),surface).GetComponent<Renderer>(),Same(surface));
        Ring("DarkRecessWall",shell,new Vector3(hp.x,-.006f,hp.z),holeRadius+.012f,.025f,black,.43f);
        Disc("RecessBottom",shell,new Vector3(hp.x,-.44f,hp.z),holeRadius,.025f,black,.006f);
        Bind(surfaces,MeshObject("BeveledGoalRim",shell,new Vector3(hp.x,.025f,hp.z),ToyBoxGeometry.Torus(holeRadius+.075f,.075f),goals[0]).GetComponent<Renderer>(),goals);
        // Outside the existing walls: cosmetic rails cannot affect collisions.
        for(int side=-1;side<=1;side+=2)
        {
            Bind(surfaces,Box("WoodSideRail",shell,new Vector3(side*4.95f,-.05f,0),new Vector3(.85f,1.0f,15.1f),rails[0],.20f).GetComponent<Renderer>(),rails);
            Bind(surfaces,Box("WoodEndRail",shell,new Vector3(0,-.05f,side*7.45f),new Vector3(10.65f,1.0f,.85f),rails[0],.20f).GetComponent<Renderer>(),rails);
            for(int end=-1;end<=1;end+=2)
            {
                Bind(surfaces,Disc("MintCornerCap",shell,new Vector3(side*4.95f,.49f,end*7.40f),.31f,.13f,plugs[0],.04f).GetComponent<Renderer>(),plugs);
                Disc("BirchLeg",shell,new Vector3(side*4.35f,-2.30f,end*6.8f),.48f,1.72f,wood,.12f);
            }
        }
        // Layered cabinet joinery and metal fasteners catch the studio highlights.
        for(int side=-1;side<=1;side+=2)
        {
            Box("BirchSideInlay",shell,new Vector3(side*5.205f,-1.27f,0),new Vector3(.025f,.065f,14.9f),wood,.012f);
            for(int end=-1;end<=1;end+=2)
            {
                var fastener=Disc("SolidCornerFastener",shell,new Vector3(side*4.84f,-.77f,end*7.755f),.07f,.025f,cream,.008f);
                fastener.transform.localRotation=Quaternion.Euler(90,0,0);
            }
        }
        Text("BUCA",shell,new Vector3(-3.72f,-.90f,-7.79f),.48f,cream,false,.16f,.02f);
        var binding=level.AddComponent<BucaSkinBinding>(); binding.surfaces=surfaces.ToArray(); binding.Apply(0);
    }
}
