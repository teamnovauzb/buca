using UnityEngine;

/// <summary>
/// Editor-authored metadata describing the intended difficulty of one level.
/// It has no runtime gameplay behavior; it makes all 30 profiles inspectable.
/// </summary>
public sealed class LevelDifficultyProfile : MonoBehaviour
{
    public int balanceVersion = 2;
    [Range(1, 30)] public int levelNumber = 1;
    [Range(1, 5)] public int chapter = 1;
    [Range(1, 5)] public int difficultyRating = 1;
    [Range(1, 30)] public int difficultyIndex = 1;
    [TextArea] public string mechanicHint;
}
