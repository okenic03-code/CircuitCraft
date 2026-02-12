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
    ///   2★ = circuit works + (under budget OR within component limit)
    ///   3★ = circuit works + under budget + within component limit
    /// </summary>
    public class ScoringSystem
    {
        private const int BASE_SCORE = 1000;
        private const int BUDGET_BONUS = 500;
        private const int COMPACT_BONUS = 300;

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
                return BuildResult(baseScore, 0, 0, false, lineItems);
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

            // --- Compact bonus ---
            bool withinComponentLimit = IsWithinComponentLimit(input);
            int compactBonus = 0;
            if (withinComponentLimit)
            {
                compactBonus = COMPACT_BONUS;
                if (input.MaxComponentCount > 0)
                    lineItems.Add(new ScoreLineItem(
                        $"Compact Build ({input.ComponentCount}/{input.MaxComponentCount})",
                        COMPACT_BONUS));
                else
                    lineItems.Add(new ScoreLineItem("Components: No Limit", COMPACT_BONUS));
            }
            else
            {
                lineItems.Add(new ScoreLineItem(
                    $"Too Many Components ({input.ComponentCount}/{input.MaxComponentCount})",
                    0));
            }

            return BuildResult(baseScore, budgetBonus, compactBonus, true, lineItems);
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
        /// Within component limit if no limit is set (0) or count &lt;= max.
        /// </summary>
        private static bool IsWithinComponentLimit(ScoringInput input)
        {
            if (input.MaxComponentCount <= 0) return true; // no limit → auto-pass
            return input.ComponentCount <= input.MaxComponentCount;
        }

        /// <summary>
        /// Calculates star count from bonus flags.
        /// </summary>
        private static int CalculateStars(bool passed, bool underBudget, bool withinComponentLimit)
        {
            if (!passed) return 0;

            int bonusCount = 0;
            if (underBudget) bonusCount++;
            if (withinComponentLimit) bonusCount++;

            // 1★ base + bonuses (max 3★)
            return 1 + bonusCount;
        }

        private static ScoreBreakdown BuildResult(
            int baseScore,
            int budgetBonus,
            int compactBonus,
            bool passed,
            List<ScoreLineItem> lineItems)
        {
            int totalScore = baseScore + budgetBonus + compactBonus;
            bool underBudget = budgetBonus > 0;
            bool withinLimit = compactBonus > 0;
            int stars = CalculateStars(passed, underBudget, withinLimit);

            string summary = BuildSummary(passed, stars, totalScore, lineItems);

            return new ScoreBreakdown(
                baseScore,
                budgetBonus,
                compactBonus,
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
