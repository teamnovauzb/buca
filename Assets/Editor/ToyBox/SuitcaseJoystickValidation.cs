#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Exercises the real arcade adapter and map with temporary controller devices.</summary>
public static class SuitcaseJoystickValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Layout = "BucaSuitcaseValidationJoystick";
    static readonly MethodInfo UpdateMap = typeof(TwilightRailwayMap).GetMethod("Update", Private);
    static readonly MethodInfo Navigate = typeof(TwilightRailwayMap).GetMethod("Navigate", Private);

    public static void Check(TwilightRailwayMap map)
    {
        if (!Application.isPlaying) throw new Exception("Joystick validation requires Play mode.");
        if (Joystick.current != null) throw new Exception("Disconnect the physical joystick before simulated input validation.");
        var originalBack = (Action)Get(map, "back");
        var config = Luxodd.Game.ArcadeControls.Config;
        InputDevice device = null;
        bool registered = false;
        int cancelled = 0;
        try
        {
            Luxodd.Game.ArcadeControls.Config = null;
            var pad = InputSystem.AddDevice<Gamepad>(); device = pad; pad.MakeCurrent();
            map.Open(() => cancelled++);
            int initial = map.Selected;
            Pad(map, pad, Vector2.right); Expect(map, initial); // Held input cannot leak into this screen.
            Pad(map, pad, Vector2.zero);
            map.Select(8);
            Pad(map, pad, Vector2.right); Expect(map, 9);
            Pad(map, pad, Vector2.right); Expect(map, 9); // No immediate double step.
            Pad(map, pad, Vector2.left); Expect(map, 8); // Reversing responds immediately.
            Pad(map, pad, Vector2.zero);
            Pad(map, pad, Vector2.zero, GamepadButton.DpadRight); Expect(map, 9);
            Pad(map, pad, Vector2.zero);
            Pad(map, pad, Vector2.zero, GamepadButton.DpadDown); Expect(map, 10);
            Pad(map, pad, Vector2.zero);
            Pad(map, pad, Vector2.zero, GamepadButton.DpadUp); Expect(map, 9);
            Pad(map, pad, Vector2.zero);
            Pad(map, pad, Vector2.zero, GamepadButton.East);
            Require(cancelled == 1, "Red/B did not invoke Back exactly once.");
            Pad(map, pad, Vector2.zero);

            // Advance a controlled clock through the actual navigation routine.
            map.Select(8);
            Nav(map, Vector2.zero, 0); Nav(map, Vector2.right, 1); Expect(map, 9);
            Nav(map, Vector2.right, 1.2f); Expect(map, 9);
            Nav(map, Vector2.right, 1.41f); Expect(map, 10);
            Nav(map, Vector2.right, 1.64f); Expect(map, 11);
            Nav(map, Vector2.left, 1.65f); Expect(map, 10);
            Nav(map, Vector2.one * .1f, 2); Expect(map, 10);
            Nav(map, Vector2.one, 3); Expect(map, 11); // Diagonals give one horizontal step.
            map.Select(0);
            for (int i = 1; i < 50; i++)
            { Nav(map, Vector2.zero, 0); Nav(map, Vector2.right, 1); Expect(map, i); }
            Set(map, "countdown", 9f);
            Nav(map, Vector2.right, 2); Expect(map, 49);
            Require(map.Countdown == 9f, "Holding at level 50 reset the timer.");
            for (int i = 48; i >= 0; i--)
            { Nav(map, Vector2.zero, 0); Nav(map, Vector2.left, 1); Expect(map, i); }
            Set(map, "countdown", 9f);
            Nav(map, Vector2.left, 2); Expect(map, 0);
            Require(map.Countdown == 9f, "Holding at level 1 reset the timer.");

            var events = EventSystem.current;
            Require(events != null && !events.sendNavigationEvents, "Map does not own controller navigation.");
            events.SetSelectedGameObject(map.illustratedMenu.fullscreenButton.gameObject);
            Nav(map, Vector2.right, 3);
            Require(events.currentSelectedGameObject == null, "Controller left a stale footer button selected.");
            map.enabled = false;
            Require(events.sendNavigationEvents, "Navigation was not restored when leaving the map.");
            map.enabled = true;
            Require(!events.sendNavigationEvents, "Navigation was not reclaimed on reopening the map.");

            if (map.HighestUnlocked < 49)
            {
                map.Select(map.HighestUnlocked + 1);
                Pad(map, pad, Vector2.zero);
                Pad(map, pad, Vector2.zero, GamepadButton.South);
                Require(!(bool)Get(map, "playWhenParked") && !(bool)Get(map, "loading"), "Black/A launched a locked level.");
            }
            InputSystem.RemoveDevice(device); device = null;

            InputSystem.RegisterLayout(@"{
                ""name"":""BucaSuitcaseValidationJoystick"", ""extend"":""Joystick"",
                ""controls"":[
                    {""name"":""button0"",""layout"":""Button""},
                    {""name"":""button1"",""layout"":""Button""},
                    {""name"":""hat"",""layout"":""Vector2"",""usage"":""Hatswitch""}
                ]}", Layout);
            registered = true;
            var joystick = (Joystick)InputSystem.AddDevice(Layout); device = joystick; joystick.MakeCurrent();
            map.Open(() => cancelled++);
            Stick(map, joystick, Vector2.zero, Vector2.zero);
            map.Select(8);
            Stick(map, joystick, Vector2.right, Vector2.zero); Expect(map, 9);
            Stick(map, joystick, Vector2.zero, Vector2.zero);
            Stick(map, joystick, Vector2.zero, Vector2.left); Expect(map, 8);
            Stick(map, joystick, Vector2.zero, Vector2.zero);
            Stick(map, joystick, Vector2.zero, Vector2.zero, red: true);
            Require(cancelled == 2, "Generic joystick Red did not invoke Back.");
            // Unlocked confirmation now loads Game immediately. Exercise it separately
            // from this in-scene input check, whose cleanup still needs the map alive.
            File.WriteAllText("output/suitcase-menu/joystick-validation.txt",
                "PASS: real Input System gamepad stick, D-pad, generic joystick axes and hat; Red returns; entry neutral gate; tap/hold/reversal/deadzone; all 50 levels both directions with matching digits/canopy; endpoint timer unchanged; locked confirm blocked; UI navigation ownership restored. Immediate unlocked Play requires a separate scene-transition check. Temporary input devices removed.");
        }
        finally
        {
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            if (registered) InputSystem.RemoveLayout(Layout);
            Luxodd.Game.ArcadeControls.Config = config;
            map.enabled = true;
            map.Open(originalBack);
        }
    }

    public static void Confirm(TwilightRailwayMap map)
    {
        var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
        try
        {
            Pad(map, pad, Vector2.zero);
            Pad(map, pad, Vector2.zero, GamepadButton.South);
        }
        finally { InputSystem.RemoveDevice(pad); }
    }

    static void Pad(TwilightRailwayMap map, Gamepad pad, Vector2 stick, GamepadButton? button = null)
    {
        var state = new GamepadState { leftStick = stick };
        if (button.HasValue) state = state.WithButton(button.Value);
        InputSystem.QueueStateEvent(pad, state);
        InputSystem.Update(); UpdateMap.Invoke(map, null);
    }
    static void Stick(TwilightRailwayMap map, Joystick joystick, Vector2 stick, Vector2 hat, bool black = false, bool red = false)
    {
        using (StateEvent.From(joystick, out var input))
        {
            joystick.stick.WriteValueIntoEvent(stick, input);
            joystick.hatswitch.WriteValueIntoEvent(hat, input);
            joystick.GetChildControl<ButtonControl>("button0").WriteValueIntoEvent(black ? 1f : 0f, input);
            joystick.GetChildControl<ButtonControl>("button1").WriteValueIntoEvent(red ? 1f : 0f, input);
            InputSystem.QueueEvent(input);
        }
        InputSystem.Update(); UpdateMap.Invoke(map, null);
    }
    static void Nav(TwilightRailwayMap map, Vector2 stick, float now) => Navigate.Invoke(map, new object[] { stick, now });
    static object Get(TwilightRailwayMap map, string field) => typeof(TwilightRailwayMap).GetField(field, Private).GetValue(map);
    static void Set(TwilightRailwayMap map, string field, object value) => typeof(TwilightRailwayMap).GetField(field, Private).SetValue(map, value);
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Expect(TwilightRailwayMap map, int selected)
    {
        var board = map.illustratedMenu.GetComponentInChildren<SuitcaseDepartureBoard>();
        board.Refresh(true);
        Require(map.Selected == selected, "Expected level " + (selected + 1) + ", got " + (map.Selected + 1));
        Require(board.tens.text + board.ones.text == (selected + 1).ToString("00"), "Departure digits did not follow joystick.");
        Require(Vector2.Distance(board.marker.anchoredPosition, map.illustratedMenu.stopRects[selected].anchoredPosition + Vector2.up * 24) < .01f,
            "Canopy did not follow joystick.");
    }
}
#endif
