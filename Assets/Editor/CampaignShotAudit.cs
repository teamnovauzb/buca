#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Searches the actual aiming predictor; this is not a substitute for cabinet playtesting.</summary>
public static class CampaignShotAudit
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo Preview=typeof(PuckController).GetMethod("UpdatePreview",Private);
    static readonly FieldInfo Points=typeof(PuckController).GetField("_previewPoints",Private);
    static readonly PropertyInfo Singleton=typeof(LevelManager).GetProperty("Instance");
    sealed class Candidate { public Vector3 position; public string shots; public float rank; }

    public static string RunLevel(int n)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only");
        var previous=LevelManager.Instance;
        var active=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var level=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ToyBoxMenu/Levels/Level_{n:00}.prefab"));
            foreach(var t in level.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
            foreach(var moving in level.GetComponentsInChildren<MovingWall>())typeof(MovingWall).GetMethod("Awake",Private).Invoke(moving,null);
            foreach(var rotating in level.GetComponentsInChildren<RotatingWall>())typeof(RotatingWall).GetMethod("Awake",Private).Invoke(rotating,null);
            var manager=new GameObject("AuditManager").AddComponent<LevelManager>();
            Singleton.GetSetMethod(true).Invoke(null,new object[]{manager});
            typeof(LevelManager).GetField("_currentInstance",Private).SetValue(manager,level);
            var go=new GameObject("AuditPuck");var rb=go.AddComponent<Rigidbody>();rb.useGravity=false;rb.mass=1f;rb.linearDamping=.9f;
            rb.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotation;
            var sphere=go.AddComponent<SphereCollider>();sphere.radius=.3f;sphere.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/PuckBouncy.physicMaterial");
            var puck=go.AddComponent<PuckController>();puck.previewLine=go.AddComponent<LineRenderer>();
            puck.previewMask=1<<29;puck.previewMaxSteps=300;puck.previewBounces=20;puck.previewMaxDistance=50;
            typeof(PuckController).GetMethod("Awake",Private).Invoke(puck,null);
            var hole=level.transform.Find("Hole").GetComponent<Collider>();
            var hazards=level.GetComponentsInChildren<DeadlyTrigger>().Select(x=>x.GetComponent<Collider>()).ToArray();
            var start=level.transform.Find("PuckStart").position;start.y=.3f;
            var beam=new List<Candidate>{new Candidate{position=start,shots=""}};
            int tested=0;int budget=n<=10?5:n<=20?4:3;
            for(int depth=1;depth<=budget;depth++)
            {
                var next=new List<Candidate>();
                foreach(var state in beam)
                {
                    var dir=hole.transform.position-state.position;dir.y=0;float direct=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;
                    var angles=new List<float>{direct-4,direct-2,direct,direct+2,direct+4};
                    for(int a=-180;a<180;a+=12)angles.Add(a);
                    foreach(float angle in angles)foreach(float power in new[]{.18f,.30f,.42f,.54f,.66f,.78f,.90f,1f})
                    {
                        rb.position=state.position;go.transform.position=state.position;
                        float rad=angle*Mathf.Deg2Rad;
                        var drag=-new Vector3(Mathf.Sin(rad),0,Mathf.Cos(rad))*puck.maxDragDistance*power;
                        Preview.Invoke(puck,new object[]{drag});tested++;
                        var points=(List<Vector3>)Points.GetValue(puck);if(points.Count<2)continue;
                        Vector3 end=points[points.Count-1];
                        string shot=state.shots+$" [{angle:F1}deg,{power:F2}]";
                        if((hole.ClosestPoint(end)-end).sqrMagnitude<=.301f*.301f)
                            return $"{n:00}: predictor solution in {depth} shots; tested {tested};{shot}";
                        // A preview truncated while still moving is not a legal next-shot position.
                        puck.previewMaxSteps=280;
                        Preview.Invoke(puck,new object[]{drag});
                        var shorter=(List<Vector3>)Points.GetValue(puck);
                        bool settled=Vector3.Distance(end,shorter[shorter.Count-1])<.0001f;
                        puck.previewMaxSteps=300;
                        if(!settled)continue;
                        if(hazards.Any(h=>(h.ClosestPoint(end)-end).sqrMagnitude<.31f*.31f))continue;
                        if(Vector3.Distance(end,state.position)<.7f)continue;
                        next.Add(new Candidate{position=end,shots=shot,rank=Vector3.Distance(end,hole.transform.position)});
                    }
                }
                var used=new HashSet<Vector2Int>();beam.Clear();
                foreach(var candidate in next.OrderBy(x=>x.rank))
                {
                    var cell=new Vector2Int(Mathf.RoundToInt(candidate.position.x/.65f),Mathf.RoundToInt(candidate.position.z/.65f));
                    if(used.Add(cell))beam.Add(candidate);if(beam.Count==10)break;
                }
                if(beam.Count==0)break;
            }
            return $"{n:00}: NO predictor solution within {budget} shots ({tested} samples); manual review required";
        }
        finally
        {
            Singleton.GetSetMethod(true).Invoke(null,new object[]{previous});
            SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);
        }
    }
    public static void RunRange(int first,int last)
    {
        Directory.CreateDirectory("output/balance/shots");
        for(int n=first;n<=last;n++)
        {
            try {File.WriteAllText($"output/balance/shots/{n:00}.txt",RunLevel(n));}
            catch(Exception e){File.WriteAllText($"output/balance/shots/{n:00}.txt",e.ToString());throw;}
        }
    }
}
#endif
