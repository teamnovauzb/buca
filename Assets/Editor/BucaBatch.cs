#if UNITY_EDITOR
/// <summary>
/// Shared flag so the "one click" master tool can run the individual one-shot tools
/// without each one popping its own confirmation dialog. When Silent is true, the
/// tools skip their success dialog; the master shows a single summary at the end.
/// </summary>
public static class BucaBatch
{
    public static bool Silent;
}
#endif
