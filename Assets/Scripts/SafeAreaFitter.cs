using UnityEngine;
using Luxodd.Game.Scripts.Runtime.Viewport.Context;

/// <summary>
/// Keeps a fixed-design UI card fully inside the visible safe area on ANY
/// screen size / aspect ratio. It uniformly scales the target down (never up
/// past <see cref="maxScale"/>) so the whole card fits inside the safe rect
/// minus padding, and re-centers it between the safe-area insets — so the top
/// never slips under a device border / notch and nothing is clipped off the
/// edges.
///
/// The card's internal layout is untouched — only its localScale and
/// anchoredPosition change, so the visual design is preserved exactly.
///
/// Safe-area source (largest inset per side wins):
///   • Luxodd WebGL visualViewport insets — the arcade cabinet's real safe
///     area (Unity WebGL does NOT populate Screen.safeArea, so this is the
///     only truthful source on the cabinet).
///   • Unity Screen.safeArea — native notches; full-screen everywhere else.
/// Both are read as FRACTIONS of the viewport, so the result is resolution-
/// and devicePixelRatio-independent.
/// </summary>
[DisallowMultipleComponent]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("The card to scale + position. Defaults to this GameObject's RectTransform.")]
    public RectTransform target;

    [Tooltip("Full-screen reference rect (stretch-stretch). Defaults to target's parent.")]
    public RectTransform area;

    [Tooltip("The card's design size at scale 1. Captured from the target on Awake if left at zero.")]
    public Vector2 designSize;

    [Header("Padding inside the safe area (design px)")]
    public float padTop = 48f;
    public float padBottom = 40f;
    public float padSides = 24f;

    [Header("Scale clamp")]
    [Tooltip("Never shrink below this — a floor so a bogus huge inset can't collapse the card.")]
    public float minScale = 0.4f;
    [Tooltip("Never grow past this — 1 keeps the card at its design size on large screens.")]
    public float maxScale = 1f;

    [Tooltip("Seconds between safe-area re-checks (browser chrome can show/hide, changing insets).")]
    public float pollInterval = 0.25f;

    [Tooltip("Extra multiplier composed on top of the fit scale — used by open/close " +
             "animations (e.g. the level-select pop) so they don't fight the fitter. " +
             "Leave at 1 when not animating.")]
    [HideInInspector] public float animScale = 1f;

    float _nextPoll;

    void Awake()
    {
        if (target == null) target = transform as RectTransform;
        if (area == null && target != null) area = target.parent as RectTransform;
        if (designSize == Vector2.zero && target != null) designSize = target.rect.size;
    }

    void OnEnable() => Apply();
    void OnRectTransformDimensionsChange() => Apply();

    void Update()
    {
        // Fixed-size cards don't fire OnRectTransformDimensionsChange when the
        // canvas resizes, so poll to catch resize / rotation / inset changes.
        if (Time.unscaledTime < _nextPoll) return;
        _nextPoll = Time.unscaledTime + Mathf.Max(0.05f, pollInterval);
        Apply();
    }

    /// <summary>Re-fit now. Safe to call every frame.</summary>
    public void Apply()
    {
        if (target == null) return;
        if (area == null) area = target.parent as RectTransform;
        if (area == null) return;
        if (designSize == Vector2.zero) designSize = target.rect.size;
        if (designSize.x <= 0f || designSize.y <= 0f) return;

        float areaW = area.rect.width;
        float areaH = area.rect.height;
        if (areaW <= 0f || areaH <= 0f) return;

        GetInsetFractions(out float lf, out float tf, out float rf, out float bf);

        float leftIn   = lf * areaW;
        float rightIn  = rf * areaW;
        float topIn    = tf * areaH;
        float bottomIn = bf * areaH;

        float safeW = areaW - leftIn - rightIn - 2f * padSides;
        float safeH = areaH - topIn - bottomIn - padTop - padBottom;
        if (safeW <= 1f || safeH <= 1f) return;

        float scale = Mathf.Min(safeW / designSize.x, safeH / designSize.y);
        scale = Mathf.Clamp(scale, minScale, maxScale);

        // Center of the safe rect, expressed as an offset from the area center.
        // A larger top inset pushes the card DOWN; a larger left inset pushes it RIGHT.
        float cx = (leftIn - rightIn) * 0.5f;
        float cy = (bottomIn - topIn) * 0.5f;

        float s = scale * (animScale <= 0f ? 1f : animScale);
        target.localScale = new Vector3(s, s, 1f);
        target.anchoredPosition = new Vector2(cx, cy);
    }

    void GetInsetFractions(out float left, out float top, out float right, out float bottom)
    {
        left = top = right = bottom = 0f;

        // 1) Luxodd WebGL visualViewport insets (the arcade cabinet). Returns
        //    all-zero off-WebGL, so this branch is a no-op in the editor.
        var vv = LuxoddRuntimeContext.GetVisualViewportInsets();
        if (vv.ViewWidth > 0f && vv.ViewHeight > 0f)
        {
            left   = Mathf.Max(left,   vv.Left   / vv.ViewWidth);
            right  = Mathf.Max(right,  vv.Right  / vv.ViewWidth);
            top    = Mathf.Max(top,    vv.Top    / vv.ViewHeight);
            bottom = Mathf.Max(bottom, vv.Bottom / vv.ViewHeight);
        }

        // 2) Unity Screen.safeArea (native notches; full-screen otherwise).
        var sa = Screen.safeArea;
        float sw = Screen.width, sh = Screen.height;
        if (sw > 0f && sh > 0f)
        {
            left   = Mathf.Max(left,   sa.xMin / sw);
            right  = Mathf.Max(right,  (sw - sa.xMax) / sw);
            bottom = Mathf.Max(bottom, sa.yMin / sh);
            top    = Mathf.Max(top,    (sh - sa.yMax) / sh);
        }

        // Guard against a bogus inset collapsing the card (never allow a single
        // side to eat more than 40% of the viewport).
        left   = Mathf.Clamp(left,   0f, 0.4f);
        right  = Mathf.Clamp(right,  0f, 0.4f);
        top    = Mathf.Clamp(top,    0f, 0.4f);
        bottom = Mathf.Clamp(bottom, 0f, 0.4f);
    }
}
