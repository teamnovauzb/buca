#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
public static class HoneyMiniatureAuthoring
{
    static Transform root;
    static GameObject Part(string n,Mesh mesh,Material mat,Vector3 p)
    {var t=root.Find(n);var g=t?t.gameObject:new GameObject(n,typeof(MeshFilter),typeof(MeshRenderer));g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=mat;g.SetActive(true);return g;}
    static Material Mat(string n,Color c,float smooth)
    {string p="Assets/Materials/TrainLeaderboard3D/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.color=c;m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
    static Mesh Puddle()
    {
        var v=new List<Vector3>{new Vector3(0,0,-.16f)};var tri=new List<int>();var uv=new List<Vector2>{new Vector2(.5f,.5f)};const int N=96;const int R=8;
        for(int r=1;r<=R;r++)for(int i=0;i<N;i++)
        {float a=i*Mathf.PI*2/N;float w=1+.035f*Mathf.Sin(a*5)+.022f*Mathf.Cos(a*9);float f=r/(float)R;float x=Mathf.Cos(a)*1.6f*w*f,y=Mathf.Sin(a)*1.12f*w*f;v.Add(new Vector3(x,y,-.16f*Mathf.Sqrt(Mathf.Clamp01(1-Mathf.Pow(f,10)))));uv.Add(new Vector2(x/3.5f+.5f,y/2.5f+.5f));}
        for(int i=0;i<N;i++){tri.Add(0);tri.Add(1+(i+1)%N);tri.Add(1+i);}
        for(int r=1;r<R;r++)for(int i=0;i<N;i++){int a=1+(r-1)*N+i,b=1+(r-1)*N+(i+1)%N,c=1+r*N+i,d=1+r*N+(i+1)%N;tri.AddRange(new[]{a,b,c,b,d,c});}
        var colors=new List<Color>{new Color(0,0,0,1)};for(int r=1;r<=R;r++)for(int i=0;i<N;i++)colors.Add(new Color(r/(float)R,0,0,1));var mesh=new Mesh();mesh.SetVertices(v);mesh.SetColors(colors);mesh.SetTriangles(tri,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return ToyBoxGeometry.Save(mesh,"Assets/ToyBoxMenu/Meshes/HoneyLiquidPuddleSculpted.asset");
    }
    static Mesh LiquidEdge()
    {
        var v=new List<Vector3>();var t=new List<int>();const int count=144, sides=8;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,w=1+.035f*Mathf.Sin(a*5)+.022f*Mathf.Cos(a*9);
            var p=new Vector3(Mathf.Cos(a)*1.6f*w,Mathf.Sin(a)*1.12f*w,0);
            var outward=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            for(int j=0;j<sides;j++){float b=j*Mathf.PI*2/sides;v.Add(p+outward*(Mathf.Cos(b)*.015f)+Vector3.forward*(Mathf.Sin(b)*.012f-.006f));}
        }
        for(int i=0;i<count;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=((i+1)%count)*sides+j,d=((i+1)%count)*sides+(j+1)%sides;t.AddRange(new[]{a,c,b,b,c,d});}
        var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();return ToyBoxGeometry.Save(m,"Assets/ToyBoxMenu/Meshes/HoneyLiquidEdge.asset");
    }
    static TMPro.TMP_Text Label(string n,string text,Vector3 pos,float size,Vector2 bounds,Material unused,TMPro.TMP_FontAsset font)
    {var t=root.Find(n);var g=t?t.gameObject:new GameObject(n,typeof(TMPro.TextMeshPro));g.SetActive(true);g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=pos;var label=g.GetComponent<TMPro.TMP_Text>();label.font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Fonts & Materials/Roboto-Bold SDF.asset");label.fontStyle=TMPro.FontStyles.Bold;label.text=text;label.fontSize=size;label.color=new Color(.035f,.12f,.15f);label.alignment=TMPro.TextAlignmentOptions.Center;label.rectTransform.sizeDelta=bounds;return label;}
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode only");var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");var b=Object.FindFirstObjectByType<HoneyTutorialBoard>();var stage=b.stage.transform;ToyBoxGeometry.Initialize();
        foreach(Transform t in stage)if(t.GetComponent<Renderer>()!=null && t!=b.demoPuck.transform)t.gameObject.SetActive(false);
        b.secondPuck.gameObject.SetActive(false);b.secondHoney.gameObject.SetActive(false);
        root=stage.Find("MiniatureBoard");if(!root){root=new GameObject("MiniatureBoard").transform;root.SetParent(stage,false);}
        foreach(Transform child in root)if(child.name.StartsWith("Hex"))child.gameObject.SetActive(false);
        var maple=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TrainLeaderboard3D/CreamMaple.mat");var teal=b.dim;var gold=b.lit;
        var cream=Mat("HoneyMiniCream",new Color(1,.9f,.67f),.4f);var coral=Mat("HoneyMiniCoral",new Color(.94f,.28f,.16f),.55f);var amber=Mat("HoneyMiniLiquid",new Color(.84f,.48f,.075f),.94f);var navy=Mat("HoneyMiniHole",new Color(.005f,.025f,.03f),.2f);
        Part("Body",ToyBoxGeometry.RoundedBox(new Vector3(9.4f,4.3f,.95f),.25f),teal,new Vector3(0,0,.17f));
        Part("Maple",ToyBoxGeometry.RoundedBox(new Vector3(8.95f,3.85f,.13f),.055f),maple,new Vector3(0,0,-.36f));
        Part("BackRail",ToyBoxGeometry.RoundedBox(new Vector3(9.4f,.22f,.45f),.105f),teal,new Vector3(0,2.04f,-.42f));
        Part("FrontRail",ToyBoxGeometry.RoundedBox(new Vector3(9.4f,.28f,.45f),.13f),teal,new Vector3(0,-2.04f,-.42f));
        for(int i=0;i<2;i++)Part("SideRail"+i,ToyBoxGeometry.RoundedBox(new Vector3(.24f,4.1f,.45f),.11f),teal,new Vector3(i==0?-4.58f:4.58f,0,-.42f));
        // Transparent dielectric surface: wood remains visible through the amber liquid.
        amber.SetColor("_BaseColor",new Color(1f,.64f,.18f,.62f));
        amber.SetFloat("_Surface",1);amber.SetFloat("_Blend",0);
        amber.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        amber.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        amber.SetFloat("_ZWrite",0);amber.SetFloat("_Smoothness",.97f);amber.SetFloat("_Metallic",0);
        amber.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");amber.DisableKeyword("_ALPHATEST_ON");
        amber.SetOverrideTag("RenderType","Transparent");amber.renderQueue=3000;amber.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(amber);
        amber.shader=Shader.Find("Universal Render Pipeline/Lit");amber.SetColor("_BaseColor",new Color(.92f,.43f,.035f,.88f));amber.SetFloat("_Smoothness",.96f);EditorUtility.SetDirty(amber);
        var liquid=Part("Liquid",Puddle(),amber,new Vector3(0,0,-.435f));
        liquid.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var oldEdge=root.Find("LiquidMeniscus");if(oldEdge)oldEdge.gameObject.SetActive(false);
        var bubbleTemp=GameObject.CreatePrimitive(PrimitiveType.Sphere);var bubbleMesh=bubbleTemp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(bubbleTemp);
        for(int i=0;i<4;i++)
        {
            var bubble=Part("TrappedBubble"+i,bubbleMesh,amber,new Vector3(-.8f+i*.48f,.4f+Mathf.Sin(i*2.1f)*.18f,-.473f));
            float size=.028f+i*.008f;bubble.transform.localScale=new Vector3(size,size,size*.32f);bubble.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // A recognisable wooden honey dipper stays clear of the shot path.
        var dipperWood=Mat("HoneyDipperMaple",new Color(.69f,.39f,.13f),.35f);
        var handle=Part("DipperHandle",ToyBoxGeometry.Disc(.065f,1.1f,.035f),dipperWood,new Vector3(-2.5f,1.12f,-.58f));handle.transform.localRotation=Quaternion.Euler(0,0,-55);
        for(int i=0;i<5;i++)
        {
            var ring=Part("DipperGroove"+i,ToyBoxGeometry.Disc(.22f,.075f,.025f),dipperWood,new Vector3(-1.98f+i*.082f,1.48f+i*.057f,-.66f));ring.transform.localRotation=Quaternion.Euler(0,0,-55);
        }
        var coating=Mat("HoneyDipperCoating",new Color(.94f,.48f,.035f),.96f);
        for(int i=0;i<3;i++)
        {
            var drop=Part("HoneyDipperCoat"+i,bubbleMesh,coating,new Vector3(-1.85f+i*.075f,1.51f+i*.06f,-.78f));
            drop.transform.localScale=new Vector3(.18f,.23f,.17f);
        }
        var drip=Part("HoneyHangingDrip",bubbleMesh,coating,new Vector3(-1.65f,1.35f,-.73f));drip.transform.localScale=new Vector3(.12f,.40f,.12f);
        var bead=Part("HoneyDripBead",bubbleMesh,coating,new Vector3(-1.65f,1.16f,-.69f));bead.transform.localScale=new Vector3(.19f,.22f,.16f);
        // Fine honeycomb engraving follows the surface, leaving a smooth liquid edge.
        var engraving=Mat("HoneyMiniEngraving",new Color(1,.71f,.26f),.7f);
        for(int row=-2;row<=2;row++)for(int col=-3;col<=3;col++)
        {float x=col*.36f,y=row*.416f+(col%2)*.208f;if(x*x/1.8f+y*y/.7f>.85f)continue;var g=root.Find("Hex"+row+"_"+col);var go=g?g.gameObject:new GameObject("Hex"+row+"_"+col,typeof(LineRenderer));go.SetActive(false);go.layer=30;go.transform.SetParent(root,false);var line=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=6;line.widthMultiplier=.009f;line.sharedMaterial=engraving;for(int k=0;k<6;k++){float a=k*Mathf.PI/3;line.SetPosition(k,new Vector3(x+Mathf.Cos(a)*.22f,y+Mathf.Sin(a)*.22f,-.485f));}}
        var arrowRoot=root.Find("AimGuide");if(!arrowRoot){arrowRoot=new GameObject("AimGuide").transform;arrowRoot.SetParent(root,false);}
        b.aimGuide=arrowRoot.gameObject;
        var arrowMat=Mat("HoneyAimCream",new Color(1,.92f,.65f),0);arrowMat.shader=Shader.Find("Universal Render Pipeline/Unlit");arrowMat.SetColor("_BaseColor",new Color(1,.92f,.65f));EditorUtility.SetDirty(arrowMat);
        for(int piece=0;piece<3;piece++)
        {
            var t=arrowRoot.Find("Line"+piece);var g=t?t.gameObject:new GameObject("Line"+piece,typeof(LineRenderer));g.layer=30;g.transform.SetParent(arrowRoot,false);var line=g.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;line.widthMultiplier=.055f;line.sharedMaterial=arrowMat;line.numCapVertices=5;
            line.SetPosition(0,piece==0?new Vector3(-2.8f,0,-.53f):new Vector3(2.8f,0,-.53f));line.SetPosition(1,piece==0?new Vector3(2.8f,0,-.53f):new Vector3(2.5f,piece==1?.17f:-.17f,-.53f));
        }
        var fill=stage.Find("HoneyFill").GetComponent<Light>();fill.transform.localPosition=new Vector3(1,-5,-5);fill.intensity=4;fill.range=20;
        var hole=Part("GoalWell",ToyBoxGeometry.Disc(.46f,.03f,.01f),navy,new Vector3(3.15f,0,-.447f));hole.transform.localRotation=Quaternion.Euler(90,0,0);
        var rim=Part("GoalLip",ToyBoxGeometry.Torus(.48f,.07f),teal,new Vector3(3.15f,0,-.49f));rim.transform.localRotation=Quaternion.Euler(90,0,0);
        Part("ShootMount",ToyBoxGeometry.RoundedBox(new Vector3(1.75f,1.5f,.3f),.14f),teal,new Vector3(2.55f,-2.7f,-.25f));
        var button=Part("ShootButton",ToyBoxGeometry.Disc(.47f,.18f,.06f),coral,new Vector3(2.55f,-2.85f,-.54f));button.transform.localRotation=Quaternion.Euler(90,0,0);b.shootButton=button.transform;
        b.chargeDots=new Renderer[12];for(int i=0;i<12;i++){float a=Mathf.Lerp(15,165,i/11f)*Mathf.Deg2Rad;var seg=Part("ChargeSegment"+i,ToyBoxGeometry.RoundedBox(new Vector3(.13f,.13f,.09f),.04f),cream,new Vector3(2.55f+Mathf.Cos(a)*.64f,-2.85f+Mathf.Sin(a)*.64f,-.50f));b.chargeDots[i]=seg.GetComponent<Renderer>();}
        Part("CaptionPlaque",ToyBoxGeometry.RoundedBox(new Vector3(5.6f,.7f,.18f),.085f),cream,new Vector3(0,2.72f,-.42f));
        b.caption.gameObject.SetActive(false);b.caption=Label("Instruction","HOLD, THEN RELEASE",new Vector3(0,2.72f,-.53f),3.4f,new Vector2(5.4f,.6f),null,b.caption.font);
        var brand=Label("Brand","BUCA",new Vector3(-.3f,-2.16f,.16f),3.8f,new Vector2(2,.5f),null,b.caption.font);brand.color=new Color(1,.84f,.52f);brand.transform.localRotation=Quaternion.Euler(-90,0,0);
        b.demoPuck.gameObject.SetActive(true);b.demoPuck.transform.localPosition=new Vector3(-3.2f,0,-.70f);b.demoPuck.transform.localScale=Vector3.one;
        b.honey.gameObject.SetActive(true);b.honey.transform.localPosition=new Vector3(0,0,-.6f);b.honey.GetComponent<BoxCollider>().size=new Vector3(2.7f,2.3f,1);
        var cam=stage.GetComponentInChildren<Camera>(true);cam.transform.localPosition=new Vector3(1.8f,-11f,-12);cam.transform.LookAt(stage.TransformPoint(new Vector3(0,.1f,0)));cam.orthographicSize=3.85f;
        b.card.sizeDelta=new Vector2(1500,780);b.singleBoard=true;b.stage.SetActive(false);
        EditorUtility.SetDirty(b);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Single 3D miniature board saved";
    }
}
#endif
