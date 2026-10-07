#if UNITY_EDITOR
public static partial class BuildToyBoxMainMenu
{
    public static void PolishLearningTutorial()
    {
        // Keep text owned by the prefab. Scene-only replacement labels become
        // stale overrides when the tutorial presentation is updated later.
        PolishTeddyCoachTutorial();
        RepairTutorialScene();
    }
}
#endif
