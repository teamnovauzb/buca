#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// ONE-SHOT: upgrade the in-game LEADERBOARD panel.
///   • clones rows up to a full 10 (rank 1..10), continuing the existing spacing
///   • restyles the card background to premium dark + a cyan accent bar
///   • highlights the player's own row in the board's cyan
///
///   RealBuca ▸ Redesign Leaderboard (10 rows + premium bg)
///
/// Pairs with the data-padding in LuxoddGameBridge (fills to 10 players).
/// AFTER running you can DELETE THIS FILE.
/// </summary>
public static class RedesignLeaderboard
{
    const string GameScene = "Assets/Scenes/Game.unity";
    static readonly Color CardDark = new Color(0.05f, 0.07f, 0.14f, 0.95f);
    static readonly Color Cyan     = new Color(0.18f, 0.88f, 1f, 1f);

    [MenuItem("RealBuca/Redesign Leaderboard (10 rows + premium bg)")]
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

        // ── grow rows up to 10 ──
        var rows = new System.Collections.Generic.List<LeaderboardRow>();
        if (panel.rows != null) foreach (var r in panel.rows) if (r != null) rows.Add(r);

        int added = 0;
        if (rows.Count >= 2 && rows.Count < 10)
        {
            var template = rows[rows.Count - 1];
            var parent = template.rectTransform != null ? template.rectTransform.parent : template.transform.parent;

            float spacing = 60f;
            if (rows[0].rectTransform != null && rows[1].rectTransform != null)
                spacing = Mathf.Abs(rows[0].rectTransform.anchoredPosition.y - rows[1].rectTransform.anchoredPosition.y);
            if (spacing < 1f) spacing = 60f;

            var lastRT = template.rectTransform != null ? template.rectTransform : template.GetComponent<RectTransform>();
            float baseY = lastRT.anchoredPosition.y;
            float baseX = lastRT.anchoredPosition.x;

            int needed = 10 - rows.Count;
            for (int i = 1; i <= needed; i++)
            {
                var clone = Object.Instantiate(template.gameObject, parent);
                clone.name = "Row_" + (rows.Count + i);
                var lr = clone.GetComponent<LeaderboardRow>();
                if (lr.rectTransform == null) lr.rectTransform = clone.GetComponent<RectTransform>();
                lr.rectTransform.anchoredPosition = new Vector2(baseX, baseY - spacing * i);
                rows.Add(lr);
                added++;
            }
            panel.rows = rows.ToArray();
        }

        // ── premium background ──
        if (panel.card != null)
        {
            RecolorCardBackground(panel.card, CardDark);
            AddAccentBar(panel.card);
        }
        panel.myRowColor = Cyan;   // player's row matches the board's neon

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Leaderboard upgraded ✓",
            $"• Rows: now {rows.Count} (added {added}).\n" +
            "• Background: premium dark + cyan accent bar.\n" +
            "• Your row highlights in cyan.\n\n" +
            "The data side (LuxoddGameBridge) now fills the board to 10 players with " +
            "filler names when the server has fewer.\n\n" +
            "Rebuild WebGL + upload to Luxodd to see it. You can DELETE this file.",
            "OK");
    }

    static void RecolorCardBackground(RectTransform card, Color c)
    {
        var own = card.GetComponent<Image>();
        if (own != null) own.color = c;
        foreach (var img in card.GetComponentsInChildren<Image>(true))
        {
            if (img.transform == card.transform) continue;
            if (img.GetComponentInParent<LeaderboardRow>() != null) continue;  // never touch row images
            var rt = img.rectTransform;
            bool stretched = rt.anchorMin.x < 0.15f && rt.anchorMax.x > 0.85f &&
                             rt.anchorMin.y < 0.15f && rt.anchorMax.y > 0.85f;
            if (stretched) img.color = c;
        }
    }

    static void AddAccentBar(RectTransform card)
    {
        var existing = card.Find("LB_AccentBar");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject("LB_AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(card, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.07f, 1f);
        rt.anchorMax = new Vector2(0.93f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -16f);
        rt.sizeDelta = new Vector2(0f, 6f);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.88f, 1f, 0.9f);
        img.raycastTarget = false;
    }
}
#endif
