using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen hollow-vignette overlay. Two Images are layered on a
/// canvas covering the whole screen — one yellow (near hole / magnet),
/// one pink (near death / high speed). LevelManager drives their
/// alpha via SetIntensities() each frame.
///
/// The images should use a texture shaped like a "hollow" radial gradient
/// (opaque at edges, transparent in the middle). BucaSetupHelper builds
/// this texture procedurally so no art assets are needed.
/// </summary>
public class EdgeGlowEffect : MonoBehaviour
{
    public Image yellowGlow;
    public Image pinkGlow;

    [Header("Intensity smoothing")]
    [Tooltip("Higher = reacts faster to state changes.")]
    public float lerpSpeed = 8f;
    [Tooltip("Max alpha the glow reaches (0–1).")]
    public float maxAlpha = 0.6f;

    float _yellowTarget, _pinkTarget;
    float _yellowCurrent, _pinkCurrent;

    public void SetIntensities(float yellow, float pink)
    {
        _yellowTarget = Mathf.Clamp01(yellow);
        _pinkTarget = Mathf.Clamp01(pink);
    }

    void Update()
    {
        _yellowCurrent = Mathf.Lerp(_yellowCurrent, _yellowTarget, Time.deltaTime * lerpSpeed);
        _pinkCurrent = Mathf.Lerp(_pinkCurrent, _pinkTarget, Time.deltaTime * lerpSpeed);

        // Mild breathing pulse on the active glow for life
        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 3.8f);

        if (yellowGlow != null)
        {
            var c = yellowGlow.color;
            c.a = _yellowCurrent * maxAlpha * pulse;
            yellowGlow.color = c;
        }
        if (pinkGlow != null)
        {
            var c = pinkGlow.color;
            c.a = _pinkCurrent * maxAlpha * pulse;
            pinkGlow.color = c;
        }
    }
}
