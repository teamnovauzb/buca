using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Reject missing authored tutorial content before a player build.</summary>
public sealed class PrebuiltContentValidation : IPreprocessBuildWithReport
{
    public int callbackOrder=>0;
    public void OnPreprocessBuild(BuildReport report)=>Validate();
    [MenuItem("RealBuca/Prebuild/Validate Pregenerated Content")]
    public static void Validate()
    {
        var view=AssetDatabase.LoadAssetAtPath<BoardCoachTutorial>("Assets/Resources/Tutorials/BoardCoachTutorial.prefab");
        Require(view!=null,"Board coach prefab");
        Require(view.demoPuck!=null && view.demoGuide!=null && view.demoGuide.sharedMaterial!=null,"Puck and guide must be baked");
        Require(view.demoRenderers!=null && view.demoRenderers.Length>=16,"Puck renderer slots");
        Require(Array.Exists(view.demoRenderers,r=>r!=null && r.GetComponent<MeshFilter>().sharedMesh!=null),"Saved puck mesh");
        foreach(var button in new[]{view.play,view.replay,view.pause})
        {
            var face=button.GetComponentInChildren<ArcadeButtonFace>(true);
            Require(face!=null && face.raisedSprite!=null && face.pressedSprite!=null,"Saved raised/pressed sprites");
        }
        var cinematic=view.GetComponent<CinematicMechanicTutorial>();
        Require(cinematic!=null && !cinematic.enabled,"Saved inactive cinematic driver");
        var comparison=AssetDatabase.LoadAssetAtPath<HoneyComparisonTutorial>("Assets/Resources/Tutorials/HoneyComparisonTutorial.prefab");
        Require(comparison!=null && comparison.artwork.GetComponent<CanvasGroup>()!=null,"Saved comparison canvas group");
        foreach(var graphic in comparison.GetComponentsInChildren<HoneyDemoPuckGraphic>(true))Require(graphic.sprite!=null,"Saved comparison puck sprite");
        Debug.Log("PREBUILT_CONTENT_VALIDATED: button sprites, puck meshes/slots, guide material, cinematic component and comparison UI.");
    }
    static void Require(bool condition,string name){if(!condition)throw new BuildFailedException("Missing pregenerated content: "+name+". Run RealBuca > Prebuild > Bake Tutorial Content.");}
}
