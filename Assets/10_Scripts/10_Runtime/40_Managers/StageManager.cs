using System;
using System.Collections.Generic;
using System.Reflection;
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
    /// the simulation, evaluation, and scoring flow.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        private const double VoltageComparisonEpsilon = 1e-6;
        internal const string AutoOutputProbeComponentId = "__auto_output_probe__";
        private const string AutoOutputProbeDisplayName = "Output Terminal";
        private static readonly FieldInfo ComponentIdField = typeof(ComponentDefinition).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ComponentDisplayNameField = typeof(ComponentDefinition).GetField("_displayName", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ComponentKindField = typeof(ComponentDefinition).GetField("_kind", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ComponentPinsField = typeof(ComponentDefinition).GetField("_pins", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ComponentBaseCostField = typeof(ComponentDefinition).GetField("_baseCost", BindingFlags.NonPublic | BindingFlags.Instance);

        [Header("Dependencies")]
        [SerializeField, Tooltip("Game manager that owns the active board state lifecycle.")]
        private GameManager _gameManager;

        [SerializeField, Tooltip("Simulation manager responsible for circuit simulation execution.")]
        private SimulationManager _simulationManager;

        private StageDefinition _currentStage;
        private ObjectiveEvaluator _objectiveEvaluator;
        private ScoringSystem _scoringSystem;
        private DRCChecker _drcChecker;
        private ComponentDefinition _runtimeOutputProbeDefinition;

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
            var boardState = _gameManager.BoardState;
            if (boardState is null)
                return;

            bool hasFixedProbe = false;

            if (stage.FixedPlacements is not null)
            {
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
                    for (int pinIndex = 0; pinIndex < placed.Pins.Count; pinIndex++)
                    {
                        var pin = placed.Pins[pinIndex];
                        var pinWorld = placed.GetPinWorldPosition(pinIndex);
                        Debug.Log(
                            $"StageManager: Fixed pin debug comp='{fp.component.Id}' instance={placed.InstanceId} " +
                            $"pin[{pinIndex}]='{pin.PinName}' local={pin.LocalPosition} world={pinWorld}");
                    }

                    if (fp.component.Kind == ComponentKind.Probe && placed.Pins.Count > 0)
                    {
                        hasFixedProbe = true;
                        var outNet = boardState.CreateNet("OUT");
                        var pinRef = new PinReference(placed.InstanceId, 0, placed.GetPinWorldPosition(0));
                        boardState.ConnectPinToNet(outNet.NetId, pinRef);
                        Debug.Log($"StageManager: Created net 'OUT' for fixed probe {placed.InstanceId} and connected pin 0.");
                    }
                }
            }

            EnsureAutoOutputTerminal(stage, boardState, hasFixedProbe);
        }

        private void EnsureAutoOutputTerminal(StageDefinition stage, BoardState boardState, bool hasFixedProbe)
        {
            if (stage?.TestCases is not { Length: > 0 })
                return;

            if (hasFixedProbe)
                return;

            var probeDefinition = GetOrCreateRuntimeOutputProbeDefinition();
            if (probeDefinition == null)
                return;

            _simulationManager?.RegisterRuntimeComponentDefinition(probeDefinition);

            var pinInstances = PinInstanceFactory.CreatePinInstances(probeDefinition);
            if (pinInstances.Count == 0)
                return;

            var position = FindAutoOutputTerminalPosition(boardState);
            if (boardState.IsPositionOccupied(position))
            {
                Debug.LogWarning($"StageManager: Could not place auto output terminal at {position} because the cell is occupied.");
                return;
            }

            boardState.PlaceComponent(
                probeDefinition.Id,
                position,
                rotation: 0,
                pins: pinInstances,
                customValue: null,
                isFixed: true
            );
            Debug.Log($"StageManager: Auto output terminal placed at {position}.");
        }

        private ComponentDefinition GetOrCreateRuntimeOutputProbeDefinition()
        {
            if (_runtimeOutputProbeDefinition != null)
                return _runtimeOutputProbeDefinition;

            if (ComponentIdField == null || ComponentDisplayNameField == null || ComponentKindField == null || ComponentPinsField == null || ComponentBaseCostField == null)
            {
                Debug.LogError("StageManager: Failed to create runtime output probe definition due to missing reflection fields.");
                return null;
            }

            var definition = ScriptableObject.CreateInstance<ComponentDefinition>();
            definition.hideFlags = HideFlags.DontSave;
            ComponentIdField.SetValue(definition, AutoOutputProbeComponentId);
            ComponentDisplayNameField.SetValue(definition, AutoOutputProbeDisplayName);
            ComponentKindField.SetValue(definition, ComponentKind.Probe);
            ComponentPinsField.SetValue(definition, Array.Empty<PinDefinition>());
            ComponentBaseCostField.SetValue(definition, 0f);
            _runtimeOutputProbeDefinition = definition;
            return _runtimeOutputProbeDefinition;
        }

        private static GridPosition FindAutoOutputTerminalPosition(BoardState boardState)
        {
            int width = Math.Max(1, boardState.SuggestedBounds.Width);
            int height = Math.Max(1, boardState.SuggestedBounds.Height);
            int centerX = width / 2;
            int centerY = height / 2;

            var center = new GridPosition(centerX, centerY);
            if (!boardState.IsPositionOccupied(center))
                return center;

            int maxRadius = Math.Max(width, height);
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        int x = centerX + dx;
                        int y = centerY + dy;
                        if (x < 0 || x >= width || y < 0 || y >= height)
                            continue;

                        var candidate = new GridPosition(x, y);
                        if (!boardState.IsPositionOccupied(candidate))
                            return candidate;
                    }
                }
            }

            return center;
        }

        private string ResolveDynamicProbeNode(BoardState boardState)
        {
            if (boardState == null)
                return null;

            foreach (var component in boardState.Components)
            {
                if (component.ComponentDefinitionId != AutoOutputProbeComponentId)
                    continue;

                if (TryResolveProbeNetName(boardState, component, out var autoProbeNode))
                    return autoProbeNode;
            }

            foreach (var component in boardState.Components)
            {
                var definition = _simulationManager?.GetComponentDefinition(component.ComponentDefinitionId);
                if (definition == null || definition.Kind != ComponentKind.Probe)
                    continue;

                if (TryResolveProbeNetName(boardState, component, out var probeNode))
                    return probeNode;
            }

            return null;
        }

        private static bool TryResolveProbeNetName(BoardState boardState, PlacedComponent component, out string netName)
        {
            netName = null;

            if (component == null || component.Pins.Count == 0)
                return false;

            var pin = component.Pins[0];
            if (!pin.ConnectedNetId.HasValue)
                return false;

            var net = boardState.GetNet(pin.ConnectedNetId.Value);
            if (net == null || string.IsNullOrWhiteSpace(net.NetName))
                return false;

            if (string.Equals(net.NetName, "0", StringComparison.Ordinal))
                return false;

            netName = net.NetName;
            return true;
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
                Debug.Log($"StageManager: DRC completed - {drcResult}");
                OnDRCCompleted?.Invoke(drcResult);

                // 2. If shorts detected, auto-fail without running simulation
                if (drcResult.ShortCount > 0)
                {
                    Debug.LogError($"StageManager: {drcResult.ShortCount} short(s) detected - skipping simulation.");
                    foreach (var violation in drcResult.Violations)
                    {
                        if (violation.ViolationType == DRCViolationType.Short)
                            Debug.LogError($"  {violation.Message}");
                    }

                    var failedBreakdown = CreateDRCFailedBreakdown(drcResult);
                    Debug.Log($"StageManager: Stage '{_currentStage.DisplayName}' completed - {failedBreakdown.Summary}");
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

                string dynamicProbeNode = ResolveDynamicProbeNode(boardState);
                if (!string.IsNullOrEmpty(dynamicProbeNode))
                {
                    Debug.Log($"StageManager: Overriding stage probe nodes with dynamic probe net '{dynamicProbeNode}'.");
                    probes.Clear();
                    testCaseInputs.Clear();
                    foreach (var tc in _currentStage.TestCases)
                    {
                        if (tc is null)
                            continue;

                        string testName = tc.TestName;
                        if (string.IsNullOrWhiteSpace(testName))
                            continue;

                        probes.Add(ProbeDefinition.Voltage($"V_{testName}", dynamicProbeNode));
                        testCaseInputs.Add(new TestCaseInput(
                            dynamicProbeNode,
                            tc.ExpectedVoltage,
                            tc.Tolerance
                        ));
                    }
                }

                var additionalObjectiveFailures = CollectAdditionalObjectiveFailures(_currentStage, testCaseInputs, null);
                if (additionalObjectiveFailures.Count > 0)
                {
                    var failedBreakdown = CreateObjectiveFailedBreakdown(additionalObjectiveFailures);
                    Debug.LogWarning($"StageManager: Stage '{_currentStage.DisplayName}' failed objective pre-checks.");
                    OnStageCompleted?.Invoke(failedBreakdown);
                    return;
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
                // Stage clear evaluation only requires voltage probes.
                // Current probes can trigger strategy/export issues on some element types.
                await _simulationManager.RunSimulationAsync(boardState, probes, true, false, cancellationToken);
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

                additionalObjectiveFailures = CollectAdditionalObjectiveFailures(_currentStage, testCaseInputs, simResult);
                if (additionalObjectiveFailures.Count > 0)
                {
                    var failedBreakdown = CreateObjectiveFailedBreakdown(additionalObjectiveFailures);
                    Debug.Log($"StageManager: Stage '{_currentStage.DisplayName}' completed - {failedBreakdown.Summary}");
                    OnStageCompleted?.Invoke(failedBreakdown);
                    return;
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

                Debug.Log($"StageManager: Stage '{_currentStage.DisplayName}' completed - {scoreBreakdown.Summary}");
                OnStageCompleted?.Invoke(scoreBreakdown);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        internal static bool HasOutputTerminal(StageDefinition stage)
        {
            if (stage == null)
                return false;

            if (stage.FixedPlacements is not null)
            {
                foreach (var placement in stage.FixedPlacements)
                {
                    if (placement.component != null && placement.component.Kind == ComponentKind.Probe)
                        return true;
                }
            }

            if (stage.TestCases is not null)
            {
                foreach (var testCase in stage.TestCases)
                {
                    if (testCase != null && testCase.HasProbeNode)
                        return true;
                }
            }

            return false;
        }

        internal static bool TryGetInputVoltage(StageDefinition stage, out double inputVoltage)
        {
            inputVoltage = 0d;

            if (stage?.FixedPlacements is null)
                return false;

            bool foundVoltageSource = false;
            foreach (var placement in stage.FixedPlacements)
            {
                if (placement.component == null || placement.component.Kind != ComponentKind.VoltageSource)
                    continue;

                double sourceVoltage = placement.overrideCustomValue
                    ? placement.customValue
                    : placement.component.VoltageVolts;
                sourceVoltage = Math.Abs(sourceVoltage);

                if (!foundVoltageSource || sourceVoltage > inputVoltage)
                {
                    inputVoltage = sourceVoltage;
                    foundVoltageSource = true;
                }
            }

            return foundVoltageSource && inputVoltage > VoltageComparisonEpsilon;
        }

        internal static List<string> CollectAdditionalObjectiveFailures(
            StageDefinition stage,
            IReadOnlyList<TestCaseInput> testCaseInputs,
            SimulationResult simResult)
        {
            var failures = new List<string>();
            bool hasAnyTestCase = stage?.TestCases is { Length: > 0 };
            bool requiresVoltageDivision = IsVoltageDivisionStage(stage);

            if (hasAnyTestCase && !HasOutputTerminal(stage))
            {
                bool hasLegacyOutputTarget = testCaseInputs is { Count: > 0 };
                if (!hasLegacyOutputTarget)
                {
                    failures.Add("Output terminal is missing. Add a Probe component or configure ProbeNode on test cases.");
                }
            }

            if (simResult is null || !simResult.IsSuccess)
                return failures;

            if (testCaseInputs is null || testCaseInputs.Count == 0)
                return failures;

            if (!TryGetInputVoltage(stage, out var inputVoltage))
                return failures;

            foreach (var testCase in testCaseInputs)
            {
                bool expectsDividedVoltage =
                    requiresVoltageDivision ||
                    testCase.ExpectedVoltage + testCase.Tolerance < inputVoltage - VoltageComparisonEpsilon;
                if (!expectsDividedVoltage)
                    continue;

                var outputVoltage = simResult.GetVoltage(testCase.TestName);
                if (!outputVoltage.HasValue)
                    continue;

                if (outputVoltage.Value >= inputVoltage - VoltageComparisonEpsilon)
                {
                    failures.Add(
                        $"Voltage divider check failed: Vin {inputVoltage:F2}V must be greater than '{testCase.TestName}' {outputVoltage.Value:F2}V.");
                }
            }

            return failures;
        }

        internal static bool IsVoltageDivisionStage(StageDefinition stage)
        {
            if (stage == null)
                return false;

            string displayName = stage.DisplayName ?? string.Empty;
            string description = stage.CircuitDiagramDescription ?? string.Empty;
            string text = (displayName + " " + description).ToLowerInvariant();

            return text.Contains("divider")
                   || text.Contains("atten")
                   || text.Contains("분압")
                   || text.Contains("분배");
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
                summary: $"FAILED - DRC: {drcResult.ShortCount} short(s) detected"
            );
        }

        private static ScoreBreakdown CreateObjectiveFailedBreakdown(IReadOnlyList<string> failures)
        {
            var lineItems = new List<ScoreLineItem>
            {
                new ScoreLineItem("Circuit Failed", 0)
            };

            if (failures is not null)
            {
                foreach (var failure in failures)
                {
                    if (string.IsNullOrWhiteSpace(failure))
                        continue;

                    lineItems.Add(new ScoreLineItem(failure, 0));
                }
            }

            string summary = lineItems.Count > 1
                ? $"FAILED - {lineItems[1].Label}"
                : "FAILED - Objective constraints not met.";

            return new ScoreBreakdown(
                baseScore: 0,
                budgetBonus: 0,
                areaBonus: 0,
                totalScore: 0,
                stars: 0,
                passed: false,
                lineItems: lineItems,
                summary: summary
            );
        }
    }
}

