using UnityEngine;

/// <summary>
/// Pure scoring math. No MonoBehaviour, no state — just takes raw level
/// stats and returns a ScoreBreakdown struct.
/// </summary>
public static class ScoreCalculator
{
    public struct ScoreBreakdown
    {
        public int basePoints;
        public int timeBonus;
        public int railBonus;
        public int strokeBonus;
        public float comboMultiplier;
        public string comboName;
        public int subtotal;
        public int total;
        public int stars;
        public int strokesUsed;
        public int threeStarStrokeTarget;
        public int par;
        public int strokesToPar;
        public string golfResult;
        public string golfScore;
        public string starReason;
    }

    /// <summary>Returns the familiar golf name for a completed level.</summary>
    public static string GetGolfResultName(int strokes, int par)
    {
        return strokes == 1 ? "HOLE IN ONE" : "LEVEL COMPLETE";
    }

    public static string GetGolfScore(int strokes, int par)
    {
        return Mathf.Max(1, strokes) + " SHOTS";
    }

    /// <summary>
    /// Kid-friendly wording for a golf score. Use this in visible UI instead
    /// of an unexplained shorthand such as -1, +1 or E.
    /// </summary>
    public static string FormatStrokesToPar(int difference)
    {
        return string.Empty;
    }

    /// <param name="strokes">Shots the player took.</param>
    /// <param name="timeRemaining">Seconds left on the clock.</param>
    /// <param name="timeLimit">Total seconds the level allowed.</param>
    /// <param name="litRails">Number of rails the puck touched.</param>
    /// <param name="totalRails">Total rails in the level.</param>
    /// <param name="threeStarStrokes">Max strokes for 3 stars.</param>
    /// <param name="comboType">0=none, 1=NICE SAVE, 2=ONE SHOT, 3=PERFECT, 4=HOLE IN ONE</param>
    public static ScoreBreakdown Calculate(
        int strokes, float timeRemaining, float timeLimit,
        int litRails, int totalRails,
        int threeStarStrokes, int comboType)
    {
        var b = new ScoreBreakdown();

        // Base — always awarded on any win
        b.basePoints = 500;

        // Time bonus — fraction of time remaining × 250
        b.timeBonus = timeLimit > 0f
            ? Mathf.RoundToInt(Mathf.Clamp01(timeRemaining / timeLimit) * 250f)
            : 0;

        // Rail bonus — fraction of rails lit × 150
        b.railBonus = totalRails > 0
            ? Mathf.RoundToInt(Mathf.Clamp01((float)litRails / totalRails) * 150f)
            : 0;

        // Stroke bonus — fewer strokes = higher bonus, capped at 0
        int maxStrokes = threeStarStrokes * 2;
        b.strokeBonus = Mathf.Max(0, (maxStrokes - strokes) * 75);

        b.subtotal = b.basePoints + b.timeBonus + b.railBonus + b.strokeBonus;

        // Combo multiplier
        switch (comboType)
        {
            case 4: b.comboMultiplier = 1.5f; b.comboName = "HOLE IN ONE"; break;
            case 3: b.comboMultiplier = 1.3f; b.comboName = "PERFECT";     break;
            case 2: b.comboMultiplier = 1.2f; b.comboName = "ONE SHOT";    break;
            case 1: b.comboMultiplier = 1.1f; b.comboName = "NICE SAVE";   break;
            default: b.comboMultiplier = 1.0f; b.comboName = "";           break;
        }

        b.total = Mathf.RoundToInt(b.subtotal * b.comboMultiplier);

        // Stars are intentionally based on shot efficiency only. Rails remain
        // valuable through railBonus, but an optional rail route must never
        // downgrade a hole-in-one to two stars.
        int threeStarTarget = Mathf.Max(1, threeStarStrokes);
        int twoStarTarget = threeStarTarget * 2;
        b.strokesUsed = Mathf.Max(1, strokes);
        b.threeStarStrokeTarget = threeStarTarget;
        b.par = threeStarTarget;
        b.strokesToPar = b.strokesUsed - b.par;
        b.golfResult = GetGolfResultName(b.strokesUsed, b.par);
        b.golfScore = GetGolfScore(b.strokesUsed, b.par);
        string golfMeaning = FormatStrokesToPar(b.strokesToPar);

        if (b.strokesUsed <= threeStarTarget)
        {
            b.stars = 3;
            b.starReason = $"{b.golfResult}  •  {golfMeaning}  •  3 PUCKS WON";
        }
        else if (b.strokesUsed <= twoStarTarget)
        {
            b.stars = 2;
            b.starReason = $"{b.golfResult}  •  {golfMeaning}  •  2 PUCKS WON";
        }
        else
        {
            b.stars = 1;
            b.starReason = $"{b.golfResult}  •  {golfMeaning}  •  1 PUCK WON";
        }

        return b;
    }
}
