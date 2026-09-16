using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Minimal leaderboard panel shown after death or time-up, before the
/// Luxodd Continue popup. Displays the player's rank + top N entries on a
/// clean neon card that matches the in-game theme (cyan on near-black, the
/// player's own row in magenta).
///
/// Flow:
///   1. Rows are populated to their final state (rank / name / score)
///   2. The whole panel fades in once (no scale bounce, no per-row stagger)
///   3. TIME IS OVER waits for a fresh player button press; other outcomes
///      advance after autoAdvanceSeconds
///   4. Panel fades out → the Luxodd Continue popup is triggered
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [Header("Panel structure")]
    public CanvasGroup group;
    public RectTransform card;
    public TMP_Text titleText;
    public TMP_Text myRankText;
    public TMP_Text continueHintText;

    [Header("Entry rows (pre-built in scene by BucaSetupHelper)")]
    [Tooltip("Rows to populate — index 0 = rank #1. Typically 5–10 rows.")]
    public LeaderboardRow[] rows;

    [Header("Timing")]
    public float fadeInDuration = 0.35f;
    public float autoAdvanceSeconds = 4f;

    [Header("Colors")]
    [Tooltip("Text color for a normal row (neon cyan-soft).")]
    public Color normalRowColor = new Color(0.64f, 0.90f, 1f, 1f);
    [Tooltip("Text color for the player's own row (theme hot pink #FF2E88 — matches the PLAY button).")]
    public Color myRowColor = new Color(1f, 0.18f, 0.53f, 1f);

    bool _isAnimating;
    Action _onFinished;
    TMP_Text _outcomeText;
    CanvasGroup _outcomeGroup;
    RectTransform _outcomeCard;
    CanvasGroup _cardGroup;
    RawImage _blurImage;
    RenderTexture _blurTexture;
    Material _blurMaterial;
    Vector2 _cardHome;
    Vector2 _outcomeHome;
    readonly Vector3 _cardDisplayScale = Vector3.one * 0.64f;
    string _outcomeMessage = "YOU LOST";
    PremiumFailureSummary3D _premiumFailureSummary3D;
    PremiumLeaderboardFrame3D _premiumLeaderboardFrame3D;
    bool _premiumLeaderboardStyled;

    void Awake()
    {
        EnsureSideBySideLayout();
        if (continueHintText != null)
            continueHintText.gameObject.SetActive(false);
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
    }

    /// <summary>
    /// Keeps the game board visible and creates one balanced two-column group:
    /// the loss reason on the left and the leaderboard on the right.
    /// </summary>
    void EnsureSideBySideLayout()
    {
        Transform dim = transform.Find("Dim");
        if (dim != null) dim.gameObject.SetActive(false);

        Transform blurExisting = transform.Find("BlurredBackground");
        GameObject blurGo = blurExisting != null ? blurExisting.gameObject
            : new GameObject("BlurredBackground", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
        if (blurExisting == null) blurGo.transform.SetParent(transform, false);
        _blurImage = blurGo.GetComponent<RawImage>();
        RectTransform blurRt = _blurImage.rectTransform;
        blurRt.anchorMin = Vector2.zero;
        blurRt.anchorMax = Vector2.one;
        blurRt.offsetMin = Vector2.zero;
        blurRt.offsetMax = Vector2.zero;
        _blurImage.raycastTarget = false;
        _blurImage.color = Color.white;
        _blurImage.transform.SetSiblingIndex(0);

        if (card != null)
        {
            _cardGroup = card.GetComponent<CanvasGroup>();
            if (_cardGroup == null) _cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            DisableDecoration(card, "LB_AccentBar");
            DisableDecoration(card, "Divider");
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            _cardHome = new Vector2(420f, 0f);
            card.anchoredPosition = _cardHome;
            card.localScale = _cardDisplayScale;

            // Luxodd currently supplies rank/name/points only. Label this
            // column honestly so it is never confused with golf strokes,
            // where the lower number wins.
            TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null) continue;
                if (label.gameObject.name == "Header_SCORE"
                    || string.Equals(label.text?.Trim(), "SCORE", StringComparison.OrdinalIgnoreCase))
                    label.text = "POINTS";
            }

            EnsurePremiumLeaderboardVisuals();
        }

        Transform outcomeCardExisting = transform.Find("GolfResultCard");
        GameObject outcomeCardGO = outcomeCardExisting != null
            ? outcomeCardExisting.gameObject
            : new GameObject("GolfResultCard", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
        if (outcomeCardExisting == null) outcomeCardGO.transform.SetParent(transform, false);
        _outcomeCard = (RectTransform)outcomeCardGO.transform;
        _outcomeCard.anchorMin = _outcomeCard.anchorMax = new Vector2(0.5f, 0.5f);
        _outcomeCard.pivot = new Vector2(0.5f, 0.5f);
        _outcomeHome = new Vector2(-390f, 0f);
        _outcomeCard.anchoredPosition = _outcomeHome;
        _outcomeCard.sizeDelta = new Vector2(650f, 560f);

        Image outcomeImage = outcomeCardGO.GetComponent<Image>();
        Image leaderboardImage = card != null ? card.GetComponent<Image>() : null;
        if (leaderboardImage != null && leaderboardImage.sprite != null)
        {
            outcomeImage.sprite = leaderboardImage.sprite;
            outcomeImage.type = Image.Type.Sliced;
        }
        outcomeImage.color = new Color(0.025f, 0.055f, 0.11f, 0.97f);
        outcomeImage.raycastTarget = false;
        Outline outline = outcomeCardGO.GetComponent<Outline>();
        if (outline == null) outline = outcomeCardGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.16f, 0.88f, 1f, 0.58f);
        outline.effectDistance = new Vector2(3f, -3f);

        _outcomeGroup = outcomeCardGO.GetComponent<CanvasGroup>();
        if (_outcomeGroup == null) _outcomeGroup = outcomeCardGO.AddComponent<CanvasGroup>();
        ConfigureOutcomeAccent(outcomeCardGO.transform, "TopAccent", true);
        ConfigureOutcomeAccent(outcomeCardGO.transform, "BottomAccent", false);

        Transform existing = outcomeCardGO.transform.Find("OutcomeMessage");
        GameObject go = existing != null ? existing.gameObject
            : new GameObject("OutcomeMessage", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (existing == null) go.transform.SetParent(outcomeCardGO.transform, false);

        _outcomeText = go.GetComponent<TMP_Text>();
        if (_outcomeText == null) _outcomeText = go.AddComponent<TextMeshProUGUI>();
        if (titleText != null && titleText.font != null) _outcomeText.font = titleText.font;
        RectTransform rt = _outcomeText.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(28f, 24f);
        rt.offsetMax = new Vector2(-28f, -24f);
        _outcomeText.fontSize = 64f;
        _outcomeText.fontStyle = FontStyles.Bold;
        _outcomeText.characterSpacing = 2f;
        _outcomeText.alignment = TextAlignmentOptions.Center;
        _outcomeText.color = Color.white;
        _outcomeText.enableWordWrapping = false;
        _outcomeText.overflowMode = TextOverflowModes.Overflow;
        _outcomeText.raycastTarget = false;
        _outcomeText.text = _outcomeMessage;

        TMP_FontAsset outcomeFont = titleText != null ? titleText.font : null;
        _premiumFailureSummary3D = PremiumFailureSummary3D.Ensure(
            outcomeCardGO.transform, outcomeFont);
        if (_premiumFailureSummary3D != null && _premiumFailureSummary3D.IsReady)
        {
            outcomeImage.enabled = false;
            outline.enabled = false;
            Transform topAccent = outcomeCardGO.transform.Find("TopAccent");
            Transform bottomAccent = outcomeCardGO.transform.Find("BottomAccent");
            if (topAccent != null) topAccent.gameObject.SetActive(false);
            if (bottomAccent != null) bottomAccent.gameObject.SetActive(false);
            _outcomeText.enabled = false;
        }

        outcomeCardGO.transform.SetAsLastSibling();

        autoAdvanceSeconds = Mathf.Max(4f, autoAdvanceSeconds);
    }

    void EnsurePremiumLeaderboardVisuals()
    {
        if (card == null) return;

        _premiumLeaderboardFrame3D = PremiumLeaderboardFrame3D.Ensure(card);
        if (_premiumLeaderboardFrame3D == null || !_premiumLeaderboardFrame3D.IsReady)
            return;

        // The former flat teal rectangle is replaced by a genuine lit 3D
        // render. Keep the existing TMP data labels above it because they stay
        // pin-sharp at every arcade resolution.
        Image oldCard = card.GetComponent<Image>();
        if (oldCard != null) oldCard.enabled = false;
        DisableDecoration(card, "BorderOutline");
        DisableDecoration(card, "Divider");

        if (_premiumLeaderboardStyled) return;
        _premiumLeaderboardStyled = true;

        normalRowColor = new Color(0.82f, 0.94f, 1f, 1f);
        myRowColor = new Color(1f, 0.72f, 0.24f, 1f);

        if (titleText != null)
        {
            titleText.text = "TOP 10  •  POINTS";
            titleText.fontSize = 56f;
            titleText.color = new Color(1f, 0.73f, 0.28f, 1f);
            titleText.characterSpacing = 3.2f;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Overflow;
            AddPremiumTextDepth(titleText, true);
        }

        if (myRankText != null)
        {
            myRankText.fontSize = 31f;
            myRankText.color = new Color(0.50f, 0.90f, 1f, 1f);
            AddPremiumTextDepth(myRankText, false);
        }

        Transform header = card.Find("HeaderStrip");
        if (header != null)
        {
            TMP_Text[] headerLabels = header.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < headerLabels.Length; i++)
            {
                TMP_Text label = headerLabels[i];
                if (label == null) continue;
                label.fontSize = 24f;
                label.color = new Color(0.46f, 0.85f, 0.96f, 0.92f);
                label.characterSpacing = 3f;
                AddPremiumTextDepth(label, false);
            }
        }

        if (rows == null) return;
        for (int i = 0; i < rows.Length; i++)
        {
            LeaderboardRow row = rows[i];
            if (row == null) continue;
            if (row.rowBackground != null) row.rowBackground.enabled = false;
            if (row.rankText != null)
            {
                row.rankText.fontSize = 30f;
                AddPremiumTextDepth(row.rankText, false);
            }
            if (row.nameText != null)
            {
                row.nameText.fontSize = 34f;
                AddPremiumTextDepth(row.nameText, false);
            }
            if (row.scoreText != null)
            {
                row.scoreText.fontSize = 35f;
                AddPremiumTextDepth(row.scoreText, false);
            }
        }
    }

    static void AddPremiumTextDepth(TMP_Text text, bool strong)
    {
        if (text == null) return;
        Outline outline = text.GetComponent<Outline>();
        if (outline == null) outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = strong
            ? new Color(0.08f, 0.025f, 0f, 0.96f)
            : new Color(0f, 0.01f, 0.025f, 0.90f);
        outline.effectDistance = strong ? new Vector2(2.5f, -2.5f) : new Vector2(1.3f, -1.3f);
    }

    static void ConfigureOutcomeAccent(Transform parent, string name, bool top)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null) go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.anchoredPosition = new Vector2(0f, top ? -12f : 12f);
        rt.sizeDelta = new Vector2(540f, 4f);
        Image image = go.GetComponent<Image>();
        image.color = top
            ? new Color(0.16f, 0.88f, 1f, 0.9f)
            : new Color(1f, 0.18f, 0.53f, 0.72f);
        image.raycastTarget = false;
    }

    void PrepareEntrancePose()
    {
        if (_cardGroup != null)
        {
            _cardGroup.alpha = 0f;
            _cardGroup.interactable = false;
            _cardGroup.blocksRaycasts = false;
        }
        if (card != null)
        {
            card.anchoredPosition = _cardHome + new Vector2(150f, 12f);
            card.localScale = _cardDisplayScale * 0.88f;
        }

        if (_outcomeGroup != null)
        {
            _outcomeGroup.alpha = 0f;
            _outcomeGroup.interactable = false;
            _outcomeGroup.blocksRaycasts = false;
        }
        if (_outcomeCard != null)
        {
            _outcomeCard.anchoredPosition = _outcomeHome + new Vector2(-115f, 0f);
            _outcomeCard.localScale = Vector3.one * 0.92f;
        }
    }

    static float EaseOutCubic(float value)
    {
        float x = 1f - Mathf.Clamp01(value);
        return 1f - x * x * x;
    }

    static float EaseOutBack(float value, float overshoot = 1.15f)
    {
        float x = Mathf.Clamp01(value) - 1f;
        return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
    }

    static void DisableDecoration(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null) child.gameObject.SetActive(false);
    }

    IEnumerator CaptureBlurredBackground()
    {
        // Capture while this panel's CanvasGroup is still transparent, then
        // run a separable Gaussian blur. The result is kept at quarter size:
        // smooth enough for a loss-screen backdrop without a continuous
        // full-resolution post-processing cost on WebGL arcade hardware.
        yield return new WaitForEndOfFrame();
        ReleaseBlurTexture();

        Texture2D screenshot = null;
        RenderTexture passTexture = null;
        try
        {
            screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null) throw new InvalidOperationException("Screen capture returned null.");

            int width = Mathf.Max(1, screenshot.width / 4);
            int height = Mathf.Max(1, screenshot.height / 4);
            _blurTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "BucaLossBackgroundBlur",
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

                // Three horizontal/vertical passes produce a properly smooth
                // Gaussian result instead of visibly enlarged low-res pixels.
                for (int pass = 0; pass < 3; pass++)
                {
                    float radius = 1.2f + pass * 0.65f;
                    blur.SetVector("_BlurDirection", new Vector4(radius, 0f, 0f, 0f));
                    Graphics.Blit(_blurTexture, passTexture, blur);
                    blur.SetVector("_BlurDirection", new Vector4(0f, radius, 0f, 0f));
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
            Debug.LogWarning($"[LeaderboardPanel] Background blur capture failed: {ex.Message}");
            Transform dim = transform.Find("Dim");
            if (dim != null) dim.gameObject.SetActive(true);
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

        // Loading from Resources keeps the shader in WebGL builds even when
        // the material is constructed at runtime rather than stored in a scene.
        Shader shader = Resources.Load<Shader>("BucaGaussianBlur");
        if (shader == null) shader = Shader.Find("Hidden/Buca/GaussianBlur");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("[LeaderboardPanel] Gaussian blur shader is unavailable; using bilinear fallback.");
            return null;
        }

        _blurMaterial = new Material(shader)
        {
            name = "Buca Gaussian Blur (Runtime)",
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

    void OnDestroy()
    {
        ReleaseBlurTexture();
        if (_blurMaterial != null)
        {
            Destroy(_blurMaterial);
            _blurMaterial = null;
        }
    }

    /// <summary>
    /// Shows the panel with the given leaderboard data. TIME IS OVER remains
    /// visible until the player presses a button; other outcomes retain their
    /// timed advance. The reveal animation acts as an input grace period so the
    /// gameplay input that preceded the result cannot dismiss it accidentally.
    /// </summary>
    public void Show(LeaderboardData[] entries, int myRank, int myScore,
        string myName, string outcomeMessage, Action onFinished)
    {
        // Re-entrancy guard: a second Show() call while we're already animating
        // would otherwise overwrite _onFinished and orphan the first caller's
        // callback. Cancel the in-flight routine first and chain the new one.
        if (_isAnimating)
        {
            Debug.LogWarning("[LeaderboardPanel] Show() called while already showing — " +
                             "cancelling current animation. Previous onFinished WILL fire so " +
                             "the original caller's flow doesn't deadlock.");
            var prev = _onFinished;
            _onFinished = null;
            StopAllCoroutines();
            prev?.Invoke();
        }

        _isAnimating = true;
        _onFinished = onFinished;
        _outcomeMessage = NormalizeOutcome(outcomeMessage);
        gameObject.SetActive(true);
        EnsureSideBySideLayout();
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        PopulateOutcomeGolfCard();
        if (_premiumFailureSummary3D != null) _premiumFailureSummary3D.Show();
        if (_premiumLeaderboardFrame3D != null) _premiumLeaderboardFrame3D.Show();
        PrepareEntrancePose();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPanelOpen();
        StartCoroutine(ShowRoutine(entries, myRank, myScore, myName));
    }

    public void Show(LeaderboardData[] entries, int myRank, int myScore,
        string myName, Action onFinished)
    {
        Show(entries, myRank, myScore, myName, "YOU LOST", onFinished);
    }

    /// <summary>Legacy overload for calls that don't provide a name.</summary>
    public void Show(LeaderboardData[] entries, int myRank, int myScore, Action onFinished)
    {
        Show(entries, myRank, myScore, "YOU", "YOU LOST", onFinished);
    }

    static string NormalizeOutcome(string outcome)
    {
        if (!string.IsNullOrWhiteSpace(outcome)
            && outcome.IndexOf("TIME", StringComparison.OrdinalIgnoreCase) >= 0)
            return "TIME IS OVER";
        return "YOU LOST";
    }

    void PopulateOutcomeGolfCard()
    {
        if (_outcomeText == null) return;

        LevelManager manager = LevelManager.Instance;
        if (manager == null)
        {
            _outcomeText.text = $"<size=48><color=#FF405A>{_outcomeMessage}</color></size>";
            return;
        }

        int strokes = Mathf.Max(0, manager.CurrentShotCount);
        int par = Mathf.Max(1, manager.CurrentPar);
        int difference = strokes - par;
        string golfScore = strokes == 0
            ? "—"
            : difference == 0 ? "E" : difference > 0 ? $"+{difference}" : difference.ToString();
        string relation = strokes == 0
            ? "NO COMPLETED SCORE"
            : difference == 0
                ? "EVEN PAR"
                : difference < 0
                    ? $"{Mathf.Abs(difference)} UNDER PAR"
                    : $"{difference} OVER PAR";
        string relationColor = strokes > 0 && difference <= 0 ? "#FFD84D" : "#FF4D72";
        bool hasCourse = manager.CampaignCompletedHoles > 0;
        string courseScore = hasCourse ? LevelManager.FormatToPar(manager.CampaignToPar) : "—";
        string courseColor = hasCourse && manager.CampaignToPar <= 0 ? "#FFD84D" : "#FF4D72";
        string courseLine = hasCourse
            ? manager.BuildCourseScorecard(6, 6)
            : "NO COMPLETED HOLES YET";

        if (_premiumFailureSummary3D != null)
            _premiumFailureSummary3D.SetData(_outcomeMessage,
                manager.CurrentLevelNumber, strokes, par);

        _outcomeText.text =
            $"<size=42><color=#FF405A>{_outcomeMessage}</color></size>\n" +
            $"<size=21><color=#7BEAFF>LEVEL {manager.CurrentLevelNumber}  •  CURRENT ATTEMPT</color></size>\n\n" +
            $"<size=52><color={relationColor}>HOLE  {golfScore}</color></size>\n" +
            $"<size=24><color=#DDF7FF>STROKES  {strokes}    •    PAR  {par}</color></size>\n" +
            $"<size=22><color={relationColor}>{relation}</color></size>\n\n" +
            $"<size=21><color=#75EDFF>COURSE  •  {manager.CampaignCompletedHoles} COMPLETED</color></size>\n" +
            $"<size=40><color={courseColor}>{courseScore}</color></size>\n" +
            $"<size=18><color=#DDF7FF>{courseLine}</color></size>";
    }

    IEnumerator ShowRoutine(LeaderboardData[] entries, int myRank, int myScore, string myName)
    {
        yield return CaptureBlurredBackground();
        if (titleText != null) titleText.text = "TOP 10 • POINTS";

        // Only show the "YOUR RANK" header when the player is NOT in the visible
        // list. Otherwise the highlighted row already tells them their rank.
        bool meInList = false;
        if (entries != null)
        {
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.playerName) && !string.IsNullOrEmpty(myName)
                    && e.playerName.Equals(myName, StringComparison.OrdinalIgnoreCase))
                {
                    meInList = true;
                    break;
                }
            }
        }
        if (myRankText != null)
        {
            if (meInList)
            {
                myRankText.text = "";
                var c = myRankText.color; c.a = 0f; myRankText.color = c;
            }
            else
            {
                string rankStr = myRank > 0 ? $"#{myRank}" : "UNRANKED";
                string nameStr = string.IsNullOrEmpty(myName) ? "YOU" : myName.ToUpper();
                myRankText.text = $"{nameStr}   {rankStr}   POINTS  {myScore:N0}";
                var c = myRankText.color; c.a = 1f; myRankText.color = c; // revealed by the group fade
            }
        }
        if (continueHintText != null)
        {
            continueHintText.gameObject.SetActive(false);
        }

        // Populate all rows to their FINAL state while the group is still
        // invisible, so the single fade below reveals the whole board at once.
        int highlightedRow = -1;
        if (rows != null)
            foreach (var r in rows)
                if (r != null) r.Clear();

        if ((entries == null || entries.Length == 0) && rows != null && rows.Length > 0)
        {
            // No entries → a single row with the player's own info.
            rows[0]?.Populate(myRank > 0 ? myRank : 1,
                string.IsNullOrEmpty(myName) ? "YOU" : myName,
                myScore, myRowColor, true);
            highlightedRow = 0;
        }
        else
        {
            int count = Mathf.Min(rows?.Length ?? 0, entries?.Length ?? 0);
            for (int i = 0; i < count; i++)
            {
                var row = rows[i];
                var data = entries[i];
                if (row == null) continue;
                bool isMe = !string.IsNullOrEmpty(data.playerName)
                            && !string.IsNullOrEmpty(myName)
                            && data.playerName.Equals(myName, StringComparison.OrdinalIgnoreCase);
                row.Populate(data.rank, data.playerName, data.score,
                    isMe ? myRowColor : normalRowColor, isMe);
                if (isMe) highlightedRow = i;
            }
        }
        if (_premiumLeaderboardFrame3D != null)
            _premiumLeaderboardFrame3D.SetHighlightedRow(highlightedRow);

        // Premium two-column reveal. The blurred world fades first, then the
        // result message glides in from the left, followed by the leaderboard
        // card from the right with a restrained overshoot/settle.
        float t = 0f;
        float entranceDuration = Mathf.Max(0.75f, fadeInDuration);
        Vector2 outcomeStart = _outcomeHome + new Vector2(-115f, 0f);
        Vector2 cardStart = _cardHome + new Vector2(150f, 12f);
        while (t < entranceDuration)
        {
            t += Time.unscaledDeltaTime;
            float timeline = Mathf.Clamp01(t / entranceDuration);

            float backdropK = EaseOutCubic(timeline / 0.28f);
            if (group != null) group.alpha = backdropK;

            float outcomeK = EaseOutBack((timeline - 0.05f) / 0.53f, 0.75f);
            if (_outcomeGroup != null) _outcomeGroup.alpha = EaseOutCubic(
                (timeline - 0.05f) / 0.38f);
            if (_outcomeCard != null)
            {
                _outcomeCard.anchoredPosition = Vector2.LerpUnclamped(
                    outcomeStart, _outcomeHome, outcomeK);
                _outcomeCard.localScale = Vector3.LerpUnclamped(
                    Vector3.one * 0.92f, Vector3.one, outcomeK);
            }

            float cardK = EaseOutBack((timeline - 0.16f) / 0.68f, 1.05f);
            if (_cardGroup != null) _cardGroup.alpha = EaseOutCubic(
                (timeline - 0.16f) / 0.48f);
            if (card != null)
            {
                card.anchoredPosition = Vector2.LerpUnclamped(
                    cardStart, _cardHome, cardK);
                card.localScale = Vector3.LerpUnclamped(
                    _cardDisplayScale * 0.88f, _cardDisplayScale, cardK);
            }
            yield return null;
        }

        if (_outcomeCard != null)
        {
            _outcomeCard.anchoredPosition = _outcomeHome;
            _outcomeCard.localScale = Vector3.one;
        }
        if (_outcomeGroup != null) _outcomeGroup.alpha = 1f;
        if (card != null) card.anchoredPosition = _cardHome;
        if (card != null) card.localScale = _cardDisplayScale;
        if (_cardGroup != null) _cardGroup.alpha = 1f;
        if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }

        bool waitForPlayer = string.Equals(
            _outcomeMessage, "TIME IS OVER", StringComparison.Ordinal);
        if (waitForPlayer)
        {
            if (continueHintText != null)
            {
                continueHintText.text = "PRESS ANY BUTTON TO CONTINUE";
                continueHintText.gameObject.SetActive(true);
                Color hintColor = continueHintText.color;
                hintColor.a = 1f;
                continueHintText.color = hintColor;
            }

            // Require a new action after the completed reveal. Orange remains
            // reserved for Luxodd's system/help overlay; White is accepted as a
            // player-facing Back button alongside the six gameplay buttons.
            while (!ArcadeInputAdapter.AnyGameplayButtonDown()
                   && !ArcadeInputAdapter.CancelDown()
                   && !Input.anyKeyDown)
            {
                yield return null;
            }
        }
        else
        {
            float countdown = autoAdvanceSeconds;
            while (countdown > 0f)
            {
                countdown -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // Fade out
        float outT = 0f, outDur = 0.25f;
        float startAlpha = group != null ? group.alpha : 1f;
        while (outT < outDur)
        {
            outT += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(outT / outDur));
            if (group != null) group.alpha = Mathf.Lerp(startAlpha, 0f, k);
            if (card != null)
            {
                card.anchoredPosition = Vector2.Lerp(
                    _cardHome, _cardHome + new Vector2(70f, 0f), k);
                card.localScale = Vector3.Lerp(
                    _cardDisplayScale, _cardDisplayScale * 0.96f, k);
            }
            if (_outcomeCard != null)
            {
                _outcomeCard.anchoredPosition = Vector2.Lerp(
                    _outcomeHome, _outcomeHome + new Vector2(-55f, 0f), k);
                _outcomeCard.localScale = Vector3.Lerp(
                    Vector3.one, Vector3.one * 0.97f, k);
            }
            yield return null;
        }
        if (card != null) card.anchoredPosition = _cardHome;
        if (card != null) card.localScale = _cardDisplayScale;
        if (_outcomeCard != null)
        {
            _outcomeCard.anchoredPosition = _outcomeHome;
            _outcomeCard.localScale = Vector3.one;
        }
        if (group != null) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        if (_premiumFailureSummary3D != null) _premiumFailureSummary3D.Hide();
        if (_premiumLeaderboardFrame3D != null) _premiumLeaderboardFrame3D.Hide();
        ReleaseBlurTexture();
        gameObject.SetActive(false);

        // Capture + clear before invoking so a re-entrant Show() in the
        // callback chain doesn't see stale state.
        var cb = _onFinished;
        _onFinished = null;
        _isAnimating = false;
        cb?.Invoke();
    }

    [Serializable]
    public struct LeaderboardData
    {
        public int rank;
        public string playerName;
        public int score;
    }
}
