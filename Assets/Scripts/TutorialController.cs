using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// First-play onboarding. Detects input mode and shows the matching demo:
///
///   • Mouse mode: ghost hand drags back, puck flies forward (existing)
///   • Arcade mode: joystick stick tilts → Black button presses + power arc
///     fills → Black releases + puck flies in aim direction (NEW)
///
/// Cycles indefinitely until the player takes their first real shot, at
/// which point the tutorial fades out and the "seen" flag is persisted to
/// PlayerPrefs. Re-detects input mode each cycle so it adapts on the fly
/// if the player switches between mouse and joystick.
///
/// Runs only when PlayerPrefs["BucaTutorialSeen"] is 0. Call MarkSeen() to
/// persist and hide; LevelManager invokes this after the first shot.
/// </summary>
public class TutorialController : MonoBehaviour
{
    [Header("Visuals (assigned in scene or by BucaSetupHelper)")]
    public CanvasGroup group;
    public RectTransform ghostPuck;
    public RectTransform ghostHand;
    public TMP_Text instructionText;

    [Header("Arcade-mode visuals (optional — built by BucaSetupHelper)")]
    [Tooltip("Image showing the joystick (with a tilting ball). Tilts during the AIM phase of the arcade demo.")]
    public RectTransform ghostJoystick;
    [Tooltip("Sub-element inside ghostJoystick representing the moving ball. Animates the tilt direction.")]
    public RectTransform ghostJoystickBall;
    [Tooltip("Image of the Black arcade button. Brightens during the CHARGE phase of the arcade demo.")]
    public Image ghostBlackButton;
    [Tooltip("Image of a power-arc ring that fills 0→1 during the CHARGE phase.")]
    public Image ghostPowerRing;

    [Header("Animation")]
    public float loopDuration = 1.8f;
    public float dragDistance = 240f;
    [Tooltip("How far (px) the joystick ball tilts during the arcade AIM phase.")]
    public float joystickTiltDistance = 24f;

    public const string PrefSeen = "BucaTutorialSeen";

    bool _active;
    Vector2 _puckBase, _handBase, _ballBase;
    Color _blackBtnDim = new Color(0.18f, 0.18f, 0.22f, 1f);
    Color _blackBtnLit = new Color(0.55f, 0.55f, 0.65f, 1f);

    public static bool Seen => PlayerPrefs.GetInt(PrefSeen, 0) == 1;

    void Awake()
    {
        if (ghostPuck != null) _puckBase = ghostPuck.anchoredPosition;
        if (ghostHand != null) _handBase = ghostHand.anchoredPosition;
        if (ghostJoystickBall != null) _ballBase = ghostJoystickBall.anchoredPosition;
        if (group != null) group.alpha = 0f;
    }

    void Start()
    {
        if (Seen) { if (group != null) group.alpha = 0f; gameObject.SetActive(false); return; }
        _active = true;
        StartCoroutine(DemoLoop());
    }

    IEnumerator DemoLoop()
    {
        // Fade in
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (group != null) group.alpha = Mathf.Clamp01(t / 0.5f);
            yield return null;
        }

        while (_active)
        {
            // Pick demo based on what input the player has been using.
            // ArcadeInputAdapter.ArcadeUsedThisSession latches true the first
            // time any joystick/button is detected — until then, mouse demo.
            // modeOverride lets you preview either mode for testing.
            bool arcade;
            switch (modeOverride)
            {
                case DemoModeOverride.ForceArcade: arcade = true; break;
                case DemoModeOverride.ForceMouse:  arcade = false; break;
                default: arcade = ArcadeInputAdapter.ArcadeUsedThisSession; break;
            }
            SetVisualMode(arcade);
            if (arcade) yield return PlayArcadeDemo();
            else        yield return PlayMouseDemo();
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>Toggle which visual stack is shown for the current demo mode.</summary>
    void SetVisualMode(bool arcade)
    {
        if (ghostHand != null) ghostHand.gameObject.SetActive(!arcade);
        if (ghostJoystick != null) ghostJoystick.gameObject.SetActive(arcade);
        if (ghostBlackButton != null) ghostBlackButton.gameObject.SetActive(arcade);
        if (ghostPowerRing != null) ghostPowerRing.gameObject.SetActive(arcade);
    }

    // ─────────────────────────────────────────────────────────
    // Mouse demo (existing behavior)
    // ─────────────────────────────────────────────────────────
    IEnumerator PlayMouseDemo()
    {
        if (instructionText != null) instructionText.text = "PULL BACK";
        yield return null;

        float dur = loopDuration * 0.55f, t = 0f;
        while (t < dur && _active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = 1f - Mathf.Pow(1f - k, 2f);
            if (ghostHand != null) ghostHand.anchoredPosition = _handBase + new Vector2(0f, -dragDistance * e);
            if (ghostPuck != null) ghostPuck.anchoredPosition = _puckBase;
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);

        if (instructionText != null) instructionText.text = "RELEASE TO SHOOT";
        dur = loopDuration * 0.35f; t = 0f;
        while (t < dur && _active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            if (ghostHand != null)
            {
                float eBack = 1f - Mathf.Pow(1f - k, 3f);
                ghostHand.anchoredPosition = Vector2.Lerp(
                    _handBase + new Vector2(0f, -dragDistance), _handBase, eBack);
            }
            if (ghostPuck != null)
            {
                float eFwd = 1f - Mathf.Pow(1f - k, 2f);
                ghostPuck.anchoredPosition = _puckBase + new Vector2(0f, dragDistance * 0.9f * eFwd);
            }
            yield return null;
        }

        if (ghostPuck != null) ghostPuck.anchoredPosition = _puckBase;
        if (ghostHand != null) ghostHand.anchoredPosition = _handBase;
    }

    // ─────────────────────────────────────────────────────────
    // Arcade demo: joystick → Black hold + power fill → release
    // ─────────────────────────────────────────────────────────
    IEnumerator PlayArcadeDemo()
    {
        // Reset visuals to neutral
        if (ghostJoystickBall != null) ghostJoystickBall.anchoredPosition = _ballBase;
        if (ghostBlackButton != null) ghostBlackButton.color = _blackBtnDim;
        if (ghostPowerRing != null) ghostPowerRing.fillAmount = 0f;
        if (ghostPuck != null) ghostPuck.anchoredPosition = _puckBase;

        // Phase 1 — TILT JOYSTICK TO AIM (~30% of cycle)
        if (instructionText != null) instructionText.text = "TILT TO AIM";
        float dur = loopDuration * 0.30f, t = 0f;
        while (t < dur && _active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = 1f - Mathf.Pow(1f - k, 2f);
            // Tilt the ball UP (the aim direction we'll launch in)
            if (ghostJoystickBall != null)
                ghostJoystickBall.anchoredPosition = _ballBase + new Vector2(0f, joystickTiltDistance * e);
            yield return null;
        }

        // Phase 2 — HOLD BLACK / CHARGE (~45% of cycle)
        if (instructionText != null) instructionText.text = "HOLD  ●  TO CHARGE";
        dur = loopDuration * 0.45f; t = 0f;
        while (t < dur && _active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            // Black button "pressed" — brighten + slight scale pulse
            if (ghostBlackButton != null)
            {
                ghostBlackButton.color = Color.Lerp(_blackBtnDim, _blackBtnLit, k);
                ghostBlackButton.transform.localScale = Vector3.one * (1f + 0.08f * k);
            }
            // Power ring fills 0 → 1
            if (ghostPowerRing != null)
                ghostPowerRing.fillAmount = k;
            yield return null;
        }

        // Phase 3 — RELEASE → puck flies in aim direction (~25% of cycle)
        if (instructionText != null) instructionText.text = "RELEASE TO FIRE";
        dur = loopDuration * 0.25f; t = 0f;
        while (t < dur && _active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = 1f - Mathf.Pow(1f - k, 2f);
            if (ghostBlackButton != null)
            {
                // Snap back to dim
                ghostBlackButton.color = Color.Lerp(_blackBtnLit, _blackBtnDim, k);
                ghostBlackButton.transform.localScale = Vector3.Lerp(
                    Vector3.one * 1.08f, Vector3.one, k);
            }
            if (ghostPowerRing != null)
                ghostPowerRing.fillAmount = 1f - k; // empty as the puck flies
            if (ghostJoystickBall != null)
                ghostJoystickBall.anchoredPosition = Vector2.Lerp(
                    _ballBase + new Vector2(0f, joystickTiltDistance), _ballBase, k);
            // Puck flies UP (matches the aim direction)
            if (ghostPuck != null)
                ghostPuck.anchoredPosition = _puckBase + new Vector2(0f, dragDistance * 0.9f * e);
            yield return null;
        }

        // Reset for next loop
        if (ghostPuck != null) ghostPuck.anchoredPosition = _puckBase;
        if (ghostJoystickBall != null) ghostJoystickBall.anchoredPosition = _ballBase;
        if (ghostBlackButton != null)
        {
            ghostBlackButton.color = _blackBtnDim;
            ghostBlackButton.transform.localScale = Vector3.one;
        }
        if (ghostPowerRing != null) ghostPowerRing.fillAmount = 0f;
    }

    [Header("Mode override (debug / preview)")]
    [Tooltip("None = auto-detect (mouse vs arcade based on input). " +
             "ForceArcade / ForceMouse = lock the demo to that mode regardless of input.")]
    public DemoModeOverride modeOverride = DemoModeOverride.None;
    public enum DemoModeOverride { None, ForceMouse, ForceArcade }

    /// <summary>
    /// Editor-only: right-click the TutorialController component in Play
    /// mode → "Preview Arcade Demo" to see the joystick/button/ring demo
    /// without needing real arcade hardware. Useful for design + screenshots.
    /// </summary>
    [ContextMenu("Preview Arcade Demo")]
    public void PreviewArcadeDemo()
    {
        modeOverride = DemoModeOverride.ForceArcade;
        PlayerPrefs.DeleteKey(PrefSeen);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        StopAllCoroutines();
        _active = true;
        if (group != null) group.alpha = 1f;
        StartCoroutine(DemoLoop());
    }

    [ContextMenu("Preview Mouse Demo")]
    public void PreviewMouseDemo()
    {
        modeOverride = DemoModeOverride.ForceMouse;
        PlayerPrefs.DeleteKey(PrefSeen);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        StopAllCoroutines();
        _active = true;
        if (group != null) group.alpha = 1f;
        StartCoroutine(DemoLoop());
    }

    /// <summary>
    /// Called by LevelManager right after the player's first real shot.
    /// Fades the tutorial out and persists the "seen" flag.
    /// </summary>
    public void MarkSeen()
    {
        if (!_active) return;
        _active = false;
        PlayerPrefs.SetInt(PrefSeen, 1);
        PlayerPrefs.Save();
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        float t = 0f;
        float startAlpha = group != null ? group.alpha : 1f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            if (group != null) group.alpha = Mathf.Lerp(startAlpha, 0f, t / 0.4f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
