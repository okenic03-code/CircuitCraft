using System;
using System.Collections.Generic;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Pure C# DTO carrying all inputs needed for score calculation.
    /// The Unity caller constructs this from StageDefinition + BoardState data.
    /// </summary>
    public readonly struct ScoringInput
    {
        /// <summary>Whether the circuit passed all test cases.</summary>
        public bool CircuitPassed { get; }

        /// <summary>Sum of BaseCost for every placed component.</summary>
        public float TotalComponentCost { get; }

        /// <summary>Budget limit from StageDefinition. 0 = no limit (auto-pass).</summary>
        public float BudgetLimit { get; }

        /// <summary>Total board area covered by the placed components.</summary>
        public int BoardArea { get; }

        /// <summary>Target board area used for scoring.</summary>
        public int TargetArea { get; }

        /// <summary>Number of traces (wires) on the board.</summary>
        public int TraceCount { get; }

        public ScoringInput(
            bool circuitPassed,
            float totalComponentCost,
            float budgetLimit,
            int boardArea,
            int targetArea,
            int traceCount)
        {
            CircuitPassed = circuitPassed;
            TotalComponentCost = totalComponentCost;
            BudgetLimit = budgetLimit;
            BoardArea = boardArea;
            TargetArea = targetArea;
            TraceCount = traceCount;
        }
    }

    /// <summary>
    /// A single line in the score breakdown (e.g. "Circuit Works: +1000").
    /// </summary>
    public class ScoreLineItem
    {
        /// <summary>Human-readable label for this score component.</summary>
        public string Label { get; }

        /// <summary>Points awarded (or 0 if not earned).</summary>
        public int Points { get; }

        public ScoreLineItem(string label, int points)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Points = points;
        }

        public override string ToString() => $"{Label}: {(Points >= 0 ? "+" : "")}{Points}";
    }

    /// <summary>
    /// Immutable result of scoring a completed circuit.
    /// Contains point totals, star rating, and a detailed line-item breakdown.
    /// </summary>
    public class ScoreBreakdown
    {
        /// <summary>1000 if circuit works, 0 otherwise.</summary>
        public int BaseScore { get; }

        /// <summary>+500 if total component cost is within budget.</summary>
        public int BudgetBonus { get; }

        /// <summary>+300 scaled by area ratio versus target area.</summary>
        public int AreaBonus { get; }

        /// <summary>Sum of BaseScore + BudgetBonus + AreaBonus.</summary>
        public int TotalScore { get; }

        /// <summary>0-3 star rating.</summary>
        public int Stars { get; }

        /// <summary>Whether the circuit passed evaluation.</summary>
        public bool Passed { get; }

        /// <summary>Detailed line-item breakdown of the score.</summary>
        public List<ScoreLineItem> LineItems { get; }

        /// <summary>Human-readable summary of the score.</summary>
        public string Summary { get; }

        public ScoreBreakdown(
            int baseScore,
            int budgetBonus,
            int areaBonus,
            int totalScore,
            int stars,
            bool passed,
            List<ScoreLineItem> lineItems,
            string summary)
        {
            BaseScore = baseScore;
            BudgetBonus = budgetBonus;
            AreaBonus = areaBonus;
            TotalScore = totalScore;
            Stars = stars;
            Passed = passed;
            LineItems = lineItems ?? new();
            Summary = summary ?? string.Empty;
        }
    }
}
