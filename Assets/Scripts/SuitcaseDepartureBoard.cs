using TMPro;
using UnityEngine;

/// <summary>Departure-board controls share the map's selection, journey and saved progress.</summary>
[ExecuteAlways, DefaultExecutionOrder(50)]
public sealed class SuitcaseDepartureBoard : MonoBehaviour
{
    public TwilightIllustratedMenu view;
    public UnityEngine.UI.Button previousButton, nextButton;
    public TMP_Text tens, ones, caption;
    public RectTransform marker;
    public UnityEngine.UI.RawImage markerArt;
    int target = -1, from = -1;
    float flipStarted;
    bool bound;
    const float FlipSeconds = .18f;

    void OnEnable()
    {
        if (Application.isPlaying && previousButton != null && nextButton != null)
        {
            previousButton.onClick.AddListener(Previous);
            nextButton.onClick.AddListener(Next);
            bound = true;
        }
        Refresh(true);
    }

    void OnDisable()
    {
        if (!bound) return;
        previousButton.onClick.RemoveListener(Previous);
        nextButton.onClick.RemoveListener(Next);
        bound = false;
    }

    void LateUpdate()
    {
        Refresh();
        LayoutMenu();
    }

    public void Previous() => Step(-1);
    public void Next() => Step(1);

    void Step(int direction)
    {
        if (view == null || view.map == null) return;
        if (!view.map.StepSelection(direction)) return;
        Refresh();
    }

    public void Refresh(bool immediate = false)
    {
        if (view == null || view.map == null || tens == null || ones == null || marker == null) return;
        int selected = Mathf.Clamp(view.map.Selected, 0, view.stopRects.Length - 1);
        int number = selected + 1;
        if (number != target)
        {
            from = target < 0 ? number : target;
            target = number;
            flipStarted = Time.unscaledTime;
        }
        if (immediate || !Application.isPlaying)
        {
            from = target;
            flipStarted = Time.unscaledTime - FlipSeconds;
        }
        float progress = Mathf.Clamp01((Time.unscaledTime - flipStarted) / FlipSeconds);
        Digit(tens, from / 10, target / 10, progress);
        Digit(ones, from % 10, target % 10, progress);
        if (progress >= 1) from = target;
        caption.text = "LEVEL " + number + " / " + view.stopRects.Length;
        previousButton.interactable = selected > 0;
        nextButton.interactable = selected < view.stopRects.Length - 1;
        bool locked = selected > view.map.HighestUnlocked;
        Color ink = locked ? new Color(.65f, .64f, .58f) : new Color(1, .91f, .73f);
        tens.color = ones.color = ink;
        marker.anchoredPosition = view.stopRects[selected].anchoredPosition + Vector2.up * 24;
        markerArt.color = locked ? new Color(.7f, .65f, .5f, .8f) : Color.white;
    }

    static void Digit(TMP_Text label, int before, int after, float progress)
    {
        string value = (progress < .5f ? before : after).ToString();
        if (label.text != value) label.text = value;
        float height = before == after ? 1 : Mathf.Max(.02f, Mathf.Abs(Mathf.Cos(progress * Mathf.PI)));
        label.rectTransform.localScale = new Vector3(1, height, 1);
    }

    public void LayoutMenu()
    {
        if (view == null || view.artwork == null || view.canvas == null) return;
        float scale = view.artwork.localScale.x;
        if (scale <= 0) return;
        float width = ((RectTransform)view.canvas.transform).rect.width / scale;
        float leftCrop = Mathf.Max(0, (TwilightIllustratedMenu.ArtWidth - width) * .5f);
        // Laptop aspect ratios crop the outer leather. Keep Menu above the console there.
        var position = leftCrop > 20 ? new Vector2(leftCrop + 64, 742) : new Vector2(70, 887);
        ((RectTransform)view.backButton.transform).anchoredPosition =
            new Vector2(position.x - 836, 470.5f - position.y);
    }
}
