#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Compatibility shim for old editor tooling. Preferred-route markers are now
/// authored directly in their level prefabs by PrebuiltRuntimeContentBaker.
/// </summary>
public static class PreferredRouteBuilder
{
    public static void Install(GameObject levelRoot, int levelNumber)
    {
        Debug.LogWarning("PreferredRouteBuilder.Install is editor-only and no longer creates content. " +
                         "Run RealBuca/Prebuild/Step 3 - Preferred Routes instead.", levelRoot);
    }
}
#endif
