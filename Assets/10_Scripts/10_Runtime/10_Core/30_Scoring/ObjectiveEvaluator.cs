using System;
using System.Collections.Generic;
using System.Text;
using CircuitCraft.Simulation;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Evaluates simulation results against test case expectations.
    /// Pure C# — no Unity dependencies. Lives in the Domain layer.
    /// </summary>
    public class ObjectiveEvaluator
    {
        /// <summary>
        /// Evaluates a simulation result against a set of test case inputs.
        /// </summary>
        /// <param name="simResult">The simulation result to evaluate.</param>
        /// <param name="testCases">Test case inputs (converted from StageTestCase by the caller).</param>
        /// <returns>An EvaluationResult with pass/fail and per-test details.</returns>
        public EvaluationResult Evaluate(SimulationResult simResult, TestCaseInput[] testCases)
        {
            if (simResult == null)
                throw new ArgumentNullException(nameof(simResult));
            if (testCases == null)
                throw new ArgumentNullException(nameof(testCases));

            // If simulation itself failed, auto-fail the evaluation
            if (!simResult.IsSuccess)
            {
                return EvaluationResult.SimulationFailed(
                    simResult.StatusMessage ?? "Unknown simulation failure"
                );
            }

            var results = new List<TestCaseResult>(testCases.Length);
            bool allPassed = true;

            foreach (var testCase in testCases)
            {
                var result = EvaluateTestCase(simResult, testCase);
                results.Add(result);

                if (!result.Passed)
                    allPassed = false;
            }

            string summary = BuildSummary(allPassed, results);
            return new EvaluationResult(allPassed, results, summary);
        }

        private TestCaseResult EvaluateTestCase(SimulationResult simResult, TestCaseInput testCase)
        {
            double? actualVoltage = simResult.GetVoltage(testCase.TestName);

            if (!actualVoltage.HasValue)
            {
                return new TestCaseResult(
                    testCase.TestName,
                    testCase.ExpectedVoltage,
                    double.NaN,
                    testCase.Tolerance,
                    false,
                    $"No voltage reading found for node '{testCase.TestName}'"
                );
            }

            double actual = actualVoltage.Value;
            double difference = Math.Abs(actual - testCase.ExpectedVoltage);
            bool passed = difference <= testCase.Tolerance;

            string message = passed
                ? $"PASS: '{testCase.TestName}' = {actual:F4}V (expected {testCase.ExpectedVoltage:F4}V ±{testCase.Tolerance:F4}V)"
                : $"FAIL: '{testCase.TestName}' = {actual:F4}V (expected {testCase.ExpectedVoltage:F4}V ±{testCase.Tolerance:F4}V, off by {difference:F4}V)";

            return new TestCaseResult(
                testCase.TestName,
                testCase.ExpectedVoltage,
                actual,
                testCase.Tolerance,
                passed,
                message
            );
        }

        private static string BuildSummary(bool allPassed, List<TestCaseResult> results)
        {
            int passCount = 0;
            foreach (var r in results)
            {
                if (r.Passed) passCount++;
            }

            var sb = new StringBuilder();
            sb.Append(allPassed ? "PASSED" : "FAILED");
            sb.Append($" ({passCount}/{results.Count} test cases)");

            foreach (var r in results)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append(r.Message);
            }

            return sb.ToString();
        }
    }
}
