using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Joystick + arrow-key driven UI navigation. Attach to any Canvas that
/// has Buttons. Joystick up/down (or up/down arrows) cycles through the
/// selectables, BLACK button (or Enter) confirms.
///
/// Visible highlight strategy: every frame we ENFORCE that the focused
/// button has both an explicit scale bump AND a pulsing colored outline
/// drawn behind it. This makes the focus state unambiguous regardless
/// of what the button's own ColorBlock is set to (default Unity tints
/// are nearly invisible on already-bright buttons).
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
    public float selectedScale = 1.15f;
    [Tooltip("Outline color drawn behind the selected button. Pulses alpha.")]
    public Color highlightOutlineColor = new Color(1f, 0.85f, 0.30f, 1f);
    [Tooltip("Thickness of the highlight outline (px).")]
    public float highlightOutlinePadding = 14f;
    [Tooltip("Pulse frequency in Hz for the outline alpha.")]
    public float pulseHz = 2.5f;

    int _currentIndex;
    float _nextMoveTime;
    Selectable _lastHighlighted;
    GameObject _outlineGO;
    Image _outlineImg;

    // Edge-detection state — one tap = one move. Without this, holding the
    // joystick / arrow key for 0.4s would auto-repeat past intermediate
    // buttons (e.g. PLAY → LEVELS → QUIT in two ticks, visibly skipping
    // LEVELS). Edge detection requires the player to release between moves.
    bool _vertWasUp;
    bool _vertWasDown;

    void OnEnable()
    {
        EnsureOutline();
        // Self-heal: if the inspector array is empty, has nulls, or is missing
        // buttons that exist on this canvas, rediscover everything fresh. This
        // covers the case where Setup ran BEFORE a button (like LEVELS) was
        // created, leaving a stale selectables array that skips that button.
        RediscoverIfStale();

        // Force-disable Unity's built-in Automatic navigation on every
        // managed button. Spatial nav silently overrides our manual index
        // (PLAY→QUIT skipping LEVELS). This is the runtime safety net for
        // scenes that haven't been re-set-up after the navigator change.
        if (selectables != null)
        {
            foreach (var s in selectables)
            {
                if (s == null) continue;
                var nav = s.navigation;
                if (nav.mode != Navigation.Mode.None)
                {
                    nav.mode = Navigation.Mode.None;
                    s.navigation = nav;
                }
            }
        }

        _currentIndex = 0;
        if (selectables != null && selectables.Length > 0)
            HighlightCurrent();
    }

    void RediscoverIfStale()
    {
        // Find every Button that's not inside the LevelSelect panel
        var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
        if (canvas == null) return;
        var allBtns = canvas.GetComponentsInChildren<Button>(true);
        var fresh = new System.Collections.Generic.List<Button>();
        foreach (var b in allBtns)
        {
            if (b == null) continue;
            if (b.GetComponentInParent<LevelSelectController>() != null) continue;
            fresh.Add(b);
        }
        // Sort top-to-bottom by world-space Y so up/down navigation feels right
        fresh.Sort((a, b) =>
            ((RectTransform)b.transform).position.y.CompareTo(
                ((RectTransform)a.transform).position.y));

        bool stale = selectables == null || selectables.Length != fresh.Count;
        if (!stale && selectables != null)
        {
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i] == null) { stale = true; break; }
                if (i >= fresh.Count || selectables[i] != fresh[i]) { stale = true; break; }
            }
        }
        if (stale)
        {
            Debug.Log($"[ArcadeUINavigator] Selectables list was stale — rediscovered " +
                      $"{fresh.Count} button{(fresh.Count == 1 ? "" : "s")}: " +
                      $"{string.Join(", ", fresh.ConvertAll(b => b.name))}");
            selectables = fresh.ToArray();
        }
    }

    void Update()
    {
        if (selectables == null || selectables.Length == 0) return;

        // STEP 1 (was previously SyncIndexFromEventSystem) — moved BELOW
        // the input handling. Reason: when Unity's StandaloneInputModule
        // fires move events from the same input that we're reading here,
        // Sync would pull our index to whatever the input module landed
        // on FIRST, then our increment would advance from THAT — skipping
        // the button between. Now: we read input + move our index, THEN
        // assert our index onto the EventSystem at the bottom of Update.

        // STEP 2 — joystick + arrow-key navigation (EDGE-TRIGGERED).
        // Each move requires a fresh push: vertical must cross from below
        // 0.5 → above 0.5 (or vice-versa). Continuous holding does NOT
        // auto-repeat — that prevented users from cleanly landing on a
        // mid-list option like LEVELS without skipping past it.
        float vertical = ArcadeInputAdapter.GetStick().y;
        if (Input.GetKey(KeyCode.UpArrow))   vertical =  1f;
        if (Input.GetKey(KeyCode.DownArrow)) vertical = -1f;

        bool vertUp   = vertical >  0.5f;
        bool vertDown = vertical < -0.5f;

        if (vertUp && !_vertWasUp)
        {
            _currentIndex = (_currentIndex - 1 + selectables.Length) % selectables.Length;
            HighlightCurrent();
        }
        else if (vertDown && !_vertWasDown)
        {
            _currentIndex = (_currentIndex + 1) % selectables.Length;
            HighlightCurrent();
        }
        _vertWasUp = vertUp;
        _vertWasDown = vertDown;

        // STEP 3 — confirm. ALSO require the EventSystem's selected GO to
        // match what we think is focused; if mouse hover landed on something
        // outside our selectables (or nothing), we don't fire any button.
        if (ArcadeInputAdapter.ConfirmDown() || Input.GetKeyDown(KeyCode.Return))
        {
            var current = selectables[_currentIndex];
            var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            // Sanity gate: only invoke when EventSystem selection truly matches our index,
            // OR the EventSystem has no selection yet (cold-start case).
            if (current != null && (sel == null || sel == current.gameObject))
            {
                var btn = current as Button;
                if (btn != null) btn.onClick.Invoke();
            }
            else if (current != null)
            {
                Debug.LogWarning($"[ArcadeUINavigator] Confirm ignored: nav focus is " +
                                 $"'{current.name}' but EventSystem-selected is " +
                                 $"'{(sel != null ? sel.name : "null")}'. " +
                                 "Mouse hover diverged from gamepad nav — re-navigate first.");
            }
        }

        // STEP 4 — sync from EventSystem (mouse hover support). Now that
        // our move has already been applied, this only kicks in when the
        // mouse hovers a different button than gamepad nav landed on.
        SyncIndexFromEventSystem();

        // Re-apply visuals every frame so the pulse animates AND any
        // accidental scale resets get re-asserted.
        ApplyHighlightVisuals();
    }

    void SyncIndexFromEventSystem()
    {
        if (EventSystem.current == null) return;
        var sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return;
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null && selectables[i].gameObject == sel)
            {
                if (_currentIndex != i)
                {
                    _currentIndex = i;
                    HighlightCurrent();
                }
                return;
            }
        }
    }

    void HighlightCurrent()
    {
        // Reset previous highlight scale
        if (_lastHighlighted != null && _lastHighlighted != selectables[_currentIndex])
            _lastHighlighted.transform.localScale = Vector3.one;

        var current = selectables[_currentIndex];
        if (current == null) return;

        // Set EventSystem selection so Unity's built-in highlight kicks in too
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(current.gameObject);

        _lastHighlighted = current;

        // Reposition outline immediately under the new selection so it
        // doesn't briefly draw at the old button's position before the
        // per-frame ApplyHighlightVisuals runs.
        ApplyHighlightVisuals();
    }

    void ApplyHighlightVisuals()
    {
        if (_lastHighlighted == null) return;

        // Scale bump on the selected button
        _lastHighlighted.transform.localScale = Vector3.one * selectedScale;

        // Move + size the outline image to wrap the selected button
        if (_outlineGO != null && _outlineImg != null)
        {
            _outlineGO.SetActive(true);
            var srcRT = (RectTransform)_lastHighlighted.transform;
            var outRT = (RectTransform)_outlineGO.transform;

            outRT.SetParent(srcRT.parent, false);
            // Render the outline behind the selected button
            outRT.SetSiblingIndex(srcRT.GetSiblingIndex());

            // Match the button's rect, expanded by `highlightOutlinePadding`
            outRT.anchorMin = srcRT.anchorMin;
            outRT.anchorMax = srcRT.anchorMax;
            outRT.pivot = srcRT.pivot;
            outRT.anchoredPosition = srcRT.anchoredPosition;
            outRT.sizeDelta = srcRT.sizeDelta + Vector2.one * highlightOutlinePadding * 2f;
            outRT.localScale = Vector3.one * selectedScale;

            // Pulse alpha 0.35 → 1.0 at pulseHz
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * pulseHz);
            var c = highlightOutlineColor;
            c.a = Mathf.Lerp(0.35f, 1f, p) * highlightOutlineColor.a;
            _outlineImg.color = c;
        }
    }

    void EnsureOutline()
    {
        if (_outlineGO != null) return;
        _outlineGO = new GameObject("ArcadeNavOutline", typeof(RectTransform));
        _outlineImg = _outlineGO.AddComponent<Image>();
        _outlineImg.sprite = BuildHollowRingSprite();
        _outlineImg.type = Image.Type.Sliced;
        _outlineImg.raycastTarget = false;
        _outlineImg.color = highlightOutlineColor;
        _outlineGO.SetActive(false);
    }

    void OnDisable()
    {
        // Reset scale on all
        if (selectables != null)
            foreach (var s in selectables)
                if (s != null) s.transform.localScale = Vector3.one;
        if (_outlineGO != null) _outlineGO.SetActive(false);
    }

    /// <summary>
    /// Procedural hollow rounded-rectangle ring sprite — drawn solid on
    /// the perimeter, transparent in the middle. 9-sliced so it scales
    /// to any rect without distortion.
    /// </summary>
    static Sprite BuildHollowRingSprite()
    {
        const int size = 64, corner = 16, thickness = 6;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // Distance to nearest edge
            float dx = Mathf.Min(x, size - 1 - x);
            float dy = Mathf.Min(y, size - 1 - y);
            float d  = Mathf.Min(dx, dy);

            // Corner rounding — only round the actual corner pixels
            bool inCorner = (x < corner && y < corner)
                         || (x < corner && y >= size - corner)
                         || (x >= size - corner && y < corner)
                         || (x >= size - corner && y >= size - corner);
            if (inCorner)
            {
                float cx = x < corner ? corner : size - 1 - corner;
                float cy = y < corner ? corner : size - 1 - corner;
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                d = Mathf.Min(d, corner - dist);
            }

            // Hollow ring: alpha=1 only inside the perimeter band
            float a = 0f;
            if (d >= 0f && d <= thickness)
                a = 1f - Mathf.Abs(d - thickness * 0.5f) / (thickness * 0.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(corner, corner, corner, corner));
    }
}
