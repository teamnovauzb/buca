using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    static void BuildSkinPreview(Transform root, ToyBoxMenuController menu)
    {
        var stage = Group("FullSizeSkinPreview", root);
        menu.previewRoot = stage.gameObject;
        var scenery = new List<GameObject>();
        foreach (string name in new[] { "ToyTable", "RaisedBucaSign", "ToyRoomScenery", "CameraAndLighting" })
            scenery.Add(root.Find(name).gameObject);
        menu.menuScenery = scenery.ToArray();

        // Copy only saved visual meshes: no gameplay scripts, triggers or physics.
        var source = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Levels/Level_01.prefab");
        try
        {
            BakeBoardSkin(source);
            var board = Group("ActualBoardPreview", stage);
            var copies = new Dictionary<Renderer, Renderer>();
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled || renderer.name == "BUCA") continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var copy = MeshObject(renderer.name, board, renderer.transform.position,
                    filter.sharedMesh, renderer.sharedMaterial);
                copy.transform.localRotation = renderer.transform.rotation;
                copy.transform.localScale = renderer.transform.lossyScale;
                copy.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                copies.Add(renderer, copy.GetComponent<MeshRenderer>());
            }
            menu.previewBoard = board.gameObject.AddComponent<BucaSkinBinding>();
            var bindings = new List<BucaSkinBinding.Surface>();
            foreach (var surfaceBinding in source.GetComponent<BucaSkinBinding>().surfaces)
                if (copies.TryGetValue(surfaceBinding.target, out var renderer))
                    Bind(bindings, renderer, surfaceBinding.variants);
            menu.previewBoard.surfaces = bindings.ToArray();
        }
        finally { PrefabUtility.UnloadPrefabContents(source); }

        var puck = Group("ActualPlayerPuckPreview", stage, new Vector3(0,.11f,-5.5f));
        var body = Disc("PlayerPuck", puck, Vector3.zero, .30f, .20f, pucks[0], .035f);
        var motif = MeshObject("PuckInlay", puck, new Vector3(0,.106f,0), PuckMotif(0,.30f), accents[0]);
        var centre = Disc("BullseyeCentre", puck, new Vector3(0,.108f,0), .081f, .012f, pucks[0], .003f);
        menu.previewPuck = puck.gameObject.AddComponent<BucaSkinBinding>();
        var puckBindings = new List<BucaSkinBinding.Surface>();
        Bind(puckBindings, body.GetComponent<Renderer>(), pucks);
        Bind(puckBindings, motif.GetComponent<Renderer>(), accents);
        Bind(puckBindings, centre.GetComponent<Renderer>(), new[] { pucks[0],pucks[1],accents[2],accents[3],pucks[4] });
        menu.previewPuck.surfaces = puckBindings.ToArray();
        menu.previewPuck.motif = motif.GetComponent<MeshFilter>();
        menu.previewPuck.motifMeshes = new Mesh[5];
        for (int i=0;i<5;i++) menu.previewPuck.motifMeshes[i]=PuckMotif(i,.30f);

        var roomAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ToyPlayroom.prefab");
        if (roomAsset != null)
        {
            var room=(GameObject)PrefabUtility.InstantiatePrefab(roomAsset,stage);
            room.transform.localPosition=new Vector3(0,-1.7f,0);
        }
        var cameraObject=Group("SkinPreviewCamera",stage).gameObject;
        menu.previewCamera=cameraObject.AddComponent<Camera>();
        cameraObject.tag="MainCamera";
        cameraObject.AddComponent<AudioListener>();
        ConfigureComfortableCamera(menu.previewCamera);
        menu.previewCamera.farClipPlane=100;
        menu.previewCamera.nearClipPlane=.1f;
        var data=cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());
        data.renderPostProcessing=true;
        data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;

        Box("SkinNamePlaque",stage,new Vector3(0,1.0f,7.0f),new Vector3(7.6f,.9f,.22f),wood,.12f);
        menu.previewTitles=new GameObject[5];
        for(int i=0;i<5;i++)
        {
            menu.previewTitles[i]=Text(SkinNames[i],stage,new Vector3(0,1.05f,6.85f),.32f,ink);
            menu.previewTitles[i].SetActive(i==0);
        }
        Text("EQUIPPED  -  BOARD + PUCK",stage,new Vector3(0,.73f,6.84f),.13f,ink);
        menu.previewButtons=new ToyBoxMenuController.PuckButton[3];
        string[] labels={"PREV","NEXT","BACK"};
        for(int i=0;i<3;i++)
        {
            var holder=Group(labels[i]+"Control",stage,new Vector3((i-1)*3.5f,-.85f,-7.95f));
            holder.localRotation=Quaternion.Euler(-90,0,0);
            menu.previewButtons[i]=Button(holder,labels[i],Vector3.zero,.47f,i==2?mint:yellow,.19f,false);
        }
        SavePart(stage,"FullSizeSkinPreview");
        stage.gameObject.SetActive(false);
    }
}
