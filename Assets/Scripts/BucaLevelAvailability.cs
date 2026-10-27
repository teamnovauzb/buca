using UnityEngine;

/// <summary>Playable campaign bounds; saved progress unlocks World 2 after level 30.</summary>
public static class BucaLevelAvailability
{
    public static readonly int PlayableLevelCount = 50;
    public const string AllLevelsLockedKey="BucaAllLevelsLocked";
    public const string ManualLockPrefix = "BucaLevelManualLock_";
    public static bool IsManuallyLocked(int index) =>
        // Entering World 2 proves level 30 was completed; an old preview lock must not block replay.
        PlayerPrefs.GetInt(AllLevelsLockedKey,0)!=0 ||
        !(index == 29 && HighestUnlocked >= 30) && PlayerPrefs.GetInt(ManualLockPrefix + index, 0) != 0;

    public static int ClampPlayableIndex(int index) =>
        Mathf.Clamp(index, 0, PlayableLevelCount - 1);

    public static int HighestUnlocked => ClampPlayableIndex(
        PlayerPrefs.GetInt(LevelSelectController.HighestUnlockedKey, 0));
}
