#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.Reflection;
using Luxodd.Game;
using Luxodd.Game.Scripts.Input;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyLuxoddArcadeInput
{
    const string LayoutName = "BucaGenericWebGLArcadeTest";
    const string LayoutJson = @"
    {
        ""name"": ""BucaGenericWebGLArcadeTest"",
        ""extend"": ""Joystick"",
        ""controls"": [
            { ""name"": ""Button 1"",  ""layout"": ""Button"" },
            { ""name"": ""Button 2"",  ""layout"": ""Button"" },
            { ""name"": ""Button 3"",  ""layout"": ""Button"" },
            { ""name"": ""Button 4"",  ""layout"": ""Button"" },
            { ""name"": ""Button 5"",  ""layout"": ""Button"" },
            { ""name"": ""Button 6"",  ""layout"": ""Button"" },
            { ""name"": ""Button 7"",  ""layout"": ""Button"" },
            { ""name"": ""Button 8"",  ""layout"": ""Button"" },
            { ""name"": ""Button 9"",  ""layout"": ""Button"" },
            { ""name"": ""Button 10"", ""layout"": ""Button"" }
        ]
    }";

    static readonly ArcadeButtonColor[] Colors =
    {
        ArcadeButtonColor.Black, ArcadeButtonColor.Red,
        ArcadeButtonColor.Green, ArcadeButtonColor.Yellow,
        ArcadeButtonColor.Blue, ArcadeButtonColor.Purple,
        ArcadeButtonColor.Orange, ArcadeButtonColor.White
    };
    static readonly int[] Indices = { 0, 1, 2, 3, 4, 5, 8, 9 };

    [MenuItem("RealBuca/Verify Luxodd Arcade Mapping")]
    public static void Run()
    {
        InputDevice device = null;
        bool layoutRegistered = false;
        ArcadeInputConfigAsset originalConfig = ArcadeControls.Config;
        try
        {
            ArcadeControls.Config = null;
            InputSystem.RegisterLayout(LayoutJson, LayoutName);
            layoutRegistered = true;
            device = InputSystem.AddDevice(LayoutName);
            var joystick = device as Joystick;
            if (joystick == null)
                throw new BuildFailedException("Generic WebGL device was not created as Joystick.");
            joystick.MakeCurrent();

            MethodInfo mapper = typeof(ArcadeControls).GetMethod(
                "MapColorToButtonControl", BindingFlags.Static | BindingFlags.NonPublic);
            if (mapper == null)
                throw new BuildFailedException("Luxodd button mapper was not found.");

            for (int i = 0; i < Colors.Length; i++)
            {
                var control = mapper.Invoke(null, new object[] { Colors[i] }) as ButtonControl;
                string expected = "Button " + (Indices[i] + 1);
                if (control == null || !string.Equals(control.name, expected,
                        StringComparison.OrdinalIgnoreCase))
                    throw new BuildFailedException(
                        $"Generic mapping failed for {Colors[i]}: expected {expected}, got " +
                        $"{(control != null ? control.name : "null")}.");
            }

            InputSystem.RemoveDevice(device);
            device = null;
            InputSystem.RemoveLayout(LayoutName);
            layoutRegistered = false;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            device = gamepad;
            gamepad.MakeCurrent();

            QueueButton(gamepad, GamepadButton.West);
            if (!ArcadeControls.GetButton(ArcadeButtonColor.Green))
                throw new BuildFailedException("WebGL button index 2 did not map to Green.");

            QueueButton(gamepad, GamepadButton.LeftShoulder);
            if (!ArcadeControls.GetButton(ArcadeButtonColor.Blue))
                throw new BuildFailedException("WebGL button index 4 did not map to Blue.");

            QueueButton(gamepad, GamepadButton.DpadUp);
            Vector2 direction = ArcadeControls.GetStick().Vector;
            if (direction.y < 0.9f)
                throw new BuildFailedException($"Digital D-pad fallback failed: {direction}.");

            Debug.Log("LUXODD_ARCADE_MAPPING_VERIFIED: generic buttons, standard indices, and digital D-pad passed.");
        }
        finally
        {
            ArcadeControls.Config = originalConfig;
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            if (layoutRegistered) InputSystem.RemoveLayout(LayoutName);
        }
    }

    static void QueueButton(Gamepad gamepad, GamepadButton button)
    {
        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(button));
        InputSystem.Update();
    }
}
#endif
