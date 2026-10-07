#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>Saved goal flag art and orbiting pole; all visuals are baked in the Editor.</summary>
public static class CreamGoalFlagBaker
{
    const string Folder = "Assets/Art/CreamGoalFlag";
    const string Marker = "SavedCreamGoalFlag";
    static Vector3 Cloth(float u, float v)
    {
        float end = 1.05f - .28f * (1f - Mathf.Abs(v * 2f - 1f));
        float x = .045f + u * end;
        return new Vector3(x, 1.03f + v * .67f - .055f * u,
            .075f * Mathf.Sin(u * Mathf.PI * 1.7f) * u + .016f * Mathf.Sin(v * Mathf.PI) * u);
    }

    [MenuItem("RealBuca/Bake Cream Goal Flags")]
    public static string Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before baking flags.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        ToyBoxGeometry.Initialize();
        var cream = Mat("CreamLinen", new Color(.96f,.87f,.65f), 0f, .22f);
        var teal = Mat("TealPiping", new Color(.015f,.23f,.23f), .05f, .35f);
        var gold = Mat("BrassFinial", new Color(.83f,.56f,.22f), .7f, .55f);
        var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/Materials/NaturalBirch.mat");
        var weave = new Texture2D(64,64,TextureFormat.RGB24,true) { name="LinenWeave",wrapMode=TextureWrapMode.Repeat };
        var pixels = new Color[64*64];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float shade=.91f+.065f*Mathf.Sin(x*Mathf.PI*.5f)*Mathf.Sin(y*Mathf.PI*.5f);
            pixels[y*64+x]=new Color(shade,shade,shade);
        }
        weave.SetPixels(pixels);weave.Apply();
        cream.SetTexture("_BaseMap",ToyBoxGeometry.Save(weave,Folder+"/LinenWeave.asset"));
        cream.SetTextureScale("_BaseMap",new Vector2(5,3));
        cream.SetFloat("_Cull",0); teal.SetFloat("_Cull",0);
        var root = new GameObject(Marker);
        try
        {
            Part(root.transform,"TealSocket",new Vector3(0,.12f,0),ToyBoxGeometry.Disc(.115f,.24f,.028f),teal);
            Part(root.transform,"WoodPole",new Vector3(0,.93f,0),ToyBoxGeometry.Disc(.046f,1.70f,.017f),wood);
            // Rounded brass finial, baked from Unity's sphere mesh into the saved visual.
            var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var sphere=ball.GetComponent<MeshFilter>().sharedMesh;
            var cap=Part(root.transform,"BrassCap",new Vector3(0,1.81f,0),sphere,gold);
            cap.transform.localScale=Vector3.one*.145f;
            UnityEngine.Object.DestroyImmediate(ball);
            Part(root.transform,"CreamSwallowtail",Vector3.zero,Surface(),cream);
            Part(root.transform,"TealStitchedEdge",Vector3.zero,Border(),teal);
            Part(root.transform,"TealRoundEmblem",Vector3.zero,Emblem(),teal);
            foreach(Transform part in root.transform){part.localPosition*=.65f;part.localScale*=.65f;}
            var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var pole=root.AddComponent<CapsuleCollider>();pole.direction=1;pole.radius=.075f;pole.height=1.18f;pole.center=new Vector3(0,.59f,0);
            var response=new PhysicsMaterial("FlagPoleResponse"){bounciness=.5f,dynamicFriction=0,staticFriction=0,bounceCombine=PhysicsMaterialCombine.Maximum,frictionCombine=PhysicsMaterialCombine.Minimum};
            pole.sharedMaterial=ToyBoxGeometry.Save(response,Folder+"/FlagPoleResponse.physicMaterial");
            root.AddComponent<OrbitingGoalFlag>();
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/CreamGoalFlag.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }

        int count=0;
        string backup="output/goal-flag/backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        foreach(string folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.transform.Find("Hole")==null)continue;
            string destination=backup+"/"+path;Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(path,destination);
            var level=PrefabUtility.LoadPrefabContents(path);
            try
            {
                Apply(level);
                ClearOrbit(level);
                PrefabUtility.SaveAsPrefabAsset(level,path);count++;
            }
            finally { PrefabUtility.UnloadPrefabContents(level); }
        }
        AssetDatabase.SaveAssets();
        string result=Validate()+"\nSaved flags in "+count+" level prefabs. Backup: "+backup;
        File.WriteAllText("output/goal-flag/verification.txt",result);
        return result;
    }

    public static void Apply(GameObject level)
    {
        var hole=level.transform.Find("Hole");if(hole==null)return;
        foreach(var old in level.GetComponentsInChildren<Transform>(true).Where(t=>t.name==Marker).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
        var flag=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CreamGoalFlag.prefab"),level.scene);
        flag.name=Marker;
        flag.transform.SetParent(level.transform,false);
        Vector3 hp=level.transform.InverseTransformPoint(hole.position);
        flag.transform.localPosition=new Vector3(hp.x-.65f,.008f,hp.z+.40f);
        // Keep the physics body under the unscaled level, separate from the goal trigger body.
        var motion=flag.GetComponent<OrbitingGoalFlag>();motion.goal=hole;
        motion.radius=new Vector2(.65f,.4f).magnitude;motion.phaseDegrees=Mathf.Atan2(.4f,-.65f)*Mathf.Rad2Deg;
        PrefabUtility.RecordPrefabInstancePropertyModifications(motion);
    }

    // Local clearance only: preserve each obstacle's shape and move its attached art with it.
    static void ClearOrbit(GameObject level)
    {
        var flag=level.GetComponentInChildren<OrbitingGoalFlag>();
        var pole=flag.GetComponent<CapsuleCollider>();
        foreach(var obstacle in level.GetComponentsInChildren<Collider>())
        {
            if(obstacle==pole||obstacle.isTrigger||!obstacle.enabled||!obstacle.gameObject.activeInHierarchy)continue;
            Vector3 initial=obstacle.transform.position;
            for(int pass=0;pass<8;pass++)
            {
                float deepest=0;Vector3 correction=Vector3.zero;
                for(int i=0;i<96;i++)
                {
                    float angle=i*Mathf.PI*2/96;
                    var position=flag.goal.position+new Vector3(Mathf.Cos(angle)*flag.radius,0,Mathf.Sin(angle)*flag.radius);position.y=.008f;
                    if(Physics.ComputePenetration(pole,position,Quaternion.identity,obstacle,obstacle.transform.position,obstacle.transform.rotation,out var normal,out float depth)&&depth>deepest)
                    {deepest=depth;correction=normal;}
                }
                if(deepest<.001f)break;
                correction.y=0;
                if(correction.sqrMagnitude<.1f)throw new Exception("Orbit clearance requires vertical change: "+level.name+"/"+obstacle.name);
                obstacle.transform.position-=correction.normalized*(deepest+.035f);
                if(Vector3.Distance(initial,obstacle.transform.position)>.4f)throw new Exception("Orbit requires too much clearance: "+level.name+"/"+obstacle.name);
            }
        }
    }

    static Material Mat(string name,Color color,float metallic,float gloss)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",gloss);
        return ToyBoxGeometry.Save(mat,Folder+"/"+name+".mat");
    }
    static GameObject Part(Transform parent,string name,Vector3 p,Mesh mesh,Material mat)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
    }
    static Mesh Save(string name,List<Vector3> v,List<int> t,List<Vector2> uv=null)
    {
        var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);if(uv!=null)m.SetUVs(0,uv);
        m.RecalculateNormals();m.RecalculateBounds();if(uv!=null)m.RecalculateTangents();
        return ToyBoxGeometry.Save(m,Folder+"/"+name+".asset");
    }
    static Mesh Surface()
    {
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();const int nx=24,ny=16;
        for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
        {
            float u=x/(float)nx,w=y/(float)ny;v.Add(Cloth(u,w));uv.Add(new Vector2(u,w));
            if(x<nx&&y<ny){int i=y*(nx+1)+x;t.AddRange(new[]{i,i+nx+1,i+1,i+1,i+nx+1,i+nx+2});}
        }
        return Save("CurvedCreamSwallowtail",v,t,uv);
    }
    static Mesh Border()
    {
        var points=new List<Vector3>();const int n=32,sides=8;
        for(int i=0;i<n;i++)points.Add(Cloth(i/(float)n,0));
        for(int i=0;i<n;i++)points.Add(Cloth(1,i/(float)n));
        for(int i=0;i<n;i++)points.Add(Cloth(1-i/(float)n,1));
        for(int i=0;i<n;i++)points.Add(Cloth(0,1-i/(float)n));
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<points.Count;i++)
        {
            Vector3 tangent=(points[(i+1)%points.Count]-points[(i+points.Count-1)%points.Count]).normalized;
            Vector3 a=Vector3.Cross(tangent,Vector3.forward).normalized,b=Vector3.Cross(tangent,a).normalized;
            for(int j=0;j<sides;j++)
            {
                float angle=j*Mathf.PI*2/sides;v.Add(points[i]+.016f*(a*Mathf.Cos(angle)+b*Mathf.Sin(angle)));
                int k=i*sides+j,l=i*sides+(j+1)%sides,m=((i+1)%points.Count)*sides+j,q=((i+1)%points.Count)*sides+(j+1)%sides;
                t.AddRange(new[]{k,m,l,l,m,q});
            }
        }
        return Save("RoundedTealPiping",v,t);
    }
    static Mesh Emblem()
    {
        var v=new List<Vector3>();var t=new List<int>();
        // Conform both sides to the curved linen, avoiding a floating or intersecting badge.
        foreach(float side in new[]{-1f,1f})
        {
            int start=v.Count;v.Add(Cloth(.44f,.51f)+Vector3.forward*side*.004f);
            for(int i=0;i<=48;i++)
            {
                float a=i*Mathf.PI*2/48;
                float vv=.51f+Mathf.Sin(a)*.15f/.67f;
                float width=1.05f-.28f*(1-Mathf.Abs(vv*2-1));
                float uu=(Cloth(.44f,.51f).x-.045f+Mathf.Cos(a)*.15f)/width;
                v.Add(Cloth(uu,vv)+Vector3.forward*side*.004f);
                if(i>0)t.AddRange(new[]{start,start+i,start+i+1});
            }
        }
        return Save("TealCircleEmblem",v,t);
    }
    public static string Validate()
    {
        int count=0;
        foreach(string folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var level=AssetDatabase.LoadAssetAtPath<GameObject>(path);var hole=level.transform.Find("Hole");if(hole==null)continue;
            var flag=level.transform.Find(Marker);if(flag==null)throw new Exception("Missing flag: "+path);
            if(flag.GetComponentsInChildren<Collider>(true).Length!=1||flag.GetComponent<Collider>().isTrigger)throw new Exception("Flag needs one solid pole: "+path);
            if(flag.GetComponent<OrbitingGoalFlag>().goal!=hole||!flag.GetComponent<Rigidbody>().isKinematic)throw new Exception("Unwired flag: "+path);
            foreach(var mf in flag.GetComponentsInChildren<MeshFilter>(true))if(mf.sharedMesh==null||!EditorUtility.IsPersistent(mf.sharedMesh))throw new Exception("Unsaved mesh: "+path);
            if((flag.lossyScale-Vector3.one).sqrMagnitude>.0001f)throw new Exception("Distorted marker: "+path);
            count++;
        }
        return "PASS: "+count+" goals have saved flags, unit world scale, a solid kinematic pole and a wired orbit center.";
    }
    static void Capture(Camera cam,string name,int width,int height)
    {
        var rt=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;float aspect=cam.aspect;
        try {
            cam.targetTexture=rt;cam.aspect=(float)width/height;cam.Render();RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
            Directory.CreateDirectory("output/toy-box");File.WriteAllBytes("output/toy-box/"+name+".png",texture.EncodeToPNG());
        } finally {cam.targetTexture=null;cam.aspect=aspect;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    public static string Preview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required.");
        string restore=SceneManager.GetActiveScene().path;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        try
        {
            var board=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_01.prefab"),scene);
            var cam=Camera.main;var hp=board.transform.Find("Hole").position;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            Capture(cam,"CreamFlag-Gameplay",1600,1000);
            cam.transform.position=hp+new Vector3(2.6f,2.7f,-4.5f);cam.transform.LookAt(hp+new Vector3(.15f,.68f,.25f));cam.fieldOfView=39;
            Capture(cam,"CreamFlag-Closeup",1400,1100);
            return "output/toy-box/CreamFlag-Gameplay.png and CreamFlag-Closeup.png";
        }
        finally { EditorSceneManager.OpenScene(restore); }
    }
}
#endif
