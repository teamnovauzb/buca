#if UNITY_EDITOR
using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class InstantPlayValidation
{
    const string Key = "BucaInstantPlayValidation";
    static Stopwatch watch;
    static int preparedFrame = -1;
    static bool pressed;
    static double revealMs;
    static InstantPlayValidation() { EditorApplication.update += Tick; }

    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        SessionState.SetString(Key + "Result", "Running");
        SessionState.SetBool(Key, true);
        SessionState.SetInt(Key + "Current", PlayerPrefs.GetInt("BucaCurrentLevel", -1));
        SessionState.SetInt(Key + "Pending", PlayerPrefs.GetInt("BucaPendingLevel", -1));
        SessionState.SetBool(Key + "Background", Application.runInBackground);
        Application.runInBackground = true;
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (pressed) return;
            if (!GameScenePreloader.Ready) return;
            if (preparedFrame < 0) { preparedFrame = Time.frameCount; return; }
            if (Time.frameCount <= preparedFrame + 2) return;
            var game = SceneManager.GetSceneByName("Game");
            foreach (var root in game.GetRootGameObjects())
                if (root.activeSelf) throw new Exception("Hidden gameplay was active before Play.");
            if (SceneManager.GetActiveScene().name != "MainMenu") throw new Exception("Menu lost active scene.");
            var map = UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
            map.gameObject.SetActive(true);
            map.Open(null);
            map.Select(16);
            CheckArcadeButtons(map);
            RenderPipelineManager.endFrameRendering += OnRendered;
            pressed = true;
            watch = Stopwatch.StartNew();
            map.RequestPlay();
            revealMs = watch.Elapsed.TotalMilliseconds;
            if (SceneManager.GetActiveScene().name != "Game") throw new Exception("Play did not reveal Game synchronously.");
        }
        catch (Exception ex) { Finish("FAIL: " + ex); }
    }

    static void OnRendered(ScriptableRenderContext context, Camera[] cameras)
    {
        if (!pressed || SceneManager.GetActiveScene().name != "Game") return;
        bool renderedGame = false;
        foreach (var camera in cameras) if (camera != null && camera.gameObject.scene.name == "Game") renderedGame = true;
        if (!renderedGame) return;
        var manager = LevelManager.Instance;
        if (manager == null || manager.CurrentLevelNumber != 17 || manager.CurrentLevelRoot == null)
        { Finish("FAIL: Selected level 17 was not initialized on first gameplay render."); return; }
        Finish($"PASS: hidden gameplay inactive; menu frames advance; Play reveals Game in {revealMs:F1} ms; first gameplay render in {watch.Elapsed.TotalMilliseconds:F1} ms; selected level 17 initialized.");
    }

    static void CheckArcadeButtons(TwilightRailwayMap map)
    {
        var config=Luxodd.Game.ArcadeControls.Config;
        var old=UnityEngine.InputSystem.Gamepad.current;
        var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
        try
        {
            Luxodd.Game.ArcadeControls.Config=null;pad.MakeCurrent();
            var navigate=typeof(TwilightRailwayMap).GetMethod("NavigateArcadeButtons",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.West));
            UnityEngine.InputSystem.InputSystem.Update();navigate.Invoke(map,null);
            if(map.Selected!=15)throw new Exception("Green/3 did not select previous.");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());
            UnityEngine.InputSystem.InputSystem.Update();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.LeftShoulder));
            UnityEngine.InputSystem.InputSystem.Update();navigate.Invoke(map,null);
            if(map.Selected!=16)throw new Exception("Blue/5 did not select next.");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));
            UnityEngine.InputSystem.InputSystem.Update();
            if(!ArcadeInputAdapter.ConfirmDown())throw new Exception("Black/1 did not confirm.");
        }
        finally
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
            if(old!=null)old.MakeCurrent();Luxodd.Game.ArcadeControls.Config=config;
        }
    }

    static void Finish(string result)
    {
        RenderPipelineManager.endFrameRendering -= OnRendered;
        SessionState.SetString(Key + "Result", result);
        SessionState.SetBool(Key, false);
        Time.timeScale = 0;
        EditorApplication.delayCall += () => { EditorApplication.isPlaying = false; };
    }

    public static string Restore()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Wait for test to finish.");
        foreach (var pair in new[] { new[] {"Current", "BucaCurrentLevel"}, new[] {"Pending", "BucaPendingLevel"} })
        {
            int value = SessionState.GetInt(Key + pair[0], -1);
            if (value < 0) PlayerPrefs.DeleteKey(pair[1]); else PlayerPrefs.SetInt(pair[1], value);
        }
        PlayerPrefs.Save();
        Application.runInBackground = SessionState.GetBool(Key + "Background", false);
        Time.timeScale = 1;
        return SessionState.GetString(Key + "Result", "Not run");
    }
}
#endif
