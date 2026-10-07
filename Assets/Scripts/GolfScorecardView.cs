using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pre-authored course scorecard. Runtime code only updates serialized text and
/// colors; every visual object is baked into the prefab by the editor pipeline.
/// </summary>
public sealed class GolfScorecardView : MonoBehaviour
{
    [Header("Prebuilt references")]
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text currentResultText;
    [SerializeField] TMP_Text courseTotalText;
    [SerializeField] TMP_Text[] holeTexts;
    [SerializeField] Image[] holeBackgrounds;

    static readonly Color EmptyCell = new Color(0.025f, 0.08f, 0.13f, 0.88f);
    static readonly Color UnderCell = new Color(0.035f, 0.25f, 0.22f, 0.96f);
    static readonly Color EvenCell = new Color(0.10f, 0.20f, 0.28f, 0.96f);
    static readonly Color OverCell = new Color(0.30f, 0.075f, 0.12f, 0.96f);
    static readonly Color CurrentCell = new Color(0.06f, 0.38f, 0.49f, 1f);

    public void Configure(CanvasGroup canvasGroup, TMP_Text currentResult,
        TMP_Text courseTotal, TMP_Text[] cells, Image[] backgrounds)
    {
        group = canvasGroup;
        currentResultText = currentResult;
        courseTotalText = courseTotal;
        holeTexts = cells;
        holeBackgrounds = backgrounds;
    }

    public void Populate(IReadOnlyList<LevelManager.HoleScoreEntry> entries,
        int currentHoleNumber, int totalHoleCount)
    {
        if (group != null) { group.alpha=0f; group.interactable=false; group.blocksRaycasts=false; }
        gameObject.SetActive(false);
    }

    static bool TryFindEntry(IReadOnlyList<LevelManager.HoleScoreEntry> entries,
        int holeNumber, out LevelManager.HoleScoreEntry result)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].holeNumber != holeNumber) continue;
                result = entries[i];
                return true;
            }
        }

        result = default;
        return false;
    }

    static string ResultColor(int toPar)
    {
        if (toPar < 0) return "#5CFFB0";
        if (toPar == 0) return "#64E9FF";
        return "#FF637D";
    }

    static string PlainResult(int toPar)
    {
        if (toPar < 0) return $"{Mathf.Abs(toPar)} UNDER PAR";
        if (toPar == 0) return "EVEN PAR";
        return $"{toPar} OVER PAR";
    }
}
