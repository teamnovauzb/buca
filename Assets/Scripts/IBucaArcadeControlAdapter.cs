using System;
using UnityEngine;

/// <summary>
/// Gameplay-facing arcade actions for Buca.
///
/// The puck controller depends on these actions rather than knowing which
/// physical Luxodd buttons provide them. This keeps the cabinet mapping in one
/// place and follows the adapter boundary recommended by the Luxodd guide.
/// </summary>
public interface IBucaArcadeControlAdapter
{
    Vector2 AimVector { get; }
    bool IsShootButtonPressed { get; }
    bool IsFineTuneButtonPressed { get; }
    bool ShootPressedThisFrame { get; }
    bool ShootReleasedThisFrame { get; }
    bool CancelPressedThisFrame { get; }

    event Action ShootPressed;
    event Action ShootReleased;
    event Action CancelPressed;
}
