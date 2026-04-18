using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated ghost-arrow UI hint shown when the puck is idle and waiting
/// for input. Replaces the "DRAG BACK TO AIM" text hint. The player
/// sets up the arrow in the scene (any UI Image works) and assigns it
/// to the `arrow` field — this component animates its position,
/// rotation, and alpha to pulse downward/outward, cueing the drag.
///
/// Assign the same arrow reference to LevelManager.dragHintArrow so
/// it fades in/out with the same idle-puck visibility rules.
/// </summary>
public class DragArrowHint : MonoBehaviour
{
    [Tooltip("Image used as the arrow. Any sprite works — a simple " +
             "triangle or chevron reads best.")]
    public RectTransform arrow;
    public Graphic arrowGraphic; // Image or RawImage — fades via .color.a

    [Header("Motion")]
    [Tooltip("How far the arrow slides down during the drag loop.")]
    public float slideDistance = 120f;
    [Tooltip("Seconds for one full slide-down loop.")]
    public float loopDuration = 1.1f;
    [Tooltip("Alpha at the brightest point of the loop.")]
    public float peakAlpha = 0.9f;

    [HideInInspector] public float externalAlpha = 1f; // set by LevelManager

    Vector2 _basePos;
    float _t;

    void Awake()
    {
        if (arrow != null) _basePos = arrow.anchoredPosition;
    }

    void Update()
    {
        if (arrow == null || arrowGraphic == null) return;

        _t += Time.deltaTime / loopDuration;
        if (_t > 1f) _t -= 1f;

        // Slide from top (0) to bottom (1), fading in then out
        float slide = _t;
        float alphaEnvelope = Mathf.Sin(_t * Mathf.PI); // 0 → 1 → 0

        arrow.anchoredPosition = _basePos + new Vector2(0f, -slideDistance * slide);
        var c = arrowGraphic.color;
        c.a = peakAlpha * alphaEnvelope * externalAlpha;
        arrowGraphic.color = c;
    }
}
