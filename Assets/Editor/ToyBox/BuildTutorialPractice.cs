using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static partial class BuildToyBoxMainMenu
{
    static void BuildPracticeBoards(Transform stage,WatchCopyTutorial3D view)
    {
        string materialPath=Root+"/Materials/TutorialBounce.physicMaterial";
        var bounce=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
        if(bounce==null) { bounce=new PhysicsMaterial("TutorialBounce"); AssetDatabase.CreateAsset(bounce,materialPath); }
        bounce.bounciness=.85f; bounce.dynamicFriction=0; bounce.staticFriction=0;
        bounce.bounceCombine=PhysicsMaterialCombine.Maximum; EditorUtility.SetDirty(bounce);
        foreach(var lesson in view.lessons)
        {
            var root=UnityEngine.Object.Instantiate(lesson.root,stage);
            root.name="Practice_"+lesson.root.name;
            root.SetActive(false);
            var practice=root.AddComponent<TutorialPractice3D>(); practice.mechanic=lesson.key;
            var puck=root.transform.Find("Puck");
            practice.startPosition=new Vector3(-1,.24f,-3.8f); puck.localPosition=practice.startPosition;
            practice.puck=puck.gameObject.AddComponent<Rigidbody>();
            practice.puck.useGravity=false; practice.puck.linearDamping=.9f;
            practice.puck.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;
            practice.puck.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            practice.puck.isKinematic=true;
            var sphere=puck.gameObject.AddComponent<SphereCollider>(); sphere.radius=.40f; sphere.sharedMaterial=bounce;
            puck.gameObject.AddComponent<TutorialPracticePuck>().practice=practice;
            practice.arrow=root.transform.Find("AimArrow");
            practice.goal=root.transform.Find("Goal"); practice.goal.name="Hole";
            var goalCollider=practice.goal.gameObject.AddComponent<SphereCollider>(); goalCollider.radius=.67f; goalCollider.isTrigger=true;
            var goalContact=practice.goal.gameObject.AddComponent<TutorialPracticeContact>(); goalContact.practice=practice; goalContact.goal=true;
            foreach(int side in new[]{-1,1})
            {
                var rail=Group("PracticeSide",root.transform,new Vector3(side*5.4f,.32f,0));
                var collider=rail.gameObject.AddComponent<BoxCollider>(); collider.size=new Vector3(.48f,1.2f,11.5f); collider.sharedMaterial=bounce;
                var end=Group("PracticeEnd",root.transform,new Vector3(0,.32f,side*5.6f));
                collider=end.gameObject.AddComponent<BoxCollider>(); collider.size=new Vector3(11.2f,1.2f,.48f); collider.sharedMaterial=bounce;
            }
            Text("GOAL",practice.goal,new Vector3(0,.13f,.9f),.22f,blue,true);
            practice.goal.Find("GoalLight").gameObject.SetActive(true);
            var prop=root.transform.Find("Mechanic");
            var collectibles=new List<GameObject>();
            string key=lesson.key;
            if(key=="JUMP")
            {
                var oldTitle=root.transform.Find("JUMP OVER THE WALL");
                if(oldTitle!=null) UnityEngine.Object.DestroyImmediate(oldTitle.gameObject);
                Text("BOUNCE PAD - SHOOT INTO THE PAD",root.transform,new Vector3(0,1.15f,5.27f),.25f,ink);
            }
            foreach(Transform piece in prop)
            {
                string name=piece.name;
                if(name=="ArrowPeg" && (key=="JUMP" || key=="WIND" || key=="PUSH"))
                    piece.localRotation=Quaternion.Euler(0,key=="JUMP"?145:90,0)*piece.localRotation;
                if(name=="BounceWall" || name=="MovingWall" || name=="Kicker")
                {
                    var filter=piece.GetComponent<MeshFilter>();
                    var col=piece.gameObject.AddComponent<BoxCollider>(); col.size=filter.sharedMesh.bounds.size; col.center=filter.sharedMesh.bounds.center; col.sharedMaterial=bounce;
                    var contact=piece.gameObject.AddComponent<TutorialPracticeContact>(); contact.practice=practice;
                    if(key=="RIM") piece.gameObject.AddComponent<RimRebound>();
                    if(key=="BANK") piece.gameObject.AddComponent<BankingRail>();
                    if(key=="KICK") piece.gameObject.AddComponent<KickerBumper>();
                    if(key=="SLIDE") { var motion=piece.gameObject.AddComponent<MovingWall>(); motion.distance=4f; motion.cycleSeconds=4; }
                    if(key=="SPIN") piece.gameObject.AddComponent<RotatingWall>().speedDegPerSec=40;
                    if(key=="GATE") piece.gameObject.AddComponent<DisappearingWall>();
                }
                if(name=="JumpWall") piece.gameObject.SetActive(false); // Actual BouncePad redirects in the board plane; it is not a vertical jump.
                if(name=="SurfacePatch")
                {
                    var col=piece.gameObject.AddComponent<BoxCollider>(); col.size=new Vector3(3.3f,1f,2.3f); col.isTrigger=true;
                    piece.gameObject.AddComponent<TutorialPracticeContact>().practice=practice;
                    if(key=="MUD" || key=="ICE") piece.gameObject.AddComponent<IcePatch>().patchDamping=key=="MUD"?6f:.05f;
                    if(key=="FAST") piece.gameObject.AddComponent<SpeedBoost>().boostAmount=5;
                    if(key=="WIND" || key=="PUSH")
                    {
                        var wind=piece.gameObject.AddComponent<BucaWindZone>(); wind.forceMagnitude=key=="WIND"?5f:3f;
                        piece.localRotation=Quaternion.Euler(0,90,0);
                    }
                    if(key=="JUMP")
                    {
                        piece.localRotation=Quaternion.Euler(0,145,0);
                        piece.gameObject.AddComponent<BouncePad>().launchSpeed=8;
                        practice.goal.localPosition=new Vector3(2.4f,.16f,-3f);
                    }
                }
                if(name.StartsWith("AvoidTrap"))
                {
                    var col=piece.gameObject.AddComponent<SphereCollider>(); col.radius=.40f; col.isTrigger=true;
                    piece.gameObject.AddComponent<DeadlyTrigger>();
                }
                if(name.StartsWith("Gold"))
                {
                    var col=piece.gameObject.AddComponent<SphereCollider>(); col.radius=.3f; col.isTrigger=true;
                    piece.gameObject.AddComponent<ScorePickup>(); collectibles.Add(piece.gameObject);
                }
                if(name=="GravityBall")
                {
                    var gravity=piece.gameObject.AddComponent<GravityWell>(); gravity.practicePuck=practice.puck; gravity.range=2.5f; gravity.strength=7;
                }
            }
            if(key=="WARP")
            {
                var portals=new List<Teleporter>();
                foreach(Transform piece in prop) if(piece.name=="PortalWell")
                {
                    var col=piece.gameObject.AddComponent<SphereCollider>(); col.radius=.6f; col.isTrigger=true;
                    portals.Add(piece.gameObject.AddComponent<Teleporter>());
                    piece.gameObject.AddComponent<TutorialPracticeContact>().practice=practice;
                }
                portals[0].partner=portals[1]; portals[1].partner=portals[0];
                Text("IN",portals[0].transform,new Vector3(0,.14f,-.9f),.20f,blue,true);
                Text("OUT",portals[1].transform,new Vector3(0,.14f,.9f),.20f,blue,true);
            }
            if(key=="MOVE")
            {
                var mover=Group("HoleOrbiter",root.transform).gameObject.AddComponent<OrbitingHole>(); mover.radius=.65f; mover.cycleSeconds=6;
            }
            practice.collectibles=collectibles.ToArray();
            // Every practice board is a separate saved prefab with its own puck, target and real mechanics.
            string path=Root+"/Prefabs/"+root.name+".prefab";
            var asset=PrefabUtility.SaveAsPrefabAsset(root,path);
            UnityEngine.Object.DestroyImmediate(root);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,stage);
            lesson.practice=instance.GetComponent<TutorialPractice3D>();
        }
    }
}
