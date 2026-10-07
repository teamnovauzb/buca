using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    static void BuildLevelSevenLessons(WatchCopyTutorial3D view)
    {
        var level = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Levels/Level_07.prefab");
        if (level == null) throw new System.InvalidOperationException("Missing Level 7 tutorial source");
        float mudDamping = level.GetComponentInChildren<IcePatch>(true).patchDamping;
        const float scale = .70f;
        var titleInk = Material("CoachInk",new Color(.015f,.055f,.085f),.1f);
        foreach (var lesson in view.lessons)
        {
            if (lesson.key != "MUD" && lesson.key != "PEGS") continue;
            bool mud = lesson.key == "MUD";
            var root = lesson.root.transform;
            var remove = new List<GameObject>();
            foreach (Transform child in root)
                if (child.name == "Mechanic" || child.name == "Level7Layout" || child.name == "Level7Title" ||
                    (child.GetComponent<Renderer>() != null && child.localPosition.z > 5.2f)) remove.Add(child.gameObject);
            foreach (var item in remove) Object.DestroyImmediate(item);
            var layout = Group("Level7Layout", root);
            foreach (var source in level.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = source.name;
                if (!source.enabled || !(n.StartsWith("Mouth_") || n.StartsWith("Chute_") ||
                    n == "NM3_MudPatch" || n == "NM2_GuardPeg")) continue;
                var copy = new GameObject(n, typeof(MeshFilter), typeof(MeshRenderer));
                copy.transform.SetParent(layout, false);
                copy.transform.localPosition = source.transform.position * scale + Vector3.up * .15f;
                copy.transform.localRotation = source.transform.rotation;
                copy.transform.localScale = source.transform.lossyScale * scale;
                copy.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                copy.GetComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
            }
            Text(mud ? "LEVEL 7 - STICKY HONEY SLOWS THE PUCK" : "LEVEL 7 - GO AROUND THE POSTS",
                root, new Vector3(0,1.15f,5.27f), .26f, titleInk).name = "Level7Title";
            Vector3 goal = new Vector3(0,.24f,5.4f);
            root.Find("Goal").localPosition = new Vector3(0,.16f,goal.z * scale);
            // Use the game's puck-to-obstacle size ratio, including the guard posts.
            root.Find("Puck").localScale = Vector3.one * (.30f / .43f * scale);
            Vector3 start = mud ? new Vector3(0,.24f,-5.5f) : new Vector3(2,.24f,5.4f);
            Vector3 velocity = mud ? Vector3.forward * 8f : Vector3.left * 3f;
            Vector3 position = start;
            var times = new List<float> {0,4};
            var points = new List<Vector3> {start * scale,start * scale};
            bool reached = false;
            // Same planar semi-implicit damping as the level Rigidbody and its
            // IcePatch. No invented mid-flight bends or movement before release.
            for (int i=1;i<=175;i++)
            {
                const float dt=.02f;
                float damping = mud && Mathf.Abs(position.x)<1.3f && position.z> -3.95f && position.z< -2.05f ? mudDamping : .9f;
                if (!reached)
                {
                    velocity *= Mathf.Max(0,1-damping*dt);
                    position += velocity*dt;
                    if (velocity.magnitude<.25f) velocity=Vector3.zero;
                    if (!mud && Vector3.Distance(position,goal)<.38f) {reached=true;position=goal;velocity=Vector3.zero;}
                }
                times.Add(4+i*dt);
                var sample=position*scale;
                if(reached) sample.y=.03f;
                points.Add(sample);
            }
            lesson.demonstrationScores = reached;
            var clip = new AnimationClip {name="Level7_"+lesson.key,legacy=true,wrapMode=WrapMode.ClampForever};
            for(int axis=0;axis<3;axis++)
            {
                var values=new float[points.Count];for(int i=0;i<values.Length;i++)values[i]=points[i][axis];
                Curve(clip,root.name+"/Puck","localPosition."+"xyz"[axis],times.ToArray(),values);
            }
            lesson.aimArrow.localPosition = start*scale;
            lesson.aimArrow.localRotation = Quaternion.LookRotation(mud?Vector3.forward:Vector3.left);
            float length=Vector3.Distance(start,position)*scale;
            // Guide length matches this recorded shot, including the mud slowdown.
            foreach(string axis in new[]{"x","y","z"})
                Curve(clip,root.name+"/AimArrow","localScale."+axis,new[]{0f,2.3f,3.9f,4f,7.5f},
                    new[]{axis=="z"?length*.5f:1,axis=="z"?length*.5f:1,axis=="z"?length:1,0f,0f});
            lesson.demonstration=ToyBoxGeometry.Save(clip,Root+"/Animations/Level7_"+lesson.key+".anim");
        }
    }
}
