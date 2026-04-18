using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime one-shot helper for the MainMenu scene. Drop on any GameObject
/// in the MainMenu scene, right-click header → "Spawn Level Select Grid".
/// Builds a 5-tile grid (one per level prefab) with star indicators,
/// wires each tile's button to LevelSelectController.StartLevel(i).
///
/// Delete the helper GameObject after you've run it once and saved.
/// </summary>
[DisallowMultipleComponent]
public class MenuSetupHelper : MonoBehaviour
{
    [Tooltip("How many levels to generate tiles for.")]
    public int levelCount = 5;
    [Tooltip("Parent canvas to add the grid into. Auto-found if left empty.")]
    public Canvas mainMenuCanvas;

    [ContextMenu("Spawn Level Select Grid")]
    public void SpawnLevelSelectGrid()
    {
        if (mainMenuCanvas == null)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases) if (c.name.Contains("MainMenu")) { mainMenuCanvas = c; break; }
            if (mainMenuCanvas == null && canvases.Length > 0) mainMenuCanvas = canvases[0];
        }
        if (mainMenuCanvas == null) { Debug.LogError("[MenuSetupHelper] No Canvas found."); return; }

        var existing = mainMenuCanvas.transform.Find("LevelSelect");
        if (existing != null) DestroyImmediate(existing.gameObject);

        // Root panel — covers bottom 50% of the canvas, below the menu buttons
        var rootGO = new GameObject("LevelSelect", typeof(RectTransform));
        rootGO.transform.SetParent(mainMenuCanvas.transform, false);
        var rrt = (RectTransform)rootGO.transform;
        rrt.anchorMin = new Vector2(0.5f, 0.5f);
        rrt.anchorMax = new Vector2(0.5f, 0.5f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0f, -380f);
        rrt.sizeDelta = new Vector2(980f, 900f);

        // Header
        var headerGO = new GameObject("Header", typeof(RectTransform));
        headerGO.transform.SetParent(rootGO.transform, false);
        var hrt = (RectTransform)headerGO.transform;
        hrt.anchorMin = new Vector2(0.5f, 1f);
        hrt.anchorMax = new Vector2(0.5f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = new Vector2(0f, 0f);
        hrt.sizeDelta = new Vector2(800f, 120f);
        var header = headerGO.AddComponent<TextMeshProUGUI>();
        header.text = "SELECT LEVEL";
        header.fontSize = 82;
        header.fontStyle = FontStyles.Bold;
        header.alignment = TextAlignmentOptions.Center;
        header.color = new Color(1f, 1f, 1f, 0.95f);
        header.outlineWidth = 0.18f;
        header.outlineColor = new Color(0.1f, 0.02f, 0.18f, 1f);

        // Grid container (5 tiles in one row — or 3+2 if you have more; here
        // we do a simple horizontal layout good for 3–6 levels).
        var gridGO = new GameObject("Grid", typeof(RectTransform));
        gridGO.transform.SetParent(rootGO.transform, false);
        var grt = (RectTransform)gridGO.transform;
        grt.anchorMin = new Vector2(0.5f, 1f);
        grt.anchorMax = new Vector2(0.5f, 1f);
        grt.pivot = new Vector2(0.5f, 1f);
        grt.anchoredPosition = new Vector2(0f, -160f);
        grt.sizeDelta = new Vector2(960f, 700f);

        // Controller
        var ctrl = rootGO.AddComponent<LevelSelectController>();
        ctrl.gameSceneName = "Game";

        int cols = Mathf.Min(5, levelCount);
        float tileW = 170f, tileH = 220f, spacing = 22f;
        float rowWidth = cols * tileW + (cols - 1) * spacing;
        float startX = -rowWidth * 0.5f + tileW * 0.5f;

        var sprite = BuildTileSprite();
        var starSprite = BuildStarSprite();

        for (int i = 0; i < levelCount; i++)
        {
            int row = i / cols;
            int col = i % cols;

            var tile = new GameObject($"Level_{i+1}", typeof(RectTransform));
            tile.transform.SetParent(gridGO.transform, false);
            var trt = (RectTransform)tile.transform;
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(
                startX + col * (tileW + spacing),
                -row * (tileH + spacing));
            trt.sizeDelta = new Vector2(tileW, tileH);

            var bg = tile.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.12f, 0.06f, 0.2f, 0.95f);

            var btn = tile.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols2 = btn.colors;
            cols2.normalColor = Color.white;
            cols2.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            btn.colors = cols2;

            // Number
            var numGO = new GameObject("Number", typeof(RectTransform));
            numGO.transform.SetParent(tile.transform, false);
            var nrt = (RectTransform)numGO.transform;
            nrt.anchorMin = new Vector2(0.5f, 1f);
            nrt.anchorMax = new Vector2(0.5f, 1f);
            nrt.pivot = new Vector2(0.5f, 1f);
            nrt.anchoredPosition = new Vector2(0f, -18f);
            nrt.sizeDelta = new Vector2(150f, 130f);
            var num = numGO.AddComponent<TextMeshProUGUI>();
            num.text = (i + 1).ToString();
            num.fontSize = 110;
            num.fontStyle = FontStyles.Bold;
            num.alignment = TextAlignmentOptions.Center;
            num.color = new Color(1f, 0.9f, 0.4f, 1f);
            num.outlineWidth = 0.2f;
            num.outlineColor = new Color(0.1f, 0.02f, 0.18f, 1f);

            // Star row
            var starRowGO = new GameObject("Stars", typeof(RectTransform));
            starRowGO.transform.SetParent(tile.transform, false);
            var strt = (RectTransform)starRowGO.transform;
            strt.anchorMin = new Vector2(0.5f, 0f);
            strt.anchorMax = new Vector2(0.5f, 0f);
            strt.pivot = new Vector2(0.5f, 0f);
            strt.anchoredPosition = new Vector2(0f, 22f);
            strt.sizeDelta = new Vector2(140f, 40f);

            var starImgs = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                var sGO = new GameObject($"Star_{s+1}", typeof(RectTransform));
                sGO.transform.SetParent(starRowGO.transform, false);
                var srt2 = (RectTransform)sGO.transform;
                srt2.anchorMin = new Vector2(0f, 0.5f);
                srt2.anchorMax = new Vector2(0f, 0.5f);
                srt2.pivot = new Vector2(0f, 0.5f);
                srt2.anchoredPosition = new Vector2(s * 46f, 0f);
                srt2.sizeDelta = new Vector2(40f, 40f);
                var img = sGO.AddComponent<Image>();
                img.sprite = starSprite;
                img.color = new Color(1f, 1f, 1f, 0.12f);
                starImgs[s] = img;
            }

            ctrl.levels.Add(new LevelSelectController.LevelEntry
            {
                displayName = $"LEVEL {i+1}",
                button = btn,
                label = num,
                stars = starImgs,
            });
        }

        Debug.Log($"[MenuSetupHelper] ✔ Spawned Level Select with {levelCount} tiles.");
    }

    // ──── Procedural sprites ────
    static Sprite BuildTileSprite()
    {
        const int size = 128, r = 22;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(0, Mathf.Max(r - x, x - (size - 1 - r)));
            float dy = Mathf.Max(0, Mathf.Max(r - y, y - (size - 1 - r)));
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01((r - d) / 1.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    static Sprite BuildStarSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float rOut = size * 0.46f;
        float rIn = rOut * 0.44f;
        var verts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float r = (i % 2 == 0) ? rOut : rIn;
            float ang = -Mathf.PI / 2f + i * Mathf.PI / 5f;
            verts[i] = c + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, PointInPolygon(new Vector2(x, y), verts)
                ? Color.white : new Color(1f, 1f, 1f, 0f));
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) /
                       (poly[j].y - poly[i].y + 1e-6f) + poly[i].x))
                inside = !inside;
            j = i;
        }
        return inside;
    }
}
