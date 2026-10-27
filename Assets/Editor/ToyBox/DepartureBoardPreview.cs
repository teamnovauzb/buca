#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;

/// <summary>Holds only the map timer on entering a visual QA session; never changes scene or player preferences.</summary>
public static class DepartureBoardPreview
{
    const string Key="DepartureBoard.HoldPreview";
    static int frames;
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        EditorApplication.update-=Hold;
        EditorApplication.update+=Hold;
    }
    public static string Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Start in Edit mode.");
        frames=0;
        var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var window=EditorWindow.GetWindow(type);
        var size=type.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        SessionState.SetInt("DepartureBoard.OriginalGameSize",(int)size.GetValue(window));
        size.SetValue(window,2);
        SessionState.SetBool(Key,true);
        SessionState.SetInt(Key+"Stage",0);
        EditorApplication.EnterPlaymode();
        return "Starting held menu preview for visual QA.";
    }
    static void Hold()
    {
        if(!SessionState.GetBool(Key,false)||!Application.isPlaying)return;
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>();
        if(map==null||map.illustratedMenu==null)return;
        map.enabled=false;
        map.Select(map.Parked);
        map.illustratedMenu.Refresh();
        map.illustratedMenu.GetComponentInChildren<SuitcaseDepartureBoard>().Refresh(true);
        if(++frames<12)return;
        int stage=SessionState.GetInt(Key+"Stage",0);
        int width=stage==0?1600:1920,height=stage==0?1000:1080;
        var capture=System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var result=capture.GetMethod("CaptureGameView",BindingFlags.Static|BindingFlags.Public)
            .Invoke(null,new object[]{width,height,null,null,false,0,"screen"});
        string path=stage==0?"output/suitcase-menu/DepartureBoard-laptop.png":"output/suitcase-menu/DepartureBoard-live.png";
        System.IO.File.WriteAllBytes(path,System.Convert.FromBase64String((string)result.GetType().GetProperty("Base64").GetValue(result)));
        if(stage==0)
        {
            var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var window=EditorWindow.GetWindow(type);
            type.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)
                .SetValue(window,SessionState.GetInt("DepartureBoard.OriginalGameSize",6));
            SessionState.SetInt(Key+"Stage",1);frames=0;
        }
        else SessionState.SetBool(Key,false);
    }
}
#endif
