using System;
using System.Collections.Generic;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Simulation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CircuitCraft.Managers
{
    /// <summary>
    /// Bootstraps a stage from a StageDefinition.
    /// Resets the board, populates the component palette, and orchestrates
    /// the simulation → evaluation → scoring flow.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private SimulationManager _simulationManager;

        private StageDefinition _currentStage;
        private ObjectiveEvaluator _objectiveEvaluator;
        private ScoringSystem _scoringSystem;
        private DRCChecker _drcChecker;

        /// <summary>Raised after a stage is loaded and ready for play.</summary>
        public event Action OnStageLoaded;

        /// <summary>Raised when simulation + evaluation + scoring completes.</summary>
        public event Action<ScoreBreakdown> OnStageCompleted;

        /// <summary>Raised when design rule checks complete, before simulation runs.</summary>
        public event Action<DRCResult> OnDRCCompleted;

        /// <summary>The currently loaded stage, or null.</summary>
        public StageDefinition CurrentStage => _currentStage;

        private void Awake()
        {
            _objectiveEvaluator = new ObjectiveEvaluator();
            _scoringSystem = new ScoringSystem();
            _drcChecker = new DRCChecker();
        }

        /// <summary>
        /// Loads a stage: resets the board to the derived suggested area,
        /// populates the palette with allowed components, and fires OnStageLoaded.
        /// </summary>
        /// <param name="stage">The stage definition to load.</param>
        public void LoadStage(StageDefinition stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));

            _currentStage = stage;

            // Derive a square suggested board from the stage target area.
            int side = (int)Math.Ceiling(Math.Sqrt(stage.TargetArea));
            _gameManager.ResetBoard(side, side);

            Debug.Log($"StageManager: Loaded stage '{stage.DisplayName}' (suggested area {side}x{side})");
            OnStageLoaded?.Invoke();
        }

        /// <summary>
        /// Runs simulation on the current board, evaluates against stage test cases,
        /// calculates the score, and fires OnStageCompleted with the breakdown.
        /// </summary>
        public void RunSimulationAndEvaluate()
        {
            if (_currentStage == null)
            {
                Debug.LogWarning("StageManager: No stage loaded. Call LoadStage first.");
                return;
            }

            if (_simulationManager.IsSimulating)
            {
                Debug.LogWarning("StageManager: Simulation already in progress.");
                return;
            }

            RunSimulationAndEvaluateAsync().Forget();
        }

        private async UniTaskVoid RunSimulationAndEvaluateAsync()
        {
            var boardState = _gameManager.BoardState;

            // 1. Run design rule checks before simulation
            var drcResult = _drcChecker.Check(boardState);
            Debug.Log($"StageManager: DRC completed — {drcResult}");
            OnDRCCompleted?.Invoke(drcResult);

            // 2. If shorts detected, auto-fail without running simulation
            if (drcResult.ShortCount > 0)
            {
                Debug.LogError($"StageManager: {drcResult.ShortCount} short(s) detected — skipping simulation.");
                foreach (var violation in drcResult.Violations)
                {
                    if (violation.ViolationType == DRCViolationType.Short)
                        Debug.LogError($"  {violation.Message}");
                }

                var failedBreakdown = CreateDRCFailedBreakdown(drcResult);
                Debug.Log($"StageManager: Stage '{_currentStage.DisplayName}' completed — {failedBreakdown.Summary}");
                OnStageCompleted?.Invoke(failedBreakdown);
                return;
            }

            // 3. Log unconnected pin warnings (don't block simulation)
            if (drcResult.UnconnectedCount > 0)
            {
                Debug.LogWarning($"StageManager: {drcResult.UnconnectedCount} unconnected pin(s) detected.");
                foreach (var violation in drcResult.Violations)
                {
                    if (violation.ViolationType == DRCViolationType.UnconnectedPin)
                        Debug.LogWarning($"  {violation.Message}");
                }
            }

            // 4. Run simulation and await completion
            var probes = new List<ProbeDefinition>();
            if (_currentStage.TestCases != null)
            {
                foreach (var tc in _currentStage.TestCases)
                {
                    probes.Add(ProbeDefinition.Voltage($"V_{tc.TestName}", tc.ProbeNode));
                }
            }

            await _simulationManager.RunSimulationAsync(boardState, probes, true, true);
            var simResult = _simulationManager.LastSimulationResult;

            // Convert StageTestCase[] → TestCaseInput[] (domain DTO bridge)
            var stageTestCases = _currentStage.TestCases;
            var testCaseInputs = new TestCaseInput[stageTestCases != null ? stageTestCases.Length : 0];
            for (int i = 0; i < testCaseInputs.Length; i++)
            {
                var stc = stageTestCases[i];
                testCaseInputs[i] = new TestCaseInput(
                    stc.ProbeNode,
                    stc.ExpectedVoltage,
                    stc.Tolerance
                );
            }

            // Evaluate objectives against test cases
            var evalResult = _objectiveEvaluator.Evaluate(simResult, testCaseInputs);

            // Calculate total component cost by summing BaseCost of each placed component
            float totalCost = 0f;
            foreach (var component in boardState.Components)
            {
                var def = _simulationManager.GetComponentDefinition(component.ComponentDefinitionId);
                if (def != null)
                {
                    totalCost += def.BaseCost;
                }
            }

            // Build scoring input from evaluation + board size + stage target.
            var contentBounds = _gameManager.BoardState.ComputeContentBounds();
            int boardArea = Math.Max(1, contentBounds.Width * contentBounds.Height);
            int targetArea = _currentStage.TargetArea;

            var scoringInput = new ScoringInput(
                circuitPassed: evalResult.Passed,
                totalComponentCost: totalCost,
                budgetLimit: _currentStage.BudgetLimit,
                boardArea: boardArea,
                targetArea: targetArea,
                traceCount: boardState.Traces.Count
            );

            // Calculate final score breakdown
            var scoreBreakdown = _scoringSystem.Calculate(scoringInput);

            Debug.Log($"StageManager: Stage '{_currentStage.DisplayName}' completed — {scoreBreakdown.Summary}");
            OnStageCompleted?.Invoke(scoreBreakdown);
        }

        /// <summary>
        /// Creates a failed ScoreBreakdown when DRC detects shorts,
        /// including DRC violation details in the line items.
        /// </summary>
        /// <param name="drcResult">The DRC result containing short violations.</param>
        /// <returns>A zero-score, failed ScoreBreakdown.</returns>
        private static ScoreBreakdown CreateDRCFailedBreakdown(DRCResult drcResult)
        {
            var lineItems = new List<ScoreLineItem>
            {
                new ScoreLineItem($"DRC Failed: {drcResult.ShortCount} short(s)", 0)
            };

            if (drcResult.UnconnectedCount > 0)
            {
                lineItems.Add(new ScoreLineItem(
                    $"DRC Warning: {drcResult.UnconnectedCount} unconnected pin(s)", 0));
            }

            return new ScoreBreakdown(
                baseScore: 0,
                budgetBonus: 0,
                areaBonus: 0,
                totalScore: 0,
                stars: 0,
                passed: false,
                lineItems: lineItems,
                summary: $"FAILED — DRC: {drcResult.ShortCount} short(s) detected"
            );
        }
    }
}
