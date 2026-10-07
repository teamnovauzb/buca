#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
public static class BucaTrainLeaderboard3DBaker
{
    const string Dir="Assets/Materials/TrainLeaderboard3D";
    static Transform root;
    static Material Mat(string name,Color color,float metal=0,float smooth=.38f,bool wood=false)
    {
        var path=Dir+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",new Color(color.r*.50f,color.g*.50f,color.b*.50f,1));m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",.25f);
        if(wood)
        {
            var tex=new Texture2D(512,512,TextureFormat.RGB24,false);
            for(int y=0;y<512;y++)for(int x=0;x<512;x++)
            {float n=Mathf.PerlinNoise(x*.009f,y*.07f);float grain=Mathf.Sin(y*.48f+n*12f+x*.008f);float v=.94f+.04f*grain+.025f*n;tex.SetPixel(x,y,new Color(v,v,v));}
            tex.Apply();var tp=Dir+"/"+name+"Grain.asset";
            var old=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);if(old==null)AssetDatabase.CreateAsset(tex,tp);else{EditorUtility.CopySerialized(tex,old);Object.DestroyImmediate(tex);tex=old;}
            m.SetTexture("_BaseMap",tex);
        }
        EditorUtility.SetDirty(m);return m;
    }
    static Vector2[] Outline(float w,float h,float radius,bool arch)
    {
        var points=new List<Vector2>();
        for(int c=0;c<4;c++)for(int j=0;j<12;j++)
        {
            float angle=(c*90+j*90f/11)*Mathf.Deg2Rad;
            float cx=(c==0||c==3?1:-1)*(w/2-radius),cy=(c<2?1:-1)*(h/2-radius);
            var p=new Vector2(cx+Mathf.Cos(angle)*radius,cy+Mathf.Sin(angle)*radius);
            points.Add(p);
        }
        // CCW rounded outline; raise the upper edge into the reference's wooden arch.
        if(arch){for(int i=0;i<points.Count;i++){var p=points[i];if(p.y>h*.35f)p.y-=.7f;points[i]=p;}
            points.InsertRange(12,Enumerable.Range(1,25).Select(i=>{float x=w/2-radius-(w-2*radius)*i/26f;return new Vector2(x,h/2-.7f+.7f*Mathf.Pow(Mathf.Max(0,1-x*x/12f),.5f));}));}
        return points.ToArray();
    }
    static Mesh Mesh(float w,float h,float d,float radius,bool arch)
    {
        var outline=Outline(w,h,radius,arch);int n=outline.Length;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++)
        {float inset=ring==0||ring==3?.045f:0;float z=ring==0?-d/2: ring==1?-d/2+.055f:ring==2?d/2-.055f:d/2;
            var p=outline[i];vertices.Add(new Vector3(p.x*(1-inset),p.y*(1-inset),z));uv.Add(new Vector2(p.x/w+.5f,p.y/h+.5f));}
        for(int r=0;r<3;r++)for(int i=0;i<n;i++){int a=r*n+i,b=r*n+(i+1)%n;tris.AddRange(new[]{a,b,b+n,a,b+n,a+n});}
        int center=vertices.Count;vertices.Add(new Vector3(0,0,-d/2));uv.Add(new Vector2(.5f,.5f));
        for(int i=0;i<n;i++)tris.AddRange(new[]{center,(i+1)%n,i});
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();var normals=mesh.normals;for(int i=0;i<n;i++)normals[i]=Vector3.back;normals[center]=Vector3.back;mesh.normals=normals;mesh.RecalculateBounds();return mesh;
    }
    static Renderer Box(string name,Transform parent,Vector3 pos,float w,float h,float d,Material m,bool arch=false)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=31;go.transform.SetParent(parent,false);go.transform.localPosition=pos;
        var mesh=Mesh(w,h,d,.16f,arch);var path=Dir+"/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=m;return r;
    }
    static void Disc(string name,Transform parent,Vector3 pos,float diameter,float depth,Material m)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.layer=31;go.transform.SetParent(parent,false);go.transform.localPosition=pos;
        go.transform.localRotation=Quaternion.Euler(90,0,0);go.transform.localScale=new Vector3(diameter,depth/2,diameter);
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;
        var rim=GameObject.CreatePrimitive(PrimitiveType.Sphere);rim.name=name+"RoundedFace";rim.layer=31;rim.transform.SetParent(parent,false);rim.transform.localPosition=pos+Vector3.back*depth*.45f;rim.transform.localScale=new Vector3(diameter*.95f,diameter*.95f,depth*.55f);Object.DestroyImmediate(rim.GetComponent<Collider>());rim.GetComponent<Renderer>().sharedMaterial=m;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop play first");
        System.IO.Directory.CreateDirectory(Dir);
        var panel=Object.FindFirstObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);
        var old=panel.GetComponent<BucaTrainLeaderboard3D>();if(old!=null&&old.stage!=null)Object.DestroyImmediate(old.stage);
        var controller=old??panel.gameObject.AddComponent<BucaTrainLeaderboard3D>();controller.panel=panel;
        root=new GameObject("LeaderboardTrain3DStage").transform;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root.gameObject,panel.gameObject.scene);root.position=new Vector3(0,3000,0);controller.stage=root.gameObject;
        var maple=Mat("Maple",new Color(.93f,.76f,.49f),0,.33f,true);
        var face=Mat("CreamMaple",new Color(1,.88f,.66f),0,.38f,true);
        var teal=Mat("PaintedTeal",new Color(.13f,.42f,.43f),.12f,.42f,true);
        var mint=Mat("MintPlayer",new Color(.49f,.83f,.73f),.06f,.4f,true);
        var walnut=Mat("WalnutWheels",new Color(.46f,.25f,.10f),0,.35f,true);
        var gold=Mat("Gold",new Color(.96f,.67f,.24f),.58f,.48f);
        var silver=Mat("Silver",new Color(.78f,.83f,.82f),.6f,.5f);
        var bronze=Mat("Bronze",new Color(.77f,.43f,.23f),.5f,.42f);
        Box("TealArchedBoard",root,new Vector3(0,0,.35f),9.6f,11.1f,.50f,teal,true);
        Box("MapleArchedInset",root,new Vector3(0,0,0),9.22f,10.72f,.32f,maple,true);
        controller.carriages=new GameObject[5];controller.faces=new Renderer[5];controller.maple=face;controller.mint=mint;
        for(int i=0;i<5;i++)
        {
            var car=new GameObject("Carriage"+(i+1)).transform;car.SetParent(root,false);car.localPosition=new Vector3(0,2.37f-i*1.39f,-.25f);controller.carriages[i]=car.gameObject;
            Box("Coupling"+i,car,new Vector3(-2.92f,0,-.08f),.72f,.22f,.18f,bronze);
            Box("CarTrim"+i,car,new Vector3(.57f,0,-.12f),6.60f,1.08f,.32f,i==0?gold:teal);
            controller.faces[i]=Box("CarFace"+i,car,new Vector3(.57f,.025f,-.34f),6.40f,.87f,.22f,face);
            controller.faces[i].receiveShadows=false;
            Disc("Medal"+i,car,new Vector3(-3.59f,0,-.23f),1.06f,.28f,i==0?gold:i==1?silver:i==2?bronze:maple);
            foreach(float x in new[]{-2.13f,3.26f}){Disc("Wheel"+i+x,car,new Vector3(x,-.49f,-.46f),.49f,.25f,walnut);Disc("Hub"+i+x,car,new Vector3(x,-.49f,-.63f),.23f,.09f,gold);}
        }
        for(int i=0;i<4;i++)Disc("Pin"+i,root,new Vector3(i%2==0?-4.2f:4.2f,i<2?4.5f:-4.9f,-.27f),.23f,.15f,gold);
        var camGO=new GameObject("TrainCamera",typeof(Camera));camGO.transform.SetParent(root,false);camGO.transform.localPosition=new Vector3(0,0,-20);
        var cam=camGO.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=5.55f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<31;cam.nearClipPlane=.1f;cam.farClipPlane=30;
        var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(Dir+"/TrainRender.renderTexture");if(rt==null){rt=new RenderTexture(1920,2220,24,RenderTextureFormat.ARGB32);rt.antiAliasing=2;AssetDatabase.CreateAsset(rt,Dir+"/TrainRender.renderTexture");}cam.targetTexture=rt;
        var lightGO=new GameObject("WarmSoftbox",typeof(Light));lightGO.transform.SetParent(root,false);lightGO.transform.localRotation=Quaternion.Euler(25,-25,0);var light=lightGO.GetComponent<Light>();light.type=LightType.Point;lightGO.transform.localPosition=new Vector3(-4,5,-6);light.range=25;light.intensity=5f;light.color=new Color(1,.94f,.82f);light.cullingMask=1<<31;light.shadows=LightShadows.Soft;
        foreach(var image in panel.card.GetComponentsInChildren<UnityEngine.UI.Image>(true))image.enabled=false;
        var view=panel.card.Find("LitTrainView");var vg=view!=null?view.gameObject:new GameObject("LitTrainView",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));vg.transform.SetParent(panel.card,false);vg.transform.SetAsFirstSibling();var vr=(RectTransform)vg.transform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;vg.GetComponent<UnityEngine.UI.RawImage>().texture=rt;vg.GetComponent<UnityEngine.UI.RawImage>().raycastTarget=false;
        controller.stage.SetActive(false);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);return "Saved real mesh train board, render camera, wood materials and medals";
    }
}
#endif
