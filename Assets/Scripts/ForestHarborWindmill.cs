using UnityEngine;

/// <summary>Turns the four windmill sails around their fixed axle on the map.</summary>
public sealed class ForestHarborWindmill : MonoBehaviour
{
    public RectTransform blades;
    [Min(1)] public float revolutionSeconds = 24;
    float elapsed;

    void OnEnable() => Pose(elapsed);

    void Update()
    {
        if (!Application.isPlaying) return;
        elapsed += Time.unscaledDeltaTime;
        Pose(elapsed);
    }

    public void Pose(float seconds)
    {
        if (blades == null) return;
        // Continuous phase: small, gradual wind changes without a speed jump at each revolution.
        float period = Mathf.Max(1, revolutionSeconds);
        float turns = seconds / period;
        float wind = .012f * Mathf.Sin(seconds * .32f) + .004f * Mathf.Sin(seconds * .71f);
        blades.localRotation = Quaternion.Euler(0, 0, -360 * Mathf.Repeat(turns + wind, 1));
    }
}
