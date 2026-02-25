using System.Collections.Generic;
using System.Text;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using UnityEngine.UIElements;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Tracks component placement costs against the current stage budget limit.
    /// </summary>
    public class BudgetTracker
    {
        private readonly Label _statusBudget;
        private readonly Label _stageTitle;
        private readonly Label _stageTargets;
        private readonly GameManager _gameManager;
        private readonly StageManager _stageManager;
        private readonly Dictionary<string, float> _componentCostLookup = new();
        private BoardState _subscribedBoardState;
        private float _currentBudgetLimit;

        /// <summary>
        /// Creates a budget tracker bound to stage labels and manager data sources.
        /// </summary>
        public BudgetTracker(
            Label statusBudget,
            Label stageTitle,
            Label stageTargets,
            GameManager gameManager,
            StageManager stageManager)
        {
            _statusBudget = statusBudget;
            _stageTitle = stageTitle;
            _stageTargets = stageTargets;
            _gameManager = gameManager;
            _stageManager = stageManager;
        }
        /// <summary>
        /// Refreshes budget context and stage labels after a stage load.
        /// </summary>
        public void HandleStageLoaded()
        {
            StageDefinition stage = _stageManager != null ? _stageManager.CurrentStage : null;
            if (stage == null)
            {
                _componentCostLookup.Clear();
                _currentBudgetLimit = 0f;
                if (_stageTitle is not null)
                    _stageTitle.text = "No Stage Loaded";
                if (_stageTargets is not null)
                    _stageTargets.text = "No stage objectives.";
                RebindBoardState();
                UpdateBudget(0f, 0f);
                return;
            }
            if (_stageTitle is not null)
                _stageTitle.text = stage.DisplayName;
            if (_stageTargets is not null)
                _stageTargets.text = BuildTargetsText(stage.TestCases);
            RebuildComponentCostLookup(stage);
            _currentBudgetLimit = stage.BudgetLimit;
            RebindBoardState();
            RefreshBudgetDisplay();
        }

        /// <summary>
        /// Updates the budget status label with the current and maximum budget values.
        /// </summary>
        public void UpdateBudget(float currentCost, float budgetLimit)
        {
            if (_statusBudget is null)
                return;
            if (budgetLimit <= 0f)
                _statusBudget.text = $"Cost: ${currentCost:F0}";
            else
                _statusBudget.text = $"Budget: ${currentCost:F0} / ${budgetLimit:F0}";
        }

        /// <summary>
        /// Re-subscribes to the current board state so budget updates track board mutations.
        /// </summary>
        public void RebindBoardState()
        {
            BoardState boardState = _gameManager != null ? _gameManager.BoardState : null;
            if (ReferenceEquals(_subscribedBoardState, boardState))
                return;
            UnregisterBoardStateSubscriptions();
            _subscribedBoardState = boardState;
            if (_subscribedBoardState is null)
                return;
            _subscribedBoardState.OnComponentPlaced += OnBoardComponentPlaced;
            _subscribedBoardState.OnComponentRemoved += OnBoardComponentRemoved;
        }

        /// <summary>
        /// Releases board state subscriptions held by this tracker.
        /// </summary>
        public void Dispose() => UnregisterBoardStateSubscriptions();

        private static string BuildTargetsText(StageTestCase[] testCases)
        {
            if (testCases is null || testCases.Length == 0)
                return "No stage objectives.";
            StringBuilder sb = new();
            foreach (StageTestCase testCase in testCases)
            {
                if (testCase is null)
                    continue;
                sb.AppendLine($"- {testCase.TestName}: {testCase.ExpectedVoltage:F2}V +/-{testCase.Tolerance:F2}V");
            }
            return sb.Length > 0 ? sb.ToString().TrimEnd() : "No stage objectives.";
        }
        private void RebuildComponentCostLookup(StageDefinition stage)
        {
            _componentCostLookup.Clear();
            ComponentDefinition[] allowedComponents = stage != null ? stage.AllowedComponents : null;
            if (allowedComponents is null)
                return;
            foreach (ComponentDefinition component in allowedComponents)
            {
                if (component == null || string.IsNullOrEmpty(component.Id))
                    continue;
                _componentCostLookup[component.Id] = component.BaseCost;
            }
        }
        private void UnregisterBoardStateSubscriptions()
        {
            if (_subscribedBoardState is null)
                return;
            _subscribedBoardState.OnComponentPlaced -= OnBoardComponentPlaced;
            _subscribedBoardState.OnComponentRemoved -= OnBoardComponentRemoved;
            _subscribedBoardState = null;
        }
        private void OnBoardComponentPlaced(PlacedComponent component) => RefreshBudgetDisplay();
        private void OnBoardComponentRemoved(int instanceId) => RefreshBudgetDisplay();

        private void RefreshBudgetDisplay()
        {
            float currentCost = 0f;
            if (_subscribedBoardState is not null)
            {
                foreach (PlacedComponent component in _subscribedBoardState.Components)
                {
                    if (component is not null && _componentCostLookup.TryGetValue(component.ComponentDefinitionId, out float cost))
                        currentCost += cost;
                }
            }
            UpdateBudget(currentCost, _currentBudgetLimit);
        }
    }
}
