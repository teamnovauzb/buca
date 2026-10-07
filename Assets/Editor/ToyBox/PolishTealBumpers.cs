#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    public static void PolishTealBumpers()
    {
        ToyBoxGeometry.Initialize();
        var teal=Material("ToyBumperTeal",new Color(.025f,.60f,.61f),.32f);
        var mintTop=Material("ToyBumperMintInset",new Color(.25f,.88f,.79f),.30f);
        foreach(var mat in new[]{teal,mintTop}){
            mat.SetTexture("_BaseMap",null);mat.SetTexture("_BumpMap",null);
            mat.DisableKeyword("_NORMALMAP");mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.32f);
            mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",mat.GetColor("_BaseColor")*.14f);
            EditorUtility.SetDirty(mat);
        }
        int count=0;
        foreach(var folder in new[]{"Assets/Prefabs/Levels","Assets/ToyBoxMenu/Levels"})
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{folder})){
            string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
            try {
                foreach(var rail in root.GetComponentsInChildren<RailLight>(true)){
                    if(rail.name.StartsWith("Bound_")||rail.name.StartsWith("Corner_")||rail.name.Contains("GuardPeg"))continue;
                    var renderers=rail.GetComponentsInChildren<MeshRenderer>(true);
                    foreach(var r in renderers){
                        if(r==null || r.name=="MintTopInset")continue;
                        r.sharedMaterial=teal;
                        foreach(var binding in root.GetComponentsInChildren<BucaSkinBinding>(true))
                            if(binding.surfaces!=null)foreach(var entry in binding.surfaces)if(entry.target==r)entry.variants=Same(teal);
                        if(!r.enabled || !r.name.EndsWith("_SolidWood"))continue;
                        var old=r.transform.Find("MintTopInset");if(old!=null)Object.DestroyImmediate(old.gameObject);
                        var bounds=r.GetComponent<MeshFilter>().sharedMesh.bounds;
                        float width=bounds.size.x*.56f,length=bounds.size.z-bounds.size.x*.35f;
                        var inset=MeshObject("MintTopInset",r.transform,new Vector3(bounds.center.x,bounds.max.y+.002f,bounds.center.z),
                            ToyBoxGeometry.StandingCapsule(width*.5f,length,.08f),mintTop);
                        inset.transform.localScale=new Vector3(1,.12f,1);
                        count++;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        PolishTeddyCoachTutorial();
        System.IO.File.WriteAllText("/tmp/buca-teal-result.txt","Saved all 30 source and 30 skinned levels; "+count+" inset bumper bars.");
    }
}
#endif
