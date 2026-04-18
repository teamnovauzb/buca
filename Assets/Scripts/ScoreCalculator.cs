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
        b.basePoints = 1000;

        // Time bonus — fraction of time remaining × 500
        b.timeBonus = timeLimit > 0f
            ? Mathf.RoundToInt((timeRemaining / timeLimit) * 500f)
            : 0;

        // Rail bonus — fraction of rails lit × 300
        b.railBonus = totalRails > 0
            ? Mathf.RoundToInt(((float)litRails / totalRails) * 300f)
            : 0;

        // Stroke bonus — fewer strokes = higher bonus, capped at 0
        int maxStrokes = threeStarStrokes * 2;
        b.strokeBonus = Mathf.Max(0, (maxStrokes - strokes) * 150);

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

        // Stars
        bool allLit = totalRails > 0 && litRails >= totalRails;
        if (strokes <= threeStarStrokes && allLit) b.stars = 3;
        else if (strokes <= threeStarStrokes || allLit) b.stars = 2;
        else b.stars = 1;

        return b;
    }
}
