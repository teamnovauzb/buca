using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static partial class BuildToyBoxMainMenu
{
 public static string BuildMoonlitReplay()
 {
  PrepareFolders();ToyBoxGeometry.Initialize();
  var steel=NextMaterial("ReplayBrushedSteel",new Color(.115f,.17f,.23f),.48f,.29f);
  var silver=NextMaterial("ReplaySilverEdges",new Color(.36f,.47f,.55f),.62f,.36f);
  var navy=NextMaterial("ReplayMidnightEnamel",new Color(.018f,.065f,.11f),.3f,.36f);
  var ivory=NextMaterial("ReplayIvoryType",new Color(.82f,.83f,.75f),.06f,.3f);
  var cyan=NextMaterial("ReplayIceLight",new Color(.03f,.25f,.36f),.2f,.3f);
  cyan.EnableKeyword("_EMISSION");cyan.SetColor("_EmissionColor",new Color(.015f,.06f,.09f));EditorUtility.SetDirty(cyan);
  var root=PrefabUtility.LoadPrefabContents(BestShotReplayBaker.PrefabPath);
  try {
   var v=root.GetComponent<BestShotReplay>();var parts=new List<Renderer>();var originals=new List<Material>();var replacements=new List<Material>();
   foreach(var r in v.frame.GetComponentsInChildren<MeshRenderer>(true)) {
    string n=r.name;if(n=="LiveRecordedShot"){v.videoScreen=r;continue;}
    Material m=navy;
    if(n.Contains("Brass")||n.Contains("Gold")||n.Contains("Pin"))m=silver;
    if(n=="MapleFrameFace")m=steel;
    if(n.Contains("Letters")||n.Contains("Glyph"))m=ivory;
    if(n.Contains("Mint")||n=="TealHighlight")m=cyan;
    if(n.Contains("IvoryFace")||n=="TitleMapleFace")m=navy;
    if(n=="MarkerMapleFace"||n=="Number")m=ivory;
    if(n=="InsetRim")m=silver;
    parts.Add(r);originals.Add(r.sharedMaterial);replacements.Add(m);
   }
   v.cityFrameParts=parts.ToArray();v.originalFrameMaterials=originals.ToArray();v.cityFrameMaterials=replacements.ToArray();
   // Preserve highlights before the city replay's display tone mapping.
   v.replayTexture.Release();v.replayTexture.format=RenderTextureFormat.ARGBHalf;
   EditorUtility.SetDirty(v.replayTexture);v.playbackCamera.allowHDR=true;
   PrefabUtility.SaveAsPrefabAsset(root,BestShotReplayBaker.PrefabPath);AssetDatabase.SaveAssets();return "Moonlit steel theme saved on "+parts.Count+" physical mesh parts";
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}
