using System;
using Luxodd.Game;
using Luxodd.Game.Scripts.Input;
using UnityEngine;

/// <summary>
/// Converts Luxodd cabinet input into Buca gameplay actions.
///
/// Cabinet mapping:
///   Joystick = puck aim direction (digital or analog)
///   Black    = hold to charge, release to shoot
///   Green    = hold for slower, fine aim rotation
///   White    = cancel the current shot
///
/// Orange remains reserved for the Luxodd system/help overlay. Menu confirmation
/// also uses Black, but the post-death popup is delayed until Black has been
/// fully released so a gameplay press cannot accidentally select Continue.
/// </summary>
[DefaultExecutionOrder(-200)]
public sealed class BucaArcadeControlAdapter : MonoBehaviour, IBucaArcadeControlAdapter
{
    [SerializeField]
    [Tooltip("Luxodd input configuration used by ArcadeControls. The setup editor script assigns this automatically.")]
    ArcadeInputConfigAsset inputConfig;

    public Vector2 AimVector { get; private set; }
    public bool IsShootButtonPressed { get; private set; }
    public bool IsFineTuneButtonPressed { get; private set; }
    public bool ShootPressedThisFrame { get; private set; }
    public bool ShootReleasedThisFrame { get; private set; }
    public bool CancelPressedThisFrame { get; private set; }

    public event Action ShootPressed;
    public event Action ShootReleased;
    public event Action CancelPressed;

    void Awake()
    {
        // The Luxodd plugin prefab normally assigns this in MainMenu. Assigning
        // it here as well makes direct Game-scene testing deterministic.
        if (inputConfig != null)
            ArcadeControls.Config = inputConfig;
    }

    void Update()
    {
        // Read input in Update as required by the Luxodd integration guide.
        // A cabinet stick may report diagonals with magnitude > 1, so clamp
        // without changing its direction.
        Vector2 stick = ArcadeControls.GetStick().Vector;
        AimVector = stick.sqrMagnitude > 1f ? stick.normalized : stick;

        ShootPressedThisFrame = ArcadeControls.GetButtonDown(ArcadeButtonColor.Black);
        ShootReleasedThisFrame = ArcadeControls.GetButtonUp(ArcadeButtonColor.Black);
        IsShootButtonPressed = ArcadeControls.GetButton(ArcadeButtonColor.Black);
        IsFineTuneButtonPressed = ArcadeControls.GetButton(ArcadeButtonColor.Green);
        CancelPressedThisFrame = ArcadeControls.GetButtonDown(ArcadeButtonColor.White);

        if (ShootPressedThisFrame) ShootPressed?.Invoke();
        if (ShootReleasedThisFrame) ShootReleased?.Invoke();
        if (CancelPressedThisFrame) CancelPressed?.Invoke();
    }

    void OnDisable()
    {
        AimVector = Vector2.zero;
        IsShootButtonPressed = false;
        IsFineTuneButtonPressed = false;
        ShootPressedThisFrame = false;
        ShootReleasedThisFrame = false;
        CancelPressedThisFrame = false;
    }
}
