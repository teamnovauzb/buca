#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static partial class WorldOneChallengeBalance
{
    // Conservative geometric audit: a puck-sized centre path must remain open.
    // This measures clearance, not the player's ability to execute a shot sequence.
    public static string AuditRoutes(string label)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        Directory.CreateDirectory(Output);
        var report=new StringBuilder("Level,ReachablePoses,TestedPoses,ShortestClearPath,OpenCells\n");
        var failed=new List<int>();
        for(int n=1;n<=30;n++)
        {
            var root=PrefabUtility.LoadPrefabContents(PathFor(Folders[1],n));
            try
            {
                var moving=root.GetComponentsInChildren<MovingWall>(true);
                var spinning=root.GetComponentsInChildren<RotatingWall>(true);
                var bases=moving.ToDictionary(x=>x,x=>x.transform.position);
                var rotations=spinning.ToDictionary(x=>x,x=>x.transform.eulerAngles);
                var colliders=root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&
                    (c is CapsuleCollider||c.GetComponent<DeadlyTrigger>()!=null||c.GetComponent<KickerBumper>()!=null||c.name=="SavedCreamGoalFlag")).ToArray();
                int samples=moving.Length+spinning.Length>0?8:1,reachable=0,open=0;float shortest=float.PositiveInfinity;
                for(int sample=0;sample<samples;sample++)
                {
                    float time=sample*.8f;
                    foreach(var m in moving)m.transform.position=bases[m]+m.axis.normalized*Mathf.Sin((time/m.cycleSeconds+m.phase)*Mathf.PI*2)*m.distance*.5f;
                    foreach(var s in spinning){var e=rotations[s];s.transform.rotation=Quaternion.Euler(e.x,e.y+s.phaseDeg+time*s.speedDegPerSec,e.z);}
                    Physics.SyncTransforms();
                    float length=ClearPath(root,colliders,out int cells);open=Mathf.Max(open,cells);
                    if(length>=0){reachable++;shortest=Mathf.Min(shortest,length);}
                }
                if(reachable==0)failed.Add(n);
                report.AppendLine($"{n},{reachable},{samples},{(reachable>0?shortest:-1):F2},{open}");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllText(Output+"/routes-"+label+".csv",report.ToString());
        string result=failed.Count==0?"PASS: puck-clear routes through all 30 boards at sampled obstacle poses.":"CHECK: no geometric route found in levels "+string.Join(",",failed);
        File.WriteAllText(Output+"/routes-"+label+".txt",result+"\nThis is a geometric clearance check; it does not simulate shot power, force fields or player skill.\n");
        return result;
    }
    static float ClearPath(GameObject root,Collider[] obstacles,out int openCells)
    {
        const float step=.14f,radius=.29f;const int width=59,height=95;
        var clear=new bool[width*height];var distance=new int[clear.Length];
        Vector3 Point(int i)=>new Vector3(-4.06f+(i%width)*step,.22f,-6.58f+(i/width)*step);
        int Index(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt((p.z+6.58f)/step),0,height-1)*width+Mathf.Clamp(Mathf.RoundToInt((p.x+4.06f)/step),0,width-1);
        openCells=0;
        for(int i=0;i<clear.Length;i++)
        {
            var p=Point(i);bool safe=true;
            foreach(var obstacle in obstacles)
            {
                var bounds=obstacle.bounds;bounds.Expand(radius*2);
                if(!bounds.Contains(p))continue;
                if((obstacle.ClosestPoint(p)-p).sqrMagnitude<radius*radius){safe=false;break;}
            }
            clear[i]=safe;distance[i]=-1;if(safe)openCells++;
        }
        int start=Index(root.transform.Find("PuckStart").position);
        if(!clear[start])return -1;
        var goal=root.transform.Find("Hole").position;goal.y=.22f;
        var queue=new Queue<int>();queue.Enqueue(start);distance[start]=0;
        var portals=root.GetComponentsInChildren<Teleporter>(true);
        while(queue.Count>0)
        {
            int i=queue.Dequeue();var p=Point(i);
            if((p-goal).sqrMagnitude<.35f*.35f)return distance[i]*step;
            int x=i%width,z=i/width;
            foreach(int next in new[]{x>0?i-1:-1,x<width-1?i+1:-1,z>0?i-width:-1,z<height-1?i+width:-1})
                if(next>=0&&clear[next]&&distance[next]<0){distance[next]=distance[i]+1;queue.Enqueue(next);}
            foreach(var portal in portals)
                if(portal.partner!=null&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(portal.transform.position.x,portal.transform.position.z))<.35f)
                {int next=Index(portal.partner.transform.position);if(clear[next]&&distance[next]<0){distance[next]=distance[i]+1;queue.Enqueue(next);}}
        }
        return -1;
    }
}
#endif
