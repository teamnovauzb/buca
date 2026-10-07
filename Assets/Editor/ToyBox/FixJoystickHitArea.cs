#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public static class FixJoystickHitArea
{
    public static void Bake()
    {
        const string path="Assets/ToyBoxMenu/Prefabs/SolidGameplayHud.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud=root.GetComponentInChildren<ToyBoxGameplayHud>(true);
            var hit=(SphereCollider)hud.joystickHit;
            hit.center=new Vector3(0,.6f,0); hit.radius=1.05f;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("JOYSTICK_HIT_AREA_BAKED");
    }
}
#endif
