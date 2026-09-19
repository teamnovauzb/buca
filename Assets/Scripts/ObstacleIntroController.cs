using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Session-scoped mechanic introductions. The complete presentation is authored
/// in the Game scene from a saved prefab; runtime code only changes copy, color,
/// visibility, and animation state.
/// </summary>
[DisallowMultipleComponent]
public sealed class ObstacleIntroController : MonoBehaviour
{
    enum MechanicKind
    {
        RimRebound, BankingRail, StickyMud, OrbitingHole, SlipperyIce, SpeedBoost,
        BonusRoute, DeadlyHazard, Conveyor, WindCurrent, GravityWell, BouncePad,
        KickerBumper, Teleporter
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
        new IntroDefinition(MechanicKind.RimRebound, "RIM", "WATCH IT BOUNCE!",
            "HIT THE GLOWING RAIL  →  BOUNCE  →  GOAL!",
            "AIM  •  BOUNCE  •  SCORE!", new Color(1f, 0.72f, 0.24f, 1f)),
        new IntroDefinition(MechanicKind.BankingRail, "BANK", "SUPER BOUNCE WALL!",
            "HIT THE BLUE WALL  →  PUCK BOUNCES BACK!",
            "IT BLOCKS YOU — AIM FOR THE BOUNCE!", new Color(0.20f, 0.86f, 1f, 1f)),
        new IntroDefinition(MechanicKind.StickyMud, "MUD", "SILLY STICKY MUD!",
            "Uh-oh! Mud makes your puck sloooow.",
            "PUSH HARD OR GO AROUND!", new Color(0.80f, 0.48f, 0.20f, 1f)),
        new IntroDefinition(MechanicKind.OrbitingHole, "MOVE", "CATCH ME IF YOU CAN!",
            "The hole wiggles around. Aim where it is going.",
            "READY... LEAD... SCORE!", new Color(0.92f, 0.68f, 1f, 1f)),
        new IntroDefinition(MechanicKind.SlipperyIce, "ICE", "WHEEE — ICE!",
            "Your puck keeps sliding across the icy patch.",
            "USE A TINY PUSH!", new Color(0.42f, 0.92f, 1f, 1f)),
        new IntroDefinition(MechanicKind.SpeedBoost, "FAST", "TURBO TIME!",
            "Ride the arrows and your puck goes ZOOM!",
            "3... 2... 1... WHOOSH!", new Color(0.20f, 1f, 0.58f, 1f)),
        new IntroDefinition(MechanicKind.BonusRoute, "+PTS", "GOLD HUNT!",
            "Collect the shiny dots for extra points.",
            "GRAB THE GOLD!", new Color(1f, 0.67f, 0.10f, 1f)),
        new IntroDefinition(MechanicKind.DeadlyHazard, "!", "PINK MEANS OUCH!",
            "Pink traps pop your puck. Wiggle around them!",
            "DODGE THE GLOW!", new Color(1f, 0.16f, 0.48f, 1f)),
        new IntroDefinition(MechanicKind.Conveyor, "PUSH", "PUSHY FLOOR!",
            "This moving floor nudges your puck along.",
            "AIM THE OTHER WAY!", new Color(0.94f, 0.70f, 0.28f, 1f)),
        new IntroDefinition(MechanicKind.WindCurrent, "WIND", "WHOOSHY WIND!",
            "The arrows blow your puck sideways.",
            "LEAN INTO THE BREEZE!", new Color(0.30f, 0.88f, 1f, 1f)),
        new IntroDefinition(MechanicKind.GravityWell, "PULL", "SPACE SUCKER!",
            "The glowing ball pulls your puck closer.",
            "AIM WIDE!", new Color(0.70f, 0.36f, 1f, 1f)),
        new IntroDefinition(MechanicKind.BouncePad, "JUMP", "BOING PAD!",
            "Touch the arrow and leap across the board.",
            "BOUNCE TO VICTORY!", new Color(0.32f, 1f, 0.52f, 1f)),
        new IntroDefinition(MechanicKind.KickerBumper, "KICK", "KICKY BUMPER!",
            "Bump it and BLAST away super fast.",
            "BONK... ZOOM!", new Color(1f, 0.68f, 0.16f, 1f)),
        new IntroDefinition(MechanicKind.Teleporter, "WARP", "MAGIC TUNNEL!",
            "Go in one portal... pop out of the other!",
            "NOW YOU SEE ME...", new Color(0.55f, 0.35f, 1f, 1f))
    };

    const float CardDisplaySeconds = 6.6f;

    [Header("Prebuilt view")]
    [SerializeField] GameObject _canvasRoot;
    [SerializeField] CanvasGroup _rootGroup;
    [SerializeField] CanvasGroup _cardGroup;
    [SerializeField] RectTransform _card;
    [SerializeField] Image _glow;
    [SerializeField] Image _cardImage;
    [SerializeField] Image _topAccent;
    [SerializeField] Image _sideAccent;
    [SerializeField] Image _iconImage;
    [SerializeField] TMP_Text _eyebrowText;
    [SerializeField] TMP_Text _iconText;
    [SerializeField] TMP_Text _titleText;
    [SerializeField] TMP_Text _descriptionText;
    [SerializeField] TMP_Text _adviceText;
    [SerializeField] TMP_Text _hintText;
    [SerializeField] Button _skipButton;

    readonly HashSet<MechanicKind> _seen = new HashSet<MechanicKind>();
    bool _showing;
    bool _skipRequested;
    Action _onFinished;

    public bool IsShowing => _showing;

    void Awake()
    {
        if (_skipButton != null)
            _skipButton.onClick.AddListener(SkipTutorial);
        if (_rootGroup != null)
        {
            _rootGroup.alpha = 0f;
            _rootGroup.interactable = false;
            _rootGroup.blocksRaycasts = false;
        }
        if (_cardGroup != null) _cardGroup.alpha = 0f;
    }

    public bool TryShowForLevel(GameObject levelRoot, int levelNumber, Action onFinished)
    {
        if (_showing || levelRoot == null || !HasPrebuiltView()) return false;

        var pending = new List<IntroDefinition>(Definitions.Length);
        for (int i = 0; i < Definitions.Length; i++)
        {
            IntroDefinition definition = Definitions[i];
            if (_seen.Contains(definition.kind) || !LevelContains(levelRoot, definition.kind))
                continue;

            _seen.Add(definition.kind);
            pending.Add(definition);
        }

        if (pending.Count == 0) return false;

        _showing = true;
        _skipRequested = false;
        _onFinished = onFinished;
        _canvasRoot.SetActive(true);
        StartCoroutine(ShowQueue(pending, levelNumber));
        return true;
    }

    public void SkipTutorial()
    {
        if (_showing) _skipRequested = true;
    }

    bool HasPrebuiltView()
    {
        bool valid = _canvasRoot != null && _rootGroup != null && _cardGroup != null &&
            _card != null && _titleText != null && _descriptionText != null;
        if (!valid)
            Debug.LogError("[ObstacleIntro] Prebuilt view is incomplete. Run the runtime-content baker.", this);
        return valid;
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
                return ContainsSurface(root, true);
            case MechanicKind.OrbitingHole:
                return root.GetComponentInChildren<OrbitingHole>(true) != null;
            case MechanicKind.SlipperyIce:
                return ContainsSurface(root, false);
            case MechanicKind.SpeedBoost:
                return root.GetComponentInChildren<SpeedBoost>(true) != null;
            case MechanicKind.BonusRoute:
                return root.GetComponentInChildren<ScorePickup>(true) != null;
            case MechanicKind.DeadlyHazard:
                return root.GetComponentInChildren<DeadlyTrigger>(true) != null;
            case MechanicKind.Conveyor:
                return ContainsWind(root, true);
            case MechanicKind.WindCurrent:
                return ContainsWind(root, false);
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
        _rootGroup.interactable = false;
        _rootGroup.blocksRaycasts = true;
        _cardGroup.alpha = 0f;
        _card.anchoredPosition = new Vector2(0f, -45f);
        _card.localScale = Vector3.one * 0.91f;

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
        FinishShowing();
    }

    void ApplyDefinition(IntroDefinition definition, int levelNumber,
        int cardNumber, int totalCards)
    {
        Color accent = definition.accent;
        if (_glow != null) _glow.color = new Color(accent.r, accent.g, accent.b, 0.18f);
        if (_topAccent != null) _topAccent.color = accent;
        if (_sideAccent != null) _sideAccent.color = accent;
        if (_iconImage != null)
            _iconImage.color = new Color(accent.r * 0.34f, accent.g * 0.34f, accent.b * 0.34f, 1f);
        if (_iconText != null)
        {
            _iconText.color = accent;
            _iconText.text = definition.icon;
        }
        if (_adviceText != null)
        {
            _adviceText.color = accent;
            _adviceText.text = definition.advice;
        }
        if (_hintText != null) _hintText.text = "PURPLE OR SKIP TO CONTINUE";
        if (_eyebrowText != null)
        {
            string sequence = totalCards > 1 ? $"  •  {cardNumber}/{totalCards}" : string.Empty;
            _eyebrowText.text = $"LEVEL {levelNumber}  •  NEW TRICK!{sequence}";
        }
        if (_titleText != null) _titleText.text = definition.title;
        if (_descriptionText != null) _descriptionText.text = definition.description;
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
            _card.localScale = Vector3.LerpUnclamped(Vector3.one * 0.91f, Vector3.one, settle);
            yield return null;
        }

        _rootGroup.alpha = 1f;
        _rootGroup.interactable = true;
        _cardGroup.alpha = 1f;
        _card.anchoredPosition = Vector2.zero;
        _card.localScale = Vector3.one;
    }

    IEnumerator FadeCard(float from, float to, float duration, float slideFrom)
    {
        float elapsed = 0f;
        Vector2 start = from > to ? Vector2.zero : new Vector2(slideFrom, 0f);
        Vector2 end = from > to ? new Vector2(-slideFrom, 0f) : Vector2.zero;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = EaseOutCubic(elapsed / Mathf.Max(0.01f, duration));
            _cardGroup.alpha = Mathf.Lerp(from, to, k);
            _card.anchoredPosition = Vector2.LerpUnclamped(start, end, k);
            yield return null;
        }
        _cardGroup.alpha = to;
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

    IEnumerator AnimateOut()
    {
        _rootGroup.interactable = false;
        const float duration = 0.26f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            _rootGroup.alpha = 1f - k;
            _card.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0f, 24f), k);
            _card.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.975f, k);
            yield return null;
        }
    }

    void Update()
    {
        if (_showing && ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Purple))
            SkipTutorial();
    }

    void FinishShowing()
    {
        _showing = false;
        _skipRequested = false;
        Action callback = _onFinished;
        _onFinished = null;
        HideImmediate();
        callback?.Invoke();
    }

    void HideImmediate()
    {
        if (_rootGroup != null)
        {
            _rootGroup.alpha = 0f;
            _rootGroup.interactable = false;
            _rootGroup.blocksRaycasts = false;
        }
        if (_cardGroup != null) _cardGroup.alpha = 0f;
        if (_card != null)
        {
            _card.anchoredPosition = Vector2.zero;
            _card.localScale = Vector3.one;
        }
        if (_canvasRoot != null) _canvasRoot.SetActive(false);
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

    void OnDestroy()
    {
        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(SkipTutorial);
    }
}
