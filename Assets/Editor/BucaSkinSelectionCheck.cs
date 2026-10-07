#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
public static class BucaSkinSelectionCheck
{
    public static string Run()
    {
        var selector=UnityEngine.Object.FindFirstObjectByType<ToyPaintSkinSelector>(FindObjectsInactive.Include);
        int saved=BucaSkinBinding.Selected;int previews=0,checks=0;
        var renderers=selector.GetComponentsInChildren<Renderer>(true);
        var materials=Array.ConvertAll(renderers,r=>r.sharedMaterials);
        var rings=Array.ConvertAll(selector.buttons,b=>b.selectedRing.activeSelf);
        var pots=Array.ConvertAll(selector.potRings,g=>g.activeSelf);
        try
        {
            for(int skin=0;skin<5;skin++)
            {
                int before=BucaSkinBinding.Selected;
                selector.Focus(skin);
                if(BucaSkinBinding.Selected!=before)throw new Exception("Hover saved unexpectedly");
                selector.Activate();
                if(BucaSkinBinding.Selected!=skin)throw new Exception("Color press failed to save");
                previews++;
            }
            for(int n=1;n<=30;n++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToyBoxMenu/Levels/Level_"+n.ToString("00")+".prefab");
                var clone=UnityEngine.Object.Instantiate(prefab);clone.hideFlags=HideFlags.HideAndDontSave;
                try
                {
                    var binding=clone.GetComponent<BucaSkinBinding>();
                    for(int skin=0;skin<5;skin++)
                    {
                        binding.Apply(skin);
                        foreach(var surface in binding.surfaces)
                            if(surface.target==null||surface.variants[skin]==null||surface.target.sharedMaterial!=surface.variants[skin])throw new Exception("Broken level skin "+n+" / "+skin);
                        checks++;
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(clone);}
            }
            return previews+" skin selection/save checks; "+checks+" level/skin material checks passed. Original selection restored.";
        }
        finally
        {
            BucaSkinBinding.Select(saved);selector.Focus(saved);
            for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=materials[i];
            for(int i=0;i<rings.Length;i++)selector.buttons[i].selectedRing.SetActive(rings[i]);
            for(int i=0;i<pots.Length;i++)selector.potRings[i].SetActive(pots[i]);
        }
    }
}
#endif
