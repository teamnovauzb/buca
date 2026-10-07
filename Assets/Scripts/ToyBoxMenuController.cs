using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Luxodd.Game.Scripts.Runtime.Viewport.Context;

/// <summary>
/// Operates the editor-baked Toy Box menu. Never constructs objects, text meshes,
/// materials or UI. Even the clock digits are seven pre-authored solid segments.
/// </summary>
public sealed class ToyBoxMenuController : MonoBehaviour
{
    public static bool OpenLevelsOnNextLoad;
    [Serializable]
    public sealed class PuckButton
    {
        public Transform cap;
        public Collider hit;
        public GameObject selectedRing;
        public MeshRenderer face;
        public GameObject lockMark;
        public GameObject[] awards;
        public Vector3 restPosition;
    }

    public Camera menuCamera, mapCamera;
    public Material[] chapterMaterials;
    public Material currentLevelMaterial;
    public GameObject homeRoot, levelsRoot, resumeRoot, skinsRoot;
    public PuckButton[] homeButtons, levelButtons, resumeButtons, skinButtons;
    public PuckButton backButton, skinsBackButton;
    public GameObject[] equippedMarkers;
    public GameObject[] clockSegments;
    public GameObject[] mapClockSegments, chapterLocks;
    public Material mapPuckMaterial, mapLockedMaterial;
    public float mapSelectionSeconds = 30f;
    float _mapRemaining;
    public Material lockedMaterial, levelMaterial;
    public string gameScene = "Game";
    public Bounds framingBounds = new Bounds(new Vector3(0, 2.1f, 0), new Vector3(13.7f, 5.7f, 10f));
    public Vector3[] framingPoints;

    public GameObject previewRoot;
    public Camera previewCamera;
    public GameObject[] menuScenery, previewTitles;
    public PuckButton[] previewButtons;
    public BucaSkinBinding previewBoard, previewPuck;
    public ToyPaintSkinSelector paintWorkshop;
    public void ClosePaintWorkshop() => ShowPage(Page.Home);
    public Camera ActiveCamera => _page == Page.Skins && paintWorkshop != null ? paintWorkshop.viewCamera : _page == Page.SkinPreview ? previewCamera : _page == Page.Levels ? mapCamera : menuCamera;
    enum Page { Home, Levels, Resume, Skins, SkinPreview }
    Page _page;
    int _focus, _highest, _lastSecond = -1;
    float _remaining = 30f, _nextNavigation, _nextStoragePoll;
    bool _loading, _awaitNeutral;
    Vector3 _lastPointer;
    Color _menuSky, _menuEquator, _menuGround;
    bool _menuLightingCaptured;
    void CaptureMenuLighting()
    {
        if(_menuLightingCaptured)return;
        _menuSky=RenderSettings.ambientSkyColor;
        _menuEquator=RenderSettings.ambientEquatorColor;
        _menuGround=RenderSettings.ambientGroundColor;
        _menuLightingCaptured=true;
    }
    Rect _lastViewport;
    int _lastWidth, _lastHeight;
    static readonly int[] DigitMasks = { 63, 6, 91, 79, 102, 109, 125, 7, 127, 111 };

    void Start()
    {
        Time.timeScale = 1f;
        CaptureMenuLighting();
        _lastPointer = Input.mousePosition;
        ShowPage(OpenLevelsOnNextLoad ? Page.Levels : Page.Home);
        OpenLevelsOnNextLoad = false;
        SetClock(30);
        FitCamera();
    }

    void Update()
    {
        if (_loading) return;
        FitCamera();
        if (_page == Page.Skins && paintWorkshop != null) return;
        if (_page == Page.Home)
        {
            _remaining -= Time.unscaledDeltaTime;
            int second = Mathf.Max(1, Mathf.CeilToInt(_remaining));
            if (second != _lastSecond)
            {
                SetClock(second);
                if (second <= 5 && AudioManager.Instance != null)
                    AudioManager.Instance.PlayCountdownTick();
            }
            if (_remaining <= 0f) { StartLevel(0); return; }
        }
        if (_page == Page.Levels && Time.unscaledTime >= _nextStoragePoll)
        {
            RefreshProgress(); // Includes a Luxodd state response arriving while the tray is open.
            _nextStoragePoll = Time.unscaledTime + 0.5f;
        }
        if (_page == Page.Levels && mapClockSegments != null && mapClockSegments.Length == 14)
        {
            TickMapCountdown(Time.unscaledDeltaTime);
            if (_loading) return;
        }

        bool confirmHeld = ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)
            || Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0);
        if (_awaitNeutral)
        {
            AnimateButtons();
            if (!confirmHeld) _awaitNeutral = false;
            return;
        }

        if (ArcadeInputAdapter.CancelDown() || Input.GetKeyDown(KeyCode.Escape))
        {
            if (_page != Page.Home) ShowPage(_page == Page.SkinPreview ? Page.Skins : Page.Home);
            return;
        }

        Vector2 stick = ArcadeInputAdapter.GetStick();
        if (Input.GetKey(KeyCode.LeftArrow)) stick.x = -1;
        if (Input.GetKey(KeyCode.RightArrow)) stick.x = 1;
        if (Input.GetKey(KeyCode.UpArrow)) stick.y = 1;
        if (Input.GetKey(KeyCode.DownArrow)) stick.y = -1;
        if (stick.sqrMagnitude < 0.2f) _nextNavigation = 0f;
        else if (Time.unscaledTime >= _nextNavigation)
        {
            Navigate(stick);
            _nextNavigation = Time.unscaledTime + 0.22f;
        }

        bool click = Input.GetMouseButtonDown(0);
        Vector3 pointer = Input.mousePosition;
        if (click || (pointer - _lastPointer).sqrMagnitude > 0.5f)
        {
            _lastPointer = pointer;
            if (HandlePointer(pointer, click)) return;
        }
        if (ArcadeInputAdapter.ConfirmDown() || Input.GetKeyDown(KeyCode.Return)) Activate();
        if (!_loading) AnimateButtons();
    }

    PuckButton[] ActiveButtons => _page == Page.Home ? homeButtons :
        _page == Page.Levels ? levelButtons : _page == Page.Skins ? skinButtons : _page == Page.SkinPreview ? previewButtons : resumeButtons;

    bool HandlePointer(Vector3 pointer, bool click)
    {
        if (!ActiveCamera.pixelRect.Contains(pointer)) return false;
        // Test the visible page's saved button colliders directly. Scenery and hidden-page
        // colliders must not intercept a selector click.
        Physics.SyncTransforms();
        Ray ray=ActiveCamera.ScreenPointToRay(pointer);
        int index=-1; float nearest=float.MaxValue;
        var buttons=ActiveButtons;
        for(int i=0;i<buttons.Length;i++)
            if(buttons[i].hit.Raycast(ray,out var hit,150f) && hit.distance<nearest)
            { index=i; nearest=hit.distance; }
        Collider back=_page==Page.Levels?backButton.hit:_page==Page.Skins?skinsBackButton.hit:null;
        if(back!=null && back.Raycast(ray,out var backHit,150f) && backHit.distance<nearest)
            index=_page==Page.Levels?30:5;
        if (index < 0) return false;
        SetFocus(index);
        if (click) Activate();
        return click;
    }

    int FindButton(Collider hit)
    {
        var buttons = ActiveButtons;
        for (int i = 0; i < buttons.Length; i++) if (buttons[i].hit == hit) return i;
        if (_page == Page.Levels && hit == backButton.hit) return 30;
        return _page == Page.Skins && hit == skinsBackButton.hit ? 5 : -1;
    }

    void Navigate(Vector2 stick)
    {
        if (_page == Page.Skins)
        {
            int next = _focus;
            if (Mathf.Abs(stick.y) > Mathf.Abs(stick.x))
                next = stick.y > 0 ? Mathf.Max(0, _focus - 3) : Mathf.Min(5, _focus + 3);
            else next = Mathf.Clamp(_focus + (stick.x > 0 ? 1 : -1), 0, 5);
            SetFocus(next);
        }
        else if (_page == Page.Levels)
        {
            // Choose the nearest control in the requested screen direction.
            Vector2 direction = Mathf.Abs(stick.x) > Mathf.Abs(stick.y)
                ? new Vector2(Mathf.Sign(stick.x),0) : new Vector2(0,Mathf.Sign(stick.y));
            var originButton = _focus == 30 ? backButton : levelButtons[_focus];
            Vector2 origin = mapCamera.WorldToViewportPoint(originButton.hit.bounds.center);
            int best = _focus; float bestScore = float.MaxValue;
            for (int i=0;i<=30;i++)
            {
                if(i==_focus) continue;
                var candidate = i==30 ? backButton : levelButtons[i];
                Vector2 delta = (Vector2)mapCamera.WorldToViewportPoint(candidate.hit.bounds.center)-origin;
                delta.x *= mapCamera.aspect;
                float forward = Vector2.Dot(delta,direction);
                if(forward<=.005f) continue;
                float lateral = Mathf.Abs(delta.x*direction.y-delta.y*direction.x);
                float score = delta.magnitude + lateral*2.5f;
                if(score<bestScore) { best=i; bestScore=score; }
            }
            SetFocus(best);
        }
        else
        {
            int direction = Mathf.Abs(stick.x) >= Mathf.Abs(stick.y)
                ? (stick.x > 0 ? 1 : -1) : (stick.y < 0 ? 1 : -1);
            SetFocus((_focus + direction + ActiveButtons.Length) % ActiveButtons.Length);
        }
    }

    void SetFocus(int index)
    {
        _focus = index;
        RefreshSelection();
        if (_page == Page.Levels) RefreshProgress();
    }

    void RefreshSelection()
    {
        var buttons = ActiveButtons;
        for (int i = 0; i < buttons.Length; i++) buttons[i].selectedRing.SetActive(i == _focus);
        if (_page == Page.Levels) backButton.selectedRing.SetActive(_focus == 30);
        if (_page == Page.Skins) skinsBackButton.selectedRing.SetActive(_focus == 5);
    }

    void Activate()
    {
        if (_page == Page.SkinPreview)
        {
            if (_focus == 2) ShowPage(Page.Skins);
            else
            {
                BucaSkinBinding.Select((BucaSkinBinding.Selected + (_focus == 0 ? 4 : 1)) % 5);
                RefreshPreview();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            }
            return;
        }
        if (_page == Page.Skins)
        {
            if (_focus == 5) ShowPage(Page.Home);
            else
            {
                BucaSkinBinding.Select(_focus);
                RefreshEquipped();
                ShowPage(Page.SkinPreview);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            }
            return;
        }
        if (_page == Page.Levels)
        {
            if (_focus == 30) ShowPage(Page.Home);
            else if (_focus <= _highest) StartLevel(_focus);
            return;
        }
        if (_page == Page.Resume)
        {
            if (_focus == 2) ShowPage(Page.Home);
            else
            {
                // Starting again preserves lessons already watched or skipped.
                StartLevel(_focus == 1 ? Mathf.Clamp(PlayerPrefs.GetInt("BucaCurrentLevel", 0), 0, 29) : 0);
            }
            return;
        }
        if (_focus == 0)
        {
            ShowPage(Page.Resume);
        }
        else if (_focus == 1) ShowPage(Page.Levels);
        else if (_focus == 2 && skinsRoot != null) ShowPage(Page.Skins);
    }

    void ShowPage(Page page)
    {
        CaptureMenuLighting();
        bool wasPreview = _page == Page.SkinPreview || _page == Page.Levels || (_page == Page.Skins && paintWorkshop != null);
        _page = page;
        if (page == Page.Skins && paintWorkshop != null)
        {
            RenderSettings.ambientSkyColor = new Color(.60f,.66f,.74f);
            RenderSettings.ambientEquatorColor = new Color(.47f,.51f,.57f);
            RenderSettings.ambientGroundColor = new Color(.32f,.36f,.41f);
        }
        else if (page == Page.Levels && mapPuckMaterial != null)
        {
            RenderSettings.ambientSkyColor = new Color(.48f,.49f,.50f);
            RenderSettings.ambientEquatorColor = new Color(.37f,.35f,.31f);
            RenderSettings.ambientGroundColor = new Color(.23f,.21f,.18f);
        }
        else if (page == Page.SkinPreview || page == Page.Levels)
        {
            RenderSettings.ambientSkyColor = new Color(.28f,.30f,.34f);
            RenderSettings.ambientEquatorColor = new Color(.23f,.20f,.17f);
            RenderSettings.ambientGroundColor = new Color(.12f,.10f,.08f);
        }
        else if (wasPreview)
        {
            RenderSettings.ambientSkyColor = _menuSky;
            RenderSettings.ambientEquatorColor = _menuEquator;
            RenderSettings.ambientGroundColor = _menuGround;
        }
        _focus = page == Page.Skins ? BucaSkinBinding.Selected : 0;
        _awaitNeutral = true;
        if (previewRoot != null) previewRoot.SetActive(page == Page.SkinPreview);
        if (menuScenery != null) foreach (var scenery in menuScenery) scenery.SetActive(page != Page.SkinPreview && page != Page.Levels && !(page == Page.Skins && paintWorkshop != null));
        if (page == Page.SkinPreview) RefreshPreview();
        homeRoot.SetActive(page == Page.Home);
        levelsRoot.SetActive(page == Page.Levels);
        resumeRoot.SetActive(page == Page.Resume);
        if (skinsRoot != null) skinsRoot.SetActive(page == Page.Skins);
        if (page == Page.Skins && paintWorkshop != null) { paintWorkshop.Open(); return; }
        if (page == Page.Skins) RefreshEquipped();
        if (page == Page.Levels)
        {
            RefreshProgress();
            _mapRemaining = mapSelectionSeconds;
            SetMapClock(Mathf.CeilToInt(_mapRemaining));
        }
        RefreshSelection();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
    }

    void RefreshPreview()
    {
        int selected = BucaSkinBinding.Selected;
        previewBoard.Apply(selected);
        previewPuck.Apply(selected);
        for (int i = 0; i < previewTitles.Length; i++) previewTitles[i].SetActive(i == selected);
    }

    void RefreshEquipped()
    {
        for (int i = 0; i < equippedMarkers.Length; i++)
            equippedMarkers[i].SetActive(i == BucaSkinBinding.Selected);
    }

    void RefreshProgress()
    {
        _highest = Mathf.Clamp(PlayerPrefs.GetInt(LevelSelectController.HighestUnlockedKey, 0), 0, 29);
        for (int i = 0; i < levelButtons.Length; i++)
        {
            var button = levelButtons[i];
            button.face.sharedMaterial = mapPuckMaterial != null
                ? (i > _highest ? mapLockedMaterial : i == _focus ? currentLevelMaterial : mapPuckMaterial)
                : i > _highest ? lockedMaterial : i == _focus ? currentLevelMaterial : chapterMaterials[i / 6];
            button.lockMark.SetActive(i > _highest);
            int stars = PlayerPrefs.GetInt(LevelManager.PrefLevelStars + i, 0);
            for (int j = 0; j < button.awards.Length; j++) button.awards[j].SetActive(j < stars || (j == 0 && PlayerPrefs.GetInt(LevelManager.PrefLevelBestStrokes + i,0)>0));
        }
        if (chapterLocks != null)
            for (int c=0;c<chapterLocks.Length;c++) chapterLocks[c].SetActive(c*6 > _highest);
    }

    void TickMapCountdown(float elapsed)
    {
        _mapRemaining = Mathf.Max(0, _mapRemaining - elapsed);
        SetMapClock(Mathf.CeilToInt(_mapRemaining));
        if (_mapRemaining <= 0)
        {
            RefreshProgress();
            StartLevel(_focus >= 0 && _focus < 30 && _focus <= _highest ? _focus : _highest);
        }
    }

    void SetMapClock(int seconds)
    {
        if (mapClockSegments == null || mapClockSegments.Length != 14) return;
        seconds = Mathf.Clamp(seconds,0,99);
        for(int digit=0;digit<2;digit++)
            for(int s=0;s<7;s++) mapClockSegments[digit*7+s].SetActive((DigitMasks[digit==0?seconds/10:seconds%10] & (1<<s))!=0);
    }

    void AnimateButtons()
    {
        var buttons = ActiveButtons;
        for (int i = 0; i < buttons.Length; i++) AnimateButton(buttons[i], i == _focus);
        if (_page == Page.Levels) AnimateButton(backButton, _focus == 30);
        if (_page == Page.Skins) AnimateButton(skinsBackButton, _focus == 5);
    }

    static void AnimateButton(PuckButton button, bool selected)
    {
        var target = button.restPosition + (selected ? Vector3.up * 0.09f : Vector3.zero);
        button.cap.localPosition = Vector3.Lerp(button.cap.localPosition, target,
            1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
    }

    public void SetClock(int seconds)
    {
        _lastSecond = Mathf.Clamp(seconds, 0, 99);
        for (int digit = 0; digit < 2; digit++)
        {
            int number = digit == 0 ? _lastSecond / 10 : _lastSecond % 10;
            int mask = DigitMasks[number];
            for (int segment = 0; segment < 7; segment++)
                clockSegments[digit * 7 + segment].SetActive((mask & (1 << segment)) != 0);
        }
    }

    void StartLevel(int level)
    {
        if (_loading) return;
        _loading = true;
        PlayerPrefs.SetInt(LevelSelectController.PendingLevelKey, level);
        PlayerPrefs.Save();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        SceneManager.LoadScene(gameScene);
    }

    public void FitMapCamera(float aspect)
    {
        if (mapCamera == null) return;
        Vector3 target = transform.TransformPoint(new Vector3(0,mapPuckMaterial != null ? 1.1f : .75f,-1.25f));
        float tan = Mathf.Tan(mapCamera.fieldOfView*Mathf.Deg2Rad*.5f), distance = 0;
        foreach(float x in new[]{-10.3f,10.3f})
            foreach(var edge in new[]{new Vector2(-.9f,-7.8f),new Vector2(2.95f,7.5f)})
            {
                Vector3 p = Quaternion.Inverse(mapCamera.transform.rotation)*(transform.TransformPoint(new Vector3(x,edge.x,edge.y))-target);
                distance = Mathf.Max(distance,Mathf.Abs(p.y)/tan-p.z,Mathf.Abs(p.x)/(tan*aspect)-p.z);
            }
        mapCamera.transform.position=target-mapCamera.transform.forward*(distance*1.025f);
    }

    [Range(1f,1.4f)] public float menuFramingPadding = 1.04f;
    public float menuVerticalFramingOffset;

    void FitCamera()
    {
        Rect safe = Screen.safeArea;
        var insets = LuxoddRuntimeContext.GetVisualViewportInsets();
        if (insets.ViewWidth > 0 && insets.ViewHeight > 0)
        {
            float left = Mathf.Max(safe.xMin, Screen.width * insets.Left / insets.ViewWidth);
            float right = Mathf.Min(safe.xMax, Screen.width * (1 - insets.Right / insets.ViewWidth));
            float bottom = Mathf.Max(safe.yMin, Screen.height * insets.Bottom / insets.ViewHeight);
            float top = Mathf.Min(safe.yMax, Screen.height * (1 - insets.Top / insets.ViewHeight));
            if (right > left && top > bottom) safe = Rect.MinMaxRect(left, bottom, right, top);
        }
        if (_lastWidth == Screen.width && _lastHeight == Screen.height && _lastViewport == safe) return;
        _lastWidth = Screen.width; _lastHeight = Screen.height; _lastViewport = safe;
        if (safe.width < 1 || safe.height < 1) return;
        menuCamera.rect = new Rect(safe.x / Screen.width, safe.y / Screen.height,
            safe.width / Screen.width, safe.height / Screen.height);
        if (mapCamera != null)
        {
            mapCamera.rect = menuCamera.rect;
            FitMapCamera(safe.width / safe.height);
        }
        if (previewCamera != null)
        {
            previewCamera.rect = menuCamera.rect;
            // Aim at the visual centre of the board, not the room behind it.
            Vector3 target = transform.TransformPoint(new Vector3(0, 0f, -2.8f));
            float previewTan = Mathf.Tan(previewCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            float previewDistance = 0;
            foreach (float x in new[] { -5.5f, 5.5f })
                foreach (var edge in new[] { new Vector2(-1.6f, -8.4f), new Vector2(1.7f, 7.7f) })
                {
                    Vector3 p = Quaternion.Inverse(previewCamera.transform.rotation) * (transform.TransformPoint(new Vector3(x, edge.x, edge.y)) - target);
                    previewDistance = Mathf.Max(previewDistance, Mathf.Abs(p.y) / previewTan - p.z,
                        Mathf.Abs(p.x) / (previewTan * safe.width / safe.height) - p.z);
                }
            previewCamera.transform.position = target - previewCamera.transform.forward * (previewDistance * 1.015f);
        }
        FitHomeCamera(safe.width / safe.height);
    }

    public void FitHomeCamera(float aspect)
    {
        // Baked framing points are relative to the menu prefab, not the scene.
        Vector3 center = transform.TransformPoint(framingBounds.center);
        float tan = Mathf.Tan(menuCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        float distance = 0f;
        for (int i = 0; i < framingPoints.Length; i++)
        {
            Vector3 corner = transform.TransformPoint(framingPoints[i]) - center;
            Vector3 local = Quaternion.Inverse(menuCamera.transform.rotation) * corner;
            distance = Mathf.Max(distance, Mathf.Abs(local.y) / tan - local.z,
                Mathf.Abs(local.x) / (tan * aspect) - local.z);
        }
        float offset=menuVerticalFramingOffset*Mathf.Min(1,aspect/1.33f);
        menuCamera.transform.position = center + menuCamera.transform.up*offset - menuCamera.transform.forward * (distance * menuFramingPadding);
    }
}
