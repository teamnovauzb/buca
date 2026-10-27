#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
public static class BuildCinematicMapDetails
{
 public static string Apply()
 {
  var source=Object.FindAnyObjectByType<WorkshopFlipDisplay>(FindObjectsInactive.Include);
  if(source==null)throw new System.Exception("Main map required");
  foreach(string path in new[]{BuildGrandTrainWelcome.Path,BuildWorldTwoHarborReveal.Path})
  {
   var root=PrefabUtility.LoadPrefabContents(path);
   try
   {
    var party=root.GetComponent<GrandTrainWelcome>();var journey=root.GetComponent<WorldTwoHarborReveal>();
    var art=party!=null?party.artwork:journey.artwork;
    var details=root.GetComponent<CinematicMapDetails>()??root.AddComponent<CinematicMapDetails>();details.artwork=art.rectTransform;details.level=party!=null?30:31;
    var backdrop=art.transform.Find("OriginalControlBackdrop") as RectTransform;
    if(backdrop==null)
    {
     backdrop=new GameObject("OriginalControlBackdrop",typeof(RectTransform)).GetComponent<RectTransform>();backdrop.SetParent(art.transform,false);backdrop.sizeDelta=new Vector2(1672,941);
     foreach(string name in new[]{"CleanArcadeButtonDeck","ScreenButtonRemoval"})
     {
      var original=source.view.artwork.Find(name);
      if(original!=null)Object.Instantiate(original.gameObject,backdrop,false);
     }
    }
    backdrop.SetAsFirstSibling();details.controlBackdrop=backdrop;
    var existing=art.transform.Find("OriginalMapControls");
    var controls=existing!=null?existing.gameObject:Object.Instantiate(source.transform.parent.gameObject,art.transform,false);
    controls.name="OriginalMapControls";details.controls=(RectTransform)controls.transform;
    foreach(var script in controls.GetComponentsInChildren<MonoBehaviour>(true))
     if(script.GetType().Assembly==typeof(LevelManager).Assembly && !(script is WorkshopStationBadge))Object.DestroyImmediate(script);
    foreach(var button in controls.GetComponentsInChildren<Button>(true)){button.onClick=new Button.ButtonClickedEvent();button.transition=Selectable.Transition.None;button.interactable=false;}
    var number=controls.transform.Find("WorkshopFlipSelector/Destination").GetComponent<TMPro.TMP_Text>();number.enabled=true;number.text=details.level.ToString();number.overflowMode=TMPro.TextOverflowModes.Overflow;
    var leaves=controls.transform.Find("WorkshopFlipSelector/TurningLeaves");if(leaves!=null)leaves.gameObject.SetActive(false);
    if(party!=null)
    {
     party.automaticJourney=true;
     var layer=art.transform.Find("LevelTickets");
     if(layer==null)
     {
      var rabbit=AssetDatabase.LoadAssetAtPath<GameObject>(BuildRabbitGiftReward.Path).GetComponent<RabbitGiftReward>();
      layer=Object.Instantiate(rabbit.ticketLayer.gameObject,art.transform,false).transform;layer.name="LevelTickets";
     }
     details.tickets=(RectTransform)layer;
    }
    else
    {
     journey.duration=22;journey.fogRevealSeconds=6;journey.preserveMapControls=true;
     art.GetComponent<AspectRatioFitter>().aspectRatio=1672f/941;
     string playPath=AnimationUtility.CalculateTransformPath(source.view.playButton.transform,source.transform.parent);
     var play=controls.transform.Find(playPath).GetComponent<Button>();
     if(journey.continueButton!=null && journey.continueButton!=play)journey.continueButton.gameObject.SetActive(false);
     journey.continueButton=play;
    }
    details.Refresh();PrefabUtility.SaveAsPrefabAsset(root,path);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  return "Original controls and 50 celebration stops restored; automatic upgrade, bridge journey and six-second fog reveal connected.";
 }
}
#endif
