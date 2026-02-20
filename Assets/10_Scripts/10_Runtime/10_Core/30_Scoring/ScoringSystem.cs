using System;
using System.Collections.Generic;
using System.Text;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Calculates a 3-star score after a circuit passes evaluation.
    /// Pure C# — no Unity dependencies. Lives in the Domain layer.
    ///
    /// Star logic:
    ///   0★ = circuit didn't pass
    ///   1★ = circuit works (base score)
    ///   2★ = circuit works + (under budget OR within area target)
    ///   3★ = circuit works + under budget + within area target
    /// </summary>
    public class ScoringSystem
    {
        private const int BASE_SCORE = 1000;
        private const int BUDGET_BONUS = 500;
        private const int AREA_BONUS = 300;

        /// <summary>
        /// Calculates score breakdown from the given scoring input.
        /// </summary>
        /// <param name="input">Scoring data assembled by the caller from stage + board state.</param>
        /// <returns>Immutable score breakdown with star rating and line items.</returns>
        public ScoreBreakdown Calculate(ScoringInput input)
        {
            var lineItems = new List<ScoreLineItem>();

            // --- Base score ---
            int baseScore = 0;
            if (input.CircuitPassed)
            {
                baseScore = BASE_SCORE;
                lineItems.Add(new ScoreLineItem("Circuit Works", BASE_SCORE));
            }
            else
            {
                lineItems.Add(new ScoreLineItem("Circuit Failed", 0));
                return BuildResult(baseScore, 0, 0, false, lineItems, false);
            }

            // --- Budget bonus ---
            bool underBudget = IsUnderBudget(input);
            int budgetBonus = 0;
            if (underBudget)
            {
                budgetBonus = BUDGET_BONUS;
                if (input.BudgetLimit > 0f)
                    lineItems.Add(new ScoreLineItem(
                        $"Under Budget ({input.TotalComponentCost:F0}/{input.BudgetLimit:F0})",
                        BUDGET_BONUS));
                else
                    lineItems.Add(new ScoreLineItem("Budget: No Limit", BUDGET_BONUS));
            }
            else
            {
                lineItems.Add(new ScoreLineItem(
                    $"Over Budget ({input.TotalComponentCost:F0}/{input.BudgetLimit:F0})",
                    0));
            }

            // --- Area bonus ---
            bool withinTarget = IsWithinAreaTarget(input);
            int areaBonus = CalculateAreaBonus(input);
            string areaLabel = withinTarget
                ? $"Small Footprint ({input.BoardArea}/{input.TargetArea})"
                : $"Over Footprint Target ({input.BoardArea}/{input.TargetArea})";
            lineItems.Add(new ScoreLineItem(areaLabel, areaBonus));

            return BuildResult(baseScore, budgetBonus, areaBonus, true, lineItems, withinTarget);
        }

        /// <summary>
        /// Under budget if no limit is set (0) or cost &lt;= limit.
        /// </summary>
        private static bool IsUnderBudget(ScoringInput input)
        {
            if (input.BudgetLimit <= 0f) return true; // no limit → auto-pass
            return input.TotalComponentCost <= input.BudgetLimit;
        }

        /// <summary>
        /// Within target area if no target is set (0) or board area is within target.
        /// </summary>
        private static bool IsWithinAreaTarget(ScoringInput input)
        {
            float targetArea = Math.Max(1f, input.TargetArea);
            float boardArea = Math.Max(1f, input.BoardArea);
            return boardArea <= targetArea;
        }

        /// <summary>
        /// Compute linear area bonus points from board-to-target area ratio.
        /// </summary>
        private static int CalculateAreaBonus(ScoringInput input)
        {
            float targetArea = Math.Max(1f, input.TargetArea);
            float boardArea = Math.Max(1, input.BoardArea);
            float areaRatio = boardArea / targetArea;
            float areaFactor = Math.Max(0f, Math.Min(1f, 2f - areaRatio));
            return (int)Math.Round(AREA_BONUS * areaFactor);
        }

        /// <summary>
        /// Calculates star count from bonus flags.
        /// </summary>
        private static int CalculateStars(bool passed, bool underBudget, bool withinTarget)
        {
            if (!passed) return 0;

            int bonusCount = 0;
            if (underBudget) bonusCount++;
            if (withinTarget) bonusCount++;

            // 1★ base + bonuses (max 3★)
            return 1 + bonusCount;
        }

        private static ScoreBreakdown BuildResult(
            int baseScore,
            int budgetBonus,
            int areaBonus,
            bool passed,
            List<ScoreLineItem> lineItems,
            bool withinTarget)
        {
            int totalScore = baseScore + budgetBonus + areaBonus;
            bool underBudget = budgetBonus > 0;
            int stars = CalculateStars(passed, underBudget, withinTarget);

            string summary = BuildSummary(passed, stars, totalScore, lineItems);

            return new ScoreBreakdown(
                baseScore,
                budgetBonus,
                areaBonus,
                totalScore,
                stars,
                passed,
                lineItems,
                summary);
        }

        private static string BuildSummary(bool passed, int stars, int totalScore, List<ScoreLineItem> lineItems)
        {
            var sb = new StringBuilder();

            if (!passed)
            {
                sb.Append("FAILED — 0 Stars");
                return sb.ToString();
            }

            sb.Append(stars);
            sb.Append(stars == 1 ? " Star" : " Stars");
            sb.Append($" — {totalScore} pts");

            foreach (var item in lineItems)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append(item.ToString());
            }

            return sb.ToString();
        }
    }
}
