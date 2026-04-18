using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Simple joystick-driven UI navigation. Attach to any Canvas that has
/// Buttons. Joystick up/down cycles through selectable buttons,
/// JoystickButton0 (Black) confirms, JoystickButton9 (White) goes back.
///
/// Auto-detects: only activates when arcade input is detected via
/// LuxoddGameBridge.IsArcadeInputActive. Otherwise does nothing so
/// mouse/keyboard still works normally in the editor.
/// </summary>
public class ArcadeUINavigator : MonoBehaviour
{
    [Tooltip("Ordered list of buttons to navigate. First = default selected.")]
    public Selectable[] selectables;

    [Header("Timing")]
    [Tooltip("Seconds between joystick repeat-steps when held.")]
    public float repeatDelay = 0.25f;

    [Header("Visual feedback")]
    [Tooltip("Scale applied to the currently selected button.")]
    public float selectedScale = 1.08f;

    int _currentIndex;
    float _nextMoveTime;
    Selectable _lastHighlighted;

    void OnEnable()
    {
        _currentIndex = 0;
        if (selectables != null && selectables.Length > 0)
            HighlightCurrent();
    }

    void Update()
    {
        if (selectables == null || selectables.Length == 0) return;
        // Only run when arcade input is active
        if (!LuxoddGameBridge.IsArcadeInputActive) return;

        float vertical = Input.GetAxisRaw("Vertical");

        // Navigate up/down with repeat delay
        if (Time.unscaledTime >= _nextMoveTime)
        {
            if (vertical > 0.5f)
            {
                _currentIndex = (_currentIndex - 1 + selectables.Length) % selectables.Length;
                _nextMoveTime = Time.unscaledTime + repeatDelay;
                HighlightCurrent();
            }
            else if (vertical < -0.5f)
            {
                _currentIndex = (_currentIndex + 1) % selectables.Length;
                _nextMoveTime = Time.unscaledTime + repeatDelay;
                HighlightCurrent();
            }
        }

        // Reset repeat when joystick returns to center
        if (Mathf.Abs(vertical) < 0.2f)
            _nextMoveTime = 0f;

        // Black button = confirm
        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            var btn = selectables[_currentIndex] as Button;
            if (btn != null) btn.onClick.Invoke();
        }
    }

    void HighlightCurrent()
    {
        // Reset previous highlight scale
        if (_lastHighlighted != null)
            _lastHighlighted.transform.localScale = Vector3.one;

        var current = selectables[_currentIndex];
        if (current == null) return;

        // Set EventSystem selection for visual feedback (Button highlight state)
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(current.gameObject);

        // Scale bump
        current.transform.localScale = Vector3.one * selectedScale;
        _lastHighlighted = current;
    }

    void OnDisable()
    {
        // Reset scale on all
        if (selectables != null)
            foreach (var s in selectables)
                if (s != null) s.transform.localScale = Vector3.one;
    }
}
