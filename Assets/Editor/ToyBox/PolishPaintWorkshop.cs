using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/Polish Wooden Skin Workshop")]
    public static void PolishPaintWorkshop()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode before saving workshop art.");
        ToyBoxGeometry.Initialize();
        foreach(string path in new[]{Root+"/Prefabs/PaintWorkshopSkins.prefab",PrefabPath})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var selector=root.GetComponentInChildren<ToyPaintSkinSelector>(true);
                if(selector==null) throw new System.InvalidOperationException("Missing workshop: "+path);
                ApplyPremiumWorkshop(selector.transform);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }

    static void ApplyPremiumWorkshop(Transform room)
    {
        var maple=Material("PremiumWorkshopMaple",new Color(.72f,.68f,.58f),.30f,WorkshopTexture("WorkshopMaplePhoto"));
        var cream=Material("PremiumWorkshopCream",new Color(.88f,.80f,.64f),.26f);
        var plaster=Material("PremiumWorkshopPlaster",new Color(.69f,.63f,.53f),.12f);
        var panel=Material("PremiumWorkshopPanel",new Color(.77f,.71f,.61f),.20f);
        var teal=Material("PremiumWorkshopTeal",new Color(.025f,.25f,.27f),.35f);
        var dark=Material("PremiumWorkshopInk",new Color(.018f,.067f,.075f),.24f);
        var brass=Material("PremiumWorkshopBrass",new Color(.61f,.40f,.17f),.38f); brass.SetFloat("_Metallic",.42f);
        var mint=Material("PremiumWorkshopMint",new Color(.22f,.69f,.51f),.35f);
        var selected=Material("PremiumWorkshopSelection",new Color(.96f,.71f,.25f),.42f);
        var deck=Material("PremiumWorkshopDeck",new Color(.64f,.59f,.49f),.28f,WorkshopTexture("WorkshopMaplePhoto"));
        var colors=new[]{new Color(.035f,.20f,.48f),new Color(.17f,.48f,.35f),new Color(.85f,.47f,.035f),new Color(.47f,.035f,.13f),new Color(.025f,.35f,.42f)};
        var replacements=new Dictionary<Material,Material>();
        var selector=room.GetComponent<ToyPaintSkinSelector>();
        foreach(var binding in room.GetComponentsInChildren<BucaSkinBinding>(true))
        foreach(var surfaceBinding in binding.surfaces)
        for(int i=0;i<surfaceBinding.variants.Length;i++)
        {
            var old=surfaceBinding.variants[i]; if(old==null || !old.name.StartsWith("ArcadeEnamel")) continue;
            var replacement=Material("PremiumWorkshopEnamel"+i,colors[i],.34f);
            replacements[old]=replacement; surfaceBinding.variants[i]=replacement;
        }
        foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>(true))
        {
            var old=renderer.sharedMaterial; if(old==null) continue;
            if(replacements.TryGetValue(old,out var replacement)) renderer.sharedMaterial=replacement;
            string name=renderer.name;
            if(old.name=="ArcadeBrushedSilver") renderer.sharedMaterial=brass;
            if(old.name=="ArcadeNavyInk") renderer.sharedMaterial=dark;
            if(old.name=="ArcadeIvoryLabels") renderer.sharedMaterial=cream;
            if(old.name=="ArcadeCyanGlow") renderer.sharedMaterial=selected;
            if(name=="SolidWorkbench") renderer.sharedMaterial=maple;
            if(name=="CoolGrayWall") renderer.sharedMaterial=plaster;
            if(name=="CentralWallPanel") renderer.sharedMaterial=panel;
            if(name=="MetalLightChannel" || name=="CyanWallLight") renderer.sharedMaterial=maple;
            if(name=="HangingSign" || name=="NamePlaque") renderer.sharedMaterial=cream;
            if(name=="SolidMapleWithHole") renderer.sharedMaterial=deck;
            if(name=="LayeredNavyBase" || name=="SilverBaseBand" || name=="MetalFoot") renderer.sharedMaterial=teal;
            if(name=="SatinSocket") renderer.sharedMaterial=teal;
            if(name=="CeramicPot") renderer.sharedMaterial=cream;
            if(name=="ChromePebble" || name=="BlueSculpture") renderer.sharedMaterial=maple;
            if(name=="SelectedPotHalo") renderer.sharedMaterial=selected;
            if(name=="Button" && renderer.transform.parent.parent.name=="USE SKIN") renderer.sharedMaterial=mint;
            if(name=="Button" && renderer.transform.parent.parent.name=="BACK") renderer.sharedMaterial=cream;
        }
        // Keep all existing hitboxes, selection bindings and camera framing.
        var oldDetails=room.Find("PremiumWoodDetails");
        if(oldDetails!=null) Object.DestroyImmediate(oldDetails.gameObject);
        var details=Group("PremiumWoodDetails",room);
        Box("SignTealFrame",details,new Vector3(0,5.25f,6.49f),new Vector3(10.7f,1.48f,.42f),teal,.20f);
        Box("PaletteTrayWood",details,new Vector3(0,.015f,-2.5f),new Vector3(18.2f,.18f,3.9f),maple,.22f);
        Box("PaletteTrayTealEdge",details,new Vector3(0,-.09f,-2.5f),new Vector3(18.4f,.16f,4.05f),teal,.22f);
        for(int i=0;i<5;i++)
            Disc("InsetDisplayCoaster",details,new Vector3((i-2)*3.45f,.12f,-2.5f),1.52f,.11f,maple,.04f);
        foreach(var light in room.GetComponentsInChildren<Light>(true))
        {
            if(light.name=="SoftKey") { light.color=new Color(1f,.96f,.87f); light.intensity=1.02f; light.shadowStrength=.60f; }
            if(light.name=="SoftFill") { light.color=new Color(.85f,.92f,1f); light.intensity=.25f; }
            if(light.name=="WarmWindowRim") { light.color=new Color(1f,.79f,.51f); light.intensity=.32f; }
        }
        selector.viewCamera.backgroundColor=new Color(.69f,.63f,.53f);
        // Refresh the authored preview without equipping or changing player preferences.
        selector.Preview(0);
    }
}
