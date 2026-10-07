using UnityEngine;

/// <summary>
/// Per-level config: time limit, par strokes for star ratings, etc.
/// Create one asset per level via Assets → Create → Buca → Level Settings,
/// or just assign the values directly in the array on LevelManager.
/// </summary>
[CreateAssetMenu(fileName = "LevelSettings", menuName = "Buca/Level Settings")]
public class LevelSettings : ScriptableObject
{
    [Tooltip("Time in seconds the player has to sink the puck. 0 = no limit.")]
    public float timeLimit = 30f;

    [Tooltip("Level par and the stroke target for 3 stars. Above par = 2 stars, above 2× par = 1 star.")]
    public int threeStarStrokes = 2;

    [Tooltip("Starting move allowance. A missed shot consumes one move; the campaign starts with 5 and adds 1 per level after level 5.")]
    [Min(1)]
    public int maxLives = 3;

    [Tooltip("Optional display name override (e.g. 'THE FUNNEL').")]
    public string displayName;
}
