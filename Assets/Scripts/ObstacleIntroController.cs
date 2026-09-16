using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Session-scoped introductions for mechanics whose behaviour is not obvious
/// from their appearance. The UI is built once at runtime so every Game scene,
/// WebGL build and arcade session gets the same correctly-wired presentation.
///
/// A mechanic is marked seen for this controller's lifetime only. That means a
/// same-level restart never repeats an intro, while a genuinely new game session
/// teaches the next customer again instead of leaking PlayerPrefs from whoever
/// used the cabinet previously.
/// </summary>
[DisallowMultipleComponent]
public sealed class ObstacleIntroController : MonoBehaviour
{
    enum MechanicKind
    {
        RimRebound,
        BankingRail,
        StickyMud,
        OrbitingHole,
        SlipperyIce,
        SpeedBoost,
        BonusRoute,
        DeadlyHazard,
        Conveyor,
        WindCurrent,
        GravityWell,
        BouncePad,
        KickerBumper,
        Teleporter
    }

    struct IntroDefinition
    {
        public MechanicKind kind;
        public string icon;
        public string title;
        public string description;
        public string advice;
        public Color accent;

        public IntroDefinition(MechanicKind kind, string icon, string title,
            string description, string advice, Color accent)
        {
            this.kind = kind;
            this.icon = icon;
            this.title = title;
            this.description = description;
            this.advice = advice;
            this.accent = accent;
        }
    }

    static readonly IntroDefinition[] Definitions =
    {
        new IntroDefinition(
            MechanicKind.RimRebound,
            "RIM",
            "WATCH IT BOUNCE!",
            "HIT THE GLOWING RAIL  →  BOUNCE  →  GOAL!",
            "AIM  •  BOUNCE  •  SCORE!",
            new Color(1f, 0.72f, 0.24f, 1f)),

        new IntroDefinition(
            MechanicKind.BankingRail,
            "BANK",
            "SUPER BOUNCE WALL!",
            "HIT THE BLUE WALL  →  PUCK BOUNCES BACK!",
            "IT BLOCKS YOU — AIM FOR THE BOUNCE!",
            new Color(0.20f, 0.86f, 1f, 1f)),

        new IntroDefinition(
            MechanicKind.StickyMud,
            "MUD",
            "SILLY STICKY MUD!",
            "Uh-oh! Mud makes your puck sloooow.",
            "PUSH HARD OR GO AROUND!",
            new Color(0.80f, 0.48f, 0.20f, 1f)),

        new IntroDefinition(
            MechanicKind.OrbitingHole,
            "MOVE",
            "CATCH ME IF YOU CAN!",
            "The hole wiggles around. Aim where it is going.",
            "READY... LEAD... SCORE!",
            new Color(0.92f, 0.68f, 1f, 1f)),

        new IntroDefinition(
            MechanicKind.SlipperyIce,
            "ICE",
            "WHEEE — ICE!",
            "Your puck keeps sliding across the icy patch.",
            "USE A TINY PUSH!",
            new Color(0.42f, 0.92f, 1f, 1f)),

        new IntroDefinition(
            MechanicKind.SpeedBoost,
            "FAST",
            "TURBO TIME!",
            "Ride the arrows and your puck goes ZOOM!",
            "3... 2... 1... WHOOSH!",
            new Color(0.20f, 1f, 0.58f, 1f)),

        new IntroDefinition(
            MechanicKind.BonusRoute,
            "+PTS",
            "GOLD HUNT!",
            "Collect the shiny dots for extra points.",
            "GRAB THE GOLD!",
            new Color(1f, 0.67f, 0.10f, 1f)),

        new IntroDefinition(
            MechanicKind.DeadlyHazard,
            "!",
            "PINK MEANS OUCH!",
            "Pink traps pop your puck. Wiggle around them!",
            "DODGE THE GLOW!",
            new Color(1f, 0.16f, 0.48f, 1f)),

        new IntroDefinition(
            MechanicKind.Conveyor,
            "PUSH",
            "PUSHY FLOOR!",
            "This moving floor nudges your puck along.",
            "AIM THE OTHER WAY!",
            new Color(0.94f, 0.70f, 0.28f, 1f)),

        new IntroDefinition(
            MechanicKind.WindCurrent,
            "WIND",
            "WHOOSHY WIND!",
            "The arrows blow your puck sideways.",
            "LEAN INTO THE BREEZE!",
            new Color(0.30f, 0.88f, 1f, 1f)),

        new IntroDefinition(
            MechanicKind.GravityWell,
            "PULL",
            "SPACE SUCKER!",
            "The glowing ball pulls your puck closer.",
            "AIM WIDE!",
            new Color(0.70f, 0.36f, 1f, 1f)),

        new IntroDefinition(
            MechanicKind.BouncePad,
            "JUMP",
            "BOING PAD!",
            "Touch the arrow and leap across the board.",
            "BOUNCE TO VICTORY!",
            new Color(0.32f, 1f, 0.52f, 1f)),

        new IntroDefinition(
            MechanicKind.KickerBumper,
            "KICK",
            "KICKY BUMPER!",
            "Bump it and BLAST away super fast.",
            "BONK... ZOOM!",
            new Color(1f, 0.68f, 0.16f, 1f)),

        new IntroDefinition(
            MechanicKind.Teleporter,
            "WARP",
            "MAGIC TUNNEL!",
            "Go in one portal... pop out of the other!",
            "NOW YOU SEE ME...",
            new Color(0.55f, 0.35f, 1f, 1f))
    };

    // Long enough for the full 3D demonstration loop and its subtitle to read
    // once, while still keeping the level start moving quickly.
    const float CardDisplaySeconds = 6.6f;

    readonly HashSet<MechanicKind> _seen = new HashSet<MechanicKind>();
    bool _showing;
    bool _skipRequested;
    Action _onFinished;

    GameObject _canvasRoot;
    CanvasGroup _rootGroup;
    CanvasGroup _cardGroup;
    RectTransform _card;
    RectTransform _iconDiamond;
    RawImage _blurImage;
    Image _dimImage;
    RenderTexture _blurTexture;
    Material _blurMaterial;
    Image _glow;
    Image _cardImage;
    Image _topAccent;
    Image _sideAccent;
    Image _iconImage;
    TMP_Text _eyebrowText;
    TMP_Text _iconText;
    TMP_Text _titleText;
    TMP_Text _descriptionText;
    TMP_Text _adviceText;
    TMP_Text _hintText;
    KidMechanicPreview3D _demo3D;
    Button _skipButton;
    Texture2D _roundedTexture;
    Sprite _roundedSprite;

    public bool IsShowing => _showing;

    /// <summary>
    /// Scans only the active level. Returns true synchronously when at least
    /// one not-yet-seen ambiguous mechanic was found and the timer should pause.
    /// </summary>
    public bool TryShowForLevel(GameObject levelRoot, int levelNumber, Action onFinished)
    {
        if (_showing || levelRoot == null) return false;

        var pending = new List<IntroDefinition>(Definitions.Length);
        for (int i = 0; i < Definitions.Length; i++)
        {
            IntroDefinition definition = Definitions[i];
            if (_seen.Contains(definition.kind)) continue;
            if (!LevelContains(levelRoot, definition.kind)) continue;

            // Mark immediately. Even if the same level is restarted while a
            // card is fading, this session must never enqueue it twice.
            _seen.Add(definition.kind);
            pending.Add(definition);
        }

        if (pending.Count == 0) return false;

        EnsureUi();
        _showing = true;
        _skipRequested = false;
        _onFinished = onFinished;
        _canvasRoot.SetActive(true);
        StartCoroutine(ShowQueue(pending, levelNumber));
        return true;
    }

    /// <summary>Skips only the tutorial; it never skips the real scored level.</summary>
    public void SkipTutorial()
    {
        if (!_showing) return;
        _skipRequested = true;
    }

    static bool LevelContains(GameObject root, MechanicKind kind)
    {
        switch (kind)
        {
            case MechanicKind.RimRebound:
                return root.GetComponentInChildren<RimRebound>(true) != null;
            case MechanicKind.BankingRail:
                return root.GetComponentInChildren<BankingRail>(true) != null;
            case MechanicKind.StickyMud:
                return ContainsSurface(root, wantMud: true);
            case MechanicKind.OrbitingHole:
                return root.GetComponentInChildren<OrbitingHole>(true) != null;
            case MechanicKind.SlipperyIce:
                return ContainsSurface(root, wantMud: false);
            case MechanicKind.SpeedBoost:
                return root.GetComponentInChildren<SpeedBoost>(true) != null;
            case MechanicKind.BonusRoute:
                return root.GetComponentInChildren<ScorePickup>(true) != null;
            case MechanicKind.DeadlyHazard:
                return root.GetComponentInChildren<DeadlyTrigger>(true) != null;
            case MechanicKind.Conveyor:
                return ContainsWind(root, wantConveyor: true);
            case MechanicKind.WindCurrent:
                return ContainsWind(root, wantConveyor: false);
            case MechanicKind.GravityWell:
                return root.GetComponentInChildren<GravityWell>(true) != null;
            case MechanicKind.BouncePad:
                return root.GetComponentInChildren<BouncePad>(true) != null;
            case MechanicKind.KickerBumper:
                return root.GetComponentInChildren<KickerBumper>(true) != null;
            case MechanicKind.Teleporter:
                return root.GetComponentInChildren<Teleporter>(true) != null;
            default:
                return false;
        }
    }

    static bool ContainsSurface(GameObject root, bool wantMud)
    {
        IcePatch[] surfaces = root.GetComponentsInChildren<IcePatch>(true);
        for (int i = 0; i < surfaces.Length; i++)
        {
            IcePatch surface = surfaces[i];
            bool isMud = surface.patchDamping > 1f ||
                surface.name.IndexOf("Mud", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isMud == wantMud) return true;
        }
        return false;
    }

    static bool ContainsWind(GameObject root, bool wantConveyor)
    {
        BucaWindZone[] winds = root.GetComponentsInChildren<BucaWindZone>(true);
        for (int i = 0; i < winds.Length; i++)
        {
            bool isConveyor = winds[i].name.IndexOf(
                "Conveyor", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isConveyor == wantConveyor) return true;
        }
        return false;
    }

    IEnumerator ShowQueue(List<IntroDefinition> pending, int levelNumber)
    {
        _rootGroup.alpha = 0f;
        _cardGroup.alpha = 0f;
        _card.anchoredPosition = new Vector2(0f, -45f);
        _card.localScale = Vector3.one * 0.91f;

        yield return CaptureBlurredBackground();
        ApplyDefinition(pending[0], levelNumber, 1, pending.Count);
        yield return AnimateFirstCardIn();

        for (int i = 0; i < pending.Count; i++)
        {
            if (i > 0)
            {
                yield return FadeCard(1f, 0f, 0.16f, -18f);
                ApplyDefinition(pending[i], levelNumber, i + 1, pending.Count);
                yield return FadeCard(0f, 1f, 0.30f, 18f);
            }

            yield return WaitForPlayerOrTimeout();
            if (_skipRequested) break;
        }

        yield return AnimateOut();
        if (_demo3D != null) _demo3D.SetVisible(false);
        ReleaseBlurTexture();
        _canvasRoot.SetActive(false);
        _showing = false;

        Action callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();
    }

    void ApplyDefinition(IntroDefinition definition, int levelNumber,
        int cardNumber, int totalCards)
    {
        Color accent = definition.accent;
        _glow.color = new Color(accent.r, accent.g, accent.b, 0.16f);
        _topAccent.color = accent;
        _sideAccent.color = accent;
        _iconImage.color = new Color(accent.r * 0.34f, accent.g * 0.34f,
            accent.b * 0.34f, 1f);
        _iconText.color = accent;
        _adviceText.color = accent;
        string sequence = totalCards > 1 ? $"  •  {cardNumber}/{totalCards}" : "";
        _eyebrowText.text = $"LEVEL {levelNumber}  •  NEW TRICK!{sequence}";
        _iconText.text = definition.icon;
        _titleText.text = definition.title;
        _descriptionText.text = definition.description;
        _adviceText.text = definition.advice;
        _hintText.text = definition.advice;
        if (_demo3D != null) _demo3D.Configure(definition.icon, accent);
    }

    IEnumerator AnimateFirstCardIn()
    {
        const float duration = 0.56f;
        float elapsed = 0f;
        Vector2 start = new Vector2(0f, -45f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            float fade = EaseOutCubic(k);
            float settle = EaseOutBack(k, 0.9f);

            _rootGroup.alpha = fade;
            _cardGroup.alpha = EaseOutCubic((k - 0.08f) / 0.72f);
            _card.anchoredPosition = Vector2.LerpUnclamped(start, Vector2.zero, settle);
            _card.localScale = Vector3.LerpUnclamped(
                Vector3.one * 0.91f, Vector3.one, settle);
            yield return null;
        }

        _rootGroup.alpha = 1f;
        _rootGroup.interactable = true;
        _cardGroup.alpha = 1f;
        _cardGroup.interactable = false;
        _card.anchoredPosition = Vector2.zero;
        _card.localScale = Vector3.one;
    }

    IEnumerator FadeCard(float from, float to, float duration, float slideFrom)
    {
        _cardGroup.interactable = false;
        float elapsed = 0f;
        Vector2 start = new Vector2(slideFrom, 0f);
        Vector2 end = Vector2.zero;
        if (from > to)
        {
            start = Vector2.zero;
            end = new Vector2(-slideFrom, 0f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = EaseOutCubic(elapsed / Mathf.Max(0.01f, duration));
            _cardGroup.alpha = Mathf.Lerp(from, to, k);
            _card.anchoredPosition = Vector2.LerpUnclamped(start, end, k);
            yield return null;
        }

        _cardGroup.alpha = to;
        _cardGroup.interactable = false;
        _card.anchoredPosition = end;
    }

    IEnumerator WaitForPlayerOrTimeout()
    {
        float elapsed = 0f;
        while (elapsed < CardDisplaySeconds && !_skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void Update()
    {
        if (!_showing) return;

        // Purple is the cabinet shortcut for the same on-screen SKIP button.
        if (ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Purple))
            SkipTutorial();

        if (_demo3D != null) _demo3D.Tick();
    }

    IEnumerator AnimateOut()
    {
        _rootGroup.interactable = false;
        _cardGroup.interactable = false;
        const float duration = 0.26f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(elapsed / duration));
            _rootGroup.alpha = 1f - k;
            _card.anchoredPosition = Vector2.Lerp(
                Vector2.zero, new Vector2(0f, 24f), k);
            _card.localScale = Vector3.Lerp(
                Vector3.one, Vector3.one * 0.975f, k);
            yield return null;
        }

        _rootGroup.alpha = 0f;
        _card.anchoredPosition = Vector2.zero;
        _card.localScale = Vector3.one;
    }

    IEnumerator CaptureBlurredBackground()
    {
        // The canvas is active but fully transparent here, so the capture only
        // contains the real level/HUD. The intro card is drawn sharply on top
        // after the Gaussian result is ready.
        yield return new WaitForEndOfFrame();
        ReleaseBlurTexture();

        Texture2D screenshot = null;
        RenderTexture passTexture = null;
        try
        {
            screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null)
                throw new InvalidOperationException("Screen capture returned null.");

            int width = Mathf.Max(1, screenshot.width / 4);
            int height = Mathf.Max(1, screenshot.height / 4);
            _blurTexture = new RenderTexture(width, height, 0,
                RenderTextureFormat.ARGB32)
            {
                name = "BucaObstacleIntroBackgroundBlur",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            _blurTexture.Create();
            Graphics.Blit(screenshot, _blurTexture);

            Material blur = GetBlurMaterial();
            if (blur != null)
            {
                passTexture = RenderTexture.GetTemporary(width, height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                passTexture.filterMode = FilterMode.Bilinear;
                passTexture.wrapMode = TextureWrapMode.Clamp;

                for (int pass = 0; pass < 3; pass++)
                {
                    float radius = 1.2f + pass * 0.65f;
                    blur.SetVector("_BlurDirection",
                        new Vector4(radius, 0f, 0f, 0f));
                    Graphics.Blit(_blurTexture, passTexture, blur);
                    blur.SetVector("_BlurDirection",
                        new Vector4(0f, radius, 0f, 0f));
                    Graphics.Blit(passTexture, _blurTexture, blur);
                }
            }

            if (_blurImage != null)
            {
                _blurImage.texture = _blurTexture;
                _blurImage.color = Color.white;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ObstacleIntro] Background blur capture failed: {ex.Message}");
        }
        finally
        {
            if (passTexture != null) RenderTexture.ReleaseTemporary(passTexture);
            if (screenshot != null) Destroy(screenshot);
        }
    }

    Material GetBlurMaterial()
    {
        if (_blurMaterial != null) return _blurMaterial;

        Shader shader = Resources.Load<Shader>("BucaGaussianBlur");
        if (shader == null) shader = Shader.Find("Hidden/Buca/GaussianBlur");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("[ObstacleIntro] Gaussian blur shader unavailable; using bilinear fallback.");
            return null;
        }

        _blurMaterial = new Material(shader)
        {
            name = "Buca Obstacle Intro Gaussian Blur (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        return _blurMaterial;
    }

    void ReleaseBlurTexture()
    {
        if (_blurImage != null) _blurImage.texture = null;
        if (_blurTexture == null) return;
        if (_blurTexture.IsCreated()) _blurTexture.Release();
        Destroy(_blurTexture);
        _blurTexture = null;
    }

    void EnsureUi()
    {
        if (_canvasRoot != null) return;

        _canvasRoot = new GameObject("ObstacleIntroCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        _canvasRoot.transform.SetParent(transform, false);

        Canvas canvas = _canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 4800;

        CanvasScaler scaler = _canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _rootGroup = _canvasRoot.GetComponent<CanvasGroup>();
        _rootGroup.alpha = 0f;
        _rootGroup.interactable = false;
        _rootGroup.blocksRaycasts = true;

        EnsureRoundedSprite();

        GameObject blurGo = new GameObject("BlurredBackground",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        blurGo.transform.SetParent(_canvasRoot.transform, false);
        _blurImage = blurGo.GetComponent<RawImage>();
        Stretch(_blurImage.rectTransform, Vector2.zero, Vector2.zero);
        _blurImage.raycastTarget = false;
        _blurImage.color = Color.white;

        _dimImage = CreateImage("Dim", _canvasRoot.transform,
            new Color(0.005f, 0.012f, 0.035f, 0.72f));
        Stretch(_dimImage.rectTransform, Vector2.zero, Vector2.zero);
        _dimImage.raycastTarget = true;

        GameObject motionGo = new GameObject("CardMotion", typeof(RectTransform),
            typeof(CanvasGroup));
        motionGo.transform.SetParent(_canvasRoot.transform, false);
        _card = motionGo.GetComponent<RectTransform>();
        Place(_card, new Vector2(1380f, 820f), Vector2.zero);
        _cardGroup = motionGo.GetComponent<CanvasGroup>();
        _cardGroup.interactable = false;
        _cardGroup.blocksRaycasts = false;

        _glow = CreateImage("CardGlow", _card,
            new Color(0.1f, 0.8f, 1f, 0.16f));
        UseRoundedSprite(_glow);
        Stretch(_glow.rectTransform, Vector2.zero, Vector2.zero);

        _cardImage = CreateImage("Card", _card,
            new Color(0.012f, 0.030f, 0.062f, 0.99f));
        UseRoundedSprite(_cardImage);
        Place(_cardImage.rectTransform, new Vector2(1340f, 780f), Vector2.zero);
        Outline cardOutline = _cardImage.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1f, 0.58f, 0.12f, 0.92f);
        cardOutline.effectDistance = new Vector2(4f, -4f);

        Transform cardSurface = _cardImage.transform;
        _topAccent = CreateImage("TopAccent", cardSurface, Color.cyan);
        AnchorLine(_topAccent.rectTransform, true, 5f, 0f);
        _sideAccent = CreateImage("SideAccent", cardSurface, Color.cyan);
        AnchorLine(_sideAccent.rectTransform, false, 5f, 0f);

        Image demoFrame = CreateImage("Funny3DPreviewFrame", cardSurface,
            new Color(0.006f, 0.018f, 0.040f, 0.96f));
        UseRoundedSprite(demoFrame);
        Place(demoFrame.rectTransform, new Vector2(1276f, 612f), new Vector2(0f, -8f));
        Outline demoOutline = demoFrame.gameObject.AddComponent<Outline>();
        demoOutline.effectColor = new Color(0.22f, 0.90f, 1f, 0.82f);
        demoOutline.effectDistance = new Vector2(3.5f, -3.5f);

        GameObject demoGo = new GameObject("Funny3DMechanicDemo",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        demoGo.transform.SetParent(cardSurface, false);
        RawImage demoImage = demoGo.GetComponent<RawImage>();
        Place(demoImage.rectTransform, new Vector2(1252f, 588f), new Vector2(0f, -8f));
        demoImage.raycastTarget = false;
        _demo3D = KidMechanicPreview3D.Ensure(demoImage);

        Image innerTop = CreateImage("InnerTopLine", cardSurface,
            new Color(0.20f, 0.72f, 0.86f, 0.22f));
        RectTransform innerTopRt = innerTop.rectTransform;
        innerTopRt.anchorMin = new Vector2(0f, 1f);
        innerTopRt.anchorMax = new Vector2(1f, 1f);
        innerTopRt.pivot = new Vector2(0.5f, 1f);
        innerTopRt.offsetMin = new Vector2(34f, -28f);
        innerTopRt.offsetMax = new Vector2(-34f, -25f);

        _iconImage = CreateImage("IconDiamond", cardSurface,
            new Color(0.04f, 0.25f, 0.32f, 1f));
        UseRoundedSprite(_iconImage);
        _iconDiamond = _iconImage.rectTransform;
        Place(_iconDiamond, new Vector2(150f, 150f), new Vector2(-350f, 24f));
        _iconDiamond.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image iconInset = CreateImage("IconInset", _iconDiamond,
            new Color(0.005f, 0.018f, 0.040f, 0.94f));
        UseRoundedSprite(iconInset);
        Stretch(iconInset.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));

        _iconText = CreateText("IconText", cardSurface, 34f,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Place(_iconText.rectTransform, new Vector2(172f, 80f), new Vector2(-350f, 24f));
        _iconText.characterSpacing = 1.5f;
        _iconImage.gameObject.SetActive(false);
        _iconText.gameObject.SetActive(false);

        _eyebrowText = CreateText("Eyebrow", cardSurface, 18f,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Place(_eyebrowText.rectTransform, new Vector2(960f, 34f), new Vector2(0f, 356f));
        _eyebrowText.fontSize = 21f;
        _eyebrowText.color = new Color(0.62f, 0.90f, 1f, 1f);
        _eyebrowText.characterSpacing = 2f;

        _titleText = CreateText("Title", cardSurface, 46f,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Place(_titleText.rectTransform, new Vector2(1100f, 64f), new Vector2(0f, 311f));
        _titleText.color = Color.white;
        _titleText.characterSpacing = 1.2f;
        Shadow titleShadow = _titleText.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
        titleShadow.effectDistance = new Vector2(3f, -3f);

        Image subtitleBackground = CreateImage("SubtitleBackground", cardSurface,
            new Color(0.002f, 0.008f, 0.020f, 0.94f));
        UseRoundedSprite(subtitleBackground);
        Place(subtitleBackground.rectTransform, new Vector2(1252f, 104f), new Vector2(0f, -329f));
        Outline subtitleOutline = subtitleBackground.gameObject.AddComponent<Outline>();
        subtitleOutline.effectColor = new Color(1f, 0.58f, 0.12f, 0.72f);
        subtitleOutline.effectDistance = new Vector2(2f, -2f);

        _descriptionText = CreateText("Subtitle", cardSurface, 31f,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Place(_descriptionText.rectTransform, new Vector2(1170f, 82f), new Vector2(0f, -329f));
        _descriptionText.color = new Color(0.92f, 0.97f, 1f, 1f);
        _descriptionText.characterSpacing = 0.8f;
        _descriptionText.textWrappingMode = TextWrappingModes.Normal;
        _descriptionText.overflowMode = TextOverflowModes.Ellipsis;

        _adviceText = CreateText("Advice", cardSurface, 24f,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Place(_adviceText.rectTransform, new Vector2(700f, 42f), Vector2.zero);
        _adviceText.characterSpacing = 1.2f;
        _adviceText.gameObject.SetActive(false);

        Image divider = CreateImage("Divider", cardSurface,
            new Color(0.25f, 0.70f, 0.86f, 0.24f));
        divider.gameObject.SetActive(false);

        _hintText = CreateText("Hint", cardSurface, 19f,
            TextAlignmentOptions.Center, FontStyles.Normal);
        Place(_hintText.rectTransform, new Vector2(600f, 34f), Vector2.zero);
        _hintText.color = new Color(0.67f, 0.83f, 0.94f, 0.92f);
        _hintText.characterSpacing = 1.1f;
        _hintText.gameObject.SetActive(false);

        BuildSkipButton();

        _canvasRoot.SetActive(false);
    }

    void BuildSkipButton()
    {
        // A layered button rather than floating text: the offset dark base is
        // the visible extrusion, the violet rim catches light, and the raised
        // face keeps SKIP readable over every level background.
        Image hitArea = CreateImage("SkipButton3D", _canvasRoot.transform,
            new Color(1f, 1f, 1f, 0.001f));
        UseRoundedSprite(hitArea);
        RectTransform rect = hitArea.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(252f, 92f);
        rect.anchoredPosition = new Vector2(-34f, -28f);
        hitArea.raycastTarget = true;

        Image glow = CreateImage("PurpleGlow", rect,
            new Color(0.52f, 0.12f, 1f, 0.24f));
        UseRoundedSprite(glow);
        Place(glow.rectTransform, new Vector2(258f, 92f), new Vector2(0f, -1f));

        Image shadow = CreateImage("DeepShadow", rect,
            new Color(0f, 0f, 0.025f, 0.78f));
        UseRoundedSprite(shadow);
        Place(shadow.rectTransform, new Vector2(236f, 76f), new Vector2(0f, -9f));

        Image extrusion = CreateImage("PurpleExtrusion", rect,
            new Color(0.105f, 0.012f, 0.22f, 1f));
        UseRoundedSprite(extrusion);
        Place(extrusion.rectTransform, new Vector2(236f, 76f), new Vector2(0f, -4f));

        Image rim = CreateImage("MetallicVioletRim", rect,
            new Color(0.72f, 0.30f, 1f, 1f));
        UseRoundedSprite(rim);
        Place(rim.rectTransform, new Vector2(236f, 72f), new Vector2(0f, 3f));

        Image inset = CreateImage("DarkInset", rect,
            new Color(0.16f, 0.025f, 0.32f, 1f));
        UseRoundedSprite(inset);
        Place(inset.rectTransform, new Vector2(226f, 62f), new Vector2(0f, 3f));

        Image face = CreateImage("RaisedPurpleFace", rect,
            new Color(0.43f, 0.075f, 0.76f, 1f));
        UseRoundedSprite(face);
        Place(face.rectTransform, new Vector2(218f, 54f), new Vector2(0f, 7f));

        Image gloss = CreateImage("TopGloss", face.transform,
            new Color(0.92f, 0.72f, 1f, 0.22f));
        UseRoundedSprite(gloss);
        Place(gloss.rectTransform, new Vector2(190f, 17f), new Vector2(0f, 13f));

        Image lowerBevel = CreateImage("LowerBevel", face.transform,
            new Color(0.14f, 0.015f, 0.30f, 0.88f));
        UseRoundedSprite(lowerBevel);
        Place(lowerBevel.rectTransform, new Vector2(190f, 7f), new Vector2(0f, -19f));

        _skipButton = hitArea.gameObject.AddComponent<Button>();
        _skipButton.targetGraphic = face;
        _skipButton.onClick.AddListener(SkipTutorial);
        ColorBlock colors = _skipButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.08f, 1.24f, 1f);
        colors.selectedColor = new Color(1.10f, 1.04f, 1.18f, 1f);
        colors.pressedColor = new Color(0.72f, 0.62f, 0.82f, 1f);
        colors.disabledColor = new Color(0.45f, 0.42f, 0.50f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.07f;
        _skipButton.colors = colors;
        Navigation navigation = _skipButton.navigation;
        navigation.mode = Navigation.Mode.None;
        _skipButton.navigation = navigation;

        TMP_Text label = CreateText("Label", face.transform, 30f,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
        label.text = "SKIP";
        label.color = Color.white;
        label.characterSpacing = 2.8f;
        label.rectTransform.anchoredPosition = new Vector2(0f, 1f);
        Shadow labelShadow = label.gameObject.AddComponent<Shadow>();
        labelShadow.effectColor = new Color(0.035f, 0f, 0.08f, 0.95f);
        labelShadow.effectDistance = new Vector2(2.5f, -3.5f);
    }

    void EnsureRoundedSprite()
    {
        if (_roundedSprite != null) return;

        const int size = 64;
        const float radius = 15.5f;
        float innerMin = radius;
        float innerMax = size - 1f - radius;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nearestX = Mathf.Clamp(x, innerMin, innerMax);
                float nearestY = Mathf.Clamp(y, innerMin, innerMax);
                float dx = x - nearestX;
                float dy = y - nearestY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.SmoothStep(radius - 1.2f,
                    radius + 1.2f, distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Buca Obstacle Intro Rounded UI",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        _roundedTexture.SetPixels32(pixels);
        _roundedTexture.Apply(false, true);

        _roundedSprite = Sprite.Create(_roundedTexture,
            new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0u, SpriteMeshType.FullRect,
            new Vector4(18f, 18f, 18f, 18f));
        _roundedSprite.name = "Buca Obstacle Intro Rounded UI";
        _roundedSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    void UseRoundedSprite(Image image)
    {
        if (image == null || _roundedSprite == null) return;
        image.sprite = _roundedSprite;
        image.type = Image.Type.Sliced;
    }

    void OnDestroy()
    {
        ReleaseBlurTexture();
        if (_blurMaterial != null) Destroy(_blurMaterial);
        if (_roundedSprite != null) Destroy(_roundedSprite);
        if (_roundedTexture != null) Destroy(_roundedTexture);
        _blurMaterial = null;
        _roundedSprite = null;
        _roundedTexture = null;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text CreateText(string name, Transform parent, float fontSize,
        TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    static void Place(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static void AnchorLine(RectTransform rect, bool horizontal,
        float thickness, float inset)
    {
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(inset, -thickness);
            rect.offsetMax = new Vector2(-inset, 0f);
        }
        else
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(0f, inset);
            rect.offsetMax = new Vector2(thickness, -inset);
        }
    }

    static float EaseOutCubic(float value)
    {
        float x = 1f - Mathf.Clamp01(value);
        return 1f - x * x * x;
    }

    static float EaseOutBack(float value, float overshoot)
    {
        float x = Mathf.Clamp01(value) - 1f;
        return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
    }
}
