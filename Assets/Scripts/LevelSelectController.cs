using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main-menu level-select grid. Stores per-level star ratings in
/// PlayerPrefs (key "BucaStars_L0".."BucaStars_LN") and builds a
/// button for each entry in `levels`. On click, the level index is
/// stashed in PlayerPrefs as "BucaPendingLevel" and the Game scene is
/// loaded; LevelManager reads that value in Start() and jumps to it.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [System.Serializable]
    public class LevelEntry
    {
        public string displayName = "LEVEL 1";
        // Optional per-entry refs (so you can design tiles freely).
        public Button button;
        public TMP_Text label;
        public Image[] stars; // 3 star Images per tile
    }

    [Header("Scene refs")]
    public List<LevelEntry> levels = new List<LevelEntry>();
    public string gameSceneName = "Game";

    [Header("Star colors")]
    public Color starLit = new Color(1f, 0.9f, 0.3f, 1f);
    public Color starUnlit = new Color(1f, 1f, 1f, 0.12f);

    public const string PendingLevelKey = "BucaPendingLevel";

    void Start()
    {
        RefreshStars();
        for (int i = 0; i < levels.Count; i++)
        {
            int capturedIndex = i; // closure safety
            var e = levels[i];
            if (e.label != null) e.label.text = string.IsNullOrEmpty(e.displayName)
                ? $"LEVEL {i+1}" : e.displayName;
            if (e.button != null)
            {
                e.button.onClick.RemoveAllListeners();
                e.button.onClick.AddListener(() => StartLevel(capturedIndex));
            }
        }
    }

    void RefreshStars()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            int stars = PlayerPrefs.GetInt(LevelManager.PrefLevelStars + i, 0);
            var entry = levels[i];
            if (entry.stars == null) continue;
            for (int s = 0; s < entry.stars.Length; s++)
            {
                if (entry.stars[s] == null) continue;
                entry.stars[s].color = (s < stars) ? starLit : starUnlit;
            }
        }
    }

    public void StartLevel(int index)
    {
        PlayerPrefs.SetInt(PendingLevelKey, index);
        PlayerPrefs.SetInt("BucaCurrentLevel", index);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }
}
