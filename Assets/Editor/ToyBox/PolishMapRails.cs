using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    public static string PolishMapRails()
    {
        PrepareFolders();ToyBoxGeometry.Initialize();
        var walnut=AssetDatabase.LoadAssetAtPath<Material>(StationFolder+"/StationWalnut.mat");
        var steel=AssetDatabase.LoadAssetAtPath<Material>(StationFolder+"/StationRailSilver.mat");
        var brass=AssetDatabase.LoadAssetAtPath<Material>(StationFolder+"/StationBrass.mat");
        // Resolve the saved station palette regardless of its asset folder.
        Material Find(string name)=>AssetDatabase.FindAssets(name+" t:Material").Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).First(m=>m.name==name);
        walnut=Find("StationWalnut");steel=Find("StationRailSilver");brass=Find("StationBrass");var ink=Find("StationMidnight");
        int rails=0,ties=0;
        foreach(var path in new[]{"Assets/ToyBoxMenu/Prefabs/TrainLevelSelector.prefab","Assets/ToyBoxMenu/WorldStation/WorldStationJourney.prefab","Assets/ToyBoxMenu/Prefabs/ToyBoxMainMenu.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="WoodenRail").ToArray())
                {
                    r.sharedMaterial=ink;
                    var crown=Object.Instantiate(r.gameObject,r.transform.parent);crown.name="MapPolishedRailCrown";
                    crown.transform.localPosition+=Vector3.up*.05f;
                    var scale=crown.transform.localScale;scale.x*=.89f;scale.z*=.89f;crown.transform.localScale=scale;
                    crown.GetComponent<MeshRenderer>().sharedMaterial=steel;r.name="MapDarkRailWeb";rails++;
                }
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="RailwaySleeper").ToArray())
                {
                    r.sharedMaterial=walnut;r.name="MapWalnutSleeper";
                    r.transform.localScale=new Vector3(1.27f/.95f,1,.25f/.20f);
                    var parent=r.transform.parent;var center=r.transform.localPosition+Vector3.up*.055f;
                    foreach(int side in new[]{-1,1})
                    {
                        var position=center+(r.transform.localRotation*Vector3.right)*side*.36f;
                        var chair=StationBox("MapBrassRailChair",parent,position-Vector3.up*.01f,new Vector3(.29f,.045f,.22f),brass,.015f);
                        chair.transform.localRotation=r.transform.localRotation;
                        Disc("MapSleeperBolt",parent,center+(r.transform.localRotation*Vector3.right)*side*.54f+Vector3.up*.005f,.055f,.065f,steel,.015f);
                    }
                    ties++;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return $"Polished {rails} rail segments and {ties} sleepers across map prefabs.";
    }
}
