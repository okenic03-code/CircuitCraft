using System;
using System.Collections.Generic;
using System.Threading;
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
        [SerializeField, Tooltip("Game manager that owns the active board state lifecycle.")]
        private GameManager _gameManager;

        [SerializeField, Tooltip("Simulation manager responsible for circuit simulation execution.")]
        private SimulationManager _simulationManager;

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
            _objectiveEvaluator = new();
            _scoringSystem = new();
            _drcChecker = new();
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
            PlaceFixedComponents(stage);

        }

        /// <summary>
        /// Places stage-defined fixed components after board reset and view subscriptions.
        /// Probe components also create and connect to an OUT net.
        /// </summary>
        /// <param name="stage">The stage containing fixed placement definitions.</param>
        private void PlaceFixedComponents(StageDefinition stage)
        {
            if (stage.FixedPlacements is null || stage.FixedPlacements.Length == 0)
                return;

            var boardState = _gameManager.BoardState;

            for (int i = 0; i < stage.FixedPlacements.Length; i++)
            {
                var fp = stage.FixedPlacements[i];
                if (fp.component == null)
                {
                    Debug.LogWarning($"StageManager: Fixed placement at index {i} has a null component. Skipping.");
                    continue;
                }

                var pinInstances = PinInstanceFactory.CreatePinInstances(fp.component);
                var position = new GridPosition(fp.position.x, fp.position.y);
                float? customValue = fp.overrideCustomValue ? fp.customValue : (float?)null;
                var placed = boardState.PlaceComponent(fp.component.Id, position, fp.rotation, pinInstances, customValue, isFixed: true);

                Debug.Log($"StageManager: Placed fixed component '{fp.component.Id}' at {position} with rotation {fp.rotation}.");

                if (fp.component.Kind == ComponentKind.Probe && placed.Pins.Count > 0)
                {
                    var outNet = boardState.CreateNet("OUT");
                    var pinRef = new PinReference(placed.InstanceId, 0, placed.GetPinWorldPosition(0));
                    boardState.ConnectPinToNet(outNet.NetId, pinRef);
                    Debug.Log($"StageManager: Created net 'OUT' for fixed probe {placed.InstanceId} and connected pin 0.");
                }
            }
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

            var cancellationToken = this.GetCancellationTokenOnDestroy();
            RunSimulationAndEvaluateAsync(cancellationToken).Forget();
        }

        private async UniTaskVoid RunSimulationAndEvaluateAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                List<ProbeDefinition> probes = new();
                List<TestCaseInput> testCaseInputs = new();
                if (_currentStage.TestCases is not null)
                {
                    foreach (var tc in _currentStage.TestCases)
                    {
                        if (tc is null)
                        {
                            Debug.LogWarning($"StageManager: Stage '{_currentStage.DisplayName}' contains a null test case entry.");
                            continue;
                        }

                        string probeNode = tc.HasProbeNode ? tc.ProbeNode : tc.TestName;
                        if (string.IsNullOrWhiteSpace(probeNode))
                        {
                            Debug.LogWarning(
                                "StageManager: Test case has neither ProbeNode nor TestName configured. " +
                                "Skipping probe and evaluation for this test case.");
                            continue;
                        }

                        if (!tc.HasProbeNode)
                        {
                            Debug.Log(
                                $"StageManager: Test case '{tc.TestName}' has no ProbeNode configured. " +
                                $"Using TestName '{probeNode}' as fallback probe node.");
                        }

                        probes.Add(ProbeDefinition.Voltage($"V_{tc.TestName}", probeNode));
                        testCaseInputs.Add(new TestCaseInput(
                            probeNode,
                            tc.ExpectedVoltage,
                            tc.Tolerance
                        ));
                    }
                }

                // Snapshot scoring inputs before awaiting simulation to avoid race with live board edits.
                float totalCost = 0f;
                foreach (var component in boardState.Components)
                {
                    if (component.IsFixed) continue;
                    var def = _simulationManager.GetComponentDefinition(component.ComponentDefinitionId);
                    if (def != null)
                    {
                        totalCost += def.BaseCost;
                    }
                }

                var contentBounds = boardState.ComputeContentBounds();
                int boardArea = Math.Max(1, contentBounds.Width * contentBounds.Height);
                int traceCount = boardState.Traces.Count;

                cancellationToken.ThrowIfCancellationRequested();
                await _simulationManager.RunSimulationAsync(boardState, probes, true, true, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var simResult = _simulationManager.LastSimulationResult;

                EvaluationResult evalResult;
                if (testCaseInputs.Count == 0)
                {
                    Debug.LogWarning(
                        $"StageManager: Stage '{_currentStage.DisplayName}' has no test cases with a valid ProbeNode. " +
                        "Marking objective evaluation as failed.");
                    evalResult = new EvaluationResult(
                        false,
                        new(),
                        "FAILED (0/0 test cases) - No valid ProbeNode configured."
                    );
                }
                else
                {
                    // Evaluate objectives against configured test cases
                    evalResult = _objectiveEvaluator.Evaluate(simResult, testCaseInputs.ToArray());
                }

                // Build scoring input from evaluation + board size + stage target.
                int targetArea = _currentStage.TargetArea;

                var scoringInput = new ScoringInput(
                    circuitPassed: evalResult.Passed,
                    totalComponentCost: totalCost,
                    budgetLimit: _currentStage.BudgetLimit,
                    boardArea: boardArea,
                    targetArea: targetArea,
                    traceCount: traceCount
                );

                // Calculate final score breakdown
                var scoreBreakdown = _scoringSystem.Calculate(scoringInput);

                Debug.Log($"StageManager: Stage '{_currentStage.DisplayName}' completed — {scoreBreakdown.Summary}");
                OnStageCompleted?.Invoke(scoreBreakdown);
            }
            catch (OperationCanceledException)
            {
                return;
            }
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
