using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Luxodd.Game.Scripts.Runtime.Viewport.Context;

/// <summary>Art-backed presentation of the existing railway selector; progress and travel remain owned by the map.</summary>
[ExecuteAlways]
public sealed class TwilightIllustratedMenu : MonoBehaviour
{
    public TwilightRailwayMap map;
    public Canvas canvas;
    public RectTransform artwork, controls, locomotive;
    public RectTransform[] stopRects;
    public UnityEngine.UI.Button[] stopButtons;
    public UnityEngine.UI.Image[] faces, glows;
    public TMP_Text[] numbers;
    public GameObject[] locks, awards;
    public Vector2[] route;
    public UnityEngine.UI.Button playButton, backButton, fullscreenButton;
    public TMP_Text playText, statusText, countdownText, fullscreenText;
    public Sprite ivoryFace, lockedFace, selectedFace;
    public bool suitcasePresentation;
    public bool forestHarborPresentation;
    [Range(.7f, 1f)] public float boardScreenScale = 1f;
    public float forestTrainGroundOffset = 18.7f;
    public float forestTrainRailOffset;
    [Range(.4f,1f)] public float forestTrainDisplayScale = .72f;
    public const float ArtWidth = 1672, ArtHeight = 941;
    int lastSelected = -1, lastHighest = -1, lastSecond = -1;
    float nextProgress;
    bool bound, lastMoving;
    float entranceTime = 1;
    CanvasGroup entranceGroup;
    public bool Entering => Application.isPlaying && forestHarborPresentation && entranceTime < .9f;

    public void BeginEntrance()
    {
        if (!Application.isPlaying || !forestHarborPresentation || artwork == null) return;
        entranceGroup = artwork.GetComponent<CanvasGroup>() ?? artwork.gameObject.AddComponent<CanvasGroup>();
        entranceTime = 0;
        Layout();
    }
    public void ShowImmediately()
    {
        entranceTime=1;
        if(artwork!=null){var group=artwork.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1;}
        Layout();
    }

    void OnEnable()
    {
        if (Application.isPlaying) { Bind(); AnimalSurpriseMarkers.Ensure(this); }
        lastSelected = lastHighest = lastSecond = -1;
        Refresh();
    }

    void Bind()
    {
        if (bound) return;
        bound = true;
        for (int i = 0; i < stopButtons.Length; i++)
        {
            int index = i;
            stopButtons[i].onClick.AddListener(() =>
            {
                map.Select(index);
                // Keyboard/arcade navigation is owned by the map, not the last clicked UI button.
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            });
        }
        playButton.onClick.AddListener(map.RequestPlay);
        backButton.onClick.AddListener(map.GoBack);
        fullscreenButton.onClick.AddListener(ToggleFullscreen);
    }

    void LateUpdate()
    {
        if (map == null || artwork == null) return;
        if (Entering) entranceTime = Mathf.Min(.9f, entranceTime + Time.unscaledDeltaTime);
        Layout();
        if (lastSelected != map.Selected || lastHighest != map.HighestUnlocked || Time.unscaledTime >= nextProgress)
        {
            Refresh();
            nextProgress = Time.unscaledTime + .5f;
        }
        int second = Mathf.CeilToInt(map.Countdown);
        if (lastSecond != second || lastMoving != map.Moving)
        {
            lastSecond = second;
            lastMoving = map.Moving;
            countdownText.text = forestHarborPresentation ? second.ToString("00") : map.Travelling ? "TRAVELLING TO LEVEL " + (map.Selected + 1) : "DEPARTING IN  " + second.ToString("00");
        }
        if (Application.isPlaying && Input.GetKeyDown(KeyCode.F11)) ToggleFullscreen();
        fullscreenText.text = forestHarborPresentation ? "SCREEN" : IsFullscreen() ? "WINDOWED" : "FULL SCREEN";
        PoseTrain();
        if (!forestHarborPresentation && map.Selected >= 0 && map.Selected < glows.Length)
            glows[map.Selected].color = new Color(.32f, 1f, .86f, .72f + Mathf.Sin(Time.unscaledTime * 2.4f) * .16f);
    }

    public void Layout()
    {
        var bounds = ((RectTransform)canvas.transform).rect;
        if (bounds.width <= 0 || bounds.height <= 0) return;
        // Keep the complete composition visible. The full-bleed backdrop fills unusual aspect ratios.
        float scale = Mathf.Min(bounds.width / ArtWidth, bounds.height / ArtHeight);
        // The suitcase's outer leather can bleed off a laptop screen; every station stays visible.
        float aspect = bounds.width / bounds.height;
        if (suitcasePresentation && aspect >= 1.58f && aspect <= ArtWidth / ArtHeight)
            scale = bounds.height / ArtHeight;
        if (forestHarborPresentation) scale *= boardScreenScale;
        artwork.sizeDelta = new Vector2(ArtWidth, ArtHeight);
        artwork.localScale = Vector3.one * scale;
        artwork.anchoredPosition = Vector2.zero;
        if (entranceGroup != null)
        {
            float t = Mathf.Clamp01(entranceTime / .9f);
            float ease = 1 - Mathf.Pow(1 - t, 4);
            artwork.localScale *= Mathf.Lerp(.94f, 1, ease);
            artwork.anchoredPosition = new Vector2(0, Mathf.Lerp(-32 * scale, 0, ease));
            entranceGroup.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .7f));
            entranceGroup.interactable = entranceGroup.blocksRaycasts = t >= 1;
        }
        if (suitcasePresentation || forestHarborPresentation) return; // Controls belong to the authored artwork.
        float safeBottom = 0;
        if (Screen.height > 0)
        {
            safeBottom = Screen.safeArea.yMin / Screen.height * bounds.height;
            var insets = LuxoddRuntimeContext.GetVisualViewportInsets();
            if (insets.ViewHeight > 0) safeBottom = Mathf.Max(safeBottom, insets.Bottom / insets.ViewHeight * bounds.height);
        }
        controls.anchoredPosition = new Vector2(0, Mathf.Max(safeBottom + 12, (bounds.height - ArtHeight * scale) * .5f + 26 * scale));
        controls.localScale = Vector3.one * Mathf.Min(scale, bounds.width / 1300f);
    }

    public void Refresh()
    {
        if (map == null || faces == null || faces.Length == 0) return;
        lastSelected = map.Selected;
        lastHighest = map.HighestUnlocked;
        var forest = forestHarborPresentation ? GetComponent<ForestHarborMapPresentation>() : null;
        bool workshop = forest != null && forest.workshopRailway;
        for (int i = 0; i < faces.Length; i++)
        {
            bool selected = i == map.Selected, locked = map.IsLevelLocked(i);
            bool ticket = workshop && forest.HasStationPlaques && forest.stationPlaques[i]!=null && forest.stationPlaques[i].ticketPlaque;
            if (ticket)
            {
                faces[i].color = Color.clear;
                forest.stationPlaques[i].SetLocked(locked);
            }
            else if (forestHarborPresentation)
                faces[i].color = new Color(.04f, .1f, .2f, locked ? .12f : 0);
            else if (suitcasePresentation)
                faces[i].color = new Color(.03f, .08f, .1f, locked ? .34f : 0);
            else faces[i].sprite = selected ? selectedFace : locked ? lockedFace : ivoryFace;
            glows[i].gameObject.SetActive(selected && !ticket);
            numbers[i].color = ticket ? (!map.Moving && i==map.Parked ? new Color(1,.98f,.85f) : new Color(.025f,.09f,.22f)) : workshop
                ? (locked ? new Color(.34f,.38f,.40f) : new Color(.025f,.09f,.22f))
                : locked ? new Color(.82f,.82f,.74f) : new Color(.14f,.09f,.035f);
            // Ticket badges draw the centered brass lock themselves.
            locks[i].SetActive(locked && !ticket);
            awards[i].SetActive(!locked && (PlayerPrefs.GetInt(LevelManager.PrefLevelBestStrokes + i, 0) > 0 || PlayerPrefs.GetInt(LevelManager.PrefLevelStars + i, 0) > 0));
        }
        bool canPlay = !map.IsLevelLocked(map.Selected);
        playButton.interactable = canPlay;
        playText.text = canPlay ? (forestHarborPresentation ? "PLAY LEVEL " : "›  PLAY LEVEL ") + (map.Selected + 1) : "LEVEL " + (map.Selected + 1) + " LOCKED";
        statusText.text = canPlay ? (suitcasePresentation ? "THE SECRET SUITCASE" : "THE TWILIGHT CROSSING") + "  ·  " + (map.Selected < 30 ? "WORLD 1" : "WORLD 2")
            : map.Selected >= BucaLevelAvailability.PlayableLevelCount ? "LEVEL UNAVAILABLE" : "COMPLETE LEVEL " + map.Selected + " TO CONTINUE";
    }

    public void PoseTrain()
    {
        if (forestHarborPresentation)
        {
            var position = map.train.localPosition;
            locomotive.anchoredPosition = new Vector2(position.x, position.z) * 20 + Vector2.up * (forestTrainGroundOffset * forestTrainDisplayScale + forestTrainRailOffset);
            locomotive.localScale = Vector3.one * forestTrainDisplayScale;
            return;
        }
        if (suitcasePresentation)
        {
            // These are the same route coordinates that move the 3D engine and turn its wheels.
            var position = map.Point(map.RouteDistance);
            locomotive.anchoredPosition = new Vector2(position.x, position.z) * 20 + Vector2.up * 30f;
            locomotive.localScale = Vector3.one;
            return;
        }
        if (route == null || route.Length < 2 || map.stops == null) return;
        int i = 0;
        while (i < route.Length - 2 && map.RouteDistance > map.stops[i + 1].distance) i++;
        float t = Mathf.InverseLerp(map.stops[i].distance, map.stops[i + 1].distance, map.RouteDistance);
        Vector2 point = Vector2.Lerp(route[i], route[i + 1], t);
        // The engine sits just above its stop so the selected number remains readable.
        locomotive.anchoredPosition = point + new Vector2(-15, 48);
        float perspective = Mathf.Lerp(.7f, 1f, Mathf.InverseLerp(-180, 190, -point.y));
        locomotive.localScale = Vector3.one * perspective;
        if (map.Moving) locomotive.anchoredPosition += Vector2.up * Mathf.Sin(Time.unscaledTime * 14) * .65f;
    }

    public void ToggleFullscreen()
    {
#if UNITY_EDITOR
        var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
        var window = UnityEditor.EditorWindow.GetWindow(type);
        window.maximized = !window.maximized;
#else
        Screen.fullScreen = !Screen.fullScreen;
#endif
    }

    static bool IsFullscreen()
    {
#if UNITY_EDITOR
        var windows = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
        foreach (var window in windows)
            if (window.GetType().Name == "GameView") return window.maximized;
        return false;
#else
        return Screen.fullScreen;
#endif
    }
}
