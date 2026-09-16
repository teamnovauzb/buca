using Luxodd.Game.Scripts.Input;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Luxodd.Game
{
    public static class ArcadeControls
    {
        
        public static ArcadeInputConfigAsset Config { get; set; }

        public static bool GetButton(ArcadeButtonColor buttonColor)
        {
#if ENABLE_INPUT_SYSTEM
            if (IsNewInputActive())
                return GetButton_New(buttonColor);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return GetButton_Legacy(buttonColor);
#else
            return false;
#endif
        }

        public static bool GetButtonDown(ArcadeButtonColor buttonColor)
        {
#if ENABLE_INPUT_SYSTEM
            if (IsNewInputActive())
                return GetButtonDown_New(buttonColor);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return GetButtonDown_Legacy(buttonColor);
#else
            return false;
#endif
        }

        public static bool GetButtonUp(ArcadeButtonColor buttonColor)
        {
#if ENABLE_INPUT_SYSTEM
            if (IsNewInputActive())
                return GetButtonUp_New(buttonColor);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return GetButtonUp_Legacy(buttonColor);
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns stick axes as ArcadeStick (Vector2 internally).
        /// Uses Input System if enabled/active; otherwise uses Legacy Input Manager axes.
        /// </summary>
        public static ArcadeStick GetStick()
        {
            var config = Config;

            var deadZone = config ? config.DeadZone : 0.15f;
            var invertX = config && config.InvertX;
            var invertY = config && config.InvertY;

            Vector2 raw;

#if ENABLE_INPUT_SYSTEM
            if (IsNewInputActive())
            {
                raw = GetStick_New();
            }
            else
#endif
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                raw = GetStick_Legacy(config);
#else
                raw = Vector2.zero;
#endif
            }

            if (invertX) raw.x *= -1f;
            if (invertY) raw.y *= -1f;

            var deadZoneVector = ApplyDeadZone(raw, deadZone);
            return new ArcadeStick(deadZoneVector.x, deadZoneVector.y);
        }


        // Legacy Input Manager implementation

#if ENABLE_LEGACY_INPUT_MANAGER
        private static bool GetButton_Legacy(ArcadeButtonColor buttonColor) =>
            UnityEngine.Input.GetKey(ArcadeUnityMapping.GetKeyCode(buttonColor));

        private static bool GetButtonDown_Legacy(ArcadeButtonColor buttonColor) =>
            UnityEngine.Input.GetKeyDown(ArcadeUnityMapping.GetKeyCode(buttonColor));

        private static bool GetButtonUp_Legacy(ArcadeButtonColor buttonColor) =>
            UnityEngine.Input.GetKeyUp(ArcadeUnityMapping.GetKeyCode(buttonColor));

        private static Vector2 GetStick_Legacy(ArcadeInputConfigAsset config)
        {
            var xAxis = config ? config.HorizontalAxisName : "Horizontal";
            var yAxis = config ? config.VerticalAxisName : "Vertical";

            var xValue = SafeGetAxis_Legacy(xAxis);
            var yValue = SafeGetAxis_Legacy(yAxis);

            return new Vector2(xValue, yValue);
        }

        private static float SafeGetAxis_Legacy(string axisName)
        {
            try
            {
                return UnityEngine.Input.GetAxis(axisName);
            }
            catch
            {
                return 0f;
            }
        }
#endif


        // New Input System implementation (Joystick preferred; Gamepad fallback)
        
#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Detects whether the Input System is active in runtime.
        /// In Unity 6 with Active Input Handling = "Input System Package (New)",
        /// calling UnityEngine.Input will throw, so we route through InputSystem.
        /// </summary>
        private static bool IsNewInputActive()
        {
            // In projects where Input System is enabled, InputSystem.settings is non-null.
            return InputSystem.settings != null;
        }

        private static bool GetButton_New(ArcadeButtonColor buttonColor)
        {
            var ctrl = MapColorToButtonControl(buttonColor);
            return ctrl != null && ctrl.isPressed;
        }

        private static bool GetButtonDown_New(ArcadeButtonColor buttonColor)
        {
            var ctrl = MapColorToButtonControl(buttonColor);
            return ctrl != null && ctrl.wasPressedThisFrame;
        }

        private static bool GetButtonUp_New(ArcadeButtonColor buttonColor)
        {
            var ctrl = MapColorToButtonControl(buttonColor);
            return ctrl != null && ctrl.wasReleasedThisFrame;
        }

        private static Vector2 GetStick_New()
        {
            // Prefer Joystick (HID/generic arcade controller)
            var js = GetArcadeJoystick();
            if (js != null)
            {
                // Most arcade encoders expose X/Y axes as the main stick.
                var stick = js.stick != null ? js.stick.ReadValue() : Vector2.zero;
                if (stick.sqrMagnitude > 0.0001f)
                    return stick;

                // Some digital cabinet encoders expose the lever as a hat/D-pad
                // instead of analog axes. Pressure is not required: directions
                // are converted to the same Vector2 used by gameplay.
                if (js.hatswitch != null)
                {
                    var hat = js.hatswitch.ReadValue();
                    if (hat.sqrMagnitude > 0.0001f)
                        return hat;
                }
            }

            // Fallback to Gamepad
            var pad = Gamepad.current;
            if (pad != null)
            {
                var stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.0001f)
                    return stick;

                // Arcade USB boards are frequently reported by browsers as a
                // standard Gamepad whose lever is the D-pad, not leftStick.
                return pad.dpad.ReadValue();
            }

            return Vector2.zero;
        }

        private static Joystick GetArcadeJoystick()
        {
            // Often arcade controllers show up as Joystick (HID).
            return Joystick.current;
        }

        // WebGL creates numbered generic-joystick controls as "Button 1",
        // "Button 2", ... (one-based and containing a space). Native HID
        // layouts may instead use "button0" or "button1". Cache the resolved
        // controls so per-frame polling stays allocation-free.
        private const int JoystickButtonCount = 10;
        private static Joystick _cachedJoystick;
        private static readonly ButtonControl[] _cachedJoystickButtons =
            new ButtonControl[JoystickButtonCount];

        private static ButtonControl GetJoystickButton(Joystick joystick, int zeroBasedIndex)
        {
            if (joystick == null || zeroBasedIndex < 0 || zeroBasedIndex >= JoystickButtonCount)
                return null;

            if (_cachedJoystick != joystick)
            {
                _cachedJoystick = joystick;
                System.Array.Clear(_cachedJoystickButtons, 0, _cachedJoystickButtons.Length);

                // Detect the numbering convention once. Trying "button1" first
                // for every index is incorrect on one-based layouts because Red
                // (index 1) would accidentally read the first physical button.
                bool zeroBasedNames =
                    joystick.TryGetChildControl<ButtonControl>("button0") != null;

                for (int index = 0; index < JoystickButtonCount; index++)
                {
                    ButtonControl control;
                    if (zeroBasedNames)
                    {
                        control = joystick.TryGetChildControl<ButtonControl>($"button{index}");
                    }
                    else
                    {
                        int oneBased = index + 1;
                        control = joystick.TryGetChildControl<ButtonControl>($"Button {oneBased}")
                                  ?? joystick.TryGetChildControl<ButtonControl>($"button {oneBased}")
                                  ?? joystick.TryGetChildControl<ButtonControl>($"button{oneBased}");
                    }

                    _cachedJoystickButtons[index] = control;
                }

                // Older single-trigger joystick layouts may expose only the
                // first button through the canonical trigger control.
                if (_cachedJoystickButtons[0] == null)
                    _cachedJoystickButtons[0] = joystick.trigger;
            }

            return _cachedJoystickButtons[zeroBasedIndex];
        }

        /// <summary>
        /// Maps ArcadeButtonColor to a ButtonControl.
        /// 1) Tries Joystick button index mapping (your confirmed JoystickButtonX mapping).
        /// 2) Falls back to common Gamepad mapping when device is recognized as Gamepad.
        /// </summary>
        private static ButtonControl MapColorToButtonControl(ArcadeButtonColor color)
        {
            var js = GetArcadeJoystick();
            if (js != null)
            {
                var index = ColorToJoystickButtonIndex(color);
                if (index >= 0)
                {
                    var btn = GetJoystickButton(js, index);
                    if (btn != null) return btn;
                }
            }
            
            var pad = Gamepad.current;
            if (pad != null)
            {
                return ColorToGamepadButton(color, pad);
            }

            return null;
        }

        /// <summary>
        /// We are use this mapping:
        /// Black=0, Red=1, Green=2, Yellow=3, Blue=4, Purple=5, Orange=8, White=9
        /// </summary>
        private static int ColorToJoystickButtonIndex(ArcadeButtonColor color)
        {
            return color switch
            {
                ArcadeButtonColor.Black  => 0,
                ArcadeButtonColor.Red    => 1,
                ArcadeButtonColor.Green  => 2,
                ArcadeButtonColor.Yellow => 3,
                ArcadeButtonColor.Blue   => 4,
                ArcadeButtonColor.Purple => 5,
                ArcadeButtonColor.Orange => 8,
                ArcadeButtonColor.White  => 9,
                _ => -1
            };
        }

        /// <summary>
        /// Fallback mapping for Gamepad devices.
        /// Adjust if your arcade controller maps colors differently in Gamepad mode.
        /// </summary>
        private static ButtonControl ColorToGamepadButton(ArcadeButtonColor color, Gamepad pad)
        {
            return color switch
            {
                // Preserve the documented physical button indices even when
                // WebGL reports the cabinet as a standard Gamepad.
                ArcadeButtonColor.Black  => pad.buttonSouth, // A / Cross
                ArcadeButtonColor.Red    => pad.buttonEast,  // B / Circle
                ArcadeButtonColor.Green  => pad.buttonWest,  // index 2
                ArcadeButtonColor.Yellow => pad.buttonNorth, // Y / Triangle

                ArcadeButtonColor.Blue   => pad.leftShoulder,  // index 4
                ArcadeButtonColor.Purple => pad.rightShoulder,
                ArcadeButtonColor.White  => pad.startButton,
                ArcadeButtonColor.Orange => pad.selectButton,

                _ => null
            };
        }
#endif
        
        // Helpers

        private static Vector2 ApplyDeadZone(Vector2 input, float deadZone)
        {
            if (deadZone <= 0f) return input;

            var magnitude = input.magnitude;
            if (magnitude < deadZone) return Vector2.zero;

            var scaled = (magnitude - deadZone) / (1f - deadZone);
            return input.normalized * Mathf.Clamp01(scaled);
        }
    }
}
