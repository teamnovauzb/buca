#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static partial class BuildToyBoxMainMenu
{
    public static void BuildHangingTimeUp()
    {
        ToyBoxGeometry.Initialize();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Single);
        var panel=Object.FindAnyObjectByType<LevelFailedPanel>(FindObjectsInactive.Include);
        var old=Object.FindAnyObjectByType<HangingTimeUp3D>(FindObjectsInactive.Include);
        if(old!=null)Object.DestroyImmediate(old.gameObject);
        var host=new GameObject("SavedHangingTimeUp");
        var view=host.AddComponent<HangingTimeUp3D>();view.owner=panel;view.gameplayCamera=Camera.main;
        var stage=Group("Presentation",host.transform);view.presentation=stage.gameObject;
        var maple=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/PodiumMaple.mat");
        var navy=Material("HangingSignNavy",new Color(.085f,.145f,.18f),.26f);
        var trim=new Material(maple);trim.name="HangingMapleTrim";trim.SetColor("_BaseColor",new Color(.52f,.38f,.25f));
        maple=ToyBoxGeometry.Save(trim,Root+"/Materials/HangingMapleTrim.mat");
        var ivory=Material("HangingSignCream",new Color(.85f,.76f,.58f),.32f);
        var green=Material("HangingSignGreen",new Color(.34f,.56f,.37f),.32f);
        var red=Material("HangingSignCoral",new Color(.68f,.30f,.22f),.30f);
        var gold=Material("HangingSignGold",new Color(.84f,.57f,.19f),.28f);
        if(maple==null)throw new System.Exception("Maple material missing");
        var letteringShadow=Material("HangingLetterShadow",new Color(.012f,.022f,.027f),.12f);
        // Punctuation belongs to this sign only; preserve the game's existing glyph atlas.
        ToyBoxGeometry.Glyphs['\'']=new ToyBoxGeometry.Glyph {character="'",width=.20f,contours=new[]{new ToyBoxGeometry.Contour {points=new[]{new Vector2(.03f,.99f),new Vector2(.17f,.99f),new Vector2(.12f,.72f),new Vector2(.03f,.72f)}}}};
        ToyBoxGeometry.Glyphs['!']=new ToyBoxGeometry.Glyph {character="!",width=.24f,contours=new[]{new ToyBoxGeometry.Contour {points=new[]{new Vector2(.04f,1),new Vector2(.20f,1),new Vector2(.17f,.27f),new Vector2(.07f,.27f)}},new ToyBoxGeometry.Contour {points=new[]{new Vector2(.04f,.16f),new Vector2(.20f,.16f),new Vector2(.20f,0),new Vector2(.04f,0)}}}};
        view.header=Group("Header",stage,new Vector3(0,1.65f,0));
        SignBoard(view.header,6.5f,2.7f,maple,navy);
        foreach(int side in new[]{-1,1})
        {
            Box("Rope",view.header,new Vector3(side*2.6f,2.8f,.10f),new Vector3(.10f,3.2f,.10f),maple,.04f);
            Box("Peg",view.header,new Vector3(side*2.6f,1.07f,-.33f),Vector3.one*.26f,navy,.12f);
            Box("RopeKnot",view.header,new Vector3(side*2.6f,1.60f,.0f),Vector3.one*.27f,maple,.13f);
        }
        RaisedSignText("TIME'S UP!",view.header,new Vector3(0,.05f,-.39f),.59f,ivory,letteringShadow);
        RaisedSignText("TRY THIS LEVEL AGAIN",view.header,new Vector3(0,-.72f,-.34f),.23f,ivory,letteringShadow);
        // Small hourglass silhouette, built from simple saved solid bars.
        var hour=Group("Hourglass",view.header,new Vector3(0,.88f,-.24f));
        foreach(int y in new[]{-1,1}) Box("Cap",hour,new Vector3(0,y*.23f,0),new Vector3(.43f,.06f,.09f),gold,.02f);
        foreach(int direction in new[]{-1,1}){var bar=Box("Glass",hour,Vector3.zero,new Vector3(.055f,.53f,.08f),gold,.02f);bar.transform.localRotation=Quaternion.Euler(0,0,direction*36);}
        view.continueSign=Group("Continue",stage,new Vector3(0,-.68f,0));
        view.endSign=Group("EndGame",stage,new Vector3(0,-2.20f,0));
        view.buttons=new Collider[2];view.selectors=new GameObject[2];
        for(int i=0;i<2;i++)
        {
            var row=i==0?view.continueSign:view.endSign;
            SignBoard(row,6.0f,1.22f,maple,navy);
            foreach(int side in new[]{-1,1})Box("Link",row,new Vector3(side*2.45f,.88f,.08f),new Vector3(.075f,.7f,.075f),maple,.025f);
            var puck=Group("ActionPuck",row,new Vector3(-2.1f,0,-.30f));puck.localRotation=Quaternion.Euler(90,0,0);
            Disc("ButtonSocket",puck,new Vector3(0,.055f,0),.445f,.065f,navy,.022f);
            Disc("Puck",puck,Vector3.zero,.405f,.16f,i==0?green:red,.04f);
            // Front-facing sculpted icon, same orientation as the raised lettering.
            var icon=Group("Icon",row,new Vector3(-2.1f,0,-.415f));
            icon.localScale=Vector3.one*.72f;
            if(i==0)
            {
                var ring=MeshObject("RetryArc",icon,Vector3.zero,ToyBoxGeometry.Ring(.27f,.07f,.08f,285),ivory);ring.transform.localRotation=Quaternion.Euler(90,0,0);
                SignChevron(icon,new Vector3(.22f,.13f,-.06f),ivory);
            }
            else
            {
                foreach(int side in new[]{-1,1})Box("DoorFrame",icon,new Vector3(-.12f+side*.14f,0,0),new Vector3(.055f,.60f,.07f),ivory,.018f);
                foreach(int side in new[]{-1,1})Box("DoorFrame",icon,new Vector3(-.12f,side*.28f,0),new Vector3(.32f,.055f,.07f),ivory,.018f);
                var door=Box("OpenDoor",icon,new Vector3(-.15f,0,-.04f),new Vector3(.23f,.49f,.06f),ivory,.02f);door.transform.localRotation=Quaternion.Euler(0,-30,0);
                Box("ArrowShaft",icon,new Vector3(.14f,0,-.04f),new Vector3(.24f,.065f,.07f),ivory,.02f);
                SignChevron(icon,new Vector3(.19f,0,-.05f),ivory);
            }
            RaisedSignText(i==0?"CONTINUE":"END GAME",row,new Vector3(.57f,0,-.36f),.38f,ivory,letteringShadow);
            view.selectors[i]=Box("Selection",row,new Vector3(.58f,-.43f,-.27f),new Vector3(2.9f,.055f,.06f),gold,.02f);
            var hit=row.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,0,-.2f);hit.size=new Vector3(6,1.22f,.6f);view.buttons[i]=hit;
        }
        var light=Group("SignSoftLight",stage,new Vector3(-2,3,-3)).gameObject.AddComponent<Light>();
        light.type=LightType.Point;light.range=12;light.intensity=.18f;light.shadows=LightShadows.None;
        view.levelCapture=SaveResultTarget("HangingCapture",24);
        view.blurScratch=SaveResultTarget("HangingBlurScratch",0);
        view.blurredLevel=SaveResultTarget("HangingBlurred",0);
        view.blurMaterial=ToyBoxGeometry.Save(new Material(Shader.Find("Hidden/Buca/GaussianBlur")),Root+"/Materials/HangingBlur.mat");
        var background=new Material(Shader.Find("Buca/HangingBackdrop"));background.SetTexture("_MainTex",view.blurredLevel);
        background=ToyBoxGeometry.Save(background,Root+"/Materials/HangingBackdrop.mat");
        view.backdrop=Box("BlurredBackground",stage,new Vector3(0,0,2),new Vector3(1,1,.01f),background,.001f).transform;
        view.backdrop.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        stage.gameObject.SetActive(false);panel.hangingTimeUp=view;
        EditorUtility.SetDirty(panel);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("HANGING_TIME_UP_BAKED");
    }
    static void RaisedSignText(string copy,Transform parent,Vector3 position,float size,Material face,Material shadow)
    {
        Text(copy,parent,new Vector3(position.x+.035f,position.y-.045f,-.244f),size,shadow,false,.025f);
        Text(copy,parent,position,size,face,false,.23f,.035f,.018f);
    }
    static void SignChevron(Transform parent,Vector3 position,Material material)
    {
        foreach(int side in new[]{-1,1})
        {
            var bar=Box("Arrow",parent,position+new Vector3(0,side*.07f,0),new Vector3(.08f,.23f,.08f),material,.025f);
            bar.transform.localRotation=Quaternion.Euler(0,0,side*45);
        }
    }
    static void SignBoard(Transform parent,float width,float height,Material maple,Material navy)
    {
        Box("MapleFrame",parent,Vector3.zero,new Vector3(width,height,.32f),maple,.16f);
        Box("NavyFace",parent,new Vector3(0,0,-.18f),new Vector3(width-.36f,height-.36f,.10f),navy,.11f);
    }
}
#endif
