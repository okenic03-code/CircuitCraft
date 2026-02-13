using System;
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

        /// <summary>Raised after a stage is loaded and ready for play.</summary>
        public event Action OnStageLoaded;

        /// <summary>Raised when simulation + evaluation + scoring completes.</summary>
        public event Action<ScoreBreakdown> OnStageCompleted;

        /// <summary>The currently loaded stage, or null.</summary>
        public StageDefinition CurrentStage => _currentStage;

        private void Awake()
        {
            _objectiveEvaluator = new ObjectiveEvaluator();
            _scoringSystem = new ScoringSystem();
        }

        /// <summary>
        /// Loads a stage: resets the board to the stage grid size,
        /// populates the palette with allowed components, and fires OnStageLoaded.
        /// </summary>
        /// <param name="stage">The stage definition to load.</param>
        public void LoadStage(StageDefinition stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));

            _currentStage = stage;

            // Reset board to stage grid dimensions
            _gameManager.ResetBoard(stage.GridSize.x, stage.GridSize.y);

            Debug.Log($"StageManager: Loaded stage '{stage.DisplayName}' ({stage.GridSize.x}x{stage.GridSize.y})");
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

            // Run simulation and await completion
            await _simulationManager.RunSimulationAsync(boardState);
            var simResult = _simulationManager.LastSimulationResult;

            // Convert StageTestCase[] → TestCaseInput[] (domain DTO bridge)
            var stageTestCases = _currentStage.TestCases;
            var testCaseInputs = new TestCaseInput[stageTestCases != null ? stageTestCases.Length : 0];
            for (int i = 0; i < testCaseInputs.Length; i++)
            {
                var stc = stageTestCases[i];
                testCaseInputs[i] = new TestCaseInput(
                    stc.TestName,
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

            // Build scoring input from evaluation + board data + stage constraints
            int maxComponentCount = _currentStage.Constraints != null
                ? _currentStage.Constraints.MaxComponentCount
                : 0;

            var scoringInput = new ScoringInput(
                circuitPassed: evalResult.Passed,
                totalComponentCost: totalCost,
                budgetLimit: _currentStage.BudgetLimit,
                componentCount: boardState.Components.Count,
                maxComponentCount: maxComponentCount,
                traceCount: boardState.Traces.Count
            );

            // Calculate final score breakdown
            var scoreBreakdown = _scoringSystem.Calculate(scoringInput);

            Debug.Log($"StageManager: Stage '{_currentStage.DisplayName}' completed — {scoreBreakdown.Summary}");
            OnStageCompleted?.Invoke(scoreBreakdown);
        }
    }
}
