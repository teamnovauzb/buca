using System;
using UnityEngine;

/// <summary>Displays the approved artwork unchanged; input lives over its printed button.</summary>
public sealed class HoneyComparisonTutorial : MonoBehaviour
{
    public const string SeenKey = "BucaHoneyComparisonSeenV1";
    public UnityEngine.UI.Button playButton;
    public UnityEngine.UI.RawImage artwork;
    Action _finished;
    bool _awaitNeutral;
    bool _showing;
    float _entrance;
    CanvasGroup _group;
    public RectTransform weakPuck, strongPuck;
    public UnityEngine.UI.Image weakPower, strongPower;
    float _demoTime;

    public static bool TryShow(Action finished)
    {
        var prefab = Resources.Load<HoneyComparisonTutorial>("Tutorials/HoneyComparisonTutorial");
        if (prefab == null) return false;
        var view = Instantiate(prefab);
        view._finished = finished;
        view._showing = true;
        view._awaitNeutral = true;
        view._group = view.artwork.GetComponent<CanvasGroup>();
        view._group.alpha = 0;
        view.artwork.rectTransform.localScale = Vector3.one * .94f;
        view.playButton.interactable = false;
        view.playButton.onClick.AddListener(view.Continue);
        return true;
    }

    void Update()
    {
        if (!_showing) return;
        _entrance = Mathf.Min(1f, _entrance + Time.unscaledDeltaTime / .65f);
        float ease = 1f - Mathf.Pow(1f - _entrance, 3f);
        _group.alpha = ease;
        artwork.rectTransform.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, ease);
        artwork.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-2.5f, 0, ease));
        if (_entrance >= 1) _demoTime += Time.unscaledDeltaTime;
        if (Input.GetKeyDown(KeyCode.R)) _demoTime = 0;
        SampleDemonstration(_demoTime);
        if (_awaitNeutral)
        {
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space)
                || ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)) return;
            _awaitNeutral = false;
            playButton.interactable = true;
            return;
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)
            || ArcadeInputAdapter.ConfirmDown()) Continue();
    }

    public void Continue()
    {
        if (!_showing || _awaitNeutral) return;
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Close();
        Destroy(gameObject);
    }

    public void SampleDemonstration(float seconds)
    {
        if (weakPuck == null || strongPuck == null) return;
        // Charge together, then show why the short shot stalls while the stronger one succeeds.
        float t = Mathf.Repeat(Mathf.Max(0, seconds), 8f);
        weakPower.fillAmount = Mathf.Min(.30f, t / 1.8f);
        strongPower.fillAmount = Mathf.Min(.85f, t / 1.8f);
        float weakTravel = Mathf.Clamp01((t - 1.8f) / 1.6f);
        weakTravel = 1f - Mathf.Pow(1f - weakTravel, 3f);
        SetPuck(weakPuck, Vector2.Lerp(new Vector2(360,637), new Vector2(465,566), weakTravel));
        float strongTravel = Mathf.Clamp01((t - 1.8f) / 2.5f);
        SetPuck(strongPuck, Vector2.Lerp(new Vector2(974,624), new Vector2(1187,466), strongTravel));
        float sink = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 4.3f) / .45f));
        strongPuck.localScale = Vector3.one * (1f - sink);
        weakPuck.localScale = Vector3.one;
    }

    static void SetPuck(RectTransform puck, Vector2 pixel)
    {
        Vector2 center = new Vector2(pixel.x / 1536f, 1f - pixel.y / 1024f);
        Vector2 half = new Vector2(27f / 1536f, 22f / 1024f);
        puck.anchorMin = center - half; puck.anchorMax = center + half;
        puck.offsetMin = puck.offsetMax = Vector2.zero;
    }

    void Close()
    {
        if (!_showing) return;
        _showing = false;
        var callback = _finished;
        _finished = null;
        callback?.Invoke();
    }

    void OnDisable() { _showing = false; _finished = null; }
}
