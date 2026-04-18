using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// First-play onboarding. Shows an animated "pull back and release" demo
/// the first time a player opens level 1. A ghost puck + ghost hand repeats
/// a drag-and-release motion until the player takes their first real shot,
/// at which point the tutorial fades out and the flag is persisted.
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

    [Header("Animation")]
    public float loopDuration = 1.8f;
    public float dragDistance = 240f;

    public const string PrefSeen = "BucaTutorialSeen";

    bool _active;
    Vector2 _puckBase, _handBase;

    public static bool Seen => PlayerPrefs.GetInt(PrefSeen, 0) == 1;

    void Awake()
    {
        if (ghostPuck != null) _puckBase = ghostPuck.anchoredPosition;
        if (ghostHand != null) _handBase = ghostHand.anchoredPosition;
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
            yield return PlayDemo();
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator PlayDemo()
    {
        // Phase 1: Press
        if (instructionText != null) instructionText.text = "PULL BACK";
        yield return null;

        // Phase 2: Drag back
        float dur = loopDuration * 0.55f, t = 0f;
        while (t < dur && _active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = 1f - Mathf.Pow(1f - k, 2f); // easeOutQuad
            if (ghostHand != null) ghostHand.anchoredPosition = _handBase + new Vector2(0f, -dragDistance * e);
            if (ghostPuck != null) ghostPuck.anchoredPosition = _puckBase; // puck stays put
            yield return null;
        }

        // Brief pause at full drag
        yield return new WaitForSeconds(0.2f);

        // Phase 3: Release — flick puck forward opposite of drag
        if (instructionText != null) instructionText.text = "RELEASE TO SHOOT";
        dur = loopDuration * 0.35f; t = 0f;
        while (t < dur && _active)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            if (ghostHand != null)
            {
                // Hand fades out / returns to base
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

        // Reset puck back to base quickly
        if (ghostPuck != null) ghostPuck.anchoredPosition = _puckBase;
        if (ghostHand != null) ghostHand.anchoredPosition = _handBase;
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
