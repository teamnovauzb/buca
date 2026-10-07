#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HoneyPolishAuthoring
{
    static Transform root; static Mesh sphere;
    static GameObject Part(string name,Mesh mesh,Material mat,Vector3 pos,Vector3 scale)
    {
        var t=root.Find(name);var g=t?t.gameObject:new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.identity;g.transform.localScale=scale;
        g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=mat;return g;
    }
    static Material Mat(string name,Color color,float smooth)
    {
        string path="Assets/Materials/TrainLeaderboard3D/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Edit mode only");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");var b=Object.FindFirstObjectByType<HoneyTutorialBoard>();root=b.stage.transform;
        ToyBoxGeometry.Initialize();var primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
        var amber=Mat("HoneySculptAmber",new Color(1,.51f,.025f),.92f);var cream=Mat("HoneyBeeCream",new Color(1,.91f,.66f),.45f);var navy=Mat("HoneyBeeInk",new Color(.025f,.10f,.12f),.5f);
        foreach(Transform t in root)if(t.name.StartsWith("HoneyGloss")||t.name.StartsWith("CellRim"))t.gameObject.SetActive(false);
        b.caption.text="HONEY SLOWS YOU";b.caption.fontSize=3.3f;b.caption.transform.localPosition=new Vector3(.45f,1.98f,-.7f);
        for(int lane=0;lane<2;lane++)
        {
            float y=lane==0?.7f:-1.05f;
            root.Find("LaneRim"+lane).GetComponent<MeshFilter>().sharedMesh=ToyBoxGeometry.RoundedBox(new Vector3(7.45f,1.48f,.46f),.17f);
            root.Find("LaneRim"+lane).localPosition=new Vector3(0,y,-.33f);
            // Low, connected rounded lobes make a sculpted sticky puddle, with a raised meniscus.
            Part("HoneyPool"+lane,sphere,amber,new Vector3(0,y,-.71f),new Vector3(1.50f,.93f,.16f));
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;Part("HoneyLobe"+lane+i,sphere,amber,new Vector3(Mathf.Cos(a)*.52f,y+Mathf.Sin(a)*.28f,-.70f),new Vector3(.53f,.43f,.14f));
            }
            for(int i=0;i<3;i++)Part("HoneyBubble"+lane+i,sphere,b.lit,new Vector3(-.35f+i*.32f,y+.18f,-.81f),Vector3.one*(.075f+i*.015f));
            var text=root.Find("Result"+lane).GetComponent<TMPro.TMP_Text>();text.text=lane==0?"SOFT":"STRONG";text.fontSize=2.2f;
            // Inlaid guide dots draw the eye from puck to target without competing with the moving puck.
            for(int i=0;i<7;i++)
            {float x=-2.65f+i*.77f;if(Mathf.Abs(x)<.9f)continue;Part("GuideInset"+lane+i,sphere,cream,new Vector3(x,y,-.675f),new Vector3(.065f,.065f,.018f));}
            var rim=root.Find(lane==0?"GoalRim":"GoalRim2");rim.GetComponent<Renderer>().sharedMaterial=b.lit;
        }
        // A little painted wooden bee, with eyes and cream wings, beside the heading.
        Part("BeeBody",sphere,b.lit,new Vector3(-2.65f,1.98f,-.86f),new Vector3(.65f,.40f,.35f));
        for(int i=0;i<2;i++)Part("BeeStripe"+i,sphere,navy,new Vector3(-2.8f+i*.2f,1.98f,-.88f),new Vector3(.10f,.40f,.35f));
        Part("BeeHead",sphere,navy,new Vector3(-2.28f,1.99f,-.89f),new Vector3(.30f,.30f,.30f));
        Part("BeeEye",sphere,cream,new Vector3(-2.20f,2.05f,-1.02f),Vector3.one*.075f);
        for(int i=0;i<2;i++)Part("BeeWing"+i,sphere,cream,new Vector3(-2.68f+i*.2f,2.24f,-.80f),new Vector3(.25f,.40f,.10f));
        var cam=root.GetComponentInChildren<Camera>(true);cam.transform.localPosition=new Vector3(0,4.6f,-15);cam.transform.LookAt(root.TransformPoint(new Vector3(0,0,-.3f)));cam.orthographicSize=3.12f;
        var light=root.Find("SoftLight").GetComponent<Light>();light.transform.localPosition=new Vector3(-3,5,-6);light.intensity=6;light.shadows=LightShadows.Soft;
        var fill=root.Find("HoneyFill");if(!fill){var g=new GameObject("HoneyFill",typeof(Light));g.transform.SetParent(root,false);fill=g.transform;}
        fill.localPosition=new Vector3(4,0,-5);var f=fill.GetComponent<Light>();f.type=LightType.Point;f.range=16;f.intensity=2;f.cullingMask=1<<30;
        b.card.sizeDelta=new Vector2(1450,755);b.backdrop.color=new Color(.85f,.85f,.85f,1);
        EditorUtility.SetDirty(b);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Sculpted honey, tilted 3D trays, wooden bee and lighting saved";
    }
}
#endif
