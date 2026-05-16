using UnityEngine;

/// <summary>
/// Single entry point for arcade joystick + button reads.
///
/// Internally routes through Luxodd's ArcadeControls when the plugin is
/// present (works under both Legacy Input Manager and the new Input
/// System, with the configured deadzone applied). Falls back to raw
/// UnityEngine.Input + KeyCode.JoystickButtonN if the plugin isn't
/// installed, so editor testing on a regular gamepad still works.
///
/// Color mapping (matches the arcade panel + Luxodd docs):
///   Black  = JoystickButton0  (default Confirm / Launch)
///   Red    = JoystickButton1
///   Green  = JoystickButton2
///   Yellow = JoystickButton3
///   Blue   = JoystickButton4
///   Purple = JoystickButton5
///   Orange = JoystickButton8  (system overlay / help)
///   White  = JoystickButton9  (system Back / Cancel)
///
/// Use Black for "go" actions (launch puck, advance panels). Use White
/// for "back" actions (cancel from menus). Reserved system buttons
/// (Orange / White) should not drive gameplay.
/// </summary>
public static class ArcadeInputAdapter
{
    /// <summary>Stable, friendly enum to use from gameplay code.</summary>
    public enum Button
    {
        Black, Red, Green, Yellow, Blue, Purple, Orange, White
    }

    // ─────────────────────────────────────────────────────────
    // Stick
    // ─────────────────────────────────────────────────────────
    public static Vector2 GetStick()
    {
        // Prefer Luxodd's ArcadeControls (handles both input systems +
        // applies configured deadzone + axis inversion).
        try
        {
            var s = Luxodd.Game.ArcadeControls.GetStick();
            return new Vector2(s.X, s.Y);
        }
        catch (System.Exception e)
        {
            LogFallbackOnce("GetStick", e);
            // Fallback: raw Input axes
            try
            {
                return new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical"));
            }
            catch
            {
                return Vector2.zero;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // Buttons
    // ─────────────────────────────────────────────────────────
    public static bool GetButton(Button b)
    {
        try { return Luxodd.Game.ArcadeControls.GetButton(ToLuxodd(b)); }
        catch (System.Exception e) { LogFallbackOnce("GetButton", e); return Input.GetKey(ToKeyCode(b)); }
    }

    public static bool GetButtonDown(Button b)
    {
        try { return Luxodd.Game.ArcadeControls.GetButtonDown(ToLuxodd(b)); }
        catch (System.Exception e) { LogFallbackOnce("GetButtonDown", e); return Input.GetKeyDown(ToKeyCode(b)); }
    }

    public static bool GetButtonUp(Button b)
    {
        try { return Luxodd.Game.ArcadeControls.GetButtonUp(ToLuxodd(b)); }
        catch (System.Exception e) { LogFallbackOnce("GetButtonUp", e); return Input.GetKeyUp(ToKeyCode(b)); }
    }

    // Throttle the fallback log so it doesn't spam the console — print
    // each unique method name once per session.
    static readonly System.Collections.Generic.HashSet<string> _loggedFallbacks
        = new System.Collections.Generic.HashSet<string>();
    static void LogFallbackOnce(string method, System.Exception e)
    {
        if (_loggedFallbacks.Add(method))
            Debug.LogWarning($"[ArcadeInputAdapter] Luxodd ArcadeControls.{method} threw " +
                             $"({e.GetType().Name}: {e.Message}) — falling back to raw Input. " +
                             "This warning prints once per method per session.");
    }

    /// <summary>Black is the canonical "Confirm / Launch" button.</summary>
    public static bool ConfirmDown() => GetButtonDown(Button.Black);
    /// <summary>White is the canonical "Back / Cancel" button.</summary>
    public static bool CancelDown() => GetButtonDown(Button.White);

    // ─────────────────────────────────────────────────────────
    // Activity detection — used to auto-switch from mouse to arcade
    // ─────────────────────────────────────────────────────────
    static bool _detected;
    /// <summary>True if any arcade input has been observed this session.</summary>
    public static bool ArcadeUsedThisSession => _detected;

    /// <summary>
    /// Reset the cached detection flag. Static fields survive Domain Reload
    /// when "Reload Domain" is disabled in Play Mode settings, which would
    /// leak the detected=true state across separate Play sessions and force
    /// arcade mode permanently. Tagged so Unity calls it on every play start.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticState()
    {
        _detected = false;
        if (_loggedFallbacks != null) _loggedFallbacks.Clear();
    }

    /// <summary>
    /// Polls all arcade input sources. Returns true if anything is active.
    /// Caches the result so once arcade is detected, it stays detected.
    /// </summary>
    public static bool DetectAnyArcadeInput(float stickThreshold = 0.15f)
    {
        if (_detected) return true;
        var s = GetStick();
        if (Mathf.Abs(s.x) > stickThreshold || Mathf.Abs(s.y) > stickThreshold)
        { _detected = true; return true; }
        for (int i = 0; i < 8; i++)
        {
            if (GetButton((Button)i)) { _detected = true; return true; }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────
    // Mapping helpers
    // ─────────────────────────────────────────────────────────
    static Luxodd.Game.Scripts.Input.ArcadeButtonColor ToLuxodd(Button b) => b switch
    {
        Button.Black  => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Black,
        Button.Red    => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Red,
        Button.Green  => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Green,
        Button.Yellow => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Yellow,
        Button.Blue   => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Blue,
        Button.Purple => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Purple,
        Button.Orange => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Orange,
        Button.White  => Luxodd.Game.Scripts.Input.ArcadeButtonColor.White,
        _ => Luxodd.Game.Scripts.Input.ArcadeButtonColor.Black,
    };

    static KeyCode ToKeyCode(Button b) => b switch
    {
        Button.Black  => KeyCode.JoystickButton0,
        Button.Red    => KeyCode.JoystickButton1,
        Button.Green  => KeyCode.JoystickButton2,
        Button.Yellow => KeyCode.JoystickButton3,
        Button.Blue   => KeyCode.JoystickButton4,
        Button.Purple => KeyCode.JoystickButton5,
        Button.Orange => KeyCode.JoystickButton8,
        Button.White  => KeyCode.JoystickButton9,
        _ => KeyCode.JoystickButton0,
    };
}
