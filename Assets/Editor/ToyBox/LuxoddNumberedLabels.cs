#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Option B: editor-baked colour/number badges beside action labels.</summary>
public static class LuxoddNumberedLabels
{
    const string Folder = "Assets/Art/LuxoddNumberedLabels";
    const string Prefabs = "Assets/ToyBoxMenu/Prefabs/";
    const string BadgeName = "LuxoddNumberBadge";
    static readonly Color Cream = new Color(1f, .95f, .80f);
    static readonly Color Ink = new Color(.018f, .10f, .11f);
    static Material cream, ink, rim;
    static readonly Dictionary<int, Material> colors = new Dictionary<int, Material>();
    static string backup;
    static readonly List<string> report = new List<string>();

    // Luxodd DISPLAY numbers, deliberately independent of Unity's button indexes.
    public static int DisplayNumber(ArcadeInputAdapter.Button button)
    {
        switch (button)
        {
            case ArcadeInputAdapter.Button.Black: return 1;
            case ArcadeInputAdapter.Button.Red: return 2;
            case ArcadeInputAdapter.Button.Green: return 3;
            case ArcadeInputAdapter.Button.Yellow: return 4;
            case ArcadeInputAdapter.Button.Blue: return 5;
            case ArcadeInputAdapter.Button.Purple: return 6;
            case ArcadeInputAdapter.Button.Orange: return 7;
            case ArcadeInputAdapter.Button.White: return 8;
            default: throw new ArgumentOutOfRangeException(nameof(button));
        }
    }
    static Color ColorFor(int number)
    {
        return new[] { Color.clear, new Color(.025f,.032f,.035f), new Color(.77f,.035f,.025f),
            new Color(.035f,.46f,.13f), new Color(.95f,.72f,.08f), new Color(.035f,.18f,.80f),
            new Color(.48f,.12f,.74f), new Color(.95f,.34f,.025f), new Color(.97f,.95f,.87f) }[number];
    }
    static void Prepare()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art", "LuxoddNumberedLabels");
        ToyBoxGeometry.Initialize();
        cream = Material("Ivory", Cream, true); ink = Material("Ink", Ink, true);
        rim = Material("EnamelRim", new Color(.50f,.46f,.34f), false);
        colors.Clear();
        for (int n=1; n<=8; n++) colors[n] = Material("Button"+n, ColorFor(n), false);
    }
    static Material Material(string name, Color color, bool unlit)
    {
        var saved=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name+".mat");
        if (saved != null) return saved;
        var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")) {name=name};
        m.SetColor("_BaseColor",color); if(!unlit)m.SetFloat("_Smoothness",.38f);
        AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat"); return m;
    }
    static Transform Child(Transform parent, string name)
    {
        var t=parent.Find(name); if(t!=null)return t;
        t=new GameObject(name).transform; t.SetParent(parent,false); t.gameObject.layer=parent.gameObject.layer; return t;
    }
    static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 position)
    {
        var t=Child(parent,name);t.localPosition=position;
        var mf=t.GetComponent<MeshFilter>();if(mf==null)mf=t.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=mesh;
        var mr=t.GetComponent<MeshRenderer>();if(mr==null)mr=t.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=material;
        return t.gameObject;
    }
    static void Text(Transform parent,string name,string words,Vector3 position,float height,float width,Material material)
    {
        var mesh=ToyBoxGeometry.Text(words,.08f,.014f,.028f);
        var t=Part(parent,name,mesh,material,position).transform;
        float scale=Mathf.Min(height/mesh.bounds.size.y,width/mesh.bounds.size.x);
        t.localScale=Vector3.one*scale;t.localRotation=Quaternion.identity;
        t.localPosition=position-new Vector3(mesh.bounds.center.x,mesh.bounds.center.y,0)*scale;
    }
    static void Plate(Transform parent,string name,Vector3 position,float width,float height)
    {
        var t=Child(parent,name);t.localPosition=position;
        var edge=Part(t,"TealRim",ToyBoxGeometry.RoundedBox(new Vector3(width,height,height),height*.45f),ink,Vector3.zero);
        edge.transform.localScale=new Vector3(1,1,.12f);
        var face=Part(t,"IvoryInset",ToyBoxGeometry.RoundedBox(new Vector3(width-.04f,height-.04f,height-.04f),(height-.04f)*.45f),cream,new Vector3(0,0,-height*.065f));
        face.transform.localScale=new Vector3(1,1,.10f);
    }
    static void Badge(Transform parent,int number,Vector3 position,float diameter)
    {
        var t=Child(parent,BadgeName);t.localPosition=position;t.localRotation=Quaternion.identity;t.localScale=Vector3.one;
        foreach (var item in new[] {("InsetRim", diameter*.5f, .012f, rim), ("ColoredFace", diameter*.447f, -.005f, colors[number])})
        {
            var disc=Part(t,item.Item1,ToyBoxGeometry.Disc(item.Item2,diameter*.095f,diameter*.03f),item.Item4,new Vector3(0,0,item.Item3));
            disc.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        Text(t,"Number",number.ToString(),new Vector3(0,0,-diameter*.075f),diameter*.64f,diameter*.65f,number==4||number==8?ink:cream);
    }
    static void Refit(Transform text,Vector3 position,float height,float width)
    {
        if(text==null)throw new InvalidOperationException("Expected an existing action label.");
        var b=text.GetComponent<MeshFilter>().sharedMesh.bounds;
        float s=Mathf.Min(height/b.size.y,width/b.size.x);
        text.localRotation=Quaternion.identity;text.localScale=Vector3.one*s;
        text.localPosition=position-new Vector3(b.center.x,b.center.y,0)*s;
    }
    public static void Gameplay(Transform hints)
    {
        Prepare();
        var cards=hints.Find("ControlInstructionCards");
        if(cards==null)throw new InvalidOperationException("Saved gameplay instruction plaques are missing.");
        foreach(var item in new[]{("SHOOT",1,"HOLD THEN RELEASE"),("UNDO",3,"LAST SHOT")})
        {
            var card=cards.Find(item.Item1+"Instructions");
            Refit(card.Find(item.Item1),new Vector3(.20f,.084f,-.061f),.124f,1.22f);
            Refit(card.Find(item.Item3),new Vector3(.20f,-.086f,-.061f),.086f,1.22f);
            Badge(card,item.Item2,new Vector3(-.647f,0,-.055f),.335f);
        }
    }
    static void Tutorial(GameObject root)
    {
        foreach(var face in root.GetComponentsInChildren<ArcadeButtonFace>(true))
        {
            var button=face.GetComponentInParent<UnityEngine.UI.Button>();if(button==null)continue;
            var label=button.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="Label");
            var plate=button.transform.Find("WoodenLabelPlate") as RectTransform;
            if(plate==null)continue;
            UiBadge(plate,DisplayNumber(face.arcadeButton),32);
            // Preserve the raised plaque's vertical placement and its runtime label references.
            label.rectTransform.anchorMin=new Vector2(0,label.rectTransform.anchorMin.y);
            label.rectTransform.anchorMax=new Vector2(1,label.rectTransform.anchorMax.y);
            label.rectTransform.offsetMin=new Vector2(44,label.rectTransform.offsetMin.y);
            label.rectTransform.offsetMax=new Vector2(-8,label.rectTransform.offsetMax.y);
            label.margin=Vector4.zero;label.characterSpacing=0;label.fontStyle=FontStyles.Bold;
            label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=20;
        }
    }
    static Sprite Circle()
    {
        string path=Folder+"/BadgeCircle.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(saved!=null)return saved;
        const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="BadgeCircleTexture",filterMode=FilterMode.Bilinear};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {float d=new Vector2(x+.5f-size*.5f,y+.5f-size*.5f).magnitude;pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(size*.5f-1-d));}
        texture.SetPixels(pixels);texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);sprite.name="BadgeCircle";
        AssetDatabase.CreateAsset(sprite,path);AssetDatabase.AddObjectToAsset(texture,sprite);return sprite;
    }
    static RectTransform Rect(Transform parent,string name)
    {
        var old=parent.Find(name) as RectTransform;if(old!=null)return old;
        var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);return (RectTransform)go.transform;
    }
    static Sprite NumberSprite(int number)
    {
        string path=Folder+"/Numeral"+number+".asset";
        var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(saved!=null)return saved;
        // Bake the same solid rounded numeral into a UI sprite. No additional TMP
        // label is introduced, so existing GetComponentInChildren label bindings stay valid.
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var rt=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);
        try
        {
            var host=new GameObject("NumeralPreview");SceneManager.MoveGameObjectToScene(host,scene);host.layer=31;
            Text(host.transform,"Number",number.ToString(),Vector3.zero,.80f,.65f,cream);
            var cameraObject=new GameObject("NumeralCamera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=.5f;
            camera.transform.position=new Vector3(0,0,-3);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.cullingMask=1<<31;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(128,128,TextureFormat.RGBA32,false){name="Numeral"+number+"Texture",filterMode=FilterMode.Bilinear};
            texture.ReadPixels(new Rect(0,0,128,128),0,0);texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,128,128),new Vector2(.5f,.5f),100);sprite.name="Numeral"+number;
            AssetDatabase.CreateAsset(sprite,path);AssetDatabase.AddObjectToAsset(texture,sprite);return sprite;
        }
        finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void UiBadge(RectTransform plate,int number,float diameter)
    {
        var badge=Rect(plate,BadgeName);badge.anchorMin=badge.anchorMax=new Vector2(0,.5f);badge.pivot=new Vector2(.5f,.5f);
        badge.anchoredPosition=new Vector2(24,1);badge.sizeDelta=Vector2.one*diameter;badge.SetAsLastSibling();
        var border=badge.GetComponent<UnityEngine.UI.Image>();if(border==null)border=badge.gameObject.AddComponent<UnityEngine.UI.Image>();
        border.sprite=Circle();border.color=new Color(.53f,.46f,.31f);border.raycastTarget=false;
        var face=Rect(badge,"ColoredFace");face.anchorMin=Vector2.zero;face.anchorMax=Vector2.one;face.offsetMin=Vector2.one*2;face.offsetMax=-Vector2.one*2;
        var img=face.GetComponent<UnityEngine.UI.Image>();if(img==null)img=face.gameObject.AddComponent<UnityEngine.UI.Image>();img.sprite=Circle();img.color=ColorFor(number);img.raycastTarget=false;
        var digit=Rect(badge,"Number");digit.anchorMin=Vector2.zero;digit.anchorMax=Vector2.one;digit.offsetMin=digit.offsetMax=Vector2.zero;
        var numeral=digit.GetComponent<UnityEngine.UI.Image>();if(numeral==null)numeral=digit.gameObject.AddComponent<UnityEngine.UI.Image>();
        numeral.sprite=NumberSprite(number);numeral.color=number==4||number==8?Ink:Color.white;numeral.raycastTarget=false;
    }
    static void Edit(string path,Action<GameObject> action)
    {
        if(!File.Exists(path))throw new FileNotFoundException(path);
        var destination=Path.Combine(backup,path);Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if(!File.Exists(destination))File.Copy(path,destination);
        var root=PrefabUtility.LoadPrefabContents(path);
        try{action(root);PrefabUtility.SaveAsPrefabAsset(root,path);report.Add(path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    [MenuItem("RealBuca/Controls/Apply Luxodd Number Labels - Option B")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        backup="output/controls/backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");report.Clear();Prepare();
        Edit(Prefabs+"SolidGameplayHud.prefab",r=>Gameplay(r.transform.Find("PhysicalControlHints")));
        Edit("Assets/Resources/Tutorials/BoardCoachTutorial.prefab",Tutorial);
        Edit(Prefabs+"ResultsAndLeaderboard.prefab",Results);
        Edit("Assets/ToyBoxMenu/BestShotReplay/WoodenReplayFrame.prefab",Replay);
        Edit(Prefabs+"ToyBoxMainMenu.prefab",Menus);
        Edit("Assets/Prefabs/Prebuilt/ReturnPlayerPrompt.prefab",r=>UiActions(r,false));
        Edit("Assets/Prefabs/Prebuilt/LevelFailedPanel.prefab",r=>UiActions(r,true));
        Edit("Assets/Resources/Tutorials/HoneyComparisonTutorial.prefab",r=>UiActions(r,false));
        AssetDatabase.SaveAssets();Directory.CreateDirectory("output/controls");
        File.WriteAllText("output/controls/numbered-labels-report.txt",string.Join("\n",report)+"\nBackup: "+backup);
        return "Saved Option B labels. "+string.Join(", ",report);
    }
    static void UiActions(GameObject root,bool exitButton)
    {
        foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            var label=button.GetComponentInChildren<TMP_Text>(true);if(label==null)continue;
            UiBadge((RectTransform)button.transform,exitButton&&button.name=="ExitButton"?2:1,32);
            var rect=label.rectTransform;rect.offsetMin=new Vector2(Mathf.Max(44,rect.offsetMin.x),rect.offsetMin.y);
            label.enableAutoSizing=true;label.fontSizeMin=Mathf.Min(label.fontSizeMin,16);
        }
    }
    static void Results(GameObject root)
    {
        var view=root.GetComponent<ToyBoxResults3D>();
        var stage=view.presentation.transform;
        var shortcut=stage.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="GREEN - MY SHOTS - BACK");
        if(shortcut!=null)
        {
            var holder=view.scorecardShortcut.transform;
            var buttonPosition=view.scorecardHit.transform.localPosition;buttonPosition.y=5.58f;
            view.scorecardHit.transform.localPosition=buttonPosition;
            var label=BelowLabel(holder,new Vector3(0,5.02f,-.74f),6.2f,.43f);
            shortcut.SetParent(label,false);
            shortcut.GetComponent<MeshFilter>().sharedMesh=ToyBoxGeometry.Text("MY SHOTS",.08f,.014f,.028f);
            Refit(shortcut,new Vector3(.20f,0,-.06f),.23f,4.6f);
            MoveBadge(holder,label,3,new Vector3(-2.77f,0,-.06f),.32f);
        }
        var back=stage.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="BLACK - BACK TO RESULT");
        if(back!=null)
        {
            back.GetComponent<MeshFilter>().sharedMesh=ToyBoxGeometry.Text("BACK TO RESULT",.08f,.014f,.028f);
            Refit(back,new Vector3(.24f,-4.03f,-.26f),.22f,4.3f);
            Badge(back.parent,1,new Vector3(-2.27f,-4.03f,-.26f),.34f);
        }
        // Keep the saved action text and live countdown references, placing both
        // on a separate plaque below the pressable cap.
        var nextLabel=BelowLabel(view.nextRoot,new Vector3(0,.20f,-1.02f),4.9f,.54f);
        nextLabel.localRotation=Quaternion.Euler(90,0,0);
        var next=view.nextRoot.GetComponentsInChildren<Transform>(true).First(t=>t.name=="NEXT");
        next.SetParent(nextLabel,false);Refit(next,new Vector3(-.30f,0,-.065f),.31f,1.65f);
        var countdown=view.countdownText.transform.parent;countdown.SetParent(nextLabel,false);
        countdown.localPosition=new Vector3(1.18f,0,-.065f);countdown.localRotation=Quaternion.identity;
        MoveBadge(view.nextCap,nextLabel,1,new Vector3(-2.08f,0,-.06f),.40f);
        var well=root.GetComponentInChildren<ToyBoxWellDone3D>(true);
        if(well!=null)
        {
            var label=BelowLabel(well.nextButton,new Vector3(0,-1.04f,-.55f),4.9f,.54f);
            var letters=well.nextButton.GetComponentsInChildren<Transform>(true).First(t=>t.name=="NextLetters");
            letters.SetParent(label,false);Refit(letters,new Vector3(.20f,0,-.065f),.31f,3.85f);
            letters.GetComponent<MeshRenderer>().sharedMaterial=ink;
            MoveBadge(well.nextCap,label,1,new Vector3(-2.08f,0,-.06f),.40f);
        }
    }
    static Transform BelowLabel(Transform parent,Vector3 position,float width,float height)
    {
        var label=Child(parent,"BelowButtonLabel");label.localPosition=position;label.localRotation=Quaternion.identity;
        Plate(label,"Nameplate",Vector3.zero,width,height);return label;
    }
    static void MoveBadge(Transform oldParent,Transform label,int number,Vector3 position,float diameter)
    {
        var badge=oldParent.Find(BadgeName);
        if(badge!=null && label.Find(BadgeName)==null)badge.SetParent(label,false);
        Badge(label,number,position,diameter);
    }
    static void Replay(GameObject root)
    {
        var plate=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SkipNameplate");
        var skip=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="BlackArcadeSkip");
        skip.localPosition=new Vector3(6.32f,-3.14f,-.77f);skip.localScale=Vector3.one*.74f;
        plate.localPosition=new Vector3(6.32f,-3.90f,-.75f);
        plate.Find("SkipBrassBorder").localScale=new Vector3(1,.60f,1);
        plate.Find("SkipIvoryFace").localScale=new Vector3(1,.60f,1);
        Refit(plate.Find("SkipLetters"),new Vector3(.20f,0,-.23f),.24f,.99f);
        Badge(plate,1,new Vector3(-.59f,0,-.22f),.30f);
    }
    static void MenuButton(ToyBoxMenuController.PuckButton button,string words,int number)
    {
        if(button==null||button.cap==null)return;
        var text=button.cap.Find(words);if(text==null)return;
        // One cream plaque holds both the display number and action, like the gameplay labels.
        var plate=Child(button.cap,"NumberedActionPlaque");PositionBelowButton(button,plate);
        Plate(plate,"Nameplate",Vector3.zero,1.70f,.51f);
        Badge(plate,number,new Vector3(-.61f,0,-.066f),.34f);
        text.gameObject.SetActive(false);
        Text(plate,"Action",words,new Vector3(.20f,0,-.069f),.20f,1.13f,ink);
    }
    static void PositionBelowButton(ToyBoxMenuController.PuckButton button,Transform plate)
    {
        var socket=button.cap.parent.Find("CobaltSocket");
        if(socket==null)throw new InvalidOperationException("Expected the saved menu button socket.");
        float radius=socket.GetComponent<MeshFilter>().sharedMesh.bounds.extents.z*socket.localScale.z;
        // The plaque sits on the board in front of the full socket, clear of the cap.
        // Keep the existing hierarchy, collider and control bindings intact.
        plate.localPosition=new Vector3(0,.09f-button.restPosition.y,-radius-.34f);
        plate.localRotation=Quaternion.Euler(90,0,0);
    }
    static void KeepMenuLabelsWithinBoard(ToyBoxMenuController menu)
    {
        // Reserve room for the plaque before the level tray's front rail.
        var back=menu.backButton.cap.parent;
        var position=back.localPosition;position.z=-5.50f;back.localPosition=position;
        // These three controls are mounted vertically on the preview's front apron.
        // Raise the whole mount (including its hit area) to fit the label underneath.
        foreach(var button in menu.previewButtons)
        {
            var mount=button.cap.parent.parent;
            position=mount.localPosition;position.y=-.40f;mount.localPosition=position;
        }
    }
    [MenuItem("RealBuca/Controls/Move Menu Number Labels Below Buttons")]
    public static string MoveMenuLabelsBelowButtons()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        backup="output/controls/layout-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");report.Clear();
        int count=0;
        Edit(Prefabs+"ToyBoxMainMenu.prefab",root=>
        {
            var menu=root.GetComponent<ToyBoxMenuController>();
            foreach(var button in menu.homeButtons.Concat(menu.resumeButtons).Concat(menu.previewButtons).Concat(new[]{menu.backButton}))
            {
                var plate=button.cap.Find("NumberedActionPlaque");
                if(plate==null)throw new InvalidOperationException("Expected the saved numbered action plaque.");
                PositionBelowButton(button,plate);count++;
            }
            KeepMenuLabelsWithinBoard(menu);
        });
        AssetDatabase.SaveAssets();
        return "Moved "+count+" saved menu labels below their buttons. Backup: "+backup;
    }
    static void Menus(GameObject root)
    {
        var menu=root.GetComponent<ToyBoxMenuController>();if(menu==null)menu=root.GetComponentInChildren<ToyBoxMenuController>(true);
        for(int i=0;i<menu.homeButtons.Length;i++)MenuButton(menu.homeButtons[i],new[]{"PLAY","LEVELS","SKINS"}[i],1);
        for(int i=0;i<menu.resumeButtons.Length;i++)
        {
            var b=menu.resumeButtons[i];var label=b.cap.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(t=>new[]{"LEVEL 1","START 1","CONTINUE","BACK"}.Contains(t.name));
            if(label!=null)MenuButton(b,label.name,label.name=="BACK"?2:1);
        }
        MenuButton(menu.backButton,"BACK",2);
        if(menu.previewButtons!=null)for(int i=0;i<menu.previewButtons.Length;i++)MenuButton(menu.previewButtons[i],new[]{"PREV","NEXT","BACK"}[i],i==2?2:1);
        KeepMenuLabelsWithinBoard(menu);
        WorkshopLabelsBelowButtons(menu);
    }
    static void WorkshopLabelsBelowButtons(ToyBoxMenuController menu)
    {
        var workshop=menu.paintWorkshop;
        if(workshop!=null)for(int i=5;i<=6;i++)
        {
            var cap=workshop.buttons[i].cap;string words=i==5?"BACK":"USE SKIN";
            var group=Child(cap,"NumberedActionPlaque");
            var label=cap.Find(words)??group.Find(words);
            if(label==null)throw new InvalidOperationException("Missing saved workshop action label.");
            group.localPosition=new Vector3(0,.09f-workshop.buttons[i].restPosition.y,-.95f);
            group.localRotation=Quaternion.Euler(90,0,0);
            Plate(group,"Nameplate",Vector3.zero,3.70f,.52f);
            // The workshop is a nested prefab: preserve its original hierarchy
            // and bake the relocated lettering as an added child, as on the menu.
            label.gameObject.SetActive(false);
            Text(group,"Action",words,new Vector3(.21f,0,-.066f),.28f,2.70f,ink);
            Badge(group,i==5?2:1,new Vector3(-1.51f,0,-.06f),.39f);
        }
        if(workshop!=null)for(int i=0;i<workshop.framingPoints.Length;i++)
            if(workshop.framingPoints[i].z < -6f)workshop.framingPoints[i].z=-6.90f;
    }
    [MenuItem("RealBuca/Controls/Move Other Screen Labels Below Buttons")]
    public static string MoveOtherScreenLabelsBelowButtons()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        backup="output/controls/other-screens-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");report.Clear();Prepare();
        Edit(Prefabs+"ToyBoxMainMenu.prefab",r=>WorkshopLabelsBelowButtons(r.GetComponent<ToyBoxMenuController>()));
        Edit(Prefabs+"ResultsAndLeaderboard.prefab",Results);
        Edit("Assets/ToyBoxMenu/BestShotReplay/WoodenReplayFrame.prefab",Replay);
        AssetDatabase.SaveAssets();
        return "Saved separate labels for skin actions, result actions and replay Skip. Backup: "+backup;
    }
    public static string Preview(string page)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Clean edit mode required for previews.");
        string restore=SceneManager.GetActiveScene().path;
        var scene=EditorSceneManager.OpenScene(page=="Game"||page=="Tutorial"||page=="Results"?"Assets/Scenes/Game.unity":"Assets/Scenes/MainMenu.unity");
        try
        {
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            if(page=="Game"||page=="Tutorial"||page=="Results")
            {
                var manager=Object.FindAnyObjectByType<LevelManager>();
                PrefabUtility.InstantiatePrefab(manager.levelPrefabs[0],scene);
                var camera=Camera.main;camera.aspect=16f/9f;
                if(page=="Results")
                {
                    var view=manager.levelCompletePanel.premiumResults;
                    view.Show(new ScoreCalculator.ScoreBreakdown{basePoints=500,timeBonus=242,railBonus=0,strokeBonus=75,total=1226,stars=3,strokesUsed=1},()=>{});
                    view.Tick(2);Capture(view.resultsCamera,"Results",1920,1080);view.Hide(false);
                    view.Show(new ScoreCalculator.ScoreBreakdown{basePoints=500,timeBonus=100,total=840,stars=2,strokesUsed=3},()=>{});
                    view.Tick(2);Capture(view.resultsCamera,"WellDone",1920,1080);view.Hide(false);
                }
                else if(page=="Game")
                {
                    Capture(camera,"Gameplay",1920,1080);
                    var hints=Object.FindAnyObjectByType<ToyBoxGameplayHud>().hints.transform;
                    var target=hints.position+new Vector3(0,-.40f,0);
                    camera.transform.position=target+new Vector3(0,4.4f,-8);camera.transform.LookAt(target);camera.fieldOfView=38;
                    Capture(camera,"Gameplay-Closeup",1600,800);
                }
                else
                {
                    var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Tutorials/BoardCoachTutorial.prefab"),scene);
                    var view=root.GetComponent<BoardCoachTutorial>();view.group.alpha=1;view.header.gameObject.SetActive(false);
                    var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                    view.caption.text="WATCH HOW THIS MECHANIC WORKS";
                    Capture(camera,"Tutorial",1920,1080);
                    view.SetPauseLabel(true);view.play.GetComponentInChildren<TMP_Text>(true).text="YOUR TURN";
                    Capture(camera,"Tutorial-Resume",1920,1080);
                }
            }
            else
            {
                var menu=Object.FindAnyObjectByType<ToyBoxMenuController>();
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var type=typeof(ToyBoxMenuController).GetNestedType("Page",System.Reflection.BindingFlags.NonPublic);
                typeof(ToyBoxMenuController).GetMethod("ShowPage",flags).Invoke(menu,new[]{Enum.Parse(type,page)});
                menu.SetClock(30);
                if(page=="Skins")menu.paintWorkshop.Fit(16f/9f);
                else if(page=="Levels")menu.FitMapCamera(16f/9f);
                else typeof(ToyBoxMenuController).GetMethod("FitCamera",flags).Invoke(menu,null);
                Capture(menu.ActiveCamera,page,1920,1080);
            }
            return "Saved preview(s) to output/controls: "+page;
        }
        finally{EditorSceneManager.OpenScene(restore);}
    }
    static void Capture(Camera camera,string name,int width,int height)
    {
        var original=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
        var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.aspect=(float)width/height;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();Directory.CreateDirectory("output/controls");File.WriteAllBytes("output/controls/"+name+".png",image.EncodeToPNG());
        }
        finally{camera.targetTexture=original;camera.aspect=aspect;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
    }
}
#endif
