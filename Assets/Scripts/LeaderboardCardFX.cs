using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the leaderboard card's visual flair while it's open:
/// rotating glow halo, scrolling shimmer line across the title bar,
/// and pulsing border outline. Driven entirely by Time.unscaledTime
/// so it keeps animating during the timeScale=0 Continue popup.
/// </summary>
public class LeaderboardCardFX : MonoBehaviour
{
    [Tooltip("Image at the back of the card that rotates slowly to add motion.")]
    public RectTransform glowHalo;
    [Tooltip("Image positioned across the title bar that translates left↔right as a shimmer.")]
    public RectTransform titleShimmer;
    [Tooltip("Outline image around the card — pulses alpha in and out.")]
    public Image borderOutline;

    [Header("Tuning")]
    public float haloSpinDegPerSec = 18f;
    public float shimmerCycleSeconds = 3.0f;
    public float shimmerTravelDistance = 700f;
    public float borderPulseSpeed = 1.6f;
    public float borderPulseMin = 0.4f;
    public float borderPulseMax = 1.0f;

    Color _borderBase = Color.white;

    void Awake()
    {
        if (borderOutline != null) _borderBase = borderOutline.color;
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (glowHalo != null)
        {
            float yaw = (t * haloSpinDegPerSec) % 360f;
            glowHalo.localRotation = Quaternion.Euler(0f, 0f, yaw);
        }

        if (titleShimmer != null)
        {
            // ping-pong from -shimmerTravelDistance/2 to +shimmerTravelDistance/2
            float phase = Mathf.Repeat(t / shimmerCycleSeconds, 1f);
            // Sawtooth then bounce — looks like a streak crossing the title
            float xn = Mathf.Sin(phase * Mathf.PI * 2f); // -1..1
            var p = titleShimmer.anchoredPosition;
            p.x = xn * shimmerTravelDistance * 0.5f;
            titleShimmer.anchoredPosition = p;
        }

        if (borderOutline != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * borderPulseSpeed * Mathf.PI);
            float a = Mathf.Lerp(borderPulseMin, borderPulseMax, pulse);
            var c = _borderBase; c.a = _borderBase.a * a;
            borderOutline.color = c;
        }
    }
}
