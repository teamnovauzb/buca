using System;
using UnityEditor;
using UnityEngine;

public static class ToyBoxSkinValidation
{
    public static void CheckCampaign()
    {
        for(int i=1;i<=30;i++)
        {
            string file="Level_"+i.ToString("00")+".prefab";
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Levels/"+file);
            var skinned=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/"+file);
            if(original==null||skinned==null) throw new Exception("Missing campaign prefab: "+file);
            var a=original.GetComponentsInChildren<Collider>(true);
            var b=skinned.GetComponentsInChildren<Collider>(true);
            if(a.Length!=b.Length) throw new Exception("Skin changed collision count: "+file);
            for(int colliderIndex=0;colliderIndex<a.Length;colliderIndex++)
            {
                var collider=a[colliderIndex];
                string path=AnimationUtility.CalculateTransformPath(collider.transform,original.transform);
                // Sibling names repeat in authored levels; use stable hierarchy traversal order.
                var other=b[colliderIndex];
                if(other==null || collider.GetType()!=other.GetType() || path!=AnimationUtility.CalculateTransformPath(other.transform,skinned.transform) || collider.isTrigger!=other.isTrigger || collider.enabled!=other.enabled || collider.sharedMaterial!=other.sharedMaterial ||
                    collider.transform.localPosition!=other.transform.localPosition || collider.transform.localRotation!=other.transform.localRotation || collider.transform.localScale!=other.transform.localScale)
                    throw new Exception("Skin changed collision transform/configuration: "+file+"/"+path);
                if(collider is BoxCollider box && other is BoxCollider box2 && (box.center!=box2.center || box.size!=box2.size)) throw new Exception("Box shape changed.");
                if(collider is SphereCollider sphere && other is SphereCollider sphere2 && (sphere.center!=sphere2.center || sphere.radius!=sphere2.radius)) throw new Exception("Sphere shape changed.");
                if(collider is CapsuleCollider cap && other is CapsuleCollider cap2 && (cap.center!=cap2.center || cap.radius!=cap2.radius || cap.height!=cap2.height || cap.direction!=cap2.direction)) throw new Exception("Capsule shape changed.");
                if(collider is MeshCollider mesh && other is MeshCollider mesh2 && (mesh.sharedMesh!=mesh2.sharedMesh || mesh.convex!=mesh2.convex)) throw new Exception("Mesh collision changed.");
            }
            var binding=skinned.GetComponent<BucaSkinBinding>();
            if(binding==null || binding.surfaces.Length<10) throw new Exception("Incomplete board skin: "+file);
            foreach(var surface in binding.surfaces)
            {
                if(surface.target==null||surface.variants.Length!=5) throw new Exception("Broken skin binding: "+file);
                foreach(var mat in surface.variants) if(mat==null||!AssetDatabase.Contains(mat)) throw new Exception("Unsaved skin material.");
            }
            foreach(var rail in skinned.GetComponentsInChildren<RailLight>(true))
            {
                var capsule=rail.GetComponent<CapsuleCollider>();
                if(capsule==null || capsule.direction!=1 || Mathf.Abs(rail.transform.up.y)>.5f) continue;
                if(rail.transform.Find(rail.name+"_SolidWood")==null)
                    throw new Exception("Solid rail is not attached to its authored motion transform: "+file+"/"+rail.name);
            }
            foreach(var filter in skinned.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh==null) throw new Exception("Missing saved mesh: "+filter.name);
        }
        Debug.Log("TOYBOX_SKIN_CAMPAIGN_PASSED: all 30 saved level prefabs, five material sets, original collider shapes and transforms retained.");
    }
}
