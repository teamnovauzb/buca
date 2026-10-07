#if UNITY_EDITOR
using UnityEditor;

/// <summary>Legacy menu entry delegates to the current, layout-specific campaign balance.</summary>
public static class RebalanceAllLevelDifficulties
{
    [MenuItem("RealBuca/Rebalance All 30 Levels")]
    public static void Apply() => CampaignDifficultyBalance.Apply();
}
#endif
