using System.Collections.Generic;
using NUnit.Framework;
using CircuitCraft.Core;

namespace CircuitCraft.Tests.Core
{
    [TestFixture]
    public class ScoringSystemTests
    {
        private ScoringSystem _scoringSystem;

        [SetUp]
        public void SetUp()
        {
            _scoringSystem = new ScoringSystem();
        }

        // ------------------------------------------------------------------ //
        // Failed circuit
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_FailedCircuit_Returns0StarsAnd0Score()
        {
            var input = new ScoringInput(
                circuitPassed: false,
                totalComponentCost: 50f,
                budgetLimit: 100f,
                boardArea: 5,
                targetArea: 10,
                traceCount: 0);

            var result = _scoringSystem.Calculate(input);

            Assert.IsFalse(result.Passed, "Passed should be false when circuit fails");
            Assert.AreEqual(0, result.Stars, "Failed circuit should yield 0 stars");
            Assert.AreEqual(0, result.TotalScore, "Failed circuit should yield 0 total score");
            Assert.AreEqual(0, result.BaseScore, "Failed circuit should yield 0 base score");
            Assert.AreEqual(0, result.BudgetBonus, "Failed circuit should yield 0 budget bonus");
            Assert.AreEqual(0, result.AreaBonus, "Failed circuit should yield 0 area bonus");
        }

        [Test]
        public void Calculate_FailedCircuit_SummaryContainsFailedText()
        {
            var input = new ScoringInput(false, 0f, 0f, 1, 1, 0);

            var result = _scoringSystem.Calculate(input);

            StringAssert.Contains("FAILED", result.Summary,
                "Summary should contain FAILED for a failed circuit");
        }

        [Test]
        public void Calculate_FailedCircuit_LineItemContainsCircuitFailed()
        {
            var input = new ScoringInput(false, 0f, 0f, 1, 1, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.Greater(result.LineItems.Count, 0, "LineItems should not be empty");
            bool found = false;
            foreach (var item in result.LineItems)
            {
                if (item.Label.Contains("Circuit Failed"))
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "LineItems should contain a 'Circuit Failed' entry");
        }

        // ------------------------------------------------------------------ //
        // Passed circuit — all bonuses earned (3 stars)
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_PassedUnderBudgetWithinArea_Returns3Stars()
        {
            var input = new ScoringInput(
                circuitPassed: true,
                totalComponentCost: 50f,
                budgetLimit: 100f,
                boardArea: 5,
                targetArea: 10,
                traceCount: 0);

            var result = _scoringSystem.Calculate(input);

            Assert.IsTrue(result.Passed, "Passed should be true");
            Assert.AreEqual(3, result.Stars, "Should be 3 stars with all bonuses");
        }

        [Test]
        public void Calculate_PassedUnderBudgetWithinArea_BaseScoreIs1000()
        {
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(1000, result.BaseScore, "Base score should be 1000 on pass");
        }

        [Test]
        public void Calculate_PassedUnderBudgetWithinArea_BudgetBonusIs500()
        {
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(500, result.BudgetBonus, "Budget bonus should be 500 when under budget");
        }

        [Test]
        public void Calculate_PassedUnderBudgetWithinArea_AreaBonusIsPositive()
        {
            // boardArea=5, targetArea=10 → ratio=0.5 → factor=min(1, 2-0.5)=1.0 → areaBonus=300
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.Greater(result.AreaBonus, 0, "Area bonus should be positive when within target");
        }

        [Test]
        public void Calculate_PassedUnderBudgetWithinArea_TotalScoreIs1800()
        {
            // boardArea=5, targetArea=10 → ratio=0.5 → areaFactor=1.0 → areaBonus=300
            // total = 1000 + 500 + 300 = 1800
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(1800, result.TotalScore, "Total score should be 1800 for full 3-star run");
        }

        // ------------------------------------------------------------------ //
        // Passed + over budget + within area (2 stars)
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_PassedOverBudgetWithinArea_Returns2Stars()
        {
            var input = new ScoringInput(
                circuitPassed: true,
                totalComponentCost: 150f,
                budgetLimit: 100f,
                boardArea: 5,
                targetArea: 10,
                traceCount: 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(2, result.Stars, "Over-budget pass should yield 2 stars");
        }

        [Test]
        public void Calculate_PassedOverBudgetWithinArea_BudgetBonusIs0()
        {
            var input = new ScoringInput(true, 150f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(0, result.BudgetBonus, "Budget bonus should be 0 when over budget");
        }

        // ------------------------------------------------------------------ //
        // Passed + under budget + over area (2 stars)
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_PassedUnderBudgetOverArea_Returns2Stars()
        {
            var input = new ScoringInput(
                circuitPassed: true,
                totalComponentCost: 50f,
                budgetLimit: 100f,
                boardArea: 20,
                targetArea: 10,
                traceCount: 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(2, result.Stars, "Over-area pass should yield 2 stars");
        }

        [Test]
        public void Calculate_PassedUnderBudgetOverArea_AreaBonusIs0()
        {
            // boardArea=20, targetArea=10 → ratio=2.0 → factor=max(0, 2-2.0)=0 → areaBonus=0
            var input = new ScoringInput(true, 50f, 100f, 20, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(0, result.AreaBonus, "Area bonus should be 0 when twice the target");
        }

        // ------------------------------------------------------------------ //
        // Passed + over budget + over area (1 star)
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_PassedOverBudgetOverArea_Returns1Star()
        {
            var input = new ScoringInput(
                circuitPassed: true,
                totalComponentCost: 150f,
                budgetLimit: 100f,
                boardArea: 20,
                targetArea: 10,
                traceCount: 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(1, result.Stars, "No bonus pass should yield 1 star");
        }

        [Test]
        public void Calculate_PassedOverBudgetOverArea_TotalScoreIs1000()
        {
            var input = new ScoringInput(true, 150f, 100f, 20, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(1000, result.TotalScore, "Total score should be 1000 (base only) when no bonuses");
        }

        // ------------------------------------------------------------------ //
        // No budget limit (auto-pass budget)
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_NoBudgetLimit_BudgetBonusEarned()
        {
            // budgetLimit=0 → auto-pass regardless of cost
            var input = new ScoringInput(
                circuitPassed: true,
                totalComponentCost: 99999f,
                budgetLimit: 0f,
                boardArea: 20,
                targetArea: 10,
                traceCount: 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(500, result.BudgetBonus, "budgetLimit=0 should auto-pass and grant 500 budget bonus");
        }

        [Test]
        public void Calculate_NoBudgetLimit_LineItemContainsBudgetNoLimit()
        {
            var input = new ScoringInput(true, 99999f, 0f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            bool found = false;
            foreach (var item in result.LineItems)
            {
                if (item.Label.Contains("Budget: No Limit"))
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "LineItems should show 'Budget: No Limit' when budgetLimit is 0");
        }

        // ------------------------------------------------------------------ //
        // Area bonus linear scaling
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_AreaEqualToTarget_AreaBonusIs300()
        {
            // boardArea == targetArea → ratio=1.0 → factor=min(1, 2-1)=1 → bonus=300
            var input = new ScoringInput(true, 50f, 100f, 10, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(300, result.AreaBonus, "Area bonus should be 300 when board == target area");
        }

        [Test]
        public void Calculate_AreaTwiceTarget_AreaBonusIs0()
        {
            // boardArea == 2*targetArea → ratio=2.0 → factor=max(0, 0)=0 → bonus=0
            var input = new ScoringInput(true, 50f, 100f, 20, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(0, result.AreaBonus, "Area bonus should be 0 when board is twice the target area");
        }

        [Test]
        public void Calculate_Area1p5xTarget_AreaBonusIs150()
        {
            // boardArea=15, targetArea=10 → ratio=1.5 → factor=max(0, min(1, 2-1.5))=0.5 → bonus=300*0.5=150
            var input = new ScoringInput(true, 50f, 100f, 15, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(150, result.AreaBonus, 1,
                "Area bonus should be ~150 when board is 1.5x the target area");
        }

        [Test]
        public void Calculate_AreaHalfTarget_AreaBonusIs300()
        {
            // boardArea < targetArea → ratio<1 → factor=min(1, something>1)=1 → bonus=300
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(300, result.AreaBonus, "Area bonus is capped at 300 even when well under target");
        }

        // ------------------------------------------------------------------ //
        // Line item label verification
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_Passed_LineItemContainsCircuitWorks()
        {
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            bool found = false;
            foreach (var item in result.LineItems)
            {
                if (item.Label.Contains("Circuit Works"))
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "LineItems should contain 'Circuit Works' on pass");
        }

        [Test]
        public void Calculate_OverBudget_LineItemContainsBudgetValues()
        {
            var input = new ScoringInput(true, 150f, 100f, 20, 10, 0);

            var result = _scoringSystem.Calculate(input);

            bool found = false;
            foreach (var item in result.LineItems)
            {
                if (item.Label.Contains("150") && item.Label.Contains("100"))
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "LineItems should contain cost/limit values in budget line");
        }

        [Test]
        public void Calculate_UnderBudget_LineItemContainsBudgetValues()
        {
            var input = new ScoringInput(true, 50f, 100f, 20, 10, 0);

            var result = _scoringSystem.Calculate(input);

            bool found = false;
            foreach (var item in result.LineItems)
            {
                if (item.Label.Contains("50") && item.Label.Contains("100"))
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "LineItems should contain cost/limit values in budget line");
        }

        [Test]
        public void Calculate_AreaLine_ContainsBoardAreaAndTargetArea()
        {
            var input = new ScoringInput(true, 50f, 100f, 7, 10, 0);

            var result = _scoringSystem.Calculate(input);

            bool found = false;
            foreach (var item in result.LineItems)
            {
                if (item.Label.Contains("7") && item.Label.Contains("10"))
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "LineItems should contain boardArea/targetArea values in area line");
        }

        // ------------------------------------------------------------------ //
        // Summary text
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_Passed_SummaryContainsStarCount()
        {
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            StringAssert.Contains("3 Stars", result.Summary);
        }

        [Test]
        public void Calculate_1Star_SummaryContainsSingularStar()
        {
            var input = new ScoringInput(true, 150f, 100f, 20, 10, 0);

            var result = _scoringSystem.Calculate(input);

            StringAssert.Contains("1 Star", result.Summary);
        }

        // ------------------------------------------------------------------ //
        // Edge cases
        // ------------------------------------------------------------------ //

        [Test]
        public void Calculate_ExactlyAtBudgetLimit_EarnsBonus()
        {
            var input = new ScoringInput(true, 100f, 100f, 20, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(500, result.BudgetBonus,
                "Cost exactly at limit should still earn budget bonus");
        }

        [Test]
        public void Calculate_ZeroTargetArea_DoesNotCrash()
        {
            // targetArea=0 → clamped to max(1,0)=1, boardArea=0 → clamped to 1 → ratio=1 → bonus=300
            var input = new ScoringInput(true, 50f, 100f, 0, 0, 0);

            // Should not throw
            var result = _scoringSystem.Calculate(input);

            Assert.IsNotNull(result, "Result should not be null even with zero areas");
        }

        [Test]
        public void Calculate_LineItemsCount_MatchesExpectedStructure()
        {
            // Passed circuit always produces: Circuit Works + budget line + area line = 3 items
            var input = new ScoringInput(true, 50f, 100f, 5, 10, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(3, result.LineItems.Count,
                "Passed circuit should have 3 line items: circuit, budget, area");
        }

        [Test]
        public void Calculate_FailedCircuit_LineItemsCount_Is1()
        {
            // Fail early-returns after adding Circuit Failed item only
            var input = new ScoringInput(false, 0f, 0f, 0, 0, 0);

            var result = _scoringSystem.Calculate(input);

            Assert.AreEqual(1, result.LineItems.Count,
                "Failed circuit should have exactly 1 line item");
        }
    }
}
