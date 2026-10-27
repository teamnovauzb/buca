#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public static class BuildRabbitGift3D
{
    const string Folder="Assets/Art/RabbitGift";
    static Transform Group(string name,Transform parent)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.gameObject.layer=28;return t;}
    static Mesh SaveMesh(string name,Mesh mesh)
    {
        string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old!=null){Object.DestroyImmediate(mesh);return old;}
        mesh.name=name;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh Star()
    {
        var v=new List<Vector3>();var t=new List<int>();
        // Four rings make the front/back bevels and thick gold edge visible during rotation.
        for(int ring=0;ring<4;ring++)for(int i=0;i<10;i++)
        {
            float a=(90+i*36)*Mathf.Deg2Rad;
            float radius=(i%2==0?1:.48f)*(ring==0||ring==3?.83f:1);
            v.Add(new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,new[]{-.20f,-.10f,.10f,.20f}[ring]));
        }
        for(int ring=0;ring<3;ring++)for(int i=0;i<10;i++)
        {int a=ring*10+i,b=ring*10+(i+1)%10;t.AddRange(new[]{a,b,b+10,a,b+10,a+10});}
        v.Add(new Vector3(0,0,-.20f));v.Add(new Vector3(0,0,.20f));
        for(int i=0;i<10;i++){int next=(i+1)%10;t.AddRange(new[]{40,next,i,41,30+i,30+next});}
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);return SaveMesh("CelebrationBeveledStar",mesh);
    }
    static Mesh Ribbon()
    {
        var v=new List<Vector3>();var tris=new List<int>();
        const int steps=24;
        for(int i=0;i<=steps;i++)
        {
            float u=i/(float)steps,a=u*Mathf.PI*2.3f;
            var center=new Vector3(Mathf.Sin(a)*.32f,(u-.5f)*2.7f,Mathf.Cos(a)*.32f);
            var across=new Vector3(Mathf.Cos(a),0,-Mathf.Sin(a))*.16f;
            var depth=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*.035f;
            v.Add(center-across-depth);v.Add(center+across-depth);v.Add(center+across+depth);v.Add(center-across+depth);
            if(i==0)continue;
            for(int side=0;side<4;side++)
            {int p=(i-1)*4+side,q=(i-1)*4+(side+1)%4;tris.AddRange(new[]{p,q,q+4,p,q+4,p+4});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tris,0);return SaveMesh("CelebrationCurledRibbon",mesh);
    }
    public static void Configure(RabbitGiftReward reward)
    {
        var fx=reward.celebration;
        if(fx.stage!=null)
        {
            if(fx.pieces.Length<320)
            {
                int count=fx.pieces.Length;var pieces=new Transform[320];System.Array.Copy(fx.pieces,pieces,count);
                var savedRibbon=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/CelebrationCurledRibbon.asset");
                for(int i=count;i<pieces.Length;i++)
                {
                    var p=Group("CurledRibbon"+i,fx.stage);pieces[i]=p;
                    p.gameObject.AddComponent<MeshFilter>().sharedMesh=savedRibbon;
                    p.gameObject.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/CelebrationGloss"+(1+i%5)+".mat");
                }
                fx.pieces=pieces;
            }
            return;
        }
        var old=fx.GetComponent<CanvasRenderer>();if(old!=null)Object.DestroyImmediate(old);
        fx.display=fx.gameObject.AddComponent<UnityEngine.UI.RawImage>();fx.display.raycastTarget=false;
        fx.stage=Group("Celebration3DStage",reward.transform);
        var camera=Group("CelebrationCamera",fx.stage).gameObject.AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=5.4f;camera.aspect=16f/9;
        camera.transform.localPosition=new Vector3(0,0,-18);camera.cullingMask=1<<28;
        camera.nearClipPlane=.1f;camera.farClipPlane=35;camera.enabled=false;camera.allowHDR=false;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;fx.effectCamera=camera;
        var star=Star();var ribbon=Ribbon();
        Color[] colors={new Color(1,.67f,.08f),new Color(1,.16f,.31f),new Color(.08f,.83f,.73f),new Color(.2f,.46f,1),new Color(.72f,.22f,1),new Color(1,.42f,.12f)};
        var mats=new Material[colors.Length];
        for(int i=0;i<colors.Length;i++)
        {
            string path=Folder+"/CelebrationGloss"+i+".mat";
            mats[i]=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mats[i]==null){mats[i]=new Material(Shader.Find("BUCA/Woodland Toy"));AssetDatabase.CreateAsset(mats[i],path);}
            mats[i].SetColor("_BaseColor",colors[i]);mats[i].SetFloat("_Metallic",i==0?.8f:.35f);mats[i].SetFloat("_Smoothness",.7f);mats[i].SetFloat("_GrainStrength",0);EditorUtility.SetDirty(mats[i]);
        }
        fx.pieces=new Transform[320];
        for(int i=0;i<fx.pieces.Length;i++)
        {
            var piece=Group((i<16?"GoldenStar":"CurledRibbon")+i,fx.stage);fx.pieces[i]=piece;
            piece.gameObject.AddComponent<MeshFilter>().sharedMesh=i<16?star:ribbon;
            piece.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mats[i<16?0:1+i%5];
        }
        fx.display.enabled=false;fx.stage.gameObject.SetActive(false);
    }
}
#endif
