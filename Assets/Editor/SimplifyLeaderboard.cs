#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ONE-SHOT: strip the in-game LEADERBOARD down to a simple, on-theme panel.
///
///   • removes the rotating glow halo, the title shimmer, and the pulsing
///     border animator (LeaderboardCardFX)
///   • recolors the card to the neon theme (near-black + static cyan border,
///     solid-cyan title, thin cyan divider)
///   • removes the gold/silver/bronze rank badges → flat rank / name / score
///   • player's own row highlights in magenta
///
///   RealBuca ▸ Simplify Leaderboard (neon minimal)
///
/// Safe to run repeatedly. Edits the existing baked objects in place (keeps
/// their sprites) rather than rebuilding, so nothing is regenerated.
/// </summary>
public static class SimplifyLeaderboard
{
    const string GameScene = "Assets/Scenes/Game.unity";

    static readonly Color CardDark  = new Color(0.05f, 0.07f, 0.14f, 0.97f);
    static readonly Color Cyan      = new Color(0.18f, 0.88f, 1.00f, 1f);
    static readonly Color CyanBorder= new Color(0.18f, 0.88f, 1.00f, 0.45f);
    static readonly Color CyanSoft  = new Color(0.64f, 0.90f, 1.00f, 1f);
    static readonly Color HotPink   = new Color(1.00f, 0.18f, 0.53f, 1f);   // #FF2E88 (theme PLAY-button pink)

    [MenuItem("RealBuca/Simplify Leaderboard (neon minimal)")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        LeaderboardPanel panel = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            panel = root.GetComponentInChildren<LeaderboardPanel>(true);
            if (panel != null) break;
        }
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Leaderboard not found",
                "Couldn't find a LeaderboardPanel in the Game scene. Nothing changed.", "OK");
            return;
        }

        var card = panel.card != null ? panel.card : FindChild(panel.transform, "Card");
        if (card == null)
        {
            EditorUtility.DisplayDialog("Card not found",
                "The LeaderboardPanel has no 'Card' child to restyle. Nothing changed.", "OK");
            return;
        }

        // ── remove the flair animator + its moving pieces ──
        var fx = card.GetComponent<LeaderboardCardFX>();
        if (fx != null) Object.DestroyImmediate(fx);

        DestroyChild(card, "GlowHalo");

        var titleBar = FindChild(card, "TitleBar");
        if (titleBar != null)
        {
            DestroyChild(titleBar, "Shimmer");
            var mask = titleBar.GetComponent<RectMask2D>();
            if (mask != null) Object.DestroyImmediate(mask);

            var titleTmp = FindTmp(titleBar, "Title");
            if (titleTmp != null)
            {
                titleTmp.enableVertexGradient = false;
                titleTmp.color = Cyan;
                titleTmp.characterSpacing = 6f;
                titleTmp.outlineWidth = 0f;
            }
        }

        // ── card background + static cyan border ──
        var cardImg = card.GetComponent<Image>();
        if (cardImg != null) cardImg.color = CardDark;

        var border = FindImage(card, "BorderOutline");
        if (border != null) border.color = CyanBorder;

        // ── subtitle + column headers to the theme ──
        var myRank = FindTmp(card, "MyRank");
        if (myRank != null) { var c = CyanSoft; c.a = 0.95f; myRank.color = c; }

        var header = FindChild(card, "HeaderStrip");
        if (header != null)
            foreach (var t in header.GetComponentsInChildren<TMP_Text>(true))
            { var c = CyanSoft; c.a = 0.55f; t.color = c; }

        // ── thin cyan divider under the title ──
        AddDivider(card);

        // ── flatten rows: no medal badges, magenta player row ──
        var rows = (panel.rows != null && panel.rows.Length > 0)
            ? panel.rows
            : card.GetComponentsInChildren<LeaderboardRow>(true);
        int rowsCleaned = 0;
        foreach (var row in rows)
        {
            if (row == null) continue;
            DestroyChild(row.transform, "RankBadge");
            row.rankBadge = null;
            row.playerRowTintColor = new Color(HotPink.r, HotPink.g, HotPink.b, 0.18f);
            if (row.rowBackground != null)
            {
                var c = row.rowBackground.color; c.a = 0f; row.rowBackground.color = c;
            }
            EditorUtility.SetDirty(row);
            rowsCleaned++;
        }

        panel.normalRowColor = CyanSoft;
        panel.myRowColor = HotPink;

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Leaderboard simplified ✓",
            "The leaderboard is now the neon-minimal panel:\n\n" +
            "• Near-black card + static cyan border + solid-cyan title\n" +
            "• Thin cyan divider, flat rank / name / score rows\n" +
            $"• {rowsCleaned} row(s) de-badged; your row highlights in hot pink\n" +
            "• Gentle fade-in — no halo, shimmer, count-up, or pulse\n\n" +
            "Rebuild WebGL + upload to Luxodd to see it in the arcade build.",
            "OK");
    }

    static void AddDivider(Transform card)
    {
        var existing = card.Find("Divider");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(card, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -170f);
        rt.sizeDelta = new Vector2(700f, 3f);
        var img = go.GetComponent<Image>();
        img.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.85f);
        img.raycastTarget = false;
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t != parent && t.name == name) return t;
        return null;
    }

    static void DestroyChild(Transform parent, string name)
    {
        var t = FindChild(parent, name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static TMP_Text FindTmp(Transform parent, string name)
    {
        var t = FindChild(parent, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    static Image FindImage(Transform parent, string name)
    {
        var t = FindChild(parent, name);
        return t != null ? t.GetComponent<Image>() : null;
    }
}
#endif
