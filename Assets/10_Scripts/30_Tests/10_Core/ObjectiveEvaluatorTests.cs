using NUnit.Framework;
using System;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Simulation;

namespace CircuitCraft.Tests.Core
{
    [TestFixture]
    public class ObjectiveEvaluatorTests
    {
        private ObjectiveEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new ObjectiveEvaluator();
        }

        // ------------------------------------------------------------------ //
        // Helpers — no tuple syntax for LSP compatibility
        // ------------------------------------------------------------------ //

        /// <summary>Builds a successful SimulationResult with no probes.</summary>
        private static SimulationResult MakeSuccessResult()
        {
            return SimulationResult.Success(SimulationType.DCOperatingPoint, 1.0);
        }

        /// <summary>Builds a successful SimulationResult with one voltage probe.</summary>
        private static SimulationResult MakeSuccessResult(string node, double voltage)
        {
            var result = SimulationResult.Success(SimulationType.DCOperatingPoint, 1.0);
            result.ProbeResults.Add(new ProbeResult(node, ProbeType.Voltage, node, voltage));
            return result;
        }

        /// <summary>Builds a successful SimulationResult with two voltage probes.</summary>
        private static SimulationResult MakeSuccessResult(
            string node1, double voltage1,
            string node2, double voltage2)
        {
            var result = SimulationResult.Success(SimulationType.DCOperatingPoint, 1.0);
            result.ProbeResults.Add(new ProbeResult(node1, ProbeType.Voltage, node1, voltage1));
            result.ProbeResults.Add(new ProbeResult(node2, ProbeType.Voltage, node2, voltage2));
            return result;
        }

        /// <summary>Builds a successful SimulationResult with three voltage probes.</summary>
        private static SimulationResult MakeSuccessResult(
            string node1, double voltage1,
            string node2, double voltage2,
            string node3, double voltage3)
        {
            var result = SimulationResult.Success(SimulationType.DCOperatingPoint, 1.0);
            result.ProbeResults.Add(new ProbeResult(node1, ProbeType.Voltage, node1, voltage1));
            result.ProbeResults.Add(new ProbeResult(node2, ProbeType.Voltage, node2, voltage2));
            result.ProbeResults.Add(new ProbeResult(node3, ProbeType.Voltage, node3, voltage3));
            return result;
        }

        /// <summary>Builds a failed SimulationResult.</summary>
        private static SimulationResult MakeFailureResult(string message)
        {
            return SimulationResult.Failure(
                SimulationType.DCOperatingPoint,
                SimulationStatus.ConvergenceFailure,
                message);
        }

        private static SimulationResult MakeFailureResult()
        {
            return MakeFailureResult("Convergence failure");
        }

        // ------------------------------------------------------------------ //
        // Null guard tests
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_NullSimResult_ThrowsArgumentNullException()
        {
            var testCases = new TestCaseInput[0];

            Assert.Throws<ArgumentNullException>(() =>
                _evaluator.Evaluate(null, testCases));
        }

        [Test]
        public void Evaluate_NullTestCases_ThrowsArgumentNullException()
        {
            var simResult = MakeSuccessResult();

            Assert.Throws<ArgumentNullException>(() =>
                _evaluator.Evaluate(simResult, null));
        }

        // ------------------------------------------------------------------ //
        // Simulation-failed path
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_SimulationFailed_ReturnsFailed()
        {
            var simResult = MakeFailureResult("No convergence");
            var testCases = new[]
            {
                new TestCaseInput("nodeA", 5.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Passed, "Evaluation must fail when simulation failed");
        }

        [Test]
        public void Evaluate_SimulationFailed_ReturnsEmptyResults()
        {
            var simResult = MakeFailureResult();
            var testCases = new[]
            {
                new TestCaseInput("nodeA", 5.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.AreEqual(0, result.Results.Count,
                "SimulationFailed path should return empty Results list");
        }

        [Test]
        public void Evaluate_SimulationFailed_SummaryContainsReason()
        {
            var simResult = MakeFailureResult("Timed out after 5s");
            var testCases = new TestCaseInput[0];

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("Timed out after 5s", result.Summary,
                "Summary should include the failure reason from StatusMessage");
        }

        [Test]
        public void Evaluate_SimulationFailed_SummaryContainsEvaluationFailed()
        {
            var simResult = MakeFailureResult("any error");
            var testCases = new TestCaseInput[0];

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("Evaluation failed", result.Summary);
        }

        // ------------------------------------------------------------------ //
        // Empty test cases
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_EmptyTestCases_ReturnsPassedTrue()
        {
            var simResult = MakeSuccessResult("nodeA", 5.0);
            var testCases = new TestCaseInput[0];

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(result.Passed,
                "With no test cases, allPassed starts true and stays true");
        }

        [Test]
        public void Evaluate_EmptyTestCases_ReturnsEmptyResults()
        {
            var simResult = MakeSuccessResult();
            var testCases = new TestCaseInput[0];

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.AreEqual(0, result.Results.Count);
        }

        [Test]
        public void Evaluate_EmptyTestCases_SummaryContainsPassed()
        {
            var simResult = MakeSuccessResult();
            var testCases = new TestCaseInput[0];

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("PASSED", result.Summary);
        }

        // ------------------------------------------------------------------ //
        // All test cases pass
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_AllTestCasesPass_ReturnsPassed()
        {
            var simResult = MakeSuccessResult("out", 5.0, "vcc", 9.0);
            var testCases = new[]
            {
                new TestCaseInput("out", 5.0, 0.01),
                new TestCaseInput("vcc", 9.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(result.Passed, "All passing test cases should yield Passed=true");
        }

        [Test]
        public void Evaluate_AllTestCasesPass_AllResultsArePassed()
        {
            var simResult = MakeSuccessResult("out", 5.0, "vcc", 9.0);
            var testCases = new[]
            {
                new TestCaseInput("out", 5.0, 0.01),
                new TestCaseInput("vcc", 9.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            foreach (var r in result.Results)
            {
                Assert.IsTrue(r.Passed, "Test case '" + r.TestName + "' should have passed");
            }
        }

        [Test]
        public void Evaluate_AllTestCasesPass_SummaryContainsPassed()
        {
            var simResult = MakeSuccessResult("out", 5.0);
            var testCases = new[] { new TestCaseInput("out", 5.0, 0.01) };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("PASSED", result.Summary);
        }

        [Test]
        public void Evaluate_AllTestCasesPass_ResultCountMatchesTestCases()
        {
            var simResult = MakeSuccessResult("a", 1.0, "b", 2.0, "c", 3.0);
            var testCases = new[]
            {
                new TestCaseInput("a", 1.0, 0.01),
                new TestCaseInput("b", 2.0, 0.01),
                new TestCaseInput("c", 3.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.AreEqual(3, result.Results.Count);
        }

        // ------------------------------------------------------------------ //
        // Some test cases fail
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_OneTestCaseFails_ReturnsFailed()
        {
            var simResult = MakeSuccessResult("out", 3.0); // expected 5.0
            var testCases = new[]
            {
                new TestCaseInput("out", 5.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Passed, "Any failing test case should set Passed=false");
        }

        [Test]
        public void Evaluate_OneTestCaseFails_FailedResultIsMarkedFailed()
        {
            var simResult = MakeSuccessResult("out", 3.0);
            var testCases = new[]
            {
                new TestCaseInput("out", 5.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Results[0].Passed);
        }

        [Test]
        public void Evaluate_MixedResults_PassedIsFalseWhenAnyFail()
        {
            var simResult = MakeSuccessResult("nodeA", 5.0, "nodeB", 2.0); // nodeB expected 9.0
            var testCases = new[]
            {
                new TestCaseInput("nodeA", 5.0, 0.01),  // pass
                new TestCaseInput("nodeB", 9.0, 0.01)   // fail
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Passed);
            Assert.IsTrue(result.Results[0].Passed, "nodeA should pass");
            Assert.IsFalse(result.Results[1].Passed, "nodeB should fail");
        }

        [Test]
        public void Evaluate_SomeFail_SummaryContainsFailed()
        {
            var simResult = MakeSuccessResult("out", 0.0);
            var testCases = new[] { new TestCaseInput("out", 5.0, 0.01) };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("FAILED", result.Summary);
        }

        [Test]
        public void Evaluate_SomeFail_SummaryContainsPassRatio()
        {
            // 1 of 2 pass
            var simResult = MakeSuccessResult("a", 1.0, "b", 99.0);
            var testCases = new[]
            {
                new TestCaseInput("a", 1.0, 0.01),   // pass
                new TestCaseInput("b", 5.0, 0.01)    // fail
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("1/2", result.Summary,
                "Summary should contain pass ratio e.g. '1/2 test cases'");
        }

        // ------------------------------------------------------------------ //
        // Missing node (voltage not found in sim result)
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_NodeMissingFromSimResult_TestCaseFails()
        {
            var simResult = MakeSuccessResult(); // no probes for "missing_node"
            var testCases = new[]
            {
                new TestCaseInput("missing_node", 5.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Passed);
            Assert.IsFalse(result.Results[0].Passed);
        }

        [Test]
        public void Evaluate_NodeMissingFromSimResult_ActualValueIsNaN()
        {
            var simResult = MakeSuccessResult();
            var testCases = new[]
            {
                new TestCaseInput("ghost_node", 5.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(double.IsNaN(result.Results[0].ActualValue),
                "Missing node should produce NaN as actual value");
        }

        [Test]
        public void Evaluate_NodeMissingFromSimResult_MessageContainsNodeName()
        {
            var simResult = MakeSuccessResult();
            var testCases = new[]
            {
                new TestCaseInput("node_xyz", 5.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("node_xyz", result.Results[0].Message,
                "Failure message should name the missing node");
        }

        // ------------------------------------------------------------------ //
        // Tolerance boundary tests
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_ValueExactlyAtExpected_Passes()
        {
            var simResult = MakeSuccessResult("vout", 5.0);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.001) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(result.Results[0].Passed,
                "Exact match should always pass");
        }

        [Test]
        public void Evaluate_ValueExactlyAtPositiveTolerance_Passes()
        {
            // actual = expected + tolerance exactly -> difference == tolerance -> passed (<=)
            var simResult = MakeSuccessResult("vout", 5.1);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.1) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(result.Results[0].Passed,
                "Value at exactly +tolerance boundary should pass (<=)");
        }

        [Test]
        public void Evaluate_ValueExactlyAtNegativeTolerance_Passes()
        {
            // actual = expected - tolerance exactly -> passes
            var simResult = MakeSuccessResult("vout", 4.9);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.1) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(result.Results[0].Passed,
                "Value at exactly -tolerance boundary should pass (<=)");
        }

        [Test]
        public void Evaluate_ValueJustBeyondPositiveTolerance_Fails()
        {
            // actual = expected + tolerance + epsilon -> fails
            var simResult = MakeSuccessResult("vout", 5.101);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.1) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Results[0].Passed,
                "Value beyond +tolerance boundary should fail");
        }

        [Test]
        public void Evaluate_ValueJustBeyondNegativeTolerance_Fails()
        {
            var simResult = MakeSuccessResult("vout", 4.899);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.1) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Results[0].Passed,
                "Value beyond -tolerance boundary should fail");
        }

        [Test]
        public void Evaluate_ZeroTolerance_ExactMatchPasses()
        {
            var simResult = MakeSuccessResult("vout", 5.0);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.0) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(result.Results[0].Passed,
                "Zero tolerance with exact match should pass");
        }

        [Test]
        public void Evaluate_ZeroTolerance_AnyDifferenceFails()
        {
            var simResult = MakeSuccessResult("vout", 5.0001);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.0) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsFalse(result.Results[0].Passed,
                "Zero tolerance: any difference should fail");
        }

        // ------------------------------------------------------------------ //
        // Result DTO correctness
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_PassingTestCase_ResultCarriesCorrectValues()
        {
            var simResult = MakeSuccessResult("vout", 4.95);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.1) };

            var result = _evaluator.Evaluate(simResult, testCases);

            var r = result.Results[0];
            Assert.AreEqual("vout", r.TestName);
            Assert.AreEqual(5.0, r.ExpectedValue, 1e-9);
            Assert.AreEqual(4.95, r.ActualValue, 1e-9);
            Assert.AreEqual(0.1, r.Tolerance, 1e-9);
            Assert.IsTrue(r.Passed);
        }

        [Test]
        public void Evaluate_FailingTestCase_MessageContainsFAIL()
        {
            var simResult = MakeSuccessResult("vout", 3.0);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.01) };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("FAIL", result.Results[0].Message);
        }

        [Test]
        public void Evaluate_PassingTestCase_MessageContainsPASS()
        {
            var simResult = MakeSuccessResult("vout", 5.0);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.01) };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("PASS", result.Results[0].Message);
        }

        [Test]
        public void Evaluate_FailingTestCase_MessageContainsDifferenceValue()
        {
            // actual=3.0, expected=5.0, difference=2.0
            var simResult = MakeSuccessResult("vout", 3.0);
            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.01) };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("off by", result.Results[0].Message,
                "Failure message should include 'off by <difference>'");
        }

        // ------------------------------------------------------------------ //
        // CompletedWithWarnings is treated as success
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_SimulationCompletedWithWarnings_IsEvaluated()
        {
            // CompletedWithWarnings -> IsSuccess=true -> should be evaluated normally
            var simResult = new SimulationResult
            {
                HasRun = true,
                Status = SimulationStatus.CompletedWithWarnings,
                StatusMessage = "Completed with minor warnings",
                SimulationType = SimulationType.DCOperatingPoint
            };
            simResult.ProbeResults.Add(new ProbeResult("vout", ProbeType.Voltage, "vout", 5.0));

            var testCases = new[] { new TestCaseInput("vout", 5.0, 0.01) };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.IsTrue(result.Passed,
                "CompletedWithWarnings is treated as IsSuccess=true and should be evaluated normally");
        }

        // ------------------------------------------------------------------ //
        // Multiple test cases — order preservation
        // ------------------------------------------------------------------ //

        [Test]
        public void Evaluate_MultipleTestCases_ResultOrderPreserved()
        {
            var simResult = MakeSuccessResult("a", 1.0, "b", 2.0, "c", 3.0);
            var testCases = new[]
            {
                new TestCaseInput("a", 1.0, 0.01),
                new TestCaseInput("b", 2.0, 0.01),
                new TestCaseInput("c", 3.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            Assert.AreEqual("a", result.Results[0].TestName);
            Assert.AreEqual("b", result.Results[1].TestName);
            Assert.AreEqual("c", result.Results[2].TestName);
        }

        [Test]
        public void Evaluate_AllPassed_SummaryContainsFullPassRatio()
        {
            var simResult = MakeSuccessResult("a", 1.0, "b", 2.0);
            var testCases = new[]
            {
                new TestCaseInput("a", 1.0, 0.01),
                new TestCaseInput("b", 2.0, 0.01)
            };

            var result = _evaluator.Evaluate(simResult, testCases);

            StringAssert.Contains("2/2", result.Summary,
                "Summary should show full pass count e.g. '2/2 test cases'");
        }
    }
}
