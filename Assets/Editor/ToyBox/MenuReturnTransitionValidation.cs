#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class MenuReturnTransitionValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static string Check()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only");
        var original=Object.FindAnyObjectByType<ToyBoxMenuController>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();
        var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
        try
        {
            var copy=Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(copy,scene);copy.SetActive(false);
            var menu=copy.GetComponent<ToyBoxMenuController>();var map=menu.twilightMap;
            var transition=menu.menuReturnTransition;var lever=map.menuLever;
            map.eventAudio=map.motionAudio=null;
            foreach(var c in copy.GetComponentsInChildren<Camera>(true)){c.enabled=false;c.scene=scene;}
            foreach(var canvas in copy.GetComponentsInChildren<Canvas>(true))canvas.enabled=false;
            copy.SetActive(true);
            var pageType=typeof(ToyBoxMenuController).GetNestedType("Page",BindingFlags.NonPublic);
            typeof(ToyBoxMenuController).GetMethod("ShowPage",Private).Invoke(menu,new[]{Enum.Parse(pageType,"Levels")});
            Require(lever.returnTransition==transition,"Lever points outside cloned menu");
            Require(lever.pullSound!=null && lever.pullSound.name=="RailwayLeverMechanism","Mechanical lever sound missing");
            Require(transition.sweepSound==null,"Extra transition sound masks the mechanism");
            float fov=menu.menuCamera.fieldOfView;
            typeof(ToyBoxMenuController).GetField("_remaining",Private).SetValue(menu,1f);
            map.RequestMenuReturn();
            lever.TickReturn(.24f);
            Require(!transition.Busy&&map.gameObject.activeSelf,"Map vanished before lever movement finished");
            lever.TickReturn(.25f);
            Require(transition.Busy&&map.gameObject.activeSelf,"Transition failed to begin");
            int selection=map.Selected;map.Select(Math.Min(49,selection+1));Require(map.Selected==selection,"Selection changed during return");
            transition.Tick(.14f);Require(transition.curtain.color.a>.3f&&transition.curtain.color.a<.8f,"Cover is not gradual");
            Require(!menu.homeRoot.activeSelf,"Home appeared before cover");
            transition.Tick(.14f);
            Require(menu.homeRoot.activeSelf&&!menu.levelsRoot.activeSelf,"Did not return to actual Home page");
            Require(transition.curtain.color.a>.99f,"Home swap was visible");
            Require(transition.Busy&&transition.overlay.gameObject.activeSelf,"Curtain closed with map");
            transition.Tick(.24f);Require(transition.curtain.color.a>.3f&&transition.curtain.color.a<.8f,"Home reveal is not gradual");
            Require(menu.menuCamera.fieldOfView>fov,"Home camera reveal missing");
            transition.Tick(.25f);Require(!transition.Busy&&!transition.overlay.gameObject.activeSelf,"Overlay blocks menu after return");
            Require(Mathf.Approximately(fov,menu.menuCamera.fieldOfView),"Camera framing drift");
            Require((float)typeof(ToyBoxMenuController).GetField("_remaining",Private).GetValue(menu)==30f,"Home countdown not reset");
            var plaque=lever.handle.parent.Find("Wooden instruction plaque");
            foreach(string n in new[]{"Menu title","Push left hint"})
            {
                var t=plaque.Find(n);var bounds=t.GetComponent<MeshFilter>().sharedMesh.bounds;
                Require(Mathf.Abs(t.localPosition.x)<.001f,"Plaque lettering not centered");
                Require(Mathf.Abs(t.localPosition.y)+bounds.extents.y*t.localScale.y<.23f,"Lettering too close to board edge");
            }
            // Reopening starts from a fresh lever pose; no stored completion state.
            typeof(ToyBoxMenuController).GetMethod("ShowPage",Private).Invoke(menu,new[]{Enum.Parse(pageType,"Levels")});
            Require(!lever.Returning,"Lever failed to reset when map reopened");
            return "PASS: actual Home return, cover/reveal midpoint, camera restoration, input lock, 30-second reset, replayability, smaller centered text and sound references. No Play mode.";
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;
        }
    }
}
#endif
