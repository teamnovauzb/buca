using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    // Smooth domed solid heart, baked in editor; front faces the HUD camera (-Z).
    static Mesh HeartMesh()
    {
        const int count=96,rings=16;
        var v=new List<Vector3>();var t=new List<int>();
        for(int r=0;r<=rings;r++)
        {
            float s=(r+.001f)/(rings+.001f);
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count;
                float x=16*Mathf.Pow(Mathf.Sin(a),3)/32f;
                float y=(13*Mathf.Cos(a)-5*Mathf.Cos(2*a)-2*Mathf.Cos(3*a)-Mathf.Cos(4*a)+2)/32f;
                v.Add(new Vector3(x*s,y*s,-.19f*Mathf.Sqrt(1-s*s)));
                if(r>0){int b=r*count+i,c=r*count+(i+1)%count; t.AddRange(new[]{b,c,b-count,c,c-count,b-count});}
            }
        }
        // Flat back cap, hidden against socket.
        int center=v.Count;v.Add(Vector3.zero);
        for(int i=0;i<count;i++) t.AddRange(new[]{center,rings*count+(i+1)%count,rings*count+i});
        var mesh=new Mesh{name="DomedHeart"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Root+"/Meshes/DomedShotHeart.asset");
    }
    static void BuildHeartIndicator(Transform allowance,ToyBoxGameplayHud hud)
    {
        if(hud.limitedLivesLabel!=null) Object.DestroyImmediate(hud.limitedLivesLabel);
        var limited=Group("IconOnlyHearts",allowance);hud.limitedLivesLabel=limited.gameObject;
        hud.livesLeftDigits=new MeshFilter[0];hud.livesTotalDigits=new MeshFilter[0];
        hud.heartSlots=new GameObject[8];hud.heartFills=new GameObject[8];
        var mesh=HeartMesh();
        var red=Material("TileHeartCoral",new Color(.86f,.24f,.16f),.30f);
        var cream=Material("HeartTileCream",new Color(.89f,.82f,.66f),.27f);
        var support=Material("HeartTileSupport",new Color(.06f,.28f,.29f),.25f);
        allowance.Find("AllowancePanel").GetComponent<Renderer>().sharedMaterial=support;
        var rim=Material("HeartTileSocketEdge",new Color(.58f,.46f,.32f),.22f);
        var empty=Material("HeartTileSocket",new Color(.73f,.63f,.47f),.22f);
        for(int i=0;i<8;i++)
        {
            var slot=Group("HeartSlot"+i,limited);hud.heartSlots[i]=slot.gameObject;
            Box("CreamTile",slot,new Vector3(0,0,-.03f),new Vector3(.56f,.74f,.14f),cream,.065f);
            var border=MeshObject("HeartRecessEdge",slot,new Vector3(0,0,-.106f),mesh,rim);border.transform.localScale=new Vector3(.43f,.43f,.04f);
            var inset=MeshObject("EmptyHeartSocket",slot,new Vector3(0,0,-.115f),mesh,empty);inset.transform.localScale=new Vector3(.39f,.39f,.04f);
            var fill=MeshObject("RedHeart",slot,new Vector3(0,0,-.125f),mesh,red);fill.transform.localScale=new Vector3(.41f,.41f,.48f);hud.heartFills[i]=fill;
        }
        BuildMoveCounter(hud);
        hud.RefreshHearts(5,5);
    }
    static void BuildMoveCounter(ToyBoxGameplayHud hud)
    {
        var parent=hud.limitedLivesLabel.transform;
        var old=parent.Find("SavedMoveCounter");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var counter=Group("SavedMoveCounter",parent);hud.numericMoves=counter.gameObject;
        var face=hud.heartSlots[0].transform.Find("CreamTile").GetComponent<Renderer>().sharedMaterial;
        var heart=hud.heartFills[0].GetComponent<MeshFilter>().sharedMesh;
        var red=hud.heartFills[0].GetComponent<Renderer>().sharedMaterial;
        Box("MoveCountTile",counter,new Vector3(0,0,-.18f),new Vector3(3.10f,.74f,.14f),face,.065f);
        var icon=MeshObject("Heart",counter,new Vector3(-.98f,0,-.27f),heart,red);
        icon.transform.localScale=new Vector3(.56f,.56f,.48f);
        Text("MOVES",counter,new Vector3(.32f,.21f,-.27f),.16f,hud.normalTime);
        hud.livesLeftDigits=Number(counter,new Vector3(.32f,-.13f,-.27f),2,.38f,hud.normalTime,hud.digitMeshes);
        hud.livesTotalDigits=new MeshFilter[0];
        counter.gameObject.SetActive(false);
    }
    [MenuItem("RealBuca/Toy Box 3D/Update Move Counter")]
    public static string GenerateMoveCounter()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode required");
        PrepareFolders();ToyBoxGeometry.Initialize();
        string path=Root+"/Prefabs/SolidGameplayHud.prefab";
        string backup="output/balance/hud-before-moves-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".prefab";
        System.IO.Directory.CreateDirectory("output/balance");System.IO.File.Copy(path,backup,false);
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud=root.GetComponent<ToyBoxGameplayHud>();BuildMoveCounter(hud);
            for(int max=5;max<=32;max++)for(int left=0;left<=max;left++)
            {
                hud.RefreshHearts(left,max);
                if(hud.numericMoves.activeSelf!=(max>5))throw new System.Exception("Wrong move display mode");
                for(int i=0;i<hud.heartSlots.Length;i++)
                    if(hud.heartSlots[i].activeSelf!=(max<=5&&i<max))throw new System.Exception("Wrong heart visibility");
                if(max>5)
                {
                    if(hud.livesLeftDigits[1].sharedMesh!=hud.digitMeshes[left%10])throw new System.Exception("Wrong units digit");
                    if(left>=10 && hud.livesLeftDigits[0].sharedMesh!=hud.digitMeshes[left/10])throw new System.Exception("Wrong tens digit");
                    if(hud.livesLeftDigits[0].gameObject.activeSelf!=(left>=10))throw new System.Exception("Wrong leading digit visibility");
                }
            }
            hud.RefreshHearts(5,5);PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        return "PASS: saved move counter, every remaining-move state for allowances 5–32, including star rewards. Backup: "+backup;
    }
    public static void GenerateHeartIndicator()
    {
        PrepareFolders();ToyBoxGeometry.Initialize();
        string path=Root+"/Prefabs/SolidGameplayHud.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var hud=root.GetComponent<ToyBoxGameplayHud>();BuildHeartIndicator(root.transform.Find("ShotAllowance"),hud);
            for(int max=3;max<=5;max++)for(int left=0;left<=max;left++) {
                hud.RefreshHearts(left,max);
                for(int i=0;i<8;i++)if(hud.heartSlots[i].activeSelf!=(i<max)||hud.heartFills[i].activeSelf!=(i<left))throw new System.Exception("Heart indicator mismatch");
            }
            hud.RefreshHearts(5,5);PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("HEART_INDICATOR_PASSED: all remaining shots for 3, 4 and 5 hearts.");
    }
}
