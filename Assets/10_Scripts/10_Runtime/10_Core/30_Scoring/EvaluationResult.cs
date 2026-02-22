using System;
using System.Collections.Generic;
using System.Text;

namespace CircuitCraft.Core
{
    /// <summary>
    /// Pure C# DTO that mirrors the data needed from StageTestCase
    /// so the evaluator stays Unity-free. The Unity caller converts
    /// StageTestCase[] to TestCaseInput[].
    /// </summary>
    public readonly struct TestCaseInput
    {
        /// <summary>Node name used in SimulationResult.GetVoltage().</summary>
        public string TestName { get; }

        /// <summary>Expected voltage value.</summary>
        public double ExpectedVoltage { get; }

        /// <summary>Allowable error margin.</summary>
        public double Tolerance { get; }

        public TestCaseInput(string testName, double expectedVoltage, double tolerance)
        {
            TestName = testName ?? throw new ArgumentNullException(nameof(testName));
            ExpectedVoltage = expectedVoltage;
            Tolerance = tolerance;
        }
    }

    /// <summary>
    /// Result of evaluating a single test case against simulation output.
    /// </summary>
    public class TestCaseResult
    {
        /// <summary>Name of the test case / node.</summary>
        public string TestName { get; }

        /// <summary>Expected voltage value from the test case.</summary>
        public double ExpectedValue { get; }

        /// <summary>Actual voltage value from the simulation.</summary>
        public double ActualValue { get; }

        /// <summary>Allowable error margin.</summary>
        public double Tolerance { get; }

        /// <summary>Whether this individual test case passed.</summary>
        public bool Passed { get; }

        /// <summary>Human-readable message describing the result.</summary>
        public string Message { get; }

        public TestCaseResult(string testName, double expectedValue, double actualValue, double tolerance, bool passed, string message)
        {
            TestName = testName;
            ExpectedValue = expectedValue;
            ActualValue = actualValue;
            Tolerance = tolerance;
            Passed = passed;
            Message = message;
        }
    }

    /// <summary>
    /// Aggregated result of evaluating all test cases against a simulation result.
    /// </summary>
    public class EvaluationResult
    {
        /// <summary>True only if ALL test cases passed.</summary>
        public bool Passed { get; }

        /// <summary>Individual results per test case.</summary>
        public List<TestCaseResult> Results { get; }

        /// <summary>Human-readable summary of the evaluation.</summary>
        public string Summary { get; }

        public EvaluationResult(bool passed, List<TestCaseResult> results, string summary)
        {
            Passed = passed;
            Results = results ?? new();
            Summary = summary ?? string.Empty;
        }

        /// <summary>
        /// Creates a failed evaluation result for when the simulation itself failed.
        /// </summary>
        public static EvaluationResult SimulationFailed(string reason)
        {
            return new EvaluationResult(
                false,
                new(),
                $"Evaluation failed: simulation did not succeed. {reason}"
            );
        }
    }
}
