using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class BuildToyBoxMainMenu
{
    [UnityEditor.MenuItem("RealBuca/Toy Box 3D/9 - Generate Clear Gameplay Signs")]
    public static void GenerateClearGameplaySigns()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first.");
        if(!Application.isBatchMode && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders(); ToyBoxGeometry.Initialize(); MakeMaterials(); MakeSkinMaterials();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var manager=UnityEngine.Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
        InstallSolidHud(scene,manager);
        InstallResultsLeaderboard(scene);
        var puck=manager.puck.GetComponent<PuckController>();
        if(puck.previewCoverageLine!=null)
        {
            var band=puck.previewCoverageLine;
            band.widthMultiplier=1; band.widthCurve=AnimationCurve.Linear(0,.16f,1,.38f);
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.8f,.25f),0),new GradientColorKey(new Color(1,.8f,.25f),1)},
                new[]{new GradientAlphaKey(.22f,0),new GradientAlphaKey(.08f,1)});
            band.colorGradient=gradient; band.numCornerVertices=6; band.numCapVertices=6;
            band.enabled=false; band.positionCount=0;
            PrefabUtility.RecordPrefabInstancePropertyModifications(band); EditorUtility.SetDirty(band);
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }
    static void InstallComboSign(Scene scene,LevelManager manager)
    {
        foreach(var item in scene.GetRootGameObjects()) if(item.GetComponent<ToyBoxComboSign>()!=null) UnityEngine.Object.DestroyImmediate(item);
        var root=Group("SavedWoodenComboSign",null); SceneManager.MoveGameObjectToScene(root.gameObject,scene);
        var sign=root.gameObject.AddComponent<ToyBoxComboSign>();
        sign.presentation=Group("Presentation",root);
        Box("MapleBonusPlaque",sign.presentation,Vector3.zero,new Vector3(4.7f,1.10f,.24f),wood,.11f);
        sign.messages=new GameObject[4];
        string[] titles={"DOUBLE BONUS!","TRIPLE BONUS!","4X BONUS!","5X BONUS!"};
        for(int i=0;i<4;i++)
        {
            var message=Group("Bonus"+(i+2),sign.presentation); sign.messages[i]=message.gameObject;
            Text(titles[i],message,new Vector3(-.4f,.20f,-.15f),.24f,ink,false,.18f,.015f);
            Text((25*(i+2))+" BONUS POINTS",message,new Vector3(-.4f,-.23f,-.15f),.18f,ink);
            var badge=Group("GoldMultiplier",message,new Vector3(1.75f,0,-.20f)); badge.localRotation=Quaternion.Euler(90,0,0);
            Disc("GoldCoin",badge,Vector3.zero,.42f,.13f,yellow,.045f);
            Text("X"+(i+2),message,new Vector3(1.75f,0,-.29f),.28f,ink,false,.18f,.015f);
        }
        var award=Group("HoleInOneAward",sign.presentation); sign.holeInOne=award.gameObject;
        Text("HOLE IN ONE",award,new Vector3(0,.20f,-.15f),.30f,ink,false,.18f,.015f);
        Text("ONE SHOT - 50 PERCENT EXTRA POINTS",award,new Vector3(0,-.23f,-.15f),.14f,ink);
        award.gameObject.SetActive(false);
        sign.presentation.gameObject.SetActive(false);
        SavePart(root,"WoodenComboSign"); manager.comboSign=sign;
        EditorUtility.SetDirty(manager);
    }
    static void HideHudBranch(Component component)
    {
        if(component==null) return;
        Transform branch=component.transform;
        while(branch.parent!=null && branch.parent.name!="GameHUD" && branch.parent.GetComponent<LevelManager>()==null)
            branch=branch.parent;
        if(branch.name=="GameHUD") branch=component.transform;
        foreach(var graphic in branch.GetComponentsInChildren<Graphic>(true)) graphic.enabled=false;
    }
    static MeshFilter[] Number(Transform parent,Vector3 position,int places,float height,Material mat,Mesh[] digits)
    {
        var result=new MeshFilter[places];
        var number=Group("Number",parent,position);
        for(int i=0;i<places;i++)
        {
            var go=MeshObject("SolidDigit_"+i,number,Vector3.right*((i-(places-1)*.5f)*height*.85f),digits[0],mat);
            go.transform.localScale=Vector3.one*height; result[i]=go.GetComponent<MeshFilter>();
        }
        return result;
    }
    static void InstallSolidHud(Scene game,LevelManager manager)
    {
        foreach(var r in game.GetRootGameObjects()) if(r.name=="SavedSolidGameplayHud") UnityEngine.Object.DestroyImmediate(r);
        var root=Group("SavedSolidGameplayHud",null); SceneManager.MoveGameObjectToScene(root.gameObject,game);
        var hud=root.gameObject.AddComponent<ToyBoxGameplayHud>(); hud.manager=manager;
        hud.digitMeshes=new Mesh[10];
        for(int i=0;i<10;i++) hud.digitMeshes[i]=ToyBoxGeometry.Text(i.ToString(),.15f,.02f,.035f);
        var level=Group("LevelPlaque",root,new Vector3(-3.15f,.92f,7.00f));
        level.localRotation=Quaternion.Euler(12,0,0);
        Box("BirchPlaque",level,Vector3.zero,new Vector3(2.5f,.92f,.24f),wood,.12f);
        Text("LEVEL",level,new Vector3(-.50f,0,-.14f),.27f,ink);
        hud.levelDigits=Number(level,new Vector3(.68f,0,-.15f),2,.36f,ink,hud.digitMeshes);
        var timer=Group("TimePlaque",root,new Vector3(3.15f,.92f,7.00f));
        timer.localRotation=Quaternion.Euler(12,0,0);
        Box("BirchPlaque",timer,Vector3.zero,new Vector3(2.5f,.92f,.24f),wood,.12f);
        Text("TIME LEFT",timer,new Vector3(0,.24f,-.14f),.20f,ink);
        hud.timeDigits=Number(timer,new Vector3(0,-.19f,-.15f),2,.36f,ink,hud.digitMeshes);
        hud.timeFaces=new Renderer[2];for(int i=0;i<2;i++)hud.timeFaces[i]=hud.timeDigits[i].GetComponent<Renderer>();
        hud.normalTime=ink;hud.urgentTime=coral;
        BuildCleanCabinetFront(root,hud);
        BuildArcadeControlDeck(root,hud);
        HideHudBranch(manager.levelLabel);HideHudBranch(manager.levelBanner);
        HideHudBranch(manager.shotCounter);HideHudBranch(manager.scoreDisplay);
        HideHudBranch(manager.timerDisplay);HideHudBranch(manager.dragHint);HideHudBranch(manager.dragArrow);
        foreach(var hint in manager.GetComponentsInChildren<ControlHintBar>(true)) HideHudBranch(hint);
        // Persist a prefab without a scene reference, then bind its installed instance.
        InstallComboSign(game,manager);
        hud.manager=null; SavePart(root,"SolidGameplayHud"); hud.manager=manager;
        PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
    }
}
