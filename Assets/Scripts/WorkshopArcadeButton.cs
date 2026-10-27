using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Independent arcade-button face; the existing Button owns all click actions.</summary>
[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class WorkshopArcadeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public UnityEngine.UI.RawImage face;
    public Rect raisedUV, pressedUV;
    public int direction;
    UnityEngine.UI.Button button;
    bool held, hovered;
    float releaseAt;

    void OnEnable() { button = GetComponent<UnityEngine.UI.Button>(); RefreshVisual(false); }
    void Update()
    {
        bool keyboard = false;
        if (direction == 0)
            keyboard = Input.GetKey(KeyCode.Return) || ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
        else
        {
            var stick = ArcadeInputAdapter.GetStick();
            if (Input.GetKey(KeyCode.LeftArrow)) stick.x = -1;
            if (Input.GetKey(KeyCode.RightArrow)) stick.x = 1;
            if (Input.GetKey(KeyCode.UpArrow)) stick.y = 1;
            if (Input.GetKey(KeyCode.DownArrow)) stick.y = -1;
            float axis = Mathf.Abs(stick.x) >= Mathf.Abs(stick.y) ? stick.x : -stick.y;
            keyboard = axis * direction > .55f || ArcadeInputAdapter.GetButton(
                direction < 0 ? ArcadeInputAdapter.Button.Green : ArcadeInputAdapter.Button.Blue);
        }
        RefreshVisual(held || keyboard || Time.unscaledTime < releaseAt);
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
        if (e.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;
        held = true;
        releaseAt = Time.unscaledTime + .10f;
        RefreshVisual(true);
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        held = false;
        RefreshVisual(Time.unscaledTime < releaseAt);
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; RefreshVisual(held); }
    public void OnPointerExit(PointerEventData e)
    {
        held = hovered = false; releaseAt = 0; RefreshVisual(false);
    }
    void OnDisable() { held = hovered = false; releaseAt = 0; RefreshVisual(false); }

    public void RefreshVisual(bool pressed)
    {
        if (face == null) return;
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
        bool enabled = button.IsInteractable();
        face.uvRect = enabled && pressed ? pressedUV : raisedUV;
        face.color = !enabled ? new Color(.52f, .52f, .52f, .85f)
            : hovered && !pressed ? new Color(1, 1, .9f, 1) : Color.white;
    }
}
