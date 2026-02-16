using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CircuitCraft.Commands;
using CircuitCraft.Controllers;
using CircuitCraft.Core;
using CircuitCraft.Data;
using CircuitCraft.Managers;
using CircuitCraft.Views;

namespace CircuitCraft.UI
{
    /// <summary>
    /// Controls the main HUD layout: Toolbar, GameView, StatusBar.
    /// Component palette, simulation button, and results panel are handled
    /// by their own dedicated controllers.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GridCursor _gridCursor;
        [SerializeField] private PlacementController _placementController;
        [SerializeField] private GridSettings _gridSettings;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private StageManager _stageManager;

        private CommandHistory _commandHistory;

        private VisualElement _root;
        private VisualElement _toolbar;
        private VisualElement _gameView;
        private VisualElement _statusBar;

        private Label _statusText;
        private Label _statusCoords;
        private Label _statusGrid;
        private Label _statusZoom;
        private Label _statusBudget;
        private Label _stageTitle;
        private Label _stageTargets;

        private Button _clearBoardButton;
        private Button _undoButton;
        private Button _redoButton;

        private Vector2Int _lastGridPos = new Vector2Int(int.MinValue, int.MinValue);
        private int _lastGridX = int.MinValue;
        private int _lastGridY = int.MinValue;
        private bool _lastGridValid = false;
        private float _lastCellSize = float.MinValue;
        private int _lastZoomPercent = int.MinValue;
        private bool _lastCanUndo = false;
        private bool _lastCanRedo = false;
        private readonly Dictionary<string, float> _componentCostLookup = new Dictionary<string, float>();
        private BoardState _subscribedBoardState;
        private float _currentBudgetLimit;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_gridCursor == null)
                _gridCursor = FindFirstObjectByType<GridCursor>();

            if (_placementController == null)
                _placementController = FindFirstObjectByType<PlacementController>();

            if (_gameManager == null)
                _gameManager = FindFirstObjectByType<GameManager>();

            if (_stageManager == null)
                _stageManager = FindFirstObjectByType<StageManager>();
        }

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                Debug.LogError("UIController: No UIDocument component found.");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("UIController: UIDocument has no root visual element.");
                return;
            }

            QueryVisualElements();

            if (_gameManager != null)
                _commandHistory = _gameManager.CommandHistory;

            RegisterCallbacks();
            RegisterDataSubscriptions();
            SetStatusText("Ready");
            OnStageLoaded();
        }

        private void OnDisable()
        {
            if (_clearBoardButton != null)
                _clearBoardButton.clicked -= OnClearBoard;

            if (_undoButton != null)
                _undoButton.clicked -= OnUndo;

            if (_redoButton != null)
                _redoButton.clicked -= OnRedo;

            if (_stageManager != null)
                _stageManager.OnStageLoaded -= OnStageLoaded;

            UnregisterBoardStateSubscriptions();
        }

        private void Update()
        {
            UpdateStatusBar();
        }

        private void QueryVisualElements()
        {
            _toolbar = _root.Q<VisualElement>("Toolbar");
            _gameView = _root.Q<VisualElement>("GameView");
            _statusBar = _root.Q<VisualElement>("StatusBar");

            _statusText = _root.Q<Label>("StatusText");
            _statusBudget = _root.Q<Label>("StatusBudget");
            _statusCoords = _root.Q<Label>("StatusCoords");
            _statusGrid = _root.Q<Label>("StatusGrid");
            _statusZoom = _root.Q<Label>("StatusZoom");
            _stageTitle = _root.Q<Label>("stage-title");
            _stageTargets = _root.Q<Label>("stage-targets");

            _clearBoardButton = _root.Q<Button>("ClearBoardButton");
            _undoButton = _root.Q<Button>("UndoButton");
            _redoButton = _root.Q<Button>("RedoButton");

            if (_statusText == null) Debug.LogWarning("UIController: StatusText not found.");
            if (_toolbar == null) Debug.LogWarning("UIController: Toolbar not found.");
            if (_gameView == null) Debug.LogWarning("UIController: GameView not found.");
            if (_statusBar == null) Debug.LogWarning("UIController: StatusBar not found.");
            if (_undoButton == null) Debug.LogWarning("UIController: UndoButton not found.");
            if (_redoButton == null) Debug.LogWarning("UIController: RedoButton not found.");
        }

        private void RegisterDataSubscriptions()
        {
            if (_stageManager != null)
                _stageManager.OnStageLoaded += OnStageLoaded;

            RebindBoardStateSubscriptions();
        }

        private void RegisterCallbacks()
        {
            if (_clearBoardButton != null)
                _clearBoardButton.clicked += OnClearBoard;

            if (_undoButton != null)
                _undoButton.clicked += OnUndo;

            if (_redoButton != null)
                _redoButton.clicked += OnRedo;
        }

        private void UpdateStatusBar()
        {
            // Grid coordinates from GridCursor
            if (_statusCoords != null && _gridCursor != null)
            {
                Vector2Int gridPos = _gridCursor.GetCurrentGridPosition();
                bool overGrid = _gridCursor.IsOverValidGrid();
                if (overGrid)
                {
                    if (!_lastGridValid || gridPos.x != _lastGridX || gridPos.y != _lastGridY)
                    {
                        _statusCoords.text = $"Pos: ({gridPos.x}, {gridPos.y})";
                        _lastGridPos = gridPos;
                        _lastGridX = gridPos.x;
                        _lastGridY = gridPos.y;
                        _lastGridValid = true;
                    }
                }
                else if (_lastGridValid)
                {
                    _statusCoords.text = "Pos: --";
                    _lastGridValid = false;
                    _lastGridX = int.MinValue;
                    _lastGridY = int.MinValue;
                    _lastGridPos = new Vector2Int(int.MinValue, int.MinValue);
                }
            }

            // Grid cell size from GridSettings
            if (_statusGrid != null && _gridSettings != null)
            {
                if (!Mathf.Approximately(_lastCellSize, _gridSettings.CellSize))
                {
                    _statusGrid.text = $"Grid: {_gridSettings.CellSize:F1} | \u221e";
                    _lastCellSize = _gridSettings.CellSize;
                }
            }

            // Zoom percentage from camera orthographic size
            if (_statusZoom != null && _mainCamera != null && _mainCamera.orthographic)
            {
                // Default ortho size = 12, treat as 100%
                float zoomPercent = (12f / _mainCamera.orthographicSize) * 100f;
                int roundedZoomPercent = Mathf.RoundToInt(zoomPercent);
                if (_lastZoomPercent != roundedZoomPercent)
                {
                    _statusZoom.text = roundedZoomPercent + "%";
                    _lastZoomPercent = roundedZoomPercent;
                }
            }

            // Undo/Redo button enabled state
            UpdateUndoRedoState();
        }

        /// <summary>
        /// Updates the status text in the bottom-left of the status bar.
        /// </summary>
        public void SetStatusText(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        public void UpdateBudget(float currentCost, float budgetLimit)
        {
            if (_statusBudget == null) return;

            if (budgetLimit <= 0f)
                _statusBudget.text = $"Cost: ${currentCost:F0}";
            else
                _statusBudget.text = $"Budget: ${currentCost:F0} / ${budgetLimit:F0}";
        }

        private void OnStageLoaded()
        {
            var stage = _stageManager?.CurrentStage;
            if (stage == null)
            {
                _componentCostLookup.Clear();
                _currentBudgetLimit = 0f;

                if (_stageTitle != null)
                    _stageTitle.text = "No Stage Loaded";

                if (_stageTargets != null)
                    _stageTargets.text = "No stage objectives.";

                RebindBoardStateSubscriptions();
                UpdateBudget(0f, 0f);
                return;
            }

            if (_stageTitle != null)
                _stageTitle.text = stage.DisplayName;

            if (_stageTargets != null)
                _stageTargets.text = BuildTargetsText(stage.TestCases);

            RebuildComponentCostLookup(stage);
            _currentBudgetLimit = stage.BudgetLimit;

            RebindBoardStateSubscriptions();
            RefreshBudgetDisplay();
        }

        private static string BuildTargetsText(StageTestCase[] testCases)
        {
            if (testCases == null || testCases.Length == 0)
                return "No stage objectives.";

            StringBuilder sb = new StringBuilder();
            foreach (var testCase in testCases)
            {
                if (testCase == null) continue;
                sb.AppendLine($"- {testCase.TestName}: {testCase.ExpectedVoltage:F2}V +/-{testCase.Tolerance:F2}V");
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd() : "No stage objectives.";
        }

        private void RebuildComponentCostLookup(StageDefinition stage)
        {
            _componentCostLookup.Clear();

            var allowedComponents = stage?.AllowedComponents;
            if (allowedComponents == null)
                return;

            foreach (var component in allowedComponents)
            {
                if (component == null || string.IsNullOrEmpty(component.Id))
                    continue;

                _componentCostLookup[component.Id] = component.BaseCost;
            }
        }

        private void RebindBoardStateSubscriptions()
        {
            var boardState = _gameManager != null ? _gameManager.BoardState : null;
            if (ReferenceEquals(_subscribedBoardState, boardState))
                return;

            UnregisterBoardStateSubscriptions();
            _subscribedBoardState = boardState;

            if (_subscribedBoardState == null)
                return;

            _subscribedBoardState.OnComponentPlaced += OnBoardComponentPlaced;
            _subscribedBoardState.OnComponentRemoved += OnBoardComponentRemoved;
        }

        private void UnregisterBoardStateSubscriptions()
        {
            if (_subscribedBoardState == null)
                return;

            _subscribedBoardState.OnComponentPlaced -= OnBoardComponentPlaced;
            _subscribedBoardState.OnComponentRemoved -= OnBoardComponentRemoved;
            _subscribedBoardState = null;
        }

        private void OnBoardComponentPlaced(PlacedComponent component)
        {
            RefreshBudgetDisplay();
        }

        private void OnBoardComponentRemoved(int instanceId)
        {
            RefreshBudgetDisplay();
        }

        private void RefreshBudgetDisplay()
        {
            float currentCost = 0f;
            if (_subscribedBoardState != null)
            {
                foreach (var component in _subscribedBoardState.Components)
                {
                    if (component != null && _componentCostLookup.TryGetValue(component.ComponentDefinitionId, out var cost))
                        currentCost += cost;
                }
            }

            UpdateBudget(currentCost, _currentBudgetLimit);
        }

        private void OnClearBoard()
        {
            if (_gameManager == null) return;
            
            // Use current stage grid size, or default
            int width = 20, height = 15;
            if (_stageManager != null && _stageManager.CurrentStage != null)
            {
                width = _stageManager.CurrentStage.GridSize.x;
                height = _stageManager.CurrentStage.GridSize.y;
            }
            
            _gameManager.ResetBoard(width, height);

            RebindBoardStateSubscriptions();
            RefreshBudgetDisplay();
            
            // Reset command history
            if (_commandHistory != null)
            {
                _commandHistory.Clear();
            }
            
            Debug.Log("UIController: Board cleared.");
            SetStatusText("Board cleared.");
        }

        private void OnUndo()
        {
            if (_commandHistory != null && _commandHistory.CanUndo)
            {
                _commandHistory.Undo();
                SetStatusText("Undo");
            }
        }

        private void OnRedo()
        {
            if (_commandHistory != null && _commandHistory.CanRedo)
            {
                _commandHistory.Redo();
                SetStatusText("Redo");
            }
        }

        private void UpdateUndoRedoState()
        {
            if (_commandHistory == null)
                return;

            bool canUndo = _commandHistory.CanUndo;
            bool canRedo = _commandHistory.CanRedo;

            if (_undoButton != null && _lastCanUndo != canUndo)
            {
                _undoButton.SetEnabled(canUndo);
                _lastCanUndo = canUndo;
            }

            if (_redoButton != null && _lastCanRedo != canRedo)
            {
                _redoButton.SetEnabled(canRedo);
                _lastCanRedo = canRedo;
            }
        }
    }
}
