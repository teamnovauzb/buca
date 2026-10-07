using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/7 - Generate Level Results")]
    public static void GenerateResultsLeaderboard()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareFolders(); ToyBoxGeometry.Initialize(); MakeMaterials(); MakeSkinMaterials();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Single);
        InstallResultsLeaderboard(scene);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("RESULTS_LEADERBOARD_BAKED");
    }
    static void InstallResultsLeaderboard(Scene scene)
    {
        LevelCompletePanel panel=null;
        foreach(var obj in scene.GetRootGameObjects())
        {
            if(obj.GetComponent<ToyBoxResults3D>()!=null) UnityEngine.Object.DestroyImmediate(obj);
            else if(obj.GetComponentInChildren<LevelCompletePanel>(true)!=null) panel=obj.GetComponentInChildren<LevelCompletePanel>(true);
        }
        if(panel==null) throw new System.InvalidOperationException("Result panel missing.");
        var root=new GameObject("ToyBoxLevelResults"); SceneManager.MoveGameObjectToScene(root,scene); root.transform.position=new Vector3(150,0,0);
        var view=root.AddComponent<ToyBoxResults3D>();
        var stage=Group("Presentation",root.transform); view.presentation=stage.gameObject;
        // Use a quieter grain on this close-up display without changing the game's skin materials.
        var podiumWood=new Material(wood) { name="PodiumMaple" }; podiumWood.SetFloat("_BumpScale",.10f);
        podiumWood.SetFloat("_Smoothness",.28f);
        wood=ToyBoxGeometry.Save(podiumWood,Root+"/Materials/PodiumMaple.mat");
        // All furniture, lettering and controls are saved in this prefab in edit mode.
        view.podiumBase=Group("SharedMaplePodium",stage,new Vector3(0,-4.3f,0));
        PodiumSlab("RoundedMapleBase",view.podiumBase,Vector3.zero,6.9f,1.05f,1.75f,.35f,wood);
        view.baseBranding=Group("BaseBranding",view.podiumBase);
        Text("BUCA",view.baseBranding,new Vector3(-5,0,-.94f),.40f,blue,false,.18f,.02f);
        Text("SMALL SHOTS",view.baseBranding,new Vector3(5,.16f,-.94f),.15f,ink);
        Text("BIG FUN",view.baseBranding,new Vector3(5,-.17f,-.94f),.20f,ink);
        view.leftBoard=Group("YourResult",stage,Vector3.zero);
        PodiumSlab("MedalStand",view.leftBoard,new Vector3(0,-1.5f,.22f),5.8f,4.7f,.65f,.55f,blue);
        var face=Group("RoundAward",view.leftBoard,new Vector3(0,1.25f,0)); face.localRotation=Quaternion.Euler(90,0,0);
        Disc("MapleMedalRim",face,Vector3.zero,3.05f,.55f,wood,.16f);
        Disc("CobaltMedalFace",face,new Vector3(0,-.31f,0),2.84f,.20f,blue,.09f);
        foreach(int side in new[]{-1,1})
            PodiumSlab("MedalFoot",view.leftBoard,new Vector3(side*2.75f,-3.35f,.02f),.40f,1.0f,.85f,.19f,wood);
        var regularTitle=Group("RegularTitle",view.leftBoard); view.regularTitle=regularTitle.gameObject;
        var oneTitle=Group("HoleInOneTitle",view.leftBoard); view.holeInOneTitle=oneTitle.gameObject;
        Text("HOLE IN",oneTitle,new Vector3(0,2.11f,-.52f),.62f,cream,false,.23f,.02f);
        Text("ONE!",oneTitle,new Vector3(0,1.32f,-.52f),.68f,yellow,false,.23f,.02f);
        oneTitle.gameObject.SetActive(false);
        Text("LEVEL",regularTitle,new Vector3(0,2.11f,-.52f),.68f,cream,false,.23f,.02f);
        Text("COMPLETE!",regularTitle,new Vector3(0,1.32f,-.52f),.60f,cream,false,.23f,.02f);
        view.earnedStars=new GameObject[3];
        for(int i=0;i<3;i++)
        {
            var badge=Group("Star"+i,view.leftBoard,new Vector3((i-1)*1.64f,i==1?3.85f:3.18f,-.51f)); badge.localRotation=Quaternion.AngleAxis(-24,Vector3.forward)*Quaternion.Euler(90,0,0);
            MeshObject("StarBacking",badge,Vector3.zero,ToyBoxGeometry.MapPrism(10,.75f,.17f,.045f,true),wood);
            view.earnedStars[i]=MeshObject("EarnedStar",badge,new Vector3(0,-.15f,0),ToyBoxGeometry.MapPrism(10,.68f,.24f,.06f,true),yellow);
        }
        TMP_FontAsset font=panel.totalText.font;
        view.golfSummary=ResultText("ClearGolfResult",view.leftBoard,new Vector3(0,.74f,-.73f),new Vector2(5.4f,.3f),font,1.55f,cream.color,TextAlignmentOptions.Center);
        foreach(var glyph in view.golfSummary.GetComponent<ToyBoxPodiumLabel>().places) glyph.GetComponent<Renderer>().sharedMaterial=cream;
        var cardButton=PodiumSlab("MyShotsButton",stage,new Vector3(0,5.1f,-.6f),6.2f,.58f,.20f,.14f,mint);
        view.scorecardHit=cardButton.AddComponent<BoxCollider>(); ((BoxCollider)view.scorecardHit).size=new Vector3(6.2f,.58f,.20f);
        Text("GREEN - MY SHOTS - BACK",stage,new Vector3(0,5.1f,-.73f),.20f,ink);
        var card=Group("MyShotsScorecard",stage); view.scorecardRoot=card.gameObject;
        PodiumSlab("ScorecardMaple",card,new Vector3(0,0,.1f),7.3f,9.1f,.45f,.35f,wood);
        view.scorecardTitle=ResultText("ScorecardTitle",card,new Vector3(0,3.8f,-.2f),new Vector2(6.8f,.5f),font,2.8f,ink.color,TextAlignmentOptions.Center);
        Text("THIS RUN AND YOUR BEST SHOTS",card,new Vector3(0,3.14f,-.2f),.20f,ink);
        view.scorecardRows=new TMP_Text[6];
        for(int row=0;row<6;row++)
        {
            float y=2.25f-row*.83f;
            PodiumSlab("HoleRow"+row,card,new Vector3(0,y,-.2f),6.8f,.70f,.16f,.10f,cream);
            view.scorecardRows[row]=ResultText("HoleText"+row,card,new Vector3(0,y,-.31f),new Vector2(6.5f,.5f),font,1.75f,ink.color,TextAlignmentOptions.Center);
        }
        view.scorecardFooter=ResultText("PersonalBestNote",card,new Vector3(0,-3.05f,-.22f),new Vector2(6.8f,.4f),font,1.7f,ink.color,TextAlignmentOptions.Center);
        Text("JOYSTICK - CHANGE PAGE",card,new Vector3(0,-3.63f,-.22f),.20f,ink);
        Text("BLACK - BACK TO RESULT",card,new Vector3(0,-4.03f,-.22f),.20f,ink);
        card.gameObject.SetActive(false);
        view.scoreValues=new TMP_Text[5];
        string[] labels={"LEVEL POINTS","TIME BONUS","RAIL BONUS","SHOT BONUS"};
        for(int i=0;i<4;i++)
        {
            float y=-1.43f-i*.65f;
            PodiumSlab("ScoreTile"+i,view.leftBoard,new Vector3(0,y,-.43f),5.05f,.57f,.22f,.16f,wood);
            Text(labels[i],view.leftBoard,new Vector3(-.65f,y,-.58f),.18f,ink,false,.17f,.01f);
            view.scoreValues[i]=ResultText("Value"+i,view.leftBoard,new Vector3(1.69f,y,-.59f),new Vector2(1.4f,.46f),font,3.1f,ink.color,TextAlignmentOptions.Right);
            var icon=Group("ScoreIcon"+i,view.leftBoard,new Vector3(-2.12f,y,-.62f)); icon.localRotation=Quaternion.Euler(90,0,0);
            if(i==0) MeshObject("Star",icon,Vector3.zero,ToyBoxGeometry.MapPrism(10,.21f,.10f,.02f,true),yellow);
            if(i==1)
            {
                Disc("Clock",icon,Vector3.zero,.21f,.08f,coral,.03f);
                Disc("ClockFace",icon,new Vector3(0,-.05f,0),.16f,.03f,cream,.01f);
                Box("Hand",icon,new Vector3(0,-.08f,-.055f),new Vector3(.025f,.025f,.13f),ink,.008f);
                Box("Hand",icon,new Vector3(.04f,-.08f,0),new Vector3(.10f,.025f,.025f),ink,.008f);
            }
            if(i==2)
            {
                Box("Rail",icon,Vector3.zero,new Vector3(.40f,.10f,.09f),blue,.035f);
                foreach(int side in new[]{-1,1}) Box("Post",icon,new Vector3(side*.15f,0,0),new Vector3(.08f,.10f,.34f),blue,.03f);
            }
            if(i==3) Disc("Puck",icon,Vector3.zero,.20f,.11f,coral,.04f);
        }
        PodiumSlab("TotalPlaque",view.leftBoard,new Vector3(0,-.22f,-.51f),5.05f,1.63f,.32f,.35f,wood);
        Text("TOTAL",view.leftBoard,new Vector3(0,.30f,-.71f),.28f,ink,false,.16f,.01f);
        view.scoreValues[4]=ResultText("TotalValue",view.leftBoard,new Vector3(0,-.34f,-.72f),new Vector2(4.7f,.9f),font,6,coral.color,TextAlignmentOptions.Center);
        view.scoreValues[4].gameObject.SetActive(false); // State retained; visible total uses saved solid glyph meshes.
        view.digitMeshes=new Mesh[10];
        for(int i=0;i<10;i++) view.digitMeshes[i]=ToyBoxGeometry.Text(i.ToString(),.20f,.025f,.025f);
        view.totalDigits=Number(view.leftBoard,new Vector3(0,-.35f,-.76f),10,.80f,coral,view.digitMeshes);
        view.comboText=ResultText("ComboBonus",view.leftBoard,new Vector3(0,-.89f,-.74f),new Vector2(4.5f,.25f),font,1.65f,ink.color,TextAlignmentOptions.Center);
        view.nextRoot=Group("NextControl",stage,new Vector3(0,-4.28f,-1.02f)); view.nextRoot.localRotation=Quaternion.Euler(-90,0,0);
        PodiumSlab("NextSocket",view.nextRoot,Vector3.zero,4.9f,1.03f,.18f,.33f,wood).transform.localRotation=Quaternion.Euler(90,0,0);
        view.selectedRing=PodiumSlab("SelectedGoldBorder",view.nextRoot,new Vector3(0,.08f,0),4.72f,.94f,.12f,.30f,yellow);
        view.selectedRing.transform.localRotation=Quaternion.Euler(90,0,0);
        view.nextCap=Group("PressableMintCap",view.nextRoot,new Vector3(0,.20f,0)); view.nextRest=view.nextCap.localPosition;
        var cap=PodiumSlab("MintButton",view.nextCap,Vector3.zero,4.55f,.82f,.23f,.27f,mint); cap.transform.localRotation=Quaternion.Euler(90,0,0);
        var collider=cap.AddComponent<BoxCollider>(); collider.size=new Vector3(4.55f,.82f,.23f); view.nextHit=collider;
        Text("NEXT",view.nextCap,new Vector3(-.49f,.15f,0),.40f,ink,true,.18f,.01f);
        var countdown=Group("CountdownFace",view.nextCap,new Vector3(.97f,.16f,0)); countdown.localRotation=Quaternion.Euler(90,0,0);
        view.countdownText=ResultText("NextCountdown",countdown,Vector3.zero,new Vector2(1.48f,.45f),font,3.1f,ink.color,TextAlignmentOptions.Center);
        var cam=Group("ResultsCamera",stage,new Vector3(0,1.2f,-23)).gameObject.AddComponent<Camera>(); view.resultsCamera=cam;
        cam.orthographic=false; cam.fieldOfView=29; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.78f,.69f,.52f); cam.nearClipPlane=.1f; cam.farClipPlane=65;
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(ToyBoxSurfaceBaker.EnsureMenuRenderer());
        view.levelCapture=SaveResultTarget("ResultsLevelCapture",24);
        view.blurScratch=SaveResultTarget("ResultsBlurScratch",0);
        view.blurredLevel=SaveResultTarget("ResultsBlurredLevel",0);
        view.blurMaterial=ToyBoxGeometry.Save(new Material(Shader.Find("Hidden/Buca/GaussianBlur")),Root+"/Materials/ResultsBlur.mat");
        var backdropMaterial=new Material(Shader.Find("Buca/ResultsBackdrop")); backdropMaterial.SetTexture("_MainTex",view.blurredLevel);
        backdropMaterial=ToyBoxGeometry.Save(backdropMaterial,Root+"/Materials/ResultsBackdrop.mat");
        view.levelBackdrop=Box("BlurredCurrentLevel",cam.transform,new Vector3(0,0,40),new Vector3(2,2,.01f),backdropMaterial,.001f).transform;
        var lamp=Group("ResultKeyLight",stage,new Vector3(-5,7,-7)).gameObject.AddComponent<Light>();
        lamp.type=LightType.Point; lamp.range=28; lamp.intensity=12; lamp.color=new Color(1,.94f,.83f); lamp.shadows=LightShadows.Soft;
        var fill=Group("ResultFill",stage,new Vector3(6,3,-4)).gameObject.AddComponent<Light>(); fill.type=LightType.Point; fill.range=24; fill.intensity=6; fill.color=new Color(.82f,.91f,1);
        view.solidLabels=stage.GetComponentsInChildren<ToyBoxPodiumLabel>(true);
        stage.gameObject.SetActive(false);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/ResultsAndLeaderboard.prefab"); UnityEngine.Object.DestroyImmediate(root);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        panel.premiumResults=instance.GetComponent<ToyBoxResults3D>(); EditorUtility.SetDirty(panel); PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
        FullScreenResultsCelebrationBaker.Bake();
        WoodenWellDoneBaker.Bake();
    }
    static RenderTexture SaveResultTarget(string name,int depth)
    {
        var target=new RenderTexture(1600,900,depth,RenderTextureFormat.ARGB32) {name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        return ToyBoxGeometry.Save(target,Root+"/Materials/"+name+".renderTexture");
    }
    // Convex extruded profile with independent broad faces and a rounded bevel.
    // Unlike scaling a thin cube, this preserves visibly rounded silhouette corners.
    static GameObject PodiumSlab(string name,Transform parent,Vector3 position,float width,float height,float depth,float radius,Material material,bool arch=false)
    {
        var outline=new System.Collections.Generic.List<Vector2>();
        if(arch)
        {
            for(int i=0;i<=48;i++) { float a=i*Mathf.PI/48; outline.Add(new Vector2(Mathf.Cos(a)*width*.5f,height*.5f-.85f+Mathf.Sin(a)*.85f)); }
            for(int corner=0;corner<2;corner++) for(int i=0;i<=12;i++)
            {
                float a=(180+corner*90+i*90f/12)*Mathf.Deg2Rad;
                outline.Add(new Vector2((corner==0?-1:1)*(width*.5f-radius)+Mathf.Cos(a)*radius,-height*.5f+radius+Mathf.Sin(a)*radius));
            }
        }
        else for(int corner=0;corner<4;corner++) for(int i=0;i<=12;i++)
        {
            float a=(corner*90+i*90f/12)*Mathf.Deg2Rad;
            outline.Add(new Vector2((corner==0||corner==3?1:-1)*(width*.5f-radius)+Mathf.Cos(a)*radius,(corner<2?1:-1)*(height*.5f-radius)+Mathf.Sin(a)*radius));
        }
        var vertices=new System.Collections.Generic.List<Vector3>(); var triangles=new System.Collections.Generic.List<int>(); var uv=new System.Collections.Generic.List<Vector2>();
        int n=outline.Count; float bevel=Mathf.Min(.09f,depth*.32f);
        for(int ring=0;ring<4;ring++) foreach(var p in outline)
        {
            float inset=ring==0||ring==3?bevel:0;
            var point=new Vector3(p.x*(1-inset/(width*.5f)),p.y*(1-inset/(height*.5f)),ring==0?-depth*.5f:ring==1?-depth*.5f+bevel:ring==2?depth*.5f-bevel:depth*.5f);
            vertices.Add(point); uv.Add(new Vector2(point.x,point.y)*.2f);
        }
        for(int ring=0;ring<3;ring++) for(int i=0;i<n;i++)
        {
            int a=ring*n+i,b=ring*n+(i+1)%n,c=b+n,d=a+n;
            triangles.AddRange(new[]{a,b,c,a,c,d});
        }
        for(int side=0;side<2;side++)
        {
            int center=vertices.Count; float z=(side==0?-1:1)*depth*.5f; vertices.Add(new Vector3(0,0,z)); uv.Add(Vector2.zero);
            foreach(var p in outline) { vertices.Add(new Vector3(p.x*(1-bevel/(width*.5f)),p.y*(1-bevel/(height*.5f)),z)); uv.Add(p*.2f); }
            for(int i=0;i<n;i++) { int a=center+1+i,b=center+1+(i+1)%n; triangles.AddRange(side==0?new[]{center,b,a}:new[]{center,a,b}); }
        }
        var mesh=new Mesh { name="Podium_"+name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.SetUVs(0,uv); mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        mesh=ToyBoxGeometry.Save(mesh,Root+"/Meshes/Podium_"+name+".asset");
        return MeshObject(name,parent,position,mesh,material);
    }
    static TMP_Text ResultText(string name,Transform parent,Vector3 position,Vector2 size,TMP_FontAsset font,float fontSize,Color color,TextAlignmentOptions alignment)
    {
        var obj=Group(name,parent,position).gameObject;
        var text=obj.AddComponent<TextMeshPro>(); text.font=font; text.fontSize=fontSize; text.color=color;
        text.rectTransform.sizeDelta=size; text.alignment=alignment; text.richText=false; text.enableWordWrapping=false;
        text.overflowMode=TextOverflowModes.Ellipsis; text.text="";
        if(name!="TotalValue")
        {
            var label=obj.AddComponent<ToyBoxPodiumLabel>(); label.source=text; label.height=fontSize*.087f;
            label.characters="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-."; label.glyphs=new Mesh[label.characters.Length];
            for(int i=0;i<label.characters.Length;i++)
            {
                if(label.characters[i]!='.') label.glyphs[i]=ToyBoxGeometry.Text(label.characters[i].ToString(),.16f,.012f,.025f);
                else label.glyphs[i]=ToyBoxGeometry.RoundedBox(new Vector3(.13f,.13f,.12f),.04f);
            }
            label.places=new MeshFilter[52];
            for(int i=0;i<label.places.Length;i++)
            {
                label.places[i]=MeshObject("SavedGlyph"+i,obj.transform,Vector3.zero,label.glyphs[0],ink).GetComponent<MeshFilter>();
                label.places[i].gameObject.SetActive(false);
            }
        }
        return text;
    }
}
